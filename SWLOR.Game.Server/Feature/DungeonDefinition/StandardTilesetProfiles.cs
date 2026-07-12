using System.Collections.Generic;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.Game.Server.Feature.DungeonDefinition
{
    /// <summary>
    /// Tileset profiles for the verified generation tilesets. Lighting values are sampled from
    /// hand-built reference areas; accent terrains are only declared where the tileset has full
    /// (open, accent) corner coverage among resolver-usable tiles (verified offline).
    /// </summary>
    public class StandardTilesetProfiles : IDungeonTilesetProfileListDefinition
    {
        public const string Cavern = "cavern";
        public const string Sewers = "sewers";
        public const string Facility = "facility";
        public const string AncientRuin = "ancientruin";

        private readonly DungeonTilesetProfileBuilder _builder = new();

        public Dictionary<string, DungeonTilesetProfile> BuildTilesetProfiles()
        {
            // Sea Caves (reference: moncaladungeon1). Water accents fully covered.
            // Feature tiles: tdt01 open ('Floor') 1x1 groups (Portal/Chessboard deliberately excluded).
            _builder.Create(Cavern, "Cavern")
                .Tileset("tdt01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .AccentTerrain("Water")
                .FeatureTile("Treasure01", 2)
                .FeatureTile("Treasure02", 2)
                .FeatureTile("Pillar01")
                .FeatureTile("Pillar02")
                .FeatureTile("Hot_Springs");

            // Sewers (reference: veles_sewers). Pit channel accents fully covered.
            // Feature tiles: tds01 open ('Floor') 1x1 groups (Portal/Chessboard deliberately excluded).
            _builder.Create(Sewers, "Sewers")
                .Tileset("tds01")
                .Placeholder("gen_placeholder3")
                .TileLighting(0, 2, 2, 2)
                .AccentTerrain("Pit")
                .FeatureTile("Treasure01", 2)
                .FeatureTile("Treasure02", 2)
                .FeatureTile("Pillar01")
                .FeatureTile("Pillar02")
                .FeatureTile("Pillar03")
                .FeatureTile("Camp");

            // Sci-Fi Base (reference: czs220_maintlvl). No accent coverage. Every partially-open
            // corner combo on zsf01 carries a movement-restricted pathnode (H/I) — only fully-open
            // tiles are pathnode A — so 1-wide door gaps and corridors fail the engine path check.
            _builder.Create(Facility, "Facility")
                .Tileset("zsf01")
                .Placeholder("gen_placeholder2")
                .TileLighting(4, 0, 2, 2)
                .MinimumOpeningWidth(2)
                // zsf01's declared floor ("Floor2") has a single fully-open tile; 'floor' is the
                // terrain czs220_maintlvl builds its rooms from (3 diagonal variants + doorway tiles).
                .PrimaryOpenTerrain("floor");

            // Alien Ruin (reference: korr_crypt_zil). Chasm lacks coverage — no accents.
            // 'Plaza' carries 11 fully-open tile variants vs 4 on the declared 'Floor'.
            // Feature tiles: vmr01's nine curated 1x1 groups (Portal/Chessboard/Mosaic_Plaza_2x2
            // deliberately excluded); TileResolver's corner-key matching naturally excludes the
            // handful whose own corners are 'Floor' rather than 'Plaza' (e.g. InteriorRubble).
            _builder.Create(AncientRuin, "Ancient Ruin")
                .Tileset("vmr01")
                .Placeholder("gen_placeholder4")
                .TileLighting(31, 27, 10, 12)
                .PrimaryOpenTerrain("Plaza")
                .FeatureTile("ExteriorFountain", 2)
                .FeatureTile("ExteriorOvergrownGarden", 2)
                .FeatureTile("ExteriorPool", 2)
                .FeatureTile("InteriorRubble", 2)
                .FeatureTile("RuinedHouse", 2)
                .FeatureTile("Exterior Pillar 1")
                .FeatureTile("Exterior Pillar 2")
                .FeatureTile("Exterior Dais 1")
                .FeatureTile("Exterior Dais 2");

            return _builder.Build();
        }
    }
}
