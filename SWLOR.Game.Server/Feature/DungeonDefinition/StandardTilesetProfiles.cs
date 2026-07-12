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
            _builder.Create(Cavern, "Cavern")
                .Tileset("tdt01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .AccentTerrain("Water");

            // Sewers (reference: veles_sewers). Pit channel accents fully covered.
            _builder.Create(Sewers, "Sewers")
                .Tileset("tds01")
                .Placeholder("gen_placeholder3")
                .TileLighting(0, 2, 2, 2)
                .AccentTerrain("Pit");

            // Sci-Fi Base (reference: czs220_maintlvl). No accent coverage. Every partially-open
            // corner combo on zsf01 carries a movement-restricted pathnode (H/I) — only fully-open
            // tiles are pathnode A — so 1-wide door gaps and corridors fail the engine path check.
            _builder.Create(Facility, "Facility")
                .Tileset("zsf01")
                .Placeholder("gen_placeholder2")
                .TileLighting(4, 0, 2, 2)
                .MinimumOpeningWidth(2);

            // Alien Ruin (reference: korr_crypt_zil). Chasm/Plaza lack coverage — no accents.
            _builder.Create(AncientRuin, "Ancient Ruin")
                .Tileset("vmr01")
                .Placeholder("gen_placeholder4")
                .TileLighting(31, 27, 10, 12);

            return _builder.Build();
        }
    }
}
