#nullable disable
using System.Collections.Generic;
using SWLOR.Toolset.Domain.AreaGeneration;
using SWLOR.Toolset.Domain.AreaGeneration.Decoration;

namespace SWLOR.Toolset.Domain.AreaGeneration.Definitions
{
    /// <summary>
    /// Mine/Cave dungeon theme on tdt01 (sw_t_minecave), the first shipped procedural dungeon
    /// (design/ProceduralAreaGeneration.md, M4). Tiers 1-3 reuse existing Bible-balanced cave
    /// creatures and loot tables already defined for Korriban, Dantooine, and Viscara so no new
    /// NPC balance work is required for the content loop.
    /// </summary>
    public class MineCaveDungeonDefinition : IDungeonListDefinition
    {
        public const string ThemeKey = "minecave";

        private readonly DungeonDefinitionBuilder _builder = new();

        public Dictionary<string, DungeonDetail> BuildDungeons()
        {
            _builder.Create(ThemeKey, "Mine/Cave")
                .TilesetProfile(StandardTilesetProfiles.Cavern)
                .LayoutProfile(StandardLayoutProfiles.Organic)
                .SizeRange(8, 32)
                .ExitPlaceable("_mdrn_placedoord", "Cave Exit")
                .ExitDoor("_mdrn_dt_rough")
                .TreasurePlaceable("structure_rubble", "Ore-Strewn Cache")

                // Decoration: the bulk of the visual dressing now lives on the Cavern tileset profile
                // (its own tdt01 evidence — see StandardTilesetProfiles.Cavern); only a couple of
                // theme accents (an abandoned shrine) are curated here.
                .DecorationDensity(0.15)
                .Decoration("zep_altar002", 1, DecorationContext.RoomCenter)
                .Decoration("_mdrn_pl_colony9", 1, DecorationContext.DoorwayFlank)

                // Tier 1 — low-level cave vermin (CR ~3-7 ambient). Boss: Shyrack (CR 14).
                .Tier(1)
                .AddCreature("crystalspider", 40)
                .AddCreature("ww_kinrath", 40)
                .AddCreature("silkshade", 20)
                .CreaturesPerRoom(1, 2)
                .Boss("shyrack")
                .Treasure("KORRIBAN_SHYRACK", 2)
                .LevelNote("Ambient CR ~3-7 (Crystal Spider/Wildwoods Kinrath/Silkshade); boss CR 14 (Shyrack).")

                // Tier 2 — hardier cave predators (CR ~9-16 ambient). Boss: Tukata (CR 34).
                .Tier(2)
                .AddCreature("korr_klorslug", 30)
                .AddCreature("shardeye", 30)
                .AddCreature("korr_wraid", 40)
                .CreaturesPerRoom(2, 3)
                .Boss("tukata")
                .Treasure("KORRIBAN_TUKATA", 3)
                .LevelNote("Ambient CR ~9-16 (Klorslug/Shardeye/Wraid); boss CR 34 (Tukata).")

                // Tier 3 — deep-cave elites (CR ~20-61 ambient). Boss: Kinrath Queen (CR 197).
                .Tier(3)
                .AddCreature("sithsnake", 30)
                .AddCreature("hkinrath", 40)
                .AddCreature("tukata", 30)
                .CreaturesPerRoom(2, 4)
                .Boss("vqueenkin")
                .Treasure("DANTOOINE_KINRATH_QUEEN", 4)
                .LevelNote("Ambient CR ~20-61 (Sith Snake/Hive Kinrath/Tukata); boss CR 197 (Kinrath Queen).");

            return _builder.Build();
        }
    }
}
