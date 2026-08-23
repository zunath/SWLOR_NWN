#nullable disable
using System.Collections.Generic;
using SWLOR.Toolset.Domain.AreaGeneration;
using SWLOR.Toolset.Domain.AreaGeneration.Decoration;

namespace SWLOR.Toolset.Domain.AreaGeneration.Definitions
{
    /// <summary>
    /// Qion Hive dungeon theme on tii01 (sw_t_illithid): an alien insectoid hive burrowed into
    /// Hutlar's ice, defended by Byysk warrior-broods and fed by its Qion broodmother. Tiers 1-3
    /// reuse existing Bible-balanced Hutlar hive creatures and loot tables (HutlarSpawnDefinition's
    /// dedicated QionHive dungeon-raid spawn tables / HutlarLootTableDefinition) so no new NPC
    /// balance work is required for the content loop. Lighting is inherited from
    /// BaseGameTilesetProfiles.IllithidInterior's own registered TileLighting (0,0,8,8) — an
    /// uncalibrated placeholder per that profile's own doc comment, not newly sampled here.
    /// IllithidInterior is listed in TunnelVocabularyCheckTests.ExpectedUnsupported (missing
    /// T-with-port junction shape — Complex genuinely downgrades to OpenLane for this tileset), so
    /// this theme uses Warren (dense corridor warren) instead of Complex.
    /// </summary>
    public class QionHiveDungeonDefinition : IDungeonListDefinition
    {
        public const string ThemeKey = "qionhive";

        private readonly DungeonDefinitionBuilder _builder = new();

        public Dictionary<string, DungeonDetail> BuildDungeons()
        {
            _builder.Create(ThemeKey, "Qion Hive")
                .TilesetProfile(BaseGameTilesetProfiles.IllithidInterior)
                .LayoutProfile(StandardLayoutProfiles.Warren)
                .SizeRange(8, 32)
                .ExitPlaceable("zep_portal001", "Hive Membrane")
                .ExitDoor("_mdrn_dt_slid001")
                .TreasurePlaceable("cz220_cache", "Larval Cache")

                // Decoration: the bulk of the visual dressing now lives on the IllithidInterior
                // tileset profile (its own tii01 evidence — see BaseGameTilesetProfiles.
                // IllithidInterior); only a couple of theme accents (hive-slime pooling, a fire
                // pillar) are curated here.
                .DecorationDensity(0.2)
                .Decoration("qionhiveslime001", 1, DecorationContext.CorridorSide)
                .Decoration("zep_firepillr003", 1, DecorationContext.RoomCenter)

                // Tier 1 — hive vermin (CR ~17-32 ambient). Boss: Qion Hive Tunneler (CR 45).
                .Tier(1)
                .AddCreature("qion_slug001", 30)
                .AddCreature("qion_slug", 40)
                .AddCreature("qion_tiger", 20)
                .CreaturesPerRoom(1, 2)
                .Boss("qion_hive_tunnel")
                .Treasure("HUTLAR_QION_SLUGS", 2)
                .LevelNote("Ambient CR ~17-32 (Qion Hive Slug/Qion Slug/Qion Tiger); boss CR 45 (Qion Hive Tunneler).")

                // Tier 2 — deep-hive tunnelers (CR ~32-45 ambient). Boss: Qion Hive Broodmother
                // (CR 118).
                .Tier(2)
                .AddCreature("qion_tiger", 30)
                .AddCreature("qion_hive_tunnel", 30)
                .CreaturesPerRoom(2, 3)
                .Boss("huthivebroodmoth")
                .Treasure("QIONHIVE_BROODMOTHER", 3)
                .LevelNote("Ambient CR ~32-45 (Qion Tiger/Qion Hive Tunneler); boss CR 118 (Qion Hive Broodmother).")

                // Tier 3 — Byysk warrior-brood defending the broodmother's chamber (CR ~122-132
                // ambient). Boss: Byysk Chieftain (CR 220).
                .Tier(3)
                .AddCreature("byysk_shaman", 30)
                .AddCreature("byysk_guard001", 30)
                .AddCreature("byysk_guard002", 30)
                .CreaturesPerRoom(2, 4)
                .Boss("byysk_chieftain")
                .Treasure("HUTLAR_BYYSK_GEAR_RARES", 4)
                .LevelNote("Ambient CR ~122-132 (Byysk Shaman/Byysk Guardian); boss CR 220 (Byysk Chieftain).");

            return _builder.Build();
        }
    }
}
