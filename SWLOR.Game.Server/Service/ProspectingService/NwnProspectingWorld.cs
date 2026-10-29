using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.ItemDefinition;
using SWLOR.Game.Server.Service.ActivityService;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.LootService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Service.ProspectingService
{
    /// <summary>The game-engine side of a Prospecting session. The rules it carries out are in <see cref="ProspectingLifecycle"/>.</summary>
    public sealed class NwnProspectingWorld : IProspectingWorld
    {
        public static readonly NwnProspectingWorld Instance = new();

        /// <summary>The local variable a node keeps its content in between sessions.</summary>
        public const string NodeVariable = "PROSPECTING_NODE";

        /// <summary>The local variable the harvesting before Prospecting kept a node's remaining units in.</summary>
        public const string LegacyStockVariable = "RESOURCE_COUNT";

        public string LoadNode(uint node)
        {
            var content = GetLocalString(node, NodeVariable);
            return string.IsNullOrEmpty(content) ? null : content;
        }

        public void SaveNode(uint node, string content)
        {
            SetLocalString(node, NodeVariable, content);
            DeleteLocalInt(node, LegacyStockVariable);
        }

        public int LegacyStock(uint node) => Math.Max(0, GetLocalInt(node, LegacyStockVariable));

        public bool NodeExists(uint node) => GetIsObjectValid(node);

        public double GemChance(string lootTable) =>
            Loot.LootTableExists(lootTable) ? ProspectingGems.GemChance(Loot.GetLootTableByName(lootTable)) : 0;

        public void DestroyNode(uint node)
        {
            if (!GetIsObjectValid(node))
                return;

            // DestroyObject bypasses the OnDeath event, and removes the object so we can't send events.
            // Use EffectDeath to ensure that we trigger death processing.
            SetPlotFlag(node, false);
            ApplyEffectToObject(DurationType.Instant, EffectDeath(), node);

            HarvesterItemDefinition.DestroyProp(node);
        }

        public int GetCharges(uint harvester) => GetIsObjectValid(harvester) ? GetItemCharges(harvester) : 0;

        public void SetCharges(uint harvester, int charges)
        {
            if (GetIsObjectValid(harvester))
                SetItemCharges(harvester, charges);
        }

        public void DestroyHarvester(uint harvester)
        {
            if (GetIsObjectValid(harvester))
                DestroyObject(harvester);
        }

        public void GiveHarvest(uint player, uint node, string lootTable, HarvestDelivery delivery)
        {
            var table = Loot.GetLootTableByName(lootTable);
            var units = new List<string>();
            for (var roll = 0; roll < delivery.Rolls; roll++)
                units.Add(table.GetRandomItem().Resref);

            var ore = ProspectingGems.OreEntries(table);
            for (var roll = 0; roll < delivery.ExtraOreRolls; roll++)
                units.Add(Pick(ore.Count > 0 ? ore : table).Resref);

            var gems = ProspectingGems.GemEntries(table);
            for (var gem = 0; gem < delivery.Gems && gems.Count > 0; gem++)
                units.Add(Pick(gems).Resref);

            var dropped = DeliverAndJournal(player, units);

            // Additional loot tables - these adhere to standard loot table rules, and are rolled once for each unit as they always were.
            for (var roll = 0; roll < delivery.Rolls; roll++)
                Loot.SpawnLoot(node, player, "LOOT_TABLE_");

            if (dropped)
                SendMessageToPC(player, HarvestDroppedMessage);

            if (GetIsObjectValid(node))
                ApplyEffectAtLocation(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Summon_Monster_3), GetLocation(node));

            ExecuteScript(ScriptName.OnHarvesterUsed, player);
        }

        /// <summary>
        /// Puts one unit in the player's inventory. When it did not arrive there (no room), it is placed at the player's feet instead, so
        /// a harvest the node has already paid out is never lost. The inventory count decides, since a unit that merged into a stack can
        /// come back as an invalid handle. Returns true when the unit ended up outside the inventory.
        /// </summary>
        private static bool Deliver(uint player, string resref)
        {
            var before = NwnQuickCraftWorld.CountComponent(player, resref);
            var item = CreateItemOnObject(resref, player);
            if (NwnQuickCraftWorld.CountComponent(player, resref) > before)
                return false;

            // A valid item that did not reach the inventory is already on the ground.
            if (!GetIsObjectValid(item))
                CreateObject(ObjectType.Item, resref, GetLocation(player));
            return true;
        }

        public const string HarvestDroppedMessage = "Your inventory is full, so some of the harvest was placed at your feet.";

        public const string HarvestReturnedMessage = "Harvest you gathered before the server went down has been returned to you.";

        /// <summary>
        /// Delivers the units, then saves the ones that reached the inventory in the player's harvest journal at the next craft file
        /// sequence. A unit left at the player's feet is not journaled: it is not in the character file either way, so giving it again
        /// at login would duplicate it. A caller that already changed the record passes it as <paramref name="dbPlayer"/>, so its change
        /// and the new entry are one save. Returns true when a unit ended up at the player's feet.
        /// </summary>
        private static bool DeliverAndJournal(uint player, IReadOnlyCollection<string> units, Player dbPlayer = null)
        {
            var placed = new List<string>();
            var dropped = false;
            foreach (var resref in units)
            {
                if (Deliver(player, resref))
                    dropped = true;
                else
                    placed.Add(resref);
            }

            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return dropped;

            dbPlayer ??= DB.Get<Player>(GetObjectUUID(player));
            if (dbPlayer == null)
                return dropped;

            if (placed.Count > 0)
            {
                var sequence = GetLocalInt(player, CraftSessionFileSequence.Variable) + 1;
                SetLocalInt(player, CraftSessionFileSequence.Variable, sequence);
                HarvestReturnJournal.Record(dbPlayer, GetLocalInt(player, CraftSessionFileSequence.ExportedVariable), sequence, placed);
            }

            DB.Set(dbPlayer);
            return dropped;
        }

        /// <summary>
        /// At login: gives again the harvest the loaded character file predates (see <see cref="HarvestReturnJournal"/>). The old entries
        /// are cleared in the same record save that journals the units given again at a new sequence, so they stay protected until a file
        /// holds them. Call it before anything exports the character, since it compares the journal with the sequence the file loaded with.
        /// </summary>
        public void ReturnPendingHarvest(uint player)
        {
            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            if (dbPlayer == null)
                return;

            var redo = HarvestReturnJournal.TakeAtLogin(dbPlayer, GetLocalInt(player, CraftSessionFileSequence.LoadedVariable));
            if (redo == null)
                return;

            if (redo.Count == 0)
            {
                DB.Set(dbPlayer);
                return;
            }

            var units = redo.SelectMany(unit => Enumerable.Repeat(unit.Key, unit.Value)).ToList();
            var dropped = DeliverAndJournal(player, units, dbPlayer);

            SendMessageToPC(player, HarvestReturnedMessage);
            if (dropped)
                SendMessageToPC(player, HarvestDroppedMessage);
        }

        public int GetGatheringRank(uint player) => Skill.GetCreatureSkillRank(player, SkillType.Gathering);

        public void GiveGatheringXp(uint player, int xp)
        {
            if (GetIsPC(player) && !GetIsDM(player))
                Skill.GiveSkillXP(player, SkillType.Gathering, xp, false, false);
        }

        public void Message(uint player, string text)
        {
            SendMessageToPC(player, text);
        }

        public void BeginSession(uint player)
        {
            Activity.SetBusy(player, ActivityStatusType.Prospecting);
        }

        public void EndSession(uint player)
        {
            // A replacement activity may already own the player's busy state.
            if (GetIsObjectValid(player) && Activity.GetBusyType(player) == ActivityStatusType.Prospecting)
                Activity.ClearBusy(player);
        }

        public void PlayWork(uint player, string actionId, float seconds)
        {
            var clip = WorkAnimation.ForProspecting(actionId);
            if (clip != null)
                NamedAnimation.Play(player, clip);
            else
                AssignCommand(player, () => ActionPlayAnimation(Animation.LoopingGetMid, 1.0f, seconds));
            if (GetIsPC(player))
                PlayerPlugin.StartGuiTimingBar(player, seconds);
        }

        public bool OpenWindow(uint player, ProspectingSession session)
        {
            // The result screen of the last session may still be open.
            if (Gui.IsWindowOpen(player, GuiWindowType.Prospecting))
                Gui.ClosePlayerWindow(player, GuiWindowType.Prospecting);

            Gui.TogglePlayerWindow(player, GuiWindowType.Prospecting, new ProspectingPayload(session));
            return Gui.IsWindowOpen(player, GuiWindowType.Prospecting);
        }

        public void CloseWindow(uint player)
        {
            if (Gui.IsWindowOpen(player, GuiWindowType.Prospecting))
                Gui.ClosePlayerWindow(player, GuiWindowType.Prospecting);
        }

        public ProspectingPlayerState GetPlayerState(uint player, uint harvester)
        {
            if (!GetIsObjectValid(player))
                return default;

            return new ProspectingPlayerState(
                true,
                GetIsDead(player),
                GetIsInCombat(player),
                GetPosition(player),
                Gui.IsWindowOpen(player, GuiWindowType.Prospecting),
                GetIsObjectValid(harvester) && GetItemPossessor(harvester) == player,
                Activity.GetBusyType(player) == ActivityStatusType.Prospecting);
        }

        public DateTime UtcNow => DateTime.UtcNow;

        public void Schedule(float seconds, Action action)
        {
            AssignCommand(GetModule(), () => DelayCommand(seconds, action));
        }

        private static LootTableItem Pick(IReadOnlyList<LootTableItem> entries)
        {
            var weights = entries.Select(entry => Math.Max(1, entry.Weight)).ToArray();
            return entries[Random.GetRandomWeightedIndex(weights)];
        }
    }
}
