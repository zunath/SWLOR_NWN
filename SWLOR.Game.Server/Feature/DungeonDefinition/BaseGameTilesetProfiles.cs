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

        // tde01's Water/Sewer/Ice/Pit accent-slot palettes -- PaletteVariant profiles recomposing the
        // SAME tde01 hak data the base Dungeon profile above uses (same Wall/Floor, a different single
        // AccentTerrain slot -- see DungeonWater's own doc comment).
        public const string DungeonWater = "dungeon_water";
        public const string DungeonSewer = "dungeon_sewer";
        public const string DungeonIce = "dungeon_ice";
        public const string DungeonPit = "dungeon_pit";
        public const string CityInterior = "cityinterior";

        // Palette-variant profiles: recompose an already-onboarded tileset resref against one of its
        // alternate district/palette families (see DungeonTilesetProfile.IsPaletteVariant). Registered
        // to close TileCoverageCensusTests' "alternate-palette/decorative vocabulary" exemption bucket --
        // the census counts a tile reachable if ANY profile sharing its TilesetResref composes it.
        public const string CryptGrey = "crypt_grey";
        public const string CryptDwarven = "crypt_dwarven";
        public const string MinesAndCavernsDesert = "minescaverns_desert";
        public const string MinesAndCavernsOrganic = "minescaverns_organic";
        public const string RuinsPlaza = "ruins_plaza";

        // Second, independent alternate Tunnel body/port families on the SAME tdm01 districts as the
        // three profiles above -- see MinesAndCavernsTracks/MinesAndCavernsDesertTracks/
        // MinesAndCavernsOrganicTracks's own doc comments for why a dedicated profile is required (a
        // composition carries only one Tunnel body/port slot, already claimed by Corridor/DesertCorridor/
        // OrganicCorridor in the profiles above).
        public const string MinesAndCavernsTracks = "minescaverns_tracks";
        // [City]'s CityWater accent family -- see the MinesAndCavernsCity profile's own doc comment.
        public const string MinesAndCavernsCity = "minescaverns_city";
        public const string MinesAndCavernsDesertTracks = "minescaverns_desert_tracks";
        public const string MinesAndCavernsOrganicTracks = "minescaverns_organic_tracks";

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

        // tic01's Storage/Rich/Library/Jail district palettes -- PaletteVariant profiles recomposing the
        // SAME tic01 hak data the base CastleInterior profile above uses. See CastleInteriorStorage's own
        // doc comment.
        public const string CastleInteriorStorage = "castleinterior_storage";
        public const string CastleInteriorRich = "castleinterior_rich";
        public const string CastleInteriorLibrary = "castleinterior_library";
        public const string CastleInteriorJail = "castleinterior_jail";
        public const string DrowInterior = "drowinterior";
        public const string IllithidInterior = "illithidinterior";
        public const string CityInterior2 = "cityinterior2";
        public const string Steamworks = "steamworks";
        public const string FortInterior = "fortinterior";
        public const string FortInteriorLegacy = "fortinterior_legacy";

        // Wave-3: the first EXTERIOR base-game tilesets (ttd01/ttf01/ttf02) -- see the base-game tileset
        // census (TileCoverageCensusTests.PilotTilesetKeys) for coverage numbers and
        // TileCoverageCensusTests.PilotExpectedExemptions for the exact accounting. Resolution note:
        // ttd01 and ttf01 have SWLOR hak copies (SWLOR_Haks/sw_t_tatooine/ttd01.set, 388 tiles, and
        // SWLOR_Haks/sw_t_forest/ttf01.set, 1148 tiles -- both HasHeightTransition=1 supersets of the
        // 212/168-tile vanilla versions the 2026-07-12 census was run against), and TilesetSetSource
        // resolves hak copies FIRST, so these two profiles are curated against the hak data. ttf02 is
        // genuinely BIF-only (no hak copy exists); it resolves from basegame_sets/ttf02.set, the
        // committed vanilla extraction (HasHeightTransition=0, fully flat).
        //
        // All three share the SAME degenerate GENERAL quirk: Default and Floor are declared as the
        // SAME terrain (Desert/Forest) -- and, worse than Barrows' variant of it, that shared terrain
        // is the WALKABLE ground, not the wall: the fully-Desert/fully-Forest tiles are pathnode A,
        // while the fully-Cliff tile is pathnode-restricted ('T') in all three (verified directly
        // against the raw .set pathnode data). Composing with the engine's usual SolidTerrain=Default
        // rule would therefore carve UNWALKABLE cliff "rooms" out of a walkable "wall" mass --
        // inverted for gameplay. These profiles are the reason
        // DungeonTilesetProfile.SolidTerrainOverride exists: each declares SolidTerrainOverride
        // ("Cliff") + PrimaryOpenTerrain("Desert"/"Forest"), giving real dungeon-style enclosure --
        // impassable cliff walls (including the area's forced solid border ring) around walkable
        // desert/forest clearings. Coverage is symmetric (the Desert/Forest-vs-Cliff simple-tile pool
        // covers all 16 combos under either orientation of the pair, verified directly), so the
        // inversion costs nothing structurally.
        //
        // Tunnel vocabulary: NONE under the Cliff solid. Every crosser family in all three tilesets
        // (Wall/Road/Trench/Stream/..., each a same-name body/port pair) resolves its full shape
        // inventory ONLY against Solid=Desert/Forest compositions (verified directly via
        // TunnelVocabularyCheck.SupportsTunnels over every ordered crosser pairing) -- roads, walls
        // and streams all run across the walkable ground, and nothing crosses the cliff mass -- so
        // Complex's Tunnel mode downgrades to OpenLane for all three (the Barrows/Crypt-Dwarven
        // fallback, locked in by TunnelVocabularyCheckTests.ExpectedUnsupported).
        //
        // PathNodeOpeningWidthAudit (with the profile's own solid/open pair) confirms
        // MinimumOpeningWidth stays the default 1 for all three (partial Cliff-vs-Desert/Forest
        // combos carry pathnode-A candidates) -- locked in by
        // OnboardedTilesetPipelineTests.MinimumOpeningWidth_MatchesFreshPathNodeAudit.
        public const string Desert = "desert";
        public const string Forest = "forest";
        public const string ForestFacelift = "forest_facelift";
        public const string ForestPlatform = "forest_platform";
        public const string ForestRural = "forest_rural";

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

            // Crypt (Grey) -- tdc01's "[Grey]" decorative palette, a PaletteVariant profile recomposing
            // the SAME tdc01 hak data the base Crypt profile above uses. Verified by direct probe
            // (VariantProbe against the real SWLOR_Haks-resolved tdc01.set): solid stays "Wall" (the
            // .set has only ONE wall terrain shared by every district -- Tan/Grey/Dwarven differ purely
            // in floor/pit coloring, not wall art), PrimaryOpenTerrain("GreyFloor") has full 16/16
            // simple-tile corner coverage against Wall (same eligible-tile pool/pathnode-A distribution
            // as Tan's Floor), and PathNodeOpeningWidthAudit confirms MinimumOpeningWidth stays the
            // default 1. AccentTerrain("GreyPit") mirrors Tan's Pit -- "[Grey] Door - Bridge 1" is an
            // all-GreyPit-cornered Bridge-gated adapter, the identical shape. "[Grey] Door - Fence 1/2"
            // and "[Grey] Door - Transition" use the CANONICAL Fence/Doorway crosser names (not a
            // Grey-prefixed variant), so they compose normally. TunnelCrossers("GreyCorridor", "Doorway")
            // declares the district's own body-renamed Tunnel vocabulary -- verified via
            // TunnelVocabularyCheck.SupportsTunnels(..., CorridorCrosserType.Custom, "GreyCorridor",
            // "Doorway") returning true (the district keeps the CANONICAL "Doorway" port -- only the
            // body chain is renamed -- confirmed directly: "[Grey] Door - Transition"/TILE675 and the
            // GreyFloor|Wall boundary tile TILE515 both carry a literal "Doorway" edge, never a
            // "GreyDoorway"). This closes the last real capability gap: "[Grey] Door - Big 1/2"
            // (TILE575/640, a GreyCorridor opposite-pair) and "[Grey] Stairs - Down/Up" (TILE578/579, a
            // GreyCorridor single-edge dead end) now classify as SetPieceCorridorInsert/
            // SetPieceCorridorStub the same way tdt01's BigDoor01/02 and StairsDown01/StairsUp01 always
            // have (see LayoutGroupStamper.CorridorInsertCrossersFor/CorridorStubCrossersFor). Every
            // other Grey group (Platforms, Wall Sections, Pillar, Stairs 2x2, Treasure, Chessboard,
            // Portal, Mass Grave, Exit 1/2) mirrors Tan's own wired set piece/feature-tile/exit-group
            // shapes tile-for-tile. IsPaletteVariant() excludes this profile from --matrix's full
            // tileset x layout cross-product (see SWLOR.ProcgenReview/Program.cs) -- it gets one
            // showcase area instead.
            _builder.Create(CryptGrey, "Crypt (Grey)")
                .Tileset("tdc01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .PaletteVariant()
                .PrimaryOpenTerrain("GreyFloor")
                .AccentTerrain("GreyPit")
                .TunnelCrossers("GreyCorridor", "Doorway")
                .FeatureTile("[Grey] Treasure 1", 2)
                .FeatureTile("[Grey] Treasure 2", 2)
                .FeatureTile("[Grey] Pillar 1")
                .FeatureTile("[Grey] Pillar 2")
                .FeatureTile("[Grey] Pillar 3")
                .FeatureTile("[Grey] Portal")
                .FeatureTile("[Grey] Chessboard")
                .FeatureTile("[Grey] Mass Grave")
                .SetPiece("[Grey] Platform 1 (2x2)")
                .SetPiece("[Grey] Platform 2 (2x2)")
                .SetPiece("[Grey] Platform 3 (2x2)")
                .SetPiece("[Grey] Platform 4 (1x2)")
                .SetPiece("[Grey] Platform 5 (1x2)")
                .SetPiece("[Grey] Pillar (1x2)", 2)
                .SetPiece("[Grey] Wall Section 1 (1x2)")
                .SetPiece("[Grey] Wall Section 2 (1x2)")
                .SetPiece("[Grey] Door - Bridge 1", 1)
                .SetPiece("[Grey] Door - Fence 1", 1)
                .SetPiece("[Grey] Door - Fence 2", 1)
                .SetPiece("[Grey] Door - Transition", 1)
                .SetPiece("[Grey] Door - Big 1", 1)
                .SetPiece("[Grey] Door - Big 2", 1)
                .SetPiece("[Grey] Stairs - Down", 1)
                .SetPiece("[Grey] Stairs - Up", 1)
                .SetPiece("[Grey] Stairs - Down (2x2)")
                .SetPiece("[Grey] Stairs - Up (2x2)")
                .ExitGroup("[Grey] Exit 1")
                .ExitGroup("[Grey] Exit 2");

            // Crypt (Dwarven) -- tdc01's "[Dwarven]" decorative palette, same PaletteVariant shape as
            // Crypt (Grey) immediately above. Verified by direct probe: solid "Wall" (shared) vs
            // PrimaryOpenTerrain("DwarvenFloor") also has full 16/16 simple-tile coverage, and
            // PathNodeOpeningWidthAudit confirms MinimumOpeningWidth 1. AccentTerrain("DwarvenPit")
            // mirrors Grey/Tan's Pit-channel Bridge adapter ("[Dwarven] Door - Bridge 1"). Unlike Grey,
            // BOTH of Dwarven's district crossers are non-canonical -- "DwarvenCorridor" (Door - Big 1/2,
            // Stairs - Down/Up 1x1) AND "DwarvenDoorway" (Door - Transition) -- so this palette has no
            // wired corridor-stub or door-transition adapter at all; only the canonical Fence ("[Dwarven]
            // Door - Fence 1/2") and Bridge crossers compose. This narrower crosser vocabulary is the
            // same real capability gap as Crypt (Grey)'s GreyCorridor exclusion, just larger here:
            // because [Dwarven] keeps even its DOOR TRANSITIONS on the non-canonical "DwarvenDoorway"
            // (where [Grey]/[Tan] use the canonical "Doorway"), no Doorway-port boundary shape exists
            // against DwarvenFloor at all -- confirmed even under the generalized Custom-vocabulary
            // probe (TunnelVocabularyCheck.SupportsTunnels(..., CorridorCrosserType.Custom,
            // "DwarvenCorridor", "DwarvenDoorway") returns false), so TunnelCrossers() is deliberately
            // NOT called here: unlike Grey/Desert/Organic, this isn't a body-only rename with an intact
            // canonical port, it's a genuine missing boundary shape (locked in by
            // TunnelVocabularyCheckTests.ExpectedUnsupported), and MacroLayoutGenerator downgrades
            // Complex's Tunnel mode to OpenLane before dispatch -- the same machinery as Barrows'
            // missing-Doorway gap, verified green in OnboardedTilesetPipelineTests. "[Dwarven] Cave
            // Entrance (2x1)" mixes THREE terrains (DwarvenFloor/DwarvenPit/
            // Wall) in one group -- outside ClassifyMultiTileSetPiece's two-terrain (Solid/Open or
            // Solid/Secondary) shape -- and stays exempted, a genuine structural gap shared with the
            // base Crypt profile's own scope (multi-terrain sets are never wired anywhere in this file).
            _builder.Create(CryptDwarven, "Crypt (Dwarven)")
                .Tileset("tdc01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .PaletteVariant()
                .PrimaryOpenTerrain("DwarvenFloor")
                .AccentTerrain("DwarvenPit")
                .FeatureTile("[Dwarven] Treasure 1", 2)
                .FeatureTile("[Dwarven] Treasure 2", 2)
                .FeatureTile("[Dwarven] Pillar 1")
                .FeatureTile("[Dwarven] Pillar 2")
                .FeatureTile("[Dwarven] Pillar 3")
                .FeatureTile("[Dwarven] Portal")
                .FeatureTile("[Dwarven] Chessboard")
                .SetPiece("[Dwarven] Platform 1 (2x2)")
                .SetPiece("[Dwarven] Platform 2 (2x2)")
                .SetPiece("[Dwarven] Platform 3 (2x2)")
                .SetPiece("[Dwarven] Platform 4 (1x2)")
                .SetPiece("[Dwarven] Platform 5 (1x2)")
                .SetPiece("[Dwarven] Platform 6 (1x2)")
                .SetPiece("[Dwarven] Pillar (1x2)", 2)
                .SetPiece("[Dwarven] Door - Bridge 1", 1)
                .SetPiece("[Dwarven] Door - Fence 1", 1)
                .SetPiece("[Dwarven] Door - Fence 2", 1)
                .SetPiece("[Dwarven] Stairs - Down (2x2)")
                .SetPiece("[Dwarven] Stairs - Up (2x2)")
                .ExitGroup("[Dwarven] Exit 1")
                .ExitGroup("[Dwarven] Exit 2");

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
            // The 1x1-GROUPed "Ramp - Straight"/"Ramp - Corner, *" pieces are now wired via
            // LayoutGroupStamper's ReliefPiece kind (non-flat 1x1 pieces stamped onto painted
            // height-matching cells -- see the SetPiece calls below and the accent variants' own
            // "Ramp - Corner, <Accent>" analogs).
            // Confirmed by direct probe: Wall (this profile's solid terrain) NEVER carries a nonzero
            // corner height anywhere in tde01's 1092-tile inventory, so LayoutElevationPainter's
            // SolidTerrain-blob mechanism is structurally inert here (its own shape probe correctly
            // finds no rim vocabulary and paints zero); only the OpenTerrain ("Floor") room-interior
            // "split-level" mechanism has real support, raising a small floor patch strictly inside a
            // room via corner-height blending alone -- optionally with a Ramp edge-crosser lane spliced
            // into one rim edge when MacroLayoutParameters.ElevationRamps is enabled (see
            // LayoutElevationPainter.TryAddRampLane, live-probed against the 32 ungrouped "Ramp"
            // edge-crosser tiles, e.g. TILE560-562). MaxElevationRegions(2) caps how many split-level
            // patches a composition may request; the pass itself re-verifies every candidate against
            // the real tileset regardless. MaxPoolRegions(2) similarly caps LayoutElevationPoolPainter's
            // depth pools (a small Lava-terrain interior sunk one story below a raised Floor rim,
            // reusing the identical rectangle/rim machinery) -- live-probed against the Floor/Lava
            // mixed-terrain, mixed-height tile family (e.g. TILE505/506/510/521...) this pilot's earlier
            // census left height-exempted.
            _builder.Create(Dungeon, "Dungeon")
                .Tileset("tde01")
                .MaxElevationRegions(2)
                .MaxPoolRegions(2)
                // Per-corner relief (LayoutReliefPainter): tde01's raised inventory is dominated by
                // per-corner-independent (terrain, height) content -- mixed Floor/accent cells at
                // mixed grades, accent corners at two different heights within one cell, same-terrain
                // diagonal saddles -- verified reachable corner-by-corner via the painter's own
                // perturb-and-verify probes (see TileCoverageCensusTests.IsTerrainReliefReachable's
                // BFS mirror, which closes this tileset's entire former height-exemption bucket).
                .MaxReliefRegions(2)
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
                // Baked-mesh ramp pieces (1x1 GROUPs, non-flat): stamped by LayoutGroupStamper's
                // ReliefPiece kind onto cells whose PAINTED corner (terrain, height) field exactly
                // matches each piece's own profile -- "Ramp - Straight" straddles a raised patch's
                // straight rim edge ([Floor 0,0,1,1]), "Ramp - Corner, Floor" its convex corner, and
                // "Ramp - Corner, Lava" a raised Lava corner against flat Floor. Sites are produced by
                // the elevation/pool/relief passes above.
                .SetPiece("Ramp - Straight", 1)
                .SetPiece("Ramp - Corner, Floor", 1)
                .SetPiece("Ramp - Corner, Lava", 1)
                .ExitGroup("Exit 1")
                .ExitGroup("Exit 2 - Lava");

            // Dungeon (Water/Sewer/Ice/Pit) -- tde01's four other accent-slot palettes, PaletteVariant
            // profiles recomposing the SAME tde01 hak data the base Dungeon profile above uses: identical
            // Wall/Floor (no PrimaryOpenTerrain override needed), just a different single AccentTerrain.
            // "Door - Bridge 1, <Accent>" (all-<Accent>-cornered, an opposite Bridge-crosser pair,
            // verified directly against the .set data for all four) is this bucket's actual gap: it only
            // classifies as SetPieceCorridorInsert(Bridge) when vocab.Channel/Accent equals that specific
            // accent, which the base profile's single AccentTerrain("Lava") slot can't also be. Water/
            // Sewer/Ice additionally have their own "Platform 4 - <Accent> (1x2)"/"Exit 2 - <Accent>"/
            // "Pillar 1/2 - <Accent>" analogs of the base profile's own Lava-specific pieces (Pit has
            // neither -- verified directly, only its Door - Bridge piece exists); these were already
            // structurally reachable regardless of any profile's AccentTerrain declaration (FeatureTile/
            // ExitGroup/OpenSetPiece eligibility don't depend on it), but are re-wired here too so each
            // variant is fully self-sufficient at composition time, mirroring RuinsPlaza's own precedent.
            // "Ramp - Corner, <Accent>" is a raised (HasHeightTransition) 1x1 piece, wired via
            // LayoutGroupStamper's ReliefPiece kind the same way the base profile's own Ramp pieces
            // now are; each variant also declares the full MaxElevationRegions/MaxPoolRegions/
            // MaxReliefRegions(2) trio the base profile carries -- the vocabulary is per-accent
            // symmetric (verified by the same probes: every accent family carries the identical
            // rim/pool-bank/relief shapes, see TileCoverageCensusTests). PaletteVariant() excludes
            // each from --matrix's full cross-product -- one showcase area each instead.
            _builder.Create(DungeonWater, "Dungeon (Water)")
                .Tileset("tde01")
                .MaxElevationRegions(2)
                .MaxPoolRegions(2)
                .MaxReliefRegions(2)
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .PaletteVariant()
                .AccentTerrain("Water")
                .FeatureTile("Treasure 1", 2)
                .FeatureTile("Treasure 2", 2)
                .FeatureTile("Pillar 3")
                .FeatureTile("Pillar 4")
                .FeatureTile("Pillar 1 - Water")
                .FeatureTile("Pillar 2 - Water")
                .SetPiece("Platform 1 (2x2)")
                .SetPiece("Platform 2 (2x2)")
                .SetPiece("Platform 3 (2x2)")
                .SetPiece("Platform 4 - Water (1x2)")
                .SetPiece("Platform 5 (1x2)")
                .SetPiece("Pillar (1x2)", 2)
                .SetPiece("Energy Source (1x2)")
                .SetPiece("Wall Section 1 (1x2)")
                .SetPiece("Wall Section 2 (1x2)")
                .SetPiece("Door - Big 1", 1)
                .SetPiece("Door - Big 2", 1)
                .SetPiece("Door - Bridge 1, Water", 1)
                .SetPiece("Door - Fence 1", 1)
                .SetPiece("Door - Fence 2", 1)
                .SetPiece("Door - Transition", 1)
                .SetPiece("Stairs - Down", 1)
                .SetPiece("Stairs - Up", 1)
                .SetPiece("Stairs - Up (2x2)")
                .SetPiece("Ramp - Straight", 1)
                .SetPiece("Ramp - Corner, Floor", 1)
                .SetPiece("Ramp - Corner, Water", 1)
                .ExitGroup("Exit 1")
                .ExitGroup("Exit 2 - Water");

            _builder.Create(DungeonSewer, "Dungeon (Sewer)")
                .Tileset("tde01")
                .MaxElevationRegions(2)
                .MaxPoolRegions(2)
                .MaxReliefRegions(2)
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .PaletteVariant()
                .AccentTerrain("Sewer")
                .FeatureTile("Treasure 1", 2)
                .FeatureTile("Treasure 2", 2)
                .FeatureTile("Pillar 3")
                .FeatureTile("Pillar 4")
                .FeatureTile("Pillar 1 - Sewer")
                .FeatureTile("Pillar 2 - Sewer")
                .SetPiece("Platform 1 (2x2)")
                .SetPiece("Platform 2 (2x2)")
                .SetPiece("Platform 3 (2x2)")
                .SetPiece("Platform 4 - Sewer (1x2)")
                .SetPiece("Platform 5 (1x2)")
                .SetPiece("Pillar (1x2)", 2)
                .SetPiece("Energy Source (1x2)")
                .SetPiece("Wall Section 1 (1x2)")
                .SetPiece("Wall Section 2 (1x2)")
                .SetPiece("Door - Big 1", 1)
                .SetPiece("Door - Big 2", 1)
                .SetPiece("Door - Bridge 1, Sewer", 1)
                .SetPiece("Door - Fence 1", 1)
                .SetPiece("Door - Fence 2", 1)
                .SetPiece("Door - Transition", 1)
                .SetPiece("Stairs - Down", 1)
                .SetPiece("Stairs - Up", 1)
                .SetPiece("Stairs - Up (2x2)")
                .SetPiece("Ramp - Straight", 1)
                .SetPiece("Ramp - Corner, Floor", 1)
                .SetPiece("Ramp - Corner, Sewer", 1)
                .ExitGroup("Exit 1")
                .ExitGroup("Exit 2 - Sewer");

            _builder.Create(DungeonIce, "Dungeon (Ice)")
                .Tileset("tde01")
                .MaxElevationRegions(2)
                .MaxPoolRegions(2)
                .MaxReliefRegions(2)
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .PaletteVariant()
                .AccentTerrain("Ice")
                .FeatureTile("Treasure 1", 2)
                .FeatureTile("Treasure 2", 2)
                .FeatureTile("Pillar 3")
                .FeatureTile("Pillar 4")
                .FeatureTile("Pillar 1 - Ice")
                .FeatureTile("Pillar 2 - Ice")
                .SetPiece("Platform 1 (2x2)")
                .SetPiece("Platform 2 (2x2)")
                .SetPiece("Platform 3 (2x2)")
                .SetPiece("Platform 4 - Ice (1x2)")
                .SetPiece("Platform 5 (1x2)")
                .SetPiece("Pillar (1x2)", 2)
                .SetPiece("Energy Source (1x2)")
                .SetPiece("Wall Section 1 (1x2)")
                .SetPiece("Wall Section 2 (1x2)")
                .SetPiece("Door - Big 1", 1)
                .SetPiece("Door - Big 2", 1)
                .SetPiece("Door - Bridge 1, Ice", 1)
                .SetPiece("Door - Fence 1", 1)
                .SetPiece("Door - Fence 2", 1)
                .SetPiece("Door - Transition", 1)
                .SetPiece("Stairs - Down", 1)
                .SetPiece("Stairs - Up", 1)
                .SetPiece("Stairs - Up (2x2)")
                .SetPiece("Ramp - Straight", 1)
                .SetPiece("Ramp - Corner, Floor", 1)
                .SetPiece("Ramp - Corner, Ice", 1)
                .ExitGroup("Exit 1")
                .ExitGroup("Exit 2 - Ice");

            _builder.Create(DungeonPit, "Dungeon (Pit)")
                .Tileset("tde01")
                .MaxElevationRegions(2)
                .MaxPoolRegions(2)
                .MaxReliefRegions(2)
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .PaletteVariant()
                .AccentTerrain("Pit")
                .FeatureTile("Treasure 1", 2)
                .FeatureTile("Treasure 2", 2)
                .FeatureTile("Pillar 3")
                .FeatureTile("Pillar 4")
                .SetPiece("Platform 1 (2x2)")
                .SetPiece("Platform 2 (2x2)")
                .SetPiece("Platform 3 (2x2)")
                .SetPiece("Platform 5 (1x2)")
                .SetPiece("Pillar (1x2)", 2)
                .SetPiece("Energy Source (1x2)")
                .SetPiece("Wall Section 1 (1x2)")
                .SetPiece("Wall Section 2 (1x2)")
                .SetPiece("Door - Big 1", 1)
                .SetPiece("Door - Big 2", 1)
                .SetPiece("Door - Bridge 1, Pit", 1)
                .SetPiece("Door - Fence 1", 1)
                .SetPiece("Door - Fence 2", 1)
                .SetPiece("Door - Transition", 1)
                .SetPiece("Stairs - Down", 1)
                .SetPiece("Stairs - Up", 1)
                .SetPiece("Stairs - Up (2x2)")
                .SetPiece("Ramp - Straight", 1)
                .SetPiece("Ramp - Corner, Floor", 1)
                .SetPiece("Ramp - Corner, Pit", 1)
                .ExitGroup("Exit 1");

            // City Interior (tin01). Multi-room-type interior (Livingroom/Kitchen/Inn/Shop), each with
            // its own single-tile WallAlcove door group plus a themed furnished-room set piece.
            // PrimaryOpenTerrain left empty (defaults to the declared Floor terrain, "Inn" -- tied for
            // best coverage with the other three room terrains per the base-game tileset census). The
            // "*Room01_1x2"/"*Room02_1x2" two-tile door-entrance pairs (Livingroom/Kitchen/Inn/Shop, and
            // Bordello) each pair a blank wall tile with a tile carrying BOTH a Doorway edge crosser AND
            // a door slot -- LayoutGroupStamper's WallRoom classification now tolerates this shape (see
            // that method's own doc comment on the door-slot relaxation, closed alongside Castle
            // Interior/Illithid Interior/City Interior 2/Fort Interior's own equivalent families), so
            // all nine are wired as SetPieces here; see TileCoverageCensusTests' PilotExpectedExemptions
            // for the exact accounting. Bedroom_1/2, Tent, Baracks, the three Temple variants, Wizards
            // Den, Smithy, Barn, SlumHome01/02, and HomeLower/Upper01-05 are furnished-room set pieces
            // verified flat/Wall-doorway-consistent with the existing AncientRuin Room1-5 pattern.
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
                .SetPiece("Livingroom01_1x2")
                .SetPiece("Livingroom02_1x2")
                .SetPiece("KitchenRoom01_1x2")
                .SetPiece("KitchenRoom02_1x2")
                .SetPiece("InnRoom01_1x2")
                .SetPiece("InnRoom02_1x2")
                .SetPiece("ShopRoom01_1x2")
                .SetPiece("ShopRoom02_1x2")
                .SetPiece("Bordello")
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
            // black/barrow, no third channel-capable terrain exists.
            //
            // TunnelCrossers("corridor", "door_corridor") + DoorSlotCrossers("door_corridor", "corridor",
            // "door_barrow"): tbw01's real "wall-embedded tunnel" vocabulary renames BOTH halves of the canonical
            // Corridor/Doorway pair (body "corridor", port "door_corridor"), and every single tile that
            // carries either crosser also carries a door slot -- TileResolver's crosser+door admission
            // gate used to hardcode literal "Doorway"/"Bridge" and silently exclude every one of these
            // tiles from candidate lookup regardless of shape, so TunnelVocabularyCheck.SupportsTunnels
            // always read false for this pair no matter what was declared. Declaring "door_corridor" via
            // DoorSlotCrossers closes that gate (see TileResolver's class doc comment) and
            // TunnelVocabularyCheck.SupportsTunnels now verifies the full body/port shape inventory
            // (straight/turn/T/X body, straight/turn/T/X-with-port, double-port, and the boundary port
            // shape against "barrow") all resolve -- confirmed directly via ProbeBarrowsTunnelVocabulary
            // during development. "corridor" is ALSO declared as a DoorSlotCrosser (beyond "door_corridor"):
            // TILE13 (ungrouped) is a boundary tile pairing a door slot with a bare "corridor" edge
            // instead of "door_corridor" -- the body crosser itself doubling as this one boundary tile's
            // port -- which the admission gate would otherwise still exclude even with "door_corridor"
            // declared. CorridorDown_1x2/Corridor_Up_1x2/Corridor_Up_1x2_02 are multi-tile GROUPs whose
            // outer member carries a lone perimeter "corridor" body-crosser edge (not a Doorway/
            // "door_corridor" port) with the inner member a blank, door-bearing dead end -- structurally
            // a two-cell CorridorStub, not a WallRoom (no port pairing). LayoutGroupStamper now has a
            // dedicated CorridorStubChain classification/placement for exactly this shape (splices onto
            // an existing "corridor" chain the same way TryPlaceCorridorStub's single-cell version does,
            // via IsCorridorStubChainSiteValid) -- wired below via three SetPiece calls; verified placing
            // via OnboardedTilesetPipelineTests.CorridorStubChainFamily_ComplexActuallyPlacesTheGroup.
            // TILE51 (ungrouped) stays exempt: a diagonal-split-corner door tile with NO crosser at all
            // (TileDoorPlanner's TryGetSingleDoorwaySlot requires exactly one Doorway edge, which this
            // tile has zero of) -- a genuinely different, unaddressed door mechanism, left exempt (see
            // TileCoverageCensusTests.PilotExpectedExemptions). "door_barrow" (a THIRD crosser name, used
            // nowhere else in the tileset) is declared as a THIRD DoorSlotCrosser: TILE39 (ungrouped) is
            // the identical boundary-tile shape as TILE13 above (a door slot paired with a bare
            // "door_barrow" edge instead of "door_corridor"/"corridor"), closed the same way. SideChamber1
            // (a 1x1 group, TILE60) stays exempt: MacroLayoutParameters only carries one Tunnel port
            // crosser slot per composition (already claimed by "door_corridor" above), and
            // LayoutGroupStamper.TryPlaceCorridorStub's site search requires an already-carved matching-
            // crosser neighbor that can never exist for a port name nothing ever carves -- a genuinely
            // unreachable single tile (auto-tagged alternate-vocabulary; see
            // TileCoverageCensusTests.PilotAlternateVocabCrossers["tbw01"]).
            // FinalArea_7x7 is a large (49-tile), fully solid-or-barrow decorative set piece (a boss/
            // finale chamber) -- structurally a valid OpenSetPiece like any smaller one, included at
            // maxPerArea 1.
            _builder.Create(Barrows, "Barrows Interior")
                .Tileset("tbw01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .PrimaryOpenTerrain("barrow")
                .TunnelCrossers("corridor", "door_corridor")
                .DoorSlotCrossers("door_corridor", "corridor", "door_barrow")
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
                .SetPiece("CorridorDown_1x2")
                .SetPiece("Corridor_Up_1x2")
                .SetPiece("Corridor_Up_1x2_02")
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
            // Pit"/"Lava" are the same shape on the two unwired accents and excluded. "[Cave] Ramp" is
            // now wired via LayoutGroupStamper's ReliefPiece kind (see the SetPiece below); "Cave
            // Entrance" stays excluded -- a raised 1x1 group WITH a door slot, which no mechanism
            // (ReliefPiece is doorless-only, GroupExitPlanner is flat-only) can place. "[Cave] Door -
            // Transition", "[Cave] Ship - Docked", "[Cave] Docks (1x2)" don't structurally classify
            // under any current mechanism and are excluded.
            _builder.Create(MinesAndCaverns, "Mines and Caverns")
                .Tileset("tdm01")
                // Raised-terrain support (probed directly against the .set data): Floor carries the
                // one-corner/two-adjacent-raised rim shapes LayoutElevationPainter needs (so
                // MaxElevationRegions(2)), and the [Cave] family's height content -- Slope-crossered
                // lanes, GentleSlope blend corners, same-terrain diagonal saddles -- is per-corner
                // relief vocabulary (MaxReliefRegions(2), RampCrosser("Slope"),
                // ReliefBlendTerrain("GentleSlope"); GentleSlope has full 16/16 flat coverage against
                // Floor). NO MaxPoolRegions: tdm01 has no raised pool-bank vocabulary at all (a Floor
                // corner one story above an adjacent Water corner never occurs -- banks sit at grade),
                // so depth pools stay off for every tdm01 profile.
                .MaxElevationRegions(2)
                .MaxReliefRegions(2)
                .RampCrosser("Slope")
                .ReliefBlendTerrain("GentleSlope")
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
                // Baked-mesh ramp piece (1x1 GROUP, non-flat [Floor 0,1,1,0]) -- stamped by
                // LayoutGroupStamper's ReliefPiece kind onto a painted raised rim edge.
                .SetPiece("[Cave] Ramp", 1)
                .ExitGroup("[Cave] Exit 1")
                .ExitGroup("[Cave] Exit 2")
                .ExitGroup("[Cave] Exit 3");

            // Mines and Caverns (Desert) -- tdm01's "[Desert]" district, a PaletteVariant profile
            // recomposing the SAME tdm01 hak data the base MinesAndCaverns profile above uses. Verified
            // by direct probe: solid stays "Wall" (shared across [Cave]/[Desert]/[Organic]/[City], the
            // same single-wall-texture pattern as Crypt's Tan/Grey/Dwarven), PrimaryOpenTerrain("Desert")
            // has full 16/16 simple-tile coverage against Wall, and PathNodeOpeningWidthAudit confirms
            // MinimumOpeningWidth stays the default 1. AccentTerrain("DesertWater") mirrors [Cave]'s own
            // Water pick among Desert's three Bridge-gated accent variants (DesertWater/DesertPit/
            // DesertLava) -- "[Desert] Door - Bridge, Water" is an all-DesertWater-cornered Bridge
            // adapter, the identical shape. "[Desert] Door - Transition" uses the CANONICAL "Doorway"
            // crosser (composes normally); "[Desert] Door - Fence 1/2" use the canonical "Fence" crosser.
            // "[Desert] Door - Big 1/2" (TILE770/851) and "[Desert] Stairs - Down/Up 1" (TILE774/775)
            // carry a "DesertCorridor" edge; TunnelCrossers("DesertCorridor", "Doorway") declares that
            // body-renamed family (port stays the CANONICAL "Doorway" -- "[Desert] Door - Transition"
            // uses it directly, mirroring Crypt Grey's own body-only-renamed shape). Verified via
            // TunnelVocabularyCheck.SupportsTunnels(..., CorridorCrosserType.Custom, "DesertCorridor",
            // "Doorway") returning true; this closes Door - Big 1/2 and Stairs - Down/Up 1 as
            // SetPieceCorridorInsert/SetPieceCorridorStub. "[Desert] Door - Big 3/4" and "[Desert]
            // Stairs - Down/Up 2" are a SECOND, independent alternate body family ("DesertTracks",
            // itself independently shape-verified True) -- a tileset profile carries only one Tunnel
            // body/port slot, so wiring DesertCorridor here leaves DesertTracks's own four pieces
            // exempt; closing that second family needs a dedicated profile (same "one body crosser per
            // composition" constraint LayoutTunnelCarver enforces), left for a future wave.
            // "[Desert] Ramp" is non-flat and wired via LayoutGroupStamper's ReliefPiece kind
            // (matching [Cave]'s own Ramp piece); "[Desert] Cave Entrance" stays excluded -- raised
            // AND door-bearing, which no mechanism can place (see the base profile's comment). Every other Desert group (Platforms, Pillar, Stairs 2x2, Treasure,
            // Crystal Casket/Column/Crypt, Chessboard, Portal, Mineshaft, Wall Section, Exit 1/2/3)
            // mirrors [Cave]'s own wired set piece/feature-tile/exit-group shapes tile-for-tile.
            // IsPaletteVariant() excludes this profile from --matrix's full cross-product (see
            // SWLOR.ProcgenReview/Program.cs) -- it gets one showcase area instead. [Organic] and [City]
            // remain unwired (left for a future wave; [Organic] mirrors [Desert]'s shape closely but
            // [City] has a much smaller, differently-shaped tile family and would need its own probe).
            _builder.Create(MinesAndCavernsDesert, "Mines and Caverns (Desert)")
                .Tileset("tdm01")
                // Same raised-terrain trio as the base [Cave] profile, on the Desert family's own
                // names (GentleDesert has full 16/16 flat coverage against Desert; the Slope crosser
                // is shared district-wide). No pools -- see the base profile's own comment.
                .MaxElevationRegions(2)
                .MaxReliefRegions(2)
                .RampCrosser("Slope")
                .ReliefBlendTerrain("GentleDesert")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .PaletteVariant()
                .PrimaryOpenTerrain("Desert")
                .AccentTerrain("DesertWater")
                .TunnelCrossers("DesertCorridor", "Doorway")
                .FeatureTile("[Desert] Treasure 1", 2)
                .FeatureTile("[Desert] Treasure 2 - Water", 2)
                .FeatureTile("[Desert] Treasure 2 - Lava", 2)
                .FeatureTile("[Desert] Pillar 1")
                .FeatureTile("[Desert] Pillar 2")
                .FeatureTile("[Desert] Crystal Column")
                .FeatureTile("[Desert] Crystal Casket 1", 2)
                .FeatureTile("[Desert] Crystal Casket 2", 2)
                .FeatureTile("[Desert] Portal")
                .FeatureTile("[Desert] Chessboard")
                .FeatureTile("[Desert] Mineshaft")
                .SetPiece("[Desert] Door - Fence 1", 1)
                .SetPiece("[Desert] Door - Fence 2", 1)
                .SetPiece("[Desert] Door - Bridge, Water", 1)
                .SetPiece("[Desert] Door - Transition", 1)
                .SetPiece("[Desert] Door - Big 1", 1)
                .SetPiece("[Desert] Door - Big 2", 1)
                .SetPiece("[Desert] Stairs - Down 1", 1)
                .SetPiece("[Desert] Stairs - Up 1", 1)
                .SetPiece("[Desert] Stairs - Down (2x2)")
                .SetPiece("[Desert] Stairs - Up, Water (2x2)")
                .SetPiece("[Desert] Platform 1 (2x2)")
                .SetPiece("[Desert] Platform 2 (2x2)")
                .SetPiece("[Desert] Platform 3 (2x2)")
                .SetPiece("[Desert] Platform 4 (1x2)")
                .SetPiece("[Desert] Platform 5 (1x2)")
                .SetPiece("[Desert] Pillar (1x2)", 2)
                .SetPiece("[Desert] Wall Section 1 - Water (1x2)")
                .SetPiece("[Desert] Wall Section 2 (1x2)")
                .SetPiece("[Desert] Portal (2x2)")
                .SetPiece("[Desert] Crystal Crypt 1")
                .SetPiece("[Desert] Crystal Crypt 2")
                .SetPiece("[Desert] Ramp", 1)
                .ExitGroup("[Desert] Exit 1")
                .ExitGroup("[Desert] Exit 2")
                .ExitGroup("[Desert] Exit 3");

            // Mines and Caverns (Organic) -- tdm01's "[Organic]" district, the same PaletteVariant
            // shape as Mines and Caverns (Desert) immediately above. Verified by direct probe: solid
            // "Wall" (shared) vs PrimaryOpenTerrain("Organic") has full 16/16 simple-tile coverage, and
            // PathNodeOpeningWidthAudit confirms MinimumOpeningWidth 1. AccentTerrain("OrganicWater")
            // mirrors Desert's own Water pick among Organic's three Bridge-gated accent variants
            // (OrganicWater/OrganicPit/OrganicSlime). The curation is a name-for-name mirror of Desert's
            // (this mega-set's four districts are authored in lockstep): canonical-crosser pieces
            // (Door - Transition on "Doorway", Door - Fence 1/2 on "Fence", Door - Bridge, Water) are
            // wired; TunnelCrossers("OrganicCorridor", "Doorway") mirrors Desert's own body-only-rename
            // shape (verified via TunnelVocabularyCheck.SupportsTunnels(..., CorridorCrosserType.Custom,
            // "OrganicCorridor", "Doorway") returning true), closing "[Organic] Door - Big 1/2"
            // (TILE1123/1204) and "[Organic] Stairs - Down/Up 1" (TILE1127/1128) as
            // SetPieceCorridorInsert/SetPieceCorridorStub; "OrganicTracks" (Door - Big 3/4, Stairs -
            // Down/Up 2) is the same second independent alternate family as Desert's own DesertTracks and
            // stays unwired for the same one-body-crosser-per-profile reason; "[Organic] Ramp" is wired
            // via ReliefPiece and "[Organic] Cave Entrance" stays excluded (raised AND door-bearing) --
            // see the Desert profile's comment for the full reasoning.
            _builder.Create(MinesAndCavernsOrganic, "Mines and Caverns (Organic)")
                .Tileset("tdm01")
                // Same raised-terrain trio as [Cave]/[Desert], on the Organic family's own names
                // (GentleOrganic has full 16/16 flat coverage against Organic). No pools.
                .MaxElevationRegions(2)
                .MaxReliefRegions(2)
                .RampCrosser("Slope")
                .ReliefBlendTerrain("GentleOrganic")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .PaletteVariant()
                .PrimaryOpenTerrain("Organic")
                .AccentTerrain("OrganicWater")
                .TunnelCrossers("OrganicCorridor", "Doorway")
                .FeatureTile("[Organic] Treasure 1", 2)
                .FeatureTile("[Organic] Treasure 2 - Water", 2)
                .FeatureTile("[Organic] Treasure 2 - Slime", 2)
                .FeatureTile("[Organic] Pillar 1")
                .FeatureTile("[Organic] Pillar 2")
                .FeatureTile("[Organic] Crystal Column")
                .FeatureTile("[Organic] Crystal Casket 1", 2)
                .FeatureTile("[Organic] Crystal Casket 2", 2)
                .FeatureTile("[Organic] Portal")
                .FeatureTile("[Organic] Chessboard")
                .FeatureTile("[Organic] Mineshaft")
                .SetPiece("[Organic] Door - Fence 1", 1)
                .SetPiece("[Organic] Door - Fence 2", 1)
                .SetPiece("[Organic] Door - Bridge, Water", 1)
                .SetPiece("[Organic] Door - Transition", 1)
                .SetPiece("[Organic] Door - Big 1", 1)
                .SetPiece("[Organic] Door - Big 2", 1)
                .SetPiece("[Organic] Stairs - Down 1", 1)
                .SetPiece("[Organic] Stairs - Up 1", 1)
                .SetPiece("[Organic] Stairs - Down (2x2)")
                .SetPiece("[Organic] Stairs - Up, Water (2x2)")
                .SetPiece("[Organic] Stairs - Up, Slime (2x2)")
                .SetPiece("[Organic] Platform 1 (2x2)")
                .SetPiece("[Organic] Platform 2 (2x2)")
                .SetPiece("[Organic] Platform 3 (2x2)")
                .SetPiece("[Organic] Platform 4 (1x2)")
                .SetPiece("[Organic] Platform 5 (1x2)")
                .SetPiece("[Organic] Pillar (1x2)", 2)
                .SetPiece("[Organic] Wall Section 1 - Water (1x2)")
                .SetPiece("[Organic] Wall Section 1 - Slime (1x2)")
                .SetPiece("[Organic] Wall Section 2 (1x2)")
                .SetPiece("[Organic] Portal (2x2)")
                .SetPiece("[Organic] Crystal Crypt 1")
                .SetPiece("[Organic] Crystal Crypt 2")
                .SetPiece("[Organic] Ramp", 1)
                .ExitGroup("[Organic] Exit 1")
                .ExitGroup("[Organic] Exit 2")
                .ExitGroup("[Organic] Exit 3");

            // Mines and Caverns (City Water) -- tdm01's [City] canal/water accent family composed
            // against the shared [Cave] Floor. Unlike Desert/Organic, [City] is NOT a full district
            // (its own open-terrain family is tiny and differently shaped -- see the tdm01 census
            // notes on PilotAlternateVocabTerrains), but its CityWater ACCENT vocabulary is complete
            // and probe-verified: full 16/16 flat (Floor, CityWater) corner coverage, a fully-CityWater
            // interior tile, AND -- uniquely among tdm01's accent families -- the raised pool-bank
            // shapes (a Floor rim one story above an adjacent CityWater corner), so this is the one
            // tdm01 profile that declares MaxPoolRegions. MaxReliefRegions(2) additionally reaches the
            // CityWater bank tiles whose Floor/CityWater corners sit at per-corner-independent mixed
            // grades. No blend terrain ([City] has none), no alternate Tunnel family wired (the
            // canonical Corridor/Doorway family composes normally on the shared solid/Floor terrain).
            // PaletteVariant() -- one showcase area, excluded from --matrix.
            _builder.Create(MinesAndCavernsCity, "Mines and Caverns (City Water)")
                .Tileset("tdm01")
                .MaxElevationRegions(2)
                .MaxPoolRegions(2)
                .MaxReliefRegions(2)
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .PaletteVariant()
                .AccentTerrain("CityWater")
                .FeatureTile("[Cave] Treasure 1", 2)
                .FeatureTile("[Cave] Pillar 1")
                .FeatureTile("[Cave] Pillar 2")
                .SetPiece("[Cave] Door - Big 1", 1)
                .SetPiece("[Cave] Door - Big 2", 1)
                .SetPiece("[Cave] Ramp", 1)
                .ExitGroup("[Cave] Exit 1")
                .ExitGroup("[Cave] Exit 2")
                .ExitGroup("[Cave] Exit 3");

            // Mines and Caverns ([Cave] Tracks) -- tdm01's [Cave] district again, but declaring the
            // SECOND, independent alternate Tunnel body family "Tracks" (paired with the canonical
            // "Doorway" port -- "[Cave] Door - Transition" carries a lone Doorway edge on all-solid
            // corners, the same port shape [Desert]/[Organic]'s own Door - Transition pieces use)
            // instead of the base MinesAndCaverns profile's plain "Corridor". Verified via
            // TunnelVocabularyCheck.SupportsTunnels(..., CorridorCrosserType.Custom, "Tracks", "Doorway")
            // returning true. A tileset profile carries only one Tunnel body/port slot
            // (MacroLayoutParameters.TunnelBodyCrosser/TunnelPortCrosser), so this is a dedicated
            // PaletteVariant rather than added to the base [Cave] profile -- "[Cave] Door - Big 3"
            // (TILE115)/"Door - Big 4" (TILE196) are an all-solid opposite-Tracks-edge-pair-with-door
            // shape (SetPieceCorridorInsert); "[Cave] Stairs - Down 2" (TILE120)/"Stairs - Up 2" (TILE121)
            // are an all-solid single-Tracks-edge dead end with a door slot (SetPieceCorridorStub) --
            // both verified directly against the .set data. Everything else mirrors the base
            // MinesAndCaverns profile's own wiring (same solid/open terrain, same feature tiles/other set
            // pieces/exit groups); PaletteVariant() excludes this from --matrix's full cross-product, one
            // showcase area instead -- closing TileCoverageCensusTests.PilotAlternateVocabCrossers["tdm01"]'s
            // "Tracks" entry.
            _builder.Create(MinesAndCavernsTracks, "Mines and Caverns (Tracks)")
                .Tileset("tdm01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .PaletteVariant()
                .AccentTerrain("Water")
                .TunnelCrossers("Tracks", "Doorway")
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
                .SetPiece("[Cave] Door - Big 3", 1)
                .SetPiece("[Cave] Door - Big 4", 1)
                .SetPiece("[Cave] Door - Fence 1", 1)
                .SetPiece("[Cave] Door - Fence 2", 1)
                .SetPiece("[Cave] Door - Bridge, Water", 1)
                .SetPiece("[Cave] Stairs - Down 2", 1)
                .SetPiece("[Cave] Stairs - Up 2", 1)
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

            // Mines and Caverns (Desert Tracks) -- tdm01's [Desert] district again, declaring the SECOND,
            // independent alternate body family "DesertTracks" (paired with the canonical "Doorway" port,
            // same as the base Desert profile's own Door - Transition) instead of "DesertCorridor".
            // Verified via TunnelVocabularyCheck.SupportsTunnels(..., CorridorCrosserType.Custom,
            // "DesertTracks", "Doorway") returning true. "[Desert] Door - Big 3" (TILE771)/"Door - Big 4"
            // (TILE852) are the CorridorInsert opposite-DesertTracks-pair-with-door shape; "[Desert]
            // Stairs - Down 2" (TILE776)/"Stairs - Up 2" (TILE777) are the CorridorStub single-edge dead
            // end -- verified directly against the .set data, mirroring [Cave] Tracks' own shapes.
            // Everything else mirrors the base MinesAndCavernsDesert profile's own wiring.
            _builder.Create(MinesAndCavernsDesertTracks, "Mines and Caverns (Desert Tracks)")
                .Tileset("tdm01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .PaletteVariant()
                .PrimaryOpenTerrain("Desert")
                .AccentTerrain("DesertWater")
                .TunnelCrossers("DesertTracks", "Doorway")
                .FeatureTile("[Desert] Treasure 1", 2)
                .FeatureTile("[Desert] Treasure 2 - Water", 2)
                .FeatureTile("[Desert] Treasure 2 - Lava", 2)
                .FeatureTile("[Desert] Pillar 1")
                .FeatureTile("[Desert] Pillar 2")
                .FeatureTile("[Desert] Crystal Column")
                .FeatureTile("[Desert] Crystal Casket 1", 2)
                .FeatureTile("[Desert] Crystal Casket 2", 2)
                .FeatureTile("[Desert] Portal")
                .FeatureTile("[Desert] Chessboard")
                .FeatureTile("[Desert] Mineshaft")
                .SetPiece("[Desert] Door - Fence 1", 1)
                .SetPiece("[Desert] Door - Fence 2", 1)
                .SetPiece("[Desert] Door - Bridge, Water", 1)
                .SetPiece("[Desert] Door - Transition", 1)
                .SetPiece("[Desert] Door - Big 3", 1)
                .SetPiece("[Desert] Door - Big 4", 1)
                .SetPiece("[Desert] Stairs - Down 2", 1)
                .SetPiece("[Desert] Stairs - Up 2", 1)
                .SetPiece("[Desert] Stairs - Down (2x2)")
                .SetPiece("[Desert] Stairs - Up, Water (2x2)")
                .SetPiece("[Desert] Platform 1 (2x2)")
                .SetPiece("[Desert] Platform 2 (2x2)")
                .SetPiece("[Desert] Platform 3 (2x2)")
                .SetPiece("[Desert] Platform 4 (1x2)")
                .SetPiece("[Desert] Platform 5 (1x2)")
                .SetPiece("[Desert] Pillar (1x2)", 2)
                .SetPiece("[Desert] Wall Section 1 - Water (1x2)")
                .SetPiece("[Desert] Wall Section 2 (1x2)")
                .SetPiece("[Desert] Portal (2x2)")
                .SetPiece("[Desert] Crystal Crypt 1")
                .SetPiece("[Desert] Crystal Crypt 2")
                .ExitGroup("[Desert] Exit 1")
                .ExitGroup("[Desert] Exit 2")
                .ExitGroup("[Desert] Exit 3");

            // Mines and Caverns (Organic Tracks) -- tdm01's [Organic] district again, declaring the
            // SECOND, independent alternate body family "OrganicTracks" (paired with the canonical
            // "Doorway" port) instead of "OrganicCorridor". Verified via
            // TunnelVocabularyCheck.SupportsTunnels(..., CorridorCrosserType.Custom, "OrganicTracks",
            // "Doorway") returning true. "[Organic] Door - Big 3"/"Door - Big 4" are the CorridorInsert
            // opposite-OrganicTracks-pair-with-door shape; "[Organic] Stairs - Down 2"/"Stairs - Up 2" are
            // the CorridorStub single-edge dead end -- mirroring [Cave] Tracks/Desert Tracks' own shapes.
            // Everything else mirrors the base MinesAndCavernsOrganic profile's own wiring.
            _builder.Create(MinesAndCavernsOrganicTracks, "Mines and Caverns (Organic Tracks)")
                .Tileset("tdm01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .PaletteVariant()
                .PrimaryOpenTerrain("Organic")
                .AccentTerrain("OrganicWater")
                .TunnelCrossers("OrganicTracks", "Doorway")
                .FeatureTile("[Organic] Treasure 1", 2)
                .FeatureTile("[Organic] Treasure 2 - Water", 2)
                .FeatureTile("[Organic] Treasure 2 - Slime", 2)
                .FeatureTile("[Organic] Pillar 1")
                .FeatureTile("[Organic] Pillar 2")
                .FeatureTile("[Organic] Crystal Column")
                .FeatureTile("[Organic] Crystal Casket 1", 2)
                .FeatureTile("[Organic] Crystal Casket 2", 2)
                .FeatureTile("[Organic] Portal")
                .FeatureTile("[Organic] Chessboard")
                .FeatureTile("[Organic] Mineshaft")
                .SetPiece("[Organic] Door - Fence 1", 1)
                .SetPiece("[Organic] Door - Fence 2", 1)
                .SetPiece("[Organic] Door - Bridge, Water", 1)
                .SetPiece("[Organic] Door - Transition", 1)
                .SetPiece("[Organic] Door - Big 3", 1)
                .SetPiece("[Organic] Door - Big 4", 1)
                .SetPiece("[Organic] Stairs - Down 2", 1)
                .SetPiece("[Organic] Stairs - Up 2", 1)
                .SetPiece("[Organic] Stairs - Down (2x2)")
                .SetPiece("[Organic] Stairs - Up, Water (2x2)")
                .SetPiece("[Organic] Stairs - Up, Slime (2x2)")
                .SetPiece("[Organic] Platform 1 (2x2)")
                .SetPiece("[Organic] Platform 2 (2x2)")
                .SetPiece("[Organic] Platform 3 (2x2)")
                .SetPiece("[Organic] Platform 4 (1x2)")
                .SetPiece("[Organic] Platform 5 (1x2)")
                .SetPiece("[Organic] Pillar (1x2)", 2)
                .SetPiece("[Organic] Wall Section 1 - Water (1x2)")
                .SetPiece("[Organic] Wall Section 1 - Slime (1x2)")
                .SetPiece("[Organic] Wall Section 2 (1x2)")
                .SetPiece("[Organic] Portal (2x2)")
                .SetPiece("[Organic] Crystal Crypt 1")
                .SetPiece("[Organic] Crystal Crypt 2")
                .ExitGroup("[Organic] Exit 1")
                .ExitGroup("[Organic] Exit 2")
                .ExitGroup("[Organic] Exit 3");

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

            // Ruins (Plaza) -- tdr01's "Plaza" exterior-district palette, a PaletteVariant profile
            // recomposing the SAME tdr01 hak data the base Ruins profile above uses. Verified by direct
            // probe: solid "Wall" (shared) vs PrimaryOpenTerrain("Plaza") has full 16/16 simple-tile
            // coverage (the base profile's own comment already noted Plaza as "a second fully-covered
            // open terrain" that the single PrimaryOpenTerrain slot couldn't hold), and
            // PathNodeOpeningWidthAudit confirms MinimumOpeningWidth 1. This variant unlocks the
            // all-Plaza-cornered exterior set pieces the base profile's Floor vocabulary REJECTed
            // (Mosaic_Plaza_2x2, ExteriorStage_2x2, ExteriorRuinedTower_2x2, ExteriorWalkway_2x2,
            // Amphitheater_2x2, ExteriorStairsDown/Up_2x2 -- all structurally OpenSetPiece once Plaza IS
            // the open terrain, verified via corner inspection) plus ExteriorFenceDoor (a canonical
            // Fence gate on all-Plaza corners, which the Fence CorridorInsert branch accepts only for
            // the open/secondary terrain -- unreachable under Floor, valid here). The all-Plaza 1x1
            // feature tiles (ExteriorFountain/OvergrownGarden/Pool/RuinedHouse/Portal/Chessboard) and
            // the Plaza/Wall ExteriorExit01/02 are wired on the base profile already (FeatureTile/
            // ExitGroup eligibility is terrain-agnostic) and are re-wired here so the variant is fully
            // self-sufficient at composition time. No AccentTerrain and no ChannelTerrain: Chasm's
            // channel/bank coverage was verified against FLOOR banks only (see the base profile's
            // comment); no Plaza-and-Chasm-mixed crosser-free tile has been verified, so channels stay
            // off and LayoutAccentChannelCarver's own CanCarve probe keeps any composed channel request
            // a graceful no-op. Alley/Streets remains out of scope exactly as on the base profile.
            _builder.Create(RuinsPlaza, "Ruins (Plaza)")
                .Tileset("tdr01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .PaletteVariant()
                .PrimaryOpenTerrain("Plaza")
                .FeatureTile("ExteriorFountain")
                .FeatureTile("ExteriorOvergrownGarden")
                .FeatureTile("ExteriorPool")
                .FeatureTile("RuinedHouse")
                .FeatureTile("Portal")
                .FeatureTile("Chessboard")
                .SetPiece("ExteriorFenceDoor", 1)
                .SetPiece("ExteriorStairsDown_2x2")
                .SetPiece("ExteriorStairsUp_2x2")
                .SetPiece("Mosaic_Plaza_2x2")
                .SetPiece("ExteriorStage_2x2")
                .SetPiece("ExteriorWalkway_2x2")
                .SetPiece("ExteriorRuinedTower_2x2")
                .SetPiece("Amphitheater_2x2")
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
            // The Room-* / Room1/Room2 groups (Storage/Bedroom/Library/Jail/Stone 1/2 (1x2), Storage
            // Empty (2x1), Bath 1/2 (2x1)) each pair a blank wall tile with a tile carrying BOTH a
            // Doorway edge crosser AND a door slot -- LayoutGroupStamper's WallRoom classification now
            // tolerates this shape (see that method's own doc comment), so all thirteen are wired here.
            // Turret Interior - Lit/Dark (2x1) stays unreachable: each member's own Doorway edge faces
            // its group-mate (an interior, not perimeter, opening -- verified directly, not assumed) --
            // see TileCoverageCensusTests.PilotExpectedExemptions. "Exit - Corridor"/"Exit - Corridor,
            // Big" are named as exits but structurally classify as CorridorStub (they carry a Corridor
            // crosser, disqualifying them from GroupExitPlanner's crosser-free ExitGroup rule) -- wired
            // as SetPieces instead, matching their real structural shape; no ExitGroup candidate exists
            // in this tileset. Window-* pieces (Window crosser), Maze-* pieces (MazeMosaic/MazeMarble
            // crossers), and the separate "[Tower]" brown/grey sub-district (own "Tower" terrain, no
            // coverage) are all alternate vocabulary and excluded.
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
                .SetPiece("[Castle] Dais")
                .SetPiece("[Castle] Room - Storage 1 (1x2)")
                .SetPiece("[Castle] Room - Storage 2 (1x2)")
                .SetPiece("[Castle] Room - Bedroom 1 (1x2)")
                .SetPiece("[Castle] Room - Bedroom 2 (1x2)")
                .SetPiece("[Castle] Room - Library 1 (1x2)")
                .SetPiece("[Castle] Room - Library 2 (1x2)")
                .SetPiece("[Castle] Room - Jail 1 (1x2)")
                .SetPiece("[Castle] Room - Jail 2 (1x2)")
                .SetPiece("[Castle] Room - Stone 1 (1x2)")
                .SetPiece("[Castle] Room - Stone 2 (1x2)")
                .SetPiece("[Castle] Room - Storage, Empty (2x1)")
                .SetPiece("[Castle] Room - Bath 1 (2x1)")
                .SetPiece("[Castle] Room - Bath 2 (2x1)");

            // Castle Interior (Storage/Rich/Library/Jail) -- tic01's four alternate room-type district
            // palettes, PaletteVariant profiles recomposing the SAME tic01 hak data the base
            // CastleInterior profile above uses. Each district's own terrain has full simple-tile
            // coverage against the shared "Wall" solid (verified via direct TileResolver.HasCandidate
            // boundary-shape probe: 31/50/42/36 ungrouped tiles respectively reference Storage/Rich/
            // Library/Jail corners) -- PrimaryOpenTerrain(<district>) alone closes all of them via
            // CornerEdgeResolver, the same "declare the terrain, the ordinary resolver does the rest"
            // pattern as every other PaletteVariant here. "Door - <District> 1/2" (corners [AltTerrain,
            // Wall, Wall, AltTerrain], a door slot, no crosser) were excluded from the base profile purely
            // because only "Stone" was wired as PrimaryOpenTerrain there -- once each district's own
            // terrain is wired, they classify as OpenSetPiece (matchesPrimary now true for that terrain).
            // Rich additionally has "Bath" (ALL four corners the district's own open terrain, no
            // crosser -- an OpenSetPiece, not a WallAlcove, since matchesPrimary's "every corner solid-
            // or-open" check is trivially satisfied with zero solid corners) and "Round Corner -
            // Empty/Decorated, Rich"/"Stairs - Up/Down, Rich Corner" (OpenSetPiece, a mostly-solid corner
            // mix with one district-terrain corner); Library has the same Round Corner/Stairs Corner
            // quartet. "Round Corner - Window, Library" is NOT wired: its tile carries a genuine "Window"
            // edge crosser -- outside this pilot's wired vocabulary, the same exclusion as every other
            // Window-* piece on the base profile -- so it fails even the relaxed member-edge gate and
            // stays unclassified. The base profile's own terrain-agnostic pieces (Stairs - Up/Down,
            // Exit - Corridor/Big, Round Corner - Empty/Decorated Stone, Stairs - Up/Down Stone Corner,
            // Dais, every Room-* WallRoom family) already work regardless of which PrimaryOpenTerrain is
            // composed, so they are NOT re-wired here -- these variants add ONLY their own
            // district-specific pieces. PaletteVariant() excludes each from --matrix's full
            // cross-product -- one showcase area each instead.
            _builder.Create(CastleInteriorStorage, "Castle Interior (Storage)")
                .Tileset("tic01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .PaletteVariant()
                .PrimaryOpenTerrain("Storage")
                .SetPiece("[Castle] Door - Storage 1", 1)
                .SetPiece("[Castle] Door - Storage 2", 1);

            _builder.Create(CastleInteriorRich, "Castle Interior (Rich)")
                .Tileset("tic01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .PaletteVariant()
                .PrimaryOpenTerrain("Rich")
                .SetPiece("[Castle] Door - Rich 1", 1)
                .SetPiece("[Castle] Door - Rich 2", 1)
                .SetPiece("[Castle] Bath")
                .SetPiece("[Castle] Round Corner - Empty, Rich")
                .SetPiece("[Castle] Round Corner - Decorated, Rich")
                .SetPiece("[Castle] Stairs - Up, Rich Corner")
                .SetPiece("[Castle] Stairs - Down, Rich Corner");

            _builder.Create(CastleInteriorLibrary, "Castle Interior (Library)")
                .Tileset("tic01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .PaletteVariant()
                .PrimaryOpenTerrain("Library")
                .SetPiece("[Castle] Door - Library 1", 1)
                .SetPiece("[Castle] Door - Library 2", 1)
                .SetPiece("[Castle] Round Corner - Empty, Library")
                .SetPiece("[Castle] Round Corner - Decorated, Library")
                .SetPiece("[Castle] Stairs - Up, Library Corner")
                .SetPiece("[Castle] Stairs - Down, Library Corner");

            _builder.Create(CastleInteriorJail, "Castle Interior (Jail)")
                .Tileset("tic01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .PaletteVariant()
                .PrimaryOpenTerrain("Jail")
                .SetPiece("[Castle] Door - Jail 1", 1)
                .SetPiece("[Castle] Door - Jail 2", 1);

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
            // StorageRoom01/02_1x2, Bedroom01/02_1x2, LibraryRoom01/02_1x2, JailRoom01/02_1x2,
            // StoneRoom01/02_1x2, and CollapsedRoom2x2 are the same door-entrance-pair shape as Castle
            // Interior's own Room-* families -- LayoutGroupStamper's WallRoom relaxation now covers
            // them, wired here accordingly. Mythallar_3x3's shared member edges carry the plain
            // "corridor" crosser, not Doorway -- LayoutGroupStamper's dedicated CorridorStubChain
            // classification/placement now reaches this shape (a multi-tile CorridorStub splice, not a
            // WallRoom port pairing -- see that class's TryPlaceCorridorStubChain and
            // BaseGameTilesetProfiles.FortInterior's own doc comment for the equivalent twc03 family),
            // so it's wired here too.
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
                .SetPiece("basement_1x2")
                .SetPiece("StorageRoom01_1x2")
                .SetPiece("StorageRoom02_1x2")
                .SetPiece("Bedroom01_1x2")
                .SetPiece("Bedroom02_1x2")
                .SetPiece("LibraryRoom01_1x2")
                .SetPiece("LibraryRoom02_1x2")
                .SetPiece("JailRoom01_1x2")
                .SetPiece("JailRoom02_1x2")
                .SetPiece("StoneRoom01_1x2")
                .SetPiece("StoneRoom02_1x2")
                .SetPiece("CollapsedRoom2x2")
                .SetPiece("Mythallar_3x3");

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
            // "Observation pit" and "Fighting Pit" (both 3x3, all-solid-cornered, door-bearing) clear as
            // WallAlcove. "Great Brain" (this tileset's signature centerpiece, 3x3), "Resting Pods"
            // (3x3), "Resting Pod" (1x1), "Cell" (1x1), and "Transition Door" (1x1) each carry a Doorway
            // edge together with a door slot on the same member tile -- LayoutGroupStamper's WallRoom
            // classification now tolerates this shape (see that method's own doc comment), and all five
            // structurally verify a real PERIMETER Doorway opening (not merely an interior one shared
            // between two members of the same group), so all five are wired here too, matching this
            // codebase's "structurally-valid config counts even if not currently exercised" convention
            // (see TileCoverageCensusTests' own class doc comment). tii01 fails
            // TunnelVocabularyCheck.SupportsTunnels purely on its missing T-with-port junction shape
            // (see IllithidComplexDowngradesToOpenLaneWithNoTunnelCrossers in OnboardedTilesetPipelineTests),
            // so Complex -- this tileset's only Tunnel-mode-composed layout -- always downgrades to
            // OpenLane before dispatch, and tii01 has no other declared crosser pair (only the canonical
            // "Doorway"/"Corridor" exist in this tileset's vocabulary at all -- verified directly against
            // the .set data), so the T-with-port gap itself is out of scope here (a separate, pre-existing
            // issue). Instead LayoutGroupStamper.IsWallRoomSiteValid now ALSO accepts an OpenLane-adjacent
            // site: a solid WallRoom cell whose perimeter Doorway edge borders a genuine open-lane/room
            // boundary cell (near corners shared with the WallRoom, guaranteed solid; far corners this
            // layout's own OpenTerrain), guarded by SupportsWallRoomOpenLaneBoundary's whole-tileset
            // capability probe (the same boundary shape TunnelVocabularyCheck.SupportsBoundaryShape
            // already verifies for every ordinary room door) so it can never stamp an unresolvable cell.
            // Confirmed via direct 200-seed isolated probe: 200/200 placements (see
            // OnboardedTilesetPipelineTests.DoorSlotWallRoomFamily_ComplexActuallyPlacesTheGroup, which
            // now covers Illithid alongside every other tileset in that sweep). "Transporter"
            // is the tileset's only FeatureTile-eligible group (1x1, flat, crosser-free, doorless,
            // pathnode A). No ExitGroup candidate exists in this tileset.
            _builder.Create(IllithidInterior, "Illithid Interior")
                .Tileset("tii01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .FeatureTile("Transporter")
                .SetPiece("Stairs up", 1)
                .SetPiece("Stairs Down", 1)
                .SetPiece("Observation pit")
                .SetPiece("Fighting Pit")
                .SetPiece("Great Brain")
                .SetPiece("Resting Pods")
                .SetPiece("Resting Pod")
                .SetPiece("Cell")
                .SetPiece("Transition Door");

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
            // classify). The *Room01_1x2/*Room02_1x2 door-entrance pairs and "Bordello" (this hak copy's
            // own separate but structurally identical Livingroom/Kitchen/Inn/Shop room-entrance tiles)
            // now classify via LayoutGroupStamper's WallRoom door-slot relaxation, mirroring City
            // Interior's own equivalent family -- wired here accordingly. The LivingroomCorner*/
            // KitchenCorner* stair/exit pieces reference alternate terrain corners and don't classify.
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
                .SetPiece("Livingroom01_1x2")
                .SetPiece("Livingroom02_1x2")
                .SetPiece("KitchenRoom01_1x2")
                .SetPiece("KitchenRoom02_1x2")
                .SetPiece("InnRoom01_1x2")
                .SetPiece("InnRoom02_1x2")
                .SetPiece("ShopRoom01_1x2")
                .SetPiece("ShopRoom02_1x2")
                .SetPiece("Bordello")
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
            // "legacy"-sounding name. Their non-"OLD_" *_2x1/*_2x2/*_1x2 replacements (StoreRoom_2x2L,
            // Cells_2x2, Kitchen_1x2, Generic_Room_2x1/2x2, Barracks_2x2, Bedroom_02_2x2/03_2x1,
            // Smithy_1x2, Portal_Hall_2x3) each carry a Doorway edge together with a door slot on the
            // same member tile -- LayoutGroupStamper's WallRoom classification now tolerates this shape
            // (see that method's own doc comment, the same relaxation that closed Castle Interior/
            // Illithid Interior/City Interior/City Interior 2's own equivalent families), so all ten are
            // wired here. The legacy "OLD_"-prefixed superseded groups and Mythallar_3x3 use the plain
            // "corridor" body crosser directly on their entrance/wall tile instead of a Doorway-family
            // port -- LayoutGroupStamper now has a dedicated CorridorStubChain classification/placement
            // for exactly this shape (a multi-tile CorridorStub splice, not a WallRoom port pairing --
            // see that class's TryPlaceCorridorStubChain), so they're structurally reachable too, but
            // NOT wired into this live profile: they are superseded by the non-"OLD_" replacements
            // already wired above, and mixing both would place near-duplicate furnished rooms in the
            // same generated area. See FortInteriorLegacy immediately below for a dedicated showcase
            // that wires them instead of the current replacements. Large_Door stays genuinely
            // unreachable regardless: its TILE36 has mixed floor/black corners, so it fails the
            // all-solid CorridorStubChain/CorridorInsert/CorridorStub checks too -- see
            // TileCoverageCensusTests.PilotExpectedExemptions. No FeatureTile-
            // eligible group exists in this tileset. "Exit_1x1"/"Exit_Down_1x1"/"Exit_CollapsedWall" are
            // the genuine crosser-free door-bearing ExitGroup candidates; "Storage_1x1_1"/"Stairway_up"/
            // "Stairway_down" carry the identical structural shape (floor/floor/black/black corners, a
            // door slot, no crosser) but read as furnished-room decor by name, so they are wired as
            // SetPieces instead. DoorSlotCrossers("corridor", "wall") closes six ungrouped boundary/gate
            // tiles that pair a door slot with a non-Doorway crosser the admission gate would otherwise
            // exclude: TILE23/29 (a solid or mixed boundary tile with a bare "corridor" edge, the same
            // shape as Barrows' own TILE13/39 fix) and TILE95/96/105/106 (an open-floor gate tile with
            // one or three "wall" edges cutting straight through the room, plus a door). TILE125/127/128
            // stay exempt: diagonal-split or single-corner-cut door tiles with NO crosser at all
            // (TileDoorPlanner's TryGetSingleDoorwaySlot requires a genuine Doorway edge, which none of
            // these three have) -- a genuinely different, unaddressed door mechanism, left exempt (see
            // TileCoverageCensusTests.PilotExpectedExemptions).
            _builder.Create(FortInterior, "Fort Interior")
                .Tileset("twc03")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .PrimaryOpenTerrain("floor")
                .AccentTerrain("water")
                .DoorSlotCrossers("corridor", "wall")
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
                .SetPiece("StoreRoom_2x2L")
                .SetPiece("Cells_2x2")
                .SetPiece("Kitchen_1x2")
                .SetPiece("Generic_Room_2x1")
                .SetPiece("Generic_Room_2x2")
                .SetPiece("Barracks_2x2")
                .SetPiece("Bedroom_02_2x2")
                .SetPiece("Bedroom_03_2x1")
                .SetPiece("Smithy_1x2")
                .SetPiece("Portal_Hall_2x3")
                .ExitGroup("Exit_1x1")
                .ExitGroup("Exit_Down_1x1")
                .ExitGroup("Exit_CollapsedWall");

            // Fort Interior (Legacy) -- twc03's "OLD_"-prefixed superseded furnished-room family, a
            // PaletteVariant profile recomposing the SAME twc03 hak data the base FortInterior profile
            // above uses. Same solid/open/accent terrain, same base-shape pieces (Arena/Storage/Dais/
            // Stairway/Corr_SpiralStair/OLD_*_1x1/Corridor_Exit/Room_1x2/LargeGate/Fireplace/Platform),
            // but swaps the CURRENT non-"OLD_" furnished-room replacements for their "OLD_"-prefixed
            // originals (OLD_StoreRoom_2x2L_old, OLD_Cells_2x2_old, OLD_Kitchen_1x2, OLD_Generic_Room_2x1/
            // 2x2, OLD_Barracks_2x2, OLD_Bedroom_02_2x1, OLD_Bedroom_03_2x1, OLD_Smithy_1x2,
            // OLD_Portal_Hall_2x3) plus Mythallar_3x3 -- each carries the plain "corridor" body crosser
            // directly on its entrance/wall tile instead of a Doorway-family port, which
            // LayoutGroupStamper's CorridorStubChain classification/placement now reaches (see
            // FortInterior's own doc comment above and TryPlaceCorridorStubChain). Verified via direct
            // pipeline sweep (OnboardedTilesetPipelineTests.CorridorStubChainFamily_ComplexActuallyPlacesTheGroup).
            // Large_Door is NOT wired here either: its TILE36 has mixed floor/black corners and never
            // classifies under any mechanism (see TileCoverageCensusTests.PilotExpectedExemptions).
            // PaletteVariant() excludes this from --matrix's full cross-product -- one showcase area
            // instead.
            _builder.Create(FortInteriorLegacy, "Fort Interior (Legacy)")
                .Tileset("twc03")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .PaletteVariant()
                .PrimaryOpenTerrain("floor")
                .AccentTerrain("water")
                .DoorSlotCrossers("corridor", "wall")
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
                .SetPiece("OLD_StoreRoom_2x2L_old")
                .SetPiece("OLD_Cells_2x2_old")
                .SetPiece("OLD_Kitchen_1x2")
                .SetPiece("OLD_Generic_Room_2x1")
                .SetPiece("OLD_Generic_Room_2x2")
                .SetPiece("OLD_Barracks_2x2")
                .SetPiece("OLD_Bedroom_02_2x1")
                .SetPiece("OLD_Bedroom_03_2x1")
                .SetPiece("OLD_Smithy_1x2")
                .SetPiece("OLD_Portal_Hall_2x3")
                .SetPiece("Mythallar_3x3")
                .ExitGroup("Exit_1x1")
                .ExitGroup("Exit_Down_1x1")
                .ExitGroup("Exit_CollapsedWall");

            // Desert (ttd01, SWLOR_Haks/sw_t_tatooine -- a 388-tile HasHeightTransition=1 superset of
            // the 212-tile vanilla version; hak copies win TilesetSetSource resolution). See the
            // wave-level comment at the Desert/Forest/ForestFacelift constants above for the shared
            // INVERTED composition: SolidTerrainOverride("Cliff") + PrimaryOpenTerrain("Desert"),
            // because the GENERAL Default ("Desert") is the WALKABLE ground here, not the wall.
            //
            // AccentTerrain("Chasm") is the Bridge-gated third terrain (BridgeDoor01 is an
            // all-Chasm-cornered opposite-Bridge-pair -- the same CorridorInsert(Bridge)/allAccent
            // shape Dungeon/MinesAndCaverns's own "Door - Bridge" pieces use). No Tunnel vocabulary:
            // every crosser family (Wall/Road/Trench, each a same-name body/port pair) resolves ONLY
            // against Solid=Desert compositions (verified directly via TunnelVocabularyCheck, every
            // ordered crosser pairing) -- i.e. the roads/walls/trenches all run through what is now
            // the OPEN walkable ground, and no crosser family exists through the Cliff solid at all,
            // so Complex's Tunnel mode downgrades to OpenLane (the Barrows/Crypt-Dwarven fallback).
            // Ungrouped flat Road/Wall/Trench tiles (no door slots) all still resolve via the corner/
            // edge resolver regardless.
            //
            // Heights (30 non-flat tiles of 388, all on the Desert open terrain -- Cliff never carries
            // a nonzero corner height anywhere in the inventory, verified directly):
            // MaxElevationRegions(2) -- the 7 crosser-free raised-Desert rim tiles support the open
            // split-level mechanism; RampCrosser("Dunes") -- the tileset's dune-face lanes are its
            // ramp vocabulary (raised Desert tiles carrying Dunes edges, e.g. TILE244-252/256-261),
            // spliced by LayoutElevationPainter.TryAddRampLane and batch-verified by
            // LayoutReliefPainter; MaxReliefRegions(2) -- the census's relief BFS mirror
            // (IsTerrainReliefReachable) verifies 17 raised tiles reachable corner-by-corner under
            // this vocabulary, and the 1x1 raised "Ramp" group (TILE242, all-Desert [0,1,1,0])
            // classifies as a ReliefPiece stamped onto painted rim edges (wired below). No pools:
            // no raised open-vs-Chasm bank shape exists (Chasm banks sit at grade).
            // Residual height exemptions, each shape-verified unreachable: TILE239-241 are
            // Road-crossered raised lanes and TILE255 mixes Dunes+Road on one cell (ramp lanes carry
            // ONE declared crosser name -- "Dunes" -- and no mechanism splices a second family);
            // "SmallCave" (TILE243) is raised AND door-slot-bearing -- no mechanism places a raised
            // door group (ReliefPiece is doorless-only, GroupExitPlanner flat-only; the tdm01 Cave
            // Entrance gap).
            //
            // The hak adds two extra terrains, "Svirfneblin" and "Poor" (village-hut ground palettes):
            // best coverage against the Desert open is 14/16 with no Cliff blending at all (verified
            // directly), so both are auto-exempted via TileCoverageCensusTests.
            // PilotAlternateVocabTerrains -- this also covers the eight ungrouped door-slot tiles on
            // those palettes (TILE203/205/207/208/211/212/221/223, bare door slots with no crosser,
            // which TileDoorPlanner's single-Doorway-edge rule could never place anyway).
            //
            // Groups: the four "crossroads" gates (WallGate01/02 = Wall+Road, TrenchBridge01/02 =
            // Trench+Road -- TWO independent crosser families on perpendicular opposite-edge pairs of
            // ONE tile) stay exempt: no mechanism models a two-family intersection cell. Everything
            // else classifies: the all-Desert building/decor families (Temple/AdobeBuilding/
            // DesertCityBlock/Oasis_3x3/Marketplace/obi_hutt/palais_jabba/Astroport/Maison_1/2/
            // Barge/Dowager/...) as OpenSetPieces standing in the walkable desert clearings,
            // Desert+Cliff mixed groups (Carved_Exit_2x2, CarvedCorner) as OpenSetPieces too, and the
            // 1x1 door-bearing exits (Exit/CliffStairs/CaveEntrance/ChasmStairs) as ExitGroups.
            // Star Wars-specific hak additions (palais_jabba 6x6, Astroport 8x8) are wired at
            // maxPerArea 1, mirroring Barrows' FinalArea_7x7 large-set-piece precedent (placement
            // requires a room large enough for the footprint; smaller rooms simply never site them).
            _builder.Create(Desert, "Desert")
                .Tileset("ttd01")
                .SolidTerrainOverride("Cliff")
                .PrimaryOpenTerrain("Desert")
                .MaxElevationRegions(2)
                .MaxReliefRegions(2)
                .RampCrosser("Dunes")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .AccentTerrain("Chasm")
                .FeatureTile("Ruin")
                .FeatureTile("Camp")
                .FeatureTile("Camp 2")
                .FeatureTile("GiantHead")
                .FeatureTile("CaravanWagon2")
                .FeatureTile("Chessboard")
                .FeatureTile("Well")
                .FeatureTile("Sarlacc")
                .FeatureTile("Grande_tour")
                .FeatureTile("tour_oblong")
                .FeatureTile("boutique")
                .FeatureTile("element")
                // "Portal" is deliberately NOT a FeatureTile: a semantic teleporter tile, excluded
                // from random sprinkling and wired as a rare set piece instead.
                .SetPiece("Portal", 1)
                .SetPiece("BridgeDoor01", 1)
                .SetPiece("Ruin01_2x2")
                .SetPiece("Ruin02_1x2")
                .SetPiece("Temple_3x2")
                .SetPiece("DesertCityBlock_2x2")
                .SetPiece("DesertCityBlock_2_2x2")
                .SetPiece("AdobeBuilding_1x2")
                .SetPiece("AdobeBuilding_2x2")
                .SetPiece("Camp01_2x2")
                .SetPiece("Camp02_1x2")
                .SetPiece("SmallTent", 1)
                .SetPiece("Marketplace")
                .SetPiece("Oasis_3x3")
                .SetPiece("Carved_Exit_2x2")
                .SetPiece("CarvedCorner", 1)
                .SetPiece("NeutralTemple_2x2")
                .SetPiece("GoodTemple_3x3")
                .SetPiece("EvilTemple_2x3")
                .SetPiece("TurfHouse", 1)
                .SetPiece("RuinedTowers_2x3")
                .SetPiece("CaravanWagon1", 1)
                .SetPiece("Minaret", 1)
                .SetPiece("LargeTent_2x2")
                .SetPiece("obi_hutt")
                .SetPiece("palais_jabba", 1)
                .SetPiece("Astroport", 1)
                .SetPiece("Barge")
                .SetPiece("Maison_1")
                .SetPiece("Maison_2")
                .SetPiece("Dowager")
                // Baked-mesh dune ramp (1x1 raised GROUP, all-Desert [0,1,1,0]) -- stamped by
                // LayoutGroupStamper's ReliefPiece kind onto a painted raised rim edge.
                .SetPiece("Ramp", 1)
                .ExitGroup("Exit")
                .ExitGroup("CliffStairs")
                .ExitGroup("ChasmStairs")
                .ExitGroup("CaveEntrance");

            // Forest (ttf01, SWLOR_Haks/sw_t_forest -- a 1148-tile HasHeightTransition=1 mega-set, an
            // 11-terrain/13-crosser superset of the 168-tile vanilla version). Same INVERTED
            // composition as Desert above: SolidTerrainOverride("Cliff") + PrimaryOpenTerrain
            // ("Forest") -- the GENERAL Default ("Forest") is the walkable ground (its fully-open
            // tile is pathnode A; Cliff's is pathnode-restricted).
            //
            // AccentTerrain("Water") (full 16/16 flat coverage against BOTH Forest and Cliff, zero
            // combos lacking pathnode-A) gives LayoutAccentPainter blob lakes; ChannelTerrain("Pit")
            // keeps the Bridge-gated chasm channel separate (vmr01's own Chasm-channel precedent) --
            // "Door - Bridge, Pit" is the all-Pit opposite-Bridge-pair CorridorInsert. No Tunnel
            // vocabulary under the Cliff solid: every crosser family (Wall/Road/Stream/RuralStream/
            // ..., same-name pairs) resolves only against Solid=Forest compositions (verified
            // directly) -- they all run through the walkable open ground -- so Complex downgrades to
            // OpenLane, and "Tower - Archer, Forest Wall/Corner" (the Wall family's insert tiles)
            // stays exempt (see PilotExpectedExemptions).
            //
            // Heights (192 non-flat tiles of 1148; Cliff never carries a nonzero corner height,
            // verified directly): MaxElevationRegions(2) (crosser-free raised-Forest rim tiles),
            // RampCrosser("Slope") -- the ubiquitous outdoor slope lanes (raised Forest tiles
            // carrying Slope edges, e.g. TILE547-562/602-622) are this tileset's ramp vocabulary --
            // and MaxReliefRegions(2) (the census relief BFS verifies 32 raised tiles reachable under
            // this vocabulary, including the multi-Slope-edge lane cells; the 1x1 raised "Ramp" group
            // classifies as a ReliefPiece, wired below). No pools: no raised open-vs-Water bank shape
            // exists at the RaiseDelta the pool painter needs. Residual height exemptions,
            // bucket-verified unreachable: (a) raised banks mixing the unwired RuralWater/RuralTrees
            // palettes (TILE500-529/541-546/563-573/600-601/623-628/883-887); (b) raised lanes on the
            // unwired Road/RuralStream/RuralWallOne/Two/CityWall/MossWall crosser families
            // (TILE530-532/606-609/719-734/741-779/801-824 -- ramp lanes carry ONE declared crosser
            // name, "Slope"); (c) raised Bridge/StoneBridge banks (TILE895/896/898); (d) raised
            // GROUPS: "Cave" (raised AND door-bearing -- the tdm01 Cave Entrance gap), the City
            // Gate/Wall - Breach/Door/Tower/Ramp - City Wall/Moss Wall families (raised wall-top
            // content on unwired crosser families).
            //
            // Alternate palettes (auto-exempted via PilotAlternateVocabTerrains, each verified
            // directly): GoodCastle/EvilCastle and RuralTrees/RuralWater are full separate district
            // palettes (out of this wave's scope -- the tni01 room-palette precedent); Marsh reaches
            // only 14/16 against Forest. Platform and HighForest blend only 2/16 against Forest, and
            // HighForest only 2/16 against Cliff too -- but Platform reaches 16/16 against Cliff AND
            // 16/16 against Pit, and HighForest also reaches 16/16 against Pit (verified directly by
            // 16-combo probe): see the "Forest (Platform)" PaletteVariant below, which declares
            // SolidTerrainOverride("Pit") + PrimaryOpenTerrain("Platform") to close the Platform
            // GROUPS that need a Solid+Open pair covering Pit and Platform simultaneously (every
            // ungrouped Platform/HighForest-cornered simple tile was ALREADY CornerEdgeResolver-
            // reachable regardless of vocab, so only the groups needed a dedicated variant; a
            // dedicated HighForest variant would add no additional coverage since no group uses
            // HighForest corners). Still exempt after that variant, still terrain-listed here:
            // "Platform - Cliff Door" and "Platform - Cliff Section" mix Platform+Cliff+Pit (three
            // terrains on one group -- no two-terrain classifier reaches it), and the four remaining
            // GoodCastle/EvilCastle/RuralTrees/RuralWater tiles. Unwired crosser families
            // (PilotAlternateVocabCrossers):
            // DlaEdgeFix, StoneBridge, RuralStream, MossWall, CityWall, RuinWall, RuralWallOne/Two --
            // their flat door-free tiles all resolve via CornerEdgeResolver regardless; the entries
            // exempt the few flat door/group tiles (e.g. "Bridge - Footbridge, Rural Stream",
            // "Wall - Gate, Ruin").
            //
            // FeatureTile curation: semantic/functional tiles are deliberately NOT sprinkled --
            // "Portal - Forest"/"Portal - Platform" (teleporters), "Entrance - Dungeon" (a transition
            // mouth), "Platform - Elevator, Upper/Lower" (paired elevators) are wired as maxPerArea-1
            // set pieces instead ("Portal - Platform" and the elevators are additionally
            // Platform-palette content and stay unwired entirely).
            _builder.Create(Forest, "Forest")
                .Tileset("ttf01")
                .SolidTerrainOverride("Cliff")
                .PrimaryOpenTerrain("Forest")
                .MaxElevationRegions(2)
                .MaxReliefRegions(2)
                .RampCrosser("Slope")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .AccentTerrain("Water")
                .ChannelTerrain("Pit")
                .FeatureTile("Ruin")
                .FeatureTile("Camp")
                .FeatureTile("Graveyard")
                .FeatureTile("Webbed Forest")
                .FeatureTile("Tree - Big 1")
                .FeatureTile("Tree - Big 2")
                .FeatureTile("Tree - Hollow 1")
                .FeatureTile("Tree - Hollow 2")
                .FeatureTile("Tree - Giant")
                .FeatureTile("Chessboard")
                .FeatureTile("Tower - Archer")
                .FeatureTile("Wagon - Caravan 1")
                .FeatureTile("Wagon - Caravan 2")
                .FeatureTile("Wall Chunk")
                .FeatureTile("Building - Destroyed 1")
                .FeatureTile("Building - Burned")
                .FeatureTile("Rock Formation")
                .FeatureTile("Cobbles")
                .FeatureTile("Crystal - Big")
                .FeatureTile("Fountain")
                .FeatureTile("Menhir")
                .FeatureTile("Market")
                .SetPiece("Portal - Forest", 1)
                .SetPiece("Entrance - Dungeon", 1)
                .SetPiece("Door - Bridge, Pit", 1)
                .SetPiece("Ruin 1 (2x2)")
                .SetPiece("Ruin 2 (1x2)")
                .SetPiece("Temple - Forest (3x2)")
                .SetPiece("House - Shack 1 (2x2)")
                .SetPiece("House - Shack 2 (1x2)")
                .SetPiece("Lodge (2x2)")
                .SetPiece("Camp 1 - Deciduous (2x2)")
                .SetPiece("Camp 2 - Deciduous (1x2)")
                .SetPiece("Camp 1 - Coniferous (2x2)")
                .SetPiece("Camp 2 - Coniferous (1x2)")
                .SetPiece("Graveyard (1x2)")
                .SetPiece("Meeting Area (1x2)")
                .SetPiece("Grove 1 (3x3)")
                .SetPiece("Exit 1 (2x3)")
                .SetPiece("Exit 2 (2x2)")
                .SetPiece("Webbed Corner", 1)
                .SetPiece("Temple - Good (3x3)")
                .SetPiece("Temple - Neutral (2x2)")
                .SetPiece("Temple - Evil (2x3)")
                .SetPiece("Tower - Cloak (2x2)")
                .SetPiece("House - Turf (2x2)")
                .SetPiece("Tower - Ruined (2x2)")
                .SetPiece("Barracks (2x2)")
                .SetPiece("Barracks (1x2)")
                .SetPiece("Tower - Guard, Forest (1x2)")
                .SetPiece("Tower - Wizard (1x2)")
                .SetPiece("Ruined Park (1x2)")
                .SetPiece("House - Small (1x2)")
                .SetPiece("Ship - Air, Docked (3x1)", 1)
                .SetPiece("House - Elven 1 (3x3)")
                .SetPiece("House - Elven 2 (2x2)")
                .SetPiece("House - Elven 3 (2x2)")
                .SetPiece("House - Elven 4 (2x2)")
                .SetPiece("House - Treehouse 1 (1x2)")
                .SetPiece("House - Treehouse 2 (2x2)")
                .SetPiece("Cave - Hill (2x2)")
                .SetPiece("Temple - Elven 1 (2x2)")
                .SetPiece("Tower - Elven (3x3)")
                .SetPiece("Plaza - Elven (3x3)")
                .SetPiece("Gate - Cliff (1x3)")
                .SetPiece("Building - State, Ruined (2x3)")
                .SetPiece("Building - Destroyed 2 (1x2)")
                .SetPiece("Tower - Archer Platform (1x2)")
                .SetPiece("Smithy (1x2)")
                .SetPiece("Tree - Giant (2x2)")
                // Baked-mesh slope ramp (1x1 raised GROUP, all-Forest) -- ReliefPiece kind, stamped
                // onto a painted raised rim edge.
                .SetPiece("Ramp", 1)
                .ExitGroup("Exit")
                .ExitGroup("Stairs - Cliff")
                .ExitGroup("Stairs - Pit")
                .ExitGroup("House - Small 1")
                .ExitGroup("House - Small 2")
                .ExitGroup("House - Small 3")
                .ExitGroup("House - Turf")
                .ExitGroup("House - Ruined")
                .ExitGroup("Tower - Stone");

            // Forest (Platform) -- ttf01's "Platform" chasm-bridge district, a PaletteVariant profile
            // recomposing the SAME ttf01 hak data the base Forest profile above uses, closing part of
            // the "Platform and HighForest blend ONLY with Pit... no composition can make Pit its
            // solid" assumption in the base profile's own doc comment above -- that assumption
            // predates SolidTerrainOverride's ability to pick an INVERTED solid per PaletteVariant
            // (verified false by direct 16-combo probe: PrimaryOpenTerrain("Platform") +
            // SolidTerrainOverride("Pit") gives full 16/16 flat corner coverage, same as
            // Platform-vs-Cliff; HighForest also reaches 16/16 against Pit, but every ungrouped
            // HighForest-cornered tile (TILE906/907/921-924/928-945, mixed freely with Platform/Pit/
            // Cliff) was ALREADY CornerEdgeResolver-reachable under the base profile regardless of
            // vocab -- IsCornerEdgeResolverReachable resolves a flat, door-free (or Doorway/Bridge-
            // door-bearing) tile against its own raw corners, independent of any profile's declared
            // Open/Solid pair -- so no dedicated HighForest variant is needed; this profile exists to
            // close the GROUPS that fail ClassifySetPiece's two-terrain (Solid/Open) binary under the
            // base Cliff/Forest vocab because they use Platform+Pit corners instead.
            // Closes (Solid=Pit, Open=Platform, all corners verified in {Pit, Platform} only):
            // "Platform - Building (2x3)" (all-Platform), "Platform - Elevator, Upper" (all-Platform),
            // "Platform - House 1/2", "Platform - Pillar 1/2", "Platform - Plaza"/"Plaza (1x2)"
            // (Pit/Platform mixes), "Tower - Guard, Pit (1x2)" (one Platform-cornered member, one
            // all-Pit member -- the group-wide "at least one Open corner somewhere in the group"
            // rule). Stay exempt (genuinely unmodeled, verified directly): "Platform - Cliff Door"
            // (TILE966, [Platform,Cliff,Cliff,Platform] -- Cliff isn't in this variant's vocab, and
            // making Cliff the solid instead reopens the base profile's own Platform-vs-Cliff 16/16
            // pairing but abandons Pit, which "Platform - Cliff Section" below still needs); "Platform
            // - Cliff Section (2x3)" (TILE949-954, genuinely THREE terrains -- Platform, Cliff, AND
            // Pit -- on one group's members; ClassifySetPiece's matchesPrimary/matchesSecondary each
            // only ever admit a Solid+ONE-other-terrain pair, never three simultaneously, so no single
            // profile composition can close a true three-terrain group). "Portal - Platform"/"Platform
            // - Elevator, Lower"/"Crystal - Platform"/"Tower - Archer Platform" are misleadingly named
            // but physically all-Forest -- already reachable under the base profile, untouched by this
            // variant. "Tower - Guard, Pit" (solo, TILE963, all-Pit) is a separate, pre-existing
            // "uniform accent terrain, no Open/Solid corner at all" gap (see PilotExpectedExemptions --
            // ClassifySetPiece's matchesPrimary requires at least one Open corner even when every
            // corner is otherwise a valid Solid), unrelated to Platform/HighForest and unchanged here.
            // Bonus closure this SAME SolidTerrainOverride("Pit") composition unlocks for free: "Ship -
            // Air, Above Pit (3x1)" (TILE987-989, uniform Pit, WITH a door slot) now satisfies
            // ClassifyMultiTileSetPiece's allCornersSolid+hasAnyDoor rule -> SetPieceWallAlcove (a
            // supported LayoutGroupStamper production kind, not census-only) -- wired below so this is
            // a real placement, not just a passive census credit -- verified via direct seed sweeps: it
            // places in 100/100 real generations. The doorless uniform-Pit siblings ("Tower - Guard,
            // Pit" solo, "Island (3x3)") still lack a door slot and stay exempt (see
            // TileCoverageCensusTests.PilotExpectedExemptions's updated ttf01 comment).
            // Caveat on the OpenSetPiece groups above (House/Pillar/Plaza/Building/TowerGuardPit): a
            // direct reflection probe confirms TryClassify correctly resolves them to Kind=OpenSetPiece
            // and TryPlaceOpenSetPiece correctly stamps them given an adequately roomy, off-center
            // site. This USED TO be rare in practice -- real Halls/Complex-carved rooms at typical
            // sizes essentially never offered a site clear of the room's unconditionally-reserved
            // CenterTile, a gap reproduced identically on tdm01 (interior) and this same base Forest
            // profile's own long-shipped "Ruin 1 (2x2)" (0/90 seed hits each, verified directly), not
            // introduced by this variant. LayoutGroupStamper.IsOpenSetPieceSiteValid now relocates
            // CenterTile to another still-open room tile when a candidate site would otherwise consume
            // it, instead of rejecting the site outright (see OpenSetPiecePlacementRateTests) -- Halls-
            // paired compositions (MaxRoomCornerSize=6, one tile of slack beyond a 2x2 footprint's
            // margin) now place at a real, measured, nonzero rate (e.g. "Ruin 1 (2x2)" isolated on
            // Forest/Halls: 35.7%, 107/300). Complex-paired compositions (MaxRoomCornerSize=5, zero
            // slack) remain a genuine, separate geometric ceiling this fix cannot address -- see
            // OpenSetPiecePlacementRateTests' own doc comment for the full before/after accounting.
            // PaletteVariant() excludes this from --matrix's full cross-product -- one showcase area.
            _builder.Create(ForestPlatform, "Forest (Platform)")
                .Tileset("ttf01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .PaletteVariant()
                .PrimaryOpenTerrain("Platform")
                .SolidTerrainOverride("Pit")
                .SetPiece("Platform - Building (2x3)", 1)
                .SetPiece("Platform - Elevator, Upper", 1)
                .SetPiece("Platform - House 1", 1)
                .SetPiece("Platform - House 2", 1)
                .SetPiece("Platform - Pillar 1")
                .SetPiece("Platform - Pillar 2")
                .SetPiece("Platform - Plaza")
                .SetPiece("Platform - Plaza (1x2)")
                .SetPiece("Tower - Guard, Pit (1x2)", 1)
                .SetPiece("Ship - Air, Above Pit (3x1)", 1);

            // Forest (Rural) -- ttf01's unwired RuralWater/RuralTrees raised-bank district (see
            // BaseGameTilesetProfiles.Forest's own doc comment, "unwired RuralWater/RuralTrees" note),
            // a PaletteVariant profile recomposing the SAME ttf01 hak data the base Forest profile
            // uses. Direct probe (TileResolver.HasCandidate over all 16 flat corner masks) confirms
            // RuralWater and RuralTrees each blend 16/16 against Solid=Forest -- i.e. they are walkable
            // "ground cover" terrains layered onto Forest, not a Cliff-style solid mass, matching the
            // hak's own raised-bank tile inventory (TILE500-529/541-546/563-573/600-601): every
            // non-flat RuralWater/RuralTrees tile mixes ONLY {Forest, RuralWater} or {Forest,
            // RuralTrees} corners (never Cliff), one-step height deltas, blank edges, doorless,
            // ungrouped -- the exact shape LayoutElevationPoolPainter's irregular pool-bank grower
            // (IsPoolBankReachable's mirror) and LayoutReliefPainter's relief-blend flip
            // (IsTerrainReliefReachable's mirror) already carve for a (PrimaryOpenTerrain,
            // AccentTerrain)/(PrimaryOpenTerrain, ReliefBlendTerrain) pair -- so RuralWater is wired as
            // this variant's pool AccentTerrain (banks around an irregular water-pool patch) and
            // RuralTrees as its ReliefBlendTerrain (a gentle grade blend, the same "walkable slope"
            // role tdm01's GentleSlope/GentleDesert/GentleOrganic play), both against the SAME base
            // Solid=Cliff/Open=Forest pair the base Forest profile already verified 16/16. (The flat,
            // door-free RuralStream/RuralWallOne/RuralWallTwo/Road/CityWall-crossered siblings on
            // these same corners were ALREADY CornerEdgeResolver-reachable before this variant --
            // TileResolver.HasCandidate matches a tile's own raw corners/edges regardless of any
            // profile's declared vocabulary -- so CornerEdgeResolver's count is unchanged by this
            // profile; verified directly via the census re-run below.)
            // Closes: 52 of ttf01's 150 previously height-exempt tiles (PoolBank +6: TILE510/511/513/
            // 541/542/544, single-corner RuralWater rim-bank shapes; TerrainRelief +46). Since
            // IsTerrainReliefReachable's InPalette check accepts Open, Accent, OR Blend independently
            // per corner (not just a two-terrain pair), this ALSO closes two shapes beyond the single-
            // terrain-pair case its own doc comment anticipated -- both re-verified directly, not
            // assumed: (1) genuine three-terrain ADJACENT mixes, e.g. TILE514-519 (Forest+RuralWater+
            // RuralTrees corners sharing an edge each); (2) blank-edge, ALL-FOREST diagonal saddles
            // (two NON-adjacent raised corners, the same shape IsElevationBlobReachable's adjacency
            // check alone would reject) -- TILE507/538, closed via the BFS field search
            // (IsReliefFieldReachable) finding an intermediate Forest/RuralTrees-blended construction
            // path even though the FINAL tile itself never shows a RuralTrees corner. Real-generation
            // placement proof: LayoutReliefPainterTests.
            // RealForestRuralComplexComposition_PaintsRuralWaterAndRuralTreesBanks (Complex composition,
            // 60 seeds, both RuralWater pool-bank corners and RuralTrees relief-blend corners actually
            // painted). Stays exempt (genuinely unmodeled, verified directly): blank-edge DIAGONAL
            // two-terrain splits, e.g. TILE512/528/543/747/896 (Forest+RuralWater or Forest+RuralTrees
            // on NON-adjacent corners -- the BFS construction path that closes the single-terrain
            // diagonal case above never finds one for a two-terrain diagonal); the uniform-RuralWater
            // rim blob TILE884 (IsElevationBlobReachable only ever checks vocab.Solid/vocab.Open
            // uniformity, never vocab.Accent); and every crosser-bearing raised tile whose crosser
            // isn't the declared Ramp ("Slope") -- CityWall/MossWall/RuralWallOne/RuralWallTwo/
            // RuralStream/Road/StoneBridge raised lanes (the bulk of the remaining exempt bucket) stay
            // a SEPARATE gap (a composition carries only one RampCrosser slot, already claimed by
            // "Slope" here -- see the closure toolkit's "additional families = additional variants"
            // note).
            // PaletteVariant() excludes this from --matrix's full cross-product -- one showcase area.
            _builder.Create(ForestRural, "Forest (Rural)")
                .Tileset("ttf01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .PaletteVariant()
                .SolidTerrainOverride("Cliff")
                .PrimaryOpenTerrain("Forest")
                .AccentTerrain("RuralWater")
                .ReliefBlendTerrain("RuralTrees")
                .MaxElevationRegions(2)
                .MaxPoolRegions(2)
                .MaxReliefRegions(2)
                .RampCrosser("Slope");

            // Forest - Facelift (ttf02, BIF-only: no hak copy exists anywhere under SWLOR_Haks --
            // verified directly -- so TilesetSetSource's hak-first lookup falls through to the
            // committed basegame_sets/ttf02.set vanilla extraction, 211 tiles, fully flat). A
            // DIFFERENT tileset resref from Forest (ttf01) above, not a PaletteVariant of it, even
            // though its GENERAL/terrain/crosser vocabulary is identical (Default==Floor=="Forest",
            // same Cliff/Pit terrains, same Wall/Road/Stream/Bridge crossers) and it shares most of
            // vanilla ttf01's group inventory tile-for-tile. Same INVERTED composition:
            // SolidTerrainOverride("Cliff") + PrimaryOpenTerrain("Forest"); AccentTerrain("Pit") is
            // the Bridge-gated channel (BridgeDoor01). No Tunnel vocabulary under the Cliff solid
            // (Wall/Road/Stream all resolve only against Solid=Forest, verified directly) -- Complex
            // downgrades to OpenLane. No height content at all (HasHeightTransition=0, zero non-flat
            // tiles), so every height knob correctly stays off.
            //
            // ttf02 adds a facelift-only decorative family beyond vanilla ttf01's inventory: Monument
            // (1x1 pathnode-A FeatureTile) and three all-Forest log groups (SideLog1 3x4, UpgrightLog1
            // 4x4, UprightLog2 2x2) that classify as OpenSetPieces like the rest of the all-Forest
            // building/decor families.
            //
            // Exemptions (PilotExpectedExemptions, verified directly): WallGate01/02 (Wall+Road) and
            // StreamBridge01/02 (Stream+Road) are the same two-crosser-family crossroads cells as
            // Desert's; Island_Tree (3x3, uniform-Pit corners with one Bridge edge that never
            // triggers CorridorStubChain since Bridge is not a body crosser) and Island_Connector
            // (1x1, Forest+Pit mixed corners) are accent-terrain groups no mechanism stamps (the same
            // gap as ttf01's Island (3x3), on this tileset's own smaller island family).
            _builder.Create(ForestFacelift, "Forest - Facelift")
                .Tileset("ttf02")
                .SolidTerrainOverride("Cliff")
                .PrimaryOpenTerrain("Forest")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .AccentTerrain("Pit")
                .FeatureTile("Ruin")
                .FeatureTile("Camp")
                .FeatureTile("Graveyard")
                .FeatureTile("WebbedForest")
                .FeatureTile("BigTree")
                .FeatureTile("Chessboard")
                .FeatureTile("Monument")
                .SetPiece("Portal", 1)
                .SetPiece("BridgeDoor01", 1)
                .SetPiece("Ruin01_2x2")
                .SetPiece("Ruin02_1x2")
                .SetPiece("Temple_3x2")
                .SetPiece("Shack01_2x2")
                .SetPiece("Shack02_1x2")
                .SetPiece("Lodge_2x2")
                .SetPiece("Camp01_2x2")
                .SetPiece("Camp02_1x2")
                .SetPiece("Graveyard_1x2")
                .SetPiece("Meeting_Area")
                .SetPiece("Grove01_3x3")
                .SetPiece("Exit01_2x3")
                .SetPiece("Exit02_2x2")
                .SetPiece("WebbedCorner", 1)
                .SetPiece("SideLog1")
                .SetPiece("UpgrightLog1")
                .SetPiece("UprightLog2")
                .ExitGroup("Exit")
                .ExitGroup("Tower");

            return _builder.Build();
        }
    }
}
