using System.Collections.Generic;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.Game.Server.Feature.DungeonDefinition
{
    /// <summary>
    /// Pilot wave of base-game (non-hak) tileset profiles: Crypt (tdc01), Dungeon (tde01), and
    /// City Interior (tin01), resolved from basegame_sets via the shared TilesetSetSource (see
    /// the base-game tileset census, SWLOR.Game.Server.Tests/AreaGeneration/TileCoverageCensusTests.cs).
    /// These are tileset profiles only -- no theme/content is registered here; the existing themes
    /// keep their StandardTilesetProfiles defaults, and these three are only reachable via an
    /// explicit tileset override (ContentBuilder's tileset dropdown, or an --areas/--matrix override).
    ///
    /// All three stamp onto the existing gen_placeholder1 module area (cross-tileset override is
    /// proven live -- see the base-game tileset census work).
    ///
    /// Lighting values are uncalibrated placeholders (reused from Cavern's 0,0,8,8) pending visual
    /// review in the toolset -- these are NOT sampled from a hand-built reference area for these
    /// tilesets, unlike StandardTilesetProfiles' entries.
    /// </summary>
    public class BaseGameTilesetProfiles : IDungeonTilesetProfileListDefinition
    {
        public const string Crypt = "crypt";
        public const string Dungeon = "dungeon";
        public const string CityInterior = "cityinterior";

        // Wave-2 (Wave-1 READY-FLAT queue continued): ten more interior base-game tilesets, all
        // resolved to their SWLOR_Haks copy by TilesetSetSource (every one of these ten has been
        // copied into a hak, unlike the pilot three where only Crypt/Dungeon had hak copies and City
        // Interior stayed vanilla-only). See the base-game tileset census
        // (TileCoverageCensusTests.PilotTilesetKeys) for the coverage numbers and
        // TileCoverageCensusTests.PilotAlternateVocabTerrains/Crossers + PilotExpectedExemptions for
        // the exact per-tileset exemption accounting these profiles were curated against.
        public const string Barrows = "barrows";
        public const string MinesAndCaverns = "minescaverns";
        public const string Ruins = "ruins";
        public const string CastleInterior = "castleinterior";
        public const string CastleInterior2 = "castleinterior2";
        public const string DrowInterior = "drowinterior";
        public const string IllithidInterior = "illithidinterior";
        public const string CityInterior2 = "cityinterior2";
        public const string Steamworks = "steamworks";
        public const string FortInterior = "fortinterior";

        private readonly DungeonTilesetProfileBuilder _builder = new();

        public Dictionary<string, DungeonTilesetProfile> BuildTilesetProfiles()
        {
            // Crypt (tdc01). The SWLOR hak copy (SWLOR_Haks/sw_t_crypt/tdc01.set, resolved ahead of
            // basegame_sets by TilesetSetSource since hak copies win) is a heavily-expanded 891-tile
            // superset of vanilla tdc01: the base Wall/Floor/Pit palette (Corridor/Doorway/Bridge/
            // Fence crossers, StairsUp/Down, Platform01-05, WallSection01/02, BigDoor01/02,
            // BridgeDoor01, Door_Trans) plus three alternate-district decorative palettes ("Grey",
            // "Dwarven", and an unbracketed "Chult" crosser-only variant) using their OWN terrain
            // names (GreyFloor/GreyPit/DwarvenFloor/DwarvenPit) and OWN crosser names (GreyCorridor/
            // DwarvenDoorway/DwarvenCorridor/ChultDoorway/ChultCorridor) outside the shared layout
            // carvers' canonical Doorway/Corridor/Alley/Fence/Bridge vocabulary -- see
            // TileCoverageCensusTests' PilotAlternateVocabTerrains/PilotAlternateVocabCrossers for the
            // exact exemption accounting. PrimaryOpenTerrain left empty (defaults to "Floor", which
            // has full 16/16 corner coverage). AccentTerrain("Pit") mirrors Sewers' (tds01) pattern --
            // this family's Bridge-gated Pit channel is the same proven shape. MinimumOpeningWidth
            // left at the default 1: PathNodeOpeningWidthAudit (SWLOR.Game.Server.Tests/AreaGeneration/
            // PathNodeOpeningWidthAudit.cs) finds partially-open Floor/Wall combos with a pathnode-A
            // candidate, so (unlike zsf01) 1-wide openings path fine.
            // Group names are curated to the "[Tan]" palette specifically -- every group in this
            // tileset is prefixed "[Tan]"/"[Grey]"/"[Dwarven]" (there is no unprefixed base family);
            // "[Tan]" is simply the base Wall/Floor/Pit palette's curation label (its groups' corners
            // use plain "Floor"/"Pit", not a "Tan"-named terrain), verified directly against the .set
            // data. Fence doors are included even though no shipped layout profile currently pairs
            // Fence carving with this tileset, matching StandardTilesetProfiles' own convention of
            // registering structurally-valid pieces a future layout pairing can exercise.
            _builder.Create(Crypt, "Crypt")
                .Tileset("tdc01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .AccentTerrain("Pit")
                .FeatureTile("[Tan] Treasure 1", 2)
                .FeatureTile("[Tan] Treasure 2", 2)
                .FeatureTile("[Tan] Pillar 1")
                .FeatureTile("[Tan] Pillar 2")
                .FeatureTile("[Tan] Pillar 3")
                .SetPiece("[Tan] Platform 1 (2x2)")
                .SetPiece("[Tan] Platform 2 (2x2)")
                .SetPiece("[Tan] Platform 3 (2x2)")
                .SetPiece("[Tan] Platform 4 (1x2)")
                .SetPiece("[Tan] Platform 5 (1x2)")
                .SetPiece("[Tan] Pillar (1x2)", 2)
                .SetPiece("[Tan] Wall Section 1 (1x2)")
                .SetPiece("[Tan] Wall Section 2 (1x2)")
                .SetPiece("[Tan] Door - Big 1", 1)
                .SetPiece("[Tan] Door - Big 2", 1)
                .SetPiece("[Tan] Door - Bridge 1", 1)
                .SetPiece("[Tan] Door - Fence 1", 1)
                .SetPiece("[Tan] Door - Fence 2", 1)
                .SetPiece("[Tan] Door - Transition", 1)
                .SetPiece("[Tan] Stairs - Down", 1)
                .SetPiece("[Tan] Stairs - Up", 1)
                .SetPiece("[Tan] Stairs - Down (2x2)")
                .SetPiece("[Tan] Stairs - Up (2x2)")
                .ExitGroup("[Tan] Exit 1")
                .ExitGroup("[Tan] Exit 2");

            // Dungeon (tde01). The SWLOR hak copy (SWLOR_Haks/sw_t_dungeon/tde01.set) is an even
            // larger 1092-tile superset of vanilla tde01: same base Wall/Floor/Lava family as Crypt,
            // plus (a) a HasHeightTransition=1 raised-tile system (323 of the 1092 tiles are non-flat
            // -- see PilotExpectedExemptions' "requires height support" tag, the resolver's own
            // eligibility checks require flat tiles so these need height support before they're
            // reachable), (b)
            // four MORE Bridge-gated accent-channel terrain variants beyond Lava (Water/Sewer/Ice/Pit)
            // -- DungeonTilesetProfile has only one AccentTerrain slot, so only Lava is wired; the
            // other three are the same proven Bridge-channel shape, just out of scope for this pilot's
            // single accent slot, and (c) a "MazeMosaic" crosser outside the canonical vocabulary.
            // AccentTerrain("Lava") mirrors Cavern's Water / Sewers' Pit pattern.
            // Group names verified directly against the .set data. Only the base/no-suffix and
            // "-Lava"-suffixed groups are wired (matching the single AccentTerrain("Lava") slot);
            // the analogous Water/Sewer/Ice/Pit-suffixed groups (Exit 2, Platform 4, Pillar 1/2, Door
            // - Bridge 1) are the identical shape and are left for a future wave that either extends
            // DungeonTilesetProfile with more accent slots or ships a dedicated profile per palette.
            // Ramp GROUPS (1x1-wrapped "Ramp - Straight"/"Ramp - Corner, *") and the "Ramp" edge
            // crosser are still not wired -- LayoutElevationPainter's v1 scope only uses six ungrouped,
            // blank-edge, all-Floor tiles (TILE500/501/623/737/868/1002) whose normalized corner-height
            // deltas are a raised rectangle's two rim shapes (one corner raised; two adjacent corners
            // raised), verified live via TileResolver.HasHeightAwareCandidate rather than hardcoded
            // here. Confirmed by direct probe: Wall (this profile's solid terrain) NEVER carries a
            // nonzero corner height anywhere in tde01's 1092-tile inventory, so
            // LayoutElevationPainter's SolidTerrain-blob mechanism is structurally inert here (its own
            // shape probe correctly finds no rim vocabulary and paints zero); only the OpenTerrain
            // ("Floor") room-interior "split-level" mechanism has real support, raising a small floor
            // patch strictly inside a room via corner-height blending alone (no TunnelLink, no
            // Stairs-Up/Down group -- see LayoutElevationPainter class doc). MaxElevationRegions(2)
            // caps how many such patches a composition may request; the pass itself re-verifies every
            // candidate against the real tileset regardless.
            _builder.Create(Dungeon, "Dungeon")
                .Tileset("tde01")
                .MaxElevationRegions(2)
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .AccentTerrain("Lava")
                .FeatureTile("Treasure 1", 2)
                .FeatureTile("Treasure 2", 2)
                .FeatureTile("Pillar 3")
                .FeatureTile("Pillar 4")
                .FeatureTile("Pillar 1 - Lava")
                .FeatureTile("Pillar 2 - Lava")
                .SetPiece("Platform 1 (2x2)")
                .SetPiece("Platform 2 (2x2)")
                .SetPiece("Platform 3 (2x2)")
                .SetPiece("Platform 4 - Lava (1x2)")
                .SetPiece("Platform 5 (1x2)")
                .SetPiece("Pillar (1x2)", 2)
                .SetPiece("Energy Source (1x2)")
                .SetPiece("Wall Section 1 (1x2)")
                .SetPiece("Wall Section 2 (1x2)")
                .SetPiece("Door - Big 1", 1)
                .SetPiece("Door - Big 2", 1)
                .SetPiece("Door - Bridge 1, Lava", 1)
                .SetPiece("Door - Fence 1", 1)
                .SetPiece("Door - Fence 2", 1)
                .SetPiece("Door - Transition", 1)
                .SetPiece("Stairs - Down", 1)
                .SetPiece("Stairs - Up", 1)
                .SetPiece("Stairs - Down, Lava (2x2)")
                .SetPiece("Stairs - Up (2x2)")
                .ExitGroup("Exit 1")
                .ExitGroup("Exit 2 - Lava");

            // City Interior (tin01). Multi-room-type interior (Livingroom/Kitchen/Inn/Shop), each with
            // its own single-tile WallAlcove door group plus a themed furnished-room set piece.
            // PrimaryOpenTerrain left empty (defaults to the declared Floor terrain, "Inn" -- tied for
            // best coverage with the other three room terrains per the base-game tileset census). The "*Room01_1x2"/
            // "*Room02_1x2" two-tile door-entrance pairs (Livingroom/Kitchen/Inn/Shop, and Bordello)
            // are NOT wired -- each pairs a blank wall tile with a tile carrying BOTH a Doorway edge
            // crosser AND a door slot, which LayoutGroupStamper's WallRoom classification excludes
            // (WallRoom requires no door slot) and which isn't a trivial 1x1 group either (so the
            // door-transition tolerance doesn't apply); see TileCoverageCensusTests'
            // PilotExpectedExemptions for the exact accounting. Bedroom_1/2, Tent, Baracks, the three
            // Temple variants, Wizards Den, Smithy, Barn, SlumHome01/02, and HomeLower/Upper01-05 are
            // furnished-room set pieces verified flat/Wall-doorway-consistent with the existing
            // AncientRuin Room1-5 pattern.
            _builder.Create(CityInterior, "City Interior")
                .Tileset("tin01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .SetPiece("StairsUp", 1)
                .SetPiece("StairsDown", 1)
                .SetPiece("DoorLivingroom01", 1)
                .SetPiece("DoorKitchen01", 1)
                .SetPiece("DoorInn01", 1)
                .SetPiece("DoorShop01", 1)
                .SetPiece("DoorTrans", 1)
                .SetPiece("Bedroom_1")
                .SetPiece("Bedroom_2")
                .SetPiece("Tent")
                .SetPiece("Baracks")
                .SetPiece("Temple Evil")
                .SetPiece("Temple Good")
                .SetPiece("Temple Neutral")
                .SetPiece("Wizards Den")
                .SetPiece("Smithy")
                .SetPiece("Barn")
                .SetPiece("SlumHome01")
                .SetPiece("SlumHome02")
                .SetPiece("HomeLower01_2x2")
                .SetPiece("HomeLower02_2x2")
                .SetPiece("HomeLower03_2x2")
                .SetPiece("HomeLower04_2x2")
                .SetPiece("HomeLower05_2x2")
                .SetPiece("HomeUpper01_2x2")
                .SetPiece("HomeUpper02_2x2")
                .SetPiece("HomeUpper03_2x2")
                .ExitGroup("CorridorExit")
                .ExitGroup("CorridorExitBig");

            // Barrows Interior (tbw01, SWLOR_Haks/sw_t_barrow). 135-tile single-district tileset whose
            // GENERAL block declares BOTH Default and Floor as "black" (an authoring quirk -- the
            // tileset has no real "black is walkable floor" district; its actual open terrain is
            // "barrow") -- PrimaryOpenTerrain is therefore set explicitly rather than left to the
            // usual empty-means-declared-Floor default, which would otherwise wire solid==open and
            // break every corner/edge classification. No AccentTerrain: the only two terrains are
            // black/barrow, no third channel-capable terrain exists. CorridorDown_1x2/Corridor_Up_1x2/
            // Corridor_Up_1x2_02/SideChamber1 all carry a "corridor"/"door_barrow" crosser outside the
            // canonical Doorway-only multi-tile vocabulary and are excluded (see
            // TileCoverageCensusTests.PilotExpectedExemptions). FinalArea_7x7 is a large (49-tile),
            // fully solid-or-barrow decorative set piece (a boss/finale chamber) -- structurally a
            // valid OpenSetPiece like any smaller one, included at maxPerArea 1.
            _builder.Create(Barrows, "Barrows Interior")
                .Tileset("tbw01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .PrimaryOpenTerrain("barrow")
                .FeatureTile("Platform01_1x1")
                .FeatureTile("Depression1x1")
                .FeatureTile("Platform03_1x1")
                .FeatureTile("StoneFormation01")
                .FeatureTile("StoneFormation02")
                .FeatureTile("StoneCircle1x1")
                .SetPiece("WallSection1_1x1")
                .SetPiece("WallSection2_1x1")
                .SetPiece("ExitDown_2x1")
                .SetPiece("SecretPassage")
                .SetPiece("FinalArea_7x7")
                .ExitGroup("Exit_01");

            // Mines and Caverns (tdm01, SWLOR_Haks/sw_t_mine). The hak copy is a massive 1810-tile
            // superset of vanilla's 248-tile version: a HasHeightTransition=1 multi-district mega-set
            // carrying FOUR parallel themed districts under prefixed group names -- "[Cave]" (the
            // vanilla-equivalent core: Wall/Floor/Water/Pit/Lava/Ice on the canonical Corridor/Doorway/
            // Fence/Bridge vocabulary, plus a "Tracks" crosser outside it), "[Desert]", "[Organic]",
            // and "[City]" (each its own terrain family -- Desert/DesertWater/DesertPit/DesertLava,
            // Organic/OrganicWater/OrganicPit/OrganicSlime, CityWater/CityCastle -- and own crossers --
            // DesertCorridor/DesertTracks/OrganicCorridor/OrganicTracks/CityFence). Only "[Cave]" is
            // wired here (matching this profile's single AccentTerrain/PrimaryOpenTerrain slots); the
            // other three districts are the identical shape on alternate terrain/crosser vocabulary,
            // left for a future wave that either extends multi-district support or ships dedicated
            // profiles per palette -- see TileCoverageCensusTests.PilotAlternateVocabTerrains["tdm01"].
            // AccentTerrain("Water") is the one wired accent channel of [Cave]'s Water/Pit/Lava/Ice
            // quartet (mirrors Dungeon/tde01's single-accent-slot precedent); "[Cave] Door - Bridge,
            // Pit"/"Lava" are the same shape on the two unwired accents and excluded. "Ramp"/"Cave
            // Entrance" groups are raised (HasHeightTransition tiles) and excluded. "[Cave] Door -
            // Transition", "[Cave] Ship - Docked", "[Cave] Docks (1x2)" don't structurally classify
            // under any current mechanism and are excluded.
            _builder.Create(MinesAndCaverns, "Mines and Caverns")
                .Tileset("tdm01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .AccentTerrain("Water")
                .FeatureTile("[Cave] Treasure 1", 2)
                .FeatureTile("[Cave] Treasure 2 - Water", 2)
                .FeatureTile("[Cave] Treasure 2 - Lava", 2)
                .FeatureTile("[Cave] Pillar 1")
                .FeatureTile("[Cave] Pillar 2")
                .FeatureTile("[Cave] Ice Column")
                .FeatureTile("[Cave] Crystal Casket 1", 2)
                .FeatureTile("[Cave] Crystal Casket 2", 2)
                .FeatureTile("[Cave] Portal")
                .FeatureTile("[Cave] Chessboard")
                .FeatureTile("[Cave] Mineshaft")
                .SetPiece("[Cave] Door - Big 1", 1)
                .SetPiece("[Cave] Door - Big 2", 1)
                .SetPiece("[Cave] Door - Fence 1", 1)
                .SetPiece("[Cave] Door - Fence 2", 1)
                .SetPiece("[Cave] Door - Bridge, Water", 1)
                .SetPiece("[Cave] Stairs - Down 1", 1)
                .SetPiece("[Cave] Stairs - Up 1", 1)
                .SetPiece("[Cave] Stairs - Down (2x2)")
                .SetPiece("[Cave] Stairs - Up, Water (2x2)")
                .SetPiece("[Cave] Stairs - Up, Lava (2x2)")
                .SetPiece("[Cave] Platform 1 (2x2)")
                .SetPiece("[Cave] Platform 2 (2x2)")
                .SetPiece("[Cave] Platform 3 (2x2)")
                .SetPiece("[Cave] Platform 4 (1x2)")
                .SetPiece("[Cave] Platform 5 (1x2)")
                .SetPiece("[Cave] Pillar (1x2)", 2)
                .SetPiece("[Cave] Wall Section 1 - Water (1x2)")
                .SetPiece("[Cave] Wall Section 2 (1x2)")
                .SetPiece("[Cave] Wall Section 1 - Lava (1x2)")
                .SetPiece("[Cave] Portal (2x2)")
                .SetPiece("[Cave] Crystal Crypt 1")
                .SetPiece("[Cave] Crystal Crypt 2")
                .ExitGroup("[Cave] Exit 1")
                .ExitGroup("[Cave] Exit 2")
                .ExitGroup("[Cave] Exit 3");

            // Ruins (tdr01, SWLOR_Haks/sw_t_ruin). PrimaryOpenTerrain left empty (defaults to declared
            // Floor "Floor"; "Plaza" is a second fully-covered open terrain per the census but only one
            // PrimaryOpenTerrain slot exists, matching every other single-terrain profile here).
            // Channel-only accent: Chasm has verified Bridge-gated channel/bank coverage against Wall
            // (BridgeDoor01 classifies as a CorridorInsert(Bridge) gate), but zero crosser-free
            // Floor-and-Chasm-mixed corner tiles exist (verified directly against the .set data) -- an
            // Organic-style blob patch painted into open Floor space could never resolve, so
            // AccentTerrain stays empty and only ChannelTerrain is set, mirroring Ancient Ruin
            // (vmr01)'s own Chasm-vs-Plaza precedent exactly. This is the first Wave-2 tileset with a
            // verified Alley crosser (Doorway/Alley/Corridor/Fence/Bridge, 5 crossers) --
            // BigDoorAlley/ExteriorStairsDown/ExteriorStairsUp confirm Alley coverage, but Streets
            // layout pairing is out of scope for this wave (only Complex/Halls/Organic per this
            // onboarding's assignment) and left for a future wave. Excluded: Mosaic_Plaza_2x2,
            // ExteriorStairsDown/Up_2x2, ExteriorStage_2x2, ExteriorRuinedTower_2x2,
            // ExteriorWalkway_2x2, Amphitheater_2x2 (all REJECT -- corners mix Wall/Plaza in shapes that
            // don't satisfy any current classifier), ExteriorFenceDoor (non-canonical crosser
            // combination), Door_Trans/Door_Trans_Exterior (doorway-shape-mismatch).
            _builder.Create(Ruins, "Ruins")
                .Tileset("tdr01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .ChannelTerrain("Chasm")
                .FeatureTile("ExteriorFountain")
                .FeatureTile("ExteriorOvergrownGarden")
                .FeatureTile("ExteriorPool")
                .FeatureTile("InteriorRubble")
                .FeatureTile("RuinedHouse")
                .FeatureTile("Portal")
                .FeatureTile("Chessboard")
                .SetPiece("InteriorHallDoor", 1)
                .SetPiece("InteriorFenceDoor", 1)
                .SetPiece("BridgeDoor01", 1)
                .SetPiece("BigDoorAlley", 1)
                .SetPiece("InteriorStairsDown", 1)
                .SetPiece("InteriorStairsUp", 1)
                .SetPiece("ExteriorStairsDown", 1)
                .SetPiece("ExteriorStairsUp", 1)
                .SetPiece("SleepingPlatform")
                .SetPiece("ExteriorFountain_1x2")
                .SetPiece("InteriorMosaic_2x2")
                .SetPiece("WallFountain")
                .SetPiece("TentInterior_2x2")
                .SetPiece("DesertInterior1_2x2")
                .SetPiece("TurfhouseInterior_2x2")
                .SetPiece("DesertInterior2_2x2")
                .SetPiece("DesertInterior3_2x2")
                .ExitGroup("ExteriorExit01")
                .ExitGroup("ExteriorExit02");

            // Castle Interior (tic01, SWLOR_Haks/sw_t_castle1). PrimaryOpenTerrain left empty (defaults
            // to declared Floor "Stone"). This tileset's multi-room-type family (Storage/Rich/Library/
            // Jail, each with its own Room/Room1/Room2 group and Door groups) mirrors City Interior's
            // Livingroom/Kitchen/Inn/Shop shape, but here NONE of the alternate-terrain Door pieces
            // (Door - Storage/Rich/Library/Jail 1/2) structurally classify: their corners are
            // [AltTerrain, Wall, Wall, AltTerrain] with only Stone wired as the open terrain, so they
            // match neither OpenSetPiece (wrong open terrain) nor WallAlcove (not all-solid) -- verified
            // via direct corner inspection, not assumed. Only "Door - Stone 1/2" (open=Stone) is wired.
            // The Room-* / Room1/Room2 groups all carry a Doorway edge together with a door slot on the
            // same member tile (doorway-shape-mismatch -- WallRoom requires no door), so none of the
            // four alternate room-type families are reachable here at all (unlike City Interior's
            // WallAlcove-shaped rooms) -- a genuine gap in this tileset's authoring, not this profile's
            // curation. "Exit - Corridor"/"Exit - Corridor, Big" are named as exits but structurally
            // classify as CorridorStub (they carry a Corridor crosser, disqualifying them from
            // GroupExitPlanner's crosser-free ExitGroup rule) -- wired as SetPieces instead, matching
            // their real structural shape; no ExitGroup candidate exists in this tileset. Window-*
            // pieces (Window crosser), Maze-* pieces (MazeMosaic/MazeMarble crossers), and the separate
            // "[Tower]" brown/grey sub-district (own "Tower" terrain, no coverage) are all alternate
            // vocabulary and excluded.
            _builder.Create(CastleInterior, "Castle Interior")
                .Tileset("tic01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .FeatureTile("[Castle] Chessboard")
                .FeatureTile("[Castle] Portal")
                .FeatureTile("[Castle] Fountain")
                .SetPiece("[Castle] Door - Stone 1", 1)
                .SetPiece("[Castle] Door - Stone 2", 1)
                .SetPiece("[Castle] Stairs - Up", 1)
                .SetPiece("[Castle] Stairs - Down", 1)
                .SetPiece("[Castle] Exit - Corridor", 1)
                .SetPiece("[Castle] Exit - Corridor, Big", 1)
                .SetPiece("[Castle] Round Corner - Empty, Stone")
                .SetPiece("[Castle] Round Corner - Decorated, Stone")
                .SetPiece("[Castle] Stairs - Up, Stone Corner")
                .SetPiece("[Castle] Stairs - Down, Stone Corner")
                .SetPiece("[Castle] Dais");

            // Castle Interior 2 / TNO: Castle Interior (tni02, SWLOR_Haks/sw_t_tnocastle). Same family
            // as Castle Interior (tic01) -- Storage/Rich/Library/Jail/Stone room districts, lowercase
            // terrain/group naming, plus a "round" tower-stair sub-district (Round_1/Round_1st1-3/
            // Round_2x2up/down/updown/basement*) that is its own alternate-terrain vocabulary (only
            // "basement_1x2" -- all-Wall-cornered with a door slot -- structurally clears as
            // WallAlcove; every other Round_* piece either doesn't classify or references the "round"
            // terrain no profile here wires). Same DoorStorage/Rich/Library/Jail exclusion reasoning as
            // Castle Interior applies verbatim (corners are [AltTerrain, wall, wall, AltTerrain]);
            // DoorStone01/02 (open=stone) is wired. CorridorExit/CorridorExitBig are, like tic01's
            // "Exit - Corridor" pair, structurally CorridorStub (carry a Corridor crosser) rather than
            // ExitGroup-eligible -- wired as SetPieces; no ExitGroup candidate exists.
            _builder.Create(CastleInterior2, "Castle Interior 2")
                .Tileset("tni02")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .FeatureTile("Chessboard")
                .FeatureTile("Portal")
                .FeatureTile("Fountain")
                .SetPiece("DoorStone01", 1)
                .SetPiece("DoorStone02", 1)
                .SetPiece("StairsUp", 1)
                .SetPiece("StairsDown", 1)
                .SetPiece("CorridorExit", 1)
                .SetPiece("CorridorExitBig", 1)
                .SetPiece("basement_1x2");

            // Drow Interior (tid01, SWLOR_Haks/sw_t_drowint). PrimaryOpenTerrain left empty (defaults to
            // declared Floor "Floor2"; a separate "floor" terrain and a "2x2"-named terrain also exist
            // but neither is fully covered/declared -- "2x2" is only 5/16 combos per the census and is
            // not wired). Small, thin tileset (97 tiles, 12 groups): "Room (2x1)"/"Room - Bedroom (2x1)"
            // and the 1x1 "Room - Cell"/"Room" groups are genuine 1x1/2x1 WallRoom shapes (a single
            // perimeter Doorway edge each) rather than City Interior's WallAlcove pattern -- verified
            // directly, not assumed. "Temple - Drow (4x3)" and "Under Well (4x3)" are large all-solid-
            // cornered door-bearing set pieces (WallAlcove). No FeatureTile or ExitGroup candidate
            // exists in this tileset (no 1x1 crosser-free doorless pathnode-A group, and no 1x1
            // crosser-free door-bearing group either). Door - Maze End/Side, Mosaic pieces carry the
            // alternate MazeMosaic crosser and are excluded; Door - Transition is doorway-shape-
            // mismatched and excluded.
            _builder.Create(DrowInterior, "Drow Interior")
                .Tileset("tid01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .SetPiece("Stairs - Up", 1)
                .SetPiece("Stairs - Down", 1)
                .SetPiece("Temple - Drow (4x3)")
                .SetPiece("Room (2x1)")
                .SetPiece("Room - Bedroom (2x1)")
                .SetPiece("Room - Cell")
                .SetPiece("Room")
                .SetPiece("Under Well (4x3)");

            // Illithid Interior (tii01, SWLOR_Haks/sw_t_illithid). PrimaryOpenTerrain left empty
            // (defaults to declared Floor "Floor"). The smallest Wave-2 tileset (79 tiles, 10 groups):
            // only "Observation pit" and "Fighting Pit" (both 3x3, all-solid-cornered, door-bearing)
            // structurally clear as WallAlcove -- "Great Brain" (this tileset's signature centerpiece),
            // "Resting Pods"/"Resting Pod", and "Cell" all carry a Doorway edge together with a door
            // slot on the same member tile (doorway-shape-mismatch, same authoring gap as Castle
            // Interior's Room-* families) and are excluded. "Transporter" is the tileset's only
            // FeatureTile-eligible group (1x1, flat, crosser-free, doorless, pathnode A). No ExitGroup
            // candidate exists; "Transition Door" is doorway-shape-mismatched.
            _builder.Create(IllithidInterior, "Illithid Interior")
                .Tileset("tii01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .FeatureTile("Transporter")
                .SetPiece("Stairs up", 1)
                .SetPiece("Stairs Down", 1)
                .SetPiece("Observation pit")
                .SetPiece("Fighting Pit");

            // City Interior 2 / TNO: City Interior (tni01, SWLOR_Haks/sw_t_cityint2). Same
            // Livingroom/Kitchen/Inn/Shop room-type family as City Interior (tin01, the vanilla pilot),
            // plus many more furnished-room pieces (Home/Slum/Smithy/Tent/Barracks/Temple/Thatch Hut).
            // PrimaryOpenTerrain left empty (defaults to declared Floor "inn"). Only "DoorInn01" (open=
            // inn) clears among the four single-tile Door* pieces -- DoorLivingroom01/DoorKitchen01/
            // DoorShop01 have corners [AltTerrain, wall, wall, AltTerrain] like Castle Interior's
            // Storage/Rich/Library/Jail doors and don't classify. Every furnished-room piece here
            // (Shop01_1x2, HomeLower/Upper*, Tent, Baracks, the three Temples, Wizards Den, Smithy incl.
            // smithy2x2_1/2, Barn, SlumHome01/02, Shop02_1x2, ShipCabin_1x2, TentInterior_1x1/2x2/5x3
            // *_01-03, Thatch Hut 1/2) is all-Wall-cornered with a door slot -- WallAlcove, mirroring
            // City Interior's own furnished-room curation. "StairsUp"/"StairsDown"/"CorridorExit"/
            // "CorridorExitBig" all carry a Corridor crosser (CorridorStub, not ExitGroup-eligible,
            // unlike City Interior's vanilla tin01 where the analogous names WERE wired as ExitGroups --
            // this hak copy's real tile data differs) -- wired as SetPieces instead; no ExitGroup
            // candidate exists in this tileset. "Portal" is the only FeatureTile-eligible group
            // ("Chessboard" references the alternate "livingroom" terrain on all 4 corners and doesn't
            // classify). The *Room01_1x2/*Room02_1x2 door-entrance pairs and "Bordello" are excluded for
            // the same doorway-shape-mismatch reason as City Interior's pilot exemptions; the
            // LivingroomCorner*/KitchenCorner* stair/exit pieces reference alternate terrain corners and
            // don't classify either.
            _builder.Create(CityInterior2, "City Interior 2")
                .Tileset("tni01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .FeatureTile("Portal")
                .SetPiece("DoorInn01", 1)
                .SetPiece("StairsUp", 1)
                .SetPiece("StairsDown", 1)
                .SetPiece("CorridorExit", 1)
                .SetPiece("CorridorExitBig", 1)
                .SetPiece("Shop01_1x2")
                .SetPiece("Shop02_1x2")
                .SetPiece("HomeLower01_2x2")
                .SetPiece("HomeLower02_2x2")
                .SetPiece("HomeLower03_2x2")
                .SetPiece("HomeLower04_2x2")
                .SetPiece("HomeLower05_2x2")
                .SetPiece("HomeUpper01_2x2")
                .SetPiece("HomeUpper02_2x2")
                .SetPiece("HomeUpper03_2x2")
                .SetPiece("Tent")
                .SetPiece("Baracks")
                .SetPiece("Temple Evil")
                .SetPiece("Temple Good")
                .SetPiece("Temple Neutral")
                .SetPiece("Wizards Den")
                .SetPiece("Smithy")
                .SetPiece("smithy2x2_1")
                .SetPiece("smithy2x2_2")
                .SetPiece("Barn")
                .SetPiece("SlumHome01")
                .SetPiece("SlumHome02")
                .SetPiece("ShipCabin_1x2")
                .SetPiece("TentInterior_1x1_01")
                .SetPiece("TentInterior_1x1_02")
                .SetPiece("TentInterior_1x1_03")
                .SetPiece("TentInterior_2x2_01")
                .SetPiece("TentInterior_2x2_02")
                .SetPiece("TentInterior_2x2_03")
                .SetPiece("TentInterior_5x3_01")
                .SetPiece("TentInterior_5x3_02")
                .SetPiece("TentInterior_5x3_03")
                .SetPiece("Thatch Hut 1")
                .SetPiece("Thatch Hut 2");

            // Steamworks (tsw01, SWLOR_Haks/sw_t_steamwork). Same Wall/Floor/Pit family and Corridor/
            // Doorway/Bridge/Fence vocabulary as Crypt/Dungeon/Sewers (tdc01/tde01/tds01) -- PrimaryOpen
            // Terrain left empty (defaults to declared Floor "floor"). AccentTerrain("pit") mirrors
            // Sewers' own Pit-channel pattern. Door_Trans is doorway-shape-mismatched and excluded,
            // matching the identical tile in the Crypt/Dungeon/Sewers family.
            _builder.Create(Steamworks, "Steamworks")
                .Tileset("tsw01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .AccentTerrain("pit")
                .FeatureTile("Treasure01", 2)
                .FeatureTile("Treasure02", 2)
                .FeatureTile("Pillar01")
                .FeatureTile("Pillar02")
                .FeatureTile("Pillar03")
                .FeatureTile("Camp")
                .FeatureTile("Portal")
                .FeatureTile("Chessboard")
                .SetPiece("BigDoor01", 1)
                .SetPiece("BigDoor02", 1)
                .SetPiece("BridgeDoor01", 1)
                .SetPiece("FenceDoor01", 1)
                .SetPiece("FenceDoor02", 1)
                .SetPiece("StairsDown", 1)
                .SetPiece("StairsUp", 1)
                .SetPiece("StairsDown_2x2")
                .SetPiece("StairsUp_2x2")
                .SetPiece("Platform01_2x2")
                .SetPiece("Platform02_2x2")
                .SetPiece("Platform03_2x2")
                .SetPiece("Platform04_1x2")
                .SetPiece("Platform05_1x2")
                .SetPiece("Pillar_1x2", 2)
                .SetPiece("WallSection01_2x1")
                .SetPiece("WallSection02_2x1")
                .SetPiece("CampWall")
                .ExitGroup("Exit01")
                .ExitGroup("Exit02");

            // Fort Interior / TNO: Fort Interior (twc03, SWLOR_Haks/sw_t_fortint). GENERAL declares
            // BOTH Default and Floor as "black" (the same authoring quirk as Barrows) -- PrimaryOpen
            // Terrain is set explicitly to "floor" rather than left empty. AccentTerrain("water")
            // mirrors the Water-channel pattern; only "wall" (a crosser name here, not a terrain) and no
            // canonical Bridge-gated door onto water was found, so no Bridge-crosser SetPiece is wired,
            // matching the tileset's own actual door inventory. Many groups are prefixed "OLD_" (legacy
            // authored content the tileset keeps for back-compat) but still structurally classify as
            // CorridorStub (all-solid-cornered, single Corridor edge, e.g. OLD_Bedroom_01_1x1/
            // OLD_Library_1x1/OLD_Storage_1x1/OLD_Generic_Room_1x1/OLD_Cells_1x1) -- included per this
            // profile's own precedent of registering structurally-valid pieces regardless of a
            // "legacy"-sounding name; their non-"OLD_" replacements (Storage_1x1/Cells_1x1/Library_1x1/
            // Generic_Room_1x1/Bedroom_01_1x1/Barracks_2x2/Smithy_1x2/Kitchen_1x2/Portal_Hall_2x3/the
            // Doorw_SpiralStair_* trio) all carry a Doorway edge together with a door slot on the same
            // tile (doorway-shape-mismatch) and are excluded, the same authoring gap seen in Castle
            // Interior/Illithid Interior. No FeatureTile-eligible group exists in this tileset.
            // "Exit_1x1"/"Exit_Down_1x1"/"Exit_CollapsedWall" are the genuine crosser-free door-bearing
            // ExitGroup candidates; "Storage_1x1_1"/"Stairway_up"/"Stairway_down" carry the identical
            // structural shape (floor/floor/black/black corners, a door slot, no crosser) but read as
            // furnished-room decor by name, so they are wired as SetPieces instead.
            _builder.Create(FortInterior, "Fort Interior")
                .Tileset("twc03")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .PrimaryOpenTerrain("floor")
                .AccentTerrain("water")
                .SetPiece("Arena_1x2")
                .SetPiece("Storage_1x1_1")
                .SetPiece("Dais_1x2")
                .SetPiece("Stairway_up")
                .SetPiece("Stairway_down")
                .SetPiece("Corr_SpiralStair_updown", 1)
                .SetPiece("Corr_SpiralStair_up", 1)
                .SetPiece("Corr_SpiralStair_down", 1)
                .SetPiece("OLD_Bedroom_01_1x1", 1)
                .SetPiece("OLD_Cells_1x1", 1)
                .SetPiece("OLD_Library_1x1", 1)
                .SetPiece("OLD_Storage_1x1", 1)
                .SetPiece("OLD_Generic_Room_1x1", 1)
                .SetPiece("Corridor_Exit", 1)
                .SetPiece("Room_1x2")
                .SetPiece("LargeGate_1x2")
                .SetPiece("LargeGate_Exit")
                .SetPiece("Fireplace")
                .SetPiece("Platform_1x2_01")
                .ExitGroup("Exit_1x1")
                .ExitGroup("Exit_Down_1x1")
                .ExitGroup("Exit_CollapsedWall");

            return _builder.Build();
        }
    }
}
