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
            // The 1x1-GROUPed "Ramp - Straight"/"Ramp - Corner, *" pieces are still not wired (non-flat,
            // so LayoutGroupStamper rejects them outright -- a separate, still-unclaimed mechanism).
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
            // "Ramp - Corner, <Accent>" is a raised (HasHeightTransition) tile, excluded the same way the
            // base profile's own Ramp pieces are. PaletteVariant() excludes each from --matrix's full
            // cross-product -- one showcase area each instead.
            _builder.Create(DungeonWater, "Dungeon (Water)")
                .Tileset("tde01")
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
                .ExitGroup("Exit 1")
                .ExitGroup("Exit 2 - Water");

            _builder.Create(DungeonSewer, "Dungeon (Sewer)")
                .Tileset("tde01")
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
                .ExitGroup("Exit 1")
                .ExitGroup("Exit 2 - Sewer");

            _builder.Create(DungeonIce, "Dungeon (Ice)")
                .Tileset("tde01")
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
                .ExitGroup("Exit 1")
                .ExitGroup("Exit 2 - Ice");

            _builder.Create(DungeonPit, "Dungeon (Pit)")
                .Tileset("tde01")
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
            // "[Desert] Cave Entrance" and "[Desert] Ramp" are non-flat (HasHeightTransition tiles,
            // outside this pilot's flat-only classifiers) and excluded, matching [Cave]'s own Ramp/Cave
            // Entrance exclusion. Every other Desert group (Platforms, Pillar, Stairs 2x2, Treasure,
            // Crystal Casket/Column/Crypt, Chessboard, Portal, Mineshaft, Wall Section, Exit 1/2/3)
            // mirrors [Cave]'s own wired set piece/feature-tile/exit-group shapes tile-for-tile.
            // IsPaletteVariant() excludes this profile from --matrix's full cross-product (see
            // SWLOR.ProcgenReview/Program.cs) -- it gets one showcase area instead. [Organic] and [City]
            // remain unwired (left for a future wave; [Organic] mirrors [Desert]'s shape closely but
            // [City] has a much smaller, differently-shaped tile family and would need its own probe).
            _builder.Create(MinesAndCavernsDesert, "Mines and Caverns (Desert)")
                .Tileset("tdm01")
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
            // stays unwired for the same one-body-crosser-per-profile reason, and Cave Entrance/Ramp stay
            // excluded as non-flat -- see the Desert profile's comment for the full reasoning.
            _builder.Create(MinesAndCavernsOrganic, "Mines and Caverns (Organic)")
                .Tileset("tdm01")
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
                .ExitGroup("[Organic] Exit 1")
                .ExitGroup("[Organic] Exit 2")
                .ExitGroup("[Organic] Exit 3");

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

            return _builder.Build();
        }
    }
}
