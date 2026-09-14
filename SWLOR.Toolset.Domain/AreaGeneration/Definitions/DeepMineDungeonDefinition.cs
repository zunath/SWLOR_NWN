#nullable disable
using System.Collections.Generic;
using SWLOR.Toolset.Domain.AreaGeneration;
using SWLOR.Toolset.Domain.AreaGeneration.Decoration;

namespace SWLOR.Toolset.Domain.AreaGeneration.Definitions
{
    /// <summary>
    /// Deep Mine dungeon theme on tdm01 (sw_t_mine): an abandoned mining excavation whose tunnels
    /// have been reclaimed by mynocks, cairnmogs, and raivors, with a hive kinrath denning at the
    /// bottom shaft. Tiers 1-3 reuse existing Bible-balanced Viscara/CZ-220 cave creatures and loot
    /// tables (ViscaraSpawnDefinition/CZ220SpawnDefinition + ViscaraLootTableDefinition/
    /// DantooineLootTableDefinition) so no new NPC balance work is required for the content loop —
    /// distinct from MineCaveDungeonDefinition's Korriban-sourced roster on tdt01, avoiding overlap
    /// between the two "cave" themes. Lighting is inherited from BaseGameTilesetProfiles.
    /// MinesAndCaverns's own registered TileLighting (0,0,8,8) — an uncalibrated placeholder per that
    /// profile's own doc comment, not newly sampled here. MinesAndCaverns is not listed in
    /// TunnelVocabularyCheckTests.ExpectedUnsupported, so this theme uses Complex to exercise its
    /// elevation/relief/ramp vocabulary (MaxElevationRegions/MaxReliefRegions/RampCrosser("Slope")).
    /// </summary>
    public class DeepMineDungeonDefinition : IDungeonListDefinition
    {
        public const string ThemeKey = "deepmine";

        private readonly DungeonDefinitionBuilder _builder = new();

        public Dictionary<string, DungeonDetail> BuildDungeons()
        {
            _builder.Create(ThemeKey, "Deep Mine")
                .TilesetProfile(BaseGameTilesetProfiles.MinesAndCaverns)
                .LayoutProfile(StandardLayoutProfiles.Complex)
                .SizeRange(8, 32)
                .ExitPlaceable("_mdrn_placedoord", "Shaft Entrance")
                .ExitDoor("_mdrn_dt_rough")
                .TreasurePlaceable("structure_rubble", "Ore Vein Cache")

                // Decoration: the bulk of the visual dressing now lives on the MinesAndCaverns
                // tileset profile (its own tdm01 evidence — see BaseGameTilesetProfiles.
                // MinesAndCaverns); only a couple of theme accents (an abandoned mining cart, trailing
                // mine vines) are curated here.
                .DecorationDensity(0.3)
                .Decoration("_mdrn_pl_crgc4b", 1, DecorationContext.RoomCenter)
                .Decoration("zep_vinesh", 1, DecorationContext.DoorwayFlank)

                // Tier 1 — nesting mynocks and burrowing nashtah (CR ~1-5 ambient). Boss: Valley
                // Cairnmog (CR 6).
                .Tier(1)
                .AddCreature("mynock", 40)
                .AddCreature("czcryo_mynock", 30)
                .AddCreature("vall_nashtah", 30)
                .CreaturesPerRoom(1, 2)
                .Boss("valley_cairnmog")
                .Treasure("VISCARA_CAIRNMOG", 2)
                .LevelNote("Ambient CR ~1-5 (Mynock/Coolant-Scarred Mynock/Valley Nashtah); boss CR 6 (Valley Cairnmog).")

                // Tier 2 — deep-shaft raivors (CR ~6-12 ambient). Boss: Valley Cairnmog Alpha
                // (CR 14).
                .Tier(2)
                .AddCreature("valley_cairnmog", 30)
                .AddCreature("v_raivor", 30)
                .AddCreature("v_raivor2", 40)
                .CreaturesPerRoom(2, 3)
                .Boss("valley_cairnmog2")
                .Treasure("VISCARA_RAIVOR", 3)
                .LevelNote("Ambient CR ~6-12 (Valley Cairnmog/Raivor); boss CR 14 (Valley Cairnmog Alpha).")

                // Tier 3 — deep-mine apex predators (CR ~12-14 ambient). Boss: Hive Kinrath (CR 61).
                .Tier(3)
                .AddCreature("greyspine", 30)
                .AddCreature("redtail_kor", 30)
                .AddCreature("valley_cairnmog2", 40)
                .CreaturesPerRoom(2, 4)
                .Boss("hkinrath")
                .Treasure("DANTOOINE_HIVE_KINRATH", 4)
                .LevelNote("Ambient CR ~12-14 (Greyspine Rootbreaker/Redtail Kor/Valley Cairnmog Alpha); boss CR 61 (Hive Kinrath).");

            return _builder.Build();
        }
    }
}
