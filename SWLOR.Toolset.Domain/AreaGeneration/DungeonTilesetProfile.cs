#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Toolset.Domain.AreaGeneration.Atmosphere;
using SWLOR.Toolset.Domain.AreaGeneration.Decoration;
using SWLOR.Toolset.Domain.AreaGeneration.Frontage;

namespace SWLOR.Toolset.Domain.AreaGeneration
{
    /// <summary>
    /// Everything that is genuinely bound to a tileset: the tileset itself, the placeholder
    /// area cloned for instances, tile lighting, and what the tileset calls its accent terrain
    /// (water pools, pit channels). Layout shapes and content packages compose with any profile.
    /// </summary>
    public class DungeonTilesetProfile
    {
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string TilesetResref { get; set; } = string.Empty;
        public string PlaceholderResref { get; set; } = string.Empty;
        public DungeonTileLighting Lighting { get; set; } = new();

        /// <summary>
        /// Terrain name this tileset uses for accent patches (e.g. "Water" on tdt01, "Pit" on
        /// tds01). Empty = the tileset has no verified accent coverage; compositions skip accents.
        /// </summary>
        public string AccentTerrain { get; set; } = string.Empty;

        /// <summary>
        /// Terrain name this tileset uses for its accent CHANNEL/bank vocabulary (LayoutAccentChannelCarver)
        /// when it differs from the blob-patch AccentTerrain — e.g. vmr01's "Chasm", which has verified
        /// bank/span tile coverage against its primary open terrain (Plaza) but no verified blob-patch
        /// coverage, so AccentTerrain stays empty while this is set. Empty = channels fall back to
        /// AccentTerrain (the original, single-terrain behavior).
        /// </summary>
        public string ChannelTerrain { get; set; } = string.Empty;

        /// <summary>
        /// Narrowest corridor/door-gap width (in corners) this tileset can path through. Some
        /// tilesets (zsf01) give every partially-open corner combo a movement-restricted pathnode,
        /// so 1-wide openings fail the engine's path check; 2-wide openings put a fully-open
        /// (pathnode A) tile in the middle. Compositions raise CorridorWidth to at least this.
        /// </summary>
        public int MinimumOpeningWidth { get; set; } = 1;

        /// <summary>
        /// Room-size floor (in corners) this tileset's configured multi-tile OpenSetPiece groups need
        /// to ever stamp: LayoutGroupStamper.IsOpenSetPieceSiteValid requires a group's footprint PLUS
        /// a 1-cell margin ring PLUS at least one spare center-relocation tile all inside ONE room, so
        /// an NxM-tile group needs a room strictly larger than (N+2)x(M+2) tiles -- corner size 6+ for
        /// a 2x2 group, 7+ for a 3x3 (see OpenSetPiecePlacementRateTests' documented Complex room-size
        /// ceiling, which this knob is the anticipated "room-size-aware layout knob" fix for).
        /// Compositions raise the layout profile's MaxRoomCornerSize to at least this, mirroring
        /// CorridorWidth/MinimumOpeningWidth's own "layout expresses intent, tileset declares physical
        /// need" shape. 0 (default) = no floor; the layout profile's own room sizes stand unchanged.
        /// Only meaningful alongside configured SetPieces; declare it on tilesets whose visual identity
        /// depends on stamped structures standing in open districts (e.g. fcx01's city towers --
        /// hand-built fcx01 areas' group-tile share is ~15% of the area, all of it multi-tile
        /// building/platform footprints that need plaza-sized rooms to exist at all).
        /// </summary>
        public int SetPieceRoomCornerFloor { get; set; }

