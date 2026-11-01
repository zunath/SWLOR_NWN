using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.ShipModuleDefinition;
using SWLOR.Game.Server.Service.CurrencyService;
using SWLOR.Game.Server.Service.FactionAgentService;
using SWLOR.Game.Server.Service.FactionService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SlicingService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.TradeService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Faction agents (plan section 29) in the engine: a board offer is taken, held, credited and paid through the real
    /// quest pipeline, the board's gates hold against real player records, and each mission type's world side (kills in
    /// the system, the scan-down lead, the data heist, the courier load, the escort convoy) credits the holder. The token
    /// store spends real token currency. Headless players stand in for the pilots.
    /// </summary>
    public static class FactionAgentEngineTests
    {
        private const string Category = "FactionAgents";
        private const string ShipTag = "sd_striker";
        private const string FreighterTag = "sd_xsfreighter";
        private const string ConvoyMissionVariable = "AGENT_CONVOY_MISSION";
        private const string HeistMissionVariable = "AGENT_HEIST_MISSION";
        private const int TrustedStanding = 250;

        [EngineTest("An offer on an agent's board is taken, counted, paid and cleared from the record", Category = Category, TimeoutSeconds = 60f)]
        public static async Task OfferLifecycle(EngineTestContext ctx)
        {
            using var pilot = await PlayerAbilityFixture.CreateAsync(ctx);
            var (agent, offer) = FindOffer(x => x.Slot == AgentMissionSlot.Standard &&
                                                x.Type is AgentMissionType.Kill or AgentMissionType.ConvoyEscort or AgentMissionType.ScanDown);
            ctx.Assert(agent != null, "some agent's board offers a counted mission today");
            var npc = SpawnAgent(ctx, agent.Type);
            ctx.AssertEqual(agent.Type, FactionAgents.GetAgent(npc).Type, "the NPC is the agent its local names");

            var gold = GetGold(pilot.Creature);
            var standing = Trade.GetStanding(pilot.Creature, agent.Faction);
            var tokens = FactionAgents.GetTokens(pilot.Creature, agent);

            try
            {
                await ctx.ExecuteInCreatureContextAsync(pilot.Creature, () => FactionAgents.Accept(pilot.Creature, npc, offer.Id));

                var record = DB.Get<Player>(pilot.Id);
                ctx.AssertEqual(1, record.AgentMissions.Count, "the mission is on the player's record");
                ctx.Assert(record.AgentOffersTaken.Contains(offer.Id), "the offer is marked taken");
                ctx.Assert(record.Quests.TryGetValue(offer.Id, out var progress) && progress.CurrentState == 1, "its runtime quest is accepted");
                ctx.Assert(Quest.GetQuestByIdOrDefault(offer.Id) != null, "its quest is registered");

                var saved = JsonConvert.DeserializeObject<Player>(DB.GetRawJson<Player>(pilot.Id)).AgentMissions.Single();
                ctx.AssertEqual(offer.Credits, saved.Credits, "the saved mission keeps its credits");
                ctx.AssertEqual(offer.Tokens, saved.Tokens, "the saved mission keeps its tokens");
                ctx.AssertEqual(offer.Type, saved.Type, "the saved mission keeps its type");
                ctx.AssertEqual(offer.OrbitResref, saved.OrbitResref, "the saved mission keeps its system");
                ctx.Assert(saved.AcceptedAt != null, "the saved mission keeps when it was accepted");

                ctx.AssertEqual(AgentRefusal.AlreadyTaken, FactionAgents.GetRefusal(pilot.Creature, agent, offer), "the same offer cannot be taken again");

                for (var count = 1; count <= offer.Quantity; count++)
                {
                    await ctx.ExecuteInCreatureContextAsync(pilot.Creature,
                        () => Quest.RecordCombatObjective(pilot.Creature, FactionAgentRules.ObjectiveKey(offer.Id), npc));
                    if (count < offer.Quantity)
                    {
                        ctx.AssertEqual(count, DB.Get<Player>(pilot.Id).Quests[offer.Id].CombatProgresses[FactionAgentRules.ObjectiveKey(offer.Id)],
                            "the counter advances one at a time");
                        ctx.AssertEqual(gold, GetGold(pilot.Creature), "nothing is paid before the mission is done");
                    }
                }

                await ctx.WaitUntilAsync(() => DB.Get<Player>(pilot.Id).AgentMissions.Count == 0, 10f, "the finished mission to leave the record");
                record = DB.Get<Player>(pilot.Id);
                ctx.Assert(!record.Quests.ContainsKey(offer.Id), "its finished quest record is removed");
                ctx.Assert(record.AgentOffersTaken.Contains(offer.Id), "the offer stays taken");
                ctx.AssertEqual(gold + offer.Credits, GetGold(pilot.Creature), "the agent pays its credits");
                ctx.AssertEqual(standing + offer.Standing, Trade.GetStanding(pilot.Creature, agent.Faction), "the agent's faction standing rises");
                ctx.AssertEqual(tokens + offer.Tokens, FactionAgents.GetTokens(pilot.Creature, agent), "the agent pays its faction tokens");
                ctx.AssertEqual(AgentRefusal.AlreadyTaken, FactionAgents.GetRefusal(pilot.Creature, agent, offer), "a finished offer cannot be retaken");
            }
            finally
            {
                Quest.UnregisterRuntimeQuest(offer.Id);
            }
        }

        [EngineTest("The board's gates hold against the player's record: standing, intel, the three-mission cap and taken offers", Category = Category, TimeoutSeconds = 60f)]
        public static async Task BoardGates(EngineTestContext ctx)
        {
            using var pilot = await PlayerAbilityFixture.CreateAsync(ctx);
            var now = DateTime.UtcNow;
            // A board whose standard and priority offers need no ship, so all three can be taken here.
            var agent = FactionAgentTuning.Agents.FirstOrDefault(a => FactionAgentRules.GetBoard(a.Type, now)
                .Where(x => x.Slot != AgentMissionSlot.Intel).All(x => x.Type != AgentMissionType.SealedCourier));
            ctx.Assert(agent != null, "some agent's board has no courier run among its standard and priority offers today");
            var board = FactionAgentRules.GetBoard(agent.Type, now);
            var standard = board.Where(x => x.Slot == AgentMissionSlot.Standard).ToList();
            var priority = board.Single(x => x.Slot == AgentMissionSlot.Priority);
            var intel = board.Single(x => x.Slot == AgentMissionSlot.Intel);
            var npc = SpawnAgent(ctx, agent.Type);
            var ids = board.Select(x => x.Id).ToList();

            void SetStanding(int value) => pilot.Update(r => r.Factions[agent.Faction] = new PlayerFactionStanding { Standing = value });

            try
            {
                SetStanding(-600);
                ctx.AssertEqual(AgentRefusal.Hostile, FactionAgents.GetRefusal(pilot.Creature, agent, standard[0]), "a hostile pilot gets no missions");
                await ctx.ExecuteInCreatureContextAsync(pilot.Creature, () => FactionAgents.Accept(pilot.Creature, npc, standard[0].Id));
                ctx.AssertEqual(0, DB.Get<Player>(pilot.Id).AgentMissions.Count, "a refused accept leaves no mission");
                ctx.AssertEqual(0, DB.Get<Player>(pilot.Id).AgentOffersTaken.Count, "a refused accept takes nothing");

                SetStanding(0);
                ctx.AssertEqual(AgentRefusal.StandingTooLow, FactionAgents.GetRefusal(pilot.Creature, agent, priority), "a priority offer needs Trusted standing");
                ctx.AssertEqual(AgentRefusal.NoIntel, FactionAgents.GetRefusal(pilot.Creature, agent, intel), "an intel lead needs agent intel");
                ctx.AssertEqual(AgentRefusal.OfferGone, FactionAgents.GetRefusal(pilot.Creature, agent, null), "an offer that is not on the board is gone");

                SetStanding(TrustedStanding);
                pilot.Update(r => r.AgentIntel = 1);
                ctx.AssertEqual(AgentRefusal.None, FactionAgents.GetRefusal(pilot.Creature, agent, priority), "Trusted standing opens the priority offer");

                foreach (var offer in standard.Append(priority))
                    await ctx.ExecuteInCreatureContextAsync(pilot.Creature, () => FactionAgents.Accept(pilot.Creature, npc, offer.Id));
                ctx.AssertEqual(FactionAgentTuning.MaximumActive, DB.Get<Player>(pilot.Id).AgentMissions.Count, "three missions are held");

                ctx.AssertEqual(AgentRefusal.TooManyMissions, FactionAgents.GetRefusal(pilot.Creature, agent, intel), "a fourth mission is refused");
                await ctx.ExecuteInCreatureContextAsync(pilot.Creature, () => FactionAgents.Accept(pilot.Creature, npc, intel.Id));
                ctx.AssertEqual(FactionAgentTuning.MaximumActive, DB.Get<Player>(pilot.Id).AgentMissions.Count, "the refused fourth mission is not added");
                ctx.AssertEqual(1, DB.Get<Player>(pilot.Id).AgentIntel, "a refused intel lead keeps the intel");

                await ctx.ExecuteInCreatureContextAsync(pilot.Creature, () => FactionAgents.Abandon(pilot.Creature, priority.Id));
                var record = DB.Get<Player>(pilot.Id);
                ctx.AssertEqual(2, record.AgentMissions.Count, "an abandoned mission leaves the record");
                ctx.Assert(!record.Quests.ContainsKey(priority.Id), "an abandoned mission's quest leaves the journal");
                ctx.Assert(record.AgentOffersTaken.Contains(priority.Id), "an abandoned offer stays taken");
                ctx.AssertEqual(AgentRefusal.AlreadyTaken, FactionAgents.GetRefusal(pilot.Creature, agent, priority), "an abandoned offer cannot be retaken");

                await ctx.ExecuteInCreatureContextAsync(pilot.Creature, () => FactionAgents.Accept(pilot.Creature, npc, intel.Id));
                record = DB.Get<Player>(pilot.Id);
                ctx.Assert(record.AgentMissions.Any(x => x.Id == intel.Id), "with a free slot and intel, the intel lead is taken");
                ctx.AssertEqual(0, record.AgentIntel, "the lead spends the intel");
            }
            finally
            {
                foreach (var id in ids)
                    Quest.UnregisterRuntimeQuest(id);
            }
        }

        [EngineTest("Killing the tier's ships in the mission's system counts and a wrong-tier ship does not", Category = Category, TimeoutSeconds = 60f)]
        public static async Task KillsCountInTheMissionSystem(EngineTestContext ctx)
        {
            using var pilot = await PlayerAbilityFixture.CreateAsync(ctx);
            var low = SpawnShip(ctx, "t1fighter", 6f);
            var high = SpawnShip(ctx, "t2fighter", 8f);
            await ctx.WaitUntilAsync(() => Space.GetShipStatus(low) != null && Space.GetShipStatus(high) != null, 5f, "both ships to register as NPC ships");
            ctx.Assert(Space.TryGetMissionKill(low, out var lowTier, out _), "a preset NPC ship is a kill-mission target");
            ctx.Assert(Space.TryGetMissionKill(high, out var highTier, out _), "a second preset NPC ship is a kill-mission target");
            ctx.Assert(lowTier != highTier, "the two ships are different tiers");

            var mission = NewMission(AgentMissionType.Kill, lowTier, GetResRef(ctx.Arena), 1);
            var gold = GetGold(pilot.Creature);
            try
            {
                await HoldMission(ctx, pilot, mission);

                await Kill(ctx, pilot.Creature, high);
                ctx.AssertEqual(1, DB.Get<Player>(pilot.Id).AgentMissions.Count, "a ship of another tier does not count");
                ctx.AssertEqual(gold, GetGold(pilot.Creature), "nothing is paid for it");

                await Kill(ctx, pilot.Creature, low);
                await ctx.WaitUntilAsync(() => DB.Get<Player>(pilot.Id).AgentMissions.Count == 0, 10f, "the kill to finish the mission");
                ctx.AssertEqual(gold + mission.Credits, GetGold(pilot.Creature), "the kill pays the mission");
            }
            finally
            {
                Quest.UnregisterRuntimeQuest(mission.Id);
            }
        }

        [EngineTest("A scan-down lead answers only its holder, and opening its cache completes the mission", Category = Category, TimeoutSeconds = 60f)]
        public static async Task ScanDownLead(EngineTestContext ctx)
        {
            using var holder = await PlayerAbilityFixture.CreateAsync(ctx);
            using var outsider = await PlayerAbilityFixture.CreateAsync(ctx, 2f);
            var holderShip = CreateShip(holder, "engine-agent-scan/" + holder.Id, ShipTag);
            var outsiderShip = CreateShip(outsider, "engine-agent-scan-out/" + outsider.Id, ShipTag);

            var area = GetArea(holder.Creature);
            var mission = NewMission(AgentMissionType.ScanDown, 1, GetResRef(area), 1);
            mission.Site = SignatureType.CacheSite;
            var position = GetPositionFromLocation(ctx.GetArenaLocation(6f));
            var lead = SignatureSiteRules.CreateAgentLead("engine-agent-lead/" + holder.Id, GetResRef(area), 1, SignatureType.CacheSite,
                position.X, position.Y, position.Z, DateTime.UtcNow, mission.Id, holder.Id);
            Space.PlaceSignature(lead);

            uint FindCache() => FindInArea(area, o => GetResRef(o) == ScanningTuning.CacheResref).FirstOrDefault();
            var gold = GetGold(holder.Creature);
            try
            {
                await HoldMission(ctx, holder, mission);
                var strong = RevampModuleCatalog.Steps.First(x => x.Family == ShipModuleFamily.ProbeLauncher && x.Rank == 45);
                var status = DB.Get<PlayerShip>(holderShip).Status;

                for (var cycle = 0; cycle < 3; cycle++)
                    await ctx.ExecuteInCreatureContextAsync(outsider.Creature,
                        () => Space.RunProbeCycle(outsider.Creature, DB.Get<PlayerShip>(outsiderShip).Status, strong));
                ctx.AssertEqual(0, ScanningRules.Percent(lead, outsider.Id), "a ship outside the holder's party gathers no signal");
                ctx.Assert(!lead.IsRevealed, "the lead stays hidden from the outsider");

                for (var cycle = 0; cycle < 6 && !lead.IsRevealed; cycle++)
                    await ctx.ExecuteInCreatureContextAsync(holder.Creature, () => Space.RunProbeCycle(holder.Creature, status, strong));
                ctx.Assert(lead.IsRevealed, "the holder's probes resolve the lead");
                var cache = FindCache();
                ctx.Assert(GetIsObjectValid(cache), "the lead's site cache spawns");

                Space.OpenSiteCache(cache, outsider.Creature);
                ctx.AssertEqual(1, DB.Get<Player>(holder.Id).AgentMissions.Count, "the outsider cannot complete the holder's mission");

                Space.OpenSiteCache(cache, holder.Creature);
                await ctx.WaitUntilAsync(() => DB.Get<Player>(holder.Id).AgentMissions.Count == 0, 10f, "opening the cache to finish the mission");
                ctx.Assert(GetGold(holder.Creature) >= gold + mission.Credits + SignatureSiteRules.SiteCredits(SignatureType.CacheSite, 1),
                    "the holder gets the mission's credits and the cache's");
            }
            finally
            {
                Space.RemoveSignature(lead);
                foreach (var leftover in FindInArea(area, o => GetResRef(o) == ScanningTuning.CacheResref))
                    DestroyObject(leftover);
                Quest.UnregisterRuntimeQuest(mission.Id);
                DB.Delete<PlayerShip>(holderShip);
                DB.Delete<PlayerShip>(outsiderShip);
            }
        }

        [EngineTest("A data heist's target can completes the mission; losing it moves the mark to another can", Category = Category, TimeoutSeconds = 60f)]
        public static async Task DataHeistTarget(EngineTestContext ctx)
        {
            using var holder = await PlayerAbilityFixture.CreateAsync(ctx);
            var holderShip = CreateShip(holder, "engine-agent-heist/" + holder.Id, ShipTag);
            var area = GetArea(holder.Creature);
            var mission = NewMission(AgentMissionType.DataHeist, 1, GetResRef(area), 1);
            mission.Slot = AgentMissionSlot.Intel;
            mission.Site = SignatureType.DataSite;
            var position = GetPositionFromLocation(ctx.GetArenaLocation(6f));
            var lead = SignatureSiteRules.CreateAgentLead("engine-agent-heist-lead/" + holder.Id, GetResRef(area), 1, SignatureType.DataSite,
                position.X, position.Y, position.Z, DateTime.UtcNow, mission.Id, holder.Id);
            Space.PlaceSignature(lead);

            List<uint> Cans() => FindInArea(area, o => GetTag(o) == Space.DataCanTag);
            var gold = GetGold(holder.Creature);
            try
            {
                await HoldMission(ctx, holder, mission);
                // PlaceLead (the arrival path) records the lead for the mission; this test placed it directly, so it records it too.
                var leads = (Dictionary<(string PlayerId, string MissionId), string>)typeof(FactionAgents)
                    .GetField("_leads", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                leads[(holder.Id, mission.Id)] = lead.Id;
                var strong = RevampModuleCatalog.Steps.First(x => x.Family == ShipModuleFamily.ProbeLauncher && x.Rank == 45);
                var status = DB.Get<PlayerShip>(holderShip).Status;
                for (var cycle = 0; cycle < 6 && !lead.IsRevealed; cycle++)
                    await ctx.ExecuteInCreatureContextAsync(holder.Creature, () => Space.RunProbeCycle(holder.Creature, status, strong));
                ctx.Assert(lead.IsRevealed, "the holder's probes resolve the heist site");

                // The site's squad is hostile by design; it stands down so it cannot kill the fixture mid-test.
                foreach (var objective in lead.Objectives.Where(GetIsObjectValid).Where(o => GetObjectType(o) == ObjectType.Creature))
                    ChangeToStandardFaction(objective, StandardFaction.Defender);

                var cans = Cans();
                var targets = cans.Where(c => GetLocalString(c, HeistMissionVariable) == mission.Id).ToList();
                ctx.AssertEqual(1, targets.Count, "exactly one can is the target");
                ctx.AssertEqual("Target Data Can", GetName(targets[0]), "the target can is named");
                ctx.Assert(lead.Objectives.Count(o => GetIsObjectValid(o) && GetObjectType(o) == ObjectType.Creature) > 0, "a squad holds the site");

                var other = cans.First(c => c != targets[0]);
                await ctx.ExecuteInCreatureContextAsync(holder.Creature, () => Space.CompleteSlicing(holder.Creature, other, SlicingSourceType.DataCan, 1));
                ctx.AssertEqual(1, DB.Get<Player>(holder.Id).AgentMissions.Count, "slicing a can that is not the target does not finish the heist");

                // A failed slice destroys the target: the mark passes to another can.
                var lost = targets[0];
                await ctx.ExecuteInCreatureContextAsync(holder.Creature, () => Space.OnSlicingTargetDestroyed(lost, SlicingSourceType.DataCan));
                var marked = Cans().Where(c => GetLocalString(c, HeistMissionVariable) == mission.Id).ToList();
                ctx.AssertEqual(1, marked.Count, "the mark passes to a remaining can");
                ctx.Assert(marked[0] != lost, "the new target is a different can");
                ctx.AssertEqual(1, DB.Get<Player>(holder.Id).AgentMissions.Count, "losing the target does not end the mission");

                var target = marked[0];
                await ctx.ExecuteInCreatureContextAsync(holder.Creature, () => Space.CompleteSlicing(holder.Creature, target, SlicingSourceType.DataCan, 1));
                await ctx.WaitUntilAsync(() => DB.Get<Player>(holder.Id).AgentMissions.Count == 0, 10f, "slicing the target to finish the mission");
                ctx.Assert(GetGold(holder.Creature) >= gold + mission.Credits, "the mission pays its credits");
            }
            finally
            {
                Space.RemoveSignature(lead);
                foreach (var leftover in Cans())
                    DestroyObject(leftover);
                Quest.UnregisterRuntimeQuest(mission.Id);
                DB.Delete<PlayerShip>(holderShip);
            }
        }

        [EngineTest("A sealed courier load goes aboard a docked ship, pays on time and fails late", Category = Category, TimeoutSeconds = 60f)]
        public static async Task SealedCourier(EngineTestContext ctx)
        {
            using var pilot = await PlayerAbilityFixture.CreateAsync(ctx);
            var now = DateTime.UtcNow;
            var found = new List<(FactionAgentDetail Agent, AgentMission Offer)>();
            foreach (var agent in FactionAgentTuning.Agents)
            {
                foreach (var offer in FactionAgentRules.GetBoard(agent.Type, now).Where(x => x.Type == AgentMissionType.SealedCourier && x.Slot != AgentMissionSlot.Intel))
                    found.Add((agent, offer));
            }

            if (found.Count < 2)
                ctx.Skip("fewer than two courier offers are on today's boards");

            var (onTimeAgent, onTime) = found[0];
            var (lateAgent, late) = found.First(x => x.Offer.Id != onTime.Id);
            pilot.Update(r =>
            {
                r.Factions[onTimeAgent.Faction] = new PlayerFactionStanding { Standing = TrustedStanding };
                r.Factions[lateAgent.Faction] = new PlayerFactionStanding { Standing = TrustedStanding };
            });
            var shipIds = new List<string>();
            var propertyIds = new List<string>();
            try
            {
                var npcA = SpawnAgent(ctx, onTimeAgent.Type);
                var npcB = SpawnAgent(ctx, lateAgent.Type);
                ctx.AssertEqual(AgentRefusal.NoShipDocked, FactionAgents.GetRefusal(pilot.Creature, onTimeAgent, onTime), "a courier run needs a ship docked at the agent's dock");

                var shipA = DockShip(pilot, onTimeAgent.DockId, "engine-agent-courier-a/" + pilot.Id, shipIds, propertyIds);
                var shipB = onTimeAgent.DockId == lateAgent.DockId ? shipA : DockShip(pilot, lateAgent.DockId, "engine-agent-courier-b/" + pilot.Id, shipIds, propertyIds);

                await ctx.ExecuteInCreatureContextAsync(pilot.Creature, () => FactionAgents.Accept(pilot.Creature, npcA, onTime.Id));
                var record = DB.Get<Player>(pilot.Id);
                var mission = record.AgentMissions.Single(x => x.Id == onTime.Id);
                ctx.AssertEqual(shipA, mission.ShipId, "the mission records the ship carrying the load");
                var contract = record.FreightContracts.Single(x => x.Id == onTime.Id);
                ctx.Assert(contract.IsAgentCourier, "the load is an agent courier contract");
                ctx.AssertEqual(1, DB.Get<PlayerShip>(shipA).SealedFreight.Count(x => x.ContractId == onTime.Id), "the sealed load is aboard the ship");

                await ctx.ExecuteInCreatureContextAsync(pilot.Creature, () => FactionAgents.Accept(pilot.Creature, npcB, late.Id));
                ctx.AssertEqual(2, DB.Get<Player>(pilot.Id).AgentMissions.Count, "both courier runs are held");

                var gold = GetGold(pilot.Creature);
                await ctx.ExecuteInCreatureContextAsync(pilot.Creature, () => FactionAgents.OnCourierDelivered(pilot.Creature, onTime.Id, false));
                await ctx.WaitUntilAsync(() => DB.Get<Player>(pilot.Id).AgentMissions.All(x => x.Id != onTime.Id), 10f, "the on-time delivery to finish the run");
                ctx.AssertEqual(gold + onTime.Credits, GetGold(pilot.Creature), "an on-time delivery pays the run");
                ctx.AssertEqual(0, DB.Get<PlayerShip>(shipA).SealedFreight.Count(x => x.ContractId == onTime.Id), "the finished run leaves no sealed load behind");

                gold = GetGold(pilot.Creature);
                await ctx.ExecuteInCreatureContextAsync(pilot.Creature, () => FactionAgents.OnCourierDelivered(pilot.Creature, late.Id, true));
                await ctx.WaitUntilAsync(() => DB.Get<Player>(pilot.Id).AgentMissions.Count == 0, 10f, "the late delivery to fail the run");
                record = DB.Get<Player>(pilot.Id);
                ctx.AssertEqual(gold, GetGold(pilot.Creature), "a late delivery pays nothing");
                ctx.Assert(!record.Quests.ContainsKey(late.Id), "the failed run's quest is gone");
                ctx.AssertEqual(0, record.FreightContracts.Count(x => x.Id == late.Id), "the failed run's contract is gone");
                ctx.AssertEqual(0, DB.Get<PlayerShip>(shipB).SealedFreight.Count(x => x.ContractId == late.Id), "the failed run's load is unloaded");
            }
            finally
            {
                Quest.UnregisterRuntimeQuest(onTime.Id);
                Quest.UnregisterRuntimeQuest(late.Id);
                foreach (var id in shipIds)
                    DB.Delete<PlayerShip>(id);
                foreach (var id in propertyIds)
                    DB.Delete<WorldProperty>(id);
            }
        }

        [EngineTest("An escort convoy that reaches its lane completes the mission and one that is lost fails it", Category = Category, TimeoutSeconds = 60f)]
        public static async Task ConvoyEscort(EngineTestContext ctx)
        {
            using var pilot = await PlayerAbilityFixture.CreateAsync(ctx);
            var area = GetArea(pilot.Creature);
            var start = typeof(FactionAgents).GetMethod("StartEscort", BindingFlags.Static | BindingFlags.NonPublic);
            ctx.Assert(start != null, "FactionAgents.StartEscort exists");

            var arrives = NewMission(AgentMissionType.ConvoyEscort, 2, GetResRef(area), 1);
            var lost = NewMission(AgentMissionType.ConvoyEscort, 2, GetResRef(area), 1);
            try
            {
                await HoldMission(ctx, pilot, arrives);
                await HoldMission(ctx, pilot, lost);

                await ctx.ExecuteInCreatureContextAsync(pilot.Creature, () => start.Invoke(null, new object[] { pilot.Creature, area, arrives }));
                var haulerA = FindInArea(area, o => GetLocalString(o, ConvoyMissionVariable) == arrives.Id).SingleOrDefault();
                ctx.Assert(GetIsObjectValid(haulerA), "the escort convoy's hauler appears and carries its mission");

                var gold = GetGold(pilot.Creature);
                await ctx.ExecuteInCreatureContextAsync(haulerA, () => FactionAgents.OnConvoyArrived(haulerA));
                await ctx.WaitUntilAsync(() => DB.Get<Player>(pilot.Id).AgentMissions.All(x => x.Id != arrives.Id), 10f, "the convoy's jump to finish the mission");
                ctx.AssertEqual(gold + arrives.Credits, GetGold(pilot.Creature), "a convoy that jumps out pays the mission");
                ctx.AssertEqual(1, DB.Get<Player>(pilot.Id).AgentMissions.Count, "the other escort mission is untouched");

                await ctx.ExecuteInCreatureContextAsync(pilot.Creature, () => start.Invoke(null, new object[] { pilot.Creature, area, lost }));
                var haulerB = FindInArea(area, o => GetLocalString(o, ConvoyMissionVariable) == lost.Id).SingleOrDefault();
                ctx.Assert(GetIsObjectValid(haulerB), "the second convoy's hauler appears");

                gold = GetGold(pilot.Creature);
                await ctx.ExecuteInCreatureContextAsync(pilot.Creature, () => ApplyEffectToObject(DurationType.Instant, EffectDeath(), haulerB));
                await ctx.WaitUntilAsync(() => DB.Get<Player>(pilot.Id).AgentMissions.Count == 0, 10f, "the destroyed hauler to fail the mission");
                ctx.AssertEqual(gold, GetGold(pilot.Creature), "a lost convoy pays nothing");
                ctx.Assert(!DB.Get<Player>(pilot.Id).Quests.ContainsKey(lost.Id), "the failed mission's quest is gone");
            }
            finally
            {
                foreach (var hauler in FindInArea(area, o => !string.IsNullOrEmpty(GetLocalString(o, ConvoyMissionVariable))))
                    DestroyObject(hauler);
                Quest.UnregisterRuntimeQuest(arrives.Id);
                Quest.UnregisterRuntimeQuest(lost.Id);
            }
        }

        [EngineTest("The token store sells for the agent's faction tokens and gates by standing", Category = Category, TimeoutSeconds = 60f)]
        public static async Task TokenStore(EngineTestContext ctx)
        {
            using var pilot = await PlayerAbilityFixture.CreateAsync(ctx);
            var agent = FactionAgentRules.GetAgent(FactionAgentType.RepublicVeles);
            var npc = SpawnAgent(ctx, agent.Type);
            var currency = FactionAgentRules.GetTokenCurrency(agent.Faction);
            var items = FactionAgentRules.GetStoreItems(agent.Faction);
            var ammo = items.First(x => x.Category == FactionTokenStoreCategory.Ammo);
            var design = items.First(x => x.Category == FactionTokenStoreCategory.DesignSchematic);

            void SetTokens(int amount) => pilot.Update(r => r.Currencies[currency] = amount);
            void SetStanding(int standing) => pilot.Update(r => r.Factions[agent.Faction] = new PlayerFactionStanding { Standing = standing });
            Task Buy(FactionTokenStoreItem item) =>
                ctx.ExecuteInCreatureContextAsync(pilot.Creature, () => FactionAgents.Buy(pilot.Creature, npc, items.IndexOf(item)));
            int Held(string resref) => FindInInventory(pilot.Creature, resref);

            SetStanding(0);
            SetTokens(ammo.Tokens - 1);
            await Buy(ammo);
            ctx.AssertEqual(ammo.Tokens - 1, FactionAgents.GetTokens(pilot.Creature, agent), "too few tokens buy nothing and spend nothing");
            ctx.AssertEqual(0, Held(ammo.Resref), "no item is created");

            SetTokens(ammo.Tokens);
            await Buy(ammo);
            ctx.AssertEqual(0, FactionAgents.GetTokens(pilot.Creature, agent), "the purchase spends its price");
            ctx.AssertEqual(ammo.Quantity, Held(ammo.Resref), "the item arrives in the quantity the store sells");

            SetTokens(design.Tokens);
            await Buy(design);
            ctx.AssertEqual(design.Tokens, FactionAgents.GetTokens(pilot.Creature, agent), "an item above the pilot's standing tier is refused");
            ctx.AssertEqual(0, Held(design.Resref), "the refused item is not created");

            SetStanding(TrustedStanding);
            await Buy(design);
            ctx.AssertEqual(0, FactionAgents.GetTokens(pilot.Creature, agent), "Trusted standing opens the Design Schematic");
            ctx.AssertEqual(1, Held(design.Resref), "the schematic arrives");

            SetStanding(-600);
            SetTokens(100);
            await Buy(ammo);
            ctx.AssertEqual(100, FactionAgents.GetTokens(pilot.Creature, agent), "a hostile pilot buys nothing");
        }

        // Fixtures

        private static uint SpawnAgent(EngineTestContext ctx, FactionAgentType type)
        {
            var npc = ctx.SpawnCreature("nw_rat001", 4f);
            SetLocalInt(npc, FactionAgents.AgentVariable, (int)type);
            return npc;
        }

        private static uint SpawnShip(EngineTestContext ctx, string resref, float x)
        {
            var ship = CreateObject(ObjectType.Creature, resref, ctx.GetArenaLocation(x));
            ctx.Assert(GetIsObjectValid(ship), $"Failed to spawn ship '{resref}'.");
            ChangeToStandardFaction(ship, StandardFaction.Defender);
            ctx.Track(ship);
            return ship;
        }

        /// <summary>The first offer any agent's board shows today that matches, fewest to count first.</summary>
        private static (FactionAgentDetail Agent, AgentMission Offer) FindOffer(Func<AgentMission, bool> match)
        {
            var now = DateTime.UtcNow;
            return FactionAgentTuning.Agents
                .SelectMany(agent => FactionAgentRules.GetBoard(agent.Type, now).Where(match).Select(offer => (Agent: agent, Offer: offer)))
                .OrderBy(x => x.Offer.Quantity)
                .FirstOrDefault();
        }

        private static AgentMission NewMission(AgentMissionType type, int tier, string orbitResref, int quantity)
        {
            return new AgentMission
            {
                Id = $"agent_999_{Guid.NewGuid():N}",
                Agent = FactionAgentType.RepublicVeles,
                Faction = FactionType.RepublicMilitary,
                Slot = AgentMissionSlot.Standard,
                Type = type,
                Tier = tier,
                OrbitResref = orbitResref,
                Quantity = quantity,
                Credits = 500,
                Standing = 15,
                Tokens = 5,
                AcceptedAt = DateTime.UtcNow,
            };
        }

        /// <summary>Puts a mission on the pilot's record with its runtime quest accepted, as taking a board offer does.</summary>
        private static async Task HoldMission(EngineTestContext ctx, PlayerAbilityFixture pilot, AgentMission mission)
        {
            Quest.RegisterRuntimeQuest(AgentMissionQuest.Build(mission));
            pilot.Update(r =>
            {
                r.AgentMissions.Add(mission);
                r.AgentOffersTaken.Add(mission.Id);
            });
            var accepted = false;
            await ctx.ExecuteInCreatureContextAsync(pilot.Creature, () => accepted = Quest.AcceptQuest(pilot.Creature, GetModule(), mission.Id));
            ctx.Assert(accepted, "the mission's quest is accepted");
        }

        /// <summary>Kills a ship with the pilot as the killer and waits for it to die.</summary>
        private static async Task Kill(EngineTestContext ctx, uint killer, uint ship)
        {
            await ctx.ExecuteInCreatureContextAsync(ship, () => Enmity.ModifyEnmity(killer, ship, 100));
            await ctx.ExecuteInCreatureContextAsync(killer, () => ApplyEffectToObject(DurationType.Instant, EffectDeath(), ship));
            await ctx.WaitUntilAsync(() => GetIsDead(ship), 5f, "the ship to die");
            await ctx.WaitFrameAsync();
        }

        private static string CreateShip(PlayerAbilityFixture pilot, string shipId, string tag)
        {
            pilot.Update(p => p.ActiveShipId = shipId);
            DB.Set(new PlayerShip
            {
                Id = shipId,
                OwnerPlayerId = pilot.Id,
                Status = new ShipStatus { ItemTag = tag, Size = ShipSize.Small, MaxHull = 100, MaxShield = 100 },
            });
            return shipId;
        }

        /// <summary>A freighter docked at an NPC dock (its landing area resref), as Trade sees a docked ship.</summary>
        private static string DockShip(PlayerAbilityFixture pilot, string dockId, string shipId, List<string> shipIds, List<string> propertyIds)
        {
            var propertyId = "engine-agent-property/" + Guid.NewGuid().ToString("N");
            var property = new WorldProperty { Id = propertyId, OwnerPlayerId = pilot.Id };
            property.Positions[PropertyLocationType.DockPosition] = new PropertyLocation { AreaResref = dockId };
            DB.Set(property);
            DB.Set(new PlayerShip
            {
                Id = shipId,
                OwnerPlayerId = pilot.Id,
                PropertyId = propertyId,
                Status = new ShipStatus { ItemTag = FreighterTag, Size = ShipSize.Medium, MaxHull = 100, MaxShield = 100 },
            });
            shipIds.Add(shipId);
            propertyIds.Add(propertyId);
            return shipId;
        }

        private static List<uint> FindInArea(uint area, Func<uint, bool> match)
        {
            var found = new List<uint>();
            for (var o = GetFirstObjectInArea(area); GetIsObjectValid(o); o = GetNextObjectInArea(area))
            {
                if (match(o))
                    found.Add(o);
            }

            return found;
        }

        private static int FindInInventory(uint holder, string resref)
        {
            var count = 0;
            for (var item = GetFirstItemInInventory(holder); GetIsObjectValid(item); item = GetNextItemInInventory(holder))
            {
                if (GetResRef(item) == resref)
                    count += Math.Max(1, GetItemStackSize(item));
            }

            return count;
        }
    }
}
