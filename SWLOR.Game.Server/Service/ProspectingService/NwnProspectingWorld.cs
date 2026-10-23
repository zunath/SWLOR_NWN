using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.ItemDefinition;
using SWLOR.Game.Server.Service.ActivityService;
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

        public string LoadNode(uint node)
        {
            var content = GetLocalString(node, NodeVariable);
            return string.IsNullOrEmpty(content) ? null : content;
        }

        public void SaveNode(uint node, string content)
        {
            SetLocalString(node, NodeVariable, content);
        }

        public bool NodeExists(uint node) => GetIsObjectValid(node);

        public bool TableHasGems(string lootTable) =>
            Loot.LootTableExists(lootTable) && ProspectingGems.HasGems(Loot.GetLootTableByName(lootTable));

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
            for (var roll = 0; roll < delivery.Rolls; roll++)
            {
                var loot = table.GetRandomItem();
                CreateItemOnObject(loot.Resref, player);

                // Additional loot tables - these adhere to standard loot table rules, and are rolled once for each unit as they always were.
                Loot.SpawnLoot(node, player, "LOOT_TABLE_");
            }

            var ore = ProspectingGems.OreEntries(table);
            for (var roll = 0; roll < delivery.ExtraOreRolls; roll++)
                CreateItemOnObject(Pick(ore.Count > 0 ? ore : table).Resref, player);

            var gems = ProspectingGems.GemEntries(table);
            for (var gem = 0; gem < delivery.Gems && gems.Count > 0; gem++)
                CreateItemOnObject(Pick(gems).Resref, player);

            if (GetIsObjectValid(node))
                ApplyEffectAtLocation(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Summon_Monster_3), GetLocation(node));

            ExecuteScript(ScriptName.OnHarvesterUsed, player);
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

        public void PlayWork(uint player, float seconds)
        {
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