        /// <summary>
        /// True for a tileset whose visual identity depends on MANY stamped OpenSetPiece structures
        /// per area (e.g. fcx01's city districts, where hand-built areas measure ~0.15 group-tile
        /// share -- dozens of multi-tile buildings each): compositions additionally scale the layout
        /// profile's ROOM COUNTS with area so larger areas actually carry proportionally more
        /// stampable rooms (see LayoutParameterConstraints.ApplySetPieceRoomSupplyScaling). Without
        /// this, room supply is flat in area size -- Halls/Complex hardcode MinRooms/MaxRooms and
        /// PackedRooms caps its reported room list at MaxRooms, so a 32x32 area hosts the same ~8
        /// stampable rooms as a 20x20 and group-tile share collapses as areas grow (measured flat
        /// 0.039-0.040 at 32x32 on fcx01 regardless of SetPiece budgets, vs 0.15 hand-built).
        ///
        /// Deliberately a SEPARATE declaration from SetPieceRoomCornerFloor: four interior profiles
        /// (secretbase/modernfacility/labstorage/officeinteriors) declare the corner floor for their
        /// occasional 2x2 room-groups but are NOT set-piece-heavy -- their layouts must stay
        /// byte-identical (see RoomSupplyScalingIsolationTests), so the floor alone must never
        /// trigger room-count scaling. Only meaningful alongside configured SetPieces and a declared
        /// SetPieceRoomCornerFloor; inert below the 20x20 tuning baseline either way.
        /// </summary>
        public bool SetPieceRoomSupplyScaling { get; set; }

        /// <summary>
        /// True for a tileset whose hand-built city references assemble stamped buildings into
        /// CONTIGUOUS blocks -- multiple tower/platform groups sharing footprint edges so streets read
        /// as canyons walled by building mass, not isolated towers on an open field. Hand-built
        /// tile-built fcx01 city areas (ns_comrcial_ka, pw_ar_nsshipyard, vrotrnsslums,
        /// narshadaar_promi) measure building blocks of 24-48 contiguous tiles (several adjoined
        /// groups; single largest group footprint is 36) at 0.17-0.28 building-tile share, dominated
        /// by same-group self-tiling (Tower06 beside Tower06, d_platform2 tiled into mega-platforms)
        /// with corner-label agreement at every seam.
        ///
        /// When declared, LayoutGroupStamper's OpenSetPiece placement (a) accepts a site whose margin
        /// ring touches an already-stamped OpenSetPiece footprint, PROVIDED every shared corner label
        /// and edge crosser the new stamp would write agrees with what the earlier stamp already wrote
        /// (so seams are only ever formed between visually compatible faces), (b) prefers sites that
        /// front a carved road AND adjoin an existing building (canyon walls along streets) over
        /// road-only over building-only over free-standing, (c) re-verifies that consuming the site
        /// does not split the room's remaining open tiles into more disconnected pieces than before,
        /// and (d) doubles the road-scaled SetPiece attempt budget (adjacency unlocks sites the
        /// isolated-margin rule physically could not host). Default false -- every composition that
        /// never declares it keeps byte-identical layouts (RoomSupplyScalingIsolationTests).
        /// Only meaningful alongside configured SetPieces, and -- like SetPieceRoomSupplyScaling and
        /// the road-scaled budget -- inert at or below the 20x20 tuning baseline
        /// (LayoutParameterConstraints.RoomSupplyBaselineTiles): baseline-size compositions keep the
        /// pre-mechanism placement byte-for-byte, because both the per-tileset budgets and the urban
        /// dressing-density gates are tuned against 20x20 evidence and block assembly there starved
        /// the street-margin dressing pools below the hand-built density band.
        /// </summary>
        public bool BuildingBlockContiguity { get; set; }

        /// <summary>
        /// When true, LayoutRoadCarver routes street lanes and connector spurs as SHORTEST, then
        /// FEWEST-TURNS paths -- straight avenues with single L-corners, the hand-built city
        /// street shape -- instead of the legacy first-found breadth-first path, whose expansion
        /// order produced diagonal staircase zigzags across open plazas (measured turn-tile share
        /// 16-29% of road cells on delivered city areas vs the hand-built city band's 0-15%,
        /// r17_road_audit.py). Path lengths (and therefore road-share bands) are identical; only
        /// lane geometry changes. Default false -- every composition that never declares it keeps
        /// byte-identical lane geometry.
        /// </summary>
        public bool StraightStreetRouting { get; set; }

