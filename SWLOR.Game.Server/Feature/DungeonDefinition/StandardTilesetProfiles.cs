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
                .FeatureTile("Hot_Springs")
                .SetPiece("Platform01_2x3")
                .SetPiece("Platform02_2x2")
                .SetPiece("Pillar_1x2", 2)
                .SetPiece("WallSection01_1x2")
                .SetPiece("WallSection02_1x2")
                .SetPiece("BigDoor01", 1)
                .SetPiece("BigDoor02", 1)
                .ExitGroup("Exit01")
                .ExitGroup("Exit02")
                .ExitGroup("Exit03");

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
                .FeatureTile("Camp")
                .SetPiece("Platform01_2x2")
                .SetPiece("Platform02_2x2")
                .SetPiece("Pillar_1x2", 2)
                .SetPiece("WallSection01_2x1")
                .SetPiece("WallSection02_2x1")
                .SetPiece("CampWall")
                .SetPiece("BigDoor01", 1)
                .SetPiece("BigDoor02", 1)
                // Fence gate: LayoutGroupStamper's CorridorInsert classifier splices one of these into
                // a straight Fence run LayoutFenceCarver carves (see StandardLayoutProfiles.Warren,
                // this tileset's production layout pairing); a no-op when no Fence run exists.
                .SetPiece("FenceDoor01", 1)
                .SetPiece("FenceDoor02", 1)
                .ExitGroup("Exit01")
                .ExitGroup("Exit02");

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
                .PrimaryOpenTerrain("floor")
                // Multi-terrain districts: 'Floor2' has full 16/16 corner coverage vs 'wall' plus
                // Doorway-junction tiles (TILE54 verified offline), so some rooms can be carved as
                // walled Floor2 districts joined to 'floor' rooms via Tunnel-mode corridors — this
                // unlocks Floor2's own separate ~13-tile vocabulary that was otherwise unreachable under
                // the single-open-terrain constraint. Only takes effect because Facility's default
                // layout profile (Complex) already uses Tunnel mode.
                .SecondaryOpenTerrain("Floor2")
                .SetPiece("Cell", 2)
                .SetPiece("Room", 2)
                .SetPiece("Bedroom")
                .SetPiece("2x1Room")
                .SetPiece("Transiton");

            // Alien Ruin (reference: korr_crypt_zil). Chasm lacks coverage — no accents.
            // 'Plaza' carries 11 fully-open tile variants vs 4 on the declared 'Floor'.
            // Feature tiles: vmr01's nine curated 1x1 groups (Portal/Chessboard/Mosaic_Plaza_2x2
            // deliberately excluded). InteriorRubble/RuinedHouse are Floor-cornered: with no
            // SecondaryOpenTerrain their corner key never appears in a Plaza-only layout, so they were
            // previously placed zero times; once districts are active (SecondaryOpenTerrain("Floor")
            // below, composed with a Tunnel-mode layout) Floor rooms actually exist and these start
            // matching automatically — no resolver change needed, TileResolver's corner-key lookup is
            // already terrain-agnostic.
            _builder.Create(AncientRuin, "Ancient Ruin")
                .Tileset("vmr01")
                .Placeholder("gen_placeholder4")
                .TileLighting(31, 27, 10, 12)
                .PrimaryOpenTerrain("Plaza")
                // Multi-terrain districts: 'Floor' has full 16/16 corner coverage vs 'Wall' plus
                // Doorway-junction tiles (TILE6 verified offline), so some rooms can be carved as walled
                // Floor districts joined to Plaza rooms via Tunnel-mode corridors — this unlocks
                // Floor's ~50-tile vocabulary (including feature/group tiles like InteriorMosaic_2x2
                // that structurally require Floor corners, see SetPiece("Amphitheater_2x2") note below)
                // that was otherwise unreachable under the single-open-terrain constraint. Only takes
                // effect when composed with a Tunnel-mode layout profile (Streets; the shipped default
                // Halls pairing stays OpenLane and never activates districts).
                .SecondaryOpenTerrain("Floor")
                .FeatureTile("ExteriorFountain", 2)
                .FeatureTile("ExteriorOvergrownGarden", 2)
                .FeatureTile("ExteriorPool", 2)
                .FeatureTile("InteriorRubble", 2)
                .FeatureTile("RuinedHouse", 2)
                .FeatureTile("Exterior Pillar 1")
                .FeatureTile("Exterior Pillar 2")
                .FeatureTile("Exterior Dais 1")
                .FeatureTile("Exterior Dais 2")
                // InteriorMosaic_2x2's corners are 'Floor', not 'Plaza' — LayoutGroupStamper's
                // OpenSetPiece classifier now determines a piece's own open terrain and restricts
                // placement to rooms carved from that same terrain (LayoutRoom.OpenTerrain), so this
                // only ever stamps into a Floor district room (created when districts are active) and
                // is otherwise silently unplaceable, exactly like the feature tiles above.
                .SetPiece("InteriorMosaic_2x2")
                .SetPiece("Amphitheater_2x2")
                .SetPiece("Mosaic_Plaza_2x2")
                .SetPiece("ExteriorWalkway_2x2")
                .SetPiece("Exterior Platform 2x2")
                .SetPiece("InteriorHallDoor", 1)
                // Fence/Alley gates: LayoutGroupStamper's CorridorInsert classifier splices one of
                // these into a straight Fence run (LayoutFenceCarver) or Alley tunnel segment
                // (LayoutTunnelCarver with CorridorCrosserType.Alley, see
                // StandardLayoutProfiles.Streets); a no-op unless that run/mode is actually used.
                .SetPiece("InteriorFenceDoor", 1)
                .SetPiece("ExteriorFenceDoor", 1)
                .SetPiece("BigDoorAlley", 1)
                .ExitGroup("ExteriorExit01")
                .ExitGroup("ExteriorExit02");

            return _builder.Build();
        }
    }
}
