#nullable disable
using System.Collections.Generic;
using SWLOR.Toolset.Domain.AreaGeneration;
using SWLOR.Toolset.Domain.AreaGeneration.Decoration;

namespace SWLOR.Toolset.Domain.AreaGeneration.Definitions
{
    /// <summary>
    /// Dathomir Wildlands dungeon theme on ttf01 (sw_t_forest): tangled jungle clearings crawling
    /// with Dathomir's native predators, with a Great Arkanian Dragon denning in the deep canopy.
    /// Tiers 1-3 reuse existing Bible-balanced Dathomir wildlife (DathomirSpawnDefinition/
    /// DathomirLootTableDefinition) so no new NPC balance work is required for the content loop; the
    /// tier 3 boss is reused from Nar Shaddaa (NarShaddaaSpawnDefinition's Great Arkanian Dragon —
    /// no Dathomir creature reaches this CR band, and an ancient jungle-denning dragon fits the
    /// wildlands framing better than its original undercity placement). Lighting is inherited from
    /// BaseGameTilesetProfiles.Forest's own registered TileLighting (0,0,8,8) — an uncalibrated
    /// placeholder per that profile's own doc comment, not newly sampled here. Forest's
    /// SolidTerrainOverride("Cliff")/PrimaryOpenTerrain("Forest") composition has no verified Tunnel
    /// vocabulary (TunnelVocabularyCheckTests.ExpectedUnsupported), so this theme uses Organic
    /// (OpenLane blobby clearings) rather than Complex.
    /// </summary>
    public class DathomirWildlandsDungeonDefinition : IDungeonListDefinition
    {
        public const string ThemeKey = "dathomirwild";

        private readonly DungeonDefinitionBuilder _builder = new();

        public Dictionary<string, DungeonDetail> BuildDungeons()
        {
            _builder.Create(ThemeKey, "Dathomir Wildlands")
                .TilesetProfile(BaseGameTilesetProfiles.Forest)
                .LayoutProfile(StandardLayoutProfiles.Organic)
                .SizeRange(8, 32)
                .ExitPlaceable("zep_portal001", "Overgrown Archway")
                .ExitDoor("_mdrn_dt_stneint")
                .TreasurePlaceable("corpse", "Fallen Traveler's Pack")

                // Decoration: the bulk of the visual dressing now lives on the Forest tileset profile
                // (its own ttf01 evidence — see BaseGameTilesetProfiles.Forest); only a couple of
                // theme accents (a giant Dathomir fern, an overgrown archway) are curated here.
                .DecorationDensity(0.25)
                .Decoration("zep_giantfern", 1, DecorationContext.RoomCenter)
                .Decoration("zep_arch002", 1, DecorationContext.DoorwayFlank)

                // Tier 1 — lesser jungle predators (CR ~42-58 ambient). Boss: Squellbug (CR 62).
                .Tier(1)
                .AddCreature("vgapingspider", 40)
                .AddCreature("vdathpurbole", 30)
                .AddCreature("vdatthrancor", 20)
                .CreaturesPerRoom(1, 2)
                .Boss("vdathsquell")
                .Treasure("DATHOMIR_SQUELLBUG", 2)
                .LevelNote("Ambient CR ~42-58 (Gaping Spider/Purbole/Rancor); boss CR 62 (Squellbug).")

                // Tier 2 — deep-jungle swarms and stalkers (CR ~70-72 ambient). Boss: Dragon Turtle
                // (CR 149).
                .Tier(2)
                .AddCreature("vdathshear", 40)
                .AddCreature("vdathswampland", 30)
                .AddCreature("vdathsprantal", 30)
                .CreaturesPerRoom(2, 3)
                .Boss("vdathturtle")
                .Treasure("DATHOMIR_DRAGON_TURTLE", 3)
                .LevelNote("Ambient CR ~70-72 (Shear Mite/Jungle Bug/Sprantal); boss CR 149 (Dragon Turtle).")

                // Tier 3 — apex wildlands beasts (CR ~75-149 ambient). Boss: Great Arkanian Dragon
                // (CR 190).
                .Tier(3)
                .AddCreature("vdathssurian", 30)
                .AddCreature("vdathturtle", 20)
                .CreaturesPerRoom(2, 4)
                .Boss("garkaniandragon")
                .Treasure("NARSHADDAA_GREAT_ARKANIAN_DRAGON_TROPHY", 4)
                .LevelNote("Ambient CR ~75-149 (Ssurian/Dragon Turtle); boss CR 190 (Great Arkanian Dragon).");

            return _builder.Build();
        }
    }
}