        /// <summary>
        /// Edge-crosser name this tileset's road/route-marking tile family carves (e.g. fcx01's
        /// "Routes") -- see LayoutRoadCarver/RoadVocabularyCheck. Unlike ChannelTerrain/AccentTerrain,
        /// a road never repaints corner terrain: every road cell stays this composition's own
        /// PrimaryOpenTerrain, so no separate terrain slot is needed. Empty = the tileset has no
        /// verified road-lane vocabulary; compositions skip road carving entirely (fully back-compat).
        /// Only set after verifying RoadVocabularyCheck.SupportsRoads returns true, mirroring every
        /// other tileset-declared capability in this file.
        /// </summary>
        public string RoadCrosser { get; set; } = string.Empty;

        /// <summary>
        /// Terrain used for the SOLID (wall/impassable) mass. Empty = the tileset's declared Default
        /// terrain, which is correct for every interior tileset (their GENERAL Default IS the wall).
        /// EXTERIOR tilesets invert this: ttd01/ttf01/ttf02 declare Default==Floor=="Desert"/"Forest"
        /// (the WALKABLE ground -- their fully-open Desert/Forest tiles are pathnode A) while the
        /// impassable enclosure terrain is "Cliff" (whose fully-Cliff tile is pathnode-restricted), so
        /// composing with the default solid would carve unwalkable rock "rooms" out of a walkable
        /// "wall" mass. Declaring SolidTerrainOverride("Cliff") + PrimaryOpenTerrain("Desert") gives
        /// real dungeon-style enclosure: cliff-walled canyons with walkable clearings. Consumed by
        /// DungeonComposition.BuildLayoutParameters; LayoutSolver.Solve keeps stamping
        /// the tileset Default whenever the composed parameters carry no explicit solid.
        /// </summary>
        public string SolidTerrainOverride { get; set; } = string.Empty;

        /// <summary>
        /// Terrain used for open/walkable space. Empty = the tileset's declared Floor terrain.
        /// Some tilesets keep their richest room vocabulary on a different terrain: zsf01's declared
        /// floor has a single fully-open tile while its 'floor' terrain carries the hand-built room
        /// variants, and vmr01's 'Plaza' has 11 fully-open variants vs 4 on 'Floor'.
        /// </summary>
        public string PrimaryOpenTerrain { get; set; } = string.Empty;

        /// <summary>
        /// Optional second open-terrain label this tileset offers for multi-terrain districts (see
        /// MacroLayoutParameters.SecondaryOpenTerrain), e.g. zsf01's "Floor2" alongside its
        /// PrimaryOpenTerrain "floor", or vmr01's "Floor" alongside "Plaza". Empty = no districts for
        /// this tileset. Only takes effect when the composed layout profile uses RoomsAndCorridors in
        /// Tunnel mode; only set this after verifying full (secondary, solid) corner coverage AND
        /// Doorway-junction tiles for the secondary terrain (see MultiTerrainDistrictTests).
        /// </summary>
        public string SecondaryOpenTerrain { get; set; } = string.Empty;

        /// <summary>
        /// 1x1 "group" tiles (treasure mounds, pillars, hot springs, ...) this tileset offers as rare
        /// decorative sprinkles into open room space, keyed by the .set [GROUPn] Name with a relative
        /// weight. Empty = no feature tiles configured for this tileset. TileResolver re-verifies each
        /// name's structural eligibility (1x1, flat, doorless, crosser-free, pathnode A) at resolve
        /// time and silently drops any that fail rather than trusting this list blindly.
        /// </summary>
        public Dictionary<string, int> FeatureTiles { get; set; } = new();

