using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.FactionAgentService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.TradeService;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Faction agents (plan section 29): NPCs at the docks who hand out repeatable space missions from a small daily
    /// board and pay credits, faction standing and faction tokens, which the faction's token store takes. Missions are
    /// runtime quests built from the terms the player holds (<see cref="Player.AgentMissions"/>); the board's rules and
    /// numbers are <see cref="FactionAgentRules"/> and <see cref="FactionAgentTuning"/>.
    /// </summary>
    public static partial class FactionAgents
    {
        /// <summary>The creature local that makes an NPC a faction agent: a <see cref="FactionAgentType"/> value.</summary>
        public const string AgentVariable = "FACTION_AGENT_ID";

        /// <summary>
        /// The character-file local naming the last mission whose credits were paid. It is saved with the credits, so a
        /// pay retried after a restart knows whether they reached the character.
        /// </summary>
        private const string PaidCreditsVariable = "AGENT_MISSION_CREDITS_PAID";

        /// <summary>The agent an NPC is, or null.</summary>
        public static FactionAgentDetail GetAgent(uint npc)
        {
            return FactionAgentRules.GetAgent((FactionAgentType)GetLocalInt(npc, AgentVariable));
        }

        /// <summary>The missions the player holds.</summary>
        public static List<AgentMission> GetMissions(uint player)
        {
            return DB.Get<Player>(GetObjectUUID(player))?.AgentMissions ?? new List<AgentMission>();
        }

        public static int GetIntel(uint player)
        {
            return DB.Get<Player>(GetObjectUUID(player))?.AgentIntel ?? 0;
        }

        public static int GetTokens(uint player, FactionAgentDetail agent)
        {
            return Currency.GetCurrency(player, FactionAgentRules.GetTokenCurrency(agent.Faction));
        }

        /// <summary>
        /// A held mission's quest is registered at login, before the quest service checks the journal, so the player keeps
        /// it over a restart. A mission whose quest record is gone, or whose quest completed and was paid, is cleaned up
        /// (its courier load, lead or convoy) and dropped; one whose quest completed but was not paid (a restart in
        /// between) is paid now, then dropped.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void RegisterHeldMissions()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            if (dbPlayer == null || dbPlayer.AgentMissions.Count == 0)
                return;

            var unpaid = dbPlayer.AgentMissions
                .Where(x => !x.Paid && dbPlayer.Quests.TryGetValue(x.Id, out var quest) && quest.DateLastCompleted != null)
                .Select(x => x.Id)
                .ToList();
            var stale = dbPlayer.AgentMissions
                .Where(x => !unpaid.Contains(x.Id) && (!dbPlayer.Quests.TryGetValue(x.Id, out var quest) || quest.DateLastCompleted != null))
                .ToList();
            if (stale.Count > 0)
            {
                var playerId = GetObjectUUID(player);
                foreach (var mission in stale)
                    EndMission(playerId, mission);

                // Reloaded: ending a courier saves the record.
                dbPlayer = DB.Get<Player>(playerId);
                dbPlayer.AgentMissions.RemoveAll(x => stale.Any(s => s.Id == x.Id));
                DB.Set(dbPlayer);
            }

            foreach (var missionId in unpaid)
            {
                DelayCommand(6f, () =>
                {
                    if (!GetIsObjectValid(player))
                        return;

                    PayMission(player, missionId);
                    OnMissionCompleted(player, missionId);
                });
            }

            foreach (var mission in dbPlayer.AgentMissions.Where(x => !unpaid.Contains(x.Id)))
                Quest.RegisterRuntimeQuest(AgentMissionQuest.Build(mission));
        }

        // The board

        /// <summary>Why the player cannot take an offer now, or None.</summary>
        public static AgentRefusal GetRefusal(uint player, FactionAgentDetail agent, AgentMission offer)
        {
            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            if (dbPlayer == null)
                return AgentRefusal.OfferGone;

            var refusal = FactionAgentRules.CanAccept(offer, Trade.GetStanding(player, agent.Faction), dbPlayer.AgentMissions,
                dbPlayer.AgentOffersTaken, dbPlayer.AgentIntel);
            if (refusal != AgentRefusal.None || offer.Type != AgentMissionType.SealedCourier)
                return refusal;

            return Trade.HasCourierRoom(player, agent.DockId, offer.Volume, out var noShip)
                ? AgentRefusal.None
                : noShip ? AgentRefusal.NoShipDocked : AgentRefusal.NotEnoughRoom;
        }

        /// <summary>Takes an offer from the agent's board: its quest is accepted, and a courier run's load goes aboard.</summary>
        public static void Accept(uint player, uint agentNpc, string offerId)
        {
            var agent = GetAgent(agentNpc);
            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            if (agent == null || dbPlayer == null)
                return;

            var now = DateTime.UtcNow;
            FactionAgentRules.PruneTaken(dbPlayer.AgentOffersTaken, now);
            DB.Set(dbPlayer);
            var offer = FactionAgentRules.GetBoard(agent.Type, now).FirstOrDefault(x => x.Id == offerId);
            var refusal = GetRefusal(player, agent, offer);
            if (refusal != AgentRefusal.None)
            {
                SendMessageToPC(player, ColorToken.Red(FactionAgentRules.GetRefusalMessage(refusal)));
                return;
            }

            var mission = offer.Copy();
            mission.AcceptedAt = now;
            Quest.RegisterRuntimeQuest(AgentMissionQuest.Build(mission));

            dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            dbPlayer.AgentMissions.Add(mission);
            dbPlayer.AgentOffersTaken.Add(mission.Id);
            if (mission.Slot == AgentMissionSlot.Intel)
                dbPlayer.AgentIntel = Math.Max(0, dbPlayer.AgentIntel - 1);
            DB.Set(dbPlayer);

            if (!Quest.AcceptQuest(player, agentNpc, mission.Id))
            {
                dbPlayer = DB.Get<Player>(GetObjectUUID(player));
                dbPlayer.AgentMissions.RemoveAll(x => x.Id == mission.Id);
                dbPlayer.AgentOffersTaken.Remove(mission.Id);
                if (mission.Slot == AgentMissionSlot.Intel)
                    dbPlayer.AgentIntel++;
                DB.Set(dbPlayer);
                return;
            }

            if (mission.Type == AgentMissionType.SealedCourier)
                LoadCourier(player, agent, mission);

            SendMessageToPC(player, ColorToken.Green($"{agent.Name}: {FactionAgentRules.Describe(mission)}"));
            Log.Write(LogGroup.Space, $"AGENT ACCEPT - {PlayerName.GetAuditName(player)} - {mission.Id} {mission.Type} T{mission.Tier} " +
                                      $"{mission.OrbitResref} x{mission.Quantity} - {mission.Credits} credits, {mission.Tokens} tokens");
        }

        private static void LoadCourier(uint player, FactionAgentDetail agent, AgentMission mission)
        {
            var shipId = Trade.LoadAgentCourier(player, agent.DockId, agent.Exchange, mission.Id, mission.Destination, mission.Volume,
                mission.DeadlineMinutes);
            if (string.IsNullOrEmpty(shipId))
            {
                // The ship was checked a moment ago; if it no longer fits, the run cannot start.
                SendMessageToPC(player, ColorToken.Red(FactionAgentRules.GetRefusalMessage(AgentRefusal.NotEnoughRoom)));
                Abandon(player, mission.Id);
                return;
            }

            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            var held = dbPlayer.AgentMissions.FirstOrDefault(x => x.Id == mission.Id);
            if (held != null)
            {
                held.ShipId = shipId;
                DB.Set(dbPlayer);
            }
        }

        /// <summary>Gives a mission up: its quest is abandoned, which also unloads a courier run and clears a lead.</summary>
        public static void Abandon(uint player, string missionId)
        {
            if (Quest.GetQuestByIdOrDefault(missionId) != null)
                Quest.AbandonQuest(player, missionId);

            // A quest record already gone runs no abandon actions; this clears the mission either way.
            OnMissionAbandoned(player, missionId);
        }

        /// <summary>A mining quota's hand-in: the agent takes the ore.</summary>
        public static void HandIn(uint player, uint agentNpc, string missionId)
        {
            Quest.RequestItemsFromPlayer(player, agentNpc, missionId);
        }

        // Quest callbacks

        /// <summary>The mission's quest was abandoned (here, in the journal, or by a failed mission): it is gone, with what it set up.</summary>
        public static void OnMissionAbandoned(uint player, string missionId)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var mission = dbPlayer?.AgentMissions.FirstOrDefault(x => x.Id == missionId);
            if (mission == null)
                return;

            // What the mission set up is cleared before the mission goes, so a restart in between leaves a mission the
            // next login cleans up, never a courier load with no mission.
            EndMission(playerId, mission);
            dbPlayer = DB.Get<Player>(playerId);
            dbPlayer.AgentMissions.RemoveAll(x => x.Id == missionId);
            DB.Set(dbPlayer);
            Log.Write(LogGroup.Space, $"AGENT ABANDON - {PlayerName.GetAuditName(player)} - {mission.Id} {mission.Type}");
        }

        /// <summary>The mission's quest completed (and paid): it leaves the player's record and quest log.</summary>
        public static void OnMissionCompleted(uint player, string missionId)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            if (dbPlayer == null)
                return;

            // What the mission set up is cleared before the mission goes, so a restart in between leaves a mission the
            // next login cleans up, never a courier load with no mission.
            var mission = dbPlayer.AgentMissions.FirstOrDefault(x => x.Id == missionId);
            if (mission != null)
            {
                EndMission(playerId, mission);
                dbPlayer = DB.Get<Player>(playerId);
            }

            dbPlayer.AgentMissions.RemoveAll(x => x.Id == missionId);
            // A board offer can be taken once (AgentOffersTaken); its finished quest record would only pile up.
            dbPlayer.Quests.Remove(missionId);
            DB.Set(dbPlayer);
        }

        /// <summary>Pays a completed mission's terms: credits, standing with the agent's faction and its tokens.</summary>
        public static void PayMission(uint player, string missionId)
        {
            var mission = GetMissions(player).FirstOrDefault(x => x.Id == missionId);
            if (mission == null || mission.Paid)
                return;

            // Each part is saved with its mark in one write, so a pay retried after a restart (at the next login) gives
            // only the parts not yet saved. Standing and tokens are marked in the player record they are saved in; the
            // credits are marked in the character file they are saved in.
            if (!mission.PaidStanding)
            {
                if (mission.Standing != 0)
                    Faction.AdjustPlayerFactionStanding(player, mission.Faction, mission.Standing, MarkHeld(missionId, x => x.PaidStanding = true));
                else
                    MarkPaid(player, missionId, x => x.PaidStanding = true);
            }

            var currency = FactionAgentRules.GetTokenCurrency(mission.Faction);
            if (!mission.PaidTokens)
            {
                if (currency != CurrencyService.CurrencyType.Invalid && mission.Tokens > 0)
                    Currency.GiveCurrency(player, currency, mission.Tokens, MarkHeld(missionId, x => x.PaidTokens = true));
                else
                    MarkPaid(player, missionId, x => x.PaidTokens = true);
            }

            if (!mission.PaidCredits)
            {
                if (ObjectPlugin.GetString(player, PaidCreditsVariable) != missionId)
                {
                    GiveGoldToCreature(player, mission.Credits);
                    ObjectPlugin.SetString(player, PaidCreditsVariable, missionId, true);
                    ExportSingleCharacter(player);
                }

                MarkPaid(player, missionId, x => x.PaidCredits = true);
            }

            MarkPaid(player, missionId, x => x.Paid = true);

            var agent = FactionAgentRules.GetAgent(mission.Agent);
            SendMessageToPC(player, ColorToken.Green($"{agent?.Name ?? "The agent"} pays you {FactionAgentRules.DescribeReward(mission)}."));
            Log.Write(LogGroup.Space, $"AGENT PAY - {PlayerName.GetAuditName(player)} - {mission.Id} {mission.Type} T{mission.Tier} - " +
                                      $"{mission.Credits} credits, {mission.Standing} standing, {mission.Tokens} {currency}");
        }

        /// <summary>Saves a mark of payment on the player's held mission.</summary>
        private static void MarkPaid(uint player, string missionId, Action<AgentMission> mark)
        {
            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            if (dbPlayer == null)
                return;

            MarkHeld(missionId, mark)(dbPlayer);
            DB.Set(dbPlayer);
        }

        /// <summary>Marks the player's held mission in a record about to be saved.</summary>
        private static Action<Player> MarkHeld(string missionId, Action<AgentMission> mark) => dbPlayer =>
        {
            var held = dbPlayer.AgentMissions.FirstOrDefault(x => x.Id == missionId);
            if (held != null)
                mark(held);
        };

        /// <summary>Ends a mission the player could not finish: it is abandoned with the reason.</summary>
        private static void FailMission(uint player, string missionId, string reason)
        {
            SendMessageToPC(player, ColorToken.Red(reason));
            Abandon(player, missionId);
        }

        /// <summary>Clears what an ended mission left in the world: a courier load, a lead's signature, an escort convoy.</summary>
        private static void EndMission(string playerId, AgentMission mission)
        {
            switch (mission.Type)
            {
                case AgentMissionType.SealedCourier:
                    Trade.UnloadAgentCourier(playerId, mission.Id);
                    break;
                case AgentMissionType.ScanDown:
                case AgentMissionType.DataHeist:
                    RemoveLead(playerId, mission.Id);
                    break;
                case AgentMissionType.ConvoyEscort:
                    _convoys.Remove((playerId, mission.Id));
                    break;
            }
        }

        /// <summary>Credits a held mission's counter (its objective); a finished counter completes and pays the mission.</summary>
        private static void Credit(uint player, string missionId, uint source)
        {
            Quest.RecordCombatObjective(player, FactionAgentRules.ObjectiveKey(missionId), source);
        }

        // Agent intel

        /// <summary>Agent intel from a sliced data can: one intel lead on any faction agent's board.</summary>
        public static void GrantIntel(uint player)
        {
            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            if (dbPlayer == null)
                return;

            dbPlayer.AgentIntel++;
            DB.Set(dbPlayer);
            SendMessageToPC(player, ColorToken.Green($"You recover agent intel ({dbPlayer.AgentIntel} held). Any faction agent can turn it into an intel lead."));
        }

        // Faction token stores

        /// <summary>Buys a store item with the agent's faction tokens.</summary>
        public static void Buy(uint player, uint agentNpc, int itemIndex)
        {
            var agent = GetAgent(agentNpc);
            if (agent == null)
                return;

            var items = FactionAgentRules.GetStoreItems(agent.Faction);
            var item = itemIndex >= 0 && itemIndex < items.Count ? items[itemIndex] : null;
            var currency = FactionAgentRules.GetTokenCurrency(agent.Faction);
            var tokens = Currency.GetCurrency(player, currency);
            var refusal = FactionAgentRules.CanBuy(item, TradeStandingRules.GetTier(Trade.GetStanding(player, agent.Faction)), tokens);
            if (refusal != AgentRefusal.None)
            {
                SendMessageToPC(player, ColorToken.Red(FactionAgentRules.GetRefusalMessage(refusal)));
                return;
            }

            // The item is created before the tokens are taken: a full inventory refuses the purchase without spending them.
            var created = CreateItemOnObject(item.Resref, player, item.Quantity);
            if (!GetIsObjectValid(created))
            {
                SendMessageToPC(player, ColorToken.Red(Item.InventoryFullMessage));
                return;
            }

            if (item.Trait != SpaceService.ShipComponentTraitType.None)
                ShipComponents.SetTraits(created, new[] { item.Trait });
            Currency.TakeCurrency(player, currency, item.Tokens);

            var name = GetStoreItemName(item);
            SendMessageToPC(player, ColorToken.Green($"You requisition {item.Quantity}x {name} for {item.Tokens} {FactionAgentRules.GetTokenName(agent.Faction)}."));
            Log.Write(LogGroup.Space, $"AGENT STORE - {PlayerName.GetAuditName(player)} - {agent.Type} - {item.Quantity}x {item.Resref} for {item.Tokens} {currency}");
        }

        public static string GetStoreItemName(FactionTokenStoreItem item)
        {
            return string.IsNullOrEmpty(item.Name) ? Cache.GetItemNameByResref(item.Resref) : item.Name;
        }

        private static uint FindOnlinePlayer(string playerId)
        {
            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (GetObjectUUID(player) == playerId)
                    return player;
            }

            return OBJECT_INVALID;
        }
    }
}
