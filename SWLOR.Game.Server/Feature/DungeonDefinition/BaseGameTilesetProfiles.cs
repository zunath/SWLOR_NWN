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

        // Wave-5: Jacoby's Jungle (jac01, SWLOR_Haks/sw_t_jungle -- a 380-tile HasHeightTransition=1
        // hak-shipped exterior tileset). See the Jungle profile's own doc comment below for the full
        // probe writeup (a lean sibling of Forest/ttf01: same degenerate Default==Floor=="Forest"
        // GENERAL quirk, same inverted SolidTerrainOverride("Cliff")/PrimaryOpenTerrain("Forest")
        // composition, and a near-identical group-naming vocabulary, but only 7 terrains/5 crossers
        // against ttf01's 11/13 -- no RuralTrees/RuralWater/GoodCastle/EvilCastle/Marsh/CityWall/
        // MossWall/RuinWall/RuralWallOne/Two/StoneBridge/DlaEdgeFix districts at all).
        public const string Jungle = "jungle";
        public const string JunglePlatform = "jungle_platform";

        // Wave-7: Rural Grass (ttr01, SWLOR_Haks/sw_t_rural -- a 653-tile HasHeightTransition=1
        // hak-shipped exterior tileset, UnlocalizedName "Rural Grass*"). Same degenerate GENERAL quirk
        // as ttd01/ttf01/jac01 (Default=Floor=Border="Grass", the walkable ground), but UNLIKE every
        // prior exterior wave, ttr01 has no Cliff-equivalent wall mass at all: Grass reaches full
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

        // Wave-4: D20 Futuristic City SW (fcx01, SWLOR_Haks/sw_t_futcity -- a 239-tile hak-shipped
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
        // ttf01/ttf02 wave (locked in by TunnelVocabularyCheckTests.ExpectedUnsupported). "murs" is
        // declared via DoorSlotCrossers("murs") -- it carries real door slots on the wall/road-gate
        // GROUPS (b_wall_door/d_wall_door/b_road_door/d_road_door) and on ten flat, ungrouped,
        // murs-edged ordinary tiles (ry TILE223-232), the same "district's own body-renamed door
        // crosser" shape as Barrows' "door_corridor" precedent. "pont" (Bridge-equivalent, gates the
        // holes chasm at TILE5-7/96-98/119-124) has no wired body/port or DoorSlotCrossers vocabulary in
        // this wave -- see TileCoverageCensusTests' fcx01 PilotExpectedExemptions entries for the exact
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
        // 144 tiles (the confirmed-fcx01 reference area named for this onboarding); the two areas with
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

        // Wave-5 (onboarding wave 1 of 2): four hak tilesets probed via a throwaway NUnit harness
        // (ZZOnboardingProbe/ZZOnboardingProbe2, deleted after this pass -- their output is reproduced
        // in each profile's own doc comment below) rather than the interactive toolset. All four are
        // Interior=true, share the ordinary Default=Wall/Floor=<primary> GENERAL split (no
        // SolidTerrainOverride inversion needed, unlike the ttd01/ttf01/fcx01 exterior wave), and all
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
        // profile in the prior wave. "bridge" (gates the lava chasm, e.g. TILE112/TILE15) and "fence"
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
        // (time-boxed out of onboarding wave 1) -- generated tjsb0 content stays on accent-only/no
        // tileset-keyed palette until a follow-up pass mines the 8 real areas' placeable inventories.
        public const string SecretBase = "secretbase";

        // D20 Modern Facility (tbx78, SWLOR_Haks/sw_t_facility, 84 tiles -- the smallest of the four):
        // Wall/facility, two terrains only. Crossers: corridor, doorway1/2/3, cell, raised -- NONE of
        // the three doorway variants is the literal canonical string "Doorway", so every door-bearing
        // tile (52 of 84) needs DoorSlotCrossers to be recognized at all, unlike tjsb0's case-insensitive
        // match. Verified directly: TunnelVocabularyCheck.SupportsTunnels tried against every
        // body=corridor/port={doorway1,doorway2,doorway3} Custom pairing returns FALSE for all three (the
        // T-with-port/X-with-port shapes never resolve) -- Tunnel mode downgrades to OpenLane, the same
        // verdict as the ttd01/ttf01/fcx01 wave. DoorSlotCrossers("doorway1","doorway2","doorway3",
        // "cell","raised") is declared so CornerEdgeResolver/LayoutGroupStamper recognize all five
        // non-canonical door-implying crossers (cell gates the facility's holding-cell tiles TILE36/38/
        // 40; raised gates TILE48/50's ramp doors). SetPieceRoomCornerFloor(7): the largest group is
        // room3x1 (3x1, max dimension 3), matching FutCity's 3x3+/4x3 rule (corner size 7, the
        // machinery's own vanilla ceiling). Group-name quirk: three separate GROUP entries are all
        // literally named "room2x1" (footprints 1x2, 2x1, 2x1) -- wired once via SetPiece("room2x1"),
        // matched by name against all three real .set entries. ExitGroup("door_transition"): a 1x1
        // group, the same "*_transition"/"*Trans"/"Door_Trans" naming convention this entire onboarding
        // wave's tilesets share for their literal area-boundary marker group. Hand-built evidence: 8 real
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
        // most heavily districted of the four). KNOWN QUIRK (per this onboarding wave's brief): the raw
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
        // "*Trans"/"*Exit" group exists in this .set, unlike the other three tilesets in this wave), so
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
        // the Wave-5 doc comment above descoped (Service/Tiled/Office_Wood/Office_Alum/Foyer_L/Foyer_U)
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

        // Wave-6 (final CEP superset wave, 1 of 2): [CEP] Dungeon (zde01, SWLOR_Haks/sw_t_cepdungeon).
        // zde01.set is BYTE-IDENTICAL to the already-onboarded SWLOR hak copy of tde01 (SWLOR_Haks/
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
        // uncalibrated placeholder per this file's own pilot-wave doc comment above), zde01 ships with
        // real hand-built content: Module/are/dath_mountcaves.are.json (126 tiles) and
        // Module/are/valkorrdung1c.are.json (256 tiles), 382 placed tiles total. The measured plurality
        // across both is (MainLight1,MainLight2,SrcLight1,SrcLight2)=(0,0,0,0) at 31.2% (119/382) --
        // ahead of (0,0,2,2)/(0,11,0,0) at 10.7% each -- so this wave uses that real sampled default
        // instead of copying tde01's placeholder value.
        // Display names: UnlocalizedName verbatim ("[CEP] Dungeon"), variants cascade
        // "[CEP] Dungeon (<Qualifier>)" -- see this wave's own naming instruction, distinct from the
        // pilot wave's "Dungeon*" asterisk convention above.
        public const string CepDungeon = "cep_dungeon";
        public const string CepDungeonWater = "cep_dungeon_water";
        public const string CepDungeonSewer = "cep_dungeon_sewer";
        public const string CepDungeonIce = "cep_dungeon_ice";
        public const string CepDungeonPit = "cep_dungeon_pit";

        // Wave-6 (final CEP superset wave, 2 of 2): [CEP] City Interior 1 (zin01, SWLOR_Haks/
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
            _builder.Create(Crypt, "Crypts*")
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
            // missing-Doorway gap, verified green in OnboardedTilesetPipelineTests. "[Dwarven] Cave
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
            // and this wave's verbatim display-name convention.
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
            // the census run (see the wave's own follow-up note if it stays exempt).
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
            // left for a future wave that either extends multi-district support or ships dedicated
            // profiles per palette -- see TileCoverageCensusTests.PilotAlternateVocabTerrains["tdm01"].
            // AccentTerrain("Water") is the one wired accent channel of [Cave]'s Water/Pit/Lava/Ice
            // quartet (mirrors Dungeon/tde01's single-accent-slot precedent); "[Cave] Door - Bridge,
            // Pit"/"Lava" are the same shape on the two unwired accents and excluded. "[Cave] Ramp" and
            // "[Cave] Cave Entrance" are both wired via LayoutGroupStamper's ReliefPiece kind (see the
            // SetPiece entries below) -- ReliefPiece now tolerates a door slot the same way WallAlcove/
            // OpenSetPiece/WallRoom already do (never spawns a door object), which closes "Cave
            // Entrance"'s raised-rim-plus-doorframe shape (round-4 exterior-tail-closure generalization;
            // see LayoutGroupStamper.TryClassifyReliefPiece's own doc comment). "[Cave] Door -
            // Transition", "[Cave] Ship - Docked", "[Cave] Docks (1x2)" don't structurally classify
            // under any current mechanism and are excluded.
            _builder.Create(MinesAndCaverns, "Mines and Caverns*")
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
            // composition" constraint LayoutTunnelCarver enforces), left for a future wave.
            // "[Desert] Ramp" and "[Desert] Cave Entrance" are both non-flat and wired via
            // LayoutGroupStamper's ReliefPiece kind (matching [Cave]'s own Ramp/Cave Entrance pieces
            // -- ReliefPiece now tolerates Cave Entrance's door slot, see the base profile's own
            // comment). Every other Desert group (Platforms, Pillar, Stairs 2x2, Treasure,
            // Crystal Casket/Column/Crypt, Chessboard, Portal, Mineshaft, Wall Section, Exit 1/2/3)
            // mirrors [Cave]'s own wired set piece/feature-tile/exit-group shapes tile-for-tile.
            // IsPaletteVariant() excludes this profile from --matrix's full cross-product (see
            // SWLOR.ProcgenReview/Program.cs) -- it gets one showcase area instead. [Organic] and [City]
            // remain unwired (left for a future wave; [Organic] mirrors [Desert]'s shape closely but
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
            // edge crosser -- outside this pilot's wired vocabulary, the same exclusion as every other
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
            _builder.Create(Desert, "[SW] Tatooine")
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
                // Baked-mesh cave-mouth piece (1x1 GROUP, non-flat [Desert 1,1,0,0], crosser-free, one
                // door slot) -- same ReliefPiece kind as "Ramp" above, now door-tolerant (round-4
                // exterior-tail-closure generalization -- shares tdm01 Cave Entrance's exact shape, see
                // LayoutGroupStamper.TryClassifyReliefPiece's own doc comment). Distinct from the
                // ExitGroup("CaveEntrance") flat door-tile family below.
                .SetPiece("SmallCave", 1)
                .ExitGroup("Exit")
                .ExitGroup("CliffStairs")
                .ExitGroup("ChasmStairs")
                .ExitGroup("CaveEntrance");

            // Desert's own bulk palette — mined from ttd01 hand-built reference areas
            // (decoration_evidence/evidence_by_tileset.json['ttd01'], 49 areas — the richest sample of
            // any onboarded family). Strongest co-occurrence pairs among the desert-scrub family
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

            // Desert (Road) -- ttd01's second raised-lane crosser family (round 3 of exterior tail
            // closure; see the ttf01 wave-level comment below for the shared "one RampCrosser slot
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
            // engineered further (round-4 exterior-tail-closure work). "Cave" (raised AND door-bearing,
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
                // door slot) -- same ReliefPiece kind as "Ramp" above, now door-tolerant (round-4
                // exterior-tail-closure generalization -- shares tdm01 Cave Entrance's exact shape).
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

            // Forest raised-lane crosser families (round 3 of exterior tail closure) -- ttf01's base
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
            //     now CLOSED (round-4 exterior-tail-closure): LayoutGroupStamper.TryClassifyReliefPiece
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
            // tiles. Verified directly via probe. Also wires the family's 1x1 raised GROUPS (round-4
            // exterior-tail-closure): "Ramp - City Wall" (doorless), and the door-bearing "Wall -
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
            // total MossWall-edged tiles. Also wires the family's 1x1 raised GROUPS (round-4
            // exterior-tail-closure): "Ramp - Moss Wall" (doorless) and the door-bearing "Wall -
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
                .SolidTerrainOverride("holes")
                .PrimaryOpenTerrain("Cobble")
                // PathNodeOpeningWidthAudit (fresh against fcx01's real pathnode data, Solid=holes/
                // Open=Cobble) computes 2, not the default 1 -- locked in by OnboardedTilesetPipelineTests.
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
                // ... vocabulary in this wave", see this profile's own header comment) before this pass.
                .RoadCrosser("Routes")
                .Placeholder("gen_placeholder1")
                .TileLighting(0, 0, 0, 0)
                .FeatureTile("b_arbre")
                .FeatureTile("b_arbre2")
                .FeatureTile("b_herbe")
                .FeatureTile("b_fountain")
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
                // Re-measured at size 32 (_scratch_decor/DecorGen, 20 seeds, before/after budget-only
                // sweep): raising every group's budget substantially (Tower00 3->6, Tower02/03 1->5,
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
                // derivation and _scratch_decor/tilecomp_m1_before32.json /
                // tilecomp_m1_after32_final.json for the raw measurements this comment summarizes.
                .SetPieceRoomSupplyScaling()
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
            // Promenade"): streetlights, holo-sign kiosks, planters/fences, parked speeders — this is
            // the fix for the reported "Alien Ruin content dressed with Alien Ruin's own palette
            // regardless of the Futuristic City tileset it was actually generated on" bug. Strongest
            // structural pairing: a holo kiosk lit by a nearby streetlight -> vignette.
            //
            // CorridorSide additionally doubles as this family's "street-side" bucket: LayoutRoadCarver
            // (post-road-carving pass) makes DungeonDecorationPlanner route any wall-eligible tile
            // within one cell of a carved Routes lane into CorridorSide regardless of the owning room's
            // shape (see DungeonDecorationPlanner.IsRoadAdjacent), matching pw_ar_narpromena's own
            // pattern of streetlights and holo kiosks strung along its streets rather than confined to
            // corridor-shaped rooms or doorways. _mdrn_pl_lights3/swd_streel01 (streetlight-class) and
            // swd2_kiosk004 (kiosk-class) are additionally curated here alongside their existing
            // WallAdjacent/DoorwayFlank entries so road-anchored placements draw real street furniture,
            // not just the crosswalk decal.
            _builder
                // Sign panels / barrier fences relocated out of WallAdjacent (July 2026 city-density
                // pass): generated WallAdjacent anchors against ANY room boundary -- usually a
                // knee-high divider on fcx01 -- where a free-standing holo sign board reads as junk.
                // Hand-built fcx01 measurement says these are street furniture, not divider dressing:
                // swd_build007 31% road-adjacent / 2% building-adjacent, swd2_fence004 46% / 22%,
                // swd2_fence010 46% / 0% (n=103/126/24), so all three now live in CorridorSide (the
                // road-lining bucket -- see DungeonDecorationPlanner.IsRoadAdjacent). They were NOT
                // moved to StructureAdjacent: the measured building adjacency above shows hand-built
                // builders do not hang these on tower frontages either.
                .Decoration("swd_build007", 3, DecorationContext.CorridorSide)
                .Decoration("swd2_fence004", 2, DecorationContext.CorridorSide)
                .Decoration("swd2_fence010", 2, DecorationContext.CorridorSide)
                .Decoration("swd_trash01", 2, DecorationContext.WallAdjacent)
                .Decoration("_mdrn_pl_lights3", 3, DecorationContext.WallAdjacent)
                // StructureAdjacent (building-frontage) bucket -- the items hand-built fcx01 actually
                // anchors against stamped tower/building footprints (Chebyshev<=1 building adjacency,
                // n>=51 each): _mdrn_pl_lamp4 52% building-adjacent AND 100% road-adjacent (a
                // street-facing building lamp), _mdrn_pl_bldlit 41%/95% (building-mounted light),
                // swd_conta003 51% (container stacks against frontage walls), _mdrn_pl_df_chb 100%
                // (debris chunks at building bases). Weights follow those measured adjacency rates.
                // Entries here place ONLY within 1 tile of a stamped OpenSetPiece footprint (see
                // DungeonDecorationPlanner.IsStructureAdjacent) -- never free-standing.
                .Decoration("_mdrn_pl_lamp4", 3, DecorationContext.StructureAdjacent)
                .Decoration("_mdrn_pl_bldlit", 3, DecorationContext.StructureAdjacent)
                .Decoration("swd_conta003", 2, DecorationContext.StructureAdjacent)
                .Decoration("_mdrn_pl_df_chb", 2, DecorationContext.StructureAdjacent)
                .Decoration("swd2_vehi006", 1, DecorationContext.RoomCenter)
                .Decoration("swd2_vehi003", 1, DecorationContext.RoomCenter)
                .Decoration("swd2_vehi007", 1, DecorationContext.RoomCenter)
                .Decoration("swd2_kiosk004", 2, DecorationContext.DoorwayFlank)
                .Decoration("swd_streel01", 2, DecorationContext.DoorwayFlank)
                .Decoration("_mdrn_pl_crswlk", 1, DecorationContext.CorridorSide)
                .Decoration("_mdrn_pl_lights3", 3, DecorationContext.CorridorSide)
                .Decoration("swd_streel01", 2, DecorationContext.CorridorSide)
                .Decoration("swd2_kiosk004", 2, DecorationContext.CorridorSide)
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
                .Decoration("swd_floorm01", 3, DecorationContext.CourtyardCenter)
                .Decoration("_mdrn_pl_lghtflr", 2, DecorationContext.CourtyardCenter)
                .Decoration("_mdrn_pl_floor27", 2, DecorationContext.CourtyardCenter)
                .Decoration("_mdrn_pl_lghtpl3", 3, DecorationContext.Courtyard)
                .Decoration("_mdrn_pl_conta36", 3, DecorationContext.Courtyard)
                .Decoration("_mdrn_pl_pillr04", 2, DecorationContext.Courtyard)
                .Decoration("_mdrn_pl_barr001", 2, DecorationContext.Courtyard)
                .Decoration("_mdrn_pl_crate08", 2, DecorationContext.Courtyard)
                // _mdrn_pl_busshel (bus shelter, 19 interior occurrences) was measured into this
                // bucket too but is EXCLUDED: its appearance row (7038) has a blank ModelName and
                // renders invisible (caught by AllDungeonDefinitions_DecorationsExistAndAreVisible).
                .Decoration("swd2_kiosk004", 1, DecorationContext.Courtyard)
                .Vignette("PromenadeKioskLight", 3)
                .VignetteMember("swd2_kiosk004", 0f, 0f)
                .VignetteMember("_mdrn_pl_lights3", 0.7f, 0.5f);

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
                .FeatureTile("d_herbe")
                .FeatureTile("d_eau")
                // Tower04/d_build02 (2x2) are the Cobble2 district's only groups that fit size-20-24
                // rooms -- same site-limited ceiling reasoning as FutCity's Tower00 budget above.
                //
                // Re-measured at size 32 (_scratch_decor/DecorGen, 20 seeds against the Complex layout
                // pairing): a budget-only sweep was tried here too and reverted for the identical reason
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

            // D20 Secret Base (tjsb0) -- see this file's own Wave-5 doc comment (SecretBase) for the full
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

            // D20 Modern Facility (tbx78) -- see this file's own Wave-5 doc comment (Facility) for the
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
            // UnlocalizedName typo. See this file's own Wave-5 doc comment (LabStorage) for the full
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

            // D20 Office Interiors UDP (udp2) -- see this file's own Wave-5 doc comment
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
                // node -- locked in by OnboardedTilesetPipelineTests.MinimumOpeningWidth_MatchesFreshPathNodeAudit.
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
            // the same verdict as every prior exterior wave (ttd01/ttf01/jac01).
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

            return _builder.Build();
        }
    }
}