        /// <summary>
        /// Dressing obligation per feature group name (see <see cref="FeatureZoneDressing"/>): an
        /// area-marking feature tile (grass lawn, fountain court) that lands inside an open room
        /// must carry a composed ensemble rather than standing bare. Only consumed under the urban
        /// placement grammar (<see cref="UrbanDressing"/>); feature groups absent from this map
        /// (trees, water, treasure mounds -- art that already fills the cell) are never dressed.
        /// </summary>
        public Dictionary<string, FeatureZoneDressing> FeatureTileDressings { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Tileset "group" set pieces (wall-bounded rooms hanging off Tunnel corridors, or floor-level
        /// decorative pieces dropped into open room interiors) this tileset offers, keyed by the .set
        /// [GROUPn] Name with a max-instances-per-area count. Empty = no set pieces configured for
        /// this tileset. LayoutGroupStamper re-verifies each name's structural eligibility (shape,
        /// corners, crossers) at stamp time rather than trusting this list blindly.
        /// </summary>
        public Dictionary<string, int> SetPieces { get; set; } = new();

        /// <summary>
        /// Themed 1x1 "exit" group names (e.g. tdt01 Exit01-03) this tileset offers as a GroupExit
        /// substitution for Exit-kind transitions, in priority order tried by GroupExitPlanner.
        /// Empty = no group-exit substitution configured for this tileset (e.g. zsf01/Facility).
        /// GroupExitPlanner re-verifies each name's structural eligibility (1x1, flat, crosser-free,
        /// has a door slot) at resolve time rather than trusting this list blindly.
        /// </summary>
        public List<string> ExitGroups { get; set; } = new();

        /// <summary>
        /// Largest MacroLayoutParameters.ElevationRegions value this tileset's real tile inventory has
        /// verified rim vocabulary for (see LayoutElevationPainter.HasRimVocabulary and the census
        /// notes on BaseGameTilesetProfiles.Dungeon). 0 = no verified elevation vocabulary; a layout
        /// profile's own ElevationRegions request is clamped down to this by
        /// DungeonComposition.BuildLayoutParameters, the same "layout expresses intent, tileset caps to
        /// verified support" shape as AccentTerrain/ChannelTerrain vs AccentDensity/AccentChannels.
        /// LayoutElevationPainter re-verifies live against the real TilesetModel regardless -- this cap
        /// only controls how many regions a composition ASKS for, never whether painting one is safe.
        /// </summary>
        public int MaxElevationRegions { get; set; } = 0;

        /// <summary>
        /// Largest MacroLayoutParameters.PoolRegions value this tileset's real tile inventory has
        /// verified depth-pool vocabulary for (see LayoutElevationPoolPainter.HasPoolVocabulary). 0 = no
        /// verified pool vocabulary; a layout profile's own PoolRegions request is clamped down to this
        /// by DungeonComposition.BuildLayoutParameters, the same "layout expresses intent, tileset caps
        /// to verified support" shape as MaxElevationRegions. Pools use this profile's own AccentTerrain
        /// as the pool terrain (the same terrain LayoutAccentPainter/LayoutAccentChannelCarver already
        /// use for this tileset's secondary hazard/liquid terrain), so this only takes effect when
        /// AccentTerrain is also set.
        /// </summary>
        public int MaxPoolRegions { get; set; } = 0;

        /// <summary>
        /// Largest MacroLayoutParameters.ReliefRegions value this tileset's real tile inventory has
        /// verified per-corner relief vocabulary for (see LayoutReliefPainter's capability gate and
        /// the census notes on BaseGameTilesetProfiles.Dungeon/MinesAndCaverns). 0 = no verified
        /// relief vocabulary; a layout profile's own ReliefRegions request is clamped down to this by
        /// DungeonComposition.BuildLayoutParameters, the same "layout expresses intent, tileset caps
        /// to verified support" shape as MaxElevationRegions/MaxPoolRegions. LayoutReliefPainter
        /// re-verifies every individual perturbation live against the real TilesetModel regardless.
        /// </summary>
        public int MaxReliefRegions { get; set; } = 0;

        /// <summary>
        /// Optional "slope blend" terrain LayoutReliefPainter may flip individual open-terrain
        /// corners to while painting relief -- the terrain this tileset uses to render a gradual
        /// walkable slope between two floor grades (e.g. tdm01's GentleSlope against its [Cave]
        /// Floor, GentleDesert against Desert, GentleOrganic against Organic). Empty = no blend
        /// terrain (relief perturbs heights only). Only set after verifying full flat
        /// (open, blend) corner coverage among resolver-usable tiles -- the same verification bar as
        /// AccentTerrain -- since every blend flip's neighbors resolve from that flat vocabulary.
        /// </summary>
        public string ReliefBlendTerrain { get; set; } = string.Empty;

        /// <summary>
        /// Alternate ramp-lane edge-crosser name this tileset's raised-tile family carries instead of
        /// the canonical "Ramp" (e.g. tdm01's "Slope"). Empty = canonical "Ramp". Consumed by both
        /// LayoutElevationPainter.TryAddRampLane and LayoutReliefPainter's lane proposals; both
        /// re-verify every spliced lane live against the real TilesetModel regardless, so this only
        /// selects which name is tried, never whether a lane is safe.
        /// </summary>
        public string RampCrosser { get; set; } = string.Empty;

        /// <summary>
        /// Alternate Tunnel-mode body crosser this tileset's district/palette carves instead of the
        /// canonical "Corridor" (e.g. tdc01's "[Grey]" district uses "GreyCorridor") -- mechanically
        /// identical vocabulary, just a different string the tileset's own art was authored under.
        /// Empty = no alternate body vocabulary; the composed layout keeps using canonical Corridor/
        /// Doorway. Only takes effect when paired with TunnelPortCrosser (both or neither) and when the
        /// composed layout profile requests Corridor-type Tunnel mode (see
        /// DungeonComposition.BuildLayoutParameters, which switches CorridorCrosserType to Custom).
        /// Only set after verifying the full body/port SHAPE inventory with TunnelVocabularyCheck.
        /// SupportsTunnels (Custom overload), not merely that both crosser names appear somewhere in the
        /// tileset's declared vocabulary.
        /// </summary>
        public string TunnelBodyCrosser { get; set; } = string.Empty;

        /// <summary>See <see cref="TunnelBodyCrosser"/>. May equal the canonical "Doorway" (several
        /// districts rename only their body crosser and keep door transitions on canonical "Doorway"),
        /// or a district-specific name (e.g. a district that renames both, like tdc01's "[Dwarven]"
        /// district's "DwarvenDoorway" -- verify independently, since renaming the port is a materially
        /// different shape probe than a body-only rename).</summary>
        public string TunnelPortCrosser { get; set; } = string.Empty;

        /// <summary>
        /// Crosser names (beyond the canonical "Doorway"/"Bridge" pair) this tileset's real tile
        /// inventory uses for a door-implying crosser under a completely different name -- e.g.
        /// Barrows/tbw01's "door_corridor", paired with its own "corridor" Tunnel body crosser (see
        /// TunnelBodyCrosser/TunnelPortCrosser above) rather than the canonical Corridor/Doorway pair.
        /// Declaring a name here is what lets TileResolver register a door-slot tile carrying that
        /// crosser as an ordinary structural candidate (see TileResolver's class doc comment) --
        /// without it, every such tile is excluded from candidate lookup entirely regardless of shape.
        /// Empty = no alternate door-slot vocabulary (every tileset except one that renames its
        /// door-implying crosser entirely). Distinct from TunnelPortCrosser: a tileset may need this
        /// declared even when its Tunnel body/port pair stays canonical, and TunnelPortCrosser itself
        /// is NOT automatically credited here -- declare it explicitly if it also carries door slots.
        /// </summary>
        public List<string> DoorSlotCrossers { get; set; } = new();

        /// <summary>
        /// Physical tile IDs this tileset's real .set data resolves to structurally (real, non-PADDING
        /// corners -- the resolver would legitimately place them), but whose MODEL is confirmed
        /// placeholder/stub art that renders wrong in-game (e.g. twc03's "xyz" family: 15 tile IDs
        /// whose models are literal ASCII stubs with an untextured -- bitmap NULL -- rendered trimesh
        /// node sitting on real geometry, verified directly by dumping the raw .mdl content; they
        /// render as flat white tiles in Fort Complex generations). Empty = no exclusions (default;
        /// every existing tileset profile). Consumed by DungeonComposition.BuildLayoutParameters,
        /// threaded through MacroLayoutParameters/MacroLayout, and enforced by TileResolver at the
        /// lowest shared candidate-lookup level so every placement path (legacy flat, height-aware,
        /// feature sprinkling) skips them uniformly. Does NOT affect LayoutGroupStamper's pinned-tile
        /// path (set pieces/exit groups bypass candidate lookup entirely) -- see
        /// ExcludedTileRegressionTests, which statically asserts none of these IDs are members of any
        /// SetPieces/ExitGroups group this profile wires, so that bypass never matters in practice.
        /// </summary>
        public HashSet<int> ExcludedTiles { get; set; } = new();

        /// <summary>
        /// True for a profile that recomposes an already-registered physical tileset resref against a different
        /// terrain/district palette (e.g. "crypt_grey" recomposing tdc01's Grey palette alongside the
        /// base "crypt" profile's Tan palette) rather than registering a new physical tileset. Palette
        /// variants offer the palette as a composable option and inherit family-level defaults through
        /// <see cref="DungeonTilesetPaletteInheritance"/>. TileCoverageCensusTests iterates every
        /// profile sharing a TilesetResref, so a tile counts as reachable when any variant composes it.
        /// </summary>
        public bool IsPaletteVariant { get; set; } = false;

        /// <summary>
        /// This tileset FAMILY's own bulk "set dressing" palette, mined from its own hand-built
        /// reference areas rather than from the theme composed onto it — decoration is a
        /// function of the VISUAL family (what the tileset's own art depicts), not the content theme.
        /// A palette-variant profile (<see cref="IsPaletteVariant"/>) with no entries of its own
        /// automatically inherits the palette of the first non-variant profile sharing its
        /// <see cref="TilesetResref"/> through <see cref="DungeonTilesetPaletteInheritance"/> — only
        /// declare entries here directly when that district's own evidence genuinely differs. Empty on
        /// a base (non-variant) profile = no mined evidence exists for this family yet; documented
        /// per-profile rather than silently guessing another family's dressing.
        /// </summary>
        public List<DungeonDecorationEntry> Decorations { get; set; } = new();

        /// <summary>
        /// Evidence-backed multi-placeable groupings (see <see cref="DungeonVignette"/>) this tileset
        /// family offers as a unit placement — vignette members are never sampled independently.
        /// Inherited the same way as <see cref="Decorations"/> for palette variants.
        /// </summary>
        public List<DungeonVignette> Vignettes { get; set; } = new();

        /// <summary>
        /// This tileset FAMILY's own evidence-derived decorative-placeable density (placeables per
        /// total area tile at 100% request density), overriding the composed theme's
        /// <see cref="DungeonDetail.DecorationBaseDensity"/> when set (&gt; 0). Dressing INTENSITY is
        /// a property of the visual family, not the content theme: hand-built fcx01 city areas
        /// measure ~1.6 decoratives/tile aggregate while cave families measure ~0.15 -- a theme
        /// composed onto a city tileset must dress at CITY density. 0 (the default) keeps the
        /// theme-owned density, so families without their own mined density band behave exactly as
        /// before this override existed. Inherited by palette variants the same way as
        /// <see cref="Decorations"/> (see DungeonTilesetPaletteInheritance).
        /// </summary>
        public double DecorationDensityPerTile { get; set; }

        /// <summary>
        /// NAMED alternate decoration palettes (key = profile name, case-insensitive) selectable per
        /// theme/request -- see <see cref="DungeonDecorationProfile"/>. The standard palette stays in
        /// <see cref="Decorations"/>/<see cref="Vignettes"/>; entries here fully replace it when
        /// selected. Inherited by palette variants alongside the standard palette (see
        /// DungeonTilesetPaletteInheritance).
        /// </summary>
        public Dictionary<string, DungeonDecorationProfile> DecorationProfiles { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// True for a tileset whose hand-built reference areas follow an URBAN placement grammar,
        /// enabling DungeonDecorationPlanner's city composition rules for this family only (every
        /// other tileset's plan stays byte-identical): bearing alignment (placements face the wall/
        /// road/facade they belong to, quantized to cardinals -- hand-built fcx01 measures 73%
        /// cardinal-aligned vs 29% for random spin), road integrity (the carved road ribbon stays a
        /// clear walkway; only <see cref="DungeonDecorationEntry.AllowOnRoadSurface"/> lamp-family
        /// entries may stand on it, everything else sets back and faces the street), facade rows
        /// (road-margin and structure-frontage runs repeat a single resref at an even rhythm with a
        /// shared bearing), cargo grids (structure/corner clutter snaps to a small wall-aligned grid
        /// instead of a loose disc), and zone discipline (clutter piles anchor only against walls,
        /// structure bases, and corners -- never free-floating in plaza centers, which are reserved
        /// for composed courtyards/centerpieces). Evidence: the July 2026 city review pass measured
        /// generated fcx01 areas at chance-level bearing alignment with kiosks standing on the road
        /// ribbon and junk piles mid-plaza -- "a scattering of different objects randomly placed".
        /// </summary>
        public bool UrbanDressing { get; set; }

        /// <summary>
        /// Structural building placeables this tileset family erects along open-area perimeter
        /// edges and street margins to form canyon walls (see <see cref="BuildingFrontageEntry"/>
        /// and BuildingFrontagePlanner). Empty = no placeable-frontage system for this family
        /// (every non-city tileset; their plans and layouts stay byte-identical). Only meaningful
        /// alongside <see cref="UrbanDressing"/>. At promenade scale (12x12-20x20) this is the
        /// PRIMARY canyon mechanism -- the hand-built flagship's walls are placeables, not tiles;
        /// at 24-32 it complements the tile-block mechanism
        /// (<see cref="BuildingBlockContiguity"/>), fronting tile-block faces and walling the
        /// remaining margins. Inherited by palette variants like <see cref="Decorations"/>.
        /// </summary>
        public List<BuildingFrontageEntry> FrontageBuildings { get; set; } = new();

        /// <summary>
        /// When true, every placed frontage building rolls a subtle per-instance uniform visual
        /// scale (0.94-1.08, see BuildingFrontagePlanner.MinScaleJitter/MaxScaleJitter) so
        /// same-model neighbors read as distinct structures instead of clone rows. JUDGMENT CALL,
        /// not mined evidence: hand-built areas achieve silhouette variety with model mixing, but
        /// they also ship per-instance VisualTransform scale on placeables (90 instances in
        /// ar_pw_indusvel alone), so the mechanism itself is an established hand-building tool.
        /// The scale is applied to the footprint before the walkable-clearance fit check and is
        /// persisted by the generated-area document populator as a .git VisualTransform struct,
        /// which the toolset and client both render. Off (the default) for every
        /// family that does not declare it. Inherited by palette variants like
        /// <see cref="Decorations"/>.
        /// </summary>
        public bool FrontageScaleJitter { get; set; }

        /// <summary>
        /// Terrain labels that render as a bottomless drop (no supporting surface at open-platform
        /// level) in this tileset family -- fcx01's "holes" chasm. When non-empty,
        /// BuildingFrontagePlanner enforces the mined footprint-support envelope against the
        /// resolved corner-terrain plan (see FrontageSupportRule): a candidate building whose
        /// footprint hangs too far over in-grid chasm is rejected for that slot, so towers stand on
        /// platform lips the way every hand-built platform-level tower does instead of floating
        /// over the abyss. Distinct from <see cref="SolidTerrainOverride"/> because most families'
        /// solid terrain is WALL MASS (a raised surface, perfectly valid support), not a drop.
        /// Empty = no chasm semantics; the support rule never runs (every non-city tileset --
        /// plans stay byte-identical). Inherited by palette variants like
        /// <see cref="Decorations"/>.
        /// </summary>
        public List<string> ChasmTerrains { get; set; } = new();

        /// <summary>
        /// Wall-mounted sign/holo placeables this tileset family hangs on building faces (see
        /// <see cref="FacadeMountEntry"/> and BuildingFrontagePlanner.PlanFacadeMounts). Empty =
        /// no facade-mount pass. Only meaningful alongside <see cref="UrbanDressing"/>.
        /// Inherited by palette variants like <see cref="Decorations"/>.
        /// </summary>
        public List<FacadeMountEntry> FacadeMounts { get; set; } = new();

        /// <summary>
        /// Street-dressing placeables this tileset family lays along carved road-lane cells (see
        /// <see cref="StreetDressingEntry"/> and DungeonDecorationPlanner.PlanStreetDressing):
        /// flat road-marking plates on the lane surface plus margin accents at the lane edges --
        /// the hand-built dressed-street fill pattern that room-anchored mechanisms cannot reach
        /// on corridor-heavy layouts. Empty = no street pass (every non-city tileset; their plans
        /// stay byte-identical). Only meaningful alongside <see cref="UrbanDressing"/> and a
        /// declared <see cref="RoadCrosser"/>. Inherited by palette variants like
        /// <see cref="Decorations"/>.
        /// </summary>
        public List<StreetDressingEntry> StreetDressings { get; set; } = new();

        /// <summary>
        /// The layout-profile key of this family's signature composition: the pairing its hand-built
        /// reference areas most closely resemble. Empty means no recommended pairing. This is
        /// authoring metadata and never restricts the selectable layouts. Inherited by palette
        /// variants like <see cref="Decorations"/>.
        /// </summary>
        public string SignatureLayoutProfileKey { get; set; } = string.Empty;

        /// <summary>
        /// The area size (tiles per side) of the signature composition above -- the scale the
        /// family's hand-built reference areas dress at (e.g. the fcx01 street-canyon city at 24,
        /// where tile canyon blocks, placeable frontage, lamp lines, and full dressing all
        /// compose). 0 means no recommended size.
        /// </summary>
        public int SignatureSize { get; set; }

        /// <summary>
        /// This tileset FAMILY's standard AREA atmosphere (see <see cref="DungeonAreaAtmosphere"/>),
        /// mined from its own hand-built exemplar areas' .are properties. Null (the default) = no
        /// unambiguous family evidence exists (fewer than 3 hand-built areas agreeing on the core
        /// atmosphere tuple, or a dead tie between candidate tuples): generated documents keep the
        /// source ARE values rather than guessing. Inherited by palette variants the same way as
        /// <see cref="Decorations"/> (see DungeonTilesetPaletteInheritance).
        /// </summary>
        public DungeonAreaAtmosphere Atmosphere { get; set; }

        /// <summary>
        /// NAMED alternate atmospheres (key = profile name, case-insensitive) selectable per
        /// theme/request, mirroring <see cref="DecorationProfiles"/> exactly: the standard
        /// atmosphere stays in <see cref="Atmosphere"/>, entries here fully replace it when
        /// selected, and an unknown name falls back to the standard one. Inherited by palette
        /// variants alongside the standard atmosphere.
        /// </summary>
        public Dictionary<string, DungeonAreaAtmosphere> AtmosphereProfiles { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Resolves the effective atmosphere for one composition, mirroring
        /// DungeonDecorationPlanner.Plan's decoration-profile resolution exactly: a per-request
        /// override name wins over the theme's own <see cref="DungeonDetail.AtmosphereProfile"/>
        /// declaration; a resolved name the tileset declared selects that named atmosphere; an
        /// unknown or empty name falls back to the standard <see cref="Atmosphere"/> (which is
        /// null when this family has no mined evidence -- callers then change nothing).
        /// </summary>
        public DungeonAreaAtmosphere ResolveAtmosphere(string themeProfileName, string requestProfileName = null)
        {
            var profileName = !string.IsNullOrWhiteSpace(requestProfileName) ? requestProfileName : themeProfileName;
            if (!string.IsNullOrWhiteSpace(profileName) &&
                AtmosphereProfiles.TryGetValue(profileName, out var named))
                return named;
            return Atmosphere;
        }
    }
}
