#nullable disable
using System.Collections.Generic;
using SWLOR.Toolset.Domain.AreaGeneration;
using SWLOR.Toolset.Domain.AreaGeneration.Decoration;
using SWLOR.Toolset.Domain.AreaGeneration.Frontage;
using SWLOR.Toolset.Domain.AreaGeneration.Tileset;

namespace SWLOR.Toolset.Domain.AreaGeneration.Definitions
{
    /// <summary>
    /// Initial base-game (non-hak) tileset profiles: Crypt (tdc01), Dungeon (tde01), and
    /// City Interior (tin01), resolved from basegame_sets via the shared TilesetSetSource (see
    /// the base-game tileset census, SWLOR.Toolset.Tests/AreaGeneration/TileCoverageCensusTests.cs).
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

        // Palette-variant profiles: recompose an already-registered physical tileset resref against one of its
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

        // Additional interior base-game tilesets, all
        // resolved to their SWLOR_Haks copy by TilesetSetSource (every one of these ten has been
        // copied into a hak, unlike the initial three where only Crypt/Dungeon had hak copies and City
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

        // Exterior base-game tilesets (ttd01/ttf01/ttf02) -- see the base-game tileset
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
        // the minimum-opening-width path-node audit coverage.
        public const string Desert = "desert";
        public const string DesertRoad = "desert_road";
        public const string Forest = "forest";
        public const string ForestFacelift = "forest_facelift";
        public const string ForestPlatform = "forest_platform";
        public const string ForestRural = "forest_rural";
        public const string ForestGoodCastle = "forest_goodcastle";
        public const string ForestEvilCastle = "forest_evilcastle";
        public const string ForestMarsh = "forest_marsh";
        public const string ForestCityWall = "forest_citywall";
        public const string ForestMossWall = "forest_mosswall";
        public const string ForestRuralWallOne = "forest_ruralwallone";
        public const string ForestRuralWallTwo = "forest_ruralwalltwo";
        public const string ForestRuralStream = "forest_ruralstream";
        public const string ForestRoad = "forest_road";
        public const string ForestStoneBridge = "forest_stonebridge";

        // Jacoby's Jungle (jac01, SWLOR_Haks/sw_t_jungle -- a 380-tile HasHeightTransition=1
        // hak-shipped exterior tileset). See the Jungle profile's own doc comment below for the full
        // probe writeup (a lean sibling of Forest/ttf01: same degenerate Default==Floor=="Forest"
        // GENERAL quirk, same inverted SolidTerrainOverride("Cliff")/PrimaryOpenTerrain("Forest")
        // composition, and a near-identical group-naming vocabulary, but only 7 terrains/5 crossers
        // against ttf01's 11/13 -- no RuralTrees/RuralWater/GoodCastle/EvilCastle/Marsh/CityWall/
        // MossWall/RuinWall/RuralWallOne/Two/StoneBridge/DlaEdgeFix districts at all).
        public const string Jungle = "jungle";
        public const string JunglePlatform = "jungle_platform";

        // Rural Grass (ttr01, SWLOR_Haks/sw_t_rural -- a 653-tile HasHeightTransition=1
        // hak-shipped exterior tileset, UnlocalizedName "Rural Grass*"). Same degenerate GENERAL quirk
        // as ttd01/ttf01/jac01 (Default=Floor=Border="Grass", the walkable ground), but UNLIKE every
        // previous exterior tilesets, ttr01 has no Cliff-equivalent wall mass at all: Grass reaches full
        // 16-combo coverage against EVERY other terrain (Water/Trees/Forest/GentleHill/EvilCastle/
        // GoodCastle), and every one of those six is a minor accent/district family (1-8 uniform flat
        // tiles), not a genuine rock/wall inventory -- confirmed via a real LayoutSolver pipeline sweep
        // (15 seeds x Complex/Halls/Organic, ProbeTool), not merely the 16/16 table: Forest-as-solid
        // and Trees-as-solid both "succeed" on paper but neither carries a single GROUP (no
        // forest-specific building/wall family exists to unlock), so composing either as
        // SolidTerrainOverride would only manufacture a fake, repetitive 4-8-tile wall ring around an
        // otherwise open-field tile vocabulary. The base Rural profile therefore leaves
        // SolidTerrainOverride UNSET (LayoutSolver.Solve then stamps Solid=tileset.DefaultTerrain=
        // "Grass", identical to PrimaryOpenTerrain("Grass")) -- a genuinely open field with no wall
        // concept, matching the tile inventory's own identity: all 91 GROUPS are pastoral structures
        // (Barn/Farm/Temple/Tower/Windmill/Well/Graves/Shrine/Garden/Orchard/Anthill/Wagon/Warzone/
        // Dragon Skeleton) dropped onto open Grass, never a wall/rock mass. See RuralGrass's own doc
        // comment for the ChannelTerrain/ReliefBlendTerrain/RampCrosser vocabulary and RuralGrass
        // GoodCastle/EvilCastle/Water's own doc comments for the three PaletteVariant districts.
        public const string RuralGrass = "ruralgrass";
        public const string RuralGrassGoodCastle = "ruralgrass_goodcastle";
        public const string RuralGrassEvilCastle = "ruralgrass_evilcastle";
        public const string RuralGrassWater = "ruralgrass_water";

        // Rural Winter* (tts01) -- the winter reskin sibling of ttr01 Rural Grass (hak wins over the
        // basegame_sets fallback; RESREF TRAP: tts01 is "Rural Winter*", NOT ttr01). Same 91-group,
        // 6-terrain (Snow/Water/Trees/GentleHill/EvilCastle/GoodCastle -- no Forest terrain at all
        // here) shape as ttr01, and the same pipeline-sweep conclusion applies: Snow pairs 16/16 with
        // every other terrain (verified directly, mirroring RuralGrass's own matrix), and both
        // candidate solids (Trees: 4 uniform tiles, GentleHill: 3 uniform tiles, neither carrying a
        // GROUP -- verified directly) are the identical starved-minor-family shape RuralGrass's own doc
        // comment documents for ttr01's Forest/Trees/GentleHill. No SolidTerrainOverride here either --
        // solid==open==Snow, a genuinely open field. See RuralWinter's own doc comment below for the
        // real group-inventory deltas against ttr01 (Snowdrift/Snowy Pines/Turf House (2x2)/Wall - Over
        // Stream additions; Cave - Sea/Pier/Door - Bridge, High/the four Tower - Large pieces removed;
        // Turf House itself flips from FeatureTile to ExitGroup because tts01's copy carries a real
        // door where ttr01's does not).
        public const string RuralWinter = "ruralwinter";
        public const string RuralWinterGoodCastle = "ruralwinter_goodcastle";
        public const string RuralWinterEvilCastle = "ruralwinter_evilcastle";
        public const string RuralWinterWater = "ruralwinter_water";

        // Rural Winter - Facelift (tts02, BIF-only -- verified directly, no SWLOR_Haks copy exists
        // anywhere under sw_t_winter/sw_t_cepwinter or elsewhere; falls through TilesetSetSource to
        // basegame_sets/tts02.set, 331 tiles / 84 groups). MIRROR-CHECK (ProbeTool "mirrorvanilla"/
        // "hakmap2", model-resref-level, not name-level): tts02 is NOT a reskin of the SWLOR-hak-
        // renamed RuralWinter profile above -- it is a byte-for-byte content mirror of VANILLA
        // basegame_sets/tts01.set (the pre-hak-rename original ttf02-vs-ttf01 shape): all 74 of
        // vanilla tts01's groups exist in tts02 under the IDENTICAL underscored names (Barn01_2x2,
        // DragSkel_1x2, ...) with matching dims/doors/terrains/model-resref multisets, verified
        // directly. The 91-group, human-readable-named RuralWinter profile above is built against a
        // SEPARATE SWLOR-hak-customized copy of tts01 (SWLOR_Haks/sw_t_winter, 591 tiles/91 groups)
        // that renamed those same 74 groups to display names (Barn01_2x2 -> "Barn 1 (2x2)", confirmed
        // via per-tile Model-resref matching, not guesswork) and ADDED 17 hak-only groups borrowing
        // foreign zts01/ztr01/tcn01 tileset models (the six Castle - */Good/Evil groups, Cobbles,
        // Crystal - Platform, Fountain, the three Ship - Air groups, and the five Tower - Archer/Winter
        // Wall groups) that have NO counterpart in vanilla tts01 or tts02 at all -- not a census gap,
        // there is no tile content to account for. tts02 in turn adds 10 groups beyond vanilla tts01's
        // 74: a new "Fort" terrain (WallGate3/WallBreach/WatchTower plus three Fort-floor decorative
        // tiles CampFort/WellFort/SnowyDipFort) and four Snow-terrain additions (CampSnow, Mineshaft,
        // HouseV2, HouseV3). Every declaration below was transferred from RuralWinter's own
        // already-verified classification (via the model-resref mapping) and re-verified against
        // tts02's own data, not assumed.
        //
        // tts02 also genuinely LACKS three of RuralWinter's hak-only terrain/crosser vocabulary
        // entries: GentleHill (relief-blend terrain), Slope (ramp crosser), and EvilCastle/GoodCastle
        // (all SWLOR hak additions borrowing foreign models) -- so this profile carries no
        // ReliefBlendTerrain/MaxReliefRegions/RampCrosser declaration at all, a genuine absence, not an
        // oversight. Terrains here are only Snow/Water/Trees/Fort, Crossers only Stream/Wall1/Wall2/
        // Road (verified directly against tts02's own [TERRAIN TYPES]/[CROSSER TYPES] data). Pipeline
        // sweep (ProbeTool "dump"): Snow pairs 16/16 with Water/Trees/Fort; Water/Trees/Fort each only
        // pair 16/16 against Snow (2/16 against each other -- the identical starved-minor-family shape
        // RuralWinter's own doc comment documents for Trees/GentleHill there), so solid==open==Snow,
        // genuinely open field, no SolidTerrainOverride on the base profile.
        //
        // THEME PAIRING: the same pastoral snow-field identity as RuralWinter (Hoth/winter-frontier
        // style farms, temples, ships, graveyards) rendered in the EE facelift art pass -- treat the
        // two as alternate visual dialects of one winter-rural theme, with tts02 additionally offering
        // the unified Fort wall district (RuralWinterFaceliftFort) that tts01's Good/Evil castle split
        // does not.
        public const string RuralWinterFacelift = "ruralwinterfacelift";
        public const string RuralWinterFaceliftWater = "ruralwinterfacelift_water";
        public const string RuralWinterFaceliftFort = "ruralwinterfacelift_fort";

        // Castle Exterior, Rural* (tno01, SWLOR_Haks/sw_t_castleex). See BaseGameTilesetProfiles.
        // CastleExteriorRural's own doc comment below for the full hak-vs-vanilla delta, placeholder-art
        // audit, and composition writeup.
        public const string CastleExteriorRural = "castleexteriorrural";
        public const string CastleExteriorRuralVillage = "castleexteriorrural_village";
        public const string CastleExteriorRuralCastleWall = "castleexteriorrural_castlewall";
        public const string CastleExteriorRuralKeep = "castleexteriorrural_keep";
        public const string CastleExteriorRuralWater = "castleexteriorrural_water";
        public const string CastleExteriorRuralHarbor = "castleexteriorrural_harbor";

        // City Exterior* (tcn01, SWLOR_Haks/sw_t_cityext -- hak wins over the basegame_sets
        // fallback, 1460 tiles / 295 groups, the largest registered set yet). See CityExterior's own doc
        // comment for the full composition writeup.
        public const string CityExterior = "cityexterior";
        public const string CityExteriorFieldstone = "cityexterior_fieldstone";
        public const string CityExteriorGothic = "cityexterior_gothic";
        public const string CityExteriorSigil = "cityexterior_sigil";

        // Frozen Wastes* (tti01, SWLOR_Haks/sw_t_frozen -- hak-only, 510 tiles / 19 groups,
        // 0 crossers). See FrozenWastes' own doc comment for the full composition writeup: unlike
        // previous exterior tilesets, tti01's GENERAL Default ("Pit") and Floor ("Floor") are genuinely
        // DIFFERENT terrains (no degenerate Default==Floor quirk), so the PLAIN default composition
        // applies with no SolidTerrainOverride at all -- the same shape every interior tileset uses,
        // just on an exterior-flavored hak.
        public const string FrozenWastes = "frozenwastes";
        public const string FrozenWastesEvilCastle = "frozenwastes_evilcastle";

        // Tropical* (ttz01, SWLOR_Haks/sw_t_coastal -- hak-only, 442 tiles / 94 groups, 4
        // terrains, 4 crossers). See Tropical's own doc comment for the full composition writeup: the
        // ttr01/tts01 open-field shape (Border=Default=Floor="grass"), PLUS a second, equally-rich
        // native open ground ("sand") the SAME .set data offers -- recomposed here as a genuinely new
        // PaletteVariant shape (SolidTerrainOverride==PrimaryOpenTerrain, an open field on the OTHER
        // terrain, not an inversion like every existing Castle/Water variant) -- PLUS a real
        // water/dock/shipping roster (RuralGrassWater's own shape, twice: once against grass, once
        // against sand).
        public const string Tropical = "tropical";
        public const string TropicalSand = "tropical_sand";
        public const string TropicalWater = "tropical_water";
        public const string TropicalSandWater = "tropical_sandwater";

        // Underdark* (ttu01, SWLOR_Haks/sw_t_underdark -- hak wins over the basegame_sets
        // fallback, 559 tiles / 53 groups, 7 terrains, 5 crossers). The hak copy also silently fixes a
        // genuine Bioware typo present in the vanilla basegame_sets/ttu01.set: vanilla declares a
        // terrain literally spelled "Chasym" (486+ occurrences), but SWLOR_Haks/sw_t_underdark/
        // ttu01.set spells it correctly as "Chasm" throughout (verified directly against both raw .set
        // files) -- since TilesetSetSource always resolves the hak copy first, every profile below uses
        // the corrected "Chasm" spelling, which is what the runtime model actually reports.
        //
        // GENERAL: Border="Rock", Default="Floor", Floor="Floor" -- Default and Floor are the SAME
        // terrain, the identical degenerate quirk the ttd01/ttf01/ttf02/jac01/fcx01 exterior profiles
        // documents, and pathnode data confirms it here too: Floor is overwhelmingly pathnode A
        // (134/181 pure tiles) while Rock's lone pure tile is pathnode R (restricted). Composing with
        // the engine's plain Solid=Default rule would carve unwalkable Rock "rooms" out of a walkable
        // Floor "wall" -- inverted for gameplay -- so this profile declares
        // SolidTerrainOverride("Rock") + PrimaryOpenTerrain("Floor"), the same inversion shape as that
        // whole family. Direct 16-combo probe confirms Rock/Floor reaches 16/16 in both orientations
        // (ProbeTool "matrix2"), and PathNodeOpeningWidthAudit against Solid=Rock/Open=Floor returns 1
        // (MinimumOpeningWidth stays the default).
        //
        // Water and Chasm are the tileset's two "hazard gap" terrains -- both reach a clean 16/16
        // against EITHER Rock or Floor (verified directly), but Water-vs-Chasm itself only reaches
        // 2/16 (they never blend against each other), so only one can be a wired accent slot at a time.
        // Water is the richer of the two -- a real naval roster (Ship - Longboat/Drow Boat, Docked;
        // Ship - Air, Above Water; Dock) plus "Door - Bridge, Water" (a real Bridge-crossered
        // CorridorInsert shape once AccentTerrain("Water") is declared) -- so Water is this profile's
        // wired AccentTerrain; Chasm stays unwired (its own "Door - Bridge, Chasm"/"Ship - Air, Above
        // Chasm" siblings are the identical shape on the other accent, the same "unwired sibling
        // district" treatment MinesAndCaverns gives tdm01's Pit/Lava, see PilotAlternateVocabTerrains
        // ["ttu01"]). Drow/Svirfneblin/Poor are minor per-building doorway-threshold terrains (one pure
        // tile each) that only ever appear on ten ungrouped, flat, door-bearing, CROSSER-FREE tiles
        // (TileResolver's door-slot admission gate requires a crosser to credit a door at all, so these
        // structurally can never resolve) -- also folded into PilotAlternateVocabTerrains["ttu01"].
        //
        // Crossers: Wall/Stream/Bridge/RuinWall/Slope -- none is a canonical or near-canonical "Corridor"
        // /"Doorway" pair (verified directly: TunnelVocabularyCheck.SupportsTunnels returns FALSE for
        // every ordered Solid/Open pairing against every crosser), so Tunnel-mode composition has NO
        // wall-embedded corridor vocabulary here -- Complex's Tunnel mode downgrades to OpenLane, the
        // same verdict as the ttd01/ttf01/ttf02/jac01/fcx01 profiles. RoadVocabularyCheck.SupportsRoads
        // confirms real lane support for Wall and Stream against Open=Floor; Wall is wired as
        // RoadCrosser (a plausible drow-built walkway/railing reading over open cavern floor). "RuinWall"
        // (the ruined-outpost gate family: Ruin - Gates/House 5/Entrance Straight 1&2/Entrance Corner)
        // and "Wall" on "Door - Wall" all gate an OPEN(Floor)-cornered 1x1 group with a perimeter
        // crosser edge -- LayoutGroupStamper's WallRoom/CorridorStubChain both require all-SOLID corners
        // for a body/port edge, and the mixed-shape doorway branch explicitly rejects any PERIMETER
        // doorway-like edge (a 1x1 group's edges are always perimeter) -- so none of these six groups
        // structurally classify under any current mechanism, matching MinesAndCaverns'/Tropical's own
        // "shape doesn't reach a shipped mechanism, stays an honest exemption" precedent (see
        // PilotExpectedExemptions). "Slope" (15 tile-edge occurrences, height-transition tiles only,
        // never inside a GROUP) is left undeclared as a RampCrosser -- LayoutElevationPainter's own
        // ramp-lane check hardcodes the literal crosser name "Ramp" (not the profile's configurable
        // RampCrosser), so it could never recognize "Slope" regardless; non-flat Slope tiles fall to the
        // automatic height exemption bucket (no manual justification needed). MaxElevationRegions(2)/
        // MaxReliefRegions(2) ARE declared (mirroring FrozenWastes' identical no-RampCrosser shape) --
        // both paint raised Floor rim edges via corner-height alone, which the census's own
        // ElevationBlob(10)/TerrainRelief(7)/PoolBank(3) hit counts already confirm resolve against this
        // tileset's real inventory.
        //
        // "Cave" (1x1 GROUP, non-flat [Floor 1,1,0,0], crosser-free, one door slot) is the identical
        // baked-mesh cave-mouth shape as tdm01's "[Cave] Cave Entrance" and ttf01's own Cave/SmallCave
        // family -- classifies via LayoutGroupStamper's door-tolerant ReliefPiece kind, and (like every
        // ReliefPiece precedent) only ever PLACES under Complex, the one layout style that requests
        // nonzero ElevationRegions/ReliefRegions at all (Halls/Organic leave those knobs at 0). Measured
        // (ProbeTool "placeundk", seedBase 95000, 150 seeds, Complex, retryCount 1): successes=150,
        // hits=146 (97.3%) -- in line with FrozenWastes' own identically-shaped "Cave" ReliefPiece rate.
        //
        // "Tower - Drow (3x3)"/"Illithid Grand Lair (3x3)"/"Observation Dome (3x3)" are the largest
        // footprint in this tileset (all pure-Floor OpenSetPieces) -- measured 0/150 on BOTH Halls and
        // Complex (ProbeTool "placeundk"), the same "needs a larger contiguous open interior than a 20x20
        // area's rooms ever produce at this size" documented ceiling CastleExteriorRuralLargeFootprintPieces_
        // StillDoNotPlace_DocumentedCeilings/TropicalSandWaterShipwreck_StillDoesNotPlace_DocumentedCeiling
        // already establish -- kept wired (they still classify structurally) with a dedicated ceiling
        // test proving 0/N rather than silently claiming real placement.
        //
        // Naval "Docked"/"Above Water" pieces (Ship - Longboat Docked, Ship - Drow Boat Docked, Dock
        // (1x2), Ship - Drow Boat (1x2), Ship - Air Above Water) carry pure-or-mixed WATER corners with
        // no crosser -- OpenSetPiece only matches Solid/Open corners, never a bare Accent terrain, so
        // none of these structurally classify (the identical gap MinesAndCaverns' own "[Cave] Ship -
        // Docked" already documents) and stay PilotExpectedExemptions. "Ship - Air, Docked" is the one
        // naval piece that DOES classify -- its three members are pure Floor (Open), not Water.
        //
        // Hand-built evidence: 3 real ttu01 areas ship in the module (pw_ar_sc_arkcave, pw_sc_dath_apexd,
        // pw_sc_dath_sden -- the Kashyyyk/Dathomir Underdark-adjacent cave content), 432 placed tiles
        // total (ProbeTool "evidence" command, reading Tile_List from the .are.json + Placeable List
        // from the sibling .git.json). TileLighting(0,0,0,0) is the real plurality (181/432 tiles,
        // 41.9%); the runner-up combos are hand-lit variety, not a second systematic default. Only
        // three GROUPS appear in this real placed content (Ramp - Up x3, Ramp - Down x2, Ruin - Gates
        // x1) -- sparse, but genuine. Decoration palette mined from the same three areas' Placeable
        // List: swd_florrd01/swd_floorm01/swd_florrt01/swd_florrt02/swd_florre01 (floor debris),
        // swd3_wall001/002/003 (wall growths), zep_shrub036/zep_mushroom/zep_mushroom002 (cavern
        // flora), zep_geiser002 (a steam/mineral vent), crystalspire (a large crystal formation).
        public const string Underdark = "underdark";

        // Early Winter 2 (trs02, basegame_sets/trs02.set -- BIF-only, NO SWLOR_Haks copy
        // exists, verified directly; 1306 tiles / 94 groups, 7 terrains, 4 crossers). UnlocalizedName
        // is "Early Winter 2" verbatim (no trailing asterisk, unlike the hak-customized exterior profiles
        // -- the .set file itself carries this UnlocalizedName, no TLK fallback needed).
        //
        // GENERAL: Border=Default=Floor="Grass" -- a genuine open field (matching ttr01/tts01/ttz01's
        // own shape, NOT the ttd01/ttf01-style inversion): Grass reaches a clean 16/16 against EVERY
        // other terrain (Water/Trees/Chasm/Grass2/Mountain, all verified directly via ProbeTool
        // "matrix2"). SolidTerrainOverride is left UNSET -- LayoutSolver.Solve stamps Solid=Grass
        // (==PrimaryOpenTerrain), identical to RuralGrass/RuralWinter/Tropical's own base profile.
        //
        // Unlike those three siblings, trs02 ALSO carries a second, genuinely rich non-Grass family:
        // Mountain (144 pure tiles, overwhelmingly pathnode-restricted -- L/H/N/I/T dominate, only 3
        // pure tiles are pathnode A) hosts by far the largest door/cave GROUP roster in this tileset
        // (MountainCave1-5, Mine1/2, CornerCave1-3, InnerCornerCave1-6, StreetCave1-3, SeaCave1,
        // WaterfallCave, MountainSlope, SmallCastle -- ~25 groups, mostly mountain-cornered or
        // mountain/grass mixed). Solid=Mountain/Open=Grass ALSO reaches a clean 16/16 (verified
        // directly) with MinimumOpeningWidth 1, so this tileset supports BOTH shapes simultaneously,
        // not one-or-the-other: the base profile below is the open field (Grass, no override), and
        // EarlyWinterMountain (see its own doc comment) is a second, INVERTED profile
        // (SolidTerrainOverride("mountain")) recomposing the SAME .set data to unlock that door/cave
        // family as real dungeon-style wall content -- a genuinely new shape among this project's
        // registered variants: not a PaletteVariant recomposition of an existing accent slot (like
        // Tropical's Sand) but a second FULL inversion sharing the tileset with an open-field sibling.
        //
        // Chasm (40 pure tiles: CliffBottomCave1/2, CliffTopCave1, CliffPath1/2, CliffCaveEntry,
        // ChasmPond, ChasmRoad1/2, ChasmRoadWB1-5) is wired as SecondaryOpenTerrain("Chasm") on the
        // base (Grass) profile -- Grass/Chasm mixed corners reach 16/16 (verified directly), giving
        // CliffCaveEntry/CliffPath2/CliffBottomCave1/CliffBottomCave2/CliffTopCave1 real OpenSetPiece/
        // ExitGroup census credit. Measured real placement (ProbeTool "placeew", 150 seeds, Halls):
        // ALL FIVE are 0/150. Root cause verified directly in RoomsAndCorridorsLayout.Generate:
        // SecondaryOpenTerrain districts only ever paint when useDistricts is true, which requires
        // CorridorMode.Tunnel -- and this composition has NO Tunnel vocabulary at all (verified via
        // TunnelVocabularyCheckTests' own trs02 entry, Complex downgrades to OpenLane unconditionally),
        // so Chasm is structurally reachable (matchesSecondary, real census credit) but can never
        // actually paint under any of this project's three supported layouts. Kept wired (matches this
        // project's "keep it wired, document the ceiling" convention, e.g.
        // TropicalSandWaterShipwreck_StillDoesNotPlace_DocumentedCeiling) with a dedicated 0/150 proof
        // rather than pulled or silently claimed as real content -- see
        // EarlyWinterChasmDistrictPieces_StillDoNotPlace_DocumentedCeiling. Grass2 (62 pure tiles: SmallCave1/2,
        // WallGate1R/2R, MountainCave1/4, CornerCave1, InnerCornerCave3, Pen2, Waterfall1NW/2NW) and
        // Water (20 pure tiles, all-naval Boat1/Small_Cog/Grass_boat_docked/Ship_floating_1/2/Bulge) and
        // Trees (1 pure tile, only ever mixed into two ungrouped door-bearing boundary tiles,
        // TILE155/TILE1112 -- the identical starved-minor-terrain shape ttr01/tts01's own "Trees" entry
        // documents) all stay UNWIRED this pass (time-boxed scope, like SecretBase's decoration
        // palette) -- see PilotAlternateVocabTerrains["trs02"].
        //
        // Crossers: Stream/Wall/Ridge/Street -- NONE is a canonical or near-canonical "Corridor"/
        // "Doorway" pair (verified directly, TunnelVocabularyCheck.SupportsTunnels false for every
        // ordered pairing), so Complex downgrades to OpenLane, the same verdict as every prior exterior
        // family. RoadVocabularyCheck.SupportsRoads confirms Street supports lanes against Grass/Chasm/
        // Grass2/Mountain (the broadest of the four) -- wired as RoadCrosser. Wall/Ridge/Stream all gate
        // real GROUP content (WallGate1/2 grass+wall+door, SmallCave1 grass2+ridge+door, Bridge1/2
        // grass+stream, RiverCave1/StreetCave mountain+stream/street+door, etc.) but none is declared a
        // DoorSlotCrosser this pass -- an open-cornered (Grass=Solid=Open) 1x1 group with a perimeter
        // crosser edge would classify as WallRoom if declared, but this project's own established
        // precedent (ttr01/tts01/ttz01's identical Wall1/Wall2/Stream/Road gate families, see those
        // profiles' PilotExpectedExemptions writeups) leaves this class of shape undeclared/exempt
        // rather than risk a door object never actually forming a real boundary; all four crossers are
        // folded into PilotAlternateVocabCrossers["trs02"] instead (plus "path", a fifth, rare crosser
        // name found on exactly one group member, CliffPath1's TILE582 -- not in the tileset's own
        // 4-crosser summary at all, verified directly against the raw .set data).
        public const string EarlyWinter = "earlywinter";

        // Early Winter 2 (Mountain) -- see EarlyWinter's own doc comment above for the shared shape
        // writeup. SolidTerrainOverride("mountain") + PrimaryOpenTerrain("grass") recomposes the SAME
        // trs02 .set data as a genuine inversion (mirroring the ttd01/ttf01/jac01/fcx01/ttu01 profiles'
        // shape, not a PaletteVariant accent-slot recomposition): Mountain becomes real wall mass, and
        // its door/cave family (MountainCave1-5, Mine1/2, CornerCave1, InnerCornerCave1/3, SeaCave1 --
        // all flat, crosser-free, door-bearing 1x1 groups mixing mountain with grass/grass2/water
        // corners) all classify via IsExitGroupEligible's vocab-independent structural rule (ExitGroup
        // needs only 1x1/flat/door/no-crosser -- terrain-agnostic) and are wired here as real GroupExits
        // rather than on the open-field base profile, matching this district's own mountain-fortress
        // identity.
        //
        // Real measured placement (ProbeTool "placeew", 150 seeds, Halls, retryCount 1) splits sharply
        // by corner composition: the pure Mountain+Grass pairs -- "MountainCave2" 97.3% (146/150),
        // "MountainCave3"/"Mine1"/"Mine2" 100% (150/150) -- place readily, since Grass/Mountain is this
        // profile's own real Solid/Open pair and a generated room boundary genuinely produces that
        // corner shape. The four that mix in a THIRD, unwired terrain (grass2 or water) all measure
        // 0/150: "MountainCave1" (mountain/grass2), "CornerCave1" (mountain/grass2 x3), "SeaCave1"
        // (mountain/water) -- their own grass2/water corner never appears anywhere in a grid painted
        // only Grass/Mountain, so the exact site their door needs to attach to can never occur, a
        // genuine geometric impossibility rather than bad luck. "InnerCornerCave1" (pure mountain/grass,
        // a 3-mountain-1-grass CONCAVE inner corner) also measures 0/150 despite using only wired
        // terrain -- BSP rectangle room carving (this profile's own room shape) never produces a concave
        // inner-corner boundary cell, the same "irregular-growth-only" shape TileCoverageCensusTests'
        // own IsElevationBlobReachable doc comment documents for the elevation painter. "MountainCave4"
        // (mountain/grass2, ALSO 0/150) combines both gaps. All four are kept wired (classify
        // structurally, real census credit) with a dedicated 0/150 ceiling proof rather than pulled --
        // see EarlyWinterMountainThirdTerrainPieces_StillDoNotPlace_DocumentedCeiling.
        //
        // The remaining mountain-cave family members (StreetCave1-3, MageTower, InnerCornerCave2/4/5/6,
        // CornerCave2/3, WaterfallCave, MountainSlope, RiverCave1/2, SmallCastle) are either non-flat
        // (auto height-exempt) or crosser-gated by an undeclared Wall/Ridge/Stream/Street/path edge (the
        // same PilotAlternateVocabCrossers["trs02"] bucket the base profile documents) -- no additional
        // wiring closes them this pass.
        public const string EarlyWinterMountain = "earlywinter_mountain";

        // Medieval Rural 2 (trm02, basegame_sets/trm02.set -- BIF-only, NO SWLOR_Haks copy
        // exists, verified directly; 1644 tiles / 161 groups, 7 terrains, 6 crossers). UnlocalizedName
        // is "Medieval Rural 2" verbatim (the .set file itself carries this UnlocalizedName, DisplayName
        // is the unset -1 sentinel, so no TLK fallback is needed).
        //
        // GENERAL: Border=Default=Floor="Grass" -- a genuine open field, the same shape as
        // ttr01/tts01/ttz01/trs02 (NOT the ttd01/ttf01-style inversion): Grass reaches a clean 16/16
        // against EVERY other terrain (Sand/Water/Trees/Chasm/Grass2/Mountain, all verified directly via
        // ProbeTool "matrixtrm"). SolidTerrainOverride is left UNSET -- LayoutSolver.Solve stamps
        // Solid=Grass (==PrimaryOpenTerrain).
        //
        // trm02 shares its ENTIRE 7-terrain/6-crosser vocabulary shape with trs02 (Early Winter 2) --
        // both are "Rural"-family Bioware exterior sets with the same Sand/Water/Trees/Grass/Chasm/
        // Grass2/Mountain palette, and trm02 even ships an identically-named "HillCave1" ReliefPiece and
        // "MountainCave1-4"/"Mine1-2"/"CornerCave1"/"InnerCornerCave1/3"/"SeaCave1" ExitGroup family --
        // but trm02 is a materially richer, more populated set (1644 tiles/161 groups vs. trs02's
        // 1306/94): a genuine medieval village roster (HobbitHome1-5, ElfHouse1-3, ElfForestTower,
        // Smithy2x2, Merchant2x2, Windmill, Farm2x1/2x2 x8, Barn01/01r/02, FarmShed, Grainary, Mill2x2,
        // WaterMillStr, SmallCastle, Castle3x5) absent from trs02 entirely. Solid=Mountain/Open=Grass
        // ALSO reaches a clean 16/16 (verified directly) with MinimumOpeningWidth 1, so -- exactly
        // mirroring EarlyWinter/EarlyWinterMountain's own precedent shape -- this tileset supports BOTH
        // an open-field composition (this profile) and a second, INVERTED Mountain profile
        // (MedievalRuralMountain, see its own doc comment) sharing the same .set data.
        //
        // Chasm (a genuine cliff/canyon district: CliffBottomCave1/2, CliffTopCave1, CliffCaveEntry,
        // CliffPath1/2, CliffRockFormation, ChasmPond, ChasmBridgeWB1-5, CliffBridge1/2, CliffWillow) is
        // wired as SecondaryOpenTerrain("Chasm") -- Grass/Chasm mixed corners reach 16/16 (verified
        // directly). Measured real placement (ProbeTool "sweeptrm"/"placetrm"): the SecondaryOpenTerrain
        // district pieces are structurally reachable (real census credit) but -- exactly like trs02's
        // own Chasm district -- RoomsAndCorridorsLayout.Generate only ever paints a SecondaryOpenTerrain
        // district under CorridorMode.Tunnel, and this composition has NO Tunnel vocabulary at all
        // (verified via ProbeTool "matrixtrm": TunnelVocabularyCheck.SupportsTunnels returns FALSE for
        // every ordered Solid/Open pairing against every one of this tileset's 6 crossers) -- so Complex
        // downgrades to OpenLane and the Chasm pieces can never actually paint under any of this
        // project's three supported layouts. Kept wired with a dedicated 0/150 ceiling proof rather than
        // pulled -- see MedievalRuralChasmDistrictPieces_StillDoNotPlace_DocumentedCeiling.
        //
        // Sand (SharkCave, CoastCave, Crystal, CoastPond, Merfolk_Building_D_1x2) and Water (Boat1/2,
        // Small_Cog, Ship_floating_1/2, Grass_boat_docked, DockedShip1x4/1x3_Grass, Lighthouse, Willow1)
        // both also reach 16/16 against Grass but stay UNWIRED as accent slots this pass (time-boxed
        // scope, matching trs02's own Grass2/Water/Trees writeup). "Lighthouse" (1x1, flat, crosser-free,
        // one door slot, Grass+Water corners) is ExitGroup-eligible regardless (IsExitGroupEligible is
        // vocab-independent/structural) and IS wired as a real GroupExit -- but measures a documented
        // 0/150 (ProbeTool "placetrm", Halls): the same "its own corner terrain is never painted
        // anywhere on the grid" ceiling as the Chasm-district pieces below, since Water is never wired as
        // Primary/Secondary/Accent, so a real Grass+Water boundary cell never exists for
        // GroupExitPlanner's site search to attach to. "SmallFarm1" (1x1, flat, crosser-free, one door
        // slot, pure Grass2) is the identical shape/ceiling on the OTHER unwired terrain -- also a real
        // wired GroupExit, also a documented 0/150, since Grass2 is likewise never painted anywhere on a
        // Grass-only grid. Both are covered by
        // MedievalRuralWaterAndOffVocabSinglePieces_StillDoNotPlace_DocumentedCeiling. Their multi-tile
        // naval/Grass2 siblings (Boat1/2/Small_Cog/etc., Farm2x1_2/4/7/Barn01r_1x2) and
        // SharkCave/CoastCave/Merfolk_Building_D_1x2 all fall to the automatic alternate-vocab exemption
        // instead (no door-only escape hatch for a 2+-tile group) -- see PilotAlternateVocabTerrains
        // ["trm02"].
        //
        // Trees (a single starved minor terrain: only ElfForestTower's pure-Trees 1x1 plus two
        // ungrouped Grass/Trees boundary tiles InvisBridge/RiverEndNW use it) is the same "Trees" shape
        // ttr01/tts01/trs02's own entries already document -- stays unwired.
        //
        // Crossers: Road/Stream/Wall/Bridge/Ridge/Street -- NONE is a canonical or near-canonical
        // "Corridor"/"Doorway" pair (verified directly via ProbeTool "matrixtrm": TunnelVocabularyCheck
        // returns FALSE for every ordered pairing), so Complex downgrades to OpenLane, the same verdict
        // as the previous exterior profiles. RoadVocabularyCheck.SupportsRoads confirms Street supports lanes
        // against Grass/Grass2/Mountain (the broadest of the six, matching trs02's own Street pick) --
        // wired as RoadCrosser. Road/Stream/Wall/Bridge/Ridge all gate real GROUP content (WallGate1/2
        // grass+wall+door, Mill2x2 grass+road/stream, SmallCave1/2 grass2+ridge+door, Bridge1-3/
        // CliffBridge1-2 grass/chasm+stream, ChasmBridgeWB1-5 chasm+bridge, WaterMillStr grass2+stream/
        // street, etc.) but none is declared a DoorSlotCrosser this pass -- matching this project's own
        // established ttr01/tts01/ttz01/trs02 precedent (leaving this class of shape undeclared/exempt
        // rather than risk a door object never actually forming a real boundary). All six are folded
        // into PilotAlternateVocabCrossers["trm02"] instead (plus "path", the same rare fifth crosser
        // name trs02's own CliffPath1 sibling carries here too).
        //
        // "HillCave1" (1x1 GROUP, non-flat [Grass 1,1,0,0], crosser-free, one door slot) is the
        // IDENTICAL shape to trs02's own "HillCave1" (both literally share the name) -- classifies via
        // LayoutGroupStamper's door-tolerant ReliefPiece kind, and only ever PLACES under Complex (the
        // one layout style that requests nonzero ReliefRegions). Measured (ProbeTool "placetrm",
        // seedBase 95000, 150 seeds, Complex, retryCount 1): successes=150, hits=116 (77.3%) -- the exact
        // same rate trs02's own HillCave1 measures (same tile geometry, same shape).
        //
        // Hand-built evidence: 11 real trm02 areas ship in the module (dan_colony/dan_colonyfarms/
        // dan_destroyfarm/dan_enclosemount/dan_fieldtrail/dan_hiddenmount/dan_iriazfarm/dan_lakencave/
        // dan_playerland2/dan_tribefields/dan_wildplain -- the Dantooine farmland/frontier content),
        // 2858 placed tiles total (ProbeTool "evidence"). TileLighting(0,0,0,0) is the overwhelming
        // plurality (2758/2858 tiles, 96.5%). Decoration palette mined from the same 11 areas' Placeable
        // List (top resrefs by usage, excluding the functional "unwalkable_1" blocker): _mdrn_pl_wdfence
        // (wooden fence, 449 uses), zep_flowers017 (215), zep_shrub041 (87), zep_bamboo002 (48),
        // zep_blssmtree001 (38), _mdrn_pl_windmil (36), zep_shrub036 (36), zep_bamboo001 (30),
        // zep_pinetr22 (26), swlor_0186/swlor_0212 ("[SWLOR] Wall, Naboo" column/wall dressing, 14/12).
        public const string MedievalRural = "medievalrural";

        // Medieval Rural 2 (Mountain) -- see MedievalRural's own doc comment above for the shared shape
        // writeup. SolidTerrainOverride("mountain") + PrimaryOpenTerrain("grass") recomposes the SAME
        // trm02 .set data as a genuine inversion (mirroring EarlyWinter/EarlyWinterMountain's own shape,
        // not a PaletteVariant accent-slot recomposition): Mountain becomes real wall mass, and its door/
        // cave family (MountainCave1-5, Mine1/2, CornerCave1-3, InnerCornerCave1-6, StreetCave1-3,
        // SeaCave1, RiverCave1/2, WaterfallCave, MountainSlope, MageTower, SmallCastle, Castle3x5 -- all
        // flat-or-height-exempt, crosser-free-or-street-gated, door-bearing 1x1/multi groups mixing
        // Mountain with Grass/Grass2/Water/Trees corners) classify predominantly via IsExitGroupEligible's
        // vocab-independent structural rule (ExitGroup needs only 1x1/flat/door/no-crosser --
        // terrain-agnostic) and are wired here as real GroupExits rather than on the open-field base
        // profile, matching this district's own mountain-fortress identity (the same EarlyWinterMountain
        // shape).
        //
        // Real measured placement (ProbeTool "placetrm", 150 seeds, Halls, retryCount 1) splits sharply
        // by corner composition, mirroring EarlyWinterMountain's own precedent exactly: the pure
        // Mountain+Grass pairs -- this profile's own real Solid/Open pair -- place readily: "MountainCave2"
        // 97.3% (146/150), "MountainCave3"/"Mine1"/"Mine2" 100% (150/150). "InnerCornerCave1" (pure
        // Mountain/Grass, a 3-Mountain-1-Grass CONCAVE inner corner) measures 0/150 despite using only
        // wired terrain -- BSP rectangle room carving (this profile's own room shape) never produces a
        // concave inner-corner boundary cell, the same "irregular-growth-only" shape
        // TileCoverageCensusTests' own IsElevationBlobReachable doc comment documents for the elevation
        // painter (and the exact same result EarlyWinterMountain's own InnerCornerCave1 measures). The
        // remaining four pieces mixing in a THIRD, unwired terrain (grass2 or water) also measure 0/150:
        // "MountainCave1" (mountain/grass2), "MountainCave4" (mountain/grass2 -- BOTH same-named groups --
        // trm02 legitimately ships two distinct groups sharing the literal name "MountainCave4", one
        // non-flat pure Mountain/Grass at TILE99 [height-exempt, never wired] and one flat Mountain/Grass2
        // at TILE119 [this ExitGroup, verified directly against the raw .set data]), "CornerCave1"
        // (mountain/grass2), "InnerCornerCave3" (mountain/grass2), "SeaCave1" (mountain/water) -- their
        // own grass2/water corner never appears anywhere in a grid painted only Grass/Mountain, a genuine
        // geometric impossibility rather than bad luck. All six 0/150 pieces are kept wired (classify
        // structurally, real census credit) -- see
        // MedievalRuralMountainThirdTerrainPieces_StillDoNotPlace_DocumentedCeiling.
        //
        // The remaining mountain-cave family members (StreetCave1-3, MageTower, InnerCornerCave2/4/5/6,
        // CornerCave2/3, WaterfallCave, MountainSlope, RiverCave1/2, SmallCastle, Castle3x5) are either
        // non-flat (auto height-exempt) or crosser-gated by an undeclared Street/Stream edge (the same
        // PilotAlternateVocabCrossers["trm02"] bucket the base profile documents) -- no additional wiring
        // closes them this pass.
        public const string MedievalRuralMountain = "medievalrural_mountain";
        // Beholder Interior* (tib01, SWLOR_Haks/sw_t_beholder -- hak-only, 868 tiles / 43
        // groups, 9 terrains, 11 crossers). See Beholder's own doc comment (below, next to the profile
        // itself) for the full composition writeup, the KNOWN CALIBRATION FINDING on Room-Big/Room-Pit/
        // Room-Pillar, and the ChultDoorway/ChultCorridor exemption.
        public const string Beholder = "beholder";
        public const string BeholderBlood = "beholder_blood";
        public const string BeholderMagic = "beholder_magic";
        public const string BeholderSewer = "beholder_sewer";
        public const string BeholderUrine = "beholder_urine";
        public const string BeholderWater = "beholder_water";

        // D20 Futuristic City SW (fcx01, SWLOR_Haks/sw_t_futcity -- a 239-tile hak-shipped
        // exterior tileset; a 2026-07-12 offline probe called this "lacking coverage" using only the
        // pre-SolidTerrainOverride toolbox -- re-derived from scratch below with the current one).
        // GENERAL Default=Floor="Cobble" (the same degenerate quirk as ttd01/ttf01/ttf02): only THREE
        // terrains exist (Cobble, Cobble2, holes) and none is a classic "wall" terrain -- pathnode data
        // confirms Cobble, Cobble2, AND holes all carry pathnode-A (fully walkable) uniform tiles, so
        // there is no terrain-level solid/open split at all by pathnode. "holes" is the tileset's
        // building-footprint FILLER terrain (every Tower/platform GROUP's non-perimeter members are
        // uniformly "holes"-cornered -- verified directly against every one of the 38 groups), the same
        // structural role Forest (Platform)'s "Pit" plays for its Platform district: not a literal wall,
        // but the terrain that pairs with the walkable street to make a mixed-terrain GROUP classify.
        // SolidTerrainOverride("holes") + PrimaryOpenTerrain("Cobble") is this tileset's build: every
        // pure-Cobble-cornered GROUP (the eight Towers, b_tower, b_tower02, d_house02, b_rampe,
        // b_escalier, b_trans, b_arbre/b_arbre2/b_herbe/b_fountain/b_water) becomes a plain OpenSetPiece/
        // FeatureTile under Open=Cobble regardless of the Solid choice; the mixed Cobble/holes platform
        // GROUPS (b_platform, d_platform2) depend on Solid=holes for classification, and so do the
        // doorless pure-holes 1x1 utility GROUPS (b_tower02/d_tower02, verified reachable directly
        // against the real TileCoverageCensusTests classifier).
        //
        // Two genuine, narrow gaps stay exempt (see TileCoverageCensusTests' fcx01 PilotExpectedExemptions):
        // "platform1" (2x2, uniformly holes-cornered like b_tower02/d_tower02, but ONE member carries a
        // door slot) does not classify -- verified directly: the doorless pure-Solid shape b_tower02/
        // d_tower02 use has a wired path, a door-bearing one does not. "b_wall_door"/"d_wall_door" (1x1,
        // pure-Open(Cobble/Cobble2)-cornered, a "murs" edge crosser on two opposite sides, one door) also
        // fails: GroupIndex != -1 excludes it from CornerEdgeResolver's DoorSlotCrossers("murs") credit
        // (that credit only ever reaches ungrouped tiles), and no GROUP-level mechanism recognizes an
        // Open-cornered piece carrying a non-canonical crosser plus a door. Both were dropped from this
        // profile's SetPiece(...) calls below (never classify, so wiring them would be dead
        // configuration) rather than kept as "structurally valid but unexercised" -- unlike, say,
        // Crypt's Fence doors, these do not structurally qualify for any shipped mechanism at all.
        // Crossers: three total, all non-canonical -- "pont" (French "bridge"), "Routes" ("roads"), and
        // "murs" ("walls"). None pairs as a Corridor/Doorway body+port set (verified directly: no crosser
        // named "Corridor" or "Doorway" exists at all), so Tunnel vocabulary is NONE under this
        // composition -- Complex's Tunnel mode downgrades to OpenLane, the same verdict as the ttd01/
        // ttf01/ttf02 profiles (locked in by TunnelVocabularyCheckTests.ExpectedUnsupported). "murs" is
        // declared via DoorSlotCrossers("murs") -- it carries real door slots on the wall/road-gate
        // GROUPS (b_wall_door/d_wall_door/b_road_door/d_road_door) and on ten flat, ungrouped,
        // murs-edged ordinary tiles (ry TILE223-232), the same "district's own body-renamed door
        // crosser" shape as Barrows' "door_corridor" precedent. "pont" (Bridge-equivalent, gates the
        // holes chasm at TILE5-7/96-98/119-124) has no wired body/port or DoorSlotCrossers vocabulary in
        // this profile -- see TileCoverageCensusTests' fcx01 PilotExpectedExemptions entries for the exact
        // accounting. "Routes" (flat road-marking lanes at TILE207-216, never door-bearing) is now wired
        // as a RoadCrosser (see LayoutRoadCarver/RoadVocabularyCheck): street lanes carved between
        // transition anchors and room centers, routed around LayoutGroupStamper's already-stamped
        // building footprints, closing the "disconnected road decals crossing empty plazas" tile-
        // composition gap real hand-built fcx01 areas (pw_ar_narpromena etc.) don't have.
        // Two parallel palettes exist, prefixed by GROUP name (not by terrain alone): "b_"-prefixed
        // GROUPS and the unprefixed Towers are Cobble-cornered (this base profile); "d_"-prefixed GROUPS
        // are Cobble2-cornered (FutCityPlaza below) -- EXCEPT Tower04/Tower06 (Cobble2, wired on the
        // Plaza variant despite the unprefixed name) and d_house02 (Cobble, wired here despite the "d_"
        // name) -- verified directly per-group, name prefix is a loose convention, not a rule.
        // Hand-built evidence: 9 real fcx01 areas ship in Star Wars LOR v2.mod (Smuggler's Moon/Nar
        // Shaddaa content -- pw_ar_narpromena/narscorpd/narcatwalk, ns_industrialsec/ns_comrcial_ka,
        // pw_ar_nsshipyard/velundr, randoncity_01/02), 3468 placed tiles total. TileLighting(0,0,0,0) is
        // the REAL sampled value -- 2629/3468 (76%) of all placed tiles across all 9 areas use exactly
        // (MainLight1,MainLight2,SrcLight1,SrcLight2)=(0,0,0,0), including 100% of pw_ar_narpromena's own
        // 144 tiles (the confirmed-fcx01 reference area); the two areas with
        // other combos (pw_ar_velundr, randoncity_02) are hand-lit exceptions, not the tileset default.
        // Real usage also corroborates 22 of the 38 groups wired below (d_platform2, Tower06, b_platform,
        // Tower05, Tower02, Tower04, d_trans, b_road_door, Tower00, d_eau, d_road_door, b_fountain,
        // Tower01, d_tower02, d_monum, d_tower, d_rampe, b_tower, b_arbre/b_arbre2/b_herbe, d_wall_door)
        // -- the remaining groups are wired as structurally-valid-but-unproven content, matching this
        // file's own "optional config is exactly as reachable as wired" convention elsewhere.
        // ExitGroup: exactly four 1x1, flat, crosser-free GROUPS carry a genuine door (verified directly
        // against the real per-tile [TILEnDOORm] sections, NOT the .set "Doors=" summary field, which is
        // garbage on several of this tileset's tiles, e.g. b_tower/d_herbe/b_escalier/d_escalier all
        // declare a nonzero Doors= count but have zero real door subsections) -- "Tower01"/"d_house02"
        // (Cobble, wired here) and "d_tower"/"d_house01" (Cobble2, wired on FutCityPlaza).
        public const string FutCity = "futcity";
        public const string FutCityPlaza = "futcity_plaza";

        // Four hak tilesets probed via a temporary NUnit harness
        // (deleted after this audit -- its output is reproduced
        // in each profile's own doc comment below) rather than the interactive toolset. All four are
        // Interior=true, share the ordinary Default=Wall/Floor=<primary> GENERAL split (no
        // SolidTerrainOverride inversion needed, unlike the ttd01/ttf01/fcx01 exterior profiles), and all
        // four's PathNodeOpeningWidthAudit (run fresh against Solid=Wall/Open=<primary>) returns 1, so
        // MinimumOpeningWidth stays the machinery default.
        //
        // D20 Secret Base (tjsb0, SWLOR_Haks/sw_t_secretbs, 174 tiles): Wall/Floor/lava, three terrains,
        // crossers bridge/corridor/fence/doorway -- ALL FOUR canonical or near-canonical (case-
        // insensitive "corridor"/"doorway" match the shared Tunnel body/port names exactly). Verified
        // directly: TunnelVocabularyCheck.SupportsTunnels(tjsb0, "Floor", "Wall", CorridorCrosserType.
        // Corridor) returns TRUE -- full body/port shape inventory (straight/turn/T/X, with-port
        // variants, double-port variants) resolves with no Custom renaming needed, so this profile keeps
        // real wall-embedded Tunnel-mode corridors rather than downgrading to OpenLane, unlike every
        // profile in the earlier exterior set. "bridge" (gates the lava chasm, e.g. TILE112/TILE15) and "fence"
        // (TILE60/154 gate props) both stay undeclared: every door-bearing tile using them is already
        // GROUPed (BridgeDoor01/FenceDoor01/FenceDoor02) except TILE15, which pairs "doorway" (canonical,
        // already recognized) with "bridge" on separate edges -- Bridge is a first-class canonical
        // crosser in its own right (see BridgeChannelTests), so no DoorSlotCrossers declaration is
        // needed for it. SetPieceRoomCornerFloor(6): every multi-tile group here tops out at 2x2
        // (StairsDown_2x2/StairsUp_2x2/Platform01-03_2x2), the same footprint as FutCity's Tower00,
        // which needed corner size 6 to ever stamp (see FutCity's own doc comment for the margin-ring +
        // spare-relocation-tile derivation -- a geometric rule, not fcx01-specific). ExitGroup: exactly
        // two 1x1, crosser-free, single-door groups exist -- Exit01 (TILE69) and Exit02 (TILE70), both
        // wall/wall/floor/floor diagonal-split corners with zero crosser edges, the same shape FutCity's
        // own ExitGroup candidates used. BigDoor01/02 (corridor-crossered, doored) and BridgeDoor01/
        // FenceDoor01/02 (bridge/fence-crossered, doored) are wired as ordinary SetPieces (gate props),
        // not ExitGroups, since they carry real crosser edges. Hand-built evidence: 8 real tjsb0 areas
        // ship in the module (Module/are/{v_repubbase_hang,v_repubbase_cd,v_repubbase_2,v_repubbase_1,
        // sol_mandaloriani,r_prax_centralsp,manda_facility,city_hall}.are.json -- the Mandalorian/
        // Republic base content), 992 placed tiles total. TileLighting(0,0,0,0) is the measured plurality
        // (350/992 tiles, 34.2%, including 100% of city_hall's 36 tiles and 119/121 of v_repubbase_hang's)
        // -- several areas (v_repubbase_1/2, sol_mandaloriani, manda_facility) are hand-lit with varied
        // SrcLight(3,3)-family accents, not the tileset default. Decoration palette: NOT mined this pass
        // (not included in the verified vocabulary) -- generated tjsb0 content stays on accent-only/no
        // tileset-keyed palette until a follow-up pass mines the 8 real areas' placeable inventories.
        public const string SecretBase = "secretbase";

        // D20 Modern Facility (tbx78, SWLOR_Haks/sw_t_facility, 84 tiles -- the smallest of the four):
        // Wall/facility, two terrains only. Crossers: corridor, doorway1/2/3, cell, raised -- NONE of
        // the three doorway variants is the literal canonical string "Doorway", so every door-bearing
        // tile (52 of 84) needs DoorSlotCrossers to be recognized at all, unlike tjsb0's case-insensitive
        // match. Verified directly: TunnelVocabularyCheck.SupportsTunnels tried against every
        // body=corridor/port={doorway1,doorway2,doorway3} Custom pairing returns FALSE for all three (the
        // T-with-port/X-with-port shapes never resolve) -- Tunnel mode downgrades to OpenLane, the same
        // verdict as the ttd01/ttf01/fcx01 profiles. DoorSlotCrossers("doorway1","doorway2","doorway3",
        // "cell","raised") is declared so CornerEdgeResolver/LayoutGroupStamper recognize all five
        // non-canonical door-implying crossers (cell gates the facility's holding-cell tiles TILE36/38/
        // 40; raised gates TILE48/50's ramp doors). SetPieceRoomCornerFloor(7): the largest group is
        // room3x1 (3x1, max dimension 3), matching FutCity's 3x3+/4x3 rule (corner size 7, the
        // machinery's own vanilla ceiling). Group-name quirk: three separate GROUP entries are all
        // literally named "room2x1" (footprints 1x2, 2x1, 2x1) -- wired once via SetPiece("room2x1"),
        // matched by name against all three real .set entries. ExitGroup("door_transition"): a 1x1
        // group, the same "*_transition"/"*Trans"/"Door_Trans" naming convention these tilesets share
        // for their literal area-boundary marker group. Hand-built evidence: 8 real
        // tbx78 areas ship in the module (space_derelict_k, pw_ar_undrnasha, pw_ar_nscrafting,
        // pref_facility, pref_facilidark, nashadaa_czlabin, nanostation015, dan_crafterbase), 2475 placed
        // tiles. TileLighting(0,0,0,0) is the measured plurality (899/2475, 36.3%). Decoration palette:
        // NOT mined this pass, same as SecretBase above.
        public const string ModernFacility = "modernfacility";

        // Complex laps storage (tqq01, SWLOR_Haks/sw_t_labstore, 305 tiles -- display name kept VERBATIM
        // per the toolset's own UnlocalizedName typo). Wall + four parallel room-type districts
        // (Livingroom/Kitchen/Inn/Shop, ~65-70 corner uses each), crossers Corridor/Doorway -- BOTH
        // exactly canonical (case differs only in capitalization). Verified directly:
        // TunnelVocabularyCheck.SupportsTunnels(tqq01, open, "Wall", CorridorCrosserType.Corridor)
        // returns TRUE against EVERY district's own open terrain (Livingroom/Kitchen/Inn/Shop all pass
        // independently, not just the declared Floor="Inn") -- real wall-embedded Tunnel-mode corridors,
        // like SecretBase. This profile wires the base "Inn" district (the .set's own declared Floor
        // terrain) plus its generic (non-district) groups: StairsUp/StairsDown, CorridorExitBig/
        // CorridorExit, DoorTrans, Portal, Chessboard, and the freestanding building/decor set-pieces
        // that aren't corner-locked to one district's terrain (Tent, Baracks, the three Temple variants,
        // Wizards Den, Smithy, Barn, Bordello, SlumHome01/02, HomeLower/Upper 2x2 family). The
        // Livingroom/Kitchen/Shop districts and their own Room/Room01_1x2/Room02_1x2/DoorX01/
        // CornerStairs/CornerExit families are NOT registered as separate PaletteVariant profiles this
        // pass (time-boxed) -- PilotAlternateVocabTerrains auto-exempts them the same way BaseGame
        // TilesetProfiles.CityInterior2 (tni01) already exempts its own "livingroom"/"kitchen"/"shop"
        // terrains, a direct, already-shipped precedent for exactly this multi-district shape.
        // SetPieceRoomCornerFloor(7): the largest groups are Temple Good (4x4) and Temple Evil (4x3),
        // matching FutCity's corner-size-7 rule. ExitGroup("DoorTrans"): the 1x1, crosser-free,
        // door-transition-named group, same convention as SecretBase/Facility above. Hand-built
        // evidence: 9 real tqq01 areas ship in the module (veles_cantina, tochee_cantina,
        // tosche_cantina_s, tat_anc_gocorpst, player_rnd, dan_warehouse, dan_medinterior, cantina), 182
        // placed tiles total (a much smaller sample than the other three). TileLighting(0,0,3,3) is the
        // measured plurality (34/182, 18.7%, narrowly ahead of (0,0,2,2) at 15.4%) -- unlike the other
        // three tilesets, tqq01's real usage favors a lit SrcLight(3,3) ambient over the bare (0,0,0,0)
        // default (only 9.3% of sampled tiles), consistent with its Inn/Livingroom/Kitchen/Shop
        // furnished-interior content. Decoration palette: NOT mined this pass, same as above.
        // District registration (2026-07-16, census-vs-practice reconciliation): the "descope" above was
        // a STRUCTURAL non-issue, not a real gap -- TileCoverageCensusTests already read 305/305 (100%)
        // for tqq01 with ZERO alternate-vocab exemptions actually triggered, because every Livingroom/
        // Kitchen/Shop-cornered group already classifies via a terrain-independent mechanism (WallAlcove
        // door-corner shapes, or CornerEdgeResolver's static "does a matching candidate exist in the raw
        // .set inventory" check) that never needed PrimaryOpenTerrain in the first place. But that
        // static reachability is NOT the same as LIVE placement: LayoutGroupStamper.Stamp only iterates
        // parameters.SetPieces.Keys for the profile actually in use, and no profile here ever registered
        // ANY Livingroom/Kitchen/Shop group as a SetPiece -- so real generation never placed a single one
        // of these 27 groups, regardless of the census's 100% structural reading. Three PaletteVariant
        // profiles (LabStorageLivingroom/LabStorageKitchen/LabStorageShop) now register each district's
        // own-named groups the same way OfficeInteriorsService/Tiled/etc. do for udp2's districts --
        // PilotAlternateVocabTerrains["tqq01"] is now empty (no terrain needs auto-tagging once every
        // district composes somewhere). See each variant's own doc comment for its group inventory and
        // placement-rate proof (OpenSetPiecePlacementRateTests).
        public const string LabStorage = "labstorage";
        public const string LabStorageLivingroom = "labstorage_livingroom";
        public const string LabStorageKitchen = "labstorage_kitchen";
        public const string LabStorageShop = "labstorage_shop";

        // D20 Office Interiors UDP (udp2, SWLOR_Haks/sw_t_office, 229 tiles, 93 groups -- the largest and
        // most heavily districted of the four). KNOWN QUIRK: the raw
        // .set Doors= summary field is corrupt on several tiles (garbage counts up to ~1.8 billion);
        // TilesetSetParser.MaxDoorsPerTile already clamps this, and every door decision below is read
        // from real [TILEnDOORm] subsections, never the summary field. Wall + SEVEN parallel room-type
        // districts (Service/Tiled/Office_Vinyl/Office_Wood/Office_Alum/Foyer_L/Foyer_U). Crossers: Door,
        // Hallway1, Hallway2, Door_Garage_Sm, Door_Garage_Lg -- NONE is the literal canonical "Doorway"
        // string, so DoorSlotCrossers("Door","Door_Garage_Sm","Door_Garage_Lg") is REQUIRED for any
        // door-bearing content (all 134 door-bearing tiles/groups use "Door", not "Doorway") to be
        // recognized at all -- without it every one of udp2's real room-entry groups would be
        // structurally invisible to CornerEdgeResolver/LayoutGroupStamper. Verified directly:
        // TunnelVocabularyCheck.SupportsTunnels tried against every body={Hallway1,Hallway2}/
        // port={Door,Door_Garage_Sm,Door_Garage_Lg} Custom pairing returns FALSE for all six -- Hallway1/
        // Hallway2 are district-junction wall crossers, not a carveable corridor body vocabulary; Tunnel
        // mode downgrades to OpenLane, the same verdict as Facility above. This profile wires only the
        // primary Office_Vinyl district (the .set's own declared Floor terrain) -- Entry/Win/WinCrnr/
        // Firepl/SmRm1/SmRm2/MidRm1/MidRm2/Stair_UD/U/D/Stair2_UD/U/D (14 groups) -- plus the
        // tileset-generic, non-district-locked groups (Hallway1_Entry, Hallway2_Entry, Elevator1/2,
        // Stairwell_U/UD/D, Restrooms, Break_Room). The other six districts (Service/Tiled/Office_Wood/
        // Office_Alum/Foyer_L/Foyer_U) are NOT registered as separate PaletteVariant profiles this pass
        // (time-boxed, the same descope as LabStorage's Livingroom/Kitchen/Shop) -- PilotAlternateVocab
        // Terrains auto-exempts them. No group here is literally named as a transition/exit marker (no
        // "*Trans"/"*Exit" group exists in this .set, unlike the other three tilesets in this set), so
        // ExitGroup is left unwired pending a follow-up probe of udp2's un-dumped door tiles (only the
        // first 30 of 134 were inspected this pass). SetPieceRoomCornerFloor(6): every wired group here
        // tops out at 2x1 (max dimension 2), matching FutCity's Tower00 2x2 rule. Hand-built evidence: 17
        // real udp2 areas ship in the module (velesinterior, veles_sheriff, veles_genstore,
        // veles_holonews, v_repubbase_off, v_repubbase_jnrm, roch_govbuild, pw_ar_nsczgnstr,
        // pw_ar_nsficlub, pw_ar_ns_doffice, pw_ar_ns_medical, pw_ar_nscasino, pw_ar_nars_canhd,
        // pw_ar_gentemp, foszestate, dan_repinside, dan_jedienlibry), 1460 placed tiles. TileLighting
        // (0,0,0,0) is the measured plurality (323/1460, 22.1%). Decoration palette: NOT mined this pass,
        // same as above.
        public const string OfficeInteriors = "officeinteriors";

        // D20 Office Interiors UDP (udp2) district-closure pass: the six parallel room-type districts
        // the profile comment above excludes (Service/Tiled/Office_Wood/Office_Alum/Foyer_L/Foyer_U)
        // are PaletteVariant profiles recomposing the SAME udp2 hak data the base OfficeInteriors
        // profile above uses -- the identical CastleInteriorStorage/Rich/Library/Jail and
        // CepCityInteriorElven/Sigil "declare PrimaryOpenTerrain(<district>), the ordinary resolver does
        // the rest" pattern. Verified directly against the raw .set data: Service/Tiled/Office_Wood/
        // Office_Alum each mirror Office_Vinyl's own 14-group family tile-for-tile (same corner/edge
        // shapes, same tile count, only the district terrain name and model resrefs differ) -- Win/
        // WinCrnr/Firepl/Stair_UD/U/D/Stair2_UD/U/D (9 groups, all-solid-or-open corners, several with a
        // Doors=1 slot but NO crosser edge, the identical shape as the already-wired Office_Vinyl_Stair_U
        // etc.) classify and place exactly like their Office_Vinyl counterparts. SmRm1/SmRm2/MidRm1 2x1/
        // MidRm2 2x1 (4 groups, single-tile-per-member, all-Wall-cornered with a "Door" crosser edge) now
        // structurally CLASSIFY as WallRoom the same way Office_Vinyl's own door-bearing groups do (see
        // this file's Office_Vinyl doc comment above), but stay UNWIRED here for the identical reason:
        // udp2 has no ungrouped boundary tile shape pairing solid/open/open/solid corners with a literal
        // "Doorway" port edge (SupportsWallRoomOpenLaneBoundary always probes the canonical "Doorway"
        // string, never a DoorSlotCrossers alternate -- verified directly reading LayoutGroupStamper --
        // so this is a deterministic, tileset-wide structural fact, not a per-district empirical
        // measurement; udp2's own "Door" crosser can never satisfy it regardless of which district
        // supplies the open terrain). Each district's own "Entry 2x1" pair is now CLOSED and wired: it
        // pairs an all-Wall member with an open (district-terrain) member whose sole "Door" edge faces
        // its own group-mate -- interior, never perimeter (verified directly against every district's
        // raw .set data) -- so LayoutGroupStamper.TryClassify's mixed/open-member fallthrough (added
        // this pass; see that method's own doc comment) lets it fall through to the OpenSetPiece
        // corner-match branch instead of being rejected by the hasAnyDoorway/allCornersSolid gate.
        // TryPlaceOpenSetPiece's site search needs only an open-terrain room tile, not a corridor/
        // OpenLane boundary, so it is NOT subject to the SupportsWallRoomOpenLaneBoundary gap above --
        // measured 96.7%-100% isolated placement (see OpenSetPiecePlacementRateTests.
        // OfficeVinylEntryOnOfficeInteriors_NowPlacesInIsolation). Hallway1_Entry/Hallway2_Entry stay
        // exempt for a genuinely DIFFERENT, still-open reason: their door-family edge is the literal
        // crosser name "Hallway1"/"Hallway2", which IsAllowedMemberEdge rejects outright (not declared
        // as a DoorSlotCrosser) before allCornersSolid/hasAnyDoorway are ever consulted -- see
        // TileCoverageCensusTests' udp2 PilotAlternateVocabCrossers entry. Foyer_L/Foyer_U are smaller
        // districts (7 groups each: Entry 2x1/Win/WinCrnr/Firepl/Stair_U or Stair_D/Stair2_U or
        // Stair2_D/Grandstair_U or Grandstair_D) -- same shape family, Entry 2x1 wired the same way.
        //
        // Each variant redeclares SetPieceRoomCornerFloor(6) and DoorSlotCrossers("Door",
        // "Door_Garage_Sm", "Door_Garage_Lg") identically to the base profile: a variant may be selected
        // as a composition's own Tileset profile directly (not merely unioned in for the census), so it
        // needs the same room-size floor and door-slot vocabulary the base profile relies on for correct
        // real generation, not just census credit.
        //
        // Census: 193/229 (84.3%) -> 211/229 (92.1%) -> 225/229 (98.3%) -- the residual 4 tiles
        // (Hallway1_Entry/Hallway2_Entry) are the DoorSlotCrossers-vocabulary gap above, not a
        // per-district gap. See TileCoverageCensusTests' udp2 PilotAlternateVocabTerrains entry (now
        // empty, the six district names removed the same way CastleInteriorStorage/Rich/Library/Jail
        // emptied tic01's own entry) and PilotAlternateVocabCrossers entry (now just Hallway1/Hallway2).
        public const string OfficeInteriorsService = "officeinteriors_service";
        public const string OfficeInteriorsTiled = "officeinteriors_tiled";
        public const string OfficeInteriorsOfficeWood = "officeinteriors_office_wood";
        public const string OfficeInteriorsOfficeAlum = "officeinteriors_office_alum";
        public const string OfficeInteriorsFoyerL = "officeinteriors_foyer_l";
        public const string OfficeInteriorsFoyerU = "officeinteriors_foyer_u";

        // [CEP] Dungeon (zde01, SWLOR_Haks/sw_t_cepdungeon).
        // zde01.set is BYTE-IDENTICAL to the already-registered SWLOR hak copy of tde01 (SWLOR_Haks/
        // sw_t_dungeon/tde01.set) except for the [GENERAL] Name/UnlocalizedName header fields (verified
        // directly: `diff` between the two .set files returns only those two lines) -- 1092 tiles, 60
        // groups, identical Wall/Floor/Lava/Water/Sewer/Ice/Pit terrain family, identical Bridge/
        // Corridor/Fence/Doorway/Ramp/MazeMosaic crossers, identical group names (Treasure 1/2, Door -
        // Big 1/2, Exit 1/2-<Accent>, Stairs - Up/Down, Platform 1-5, Pillar family, Wall Section 1/2,
        // Energy Source, Door - Fence 1/2, Door - Transition, Ramp - Straight/Corner, Door - Bridge 1
        // <Accent>, Door - Maze <End 1/End 2/Side> Mosaic), identical HasHeightTransition=1 raised-tile
        // system (323/1092 non-flat). So this profile family is a straight re-composition of the SAME
        // proven Dungeon/DungeonWater/DungeonSewer/DungeonIce/DungeonPit wiring against Tileset("zde01")
        // instead of "tde01" -- same SolidTerrainOverride-free plain Wall/Floor split (Wall is
        // GENERAL Default/Border, so no override needed), same MaxElevationRegions/MaxPoolRegions/
        // MaxReliefRegions(2) trio (the height-relief probes are per-tile-shape, and every shape here is
        // identical to tde01's), same single AccentTerrain slot per profile (Lava on the base profile,
        // Water/Sewer/Ice/Pit on the four PaletteVariants), same MazeMosaic-crosser alternate-vocabulary
        // gap (see TileCoverageCensusTests.PilotAlternateVocabCrossers["zde01"]).
        // TileLighting: UNLIKE tde01 (zero hand-built areas exist, so tde01's own (0,0,8,8) is an
        // uncalibrated placeholder per this file's initial-profile comment above), zde01 ships with
        // real hand-built content: Module/are/dath_mountcaves.are.json (126 tiles) and
        // Module/are/valkorrdung1c.are.json (256 tiles), 382 placed tiles total. The measured plurality
        // across both is (MainLight1,MainLight2,SrcLight1,SrcLight2)=(0,0,0,0) at 31.2% (119/382) --
        // ahead of (0,0,2,2)/(0,11,0,0) at 10.7% each -- so this profile uses that real sampled default
        // instead of copying tde01's placeholder value.
        // Display names: UnlocalizedName verbatim ("[CEP] Dungeon"), variants cascade
        // "[CEP] Dungeon (<Qualifier>)" -- distinct from the initial "Dungeon*" asterisk convention above.
        public const string CepDungeon = "cep_dungeon";
        public const string CepDungeonWater = "cep_dungeon_water";
        public const string CepDungeonSewer = "cep_dungeon_sewer";
        public const string CepDungeonIce = "cep_dungeon_ice";
        public const string CepDungeonPit = "cep_dungeon_pit";

        // [CEP] City Interior 1 (zin01, SWLOR_Haks/
        // sw_t_cepcityin). NOT byte-identical to tin01 (428KB vs 139KB) -- a genuinely larger,
        // independently-authored 961-tile/148-group superset that follows the SAME design convention
        // as tin01 (Livingroom/Kitchen/Inn/Shop/Home furnished-room families, WallAlcove door-corner
        // groups, WallRoom Doorway-port pairs, all-Wall multi-door crosser-free furnished rooms) under
        // a "[City] "-prefixed namespace, PLUS three whole new districts tin01 never had: [Elven]
        // (ElvenFloor/ElvenPlatform/ElvenGrass, 25/9/3 groups, its own ElvenHallway crosser),
        // [Sigil] (SigilFloor, 14 groups, its own SigilHallway crosser + one quirk tile where
        // "SigilFloor" is ALSO used as a crosser name), and [Workshop] (Workshop, 9 groups, exit/
        // stairs-corner pieces only, no furnished room family or its own crosser).
        // GENERAL Default/Border=Wall, Floor=Inn (the .set's own declared default) -- Inn itself is a
        // minimal district (only 3 real-Inn-cornered groups: Portal/Chessboard/Door), so this profile
        // leaves PrimaryOpenTerrain at its Inn default (matching tin01's own "leave it at the declared
        // Floor" convention) and wires the full City+Home+Workshop-exit door/furnished-room family --
        // the same mechanisms (WallAlcove door-corner groups, WallRoom Doorway-port pairs, all-Wall
        // multi-door furnished rooms) already proven by tin01/tic01/tni01/tni02/twc03 against these
        // exact group shapes, just under zin01's own confirmed group-name spellings (verified directly
        // against the real .set data, not assumed from tin01's naming). Elven/Sigil are wired as their
        // own PaletteVariant profiles (CepCityInteriorElven/CepCityInteriorSigil) declaring
        // PrimaryOpenTerrain(<district>) the same way CastleInteriorStorage/Rich/Library/Jail
        // recompose tic01 -- ElvenFloor is the tileset's single richest secondary district (25 groups,
        // ahead of SigilFloor's 14), so it gets the primary variant slot; Sigil gets a second variant.
        // Workshop (9 groups, no furnished-room family at all, only WallAlcove exit/stairs-corner
        // pieces) is wired directly on the base profile instead of its own variant -- its groups need
        // no PrimaryOpenTerrain override to classify (WallAlcove door-corner groups don't require the
        // group's own open corner to equal the profile's declared primary terrain, mirroring tin01's
        // own Livingroom/Kitchen/Shop door groups composing fine under a Inn-primary profile).
        // TileLighting: real sampled evidence from 3 hand-built zin01 areas already in the module
        // (Module/are/jeditemp_int.are.json 252 tiles, spending_area.are.json 9 tiles,
        // tat_anc_junix.are.json 64 tiles -- 325 placed tiles total). Measured plurality
        // (MainLight1,MainLight2,SrcLight1,SrcLight2)=(0,0,0,0) at 20.9% (68/325).
        // Display name: UnlocalizedName verbatim ("[CEP] City Interior 1"), variants cascade
        // "[CEP] City Interior 1 (<Qualifier>)".
        // Tail closure (2026-07-16): "Window" is declared as a DoorSlotCrosser on the base profile --
        // probed directly against the raw .set data, it is a genuine CROSSER TYPE (CROSSER1, same
        // section as Corridor/Doorway), not a terrain name, used prolifically (150+ ordinary tiles,
        // already CornerEdgeResolver-reachable) plus on six all-Wall WallRoom-shaped GROUPs whose Window
        // edge sits on the group's own perimeter (a "window on the far wall" pattern, several paired with
        // a real Doorway-ported entrance on the SAME group) -- see
        // OpenSetPiecePlacementRateTests.WindowCrosseredGroupsOnCepCityInterior_NowPlaceInIsolation for
        // the placement proof (all six clear 28-49% isolated). Two Window-crossered groups ("Window -
        // Porthole 1/2") still correctly stay exempt: they mix Window with the Corridor body crosser on
        // the SAME tile, the identical hasAnyBodyCrosser-vs-hasAnyDoorway shape LayoutGroupStamper.
        // TryClassify already rejects everywhere else (no verified data mixes a body crosser with a
        // doorway-family edge). "Window - Home" also stays exempt (mixed Wall/Home corners, not
        // all-solid, with its sole Window edge on the group's true 1x1 perimeter -- the same geometric
        // ceiling as "[Sigil] Corridor - Entry" below: a single-tile group can never supply the interior
        // seam the mixed/open-member tolerance requires). Verified no ordinary (non-grouped) zin01 tile
        // relies on Window being EXCLUDED from the door-slot gate: every ordinary tile pairing a Window
        // edge with a real door slot also carries a literal Doorway edge on the same tile (already
        // admitted before this declaration). Census: 939/961 (97.7%) -> 952/961 (99.1%).
        public const string CepCityInterior = "cep_cityinterior";
        public const string CepCityInteriorElven = "cep_cityinterior_elven";
        public const string CepCityInteriorSigil = "cep_cityinterior_sigil";

        // Sea Ships (tss13, basegame_sets/tss13.set -- BIF-only, verified: no SWLOR_Haks/tss13
        // copy exists anywhere in the module). GENERAL Border=Default=Floor=Castle, HasHeightTransition=0,
        // 404 tiles / 132 groups, ONE declared crosser ("gangplank"). Verified directly against the raw
        // .set data: [TERRAIN TYPES] lists exactly four terrains -- Castle, City, Rural, Tropical -- and
        // every one of the 132 GROUPs is uniformly single-terrain (all four of a group's member tiles'
        // corners are the SAME terrain name; a direct scan of every GROUP's own corner list found zero
        // mixed-terrain members anywhere), i.e. this tileset is FOUR pure single-terrain recolors of the
        // identical ship/dock geometry stamped 33 groups/97 tiles apiece (Castle: TILE4-100, City:
        // TILE101-197, Rural: TILE198-294, Tropical: TILE295-391) plus 4 plain, non-grouped open-water
        // tiles per terrain (TILE0-3 and its three siblings) -- no wall/floor blend combination exists at
        // all (the direct cross-terrain 16-combo probe the verdict pass ran found only 2/16, the
        // degenerate all-same-corner combos, confirming "no wall concept" the same way every other
        // open-field base-game exterior in this file already documents). Composed here as the SAME
        // "SolidTerrainOverride(t) == PrimaryOpenTerrain(t)" open-field shape TropicalSand pioneered
        // (this file's own Tropical const doc comment above has the full mechanical writeup) -- ONE base
        // profile (Castle, the tileset's own declared GENERAL Default/Floor terrain, so no override is
        // even needed to reach it) plus THREE PaletteVariant profiles (City/Rural/Tropical, each an
        // explicit SolidTerrainOverride/PrimaryOpenTerrain pair onto its own terrain -- Tropical here is a
        // per-terrain PALETTE within tss13's own four-terrain recolor, unrelated to and NOT to be confused
        // with this file's separate ttz01 "Tropical" profile family above).
        //
        // Every one of the 132 group NAMES repeats identically across all four terrain blocks (Boat 1..8,
        // Lifeboat 1..3, plus the gangplank-bearing families below) -- verified directly (each name
        // appears exactly 4 times total in the .set GROUPS list, once per terrain, all structurally
        // identical Rows/Columns/member layouts, just recolored). LayoutGroupStamper.FindGroup/
        // GroupExitPlanner/TileResolver's feature lookup are all first-match-by-name (see this file's own
        // CastleExteriorRural const doc comment for the established precedent), and the Castle terrain's
        // copies are FIRST in file order, so EVERY duplicated name here always resolves to the CASTLE
        // physical instance regardless of which profile declares it -- the identical "real, documented
        // engine ceiling, not a wiring gap" CastleExteriorRural's grass/dirt duplicate-name pair already
        // establishes, just a 4-way instead of 2-way collision. Consequently ONLY the base (Castle)
        // profile below wires any SetPieces by name; the City/Rural/Tropical variants deliberately
        // declare none (would be dead weight -- FindGroup would still hand them the Castle copy, whose
        // corners never corner-match their own composition). Every terrain's own physical copies remain
        // structurally classify-eligible regardless (the tile-coverage census credits a group if ANY
        // profile sharing this TilesetResref classifies it, independent of FindGroup), so registering the
        // three variant profiles (with their own SolidTerrainOverride/PrimaryOpenTerrain pair, even though
        // they carry no SetPieces of their own) is still exactly what closes their own terrain's tile
        // coverage -- the same "purely to close tile-coverage census exemptions and offer the palette as
        // a composable option" role TropicalSand's own doc comment (DungeonTilesetProfile.IsPaletteVariant)
        // already documents.
        //
        // 44 crosser-free groups (11/terrain: Boat 1-8, Lifeboat 1-3), verified directly against every
        // member tile's raw edges -- none carries any crosser at all. Structural classification splits by
        // whether a member carries a door slot (tolerated by both WallAlcove and OpenSetPiece, never
        // spawns a door object): Boat 1/2/3/5/6/7 (one door-slot member each) classify WallAlcove --
        // allCornersSolid is trivially true the instant SolidTerrainOverride(t) == PrimaryOpenTerrain(t),
        // the IDENTICAL "SetPieceWallAlcove" shape this file's own Tropical const doc comment documents
        // for ttz01's Barn/Farm/Inn/Windmill/Barracks family, and since Solid==Open here too,
        // IsWallAlcoveSiteValid's open-terrain touch tolerance is satisfied by literally any neighbor cell
        // (there is no separate wall mass to fail against) -- so, unlike Tropical's own measured 60.3%
        // Organic-specific disconnection gap, no comparable placement risk is expected here. Boat 4/8 and
        // Lifeboat 1-3 (no door slot) classify OpenSetPiece under the same open-field corner-match rule
        // RuralGrass's own family uses. All 44 are wired below only where FindGroup can actually reach
        // them (the Castle base profile) -- see this const's own duplicate-name paragraph above.
        //
        // 88 gangplank-bearing groups (22/terrain: three series of Boat 1-7, one gangplank edge per member
        // in a different slot/count per series, plus one standalone 1x1 "Gangplank" piece carrying two
        // gangplank edges) are EXEMPT: "gangplank" is not declared as Doorway, a stub/body crosser, or any
        // other recognized vocabulary anywhere on any of the four profiles, so
        // LayoutGroupStamper.TryClassify's IsAllowedMemberEdge rejects every one of these groups outright
        // the instant it scans a member's edges (a perimeter connector edge on an otherwise open-cornered
        // group, the exact shape that method's own doc comment names and deliberately excludes without a
        // new GroupKind). See TileCoverageCensusTests.PilotExpectedExemptions' own "tss13" entries.
        //
        // CorridorStubChain hypothesis (explicitly tested, not just asserted, per this profile audit's
        // own verification requirement): declaring "gangplank" as TunnelBodyCrosser under this same-
        // terrain composition DOES let a gangplank-bearing Boat group structurally CLASSIFY as
        // CorridorStubChain (hasAnyBodyCrosser + allCornersSolid + a perimeter body-crosser edge, all
        // trivially satisfied once gangplank is a recognized body crosser and Solid==Open). It still never
        // PLACES: TryPlaceCorridorStubChain only ever splices onto an existing Tunnel-mode chain network
        // LayoutTunnelCarver wove through solid space, and an open-field (Solid==Open) composition never
        // enters Tunnel mode at all (MacroLayoutGenerator downgrades CorridorMode whenever there is no
        // real wall mass to carve through -- the same reason this file's every other open-field profile
        // declares no Tunnel vocabulary) -- confirmed directly (see
        // SeaShipsGangplankHypothesisTests): the classification mirror confirms a gangplank-bearing "Boat
        // 1" DOES classify as CorridorStubChain once gangplank is a declared body crosser, but 0/150
        // single-attempt (retryCount 1) placements result under Complex (the only layout that ever enters
        // real Tunnel mode -- Halls/OpenLane never attempts CorridorStubChain placement at all, since it
        // carves no Tunnel network to begin with), a synthetic profile with TunnelBodyCrosser("gangplank")
        // isolated as the only configured SetPiece. Kept undeclared on every shipped profile below; the
        // shipped gangplank exemption is not a missed unlock.
        //
        // No hand-built module areas exist stamping tss13 (verified: zero .are.json references to this
        // resref anywhere in the module), so TileLighting and decoration stay at the neutral (0,0,0,0)/
        // no-palette defaults pending a future evidence-mining pass -- the same documented-gap fallback
        // rule Tropical/EarlyWinter's own doc comments use. No relief/elevation vocabulary is declared
        // either: every one of the 404 tiles is flat (CornerHeights all zero, matching the tileset's own
        // declared HasHeightTransition=0), so MaxReliefRegions/MaxElevationRegions/MaxPoolRegions all stay
        // at their 0 (unset) defaults -- there is no raised geometry anywhere in this tileset to paint.
        // No RoadCrosser either: gangplank is a narrow boat-to-shore connector family, not a general
        // through-lane network, and is the tileset's only crosser.
        //
        // Theme pairing: an open dockside/harbor scene (moored boats, gangplanks, open water at the
        // shoreline edge) -- suited to coastal settlement or naval/smuggler-dock content on an ocean or
        // river-adjacent world, alongside this project's existing exterior profiles (CityExterior's own Dock
        // district, CastleExteriorRuralHarbor).
        public const string SeaShips = "seaships";
        public const string SeaShipsCity = "seaships_city";
        public const string SeaShipsRural = "seaships_rural";
        public const string SeaShipsTropical = "seaships_tropical";
        // Medieval City 2 (tcm02, BIF-only -- verified no SWLOR_Haks copy exists; 1872 tiles / 204
        // groups). GENERAL Border=Default=Floor="Cobble" (the ttd01/ttf01/ttf02/fcx01 degenerate quirk:
        // Default==Floor forces an explicit composition decision rather than the ordinary interior
        // Default-is-the-wall default). Seven terrains (Cobble, Building, Water, Trees, Grass, Chasm,
        // Castle); five crossers (Road, Stream, Wall, Bridge, Rock). Direct 16-combo probes (ProbeTool
        // "matrix tcm02", every ordered terrain pair, both orientations, plus same-terrain open-field)
        // decided the composition: Solid=Water/Open=Cobble reaches 16/16 (both directions), and
        // TunnelVocabularyCheck.SupportsTunnels(open=Cobble, solid=Water, Custom body=port=Bridge)
        // verifies TRUE -- the same "canal city" shape as tcn01's own City Exterior* (Water the genuine
        // solid mass, Cobble the walkable street/district space, Bridge the wall-embedded tunnel
        // crosser), so this profile mirrors that precedent rather than trs02's open-field shape for its
        // base composition. TunnelCrossers("Bridge","Bridge") is wired accordingly.
        //
        // "Building" fails EVERY terrain pairing in the 16-combo matrix (2/16 or 8/16 against every
        // other terrain, never 16/16 either direction) -- it cannot function as this tileset's Solid,
        // Open, or Secondary terrain under any composition, the identical structural dead-end tcn01's
        // own Building/EvilCastle/GoodCastle bucket documents. It is composed only as an ordinary
        // decorative facade corner on house/shop/estate GROUPs (Estate1-4/Shop-family/Guildhouse2x2/
        // SmallSquare/BridgePark/BuildCorn2x1/Townhall/CraneTower(Ship)/ArchHouse1-2/Lighthouse2x3/
        // RiverBridge1-2's building-mixed variant/etc.) -- see PilotAlternateVocabTerrains["tcm02"].
        // Many of these SAME group names still classify via IsFeatureTileEligible/IsExitGroupEligible
        // regardless (both mechanisms are structurally terrain-agnostic -- see WitcherShop/BuildingSite/
        // Shop1-3/Bakery/Museum/PatriciansHouse/Smithy/StairHouse/CornerShop1-2/CornerPub/BurntHouse1-2/
        // BuildingBad1/CornerBTower1/CornerBTower2a/InnerCornerEmpty1-2/OuterCornerEmpty1-3/
        // StraightEmpty1-2/DoubleCornerEmpty1 below), so only the genuinely MULTI-TILE building-cornered
        // groups fall through to the alternate-vocab bucket.
        //
        // "Wall" (Battlement*/CornerTower*/Drawbridge*/RiverWall1/Stable/CliffWallCave -- a
        // door-bearing crosser family) has no verified body/port/road vocabulary, and -- unlike
        // Barrows' "door_corridor" -- declaring it as a DoorSlotCrosser would not help: every carrier is
        // a 1x1 group, and IsDoorwayEdge admission on a non-Solid-cornered 1x1 group hits
        // ClassifyMultiTileSetPiece's "mixed shape tolerated only when every doorway edge is interior,
        // never perimeter" rule head-on (a 1x1 group's own edge is always perimeter by construction) --
        // verified directly, declaring it only trades one rejection branch for another. "Stream"/"Road"/
        // "Rock" carry no wired body/port/road-lane vocabulary this pass either (their few flat,
        // crosser-bearing GROUP carriers -- streamWillow/Bridge1/Bridge2/RuinedCart/CliffBridge1-2/
        // CliffWillow -- fail the same structural gate). All four are folded into
        // PilotAlternateVocabCrossers["tcm02"], alongside "path" (a fifth, rare crosser found on exactly
        // one group member, CliffPath1's single member tile, not in the tileset's own 5-crosser summary
        // at all -- the same rare-crosser quirk trs02's own "path" entry documents).
        //
        // Lighting sampled directly from the one hand-built tcm02 area (Module/are/dan_centcolony.
        // are.json, 160 tiles): uniform MainLight1=0/MainLight2=0/SrcLight1=0/SrcLight2=0, matching
        // every other exterior profile's daylight convention. That area's placeable inventory reads as
        // a Dantooine colony reskin of this tileset (modern fences/furniture/kiosks/tents, not generic
        // medieval dressing) -- the curated decoration palette below is drawn from its genuinely
        // decorative entries (excluding collision blockers and invisible marker objects) rather than
        // invented wholesale, the same "thin, reskinned evidence" caveat tcn01's own garrison-skewed
        // palette documents.
        //
        // THEME PAIRING: no theme/content registration happens here, matching every profile in this
        // file. Natural future pairing: a walled trading-town or river-port settlement world (matching
        // the Water-canal/Bridge composition and CastleTowerGate/PrisonTower/CastleHugeGate garrison
        // vocabulary below).
        public const string MedievalCity = "medievalcity";

        // Medieval City 2's Chasm/cliff sub-family -- CliffPath1-2/CliffCaveEntry/CliffBottomCave1-2/
        // CliffTopCave1/ChasmPond/ChasmBridgeWB1-5/CliffRockFormation/HillCave1 all pair Chasm with
        // Grass (never Cobble), a physically SEPARATE composition from the base profile's Water/Cobble
        // canal-city shape (a composition carries only one Solid/Open pair) -- recomposing the SAME
        // tcm02 data with SolidTerrainOverride("Chasm") + PrimaryOpenTerrain("Grass") the same way
        // RuralGrassEvilCastle/FrozenWastesEvilCastle recompose their own base tileset's data with a
        // different Solid/Open pair, hence PaletteVariant(). Direct 16-combo probe confirms Chasm/Grass
        // reaches 16/16 both directions; MinimumOpeningWidth is 2 here (verified via ProbeTool "width"),
        // unlike the base profile's 1. TunnelVocabularyCheck.SupportsTunnels(open=Grass, solid=Chasm,
        // Custom body=port=Bridge) verifies TRUE -- the SAME "Bridge" crosser name the base profile uses
        // against Water, independently reverified against Chasm here (a composition-local re-check, not
        // assumed from the base profile's own verification) -- wired as this variant's own
        // TunnelCrossers pair, closing the ChasmBridgeWB1-5 family as SetPieceCorridorStub (all-Chasm
        // corners, a single Bridge edge each). CliffPath2/CliffCaveEntry (Chasm+Grass mixed corners, no
        // crosser) classify as SetPieceOpenSetPiece under this pair directly; CliffRockFormation
        // (all-Chasm, no door/crosser) also classifies as SetPieceOpenSetPiece since an all-Solid
        // multi-tile blob still satisfies the "every corner in {Solid, Open}" rule trivially.
        // CliffBottomCave1-2/CliffTopCave1 (Chasm+Grass, 1x1, door-bearing, crosser-free) and HillCave1
        // (Grass, non-flat, crosser-free -- a SetPieceReliefPiece against this variant's own
        // PrimaryOpenTerrain) close via the SAME vocab-independent/Open-dependent mechanisms the base
        // profile's own castle-door family below already relies on. CliffPath1 (Chasm/Grass +
        // its own rare "path" crosser) and CliffWillow/CliffBridge1-2 (Chasm + "Stream") stay unwired
        // this pass -- see PilotAlternateVocabCrossers["tcm02"]. CliffPond (Chasm+Cobble) and
        // Grass_boat_docked/DockedShip1x4_Grass (Water+Grass) mix a terrain pair neither this variant
        // nor the base profile composes; see this class's own PilotExpectedExemptions-equivalent
        // writeup in TileCoverageCensusTests for the exact accounting.
        public const string MedievalCityCliffs = "medievalcity_cliffs";

        // Medieval City 2's Castle garrison sub-family -- CastleSmallDoor/CastleHugeGate/
        // CastleTowerGate1-2/PrisonTower (all Castle+Cobble corners) classify as ExitGroups on the base
        // profile too (IsExitGroupEligible is terrain-agnostic), but measure a documented 0% real
        // placement ceiling there: GroupExitPlanner needs the group's own corner pattern to match a
        // REAL site in the generated grid, and Castle terrain never gets painted anywhere under the
        // base profile's Water/Cobble composition (verified directly, ProbeTool "place" 150 seeds each,
        // Halls -- 0/150 for all five). Recomposing the SAME tcm02 data with
        // SolidTerrainOverride("Castle") (Castle/Cobble reaches 16/16, MinimumOpeningWidth 2) makes
        // Castle a genuine wall material, the identical fix ForestGoodCastle/RuralGrassGoodCastle's own
        // doc comments document for their own Castle-door families -- all five now place at a real,
        // measured rate. CastleSmallDoor2/CastleHugeGateGrass (Castle+GRASS corners) are NOT moved here:
        // a profile carries only one PrimaryOpenTerrain, already claimed by Cobble for the five
        // Castle+Cobble groups above, so those two stay on the base profile with their own documented
        // 0% ceiling.
        public const string MedievalCityCastle = "medievalcity_castle";

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
            // left at the default 1: PathNodeOpeningWidthAudit (SWLOR.Toolset.Tests/AreaGeneration/
            // PathNodeOpeningWidthAudit.cs) finds partially-open Floor/Wall combos with a pathnode-A
            // candidate, so (unlike zsf01) 1-wide openings path fine.
            // Group names are curated to the "[Tan]" palette specifically -- every group in this
            // tileset is prefixed "[Tan]"/"[Grey]"/"[Dwarven]" (there is no unprefixed base family);
            // "[Tan]" is simply the base Wall/Floor/Pit palette's curation label (its groups' corners
            // use plain "Floor"/"Pit", not a "Tan"-named terrain), verified directly against the .set
            // data. Fence doors are included even though no shipped layout profile currently pairs
            // Fence carving with this tileset, matching StandardTilesetProfiles' own convention of
            // registering structurally-valid pieces a future layout pairing can exercise.
            _builder.Create(Crypt, "Crypts*")
                .Tileset("tdc01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                // Family AREA atmosphere -- the SWLOR standard windowless-interior .are tuple, mined
                // from the hand-built tdc01 exemplars: hiddenquestzone, vrotranccsitharc,
                // v_swamp_undergro (3 of 5 module areas agree exactly on the full core tuple; every
                // other tuple is a singleton). Locked night, dim blue-grey moon ambient, no skybox,
                // no fog, no wind; LightingScheme 13 / ShadowOpacity 60 / FogClipDist 45 are
                // unanimous among the agreeing areas.
                .Atmosphere(a =>
                {
                    a.SkyBox = 0;
                    a.DayNightCycle = false;
                    a.IsNight = true;
                    a.SunAmbientColor = 0;
                    a.SunDiffuseColor = 0;
                    a.MoonAmbientColor = 2960685;
                    a.MoonDiffuseColor = 6457991;
                    a.SunFogAmount = 0;
                    a.SunFogColor = 0;
                    a.MoonFogAmount = 5;
                    a.MoonFogColor = 0;
                    a.SunShadows = false;
                    a.MoonShadows = false;
                    a.ShadowOpacity = 60;
                    a.WindPower = 0;
                    a.LightingScheme = 13;
                    a.FogClipDist = 45f;
                })
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
            _builder.Create(CryptGrey, "Crypts* (Grey)")
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
            // missing-Doorway gap, verified by the registered-tileset pipeline coverage. "[Dwarven] Cave
            // Entrance (2x1)" mixes THREE terrains (DwarvenFloor/DwarvenPit/
            // Wall) in one group -- outside ClassifyMultiTileSetPiece's two-terrain (Solid/Open or
            // Solid/Secondary) shape -- and stays exempted, a genuine structural gap shared with the
            // base Crypt profile's own scope (multi-terrain sets are never wired anywhere in this file).
            _builder.Create(CryptDwarven, "Crypts* (Dwarven)")
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
            // other three are the same proven Bridge-channel shape, just outside this profile's
            // single accent slot, and (c) a "MazeMosaic" crosser outside the canonical vocabulary.
            // AccentTerrain("Lava") mirrors Cavern's Water / Sewers' Pit pattern.
            // Group names verified directly against the .set data. Only the base/no-suffix and
            // "-Lava"-suffixed groups are wired (matching the single AccentTerrain("Lava") slot);
            // the analogous Water/Sewer/Ice/Pit-suffixed groups (Exit 2, Platform 4, Pillar 1/2, Door
            // - Bridge 1) are the identical shape and are left for future work that either extends
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
            // mixed-terrain, mixed-height tile family (e.g. TILE505/506/510/521...) this profile's earlier
            // census left height-exempted.
            _builder.Create(Dungeon, "Dungeon*")
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
            _builder.Create(DungeonWater, "Dungeon* (Water)")
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

            _builder.Create(DungeonSewer, "Dungeon* (Sewer)")
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

            _builder.Create(DungeonIce, "Dungeon* (Ice)")
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

            _builder.Create(DungeonPit, "Dungeon* (Pit)")
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

            // [CEP] Dungeon (zde01) -- byte-identical tile data to tde01 (see the CepDungeon const's
            // own doc comment above for the full writeup), so this is the SAME wiring as the base
            // Dungeon profile above, just against Tileset("zde01") with the real sampled TileLighting
            // and this profile family's verbatim display-name convention.
            _builder.Create(CepDungeon, "[CEP] Dungeon")
                .Tileset("zde01")
                .MaxElevationRegions(2)
                .MaxPoolRegions(2)
                .MaxReliefRegions(2)
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
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
                .SetPiece("Ramp - Straight", 1)
                .SetPiece("Ramp - Corner, Floor", 1)
                .SetPiece("Ramp - Corner, Lava", 1)
                .ExitGroup("Exit 1")
                .ExitGroup("Exit 2 - Lava");

            _builder.Create(CepDungeonWater, "[CEP] Dungeon (Water)")
                .Tileset("zde01")
                .MaxElevationRegions(2)
                .MaxPoolRegions(2)
                .MaxReliefRegions(2)
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
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

            _builder.Create(CepDungeonSewer, "[CEP] Dungeon (Sewer)")
                .Tileset("zde01")
                .MaxElevationRegions(2)
                .MaxPoolRegions(2)
                .MaxReliefRegions(2)
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
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

            _builder.Create(CepDungeonIce, "[CEP] Dungeon (Ice)")
                .Tileset("zde01")
                .MaxElevationRegions(2)
                .MaxPoolRegions(2)
                .MaxReliefRegions(2)
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
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

            _builder.Create(CepDungeonPit, "[CEP] Dungeon (Pit)")
                .Tileset("zde01")
                .MaxElevationRegions(2)
                .MaxPoolRegions(2)
                .MaxReliefRegions(2)
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
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

            // [CEP] City Interior 1 (zin01) -- base profile: City+Home+Workshop-exit family. See the
            // CepCityInterior const's own doc comment above for the full writeup.
            _builder.Create(CepCityInterior, "[CEP] City Interior 1")
                .Tileset("zin01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                // "Window" tail closure -- see the CepCityInterior const's own doc comment above.
                .DoorSlotCrossers("Window")
                .FeatureTile("[City] Chessboard - Home")
                .FeatureTile("[City] Chessboard - Inn")
                .FeatureTile("[City] Chessboard - Kitchen")
                .FeatureTile("[City] Chessboard - Living Room")
                .FeatureTile("[City] Chessboard - Shop")
                .FeatureTile("[City] Portal - Home")
                .FeatureTile("[City] Portal - Inn")
                .FeatureTile("[City] Portal - Kitchen")
                .FeatureTile("[City] Portal - Living Room")
                .FeatureTile("[City] Portal - Shop")
                .FeatureTile("[Workshop] Chessboard")
                .FeatureTile("[Workshop] Portal")
                .FeatureTile("[Workshop] Smelter")
                .SetPiece("[City] Door - Living Room 1", 1)
                .SetPiece("[City] Door - Kitchen 1", 1)
                .SetPiece("[City] Door - Inn 1", 1)
                .SetPiece("[City] Door - Shop 1", 1)
                .SetPiece("[City] Exit - Home Corner 1", 1)
                .SetPiece("[City] Exit - Home Corner 2", 1)
                .SetPiece("[City] Exit - Kitchen Corner 1", 1)
                .SetPiece("[City] Exit - Kitchen Corner 2", 1)
                .SetPiece("[City] Exit - Living Room Corner 1", 1)
                .SetPiece("[City] Exit - Living Room Corner 2", 1)
                .SetPiece("[City] Stairs - Both, Kitchen Corner", 1)
                .SetPiece("[City] Stairs - Both, Living Room Corner", 1)
                .SetPiece("[City] Stairs - Down, Home Corner", 1)
                .SetPiece("[City] Stairs - Down, Kitchen Corner", 1)
                .SetPiece("[City] Stairs - Down, Living Room Corner", 1)
                .SetPiece("[City] Stairs - Up, Home Corner 1", 1)
                .SetPiece("[City] Stairs - Up, Home Corner 2", 1)
                .SetPiece("[City] Stairs - Up, Kitchen Corner", 1)
                .SetPiece("[City] Stairs - Up, Living Room Corner", 1)
                .SetPiece("[City] Window - Home", 1)
                .SetPiece("[City] Window - Porthole 1")
                .SetPiece("[City] Window - Porthole 2", 1)
                .SetPiece("[City] Window - Porthole 3", 1)
                .SetPiece("[Workshop] Exit - Corner 1", 1)
                .SetPiece("[Workshop] Exit - Corner 2", 1)
                .SetPiece("[Workshop] Stairs - Both, Corner", 1)
                .SetPiece("[Workshop] Stairs - Down, Corner", 1)
                .SetPiece("[Workshop] Stairs - Up, Corner", 1)
                .SetPiece("[Workshop] Smithy")
                .SetPiece("[City] Room - Inn", 1)
                .SetPiece("[City] Room - Inn 1 (1x2)", 1)
                .SetPiece("[City] Room - Inn 2 (1x2)", 1)
                .SetPiece("[City] Room - Inn 2, Window (1x2)", 1)
                .SetPiece("[City] Room - Kitchen", 1)
                .SetPiece("[City] Room - Kitchen 1 (1x2)", 1)
                .SetPiece("[City] Room - Kitchen 1, Window (1x2)", 1)
                .SetPiece("[City] Room - Kitchen 2 (1x2)", 1)
                .SetPiece("[City] Room - Kitchen 2, Window (1x2)", 1)
                .SetPiece("[City] Room - Living Room", 1)
                .SetPiece("[City] Room - Living Room 1 (1x2)", 1)
                .SetPiece("[City] Room - Living Room 1, Window (1x2)", 1)
                .SetPiece("[City] Room - Living Room 2 (1x2)", 1)
                .SetPiece("[City] Room - Living Room 2, Window (1x2)", 1)
                .SetPiece("[City] Room - Shop", 1)
                .SetPiece("[City] Room - Shop 1 (1x2)", 1)
                .SetPiece("[City] Room - Shop 2 (1x2)", 1)
                .SetPiece("[City] Room - Bordello (2x2)", 1)
                .SetPiece("[City] Home - Lower 1 (2x2)")
                .SetPiece("[City] Home - Lower 2 (2x2)")
                .SetPiece("[City] Home - Lower 3 (2x2)")
                .SetPiece("[City] Home - Lower 4 (2x2)")
                .SetPiece("[City] Home - Lower 5 (2x2)")
                .SetPiece("[City] Home - Upper 1 (2x2)")
                .SetPiece("[City] Home - Upper 2 (2x2)")
                .SetPiece("[City] Home - Upper 3 (2x2)")
                .SetPiece("[City] Interior - Barn (3x2)")
                .SetPiece("[City] Interior - Barracks (2x3)")
                .SetPiece("[City] Interior - Shop 1 (1x2)")
                .SetPiece("[City] Interior - Shop 2 (1x2)")
                .SetPiece("[City] Interior - Slum House 1")
                .SetPiece("[City] Interior - Slum House 2")
                .SetPiece("[City] Interior - Smithy (2x1)")
                .SetPiece("[City] Interior - Temple Evil (4x3)")
                .SetPiece("[City] Interior - Temple Good (4x4)")
                .SetPiece("[City] Interior - Temple Neutral (3x3)")
                .SetPiece("[City] Interior - Tent (2x2)")
                .SetPiece("[City] Interior - Wizards Den (2x2)")
                .ExitGroup("[City] Door - Transition")
                .ExitGroup("[City] Exit - Corridor")
                .ExitGroup("[City] Exit - Corridor, Big")
                .ExitGroup("[City] Stairs - Down")
                .ExitGroup("[City] Stairs - Up");

            // [CEP] City Interior 1 (Elven) -- ElvenFloor/ElvenPlatform/ElvenGrass district
            // PaletteVariant, the tileset's single richest secondary district (25 ElvenFloor groups).
            // ElvenHallway (the district's own renamed door/hallway crosser, paralleling Barrows'
            // "corridor"/"door_corridor" and Modern Facility's "doorway1/2/3") is declared via
            // DoorSlotCrossers so CornerEdgeResolver/WallAlcove-style mechanisms recognize it on the
            // Room - Round/Stairs - Down/Up family.
            _builder.Create(CepCityInteriorElven, "[CEP] City Interior 1 (Elven)")
                .Tileset("zin01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .PaletteVariant()
                .PrimaryOpenTerrain("ElvenFloor")
                .DoorSlotCrossers("ElvenHallway")
                .FeatureTile("[Elven] Pool - Platform, Lights")
                .FeatureTile("[Elven] Pool - Platform, Tree")
                .FeatureTile("[Elven] Portal - Floor")
                .FeatureTile("[Elven] Portal - Grass")
                .FeatureTile("[Elven] Stairs - Both, Spiral")
                .FeatureTile("[Elven] Stairs - Down, Spiral")
                .FeatureTile("[Elven] Stairs - Down, Spiral/Light")
                .FeatureTile("[Elven] Stairs - Up, Spiral")
                .FeatureTile("[Elven] Stairs - Up, Spiral/Light")
                .FeatureTile("[Elven] Table - Tree")
                .FeatureTile("[Elven] Table - Tree, Lights")
                .FeatureTile("[Elven] Tree - Giant")
                .FeatureTile("[Elven] Tree - Medium")
                .FeatureTile("[Elven] Tree - Medium, Walkable")
                .FeatureTile("[Elven] Chessboard - Floor")
                .FeatureTile("[Elven] Chessboard - Grass")
                .SetPiece("[Elven] Alcove - Couch")
                .SetPiece("[Elven] Alcove - Couch/Light")
                .SetPiece("[Elven] Alcove - Grass")
                .SetPiece("[Elven] Alcove - Light")
                .SetPiece("[Elven] Alcove - Platform, Couch")
                .SetPiece("[Elven] Alcove - Platform, Couch/Light")
                .SetPiece("[Elven] Alcove - Simple")
                .SetPiece("[Elven] Corner - Curtain")
                .SetPiece("[Elven] Corner - Curtain, Couch")
                .SetPiece("[Elven] Corner - Curtain, Table")
                .SetPiece("[Elven] Exit - Grass")
                .SetPiece("[Elven] Exit - Platform", 1)
                .SetPiece("[Elven] Stub/Candle - Platform")
                .SetPiece("[Elven] Wall Pool - Platform, Lights")
                .SetPiece("[Elven] Wall Tree - Platform")
                .SetPiece("[Elven] Wall Tree - Platform, Lights")
                .SetPiece("[Elven] Room - Round", 1)
                .SetPiece("[Elven] Room - Round, Couch")
                .SetPiece("[Elven] Room - Round, Couch/Light")
                .SetPiece("[Elven] Room - Round, Light", 1)
                .SetPiece("[Elven] Stairs - Down, Long", 1)
                .SetPiece("[Elven] Stairs - Down, Short", 1)
                .SetPiece("[Elven] Stairs - Up, Short", 1)
                .SetPiece("[Elven] Room - Oval, Grass (2x3)")
                .SetPiece("[Elven] Tree House - Grass (3x3)")
                .SetPiece("[Elven] Tree House - Grass, Window (3x3)")
                .SetPiece("[Elven] Room - Tree (2x2)")
                .SetPiece("[Elven] Room - Tree, Table (2x2)")
                .SetPiece("[Elven] Room - Tree, Table/Lights (2x2)")
                .SetPiece("[Elven] Pool (1x2)")
                .SetPiece("[Elven] Pool - Trees (1x2)");

            // [CEP] City Interior 1 (Sigil) -- SigilFloor district PaletteVariant, 14 groups.
            // SigilHallway is the district's own renamed door/hallway crosser (same DoorSlotCrossers
            // pattern as Elven above); "SigilFloor" is ALSO used once as a crosser name on the
            // "Corridor - Entry" group's edges, a genuine tileset-authoring quirk -- declared alongside
            // SigilHallway so the mechanism gets a fair chance at it, but this is unverified pending
            // the census run (see the profile's own follow-up note if it stays exempt).
            _builder.Create(CepCityInteriorSigil, "[CEP] City Interior 1 (Sigil)")
                .Tileset("zin01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .PaletteVariant()
                .PrimaryOpenTerrain("SigilFloor")
                .DoorSlotCrossers("SigilHallway", "SigilFloor")
                .FeatureTile("[Sigil] Counter - Magic")
                .FeatureTile("[Sigil] Counter - Normal")
                .FeatureTile("[Sigil] Portal")
                .SetPiece("[Sigil] Corner - Snug 1 (1x2)")
                .SetPiece("[Sigil] Corner - Snug 2 (2x1)")
                .SetPiece("[Sigil] Corner - Spiral", 1)
                .SetPiece("[Sigil] Window 1")
                .SetPiece("[Sigil] Window 2")
                .SetPiece("[Sigil] Arena (3x3)", 1)
                .SetPiece("[Sigil] Bar (2x2)", 1)
                .SetPiece("[Sigil] Door - Main (2x2)", 1)
                .SetPiece("[Sigil] Doorway 1 (1x2)", 1)
                .SetPiece("[Sigil] Doorway 2 (1x2)", 1)
                .SetPiece("[Sigil] Corridor - Stairs Down", 1)
                .SetPiece("[Sigil] Corridor - Stairs Up", 1)
                .SetPiece("[Sigil] Corridor - Entry");

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

            // City Interior's own bulk palette — mined from tin01 hand-built reference areas
            // (decoration_evidence/evidence_by_theme_keyword.json/evidence_named_exemplars.json,
            // undercity keyword match — small sample, e.g. pw_ar_velundr). Strongest co-occurrence
            // pair: plc_sacks + x0_ruglarge (5) -> vignette.
            _builder
                .Decoration("zep_bedrolls002", 3, DecorationContext.WallAdjacent)
                .Decoration("_mdrn_pl_clothh1", 1, DecorationContext.WallAdjacent)
                .Decoration("zep_carpet02", 2, DecorationContext.RoomCenter)
                .Decoration("zep_cushion001", 1, DecorationContext.RoomCenter)
                .Decoration("_mdrn_pl_cageirn", 1, DecorationContext.DoorwayFlank)
                .Decoration("zep_bedrolls001", 1, DecorationContext.CorridorSide)
                .Vignette("SlumSackPile", 2)
                .VignetteMember("zep_bedrolls002", 0f, 0f)
                .VignetteMember("zep_carpet02", 0.8f, 0.5f);

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
            // via the corridor-stub-chain placement regression coverage.
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
            _builder.Create(Barrows, "TNO: Barrows Interior")
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
            // left for future work that either extends multi-district support or ships dedicated
            // profiles per palette -- see TileCoverageCensusTests.PilotAlternateVocabTerrains["tdm01"].
            // AccentTerrain("Water") is the one wired accent channel of [Cave]'s Water/Pit/Lava/Ice
            // quartet (mirrors Dungeon/tde01's single-accent-slot precedent); "[Cave] Door - Bridge,
            // Pit"/"Lava" are the same shape on the two unwired accents and excluded. "[Cave] Ramp" and
            // "[Cave] Cave Entrance" are both wired via LayoutGroupStamper's ReliefPiece kind (see the
            // SetPiece entries below) -- ReliefPiece now tolerates a door slot the same way WallAlcove/
            // OpenSetPiece/WallRoom already do (never spawns a door object), which closes "Cave
            // Entrance"'s raised-rim-plus-doorframe shape (raised exterior set-piece classification;
            // see LayoutGroupStamper.TryClassifyReliefPiece's own doc comment). "[Cave] Door -
            // Transition", "[Cave] Ship - Docked", "[Cave] Docks (1x2)" don't structurally classify
            // under any current mechanism and are excluded.
            _builder.Create(MinesAndCaverns, "Mines and Caverns*")
                .Tileset("tdm01")
                // Family AREA atmosphere -- the SWLOR standard windowless-interior .are tuple (same
                // values as tdc01/zsf01: the module authors reuse one dark-interior template), mined
                // from the hand-built tdm01 exemplars: tat_tuskcavebot, tat_tuskcavemain,
                // tat_tuskcavetunn, tat_wormden (4 of 13 module areas agree exactly on the full core
                // tuple; the runner-up tuple has 3). LightingScheme 13 / ShadowOpacity 60 /
                // FogClipDist 45 are unanimous among the agreeing areas.
                .Atmosphere(a =>
                {
                    a.SkyBox = 0;
                    a.DayNightCycle = false;
                    a.IsNight = true;
                    a.SunAmbientColor = 0;
                    a.SunDiffuseColor = 0;
                    a.MoonAmbientColor = 2960685;
                    a.MoonDiffuseColor = 6457991;
                    a.SunFogAmount = 0;
                    a.SunFogColor = 0;
                    a.MoonFogAmount = 5;
                    a.MoonFogColor = 0;
                    a.SunShadows = false;
                    a.MoonShadows = false;
                    a.ShadowOpacity = 60;
                    a.WindPower = 0;
                    a.LightingScheme = 13;
                    a.FogClipDist = 45f;
                })
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
                // Baked-mesh cave-mouth piece (1x1 GROUP, non-flat [Floor 1,1,0,0], crosser-free, one
                // door slot) -- same ReliefPiece kind as "[Cave] Ramp" above, now door-tolerant.
                .SetPiece("[Cave] Cave Entrance", 1)
                .ExitGroup("[Cave] Exit 1")
                .ExitGroup("[Cave] Exit 2")
                .ExitGroup("[Cave] Exit 3");

            // Mines and Caverns' own bulk palette — mined from tdm01 hand-built reference areas
            // (decoration_evidence/evidence_by_tileset.json['tdm01'], 13 areas). Strongest
            // co-occurrence pair: zep_grasstuft001 + zep_shrub036 (40) -> vignette.
            _builder
                .Decoration("zep_shrub036", 3, DecorationContext.WallAdjacent)
                .Decoration("_mdrn_pl_plant07", 2, DecorationContext.WallAdjacent)
                .Decoration("_mdrn_pl_qwall10", 2, DecorationContext.WallAdjacent)
                .Decoration("zep_bushfern001", 2, DecorationContext.WallAdjacent)
                .Decoration("zep_grasstuft001", 3, DecorationContext.CorridorSide)
                .Decoration("zep_shrub035", 2, DecorationContext.CorridorSide)
                .Decoration("zep_vinesh", 1, DecorationContext.CorridorSide)
                .Decoration("zep_fissure_017", 2, DecorationContext.RoomCenter)
                .Decoration("zep_lightshft008", 1, DecorationContext.RoomCenter)
                .Decoration("_mdrn_pl_conta54", 1, DecorationContext.DoorwayFlank)
                .Decoration("zep_flowers004", 1, DecorationContext.DoorwayFlank)
                .Vignette("MineGrassThicket", 3)
                .VignetteMember("zep_grasstuft001", 0f, 0f)
                .VignetteMember("zep_shrub036", 0.9f, 0.6f);

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
            // composition" constraint LayoutTunnelCarver enforces), left for future work.
            // "[Desert] Ramp" and "[Desert] Cave Entrance" are both non-flat and wired via
            // LayoutGroupStamper's ReliefPiece kind (matching [Cave]'s own Ramp/Cave Entrance pieces
            // -- ReliefPiece now tolerates Cave Entrance's door slot, see the base profile's own
            // comment). Every other Desert group (Platforms, Pillar, Stairs 2x2, Treasure,
            // Crystal Casket/Column/Crypt, Chessboard, Portal, Mineshaft, Wall Section, Exit 1/2/3)
            // mirrors [Cave]'s own wired set piece/feature-tile/exit-group shapes tile-for-tile.
            // IsPaletteVariant() excludes this profile from --matrix's full cross-product (see
            // SWLOR.ProcgenReview/Program.cs) -- it gets one showcase area instead. [Organic] and [City]
            // remain unwired (left for future work; [Organic] mirrors [Desert]'s shape closely but
            // [City] has a much smaller, differently-shaped tile family and would need its own probe).
            _builder.Create(MinesAndCavernsDesert, "Mines and Caverns* (Desert)")
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
                .SetPiece("[Desert] Cave Entrance", 1)
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
            // stays unwired for the same one-body-crosser-per-profile reason; "[Organic] Ramp" and
            // "[Organic] Cave Entrance" are both wired via ReliefPiece -- see the Desert profile's
            // comment for the full reasoning.
            _builder.Create(MinesAndCavernsOrganic, "Mines and Caverns* (Organic)")
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
                .SetPiece("[Organic] Cave Entrance", 1)
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
            _builder.Create(MinesAndCavernsCity, "Mines and Caverns* (City Water)")
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
                // "[City] Cave Entrance" (TILE1456) is Floor-cornered like [Cave]'s own family (this
                // profile has no distinct City-terrain open corner of its own -- see the class doc
                // comment above), so it's reachable here the same way "[Cave] Ramp"/"Door - Big 1/2"
                // already are.
                .SetPiece("[City] Cave Entrance", 1)
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
            _builder.Create(MinesAndCavernsTracks, "Mines and Caverns* (Tracks)")
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
            _builder.Create(MinesAndCavernsDesertTracks, "Mines and Caverns* (Desert Tracks)")
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
            _builder.Create(MinesAndCavernsOrganicTracks, "Mines and Caverns* (Organic Tracks)")
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
            // (vmr01)'s own Chasm-vs-Plaza precedent exactly. This tileset also has a
            // verified Alley crosser (Doorway/Alley/Corridor/Fence/Bridge, 5 crossers) --
            // BigDoorAlley/ExteriorStairsDown/ExteriorStairsUp confirm Alley coverage, but Streets
            // layout pairing is outside this profile's verified Complex/Halls/Organic scope and left
            // for future work. Excluded: Mosaic_Plaza_2x2,
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

            // Ruins' own bulk palette — mined from tdr01 hand-built reference areas
            // (decoration_evidence/evidence_by_tileset.json['tdr01'], 5 areas). Strongest
            // co-occurrence pair: zep_book001 + zep_notes001 (9) -> vignette.
            _builder
                .Decoration("zep_bbook003", 2, DecorationContext.WallAdjacent)
                .Decoration("zep_bbook004", 2, DecorationContext.WallAdjacent)
                .Decoration("zep_book001", 2, DecorationContext.WallAdjacent)
                .Decoration("zep_book002", 1, DecorationContext.WallAdjacent)
                .Decoration("zep_bflame003", 1, DecorationContext.RoomCenter)
                .Decoration("zep_animalcag002", 1, DecorationContext.RoomCenter)
                .Decoration("zep_bflame002", 2, DecorationContext.DoorwayFlank)
                .Decoration("structure_rubble", 1, DecorationContext.DoorwayFlank)
                .Decoration("zep_fog001", 1, DecorationContext.CorridorSide)
                .Decoration("zep_fog001", 1, DecorationContext.CorridorSide)
                .Vignette("RuinsReadingNook", 2)
                .VignetteMember("zep_book001", 0f, 0f)
                .VignetteMember("zep_notes001", 0.7f, 0.3f);

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
            _builder.Create(CastleInterior, "Castle Interior 1*")
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

            // Castle Interior's own bulk palette — mined from tic01 hand-built reference areas
            // (decoration_evidence/evidence_by_theme_keyword.json, sithacademy keyword match, 37
            // areas). Strongest thematic pairing: zep_arch002 + swp_banner0001 (both spike in the
            // ar_scor_kacademy exemplar) -> vignette.
            _builder
                .Decoration("swd3_wall001", 3, DecorationContext.WallAdjacent)
                .Decoration("_mdrn_pl_hwall25", 2, DecorationContext.WallAdjacent)
                .Decoration("_mdrn_pl_hwall28", 2, DecorationContext.WallAdjacent)
                .Decoration("zep_wall002", 1, DecorationContext.WallAdjacent)
                .Decoration("zep_fountain002", 2, DecorationContext.RoomCenter)
                .Decoration("zep_bflame002", 1, DecorationContext.RoomCenter)
                .Decoration("zep_arch002", 2, DecorationContext.DoorwayFlank)
                .Decoration("swp_banner0001", 2, DecorationContext.DoorwayFlank)
                .Decoration("zep_fog002", 1, DecorationContext.CorridorSide)
                .Decoration("zep_grasstuft001", 1, DecorationContext.CorridorSide)
                .Vignette("AcademyBannerGate", 3)
                .VignetteMember("zep_arch002", 0f, 0f)
                .VignetteMember("swp_banner0001", 0.6f, 0.4f);

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
            // edge crosser -- outside this profile's wired vocabulary, the same exclusion as every other
            // Window-* piece on the base profile -- so it fails even the relaxed member-edge gate and
            // stays unclassified. The base profile's own terrain-agnostic pieces (Stairs - Up/Down,
            // Exit - Corridor/Big, Round Corner - Empty/Decorated Stone, Stairs - Up/Down Stone Corner,
            // Dais, every Room-* WallRoom family) already work regardless of which PrimaryOpenTerrain is
            // composed, so they are NOT re-wired here -- these variants add ONLY their own
            // district-specific pieces. PaletteVariant() excludes each from --matrix's full
            // cross-product -- one showcase area each instead.
            _builder.Create(CastleInteriorStorage, "Castle Interior 1* (Storage)")
                .Tileset("tic01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .PaletteVariant()
                .PrimaryOpenTerrain("Storage")
                .SetPiece("[Castle] Door - Storage 1", 1)
                .SetPiece("[Castle] Door - Storage 2", 1);

            _builder.Create(CastleInteriorRich, "Castle Interior 1* (Rich)")
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

            _builder.Create(CastleInteriorLibrary, "Castle Interior 1* (Library)")
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

            _builder.Create(CastleInteriorJail, "Castle Interior 1* (Jail)")
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
            _builder.Create(CastleInterior2, "TNO: Castle Interior")
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
            _builder.Create(DrowInterior, "Drow Interior*")
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

            // Drow Interior's own bulk palette — mined via nightsistercoven keyword match
            // (decoration_evidence/evidence_by_theme_keyword.json — dath_grottos/dathgrottocavern/
            // pw_sc_dath_apexd, 3 areas). Strongest structural pairing: the two grotto cliff-face
            // variants -> vignette.
            _builder
                .Decoration("zep_tno_cliff_1", 3, DecorationContext.WallAdjacent)
                .Decoration("zep_tno_cliff_2", 3, DecorationContext.WallAdjacent)
                .Decoration("zep_boulder003", 2, DecorationContext.WallAdjacent)
                .Decoration("zep_shrub036", 2, DecorationContext.WallAdjacent)
                .Decoration("zep_grasstuft001", 3, DecorationContext.CorridorSide)
                .Decoration("zep_shrub041", 1, DecorationContext.CorridorSide)
                .Decoration("zep_redfern", 1, DecorationContext.CorridorSide)
                .Decoration("zep_pillrmrbl002", 2, DecorationContext.RoomCenter)
                .Decoration("zep_geiser002", 1, DecorationContext.RoomCenter)
                .Decoration("zep_arch002", 2, DecorationContext.DoorwayFlank)
                .Decoration("zep_gardenstn003", 1, DecorationContext.DoorwayFlank)
                .Vignette("GrottoCliffGrowth", 2)
                .VignetteMember("zep_tno_cliff_1", 0f, 0f)
                .VignetteMember("zep_tno_cliff_2", 1.0f, 0.3f);

            // Illithid Interior (tii01, SWLOR_Haks/sw_t_illithid). PrimaryOpenTerrain left empty
            // (defaults to declared Floor "Floor"). This compact tileset has 79 tiles and 10 groups:
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
            // (see the Illithid Complex open-lane downgrade regression coverage),
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
            // the door-slot wall-room Complex placement regression coverage, which
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

            // Illithid Interior's own bulk palette — mined from tii01 hand-built reference areas
            // (decoration_evidence/evidence_by_tileset.json['tii01'], 3 areas). Strongest
            // co-occurrence pairs: plc_altrevil + plc_fountain (67) and x3_plc_mist + x3_plc_slightr
            // (47) -> vignettes.
            _builder
                .Decoration("_mdrn_pl_wwall4t", 3, DecorationContext.WallAdjacent)
                .Decoration("swd3_wall001", 2, DecorationContext.WallAdjacent)
                .Decoration("_mdrn_pl_hwall28", 2, DecorationContext.WallAdjacent)
                .Decoration("zep_fence003", 1, DecorationContext.WallAdjacent)
                .Decoration("zep_altarevil3", 2, DecorationContext.RoomCenter)
                .Decoration("zep_fountain002", 2, DecorationContext.RoomCenter)
                .Decoration("zep_altarevil2", 1, DecorationContext.RoomCenter)
                .Decoration("_mdrn_pl_lights2", 2, DecorationContext.DoorwayFlank)
                .Decoration("zep_statues004", 2, DecorationContext.DoorwayFlank)
                .Decoration("zep_fog002", 1, DecorationContext.CorridorSide)
                .Decoration("zep_waterfall001", 1, DecorationContext.CorridorSide)
                .Vignette("HiveThroneAltar", 3)
                .VignetteMember("zep_altarevil2", 0f, 0f)
                .VignetteMember("zep_fountain002", 1.2f, 0.6f)
                .Vignette("HiveMistLight", 2)
                .VignetteMember("zep_fog002", 0f, 0f)
                .VignetteMember("_mdrn_pl_lights2", 0.6f, 0.4f);

            // City Interior 2 / TNO: City Interior (tni01, SWLOR_Haks/sw_t_cityint2). Same
            // Livingroom/Kitchen/Inn/Shop room-type family as the initial City Interior profile (tin01),
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
            _builder.Create(CityInterior2, "City Interior 2*")
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

            // Steamworks' own bulk palette — mined from tsw01 hand-built reference areas
            // (decoration_evidence/evidence_by_tileset.json['tsw01'], 1 area). Strongest
            // co-occurrence pair: _mdrn_pl_brlrad + zep_splat005 (32, a leaking radioactive-barrel
            // spill) -> vignette.
            _builder
                .Decoration("_mdrn_pl_brlrad", 3, DecorationContext.WallAdjacent)
                .Decoration("zep_splat005", 2, DecorationContext.WallAdjacent)
                .Decoration("zep_water001", 2, DecorationContext.CorridorSide)
                .Decoration("_mdrn_pl_debri20", 2, DecorationContext.CorridorSide)
                .Decoration("_mdrn_pl_debri01", 1, DecorationContext.CorridorSide)
                .Decoration("_mdrn_pl_shipp03", 1, DecorationContext.RoomCenter)
                .Decoration("_mdrn_pl_droidd2", 1, DecorationContext.RoomCenter)
                .Decoration("_mdrn_pl_cagebst", 1, DecorationContext.DoorwayFlank)
                .Decoration("_mdrn_pl_cageirn", 1, DecorationContext.DoorwayFlank)
                .Vignette("FoundryRadBarrelSpill", 3)
                .VignetteMember("_mdrn_pl_brlrad", 0f, 0f)
                .VignetteMember("zep_splat005", 0.5f, 0.3f);

            // Fort Interior / TNO: Fort Interior (twc03, SWLOR_Haks/sw_t_fortint). GENERAL declares
            // BOTH Default and Floor as "black" (the same authoring quirk as Barrows) -- PrimaryOpen
            // Terrain is set explicitly to "floor" rather than left empty. AccentTerrain("water")
            // mirrors the Water-channel pattern; only "wall" (a crosser name here, not a terrain) and no
            // canonical Bridge-gated door onto water was found, so no Bridge-crosser SetPiece is wired,
            // matching the tileset's own actual door inventory. Many groups are prefixed "OLD_" (legacy
            // authored content the tileset keeps for back-compat) but still structurally classify as
            // CorridorStub (all-solid-cornered, single Corridor edge, e.g. OLD_Bedroom_01_1x1/
            // OLD_Library_1x1/OLD_Storage_1x1/OLD_Generic_Room_1x1/OLD_Cells_1x1) -- these five USED to
            // be wired per this profile's own precedent of registering structurally-valid pieces
            // regardless of a "legacy"-sounding name, but are REMOVED below: each one's sole
            // floor/entrance tile turned out to be one of twc03's 15 confirmed-placeholder "xyz"-family
            // models (see the PLACEHOLDER ART note further down and FortInteriorLegacy's own doc
            // comment for the full evidence). Their non-"OLD_" *_2x1/*_2x2/*_1x2 replacements (StoreRoom_2x2L,
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
            //
            // PLACEHOLDER ART: ExcludedTiles(...) below declares the 15 "xyz"-family physical tile IDs
            // (twc03_xyz_01 through _15) confirmed to be placeholder-art stubs -- see
            // FortInteriorLegacy's doc comment for the full evidence. FIVE of the "OLD_"-prefixed
            // groups those IDs belong to have no non-"OLD_" replacement at all (a 1x1 room has no
            // "OLD_"/current pair the way the 2x1/2x2/1x2 families do) and so USED to be wired directly
            // here too -- OLD_Bedroom_01_1x1, OLD_Cells_1x1, OLD_Library_1x1, OLD_Storage_1x1,
            // OLD_Generic_Room_1x1 (tiles 46/58/107/111/118) -- and are REMOVED below for the same
            // reason FortInteriorLegacy dropped its whole "OLD_" family. The other ten "OLD_"-prefixed
            // groups (the ones WITH a non-"OLD_" replacement) were never wired in this base profile to
            // begin with (superseded by the replacements above).
            _builder.Create(FortInterior, "TNO: Fort Interior")
                .Tileset("twc03")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .PrimaryOpenTerrain("floor")
                .AccentTerrain("water")
                .DoorSlotCrossers("corridor", "wall")
                .ExcludedTiles(42, 44, 46, 47, 49, 51, 58, 73, 81, 84, 107, 111, 116, 118, 119)
                .SetPiece("Arena_1x2")
                .SetPiece("Storage_1x1_1")
                .SetPiece("Dais_1x2")
                .SetPiece("Stairway_up")
                .SetPiece("Stairway_down")
                .SetPiece("Corr_SpiralStair_updown", 1)
                .SetPiece("Corr_SpiralStair_up", 1)
                .SetPiece("Corr_SpiralStair_down", 1)
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

            // Fort Interior's own bulk palette — mined via mandogarrison keyword match
            // (decoration_evidence/evidence_by_theme_keyword.json — dan_repgarrison/manda_facility/
            // sol_mandaloriani, 3 areas). Strongest structural pairing: bunk beds anchored by a nearby
            // locker -> vignette.
            _builder
                .Decoration("_mdrn_pl_bunkbd5", 3, DecorationContext.WallAdjacent)
                .Decoration("_mdrn_pl_lockerm", 2, DecorationContext.WallAdjacent)
                .Decoration("_mdrn_pl_weaprck", 2, DecorationContext.WallAdjacent)
                .Decoration("_mdrn_pl_wall009", 1, DecorationContext.WallAdjacent)
                .Decoration("_mdrn_pl_deskgry", 1, DecorationContext.RoomCenter)
                .Decoration("_mdrn_pl_couch08", 1, DecorationContext.RoomCenter)
                .Decoration("_mdrn_pl_conta49", 2, DecorationContext.DoorwayFlank)
                .Decoration("_mdrn_pl_ovenold", 1, DecorationContext.DoorwayFlank)
                .Decoration("zep_grasstuft001", 1, DecorationContext.CorridorSide)
                .Decoration("zep_dirt02", 1, DecorationContext.CorridorSide)
                .Vignette("GarrisonBunkLocker", 3)
                .VignetteMember("_mdrn_pl_bunkbd5", 0f, 0f)
                .VignetteMember("_mdrn_pl_lockerm", 0.9f, 0.4f);

            // Fort Interior (Legacy) -- twc03's "OLD_"-prefixed superseded furnished-room family, a
            // PaletteVariant profile recomposing the SAME twc03 hak data the base FortInterior profile
            // above uses. Same solid/open/accent terrain, same base-shape pieces (Arena/Storage/Dais/
            // Stairway/Corr_SpiralStair/Corridor_Exit/Room_1x2/LargeGate/Fireplace/Platform/
            // Mythallar_3x3).
            //
            // PLACEHOLDER ART: every "OLD_"-prefixed furnished-room group this profile used to swap in
            // (OLD_Bedroom_01_1x1, OLD_Cells_1x1, OLD_Library_1x1, OLD_Storage_1x1,
            // OLD_Generic_Room_1x1, OLD_StoreRoom_2x2L_old, OLD_Cells_2x2_old, OLD_Kitchen_1x2,
            // OLD_Generic_Room_2x1/2x2, OLD_Barracks_2x2, OLD_Bedroom_02_2x1, OLD_Bedroom_03_2x1,
            // OLD_Smithy_1x2, OLD_Portal_Hall_2x3) has its floor/entrance tile as one of twc03's 15
            // "xyz"-family models (twc03_xyz_01 through _15, one per group, a 1:1 mapping verified
            // directly against the raw .set group data). Those models are literal Tyrants-of-the-
            // Moonsea (xp3.bif) placeholder stubs: 1.8-4.7KB hand-written ASCII .mdl files whose visible
            // render trimesh has "bitmap NULL" (no texture at all) sitting on top of or instead of the
            // room's floor -- confirmed by dumping the raw model text (see
            // audit_placeholder_art.py in the procedural-areas scratchpad) -- and they render as flat
            // white tiles in-game. All 15 groups are removed here (not just their broken tile) since a
            // furnished room missing its own floor/entrance tile isn't a usable set piece regardless.
            // ExcludedTiles(...) below declares the 15 physical IDs so TileResolver's shared candidate
            // lookup also refuses them if a future change ever makes one reachable outside group
            // stamping; TileCoverageCensusTests' new PlaceholderArtExemptionReason category accounts for
            // these 15 tiles AND their now-unwired sibling group members (e.g. OLD_Bedroom_02_2x1's
            // second tile) instead of counting them "covered". See
            // ExcludedTileRegressionTests for the static group-membership assertion and the
            // Complex-layout placement sweep.
            //
            // Large_Door is NOT wired here either: its TILE36 has mixed floor/black corners and never
            // classifies under any mechanism (see TileCoverageCensusTests.PilotExpectedExemptions).
            // PaletteVariant() excludes this from --matrix's full cross-product -- one showcase area
            // instead.
            _builder.Create(FortInteriorLegacy, "TNO: Fort Interior (Legacy)")
                .Tileset("twc03")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .PaletteVariant()
                .PrimaryOpenTerrain("floor")
                .AccentTerrain("water")
                .DoorSlotCrossers("corridor", "wall")
                .ExcludedTiles(42, 44, 46, 47, 49, 51, 58, 73, 81, 84, 107, 111, 116, 118, 119)
                .SetPiece("Arena_1x2")
                .SetPiece("Storage_1x1_1")
                .SetPiece("Dais_1x2")
                .SetPiece("Stairway_up")
                .SetPiece("Stairway_down")
                .SetPiece("Corr_SpiralStair_updown", 1)
                .SetPiece("Corr_SpiralStair_up", 1)
                .SetPiece("Corr_SpiralStair_down", 1)
                .SetPiece("Corridor_Exit", 1)
                .SetPiece("Room_1x2")
                .SetPiece("LargeGate_1x2")
                .SetPiece("LargeGate_Exit")
                .SetPiece("Fireplace")
                .SetPiece("Platform_1x2_01")
                .SetPiece("Mythallar_3x3")
                .ExitGroup("Exit_1x1")
                .ExitGroup("Exit_Down_1x1")
                .ExitGroup("Exit_CollapsedWall");

            // Desert (ttd01, SWLOR_Haks/sw_t_tatooine -- a 388-tile HasHeightTransition=1 superset of
            // the 212-tile vanilla version; hak copies win TilesetSetSource resolution). See the
            // family-level comment at the Desert/Forest/ForestFacelift constants above for the shared
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
            _builder.Create(Desert, "[SW] Tatooine")
                .Tileset("ttd01")
                // Family AREA atmosphere -- the Tatooine daylight canyon tuple, mined from the
                // hand-built ttd01 exemplars: 20 of 49 module areas agree exactly on the full core
                // tuple (anchor_entreenor/anchor_entreesud/anchor_road_est/canyon_001/
                // moseis_dow_ca001/... -- the runner-up tuple has 5). Desert skybox 77, live
                // day/night cycle, warm sun ambient, light haze fog (10) both phases, strong wind 2.
                // Among the agreeing areas: LightingScheme 6 and ShadowOpacity 50 are unanimous,
                // LoadScreenID 69 (the Tatooine loadscreen) holds on 18/20, FogClipDist 70 is the
                // modal value (12/20).
                .Atmosphere(a =>
                {
                    a.SkyBox = 77;
                    a.DayNightCycle = true;
                    a.IsNight = false;
                    a.SunAmbientColor = 3952475;
                    a.SunDiffuseColor = 7325921;
                    a.MoonAmbientColor = 0;
                    a.MoonDiffuseColor = 132358;
                    a.SunFogAmount = 10;
                    a.SunFogColor = 4890809;
                    a.MoonFogAmount = 10;
                    a.MoonFogColor = 2178364;
                    a.SunShadows = true;
                    a.MoonShadows = true;
                    a.ShadowOpacity = 50;
                    a.WindPower = 2;
                    a.LightingScheme = 6;
                    a.FogClipDist = 70f;
                    a.LoadScreenId = 69;
                })
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
                // Baked-mesh cave-mouth piece (1x1 GROUP, non-flat [Desert 1,1,0,0], crosser-free, one
                // door slot) -- same ReliefPiece kind as "Ramp" above, now door-tolerant (the raised
                // exterior set-piece rule -- shares tdm01 Cave Entrance's exact shape, see
                // LayoutGroupStamper.TryClassifyReliefPiece's own doc comment). Distinct from the
                // ExitGroup("CaveEntrance") flat door-tile family below.
                .SetPiece("SmallCave", 1)
                .ExitGroup("Exit")
                .ExitGroup("CliffStairs")
                .ExitGroup("ChasmStairs")
                .ExitGroup("CaveEntrance");

            // Desert's own bulk palette — mined from ttd01 hand-built reference areas
            // (decoration_evidence/evidence_by_tileset.json['ttd01'], 49 areas — the richest sample of
            // any registered family). Strongest co-occurrence pairs among the desert-scrub family
            // (nw_plc_kelp*, a desert scrub/weed reskin) -> vignette.
            _builder
                .Decoration("_mdrn_pl_bldstn", 3, DecorationContext.WallAdjacent)
                .Decoration("zep_boulder003", 2, DecorationContext.WallAdjacent)
                .Decoration("_mdrn_pl_df_wseb", 2, DecorationContext.WallAdjacent)
                .Decoration("_mdrn_pl_bldstn", 1, DecorationContext.WallAdjacent)
                .Decoration("zep_shrub036", 2, DecorationContext.CorridorSide)
                .Decoration("zep_bushfern001", 2, DecorationContext.CorridorSide)
                .Decoration("zep_boulder003", 1, DecorationContext.CorridorSide)
                .Decoration("zep_tree070", 1, DecorationContext.RoomCenter)
                .Decoration("zep_boulder003", 1, DecorationContext.RoomCenter)
                .Decoration("zep_arch003", 2, DecorationContext.DoorwayFlank)
                .Decoration("_mdrn_pl_lights2", 2, DecorationContext.DoorwayFlank)
                .Vignette("DesertScrubCluster", 3)
                .VignetteMember("zep_boulder003", 0f, 0f)
                .VignetteMember("zep_boulder003", 0.6f, 0.4f);

            // Desert (Road) -- ttd01's second raised-lane crosser family (see the ttf01 family-level
            // comment below for the shared "one RampCrosser slot
            // per composition" argument this mirrors). The base Desert profile above declares
            // RampCrosser("Dunes"); TILE239-241 are raised, ungrouped, doorless Road-edged lanes (all
            // pure-Desert-cornered) that stay height-exempt under that vocabulary because
            // IsTerrainReliefReachable requires every non-blank edge to equal the declared Ramp name.
            // Direct probe (mirroring IsTerrainReliefReachable) confirms all 3 resolve under
            // RampCrosser("Road"). TILE255 (Dunes AND Road edges on the SAME tile) stays exempt: a
            // dual-crosser conflict no single composition can express, the same shape as ttf01's
            // TILE606-609 (Slope+Road). PaletteVariant() excludes this from --matrix's full
            // cross-product -- one showcase area.
            _builder.Create(DesertRoad, "[SW] Tatooine (Road)")
                .Tileset("ttd01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .PaletteVariant()
                .SolidTerrainOverride("Cliff")
                .PrimaryOpenTerrain("Desert")
                .MaxReliefRegions(2)
                .RampCrosser("Road");

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
            // name, "Slope"); (c) raised Bridge/StoneBridge banks (TILE895/896/898); (d) the 2x2 "City
            // Gate - Forest/Cobbles" GROUPS: a genuine "2-wide wall mass" shape (both footprint columns
            // independently touching the CityWall lane network, plus a shared interior seam)
            // LayoutReliefPainter.TrySpliceReliefLane can never produce -- it only ever carves a lane
            // exactly ONE cell wide along a single axis (see that method's own doc comment), so no
            // painted field can ever match a 2-column mass; left exempt, documented rather than
            // engineered further during raised exterior support work. "Cave" (raised AND door-bearing,
            // the tdm01 Cave Entrance shape) and the 1x1 "Wall - Breach/Door/Tower 1/2,
            // City/Forest,Water,Cobbles"/"Ramp - City Wall"/"Ramp - Moss Wall"/"Wall - Breach/Door,
            // Moss" family are now CLOSED: LayoutGroupStamper.TryClassifyReliefPiece tolerates a door
            // slot (never spawns one, matching WallAlcove's own precedent) and an edge matching the
            // composition's own declared RampCrosser (matching IsTerrainReliefReachable's ungrouped-
            // tile rule) -- see that method's own doc comment and the SetPiece wiring below and on the
            // ForestCityWall/ForestMossWall PaletteVariant profiles.
            //
            // Alternate palettes (formerly auto-exempted via PilotAlternateVocabTerrains -- see that
            // dictionary's own doc comment for the census-vs-practice writeup for each):
            // RuralTrees/RuralWater are MOSTLY closed by BaseGameTilesetProfiles.ForestRural's
            // AccentTerrain/ReliefBlendTerrain variant (PoolBank/TerrainRelief, verified directly and
            // via a real-generation placement proof). Platform and HighForest blend only 2/16 against
            // Forest, and HighForest only 2/16 against Cliff too -- but Platform reaches 16/16 against
            // Cliff AND 16/16 against Pit, and HighForest also reaches 16/16 against Pit (verified
            // directly by 16-combo probe): see the "Forest (Platform)" PaletteVariant below, which
            // declares SolidTerrainOverride("Pit") + PrimaryOpenTerrain("Platform") to close the
            // Platform GROUPS that need a Solid+Open pair covering Pit and Platform simultaneously
            // (every ungrouped Platform/HighForest-cornered simple tile was ALREADY
            // CornerEdgeResolver-reachable regardless of vocab, so only the groups needed a dedicated
            // variant; a dedicated HighForest variant would add no additional coverage since no group
            // uses HighForest corners). Still exempt after that variant, still terrain-listed in
            // PilotAlternateVocabTerrains: "Platform - Cliff Section" (genuinely three terrains,
            // Platform+Cliff+Pit, on one group -- no two-terrain classifier reaches it, and its 3x2
            // footprint disqualifies it from IsExitGroupEligible too). "Platform - Cliff Door" is NOT
            // also exempt (a prior pass's comment here was WRONG, re-verified directly and fixed on
            // ForestPlatform's own doc comment): despite mixing Platform+Cliff, it already satisfies
            // IsExitGroupEligible's vocab-independent structural rule (any flat, crosser-free,
            // door-bearing 1x1 group), the same shape as the GoodCastle/EvilCastle door groups below.
            //
            // GoodCastle/EvilCastle/Marsh are CONFIRMED DEAD entries, not real gaps: re-probing found
            // GoodCastle and EvilCastle each reach full 16/16 flat corner coverage against
            // Solid=<faction>Castle/Open=Forest (a genuine alternate wall-material palette, same shape
            // as the base Cliff/Forest pair), and every touching tile was ALREADY counted as reachable
            // regardless -- the ~10 ungrouped simple tiles per faction via CornerEdgeResolver
            // (vocab-independent), and the three 1x1 door/breach GROUPS per faction
            // ("Castle - Main Door/Small Door/Breach, Good/Evil") via IsExitGroupEligible (also
            // vocab-independent: any flat, crosser-free, door-bearing 1x1 group qualifies as exit-group
            // candidate content regardless of its corners' terrain). Verified directly: removing
            // GoodCastle/EvilCastle from PilotAlternateVocabTerrains changes ttf01's census numbers not
            // at all. Marsh reaches 14/16 against Forest (missing both blank-edge DIAGONAL two-terrain
            // splits, the same shape ForestRural's own doc comment documents for RuralWater/RuralTrees)
            // but was likewise never a real gap: its entire real .set inventory is 11 flat, ungrouped,
            // crosser-free simple tiles (TILE838-848), none of which use the missing diagonal shape,
            // all already CornerEdgeResolver-reachable regardless of vocab -- verified directly,
            // removing "Marsh" from the dictionary changes nothing either.
            //
            // The structural "reachable regardless" finding above only means the CENSUS was never
            // blocked by these three terrains -- under the BASE profile's Solid=Cliff/Open=Forest
            // composition, none of GoodCastle/EvilCastle/Marsh is ever actually painted into a real
            // corner grid, so none of this content ever actually APPEARED in generated areas. Three
            // dedicated PaletteVariant profiles below (Forest (Good Castle)/(Evil Castle)/(Marsh))
            // recompose the same base pair with each district terrain playing a real structural role
            // (SolidTerrainOverride for the two castle palettes, AccentTerrain for Marsh) so this
            // content is genuinely reachable in real generation, not just census-credited -- see each
            // profile's own doc comment for its placement-proof evidence. Marsh's own diagonal-split
            // gap is a real, accepted residual under this variant too (LayoutAccentPainter.GrowBlob
            // only ever grows one 4-connected region per blob-painting pass, but PaintAccents calls it
            // repeatedly per composition with a fresh random seed each time, and CanAccept's solid-only
            // adjacency guard does not prevent two INDEPENDENT blob passes from landing on
            // diagonally-adjacent corners -- so the shape CAN be produced by the painter, it just has
            // no matching real tile, exactly ForestRural's own already-accepted RuralWater/RuralTrees
            // diagonal residual, tolerated the same way by the existing seed-retry pipeline).
            //
            // Unwired crosser families (PilotAlternateVocabCrossers): DlaEdgeFix, StoneBridge,
            // RuralStream, MossWall, CityWall, RuinWall, RuralWallOne/Two -- their flat door-free tiles
            // all resolve via CornerEdgeResolver regardless; the entries exempt the few flat door/group
            // tiles (e.g. "Bridge - Footbridge, Rural Stream", "Wall - Gate, Ruin").
            //
            // FeatureTile curation: semantic/functional tiles are deliberately NOT sprinkled --
            // "Portal - Forest"/"Portal - Platform" (teleporters), "Entrance - Dungeon" (a transition
            // mouth), "Platform - Elevator, Upper/Lower" (paired elevators) are wired as maxPerArea-1
            // set pieces instead ("Portal - Platform" and the elevators are additionally
            // Platform-palette content and stay unwired entirely).
            _builder.Create(Forest, "Forest*")
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
                // Baked-mesh cave-mouth piece (1x1 GROUP, non-flat [Forest 1,1,0,0], crosser-free, one
                // door slot) -- same ReliefPiece kind as "Ramp" above, now door-tolerant (the raised
                // exterior set-piece rule -- shares tdm01 Cave Entrance's exact shape).
                .SetPiece("Cave", 1)
                .ExitGroup("Exit")
                .ExitGroup("Stairs - Cliff")
                .ExitGroup("Stairs - Pit")
                .ExitGroup("House - Small 1")
                .ExitGroup("House - Small 2")
                .ExitGroup("House - Small 3")
                .ExitGroup("House - Turf")
                .ExitGroup("House - Ruined")
                .ExitGroup("Tower - Stone");

            // Forest's own bulk palette — mined from ttf01 hand-built reference areas
            // (decoration_evidence/evidence_by_tileset.json['ttf01'], 1 area — sparse dathomirwild
            // sample). Strongest co-occurrence pair: _mdrn_pl_campfre + plc_flamemedium (2) ->
            // vignette.
            _builder
                .Decoration("_mdrn_pl_mtlhut4", 2, DecorationContext.WallAdjacent)
                .Decoration("_mdrn_pl_debri03", 1, DecorationContext.WallAdjacent)
                .Decoration("zep_pinetr6", 2, DecorationContext.CorridorSide)
                .Decoration("_mdrn_pl_campfre", 2, DecorationContext.RoomCenter)
                .Decoration("zep_bflame002", 1, DecorationContext.RoomCenter)
                .Decoration("zep_skinpole001", 2, DecorationContext.DoorwayFlank)
                .Vignette("CampFirePit", 2)
                .VignetteMember("_mdrn_pl_campfre", 0f, 0f)
                .VignetteMember("zep_bflame002", 0.5f, 0f);

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
            // rule). STALE-COMMENT FIX (re-verified directly, same class of bug as the GoodCastle/
            // EvilCastle/Marsh dead-entry finding above): "Platform - Cliff Door" (TILE966,
            // [Platform,Cliff,Cliff,Platform], ONE door slot, crosser-free, 1x1) was NEVER actually
            // exempt -- it already satisfies IsExitGroupEligible's vocab-independent structural rule
            // (any flat, crosser-free, door-bearing 1x1 group) the same way the Castle door groups do,
            // so it was already classified as an ExitGroup candidate under the census's own permissive
            // definition even before this variant existed. Only "Platform - Cliff Section (2x3)"
            // (TILE949-954, genuinely THREE terrains -- Platform, Cliff, AND Pit -- on one group's
            // members; ClassifySetPiece's matchesPrimary/matchesSecondary each only ever admit a
            // Solid+ONE-other-terrain pair, never three simultaneously, so no single profile
            // composition can close a true three-terrain group, and its own 3x2 footprint disqualifies
            // it from IsExitGroupEligible's 1x1-only rule too) stays genuinely exempt. "Portal - Platform"/"Platform
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
            _builder.Create(ForestPlatform, "Forest* (Platform)")
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
            _builder.Create(ForestRural, "Forest* (Rural)")
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

            // Forest (Good Castle) / Forest (Evil Castle) -- ttf01's two "district" wall-material
            // palettes (see BaseGameTilesetProfiles.Forest's own doc comment, "GoodCastle/EvilCastle"
            // note). Direct 16-combo probe (TileResolver.HasCandidate over all 16 flat corner masks,
            // both orientations) confirms GoodCastle and EvilCastle EACH reach full 16/16 flat corner
            // coverage against Solid=<faction>Castle/Open=Forest (equivalently Solid=Forest/
            // Open=<faction>Castle -- the pairing is symmetric): each is a genuine alternate WALL
            // material, structurally identical in shape to the base profile's own Solid=Cliff/
            // Open=Forest pair, just recomposed with the castle terrain playing Cliff's role. Neither
            // blends with Cliff or Pit at all (2/16 each, only the two uniform-corner masks survive).
            //
            // The tileset's own castle inventory is exactly three 1x1 GROUPS per faction (no bulk wall-
            // lane family exists, unlike CityWall/MossWall): "Castle - Main Door", "Castle - Small
            // Door", "Castle - Breach" (TILE671/673/674 Good, TILE662/667/670 Evil), each a single tile
            // with mixed Forest/<faction>Castle corners (a vertical two-open/two-solid split) plus a
            // door slot and NO crosser edge. This shape already satisfies IsExitGroupEligible's
            // structural rule (any flat, crosser-free, door-bearing 1x1 group -- vocab-independent), so
            // the census NEVER actually exempted these six tiles even before this profile existed (see
            // BaseGameTilesetProfiles.Forest's own doc comment). But GroupExitPlanner's REAL placement
            // pass requires an exact corner-terrain match against the composition's own painted grid,
            // and the base profile's Solid=Cliff/Open=Forest pair never paints <faction>Castle
            // anywhere -- so under the base profile alone this content is census-credited but never
            // actually generated. This variant's SolidTerrainOverride makes the castle terrain a real
            // wall material, so its corners genuinely appear in the grid and GroupExitPlanner can place
            // them. Wired as ExitGroups (matching "Castle - Main/Small Door"/"Breach"'s own gate-in-a-
            // wall semantics, the same family as the base profile's "Exit"/"House - Small 1-3"/
            // "Tower - Stone"), not SetPieces -- IsExitGroupEligible/ExitGroup classification already
            // wins priority over SetPiece classification for this exact shape (see
            // TileCoverageCensusTests' own mechanism-priority ordering), so a SetPiece registration
            // would be dead code. Real-generation placement proof: OpenSetPiecePlacementRateTests'
            // GoodEvilCastleDoorGroups_PlaceAsGroupExits (Halls composition, both factions, isolated
            // ExitGroup measured across 150 seeds each -- 150/150 for all six groups).
            // PaletteVariant() excludes each from --matrix's full cross-product -- one showcase area
            // apiece.
            _builder.Create(ForestGoodCastle, "Forest* (Good Castle)")
                .Tileset("ttf01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .PaletteVariant()
                .SolidTerrainOverride("GoodCastle")
                .PrimaryOpenTerrain("Forest")
                .ExitGroup("Castle - Main Door, Good")
                .ExitGroup("Castle - Small Door, Good")
                .ExitGroup("Castle - Breach, Good");

            _builder.Create(ForestEvilCastle, "Forest* (Evil Castle)")
                .Tileset("ttf01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .PaletteVariant()
                .SolidTerrainOverride("EvilCastle")
                .PrimaryOpenTerrain("Forest")
                .ExitGroup("Castle - Main Door, Evil")
                .ExitGroup("Castle - Small Door, Evil")
                .ExitGroup("Castle - Breach, Evil");

            // Forest (Marsh) -- ttf01's unwired Marsh ground-cover district (see
            // BaseGameTilesetProfiles.Forest's own doc comment, "GoodCastle/EvilCastle/Marsh" note), a
            // PaletteVariant profile recomposing the SAME ttf01 hak data the base Forest profile uses.
            // Direct probe (TileResolver.HasCandidate over all 16 flat corner masks) confirms Marsh
            // reaches 14/16 against Solid=Forest -- i.e. it is a walkable "ground cover" terrain layered
            // onto Forest, the SAME role RuralWater/RuralTrees play for ForestRural -- missing only the
            // two blank-edge DIAGONAL two-terrain splits (Forest/Marsh/Forest/Marsh and its rotation),
            // which no real ttf01 tile uses anyway (Marsh's entire real inventory is 11 flat, ungrouped,
            // crosser-free simple tiles, TILE838-848, all single-corner or uniform-corner shapes). Marsh
            // never blends with Cliff or Pit at all (2/16 each).
            //
            // Wired as a plain flat AccentTerrain (LayoutAccentPainter's blob-patch pass) rather than a
            // ReliefBlendTerrain/ChannelTerrain like RuralWater/RuralTrees -- Marsh's own tile family
            // carries no raised bank/relief shapes, so no MaxPoolRegions/MaxReliefRegions declaration is
            // needed (matching the class doc comment's "e.g. Water pools, Pit channels" flat-patch
            // description). Real-generation placement proof: LayoutReliefPainterTests'
            // RealForestMarshComposition_PaintsMarshAccentPatches (Halls composition, many seeds, at
            // least one Marsh corner actually painted by the production painter).
            //
            // The missing diagonal-split combo is a real, accepted residual, not a provably-excluded
            // one: LayoutAccentPainter.PaintAccents calls GrowBlob repeatedly per composition (a fresh
            // random seed each pass), and GrowBlob's 4-connected growth only guarantees connectivity
            // WITHIN one blob -- CanAccept's adjacency guard only excludes touching the SOLID terrain,
            // never an existing accent corner from an earlier pass, so two independent blob passes CAN
            // legally land on diagonally-adjacent corners. This is the exact same shape ForestRural's
            // own doc comment already documents and accepts for RuralWater/RuralTrees (verified there
            // directly against real tile data, e.g. TILE512/528/543/896) -- tolerated by the existing
            // seed-retry pipeline (an occasional unresolvable cell fails that seed's attempt, not the
            // whole composition) rather than requiring a structural guard.
            // PaletteVariant() excludes this from --matrix's full cross-product -- one showcase area.
            _builder.Create(ForestMarsh, "Forest* (Marsh)")
                .Tileset("ttf01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .PaletteVariant()
                .SolidTerrainOverride("Cliff")
                .PrimaryOpenTerrain("Forest")
                .AccentTerrain("Marsh");

            // Forest raised-lane crosser families -- ttf01's base
            // Forest profile above and ForestRural both declare RampCrosser("Slope"), but a
            // composition carries only ONE RampCrosser slot (LayoutReliefPainter.TrySpliceReliefLane
            // writes a single declared crosser name per composition -- see MinesAndCavernsTracks'
            // identical "second family needs its own profile" precedent for Tunnel body crossers).
            // ttf01 carries SEVEN more raised-lane crosser families beyond Slope: CityWall, MossWall,
            // RuralWallOne, RuralWallTwo, RuralStream, Road, and StoneBridge, each with its own
            // ungrouped, doorless, one-story raised tile family (e.g. TILE741-775/801-804 for
            // CityWall) previously stuck in the "requires height support" bucket because
            // IsTerrainReliefReachable requires every non-blank edge to equal the composition's
            // declared Ramp name. One dedicated PaletteVariant per family, each recomposing the same
            // base Solid=Cliff/Open=Forest pair (+ AccentTerrain("RuralWater") where the family's
            // raised tiles mix in RuralWater corners, verified per-family below), closes each family's
            // ungrouped raised lanes the same way ForestRural closed the Slope/RuralWater case.
            //
            // Every family was shape-probed first (direct IsTerrainReliefReachable BFS probe against
            // this tileset's real data, mirroring the census's own mechanism) before being wired here;
            // numbers below are the probe's actual counts, not estimates. Remaining gaps after each
            // variant are genuinely unmodeled, not missed:
            //   - the 1x1 raised GROUPS on these families (Wall - Breach/Door/Tower 1/2,
            //     City/Forest,Water,Cobbles; Ramp - City Wall/Moss Wall; Wall - Breach/Door, Moss) are
            //     now CLOSED by the raised exterior set-piece rule: LayoutGroupStamper.TryClassifyReliefPiece
            //     tolerates a door slot and an edge matching the composition's own declared RampCrosser
            //     -- see that method's own doc comment and the SetPiece wiring below. Only the 2x2
            //     "City Gate - Forest/Cobbles" GROUPS stay height-exempt: a genuine "2-wide wall mass"
            //     shape LayoutReliefPainter.TrySpliceReliefLane can never paint (it only ever carves a
            //     lane exactly one cell wide) -- see the Forest base profile's own doc comment for the
            //     measured 0/450-seed placement-rate evidence.
            //   - dual-crosser cells (TILE606-609, Slope AND Road edges on the SAME tile) stay exempt:
            //     IsTerrainReliefReachable requires every edge to match ONE declared Ramp name, and a
            //     composition can never declare two Ramp crossers at once -- a genuine "crossroads
            //     cell" gap, the same shape as the two-crosser-family crossroads GATE groups elsewhere
            //     in this tileset (WallGate/StreamBridge-style, unrelated mechanism).
            //   - TILE747 (CityWall, corners [RuralWater,Forest,RuralWater,Forest] -- a diagonal,
            //     NON-adjacent two-terrain split) stays exempt: the same diagonal-split gap ForestRural
            //     already documented for TILE512/528/543/896 (IsReliefFieldReachable's BFS never finds
            //     a resolving intermediate chain for a diagonal split, only adjacent-corner splits).
            // PaletteVariant() excludes each from --matrix's full cross-product -- one showcase area
            // apiece.

            // Forest (City Wall) -- closes CityWall's 31 raised ungrouped lanes (TILE741-744/759/763-
            // 765/772-775/801-804, pure-Forest-cornered, 16 tiles; TILE745/746/748-754/766-771,
            // RuralWater-mixed, 15 more via AccentTerrain("RuralWater")) of 49 total CityWall-edged
            // tiles. Verified directly via probe. Also wires the family's 1x1 raised GROUPS through
            // the raised exterior set-piece rule: "Ramp - City Wall" (doorless), and the door-bearing "Wall -
            // Breach/Door/Tower 1/2, City/Forest,Water,Cobbles" family -- all now reachable via
            // LayoutGroupStamper.TryClassifyReliefPiece's door + RampCrosser tolerance (see that
            // method's own doc comment). "Wall - Tower 1/2, City/Water" mix RuralWater corners, closed
            // by this profile's own AccentTerrain("RuralWater").
            _builder.Create(ForestCityWall, "Forest* (City Wall)")
                .Tileset("ttf01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .PaletteVariant()
                .SolidTerrainOverride("Cliff")
                .PrimaryOpenTerrain("Forest")
                .AccentTerrain("RuralWater")
                .MaxReliefRegions(2)
                .RampCrosser("CityWall")
                .SetPiece("Ramp - City Wall", 1)
                .SetPiece("Wall - Breach, City/Forest", 1)
                .SetPiece("Wall - Door, City/Forest", 1)
                .SetPiece("Wall - Tower 1, City/Forest", 1)
                .SetPiece("Wall - Tower 2, City/Forest", 1)
                .SetPiece("Wall - Tower 1, City/Water", 1)
                .SetPiece("Wall - Tower 2, City/Water", 1)
                .SetPiece("Wall - Breach, City/Cobbles", 1)
                .SetPiece("Wall - Door, City/Cobbles", 1)
                .SetPiece("Wall - Tower 1, City/Cobbles", 1)
                .SetPiece("Wall - Tower 2, City/Cobbles", 1);

            // Forest (Moss Wall) -- closes MossWall's 11 raised ungrouped lanes (TILE814-824, all
            // pure-Forest-cornered -- no RuralWater mixing on this family, verified directly) of 14
            // total MossWall-edged tiles. Also wires the family's 1x1 raised GROUPS through the
            // raised exterior set-piece rule: "Ramp - Moss Wall" (doorless) and the door-bearing "Wall -
            // Breach/Door, Moss" pair -- see the City Wall profile's own comment above for the
            // mechanism (LayoutGroupStamper.TryClassifyReliefPiece's door + RampCrosser tolerance).
            _builder.Create(ForestMossWall, "Forest* (Moss Wall)")
                .Tileset("ttf01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .PaletteVariant()
                .SolidTerrainOverride("Cliff")
                .PrimaryOpenTerrain("Forest")
                .MaxReliefRegions(2)
                .RampCrosser("MossWall")
                .SetPiece("Ramp - Moss Wall", 1)
                .SetPiece("Wall - Breach, Moss", 1)
                .SetPiece("Wall - Door, Moss", 1);

            // Forest (Rural Wall One) -- closes all 4 RuralWallOne-edged raised tiles (TILE724-726
            // pure-Forest, TILE727 RuralWater-mixed via AccentTerrain). TILE776-779/812 (RuralWallOne
            // edges paired with a SECOND crosser, CityWall, on the same tile) are a dual-crosser
            // conflict, same shape/gap as TILE606-609's Slope+Road conflict above.
            _builder.Create(ForestRuralWallOne, "Forest* (Rural Wall One)")
                .Tileset("ttf01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .PaletteVariant()
                .SolidTerrainOverride("Cliff")
                .PrimaryOpenTerrain("Forest")
                .AccentTerrain("RuralWater")
                .MaxReliefRegions(2)
                .RampCrosser("RuralWallOne");

            // Forest (Rural Wall Two) -- closes all 4 RuralWallTwo-edged raised tiles (TILE728-730
            // pure-Forest, TILE731 RuralWater-mixed via AccentTerrain). Verified directly.
            _builder.Create(ForestRuralWallTwo, "Forest* (Rural Wall Two)")
                .Tileset("ttf01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .PaletteVariant()
                .SolidTerrainOverride("Cliff")
                .PrimaryOpenTerrain("Forest")
                .AccentTerrain("RuralWater")
                .MaxReliefRegions(2)
                .RampCrosser("RuralWallTwo");

            // Forest (Rural Stream) -- closes all 4 RuralStream-edged raised tiles (TILE719-721
            // pure-Forest, TILE722 RuralWater-mixed via AccentTerrain). The family's remaining
            // RuralStream-edged tiles (TILE850/865-871/881/882) are all flat -- already
            // CornerEdgeResolver-reachable regardless of this variant, unaffected either way.
            _builder.Create(ForestRuralStream, "Forest* (Rural Stream)")
                .Tileset("ttf01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .PaletteVariant()
                .SolidTerrainOverride("Cliff")
                .PrimaryOpenTerrain("Forest")
                .AccentTerrain("RuralWater")
                .MaxReliefRegions(2)
                .RampCrosser("RuralStream");

            // Forest (Road) -- closes 6 of Road's raised ungrouped lanes (TILE530-532 pure-Forest,
            // TILE732-734 RuralWater-mixed via AccentTerrain); the other Road-edged raised tiles
            // (TILE606-609) carry a second crosser (Slope) on the SAME tile, the dual-crosser gap
            // documented above. Every other Road-edged tile in the family is flat -- already
            // CornerEdgeResolver-reachable regardless of this variant (TILE849/1114's RuralWater/
            // RuralTrees door tiles are additionally closed by ForestRural's own AccentTerrain).
            _builder.Create(ForestRoad, "Forest* (Road)")
                .Tileset("ttf01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .PaletteVariant()
                .SolidTerrainOverride("Cliff")
                .PrimaryOpenTerrain("Forest")
                .AccentTerrain("RuralWater")
                .MaxReliefRegions(2)
                .RampCrosser("Road");

            // Forest (Stone Bridge) -- closes both raised StoneBridge-edged tiles (TILE896/898, both
            // RuralWater-mixed, via AccentTerrain). TILE897 (flat, all-RuralWater-cornered,
            // door-free) is unaffected by this variant either way -- already CornerEdgeResolver-
            // reachable regardless of RampCrosser vocabulary.
            _builder.Create(ForestStoneBridge, "Forest* (Stone Bridge)")
                .Tileset("ttf01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 8, 8)
                .PaletteVariant()
                .SolidTerrainOverride("Cliff")
                .PrimaryOpenTerrain("Forest")
                .AccentTerrain("RuralWater")
                .MaxReliefRegions(2)
                .RampCrosser("StoneBridge");

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

            // D20 Futuristic City SW (fcx01) -- Cobble ("b_"/unprefixed) district. See the FutCity/
            // FutCityPlaza doc comment above for the full probe writeup (solid/open choice, crosser
            // vocabulary, hand-built evidence, lighting sample).
            _builder.Create(FutCity, "D20 Futuristic City SW")
                .Tileset("fcx01")
                // Family AREA atmosphere -- the hand-built flagship's neon night-city .are tuple,
                // taken verbatim from pw_ar_narpromena (Smuggler's Moon - Promenade), the reference
                // area this family's whole dressing/frontage pipeline benchmarks against. fcx01's
                // module-wide modal tuple is a 2-2 tie (24 areas span cloudscape exteriors, landing
                // pads, and promenade streets), so the family deliberately standardizes on the
                // flagship rather than a modal vote: skybox 78 locked to permanent night
                // (DayNightCycle 0 / IsNight 1) with BRIGHT white sun-slot diffuse and warm ambient
                // -- the neon-city look is "night flag with day-grade lighting", not a dark area --
                // plus violet moon ambient/diffuse, fog amounts 0 with distinct warm/deep-blue fog
                // colors, both shadow flags on, no wind, FogClipDist 130.
                .Atmosphere(a =>
                {
                    a.SkyBox = 78;
                    a.DayNightCycle = false;
                    a.IsNight = true;
                    a.SunAmbientColor = 6566450;
                    a.SunDiffuseColor = 16777215;
                    a.MoonAmbientColor = 5987195;
                    a.MoonDiffuseColor = 5987248;
                    a.SunFogAmount = 0;
                    a.SunFogColor = 9535080;
                    a.MoonFogAmount = 0;
                    a.MoonFogColor = 2368329;
                    a.SunShadows = true;
                    a.MoonShadows = true;
                    a.ShadowOpacity = 50;
                    a.WindPower = 0;
                    a.LightingScheme = 0;
                    a.FogClipDist = 130f;
                })
                .SolidTerrainOverride("holes")
                .PrimaryOpenTerrain("Cobble")
                // "holes" renders as a bottomless chasm drop, so frontage buildings obey the mined
                // footprint-support envelope against the resolved corner plan (the frontage support
                // audit over the 19 hand-built fcx01
                // city areas: platform-level towers keep in-grid chasm footprint share <= 0.36 and
                // in-grid chasm overhang <= 9m, while off-grid rim overhang is free) -- see
                // FrontageSupportRule. Inherited by the Cobble2 plaza variant like the frontage
                // pool itself.
                .ChasmTerrain("holes")
                // PathNodeOpeningWidthAudit (fresh against fcx01's real pathnode data, Solid=holes/
                // Open=Cobble) computes 2, not the default 1 -- locked in by registered-tileset pipeline coverage.
                // MinimumOpeningWidth_MatchesFreshPathNodeAudit.
                .MinimumOpeningWidth(2)
                // Tower00 (2x2) needs rooms of corner size 6+; Tower02/Tower03 (3x3) and Tower05 (4x3)
                // need 7+ (footprint + 1-cell margin ring + a spare center-relocation tile all inside
                // ONE room -- see DungeonTilesetProfile.SetPieceRoomCornerFloor). Without this floor,
                // Complex-paired city compositions physically cannot stamp ANY multi-tile tower
                // (measured 0/460 seeds at sizes 16-24: Complex's MaxRoomCornerSize=5 caps rooms at
                // 4x4 tiles), leaving generated cities at a 0.0 group-tile share against the hand-built
                // fcx01 reference's 0.152 -- the largest tile-composition divergence measured. 7 is the
                // machinery's own vanilla default room ceiling (MacroLayoutParameters.MaxRoomCornerSize),
                // so this is well-exercised territory; Tower07 (6x6) and b_platform (5x6) stay
                // physically out of reach regardless (8x8 extended footprints exceed any room a
                // 16-24-tile area realistically carves) -- kept configured for oversized future areas.
                .SetPieceRoomCornerFloor(7)
                .DoorSlotCrossers("murs")
                // RoadVocabularyCheck.SupportsRoads(fcx01, "Cobble", "Routes") verified true: TILE207
                // (stub), TILE210 (straight), TILE207 (turn; same physical tile, TileResolver's own
                // rotation search covers both), TILE208 (T), TILE209 (X) are all uniformly Cobble-
                // cornered, PathNode=A, doorless -- see LayoutRoadCarver/RoadVocabularyCheck's own doc
                // comments. Was PilotAlternateVocabCrossers-exempt ("Routes ... have no wired body/port
                // ... vocabulary in this profile family", see this profile's own header comment) before this pass.
                .RoadCrosser("Routes")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .FeatureTile("b_arbre")
                .FeatureTile("b_arbre2")
                // Zone-marking feature tiles obligate a composed ensemble (the "a park with no park"
                // fix -- see FeatureZoneDressing/DungeonDecorationPlanner.PlanZoneDressings):
                // the flat grass lawn composes a PARK (tree/monument + facing bench ring); the
                // fountain court composes its seating surround. Tree/water tiles fill their own
                // cell and stay untouched.
                .FeatureTile("b_herbe", dressing: FeatureZoneDressing.Lawn)
                .FeatureTile("b_fountain", dressing: FeatureZoneDressing.Centerpiece)
                .FeatureTile("b_water")
                // Tower00 is the only fcx01 Cobble-district group that fits the rooms a size-20-24
                // city area actually carves (2x2; the 3x3+ towers need corner-size-7 rooms, which
                // LayoutParameterConstraints.RoomSizeBounds only allows at size 21+ -- see
                // SetPieceRoomCornerFloor above). Budget 3 raises the realized group-tile share toward
                // the hand-built 0.152 reference: measured at size 20, budget 1 placed 4 group tiles
                // on 9/10 seeds (share ~0.01); the whole-area ceiling is site-limited (each stamp
                // consumes a 4x4-tile rectangle + margin inside one corner-size-6 room), so the budget
                // is set above the observed per-area site count rather than exactly at it.
                //
                // Re-measured at size 32 over 20 seeds with a before/after budget-only sweep: raising
                // every group's budget substantially (Tower00 3->6, Tower02/03 1->5,
                // etc.) moved group_share from 0.0398 to 0.0393 -- statistically flat, and per-seed
                // counts show REDISTRIBUTION (some seeds up, some down) rather than growth, because
                // Stamp's own loop (see its doc comment above) breaks a group's attempts on the FIRST
                // failed site search -- budget past the real site ceiling is pure waste, and pulling an
                // early-alphabetized group's (Tower* sorts before b_*/d_* under StringComparer.Ordinal)
                // extra successful attempts consumes room supply a later group would otherwise have
                // used. The real size-32 ceiling is ROOM COUNT, not budget: DECORGEN_DEBUG showed a
                // flat 8 rooms/area regardless of seed, because StandardLayoutProfiles.Packed's own
                // MinRoomCornerSize/MaxRoomCornerSize (3/6) is overridden by SetPieceRoomCornerFloor(7)
                // to 5/7 -- the SAME floor that lets a tower fit AT ALL also shrinks how many rooms a
                // 32x32 area's BSP split produces, since a bigger room-size floor means fewer splits.
                // Budgets are left at their original size-20-tuned values (no measured benefit to
                // raising them); the room-supply ceiling is instead lifted by the
                // SetPieceRoomSupplyScaling declaration below, which scales the room COUNT and SIZE
                // envelope with area above the 20x20 tuning baseline (Complex/Halls hardcode
                // MinRooms=6/MaxRooms=9 regardless of area size; PackedRooms reports at most MaxRooms
                // of its BSP leaves to the stamper) and switches stamping to largest-footprint-first
                // (see LayoutGroupStamper.Stamp) so the big-room supply the envelope creates is
                // claimed by the big tower groups before 2x2 infill fragments it. Measured at 32x32
                // (32 seeds, 0 solve failures, July 2026 city-density pass): group share 0.061 (room
                // supply flat, budgets already scaled) -> 0.150 vs the hand-built 0.152 reference --
                // Tower07 (6x6) and Tower05 (4x3) only become placeable at all once corner-size-9/10
                // rooms exist. See LayoutParameterConstraints.ApplySetPieceRoomSupplyScaling for the
                // derivation and the size-32 tile-composition audit summarized here.
                .SetPieceRoomSupplyScaling()
                // Hand-built tile-built fcx01 city areas assemble their towers into CONTIGUOUS blocks
                // (24-48 adjoined tiles spanning several groups at 0.17-0.28 building share, dominated
                // by same-group self-tiling with corner-agreeing seams -- measured by the street-
                // canyon benchmark over ns_comrcial_ka/
                // pw_ar_nsshipyard/vrotrnsslums/narshadaar_promi), while isolated-margin stamping
                // plateaued at 0.145 mean share with blocks capped at single-group footprints. Every
                // Cobble-district tower group's perimeter is uniformly Cobble-cornered and crosser-
                // free (probe_handbuilt_seams.py), so seam-verified adjacency is always available.
                .BuildingBlockContiguity()
                // Straight-avenue lane routing (street-coherence pass): hand-built fcx01 streets
                // are straight boulevards with single L-corners (turn-tile share 0-15% of road
                // cells across the 19 hand-built city areas, r17_road_audit.py); the legacy
                // first-found BFS lane geometry zigzagged staircases across plazas (16-29%).
                .StraightStreetRouting()
                .SetPiece("Tower00", 3)
                .SetPiece("Tower01")
                .SetPiece("Tower02")
                .SetPiece("Tower03")
                .SetPiece("Tower05")
                .SetPiece("Tower07")
                .SetPiece("b_tower", 1)
                .SetPiece("b_tower02", 1)
                .SetPiece("d_house02")
                .SetPiece("b_platform")
                // platform1 (2x2, TILE111-114) was a genuinely unwired fcx01 Cobble-district group --
                // structurally identical footprint requirement to Tower00 (see SetPieceRoomCornerFloor
                // comment above). TryClassify re-verifies its corner terrain independently, so wiring it
                // here is safe even if it turns out to be Cobble2-cornered (it would simply never
                // classify/place on this profile, matching every other tileset-declared-but-unverified
                // group in this codebase's established pattern).
                .SetPiece("platform1", 3)
                .SetPiece("b_rampe")
                .SetPiece("b_escalier", 1)
                .SetPiece("b_trans", 1)
                .ExitGroup("Tower01")
                .ExitGroup("d_house02");

            // Futuristic City's own bulk palette — mined from the fcx01 user-named exemplar
            // (decoration_evidence/evidence_named_exemplars.json['pw_ar_narpromena'], "Smuggler's Moon
            // Promenade") and the 24 hand-built fcx01 areas' decorative inventory (10477 placeables,
            // re-mined July 2026 city review pass): streetlights, holo-sign kiosks, cargo, benches,
            // consoles, parked speeders — this is the fix for the reported "Alien Ruin content dressed
            // with Alien Ruin's own palette regardless of the Futuristic City tileset it was actually
            // generated on" bug. Strongest structural pairing: a holo kiosk lit by a nearby
            // streetlight -> vignette.
            //
            // STANDARD-vs-RUINED SPLIT (user directive "if you're going to do
            // things like destruction, these need to be separate profiles"): the STANDARD palette
            // below is CLEAN urban dressing only -- neat cargo (crates/containers/barrels), street
            // furniture (lamps/kiosks/benches/consoles/barricades), and floor decals that read as
            // signage/markings (swd_floorm01/flormh01/florrd01). Every wreckage/rubble/debris/
            // dirt-decal resref lives exclusively in the named "ruined" DecorationProfile declared
            // after the vignette -- selected only via a theme's DecorationProfile declaration or an
            // explicit request/review override, never by default.
            //
            // CorridorSide additionally doubles as this family's "street-side" bucket: LayoutRoadCarver
            // (post-road-carving pass) makes DungeonDecorationPlanner route any wall-eligible tile
            // within one cell of a carved Routes lane into CorridorSide regardless of the owning room's
            // shape (see DungeonDecorationPlanner.IsRoadAdjacent), matching pw_ar_narpromena's own
            // pattern of streetlights and holo kiosks strung along its streets rather than confined to
            // corridor-shaped rooms or doorways. Lamp-family entries are flagged AllowOnRoadSurface:
            // under the urban grammar they are the ONLY dressing allowed to stand ON the carved road
            // ribbon (hand-built streets carry their lamps/light strips on the lane surface itself);
            // everything else sets back to the road margin and faces the street.
            _builder
                // City dressing intensity is a family property: the 19 decorated hand-built fcx01
                // areas measure 1.61 decorative placeables per tile AGGREGATE (flagship promenade
                // areas 2.8-4.6), versus ~0.10-0.15 the theme-owned densities produced here -- the
                // reported "vast empty plaza" gap. The declared 2.6 target realizes ~1.4-1.5
                // per TOTAL tile on Packed (rooms cover about half the grid; the arrangement
                // mechanisms' own per-room caps absorb the rest) and ~0.65 per total tile on Halls,
                // whose chambers cover only ~70/400 tiles -- per ROOM tile both land at the
                // hand-built flagship 2.5-3.5 band (measured in the decoration-density audit);
                // District-pool recalibration: 2.6 -> 3.3 -- district-scoped pools plus per-area caps
                // shave realized pile density ~15%, and 3.0 restores the packed20 realized band
                // (~1.25-1.35 per total tile) without touching any mechanism share.
                // Ensemble/depot recalibration: 3.3 -> 3.8 -- composed ensembles/depot blocks commit
                // partially (satellite skips, segment margins), industrial pile damping trades
                // loose singles for depot rows, and the pile mechanism runs at its saturation cap
                // (so budget alone cannot restore it); measured packed20 realized 1.16 at 3.3 and
                // ~1.24 at 3.6 with the urban cap/floor adjustments -- 3.8 keeps the realized band
                // (1.2-1.35 per total tile) with margin.
                // (A later calibration checked a 3.8 -> 3.9 bump to lift three 24/32 sweep seeds sitting
                // 0.5-3% under the hand-built 2.845 decoratives-per-open-tile floor after the
                // frontage budget trim; realized output was BYTE-IDENTICAL -- the arrangement
                // mechanisms run cap-saturated, exactly as the cap-saturation analysis predicts -- so the
                // knob stays at its calibrated 3.8 and those seeds are documented sweep variance,
                // the same band-edge tail the earlier baseline showed on other cells.)
                // see DungeonTilesetProfile.DecorationDensityPerTile.
                .DecorationDensity(3.8)
                // Urban placement grammar ("it still feels like a scattering of different
                // objects randomly placed"): hand-built fcx01 dressing is 73% cardinal-aligned
                // (within 7.5 degrees of 0/90/180/270 -- measured across all 24 areas' 10477
                // decoratives) and same-resref groups share a dominant orientation; generated areas
                // measured 29% (chance). See DungeonTilesetProfile.UrbanDressing for the full rule
                // set (bearing alignment, road integrity, facade rows, cargo grids, pile zone
                // discipline) this declaration enables for the fcx01 family only.
                .UrbanDressing()
                // BUILDING-PLACEABLE CANYON FRONTAGE (see BuildingFrontagePlanner): the flagship
                // promenade's canyon walls are SKYSCRAPER PLACEABLES standing on flat cobble --
                // pw_ar_narpromena (12x12) carries 30 swd_build* placeables and ZERO building
                // tiles, build007 rows at 9.8-10.1m pitch with 100% cardinal bearings;
                // pw_ar_narscorpd (16x22) 77 swd_build*, pw_ar_nsshipyard (24x24) 60+. Weights
                // follow the mined mix (build007 dominant at ~46-61% of placements; the rest
                // accents), footprints are the measured model XY extents (FaceWidth x Depth). This
                // separate STRUCTURAL channel keeps swd_build007
                // Excluded from every scatter palette, and returns here as deliberate composed
                // structure only.
                // SALIENCE CLASSIFICATION (user:
                // "Still seeing a lot of repetition -- maybe even more than before"): perceived
                // variety follows the per-model histogram SHAPE, not entropy -- hand-builders
                // repeat PLAIN workhorse towers heavily (build007 18-36 per area) while
                // distinctive neon/emissive towers stay rare (the neon-clad build003: 1-4 per
                // comparable-mass area). Emissive _i-map coverage + diffuse neon-pixel share are
                // measured per model from the hak textures; per-area count histograms from the
                // hand-built fcx01 areas.
                //  - WORKHORSE (workhorse: true, dominant-eligible): top-2 hand-built per-area
                //    counts sum 20+ AND diffuse neon share < 0.15. build007 (36+30, neon 0.13 --
                //    the muted khaki daf_sw101 tower), build004 (21+7, 0.03), kyru08 (10+10,
                //    0.00 -- randoncity walls whole streets with the cistern). Even workhorses
                //    cap at their hand-built per-area MAXIMUM (36/21/10) so no generated area
                //    out-repeats the heaviest hand-built usage.
                //  - Every other model is an ACCENT: hard per-area cap at its hand-built maximum
                //    in comparable-mass (<=60-building) areas, plus an omnidirectional
                //    minSpacing floor mined from the hand-built same-model cross-line
                //    nearest-neighbor distances from the salience audit, so the same distinctive tower
                //    never recurs across parallel rows or facing pairs the way the reviewed
                //    halls-20 showcase repeated the cyan-lit build003 through the plaza.
                //  - FAMILIES: build001/002/003/005/006 share the daf_sw011_5/6 neon poster
                //    atlases and read as one neon line ("dafneon", family cap 15 = the
                //    narcatwalk comparable-mass family max; narpromena carries 11);
                //    buildg2/buildg4 share the full jsf_bldgtx set ("jsfsky", cap 5 =
                //    randoncity's 5+5 capped at the comparable-mass total). No same-mesh recolor
                //    pairs exist in this pool (decompiled-geometry hash check).
                .FrontageBuilding("swd_build007", 6, 13.8f, 15.1f, maxPerArea: 36, workhorse: true)
                .FrontageBuilding("swd_build003", 3, 19.3f, 9.3f, maxPerArea: 4, minSpacing: 15f,
                    family: "dafneon", familyMaxPerArea: 15)
                // The narrow lift cylinder is the mined repeat-risk accent: hand-built same-model
                // NN medians run 18.9-36.0m (narpromena 18.9,
                // nsshipyard 27.4, narscorpd 36.0) while unspaced generated areas packed pairs
                // 10m apart -- minSpacing enforces the mined spread floor.
                .FrontageBuilding("swd2_elev002", 2, 5.4f, 5.5f, maxPerArea: 9, minSpacing: 15f)
                .FrontageBuilding("swd_build006", 1, 33.5f, 17.8f, maxPerArea: 5, minSpacing: 20f,
                    family: "dafneon", familyMaxPerArea: 15)
                .FrontageBuilding("swd_build001", 1, 22.5f, 22.5f, maxPerArea: 3, minSpacing: 20f,
                    family: "dafneon", familyMaxPerArea: 15)
                .FrontageBuilding("swd_build004", 1, 61.5f, 20.3f, maxPerArea: 21, workhorse: true)
                .FrontageBuilding("swd_build005", 1, 36.5f, 36.5f, maxPerArea: 2, minSpacing: 30f,
                    family: "dafneon", familyMaxPerArea: 15)
                .FrontageBuilding("swd_build002", 1, 37.5f, 37.5f, maxPerArea: 2, minSpacing: 30f,
                    family: "dafneon", familyMaxPerArea: 15)
                .FrontageBuilding("_mdrn_pl_kyru12", 1, 11.7f, 13.3f, maxPerArea: 5, minSpacing: 12f)
                // indtowr also places via the industrial cargo-yard scatter channel at the mined
                // 10m row pitch, so its omnidirectional floor is 10m (not the 12m the kyru
                // accents carry) and the shared usage ledger keeps the combined count at the
                // hand-built max of 4.
                .FrontageBuilding("_mdrn_pl_indtowr", 1, 11.8f, 11.8f, maxPerArea: 4, minSpacing: 10f)
                // FRONTAGE POOL EXPANSION (user directive "a lot of reuse of assets -- any way to
                // get more variety?"): the delivered showcases drew 8-10 distinct building models
                // per area while comparable-mass hand-built areas draw 12-17 (nsshipyard 17,
                // narshadaar_promi 16, narscorpd 12, ns_comrcial_ka 23 --
                // mined building pool). Every entry below is mined
                // from that pool (multi-area hand-built usage, measured model XY extents from
                // r11_model_sizes.json, caps at the hand-built per-area maxima) and passes the
                // same walkable-clearance fit rules as the original ten. _mdrn_pl_pillr03 (68x68m,
                // shipyard runs of 3) is deliberately NOT added: its footprint dwarfs every other
                // frontage member and reads as terrain, not a street wall.
                .FrontageBuilding("_mdrn_pl_kyru08", 1, 11.4f, 11.4f, maxPerArea: 10, workhorse: true)
                .FrontageBuilding("_mdrn_pl_kyru14", 1, 11.2f, 10.5f, maxPerArea: 2, minSpacing: 20f)
                .FrontageBuilding("_mdrn_pl_kyru07", 1, 10.9f, 10.9f, maxPerArea: 2, minSpacing: 20f)
                .FrontageBuilding("_mdrn_pl_kyru06", 1, 21.8f, 21.8f, maxPerArea: 1)
                .FrontageBuilding("_mdrn_pl_fac13d", 1, 33.7f, 33.4f, maxPerArea: 2, minSpacing: 20f)
                .FrontageBuilding("_mdrn_pl_buildg2", 1, 42.2f, 42.2f, maxPerArea: 3, minSpacing: 25f,
                    family: "jsfsky", familyMaxPerArea: 5)
                .FrontageBuilding("_mdrn_pl_buildg4", 1, 61.0f, 36.8f, maxPerArea: 2, minSpacing: 25f,
                    family: "jsfsky", familyMaxPerArea: 5)
                .FrontageBuilding("swd_impmed01", 1, 14.5f, 10.6f, maxPerArea: 3, minSpacing: 15f)
                .FrontageBuilding("_mdrn_pl_pillr06", 1, 19.6f, 13.8f, maxPerArea: 4, minSpacing: 15f)
                .FrontageBuilding("_mdrn_pl_colony1", 1, 24.5f, 21.9f, maxPerArea: 2, minSpacing: 17f)
                .FrontageBuilding("_mdrn_pl_mechtwb", 1, 24.8f, 16.4f, maxPerArea: 2, minSpacing: 25f)
                // Subtle per-instance scale jitter on the frontage walls (see
                // DungeonTilesetProfile.FrontageScaleJitter -- documented judgment call, applied
                // to the fit-checked footprint, persisted as .git VisualTransform offline and
                // SetObjectVisualTransform live).
                .FrontageScaleJitter()
                // WALL-MOUNTED FACADE DRESSING (see BuildingFrontagePlanner.
                // PlanFacadeMounts): the dense hand-built city areas hang holo signage on building
                // faces at Z bands mined per resref (velundr/narscorpd/nsshipyard/ns_comrcial_ka/
                // narshadaar_promi measure 0.13-0.23 of decoratives above Z 0.5m, sign-family
                // median face distance ~0). Height bands below are the mined per-resref
                // z_min/z_max envelopes.
                .FacadeMount("swd_pholo06", 2, 2.4f, 6.6f)
                .FacadeMount("swd_holog05", 2, 1.3f, 7.0f)
                .FacadeMount("swd_holog07", 2, 3.0f, 4.5f)
                .FacadeMount("swd_holog10", 2, 1.1f, 5.8f)
                .FacadeMount("swd_pholo03", 1, 2.3f, 4.8f)
                .FacadeMount("swd_holog06", 1, 1.5f, 6.5f)
                .FacadeMount("swd_holog08", 1, 4.7f, 5.4f)
                // STREET DRESSING (dressed-street fill pass -- see StreetDressingEntry and
                // DungeonDecorationPlanner.PlanStreetDressing): hand-built promenade streets are
                // dressed DENSER than their plazas (2.1-6.5 decoratives per road tile), in three
                // layers -- the municipal lamp line (already composed by the road lamp-line
                // mechanism), flat road-marking plates near one per road tile, and margin accents
                // at ~0.5-1 per road tile. Mined per-road-tile inventory (July 2026 street pass):
                //   plates: swd_florrd01 narpromena 23/26 road tiles, nsshipyard 44/38,
                //           narscorpd 37/35; swd_florrt02 the rarer variant (4/area).
                //   accents: swd_trash01 narpromena 22/26 + nsshipyard 14 + narscorpd 18;
                //            _mdrn_pl_barrimw ns_comrcial_ka 40/63; _mdrn_pl_trshcn2 promi 10;
                //            _mdrn_pl_barr001 promi 10; _mdrn_pl_conso05 promi 8;
                //            _mdrn_pl_holod01/_mdrn_pl_holoco2 promi 7 each.
                // Caps follow the hand-built per-area maxima; swd_trash01's cap is COMBINED with
                // its clutter-palette usage via the shared per-area ledger.
                .StreetDressing("swd_florrd01", 3, StreetDressingKind.RoadMarking)
                .StreetDressing("swd_florrt02", 1, StreetDressingKind.RoadMarking, maxPerArea: 10)
                .StreetDressing("swd_trash01", 3, StreetDressingKind.MarginAccent, maxPerArea: 24)
                .StreetDressing("_mdrn_pl_trshcn2", 2, StreetDressingKind.MarginAccent, maxPerArea: 12)
                .StreetDressing("_mdrn_pl_barrimw", 2, StreetDressingKind.MarginAccent, maxPerArea: 20)
                .StreetDressing("_mdrn_pl_barr001", 1, StreetDressingKind.MarginAccent, maxPerArea: 10)
                .StreetDressing("_mdrn_pl_conso05", 1, StreetDressingKind.MarginAccent, maxPerArea: 8)
                .StreetDressing("_mdrn_pl_holod01", 1, StreetDressingKind.MarginAccent, maxPerArea: 8)
                .StreetDressing("_mdrn_pl_holoco2", 1, StreetDressingKind.MarginAccent, maxPerArea: 8)
                // SIGNATURE COMPOSITION: the street-canyon packed city at 24 -- the pairing/scale
                // where tile canyon blocks (BuildingBlockContiguity), placeable frontage, municipal
                // lamp lines, night-city atmosphere, and full dressing all compose into the
                // hand-built promenade look. Review tooling defaults to this when the family is
                // picked (every other layout/size stays selectable) and the default review module
                // carries one signature showcase.
                .SignatureComposition(StandardLayoutProfiles.Packed, 24)
                // STRUCTURAL-ITEM REMOVALS (July 2026 semantic-context pass, user report "this gate
                // without a wall ... doesn't make a lot of sense" -- see DecorationAnchoring):
                //  - swd_build007 is GONE (Excluded class): its model is an entire whole-tile
                //    building fragment, not dressing. The 103 hand-built placements are
                //    builder-composed architecture (median 86m from other building mass because it
                //    IS the building), which per-tile scatter cannot reproduce.
                //  - swd2_fence004/swd2_fence010 are GONE (RunSegment class): hand-built fences
                //    exist only as butt-jointed chains at model-width pitch (fence-family NN median
                //    6.58m against the powered segment's 7.12m width; the closed-door piece
                //    measures 11.87m -- wider than a whole 10m tile) with the door piece spliced
                //    INSIDE a run. The per-tile stamping model has no sub-tile chain contract, so a
                //    lone gate/fence segment was the exact reported artifact; composed fencing is
                //    tile vocabulary (see tds01's LayoutFenceCarver + FenceDoor01/02 set pieces),
                //    not placeable scatter. DungeonDecorationPlanner.MergePalette additionally
                //    strips any future RunSegment/Excluded curation outright.
                //
                // DISTRICT VOCABULARY (user report "still a lot of repetition ... make it
                // more varied and realistic to how a city might actually be laid out"): the palette
                // below is mined per DISTRICT from the 24 hand-built fcx01 areas (industrial =
                // shipyard/docks/industrial
                // sector, commercial = promenades/commerce, civic = corporate/cloud-city plazas).
                // Every entry carries: a size class measured from its decompiled model's XY extent
                // (Small <1.2m, Medium 1.2-3m, Large 3-8m,
                // Huge >=8m), a district affinity (Districts(...) -- evidence-derived from the
                // per-district placement counts), and, for repeat-risk art, a per-area cap from the
                // hand-built per-area p95 within its district (MaxPerArea(...)). Entries without
                // Districts(...) serve every district (universal street/cargo basics). See
                // DistrictFlavor/DecorationSize and DungeonDecorationPlanner.AssignDistrictFlavors.
                //
                // Road-margin street furniture (CorridorSide = the street-side bucket): barriers,
                // trash cans, consoles, holo signage, benches, market goods, kiosk rows -- the
                // market-row items that set back from the lane and face it under the urban grammar.
                .Decoration("_mdrn_pl_barr001", 2, DecorationContext.CorridorSide, size: DecorationSize.Large)
                    .Districts((DistrictFlavor.Industrial, 2), (DistrictFlavor.Commercial, 2), (DistrictFlavor.Civic, 1))
                .Decoration("_mdrn_pl_barrimw", 1, DecorationContext.CorridorSide)
                    .Districts((DistrictFlavor.Commercial, 1))
                .Decoration("_mdrn_pl_trshcn2", 1, DecorationContext.CorridorSide, size: DecorationSize.Small)
                    .Districts((DistrictFlavor.Commercial, 2), (DistrictFlavor.Industrial, 1)).MaxPerArea(12)
                .Decoration("_mdrn_pl_conso05", 1, DecorationContext.CorridorSide)
                    .Districts((DistrictFlavor.Commercial, 1))
                // Commercial holo signage rows (holod01 22 promenade placements, holoco2 15) and the
                // holotree "planters" that green the market frontage (holot03 17 commercial).
                .Decoration("_mdrn_pl_holod01", 1, DecorationContext.CorridorSide)
                    .Districts((DistrictFlavor.Commercial, 2))
                .Decoration("_mdrn_pl_holoco2", 1, DecorationContext.CorridorSide)
                    .Districts((DistrictFlavor.Commercial, 1))
                .Decoration("swd_holot03", 1, DecorationContext.CorridorSide)
                    .Districts((DistrictFlavor.Commercial, 2), (DistrictFlavor.Civic, 1)).MaxPerArea(12)
                .Decoration("swd_holot01", 1, DecorationContext.CorridorSide, size: DecorationSize.Large)
                    .Districts((DistrictFlavor.Industrial, 1), (DistrictFlavor.Commercial, 1)).MaxPerArea(6)
                .Decoration("swd_holot02", 1, DecorationContext.CorridorSide, size: DecorationSize.Large)
                    .Districts((DistrictFlavor.Civic, 1)).MaxPerArea(6)
                // Street furniture: parking meters (civic curbs), the industrial bus stop, benches
                // fronting the promenade, the fruit market + vendor bank kiosk-row fillers.
                .Decoration("swd_prkme01", 1, DecorationContext.CorridorSide, size: DecorationSize.Small)
                    .Districts((DistrictFlavor.Civic, 1), (DistrictFlavor.Commercial, 1))
                .Decoration("swd_bussto01", 1, DecorationContext.CorridorSide, size: DecorationSize.Large)
                    .Districts((DistrictFlavor.Industrial, 1)).MaxPerArea(4)
                .Decoration("swd_bench01", 1, DecorationContext.CorridorSide, size: DecorationSize.Large)
                    .Districts((DistrictFlavor.Commercial, 2), (DistrictFlavor.Civic, 1)).MaxPerArea(8)
                .Decoration("_mdrn_pl_chair26", 1, DecorationContext.CorridorSide)
                    .Districts((DistrictFlavor.Civic, 2), (DistrictFlavor.Commercial, 1))
                .Decoration("_mdrn_pl_marktfr", 1, DecorationContext.CorridorSide)
                    .Districts((DistrictFlavor.Commercial, 2)).MaxPerArea(4)
                .Decoration("_mdrn_pl_umbllar", 1, DecorationContext.CorridorSide)
                    .Districts((DistrictFlavor.Commercial, 1)).MaxPerArea(4)
                .Decoration("swd_vbank01", 1, DecorationContext.CorridorSide, size: DecorationSize.Small)
                    .Districts((DistrictFlavor.Commercial, 1)).MaxPerArea(4)
                // Civic colonnade columns strung along the boulevard (swlor_0137 "Column 2": 48
                // commercial placements at 80% road adjacency, spaced rows).
                .Decoration("swlor_0137", 1, DecorationContext.CorridorSide)
                    .Districts((DistrictFlavor.Commercial, 2), (DistrictFlavor.Civic, 1)).MaxPerArea(10)
                // CLUTTER BACKBONE -- NEAT CARGO ONLY (clean-city profile split): crates/containers/
                // barrels placed as tight piles and stacked rows, now district-scoped: the heavy
                // industrial movers concentrate in yard rooms, the promenade crates/market goods in
                // commercial rooms. Clutter-role entries feed the clutter-pile arrangement AND their
                // curated WallAdjacent bucket. Weights near-flat so no single type dominates.
                // Rubbish/debris/dirt content is deliberately ABSENT -- it lives in the "ruined"
                // profile below. _mdrn_pl_crate09 stays GONE (zero hand-built evidence, anachronism).
                // _mdrn_pl_kyru08 (the 11.4m storage silo) has MOVED to the Huge yard block at the
                // end of this palette -- an 83-silo blanket over one generated area was the
                // reported repetition.
                .Decoration("_mdrn_pl_crate08", 2, DecorationContext.WallAdjacent, DecorationRole.Clutter)
                    .Districts((DistrictFlavor.Industrial, 2), (DistrictFlavor.Civic, 2), (DistrictFlavor.Commercial, 1)).MaxPerArea(35).Stackable(1.46f)
                .Decoration("_mdrn_pl_crate07", 2, DecorationContext.WallAdjacent, DecorationRole.Clutter, size: DecorationSize.Small)
                    .Districts((DistrictFlavor.Industrial, 1), (DistrictFlavor.Civic, 2)).MaxPerArea(28).Stackable(0.96f)
                .Decoration("_mdrn_pl_crate06", 1, DecorationContext.WallAdjacent, DecorationRole.Clutter, size: DecorationSize.Small)
                    .Districts((DistrictFlavor.Commercial, 2), (DistrictFlavor.Industrial, 1)).MaxPerArea(24).Stackable(0.75f)
                .Decoration("_mdrn_pl_crate05", 1, DecorationContext.WallAdjacent, DecorationRole.Clutter, size: DecorationSize.Small)
                    .Districts((DistrictFlavor.Commercial, 2)).MaxPerArea(12).Stackable(1.50f)
                .Decoration("_mdrn_pl_conta39", 2, DecorationContext.WallAdjacent, DecorationRole.Clutter, size: DecorationSize.Large)
                    .MaxPerArea(30).Stackable(1.98f)
                .Decoration("_mdrn_pl_conta36", 2, DecorationContext.WallAdjacent, DecorationRole.Clutter, size: DecorationSize.Small)
                    .MaxPerArea(30).Stackable(0.68f)
                .Decoration("_mdrn_pl_conta54", 1, DecorationContext.WallAdjacent, DecorationRole.Clutter, size: DecorationSize.Small)
                    .Districts((DistrictFlavor.Commercial, 1), (DistrictFlavor.Civic, 1)).MaxPerArea(12)
                .Decoration("swd_conta004", 2, DecorationContext.WallAdjacent, DecorationRole.Clutter, size: DecorationSize.Small)
                    .Districts((DistrictFlavor.Industrial, 2), (DistrictFlavor.Civic, 1)).MaxPerArea(30).Stackable(0.88f)
                .Decoration("swd_conta006", 1, DecorationContext.WallAdjacent, DecorationRole.Clutter, size: DecorationSize.Large)
                    .Districts((DistrictFlavor.Industrial, 2)).MaxPerArea(20).Stackable(1.82f)
                .Decoration("swd_conta002", 1, DecorationContext.WallAdjacent, DecorationRole.Clutter, size: DecorationSize.Large)
                    .Districts((DistrictFlavor.Industrial, 1)).MaxPerArea(14).Stackable(1.60f)
                .Decoration("_mdrn_pl_conta17", 1, DecorationContext.WallAdjacent, DecorationRole.Clutter, size: DecorationSize.Small)
                    .Districts((DistrictFlavor.Industrial, 2)).MaxPerArea(14)
                .Decoration("_mdrn_pl_conta25", 1, DecorationContext.WallAdjacent, DecorationRole.Clutter, size: DecorationSize.Small)
                    .Districts((DistrictFlavor.Commercial, 2)).MaxPerArea(14).Stackable(0.70f)
                .Decoration("_mdrn_pl_conta53", 1, DecorationContext.WallAdjacent, DecorationRole.Clutter)
                    .Districts((DistrictFlavor.Commercial, 2)).MaxPerArea(12).Stackable(1.17f)
                .Decoration("_mdrn_pl_conta38", 1, DecorationContext.WallAdjacent, DecorationRole.Clutter, size: DecorationSize.Small)
                    .Districts((DistrictFlavor.Commercial, 1)).MaxPerArea(10).Stackable(0.82f)
                .Decoration("_mdrn_pl_conta51", 1, DecorationContext.WallAdjacent, DecorationRole.Clutter, size: DecorationSize.Small)
                    .Districts((DistrictFlavor.Civic, 1), (DistrictFlavor.Commercial, 1)).MaxPerArea(10)
                .Decoration("_mdrn_pl_conta32", 1, DecorationContext.WallAdjacent, DecorationRole.Clutter, size: DecorationSize.Small)
                    .Districts((DistrictFlavor.Civic, 1)).MaxPerArea(10)
                .Decoration("swd2_cont008", 1, DecorationContext.WallAdjacent, DecorationRole.Clutter, size: DecorationSize.Small)
                    .Districts((DistrictFlavor.Civic, 1), (DistrictFlavor.Industrial, 1)).MaxPerArea(16).Stackable(0.96f)
                .Decoration("_mdrn_pl_crgc4b", 1, DecorationContext.WallAdjacent, DecorationRole.Clutter, size: DecorationSize.Large)
                    .Districts((DistrictFlavor.Civic, 1), (DistrictFlavor.Industrial, 1)).MaxPerArea(10)
                .Decoration("_mdrn_pl_ration6", 1, DecorationContext.WallAdjacent, DecorationRole.Clutter, size: DecorationSize.Small)
                    .Districts((DistrictFlavor.Commercial, 2)).MaxPerArea(12).Stackable(1.20f)
                .Decoration("_mdrn_pl_malette", 1, DecorationContext.WallAdjacent, DecorationRole.Clutter, size: DecorationSize.Small)
                    .Districts((DistrictFlavor.Commercial, 1)).MaxPerArea(8)
                .Decoration("swd_palet01", 1, DecorationContext.WallAdjacent, DecorationRole.Clutter)
                    .Districts((DistrictFlavor.Industrial, 2)).MaxPerArea(16).Stackable(1.05f)
                .Decoration("swd_barrel01", 1, DecorationContext.WallAdjacent, DecorationRole.Clutter, size: DecorationSize.Small)
                    .Districts((DistrictFlavor.Industrial, 1), (DistrictFlavor.Commercial, 1)).MaxPerArea(14)
                .Decoration("swd_spack01", 1, DecorationContext.WallAdjacent, DecorationRole.Clutter, size: DecorationSize.Small)
                    .Districts((DistrictFlavor.Industrial, 1)).MaxPerArea(10).Stackable(0.84f)
                .Decoration("_mdrn_pl_cagebst", 1, DecorationContext.WallAdjacent, DecorationRole.Clutter, size: DecorationSize.Large)
                    .Districts((DistrictFlavor.Civic, 1), (DistrictFlavor.Industrial, 1)).MaxPerArea(6)
                .Decoration("swd_trash01", 1, DecorationContext.WallAdjacent, DecorationRole.Clutter, size: DecorationSize.Small)
                    .MaxPerArea(20)
                .Decoration("swd_dump003", 1, DecorationContext.WallAdjacent, DecorationRole.Clutter)
                    .Districts((DistrictFlavor.Industrial, 2), (DistrictFlavor.Civic, 1)).MaxPerArea(16)
                .Decoration("swd_dump002", 1, DecorationContext.WallAdjacent, DecorationRole.Clutter, size: DecorationSize.Large)
                    .Districts((DistrictFlavor.Industrial, 1)).MaxPerArea(4)
                // Wall infrastructure accents (pipes hug real walls at 100% cardinal in the mined
                // reference; transformers/generators/computer banks are the yard machinery that
                // makes an industrial room read industrial).
                .Decoration("_mdrn_pl_pip3s", 1, DecorationContext.WallAdjacent)
                    .Districts((DistrictFlavor.Industrial, 2), (DistrictFlavor.Commercial, 1))
                .Decoration("_mdrn_pl_pip2s", 1, DecorationContext.WallAdjacent)
                    .Districts((DistrictFlavor.Industrial, 2))
                .Decoration("_mdrn_pl_transf2", 1, DecorationContext.WallAdjacent, size: DecorationSize.Large)
                    .Districts((DistrictFlavor.Industrial, 2)).MaxPerArea(6)
                .Decoration("swd2_gene003", 1, DecorationContext.WallAdjacent, size: DecorationSize.Large)
                    .Districts((DistrictFlavor.Industrial, 1), (DistrictFlavor.Civic, 1)).MaxPerArea(4)
                .Decoration("swd_compu001", 1, DecorationContext.WallAdjacent, size: DecorationSize.Large)
                    .Districts((DistrictFlavor.Industrial, 2), (DistrictFlavor.Civic, 1)).MaxPerArea(10)
                .Decoration("swd_grate01", 1, DecorationContext.WallAdjacent)
                    .Districts((DistrictFlavor.Industrial, 1), (DistrictFlavor.Civic, 1))
                // Industrial floor hatchways (GroundDecal role: layered under yard piles, 100%
                // cardinal in the mined reference -- flrhch4 59 placements). Size Small (1.6m
                // models): these are the PILE-scale pads under the decal size-matching
                // rule (see DungeonDecorationPlanner.PickUrbanDecal).
                .Decoration("_mdrn_pl_flrhch4", 2, DecorationContext.WallAdjacent, DecorationRole.GroundDecal, size: DecorationSize.Small)
                    .Districts((DistrictFlavor.Industrial, 2))
                .Decoration("_mdrn_pl_flrhch1", 1, DecorationContext.WallAdjacent, DecorationRole.GroundDecal, size: DecorationSize.Small)
                    .Districts((DistrictFlavor.Civic, 1), (DistrictFlavor.Industrial, 1))
                .Decoration("_mdrn_pl_flrhch2", 1, DecorationContext.WallAdjacent, DecorationRole.GroundDecal, size: DecorationSize.Small)
                    .Districts((DistrictFlavor.Commercial, 1), (DistrictFlavor.Industrial, 1))
                // GROUND DECALS -- SIGNAGE/MARKINGS ONLY (clean-city profile split): swd_floorm01
                // (top structured floor piece, 360 mined placements at 100% cardinal) and the hex
                // floor plate (flormh01, 100% cardinal). Hand-built areas use decals as LAYERING
                // UNDER arrangements, never lone patches, so GroundDecal-role entries are only
                // ever emitted underneath a committed clutter pile or as a courtyard center that
                // receives clutter on top. The dirt-stain decals (_mdrn_pl_dirtyg*) moved to the
                // "ruined" profile -- dirt is destruction dressing, not clean-city signage.
                // flormh01 (metal hex) is the CIVIC floor signature (82 of its 92 mined placements
                // sit in civic areas). Size Large (8.5-9.6m PLATES, measured from the decompiled
                // models; decal size discipline): a whole-tile plate may base a composed
                // ensemble/courtyard with verified clearance, but never pads a 2m junk pile -- the
                // reported "same square gray decal under every cluster" motif. swd_florrd01 (the
                // dark ROAD-marking plate) was removed from this pool in the street-coherence
                // pass: its art is lane paint, and its courtyard-base/wall-adjacent placements on
                // plaza and apron floor were exactly the user-reviewed "isolated plates with no
                // road context" -- it now lays ONLY via the street channel's plate rows (see
                // StreetDressing below), never off the carved road ribbon.
                .Decoration("swd_floorm01", 2, DecorationContext.WallAdjacent, DecorationRole.GroundDecal, size: DecorationSize.Large)
                .Decoration("swd_flormh01", 2, DecorationContext.WallAdjacent, DecorationRole.GroundDecal, size: DecorationSize.Large)
                    .Districts((DistrictFlavor.Civic, 2), (DistrictFlavor.Commercial, 1))
                // StructureAdjacent (building-frontage) bucket -- the items hand-built fcx01 actually
                // anchors against stamped tower/building footprints (Chebyshev<=1 building adjacency):
                // _mdrn_pl_lamp4 52% building-adjacent AND 100% road-adjacent (all 89 mined
                // placements are commercial -- a promenade building lamp), _mdrn_pl_bldlit 41%/95%
                // (building-mounted light, every district). Entries here place ONLY within 1 tile of
                // a stamped OpenSetPiece footprint -- never free-standing.
                .Decoration("_mdrn_pl_lamp4", 3, DecorationContext.StructureAdjacent, size: DecorationSize.Small)
                    .Districts((DistrictFlavor.Commercial, 3), (DistrictFlavor.Civic, 1)).MaxPerArea(15)
                .Decoration("_mdrn_pl_bldlit", 3, DecorationContext.StructureAdjacent, size: DecorationSize.Small)
                // FLUSH-ANCHORED cargo/furniture (semantic-context pass): hand-built builders put
                // these against building architecture essentially always -- median
                // building-architecture distance 0.00 with flush(<=1m) fractions 0.90/0.70/0.55/
                // 0.75/0.71 (swd_conta003 n=321, _mdrn_pl_df_chb n=67, _mdrn_pl_conta42 n=71,
                // _mdrn_pl_crgc2h n=34, swd_conta001 n=57). The WallFlush anchoring contract places
                // them 0.4m inside a stamped structure footprint's cardinal face, bearing = the
                // face normal, and NOWHERE else (no piles, no doorway flanks, no plain wall runs)
                // -- see DecorationAnchoring.WallFlush. District split follows the per-district
                // counts (conta003 167 industrial/154 civic; conta42 33 of 71 commercial; df_chb
                // benches 36 civic/20 commercial).
                .Decoration("swd_conta003", 2, DecorationContext.StructureAdjacent, anchoring: DecorationAnchoring.WallFlush, size: DecorationSize.Small)
                    .Districts((DistrictFlavor.Industrial, 2), (DistrictFlavor.Civic, 2))
                .Decoration("_mdrn_pl_df_chb", 2, DecorationContext.StructureAdjacent, anchoring: DecorationAnchoring.WallFlush)
                    .Districts((DistrictFlavor.Civic, 2), (DistrictFlavor.Commercial, 2))
                .Decoration("_mdrn_pl_conta42", 2, DecorationContext.StructureAdjacent, anchoring: DecorationAnchoring.WallFlush)
                    .Districts((DistrictFlavor.Commercial, 2), (DistrictFlavor.Industrial, 1))
                .Decoration("_mdrn_pl_crgc2h", 1, DecorationContext.StructureAdjacent, anchoring: DecorationAnchoring.WallFlush)
                    .Districts((DistrictFlavor.Industrial, 2))
                .Decoration("swd_conta001", 1, DecorationContext.StructureAdjacent, anchoring: DecorationAnchoring.WallFlush, size: DecorationSize.Large)
                    .Districts((DistrictFlavor.Industrial, 2), (DistrictFlavor.Civic, 1)).MaxPerArea(10)
                // Building-frontage extras: wall signage (paint02 -- Czerka sign, flush against the
                // facade), the commercial/civic security gate, construction scaffolding, and the
                // 4.8m industrial frontage tower.
                .Decoration("_mdrn_pl_paint02", 1, DecorationContext.StructureAdjacent, anchoring: DecorationAnchoring.WallFlush)
                    .Districts((DistrictFlavor.Civic, 1), (DistrictFlavor.Commercial, 1)).MaxPerArea(4)
                .Decoration("_mdrn_pl_metalde", 1, DecorationContext.StructureAdjacent)
                    .Districts((DistrictFlavor.Commercial, 1), (DistrictFlavor.Civic, 1)).MaxPerArea(3)
                .Decoration("swd2_scaf001", 1, DecorationContext.StructureAdjacent, size: DecorationSize.Large)
                    .Districts((DistrictFlavor.Civic, 1), (DistrictFlavor.Industrial, 1)).MaxPerArea(4)
                .Decoration("_mdrn_pl_indtwr2", 1, DecorationContext.StructureAdjacent, size: DecorationSize.Large)
                    .Districts((DistrictFlavor.Industrial, 2)).MaxPerArea(6)
                // Vehicles are LANDMARK one-offs under the vignette-integrity rule: they park against
                // stamped building frontages only, now spread across six vehicle models plus the
                // industrial forklift and parked speeder bikes so no single vehicle repeats across
                // every frontage (per-area caps at the hand-built p95).
                .Decoration("swd2_vehi006", 1, DecorationContext.StructureAdjacent, DecorationRole.Landmark, size: DecorationSize.Large)
                    .Districts((DistrictFlavor.Industrial, 1), (DistrictFlavor.Commercial, 1)).MaxPerArea(3)
                .Decoration("swd2_vehi003", 1, DecorationContext.StructureAdjacent, DecorationRole.Landmark, size: DecorationSize.Large)
                    .Districts((DistrictFlavor.Commercial, 1), (DistrictFlavor.Civic, 1)).MaxPerArea(3)
                .Decoration("swd2_vehi007", 1, DecorationContext.StructureAdjacent, DecorationRole.Landmark, size: DecorationSize.Large)
                    .Districts((DistrictFlavor.Industrial, 1), (DistrictFlavor.Civic, 1)).MaxPerArea(3)
                .Decoration("swd2_vehi002", 1, DecorationContext.StructureAdjacent, DecorationRole.Landmark, size: DecorationSize.Large)
                    .Districts((DistrictFlavor.Industrial, 2)).MaxPerArea(3)
                .Decoration("swd2_vehi001", 1, DecorationContext.StructureAdjacent, DecorationRole.Landmark, size: DecorationSize.Large)
                    .Districts((DistrictFlavor.Industrial, 1)).MaxPerArea(2)
                .Decoration("swd2_vehi005", 1, DecorationContext.StructureAdjacent, DecorationRole.Landmark, size: DecorationSize.Large)
                    .Districts((DistrictFlavor.Civic, 1)).MaxPerArea(2)
                .Decoration("_mdrn_pl_forklft", 1, DecorationContext.StructureAdjacent, DecorationRole.Landmark, size: DecorationSize.Large)
                    .Districts((DistrictFlavor.Industrial, 2)).MaxPerArea(3)
                .Decoration("_mdrn_pl_swoop03", 1, DecorationContext.StructureAdjacent, DecorationRole.Landmark, size: DecorationSize.Large)
                    .Districts((DistrictFlavor.Commercial, 1), (DistrictFlavor.Industrial, 1)).MaxPerArea(2)
                .Decoration("_mdrn_pl_speedb4", 1, DecorationContext.StructureAdjacent, DecorationRole.Landmark)
                    .Districts((DistrictFlavor.Civic, 1), (DistrictFlavor.Commercial, 1)).MaxPerArea(2)
                // Plaza-interior centerpieces, per district: the concrete light pillar and coronet
                // pillar for civic squares, holographic monuments (holog01-03 at 8 civic mined
                // placements, one-off spacing), the small projection holo, and the commercial
                // market canopy.
                .Decoration("_mdrn_pl_pillr04", 2, DecorationContext.RoomCenter, size: DecorationSize.Large)
                    .Districts((DistrictFlavor.Civic, 2), (DistrictFlavor.Commercial, 1))
                .Decoration("swd_pillar01", 1, DecorationContext.RoomCenter, size: DecorationSize.Large)
                    .Districts((DistrictFlavor.Civic, 2)).MaxPerArea(6)
                .Decoration("swd_holog01", 1, DecorationContext.RoomCenter)
                    .Districts((DistrictFlavor.Civic, 2)).MaxPerArea(3)
                .Decoration("swd_holog02", 1, DecorationContext.RoomCenter)
                    .Districts((DistrictFlavor.Civic, 1)).MaxPerArea(2)
                .Decoration("swd_holog03", 1, DecorationContext.RoomCenter)
                    .Districts((DistrictFlavor.Civic, 1)).MaxPerArea(2)
                .Decoration("swd_pholo01", 1, DecorationContext.RoomCenter, size: DecorationSize.Small)
                    .Districts((DistrictFlavor.Civic, 1), (DistrictFlavor.Commercial, 1)).MaxPerArea(4)
                .Decoration("swd_canopy01", 1, DecorationContext.RoomCenter, size: DecorationSize.Large)
                    .Districts((DistrictFlavor.Commercial, 1), (DistrictFlavor.Civic, 1)).MaxPerArea(3)
                // Repeat cap: the round pale kiosk pavilion measured 7-12 per delivered
                // area across its four channels while the hand-built per-area max is 6 (NN median
                // 14.8m in the large-prop audit). The cap is
                // declared on EVERY kiosk004 entry so the shared per-resref ledger holds the
                // hand-built ceiling across all channels combined.
                .Decoration("swd2_kiosk004", 2, DecorationContext.DoorwayFlank, size: DecorationSize.Large)
                    .Districts((DistrictFlavor.Commercial, 2), (DistrictFlavor.Civic, 1)).MaxPerArea(6)
                .Decoration("swd_streel01", 2, DecorationContext.DoorwayFlank)
                .Decoration("_mdrn_pl_trshcn2", 1, DecorationContext.DoorwayFlank, size: DecorationSize.Small)
                    .Districts((DistrictFlavor.Industrial, 1), (DistrictFlavor.Civic, 1))
                // Street furniture stays road-anchored (CorridorSide is the road-lining bucket) and
                // is ACCENT-scale. The lamp FAMILY (light strips, street lights, the double street
                // lamps -- all 94-100% cardinal in the mined reference) is flagged
                // AllowOnRoadSurface: the one dressing class that legitimately stands ON the carved
                // road ribbon. Lamp models split by district: strtlm2 industrial/commercial,
                // strtlm4 civic (71 of its 75 mined placements), lights8 the industrial large-unit
                // strip -- so each neighborhood's streets read distinctly lit. lights3/streel01
                // stay universal (they light every hand-built district).
                .Decoration("_mdrn_pl_lights3", 3, DecorationContext.CorridorSide, DecorationRole.Fixture, allowOnRoadSurface: true)
                .Decoration("swd_streel01", 2, DecorationContext.CorridorSide, DecorationRole.Fixture, allowOnRoadSurface: true)
                .Decoration("_mdrn_pl_strtlm2", 2, DecorationContext.CorridorSide, DecorationRole.Fixture, allowOnRoadSurface: true, size: DecorationSize.Small)
                    .Districts((DistrictFlavor.Industrial, 2), (DistrictFlavor.Commercial, 2))
                .Decoration("_mdrn_pl_strtlm4", 1, DecorationContext.CorridorSide, DecorationRole.Fixture, allowOnRoadSurface: true, size: DecorationSize.Small)
                    .Districts((DistrictFlavor.Civic, 2))
                .Decoration("_mdrn_pl_lights8", 1, DecorationContext.CorridorSide, DecorationRole.Fixture, allowOnRoadSurface: true)
                    .Districts((DistrictFlavor.Industrial, 1))
                .Decoration("swd2_kiosk004", 2, DecorationContext.CorridorSide, size: DecorationSize.Large)
                    .Districts((DistrictFlavor.Commercial, 3), (DistrictFlavor.Civic, 1)).MaxPerArea(6)
                .Decoration("swd2_kiosk006", 1, DecorationContext.CorridorSide)
                    .Districts((DistrictFlavor.Commercial, 2), (DistrictFlavor.Civic, 1))
                // Courtyard arrangement buckets (see DungeonDecorationPlanner.PlanCourtyard), mined
                // from hand-built fcx01 INTERIOR items (>2 tiles from walls/roads across the 19
                // decorated fcx01 areas, July 2026 city-density pass): interior arrangements cluster
                // as a centerpiece + 4-13-member ring at radius ~4-9m. Measured centerpieces are
                // floor decals/lights (_mdrn_pl_lghtflr anchored the narshadaar_promi light-pole
                // ring, _mdrn_pl_floor27 the ns_industrialsec container ring; swd_floorm01 is the
                // most common structured interior floor piece at 107 interior occurrences); measured
                // ring members are light poles (_mdrn_pl_lghtpl3, 4 ring hits), containers
                // (_mdrn_pl_conta36, 4), barrels/crates (_mdrn_pl_barr001/_mdrn_pl_crate08, 2 each),
                // pillars (_mdrn_pl_pillr04, 241 interior occurrences), bus shelters
                // (_mdrn_pl_busshel), and kiosks (swd2_kiosk004 -- also the mission-reported "kiosk
                // cluster" pattern). Weights follow those measured frequencies.
                // swd_floorm01 is a flat floor marking (GroundDecal role): as a courtyard center it
                // additionally receives 1-2 ring-motif items layered on top (see
                // DungeonDecorationPlanner.PlanCourtyard) so the decal never reads as a lone patch.
                // Courtyard vocabularies split by district too: civic plazas ring pillars/benches/
                // colonnade columns around hex-floor or hologram centers, commercial squares ring
                // benches/kiosks/holotrees around the lit floor strips, industrial yards ring
                // containers/barriers around work-floor markings -- so a courtyard tells you which
                // neighborhood you are standing in.
                .Decoration("swd_floorm01", 3, DecorationContext.CourtyardCenter, DecorationRole.GroundDecal, size: DecorationSize.Large)
                .Decoration("swd_flormh01", 1, DecorationContext.CourtyardCenter, DecorationRole.GroundDecal, size: DecorationSize.Large)
                    .Districts((DistrictFlavor.Civic, 2))
                // The 10m floor light strips carry Size Large so the ensemble/park
                // mechanisms never stand one as a CENTERPIECE (a standing item's slot); they remain
                // courtyard centers exactly as before.
                .Decoration("_mdrn_pl_lghtflr", 2, DecorationContext.CourtyardCenter, size: DecorationSize.Large)
                    .Districts((DistrictFlavor.Commercial, 2), (DistrictFlavor.Industrial, 1))
                .Decoration("_mdrn_pl_floor27", 2, DecorationContext.CourtyardCenter, size: DecorationSize.Large)
                    .Districts((DistrictFlavor.Industrial, 2), (DistrictFlavor.Commercial, 1))
                .Decoration("swd_holog01", 1, DecorationContext.CourtyardCenter)
                    .Districts((DistrictFlavor.Civic, 1))
                // Mid-room ensemble centerpieces (see DungeonDecorationPlanner.
                // PlanInteriorEnsemble/PlanZoneDressings): the holotree "planter" tree centers park
                // lawns and civic gardens (holot03 is the mined commercial/civic greenery -- 17
                // commercial placements), the market kiosk and fruit stand center commercial plaza
                // ISLANDS (the kiosk + seating + trash + sign moment; swd2_kiosk004 is also the
                // mined "kiosk cluster" pattern).
                .Decoration("swd_holot03", 2, DecorationContext.CourtyardCenter)
                    .Districts((DistrictFlavor.Civic, 2), (DistrictFlavor.Commercial, 1))
                .Decoration("swd2_kiosk004", 1, DecorationContext.CourtyardCenter, size: DecorationSize.Large)
                    .Districts((DistrictFlavor.Commercial, 2)).MaxPerArea(6)
                .Decoration("_mdrn_pl_marktfr", 1, DecorationContext.CourtyardCenter)
                    .Districts((DistrictFlavor.Commercial, 1))
                .Decoration("_mdrn_pl_lghtpl3", 3, DecorationContext.Courtyard)
                .Decoration("_mdrn_pl_conta36", 3, DecorationContext.Courtyard, size: DecorationSize.Small)
                    .Districts((DistrictFlavor.Industrial, 3), (DistrictFlavor.Commercial, 1), (DistrictFlavor.Civic, 1))
                .Decoration("_mdrn_pl_pillr04", 2, DecorationContext.Courtyard, size: DecorationSize.Large)
                    .Districts((DistrictFlavor.Civic, 2))
                .Decoration("_mdrn_pl_barr001", 2, DecorationContext.Courtyard, size: DecorationSize.Large)
                    .Districts((DistrictFlavor.Industrial, 2), (DistrictFlavor.Commercial, 1))
                .Decoration("_mdrn_pl_crate08", 2, DecorationContext.Courtyard)
                    .Districts((DistrictFlavor.Industrial, 2), (DistrictFlavor.Civic, 1))
                .Decoration("swd_bench01", 2, DecorationContext.Courtyard, size: DecorationSize.Large)
                    .Districts((DistrictFlavor.Commercial, 2), (DistrictFlavor.Civic, 2))
                .Decoration("_mdrn_pl_chair26", 1, DecorationContext.Courtyard)
                    .Districts((DistrictFlavor.Civic, 1))
                .Decoration("swd_holot03", 1, DecorationContext.Courtyard)
                    .Districts((DistrictFlavor.Commercial, 1), (DistrictFlavor.Civic, 1))
                .Decoration("swlor_0136", 1, DecorationContext.Courtyard)
                    .Districts((DistrictFlavor.Civic, 1), (DistrictFlavor.Commercial, 1))
                // _mdrn_pl_busshel (bus shelter, 19 interior occurrences) was measured into this
                // bucket too but is EXCLUDED: its appearance row (7038) has a blank ModelName and
                // renders invisible (caught by AllDungeonDefinitions_DecorationsExistAndAreVisible).
                .Decoration("swd2_kiosk004", 1, DecorationContext.Courtyard, size: DecorationSize.Large)
                    .Districts((DistrictFlavor.Commercial, 1)).MaxPerArea(6)
                .Vignette("PromenadeKioskLight", 3)
                .VignetteMember("swd2_kiosk004", 0f, 0f)
                .VignetteMember("_mdrn_pl_lights3", 0.7f, 0.5f)
                // Market-stall moment (commercial promenades): the fruit market shaded by its red
                // umbrella -- both mined exclusively from the commercial promenade areas (marktfr 8,
                // umbllar 9 hand-built placements).
                .Vignette("MarketStallUmbrella", 2)
                .VignetteMember("_mdrn_pl_marktfr", 0f, 0f)
                .VignetteMember("_mdrn_pl_umbllar", 0.3f, 0.5f)

                // BUILDING-SCALE (Huge) INDUSTRIAL YARD ART -- the size-discipline block.
                // These models measure 8m+ (kyru08 storage silo 11.4x11.4x15.6m, indtowr 11.8m,
                // genl01 generator 9.2m, df_ss5 parked starfighter 11.2m -- decompiled-model
                // measurements) and place ONLY as composed
                // cargo-yard rows/pairs in Industrial-flavor rooms (see
                // DungeonDecorationPlanner.PlanCargoYard): consecutive wall tiles, shared bearing,
                // hard per-area caps. Uncapped generation carried 83 silos blanketing one area at
                // 1.4m spacing -- the reported "same massive building placeables" repetition; the
                // hand-built evidence concentrates them in shipyard/dock yards in single-digit
                // per-area counts (kyru08 industrial per-area p95 = 1-10, row pitch 10m).
                .Decoration("_mdrn_pl_kyru08", 3, DecorationContext.WallAdjacent, size: DecorationSize.Huge)
                    .Districts((DistrictFlavor.Industrial, 3)).MaxPerArea(6)
                .Decoration("_mdrn_pl_indtowr", 1, DecorationContext.WallAdjacent, size: DecorationSize.Huge)
                    .Districts((DistrictFlavor.Industrial, 1)).MaxPerArea(2)
                .Decoration("swd_genl01", 2, DecorationContext.WallAdjacent, size: DecorationSize.Huge)
                    .Districts((DistrictFlavor.Industrial, 2)).MaxPerArea(4)
                .Decoration("_mdrn_pl_df_ss5", 1, DecorationContext.WallAdjacent, DecorationRole.Landmark, size: DecorationSize.Huge)
                    .Districts((DistrictFlavor.Industrial, 1)).MaxPerArea(2)

                // ============================================================================
                // "ruined" -- the DESTRUCTION decoration profile: every
                // wreckage/rubble/debris/dirt-decal resref the standard clean-city palette
                // deliberately excludes, mined from the same 24 hand-built fcx01 areas
                // (rubb029-032 388 combined placements, pape019 69, debri01/03/20 mined from reference areas,
                // wallblk 30, jkpl002 44, dirtyg1-4 815 combined). Selected ONLY via a theme's
                // DecorationProfile declaration or an explicit request/review override -- the
                // default for every composition (including the Alien Ruin showcases) stays the
                // standard profile. Organic clutter rotation: collapse debris genuinely tumbles,
                // so pile members keep full random spin (the one sanctioned exception to the urban
                // grammar's bearing alignment); everything else -- lamps, barricades, decal
                // markings, the wrecked-vehicle landmarks -- still anchors and aligns exactly like
                // the standard profile, because a ruined CITY is still a city: destruction hugs the
                // walls, structure bases, and corners it fell from, never free-floating mid-plaza
                // (the urban pile zone discipline applies to this profile too).
                // ============================================================================
                .DecorationProfile("ruined", organicClutterRotation: true)
                // Rubble/debris/junk backbone (Clutter role feeds the pile arrangement).
                .Decoration("_mdrn_pl_rubb031", 3, DecorationContext.WallAdjacent, DecorationRole.Clutter)
                .Decoration("_mdrn_pl_rubb030", 2, DecorationContext.WallAdjacent, DecorationRole.Clutter)
                .Decoration("_mdrn_pl_rubb029", 2, DecorationContext.WallAdjacent, DecorationRole.Clutter)
                .Decoration("_mdrn_pl_rubb032", 1, DecorationContext.WallAdjacent, DecorationRole.Clutter)
                .Decoration("_mdrn_pl_pape019", 2, DecorationContext.WallAdjacent, DecorationRole.Clutter)
                .Decoration("_mdrn_pl_debri20", 2, DecorationContext.WallAdjacent, DecorationRole.Clutter)
                .Decoration("_mdrn_pl_debri03", 2, DecorationContext.WallAdjacent, DecorationRole.Clutter)
                // _mdrn_pl_debri01 ("Debris, Containment Cylinder") is GONE (semantic-context
                // pass): zero hand-built fcx01 placements -- same zero-evidence bar that removed
                // _mdrn_pl_crate09 from the standard palette. swd2_debr001 (26 hand-built
                // placements) and _mdrn_pl_rubb028 (24) replace its pool breadth with
                // evidence-backed junk. _mdrn_pl_wallblk stays: despite the "Wall Block" name it is
                // a FALLEN debris chunk (30 hand-built placements as loose rubble), not standing
                // architecture.
                .Decoration("swd2_debr001", 2, DecorationContext.WallAdjacent, DecorationRole.Clutter)
                .Decoration("_mdrn_pl_rubb028", 1, DecorationContext.WallAdjacent, DecorationRole.Clutter)
                .Decoration("_mdrn_pl_wallblk", 2, DecorationContext.WallAdjacent, DecorationRole.Clutter)
                .Decoration("swd_jkpl002", 2, DecorationContext.WallAdjacent, DecorationRole.Clutter)
                // Battered remnants of the ordinary cargo backbone, so a ruined district still
                // reads as a looted city rather than a landfill.
                .Decoration("_mdrn_pl_crate08", 2, DecorationContext.WallAdjacent, DecorationRole.Clutter)
                .Decoration("_mdrn_pl_conta39", 1, DecorationContext.WallAdjacent, DecorationRole.Clutter)
                .Decoration("swd_trash01", 2, DecorationContext.WallAdjacent, DecorationRole.Clutter)
                .Decoration("swd_dump003", 2, DecorationContext.WallAdjacent, DecorationRole.Clutter)
                // Dirt/stain decals -- destruction layering under the rubble piles (never lone
                // patches; the pile mechanism is the only emitter).
                .Decoration("_mdrn_pl_dirtyg1", 3, DecorationContext.WallAdjacent, DecorationRole.GroundDecal)
                .Decoration("_mdrn_pl_dirtyg3", 2, DecorationContext.WallAdjacent, DecorationRole.GroundDecal)
                .Decoration("_mdrn_pl_dirtyg2", 1, DecorationContext.WallAdjacent, DecorationRole.GroundDecal)
                .Decoration("_mdrn_pl_dirtyg4", 1, DecorationContext.WallAdjacent, DecorationRole.GroundDecal)
                // Street furniture survives ruination: corroded barricades line the roads, the lamp
                // family keeps its road-surface license, and wrecked speeders park (crash) against
                // building frontages as Landmark one-offs -- same anchoring contract as standard.
                // swd2_fence004 is GONE here too (RunSegment class -- a lone powered-fence segment
                // in ruins is exactly as nonsensical as in the clean city; see the standard
                // palette's structural-item removal note).
                .Decoration("_mdrn_pl_barrim2", 2, DecorationContext.CorridorSide)
                .Decoration("_mdrn_pl_lights3", 2, DecorationContext.CorridorSide, DecorationRole.Fixture, allowOnRoadSurface: true)
                .Decoration("swd_streel01", 2, DecorationContext.CorridorSide, DecorationRole.Fixture, allowOnRoadSurface: true)
                .Decoration("_mdrn_pl_lghtpl3", 2, DecorationContext.CorridorSide, DecorationRole.Fixture, allowOnRoadSurface: true)
                .Decoration("_mdrn_pl_lamp4", 3, DecorationContext.StructureAdjacent)
                .Decoration("_mdrn_pl_bldlit", 2, DecorationContext.StructureAdjacent)
                .Decoration("swd2_vehi006", 1, DecorationContext.StructureAdjacent, DecorationRole.Landmark)
                .Decoration("swd2_vehi003", 1, DecorationContext.StructureAdjacent, DecorationRole.Landmark)
                .Decoration("swd2_vehi007", 1, DecorationContext.StructureAdjacent, DecorationRole.Landmark)
                .Decoration("_mdrn_pl_pillr04", 2, DecorationContext.RoomCenter)
                .Decoration("swd_streel01", 2, DecorationContext.DoorwayFlank)
                // Courtyards persist as scorched gathering circles: a stained centerpiece ringed by
                // corroded lamps and battered containers (ring members still face the center; the
                // ring is a composed arrangement, not scatter).
                .Decoration("_mdrn_pl_dirtyg1", 2, DecorationContext.CourtyardCenter, DecorationRole.GroundDecal)
                .Decoration("swd_floorm01", 2, DecorationContext.CourtyardCenter, DecorationRole.GroundDecal, size: DecorationSize.Large)
                .Decoration("_mdrn_pl_lghtflr", 1, DecorationContext.CourtyardCenter, size: DecorationSize.Large)
                .Decoration("_mdrn_pl_lghtpl3", 3, DecorationContext.Courtyard)
                .Decoration("_mdrn_pl_conta36", 2, DecorationContext.Courtyard)
                .Decoration("_mdrn_pl_crate08", 2, DecorationContext.Courtyard)
                .Decoration("_mdrn_pl_barrim2", 1, DecorationContext.Courtyard)
                // DEBRIS-VARIETY EXPANSION, mined from the slum/undercity/ruins reference
                // areas (vrotrnsslums, pw_ar_velundr, randoncity_02): scattered newspapers and
                // rubbish bags (pape003-006 mined
                // 13-21 placements each), a second full debris-pile family (swd3_dbpil02), wrecked
                // equipment and vehicle parts (debri06/debri11), destroyed droids (droidsd/
                // droidd4), loose drums, fallen girders, and tipped trash cans -- so ruined blocks
                // stop cycling the same four rubble models. All Clutter-role: they feed the pile
                // arrangement under the same organic-rotation license as the rest of this profile.
                .Decoration("_mdrn_pl_pape005", 1, DecorationContext.WallAdjacent, DecorationRole.Clutter, size: DecorationSize.Small)
                .Decoration("_mdrn_pl_pape003", 1, DecorationContext.WallAdjacent, DecorationRole.Clutter, size: DecorationSize.Small)
                .Decoration("_mdrn_pl_pape006", 1, DecorationContext.WallAdjacent, DecorationRole.Clutter, size: DecorationSize.Small)
                .Decoration("swd3_dbpil02", 2, DecorationContext.WallAdjacent, DecorationRole.Clutter, size: DecorationSize.Large)
                .Decoration("_mdrn_pl_debri06", 2, DecorationContext.WallAdjacent, DecorationRole.Clutter)
                .Decoration("_mdrn_pl_debri11", 1, DecorationContext.WallAdjacent, DecorationRole.Clutter)
                .Decoration("_mdrn_pl_droidsd", 1, DecorationContext.WallAdjacent, DecorationRole.Clutter)
                .Decoration("_mdrn_pl_droidd4", 1, DecorationContext.WallAdjacent, DecorationRole.Clutter, size: DecorationSize.Large)
                .Decoration("_mdrn_pl_dfbar2", 1, DecorationContext.WallAdjacent, DecorationRole.Clutter, size: DecorationSize.Small)
                .Decoration("_mdrn_pl_gird001", 1, DecorationContext.WallAdjacent, DecorationRole.Clutter)
                .Decoration("_mdrn_pl_trshcn3", 1, DecorationContext.WallAdjacent, DecorationRole.Clutter, size: DecorationSize.Small)
                // Wrecked landspeeders crash against building frontages as Landmark one-offs (the
                // intact-vehicle anchoring contract, applied to the wrecks the slum evidence
                // actually carries).
                .Decoration("_mdrn_pl_lands04", 1, DecorationContext.StructureAdjacent, DecorationRole.Landmark, size: DecorationSize.Large)
                    .MaxPerArea(2)
                .Decoration("_mdrn_pl_lands11", 1, DecorationContext.StructureAdjacent, DecorationRole.Landmark, size: DecorationSize.Large)
                    .MaxPerArea(2);

            // D20 Futuristic City SW (fcx01) -- Cobble2 ("d_"-prefixed) district, a PaletteVariant
            // profile recomposing the SAME fcx01 hak data the base FutCity profile above uses (identical
            // solid "holes", crosser vocabulary, and hand-built evidence -- see FutCity's own doc
            // comment). Tower04/Tower06 are wired here (not on the base profile) despite the unprefixed
            // name -- verified directly, both are uniformly Cobble2-cornered.
            _builder.Create(FutCityPlaza, "D20 Futuristic City SW (Plaza)")
                .Tileset("fcx01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .PaletteVariant()
                .SolidTerrainOverride("holes")
                .PrimaryOpenTerrain("Cobble2")
                // Same room-size floor as the base FutCity profile: Tower04/d_build02 (2x2) need
                // corner size 6+, Tower06 (3x4)/d_build (4x3)/d_temple (2x3) need 7 -- see FutCity's
                // own SetPieceRoomCornerFloor comment above.
                .SetPieceRoomCornerFloor(7)
                .DoorSlotCrossers("murs")
                // Same "Routes" crosser as the base FutCity profile, resolving against the Cobble2-
                // cornered TILE212-216 physical variants instead -- see FutCity's own RoadCrosser
                // comment above.
                .RoadCrosser("Routes")
                // d_herbe is the Plaza district's grass lawn -- same Lawn ensemble obligation as
                // the base profile's b_herbe (see FeatureZoneDressing); d_eau is open water and
                // stays untouched.
                .FeatureTile("d_herbe", dressing: FeatureZoneDressing.Lawn)
                .FeatureTile("d_eau")
                // Tower04/d_build02 (2x2) are the Cobble2 district's only groups that fit size-20-24
                // rooms -- same site-limited ceiling reasoning as FutCity's Tower00 budget above.
                //
                // Re-measured at size 32 over 20 seeds against the Complex layout pairing: a
                // budget-only sweep was tried here too and reverted for the identical reason
                // documented on FutCity's Tower00 SetPiece above (no measured group_share benefit --
                // Complex's own MinRooms=6/MaxRooms=9 caps room supply at size 32 regardless of budget).
                // platform1 wired here too (2x2, same TryClassify self-verification safety as FutCity).
                // Room supply scaled with area exactly like the base FutCity profile -- see its own
                // SetPieceRoomSupplyScaling comment above. With room supply unblocked, the 2x2
                // workhorse Tower04 measured EXACTLY at its scaled budget ceiling at 32x32 (6.0
                // placements/area == ceil(2 x 2.56)), so the well-fitting groups' budgets are raised
                // toward the hand-built 0.15 group-share reference. Measured at 32x32 (32 seeds, 0
                // solve failures, July 2026 city-density pass, with the largest-footprint-first stamp
                // order this district's Tunnel-mode rooms rely on -- see LayoutGroupStamper.Stamp):
                // group share 0.017 (room supply flat) -> 0.070; d_temple/d_platform2/Tower06 only
                // place at all under largest-first + budget >= 2 (name order let Tower04 fragment
                // every big room first: raising Tower04's budget alone measured group share DOWN,
                // 0.050 -> 0.046). ~0.07 is this district's honest Complex-pairing ceiling -- Tunnel
                // mode keeps most of the grid as solid mass between rooms, so its stampable interior
                // is structurally smaller than the Packed pairing's (0.15 on the base profile).
                .SetPieceRoomSupplyScaling()
                // Same contiguous-block declaration as the base FutCity profile (see its own comment):
                // the Cobble2 district's tower groups (Tower04/Tower06/d_build/d_build02/d_temple)
                // likewise carry uniformly Cobble2-cornered, crosser-free perimeter faces, and the
                // hand-built Cobble2-district areas (narshadaar_promi's 48-tile block) show the same
                // adjoined-block assembly.
                .BuildingBlockContiguity()
                // Straight-avenue lane routing -- same street-coherence declaration as the base
                // FutCity profile (see its own comment).
                .StraightStreetRouting()
                .SetPiece("Tower04", 4)
                .SetPiece("Tower06", 2)
                .SetPiece("d_build", 2)
                .SetPiece("d_tower", 1)
                .SetPiece("d_tower02", 1)
                .SetPiece("d_monum", 2)
                .SetPiece("d_platform2")
                .SetPiece("platform1", 3)
                .SetPiece("d_house01")
                .SetPiece("d_build02", 4)
                .SetPiece("d_temple")
                .SetPiece("d_rampe")
                .SetPiece("d_escalier", 1)
                .SetPiece("d_trans", 1)
                .ExitGroup("d_tower")
                .ExitGroup("d_house01");

            // D20 Secret Base (tjsb0) -- see this file's SecretBase comment for the full
            // probe writeup (tunnel vocabulary, room-size floor, ExitGroup evidence, hand-built lighting).
            _builder.Create(SecretBase, "D20 Secret Base")
                .Tileset("tjsb0")
                .SetPieceRoomCornerFloor(6)
                // AccentTerrain("lava"): required for BridgeDoor01 (TILE112, uniformly lava-cornered,
                // "bridge" crosser on its Top/Bottom edge pair) to classify at all --
                // LayoutGroupStamper.TryClassifyCorridorInsert's Bridge branch only matches when the
                // tile's corners are uniformly AccentTerrain (verified directly: PilotEveryTileIs
                // ReachableOrExplicitlyExempted flagged TILE112 UNCLASSIFIED before this was declared,
                // and passed once it was). Costs nothing else this pass -- no ElevationBlob/PoolBank/
                // TerrainRelief/AccentChannel vocabulary is separately wired or verified.
                .AccentTerrain("lava")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .SetPiece("StairsDown_2x2")
                .SetPiece("StairsUp_2x2")
                .SetPiece("Platform01_2x2")
                .SetPiece("Platform02_2x2")
                .SetPiece("Platform03_2x2")
                .SetPiece("Platform04_1x2")
                .SetPiece("Platform05_1x2")
                .SetPiece("Pillar_1x2")
                .SetPiece("WallSection01_1x2")
                .SetPiece("WallSection02_1x2")
                .SetPiece("EnergySource")
                .SetPiece("Caveentrance")
                .SetPiece("Treasure01", 2)
                .SetPiece("Treasure02", 2)
                .SetPiece("Pillar01", 3)
                .SetPiece("Pillar02", 3)
                .SetPiece("Pillar03", 3)
                .SetPiece("Portal")
                .SetPiece("Chessboard")
                .SetPiece("Door_Trans")
                .SetPiece("BigDoor01")
                .SetPiece("BigDoor02")
                .SetPiece("BridgeDoor01")
                .SetPiece("FenceDoor01")
                .SetPiece("FenceDoor02")
                .ExitGroup("Exit01")
                .ExitGroup("Exit02");

            // D20 Modern Facility (tbx78) -- see this file's Facility comment for the
            // full probe writeup (DoorSlotCrossers rationale, OpenLane-only Tunnel verdict, room-size
            // floor, ExitGroup evidence, hand-built lighting).
            _builder.Create(ModernFacility, "D20 Modern Facility")
                .Tileset("tbx78")
                .SetPieceRoomCornerFloor(7)
                .DoorSlotCrossers("doorway1", "doorway2", "doorway3", "cell", "raised")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                // ladder_up/ladder_dwn/room2x1/stairs_up/room/stairs_dwn/room3x1/door_transition all now
                // structurally CLASSIFY as WallRoom (LayoutGroupStamper's group classification reads
                // MacroLayoutParameters.DoorSlotCrossers the same way CornerEdgeResolver's ungrouped-tile
                // path always has -- see LayoutGroupStamper.IsDoorwayEdge -- so their "doorway1"/
                // "doorway2" perimeter edge is recognized instead of rejected). They are deliberately
                // left UNWIRED here though: this tileset declares no Tunnel-mode corridor family, and
                // verified directly (TileResolver.HasCandidate against the real tile inventory) that it
                // ALSO has no ungrouped boundary tile shape pairing solid/open/open/solid corners with a
                // doorway-family port edge -- SupportsWallRoomOpenLaneBoundary's own probe -- so
                // IsWallRoomSiteValid can never find a legal site for ANY WallRoom group in this tileset,
                // by construction, not just empirically (measured 0/30 seeds via a real LayoutSolver run
                // before reverting the wiring). Wiring these as SetPieces would be dead weight that only
                // perturbs the RNG stream for OTHER groups (each failed site search still Shuffles every
                // candidate anchor) with zero placed content. TileCoverageCensusTests still credits them
                // (structural classification, independent of profile wiring, matching every other
                // "optional config" mechanism this census already recognizes) -- see this file's own
                // PilotExpectedExemptions entries.
                //
                // "elevator" (TILE66/67) is a GENUINELY DIFFERENT shape and IS wired: TILE66 mixes Solid
                // ("wall") and Open ("facility") corners on the SAME tile while also carrying a
                // "doorway2" edge -- but that edge faces TILE67, its own group-mate, an interior seam,
                // never the group's own perimeter (verified directly against the raw .set data), so it
                // classifies as SetPieceOpenSetPiece via LayoutGroupStamper.TryClassify's mixed/
                // open-member tolerance (see that method's own doc comment) instead of WallRoom --
                // TryPlaceOpenSetPiece's site search needs only an open-terrain room tile, not a
                // corridor/OpenLane boundary, so it is NOT subject to the SupportsWallRoomOpenLaneBoundary
                // gap above. Placement proof: OpenSetPiecePlacementRateTests.
                // ElevatorOnModernFacility_NowPlacesInIsolation.
                .SetPiece("elevator")
                .SetPiece("removed_panel")
                .SetPiece("giant_cage")
                .SetPiece("pillar", 3);

            // Complex laps storage (tqq01) -- display name kept VERBATIM per the toolset's own
            // UnlocalizedName typo. See this file's LabStorage comment for the full
            // probe writeup (real Tunnel-mode support, multi-district descope rationale, room-size floor,
            // ExitGroup evidence, hand-built lighting).
            _builder.Create(LabStorage, "Complex laps storage")
                .Tileset("tqq01")
                .SetPieceRoomCornerFloor(7)
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 3, 3)
                .SetPiece("InnRoom")
                .SetPiece("InnRoom01_1x2")
                .SetPiece("InnRoom02_1x2")
                .SetPiece("DoorInn01")
                .SetPiece("StairsUp")
                .SetPiece("StairsDown")
                .SetPiece("CorridorExitBig")
                .SetPiece("CorridorExit")
                .SetPiece("Portal")
                .SetPiece("Chessboard")
                .SetPiece("Tent")
                .SetPiece("Baracks")
                .SetPiece("Temple Evil")
                .SetPiece("Temple Good")
                .SetPiece("Temple Neutral")
                .SetPiece("Wizards Den")
                .SetPiece("Smithy")
                .SetPiece("Barn")
                .SetPiece("Bordello")
                .SetPiece("Bordello Blank")
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
                .SetPiece("HomeUpper04_2x2")
                .SetPiece("HomeUpper05_2x2")
                .ExitGroup("DoorTrans");

            // Complex laps storage (Livingroom) -- tqq01's Livingroom district PaletteVariant, mirroring
            // udp2's OfficeInteriorsService/Tiled/etc. pattern: recomposes the SAME tqq01 hak data the
            // base LabStorage profile above uses, with PrimaryOpenTerrain("Livingroom") so the district's
            // own Room/Door/CornerStairs/CornerExit family (12 groups) actually places instead of
            // sitting structurally-classifiable-but-never-wired (see BaseGameTilesetProfiles.LabStorage's
            // own doc comment for the census-vs-practice writeup). Group inventory verified directly
            // against the raw .set data: "Livingroom" (1x1, open Doorway-ported room), "Livingroom01_1x2"/
            // "02_1x2"/"03_1x2"/"04_1x2" (2-tile room pairs, blank+Doorway members, same shape as zin01's
            // Room-<Type> family), "DoorLivingroom01" (1x1, mixed Wall/Livingroom corners with a door
            // slot, no crosser -- an OpenSetPiece corner-match shape needing this variant's
            // PrimaryOpenTerrain to resolve), "LivingroomCornerStairsU/D/B" and "LivingroomCornerExit1/2"
            // (the SAME mixed-corner door-slot shape, this district's own stairs/exit family paralleling
            // the base profile's generic StairsUp/StairsDown/CorridorExit/CorridorExitBig), and
            // "Livingroom Blank" (1x1, all-Wall, doorless -- a WallAlcove-adjacent decorative filler
            // already classify-eligible via CornerEdgeResolver regardless of this variant, wired here for
            // completeness since it shares the district's own art). PaletteVariant() excludes this from
            // --matrix's full cross-product (one showcase area instead, matching every other district
            // variant in this file). Placement proof: OpenSetPiecePlacementRateTests.
            // LabStorageDistrictGroups_PlaceInIsolation.
            _builder.Create(LabStorageLivingroom, "Complex laps storage (Livingroom)")
                .Tileset("tqq01")
                .SetPieceRoomCornerFloor(7)
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 3, 3)
                .PaletteVariant()
                .PrimaryOpenTerrain("Livingroom")
                .SetPiece("Livingroom")
                .SetPiece("Livingroom01_1x2")
                .SetPiece("Livingroom02_1x2")
                .SetPiece("Livingroom03_1x2")
                .SetPiece("Livingroom04_1x2")
                .SetPiece("DoorLivingroom01")
                .SetPiece("LivingroomCornerStairsU")
                .SetPiece("LivingroomCornerStairsD")
                .SetPiece("LivingroomCornerStairsB")
                .SetPiece("LivingroomCornerExit1")
                .SetPiece("LivingroomCornerExit2")
                .SetPiece("Livingroom Blank")
                .ExitGroup("DoorTrans");

            // Complex laps storage (Kitchen) -- tqq01's Kitchen district PaletteVariant, same pattern as
            // Livingroom above. Group inventory (9 groups, verified against the raw .set data):
            // "KitchenRoom"/"KitchenRoom01_1x2"/"KitchenRoom02_1x2" (open Doorway-ported room family),
            // "DoorKitchen01" (mixed Wall/Kitchen corners, door slot, no crosser -- OpenSetPiece
            // corner-match shape), "KitchenCornerStairsB/U/D" and "KitchenCornerExit1/2" (this district's
            // own stairs/exit family, identical mixed-corner door-slot shape). Placement proof:
            // OpenSetPiecePlacementRateTests.LabStorageDistrictGroups_PlaceInIsolation.
            _builder.Create(LabStorageKitchen, "Complex laps storage (Kitchen)")
                .Tileset("tqq01")
                .SetPieceRoomCornerFloor(7)
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 3, 3)
                .PaletteVariant()
                .PrimaryOpenTerrain("Kitchen")
                .SetPiece("KitchenRoom")
                .SetPiece("KitchenRoom01_1x2")
                .SetPiece("KitchenRoom02_1x2")
                .SetPiece("DoorKitchen01")
                .SetPiece("KitchenCornerStairsB")
                .SetPiece("KitchenCornerStairsU")
                .SetPiece("KitchenCornerStairsD")
                .SetPiece("KitchenCornerExit1")
                .SetPiece("KitchenCornerExit2")
                .ExitGroup("DoorTrans");

            // Complex laps storage (Shop) -- tqq01's Shop district PaletteVariant, same pattern as
            // Livingroom/Kitchen above. Group inventory (6 groups, verified against the raw .set data):
            // "ShopRoom"/"ShopRoom01_1x2"/"ShopRoom02_1x2"/"Shop01_1x2"/"Shop02_1x2" (open Doorway-ported
            // room family -- Shop's naming is less regular than Livingroom/Kitchen's, both "Shop*_1x2"
            // and "ShopRoom*_1x2" spellings are real, distinct groups, verified directly), "DoorShop01"
            // (mixed Wall/Shop corners, door slot, no crosser -- OpenSetPiece corner-match shape). Unlike
            // Livingroom/Kitchen, Shop has NO district-specific CornerStairs/CornerExit family (verified:
            // no "Shop*Corner*" group exists anywhere in the .set) -- it relies on the base profile's
            // generic StairsUp/StairsDown/CorridorExit/CorridorExitBig the same way Inn does. Placement
            // proof: OpenSetPiecePlacementRateTests.LabStorageDistrictGroups_PlaceInIsolation.
            _builder.Create(LabStorageShop, "Complex laps storage (Shop)")
                .Tileset("tqq01")
                .SetPieceRoomCornerFloor(7)
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 3, 3)
                .PaletteVariant()
                .PrimaryOpenTerrain("Shop")
                .SetPiece("ShopRoom")
                .SetPiece("ShopRoom01_1x2")
                .SetPiece("ShopRoom02_1x2")
                .SetPiece("Shop01_1x2")
                .SetPiece("Shop02_1x2")
                .SetPiece("DoorShop01")
                .ExitGroup("DoorTrans");

            // D20 Office Interiors UDP (udp2) -- see this file's OfficeInteriors comment
            // (OfficeInteriors) for the full probe writeup (DoorSlotCrossers requirement, OpenLane-only
            // Tunnel verdict, multi-district descope rationale, room-size floor, hand-built lighting).
            _builder.Create(OfficeInteriors, "D20 Office Interiors UDP")
                .Tileset("udp2")
                .SetPieceRoomCornerFloor(6)
                .DoorSlotCrossers("Door", "Door_Garage_Sm", "Door_Garage_Lg")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                // Door-bearing groups (SmRm1/SmRm2/MidRm1/MidRm2, Elevator1/2, Stairwell_U/UD/D,
                // Restrooms, Break_Room) now structurally CLASSIFY as WallRoom the same way tbx78's do
                // (LayoutGroupStamper.IsDoorwayEdge recognizes "Door" identically to the literal
                // canonical "Doorway" string). They stay UNWIRED here for the identical reason documented
                // on BaseGameTilesetProfiles.ModernFacility above: this tileset declares no Tunnel-mode
                // corridor family and has no ungrouped boundary tile shape supporting an OpenLane
                // WallRoom site (verified directly via TileResolver.HasCandidate against the real tile
                // inventory; measured 0/30 seeds via a real LayoutSolver run before reverting the
                // wiring), so IsWallRoomSiteValid can never find a legal site for any WallRoom group here
                // either. TileCoverageCensusTests still credits them structurally -- see this file's own
                // PilotExpectedExemptions/PilotAlternateVocabCrossers entries.
                //
                // "Office_Vinyl_Entry 2x1" is a GENUINELY DIFFERENT shape and IS wired: an all-Wall
                // member paired with an Office_Vinyl-open member whose sole "Door" edge faces its own
                // group-mate (interior, never perimeter -- verified directly against the raw .set data),
                // so it classifies as SetPieceOpenSetPiece via LayoutGroupStamper.TryClassify's
                // mixed/open-member tolerance (see that method's own doc comment) instead of WallRoom --
                // TryPlaceOpenSetPiece's site search needs only an open-terrain room tile, not a
                // corridor/OpenLane boundary, so it is NOT subject to the SupportsWallRoomOpenLaneBoundary
                // gap above. Placement proof: OpenSetPiecePlacementRateTests.
                // OfficeVinylEntryOnOfficeInteriors_NowPlacesInIsolation.
                .SetPiece("Office_Vinyl_Entry 2x1")
                .SetPiece("Office_Vinyl_Win")
                .SetPiece("Office_Vinyl_WinCrnr")
                .SetPiece("Office_Vinyl_Firepl")
                .SetPiece("Office_Vinyl_Stair_UD")
                .SetPiece("Office_Vinyl_Stair_U")
                .SetPiece("Office_Vinyl_Stair_D")
                .SetPiece("Office_Vinyl_Stair2_UD")
                .SetPiece("Office_Vinyl_Stair2_U")
                .SetPiece("Office_Vinyl_Stair2_D")
                .SetPiece("Hallway1_Entry 2x1")
                .SetPiece("Hallway2_Entry 2x1");

            // D20 Office Interiors UDP (Service/Tiled/Office_Wood/Office_Alum) -- udp2's four remaining
            // full-size district palettes, PaletteVariant profiles recomposing the SAME udp2 hak data the
            // base OfficeInteriors profile above uses. See OfficeInteriorsService's own doc comment
            // above for the full probe writeup (tile-for-tile parity with Office_Vinyl's own group
            // family, the WallRoom door-group classify-but-never-place verdict). PaletteVariant()
            // excludes each from --matrix's full cross-product -- one showcase area each instead. Each
            // district's own "*_Entry 2x1" pair is wired too, same shape/reasoning as Office_Vinyl_Entry
            // above (all-Wall member + open member whose sole "Door" edge faces its own group-mate,
            // interior-only, verified directly against this district's own raw .set data).
            _builder.Create(OfficeInteriorsService, "D20 Office Interiors UDP (Service)")
                .Tileset("udp2")
                .SetPieceRoomCornerFloor(6)
                .DoorSlotCrossers("Door", "Door_Garage_Sm", "Door_Garage_Lg")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .PaletteVariant()
                .PrimaryOpenTerrain("Service")
                .SetPiece("Service_Entry 2x1")
                .SetPiece("Service_Win")
                .SetPiece("Service_WinCrnr")
                .SetPiece("Service_Firepl")
                .SetPiece("Service_Stair_UD")
                .SetPiece("Service_Stair_U")
                .SetPiece("Service_Stair_D")
                .SetPiece("Service_Stair2_UD")
                .SetPiece("Service_Stair2_U")
                .SetPiece("Service_Stair2_D");

            _builder.Create(OfficeInteriorsTiled, "D20 Office Interiors UDP (Tiled)")
                .Tileset("udp2")
                .SetPieceRoomCornerFloor(6)
                .DoorSlotCrossers("Door", "Door_Garage_Sm", "Door_Garage_Lg")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .PaletteVariant()
                .PrimaryOpenTerrain("Tiled")
                .SetPiece("Tiled_Entry 2x1")
                .SetPiece("Tiled_Win")
                .SetPiece("Tiled_WinCrnr")
                .SetPiece("Tiled_Firepl")
                .SetPiece("Tiled_Stair_UD")
                .SetPiece("Tiled_Stair_U")
                .SetPiece("Tiled_Stair_D")
                .SetPiece("Tiled_Stair2_UD")
                .SetPiece("Tiled_Stair2_U")
                .SetPiece("Tiled_Stair2_D");

            _builder.Create(OfficeInteriorsOfficeWood, "D20 Office Interiors UDP (Office Wood)")
                .Tileset("udp2")
                .SetPieceRoomCornerFloor(6)
                .DoorSlotCrossers("Door", "Door_Garage_Sm", "Door_Garage_Lg")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .PaletteVariant()
                .PrimaryOpenTerrain("Office_Wood")
                .SetPiece("Office_Wood_Entry 2x1")
                .SetPiece("Office_Wood_Win")
                .SetPiece("Office_Wood_WinCrnr")
                .SetPiece("Office_Wood_Firepl")
                .SetPiece("Office_Wood_Stair_UD")
                .SetPiece("Office_Wood_Stair_U")
                .SetPiece("Office_Wood_Stair_D")
                .SetPiece("Office_Wood_Stair2_UD")
                .SetPiece("Office_Wood_Stair2_U")
                .SetPiece("Office_Wood_Stair2_D");

            _builder.Create(OfficeInteriorsOfficeAlum, "D20 Office Interiors UDP (Office Alum)")
                .Tileset("udp2")
                .SetPieceRoomCornerFloor(6)
                .DoorSlotCrossers("Door", "Door_Garage_Sm", "Door_Garage_Lg")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .PaletteVariant()
                .PrimaryOpenTerrain("Office_Alum")
                .SetPiece("Office_Alum_Entry 2x1")
                .SetPiece("Office_Alum_Win")
                .SetPiece("Office_Alum_WinCrnr")
                .SetPiece("Office_Alum_Firepl")
                .SetPiece("Office_Alum_Stair_UD")
                .SetPiece("Office_Alum_Stair_U")
                .SetPiece("Office_Alum_Stair_D")
                .SetPiece("Office_Alum_Stair2_UD")
                .SetPiece("Office_Alum_Stair2_U")
                .SetPiece("Office_Alum_Stair2_D");

            // D20 Office Interiors UDP (Foyer L/Foyer U) -- udp2's two smaller foyer districts (7 groups
            // each instead of the 14-group full-size family above): Entry 2x1/Win/WinCrnr/Firepl plus a
            // ONE-DIRECTION stair trio (Foyer_L only ever carries the "_U" (up) member of Stair/Stair2/
            // Grandstair, Foyer_U only the "_D" (down) member -- verified directly, neither district has
            // the other's UD/opposite-direction piece). Same door-group classify-but-never-place descope
            // as the four full-size districts above for Stair/Stair2/Grandstair; Entry 2x1 is wired
            // (same OpenSetPiece shape/reasoning as Office_Vinyl_Entry above).
            _builder.Create(OfficeInteriorsFoyerL, "D20 Office Interiors UDP (Foyer L)")
                .Tileset("udp2")
                .SetPieceRoomCornerFloor(6)
                .DoorSlotCrossers("Door", "Door_Garage_Sm", "Door_Garage_Lg")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .PaletteVariant()
                .PrimaryOpenTerrain("Foyer_L")
                .SetPiece("Foyer_L_Entry 2x1")
                .SetPiece("Foyer_L_Win")
                .SetPiece("Foyer_L_WinCrnr")
                .SetPiece("Foyer_L_Firepl")
                .SetPiece("Foyer_L_Stair_U")
                .SetPiece("Foyer_L_Stair2_U")
                .SetPiece("Foyer_L_Grandstair_U");

            _builder.Create(OfficeInteriorsFoyerU, "D20 Office Interiors UDP (Foyer U)")
                .Tileset("udp2")
                .SetPieceRoomCornerFloor(6)
                .DoorSlotCrossers("Door", "Door_Garage_Sm", "Door_Garage_Lg")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                // PathNodeOpeningWidthAudit (fresh against udp2's real pathnode data, Solid=Wall/
                // Open=Foyer_U) computes 2, not the default 1: unlike the other five districts, none of
                // Foyer_U's crosser-free ungrouped partial-open Wall/Foyer_U tiles carry a pathnode-'A'
                // node -- locked in by the minimum-opening-width path-node audit coverage.
                .MinimumOpeningWidth(2)
                .PaletteVariant()
                .PrimaryOpenTerrain("Foyer_U")
                .SetPiece("Foyer_U_Entry 2x1")
                .SetPiece("Foyer_U_Win")
                .SetPiece("Foyer_U_WinCrnr")
                .SetPiece("Foyer_U_Firepl")
                .SetPiece("Foyer_U_Stair_D")
                .SetPiece("Foyer_U_Stair2_D")
                .SetPiece("Foyer_U_Grandstair_D");

            // Jacoby's Jungle (jac01, SWLOR_Haks/sw_t_jungle, 380 tiles, HasHeightTransition=1). A lean
            // sibling of Forest/ttf01: same degenerate GENERAL quirk (Border=Default=Floor="Forest",
            // the walkable ground, not a wall; the fully-Cliff tile is pathnode-restricted, verified
            // directly against the raw .set pathnode data), so the same INVERTED composition applies:
            // SolidTerrainOverride("Cliff") + PrimaryOpenTerrain("Forest").
            //
            // Terrains(7)=Cliff,Forest,Pit,Water,Trees,Platform,HighForest; Crossers(5)=Wall,Road,
            // Stream,Bridge,Hills -- roughly a third of ttf01's 11-terrain/13-crosser superset, and the
            // group vocabulary echoes ttf01/ttf02 tile-for-tile in most families (Ruin01_2x2/Ruin02_1x2/
            // Ruin, Temple_3x2, Shack01_2x2/Shack02_1x2, Lodge_2x2, Camp01_2x2/Camp02_1x2/Camp,
            // Graveyard_1x2/Graveyard, Meeting_Area, Grove01_3x3, Exit01_2x3/Exit02_2x2/Exit,
            // WebbedForest/WebbedCorner, BigTree, Chessboard, Portal, BridgeDoor01, WallGate01/02,
            // StreamBridge01/02, Ramp, Cave -- same names ttf02's own profile above wires). No Tunnel
            // vocabulary under the Cliff solid: every crosser family (Wall/Road/Stream/Bridge/Hills,
            // same-name body/port pairs) resolves only against Solid=Forest compositions (verified
            // directly, mirroring ttd01/ttf01/ttf02's own TunnelVocabularyCheck result) -- Complex's
            // Tunnel mode downgrades to OpenLane.
            //
            // AccentTerrain("Water") gives LayoutAccentPainter blob lakes (114 corner instances,
            // mirroring ttf01's Water accent role). ChannelTerrain("Pit") is the Bridge-gated channel
            // (207 corner instances) -- "BridgeDoor01" (1x1, all-Pit, one door) is the channel door,
            // the same shape as ttf01's "Door - Bridge, Pit".
            //
            // Heights (56 non-flat tiles of 380, all on Forest open terrain -- Cliff never carries a
            // nonzero corner height, verified directly): MaxElevationRegions(2) (crosser-free raised-
            // Forest rim tiles), RampCrosser("Hills") -- jac01's own ramp-lane crosser name (its analog
            // of ttd01's "Dunes"/ttf01's "Slope"), MaxReliefRegions(2). The raised 1x1 "Ramp"/"Cave"
            // groups and the raised Halfling Window Sidehill/Halfling Door Sidehill/Halfling Door
            // Corner/Halfling Window Corner/Hill Corner Door groups (all all-Forest-cornered, each
            // exactly one non-flat member, door-tolerant per LayoutGroupStamper.TryClassifyReliefPiece's
            // now-generalized rule) classify as ReliefPieces stamped onto painted raised rim edges, the
            // same mechanism ttd01's "Ramp"/"SmallCave" and ttf01's "Ramp"/"Cave" use. "Hills w/Road"
            // (TILE184, raised, carrying BOTH Hills AND Road edges on the same cell) stays exempt: a
            // dual-crosser conflict no single composition can express -- the identical shape as ttd01's
            // TILE255 (Dunes+Road) and ttf01's TILE606-609 (Slope+Road).
            //
            // Groups: WallGate01/02 (Wall+Road) and StreamBridge01/02 (Stream+Bridge) are two-
            // independent-crosser-family crossroads cells -- the same shape as Desert's WallGate01/02/
            // TrenchBridge01/02 and Forest-Facelift's WallGate01/02/StreamBridge01/02, stays exempt (no
            // mechanism models a two-family intersection cell). "Pit Tower" (all-Pit, no door) sits
            // purely on the Bridge-gated channel terrain with no Solid or Open corner anywhere in the
            // group -- ClassifySetPiece's Solid/Open binary never triggers (the channel-only-group gap
            // ttf01's own "Island"/"Island_Tree" family already documents) -- stays exempt. Its door-
            // bearing sibling "AirshipAbovePit_3x1" (also all-Pit, 3 tiles) DOES classify (verified
            // directly), so the gap is specific to doorless all-channel groups. "CarrackD_4x1" and
            // "CaravelFloating_3x1" (both all-Water, boats/wrecks) are the same accent-terrain-only-
            // group gap -- AccentTerrain is a painted overlay, not a Solid/Open composition member --
            // both stay exempt. Everything else classifies: the all-Forest building/decor families as
            // OpenSetPieces, the Cliff+Forest mixed groups (Exit01_2x3/Exit02_2x2/Exit/WebbedCorner/
            // CliffStair) as OpenSetPieces/ExitGroups too (the same Desert+Cliff mixed-group precedent
            // Desert's own doc comment describes), and single-tile Forest+Pit mixed groups (PitStair,
            // half-Forest/half-Pit corners) as OpenSetPieces.
            //
            // Two further residuals, verified directly against the raw .set data (PilotEveryTileIsReach
            // ableOrExplicitlyExempted's own UNCLASSIFIED report): "Log Bridge_1x3" and "Suspension
            // Bridge_1x3" each have a uniformly-Pit-cornered MIDDLE tile (TILE280/378 -- no Solid or
            // Open corner at all) flanked by two half-Forest/half-Pit bank tiles; unlike PitStair's
            // single-tile half-and-half shape, ClassifyMultiTileSetPiece's Solid/Open binary never
            // triggers once any one member is a pure-channel tile, so both 3-tile groups stay exempt
            // (the same channel-only-member gap "Pit Tower" hits above, just on a mixed-member group
            // this time). "Walkthrough Tree" (TILE292, a single-tile GROUP) is an
            // all-Forest-cornered tile carrying an opposite-edge Road pair -- CorridorInsert's body-
            // crosser shapes require all-SOLID corners (this tile is all-OPEN), and being GROUP-wrapped
            // (GroupIndex != -1) excludes it from CornerEdgeResolver, which only registers ungrouped
            // tiles -- the same "single-tile boxed-into-a-GROUP resolver-eligible shape" gap ttf01's
            // own "Tower - Archer, Forest Wall/Corner" family already documents.
            //
            // "Trees" (42 corner instances) and "HighForest" (45 corner instances) never appear on any
            // GROUP (verified directly) -- every ungrouped tile carrying them was already
            // CornerEdgeResolver-reachable regardless of declared vocabulary (the same "Marsh"/
            // "HighForest" simple-tile gap ttf01's own doc comment describes), so neither needs a
            // dedicated PilotAlternateVocabTerrains entry or a PaletteVariant on its own.
            //
            // Lighting sampled directly from all 5 hand-built module areas stamping jac01
            // (dath_cz_baseok, dath_landingpad, moncalajungelsu, moncalawildjungl, yavin -- 921 tiles
            // total): every single tile carries MainLight1=1, MainLight2=2, SourceLight1=1,
            // SourceLight2=2, uniformly.
            //
            // Decoration palette mined from the same 5 areas' placeable inventories (functional/
            // spawn-marker resrefs like plc_arrowcorpse/creature_spaw001 excluded): the zep_*/
            // _mdrn_pl_* families ttd01's own bulk palette already uses recur here too (zep_shrub036,
            // zep_giantfern, _mdrn_pl_pillr05/_mdrn_pl_lamp5). The areas' own "x0_ivy"/"nw_plc_palm02"
            // placeables have no utp blueprint in this module (verified via
            // AllDungeonDefinitions_DecorationsExistAndAreVisible) -- substituted with the nearest
            // blueprint-backed equivalents already in the module (zep_vinesh, zep_tree070).
            _builder.Create(Jungle, "Jacoby's Jungle")
                .Tileset("jac01")
                .SolidTerrainOverride("Cliff")
                .PrimaryOpenTerrain("Forest")
                .MaxElevationRegions(2)
                .MaxReliefRegions(2)
                .RampCrosser("Hills")
                .Placeholder("gen_placeholder1")
                .TileLighting(1, 2, 1, 2)
                .AccentTerrain("Water")
                .ChannelTerrain("Pit")
                .FeatureTile("Ruin")
                .FeatureTile("Camp")
                .FeatureTile("Graveyard")
                .FeatureTile("WebbedForest")
                .FeatureTile("BigTree")
                .FeatureTile("Chessboard")
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
                .SetPiece("Mayan Tomb")
                .SetPiece("Jungle Elevator", 1)
                .SetPiece("AirshipAbovePit_3x1", 1)
                .SetPiece("AirshipDocked1_3x1", 1)
                .SetPiece("Halfling Window Sidehill", 1)
                .SetPiece("Halfling Door Sidehill", 1)
                .SetPiece("Halfling Door Corner", 1)
                .SetPiece("Halfling Window Corner", 1)
                .SetPiece("Hill Corner Door", 1)
                // Baked-mesh raised ramp/cave-mouth pieces -- same ReliefPiece kind as ttd01's "Ramp"/
                // "SmallCave" and ttf01's "Ramp"/"Cave", stamped onto a painted raised rim edge.
                .SetPiece("Ramp", 1)
                .SetPiece("Cave", 1)
                .SetPiece("RuinedRamp", 1)
                .ExitGroup("Exit")
                .ExitGroup("CliffStair")
                .ExitGroup("PitStair")
                .ExitGroup("Tower");

            // Jungle's own bulk palette -- mined from jac01's 5 hand-built reference areas
            // (dath_cz_baseok/dath_landingpad/moncalajungelsu/moncalawildjungl/yavin), functional/
            // spawn-marker resrefs (plc_arrowcorpse, creature_spaw001) excluded. Shares ttd01's own
            // zep_*/_mdrn_pl_* decoration families.
            _builder
                .Decoration("zep_shrub036", 3, DecorationContext.WallAdjacent)
                .Decoration("_mdrn_pl_pillr05", 2, DecorationContext.WallAdjacent)
                .Decoration("_mdrn_pl_barr001", 1, DecorationContext.WallAdjacent)
                .Decoration("zep_giantfern", 2, DecorationContext.CorridorSide)
                .Decoration("zep_vinesh", 2, DecorationContext.CorridorSide)
                .Decoration("zep_tno_rockldg2", 1, DecorationContext.CorridorSide)
                .Decoration("zep_tree070", 1, DecorationContext.RoomCenter)
                .Decoration("zep_log001", 1, DecorationContext.RoomCenter)
                .Decoration("_mdrn_pl_lamp5", 2, DecorationContext.DoorwayFlank)
                .Decoration("zep_arch002", 2, DecorationContext.DoorwayFlank)
                .Vignette("JungleRuinCluster", 2)
                .VignetteMember("_mdrn_pl_pillr05", 0f, 0f)
                .VignetteMember("zep_vinesh", 0.5f, 0.2f);

            // Jungle (Platform) -- jac01's "Platform" chasm-bridge district, a PaletteVariant profile
            // recomposing the SAME jac01 hak data the base Jungle profile above uses. Mirrors ttf01's
            // own "Forest (Platform)" variant exactly: SolidTerrainOverride("Pit") +
            // PrimaryOpenTerrain("Platform") closes the GROUPS that fail the base profile's Cliff/
            // Forest Solid/Open binary because they use Platform+Pit corners instead (Platform House2,
            // Platform Boss House 2x3, Guard Tower 1x2, Platform Pillar, Platform Elevator, Platform
            // Column, Platform House, Platform Portal, Meeting Place -- all all-{Pit,Platform}-cornered,
            // verified directly). "Platform Cliff Dwellings 2x3" mixes Cliff+Pit+Platform (three
            // terrains on one group -- no two-terrain classifier reaches it) and stays exempt, the same
            // shape as ttf01's own "Platform - Cliff Section" residual. Its similarly Cliff-mixed
            // sibling "Platform Cliff Door" (Cliff+Platform, no Pit corner, one door) DOES classify
            // (verified directly), wired below as an ordinary SetPiece.
            _builder.Create(JunglePlatform, "Jacoby's Jungle (Platform)")
                .Tileset("jac01")
                .Placeholder("gen_placeholder1")
                .TileLighting(1, 2, 1, 2)
                .PaletteVariant()
                .SolidTerrainOverride("Pit")
                .PrimaryOpenTerrain("Platform")
                .SetPiece("Platform House2", 1)
                .SetPiece("Platform Boss House 2x3", 1)
                .SetPiece("Guard Tower 1x2", 1)
                .SetPiece("Platform Pillar", 1)
                .SetPiece("Platform Elevator", 1)
                .SetPiece("Platform Column", 1)
                .SetPiece("Platform House", 1)
                .SetPiece("Platform Portal", 1)
                .SetPiece("Meeting Place", 1)
                .SetPiece("Platform Cliff Door", 1);

            // Rural Grass (ttr01, SWLOR_Haks/sw_t_rural, 653 tiles/91 groups, HasHeightTransition=1,
            // UnlocalizedName "Rural Grass*"). See this file's Jungle-adjacent constants comment above
            // for the full composition writeup (no Cliff-equivalent wall mass -- Grass reaches full
            // 16/16 against every other terrain, and none of the six minor terrains carries a
            // wall/rock-scale GROUP inventory). SolidTerrainOverride left UNSET: LayoutSolver.Solve
            // stamps Solid=tileset.DefaultTerrain="Grass" == PrimaryOpenTerrain("Grass"), a genuinely
            // open field. Verified via a real pipeline sweep (ProbeTool, 15 seeds x Complex/Halls/
            // Organic, 45/45 succeeded) rather than trusting the 16/16 table alone.
            //
            // AccentTerrain("Water") gives LayoutAccentPainter blob ponds/lakes: Water's own bank
            // tiles blend freely with Grass (and a 3-way Grass/Water/Trees shoreline blend exists too,
            // all ungrouped and crosser-free -- verified directly). ReliefBlendTerrain("GentleHill")
            // matches GentleHill's own real usage exactly: 32 tiles (TILE500-532), every one an
            // ungrouped, crosser-free, ordinary-pathnode Grass/GentleHill corner blend with height 0/1
            // variance -- the per-corner "slope blend" shape LayoutReliefPainter targets, not a
            // Cliff-style solid mass (GentleHill is never used on any GROUP). RampCrosser("Slope")
            // matches the OTHER raised-tile family: TILE554-568, all-Grass-cornered with height
            // variance and a "Slope" edge crosser -- the ramp-lane analog of ttd01's "Dunes"/ttf01's
            // "Slope"/jac01's "Hills". MaxReliefRegions(2) mirrors jac01/Dungeon's own cap.
            // RoadCrosser("Road") -- RoadVocabularyCheck.SupportsRoads(Grass, "Road") verified true
            // directly (stub/straight/turn/T/X all resolve). No canonical "Doorway"/"Corridor" crosser
            // exists anywhere in this tileset (verified directly), so Complex downgrades to OpenLane,
            // the same verdict as the earlier exterior profiles (ttd01/ttf01/jac01).
            //
            // FeatureTiles are the ~24 solo, flat, crosser-free, pathnode-'A', all-Grass 1x1 groups
            // (ambient dressing: Anthill/Chessboard/Cobbles/Crystal-Platform/Crystal-Sunken/Field/
            // Fountain/Garden 1-2/Granary/Graves 1-5/Menhir/Orchard/Portal/Shrine 1-2/Tower-Archer/
            // Tower-Rural/Tree/Tree-Hollow/Turf House/Wagon-Caravan 1/Warzone 1-2/Well).
            // ExitGroups are the solo, flat, crosser-free, door-bearing 1x1 groups (House 1-2/
            // Mausoleum 1-2/Wagon-Caravan 2). SetPieces are every multi-tile all-Grass building/decor
            // group (Barn/Barracks/Dragon Skeleton/Farm/Field/Inn/Ship-Air Docked/Temple/Tower-Cloak/
            // Guard/Large/Rural/Wizard/Warzone/Windmill) plus the baked-mesh raised "Ramp"/"Cave"
            // pieces (same ReliefPiece kind as ttd01/ttf01/jac01's own "Ramp"/"Cave"/"SmallCave"
            // precedent -- stamped onto a painted raised rim edge, not auto-classified).
            //
            // Footbridge/Stream, Ruined Cart/Road, the four Tower - Archer, Rural Wall 1/2 (Corner)
            // pieces, Wall - Gate, Rural 1/2, and Wall - Road Gate, Rural 1/2 are DELIBERATELY NOT
            // wired as SetPieces, despite all being structurally WallRoom-classify-eligible once
            // Wall1/Wall2/Stream/Road are recognized as door-implying crossers (verified directly).
            // This tileset declares no canonical "Doorway"/"Corridor" crosser at all, so Complex/Halls/
            // Organic all downgrade Tunnel corridors to OpenLane -- and LayoutGroupStamper's WallRoom
            // kind exists to hang a group off a Tunnel corridor's wall face, which never carves here.
            // A direct isolated-placement probe (ProbeTool, 100 seeds x Complex/Halls/Organic, all six
            // groups) measured 0/100 on every single pairing: with no Tunnel-mode wall mass anywhere in
            // the generated grid, WallRoom's site search has nowhere to attach regardless of
            // classification eligibility. Registering these as SetPieces would be dead weight per the
            // project's placement-honesty convention (0/N placements is an exemption with the proof,
            // not a closure) -- they stay census-exempt via PilotExpectedExemptions instead. See
            // TileCoverageCensusTests.PilotExpectedExemptions' own ttr01 doc comment for the full
            // writeup (including "Ship - Air, Above Trees/Water" and "Ship - Floating", RuralGrassWater's
            // own analogous WallRoom-shaped residuals, and the TILE229/TILE179 shared-tile-id
            // accounting).
            //
            // Lighting sampled directly from all 8 hand-built module areas stamping ttr01
            // (dan_jungle1, dmfi_custom_enc, prefabgridgrass, prefabgridwater, vrotrdantcourt,
            // vrotrdantfarms, vrotrdantkhoonda, vrotrdantplains -- 840 tiles total): every single tile
            // carries MainLight1=0, MainLight2=0, SourceLight1=0, SourceLight2=0, uniformly (an
            // outdoor daylight field needs no baked light sourcing, unlike Forest/Jungle's "1,2,1,2").
            //
            // Decoration palette mined from the same 8 areas' placeable inventories (functional/loot/
            // spawn-marker resrefs -- lockedcrate001, box027, terminal, swlor_0103/0175 -- excluded):
            // dominated by the same zep_*/_mdrn_pl_* families ttd01/jac01's own bulk palettes already
            // use. The areas' own "x3_plc_tree003"/"x0_stonecircle" have no utp blueprint in this
            // module (verified via AllDungeonDefinitions_DecorationsExistAndAreVisible, the same jac01
            // "x0_ivy"/"nw_plc_palm02" gap its own doc comment describes) -- substituted with the
            // nearest blueprint-backed equivalents already in the module (zep_tree003, zep_stones018).
            _builder.Create(RuralGrass, "Rural Grass*")
                .Tileset("ttr01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                // Family AREA atmosphere -- the Dantooine-style open-grassland daylight tuple,
                // mined from the hand-built ttr01 exemplars: dmfi_custom_enc, vrotrdantcourt,
                // area042, vrotrdantkhoonda, area043 (5 of 8 module areas agree exactly on the
                // full core tuple; every other tuple is a singleton). No skybox row, live
                // day/night cycle, neutral pale-blue sun ambient with white diffuse, fog amounts 0,
                // no wind; LightingScheme 0 / ShadowOpacity 50 / FogClipDist 45 are unanimous
                // among the agreeing areas.
                .Atmosphere(a =>
                {
                    a.SkyBox = 0;
                    a.DayNightCycle = true;
                    a.IsNight = false;
                    a.SunAmbientColor = 6566450;
                    a.SunDiffuseColor = 16777215;
                    a.MoonAmbientColor = 0;
                    a.MoonDiffuseColor = 13132900;
                    a.SunFogAmount = 0;
                    a.SunFogColor = 9535080;
                    a.MoonFogAmount = 0;
                    a.MoonFogColor = 6566450;
                    a.SunShadows = true;
                    a.MoonShadows = true;
                    a.ShadowOpacity = 50;
                    a.WindPower = 0;
                    a.LightingScheme = 0;
                    a.FogClipDist = 45f;
                })
                .PrimaryOpenTerrain("Grass")
                .AccentTerrain("Water")
                .ReliefBlendTerrain("GentleHill")
                .RampCrosser("Slope")
                .MaxReliefRegions(2)
                .RoadCrosser("Road")
                .FeatureTile("Anthill")
                .FeatureTile("Chessboard")
                .FeatureTile("Cobbles")
                .FeatureTile("Crystal - Platform")
                .FeatureTile("Crystal - Sunken")
                .FeatureTile("Field")
                .FeatureTile("Fountain")
                .FeatureTile("Garden 1")
                .FeatureTile("Garden 2")
                .FeatureTile("Granary")
                .FeatureTile("Graves 1")
                .FeatureTile("Graves 2")
                .FeatureTile("Graves 3")
                .FeatureTile("Graves 4")
                .FeatureTile("Graves 5")
                .FeatureTile("Menhir")
                .FeatureTile("Orchard")
                .FeatureTile("Portal")
                .FeatureTile("Shrine 1")
                .FeatureTile("Shrine 2")
                .FeatureTile("Tower - Archer")
                .FeatureTile("Tower - Rural")
                .FeatureTile("Tree")
                .FeatureTile("Tree - Hollow")
                .FeatureTile("Turf House")
                .FeatureTile("Wagon - Caravan 1")
                .FeatureTile("Warzone 1")
                .FeatureTile("Warzone 2")
                .FeatureTile("Well")
                .ExitGroup("House 1")
                .ExitGroup("House 2")
                .ExitGroup("Mausoleum 1")
                .ExitGroup("Mausoleum 2")
                .ExitGroup("Wagon - Caravan 2")
                .SetPiece("Barn 1 (2x2)", 1)
                .SetPiece("Barn 2 (1x2)", 1)
                .SetPiece("Barn 3 (1x2)", 1)
                .SetPiece("Barracks 1 (1x2)", 1)
                .SetPiece("Barracks 2 (2x2)", 1)
                .SetPiece("Dragon Skeleton (1x2)", 1)
                .SetPiece("Farm 1 (2x2)", 1)
                .SetPiece("Farm 2 (1x2)", 1)
                .SetPiece("Farm 3 (1x2)", 1)
                .SetPiece("Field 1 (2x2)", 1)
                .SetPiece("Field 2 (2x2)", 1)
                .SetPiece("Field 3 (2x1)", 1)
                .SetPiece("Inn (1x2)", 1)
                .SetPiece("Ship - Air, Docked (3x1)", 1)
                .SetPiece("Temple - Evil (2x3)", 1)
                .SetPiece("Temple - Good (3x3)", 1)
                .SetPiece("Temple - Neutral (2x2)", 1)
                .SetPiece("Temple - Rural 1 (3x2)", 1)
                .SetPiece("Temple - Rural 2 (2x2)", 1)
                .SetPiece("Temple - Rural 3 (3x2)", 1)
                .SetPiece("Tower - Cloak (2x2)", 1)
                .SetPiece("Tower - Guard (1x2)", 1)
                .SetPiece("Tower - Large 1, Evil (2x2)", 1)
                .SetPiece("Tower - Large 1, Wizard (2x2)", 1)
                .SetPiece("Tower - Large 2, Evil (2x2)", 1)
                .SetPiece("Tower - Large 2, Wizard (2x2)", 1)
                .SetPiece("Tower - Rural (1x2)", 1)
                .SetPiece("Tower - Wizard (1x2)", 1)
                .SetPiece("Warzone (1x2)", 1)
                .SetPiece("Windmill (2x2)", 1)
                .SetPiece("Ramp", 1)
                .SetPiece("Cave", 1);

            // Rural Grass's own bulk palette -- mined from ttr01's 8 hand-built reference areas
            // (dan_jungle1/dmfi_custom_enc/prefabgridgrass/prefabgridwater/vrotrdantcourt/
            // vrotrdantfarms/vrotrdantkhoonda/vrotrdantplains). Shares ttd01/jac01's own zep_*/
            // _mdrn_pl_* decoration families (see this profile's own doc comment above for the two
            // blueprint substitutions).
            _builder
                .Decoration("zep_shrub036", 3, DecorationContext.WallAdjacent)
                .Decoration("_mdrn_pl_wdfence", 2, DecorationContext.WallAdjacent)
                .Decoration("zep_bpillar007", 1, DecorationContext.WallAdjacent)
                .Decoration("zep_giantfern", 2, DecorationContext.CorridorSide)
                .Decoration("zep_bushfern001", 2, DecorationContext.CorridorSide)
                .Decoration("zep_tree003", 1, DecorationContext.RoomCenter)
                .Decoration("zep_tree060", 1, DecorationContext.RoomCenter)
                .Decoration("zep_treebig", 1, DecorationContext.RoomCenter)
                .Decoration("zep_stones018", 1, DecorationContext.RoomCenter)
                .Decoration("_mdrn_pl_plant07", 2, DecorationContext.DoorwayFlank)
                .Decoration("zep_column004", 1, DecorationContext.DoorwayFlank)
                .Vignette("RuralFarmCluster", 2)
                .VignetteMember("_mdrn_pl_wdfence", 0f, 0f)
                .VignetteMember("zep_shrub036", 0.5f, 0.2f);

            // Rural Grass (Good Castle) / Rural Grass (Evil Castle) -- ttr01's two "district" wall-
            // material palettes, mirroring BaseGameTilesetProfiles.ForestGoodCastle/ForestEvilCastle's
            // shape exactly (see that pair's own doc comment for the full mechanism writeup). Direct
            // 16-combo probe confirms GoodCastle and EvilCastle EACH reach full 16/16 flat corner
            // coverage against Solid=<faction>Castle/Open=Grass. The tileset's own castle inventory is
            // exactly three 1x1 GROUPS per faction (Castle - Main Door/Small Door/Breach, <faction>),
            // each a single tile with mixed Grass/<faction>Castle corners plus a door slot and NO
            // crosser edge -- already IsExitGroupEligible-eligible (vocab-independent), so the base
            // profile's census was never actually exempting these six tiles even before this variant
            // existed. But GroupExitPlanner's REAL placement pass needs the castle terrain to actually
            // appear in the composed grid, which the base Grass-only composition never paints -- this
            // variant's SolidTerrainOverride makes it a real wall material so GroupExitPlanner can
            // place these door groups for real, and additionally paints the raised all-<faction>Castle
            // uniform tile (TILE644 Evil/TILE645 Good, pathnode-restricted) and the plain ungrouped
            // Grass/<faction>Castle blend tiles as real wall fill via CornerEdgeResolver.
            // PaletteVariant() excludes each from --matrix's full cross-product -- one showcase area
            // apiece.
            _builder.Create(RuralGrassGoodCastle, "Rural Grass* (Good Castle)")
                .Tileset("ttr01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .PaletteVariant()
                .SolidTerrainOverride("GoodCastle")
                .PrimaryOpenTerrain("Grass")
                .ExitGroup("Castle - Main Door, Good")
                .ExitGroup("Castle - Small Door, Good")
                .ExitGroup("Castle - Breach, Good");

            _builder.Create(RuralGrassEvilCastle, "Rural Grass* (Evil Castle)")
                .Tileset("ttr01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .PaletteVariant()
                .SolidTerrainOverride("EvilCastle")
                .PrimaryOpenTerrain("Grass")
                .ExitGroup("Castle - Main Door, Evil")
                .ExitGroup("Castle - Small Door, Evil")
                .ExitGroup("Castle - Breach, Evil");

            // Rural Grass (Water) -- ttr01's harbor/waterfront district, recomposing the SAME ttr01
            // hak data the base profile above uses with SolidTerrainOverride("Water") +
            // PrimaryOpenTerrain("Grass") (verified full 16/16, the pairing is symmetric with the base
            // profile's own Water-as-accent role). Closes the three groups that genuinely mix
            // Grass+Water corners -- Cave - Sea, Pier (both raised Grass/Water bank pieces), and
            // Ship - Docked 1 (a Grass+Water hull footprint) -- as real OpenSetPieces (structurally
            // classify-eligible AND actually registered), but real isolated-placement rates differ by
            // shape (ProbeTool, 150 seeds, Halls): Ship - Docked 1 places at 40.7% (61/150), while
            // Cave - Sea and Pier both measure 0/150. The latter two are NONFLAT (a height-1 bank edge
            // baked into the footprint itself), and TryPlaceOpenSetPiece's site search only ever finds
            // FLAT open-room interiors to stamp into under the currently-supported layouts/relief
            // budgets -- a real, separate geometric ceiling (the exact height-corner pattern these two
            // groups need never spontaneously occurs in a generated room), not a registration bug.
            // Registered anyway (harmless, matches the project's "keep it wired, document the ceiling"
            // convention for CavePlatform1OnMinesAndCavernsComplex_StillDoesNotPlace_DocumentedRoomSizeCeiling)
            // rather than pulled, since census credit and real placement are tracked/reported
            // separately per this pass's own placement-honesty accounting. "Ship - Air, Above Water
            // (3x1)" closes for real at 100% (150/150): all-Water-cornered with a real door on one
            // member (TILE573), allCornersSolid + hasAnyDoor satisfies WallAlcove regardless of any
            // crosser now that Water composes as a genuine Solid terrain here (its base-profile
            // AccentTerrain role never lets this trigger). RampCrosser("HighBridge") (instead of the
            // base profile's Slope) closes TILE603, the Road-to-HighBridge ramp tile (Water/Grass mixed
            // corners, nonflat) -- the same baked-raised-rim shape RampCrosser targets elsewhere, just
            // over a Water solid instead of a Grass/GentleHill blend, mirroring ttd01/ttf01's own
            // per-family RampCrosser variant precedent (DesertRoad/ForestStoneBridge etc.).
            //
            // "Door - Bridge" (Road crosser), "Door - Bridge, High" (HighBridge crosser), and
            // "Ship - Docked 2 (2x2)" (Road crosser, one real member + three holes) are DELIBERATELY
            // NOT wired: all three are solo, all-Water-cornered, WallRoom-classify-eligible once Road/
            // HighBridge are recognized as door-implying crossers, but the same Tunnel-corridor-
            // dependent WallRoom ceiling the base profile's own doc comment documents applies here too
            // (this tileset has no canonical Doorway/Corridor crosser at all, so Complex/Halls/Organic
            // all downgrade to OpenLane) -- verified directly (0/100 isolated across all three layouts
            // for all three groups). Census-exempt via PilotExpectedExemptions instead. "Ship -
            // Floating (2x1)" (all-Water, no door, no crosser) stays exempt too: none of OpenSetPiece/
            // WallAlcove/WallRoom's triggers apply. See TileCoverageCensusTests.PilotExpectedExemptions'
            // own ttr01 doc comment for the full writeup, including the TILE229/TILE179 shared-tile-id
            // accounting.
            _builder.Create(RuralGrassWater, "Rural Grass* (Water)")
                .Tileset("ttr01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .PaletteVariant()
                .SolidTerrainOverride("Water")
                .PrimaryOpenTerrain("Grass")
                .RampCrosser("HighBridge")
                .SetPiece("Cave - Sea", 1)
                .SetPiece("Pier", 1)
                .SetPiece("Ship - Docked 1 (2x2)", 1)
                .SetPiece("Ship - Air, Above Water (3x1)", 1);

            // Rural Winter* (tts01) -- the winter reskin sibling of ttr01 Rural Grass. No hand-built
            // module areas exist for this tileset (zero lighting/decoration evidence of its own), so
            // TileLighting and the bulk Decoration palette below are the RuralGrass fallback: the same
            // uniform (0,0,0,0) daylight-field lighting RuralGrass's own 8-area sample measured, and
            // RuralGrass's own mined palette with the visually grass/forest-specific entries swapped
            // for verified-visible winter equivalents (see the doc comment on the palette below).
            //
            // GROUP inventory is a near-exact re-skin of ttr01 (91 groups both sides, same TileIds for
            // every shared shape -- confirmed directly via ProbeTool, e.g. TILE229/TILE230 land on the
            // identical Ship - Floating/Ship - Docked 2 residue ttr01's own doc comment documents), with
            // five real deltas (all verified directly against tts01's own .set data, not assumed from
            // ttr01):
            //   1. "Turf House" (1x1) carries a real door here (doors=1) where ttr01's copy is doorless
            //      -- it moves from FeatureTile to ExitGroup, the same door-bearing-vs-doorless split
            //      RuralGrass's own doc comment draws between its ExitGroup and FeatureTile buckets.
            //   2. "Turf House (2x2)" is a new 4-member SetPiece with no ttr01 counterpart.
            //   3. "Snowdrift - Pure", "Snowdrift - Rock", "Snowy Dip", "Snowy Pines" are four new
            //      solo, flat, crosser-free, doorless, pathNode-A 1x1 groups -- the same shape as
            //      Anthill/Chessboard/Cobbles, so they join FeatureTile.
            //   4. "Wall - Over Stream, Winter 1"/"2" are two new solo, all-Snow, dual-crosser
            //      (Stream+Wall1 / Stream+Wall2) groups with no door -- the identical WallRoom-eligible-
            //      but-Tunnel-vocab-starved shape as "Wall - Road Gate" below (see that entry's own
            //      note); census-exempt via PilotExpectedExemptions, not wired.
            //   5. "Cave - Sea", "Pier", "Door - Bridge, High", and all four "Tower - Large 1/2,
            //      Evil/Wizard (2x2)" pieces simply DO NOT EXIST in tts01 (no matching group at all,
            //      verified directly) -- not a census gap (there is no tile content to account for),
            //      just a smaller building/water-bank roster than ttr01's. tts01 also has no HighBridge
            //      crosser at all (5 crossers total: Stream/Wall1/Wall2/Road/Slope), so RuralWinterWater
            //      needs no RampCrosser override -- no Water-bank ramp tile family exists to close.
            //
            // Pipeline sweep confirms the same open-field conclusion as RuralGrass: Snow pairs 16/16
            // with every other terrain (Water/Trees/GentleHill/EvilCastle/GoodCastle), and both
            // candidate solids are the identical starved-minor-family shape (Trees: 4 uniform tiles,
            // carrying only the accent-only "Ship - Air, Above Trees" boat group; GentleHill: 3 uniform
            // tiles, carrying no group at all -- verified directly). No SolidTerrainOverride here
            // either -- solid==open==Snow.
            _builder.Create(RuralWinter, "Rural Winter*")
                .Tileset("tts01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .PrimaryOpenTerrain("Snow")
                .AccentTerrain("Water")
                .ReliefBlendTerrain("GentleHill")
                .RampCrosser("Slope")
                .MaxReliefRegions(2)
                .RoadCrosser("Road")
                .FeatureTile("Anthill")
                .FeatureTile("Chessboard")
                .FeatureTile("Cobbles")
                .FeatureTile("Crystal - Platform")
                .FeatureTile("Crystal - Sunken")
                .FeatureTile("Field")
                .FeatureTile("Fountain")
                .FeatureTile("Garden 1")
                .FeatureTile("Garden 2")
                .FeatureTile("Granary")
                .FeatureTile("Graves 1")
                .FeatureTile("Graves 2")
                .FeatureTile("Graves 3")
                .FeatureTile("Graves 4")
                .FeatureTile("Graves 5")
                .FeatureTile("Menhir")
                .FeatureTile("Orchard")
                .FeatureTile("Portal")
                .FeatureTile("Shrine 1")
                .FeatureTile("Shrine 2")
                .FeatureTile("Snowdrift - Pure")
                .FeatureTile("Snowdrift - Rock")
                .FeatureTile("Snowy Dip")
                .FeatureTile("Snowy Pines")
                .FeatureTile("Tower - Archer")
                .FeatureTile("Tower - Winter")
                .FeatureTile("Tree")
                .FeatureTile("Tree - Hollow")
                .FeatureTile("Wagon - Caravan 1")
                .FeatureTile("Warzone 1")
                .FeatureTile("Warzone 2")
                .FeatureTile("Well")
                .ExitGroup("House 1")
                .ExitGroup("House 2")
                .ExitGroup("Mausoleum 1")
                .ExitGroup("Mausoleum 2")
                .ExitGroup("Turf House")
                .ExitGroup("Wagon - Caravan 2")
                .SetPiece("Barn 1 (2x2)", 1)
                .SetPiece("Barn 2 (1x2)", 1)
                .SetPiece("Barn 3 (1x2)", 1)
                .SetPiece("Barracks (1x2)", 1)
                .SetPiece("Barracks (2x2)", 1)
                .SetPiece("Dragon Skeleton (1x2)", 1)
                .SetPiece("Farm 1 (2x2)", 1)
                .SetPiece("Farm 2 (1x2)", 1)
                .SetPiece("Farm 3 (1x2)", 1)
                .SetPiece("Field 1 (2x2)", 1)
                .SetPiece("Field 2 (2x2)", 1)
                .SetPiece("Field 3 (2x1)", 1)
                .SetPiece("Inn (1x2)", 1)
                .SetPiece("Ship - Air, Docked (3x1)", 1)
                .SetPiece("Temple - Evil (2x3)", 1)
                .SetPiece("Temple - Good (3x3)", 1)
                .SetPiece("Temple - Neutral (2x2)", 1)
                .SetPiece("Temple - Winter 1 (3x2)", 1)
                .SetPiece("Temple - Winter 2 (2x2)", 1)
                .SetPiece("Temple - Winter 3 (3x2)", 1)
                .SetPiece("Tower - Cloak (2x2)", 1)
                .SetPiece("Tower - Guard (1x2)", 1)
                .SetPiece("Tower - Winter (1x2)", 1)
                .SetPiece("Tower - Wizard (1x2)", 1)
                .SetPiece("Turf House (2x2)", 1)
                .SetPiece("Warzone (1x2)", 1)
                .SetPiece("Windmill (2x2)", 1)
                .SetPiece("Ramp", 1)
                .SetPiece("Cave", 1);

            // Rural Winter's own bulk palette -- RuralGrass's own mined palette (see that profile's own
            // doc comment for the 8-area provenance), with every visually grass/forest-specific entry
            // swapped for a verified-visible winter equivalent already in the module (per this pass's
            // own evidence-fallback convention: no hand-built tts01 area exists to mine directly).
            // Terrain-neutral entries (wood fence, broken pillar, stone formation, column) carry over
            // unchanged. Substitutions, each confirmed against placeables.2da (real ModelName, not a
            // blank "****" row -- zep_iceblder001-003 and the _mdrn_pl_snowsh*/snowsp* rows were
            // checked and rejected for exactly this reason):
            //   zep_shrub036 (WallAdjacent, green shrub) -> daf_sw322 ("[SWTOR] Snow Pile 01", appearance
            //     31865, a genuine snow-pile ground prop).
            //   zep_giantfern / zep_bushfern001 (CorridorSide, forest-floor ferns) -> zep_pinetr7 /
            //     zep_pinetr10 (placeables.2da rows 3050/3051, "Tree: Pine 8, Snowy, Large/Medium* --
            //     explicitly snow-textured base-game pine models).
            //   zep_tree003 / zep_tree060 / zep_treebig (RoomCenter, bare deciduous trees) ->
            //     zep_pinetr12 / zep_pinetr20 / zep_pinecanopy (rows 3059/3053/3069, all "Snowy"/"Snow
            //     Canopy" labeled).
            //   _mdrn_pl_plant07 (DoorwayFlank, potted leafy plant) -> zep_pinetr5 (row 3046, "Snowy,
            //     Roots, Small*" -- a small flanking sapling in the same size role).
            _builder
                .Decoration("daf_sw322", 3, DecorationContext.WallAdjacent)
                .Decoration("_mdrn_pl_wdfence", 2, DecorationContext.WallAdjacent)
                .Decoration("zep_bpillar007", 1, DecorationContext.WallAdjacent)
                .Decoration("zep_pinetr7", 2, DecorationContext.CorridorSide)
                .Decoration("zep_pinetr10", 2, DecorationContext.CorridorSide)
                .Decoration("zep_pinetr12", 1, DecorationContext.RoomCenter)
                .Decoration("zep_pinetr20", 1, DecorationContext.RoomCenter)
                .Decoration("zep_pinecanopy", 1, DecorationContext.RoomCenter)
                .Decoration("zep_stones018", 1, DecorationContext.RoomCenter)
                .Decoration("zep_pinetr5", 2, DecorationContext.DoorwayFlank)
                .Decoration("zep_column004", 1, DecorationContext.DoorwayFlank)
                .Vignette("WinterFarmCluster", 2)
                .VignetteMember("_mdrn_pl_wdfence", 0f, 0f)
                .VignetteMember("daf_sw322", 0.5f, 0.2f);

            // Rural Winter (Good Castle) / (Evil Castle) -- mirrors RuralGrassGoodCastle/
            // RuralGrassEvilCastle's shape exactly (see that pair's own doc comment for the full
            // mechanism writeup). tts01's own castle inventory is the identical three 1x1 GROUPS per
            // faction (Castle - Main Door/Small Door/Breach, <faction>), each a single Snow/<faction>
            // mixed-corner door tile with no crosser edge, verified directly.
            _builder.Create(RuralWinterGoodCastle, "Rural Winter* (Good Castle)")
                .Tileset("tts01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .PaletteVariant()
                .SolidTerrainOverride("GoodCastle")
                .PrimaryOpenTerrain("Snow")
                .ExitGroup("Castle - Main Door, Good")
                .ExitGroup("Castle - Small Door, Good")
                .ExitGroup("Castle - Breach, Good");

            _builder.Create(RuralWinterEvilCastle, "Rural Winter* (Evil Castle)")
                .Tileset("tts01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .PaletteVariant()
                .SolidTerrainOverride("EvilCastle")
                .PrimaryOpenTerrain("Snow")
                .ExitGroup("Castle - Main Door, Evil")
                .ExitGroup("Castle - Small Door, Evil")
                .ExitGroup("Castle - Breach, Evil");

            // Rural Winter (Water) -- tts01's waterfront district, recomposing the same tts01 hak data
            // with SolidTerrainOverride("Water") + PrimaryOpenTerrain("Snow"). No RampCrosser override
            // (see this pass's own doc comment above -- tts01 has no HighBridge crosser and no
            // Water-bank ramp tile family to close). Closes "Ship - Docked 1 (2x2)" (flat, Snow+Water
            // mixed corners) and "Ship - Air, Above Water (3x1)" (all-Water, real door on TILE557,
            // WallAlcove via allCornersSolid+door once Water composes as a genuine Solid) as real
            // OpenSetPieces -- the identical mechanism RuralGrassWater's own doc comment documents.
            // ttr01's "Cave - Sea"/"Pier" have no tts01 counterpart at all (see this pass's own doc
            // comment above), so there is no nonflat-bank-piece residual to register here.
            //
            // "Door - Bridge" (Road crosser, all-Water, door=1) and "Ship - Docked 2 (2x2)" (Water, one
            // Road edge, one real member + three holes) are the identical WallRoom-eligible-but-Tunnel-
            // vocab-starved shape RuralGrassWater's own doc comment documents for its own analogous
            // residuals (verified directly, 0/100 isolated across all three layouts) -- census-exempt
            // via PilotExpectedExemptions, not wired. "Ship - Air, Above Trees (3x1)" stays exempt for
            // the same accent-terrain-only-group reason RuralGrassWater's own doc comment gives (Trees
            // carries no other GROUP content to justify a dedicated composition). TILE229/TILE230 (the
            // bare "Ship - Floating"/"Ship - Docked 2" anchor tiles) are the identical shared-tile-id
            // residue RuralGrassWater's own doc comment documents -- same TileIds, same mechanism,
            // verified directly against tts01's own .set data.
            _builder.Create(RuralWinterWater, "Rural Winter* (Water)")
                .Tileset("tts01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .PaletteVariant()
                .SolidTerrainOverride("Water")
                .PrimaryOpenTerrain("Snow")
                .SetPiece("Ship - Docked 1 (2x2)", 1)
                .SetPiece("Ship - Air, Above Water (3x1)", 1);

            // Rural Winter - Facelift (tts02) -- see BaseGameTilesetProfiles.RuralWinterFacelift's own
            // constant-block doc comment above for the full mirror-check writeup (byte-for-byte content
            // mirror of vanilla basegame_sets/tts01.set, NOT the SWLOR-hak-renamed RuralWinter profile).
            //
            // PLACEHOLDER-ART AUDIT (same purpose-built KeyBifReader audit as tno01's own writeup):
            // tts02 is BIF-only, so all 331 unique Model= resrefs resolve through the base game's own
            // KEY/BIF -- every one resolved (found=331, missing=0), none carry the twc03
            // "newmodel "-prefixed hand-written ASCII stub header (asciiStub=0). Exactly one,
            // "tts02_b20_01" (TILE20, ungrouped), flagged on the size-only tiny-binary heuristic
            // (5276b); its ASCII-string scan shows TWO real, correctly-mapped textures
            // ("tts02_watcliff", "tts02_water01") plus a real walkmesh node ("wm_tts02_b20_01")
            // alongside the ordinary NULL-textured helper sub-mesh -- the identical
            // genuine-small-water-cliff-model verdict as tno01's own "tno01_b20_04". CONCLUSION: no
            // confirmed placeholder/stub art in tts02 -- no ExcludedTiles(...) call on any profile
            // below.
            // Every FeatureTile/ExitGroup/SetPiece below is RuralWinter's own already-verified
            // classification, carried over via the ProbeTool "hakmap2" model-resref mapping and
            // re-verified against tts02's own .set data (dims/doors/terrains/pathnode/crossers), not
            // assumed. Two tts02-only Snow additions join the roster on the same shape as their nearest
            // sibling: "CampSnow"/"Mineshaft" (flat, doorless, crosser-free, pathnode A, uniform Snow --
            // the identical AntHill/Well/Chessboard shape) join FeatureTile; "HouseV2"/"HouseV3" (flat,
            // crosser-free, uniform Snow, real door -- the identical House01/House02 shape) join
            // ExitGroup.
            //
            // Exemptions (verified directly against tts02's own data, same reasoning as RuralWinter's
            // own doc comment/PilotExpectedExemptions entries for the identically-shaped tts01 groups):
            // "Footbridge"/Stream and "RuinedCart"/Road (solo, all-Snow, one door-implying crosser
            // edge); "Wall1Gate"/"Wall2Gate" (solo, all-Snow, one Wall1/Wall2 edge plus a real door);
            // "Wall1GateRoad"/"Wall2GateRoad" (the same shape plus a second independent Road edge --
            // dual-crosser crossroads gap); "Wall1OverStream"/"Wall2OverStream" (dual-crosser,
            // Stream+Wall1/Wall2, doorless) -- all the identical WallRoom-eligible-but-Tunnel-vocab-
            // starved shape (this tileset has no canonical Corridor/Doorway or Alley vocabulary at all,
            // verified via ProbeTool "dump"). tts02 has no counterpart at all for RuralWinter's five
            // hak-only "Tower - Archer[, Winter Wall 1/2[ Corner]]" groups or its "Ship - Air, Above
            // Trees (3x1)" (all borrow foreign zts01 models not present in vanilla data) -- not a census
            // gap, there is no matching tile content here to account for.
            _builder.Create(RuralWinterFacelift, "Rural Winter - Facelift")
                .Tileset("tts02")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .PrimaryOpenTerrain("Snow")
                .AccentTerrain("Water")
                .RoadCrosser("Road")
                .FeatureTile("AntHill")
                .FeatureTile("Chessboard")
                .FeatureTile("Crystal")
                .FeatureTile("Field")
                .FeatureTile("Garden01")
                .FeatureTile("Garden02")
                .FeatureTile("Granary")
                .FeatureTile("Graves01")
                .FeatureTile("Graves02")
                .FeatureTile("Graves03")
                .FeatureTile("Graves04")
                .FeatureTile("Graves05")
                .FeatureTile("Menhir")
                .FeatureTile("Orchard")
                .FeatureTile("Portal")
                .FeatureTile("Shrine01")
                .FeatureTile("Shrine02")
                .FeatureTile("SnowDriftWithRock")
                .FeatureTile("SnowDrift")
                .FeatureTile("SnowyDip")
                .FeatureTile("SnowyPines")
                .FeatureTile("Tower")
                .FeatureTile("Tree")
                .FeatureTile("TreeHollow")
                .FeatureTile("Caravan1")
                .FeatureTile("Warzone01")
                .FeatureTile("Warzone02")
                .FeatureTile("Well")
                .FeatureTile("CampSnow")
                .FeatureTile("Mineshaft")
                .ExitGroup("House01")
                .ExitGroup("House02")
                .ExitGroup("Mausoleum01")
                .ExitGroup("Mausoleum02")
                .ExitGroup("TurfHouse")
                .ExitGroup("Caravan2")
                .ExitGroup("HouseV2")
                .ExitGroup("HouseV3")
                .SetPiece("Barn01_2x2", 1)
                .SetPiece("Barn02_1x2", 1)
                .SetPiece("Barn03_1x2", 1)
                .SetPiece("Barracks_1x2", 1)
                .SetPiece("Barracks_2x2", 1)
                .SetPiece("DragSkel_1x2", 1)
                .SetPiece("Farm01_2x2", 1)
                .SetPiece("Farm02_1x2", 1)
                .SetPiece("Farm03_1x2", 1)
                .SetPiece("Field01_2x2", 1)
                .SetPiece("Field02_2x2", 1)
                .SetPiece("Field03_2x1", 1)
                .SetPiece("Inn_1x2", 1)
                .SetPiece("EvilTemple_2x3", 1)
                .SetPiece("GoodTemple_3x3", 1)
                .SetPiece("NeutralTemple_2x2", 1)
                .SetPiece("Temple01_3x2", 1)
                .SetPiece("Temple02_2x2", 1)
                .SetPiece("Temple03_3x2", 1)
                .SetPiece("CloakTower_2x2", 1)
                .SetPiece("GuardTower_1x2", 1)
                .SetPiece("Tower_1x2", 1)
                .SetPiece("WizardTower_1x2", 1)
                .SetPiece("Turfhouse_2x2", 1)
                .SetPiece("Warzone_1x2", 1)
                .SetPiece("Windmill_2x2", 1)
                .SetPiece("Ramp", 1)
                .SetPiece("Cave", 1);

            // Rural Winter - Facelift's bulk palette -- zero hand-built tts02 module areas exist
            // (verified directly: no Module/are/*.are.json references tts02), so this is the documented
            // nearest-family fallback: RuralWinter's own tts01 palette verbatim (which is itself the
            // RuralGrass-mined palette with verified-visible winter substitutions -- see RuralWinter's
            // own palette doc comment for the full placeables.2da provenance of every entry). The two
            // tilesets share the same pastoral-winter visual identity (tts02 is a facelift of the same
            // vanilla content), so every entry carries over unchanged. TileLighting(0, 0, 0, 0) inherits
            // the same uniform daylight-field evidence RuralGrass's own 8-area sample measured. The
            // Water/Fort PaletteVariants inherit this palette automatically via
            // DungeonTilesetPaletteInheritance (same TilesetResref).
            _builder
                .Decoration("daf_sw322", 3, DecorationContext.WallAdjacent)
                .Decoration("_mdrn_pl_wdfence", 2, DecorationContext.WallAdjacent)
                .Decoration("zep_bpillar007", 1, DecorationContext.WallAdjacent)
                .Decoration("zep_pinetr7", 2, DecorationContext.CorridorSide)
                .Decoration("zep_pinetr10", 2, DecorationContext.CorridorSide)
                .Decoration("zep_pinetr12", 1, DecorationContext.RoomCenter)
                .Decoration("zep_pinetr20", 1, DecorationContext.RoomCenter)
                .Decoration("zep_pinecanopy", 1, DecorationContext.RoomCenter)
                .Decoration("zep_stones018", 1, DecorationContext.RoomCenter)
                .Decoration("zep_pinetr5", 2, DecorationContext.DoorwayFlank)
                .Decoration("zep_column004", 1, DecorationContext.DoorwayFlank)
                .Vignette("WinterFarmCluster", 2)
                .VignetteMember("_mdrn_pl_wdfence", 0f, 0f)
                .VignetteMember("daf_sw322", 0.5f, 0.2f);

            // Rural Winter - Facelift (Water) -- tts02's waterfront district, recomposing the SAME
            // tts02 data with SolidTerrainOverride("Water") + PrimaryOpenTerrain("Snow"), the identical
            // mechanism RuralWinterWater's own doc comment documents. Closes "ShipDocked01_2x2" (flat,
            // Snow+Water mixed corners) as a real OpenSetPiece. tts02 has no "Ship - Air, Above Water"
            // counterpart (that family is a RuralWinter hak-only addition borrowing a foreign ztr01
            // model, verified directly -- no matching group exists here at all).
            //
            // "BridgeDoor" (Road crosser, all-Water, door=1) is the identical WallRoom-eligible-but-
            // Tunnel-vocab-starved shape as the base profile's own wall-group exemptions -- census-
            // exempt, not wired. "ShipDocked02_2x2"'s TileIds are [230, 180, HOLE, 179] and
            // "ShipFloating_2x1"'s are [229, 179] -- verified directly against tts02's own data, TILE179/
            // TILE180 are shared physical tiles already Cover()'ed under "ShipDocked01_2x2" (claimed
            // first); the bare TILE229 (all-Water, no door, no crosser -- no open corner for OpenSetPiece
            // and no door for WallAlcove) and TILE230 (all-Water, one Road edge, no door -- the same
            // WallRoom ceiling) have no path of their own, the identical shared-tile-id residue
            // RuralWinterWater's own doc comment documents.
            _builder.Create(RuralWinterFaceliftWater, "Rural Winter - Facelift (Water)")
                .Tileset("tts02")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .PaletteVariant()
                .SolidTerrainOverride("Water")
                .PrimaryOpenTerrain("Snow")
                .SetPiece("ShipDocked01_2x2", 1);

            // Rural Winter - Facelift (Fort) -- tts02's genuinely NEW "Fort" terrain/crosser-free wall
            // district (no counterpart anywhere in RuralWinter/tts01 -- verified directly, Fort does not
            // appear in either tts01's vanilla or SWLOR-hak terrain lists). Pipeline sweep (ProbeTool
            // "dump"): Fort pairs 16/16 ONLY against Snow (2/16 against Water/Trees -- the identical
            // starved-minor-family shape the base profile's own doc comment documents for Water/Trees
            // against each other), so SolidTerrainOverride("Fort") + PrimaryOpenTerrain("Snow") is the
            // same wall-district mechanism as the GoodCastle/EvilCastle pair on RuralWinter/RuralGrass --
            // except tts02 has only ONE unified Fort faction, not a Good/Evil split. UNLIKE that castle
            // pair, the fresh PathNodeOpeningWidthAudit for Solid=Fort/Open=Snow computes 2, not the
            // default 1 (verified directly, locked in by
            // the minimum-opening-width path-node audit coverage) -- so this
            // profile explicitly declares MinimumOpeningWidth(2), unlike RuralWinterGoodCastle/
            // EvilCastle which left it at the default.
            //
            // "WallGate3" (Fort+Snow mixed corners, one real door, pathNode F) is the sole ExitGroup:
            // the identical Castle-Main/Small-Door-eligible shape, but tts02's Fort district only has
            // one gate variant, not two. "WallBreach"/"WatchTower" (Fort+Snow mixed corners, doorless,
            // flat) are real OpenSetPieces once Fort composes as a genuine Solid -- measured isolated
            // (ProbeTool "fortprobe", 100 seeds each): 89-90% on Halls/Complex, 100% on Organic.
            // "CampFort"/"WellFort"/"SnowyDipFort" (flat, doorless, crosser-free, pathnode A, uniform
            // Fort corners) sprinkle onto this variant's own composed Fort mass cells as genuine
            // FeatureTiles -- measured isolated (same probe): sprinkled on 100/100 successful seeds on
            // both Halls and Organic. (An earlier draft of this pass assumed the solid-override terrain
            // never composes as a feature-sprinkle key and left all five exempt -- the direct
            // measurement above disproved that assumption, so they are wired for real instead.)
            _builder.Create(RuralWinterFaceliftFort, "Rural Winter - Facelift (Fort)")
                .Tileset("tts02")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .PaletteVariant()
                .SolidTerrainOverride("Fort")
                .PrimaryOpenTerrain("Snow")
                .MinimumOpeningWidth(2)
                .FeatureTile("CampFort")
                .FeatureTile("WellFort")
                .FeatureTile("SnowyDipFort")
                .SetPiece("WallBreach", 1)
                .SetPiece("WatchTower", 1)
                .ExitGroup("WallGate3");

            // Castle Exterior, Rural* (tno01, SWLOR_Haks/sw_t_castleex/tno01.set; UnlocalizedName
            // "Castle Exterior, Rural*"). GENERAL Default=Floor=Border="grass" (the degenerate
            // walkable-ground quirk shared with ttd01/ttf01/ttf02/tno01's own basegame_sets sibling).
            //
            // HAK-VS-VANILLA DELTA (the first confirmed hak-SHRINKS case -- every prior hak copy in this
            // file is a superset of its basegame_sets sibling, e.g. ttr01 653 vs an unshipped vanilla,
            // ttd01 388 vs 212). Verified directly via TilesetSetParser against both files' own
            // [TILES]/[GROUPS] Count= fields (NOT a naive "[TILE" line-count grep, which double-counts
            // each tile's own "[TILEnnnDOORn]" door sub-sections and wildly overstates both sides --
            // caught and corrected during this pass's own recon):
            //   hak (SWLOR_Haks/sw_t_castleex/tno01.set):  1254 tiles / 193 groups.
            //   vanilla (basegame_sets/tno01.set):         1287 tiles / 198 groups.
            // Group-name-set delta (both files carry duplicate group names -- e.g. "cliff_path1" and
            // every "City_House..."/"House..." family name appears TWICE, once on a Dirt-grounded copy
            // and once on a Grass-grounded copy -- so this is a distinct-NAME existence delta, not a
            // raw 1:1 structural diff): vanilla carries 7 group names absent from the hak -- "Docked Ship
            // (4x2)", "Lodge (3x2)", "MidwallDoorway", and "Ship 1/2/3/4 (3x2) - Docked" (a second,
            // larger docked-ship family, distinct from the smaller "DockedShip_City"/"Ship_3x1_Docked"
            // pieces both files still share) -- while the hak adds 7 different group names vanilla never
            // had: "Floating Island", "Halfling Burrow", "Halfling Home 1/2 1x2", "Halfling Home 3",
            // "Halfling Inn 2x3", "Oriental Teahouse". Net effect: the hak swapped one large-ship/lodge
            // family for a smaller halfling-hamlet family, not a simple truncation -- readers should not
            // assume the hak is a strict subset of the vanilla inventory the way every earlier
            // hak-superset entry in this file is a strict superset.
            //
            // PLACEHOLDER-ART AUDIT (Tyrants of the Moonsea premium family -- the same family twc03 Fort
            // Interior belongs to, whose 15 "xyz"-family tiles are confirmed hand-written ASCII
            // placeholder stubs, see FortInteriorLegacy's own ExcludedTiles(...) above). Only 14 of
            // tno01's own physical .mdl files are hak-shipped (the t01-t06/v05_61 castle-tower set); the
            // other 1230 unique Model= resrefs the .set references all resolve through the base game's
            // own KEY/BIF (verified directly: a purpose-built reader reusing SWLOR.ContentBuilder's own
            // KeyBifReader parsed data/nwn_base.key and pulled MDL (restype 2002) bytes for every one of
            // those 1230 resrefs). Every single one resolved (found=1230, missing=0) -- unlike twc03,
            // there is no missing-resource gap here. None carry the twc03 "newmodel "-prefixed
            // hand-written ASCII header (asciiStub=0). Exactly one, "tno01_b20_04" (TILE990, ungrouped),
            // is a small (4.7KB) compiled BINARY model that flagged on a size-only heuristic; a follow-up
            // ASCII-string scan of its own bytes shows it carries TWO real, correctly-mapped textures
            // ("tno01_wtcliff02", "tno01_water01") alongside "NULL"-textured sub-meshes -- the ordinary
            // invisible-collision/aabb-helper-node pattern every compiled tile model uses, not a broken
            // primary surface. CONCLUSION: no confirmed placeholder/stub art in tno01's currently-used
            // model set -- no ExcludedTiles(...) call on any profile below.
            //
            // COMPOSITION (matrix + pipeline-sweep decided, mirroring ttd01/jac01's own inverted
            // Cliff-solid precedent -- see this file's own ttd01 doc comment). Full 16-combo probe
            // (TileResolver.HasCandidate, both orientations, all C(7,2)*2=42 ordered pairs of the seven
            // terrains) found a MULTI-DISTRICT shape unlike the earlier exterior profiles: THREE separate
            // terrains each reach full 16/16 flat-corner coverage against "grass" open in BOTH
            // directions -- "cliff" (the tileset's genuine rock-wall family, 30 uniform-flat tiles plus a
            // large mixed-corner cliff/grass "sandbank"-edged shoreline-blend residue), "castlewall" (the
            // tileset's actual castle-wall material, a much denser mixed-corner inventory carrying every
            // gate/drawbridge/stables group), and "keep" (a starved 8-uniform-tile inner-keep material
            // carrying only 3 door groups). "castlewall" and "keep" ALSO reach 16/16 against "dirt" open
            // in both directions (not just grass); "cliff" vs "dirt" is the one combination that FAILS
            // (matrix "--", confirmed by a live pipeline sweep: Complex/Halls/Organic all measured 0/15,
            // every failure citing the identical missing corner combo "TL=cliff, TR=dirt, BR=cliff,
            // BL=cliff" -- no candidate tile mixes cliff and dirt on the same cell at all). "dirt" itself
            // reaches 16/16 against "grass" in both directions too (96 uniform-flat tiles, and the
            // overwhelming majority of the tileset's building GROUPs are literally duplicated once per
            // ground -- e.g. "City_House_1x1_Tower_2" and dozens of others carry both a Dirt-grounded and
            // a Grass-grounded copy of the identical building), the same shoreline-blend shape
            // ttr01/tts01's own Water-as-AccentTerrain plays, not a wall material.
            //
            // BASE profile: SolidTerrainOverride("cliff") + PrimaryOpenTerrain("grass") -- the tileset's
            // name ("Castle Exterior, Rural") and its own tile inventory both read as "a castle on a
            // cliff above open rural grassland", the identical narrative role ttd01's Cliff/Desert pairing
            // plays for Tatooine. AccentTerrain("dirt") paints the village/courtyard/road-verge ground
            // patches the duplicated building families sit on. RampCrosser("ridge") -- TILE78-116's own
            // raised, all-grass-cornered, height-varying "ridge"-edged family is the tileset's ramp-lane
            // vocabulary (RoadVocabularyCheck.SupportsRoads(grass, ridge) = FALSE, confirming it is a
            // rim/ramp crosser and not a road network, the same distinction ttr01's "Slope" draws).
            // RoadCrosser("road") -- SupportsRoads(grass, road) AND SupportsRoads(dirt, road) both
            // verified TRUE directly (stub/straight/turn/T/X all resolve on both open terrains).
            // MaxReliefRegions(2) mirrors the other exterior profiles' cap. No canonical
            // "Doorway"/"Corridor" crosser exists anywhere in the inventory (verified directly), so
            // Complex's Tunnel mode downgrades to OpenLane, the same verdict as the earlier exterior profiles.
            // A live pipeline sweep (15 seeds x Complex/Halls/Organic) confirms this composition: 45/45
            // succeeded.
            //
            // "stonewall" (172 tile refs) and "smallwall" (44 tile refs) are BOTH real low-wall/fence
            // crosser families (SupportsRoads TRUE for both on grass/dirt too -- rural garden-wall lanes,
            // not the RampCrosser) -- their doorless, ungrouped tiles resolve via CornerEdgeResolver
            // directly, and their solo door-bearing GROUPS (GrassLowWall_gate1/2, DirtLowWall_gate1/2,
            // CastleCrosser_Grass_Breach, Smallwall Break, Smallwall Stairs_Dirt/Grass) are DELIBERATELY
            // NOT wired: an open-cornered solo group carrying a non-tunnel crosser edge fails every
            // LayoutGroupStamper classification branch (CorridorInsert only splices Corridor/Alley/Fence/
            // Bridge; the WallRoom/OpenSetPiece path rejects any member edge outside the doorway/body
            // vocabulary -- see TryClassify), the identical shape ttr01's own "Wall - Gate, Rural 1/2"
            // exemption documents. Census-exempt via PilotExpectedExemptions with the classifier-rule
            // proof. "sandbank" (50 tile refs) is the flat cliff/grass shoreline-blend edge tag
            // (Boat_cliff_Landed, cliff_caveentry_1x2, cliff_path1, CliffPath_3x3, Cave Sandbank Entry
            // 1x1, Shipwreck_clifs) -- the same member-edge rejection applies to every group carrying it,
            // so the whole sandbank family is census-exempt rather than wired. "bridge" (21) and "river"
            // (43) are genuine water-crossing crossers: their ungrouped lanes resolve via ordinary
            // corner/edge matching, while Footbridge_Dirt/Footbridge_Grass (solo river-crosser gates)
            // fail classification for the same member-edge reason -- exempt, mirroring ttr01's own
            // "Footbridge" exemption verbatim. "lists"/"listssmall" (2 tile refs each) carry no GROUP at
            // all and are not wired.
            //
            // "Mill 2x2" is the one dual-crosser multi-tile building: its raised member tile carries
            // BOTH "river" and "road" edges on the SAME cell (nonflat, heights [0,1]) -- the identical
            // dual-crosser-on-one-cell conflict ttf01's own TILE606-609 doc comment documents. Left
            // unregistered; its nonflat members land in the automatic height-exemption bucket.
            //
            // Measured isolated placement rates (Halls, 150 seeds, 20x20): Cog_3x1/Ship_4x1_cliffs
            // 150/150 (WallAlcove), CliffStairs 150/150, Portal 86.0%, Range 56.0%, the grass house/
            // tent exits 40.7%, Tent 1 29.3%, Halfling Inn 2x3 8.0%, Hay_barn 7.3%. Three wired pieces
            // measured 0/150 at this size and are kept wired with a documented ceiling (the ttd01
            // palais_jabba/Astroport large-footprint precedent): "FantasyTower 4x4" and "Tower3 m69
            // 3x3" need a room with a larger contiguous open interior than a 20x20 area produces, and
            // "Cave" (ReliefPiece) needs a painted raised rim edge whose exact corner field Halls'
            // relief budget rarely produces at this size.
            //
            // VILLAGE, CASTLEWALL, KEEP, WATER, and HARBOR districts (see each variant's own doc comment
            // below) recompose this SAME tno01 hak data with a different SolidTerrainOverride/
            // PrimaryOpenTerrain pairing, mirroring RuralGrassGoodCastle/EvilCastle/Water's own
            // PaletteVariant shape -- each needed for real (not just census-eligible) placement of its
            // own building/door-group family, the same GroupExitPlanner/LayoutGroupStamper real-terrain
            // requirement that pattern documents.
            //
            // Lighting sampled directly from all 3 hand-built module areas stamping tno01
            // (vrotrviscvokouts 512 tiles, vrotrnabmission 496 tiles, ka_drps_crsh_vis 144 tiles -- 1152
            // tiles total): the dominant combination is MainLight1=0/MainLight2=0/SrcLight1=0/
            // SrcLight2=0 (998/1152, 86.6%), with a real minority MainLight2=2 variant (154/1152, 13.4%,
            // present only in the latter two areas) -- reported honestly rather than rounded to a false
            // "uniform" claim; TileLighting(0,0,0,0) is wired as the dominant value, matching every other
            // exterior profile's own daylight-field convention.
            //
            // THEME PAIRING: like every profile in this file, no theme/content registration happens
            // here -- these six profiles are reachable via explicit tileset override only. The
            // module's own hand-built tno01 usage is temperate-settlement worlds (vrotrnabmission is
            // a Naboo mission area; vrotrviscvokouts/ka_drps_crsh_vis are Vis outpost/crash sites), so
            // the natural future pairings are pastoral mid-rim settlement themes: the base profile for
            // fortified farmland, Village for walled towns, CastleWall/Keep for fortress assaults,
            // Water/Harbor for coastal settlements.
            //
            // Decoration palette mined from the same 3 areas' own placeable inventories (Module/git/
            // vrotrviscvokouts.git.json, vrotrnabmission.git.json, ka_drps_crsh_vis.git.json -- 118
            // placeables total; functional/scene-effect resrefs excluded: zep_smokeb/zep_smokea/
            // zep_smokesm are chimney/fire VFX props, not ambient dressing, and dem_color_text is a
            // scripted signage marker). Dominated by zep_dirt01 (22, a ground-clutter dirt-patch decal)
            // and _mdrn_pl_crgo001/zep_shack001/zep_shelter/zep_leanto001 (crate/shack/lean-to hamlet
            // clutter, consistent with the rural-village identity) plus x3_plc_tree003 (5, a generic
            // tree already used by ttr01/ttd01/jac01's own bulk palettes).
            _builder.Create(CastleExteriorRural, "Castle Exterior, Rural*")
                .Tileset("tno01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .SolidTerrainOverride("cliff")
                .PrimaryOpenTerrain("grass")
                .AccentTerrain("dirt")
                .RampCrosser("ridge")
                .MaxReliefRegions(2)
                .RoadCrosser("road")
                // GRASS-family content only here. tno01 duplicates most building GROUP names -- a
                // Dirt-grounded copy and a Grass-grounded copy per name, DIRT copy FIRST in .set group
                // order -- and every runtime name resolver (LayoutGroupStamper.FindGroup,
                // GroupExitPlanner.BuildCandidateGroups, TileResolver.BuildFeatureLookup) is
                // first-match-by-name: a duplicated name wired ANYWHERE always resolves to the DIRT
                // copy. Wiring those names here would be dead weight (the dirt copy never classifies/
                // corner-matches against this profile's cliff/grass composition) -- they are wired on
                // the Village district variant below instead, whose dirt-open composition is the one
                // the dirt copies actually place in. The grass copies remain structurally
                // classify-eligible (the census credits them) but are unreachable by name through
                // FindGroup's first-match rule -- a real, documented engine ceiling, not a wiring gap.
                .FeatureTile("Arena")
                .FeatureTile("Burned_house1")
                .FeatureTile("Burned_house2")
                .FeatureTile("Chessboard")
                .FeatureTile("Field_01_1x1")
                .FeatureTile("Fisherman_1")
                .FeatureTile("Fisherman_2")
                .FeatureTile("graves_grass_01")
                .FeatureTile("graves_grass_02")
                .FeatureTile("graves_grass_03")
                .FeatureTile("graves_grass_04")
                .FeatureTile("graves_grass_05")
                .FeatureTile("graves_grass_06")
                .FeatureTile("Oriental Teahouse")
                .FeatureTile("StoneCircle_1x1")
                .FeatureTile("StoneDolman")
                .FeatureTile("Thatch_House_3")
                .FeatureTile("well_grass")
                .ExitGroup("CliffStairs")
                .ExitGroup("Halfling Burrow")
                .ExitGroup("Halfling Home 3")
                .ExitGroup("Ice_Cellar")
                .ExitGroup("Small Tent 1")
                .ExitGroup("Small Tent 2")
                .ExitGroup("Small Tent 4")
                .ExitGroup("Thatch_House_1")
                .ExitGroup("Thatch_House_2")
                // "Portal" mirrors ttd01's own precedent: a semantic teleporter tile, excluded from
                // random FeatureTile sprinkling and wired as a rare set piece instead.
                .SetPiece("Portal", 1)
                .SetPiece("BarrowEntry_2x2", 1)
                .SetPiece("Burned_house2x1", 1)
                .SetPiece("Burned_L_2x2", 1)
                .SetPiece("Cog_3x1", 1)
                .SetPiece("Cog_Anchored_3x1", 1)
                .SetPiece("FantasyTower 4x4", 1)
                .SetPiece("Field_02_1x2")
                .SetPiece("Field_03_1x2")
                .SetPiece("Halfling Home 1 1x2", 1)
                .SetPiece("Halfling Home 2 1x2", 1)
                .SetPiece("Halfling Inn 2x3", 1)
                .SetPiece("Hay_barn", 1)
                .SetPiece("House_2x2_Lshape03", 1)
                .SetPiece("JoustingList", 1)
                .SetPiece("JoustStands_1x2", 1)
                .SetPiece("JoustStands_1x3", 1)
                .SetPiece("JoustStands_1x3_2", 1)
                .SetPiece("Large Tent 1", 1)
                .SetPiece("Large Tent 2", 1)
                .SetPiece("Large Tent 4", 1)
                .SetPiece("Range", 1)
                .SetPiece("ruin_2x2_Lshape_02", 1)
                .SetPiece("Ship_4x1_cliffs", 1)
                .SetPiece("StoneCircle", 1)
                .SetPiece("Tent 1", 1)
                .SetPiece("Tent 2", 1)
                .SetPiece("Tent 4", 1)
                .SetPiece("Tower Hill", 1)
                .SetPiece("tower_2x2_m70", 1)
                .SetPiece("Tower3 m69 3x3", 1)
                // Baked-mesh raised cave-mouth piece (1x1 GROUP, nonflat, all-Grass, "ridge" crosser,
                // one door slot) -- the same ReliefPiece kind ttd01/ttf01/ttr01/tts01's own "Ramp"/
                // "Cave"/"SmallCave" pieces use, stamped onto a painted raised rim edge.
                .SetPiece("Cave", 1);

            // Castle Exterior, Rural's own bulk palette -- mined from tno01's 3 hand-built reference
            // areas (see the base profile's own doc comment above for the full provenance and the
            // functional/VFX exclusions). Two evidence resrefs have no utp blueprint in this module
            // (verified via AllDungeonDefinitions_DecorationsExistAndAreVisible, the same jac01/ttr01
            // gap those palettes' own doc comments describe) and are substituted with the nearest
            // blueprint-backed equivalents: _mdrn_pl_crgo001 -> _mdrn_pl_crate01 ("[mdrn]Box 1",
            // appearance 20225, real ModelName pkt_tlcrate1 -- the _mdrn_pl_cargo1 candidate was
            // checked and REJECTED for a blank-ModelName appearance row, the same visibility-guard
            // rejection RuralWinter's own palette doc comment describes), and x3_plc_tree003 ->
            // zep_tree003 (ttr01's own identical substitution).
            _builder
                .Decoration("zep_dirt01", 3, DecorationContext.WallAdjacent)
                .Decoration("_mdrn_pl_crate01", 2, DecorationContext.WallAdjacent)
                .Decoration("zep_shack001", 1, DecorationContext.WallAdjacent)
                .Decoration("zep_shelter", 2, DecorationContext.CorridorSide)
                .Decoration("_mdrn_pl_df_hvbk", 2, DecorationContext.CorridorSide)
                .Decoration("zep_tree003", 1, DecorationContext.RoomCenter)
                .Decoration("zep_shack002", 1, DecorationContext.RoomCenter)
                .Decoration("zep_leanto001", 1, DecorationContext.RoomCenter)
                .Decoration("zep_shed001", 2, DecorationContext.DoorwayFlank)
                .Decoration("_mdrn_pl_parts10", 1, DecorationContext.DoorwayFlank)
                .Vignette("RuralHamletCluster", 2)
                .VignetteMember("zep_shack001", 0f, 0f)
                .VignetteMember("zep_dirt01", 0.5f, 0.2f);

            // Castle Exterior, Rural (Village) -- tno01's walled-town district, recomposing the SAME
            // tno01 hak data with SolidTerrainOverride("castlewall") + PrimaryOpenTerrain("dirt")
            // (verified full 16/16 both directions; live pipeline sweep 15 seeds x Complex/Halls/
            // Organic all succeeded). This is the composition the tileset's dirt-grounded building
            // copies actually place in: every duplicated building name resolves to its DIRT copy
            // through the first-match-by-name rule (see the base profile's own doc comment), and an
            // all-dirt building only classifies as an OpenSetPiece when dirt composes as a real OPEN
            // terrain (LayoutGroupStamper.TryClassify's corner-match reads the composed
            // SolidTerrain/OpenTerrain pair -- dirt-as-Accent on the base profile never qualifies).
            // "Castle-Stairs" and "Stables On Wall" (castlewall+dirt mixed groups) classify here for
            // the same reason. RoadCrosser("road") -- SupportsRoads(dirt, road) verified TRUE directly.
            // PaletteVariant() excludes this from --matrix's full cross-product.
            _builder.Create(CastleExteriorRuralVillage, "Castle Exterior, Rural* (Village)")
                .Tileset("tno01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .PaletteVariant()
                .SolidTerrainOverride("castlewall")
                .PrimaryOpenTerrain("dirt")
                // PathNodeOpeningWidthAudit measures 2 for the castlewall-solid pairing (the
                // castlewall/dirt blend tiles' pathnodes never leave a 1-cell-wide walkable lane) --
                // verified by MinimumOpeningWidth_MatchesFreshPathNodeAudit.
                .MinimumOpeningWidth(2)
                .RoadCrosser("road")
                .FeatureTile("City_Granary_m42")
                .FeatureTile("Fountain_Dirt")
                .FeatureTile("graves_dirt_01")
                .FeatureTile("graves_dirt_02")
                .FeatureTile("graves_dirt_03")
                .FeatureTile("graves_dirt_04")
                .FeatureTile("graves_dirt_05")
                .FeatureTile("graves_dirt_06")
                .FeatureTile("graves_dirt_07")
                .FeatureTile("Market Stall 1x1 m55_01")
                .FeatureTile("MarketStall02")
                .FeatureTile("MarketStall03")
                .FeatureTile("SimpleStage")
                .ExitGroup("City_House_1x1_Tower_1")
                .ExitGroup("City_House_1x1_Tower_2")
                .ExitGroup("City_House_1x1_Tower_3")
                .ExitGroup("City_House_1x1_Tower_4")
                .ExitGroup("Crypt_Dirt")
                .ExitGroup("FineHouse m50")
                .ExitGroup("GuildHouse m39")
                .ExitGroup("house 1x1 m61")
                .ExitGroup("house 1x1 m64")
                .ExitGroup("House 3 m32")
                .ExitGroup("House 3 m32_02")
                .ExitGroup("House m60")
                .ExitGroup("House_Tower_m57")
                .ExitGroup("Med Tower m58")
                .ExitGroup("Roundhouse 1x1 m21")
                .ExitGroup("Small Roundhouse m25")
                .ExitGroup("Watchtower m72")
                .SetPiece("Castle-Stairs", 1)
                .SetPiece("Chapel_3x2", 1)
                .SetPiece("City_House_1x2_m41", 1)
                .SetPiece("City_House_1x3", 1)
                .SetPiece("city_house_2x2", 1)
                .SetPiece("City_House_2x2_m26", 1)
                .SetPiece("City_Inn_1x2_m37", 1)
                .SetPiece("city_SewerEntrance", 1)
                .SetPiece("CoachInn", 1)
                .SetPiece("Forge_L_shape_2x2", 1)
                .SetPiece("House_1x2_m59", 1)
                .SetPiece("House_2x2_Arcaded", 1)
                .SetPiece("house_2x2_m40", 1)
                .SetPiece("House_Inn_2x2", 1)
                .SetPiece("Inn 2x2", 1)
                .SetPiece("MarketStall_2x2 m54", 1)
                .SetPiece("Mausoleum_dirt_2x2", 1)
                .SetPiece("RichMarket_2x2", 1)
                .SetPiece("Stables On Wall", 1)
                .SetPiece("Tower 2x2 m71", 1);

            // Castle Exterior, Rural (Castle Wall) -- tno01's outer-wall district, recomposing the SAME
            // tno01 hak data with SolidTerrainOverride("castlewall") + PrimaryOpenTerrain("grass")
            // (verified full 16/16 both directions). Wires the castlewall/grass mixed-corner door
            // groups GroupExitPlanner can actually corner-match at composed wall cells (OuterWallDoor2/
            // OuterWallDoor3/WallRaiseGate, each a solo grass+castlewall door tile -- the same
            // mixed-corner shape ttr01's own Castle - Main Door/Small Door/Breach trio places at
            // 150/150). The multi-terrain gate groups (CastleWall4/CastleGate2 2x1/CastleWall
            // Entrance (+Walkable)/Castle Gate Walkable 2x1: castlewall+dirt+grass on THREE terrains;
            // the four Drawbridge/drawbridge_passage pieces: three terrains PLUS road member edges;
            // CaveWall2x1: castlewall+cliff, cliff being neither open nor secondary in any composition)
            // are NOT wired: LayoutGroupStamper's OpenSetPiece corner rule is a strict two-terrain
            // (solid+open, or solid+secondary) match, so a three-terrain group fails classification
            // under EVERY tno01 composition -- census-exempt via PilotExpectedExemptions with that
            // proof. PaletteVariant() excludes this from --matrix's full cross-product.
            _builder.Create(CastleExteriorRuralCastleWall, "Castle Exterior, Rural* (Castle Wall)")
                .Tileset("tno01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .PaletteVariant()
                .SolidTerrainOverride("castlewall")
                .PrimaryOpenTerrain("grass")
                // PathNodeOpeningWidthAudit measures 2 for the castlewall-solid pairing -- see the
                // Village variant's own note above.
                .MinimumOpeningWidth(2)
                .RoadCrosser("road")
                .ExitGroup("OuterWallDoor2")
                .ExitGroup("OuterWallDoor3")
                .ExitGroup("WallRaiseGate");

            // Castle Exterior, Rural (Keep) -- tno01's inner-keep district, recomposing the SAME tno01
            // hak data with SolidTerrainOverride("keep") + PrimaryOpenTerrain("grass") (verified full
            // 16/16 both directions, and full 16/16 against "dirt" too). "keep" is a starved terrain (8
            // uniform-flat tiles, only 1 grouped) exactly like RuralGrassGoodCastle/EvilCastle's own
            // three-tile-per-faction shape -- its entire real GROUP inventory is three 1x1 door tiles.
            // Only "KeepDoor_Grass" (keep+grass mixed corners) is wired: it corner-matches this
            // composition's own wall cells for real (measured 150/150 isolated, Halls). "KeepDoor_Dirt"
            // (keep+dirt corners -- dirt never composes here) and "KeepTop_Stairs" (ALL-keep corners --
            // GroupExitPlanner's wall-ring candidates always carry at least two open-facing corners, so
            // an all-solid door tile never corner-matches any ring cell) both measured 0/150 isolated
            // and are NOT wired; both remain census-covered as structural ExitGroups (eligibility is
            // vocabulary-independent). PaletteVariant() excludes this from --matrix's full
            // cross-product.
            _builder.Create(CastleExteriorRuralKeep, "Castle Exterior, Rural* (Keep)")
                .Tileset("tno01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .PaletteVariant()
                .SolidTerrainOverride("keep")
                .PrimaryOpenTerrain("grass")
                // PathNodeOpeningWidthAudit measures 2 for the keep-solid pairing (same
                // wall-blend-pathnode shape as the castlewall variants) -- verified by
                // MinimumOpeningWidth_MatchesFreshPathNodeAudit.
                .MinimumOpeningWidth(2)
                .ExitGroup("KeepDoor_Grass");

            // Castle Exterior, Rural (Water) -- tno01's rural-shoreline district, recomposing the SAME
            // tno01 hak data with SolidTerrainOverride("water") + PrimaryOpenTerrain("grass") (verified
            // full 16/16 both directions, mirroring RuralGrassWater's own Water-as-real-solid
            // mechanism). Closes the grass-side water content: "Grass_docks" (a solo grass+water
            // mixed-corner dock, OpenSetPiece) and the all-water ship hulls Ship_3x1_water/
            // Ship_4x1_water (allCornersSolid + a real door -> WallAlcove, the identical mechanism
            // RuralGrassWater's own "Ship - Air, Above Water" doc comment documents). "Boat_water"
            // (all-water, DOORLESS, pathnode T) is NOT wired: with neither a door (WallAlcove's
            // trigger) nor a doorway/body crosser (WallRoom/CorridorStub's) nor an open corner
            // (OpenSetPiece's), no classification branch applies -- the identical shape ttr01's own
            // "Ship - Floating" exemption documents. "WaterRoad_gate" (all-water + road crosser +
            // door) is NOT wired either: a road-family gate's member edge fails every classification
            // branch, and even under a DoorSlotCrossers("road") declaration it would classify WallRoom
            // only to never place (this tileset has no Tunnel vocabulary, and the direct
            // isolated-placement probe below measured 0 across all three layouts) -- the identical
            // verdict ttr01's own "Wall - Road Gate" exemption documents. Census-exempt via
            // PilotExpectedExemptions. PaletteVariant() excludes this from --matrix's full
            // cross-product.
            _builder.Create(CastleExteriorRuralWater, "Castle Exterior, Rural* (Water)")
                .Tileset("tno01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .PaletteVariant()
                .SolidTerrainOverride("water")
                .PrimaryOpenTerrain("grass")
                .SetPiece("Grass_docks", 1)
                .SetPiece("Ship_3x1_water", 1)
                .SetPiece("Ship_4x1_water", 1);

            // Castle Exterior, Rural (Harbor) -- tno01's city-waterfront district, recomposing the
            // SAME tno01 hak data with SolidTerrainOverride("water") + PrimaryOpenTerrain("dirt")
            // (verified full 16/16 both directions; live pipeline sweep 15 seeds x Complex/Halls/
            // Organic all succeeded). This is the composition the DIRT-side dock family actually
            // classifies in: City_boat_docked, DockedShip_City, Docks_City, and Ship_3x1_Docked all
            // mix water+dirt corners, which the strict two-terrain OpenSetPiece rule only admits when
            // water composes as Solid and dirt as Open. Isolated rates (Halls, 150 seeds):
            // Docks_City 45.0%, City_boat_docked 22.6%, Ship_3x1_Docked 6.1%, DockedShip_City 0/150 --
            // the last is a 4x2/6-member footprint whose water+dirt shoreline pattern never
            // spontaneously occurs in a 20x20 generated room; kept wired anyway per the project's
            // "keep it wired, document the ceiling" convention (the same
            // CavePlatform1OnMinesAndCavernsComplex/RuralGrassWater Cave-Sea/Pier precedent).
            // PaletteVariant() excludes this from --matrix's full cross-product.
            // No RoadCrosser here: a carved road lane that reaches the water shoreline needs a
            // road-edged water/dirt blend for EVERY corner arrangement, and the inventory only covers
            // a few (TILE394/403-405) -- a direct pipeline run with RoadCrosser("road") failed on
            // "TL=water, TR/BR/BL=dirt + road edges" (no such tile exists). Roads stay a base/Village
            // district feature.
            _builder.Create(CastleExteriorRuralHarbor, "Castle Exterior, Rural* (Harbor)")
                .Tileset("tno01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .PaletteVariant()
                .SolidTerrainOverride("water")
                .PrimaryOpenTerrain("dirt")
                .SetPiece("City_boat_docked", 1)
                .SetPiece("DockedShip_City", 1)
                .SetPiece("Docks_City", 1)
                .SetPiece("Ship_3x1_Docked", 1);

            // City Exterior* (tcn01, SWLOR_Haks/sw_t_cityext -- UnlocalizedName "City Exterior*";
            // hak wins over basegame_sets, 1460 tiles / 295 groups, the largest registered set yet).
            // GENERAL: Border=Water, Default=Water, Floor=Cobble -- Default != Floor already (a
            // conventional composition, unlike the degenerate ttr01/tts01 Rural pair where Default ==
            // Floor forced an explicit PrimaryOpenTerrain override). No SolidTerrainOverride/
            // PrimaryOpenTerrain declared here: BuildVocabulary's own empty-means-Default/Floor
            // defaults already give Solid="Water"/Open="Cobble", and a direct 16-combo corner probe
            // (ProbeTool, all four corners drawn from {Water, Cobble}, crosser-free) resolved 16/16 --
            // full coverage with zero override needed.
            //
            // FOUR PARALLEL DISTRICTS share this one physical tileset under prefixed group/terrain/
            // crosser names: City (unprefixed, this profile), Sigil, Fieldstone, Gothic -- each
            // ~90-105 groups, verified by a direct group-name diff (Fieldstone/Gothic mirror City's
            // building family tile-for-tile minus City's own large naval fleet, plus 3-4 district-only
            // extras; Sigil is a much smaller, structurally distinct hive/chasm district). Three
            // PaletteVariant profiles below (CityExteriorFieldstone/Gothic/Sigil) recompose the SAME
            // tcn01 hak data, matching the zin01/tno01 multi-district precedent.
            //
            // TUNNEL VOCABULARY (the tileset's real "wall-embedded corridor" mechanism, verified via
            // TunnelVocabularyCheck.SupportsTunnels): Wall and Stream both FAIL every custom body/port
            // shape (never occur on 4x-uniform-Solid corners in a straight/turn/T/X/port pattern -- see
            // their own doc comments below for what they actually are). Dock and Bridge BOTH
            // independently verify TRUE as a Custom body==port pair against Solid=Water/Open=Cobble --
            // a coherent, evidence-backed reading: tcn01's City district is a canal/harbor city whose
            // districts sit on Cobble "islands" separated by a Water solid mass, connected by literal
            // docks and bridges. "[City] Door - Bridge"/"[City] Door - Dock" (both 1x1 GROUPs, all-Water
            // corners, one door, the crosser mirrored on two opposite edges) are exactly
            // TunnelVocabularyCheck's "boundary port cell" shape -- confirmed live, these are the tiles
            // LayoutTunnelCarver's own TryAddPort stamps a port onto, so they are NOT separately wired
            // via SetPiece/ExitGroup (the tunnel carver consumes them directly, the same way Barrows'
            // "door_corridor" port tiles are consumed by DoorSlotCrossers rather than hand-listed).
            // Dock is wired as the primary TunnelCrossers pair here (richer real content: the docked-
            // ship fleet -- Small/Merchant/Weathered/Caravel/Longship, some now genuinely placeable via
            // SetPieceCorridorStubChain -- carries a real Dock crosser on its hull, whereas Bridge only
            // ever appears on the single "Door - Bridge" boundary-port group). Bridge is an equally-
            // valid second real vocabulary (structurally reachable via the same mechanism a future
            // variant could switch to; not both at once since DungeonTilesetProfile only carries one
            // Tunnel body/port slot per profile) -- "Door - Bridge" and its Bridge-crossered siblings are
            // census-exempt as this pass's unwired alternate (see TileCoverageCensusTests.
            // PilotExpectedExemptions["tcn01"]). Several docked-ship hull groups (Merchant/Weathered/
            // Carrack, plus each district's own "Small, Docked") still don't classify even with Dock
            // wired: they mark a continuous Dock-crosser "keel line" down the hull spanning BOTH an
            // interior seam (between two real hull members) and a perimeter edge, which
            // ClassifyMultiTileSetPiece's CorridorStubChain rule rejects outright (no interior body
            // crosser tolerated) -- also census-exempt, see the same PilotExpectedExemptions entry.
            //
            // "Alley" (10-crosser CROSSER TYPES list, "[All] Alley") is NOT a street/lane crosser
            // despite the name: a direct census of all 138 Alley-edged tiles shows EVERY one sits on
            // uniform Building/FieldBuilding/GothicBuilding corners (or a Building/Cobble mixed corner),
            // almost always carrying a door -- i.e. Alley is a back-alley passage carved THROUGH the
            // Building solid mass (a WallRoom/tunnel-in-masonry shape), not a lane through open Cobble
            // street space. RoadVocabularyCheck.SupportsRoads(Cobble, Alley) verified FALSE on all five
            // required shapes (stub/straight/turn/T/X all fail) for exactly this reason: Alley never
            // occurs on all-Cobble corners at all. TunnelVocabularyCheck.SupportsTunnels(..., Alley)
            // also verified FALSE (the canonical-Alley overload, which checks Alley as BOTH body and
            // port against the composed Solid=Water). Since this profile composes Building as an
            // ordinary SetPiece obstacle (not the corner-match Solid terrain), Alley's real vocabulary
            // is structurally out of reach here -- left as a documented alternate-vocabulary gap for a
            // possible future "Building-embedded back-alley" sub-mode rather than forced into this
            // composition.
            //
            // STREETS (LayoutRoadCarver): Alley also fails as a RoadCrosser for the reason above. The
            // Sigil district's OWN "SigilRoad" crosser (declared only for Sigil, not City/Fieldstone/
            // Gothic) DOES verify true (SupportsRoads(SigilCobble, SigilRoad) = TRUE, all five shapes
            // resolve) -- making tcn01 the SECOND Streets-capable tileset in this codebase after vmr01,
            // wired on the Sigil PaletteVariant below via RoadCrosser("SigilRoad"). The base City
            // district itself has NO Streets vocabulary.
            //
            // "Wall" ("[All] Wall") is mostly a flat, doorless, all-Cobble opposite-edge-pair crosser
            // (PathNode=D) with a minority Cobble/Water and Cobble/Building-boundary shape (PathNode=S)
            // -- structurally a property-line/parapet divider through open street space, not a tunnel or
            // road crosser (confirmed FALSE under every TunnelVocabularyCheck combination tried). No
            // production mechanism in this codebase recognizes an arbitrarily-named "Fence"-style
            // crosser (LayoutFenceCarver/IsCorridorInsertEligible's FenceCrosser slot is the fixed
            // literal name "Fence", which tcn01 does not declare) -- left as a documented
            // alternate-vocabulary gap. "Stream" is the same shape one register over (mostly flat,
            // doorless, all-Cobble single-edge crosser, PathNode=C, minority Cobble/Water boundary,
            // PathNode=I) paired with "[City] Footbridge" (flat, all-Cobble, Stream-crossed, doorless) as
            // its crossing piece -- a decorative canal-through-downtown pair, same "no wired vocabulary
            // this profile family" verdict as Wall/Alley. Both stay unwired; their tiles fall through to the
            // automatic alternate-vocabulary/height exemption buckets.
            //
            // Lighting sampled directly from the one hand-built module area on this tileset
            // (Module/are/dan_repgarrison.are.json, a Republic garrison outpost, 100 tiles): uniform
            // MainLight1=0/MainLight2=0/SrcLight1=0/SrcLight2=0 across all 100 tiles -- matches every
            // other exterior profile's daylight convention. This area is comparatively thin evidence for
            // a 1460-tile set and its placeable inventory (133 items) skews military-garrison (troops,
            // turrets, cargo containers, chain-link fencing) rather than general downtown dressing; the
            // small directly-evidenced subset below is supplemented by the SAME city-family fallback
            // fcx01/tin01 decoration work already established for other thin-evidence exterior/city
            // profiles, not invented wholesale.
            //
            // THEME PAIRING: like every profile in this file, no theme/content registration happens
            // here -- reachable via explicit tileset override only. Natural future pairing: coastal/
            // harbor settlement worlds (matching the Bridge/Dock canal-city reading above) and walled
            // free-city or garrison-outpost worlds (matching dan_repgarrison's own military use).
            _builder.Create(CityExterior, "City Exterior*")
                .Tileset("tcn01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .TunnelCrossers("Dock", "Dock")
                .FeatureTile("[City] Chessboard")
                .FeatureTile("[City] Construction")
                .FeatureTile("[City] Fountain")
                .FeatureTile("[City] Garden - Vegetable")
                .FeatureTile("[City] Garden - Flower")
                .FeatureTile("[City] Gazebo")
                .FeatureTile("[City] Market 1")
                .FeatureTile("[City] Market 2")
                .FeatureTile("[City] Market - Slum 1")
                .FeatureTile("[City] Market - Slum 2")
                .FeatureTile("[City] Plaza 1")
                .FeatureTile("[City] Plaza 2")
                .FeatureTile("[City] Streetlight")
                .FeatureTile("[City] Tree")
                .FeatureTile("[City] Wagon")
                .FeatureTile("[City] Well")
                .FeatureTile("[City] Wall - Chunk")
                .FeatureTile("[City] Building - Destroyed 1")
                .FeatureTile("[City] Building - Destroyed 2")
                .FeatureTile("[City] Building - Destroyed 3")
                .FeatureTile("[City] Building - Burned")
                // Semantic teleporter tile -- excluded from random FeatureTile sprinkling and wired as
                // a rare set piece instead, mirroring ttd01/fcx01's own "Portal" precedent.
                .SetPiece("[City] Portal", 1)
                .ExitGroup("[City] House - Slum 1")
                .ExitGroup("[City] House - Slum 2")
                .ExitGroup("[City] House")
                .ExitGroup("[City] Sewer Entrance 1")
                .ExitGroup("[City] Wall - Breach")
                .ExitGroup("[City] Wall - Door 1")
                .ExitGroup("[City] Building - Wall Breach")
                .ExitGroup("[City] Building - Wall Temple")
                .ExitGroup("[City] Castle - Breach, Evil")
                .ExitGroup("[City] Castle - Breach, Good")
                .ExitGroup("[City] Castle - Main Door, Evil")
                .ExitGroup("[City] Castle - Main Door, Good")
                .ExitGroup("[City] Castle - Small Door, Evil")
                .ExitGroup("[City] Castle - Small Door, Good")
                // Non-flat (a raised rampart-tower segment); kept wired per the project's "wire it,
                // let TryClassify/height-exemption sort it out" convention -- no relief vocabulary is
                // declared for this profile, so these fall through to the automatic height exemption if
                // they don't independently classify.
                .ExitGroup("[City] Wall - Tower 1")
                .ExitGroup("[City] Wall - Tower 2")
                .SetPiece("[City] House 01 (2x3)")
                .SetPiece("[City] House 02 (2x2)")
                .SetPiece("[City] House 03 (2x2)")
                .SetPiece("[City] House 04 (2x2)")
                .SetPiece("[City] House 05 (2x2)")
                .SetPiece("[City] House 06 (2x2)")
                .SetPiece("[City] House 07 (1x2)")
                .SetPiece("[City] House 08 (1x2)")
                .SetPiece("[City] House 09 (1x2)")
                .SetPiece("[City] House 10 (1x2)")
                .SetPiece("[City] House - Slum (1x2)")
                .SetPiece("[City] Inn - Slum 1 (1x2)")
                .SetPiece("[City] Inn - Slum 2 (1x2)")
                .SetPiece("[City] Barracks (2x2)")
                .SetPiece("[City] Temple - Evil (2x3)")
                .SetPiece("[City] Temple - Good (3x3)")
                .SetPiece("[City] Temple - Neutral (2x2)")
                .SetPiece("[City] Tower - Cloak (2x2)")
                .SetPiece("[City] Tower - Guard (1x2)")
                .SetPiece("[City] Tower - Ruined (2x2)")
                .SetPiece("[City] Tower - Wizard (1x2)")
                // Non-flat (a raised gate arch); same "wire it, height-exemption is the safety net"
                // reasoning as the wall-tower ExitGroups above.
                .SetPiece("[City] Gate - City (2x2)")
                .SetPiece("[City] Building - State 1 (2x3)")
                .SetPiece("[City] Building - State 2 (2x3)")
                .SetPiece("[City] Arena (3x3)")
                .SetPiece("[City] Fountain (1x2)")
                .SetPiece("[City] Garden - Flower (1x2)")
                .SetPiece("[City] Market (2x1)")
                .SetPiece("[City] Plaza (2x2)")
                .SetPiece("[City] Pool - Holy (2x2)")
                .SetPiece("[City] Ruined Park (1x2)")
                .SetPiece("[City] Tree - Giant (2x2)")
                .SetPiece("[City] Building - Burned (2x1)")
                .SetPiece("[City] Building - Destroyed (1x2)")
                // 72-tile (9x8) finale/showcase piece -- the same oversized-set-piece shape as
                // Barrows' FinalArea_7x7/fcx01's Tower07, kept wired for future larger-area generation
                // even though a 20x20 area's room supply cannot realistically fit it today.
                .SetPiece("[City] Amphitheater (9x8)", 1)
                // The Cobble-cornered "Docked" airship variant only -- the "Above Buildings"/"Above
                // Water" siblings need Building/Water composed as OPEN terrain, which this profile
                // does not do, and are left as alternate-vocabulary exemptions.
                .SetPiece("[City] Ship - Air, Docked (3x1)", 1)
                // All-Water-cornered ship hulls (some Dock-crossered) -- wired per the same "TryClassify
                // re-verifies independently" convention as every other tileset-declared-but-unverified
                // group in this codebase; safe even if these turn out to need a mechanism this pass
                // doesn't wire, since an unreachable SetPiece call is simply never stamped.
                .SetPiece("[City] Boat", 1)
                .SetPiece("[City] Boathouse", 1)
                .SetPiece("[City] Ship - Caravel, Docked (3x2)", 1)
                .SetPiece("[City] Ship - Caravel, Floating (3x1)", 1)
                .SetPiece("[City] Ship - Carrack, Docked (4x2)", 1)
                .SetPiece("[City] Ship - Carrack, Floating (4x1)", 1)
                .SetPiece("[City] Ship - Galleon 1 (5x1)", 1)
                .SetPiece("[City] Ship - Galleon 2 (5x1)", 1)
                .SetPiece("[City] Ship - Longship, Docked (3x2)", 1)
                .SetPiece("[City] Ship - Longship, Floating (3x2)", 1)
                .SetPiece("[City] Ship - Merchant, Docked (3x2)", 1)
                .SetPiece("[City] Ship - Merchant, Undockable (3x1)", 1)
                .SetPiece("[City] Ship - Weathered, Docked (3x2)", 1)
                .SetPiece("[City] Ship - Weathered, Undockable (3x1)", 1);

            // City Exterior*'s own bulk palette -- directly evidenced items from the one hand-built
            // tcn01 area (Module/git/dan_repgarrison.git.json, a Republic garrison outpost, 133
            // placeables) plus the established city-family fallback (fcx01/tin01's own palettes) for
            // what that thin, garrison-skewed sample cannot support. See the base profile's own doc
            // comment above for the full evidence-thinness writeup.
            _builder
                .Decoration("zep_grasstuft001", 3, DecorationContext.WallAdjacent)
                .Decoration("zep_dirt02", 2, DecorationContext.WallAdjacent)
                .Decoration("_mdrn_pl_wall009", 2, DecorationContext.WallAdjacent)
                .Decoration("zep_shrub036", 2, DecorationContext.CorridorSide)
                .Decoration("_mdrn_pl_conta32", 1, DecorationContext.StructureAdjacent)
                .Decoration("_mdrn_pl_floor23", 2, DecorationContext.CourtyardCenter)
                .Decoration("_mdrn_pl_strtlm4", 2, DecorationContext.DoorwayFlank);

            // City Exterior* (Fieldstone) -- FieldCobble/FieldBuilding/FieldEvilCastle/FieldGoodCastle
            // district PaletteVariant, recomposing the SAME tcn01 hak data with no override needed
            // (16-combo probe against Solid=Water/Open=FieldCobble verified 16/16, mirroring the base
            // City profile exactly). Mirrors the base profile's own group family tile-for-tile (verified
            // by a direct group-name diff): every City building/gate/wall/tower/dock group has a
            // same-shaped Fieldstone counterpart, MINUS City's own large naval fleet (Fieldstone keeps
            // only the small-boat/dock/footbridge family) PLUS "Gate - City (2x3)"/"Garden -
            // Flower_1x2"/"Building - Burned (1x2)" as its own small naming-convention deltas.
            // TunnelCrossers("FieldBridge","FieldBridge") independently verified TRUE via
            // TunnelVocabularyCheck, the same Bridge-spans-Water mechanism as the base profile.
            _builder.Create(CityExteriorFieldstone, "City Exterior* (Fieldstone)")
                .Tileset("tcn01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .PaletteVariant()
                .PrimaryOpenTerrain("FieldCobble")
                .TunnelCrossers("FieldDock", "FieldDock")
                .FeatureTile("[Fieldstone] Chessboard")
                .FeatureTile("[Fieldstone] Construction")
                .FeatureTile("[Fieldstone] Fountain")
                .FeatureTile("[Fieldstone] Garden - Vegetable")
                .FeatureTile("[Fieldstone] Garden - Flower")
                .FeatureTile("[Fieldstone] Gazebo")
                .FeatureTile("[Fieldstone] Market 1")
                .FeatureTile("[Fieldstone] Market 2")
                .FeatureTile("[Fieldstone] Market - Slum 1")
                .FeatureTile("[Fieldstone] Market - Slum 2")
                .FeatureTile("[Fieldstone] Plaza 1")
                .FeatureTile("[Fieldstone] Plaza 2")
                .FeatureTile("[Fieldstone] Streetlight")
                .FeatureTile("[Fieldstone] Tree")
                .FeatureTile("[Fieldstone] Wagon")
                .FeatureTile("[Fieldstone] Well")
                .FeatureTile("[Fieldstone] Wall - Chunk")
                .FeatureTile("[Fieldstone] Building - Destroyed 1")
                .FeatureTile("[Fieldstone] Building - Destroyed 2")
                .FeatureTile("[Fieldstone] Building - Destroyed 3")
                .FeatureTile("[Fieldstone] Building - Burned")
                .SetPiece("[Fieldstone] Portal", 1)
                .ExitGroup("[Fieldstone] House - Slum 1")
                .ExitGroup("[Fieldstone] House - Slum 2")
                .ExitGroup("[Fieldstone] House")
                .ExitGroup("[Fieldstone] Sewer Entrance 1")
                .ExitGroup("[Fieldstone] Wall - Breach")
                .ExitGroup("[Fieldstone] Wall - Door 1")
                .ExitGroup("[Fieldstone] Building - Wall Breach")
                .ExitGroup("[Fieldstone] Building - Wall Temple")
                .ExitGroup("[Fieldstone] Castle - Breach, Evil")
                .ExitGroup("[Fieldstone] Castle - Breach, Good")
                .ExitGroup("[Fieldstone] Castle - Main Door, Evil")
                .ExitGroup("[Fieldstone] Castle - Main Door, Good")
                .ExitGroup("[Fieldstone] Castle - Small Door, Evil")
                .ExitGroup("[Fieldstone] Castle - Small Door, Good")
                .ExitGroup("[Fieldstone] Wall - Tower 1")
                .ExitGroup("[Fieldstone] Wall - Tower 2")
                .SetPiece("[Fieldstone] House 01 (2x3)")
                .SetPiece("[Fieldstone] House 02 (2x2)")
                .SetPiece("[Fieldstone] House 03 (2x2)")
                .SetPiece("[Fieldstone] House 04 (2x2)")
                .SetPiece("[Fieldstone] House 05 (2x2)")
                .SetPiece("[Fieldstone] House 06 (2x2)")
                .SetPiece("[Fieldstone] House 07 (1x2)")
                .SetPiece("[Fieldstone] House 08 (1x2)")
                .SetPiece("[Fieldstone] House 09 (1x2)")
                .SetPiece("[Fieldstone] House 10 (1x2)")
                .SetPiece("[Fieldstone] House - Slum (1x2)")
                .SetPiece("[Fieldstone] Inn - Slum 1 (1x2)")
                .SetPiece("[Fieldstone] Inn - Slum 2 (1x2)")
                .SetPiece("[Fieldstone] Barracks (2x2)")
                .SetPiece("[Fieldstone] Temple - Evil (2x3)")
                .SetPiece("[Fieldstone] Temple - Good (3x3)")
                .SetPiece("[Fieldstone] Temple - Neutral (2x2)")
                .SetPiece("[Fieldstone] Tower - Cloak (2x2)")
                .SetPiece("[Fieldstone] Tower - Guard (1x2)")
                .SetPiece("[Fieldstone] Tower - Ruined (2x2)")
                .SetPiece("[Fieldstone] Tower - Wizard (1x2)")
                .SetPiece("[Fieldstone] Gate - City (2x2)")
                .SetPiece("[Fieldstone] Gate - City (2x3)")
                .SetPiece("[Fieldstone] Building - State 1 (2x3)")
                .SetPiece("[Fieldstone] Building - State 2 (2x3)")
                .SetPiece("[Fieldstone] Arena (3x3)")
                .SetPiece("[Fieldstone] Fountain (1x2)")
                .SetPiece("[Fieldstone] Garden - Flower_1x2")
                .SetPiece("[Fieldstone] Market (1x2)")
                .SetPiece("[Fieldstone] Plaza (2x2)")
                .SetPiece("[Fieldstone] Pool - Holy (2x2)")
                .SetPiece("[Fieldstone] Ruined Park (1x2)")
                .SetPiece("[Fieldstone] Tree - Giant (2x2)")
                .SetPiece("[Fieldstone] Building - Burned (1x2)")
                .SetPiece("[Fieldstone] Building - Destroyed (1x2)")
                .SetPiece("[Fieldstone] Boat", 1)
                .SetPiece("[Fieldstone] Boathouse", 1)
                .SetPiece("[Fieldstone] Ship - Small, Docked (2x2)", 1)
                .SetPiece("[Fieldstone] Ship - Small, Floating (1x2)", 1);

            // City Exterior* (Gothic) -- GothicCobble/GothicBuilding/GothicEvilCastle/GothicGoodCastle
            // district PaletteVariant, same shape as Fieldstone above (16-combo probe against
            // Solid=Water/Open=GothicCobble verified 16/16; TunnelCrossers("GothicBridge","GothicBridge")
            // independently verified TRUE). Mirrors City's group family tile-for-tile minus the naval
            // fleet, plus its own "Chessboard 1"/"Chessboard 2"/"Market (1x2)"/"Building - Burned (1x2)"
            // naming deltas.
            _builder.Create(CityExteriorGothic, "City Exterior* (Gothic)")
                .Tileset("tcn01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .PaletteVariant()
                .PrimaryOpenTerrain("GothicCobble")
                .TunnelCrossers("GothicDock", "GothicDock")
                .FeatureTile("[Gothic] Chessboard 1")
                .FeatureTile("[Gothic] Chessboard 2")
                .FeatureTile("[Gothic] Construction")
                .FeatureTile("[Gothic] Fountain")
                .FeatureTile("[Gothic] Garden - Vegetable")
                .FeatureTile("[Gothic] Garden - Flower")
                .FeatureTile("[Gothic] Gazebo")
                .FeatureTile("[Gothic] Market 1")
                .FeatureTile("[Gothic] Market 2")
                .FeatureTile("[Gothic] Market - Slum 1")
                .FeatureTile("[Gothic] Market - Slum 2")
                .FeatureTile("[Gothic] Plaza 1")
                .FeatureTile("[Gothic] Plaza 2")
                .FeatureTile("[Gothic] Streetlight")
                .FeatureTile("[Gothic] Tree")
                .FeatureTile("[Gothic] Wagon")
                .FeatureTile("[Gothic] Well")
                .FeatureTile("[Gothic] Wall - Chunk")
                .FeatureTile("[Gothic] Building - Destroyed 1")
                .FeatureTile("[Gothic] Building - Destroyed 2")
                .FeatureTile("[Gothic] Building - Destroyed 3")
                .FeatureTile("[Gothic] Building - Burned")
                .SetPiece("[Gothic] Portal", 1)
                .ExitGroup("[Gothic] House - Slum 1")
                .ExitGroup("[Gothic] House - Slum 2")
                .ExitGroup("[Gothic] House")
                .ExitGroup("[Gothic] Sewer Entrance 1")
                .ExitGroup("[Gothic] Wall - Breach")
                .ExitGroup("[Gothic] Wall - Door 1")
                .ExitGroup("[Gothic] Building - Wall Breach")
                .ExitGroup("[Gothic] Building - Wall Temple")
                .ExitGroup("[Gothic] Castle - Breach, Evil")
                .ExitGroup("[Gothic] Castle - Breach, Good")
                .ExitGroup("[Gothic] Castle - Main Door, Evil")
                .ExitGroup("[Gothic] Castle - Main Door, Good")
                .ExitGroup("[Gothic] Castle - Small Door, Evil")
                .ExitGroup("[Gothic] Castle - Small Door, Good")
                .ExitGroup("[Gothic] Wall - Tower 1")
                .ExitGroup("[Gothic] Wall - Tower 2")
                .SetPiece("[Gothic] House 01 (2x3)")
                .SetPiece("[Gothic] House 02 (2x2)")
                .SetPiece("[Gothic] House 03 (2x2)")
                .SetPiece("[Gothic] House 04 (2x2)")
                .SetPiece("[Gothic] House 05 (2x2)")
                .SetPiece("[Gothic] House 06 (2x2)")
                .SetPiece("[Gothic] House 07 (1x2)")
                .SetPiece("[Gothic] House 08 (1x2)")
                .SetPiece("[Gothic] House 09 (1x2)")
                .SetPiece("[Gothic] House 10 (1x2)")
                .SetPiece("[Gothic] House - Slum (1x2)")
                .SetPiece("[Gothic] Inn - Slum 1 (1x2)")
                .SetPiece("[Gothic] Inn - Slum 2 (1x2)")
                .SetPiece("[Gothic] Barracks (2x2)")
                .SetPiece("[Gothic] Temple - Evil (2x3)")
                .SetPiece("[Gothic] Temple - Good (3x3)")
                .SetPiece("[Gothic] Temple - Neutral (2x2)")
                .SetPiece("[Gothic] Tower - Cloak (2x2)")
                .SetPiece("[Gothic] Tower - Guard (1x2)")
                .SetPiece("[Gothic] Tower - Ruined (2x2)")
                .SetPiece("[Gothic] Tower - Wizard (1x2)")
                .SetPiece("[Gothic] Gate - City (2x2)")
                .SetPiece("[Gothic] Building - State 1 (2x3)")
                .SetPiece("[Gothic] Building - State 2 (2x3)")
                .SetPiece("[Gothic] Arena (3x3)")
                .SetPiece("[Gothic] Fountain (1x2)")
                .SetPiece("[Gothic] Garden - Flower (1x2)")
                .SetPiece("[Gothic] Market (1x2)")
                .SetPiece("[Gothic] Plaza (2x2)")
                .SetPiece("[Gothic] Pool - Holy (2x2)")
                .SetPiece("[Gothic] Ruined Park (1x2)")
                .SetPiece("[Gothic] Tree - Giant (2x2)")
                .SetPiece("[Gothic] Building - Burned (1x2)")
                .SetPiece("[Gothic] Building - Destroyed (1x2)")
                .SetPiece("[Gothic] Boat", 1)
                .SetPiece("[Gothic] Boathouse", 1)
                .SetPiece("[Gothic] Ship - Small, Docked (2x2)", 1)
                .SetPiece("[Gothic] Ship - Small, Floating (1x2)", 1);

            // City Exterior* (Sigil) -- the tileset's smallest, structurally distinct district (13
            // groups / 61 tiles): a hive/chasm quarter on its own SigilCobble/SigilHill/SigilChasm/
            // SigilBuilding/SigilCastle terrain family. UNLIKE City/Fieldstone/Gothic, a direct 16-combo
            // probe against the default Solid=Water/Open=SigilCobble pairing measured only 14/16 (the
            // two diagonal-split-corner combos [Water,SigilCobble,Water,SigilCobble] and its rotation
            // never resolve) -- SolidTerrainOverride("SigilCastle") is required here, verified 16/16
            // against Open=SigilCobble. RoadCrosser("SigilRoad") verified TRUE via RoadVocabularyCheck
            // (all five shapes resolve) -- see the base CityExterior profile's own doc comment on this
            // making tcn01 the second Streets-capable tileset after vmr01. SigilRoad independently
            // verified FALSE as a Tunnel body/port (it is a street-lane crosser, not a wall-embedded
            // one) -- no TunnelCrossers declared; Sigil has no district-specific Dock/Bridge crosser
            // pair at all (only City/Fieldstone/Gothic do), consistent with Sigil being a landlocked
            // hive quarter rather than a harbor district.
            //
            // "[Sigil] Final Area (7x7)" is a 49-tile finale/boss-chamber set piece, heavily SigilChasm-
            // cornered (144/196 corners) -- the same oversized showcase-piece shape as Barrows'
            // FinalArea_7x7/City's own Amphitheater above, kept wired for future larger-area generation.
            // The five WallAlcove-shaped 1x1 door groups (Door - Castle, House - Low, House - Tall,
            // Shop - Green, Shop - Harys) mirror every other district's own single-door-building shape.
            _builder.Create(CityExteriorSigil, "City Exterior* (Sigil)")
                .Tileset("tcn01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .PaletteVariant()
                .SolidTerrainOverride("SigilCastle")
                .PrimaryOpenTerrain("SigilCobble")
                .AccentTerrain("SigilChasm")
                .RoadCrosser("SigilRoad")
                .FeatureTile("[Sigil] Fountain")
                .FeatureTile("[Sigil] Midden 1")
                .FeatureTile("[Sigil] Pipes")
                .FeatureTile("[Sigil] Puddle 1")
                .FeatureTile("[Sigil] Puddle 2")
                .FeatureTile("[Sigil] Puddle 3")
                .SetPiece("[Sigil] Door - Castle", 1)
                .SetPiece("[Sigil] House - Low")
                .SetPiece("[Sigil] House - Tall")
                .SetPiece("[Sigil] Shop - Green")
                .SetPiece("[Sigil] Shop - Harys")
                // Non-flat (a raised minaret spire, two door slots); kept wired per the same
                // height-exemption safety-net convention as City's Wall - Tower/Gate - City above.
                .SetPiece("[Sigil] Minaret", 1)
                .SetPiece("[Sigil] Final Area (7x7)", 1);

            // Frozen Wastes* (tti01, SWLOR_Haks/sw_t_frozen, 510 tiles, HasHeightTransition=1,
            // Transition=5). GENERAL Border=Default="Pit", Floor="Floor" -- unlike EVERY prior exterior
            // profiles (ttd01/ttf01/ttf02/jac01/fcx01/tno01/tcn01), Default and Floor here are genuinely
            // DIFFERENT terrains, so none of the usual "degenerate Default==Floor" inversion reasoning
            // applies. Pathnode data confirms this is the PLAIN case instead: the 4 pure-Pit tiles
            // never carry pathnode 'A' (G/T/N only, verified directly) -- Pit is a real impassable
            // void/crevasse -- while the 47 pure-Floor tiles are dominated by pathnode 'A' (32/47).
            // Direct 16-combo probe (TileResolver.HasCandidate) confirms Solid=Pit/Open=Floor reaches
            // a full 16/16 (and, for what it's worth, so does the reverse pairing -- the Floor/Pit
            // corner-blend family is rich enough, 21 flat ungrouped tiles, to support either direction
            // structurally; pathnode data is what actually decides which one is gameplay-correct). No
            // SolidTerrainOverride is declared at all: LayoutSolver.Solve's own empty-means-Default
            // stamp already yields Solid="Pit", so this is the same plain composition shape every
            // interior tileset uses (DungeonTilesetProfile.SolidTerrainOverride's own doc comment: "the
            // tileset's declared Default terrain, which is correct for every interior tileset") --
            // just happening to land on an exterior-flavored hak, a genuinely new shape among the
            // exterior profiles covered so far (neither RuralGrass/RuralWinter's "no wall concept at
            // all" open field, nor ttd01/ttf01/jac01's "invert because Default==Floor" degenerate case).
            //
            // Terrains(3)=Pit,Floor,EvilCastle; Crossers(0) -- literally none. No Road, no Tunnel body/
            // port pair, no Wall/Stream gate family, no ramp-lane crosser at all: every group and every
            // elevation transition here resolves purely off corner terrain + door slots, never an edge
            // crosser. Complex's Tunnel mode has nothing to key off and downgrades to OpenLane
            // unconditionally (TunnelVocabularyCheck.SupportsTunnels returns false immediately -- no
            // "Corridor"/"Doorway" crosser is even declared to check shapes for).
            //
            // EvilCastle (1 pure tile, pathnode 'A'; 9 Floor/EvilCastle corner-blend tiles; 3 GROUPs:
            // "Castle - Main Door/Breach/Small Door, Evil") is the identical starved-but-group-bearing
            // shape RuralGrass/RuralWinter's own Good/EvilCastle families are -- verified 16/16 both
            // ways against Floor (Solid=EvilCastle/Open=Floor and the reverse), so it becomes the
            // FrozenWastesEvilCastle PaletteVariant below rather than a base-profile terrain, mirroring
            // RuralGrassEvilCastle/RuralWinterEvilCastle exactly. EvilCastle vs Pit itself only reaches
            // 2/16 (they never appear together on any real tile -- no blend exists), which is irrelevant
            // since no composition ever pairs them.
            //
            // Heights (79 of 510 tiles carry a nonzero corner height, all on Floor, e.g. TILE0's
            // [1,1,0,1] -- verified directly, Pit and EvilCastle never carry height): MaxElevationRegions
            // (2) and MaxReliefRegions(2), mirroring jac01's own caps (the closest structural analog:
            // a natural, non-degenerate Solid/Open split with real height content). No RampCrosser or
            // ReliefBlendTerrain is declared -- there is no dedicated ramp-lane crosser (0 crossers
            // total) and no gentle-blend terrain (only Pit/Floor/EvilCastle exist) for either mechanism
            // to key off; LayoutElevationPainter's rim-vocabulary/lane gates simply find no candidates
            // and no-op, the same safe self-gating every other tileset's unset knobs rely on. The
            // "Ramp" and "Cave" GROUPs (both 1x1, all-Floor, doorless/door respectively) are wired as
            // SetPieces -- the same baked-mesh ReliefPiece stamping mechanism ttd01/ttf01/jac01's own
            // "Ramp"/"Cave"/"SmallCave" groups use, stamped onto painted raised rim edges.
            //
            // MinimumOpeningWidth stays the verified default of 1 (PathNodeOpeningWidthAudit against
            // Solid=Pit/Open=Floor finds a pathnode-'A' candidate among the 14 partially-open corner
            // combos), matching the earlier exterior profiles.
            //
            // Group census: "Chessboard"/"Portal" (FeatureTile, same names/role as RuralGrass/
            // RuralWinter's own) plus "Ice Creator"/"Market 1"/"Market 2"/"Crystal" (this tileset's own
            // FeatureTile-shaped decor, all 1x1 doorless all-Floor). "Entrance - Evil" (1x1, door, pure
            // Floor corners -- NOT an EvilCastle-terrain group) is wired as an ExitGroup on this base
            // profile rather than the castle variant, since its footprint never touches EvilCastle at
            // all. "Cave"/"Ramp" are SetPieces (ReliefPiece kind, see above). "Dragon Skeleton (1x2)",
            // "Temple - Evil 1 (2x3)"/"Temple - Neutral (2x2)"/"Temple - Evil 2 (2x3)", "Ship - Air,
            // Docked (3x1)", and "Tower - Ice" (2x2 footprint, no "(2x2)" in its own GROUP Name unlike
            // its siblings) are ordinary all-Floor OpenSetPieces, maxPerArea 1
            // each, matching the "one showcase building per area" convention the earlier profiles use.
            // "Ship - Air, Above Pit (3x1)" is all-Pit (door-bearing) -- unlike RuralGrass's own
            // "Ship - Air, Above Trees (3x1)" (exempt there because Trees is a totally unwired,
            // uncomposed terrain), Pit here IS this base profile's own composed Solid terrain, so this
            // group is wired as a SetPiece too; see tile-coverage and registered-tileset pipeline tests
            // for whether it actually places (an all-Solid-cornered door group anchored on the
            // composition's own wall mass, structurally analogous to Desert/Forest's own Solid-anchored
            // door groups).
            //
            // Lighting: 431 of 510 tiles (every ungrouped simple tile) uniformly carry MainLight1=1,
            // MainLight2=1, SourceLight1=1, SourceLight2=1 (verified directly); the remaining 79 are
            // hand-lit GROUP members with their own baked values, which TileLighting doesn't touch.
            //
            // No hand-built module areas exist stamping tti01 (verified: zero .are.json references to
            // this resref anywhere in the module). No evidence-mined decoration palette is available
            // either, so no bulk .Decoration(...)/.Vignette(...) palette is declared here -- matching
            // the documented fallback rule (nearest-family reuse is a judgment call left for a future
            // visual-review pass, not fabricated here without evidence).
            _builder.Create(FrozenWastes, "Frozen Wastes*")
                .Tileset("tti01")
                .Placeholder("gen_placeholder1")
                .TileLighting(1, 1, 1, 1)
                .PrimaryOpenTerrain("Floor")
                .MaxElevationRegions(2)
                .MaxReliefRegions(2)
                .FeatureTile("Chessboard")
                .FeatureTile("Portal")
                .FeatureTile("Ice Creator")
                .FeatureTile("Market 1")
                .FeatureTile("Market 2")
                .FeatureTile("Crystal")
                .ExitGroup("Entrance - Evil")
                .SetPiece("Cave", 1)
                .SetPiece("Ramp", 1)
                .SetPiece("Dragon Skeleton (1x2)", 1)
                .SetPiece("Temple - Evil 1 (2x3)", 1)
                .SetPiece("Temple - Neutral (2x2)", 1)
                .SetPiece("Temple - Evil 2 (2x3)", 1)
                .SetPiece("Ship - Air, Above Pit (3x1)", 1)
                .SetPiece("Ship - Air, Docked (3x1)", 1)
                .SetPiece("Tower - Ice", 1);

            // FrozenWastes' EvilCastle accent-slot palette -- PaletteVariant profile recomposing the
            // SAME tti01 hak data the base FrozenWastes profile above uses, mirroring
            // RuralGrassEvilCastle/RuralWinterEvilCastle exactly: SolidTerrainOverride("EvilCastle") +
            // PrimaryOpenTerrain("Floor") gives a walled-castle-grounds composition (verified 16/16
            // both ways above) that unlocks the "Castle - Main Door/Breach/Small Door, Evil" GROUPs as
            // ExitGroups, the same door/breach/small-door trio role those exact GROUP names play on
            // RuralGrass/RuralWinter's own castle variants.
            _builder.Create(FrozenWastesEvilCastle, "Frozen Wastes* (Evil Castle)")
                .Tileset("tti01")
                .Placeholder("gen_placeholder1")
                .TileLighting(1, 1, 1, 1)
                .PaletteVariant()
                .SolidTerrainOverride("EvilCastle")
                .PrimaryOpenTerrain("Floor")
                .ExitGroup("Castle - Main Door, Evil")
                .ExitGroup("Castle - Breach, Evil")
                .ExitGroup("Castle - Small Door, Evil");

            // Tropical* (ttz01, SWLOR_Haks/sw_t_coastal, 442 tiles, HasHeightTransition=1). GENERAL
            // Border=Default=Floor="grass" -- the same degenerate-into-simplicity shape ttr01/tts01
            // already established: all three GENERAL slots are the SAME terrain, so the base
            // composition is the ttr01/tts01 OPEN-FIELD shape (no SolidTerrainOverride at all; Solid
            // defaults to "grass" == PrimaryOpenTerrain("grass")). Direct 16-combo probe confirms grass
            // reaches a full 16/16 against every other terrain (water, trees, sand).
            //
            // Terrains(4)=grass,water,trees,sand; Crossers(4)=stream,wall1,wall2,road -- no canonical
            // "Corridor"/"Doorway" pair (verified directly), the same shape as ttr01/tts01's own 5-
            // crosser roster (Stream/Wall1/Wall2/Road/Slope) minus a ramp-lane crosser -- Complex
            // downgrades to OpenLane unconditionally. RoadVocabularyCheck confirms "road" resolves all
            // five shapes (stub/straight/turn/T/X) against grass AND water, so RoadCrosser("road") is
            // declared on the grass-open profiles below; sand only resolves "stream", not "road" (2/5
            // shapes fail), so no RoadCrosser is declared on the Sand variant. Wall1/Wall2/Stream are
            // the identical WallRoom-eligible-but-Tunnel-vocab-starved family ttr01/tts01's own
            // Wall1/Wall2/Stream already document (see PilotExpectedExemptions below) -- Complex having
            // no Tunnel-mode wall mass at all here means these gate/bridge groups never get a chance to
            // place regardless of crosser vocabulary.
            //
            // trees (1 pure tile, pathnode 'T' -- restricted, 20 grass/trees + 4 trees/water blend
            // tiles, NO GROUP anywhere touches it, verified directly) is the identical starved-minor-
            // family shape RuralGrass's own "Trees" is -- same PilotAlternateVocabTerrains treatment,
            // no PaletteVariant needed.
            //
            // sand (160 corner instances across grass, 52 pure tiles, pathnode 'A' dominant (46/52) --
            // genuinely walkable, NOT a minor accent) carries its OWN near-complete building roster
            // (Well/Crystal/TreeHollow/Menhir/Shrine/AntHill/Granary/Tower/Warzone/Field/Temple01-03/
            // Barracks/Tower_1x2/Portal/Chessboard, all suffixed "(sand)"/"(Sand)" -- 22 GROUPs) --
            // structurally the SAME shape as grass's own roster, just missing the farm-village subset
            // (Barn/Farm/Inn/House/Windmill/Mausoleum never got a sand counterpart). Direct 16-combo
            // probe confirms Solid=sand/Open=sand (i.e. sand recomposed as its OWN open field, the same
            // "no wall concept" shape the base grass profile uses) reaches full internal coverage, and
            // sand vs water/grass both reach 16/16 either direction. This is a genuinely NEW
            // PaletteVariant shape among this project's registered tilesets: every existing variant
            // (Good/EvilCastle, Water, castlewall/keep) is an INVERSION (SolidTerrainOverride differs
            // from PrimaryOpenTerrain, carving rooms/docks out of a wall/water mass). TropicalSand
            // instead explicitly declares SolidTerrainOverride("sand") == PrimaryOpenTerrain("sand") --
            // the SAME "no wall, no inversion" shape the base profile achieves via omission, just forced
            // onto the OTHER native ground terrain (sand's own Default is "grass", so it must be
            // declared explicitly here; leaving it unset would incorrectly stamp Solid="grass" while
            // Open="sand"). This is a principled extension of the existing SolidTerrainOverride ==
            // PrimaryOpenTerrain open-field rule (see RuralGrass's own doc comment), not a hack: LayoutSolver
            // has no special-case for "which terrain", only whether Solid equals Open.
            //
            // water (57 pure tiles, pathnode MOSTLY non-'A' -- N=26/I=12/L=7/A=7/H=4/T=1, genuinely
            // impassable open water except at a handful of shallow/dock tiles) carries a real
            // shipping/dock roster (18 GROUPs: ShipDocked01-03, ShipFloating, MerchantDocked01-03,
            // WeatheredDocked01-03, MerchantFloating, WeatheredFloating, MerchantWeathered, Lighthouse,
            // Bridge_Door), split across two mixed-terrain families -- grass/water (ShipDocked01,
            // MerchantDocked01/02, WeatheredDocked01/02, Lighthouse, MerchantWeathered) and sand/water
            // (ShipDocked03, MerchantDocked03, WeatheredDocked03, Shipwreck) -- plus several all-water
            // (Solid-anchored, the same "Ship - Air, Above Pit" shape FrozenWastes' own doc comment
            // documents) pieces. TWO water PaletteVariants close both families: TropicalWater
            // (SolidTerrainOverride("water") + PrimaryOpenTerrain("grass"), mirroring RuralGrassWater
            // exactly) and TropicalSandWater (SolidTerrainOverride("water") + PrimaryOpenTerrain("sand"),
            // the same water-solid shape recomposed onto the sand district) -- without the second
            // variant, ShipDocked03/MerchantDocked03/WeatheredDocked03/Shipwreck could never classify
            // under ANY composition (no variant ever pairs sand and water together otherwise).
            //
            // Two grass+sand MIXED groups -- "Mysterious_Cave" (2x2, flat, door) and "Cave(sand)" (1x1,
            // raised, door, despite its "(sand)" suffix carrying grass corners too) -- never classify
            // under any of the four compositions above: no variant ever composes BOTH grass and sand
            // together as a Solid/Open pair (each variant's pair is grass/grass, sand/sand, water/grass,
            // or water/sand), so a group whose own corners span BOTH grass and sand always has at least
            // one corner matching neither terrain in the composition's binary. See
            // TileCoverageCensusTests.PilotExpectedExemptions for both entries.
            //
            // Heights (verified directly: "Cave"/"DwarfCave"/"Ramp", all-grass, all raised with no
            // crosser) get MaxReliefRegions(2) on both grass-open profiles (base Tropical and
            // TropicalSand), the same corner-relief mechanism ttr01/tts01's own open-field profiles use
            // -- NOT MaxElevationRegions, unlike FrozenWastes/jac01: those are non-open-field
            // compositions with a real wall mass to constrain elevation blobs against. No RampCrosser
            // (no dedicated ramp-lane crosser exists among the 4 declared crossers) and no
            // ReliefBlendTerrain (no gentle-blend terrain exists -- only grass/water/trees/sand).
            // "Cave"/"DwarfCave"/"Ramp" are wired as SetPieces (ReliefPiece kind, Complex layout only --
            // see ReliefPiecePlacementRateTests' own doc comment on why only Complex ever paints a
            // nonzero relief field).
            //
            // RESOLVED GAP (was: nine door-bearing WallAlcove groups measured 36-39% isolated / 60.3%
            // full-wire single-attempt "disconnected open space" failures against Organic, so they
            // shipped unwired). Root cause, pinned with pass-by-pass instrumentation (ProbeTool
            // "dissect", seed 3, Barn01_2x2 isolated): ttz01.set spells the SAME terrain two ways --
            // [GENERAL] Default=Grass (capital) but [TERRAIN0] Name=grass with lowercase tile-corner
            // labels -- so with no SolidTerrainOverride declared here, LayoutSolver stamped
            // SolidTerrain="Grass" (from Default) while OpenTerrain="grass" (this profile), and the
            // intended Solid==Open open-field composition actually ran as a TWO-label mixed regime:
            // ordinal comparers (OrganicCaveLayout's ==, ValidateInvariants' HashSet open-label
            // connectivity) saw a real solid-mass cave, while case-insensitive comparers
            // (LayoutGroupStamper.Eq classification/site checks, TileResolver) saw one degenerate
            // terrain. Door-bearing groups route WallAlcove, whose "fully solid" site search (Eq)
            // accepted fully-OPEN field cells anywhere, and WriteMember's Canonicalize checks
            // SolidTerrain FIRST -- rewriting the stamped tiles' lowercase "grass" corners to capital
            // "Grass", i.e. physically converting open corners to solid. A stamp landing against the
            // open blob's edge pinches off a pocket => disconnection. Door-free groups route
            // OpenSetPiece (room-interior + full margin ring, which cannot enclose anything -- and on
            // Organic they rarely find a site at all), hence the exact door=True discriminator.
            // ttr01/tts01 never hit this because their .set files spell Default and [TERRAIN0]
            // identically ("Grass"/"Snow") and their profiles match that spelling; TropicalSand's
            // explicit SolidTerrainOverride("sand")==PrimaryOpenTerrain("sand") is likewise one
            // string. Fixed generally in MacroLayoutGenerator's terrain-label case unification (case-
            // split labels are snapped to the tileset's declared [TERRAIN] spelling on a clone before
            // dispatch; gated so agreeing compositions are byte-identical). Post-fix measurements
            // (ProbeTool, retryCount=1): Barn01_2x2 isolated maxPerArea=5 0/60 disconnections (was
            // 24/60 on the same seeds), full production wiring 0/300 (was 181/300), ttr01 siblings and
            // Barracks_1x2(sand) stay 0. TerrainLabelCaseUnificationTests pins all of this. The nine
            // groups are wired below (maxPerArea 1 each) alongside the nine always-safe door-free
            // groups.
            //
            // MinimumOpeningWidth stays the verified default of 1 for all four Solid/Open pairings
            // (grass/grass, sand/sand, water/grass, water/sand), matching the earlier exterior profiles.
            //
            // Lighting: all 442 tiles uniformly carry MainLight1=1, MainLight2=1, SourceLight1=1,
            // SourceLight2=1 (verified directly) -- no mixed hand-lit GROUP members exist here, unlike
            // FrozenWastes' 431/510 split.
            //
            // No hand-built module areas exist stamping ttz01 (verified: zero .are.json references to
            // this resref anywhere in the module), so no evidence-mined decoration palette is declared
            // here either -- the same documented-gap fallback rule FrozenWastes' own doc comment uses.
            _builder.Create(Tropical, "Tropical*")
                .Tileset("ttz01")
                .Placeholder("gen_placeholder1")
                .TileLighting(1, 1, 1, 1)
                .PrimaryOpenTerrain("grass")
                .AccentTerrain("water")
                .RoadCrosser("road")
                .MaxReliefRegions(2)
                .FeatureTile("Well")
                .FeatureTile("Shrine01")
                .FeatureTile("Menhir")
                .FeatureTile("Crystal")
                .FeatureTile("TreeHollow")
                .FeatureTile("AntHill")
                .FeatureTile("Granary")
                .FeatureTile("Field")
                .FeatureTile("Orchard")
                .FeatureTile("Warzone01")
                .FeatureTile("Warzone02")
                .FeatureTile("Garden01")
                .FeatureTile("Garden02")
                .FeatureTile("Tower")
                .FeatureTile("Graves01")
                .FeatureTile("Graves02")
                .FeatureTile("Graves03")
                .FeatureTile("Graves04")
                .FeatureTile("Graves05")
                .FeatureTile("Shrine02")
                .FeatureTile("Tree")
                .FeatureTile("Portal")
                .FeatureTile("Chessboard")
                .ExitGroup("House01")
                .ExitGroup("House02")
                .ExitGroup("Mausoleum01")
                .ExitGroup("Mausoleum02")
                .SetPiece("Cave", 1)
                .SetPiece("DwarfCave", 1)
                .SetPiece("Ramp", 1)
                .SetPiece("DragSkel_1x2", 1)
                .SetPiece("Field01_2x2", 1)
                .SetPiece("Field02_2x2", 1)
                .SetPiece("Field03_2x1", 1)
                .SetPiece("Tower_1x2", 1)
                .SetPiece("Warzone_1x2", 1)
                .SetPiece("Temple03_3x2", 1)
                .SetPiece("Temple02_2x2", 1)
                .SetPiece("Temple01_3x2", 1)
                .SetPiece("Barn01_2x2", 1)
                .SetPiece("Barn02_1x2", 1)
                .SetPiece("Barn03_1x2", 1)
                .SetPiece("Inn_1x2", 1)
                .SetPiece("Farm01_2x2", 1)
                .SetPiece("Farm02_1x2", 1)
                .SetPiece("Farm03_1x2", 1)
                .SetPiece("Barracks_1x2", 1)
                .SetPiece("Windmill_2x2", 1);

            // Tropical's Sand accent-slot palette -- PaletteVariant profile recomposing the SAME ttz01
            // hak data the base Tropical profile above uses, but a genuinely NEW shape among this
            // project's registered variants: SolidTerrainOverride("sand") == PrimaryOpenTerrain("sand"),
            // an open field on sand rather than an inversion. See Tropical's own doc comment above for
            // the full reasoning. No RoadCrosser (sand fails 2 of RoadVocabularyCheck's 5 shapes).
            _builder.Create(TropicalSand, "Tropical* (Sand)")
                .Tileset("ttz01")
                .Placeholder("gen_placeholder1")
                .TileLighting(1, 1, 1, 1)
                .PaletteVariant()
                .SolidTerrainOverride("sand")
                .PrimaryOpenTerrain("sand")
                .AccentTerrain("water")
                .MaxReliefRegions(2)
                .FeatureTile("Well(sand)")
                .FeatureTile("Crystal(sand)")
                .FeatureTile("TreeHollow(sand)")
                .FeatureTile("Menhir(sand)")
                .FeatureTile("Shrine(sand)")
                .FeatureTile("AntHill(sand)")
                .FeatureTile("Granary(sand)")
                .FeatureTile("Tower(sand)")
                .FeatureTile("Warzone01(sand)")
                .FeatureTile("Warzone02(sand)")
                .FeatureTile("Field(sand)")
                .FeatureTile("Shrine02(sand)")
                .FeatureTile("Tree(sand)")
                .FeatureTile("Portal(Sand)")
                .FeatureTile("Chessboard(Sand)")
                .SetPiece("DragSkel_1x2(sand)", 1)
                .SetPiece("Temple01_3x2(sand)", 1)
                .SetPiece("Temple02_2x2(sand)", 1)
                .SetPiece("Temple03_3x2(sand)", 1)
                .SetPiece("Warzone_1x2(sand)", 1)
                .SetPiece("Barracks_1x2(sand)", 1)
                .SetPiece("Tower_1x2(sand)", 1);

            // Tropical's Water accent-slot palette -- PaletteVariant profile recomposing the SAME ttz01
            // hak data with SolidTerrainOverride("water") + PrimaryOpenTerrain("grass"), mirroring
            // RuralGrassWater/RuralWinterWater exactly: a docks/shipping showcase carved out of an
            // open-water mass. Unlocks the grass/water mixed family plus the all-water (Solid-anchored)
            // pieces.
            _builder.Create(TropicalWater, "Tropical* (Water)")
                .Tileset("ttz01")
                .Placeholder("gen_placeholder1")
                .TileLighting(1, 1, 1, 1)
                .PaletteVariant()
                .SolidTerrainOverride("water")
                .PrimaryOpenTerrain("grass")
                .SetPiece("ShipDocked01_2x2", 1)
                .SetPiece("ShipDocked02_2x2", 1)
                .SetPiece("ShipFloating_2x1", 1)
                .SetPiece("MerchantDocked01_3x2", 1)
                .SetPiece("WeatheredDocked01_3x2", 1)
                .SetPiece("WeatheredDocked02_3x2", 1)
                .SetPiece("MerchantDocked02_3x2", 1)
                .SetPiece("WeatheredFloating_3x1", 1)
                .SetPiece("MerchantFloating_3x1", 1)
                .SetPiece("MerchantWeathered", 1)
                .SetPiece("Lighthouse", 1)
                .SetPiece("Bridge_Door", 1);

            // Tropical's Sand+Water accent-slot palette -- PaletteVariant profile recomposing the SAME
            // ttz01 hak data with SolidTerrainOverride("water") + PrimaryOpenTerrain("sand") -- the same
            // water-solid docks shape as TropicalWater, recomposed onto the sand district instead of
            // grass, to close the sand/water mixed family (ShipDocked03/MerchantDocked03/
            // WeatheredDocked03/Shipwreck) that never pairs with grass at all.
            _builder.Create(TropicalSandWater, "Tropical* (Sand + Water)")
                .Tileset("ttz01")
                .Placeholder("gen_placeholder1")
                .TileLighting(1, 1, 1, 1)
                .PaletteVariant()
                .SolidTerrainOverride("water")
                .PrimaryOpenTerrain("sand")
                .SetPiece("ShipDocked03_2x2", 1)
                .SetPiece("MerchantDocked03_3x2", 1)
                .SetPiece("WeatheredDocked03_3x2", 1)
                .SetPiece("Shipwreck", 1);

            // Underdark* (ttu01) -- see this file's own Underdark const doc comment above for the full
            // composition writeup (SolidTerrainOverride("Rock")/PrimaryOpenTerrain("Floor") inversion,
            // AccentTerrain("Water") vs the unwired Chasm sibling, RoadCrosser("Wall"), the RuinWall/
            // Wall gate-family exemptions, and the naval Docked-piece exemptions).
            _builder.Create(Underdark, "Underdark*")
                .Tileset("ttu01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .SolidTerrainOverride("Rock")
                .PrimaryOpenTerrain("Floor")
                .AccentTerrain("Water")
                .RoadCrosser("Wall")
                .MaxElevationRegions(2)
                .MaxReliefRegions(2)
                .FeatureTile("Market - Duergar")
                .FeatureTile("Market - Illithid")
                .FeatureTile("Market - Beholder")
                .FeatureTile("Market - Drow")
                .FeatureTile("Ruin - Old Square")
                .FeatureTile("Ruin - House 1")
                .FeatureTile("Ruin - House 2")
                .FeatureTile("Ruin - House 3")
                .ExitGroup("Building - Duergar")
                .ExitGroup("Door - Dome")
                .ExitGroup("Entrance - Catacombs")
                .ExitGroup("Ruin - Cellar 1")
                .ExitGroup("Ruin - Cellar 2")
                .ExitGroup("Ruin - House 4")
                .ExitGroup("Tower - Square")
                .ExitGroup("Tower - Round")
                .SetPiece("Stairs - Down (2x2)", 1)
                .SetPiece("Stairs - Up (2x2)", 1)
                .SetPiece("Ramp - Up", 2)
                .SetPiece("Ramp - Down", 2)
                .SetPiece("Door - Bridge, Water", 1)
                .SetPiece("Slave Trade Post (2x2)", 1)
                .SetPiece("Building - Illithid 1 (2x2)", 1)
                .SetPiece("Building - Drow (2x2)", 1)
                .SetPiece("Tower - Drow (3x3)", 1)
                .SetPiece("Building - Illithid 2 (2x2)", 1)
                .SetPiece("Building - Svirfneblin 1 (2x2)", 1)
                .SetPiece("Building - Svirfneblin 2 (2x3)", 1)
                .SetPiece("Rock Formation (2x2)", 1)
                .SetPiece("Temple - Drow (2x2)", 1)
                .SetPiece("Slave Huts (2x2)", 1)
                .SetPiece("Illithid Grand Lair (3x3)", 1)
                .SetPiece("Entrance - Beholder", 2)
                .SetPiece("Gates (2x3)", 1)
                .SetPiece("Door - Rock", 2)
                .SetPiece("Observation Dome (3x3)", 1)
                .SetPiece("Entrance - Dungeon (1x2)", 1)
                .SetPiece("Ship - Air, Docked (3x1)", 1)
                .SetPiece("Cave", 1)
                .Decoration("swd_florrd01", 3, DecorationContext.RoomCenter)
                .Decoration("swd_floorm01", 2, DecorationContext.RoomCenter)
                .Decoration("swd_florrt01", 1, DecorationContext.RoomCenter)
                .Decoration("swd_florrt02", 2, DecorationContext.RoomCenter)
                .Decoration("swd_florre01", 1, DecorationContext.RoomCenter)
                .Decoration("swd3_wall001", 3, DecorationContext.WallAdjacent)
                .Decoration("swd3_wall002", 2, DecorationContext.WallAdjacent)
                .Decoration("swd3_wall003", 1, DecorationContext.WallAdjacent)
                .Decoration("zep_shrub036", 2, DecorationContext.WallAdjacent)
                .Decoration("zep_mushroom", 1, DecorationContext.CorridorSide)
                .Decoration("zep_mushroom002", 1, DecorationContext.CorridorSide)
                .Decoration("zep_geiser002", 1, DecorationContext.RoomCenter)
                .Decoration("crystalspire", 1, DecorationContext.RoomCenter);

            // Early Winter 2 (trs02) -- see this file's own EarlyWinter const doc comment above for the
            // full composition writeup (open field on Grass, SecondaryOpenTerrain("Chasm") for the
            // cliff-canyon district, RoadCrosser("Street"), and the Grass2/Water/Trees/Wall/Ridge/
            // Stream/path exemption accounting). No hand-built module areas exist for this tileset
            // (verified: zero .are.json entries reference trs02 outside this pass's own probing), so
            // TileLighting and decoration stay at the neutral (0,0,0,0)/no-palette defaults pending a
            // future evidence-mining pass -- the same time-boxed scope decision SecretBase's own doc
            // comment documents for its palette.
            _builder.Create(EarlyWinter, "Early Winter 2")
                .Tileset("trs02")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .PrimaryOpenTerrain("Grass")
                .SecondaryOpenTerrain("Chasm")
                .RoadCrosser("Street")
                .MaxReliefRegions(2)
                .FeatureTile("Boat1")
                .FeatureTile("ChasmPond")
                .FeatureTile("Spruce")
                .FeatureTile("Spruces")
                .FeatureTile("TreeBush1")
                .FeatureTile("DeadTree1")
                .FeatureTile("Anthill")
                .FeatureTile("DeadTree2")
                .FeatureTile("Pen")
                .FeatureTile("Pond")
                .FeatureTile("HugeTree")
                .FeatureTile("HugeRockTree")
                .FeatureTile("Birch")
                .FeatureTile("CrystalG")
                .FeatureTile("Groundhole")
                .FeatureTile("Shroom1")
                .FeatureTile("Shroom2")
                .FeatureTile("GrassRockFormation")
                .FeatureTile("PoisonWater")
                .FeatureTile("MineShaft")
                .FeatureTile("Camp1")
                .FeatureTile("Orchard")
                .ExitGroup("GoblinHut2")
                .ExitGroup("PenGate")
                .ExitGroup("CliffBottomCave1")
                .ExitGroup("CliffBottomCave2")
                .ExitGroup("CliffTopCave1")
                .SetPiece("DragonSkeleton", 1)
                .SetPiece("Field1", 1)
                .SetPiece("Field2", 1)
                .SetPiece("Field3", 1)
                .SetPiece("CabbagePatch", 1)
                .SetPiece("GoblinHut1", 1)
                .SetPiece("CliffCaveEntry", 1)
                .SetPiece("CliffPath2", 1)
                .SetPiece("HillCave1", 1);

            // Early Winter 2 (Mountain) -- see this file's own EarlyWinterMountain const doc comment
            // above for the full inversion writeup.
            _builder.Create(EarlyWinterMountain, "Early Winter 2 (Mountain)")
                .Tileset("trs02")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .PaletteVariant()
                .SolidTerrainOverride("mountain")
                .PrimaryOpenTerrain("grass")
                .ExitGroup("MountainCave1")
                .ExitGroup("MountainCave2")
                .ExitGroup("MountainCave3")
                .ExitGroup("Mine1")
                .ExitGroup("Mine2")
                .ExitGroup("MountainCave4")
                .ExitGroup("CornerCave1")
                .ExitGroup("InnerCornerCave1")
                .ExitGroup("InnerCornerCave3")
                .ExitGroup("SeaCave1");

            // Medieval Rural 2 (trm02) -- see this file's own MedievalRural const doc comment above for
            // the full composition writeup (open field on Grass, SecondaryOpenTerrain("Chasm") for the
            // cliff-canyon district, RoadCrosser("Street"), MaxReliefRegions for "HillCave1", and the
            // Sand/Water/Trees/Grass2/Road/Stream/Wall/Bridge/Ridge/Street/path exemption accounting).
            _builder.Create(MedievalRural, "Medieval Rural 2")
                .Tileset("trm02")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .PrimaryOpenTerrain("Grass")
                .SecondaryOpenTerrain("Chasm")
                .RoadCrosser("Street")
                .MaxReliefRegions(2)
                .FeatureTile("Spruce")
                .FeatureTile("Spruces")
                .FeatureTile("TreeBush1")
                .FeatureTile("DeadTree1")
                .FeatureTile("Anthill")
                .FeatureTile("DeadTree2")
                .FeatureTile("Pen")
                .FeatureTile("Grainary")
                .FeatureTile("Garden")
                .FeatureTile("HugeTree")
                .FeatureTile("HugeRockTree")
                .FeatureTile("Birch")
                .FeatureTile("CrystalG")
                .FeatureTile("Groundhole")
                .FeatureTile("Shroom1")
                .FeatureTile("Shroom2")
                .FeatureTile("ChickenCoop")
                .FeatureTile("Well1")
                .FeatureTile("GrassRockFormation")
                .FeatureTile("PoisonWater")
                .FeatureTile("MineShaft")
                .FeatureTile("Camp1")
                .FeatureTile("Camp2")
                .FeatureTile("Camp3")
                .FeatureTile("Garden3")
                .FeatureTile("Orchard")
                .FeatureTile("TowerRuins")
                .FeatureTile("Crystal")
                .FeatureTile("CoastPond")
                .FeatureTile("ChasmPond")
                .FeatureTile("Pond")
                .FeatureTile("Camp2a")
                .FeatureTile("Camp3a")
                .FeatureTile("ElfForestTower")
                .ExitGroup("Lighthouse")
                .ExitGroup("GoblinHut2")
                .ExitGroup("PenGate")
                .ExitGroup("HobbitHome3")
                .ExitGroup("HobbitHome5")
                .ExitGroup("TnoHouse1")
                .ExitGroup("TnoHouse2")
                .ExitGroup("SmallFarm1")
                .ExitGroup("SmallFarm2")
                .ExitGroup("SmallFarm3")
                .ExitGroup("Windmill")
                .ExitGroup("FarmShed")
                .ExitGroup("CliffBottomCave1")
                .ExitGroup("CliffBottomCave2")
                .ExitGroup("CliffTopCave1")
                .SetPiece("DragonSkeleton", 1)
                .SetPiece("Field1", 1)
                .SetPiece("Field2", 1)
                .SetPiece("Field3", 1)
                .SetPiece("CabbagePatch", 1)
                .SetPiece("GoblinHut1", 1)
                .SetPiece("HobbitHome1", 1)
                .SetPiece("HobbitHome2", 1)
                .SetPiece("HobbitHome4", 1)
                .SetPiece("ElfHouse1", 1)
                .SetPiece("ElfHouse2", 1)
                .SetPiece("ElfHouse3", 1)
                .SetPiece("Smithy2x2", 1)
                .SetPiece("Merchant2x2", 1)
                .SetPiece("Farm2x1", 1)
                .SetPiece("Barn02_1x2", 1)
                .SetPiece("Farm2x1_3", 1)
                .SetPiece("Farm2x1_5", 1)
                .SetPiece("Farm2x2", 1)
                .SetPiece("Barn01_1x2", 1)
                .SetPiece("Farm2x1_8", 1)
                .SetPiece("CliffCaveEntry", 1)
                .SetPiece("CliffPath2", 1)
                .SetPiece("CliffRockFormation", 1)
                .SetPiece("HillCave1", 1)
                .Decoration("_mdrn_pl_wdfence", 3, DecorationContext.WallAdjacent)
                .Decoration("zep_flowers017", 3, DecorationContext.RoomCenter)
                .Decoration("zep_shrub041", 2, DecorationContext.RoomCenter)
                .Decoration("zep_bamboo002", 2, DecorationContext.RoomCenter)
                .Decoration("zep_blssmtree001", 2, DecorationContext.RoomCenter)
                .Decoration("_mdrn_pl_windmil", 1, DecorationContext.RoomCenter)
                .Decoration("zep_shrub036", 2, DecorationContext.WallAdjacent)
                .Decoration("zep_bamboo001", 1, DecorationContext.RoomCenter)
                .Decoration("zep_pinetr22", 1, DecorationContext.RoomCenter)
                .Decoration("swlor_0186", 1, DecorationContext.WallAdjacent)
                .Decoration("swlor_0212", 1, DecorationContext.WallAdjacent);

            // Medieval Rural 2 (Mountain) -- see this file's own MedievalRuralMountain const doc comment
            // above for the full inversion writeup.
            _builder.Create(MedievalRuralMountain, "Medieval Rural 2 (Mountain)")
                .Tileset("trm02")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .PaletteVariant()
                .SolidTerrainOverride("mountain")
                .PrimaryOpenTerrain("grass")
                .ExitGroup("MountainCave1")
                .ExitGroup("MountainCave2")
                .ExitGroup("MountainCave3")
                .ExitGroup("Mine1")
                .ExitGroup("Mine2")
                .ExitGroup("MountainCave4")
                .ExitGroup("CornerCave1")
                .ExitGroup("InnerCornerCave1")
                .ExitGroup("InnerCornerCave3")
                .ExitGroup("SeaCave1");
            // Sea Ships (tss13) -- see this file's own SeaShips const doc comment above for the full
            // composition writeup. The Castle terrain block is FIRST in .set group order, so it is the
            // only terrain FindGroup can ever resolve any of the 11 duplicated Boat/Lifeboat names to --
            // wired here as the base profile accordingly. maxPerArea 1 per name (11 distinct pieces is
            // already a full harbor scene's worth of variety at a 20x20 baseline).
            _builder.Create(SeaShips, "Sea Ships")
                .Tileset("tss13")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .PrimaryOpenTerrain("Castle")
                .SetPiece("Boat 1", 1)
                .SetPiece("Boat 2", 1)
                .SetPiece("Boat 3", 1)
                .SetPiece("Boat 4", 1)
                .SetPiece("Boat 5", 1)
                .SetPiece("Boat 6", 1)
                .SetPiece("Boat 7", 1)
                .SetPiece("Boat 8", 1)
                .SetPiece("Lifeboat 1", 1)
                .SetPiece("Lifeboat 2", 1)
                .SetPiece("Lifeboat 3", 1);

            // Sea Ships (City) -- recomposes the SAME tss13 .set data onto its City terrain district.
            // Declares NO SetPieces (see this file's own SeaShips const doc comment on why every
            // duplicated Boat/Lifeboat name is unreachable here through FindGroup's first-match rule) --
            // this profile exists purely to make the City terrain's own plain tiles and structurally-
            // identical Boat/Lifeboat copies count as reachable in the tile-coverage census (the same
            // "PaletteVariant... purely to close tile-coverage census exemptions and offer the palette as
            // a composable option" role TropicalSand's own doc comment documents) and to offer City as a
            // selectable open-field district in its own right.
            _builder.Create(SeaShipsCity, "Sea Ships (City)")
                .Tileset("tss13")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .PaletteVariant()
                .SolidTerrainOverride("City")
                .PrimaryOpenTerrain("City");

            // Sea Ships (Rural) -- same shape as SeaShipsCity above, recomposed onto the Rural terrain
            // district. See this file's own SeaShips const doc comment for the full duplicate-name
            // writeup on why no SetPieces are declared here.
            _builder.Create(SeaShipsRural, "Sea Ships (Rural)")
                .Tileset("tss13")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .PaletteVariant()
                .SolidTerrainOverride("Rural")
                .PrimaryOpenTerrain("Rural");

            // Sea Ships (Tropical) -- same shape as SeaShipsCity above, recomposed onto tss13's own
            // Tropical terrain district (a per-terrain palette WITHIN this tileset's own four-terrain
            // recolor -- unrelated to, and not to be confused with, this file's separate ttz01 "Tropical"
            // profile family). See this file's own SeaShips const doc comment for the full duplicate-name
            // writeup on why no SetPieces are declared here.
            _builder.Create(SeaShipsTropical, "Sea Ships (Tropical)")
                .Tileset("tss13")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .PaletteVariant()
                .SolidTerrainOverride("Tropical")
                .PrimaryOpenTerrain("Tropical");
            // Beholder Interior* (tib01, SWLOR_Haks/sw_t_beholder -- hak-only, UnlocalizedName
            // "Beholder Interior*"; a custom 868-tile/43-group expansion of vanilla's 105-tile tib01,
            // verified directly against the raw .set). Composes correctly with NO SolidTerrainOverride:
            // Default=Border=Floor="Wall" is already the solid mass, the ordinary interior shape.
            // PrimaryOpenTerrain("Room") is the base ("Lava") palette; five same-shape PaletteVariant
            // siblings below recompose the SAME hak data against RoomBlood/RoomMagic/RoomSewer/
            // RoomUrine/RoomWater -- each a genuinely separate terrain (not a district repaint of one
            // shared terrain), verified 14/16 on the Wall/<color> 16-combo matrix for every one of the
            // 6 colors, missing only the two pure-diagonal combos -- the same accepted-exemption shape
            // TileCoverageCensusTests already documents elsewhere (e.g. Underdark's alt-palette
            // exemptions). TERRAIN7/8 (RoomIce/RoomBlack) and CROSSER7/8 (CorridorIce/CorridorBlack) are
            // genuinely vestigial: 0 pure tiles, 0 crosser-edge occurrences, 0/16 (or 1/16, the lone
            // solid-only combo) on every matrix pairing -- these two declared colors were never actually
            // modeled in this hak and are excluded outright, not merely left unwired.
            //
            // DECISIVE (re-verified directly): DoorSlotCrossers("Door") + TunnelCrossers("Corridor",
            // "Door") gives this palette's real, full Custom-mode Tunnel vocabulary --
            // TunnelVocabularyCheck.SupportsTunnels(..., CorridorCrosserType.Custom, "Corridor", "Door",
            // extraDoorSlot: "Door") returns TRUE for every one of the 6 real room colors, each pairing
            // its OWN color-specific corridor BODY crosser ("Corridor"/"CorridorBlood"/"CorridorMagic"/
            // "CorridorSewer"/"CorridorUrine"/"CorridorWater") with the SAME universal "Door" PORT
            // crosser shared by every color -- confirmed via a direct 10-shape body/port breakdown
            // (straight/turn/T/X, bare and with-port, both double-port combos) all TRUE, plus the
            // open-room-to-Wall boundary junction carrying a Door edge, also TRUE for all 6 colors.
            // MinimumOpeningWidth stays the default 1 (fresh pathnode audit confirms 1, not 2).
            //
            // A genuinely UNUSED second junction family exists in the raw tile data: 18 ChultDoorway +
            // 23 ChultCorridor tile-edge occurrences (18 ungrouped Wall-solid tiles, TILE850-867, model
            // prefix "zdc04" -- a foreign/borrowed reskin, not this hak's own "tib01"/"zib01" naming),
            // with the full internal 10-shape body/port vocabulary verified TRUE in isolation. Excluded
            // here rather than wired as an alternate TunnelCrossers pairing: the open-room-to-Wall
            // BOUNDARY junction carrying a ChultDoorway edge is FALSE for every one of the 6 real room
            // colors (0/6 -- re-checked directly) -- this family only forms a closed maze inside the
            // solid Wall mass with no doorway into any playable room terrain, so it can never actually
            // connect to a generated layout regardless of which palette composes it. Documented exempt,
            // not silently unwired.
            //
            // KNOWN CALIBRATION FINDING #1 (measured directly against the real solver, not assumed from
            // the FutCity precedent): "Room - Big, <color>" (5x5, one per color including Lava)
            // classifies as a WallRoom (a chamber hanging off a Tunnel-mode corridor carved through
            // solid space), NOT an OpenSetPiece -- so SetPieceRoomCornerFloor/SetPieceRoomSupplyScaling
            // (the FutCity-style "floor the ROOM envelope" knobs, which only affect OpenSetPiece siting
            // inside already-carved room floor) have NO effect on its placement rate: measured identical
            // Big-hit rates at SetPieceRoomCornerFloor 0/5/7/8/9/10 (Complex, sizes 24-64, 150 seeds
            // each -- the floor is a pure no-op for this shape). Declaring the floor anyway is actively
            // harmful once paired with "Room - Pit/Pillar, <color>" (a 3x3, hole-shaped OpenSetPiece: a
            // plus of untouched open-Room "hole" cells around a solid, diagonal-cornered curb) -- the
            // floor is what makes Pit/Pillar's footprint reachable at all (0 hits, never even attempted,
            // at floor 0 -- the baseline Halls/Complex room ceiling is too small), and every attempt that
            // IS reached corrupts the layout: isolated bisection (Lava AND Blood, Halls AND Complex,
            // cornerFloor 7, 100 seeds each) measured 91-94% "RoomsAndCorridors layout produced
            // disconnected open space" failures with ZERO successful Pit/Pillar placements among the
            // rare survivors -- every real placement attempt disconnects the area, not merely most of
            // them. This is a genuine shape incompatibility, not a room-size tuning gap -- "Room - Pit,
            // <color>"/"Room - Pillar, <color>" (12 groups total) are excluded outright per the
            // placement-honesty convention (see trs02's own documented Chasm-district ceiling exemption
            // for the same "measured, not silently wired" shape). Neither SetPieceRoomCornerFloor nor
            // SetPieceRoomSupplyScaling is declared on ANY profile below.
            //
            // "Room - Big, Lava" is instead wired plain (no floor), budget 3 (FutCity's own "set above
            // the observed per-area site ceiling" convention): measured hit rate is low and noisy (0% at
            // this pipeline's own 20x20 gate size, rising to 4.7-9.3% of areas at 32x32-40x40, Complex
            // only, seed-base-independent -- 3 independent 150-seed sweeps at seed bases 30000/60000/
            // 95000 each landed 7-14 hits) but NEVER disconnects, and ONLY ever surfaces under Complex
            // (Tunnel-mode corridors carve the solid mass a WallRoom needs to hang off of) -- Halls
            // (OpenLane mode) measured 0/450 Big hits across every size/seed-base combination tested, the
            // same "declared but structurally unreachable under this layout style" ceiling trs02 already
            // documents, not a silent no-op.
            //
            // KNOWN CALIBRATION FINDING #2 (RESOLVED -- LayoutGroupStamper's site-search bug that
            // blocked this is now fixed; see IsCorridorTunnelBodyEdge/IsStraightTunnelBodyCell in
            // LayoutGroupStamper and the "Accept declared tunnel body crossers in placement-time site
            // searches" commit). Originally: "Room - Big, <color>"/"Door - Alcove/I/L/T/X, <color>" for
            // the FIVE SECONDARY colors (Blood/Magic/Sewer/Urine/Water) classified correctly (see the
            // Door-family classification breakdown below) and their own TunnelCrossers("Corridor<Color>", "Door")
            // Custom-mode vocabulary genuinely carved real "Corridor<Color>" tunnel-body edges, but never
            // PLACED -- IsWallRoomSiteValid's Tunnel-chain-neighbor check and
            // TryPlaceDoorwayCorridorInsert/IsValidFlankingChainCell's straight-chain search both called
            // IsStraightCorridorCell with the hardcoded literal "Corridor" constant, never consulting
            // MacroLayoutParameters.TunnelBodyCrosser -- so a WallRoom or Doorway-pair CorridorInsert
            // could never find a placement SITE on any composition whose Custom-mode body crosser wasn't
            // literally "Corridor". That gap is now closed: IsWallRoomSiteValid and
            // TryPlaceDoorwayCorridorInsert/IsValidFlankingChainCell resolve the composition's EFFECTIVE
            // body crosser (canonical "Corridor" always accepted, plus MacroLayoutParameters.
            // TunnelBodyCrosser under Custom mode), mirroring CorridorInsertCrossersFor exactly.
            //
            // POST-FIX RE-MEASUREMENT (isolated single-group probes, Complex only -- Halls stays 0
            // across every group here, confirmed directly: "Room - Big, Blood" measured 0/147 on Halls
            // at size 32, matching Lava's own documented Halls ceiling -- Halls carves no Tunnel-mode
            // corridors for a WallRoom/CorridorInsert to hang off of). Rates are byte-identical across
            // all five secondary colors -- re-measured per-color, not assumed from one flagship (seedBase
            // 95000, 150 seeds, seedStride 13, every color landed the exact same hit counts):
            //   "Room - Big, <color>": 0/150 at size 20 (still a documented ceiling, same shape as
            //     "Room - Big, Lava"'s own size-20 ceiling below), 6/150 (4.0%) at size 32, 8/150 (5.3%)
            //     at size 40 -- essentially identical to Lava's own 4.7-9.3% range. Two additional
            //     independent seed-base sweeps (30000/60000, Blood only) confirm seed-base independence:
            //     5-6/150 at size 32, 11-12/150 at size 40. Wired plain, budget 3, matching Lava's own
            //     precedent exactly.
            //   "Door - Alcove, <color>": 128/150 (85.3%) at size 20, 150/150 (100%) at size 32/40.
            //   "Door - I, <color>": 124/150 (82.7%) at size 20, 150/150 (100%) at size 32/40.
            //   "Door - L, <color>": 37/150 (24.7%) at size 20, 99/150 (66.0%) at size 32, 112/150
            //     (74.7%) at size 40.
            //   "Door - T, <color>": 3/150 (2.0%) at size 20, 8/150 (5.3%) at size 32, 18/150 (12.0%) at
            //     size 40 -- low but genuinely nonzero even at the standard size, the same "low and
            //     noisy but real" shape as "Room - Big, Lava"'s own wiring.
            //   "Door - X, <color>": 0/150 at size 20 (a documented ceiling, the rarest junction shape),
            //     but genuinely nonzero at larger sizes -- 0-1/150 at size 32 and 1-2/150 at size 40
            //     across three independent seed-base sweeps (95000/30000/60000, Blood), never zero across
            //     all three at size 40.
            //
            // Door-family classification (verified against the raw tile edge data, TILE511/512/515/517/
            // 518 for Blood): only "Door - I" (an opposite Door pair) is the Doorway-pair CorridorInsert
            // splice; "Door - Alcove/L/T/X" (one/two-adjacent/three/four Door ports, all-Wall corners)
            // classify as 1x1 WallRooms whose EVERY perimeter port must find a corridor-chain neighbor
            // at the site -- which is exactly why the measured rates fall monotonically with port count
            // (Alcove 85% > L 25% > T 2% > X 0% at size 20) while "Door - I" places near-universally by
            // splicing into any straight chain cell. Both placement paths (IsWallRoomSiteValid's
            // perimeter-neighbor check and the doorway-pair insert's flanking-chain scan) were blocked
            // by the same hardcoded literal and are both fixed. All five are 1x1 wall-embedded pieces
            // with no disconnection risk (unlike Room - Pit/Pillar's hole-shaped OpenSetPiece -- see
            // this file's own Pit/Pillar exclusion writeup above), so even Door - X's very low rate is
            // safe to wire rather than a reason to exclude it.
            //
            // All eleven groups (Room - Big plus all five Door-family shapes) are now wired as SetPieces
            // on every one of the five secondary-color profiles below -- Room - Big at budget 3 (Lava's
            // own convention), each Door-family shape at budget 1 (this file's own convention for 1x1
            // insert/gate groups, e.g. "Door - Big 1/2" above). See OpenSetPiecePlacementRateTests for the
            // placement-proof tests pinned to these measured rates.
            //
            // This fix is a shared LayoutGroupStamper change, not tib01-specific -- it also affects every
            // OTHER already-registered tileset with a renamed Custom-mode body crosser (CryptGrey's
            // "GreyCorridor", MinesAndCavernsDesert/Organic's "DesertCorridor"/"OrganicCorridor", the
            // MinesAndCavernsTracks family, CityExterior's Dock/FieldDock/GothicDock), all re-measured at
            // 145-150/150 (unchanged) when the fix landed -- their wired door-transition wall rooms
            // always had an open-boundary fallback masking the bug, so the fix is additive there, not a
            // behavior change.
            //
            // Layout support: Halls and Complex both generate this palette at ~100% success (Halls
            // 145-150/150, Complex 145-150/150 across every size/floor combination probed); ONLY Complex
            // (Tunnel mode) ever surfaces "Room - Big, Lava" (the sole SetPiece wired this pass), matching
            // every other Tunnel-gated tileset in this file.
            //
            // Decoration: exactly one hand-built tib01 area exists in the module (Module/are/
            // ziyhutdung1c.are.json, 256 tiles) -- thin evidence (n=1) but real. Lighting sampled
            // directly: (MainLight1,MainLight2,SrcLight1,SrcLight2)=(0,0,0,0) is the plurality (45.3%),
            // used as TileLighting. Placeable palette below is the subset of that area's decorations
            // that carry a real Module/utp blueprint (AllDungeonDefinitions_DecorationsExistAndAreVisible
            // requires this) -- several hand-built resrefs (plc_boulder, plc_rubble, x3_plc_rubble1-3,
            // plc_spdcocoon, x3_plc_skelmage/skelwar/skelwar2, plc_bones) have no module blueprint and
            // are omitted rather than wired blind. Corpse-pile pair (zep_cps_pile_001/002) placed as
            // RoomCenter; everything else (blood decals/stains, alien-hive growths, scattered corpses,
            // misc clutter) as WallAdjacent, weighted by the sampled occurrence counts. qionhiveslime00*/
            // _mdrn_pl_alnhve* pull from the same alien-hive family QionHiveDungeonDefinition/
            // AlienRuinDungeonDefinition already use -- the closest existing content-theme pairing for
            // this tileset (no theme change made here; this pass registers the tileset profile only).
            _builder.Create(Beholder, "Beholder Interior*")
                .Tileset("tib01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .PrimaryOpenTerrain("Room")
                .DoorSlotCrossers("Door")
                .TunnelCrossers("Corridor", "Door")
                .SetPiece("Room - Big, Lava", 3)
                .Decoration("zep_blood_004", 3, DecorationContext.WallAdjacent)
                .Decoration("zep_blood_005", 3, DecorationContext.WallAdjacent)
                .Decoration("zep_blood_006", 3, DecorationContext.WallAdjacent)
                .Decoration("qionhiveslime002", 2, DecorationContext.WallAdjacent)
                .Decoration("qionhiveslime003", 1, DecorationContext.WallAdjacent)
                .Decoration("_mdrn_pl_alnhve1", 1, DecorationContext.WallAdjacent)
                .Decoration("_mdrn_pl_alnhve2", 2, DecorationContext.WallAdjacent)
                .Decoration("zep_cps_pile_001", 1, DecorationContext.RoomCenter)
                .Decoration("zep_cps_pile_002", 1, DecorationContext.RoomCenter)
                .Decoration("_mdrn_pl_corpsh4", 1, DecorationContext.WallAdjacent)
                .Decoration("_mdrn_pl_corps02", 1, DecorationContext.WallAdjacent)
                .Decoration("_mdrn_pl_corpsh8", 1, DecorationContext.WallAdjacent)
                .Decoration("_mdrn_pl_corpsh6", 1, DecorationContext.WallAdjacent)
                .Decoration("zep_bloodstain2", 1, DecorationContext.WallAdjacent)
                .Decoration("zep_bloodstain3", 1, DecorationContext.WallAdjacent)
                .Decoration("zep_bloodstain6", 1, DecorationContext.WallAdjacent)
                .Decoration("_mdrn_pl_datapd3", 1, DecorationContext.WallAdjacent);

            // Beholder Interior* (Blood) -- PaletteVariant recomposing the SAME tib01 hak data against
            // RoomBlood/CorridorBlood (see the base Beholder profile's own doc comment above for the
            // full verification writeup shared by every color variant). "Room - Big, Blood" and all
            // five "Door - Alcove/I/L/T/X, Blood" junction groups are now wired -- see KNOWN
            // CALIBRATION FINDING #2 above for the post-fix re-measurement (LayoutGroupStamper's site-
            // search bug is fixed; this is no longer a placement-bug exemption). Budgets/rates are
            // byte-identical across all five secondary colors (re-measured per-color, not assumed) --
            // see the base profile's finding #2 writeup for the shared numbers. Decorations/lighting
            // inherit from the base Beholder profile via DungeonTilesetPaletteInheritance (no per-color
            // evidence exists).
            _builder.Create(BeholderBlood, "Beholder Interior* (Blood)")
                .Tileset("tib01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .PaletteVariant()
                .PrimaryOpenTerrain("RoomBlood")
                .DoorSlotCrossers("Door")
                .TunnelCrossers("CorridorBlood", "Door")
                .SetPiece("Room - Big, Blood", 3)
                .SetPiece("Door - Alcove, Blood", 1)
                .SetPiece("Door - I, Blood", 1)
                .SetPiece("Door - L, Blood", 1)
                .SetPiece("Door - T, Blood", 1)
                .SetPiece("Door - X, Blood", 1);

            // Beholder Interior* (Magic) -- see Blood's own doc comment immediately above; identical
            // shape against RoomMagic/CorridorMagic. "Room - Big, Magic"/"Door - Alcove/I/L/T/X, Magic"
            // are wired for the same post-fix reason (byte-identical measured rates).
            _builder.Create(BeholderMagic, "Beholder Interior* (Magic)")
                .Tileset("tib01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .PaletteVariant()
                .PrimaryOpenTerrain("RoomMagic")
                .DoorSlotCrossers("Door")
                .TunnelCrossers("CorridorMagic", "Door")
                .SetPiece("Room - Big, Magic", 3)
                .SetPiece("Door - Alcove, Magic", 1)
                .SetPiece("Door - I, Magic", 1)
                .SetPiece("Door - L, Magic", 1)
                .SetPiece("Door - T, Magic", 1)
                .SetPiece("Door - X, Magic", 1);

            // Beholder Interior* (Sewer) -- see Blood's own doc comment above; identical shape against
            // RoomSewer/CorridorSewer. "Room - Big, Sewer"/"Door - Alcove/I/L/T/X, Sewer" are wired for
            // the same post-fix reason (byte-identical measured rates).
            _builder.Create(BeholderSewer, "Beholder Interior* (Sewer)")
                .Tileset("tib01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .PaletteVariant()
                .PrimaryOpenTerrain("RoomSewer")
                .DoorSlotCrossers("Door")
                .TunnelCrossers("CorridorSewer", "Door")
                .SetPiece("Room - Big, Sewer", 3)
                .SetPiece("Door - Alcove, Sewer", 1)
                .SetPiece("Door - I, Sewer", 1)
                .SetPiece("Door - L, Sewer", 1)
                .SetPiece("Door - T, Sewer", 1)
                .SetPiece("Door - X, Sewer", 1);

            // Beholder Interior* (Urine) -- see Blood's own doc comment above; identical shape against
            // RoomUrine/CorridorUrine. "Room - Big, Urine"/"Door - Alcove/I/L/T/X, Urine" are wired for
            // the same post-fix reason (byte-identical measured rates).
            _builder.Create(BeholderUrine, "Beholder Interior* (Urine)")
                .Tileset("tib01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .PaletteVariant()
                .PrimaryOpenTerrain("RoomUrine")
                .DoorSlotCrossers("Door")
                .TunnelCrossers("CorridorUrine", "Door")
                .SetPiece("Room - Big, Urine", 3)
                .SetPiece("Door - Alcove, Urine", 1)
                .SetPiece("Door - I, Urine", 1)
                .SetPiece("Door - L, Urine", 1)
                .SetPiece("Door - T, Urine", 1)
                .SetPiece("Door - X, Urine", 1);

            // Beholder Interior* (Water) -- see Blood's own doc comment above; identical shape against
            // RoomWater/CorridorWater. "Room - Big, Water"/"Door - Alcove/I/L/T/X, Water" are wired for
            // the same post-fix reason (byte-identical measured rates).
            _builder.Create(BeholderWater, "Beholder Interior* (Water)")
                .Tileset("tib01")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .PaletteVariant()
                .PrimaryOpenTerrain("RoomWater")
                .DoorSlotCrossers("Door")
                .TunnelCrossers("CorridorWater", "Door")
                .SetPiece("Room - Big, Water", 3)
                .SetPiece("Door - Alcove, Water", 1)
                .SetPiece("Door - I, Water", 1)
                .SetPiece("Door - L, Water", 1)
                .SetPiece("Door - T, Water", 1)
                .SetPiece("Door - X, Water", 1);
            // Medieval City 2 (tcm02) -- see this file's own MedievalCity const doc comment above for
            // the full composition writeup (Solid=Water/Open=Cobble canal city, Bridge tunnel crosser,
            // Building/Wall/Stream/Road/Rock/path exemption accounting).
            _builder.Create(MedievalCity, "Medieval City 2")
                .Tileset("tcm02")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .SolidTerrainOverride("Water")
                .TunnelCrossers("Bridge", "Bridge")
                .FeatureTile("Cobbles_Grasspatch")
                .FeatureTile("Cobbles_Puddle")
                .FeatureTile("Cobbles_Rock")
                .FeatureTile("Cobbles_uneven")
                .FeatureTile("Cobbles_missing")
                .FeatureTile("Fountain1")
                .FeatureTile("Fountain2")
                .FeatureTile("Tree1")
                .FeatureTile("Tree2")
                .FeatureTile("Destroyed01")
                .FeatureTile("Destroyed02")
                .FeatureTile("SewerEntrance01")
                .FeatureTile("Boat1")
                .FeatureTile("Boat2")
                .FeatureTile("InnerCornerEmpty1")
                .FeatureTile("InnerCornerEmpty2")
                .FeatureTile("OuterCornerEmpty1")
                .FeatureTile("OuterCornerEmpty2")
                .FeatureTile("OuterCornerEmpty3")
                .FeatureTile("StraightEmpty1")
                .FeatureTile("StraightEmpty2")
                .FeatureTile("DoubleCornerEmpty1")
                .ExitGroup("House1_1x1")
                .ExitGroup("House2_1x1")
                .ExitGroup("House3_1x1")
                .ExitGroup("House4_1x1")
                .ExitGroup("House5_1x1")
                .ExitGroup("House8")
                .ExitGroup("House9")
                .ExitGroup("House10")
                .ExitGroup("Watertower")
                .ExitGroup("BuildingBad1")
                .ExitGroup("Lighthouse")
                .ExitGroup("CastleSmallDoor2")
                .ExitGroup("CastleHugeGateGrass")
                .ExitGroup("CliffBottomCave1")
                .ExitGroup("CliffBottomCave2")
                .ExitGroup("CliffTopCave1")
                .ExitGroup("SewerEntrance03")
                .ExitGroup("SewerEntrance04")
                .ExitGroup("Shop1")
                .ExitGroup("Shop2")
                .ExitGroup("Bakery")
                .ExitGroup("Museum")
                .ExitGroup("PatriciansHouse")
                .ExitGroup("Smithy")
                .ExitGroup("StairHouse")
                .ExitGroup("CornerShop1")
                .ExitGroup("CornerShop2")
                .ExitGroup("CornerPub")
                .ExitGroup("BurntHouse1")
                .ExitGroup("BurntHouse2")
                .ExitGroup("CornerBTower1")
                .ExitGroup("CornerBTower2a")
                .SetPiece("House1_2x2")
                .SetPiece("House1_1x2")
                .SetPiece("Inn_2x3")
                .SetPiece("Inn2_2x3")
                .SetPiece("Temple1_2x2")
                .SetPiece("Plaza1")
                .SetPiece("Arena", 1)
                .SetPiece("Dolphins")
                .SetPiece("CityWatch")
                .SetPiece("House2x1")
                .SetPiece("Destroyed03")
                .SetPiece("ParkL2x2")
                .SetPiece("Docks_City")
                .SetPiece("DockedShip_City", 1)
                .SetPiece("City_boat_docked")
                .SetPiece("Ship_3x1_Docked", 1)
                .SetPiece("Docks_Crane")
                .SetPiece("Jetty")
                .SetPiece("CityBoat2")
                .SetPiece("SewerEntrance02")
                .SetPiece("Temple3x3", 1)
                .SetPiece("ShipNotSailing1", 1)
                .SetPiece("ShipNotSailing2", 1)
                .SetPiece("Ship_floating_1", 1)
                .SetPiece("Ship_floating_2", 1)
                .Decoration("_mdrn_pl_wdfence", 3, DecorationContext.WallAdjacent)
                .Decoration("swd_floorm01", 2, DecorationContext.CourtyardCenter)
                .Decoration("zep_bamboo001", 2, DecorationContext.CorridorSide)
                .Decoration("zep_shrub041", 2, DecorationContext.WallAdjacent)
                .Decoration("zep_shrub036", 1, DecorationContext.CorridorSide)
                .Decoration("_mdrn_pl_strtlm4", 2, DecorationContext.DoorwayFlank)
                .Decoration("_mdrn_pl_flowpp1", 1, DecorationContext.DoorwayFlank)
                .Decoration("_mdrn_pl_buildg7", 1, DecorationContext.StructureAdjacent)
                .Decoration("_mdrn_pl_df_kios", 1, DecorationContext.StructureAdjacent)
                .Decoration("frn_bench_swlr02", 1, DecorationContext.Courtyard)
                .Decoration("zep_log001", 1, DecorationContext.CorridorSide);

            // Medieval City 2's Chasm/cliff sub-family -- see this file's own MedievalCityCliffs const
            // doc comment above for the full writeup (SolidTerrainOverride("Chasm") +
            // PrimaryOpenTerrain("Grass"), MinimumOpeningWidth(2), TunnelCrossers("Bridge","Bridge")
            // independently reverified against Chasm).
            _builder.Create(MedievalCityCliffs, "Medieval City 2 (Cliffs)")
                .Tileset("tcm02")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .PaletteVariant()
                .SolidTerrainOverride("Chasm")
                .PrimaryOpenTerrain("Grass")
                .MinimumOpeningWidth(2)
                .TunnelCrossers("Bridge", "Bridge")
                // HillCave1 (a raised, ReliefPiece-classified 1x1 group) measured 0/150 under BOTH
                // Halls and Complex with MaxReliefRegions left at the default 0: DungeonComposition.
                // BuildLayoutParameters clamps every ReliefRegions request down to this cap, so
                // LayoutReliefPainter never actually paints the specific corner/height field the piece
                // needs regardless of layout style. MaxReliefRegions(2) lets the composition request
                // real relief painting, closing the gap -- see
                // MedievalCityCliffsHillCave_PlacesOnceReliefIsRequested.
                .MaxReliefRegions(2)
                .FeatureTile("ChasmPond")
                // Also wired on the base profile (where they measure a documented 0% ceiling --
                // Chasm/Grass corners never paint under Water/Cobble); HERE the Chasm/Grass pair is
                // this variant's own Solid/Open boundary, so GroupExitPlanner finds real sites -- see
                // MedievalCityCliffsCaveDoorGroups_PlaceAsGroupExits.
                .ExitGroup("CliffBottomCave1")
                .ExitGroup("CliffBottomCave2")
                .ExitGroup("CliffTopCave1")
                .SetPiece("CliffPath2", 1)
                .SetPiece("CliffCaveEntry", 1)
                .SetPiece("CliffRockFormation", 1)
                .SetPiece("HillCave1", 1)
                .SetPiece("ChasmBridgeWB1", 1)
                .SetPiece("ChasmBridgeWB2", 1)
                .SetPiece("ChasmBridgeWB3", 1)
                .SetPiece("ChasmBridgeWB4", 1)
                .SetPiece("ChasmBridgeWB5", 1);

            // Medieval City 2's Castle garrison sub-family -- see this file's own MedievalCityCastle
            // const doc comment above for the full writeup (SolidTerrainOverride("Castle"),
            // MinimumOpeningWidth(2), moving CastleSmallDoor/CastleHugeGate/CastleTowerGate1-2/
            // PrisonTower off the base profile's documented-0%-ceiling ExitGroups onto a composition
            // where Castle is a real wall material).
            _builder.Create(MedievalCityCastle, "Medieval City 2 (Castle)")
                .Tileset("tcm02")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .PaletteVariant()
                .SolidTerrainOverride("Castle")
                .MinimumOpeningWidth(2)
                .ExitGroup("CastleSmallDoor")
                .ExitGroup("CastleHugeGate")
                .ExitGroup("CastleTowerGate1")
                .ExitGroup("CastleTowerGate2")
                .ExitGroup("PrisonTower");

            return _builder.Build();
        }
    }
}
