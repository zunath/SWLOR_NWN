using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

// Exposes internal arrangement-mechanism helpers (vignette placement, direction quantization) to the
// coherence-metrics test suite so it can verify PlaceVignette's rotation math and PlaceWallRuns'
// direction bucketing directly rather than trying to reverse-engineer them from Plan()'s flat output.
[assembly: InternalsVisibleTo("SWLOR.Game.Server.Tests")]

namespace SWLOR.Game.Server.Service.AreaGenerationService
{
    /// <summary>
    /// One planned decorative placeable spawn point: a resref, a flat (ungrounded, Z=0) world
    /// position, a facing, and the context it was planned under. Purely a data record — grounding
    /// (GetGroundHeight) and CreateObject happen in DungeonContentPlacer, which is the only part of
    /// this pass that touches the live engine, so Plan() itself is unit-testable without an area.
    /// </summary>
    public class PlannedDecoration
    {
        public string Resref { get; set; } = string.Empty;
        public Vector3 Position { get; set; }
        public float Facing { get; set; }
        public DecorationContext Context { get; set; }
    }

    /// <summary>
    /// Plans placeable "set dressing" decoration spawn points against a RESOLVED dungeon layout
    /// (post-stamper), so generated areas look furnished like hand-built ones — streetlights,
    /// planters, crates, wall clutter — instead of bare, randomly-scattered geometry.
    ///
    /// Palette source (what gets placed): the TILESET PROFILE's own bulk palette
    /// (<see cref="DungeonTilesetProfile.Decorations"/>/<see cref="DungeonTilesetProfile.Vignettes"/>),
    /// mined from that tileset family's own hand-built reference areas (decoration_evidence/ scratchpad
    /// data) — decoration is a function of the tileset's VISUAL family, not the content theme composed
    /// onto it. The theme's own <see cref="DungeonDetail.Decorations"/> contributes only a small accent
    /// list layered on top (a Sith banner, a mining cart) — see MergePalette. This is the fix for
    /// generated areas whose theme and composed tileset don't match (e.g. Alien Ruin content generated
    /// on the Futuristic City tileset previously dressed with Alien Ruin's own palette regardless).
    ///
    /// Arrangement (how it's placed): independent per-tile coin flips read as "random junk scattered
    /// around," not hand-built set dressing. This planner instead composes:
    ///  - Doorway flanking (PlanDoorwayFlanks): identical items mirrored on either side of a doorway,
    ///    placed as a pair or not at all — never a single lopsided flank.
    ///  - Per-room motifs (PickMotif): each room draws a small (1-3 resref) subset of the palette for
    ///    its wall/corridor dressing and reuses only those — rooms feel internally consistent and
    ///    distinct from each other, instead of every placement being an independent full-palette roll.
    ///  - Rhythmic wall/corridor runs (PlaceWallRuns): a room's eligible perimeter tiles are grouped into
    ///    straight runs (one per wall-facing direction) and dressed at an even spacing derived from the
    ///    calibrated density, instead of a per-tile Bernoulli trial — real cadence instead of noise.
    ///  - Vignette clusters (PlaceVignette): a curated multi-placeable grouping (e.g. crate stack,
    ///    bench+lamp) mined from hand-built nearest-neighbor co-occurrence is placed as a single unit at
    ///    one anchor tile, at most once per room.
    ///  - Centerpieces (unchanged): a rare large-room centerpiece near (never on) CenterTile.
    ///
    /// Evidence: curated per-tileset-family palettes/vignettes and per-theme base densities were mined
    /// from hand-built SWLOR reference areas (placeable resref frequency, density-per-tile, edge-vs-
    /// center tile-local position proxy, and nearest-neighbor co-occurrence pairs) — see the
    /// decoration_evidence/ scratchpad data and the DungeonTilesetProfile/DungeonDetail doc comments for
    /// the specific reference areas each palette draws from.
    ///
    /// Exclusions (never decorated) — unchanged from the original single-pass design:
    ///  - Set-piece rooms (LayoutRoom.IsSetPiece) — walkable only via their own baked walkmesh, not
    ///    the abstract tile grid this planner reasons about (same rule DungeonContentPlacer.Populate
    ///    already applies to ambient/boss content).
    ///  - Every transition anchor cell (TransitionPoint.Tile) and, for Door/GroupExit-style
    ///    transitions, the DoorCell/DoorwayCell — the tile-center "waypoint under geometry" lesson
    ///    (see TileDoorPlanner's own doc comments) applies here too: a decoration at a doorway's
    ///    exact tile center can land inside the doorframe's own baked art.
    ///  - A room's CenterTile — reserved for boss/treasure/exit content placement (see
    ///    DungeonContentPlacer.PopulateBossRoom/PlaceExit), regardless of room role.
    ///
    /// Scope note (CorridorSide): corridors carved in OpenLane mode are never recorded as their own
    /// LayoutRoom (see RoomsAndCorridorsLayout/WarrenLayout — only rectangular/chamber rooms become
    /// LayoutRoom objects) and Tunnel-mode corridors are solid cells with no open tile to decorate at
    /// all; ResolvedLayout exposes no general "is this tile open" query outside room membership
    /// (AreaSynthesizer.ComputeWalkablePoints is room-tiles-only too). CorridorSide therefore targets
    /// long/narrow ROOMS (a room whose tile bounding box has a short axis &lt;= 2 tiles — the
    /// corridor-like chambers Warren/Labyrinth/RoomsAndCorridors actually produce as real LayoutRoom
    /// objects) rather than carved corridor cells. A true carved-corridor decoration pass would need a
    /// new layout-level open-tile classification and is out of scope for this pass.
    /// </summary>
    public static class DungeonDecorationPlanner
    {
        private const float TileSize = 10f;
        private const float TileHalf = 5f;

        /// <summary>
        /// How far off the tile center a wall-hugging/corridor-side/doorway-flank decoration sits,
        /// biased toward the neighboring solid direction — matches the hand-built edge-hugging
        /// evidence (edge-hugging tile-local position fraction ~0.6-0.8 across every mined family).
        /// </summary>
        private const float WallOffset = 3.5f;

        /// <summary>
        /// Centerpiece decorations sit off-center — never ON CenterTile, which is reserved — mirroring
        /// DungeonContentPlacer's own FeatureOffset convention for treasure/exit placement.
        /// </summary>
        private const float CenterOffset = 2.5f;

        /// <summary>"Large enough" for a centerpiece: the mined evidence's center-tendency fraction is
        /// low (roughly 3-13% across families) — small rooms never got one in the hand-built sample.</summary>
        private const int MinCenterpieceRoomTiles = 6;

        /// <summary>
        /// Share of the total decoration budget (see <see cref="Plan"/>'s targetCount) reserved for
        /// RoomCenter centerpieces rather than wall/corridor/doorway "hugging" placements — the
        /// mid-point of the mined evidence's per-family center_fraction proxy (roughly 3-24% of a
        /// family's decorative placeables sit away from the perimeter, clustering 3-9% outside one
        /// single-area outlier family; see decoration_evidence/evidence_by_tileset.json
        /// context_proxy.center_fraction). Centerpieces are additionally gated per-room by
        /// MinCenterpieceRoomTiles/isCorridorLike regardless of this share.
        /// </summary>
        private const double CenterpieceTargetShare = 0.08;

        /// <summary>
        /// Share of the total decoration budget reserved for symmetric doorway-flank PAIRS (two
        /// placements per event — see PlanDoorwayFlanks) rather than wall/corridor "hugging"
        /// placements. Doorway clutter is a real but small part of the mined edge-hugging evidence
        /// (DoorwayFlank is the narrowest, most situational bucket of the four DecorationContext
        /// values); this share intentionally stays modest so most of the budget still goes to the
        /// rhythmic wall/corridor runs that dominate a hand-built room's dressing.
        /// </summary>
        private const double DoorwayFlankTargetShare = 0.10;

        /// <summary>
        /// Share of the total decoration budget reserved for vignette clusters (see PlaceVignette) —
        /// mined multi-placeable co-occurrence groupings placed as a single unit, at most one per room.
        /// </summary>
        private const double VignetteTargetShare = 0.06;

        /// <summary>
        /// Share of the total decoration budget reserved for composed courtyard arrangements (see
        /// PlanCourtyard) -- ONLY carved out of the wall-run budget when the composed palette actually
        /// curates CourtyardCenter/Courtyard buckets AND at least one room has an interior anchor
        /// (courtyardTarget stays 0 otherwise, so every tileset without curated courtyards keeps its
        /// exact pre-courtyard budget split and RNG sequence). Evidence: hand-built fcx01 interior
        /// items (&gt;2 tiles from walls/roads) are a major share of plaza dressing (13/19 decorated
        /// areas carry them), arranged as centerpiece+ring clusters rather than scatter -- see the
        /// July 2026 city-density pass courtyard measurement (ring clusters of 4-13 items at radius
        /// ~4-9m around a floor decal/light/structure centerpiece).
        /// </summary>
        private const double CourtyardTargetShare = 0.12;

        /// <summary>Ring member count bounds for one courtyard. Hand-built ring clusters measured
        /// 4-13 members; 8 caps the generated ring so one courtyard can't eat a whole room's budget
        /// (larger plazas instead qualify for a bigger ring via the room-area scaling below).</summary>
        internal const int CourtyardMinRingItems = 4;
        internal const int CourtyardMaxRingItems = 8;

        /// <summary>Ring radius band (world units): base + up to jitter. Hand-built rings measured
        /// mean radius 4.3-9.2; generated rooms are smaller than hand-built open districts (roads and
        /// stamped buildings consume the rest of the plaza), so this sits at the band's lower half --
        /// a 5.0-6.5 ring plus member jitter stays within the anchor's verified 2-tile interior
        /// clearance.</summary>
        internal const float CourtyardBaseRadius = 5.0f;
        private const float CourtyardRadiusJitter = 1.5f;

        /// <summary>
        /// Interior clearance (Chebyshev, tiles) a courtyard anchor tile needs: every tile within
        /// this range must belong to the same room (no walls, no stamped building footprints -- those
        /// are removed from room.Tiles by the stamper -- no foreign rooms) and neither the anchor nor
        /// any ring member may stand ON a road-carrying tile. Hand-built interior items sit &gt;2
        /// tiles from walls/roads, but that distance is a property of hand-built areas' LARGE open
        /// districts: measured against generated 32x32 fcx01 layouts (July 2026 city-density pass),
        /// clearance 2 leaves literally ZERO qualifying rooms (0/206 packed, 0/157 complex -- roads
        /// thread every generated plaza and stamped buildings consume the rest), while clearance 1
        /// (a full 3x3 in-room block, anchor off the road surface) qualifies 50/206 and 13/157. The
        /// ring's own radius band (5.0-6.5 + member jitter, under 7.5 world units) stays inside the
        /// verified 3x3 block, so clearance 1 is sufficient for every position the arrangement can
        /// emit.
        /// </summary>
        private const int CourtyardInteriorClearance = 1;

        /// <summary>A room counts as corridor-like when its shorter bounding-box axis is this narrow.</summary>
        private const int CorridorLikeMaxSpan = 2;

        /// <summary>
        /// Share of the total decoration budget reserved for clutter piles (see PlanClutterPile) --
        /// ONLY carved out when the composed palette curates <see cref="DecorationRole.Clutter"/>
        /// entries AND at least one room offers an anchor tile (0 otherwise, so every palette
        /// without curated clutter keeps its exact pre-pile budget split and RNG sequence). This is
        /// the DOMINANT share on purpose: hand-built fcx01 city dressing is piles, not spread
        /// singles -- 75% of hand-built decoratives sit within 3m of another decorative (all-NN
        /// median 1.6m, p25 0.5m), and the palette backbone is crates/containers/barrels/rubble
        /// with street furniture at only ~4% of placements.
        /// </summary>
        private const double ClutterPileTargetShare = 0.55;

        /// <summary>Member count bounds for one pile (junk items, excluding the optional decal
        /// underneath). Hand-built junk arrangements measure roughly 3-10 items; 8 caps a single
        /// pile so one roll can't eat a whole room's budget.</summary>
        internal const int PileMinItems = 3;
        internal const int PileMaxItems = 8;

        /// <summary>Pile radius band (world units): members land within this disc around the pile
        /// center. Matches the hand-built within-3m clustering (all-NN median 1.6m).</summary>
        internal const float PileMinRadius = 1.3f;
        internal const float PileMaxRadius = 2.8f;

        /// <summary>How far a wall-anchored pile's center sits off the tile center toward the wall
        /// -- slightly inside <see cref="WallOffset"/> so the pile's own radius stays on the tile.</summary>
        private const float PileWallOffset = 2.5f;

        /// <summary>Center jitter for an interior (no wall direction) pile anchor.</summary>
        private const float PileCenterJitter = 1.5f;

        /// <summary>
        /// Chance a pile gets a <see cref="DecorationRole.GroundDecal"/> entry layered underneath --
        /// hand-built dirt/stain decals appear as layering under junk arrangements (the two dirt
        /// decals plus the floor-marking decal total ~1000 of the mined fcx01 placements, always
        /// co-located with clutter), never as lone patches.
        /// </summary>
        private const double DecalUnderPileChance = 0.65;

        /// <summary>Rejection-sampling attempts per pile member before giving up on that member.</summary>
        private const int PilePlacementAttempts = 6;

        /// <summary>Minimum separation between two members of the same pile -- hand-built piles pack
        /// tightly (same-resref NN p25 0.5m) but distinct placeables z-fight when near-coincident.</summary>
        private const float PileMemberMinSeparation = 0.9f;

        /// <summary>Members never land closer than this to the anchor tile's boundary, so a pile can
        /// never spill over the neighboring cell (which may be a hole/chasm/foreign room on exterior
        /// tilesets where "solid" is a platform gap, not a wall).</summary>
        private const float PileTileEdgeMargin = 0.8f;

        /// <summary>A pile commits only when at least this many members landed; otherwise nothing is
        /// planned for the anchor (a 0-1-item "pile" is just scatter, and the decal must never end
        /// up effectively alone).</summary>
        private const int PileCommitMinItems = 2;

        /// <summary>
        /// Anchor-pool weight multiplier for tiles hugging a stamped structure footprint (see
        /// IsStructureAdjacent) -- tower/building bases preferentially collect piles, closing the
        /// reported "stamped tower groups with completely bare bases" gap while the same mechanism
        /// still dresses ordinary wall lines and room interiors.
        /// </summary>
        private const double PileStructureAnchorWeight = 2.0;

        /// <summary>
        /// Two-level junk motif bounds: each ROOM draws a junk POOL (the wider clutter subset its
        /// piles share -- room-level coherence), and each PILE draws its own small 2-3-type motif
        /// from that pool (a real junk stack mixes only a couple of types). The two levels together
        /// keep one room's piles related without letting three junk types dominate a whole area --
        /// measured per-area top-3 resref share drops to the hand-built scale (&lt;= ~0.35) versus
        /// ~0.44 with a single flat per-room motif.
        /// </summary>
        private const int PileRoomPoolMinResrefs = 6;
        private const int PileRoomPoolMaxResrefs = 9;
        private const int PileMotifMinResrefs = 2;
        private const int PileMotifMaxResrefs = 3;

        /// <summary>Ring radius band for the 1-2 clutter items layered ON a courtyard whose center
        /// is a ground decal (see PlanCourtyard) -- keeps the decal from reading as a lone patch at
        /// the middle of an otherwise wide (5.0-6.5m) courtyard ring.</summary>
        private const float CourtyardDecalToppingMinRadius = 0.6f;
        private const float CourtyardDecalToppingMaxRadius = 1.4f;

        /// <summary>
        /// Share of the total decoration budget reserved for composed MID-ROOM ensembles under the
        /// urban grammar (see PlanInteriorEnsemble/PlanDepotBlock) -- the round-9 "barren plaza
        /// middles" fix. Hand-built fcx01 city rooms carry REAL interior content: measured room-scale
        /// interior share (decoratives with no building/road tile within Chebyshev 1 --
        /// _scratch_decor/mine_r9_interiors.py) runs 0.16-0.51 across the structured commercial/
        /// industrial/civic reference areas, while round-8 generated flagships measured 0.10
        /// (packed 32). Active ONLY under the urban grammar when at least one room offers an
        /// ensemble anchor or depot segment, so every non-urban plan keeps its exact budget split
        /// and RNG stream. Together with <see cref="UrbanClutterPileTargetShare"/> the urban share
        /// sum stays at the round-8 total (0.91), leaving the wall/street runs their original 0.09.
        /// </summary>
        private const double InteriorEnsembleTargetShare = 0.10;

        /// <summary>
        /// URBAN pile share: the ensemble/depot share above is carved out of the pile budget (piles
        /// were the round-8 dominant 0.55), not out of the street/wall runs -- composed depot rows
        /// and mid-room ensembles ARE the structured replacement for part of the loose-pile mass.
        /// Non-urban palettes keep <see cref="ClutterPileTargetShare"/> bit for bit.
        /// </summary>
        private const double UrbanClutterPileTargetShare = 0.45;

        /// <summary>Satellite count bounds for one composed interior ensemble (excluding the
        /// centerpiece): commit needs the minimum, so an ensemble is always centerpiece + 3+
        /// satellites -- at least 4 members total, never a free-standing pair (the reported
        /// "monument fragments standing free in pairs" artifact).</summary>
        internal const int EnsembleMinSatellites = 3;
        internal const int EnsembleMaxSatellites = 5;

        /// <summary>Satellite radius bands: a commercial plaza ISLAND packs tight (kiosk + seating +
        /// trash within arm's reach); a civic monument GARDEN breathes wider (benches/planters set
        /// back from the centerpiece). Both stay well inside the courtyard ring band (5.0-6.5m) so
        /// the two interior arrangements read as different THINGS.</summary>
        internal const float IslandMinRadius = 1.9f;
        internal const float IslandMaxRadius = 3.2f;
        internal const float GardenMinRadius = 2.6f;
        internal const float GardenMaxRadius = 4.0f;

        /// <summary>Chance a committed interior ensemble stands on a base floor decal, and the
        /// margin a Large (8.5-9.6m) floor plate needs (full 3x3 in-room clearance) before it may
        /// serve as that base -- the size-matching rule: big plates only under big compositions,
        /// never under a 2m junk pile (see PickUrbanDecal).</summary>
        private const double EnsembleBaseDecalChance = 0.7;

        /// <summary>
        /// Under-decal chance for clutter piles under the URBAN grammar -- slightly below the
        /// non-urban <see cref="DecalUnderPileChance"/>: the hand-built decal-to-cluster ratio is
        /// ~1.0 INCLUDING the plates that anchor big compositions, and round-8's per-pile 0.65 pad
        /// chance was the reported "pad under every cluster" motif. Non-urban palettes keep the
        /// original constant and RNG path bit-for-bit.
        /// </summary>
        private const double UrbanDecalUnderPileChance = 0.5;

        /// <summary>
        /// Depot-block geometry (see PlanDepotBlock), measured from the hand-built industrial
        /// reference areas (pw_ar_nsshipyard/ns_industrialsec/narshadaar_midoc/vrotrnsdockbay/
        /// vrotrbeslanding -- _scratch_decor/mine_r9_interiors.py): crate-family same-family NN
        /// median 0.09m (builders literally stack), 93% within 2.2m, colinear runs median 4 / p90
        /// 12, cluster bearing dominant-share 0.81. Generated blocks use a 1.35m pitch -- true
        /// butt-joint for the 0.75-1.5m crate family without z-fighting overlap -- in 1-2 parallel
        /// rows, so a depot reads as stacked stock, not exhibits on pads.
        /// </summary>
        internal const float DepotRowPitch = 1.35f;
        private const float DepotRowSeparation = 1.5f;
        private const float DepotWallOffset = 3.4f;
        internal const int DepotBlockMinItems = 4;
        internal const int DepotBlockMaxItems = 9;

        /// <summary>Chance a second depot block composes in a large (12+ tile) industrial room
        /// after the deterministic first block.</summary>
        private const double DepotSecondBlockChance = 0.6;

        /// <summary>
        /// Pile-probability damping inside industrial-flavor rooms once depot blocks exist: loose
        /// pad-singles become the exception there, not the default -- the depot rows ARE the
        /// room's cargo statement (hand-built industrial areas' crate family is 93% butt-pitched,
        /// not spread singles).
        /// </summary>
        private const double IndustrialPileDamping = 0.55;

        /// <summary>Ensemble anchors within one room keep this Chebyshev tile spacing so two
        /// ensembles never fuse into one blob.</summary>
        private const int EnsembleAnchorSpacing = 3;

        /// <summary>
        /// Largest number of consecutive placements PlaceWallRuns emits along one (context, wall
        /// direction) bucket before forcing a real gap and starting a fresh segment — round-3
        /// decoration-quality fix for the reported "ring" artifact: an open room's perimeter used to
        /// be dressed as ONE continuous run per wall-facing direction with no cap, so a room whose
        /// entire perimeter was wall-eligible (the common case on tilesets like fcx01 where "solid
        /// terrain" is platform gaps, not real walls) got the SAME motif wrapped evenly around all
        /// four sides — a closed ring no hand-built reference area produces at this fixture density
        /// (see decoration_evidence/ round-3 statistics harness: hand-built same-resref closed-loop
        /// rate is near-zero for wall-hugging set dressing, and what little occurs is a different
        /// kind of thing — floor-decal motifs, not repeated vertical fixtures). 6 matches the
        /// hand-built per-family longest-run p90 for the typical (non-warehouse-density) families.
        /// </summary>
        internal const int MaxRunSegmentLength = 6;

        /// <summary>
        /// Extra whole-spacing steps skipped (on top of the run's own jittered spacing) when a segment
        /// hits <see cref="MaxRunSegmentLength"/>, so the next segment reads as a visually separate
        /// wall cluster rather than a continuation of the same run.
        /// </summary>
        private const int RunSegmentGapExtraSteps = 1;

        /// <summary>
        /// Largest number of times a single resref may appear as wall/corridor/doorway-hugging
        /// dressing in one room, across every run/side in that room — independent of and in addition
        /// to <see cref="MaxRunSegmentLength"/> (that caps one straight segment; this caps the whole
        /// room, so four capped sides can't still add up to one uniform ring of the same fixture).
        /// Derived from the hand-built grid-bucket same-resref-repeat p90 across the typical
        /// (non-warehouse-density) mined families — see decoration_evidence/handbuilt_summary.json.
        /// Once every motif/palette entry for a room+context hits this cap, PlaceWallRuns stops
        /// dressing that bucket rather than overflowing it (a real hand-built room does not have
        /// unlimited copies of one fixture either).
        /// </summary>
        internal const int MaxSameResrefPerRoomContext = 5;

        /// <summary>
        /// Largest number of DISTINCT resrefs a single room draws into its own wall/corridor motif
        /// (see PickMotif) — "choose 1-3 decoration types per room and repeat them" per the brief;
        /// keeps a room internally consistent instead of sampling the full palette per placement.
        /// </summary>
        internal const int MotifResrefCap = 3;

        /// <summary>Salt XORed into the layout seed so this pass draws a different RNG stream than
        /// DungeonContentPlacer's tier-scaled content pass (which uses seed ^ (tier * 397)).</summary>
        private const int SeedSalt = 0x0EC0;

        /// <summary>
        /// Expected composed cargo-yard rows per area (urban grammar): building-scale
        /// <see cref="DecorationSize.Huge"/> art places ONLY through these rows, in
        /// industrial-flavor rooms -- the hand-built pattern where silo/tank rows concentrate in
        /// shipyard/dock districts (kyru08's mined per-area counts cluster in the industrial and
        /// undercity yards; the commercial promenades carry zero) instead of blanketing the map
        /// (the round-7 regen placed 83 silos across one area -- the reported repetition).
        /// </summary>
        private const double YardTargetPerArea = 2.0;

        /// <summary>Hard per-area total across every Huge placement -- two full rows plus a
        /// landmark one-off. Hand-built mixed-city areas carry Huge silo/tower art in single-digit
        /// counts (per-area p95 by district in the round-8 evidence).</summary>
        internal const int MaxHugePerArea = 6;

        /// <summary>Members per composed yard row (consecutive wall tiles, shared bearing) --
        /// hand-built silo rows pair/triple along yard walls at tile pitch (kyru08 same-resref NN
        /// median 10.01m = exactly the 10m tile).</summary>
        internal const int YardRowMinItems = 2;
        internal const int YardRowMaxItems = 3;

        /// <summary>
        /// Consecutive same-row placements allowed for a <see cref="DecorationSize.Large"/> (3-8m)
        /// entry in an urban facade row before the row forces a gap and swaps fixtures -- 6m
        /// shipping containers repeat as pairs, never six-deep walls (the Medium row cap stays
        /// <see cref="MaxRunSegmentLength"/>).
        /// </summary>
        internal const int LargeRowSegmentCap = 2;

        /// <summary>Per-room repeat cap for a Large entry in one context (the Medium cap stays
        /// <see cref="MaxSameResrefPerRoomContext"/>).</summary>
        internal const int LargeMaxSameResrefPerRoomContext = 3;

        /// <summary>At most one Large member per composed clutter pile -- a second 3-8m model
        /// inside a 2.8m-radius pile can only interpenetrate the first.</summary>
        private const int LargeMaxPerPile = 1;

        private static readonly (int Dx, int Dy)[] CardinalDirections =
        {
            (1, 0), (-1, 0), (0, 1), (0, -1)
        };

        /// <summary>
        /// Neighbor probe order (cardinals first, then diagonals) used when hunting for a doorway's
        /// mirrored flank-tile pair — deterministic so identical layouts always resolve the same pair.
        /// </summary>
        private static readonly (int Dx, int Dy)[] FlankProbeOrder =
        {
            (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1)
        };

        /// <summary>
        /// Plans the decoration pass for a resolved layout. Deterministic: identical
        /// (layout.Seed, tileset, detail, densityPercent, decorationProfile) always produces an
        /// identical plan, in the same order. Returns an empty plan when the merged palette (tileset
        /// family palette + theme accents) is empty or densityPercent is 0 (the toggle-off case).
        ///
        /// decorationProfile selects a NAMED alternate palette on the tileset (see
        /// DungeonTilesetProfile.DecorationProfiles -- e.g. fcx01's "ruined" destruction palette);
        /// null/empty falls back to the theme's own declared DungeonDetail.DecorationProfile, and an
        /// empty/unknown name means the standard palette.
        ///
        /// Calibration: DungeonDetail.DecorationBaseDensity is evidence-derived as placeables PER TOTAL
        /// AREA TILE (layout.Width * layout.Height) from the hand-built reference areas — see
        /// decoration_evidence/mine_evidence.py's own "density: decorative placeables per tile (area
        /// Width*Height)" convention; this stays theme-owned (it is the overall dressing INTENSITY, not
        /// the palette). The eligible tile POOL this planner can actually decorate (room perimeter cells
        /// with a curated palette bucket) is a much smaller fraction of the total area (corridors
        /// carved outside LayoutRooms, interior room tiles, and every excluded cell are never eligible —
        /// see the class doc comment). Plan() runs two passes over the layout: PASS 1 (no RNG) counts
        /// the real eligible pool size for the wall/corridor "hugging" placements and the RoomCenter
        /// centerpiece slots; PASS 2 derives a per-slot probability (targetCount / eligibleCount, capped
        /// at 1) that makes the EXPECTED realized count converge on
        /// baseDensity * totalTiles * (densityPercent/100), then converts that probability into a
        /// deterministic, evenly-SPACED run (see PlaceWallRuns) rather than an independent per-tile coin
        /// flip — same expected count, real cadence instead of noise.
        /// </summary>
        public static List<PlannedDecoration> Plan(ResolvedLayout layout, DungeonTilesetProfile tileset, DungeonDetail detail, int densityPercent,
            string decorationProfile = null)
        {
            var plan = new List<PlannedDecoration>();
            if (layout == null || detail == null || densityPercent <= 0)
                return plan;

            // Named-profile resolution: an explicit per-request pick wins, then the theme's own
            // declaration; an empty/unknown name is the standard palette. A named profile fully
            // REPLACES the standard tileset Decorations/Vignettes (no merging) -- see
            // DungeonDecorationProfile.
            var profileName = !string.IsNullOrWhiteSpace(decorationProfile) ? decorationProfile : detail.DecorationProfile;
            DungeonDecorationProfile namedProfile = null;
            if (!string.IsNullOrWhiteSpace(profileName) && tileset?.DecorationProfiles != null)
                tileset.DecorationProfiles.TryGetValue(profileName, out namedProfile);

            // Urban placement grammar (see DungeonTilesetProfile.UrbanDressing): bearing alignment,
            // road integrity, facade rows, cargo grids, and pile zone discipline -- active only for
            // tilesets that declare it, so every other family's plan stays byte-identical.
            var urban = tileset?.UrbanDressing == true;
            var organicSpin = namedProfile?.OrganicClutterRotation == true;

            var palette = MergePalette(namedProfile?.Decorations ?? tileset?.Decorations, detail, urban);
            if (palette.Count == 0)
                return plan;

            var densityFraction = densityPercent / 100.0;
            // Dressing intensity is a property of the VISUAL family when the tileset declares its
            // own mined density band (see DungeonTilesetProfile.DecorationDensityPerTile) -- a theme
            // composed onto a city tileset dresses at city density; otherwise the theme's own
            // evidence-derived density applies exactly as before the override existed.
            var baseDensity = tileset != null && tileset.DecorationDensityPerTile > 0
                ? tileset.DecorationDensityPerTile
                : detail.DecorationBaseDensity;
            var targetCount = baseDensity * layout.Width * layout.Height * densityFraction;
            if (targetCount <= 0)
                return plan;

            var vignettes = namedProfile?.Vignettes ?? tileset?.Vignettes ?? new List<DungeonVignette>();
            var roadCrosser = tileset?.RoadCrosser ?? string.Empty;

            var excluded = BuildExclusionSet(layout);

            // PASS 0: structural building-placeable frontage (see BuildingFrontagePlanner) -- the
            // promenade-family canyon mechanism. Runs FIRST on its own RNG stream (independent of
            // this planner's main stream, so every existing mechanism keeps its exact sequence)
            // and publishes its occupied margin cells as PlaceableStructureCells, so the flush/
            // structure-frontage dressing below anchors against placeable buildings exactly as
            // against stamped tile structures. Strictly empty (no allocation visible anywhere)
            // for tilesets that declare no frontage buildings.
            BuildingFrontagePlanner.FrontageResult frontage = null;
            // Reset any stale frontage cells from an earlier Plan over the same layout (e.g. a
            // different tileset/profile re-plan in tests) so repeated planning stays idempotent.
            if (layout.PlaceableStructureCells is { Count: > 0 })
                layout.PlaceableStructureCells = new HashSet<(int X, int Y)>();
            if (urban && tileset?.FrontageBuildings is { Count: > 0 })
            {
                frontage = BuildingFrontagePlanner.PlanFrontage(layout, tileset, excluded, roadCrosser);
                layout.PlaceableStructureCells = frontage.OccupiedCells;
                foreach (var placement in frontage.Placements)
                    plan.Add(placement.Decoration);
            }

            // District flavors (urban grammar only -- see DistrictFlavor and AssignDistrictFlavors):
            // each open room gets a deterministic (no-RNG) neighborhood flavor, and every palette
            // pool below is resolved per flavor, so big cargo concentrates in industrial yards while
            // promenade rooms draw kiosks/benches and plazas draw pillars/monuments. Non-urban
            // tilesets use the single None view, whose pools are built EXACTLY like the pre-district
            // code built them (same lists, same order), keeping every non-city plan byte-identical.
            var flavors = urban
                ? AssignDistrictFlavors(layout, roadCrosser)
                : new Dictionary<int, DistrictFlavor>();

            var views = new Dictionary<DistrictFlavor, PaletteView>
            {
                [DistrictFlavor.None] = BuildPaletteView(palette, DistrictFlavor.None, urban)
            };
            if (urban)
            {
                foreach (var flavor in flavors.Values.Distinct().ToList())
                    views[flavor] = BuildPaletteView(palette, flavor, urban);
            }

            // Precompute each non-set-piece room's shape classification once — reused across every
            // pass so they can never drift out of sync with each other.
            var rooms = new List<RoomState>();
            foreach (var room in layout.Rooms)
            {
                if (room.IsSetPiece || room.Tiles.Count == 0)
                    continue;

                var (minX, maxX, minY, maxY) = BoundingBox(room.Tiles);
                var spanX = maxX - minX + 1;
                var spanY = maxY - minY + 1;
                var isCorridorLike = Math.Min(spanX, spanY) <= CorridorLikeMaxSpan;
                var roomTileSet = new HashSet<(int X, int Y)>(room.Tiles);
                var flavor = urban && flavors.TryGetValue(room.Id, out var assigned) ? assigned : DistrictFlavor.None;
                var view = views[flavor];
                var courtyardAnchor = view.CourtyardsCurated && !isCorridorLike
                    ? FindCourtyardAnchor(room, roomTileSet, excluded, layout, roadCrosser)
                    : null;
                // Mid-room ensemble anchors (urban, non-corridor rooms with a satellite pool):
                // interior tiles the composed civic/commercial arrangements can stand on -- see
                // FindEnsembleAnchors. Empty for every non-urban tileset, so pass-1 counting and
                // budget carving stay byte-identical there.
                var ensembleAnchors = urban && !isCorridorLike &&
                                      flavor is DistrictFlavor.Civic or DistrictFlavor.Commercial &&
                                      view.CourtyardRingEntries is { Count: > 0 }
                    ? FindEnsembleAnchors(room, roomTileSet, excluded, layout, roadCrosser)
                    : new List<(int X, int Y)>();
                rooms.Add(new RoomState
                {
                    Room = room,
                    IsCorridorLike = isCorridorLike,
                    TileSet = roomTileSet,
                    CourtyardAnchor = courtyardAnchor,
                    EnsembleAnchors = ensembleAnchors,
                    Flavor = flavor,
                    View = view
                });
            }

            var tileToRoom = new Dictionary<(int X, int Y), int>();
            for (var i = 0; i < rooms.Count; i++)
            foreach (var tile in rooms[i].Room.Tiles)
                tileToRoom.TryAdd(tile, i);

            // PASS 1: count the eligible pool for each bucket, ignoring RNG entirely — the centerpiece/
            // doorway-flank/vignette anchor tiles' exclusion from the wall pool is intentionally not
            // modeled here (each removes at most one or two tiles per qualifying room/transition,
            // negligible against the pool this normalizes — same precedent as the original single-pass
            // design's centerpiece handling).
            var wallEligibleCount = 0;
            var centerEligibleRoomCount = 0;
            var vignetteEligibleRoomCount = 0;
            var pileAnchorWeightTotal = 0.0;
            var pilesCuratedAnywhere = false;
            var yardEligibleRoomCount = 0;
            var courtyardEligibleRoomCount = rooms.Count(r => r.CourtyardAnchor != null);
            var ensembleAnchorTotal = rooms.Sum(r => r.EnsembleAnchors.Count);
            var depotEligibleRoomCount = 0;
            foreach (var state in rooms)
            {
                var (room, isCorridorLike, tileSet) = (state.Room, state.IsCorridorLike, state.TileSet);
                var view = state.View;
                // Composed depot-block eligibility (urban industrial rooms with a curated crate/
                // cargo clutter pool and a 2+-tile wall segment -- see PlanDepotBlock). Counted so
                // the wall-run budget can be carved honestly; 0 everywhere outside the urban
                // grammar, keeping every non-urban budget split byte-identical.
                // Organic-clutter profiles (ruined collapse debris) never stack neat depot rows --
                // tumbled junk keeps the loose-pile arrangement instead.
                if (urban && !organicSpin && state.Flavor == DistrictFlavor.Industrial && !isCorridorLike &&
                    view.ClutterEntries.Count > 0 &&
                    FindDepotSegment(room, tileSet, excluded, new HashSet<(int X, int Y)>(), layout, roadCrosser) != null)
                    depotEligibleRoomCount++;
                if (view.ClutterEntries.Count > 0)
                {
                    pilesCuratedAnywhere = true;
                    foreach (var tile in room.Tiles)
                    {
                        if (excluded.Contains(tile) || tile == room.CenterTile)
                            continue;
                        if (TileCarriesRoadEdge(tile, layout, roadCrosser))
                            continue;
                        // Urban zone discipline: piles anchor only against walls, structure bases,
                        // and corners -- open plaza centers are reserved for composed courtyards/
                        // centerpieces, never loose junk (see DungeonTilesetProfile.UrbanDressing).
                        if (urban && NearestWallDirection(tile, tileSet) == null && !IsStructureAdjacent(tile, layout))
                            continue;

                        pileAnchorWeightTotal += IsStructureAdjacent(tile, layout) ? PileStructureAnchorWeight : 1.0;
                    }
                }

                // Composed cargo-yard eligibility (urban industrial rooms with curated Huge art --
                // see PlanCargoYard). Corridor-like street rooms never host a yard row.
                if (urban && state.Flavor == DistrictFlavor.Industrial && !isCorridorLike && view.HugeEntries.Count > 0)
                    yardEligibleRoomCount++;

                if (!isCorridorLike && room.Tiles.Count >= MinCenterpieceRoomTiles &&
                    view.ByContext.TryGetValue(DecorationContext.RoomCenter, out var centerEntriesProbe) &&
                    centerEntriesProbe.Count > 0 &&
                    NearestOtherTile(room.CenterTile, room.Tiles, excluded) != null)
                    centerEligibleRoomCount++;

                var roomHasWallTile = false;
                foreach (var tile in room.Tiles)
                {
                    if (excluded.Contains(tile) || tile == room.CenterTile)
                        continue;

                    if (NearestWallDirection(tile, tileSet) == null)
                        continue;

                    if (TryResolveContext(tile, isCorridorLike, layout, roadCrosser, view.ByContext, out _, out var tileEntries))
                    {
                        // Urban road integrity: an on-road tile with no lamp-family entry in its
                        // bucket hosts nothing, so it is not part of the eligible pool either --
                        // keeps the probability calibration honest (see RoadSurfaceEligible).
                        if (urban && RoadSurfaceEligible(tileEntries, tile, layout, roadCrosser).Count == 0)
                            continue;

                        wallEligibleCount++;
                        roomHasWallTile = true;
                    }
                }

                if (roomHasWallTile && vignettes.Count > 0)
                    vignetteEligibleRoomCount++;
            }

            var doorwayFlankPairs = FindDoorwayFlankPairs(layout, excluded, tileToRoom, rooms);

            var centerTarget = targetCount * CenterpieceTargetShare;
            var doorwayFlankTarget = targetCount * DoorwayFlankTargetShare;
            var vignetteTarget = targetCount * VignetteTargetShare;
            // Strictly zero unless a room's palette view curates courtyards AND that room can host
            // one (CourtyardAnchor is only ever computed under a curating view) -- see
            // CourtyardTargetShare's doc comment (palettes without courtyards keep the exact
            // pre-courtyard budget split).
            var courtyardTarget = courtyardEligibleRoomCount > 0
                ? targetCount * CourtyardTargetShare
                : 0.0;
            // Strictly zero unless a room's palette view curates Clutter-role entries AND an anchor
            // tile exists -- palettes without clutter keep the exact pre-pile budget split and RNG
            // stream (same gating convention as courtyards above).
            var pileTarget = pilesCuratedAnywhere && pileAnchorWeightTotal > 0
                ? targetCount * (urban ? UrbanClutterPileTargetShare : ClutterPileTargetShare)
                : 0.0;
            // Mid-room ensemble/depot budget (urban only -- see InteriorEnsembleTargetShare):
            // strictly zero unless the urban grammar is active AND at least one room can host a
            // civic/commercial ensemble or an industrial depot block, so every non-urban palette
            // keeps its exact pre-ensemble budget split and RNG stream.
            var ensembleTarget = urban && (ensembleAnchorTotal > 0 || depotEligibleRoomCount > 0)
                ? targetCount * InteriorEnsembleTargetShare
                : 0.0;
            var wallTarget = Math.Max(0.0, targetCount - centerTarget - doorwayFlankTarget - vignetteTarget - courtyardTarget - pileTarget - ensembleTarget);

            var wallProbability = wallEligibleCount > 0 ? Math.Min(1.0, wallTarget / wallEligibleCount) : 0.0;
            var centerProbability = centerEligibleRoomCount > 0 ? Math.Min(0.95, centerTarget / centerEligibleRoomCount) : 0.0;
            // Each doorway-flank EVENT places two decorations, so the per-pair roll targets half the
            // placement count.
            var doorwayFlankProbability = doorwayFlankPairs.Count > 0
                ? Math.Min(1.0, (doorwayFlankTarget / 2.0) / doorwayFlankPairs.Count)
                : 0.0;
            var vignetteProbability = vignetteEligibleRoomCount > 0 && vignettes.Count > 0
                ? Math.Min(0.9, vignetteTarget / vignetteEligibleRoomCount)
                : 0.0;
            // One courtyard EVENT places 1 centerpiece + a ring (mid-band ring size), so the per-room
            // roll targets the event count, mirroring the doorway-flank pair convention above.
            var courtyardExpectedItems = 1.0 + (CourtyardMinRingItems + CourtyardMaxRingItems) / 2.0;
            var courtyardProbability = courtyardTarget > 0
                ? Math.Min(0.9, (courtyardTarget / courtyardExpectedItems) / courtyardEligibleRoomCount)
                : 0.0;
            // One pile EVENT places (PileMinItems+PileMaxItems)/2 members on average plus the
            // optional decal underneath, so the per-anchor roll targets the event count -- same
            // convention as the courtyard/doorway-pair math above. The probability is per unit of
            // anchor WEIGHT (structure-adjacent anchors count double -- see
            // PileStructureAnchorWeight), so the expected event count still converges on the target
            // while tower bases preferentially collect the piles.
            // The 0.6 per-anchor cap is a saturation guard for layouts whose rooms cover only a
            // small slice of the grid (fcx01 Halls chambers): without it, every chamber tile rolls
            // a pile and the room reads as wall-to-wall junk rather than dressed space.
            var pileExpectedItems = (PileMinItems + PileMaxItems) / 2.0 + DecalUnderPileChance;
            // Urban saturation cap is slightly higher (0.68 vs 0.6): the composed depot/ensemble
            // mechanisms consume anchor tiles, so the remaining pile anchors must convert a bit
            // more often to hold the realized density band (packed20 1.2-1.35); non-urban keeps
            // the original 0.6 guard bit for bit.
            var pileProbability = pileTarget > 0
                ? Math.Min(urban ? 0.68 : 0.6, (pileTarget / pileExpectedItems) / pileAnchorWeightTotal)
                : 0.0;
            // Per-eligible-room roll converging on YardTargetPerArea composed yard events (urban
            // industrial rooms only -- 0 everywhere else, so no non-urban RNG stream changes).
            var yardProbability = yardEligibleRoomCount > 0
                ? Math.Min(0.9, YardTargetPerArea / yardEligibleRoomCount)
                : 0.0;
            // Civic/commercial ensembles roll per interior anchor. The DEPOT floor is deterministic
            // (every eligible industrial room composes at least one block -- the round-9 acceptance
            // gate that pad-singles become the exception in yards), so the anchor-probability math
            // only spreads whatever ensemble budget remains after the depot floor's expected items.
            var depotExpectedItems = (DepotBlockMinItems + DepotBlockMaxItems) / 2.0 + 2.5; // + satellites/pad
            var ensembleExpectedItems = 1.0 + (EnsembleMinSatellites + EnsembleMaxSatellites) / 2.0 + EnsembleBaseDecalChance;
            var ensembleBudgetAfterDepots = Math.Max(0.0, ensembleTarget - depotEligibleRoomCount * depotExpectedItems);
            var ensembleProbability = ensembleAnchorTotal > 0 && ensembleTarget > 0
                ? Math.Min(0.85, (ensembleBudgetAfterDepots / ensembleExpectedItems) / ensembleAnchorTotal)
                : 0.0;
            // A packed layout can starve the roll entirely (many depot rooms eat the share); keep a
            // real floor so civic plazas still compose gardens -- the whole point of the round.
            // 0.5 measured against the packed20 realized-density band: the pile mechanism runs at
            // its saturation cap there, so interior ensembles are what keeps the realized total in
            // the hand-built band.
            if (ensembleAnchorTotal > 0 && ensembleTarget > 0)
                ensembleProbability = Math.Max(ensembleProbability, 0.5);

            var rng = new System.Random(layout.Seed ^ SeedSalt);
            var consumedTiles = new HashSet<(int X, int Y)>();
            // Per-area usage ledger backing DungeonDecorationEntry.MaxPerArea (and the Huge total
            // cap) -- shared by every mechanism below. Entries without caps are never filtered, so
            // palettes that declare no caps keep their exact pick pools.
            var areaUsage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var hugePlacedTotal = 0;

            // PASS 2a: doorway flank pairs, in deterministic transition order, BEFORE the per-room pass
            // so consumedTiles is fully populated before any room decides its own wall-run placements.
            // Each pair draws from the flavor view of the FIRST flank tile's room (deterministic; the
            // single None view for non-urban tilesets).
            foreach (var pair in doorwayFlankPairs)
            {
                var pairView = rooms[tileToRoom[pair.A]].View;
                var doorwayFallback = UnderAreaCap(pairView.DoorwayFallback, areaUsage);
                if (rng.NextDouble() >= doorwayFlankProbability || doorwayFallback == null || doorwayFallback.Count == 0)
                    continue;

                var resref = PickWeighted(doorwayFallback, rng);
                var doorwayEntry = doorwayFallback.FirstOrDefault(e => e.Resref == resref);
                foreach (var tile in new[] { pair.A, pair.B })
                {
                    var roomIndex = tileToRoom[tile];
                    var wallDir = NearestWallDirection(tile, rooms[roomIndex].TileSet);
                    if (wallDir == null)
                        continue;

                    // Urban road integrity: a flank tile on the road ribbon stays clear unless the
                    // picked entry is lamp-family road furniture.
                    if (urban && TileCarriesRoadEdge(tile, layout, roadCrosser) && doorwayEntry?.AllowOnRoadSurface != true)
                        continue;

                    plan.Add(urban
                        ? BuildUrbanWallPlacement(tile, wallDir.Value, resref, DecorationContext.DoorwayFlank, layout, roadCrosser)
                        : BuildWallHuggingPlacement(tile, wallDir.Value, resref, DecorationContext.DoorwayFlank));
                    RecordUse(areaUsage, resref);
                    consumedTiles.Add(tile);
                }
            }

            // PASS 2b0: zone-marking feature tiles (a grass lawn, a fountain court) that landed
            // inside rooms are dressed FIRST -- the obligation is hard (a bare park patch is the
            // reported artifact), their cells are consumed before any other mechanism, and the
            // whole pass is a no-op (zero RNG draws) unless the urban grammar is active AND the
            // tileset declares dressings AND the resolver actually placed such a tile.
            if (urban && tileset?.FeatureTileDressings is { Count: > 0 } &&
                layout.FeatureTileCells is { Count: > 0 })
            {
                PlanZoneDressings(plan, layout, tileset, rooms, tileToRoom, excluded, consumedTiles,
                    roadCrosser, rng, areaUsage);
            }

            // PASS 2b: per-room cargo yard, depot blocks, courtyard, mid-room ensembles,
            // centerpiece, vignette, clutter piles, then rhythmic wall/corridor runs.
            var motifCache = new Dictionary<(int RoomId, DecorationContext Context), List<string>>();

            foreach (var state in rooms)
            {
                var (room, isCorridorLike, tileSet, courtyardAnchor) =
                    (state.Room, state.IsCorridorLike, state.TileSet, state.CourtyardAnchor);
                var view = state.View;
                (int X, int Y)? centerpieceAnchor = null;

                // Composed cargo yard FIRST (urban industrial rooms only -- see PlanCargoYard):
                // building-scale Huge art stands only here, as consecutive-tile rows at shared
                // bearing, and its tiles are consumed before any other mechanism can double-dress
                // them. Bounded by the hard per-area Huge total.
                if (yardProbability > 0 && state.Flavor == DistrictFlavor.Industrial && !isCorridorLike &&
                    view.HugeEntries.Count > 0 && hugePlacedTotal < MaxHugePerArea &&
                    rng.NextDouble() < yardProbability)
                {
                    hugePlacedTotal += PlanCargoYard(
                        plan, room, tileSet, view.HugeEntries, excluded, consumedTiles,
                        layout, roadCrosser, rng, areaUsage, MaxHugePerArea - hugePlacedTotal);
                }

                // Composed DEPOT blocks (urban industrial rooms -- see PlanDepotBlock): the FIRST
                // block is deterministic for every eligible room (dense butt-jointed cargo rows are
                // the industrial default, loose pad-singles the exception -- the round-9 gate); a
                // second block rolls in large yards. Committed blocks damp the room's loose-pile
                // probability (see IndustrialPileDamping).
                var depotBlocksPlaced = 0;
                if (urban && !organicSpin && state.Flavor == DistrictFlavor.Industrial && !isCorridorLike &&
                    view.ClutterEntries.Count > 0)
                {
                    if (PlanDepotBlock(plan, room, tileSet, view, excluded, consumedTiles, layout,
                            roadCrosser, rng, areaUsage))
                        depotBlocksPlaced++;

                    if (depotBlocksPlaced > 0 && room.Tiles.Count >= 12 &&
                        rng.NextDouble() < DepotSecondBlockChance &&
                        PlanDepotBlock(plan, room, tileSet, view, excluded, consumedTiles, layout,
                            roadCrosser, rng, areaUsage))
                        depotBlocksPlaced++;
                }

                // Courtyard next: when one lands, it IS this room's interior arrangement -- the
                // plain single-item centerpiece roll is skipped for the room (its centerpiece slot is
                // the courtyard's own center) so the two interior mechanisms never double-dress the
                // same plaza.
                var courtyardPlaced = false;
                if (courtyardAnchor != null && courtyardProbability > 0 && rng.NextDouble() < courtyardProbability)
                {
                    courtyardPlaced = PlanCourtyard(
                        plan, room, tileSet, courtyardAnchor.Value, view.CourtyardCenterEntries, view.CourtyardRingEntries,
                        excluded, consumedTiles, layout, roadCrosser, rng, urban, areaUsage);
                    if (courtyardPlaced)
                        centerpieceAnchor = courtyardAnchor;
                }

                // Mid-room civic/commercial ensembles (urban -- see PlanInteriorEnsemble): each
                // interior anchor rolls independently; committed ensembles consume their tiles, so
                // an anchor swallowed by the courtyard (or an earlier ensemble) simply skips.
                var ensemblesPlaced = 0;
                if (ensembleProbability > 0 && state.EnsembleAnchors.Count > 0)
                {
                    foreach (var anchor in state.EnsembleAnchors)
                    {
                        if (consumedTiles.Contains(anchor) || anchor == centerpieceAnchor)
                            continue;
                        if (rng.NextDouble() >= ensembleProbability)
                            continue;

                        if (PlanInteriorEnsemble(plan, room, tileSet, anchor, state.Flavor, view,
                                excluded, consumedTiles, layout, roadCrosser, rng, areaUsage))
                        {
                            ensemblesPlaced++;
                            centerpieceAnchor ??= anchor;
                        }
                    }
                }

                if (!courtyardPlaced && ensemblesPlaced == 0 && !isCorridorLike && room.Tiles.Count >= MinCenterpieceRoomTiles &&
                    view.ByContext.TryGetValue(DecorationContext.RoomCenter, out var centerEntries) &&
                    centerEntries.Count > 0 &&
                    rng.NextDouble() < centerProbability)
                {
                    // Never the CenterTile itself — that cell is reserved for boss/treasure/exit
                    // content placement (see DungeonContentPlacer.PopulateBossRoom/PlaceExit) — so
                    // pick the nearest OTHER room tile to stand the centerpiece on instead.
                    var anchor = NearestOtherTile(room.CenterTile, room.Tiles, excluded);
                    // Urban road integrity: the set piece steps off the street ribbon to the nearest
                    // non-road tile instead (the plaza stays intentional, the walkway stays clear).
                    if (urban && anchor != null && TileCarriesRoadEdge(anchor.Value, layout, roadCrosser))
                    {
                        anchor = null;
                        var bestDistSq = int.MaxValue;
                        foreach (var candidate in room.Tiles)
                        {
                            if (candidate == room.CenterTile || excluded.Contains(candidate) ||
                                TileCarriesRoadEdge(candidate, layout, roadCrosser))
                                continue;

                            var dx = candidate.X - room.CenterTile.X;
                            var dy = candidate.Y - room.CenterTile.Y;
                            var distSq = dx * dx + dy * dy;
                            if (distSq < bestDistSq)
                            {
                                bestDistSq = distSq;
                                anchor = candidate;
                            }
                        }
                    }

                    var centerPool = UnderAreaCap(centerEntries, areaUsage);
                    if (anchor != null && centerPool.Count > 0)
                    {
                        if (urban)
                        {
                            // Urban rule (round-9 monument sweep): a plaza set piece NEVER stands
                            // free -- the plain single-item roll composes the same
                            // centerpiece+satellites ensemble the interior anchors use, or places
                            // nothing at all (a monument with no bench/planter court was the
                            // reported "fragments standing free in pairs" artifact).
                            if (PlanInteriorEnsemble(plan, room, tileSet, anchor.Value, state.Flavor, view,
                                    excluded, consumedTiles, layout, roadCrosser, rng, areaUsage))
                                centerpieceAnchor = anchor;
                        }
                        else
                        {
                            centerpieceAnchor = anchor;
                            var resref = PickWeighted(centerPool, rng);
                            var flat = TileCenter(anchor.Value.X, anchor.Value.Y);
                            var angle = rng.NextDouble() * Math.PI * 2.0;
                            var jitter = (float)(rng.NextDouble() * CenterOffset);
                            var position = new Vector3(
                                flat.X + (float)Math.Cos(angle) * jitter,
                                flat.Y + (float)Math.Sin(angle) * jitter,
                                0f);

                            plan.Add(new PlannedDecoration
                            {
                                Resref = resref,
                                Position = position,
                                Facing = (float)(rng.NextDouble() * 360.0),
                                Context = DecorationContext.RoomCenter
                            });
                            RecordUse(areaUsage, resref);
                        }
                    }
                }

                if (vignettes.Count > 0 && rng.NextDouble() < vignetteProbability)
                {
                    var anchor = FindVignetteAnchor(room, tileSet, excluded, consumedTiles, centerpieceAnchor,
                        urban ? layout : null, roadCrosser);
                    if (anchor != null)
                    {
                        var vignette = PickWeightedVignette(vignettes, rng);
                        var wallDir = NearestWallDirection(anchor.Value, tileSet)!.Value;
                        PlaceVignette(plan, anchor.Value, wallDir, vignette);
                        foreach (var member in vignette.Members)
                            RecordUse(areaUsage, member.Resref);
                        consumedTiles.Add(anchor.Value);
                    }
                }

                // Clutter piles: the dominant hand-built arrangement (see ClutterPileTargetShare).
                // Runs BEFORE the wall runs so a piled tile is consumed and never double-dressed by
                // a run placement. Anchors roll per eligible tile in the room's own stored
                // (deterministic) order; the room's junk motif is drawn lazily on its first
                // successful roll so a room with no pile consumes no motif RNG.
                // Rooms whose cargo statement is already composed depot rows keep loose piles as
                // the EXCEPTION (see IndustrialPileDamping); everywhere else the pile probability
                // is untouched.
                var roomPileProbability = urban && depotBlocksPlaced > 0
                    ? pileProbability * IndustrialPileDamping
                    : pileProbability;
                if (roomPileProbability > 0 && view.ClutterEntries.Count > 0)
                {
                    List<DungeonDecorationEntry> junkMotif = null;
                    foreach (var tile in room.Tiles)
                    {
                        if (excluded.Contains(tile) || tile == room.CenterTile || tile == centerpieceAnchor ||
                            consumedTiles.Contains(tile))
                            continue;
                        if (TileCarriesRoadEdge(tile, layout, roadCrosser))
                            continue;
                        // Urban zone discipline (mirrors the PASS-1 pool count): no free-floating
                        // junk in plaza centers -- piles need a wall, structure base, or corner.
                        if (urban && NearestWallDirection(tile, tileSet) == null && !IsStructureAdjacent(tile, layout))
                            continue;

                        var anchorWeight = IsStructureAdjacent(tile, layout) ? PileStructureAnchorWeight : 1.0;
                        if (rng.NextDouble() >= Math.Min(0.95, roomPileProbability * anchorWeight))
                            continue;

                        // The room's junk pool draws only from entries still under their
                        // per-area caps (a capped-out backbone crate wastes a pool slot AND every
                        // pick that lands on it), falling back to the unfiltered list only if
                        // literally everything is capped. No-op without caps (non-urban).
                        if (junkMotif == null)
                        {
                            var availableClutter = UnderAreaCap(view.ClutterEntries, areaUsage);
                            junkMotif = PickJunkMotif(
                                availableClutter.Count > 0 ? availableClutter : view.ClutterEntries, rng);
                        }
                        if (PlanClutterPile(plan, tile, tileSet, junkMotif, view.DecalEntries, rng,
                                urban, organicSpin, IsStructureAdjacent(tile, layout), areaUsage) &&
                            !IsStructureAdjacent(tile, layout))
                        {
                            // A piled tile is consumed so the wall runs never double-dress it --
                            // EXCEPT structure-frontage tiles: hand-built building frontages layer
                            // wall lamps and frontage containers OVER their junk (the
                            // StructureAdjacent bucket must keep dressing tower bases even when a
                            // pile landed there first, since structure anchors preferentially
                            // collect piles via PileStructureAnchorWeight).
                            consumedTiles.Add(tile);
                        }
                    }
                }

                PlaceWallRuns(plan, room, tileSet, isCorridorLike, layout, roadCrosser, view.ByContext, excluded,
                    consumedTiles, centerpieceAnchor, wallProbability, rng, motifCache, urban, areaUsage);
            }

            // FINAL PASS: wall-mounted facade dressing (see BuildingFrontagePlanner.
            // PlanFacadeMounts) -- signs/holo panels hung on building faces (frontage placeables
            // and stamped tile structures) at mined height bands. Own RNG stream, after every
            // ground mechanism, so nothing upstream shifts; a no-op for every tileset without
            // declared FacadeMounts.
            if (urban && tileset?.FacadeMounts is { Count: > 0 })
                plan.AddRange(BuildingFrontagePlanner.PlanFacadeMounts(layout, tileset, frontage, plan.Count));

            return plan;
        }

        /// <summary>
        /// Per-room planning state: shape classification plus the room's district flavor and the
        /// flavor-resolved palette view every mechanism draws from (see <see cref="PaletteView"/>).
        /// </summary>
        private sealed class RoomState
        {
            public LayoutRoom Room;
            public bool IsCorridorLike;
            public HashSet<(int X, int Y)> TileSet;
            public (int X, int Y)? CourtyardAnchor;
            /// <summary>Interior anchors eligible for mid-room civic/commercial ensembles (urban
            /// only; empty otherwise) -- see FindEnsembleAnchors.</summary>
            public List<(int X, int Y)> EnsembleAnchors = new();
            public DistrictFlavor Flavor;
            public PaletteView View;
        }

        /// <summary>
        /// A merged palette resolved for one <see cref="DistrictFlavor"/>: role-split pools (decals/
        /// clutter/context buckets/courtyard buckets/doorway fallback) with each entry's effective
        /// weight under that flavor (see DungeonDecorationEntry.DistrictWeights), and -- under the
        /// urban grammar -- the <see cref="DecorationSize.Huge"/> entries stripped out of every
        /// generic pool into their own yard-only list. The None view reproduces the pre-district
        /// pools exactly (same entries, same order, base weights), so non-urban plans stay
        /// byte-identical.
        /// </summary>
        private sealed class PaletteView
        {
            public Dictionary<DecorationContext, List<DungeonDecorationEntry>> ByContext;
            public List<DungeonDecorationEntry> ClutterEntries;
            public List<DungeonDecorationEntry> DecalEntries;
            public List<DungeonDecorationEntry> CourtyardCenterEntries;
            public List<DungeonDecorationEntry> CourtyardRingEntries;
            public bool CourtyardsCurated;
            public List<DungeonDecorationEntry> HugeEntries;
            public List<DungeonDecorationEntry> DoorwayFallback;
        }

        private static PaletteView BuildPaletteView(List<DungeonDecorationEntry> palette, DistrictFlavor flavor, bool urban)
        {
            List<DungeonDecorationEntry> effective;
            var huge = new List<DungeonDecorationEntry>();
            if (!urban)
            {
                // Non-urban: the single None view uses the merged palette verbatim -- district
                // metadata and size classes are inert (byte-identical pre-district behavior).
                effective = palette;
            }
            else
            {
                effective = new List<DungeonDecorationEntry>();
                foreach (var entry in palette)
                {
                    var weight = entry.DistrictWeights.Count == 0
                        ? entry.Weight
                        : entry.DistrictWeights.GetValueOrDefault(flavor);
                    if (weight <= 0)
                        continue;

                    var resolved = weight == entry.Weight ? entry : CloneWithWeight(entry, weight);
                    // Building-scale art never reaches a generic pool: the composed cargo-yard
                    // mechanism is the only Huge emitter (see PlanCargoYard).
                    if (entry.Size == DecorationSize.Huge)
                        huge.Add(resolved);
                    else
                        effective.Add(resolved);
                }
            }

            // Role-based pools (see DecorationRole): ground decals are NEVER placed stand-alone by
            // the run/centerpiece/flank mechanisms -- they only exist layered under clutter piles or
            // as courtyard centers -- so they are stripped from the context buckets entirely (except
            // the courtyard-center bucket, whose arrangement composes clutter on top -- see
            // PlanCourtyard). Landmark entries (vehicles, monuments) are stripped from the
            // RoomCenter/WallAdjacent buckets so a large narrative one-off can never float alone in
            // the middle of an open plaza -- they remain placeable road-side, structure-anchored,
            // at doorway flanks, or as curated vignette members.
            var byContext = effective
                .Where(d => d.Role != DecorationRole.GroundDecal || d.Context == DecorationContext.CourtyardCenter)
                .Where(d => d.Role != DecorationRole.Landmark ||
                            d.Context is not (DecorationContext.RoomCenter or DecorationContext.WallAdjacent))
                .GroupBy(d => d.Context)
                .ToDictionary(g => g.Key, g => g.ToList());

            byContext.TryGetValue(DecorationContext.CourtyardCenter, out var courtyardCenterEntries);
            byContext.TryGetValue(DecorationContext.Courtyard, out var courtyardRingEntries);

            // Flush-anchored entries never flank doorways free-standing (see
            // DecorationAnchoring.WallFlush) -- a no-op Where for every palette without them.
            byContext.TryGetValue(DecorationContext.DoorwayFlank, out var doorwayEntries);
            var doorwayFallback = doorwayEntries is { Count: > 0 }
                ? doorwayEntries
                : byContext.GetValueOrDefault(DecorationContext.WallAdjacent);
            doorwayFallback = doorwayFallback?.Where(e => e.Anchoring != DecorationAnchoring.WallFlush).ToList();

            return new PaletteView
            {
                ByContext = byContext,
                ClutterEntries = effective.Where(d => d.Role == DecorationRole.Clutter).ToList(),
                DecalEntries = effective.Where(d => d.Role == DecorationRole.GroundDecal).ToList(),
                CourtyardCenterEntries = courtyardCenterEntries,
                CourtyardRingEntries = courtyardRingEntries,
                CourtyardsCurated = courtyardCenterEntries is { Count: > 0 } && courtyardRingEntries is { Count: > 0 },
                HugeEntries = huge,
                DoorwayFallback = doorwayFallback
            };
        }

        private static DungeonDecorationEntry CloneWithWeight(DungeonDecorationEntry entry, int weight)
        {
            return new DungeonDecorationEntry
            {
                Resref = entry.Resref,
                Weight = weight,
                Context = entry.Context,
                Role = entry.Role,
                Anchoring = entry.Anchoring,
                AllowOnRoadSurface = entry.AllowOnRoadSurface,
                Size = entry.Size,
                DistrictWeights = entry.DistrictWeights,
                MaxPerArea = entry.MaxPerArea
            };
        }

        /// <summary>Records one placement of <paramref name="resref"/> against the per-area usage
        /// ledger backing <see cref="DungeonDecorationEntry.MaxPerArea"/>.</summary>
        private static void RecordUse(Dictionary<string, int> areaUsage, string resref)
        {
            areaUsage[resref] = areaUsage.GetValueOrDefault(resref) + 1;
        }

        /// <summary>Per-room repeat cap for one context: Large art repeats less (see
        /// <see cref="LargeMaxSameResrefPerRoomContext"/>); everything else keeps
        /// <see cref="MaxSameResrefPerRoomContext"/>.</summary>
        private static int RoomContextCap(DungeonDecorationEntry entry)
        {
            return entry is { Size: DecorationSize.Large } ? LargeMaxSameResrefPerRoomContext : MaxSameResrefPerRoomContext;
        }

        /// <summary>True when the entry declares a per-area cap and the ledger has reached it.</summary>
        private static bool IsAtAreaCap(DungeonDecorationEntry entry, Dictionary<string, int> areaUsage)
        {
            return entry != null && entry.MaxPerArea > 0 && areaUsage.GetValueOrDefault(entry.Resref) >= entry.MaxPerArea;
        }

        /// <summary>
        /// Filters a pick pool down to entries still under their per-area cap (see
        /// DungeonDecorationEntry.MaxPerArea). Returns the ORIGINAL list unchanged when no entry
        /// declares a cap, so palettes without caps keep their exact pick pools (and RNG streams).
        /// </summary>
        private static List<DungeonDecorationEntry> UnderAreaCap(
            List<DungeonDecorationEntry> entries, Dictionary<string, int> areaUsage)
        {
            if (entries == null || entries.All(e => e.MaxPerArea <= 0))
                return entries;

            return entries
                .Where(e => e.MaxPerArea <= 0 || areaUsage.GetValueOrDefault(e.Resref) < e.MaxPerArea)
                .ToList();
        }

        /// <summary>
        /// Deterministically (no RNG) assigns each open, non-set-piece room a district flavor --
        /// the round-8 realism mechanism: hand-built city repetition is district-scoped, so a
        /// generated city needs recognizable neighborhoods rather than one uniform mix. Scoring
        /// mirrors how the hand-built areas ARE laid out:
        ///  - COMMERCIAL prefers road-frontage rooms near the entrances (promenades line the
        ///    walkways players actually travel);
        ///  - INDUSTRIAL prefers rooms away from the entrance with stamped-structure mass and
        ///    little road frontage (shipyards/docks sit behind the city, against the big
        ///    buildings);
        ///  - CIVIC prefers large rooms with a real interior (plazas that can host courtyards).
        /// A balancing pass then guarantees each flavor is represented when enough rooms exist
        /// (>= 3 non-corridor rooms), flipping the best-scoring candidates without ever emptying
        /// another flavor -- so every generated city reads as distinct neighborhoods.
        /// </summary>
        internal static Dictionary<int, DistrictFlavor> AssignDistrictFlavors(ResolvedLayout layout, string roadCrosser)
        {
            var result = new Dictionary<int, DistrictFlavor>();
            var rooms = new List<(LayoutRoom Room, bool IsCorridorLike, HashSet<(int X, int Y)> TileSet)>();
            foreach (var room in layout.Rooms)
            {
                if (room.IsSetPiece || room.Tiles.Count == 0)
                    continue;

                var (minX, maxX, minY, maxY) = BoundingBox(room.Tiles);
                var isCorridorLike = Math.Min(maxX - minX + 1, maxY - minY + 1) <= CorridorLikeMaxSpan;
                rooms.Add((room, isCorridorLike, new HashSet<(int X, int Y)>(room.Tiles)));
            }

            if (rooms.Count == 0)
                return result;

            var maxDim = Math.Max(1, Math.Max(layout.Width, layout.Height));
            var maxRoomTiles = Math.Max(1, rooms.Max(r => r.Room.Tiles.Count));

            var scores = new Dictionary<int, (double Industrial, double Commercial, double Civic, bool IsCorridorLike)>();
            foreach (var (room, isCorridorLike, tileSet) in rooms)
            {
                var roadTiles = 0;
                var structTiles = 0;
                var interiorTiles = 0;
                foreach (var tile in room.Tiles)
                {
                    if (IsRoadAdjacent(tile, layout, roadCrosser))
                        roadTiles++;
                    // Deliberately STAMPED-only (not IsStructureAdjacent): district identity
                    // derives from tile structures and road frontage. Placeable frontage
                    // buildings (BuildingFrontagePlanner) wall commercial promenades and civic
                    // plazas alike -- counting them here would skew every walled room industrial.
                    if (IsWithin1(tile, layout?.StampedStructureTiles))
                        structTiles++;
                    if (NearestWallDirection(tile, tileSet) == null)
                        interiorTiles++;
                }

                var n = Math.Max(1, room.Tiles.Count);
                var roadFrac = (double)roadTiles / n;
                var structFrac = (double)structTiles / n;
                var interiorFrac = (double)interiorTiles / n;
                var entranceDist = layout.Transitions.Count > 0
                    ? layout.Transitions.Min(t => Chebyshev(room.CenterTile, t.Tile)) / (double)maxDim
                    : 0.5;
                var sizeNorm = (double)room.Tiles.Count / maxRoomTiles;

                var commercial = 2.0 * roadFrac + 0.4 * (1.0 - entranceDist);
                var industrial = 1.2 * structFrac + 1.6 * entranceDist + 0.5 * (1.0 - roadFrac);
                var civic = 2.5 * interiorFrac + 0.8 * sizeNorm + 0.3 * (1.0 - structFrac);
                scores[room.Id] = (industrial, commercial, civic, isCorridorLike);

                // Argmax with a deterministic tie order (Commercial first: streets are the most
                // common generated shape, and a tie means the room fronts a road anyway).
                var best = DistrictFlavor.Commercial;
                var bestScore = commercial;
                if (industrial > bestScore)
                {
                    best = DistrictFlavor.Industrial;
                    bestScore = industrial;
                }
                if (civic > bestScore)
                    best = DistrictFlavor.Civic;

                result[room.Id] = best;
            }

            // Balancing pass: with enough real rooms, every flavor should exist somewhere -- a city
            // of only warehouses is as monotonous as the round-7 uniform mix. Flip the best-scoring
            // candidate into each missing flavor, never emptying another flavor below one room.
            var nonCorridorCount = rooms.Count(r => !r.IsCorridorLike);
            if (nonCorridorCount >= 3)
            {
                foreach (var missing in new[] { DistrictFlavor.Commercial, DistrictFlavor.Industrial, DistrictFlavor.Civic })
                {
                    if (result.Values.Contains(missing))
                        continue;

                    var candidates = rooms
                        .Where(r => !r.IsCorridorLike)
                        .Where(r => result.Values.Count(f => f == result[r.Room.Id]) >= 2)
                        .ToList();
                    if (candidates.Count == 0)
                        continue;

                    var pick = candidates
                        .OrderByDescending(r => missing switch
                        {
                            DistrictFlavor.Industrial => scores[r.Room.Id].Industrial,
                            DistrictFlavor.Commercial => scores[r.Room.Id].Commercial,
                            _ => scores[r.Room.Id].Civic
                        })
                        .ThenBy(r => r.Room.Id)
                        .First();
                    result[pick.Room.Id] = missing;
                }
            }

            return result;
        }

        /// <summary>
        /// Composes one industrial cargo-yard row: 2-3 copies of ONE Huge building-scale model
        /// (a storage silo, industrial tower, or parked craft) standing on CONSECUTIVE wall tiles
        /// of an industrial-flavor room, all sharing the wall's outward bearing -- the hand-built
        /// shipyard/dock pattern (silo rows at 10m tile pitch, kyru08 same-resref NN median
        /// 10.01m) that concentrated cargo used to blanket the whole map instead. Landmark-role
        /// Huge entries (a parked starfighter) place as a single one-off rather than a row. Row
        /// tiles are consumed. Returns the number of Huge placements emitted (0 when no run of
        /// consecutive eligible wall tiles exists).
        /// </summary>
        private static int PlanCargoYard(
            List<PlannedDecoration> plan, LayoutRoom room, HashSet<(int X, int Y)> tileSet,
            List<DungeonDecorationEntry> hugeEntries, HashSet<(int X, int Y)> excluded,
            HashSet<(int X, int Y)> consumedTiles, ResolvedLayout layout, string roadCrosser,
            System.Random rng, Dictionary<string, int> areaUsage, int remainingBudget)
        {
            var pool = UnderAreaCap(hugeEntries, areaUsage);
            if (pool == null || pool.Count == 0 || remainingBudget <= 0)
                return 0;

            var resref = PickWeighted(pool, rng);
            var entry = pool.First(e => e.Resref == resref);

            var desired = entry.Role == DecorationRole.Landmark
                ? 1
                : YardRowMinItems + rng.Next(YardRowMaxItems - YardRowMinItems + 1);
            desired = Math.Min(desired, remainingBudget);
            if (entry.MaxPerArea > 0)
                desired = Math.Min(desired, entry.MaxPerArea - areaUsage.GetValueOrDefault(resref));
            // A budget squeeze must never strand a lone silo: a non-landmark yard row exists as a
            // pair minimum or not at all (the composed-row contract).
            if (desired <= 0 || (entry.Role != DecorationRole.Landmark && desired < YardRowMinItems))
                return 0;

            // Bucket eligible wall tiles by quantized wall direction (the same straight-run notion
            // PlaceWallRuns uses), then find the longest run of CONSECUTIVE tiles -- a yard row
            // must read as one deliberate line, not scattered singles.
            var byDirection = new Dictionary<int, List<(int X, int Y)>>();
            foreach (var tile in room.Tiles)
            {
                if (excluded.Contains(tile) || tile == room.CenterTile || consumedTiles.Contains(tile))
                    continue;
                if (TileCarriesRoadEdge(tile, layout, roadCrosser))
                    continue;

                var wallDir = NearestWallDirection(tile, tileSet);
                if (wallDir == null)
                    continue;

                var direction = QuantizeDirection(wallDir.Value.Dx, wallDir.Value.Dy);
                if (!byDirection.TryGetValue(direction, out var list))
                    byDirection[direction] = list = new List<(int X, int Y)>();
                list.Add(tile);
            }

            List<(int X, int Y)> bestSegment = null;
            var bestDirection = 0;
            foreach (var (direction, tiles) in byDirection.OrderBy(kv => kv.Key))
            {
                // A wall facing +/-X runs along Y; +/-Y runs along X (PlaceWallRuns' own convention).
                var alongY = direction is 0 or 1;
                var ordered = alongY
                    ? tiles.OrderBy(t => t.X).ThenBy(t => t.Y).ToList()
                    : tiles.OrderBy(t => t.Y).ThenBy(t => t.X).ToList();

                var segment = new List<(int X, int Y)>();
                for (var i = 0; i < ordered.Count; i++)
                {
                    var contiguous = segment.Count > 0 &&
                                     (alongY
                                         ? ordered[i].X == segment[^1].X && ordered[i].Y == segment[^1].Y + 1
                                         : ordered[i].Y == segment[^1].Y && ordered[i].X == segment[^1].X + 1);
                    if (!contiguous)
                    {
                        if (bestSegment == null || segment.Count > bestSegment.Count)
                        {
                            bestSegment = new List<(int X, int Y)>(segment);
                            bestDirection = direction;
                        }
                        segment.Clear();
                    }

                    segment.Add(ordered[i]);
                }

                if (bestSegment == null || segment.Count > bestSegment.Count)
                {
                    bestSegment = new List<(int X, int Y)>(segment);
                    bestDirection = direction;
                }
            }

            var minimum = entry.Role == DecorationRole.Landmark ? 1 : YardRowMinItems;
            if (bestSegment == null || bestSegment.Count < minimum)
                return 0;

            var count = Math.Min(desired, bestSegment.Count);
            var (dx, dy) = bestDirection switch
            {
                0 => (1f, 0f),
                1 => (-1f, 0f),
                2 => (0f, 1f),
                _ => (0f, -1f)
            };
            var facing = CardinalFacing(-dx, -dy);

            var placed = 0;
            for (var i = 0; i < count; i++)
            {
                var tile = bestSegment[i];
                var flat = TileCenter(tile.X, tile.Y);
                plan.Add(new PlannedDecoration
                {
                    Resref = resref,
                    Position = new Vector3(flat.X + dx * PileWallOffset, flat.Y + dy * PileWallOffset, 0f),
                    // Shared bearing: the whole row faces away from its wall, into the yard.
                    Facing = facing,
                    Context = DecorationContext.CargoYard
                });
                RecordUse(areaUsage, resref);
                consumedTiles.Add(tile);
                placed++;
            }

            return placed;
        }

        /// <summary>
        /// Merges the tileset family's bulk palette (the standard Decorations list, or a selected
        /// named profile's replacement list) with the theme's small accent list
        /// (DungeonDetail.Decorations) into one weighted pool per DecorationContext. The tileset
        /// supplies the visual bulk; the theme layers a few genuinely-theme-flavored extras on top —
        /// neither source alone need be exhaustive.
        ///
        /// Semantic anchoring guards (see <see cref="DecorationAnchoring"/>):
        ///  - Entries classified Excluded (whole building fragments, zero-evidence blueprints) or
        ///    RunSegment (fence segments/gates -- no run-composition mechanism exists; see the enum
        ///    doc for the measured model-width rationale) are stripped from BOTH sources outright,
        ///    so a misclassified curation can never reach a plan.
        ///  - Under a tileset's URBAN grammar (<paramref name="urban"/>), theme accents must pass
        ///    the same hand-built-evidence bar as the family palette itself: an accent resref the
        ///    tileset's own mined palette never curates is dropped for that composition (the July
        ///    2026 fcx01 review's Sith altar/monument standing in a clean sci-fi plaza). Non-urban
        ///    tilesets keep every accent exactly as before -- semantic gating is opt-in per family
        ///    (only fcx01 declares it), so every non-city plan stays byte-identical.
        /// </summary>
        internal static List<DungeonDecorationEntry> MergePalette(
            List<DungeonDecorationEntry> tilesetDecorations, DungeonDetail detail, bool urban)
        {
            static bool Placeable(DungeonDecorationEntry e) =>
                e.Anchoring is not (DecorationAnchoring.Excluded or DecorationAnchoring.RunSegment);

            var merged = new List<DungeonDecorationEntry>();
            if (tilesetDecorations != null)
                merged.AddRange(tilesetDecorations.Where(Placeable));

            if (detail?.Decorations != null)
            {
                var accents = detail.Decorations.Where(Placeable);
                if (urban)
                {
                    var curated = new HashSet<string>(
                        (tilesetDecorations ?? new List<DungeonDecorationEntry>()).Select(e => e.Resref),
                        StringComparer.OrdinalIgnoreCase);
                    accents = accents.Where(e => curated.Contains(e.Resref));
                }

                merged.AddRange(accents);
            }

            return merged;
        }

        /// <summary>
        /// Finds every transition's mirrored doorway-flank tile pair: two DIFFERENT wall-eligible,
        /// non-excluded room tiles within Chebyshev distance 1 of the transition's anchor that reflect
        /// each other across it (tileA + tileB == 2 * anchor) — the "either side of the doorway" shape
        /// a symmetric flank needs. A transition with no such pair (most corridor-end/alcove doorways)
        /// contributes nothing; PlanDoorwayFlanks never places a single lopsided flank.
        /// </summary>
        private static List<(int TransitionIndex, (int X, int Y) A, (int X, int Y) B)> FindDoorwayFlankPairs(
            ResolvedLayout layout, HashSet<(int X, int Y)> excluded, Dictionary<(int X, int Y), int> tileToRoom,
            List<RoomState> rooms)
        {
            var results = new List<(int, (int X, int Y), (int X, int Y))>();

            for (var t = 0; t < layout.Transitions.Count; t++)
            {
                var transition = layout.Transitions[t];
                var anchor = transition.Style is TransitionStyle.Door or TransitionStyle.GroupExit
                    ? transition.DoorwayCell
                    : transition.Tile;

                var candidates = new List<(int X, int Y)>();
                foreach (var (dx, dy) in FlankProbeOrder)
                {
                    var candidate = (anchor.X + dx, anchor.Y + dy);
                    if (excluded.Contains(candidate) || !tileToRoom.TryGetValue(candidate, out var roomIndex))
                        continue;
                    if (NearestWallDirection(candidate, rooms[roomIndex].TileSet) == null)
                        continue;
                    candidates.Add(candidate);
                }

                (int X, int Y)? bestA = null, bestB = null;
                foreach (var candidate in candidates)
                {
                    var mirror = (2 * anchor.X - candidate.X, 2 * anchor.Y - candidate.Y);
                    if (mirror == candidate || !candidates.Contains(mirror))
                        continue;

                    bestA = candidate;
                    bestB = mirror;
                    break;
                }

                if (bestA != null && bestB != null)
                    results.Add((t, bestA.Value, bestB.Value));
            }

            return results;
        }

        /// <summary>
        /// Groups a room's eligible tiles for one DecorationContext into straight wall/corridor "runs"
        /// (one group per quantized wall-facing direction, ordered along the run) and dresses each run
        /// at a JITTERED spacing derived from wallProbability, split into segments of at most
        /// <see cref="MaxRunSegmentLength"/> separated by a real gap — "an irregular cluster of a few
        /// fixtures along part of a wall" instead of either an independent per-tile coin flip OR one
        /// perfectly even run wrapping the room's entire perimeter (the reported "ring" artifact).
        /// Each run draws only from that room's own small motif set (see PickMotif) for internal
        /// consistency, additionally capped per-resref by <see cref="MaxSameResrefPerRoomContext"/>
        /// across the WHOLE room (not just one run/side) via a usage-count table scoped to this one
        /// call (PlaceWallRuns runs once per room), so multiple wall-direction buckets in the same
        /// room/context share one budget instead of each independently maxing out the same fixture.
        /// </summary>
        private static void PlaceWallRuns(
            List<PlannedDecoration> plan, LayoutRoom room, HashSet<(int X, int Y)> tileSet, bool isCorridorLike,
            ResolvedLayout layout, string roadCrosser, Dictionary<DecorationContext, List<DungeonDecorationEntry>> byContext,
            HashSet<(int X, int Y)> excluded, HashSet<(int X, int Y)> consumedTiles,
            (int X, int Y)? centerpieceAnchor, double wallProbability, System.Random rng,
            Dictionary<(int RoomId, DecorationContext Context), List<string>> motifCache, bool urban,
            Dictionary<string, int> areaUsage)
        {
            if (wallProbability <= 0)
                return;

            // Bucket this room's eligible tiles by (context, quantized wall direction) — each bucket is
            // one straight run. Iterate room.Tiles in its own stored (deterministic) order so bucket
            // membership order, and therefore the whole pass, stays reproducible per seed. Under the
            // urban grammar, on-road tiles form their own lamp-line bucket (only AllowOnRoadSurface
            // entries may stand on the street ribbon -- see RoadSurfaceEligible); OnRoad is always
            // false otherwise, so non-urban bucketing is unchanged.
            var runs = new Dictionary<(DecorationContext Context, int Direction, bool OnRoad), List<(int X, int Y)>>();

            foreach (var tile in room.Tiles)
            {
                if (excluded.Contains(tile) || tile == room.CenterTile || tile == centerpieceAnchor || consumedTiles.Contains(tile))
                    continue;

                var wallDir = NearestWallDirection(tile, tileSet);
                if (wallDir == null)
                    continue;

                if (!TryResolveContext(tile, isCorridorLike, layout, roadCrosser, byContext, out var context, out var tileEntries))
                    continue;

                var onRoad = false;
                if (urban && TileCarriesRoadEdge(tile, layout, roadCrosser))
                {
                    if (!tileEntries.Any(e => e.AllowOnRoadSurface))
                        continue;
                    onRoad = true;
                }

                var direction = QuantizeDirection(wallDir.Value.Dx, wallDir.Value.Dy);
                var key = (context, direction, onRoad);
                if (!runs.TryGetValue(key, out var list))
                {
                    list = new List<(int X, int Y)>();
                    runs[key] = list;
                }
                list.Add(tile);
            }

            var spacing = Math.Max(1, (int)Math.Round(1.0 / wallProbability));

            // Shared across every (context, direction) bucket in THIS room, so e.g. all four walls of
            // one open room draw down the SAME per-resref budget instead of each independently maxing
            // out — see MaxSameResrefPerRoomContext's doc comment.
            var resrefUsageCounts = new Dictionary<(DecorationContext Context, string Resref), int>();

            // A SECOND small motif per (room, context), drawn lazily only once the primary motif's
            // per-resref budget (MaxSameResrefPerRoomContext) runs out — "second motif ... filling
            // beyond the cap" per the round-3 decoration-quality brief, rather than falling back to
            // the room's entire palette bucket (which would blow past the hand-built-evidence-backed
            // "a room repeats a small handful of fixture types" pattern PickMotif exists to encode).
            var secondaryMotifCache = new Dictionary<DecorationContext, List<string>>();

            foreach (var ((context, direction, onRoad), tiles) in runs
                         .OrderBy(kv => kv.Key.Direction).ThenBy(kv => kv.Key.Context).ThenBy(kv => kv.Key.OnRoad))
            {
                // Sort along the run's own axis: a wall facing +/-X runs along Y, a wall facing +/-Y
                // runs along X — this is what makes the run a real straight line rather than an
                // arbitrary tile order.
                var ordered = direction is 0 or 1
                    ? tiles.OrderBy(t => t.Y).ThenBy(t => t.X).ToList()
                    : tiles.OrderBy(t => t.X).ThenBy(t => t.Y).ToList();

                if (!byContext.TryGetValue(context, out var entries) || entries.Count == 0)
                    entries = byContext.GetValueOrDefault(DecorationContext.WallAdjacent);
                if (entries == null || entries.Count == 0)
                    continue;

                // A lamp-line bucket draws exclusively from the road-surface-allowed subset; the
                // bucketing above guarantees it is non-empty for at least the resolved context, but
                // the WallAdjacent fallback may not curate any -- skip then.
                if (onRoad)
                {
                    entries = entries.Where(e => e.AllowOnRoadSurface).ToList();
                    if (entries.Count == 0)
                        continue;
                }

                var motifKey = (room.Id, context);
                if (!motifCache.TryGetValue(motifKey, out var motif))
                {
                    motif = PickMotif(entries, rng);
                    motifCache[motifKey] = motif;
                }

                var motifEntries = entries.Where(e => motif.Contains(e.Resref)).ToList();
                if (motifEntries.Count == 0)
                    motifEntries = entries;

                if (!secondaryMotifCache.TryGetValue(context, out var secondaryMotif))
                {
                    var remaining = entries.Where(e => !motif.Contains(e.Resref)).ToList();
                    secondaryMotif = remaining.Count > 0 ? PickMotif(remaining, rng) : new List<string>();
                    secondaryMotifCache[context] = secondaryMotif;
                }
                var secondaryMotifEntries = entries.Where(e => secondaryMotif.Contains(e.Resref)).ToList();

                // Jittered, segmented walk along the run instead of a perfectly even `i += spacing`
                // stride: hand-built reference wall/corridor spacing has a real coefficient of
                // variation (~0.5-1.4 across mined families, see decoration_evidence/ round-3
                // statistics harness) — machine-even spacing is itself part of what reads as
                // artificial. jitterRange = spacing gives a uniform-distributed step with CV ~0.58, a
                // conservative move toward that measured irregularity without risking a degenerate
                // (near-zero or negative) step.
                //
                // URBAN EXCEPTION (facade rows): hand-built CITY frontages are the opposite -- the
                // strongest "designed city" signal in the fcx01 reference areas is an evenly-spaced
                // row of one repeated fixture sharing one bearing (same-resref groups share a
                // dominant 15-degree orientation bin at a median 50% share). Urban buckets therefore
                // walk at the EXACT spacing and repeat a single row resref per segment, with a real
                // gap and a fresh resref between segments.
                var jitterRange = Math.Max(1, spacing);
                var i = rng.Next(spacing);
                var segmentLength = 0;
                string rowResref = null;
                // Size-aware segment cap: a Large (3-8m) urban row fixture repeats at most
                // LargeRowSegmentCap times before the row forces a gap and swaps fixtures --
                // shipping containers pair up, they never wall six-deep (Medium/Small rows keep
                // MaxRunSegmentLength). Always MaxRunSegmentLength on non-urban palettes, which
                // declare no sizes.
                var segmentCap = MaxRunSegmentLength;

                while (i < ordered.Count)
                {
                    var tile = ordered[i];
                    var wallDir = NearestWallDirection(tile, tileSet);
                    if (wallDir != null)
                    {
                        // Flush-anchoring capability of THIS tile (see DecorationAnchoring.WallFlush):
                        // a WallFlush entry may only be picked/placed here when a cardinal neighbor is
                        // a stamped structure footprint cell whose face it can sit against. Null for
                        // every palette without WallFlush entries -- purely a pick filter, no RNG.
                        var flushDir = FlushStructureDirection(tile, layout);

                        string resref;
                        if (urban)
                        {
                            if (rowResref == null ||
                                resrefUsageCounts.GetValueOrDefault((context, rowResref)) >=
                                RoomContextCap(entries.FirstOrDefault(e => e.Resref == rowResref)) ||
                                IsAtAreaCap(entries.FirstOrDefault(e => e.Resref == rowResref), areaUsage))
                            {
                                var available = entries
                                    .Where(e => resrefUsageCounts.GetValueOrDefault((context, e.Resref)) < RoomContextCap(e))
                                    .Where(e => e.Anchoring != DecorationAnchoring.WallFlush || flushDir != null)
                                    .Where(e => !IsAtAreaCap(e, areaUsage))
                                    .ToList();
                                rowResref = available.Count > 0 ? PickWeighted(available, rng) : null;
                            }

                            resref = rowResref;
                        }
                        else
                        {
                            resref = PickResrefUnderRoomCap(motifEntries, secondaryMotifEntries, entries, resrefUsageCounts,
                                context, flushDir != null, rng);
                        }

                        if (resref != null)
                        {
                            var entry = entries.FirstOrDefault(e => e.Resref == resref);
                            PlannedDecoration placement = null;
                            if (entry is { Anchoring: DecorationAnchoring.WallFlush })
                            {
                                // A row whose fixture is flush-anchored simply skips tiles with no
                                // structure face (a gap in the row), rather than stranding the item
                                // against a non-architecture room boundary.
                                if (flushDir != null)
                                    placement = BuildFlushStructurePlacement(tile, flushDir.Value, resref, context);
                            }
                            else
                            {
                                placement = urban
                                    ? BuildUrbanWallPlacement(tile, wallDir.Value, resref, context, layout, roadCrosser)
                                    : BuildWallHuggingPlacement(tile, wallDir.Value, resref, context);
                            }

                            if (placement != null)
                            {
                                plan.Add(placement);
                                resrefUsageCounts[(context, resref)] = resrefUsageCounts.GetValueOrDefault((context, resref)) + 1;
                                RecordUse(areaUsage, resref);
                                segmentLength++;
                                segmentCap = urban && entry is { Size: DecorationSize.Large }
                                    ? LargeRowSegmentCap
                                    : MaxRunSegmentLength;
                            }
                        }
                    }

                    var step = urban ? spacing : Math.Max(1, spacing + rng.Next(-jitterRange, jitterRange + 1));
                    if (segmentLength >= segmentCap)
                    {
                        // Force a real gap before starting the next segment — this is what makes a
                        // long wall read as a few distinct dressed clusters instead of one continuous
                        // run (or, worst case, a run that wraps the room's entire perimeter).
                        step += spacing * (RunSegmentGapExtraSteps + 1);
                        segmentLength = 0;
                        segmentCap = MaxRunSegmentLength;
                        // Urban rows also swap fixtures between segments so a long facade reads as
                        // several distinct rows, never one endless line of the same object.
                        rowResref = null;
                    }

                    i += step;
                }
            }
        }

        /// <summary>
        /// Weighted-picks a resref for one wall/corridor/doorway placement, excluding any resref that
        /// has already reached <see cref="MaxSameResrefPerRoomContext"/> for this room+context — tried
        /// in tiers: the room's own primary small motif first, then its secondary motif (drawn lazily
        /// once the primary is exhausted — see PlaceWallRuns), then the full palette bucket as a last
        /// resort so a heavily-capped room still keeps dressing rather than stopping early. Entries
        /// anchored WallFlush are additionally ineligible on a tile with no flush structure face
        /// (<paramref name="tileHasFlushAnchor"/>). Returns null (place nothing at this tile) only
        /// once every candidate for this room+context is at cap. The caller records usage AFTER a
        /// placement is actually emitted (see PlaceWallRuns), so a skipped flush placement never
        /// consumes budget.
        /// </summary>
        private static string PickResrefUnderRoomCap(
            List<DungeonDecorationEntry> motifEntries, List<DungeonDecorationEntry> secondaryMotifEntries,
            List<DungeonDecorationEntry> fallbackEntries,
            Dictionary<(DecorationContext Context, string Resref), int> resrefUsageCounts,
            DecorationContext context, bool tileHasFlushAnchor, System.Random rng)
        {
            bool Eligible(DungeonDecorationEntry e) =>
                resrefUsageCounts.GetValueOrDefault((context, e.Resref)) < MaxSameResrefPerRoomContext &&
                (e.Anchoring != DecorationAnchoring.WallFlush || tileHasFlushAnchor);

            var available = motifEntries.Where(Eligible).ToList();
            if (available.Count == 0)
                available = secondaryMotifEntries.Where(Eligible).ToList();
            if (available.Count == 0)
                available = fallbackEntries.Where(Eligible).ToList();
            if (available.Count == 0)
                return null;

            return PickWeighted(available, rng);
        }

        /// <summary>
        /// Draws this room's small (1-<see cref="MotifResrefCap"/> resref) motif for one context bucket
        /// — a weighted sample WITHOUT replacement over distinct resrefs, so a room repeats a handful of
        /// fixture types instead of sampling the whole palette per placement (rhythm/consistency).
        /// </summary>
        private static List<string> PickMotif(List<DungeonDecorationEntry> entries, System.Random rng)
        {
            var distinctResrefs = entries.Select(e => e.Resref).Distinct().ToList();
            var motifSize = Math.Min(MotifResrefCap, distinctResrefs.Count);
            motifSize = Math.Max(1, Math.Min(motifSize, 1 + rng.Next(MotifResrefCap)));

            var pool = new List<DungeonDecorationEntry>(entries);
            var motif = new List<string>();
            for (var i = 0; i < motifSize && pool.Count > 0; i++)
            {
                var pick = PickWeighted(pool, rng);
                motif.Add(pick);
                pool.RemoveAll(e => e.Resref == pick);
            }

            return motif;
        }

        /// <summary>
        /// Finds a room's courtyard anchor: the room tile with the deepest verified interior
        /// clearance (every tile within <see cref="CourtyardInteriorClearance"/> Chebyshev range
        /// belongs to this same room -- walls, stamped building footprints, and foreign rooms all
        /// fail the tile-set test), that is not the reserved CenterTile, not excluded, and does not
        /// itself carry a road edge (the arrangement never stands ON the street; mere road
        /// ADJACENCY is allowed -- see CourtyardInteriorClearance's doc comment for the measured
        /// generated-scale justification). Ties break on the room's stored tile order, so the anchor
        /// is deterministic per layout with zero RNG. Returns null when the room has no interior at
        /// all (small rooms, ring-shaped rooms, rooms fully consumed by roads/buildings).
        /// </summary>
        private static (int X, int Y)? FindCourtyardAnchor(
            LayoutRoom room, HashSet<(int X, int Y)> tileSet, HashSet<(int X, int Y)> excluded,
            ResolvedLayout layout, string roadCrosser)
        {
            (int X, int Y)? best = null;
            var bestClearance = CourtyardInteriorClearance - 1;

            foreach (var tile in room.Tiles)
            {
                if (excluded.Contains(tile) || tile == room.CenterTile)
                    continue;
                if (TileCarriesRoadEdge(tile, layout, roadCrosser))
                    continue;

                // Largest Chebyshev radius (up to clearance+1, no need to scan further) whose whole
                // square block stays inside this room's own tiles.
                var clearance = 0;
                for (var radius = 1; radius <= CourtyardInteriorClearance + 1; radius++)
                {
                    var intact = true;
                    for (var dx = -radius; dx <= radius && intact; dx++)
                    for (var dy = -radius; dy <= radius && intact; dy++)
                    {
                        if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != radius)
                            continue;
                        if (!tileSet.Contains((tile.X + dx, tile.Y + dy)))
                            intact = false;
                    }

                    if (!intact)
                        break;
                    clearance = radius;
                }

                if (clearance > bestClearance)
                {
                    bestClearance = clearance;
                    best = tile;
                }
            }

            return best;
        }

        /// <summary>
        /// Composes one courtyard: a CourtyardCenter item standing at the anchor tile's center, ringed
        /// by Courtyard-bucket items at a roughly constant radius and roughly even angles (small
        /// deterministic jitter on both), each facing back at the centerpiece -- the arrangement mined
        /// from hand-built fcx01 interior clusters (centerpiece + 4-13-member ring at radius ~4-9m;
        /// see CourtyardTargetShare's doc comment). Ring size scales with room area. Ring members draw
        /// from a small motif (2-3 distinct resrefs, cycled) so the ring reads as a composed mixed
        /// arrangement, matching the hand-built clusters' 2-10 distinct-resref compositions. A ring
        /// position whose tile falls outside the room (or on an excluded/consumed tile) is skipped;
        /// the courtyard commits only when the centerpiece plus at least 3 ring members landed,
        /// otherwise nothing is planned and the room falls back to the ordinary centerpiece roll.
        /// </summary>
        private static bool PlanCourtyard(
            List<PlannedDecoration> plan, LayoutRoom room, HashSet<(int X, int Y)> tileSet, (int X, int Y) anchor,
            List<DungeonDecorationEntry> centerEntries, List<DungeonDecorationEntry> ringEntries,
            HashSet<(int X, int Y)> excluded, HashSet<(int X, int Y)> consumedTiles,
            ResolvedLayout layout, string roadCrosser, System.Random rng, bool urban = false,
            Dictionary<string, int> areaUsage = null)
        {
            areaUsage ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var centerPool = UnderAreaCap(centerEntries, areaUsage);
            var ringPool = UnderAreaCap(ringEntries, areaUsage);
            if (centerPool.Count == 0 || ringPool.Count == 0)
                return false;

            var centerResref = PickWeighted(centerPool, rng);
            var center = TileCenter(anchor.X, anchor.Y);

            // Room-area scaling: a 5x5-tile room rings 4-5 items, a 9x9 plaza rings the full 8.
            var ringCount = Math.Clamp(3 + room.Tiles.Count / 16, CourtyardMinRingItems, CourtyardMaxRingItems);
            var radius = CourtyardBaseRadius + (float)(rng.NextDouble() * CourtyardRadiusJitter);

            // Mixed-resref ring motif: 2-3 distinct resrefs cycled around the ring (weighted sample
            // without replacement), matching the hand-built mixed-composition evidence. A palette
            // with a single curated ring resref still works (the hand-built sample includes one
            // all-light-pole ring too).
            var motifSize = Math.Min(3, ringPool.Select(e => e.Resref).Distinct().Count());
            var pool = new List<DungeonDecorationEntry>(ringPool);
            var motif = new List<string>();
            for (var i = 0; i < motifSize && pool.Count > 0; i++)
            {
                var pick = PickWeighted(pool, rng);
                motif.Add(pick);
                pool.RemoveAll(e => e.Resref == pick);
            }

            if (motif.Count == 0)
                return false;

            var startAngle = rng.NextDouble() * Math.PI * 2.0;
            var members = new List<PlannedDecoration>();
            var memberTiles = new List<(int X, int Y)>();

            for (var i = 0; i < ringCount; i++)
            {
                var angle = startAngle + i * (Math.PI * 2.0 / ringCount) + (rng.NextDouble() - 0.5) * 0.24;
                var r = radius + (float)((rng.NextDouble() - 0.5) * 1.0);
                var x = center.X + (float)Math.Cos(angle) * r;
                var y = center.Y + (float)Math.Sin(angle) * r;

                var tile = ((int)MathF.Floor(x / TileSize), (int)MathF.Floor(y / TileSize));
                if (!tileSet.Contains(tile) || excluded.Contains(tile) || tile == room.CenterTile || consumedTiles.Contains(tile))
                    continue;
                if (TileCarriesRoadEdge(tile, layout, roadCrosser))
                    continue;

                // Face back at the centerpiece -- hand-built ring members (benches, light poles,
                // kiosks) consistently orient into the arrangement they surround.
                var facing = (float)(Math.Atan2(center.Y - y, center.X - x) * (180.0 / Math.PI));
                members.Add(new PlannedDecoration
                {
                    Resref = motif[i % motif.Count],
                    Position = new Vector3(x, y, 0f),
                    Facing = facing,
                    Context = DecorationContext.Courtyard
                });
                memberTiles.Add(tile);
            }

            // Urban commit floor is 4 (round-9 monument sweep): a 3-member ring spread over a
            // 5-6.5m radius reads as free-standing fragments, not a composed court -- the
            // hand-built rings measure 4-13 members. Non-urban keeps the original 3.
            if (members.Count < (urban ? 4 : 3))
                return false;

            plan.Add(new PlannedDecoration
            {
                Resref = centerResref,
                Position = center,
                // Urban bearing rule: the courtyard's centerpiece squares up with the plaza grid.
                Facing = urban ? 90f * rng.Next(4) : (float)(rng.NextDouble() * 360.0),
                Context = DecorationContext.CourtyardCenter
            });
            RecordUse(areaUsage, centerResref);
            plan.AddRange(members);
            foreach (var member in members)
                RecordUse(areaUsage, member.Resref);

            // A ground-decal courtyard center must never read as a lone patch: layer 1-2 of the
            // ring's own motif items directly on top of the decal (hand-built decals exist as
            // LAYERING under arrangements), well inside the ring's 5.0-6.5m radius. Emitted under
            // ClutterPile -- these are junk layered ON the decal, not ring members, so the ring's
            // own radius-band/facing geometry contract (see CourtyardCompositionTests) stays
            // scoped to the actual ring.
            var centerEntry = centerEntries.FirstOrDefault(e => e.Resref == centerResref);
            if (centerEntry is { Role: DecorationRole.GroundDecal })
            {
                var toppingCount = 1 + rng.Next(2);
                for (var i = 0; i < toppingCount; i++)
                {
                    var angle = rng.NextDouble() * Math.PI * 2.0;
                    var r = CourtyardDecalToppingMinRadius +
                            (float)(rng.NextDouble() * (CourtyardDecalToppingMaxRadius - CourtyardDecalToppingMinRadius));
                    plan.Add(new PlannedDecoration
                    {
                        Resref = motif[i % motif.Count],
                        Position = new Vector3(
                            center.X + (float)Math.Cos(angle) * r,
                            center.Y + (float)Math.Sin(angle) * r,
                            0f),
                        Facing = urban ? 90f * rng.Next(4) : (float)(rng.NextDouble() * 360.0),
                        Context = DecorationContext.ClutterPile
                    });
                    RecordUse(areaUsage, motif[i % motif.Count]);
                }
            }

            consumedTiles.Add(anchor);
            foreach (var tile in memberTiles)
                consumedTiles.Add(tile);

            return true;
        }

        /// <summary>
        /// Finds a room's mid-room ensemble anchors: fully interior tiles (every cardinal neighbor
        /// belongs to this room) that are not excluded, not the reserved CenterTile, and carry no
        /// road edge, kept <see cref="EnsembleAnchorSpacing"/> tiles apart so two ensembles never
        /// fuse. Deterministic (no RNG): candidates are taken in the room's stored tile order.
        /// Count scales with room area (one anchor per ~20 tiles, minimum one when any interior
        /// tile exists).
        /// </summary>
        private static List<(int X, int Y)> FindEnsembleAnchors(
            LayoutRoom room, HashSet<(int X, int Y)> tileSet, HashSet<(int X, int Y)> excluded,
            ResolvedLayout layout, string roadCrosser)
        {
            var anchors = new List<(int X, int Y)>();
            var cap = 1 + room.Tiles.Count / 20;

            foreach (var tile in room.Tiles)
            {
                if (anchors.Count >= cap)
                    break;
                if (excluded.Contains(tile) || tile == room.CenterTile)
                    continue;
                if (NearestWallDirection(tile, tileSet) != null)
                    continue;
                if (TileCarriesRoadEdge(tile, layout, roadCrosser))
                    continue;
                if (anchors.Any(a => Chebyshev(a, tile) < EnsembleAnchorSpacing))
                    continue;

                anchors.Add(tile);
            }

            return anchors;
        }

        /// <summary>
        /// Weighted decal pick under the urban grammar's decal discipline (round 9): entries rotate
        /// by inverse per-area usage (weight / (1 + uses)) so one resref can never dominate the
        /// area's pads -- the hand-built clean-decal family splits its placements across four+
        /// models (floorm01 at 0.55, florrd01 0.18, flormh01 0.14, the hatch grills 0.13 combined
        /// -- _scratch_decor/mine_r9_interiors.py) while round-8 output put ~70% on one plate --
        /// and Large (8.5-9.6m) floor plates are size-matched via <paramref name="largeFactor"/>:
        /// full weight under big compositions (ensemble bases), damped under small consumers (a
        /// junk pile's pad leans to the 1.6m hatch grills), 0 to exclude them outright. Returns
        /// null when nothing eligible remains. Urban-only: non-urban palettes keep the original
        /// weighted pick and RNG path bit for bit.
        /// </summary>
        private static DungeonDecorationEntry PickUrbanDecal(
            List<DungeonDecorationEntry> pool, Dictionary<string, int> areaUsage, System.Random rng, double largeFactor)
        {
            if (pool == null || pool.Count == 0)
                return null;

            var eligible = pool
                .Where(e => !IsAtAreaCap(e, areaUsage))
                .Where(e => largeFactor > 0 || e.Size is not (DecorationSize.Large or DecorationSize.Huge))
                .ToList();
            if (eligible.Count == 0)
                return null;

            var weights = eligible
                .Select(e => e.Weight / (1.0 + areaUsage.GetValueOrDefault(e.Resref)) *
                             (e.Size is DecorationSize.Large or DecorationSize.Huge ? largeFactor : 1.0))
                .ToList();
            if (weights.Sum() <= 0)
                return null;
            var total = weights.Sum();
            var roll = rng.NextDouble() * total;
            var cumulative = 0.0;
            for (var i = 0; i < eligible.Count; i++)
            {
                cumulative += weights[i];
                if (roll < cumulative)
                    return eligible[i];
            }

            return eligible[^1];
        }

        /// <summary>
        /// Composes one mid-room ensemble at an interior anchor -- the round-9 "barren plaza
        /// middles" arrangement: a centerpiece (monument/kiosk/tree from the flavor's
        /// CourtyardCenter+RoomCenter pools), an optional base floor decal underneath (Large plates
        /// only with full 3x3 clearance -- size-matched), and 3-5 satellites (benches, planters,
        /// lamps, seating from the flavor's Courtyard ring pool) facing the centerpiece at a
        /// flavor-shaped radius: commercial plaza ISLANDS pack at 1.9-3.2m, civic monument GARDENS
        /// breathe at 2.6-4.0m. Commits only when the centerpiece plus at least
        /// <see cref="EnsembleMinSatellites"/> satellites landed -- an ensemble is never a
        /// free-standing item or pair. Committed tiles are consumed.
        /// </summary>
        private static bool PlanInteriorEnsemble(
            List<PlannedDecoration> plan, LayoutRoom room, HashSet<(int X, int Y)> tileSet,
            (int X, int Y) anchor, DistrictFlavor flavor, PaletteView view,
            HashSet<(int X, int Y)> excluded, HashSet<(int X, int Y)> consumedTiles,
            ResolvedLayout layout, string roadCrosser, System.Random rng,
            Dictionary<string, int> areaUsage)
        {
            // Centerpiece pool: the flavor's curated interior set pieces. Ground decals are a BASE,
            // not a centerpiece.
            var centerPool = new List<DungeonDecorationEntry>();
            if (view.CourtyardCenterEntries != null)
                centerPool.AddRange(view.CourtyardCenterEntries.Where(e => e.Role != DecorationRole.GroundDecal));
            if (view.ByContext.TryGetValue(DecorationContext.RoomCenter, out var roomCenterEntries))
                centerPool.AddRange(roomCenterEntries);
            centerPool = UnderAreaCap(centerPool, areaUsage);

            var satellitePool = UnderAreaCap(view.CourtyardRingEntries, areaUsage);
            if (centerPool == null || centerPool.Count == 0 || satellitePool == null || satellitePool.Count == 0)
                return false;

            var (minRadius, maxRadius) = flavor == DistrictFlavor.Commercial
                ? (IslandMinRadius, IslandMaxRadius)
                : (GardenMinRadius, GardenMaxRadius);

            var centerResref = PickWeighted(centerPool, rng);
            // A Large centerpiece (a 4.8m kiosk, a 10m lit floor strip) pushes its surround out so
            // satellites never stand inside the model's own footprint.
            if (centerPool.First(e => e.Resref == centerResref).Size == DecorationSize.Large)
            {
                minRadius = MathF.Max(minRadius, 3.2f);
                maxRadius = MathF.Max(maxRadius, 4.2f);
            }
            var center = TileCenter(anchor.X, anchor.Y);

            // Satellite motif: 2-3 distinct resrefs cycled (the hand-built mixed-composition
            // pattern every composed arrangement in this planner follows).
            var motifSize = Math.Min(3, satellitePool.Select(e => e.Resref).Distinct().Count());
            var motifPool = new List<DungeonDecorationEntry>(satellitePool);
            var motif = new List<string>();
            for (var i = 0; i < motifSize && motifPool.Count > 0; i++)
            {
                var pick = PickWeighted(motifPool, rng);
                motif.Add(pick);
                motifPool.RemoveAll(e => e.Resref == pick);
            }
            if (motif.Count == 0)
                return false;

            var satelliteCount = EnsembleMinSatellites + rng.Next(EnsembleMaxSatellites - EnsembleMinSatellites + 1);
            var radius = minRadius + (float)(rng.NextDouble() * (maxRadius - minRadius));
            var startAngle = rng.NextDouble() * Math.PI * 2.0;

            var members = new List<PlannedDecoration>();
            var memberTiles = new List<(int X, int Y)>();
            for (var i = 0; i < satelliteCount; i++)
            {
                var angle = startAngle + i * (Math.PI * 2.0 / satelliteCount) + (rng.NextDouble() - 0.5) * 0.3;
                var r = radius + (float)((rng.NextDouble() - 0.5) * 0.6);
                var x = center.X + (float)Math.Cos(angle) * r;
                var y = center.Y + (float)Math.Sin(angle) * r;

                var tile = ((int)MathF.Floor(x / TileSize), (int)MathF.Floor(y / TileSize));
                if (!tileSet.Contains(tile) || excluded.Contains(tile) || tile == room.CenterTile ||
                    consumedTiles.Contains(tile))
                    continue;
                if (TileCarriesRoadEdge(tile, layout, roadCrosser))
                    continue;

                var facing = (float)(Math.Atan2(center.Y - y, center.X - x) * (180.0 / Math.PI));
                members.Add(new PlannedDecoration
                {
                    Resref = motif[i % motif.Count],
                    Position = new Vector3(x, y, 0f),
                    Facing = facing,
                    Context = DecorationContext.EnsembleMember
                });
                memberTiles.Add(tile);
            }

            if (members.Count < EnsembleMinSatellites)
                return false;

            // Base decal: size-matched via PickUrbanDecal -- the Large plates need full 3x3
            // in-room clearance so an 8.5m plate never sticks through a wall or road.
            var hasClearance = true;
            for (var dx = -1; dx <= 1 && hasClearance; dx++)
            for (var dy = -1; dy <= 1 && hasClearance; dy++)
            {
                var probe = (anchor.X + dx, anchor.Y + dy);
                if (!tileSet.Contains(probe) || TileCarriesRoadEdge(probe, layout, roadCrosser))
                    hasClearance = false;
            }

            if (view.DecalEntries is { Count: > 0 } && rng.NextDouble() < EnsembleBaseDecalChance)
            {
                var decal = PickUrbanDecal(view.DecalEntries, areaUsage, rng, largeFactor: hasClearance ? 1.0 : 0.35);
                if (decal != null)
                {
                    plan.Add(new PlannedDecoration
                    {
                        Resref = decal.Resref,
                        Position = center,
                        Facing = 90f * rng.Next(4),
                        Context = DecorationContext.GroundDecal
                    });
                    RecordUse(areaUsage, decal.Resref);
                }
            }

            plan.Add(new PlannedDecoration
            {
                Resref = centerResref,
                Position = center,
                // An intentional plaza set piece squares up with the surrounding grid.
                Facing = 90f * rng.Next(4),
                Context = DecorationContext.EnsembleCenter
            });
            RecordUse(areaUsage, centerResref);
            plan.AddRange(members);
            foreach (var member in members)
                RecordUse(areaUsage, member.Resref);

            consumedTiles.Add(anchor);
            foreach (var tile in memberTiles)
                consumedTiles.Add(tile);

            return true;
        }

        /// <summary>
        /// Finds the longest run of CONSECUTIVE depot-eligible wall tiles in a room (non-excluded,
        /// non-consumed, off-road, with a real wall direction), bucketed by quantized wall
        /// direction -- the anchor line a composed depot block stacks against. Returns null when no
        /// run of at least 2 tiles exists. Same segment notion as PlanCargoYard's yard rows.
        /// </summary>
        private static (List<(int X, int Y)> Tiles, int Direction)? FindDepotSegment(
            LayoutRoom room, HashSet<(int X, int Y)> tileSet, HashSet<(int X, int Y)> excluded,
            HashSet<(int X, int Y)> consumedTiles, ResolvedLayout layout, string roadCrosser)
        {
            var byDirection = new Dictionary<int, List<(int X, int Y)>>();
            foreach (var tile in room.Tiles)
            {
                if (excluded.Contains(tile) || tile == room.CenterTile || consumedTiles.Contains(tile))
                    continue;
                if (TileCarriesRoadEdge(tile, layout, roadCrosser))
                    continue;

                var wallDir = NearestWallDirection(tile, tileSet);
                if (wallDir == null)
                    continue;

                var direction = QuantizeDirection(wallDir.Value.Dx, wallDir.Value.Dy);
                if (!byDirection.TryGetValue(direction, out var list))
                    byDirection[direction] = list = new List<(int X, int Y)>();
                list.Add(tile);
            }

            List<(int X, int Y)> bestSegment = null;
            var bestDirection = 0;
            foreach (var (direction, tiles) in byDirection.OrderBy(kv => kv.Key))
            {
                var alongY = direction is 0 or 1;
                var ordered = alongY
                    ? tiles.OrderBy(t => t.X).ThenBy(t => t.Y).ToList()
                    : tiles.OrderBy(t => t.Y).ThenBy(t => t.X).ToList();

                var segment = new List<(int X, int Y)>();
                void Flush()
                {
                    if (bestSegment == null || segment.Count > bestSegment.Count)
                    {
                        bestSegment = new List<(int X, int Y)>(segment);
                        bestDirection = direction;
                    }
                }

                foreach (var tile in ordered)
                {
                    var contiguous = segment.Count > 0 &&
                                     (alongY
                                         ? tile.X == segment[^1].X && tile.Y == segment[^1].Y + 1
                                         : tile.Y == segment[^1].Y && tile.X == segment[^1].X + 1);
                    if (!contiguous)
                    {
                        Flush();
                        segment.Clear();
                    }

                    segment.Add(tile);
                }

                Flush();
            }

            return bestSegment is { Count: >= 2 } ? (bestSegment, bestDirection) : null;
        }

        /// <summary>
        /// Composes one industrial DEPOT block against a wall segment -- the round-9 replacement
        /// for "crates as evenly-spaced exhibits, each on its own pad". Hand-built industrial
        /// reference areas butt their cargo into dense rows (crate-family same-family NN median
        /// 0.09m, 93% within 2.2m, colinear runs of 4-12 sharing a dominant bearing --
        /// _scratch_decor/mine_r9_interiors.py); a generated block therefore lays 4-9 crates in 1-2
        /// parallel rows at 1.35m pitch (true butt-joint for the 0.75-1.5m crate family), all
        /// sharing the wall's outward bearing (with occasional quarter turns), drawn from a 2-3
        /// type mixed-height motif of the room's Clutter pool, with 1-2 small satellite props at
        /// the row ends and ONE size-matched pad decal per BLOCK (never per item). Committed
        /// segment tiles are consumed. Returns false when no 2+-tile segment or pool remains.
        /// </summary>
        private static bool PlanDepotBlock(
            List<PlannedDecoration> plan, LayoutRoom room, HashSet<(int X, int Y)> tileSet,
            PaletteView view, HashSet<(int X, int Y)> excluded, HashSet<(int X, int Y)> consumedTiles,
            ResolvedLayout layout, string roadCrosser, System.Random rng,
            Dictionary<string, int> areaUsage)
        {
            var found = FindDepotSegment(room, tileSet, excluded, consumedTiles, layout, roadCrosser);
            if (found == null)
                return false;

            var (segment, direction) = found.Value;
            var pool = UnderAreaCap(view.ClutterEntries, areaUsage)
                .Where(e => e.Size != DecorationSize.Huge)
                .ToList();
            if (pool.Count == 0)
                return false;

            // Block motif: 2-3 distinct crate/cargo types, at most one Large -- mixed heights, but
            // a stack, not a jumble.
            var motifSize = Math.Min(3, pool.Select(e => e.Resref).Distinct().Count());
            motifSize = Math.Max(2, motifSize);
            var motifPool = new List<DungeonDecorationEntry>(pool);
            var motif = new List<DungeonDecorationEntry>();
            var largeInMotif = 0;
            for (var i = 0; i < motifSize && motifPool.Count > 0; i++)
            {
                var candidates = largeInMotif > 0
                    ? motifPool.Where(e => e.Size != DecorationSize.Large).ToList()
                    : motifPool;
                if (candidates.Count == 0)
                    break;
                var pick = PickWeighted(candidates, rng);
                var entry = candidates.First(e => e.Resref == pick);
                motif.Add(entry);
                if (entry.Size == DecorationSize.Large)
                    largeInMotif++;
                motifPool.RemoveAll(e => e.Resref == pick);
            }
            if (motif.Count == 0)
                return false;

            var (nx, ny) = direction switch
            {
                0 => (1f, 0f),
                1 => (-1f, 0f),
                2 => (0f, 1f),
                _ => (0f, -1f)
            };
            var alongY = direction is 0 or 1;
            var baseFacing = CardinalFacing(-nx, -ny);

            // World-space run span: from the first segment tile's near edge to the last one's far
            // edge, inset by the tile-edge margin so nothing spills into a neighbor cell.
            var ordered = alongY
                ? segment.OrderBy(t => t.Y).ToList()
                : segment.OrderBy(t => t.X).ToList();
            var first = ordered[0];
            var last = ordered[^1];
            var alongStart = (alongY ? first.Y : first.X) * TileSize + PileTileEdgeMargin;
            var alongEnd = (alongY ? last.Y : last.X) * TileSize + TileSize - PileTileEdgeMargin;
            // Fixed cross-axis line shared by all segment tiles.
            var crossCenter = (alongY ? first.X : first.Y) * TileSize + TileHalf;

            var totalItems = DepotBlockMinItems + rng.Next(DepotBlockMaxItems - DepotBlockMinItems + 1);
            var rowCount = totalItems >= 6 ? 2 : 1;
            var perRow = (totalItems + rowCount - 1) / rowCount;

            var members = new List<PlannedDecoration>();
            for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                var rowOffset = DepotWallOffset - rowIndex * DepotRowSeparation;
                // Each row leans on ONE dominant motif type with occasional swaps -- a real stack
                // repeats its crate, it doesn't alternate per item.
                var dominant = motif[rowIndex % motif.Count];
                var along = alongStart + 1.0f + (float)(rng.NextDouble() * 0.8);
                for (var i = 0; i < perRow && members.Count < totalItems; i++)
                {
                    if (along > alongEnd - 1.0f)
                        break;

                    var entry = rng.NextDouble() < 0.75 ? dominant : motif[rng.Next(motif.Count)];
                    var cross = crossCenter + (alongY ? nx : ny) * rowOffset;
                    var position = alongY
                        ? new Vector3(cross, along, 0f)
                        : new Vector3(along, cross, 0f);

                    members.Add(new PlannedDecoration
                    {
                        Resref = entry.Resref,
                        Position = position,
                        // Shared block bearing with occasional quarter turns, staying cardinal.
                        Facing = rng.Next(4) == 0 ? (baseFacing + 90f) % 360f : baseFacing,
                        Context = DecorationContext.DepotRow
                    });

                    along += DepotRowPitch * (0.95f + (float)(rng.NextDouble() * 0.2));
                }
            }

            if (members.Count < DepotBlockMinItems)
                return false;

            // Satellite props at the row ends: the salvage pack / lone barrel a hand-built yard
            // leaves beside its stock. Small-size, preferably outside the block motif.
            var satellitePool = pool.Where(e => e.Size == DecorationSize.Small).ToList();
            var offMotif = satellitePool.Where(e => motif.All(m => m.Resref != e.Resref)).ToList();
            if (offMotif.Count > 0)
                satellitePool = offMotif;
            if (satellitePool.Count > 0)
            {
                var satelliteCount = 1 + rng.Next(2);
                for (var i = 0; i < satelliteCount; i++)
                {
                    var atStart = i % 2 == 0;
                    var memberAlong = members
                        .Select(m => alongY ? m.Position.Y : m.Position.X)
                        .ToList();
                    var along = atStart
                        ? memberAlong.Min() - DepotRowPitch * (1.0f + (float)rng.NextDouble() * 0.5f)
                        : memberAlong.Max() + DepotRowPitch * (1.0f + (float)rng.NextDouble() * 0.5f);
                    if (along < alongStart || along > alongEnd)
                        continue;

                    var cross = crossCenter + (alongY ? nx : ny) * (DepotWallOffset - DepotRowSeparation * 0.5f);
                    var resref = PickWeighted(satellitePool, rng);
                    members.Add(new PlannedDecoration
                    {
                        Resref = resref,
                        Position = alongY ? new Vector3(cross, along, 0f) : new Vector3(along, cross, 0f),
                        Facing = (baseFacing + 90f * rng.Next(2)) % 360f,
                        Context = DecorationContext.DepotRow
                    });
                }
            }

            // ONE pad per block (never per item): a small-size decal at the block centroid.
            if (view.DecalEntries is { Count: > 0 } && rng.NextDouble() < 0.8)
            {
                // A plate under a cargo block is the hand-built industrial forecourt look, but the
                // hatch grills stay the depot's first choice.
                var decal = PickUrbanDecal(view.DecalEntries, areaUsage, rng, largeFactor: 0.5);
                if (decal != null)
                {
                    var cx = members.Average(m => m.Position.X);
                    var cy = members.Average(m => m.Position.Y);
                    plan.Add(new PlannedDecoration
                    {
                        Resref = decal.Resref,
                        Position = new Vector3((float)cx, (float)cy, 0f),
                        Facing = 90f * rng.Next(4),
                        Context = DecorationContext.GroundDecal
                    });
                    RecordUse(areaUsage, decal.Resref);
                }
            }

            plan.AddRange(members);
            foreach (var member in members)
                RecordUse(areaUsage, member.Resref);
            foreach (var tile in segment)
                consumedTiles.Add(tile);

            return true;
        }

        /// <summary>
        /// Dresses zone-marking FEATURE TILES (see DungeonTilesetProfile.FeatureTileDressings) that
        /// landed inside open rooms -- the round-9 "empty zone decal" rule: an area-marking tile
        /// IMPLIES content, so a grass lawn composes a PARK (tree/monument centerpiece + facing
        /// bench/light ring) and a fountain court composes its seating surround, all ON the feature
        /// cell itself. Deterministic obligation (no placement roll): a bare park patch is exactly
        /// the reported artifact. Cells outside rooms, excluded cells, and reserved CenterTiles are
        /// skipped. Committed cells are consumed before any other mechanism runs.
        /// </summary>
        private static void PlanZoneDressings(
            List<PlannedDecoration> plan, ResolvedLayout layout, DungeonTilesetProfile tileset,
            List<RoomState> rooms, Dictionary<(int X, int Y), int> tileToRoom,
            HashSet<(int X, int Y)> excluded, HashSet<(int X, int Y)> consumedTiles,
            string roadCrosser, System.Random rng, Dictionary<string, int> areaUsage)
        {
            foreach (var (cell, groupName) in layout.FeatureTileCells
                         .OrderBy(kv => kv.Key.Y).ThenBy(kv => kv.Key.X))
            {
                if (!tileset.FeatureTileDressings.TryGetValue(groupName, out var dressing) ||
                    dressing == FeatureZoneDressing.None)
                    continue;
                if (!tileToRoom.TryGetValue(cell, out var roomIndex))
                    continue;

                var state = rooms[roomIndex];
                if (excluded.Contains(cell) || cell == state.Room.CenterTile || consumedTiles.Contains(cell))
                    continue;

                var view = state.View;
                var satellitePool = UnderAreaCap(view.CourtyardRingEntries, areaUsage);
                if (satellitePool == null || satellitePool.Count == 0)
                    continue;

                var center = TileCenter(cell.X, cell.Y);
                var committed = new List<PlannedDecoration>();

                if (dressing == FeatureZoneDressing.Lawn)
                {
                    // Park centerpiece: a standing item (tree/monument/hologram) -- never a decal
                    // (the lawn itself is the ground statement) and never a 10m light strip.
                    var centerPool = new List<DungeonDecorationEntry>();
                    if (view.CourtyardCenterEntries != null)
                        centerPool.AddRange(view.CourtyardCenterEntries.Where(e =>
                            e.Role != DecorationRole.GroundDecal && e.Size != DecorationSize.Large));
                    if (view.ByContext.TryGetValue(DecorationContext.RoomCenter, out var roomCenterEntries))
                        centerPool.AddRange(roomCenterEntries.Where(e => e.Size != DecorationSize.Large));
                    centerPool = UnderAreaCap(centerPool, areaUsage);
                    if (centerPool.Count > 0)
                    {
                        var centerResref = PickWeighted(centerPool, rng);
                        committed.Add(new PlannedDecoration
                        {
                            Resref = centerResref,
                            Position = center,
                            Facing = 90f * rng.Next(4),
                            Context = DecorationContext.EnsembleCenter
                        });
                    }
                }

                // Facing surround, entirely on the feature cell: radius capped so every member
                // stays inside the 10x10 tile with a real margin. Always 4 members, so every
                // surround member has both ring neighbors in easy sight range -- never a sparse
                // trio that reads as strays.
                var (minR, maxR) = dressing == FeatureZoneDressing.Lawn ? (2.8f, 3.9f) : (3.2f, 4.0f);
                const int count = 4;
                var motifSize = Math.Min(3, satellitePool.Select(e => e.Resref).Distinct().Count());
                var motifPool = new List<DungeonDecorationEntry>(satellitePool);
                var motif = new List<string>();
                for (var i = 0; i < motifSize && motifPool.Count > 0; i++)
                {
                    var pick = PickWeighted(motifPool, rng);
                    motif.Add(pick);
                    motifPool.RemoveAll(e => e.Resref == pick);
                }
                if (motif.Count == 0)
                    continue;

                var startAngle = rng.NextDouble() * Math.PI * 2.0;
                for (var i = 0; i < count; i++)
                {
                    var angle = startAngle + i * (Math.PI * 2.0 / count) + (rng.NextDouble() - 0.5) * 0.2;
                    var r = minR + (float)(rng.NextDouble() * (maxR - minR));
                    var x = center.X + (float)Math.Cos(angle) * r;
                    var y = center.Y + (float)Math.Sin(angle) * r;
                    // Clamp inside the cell -- the surround belongs to the zone tile itself.
                    x = Math.Clamp(x, center.X - TileHalf + PileTileEdgeMargin, center.X + TileHalf - PileTileEdgeMargin);
                    y = Math.Clamp(y, center.Y - TileHalf + PileTileEdgeMargin, center.Y + TileHalf - PileTileEdgeMargin);

                    var facing = (float)(Math.Atan2(center.Y - y, center.X - x) * (180.0 / Math.PI));
                    committed.Add(new PlannedDecoration
                    {
                        Resref = motif[i % motif.Count],
                        Position = new Vector3(x, y, 0f),
                        Facing = facing,
                        Context = DecorationContext.EnsembleMember
                    });
                }

                if (committed.Count < 3)
                    continue;

                plan.AddRange(committed);
                foreach (var member in committed)
                    RecordUse(areaUsage, member.Resref);
                consumedTiles.Add(cell);
            }
        }

        /// <summary>
        /// Draws a room's junk POOL: the wider (5-8 distinct resref) subset of the palette's
        /// <see cref="DecorationRole.Clutter"/> entries this room's piles share -- weighted sample
        /// without replacement (see the two-level motif doc on PileRoomPoolMinResrefs). Each pile
        /// then draws its own small motif from this pool inside PlanClutterPile.
        /// </summary>
        private static List<DungeonDecorationEntry> PickJunkMotif(List<DungeonDecorationEntry> clutterEntries, System.Random rng)
        {
            var distinct = clutterEntries.Select(e => e.Resref).Distinct().Count();
            var poolSize = Math.Min(distinct, PileRoomPoolMinResrefs + rng.Next(PileRoomPoolMaxResrefs - PileRoomPoolMinResrefs + 1));

            var pool = new List<DungeonDecorationEntry>(clutterEntries);
            var roomPool = new List<DungeonDecorationEntry>();
            for (var i = 0; i < poolSize && pool.Count > 0; i++)
            {
                var pick = PickWeighted(pool, rng);
                roomPool.AddRange(pool.Where(e => e.Resref == pick));
                pool.RemoveAll(e => e.Resref == pick);
            }

            return roomPool.Count > 0 ? roomPool : clutterEntries;
        }

        /// <summary>
        /// Composes one clutter pile at an anchor tile: 3-8 junk items (from the room's junk motif)
        /// packed in a 1.2-2.5m-radius disc, optionally layered over a ground decal (see
        /// DecalUnderPileChance). A wall-eligible anchor hugs its wall (center offset toward the
        /// wall direction, like every other wall-hugging mechanism); an interior anchor jitters
        /// around the tile center -- so the same generic mechanism dresses wall lines, stamped
        /// tower bases (structure footprints removed from room.Tiles read as walls), and open
        /// plaza interiors. Members rejection-sample for a minimum mutual separation and NEVER
        /// leave the anchor tile (see PileTileEdgeMargin -- the neighbor cell may be a chasm).
        /// Commits only when at least <see cref="PileCommitMinItems"/> members landed; the decal is
        /// only emitted alongside a committed pile, so a decal can never stand alone.
        /// </summary>
        private static bool PlanClutterPile(
            List<PlannedDecoration> plan, (int X, int Y) anchor, HashSet<(int X, int Y)> tileSet,
            List<DungeonDecorationEntry> junkMotif, List<DungeonDecorationEntry> decalEntries, System.Random rng,
            bool urban = false, bool organicSpin = false, bool structureAdjacent = false,
            Dictionary<string, int> areaUsage = null)
        {
            areaUsage ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var flat = TileCenter(anchor.X, anchor.Y);
            var wallDir = NearestWallDirection(anchor, tileSet);

            Vector3 center;
            if (wallDir != null)
            {
                center = new Vector3(flat.X + wallDir.Value.Dx * PileWallOffset, flat.Y + wallDir.Value.Dy * PileWallOffset, 0f);
            }
            else
            {
                var jitterAngle = rng.NextDouble() * Math.PI * 2.0;
                var jitterRadius = rng.NextDouble() * PileCenterJitter;
                center = new Vector3(
                    flat.X + (float)(Math.Cos(jitterAngle) * jitterRadius),
                    flat.Y + (float)(Math.Sin(jitterAngle) * jitterRadius),
                    0f);
            }

            var memberCount = PileMinItems + rng.Next(PileMaxItems - PileMinItems + 1);
            var radius = PileMinRadius + (float)(rng.NextDouble() * (PileMaxRadius - PileMinRadius));

            // This pile's own small motif: 2-3 distinct types from the room's junk pool (see the
            // two-level motif doc on PileRoomPoolMinResrefs) -- a real junk stack mixes only a
            // couple of types even when the room's overall junk family is wider.
            var distinctInPool = junkMotif.Select(e => e.Resref).Distinct().Count();
            var pileMotifSize = Math.Min(distinctInPool, PileMotifMinResrefs + rng.Next(PileMotifMaxResrefs - PileMotifMinResrefs + 1));
            var motifPool = new List<DungeonDecorationEntry>(junkMotif);
            var pileMotif = new List<DungeonDecorationEntry>();
            for (var i = 0; i < pileMotifSize && motifPool.Count > 0; i++)
            {
                var pick = PickWeighted(motifPool, rng);
                pileMotif.AddRange(motifPool.Where(e => e.Resref == pick));
                motifPool.RemoveAll(e => e.Resref == pick);
            }
            if (pileMotif.Count == 0)
                pileMotif = junkMotif;

            var decalPool = UnderAreaCap(decalEntries, areaUsage);
            string decalResref = null;
            if (decalPool.Count > 0)
            {
                if (urban)
                {
                    // Urban decal discipline (round 9): lower per-pile pad chance, inverse-usage
                    // rotation through the decal family, and NO Large floor plates under a 2m junk
                    // pile -- see PickUrbanDecal. Non-urban palettes keep the original chance,
                    // pick, and RNG path bit for bit.
                    if (rng.NextDouble() < UrbanDecalUnderPileChance)
                        decalResref = PickUrbanDecal(decalPool, areaUsage, rng, largeFactor: 0.35)?.Resref;
                }
                else if (rng.NextDouble() < DecalUnderPileChance)
                {
                    decalResref = PickWeighted(decalPool, rng);
                }
            }
            var hasDecal = decalResref != null;
            // Urban bearing rule: even the layered ground decal squares up with the grid (it reads
            // as floor marking/signage in the clean city palette, not a random dirt splash).
            var decalFacing = hasDecal
                ? urban ? 90f * rng.Next(4) : (float)(rng.NextDouble() * 360.0)
                : 0f;

            // Urban base bearing: the pile faces away from its anchoring wall (cardinal), and every
            // member shares it (with small jitter, or 90-degree turns in the cargo grid) -- the
            // hand-built same-resref orientation-coherence evidence (median 50% share of one
            // 15-degree bin). The Ruined profile's organic junk keeps full random spin instead.
            var baseFacing = wallDir != null
                ? CardinalFacing(-wallDir.Value.Dx, -wallDir.Value.Dy)
                : 0f;

            // Cargo grid (urban, non-organic, structure-frontage or wall-corner anchors): members
            // snap to a small lattice aligned with the anchoring wall/structure instead of a loose
            // disc -- the hand-built stacked-depot look (crate rows against tower bases). Lattice
            // axes derive from the quantized wall normal; rows stack away from the wall, columns run
            // along it.
            var wallNeighborCount = 0;
            foreach (var (dx, dy) in CardinalDirections)
            {
                if (!tileSet.Contains((anchor.X + dx, anchor.Y + dy)))
                    wallNeighborCount++;
            }

            var gridMode = urban && !organicSpin && (structureAdjacent || wallNeighborCount >= 2);

            var members = new List<PlannedDecoration>();
            // Size discipline (urban): at most one Large (3-8m) model per pile -- a second one can
            // only interpenetrate the first inside the pile's 2.8m radius -- plus the per-area cap
            // filter. Both are no-ops for palettes without sizes/caps, so non-urban member pools
            // (and RNG streams) are untouched. Ledger updates land at COMMIT time (a discarded
            // pile must not consume budget), so in-pile picks are additionally tracked in a
            // pending tally the cap filter adds on top of the committed ledger -- otherwise one
            // pile could overshoot a cap by its whole member count. When the pile's own 2-3-type
            // motif is fully capped out, member picks FALL BACK to the room's wider junk pool
            // instead of shrinking the pile -- caps redistribute variety, they must not thin the
            // hand-built pile density.
            var largeInPile = 0;
            var pendingUse = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            List<DungeonDecorationEntry> FilterMembers(List<DungeonDecorationEntry> source)
            {
                var filtered = source
                    .Where(e => e.MaxPerArea <= 0 ||
                                areaUsage.GetValueOrDefault(e.Resref) + pendingUse.GetValueOrDefault(e.Resref) < e.MaxPerArea)
                    .ToList();
                if (urban && largeInPile >= LargeMaxPerPile && filtered.Any(e => e.Size == DecorationSize.Large))
                    filtered = filtered.Where(e => e.Size != DecorationSize.Large).ToList();
                return filtered;
            }
            List<DungeonDecorationEntry> MemberPool()
            {
                var memberPool = FilterMembers(pileMotif);
                return memberPool.Count > 0 ? memberPool : FilterMembers(junkMotif);
            }
            if (gridMode)
            {
                var normalIndex = wallDir != null ? QuantizeDirection(wallDir.Value.Dx, wallDir.Value.Dy) : 3;
                var (nx, ny) = normalIndex switch
                {
                    0 => (1f, 0f),
                    1 => (-1f, 0f),
                    2 => (0f, 1f),
                    _ => (0f, -1f)
                };
                // Lateral axis runs along the wall (perpendicular to the normal).
                var (lx, ly) = (-ny, nx);

                const float gridPitch = 1.4f;
                const float gridJitter = 0.15f;
                var columns = Math.Clamp((int)Math.Ceiling(Math.Sqrt(memberCount)), 2, 3);

                for (var k = 0; k < memberCount; k++)
                {
                    var column = k % columns - (columns - 1) / 2f;
                    var row = k / columns;

                    var jx = (float)((rng.NextDouble() - 0.5) * 2.0 * gridJitter);
                    var jy = (float)((rng.NextDouble() - 0.5) * 2.0 * gridJitter);
                    var candidate = new Vector3(
                        center.X + lx * column * gridPitch - nx * row * gridPitch + jx,
                        center.Y + ly * column * gridPitch - ny * row * gridPitch + jy,
                        0f);

                    if (MathF.Abs(candidate.X - flat.X) > TileHalf - PileTileEdgeMargin ||
                        MathF.Abs(candidate.Y - flat.Y) > TileHalf - PileTileEdgeMargin)
                        continue;

                    var memberPool = MemberPool();
                    if (memberPool.Count == 0)
                        continue;

                    var memberResref = PickWeighted(memberPool, rng);
                    pendingUse[memberResref] = pendingUse.GetValueOrDefault(memberResref) + 1;
                    if (memberPool.First(e => e.Resref == memberResref).Size == DecorationSize.Large)
                        largeInPile++;
                    members.Add(new PlannedDecoration
                    {
                        Resref = memberResref,
                        Position = candidate,
                        // Shared bearing with occasional quarter turns -- still cardinal-aligned.
                        Facing = (baseFacing + 90f * rng.Next(2)) % 360f,
                        Context = DecorationContext.ClutterPile
                    });
                }
            }
            else
            {
                for (var i = 0; i < memberCount; i++)
                {
                    Vector3? position = null;
                    for (var attempt = 0; attempt < PilePlacementAttempts && position == null; attempt++)
                    {
                        var angle = rng.NextDouble() * Math.PI * 2.0;
                        // sqrt for a uniform disc rather than a center-heavy one.
                        var r = Math.Sqrt(rng.NextDouble()) * radius;
                        var candidate = new Vector3(
                            center.X + (float)(Math.Cos(angle) * r),
                            center.Y + (float)(Math.Sin(angle) * r),
                            0f);

                        if (MathF.Abs(candidate.X - flat.X) > TileHalf - PileTileEdgeMargin ||
                            MathF.Abs(candidate.Y - flat.Y) > TileHalf - PileTileEdgeMargin)
                            continue;

                        var tooClose = false;
                        foreach (var member in members)
                        {
                            var dx = member.Position.X - candidate.X;
                            var dy = member.Position.Y - candidate.Y;
                            if (dx * dx + dy * dy < PileMemberMinSeparation * PileMemberMinSeparation)
                            {
                                tooClose = true;
                                break;
                            }
                        }

                        if (!tooClose)
                            position = candidate;
                    }

                    if (position == null)
                        continue;

                    // Aligned members under the urban grammar (base bearing +-6 degrees stays inside
                    // the cardinal band); full random spin for non-urban tilesets and the sanctioned
                    // organic-junk profiles.
                    var facing = urban && !organicSpin
                        ? (baseFacing + (float)((rng.NextDouble() - 0.5) * 12.0) + 360f) % 360f
                        : (float)(rng.NextDouble() * 360.0);

                    var memberPool = MemberPool();
                    if (memberPool.Count == 0)
                        continue;

                    var memberResref = PickWeighted(memberPool, rng);
                    pendingUse[memberResref] = pendingUse.GetValueOrDefault(memberResref) + 1;
                    if (memberPool.First(e => e.Resref == memberResref).Size == DecorationSize.Large)
                        largeInPile++;
                    members.Add(new PlannedDecoration
                    {
                        Resref = memberResref,
                        Position = position.Value,
                        Facing = facing,
                        Context = DecorationContext.ClutterPile
                    });
                }
            }

            if (members.Count < PileCommitMinItems)
                return false;

            if (decalResref != null)
            {
                plan.Add(new PlannedDecoration
                {
                    Resref = decalResref,
                    Position = center,
                    Facing = decalFacing,
                    Context = DecorationContext.GroundDecal
                });
                RecordUse(areaUsage, decalResref);
            }

            plan.AddRange(members);
            // Ledger updates only on COMMIT -- a discarded 0-1-member pile must not consume
            // per-area budget.
            foreach (var member in members)
                RecordUse(areaUsage, member.Resref);
            return true;
        }

        /// <summary>
        /// Finds the first tile in a room (in stored, deterministic order) eligible to anchor a
        /// vignette: not excluded/CenterTile/centerpiece/already consumed, with a real wall direction.
        /// When <paramref name="urbanLayout"/> is non-null (urban grammar active), tiles carrying the
        /// road crosser are additionally skipped -- vignettes never squat on the clear street ribbon.
        /// </summary>
        internal static (int X, int Y)? FindVignetteAnchor(
            LayoutRoom room, HashSet<(int X, int Y)> tileSet, HashSet<(int X, int Y)> excluded,
            HashSet<(int X, int Y)> consumedTiles, (int X, int Y)? centerpieceAnchor,
            ResolvedLayout urbanLayout = null, string roadCrosser = "")
        {
            foreach (var tile in room.Tiles)
            {
                if (excluded.Contains(tile) || tile == room.CenterTile || tile == centerpieceAnchor || consumedTiles.Contains(tile))
                    continue;

                if (urbanLayout != null && TileCarriesRoadEdge(tile, urbanLayout, roadCrosser))
                    continue;

                if (NearestWallDirection(tile, tileSet) != null)
                    return tile;
            }

            return null;
        }

        /// <summary>
        /// Places every member of a vignette as a unit, anchored at <paramref name="anchor"/>: each
        /// member's declared (OffsetX, OffsetY) — authored as if the anchor faces "north" (+Y) into the
        /// room — is rotated to match the anchor's own wall-facing direction, so the grouping always
        /// opens into the room regardless of which wall it landed on.
        /// </summary>
        internal static void PlaceVignette(
            List<PlannedDecoration> plan, (int X, int Y) anchor, (float Dx, float Dy) wallDir, DungeonVignette vignette)
        {
            var anchorFlat = TileCenter(anchor.X, anchor.Y);
            var anchorPosition = new Vector3(
                anchorFlat.X + wallDir.Dx * WallOffset,
                anchorFlat.Y + wallDir.Dy * WallOffset,
                0f);
            var baseFacingRad = Math.Atan2(-wallDir.Dy, -wallDir.Dx);
            var baseFacingDeg = baseFacingRad * (180.0 / Math.PI);
            // Rotation matching the "author facing north" convention: north (0,1) maps onto the
            // anchor's own into-the-room direction (-wallDir).
            var cos = (float)Math.Cos(baseFacingRad - Math.PI / 2.0);
            var sin = (float)Math.Sin(baseFacingRad - Math.PI / 2.0);

            foreach (var member in vignette.Members)
            {
                var rotatedX = member.OffsetX * cos - member.OffsetY * sin;
                var rotatedY = member.OffsetX * sin + member.OffsetY * cos;

                plan.Add(new PlannedDecoration
                {
                    Resref = member.Resref,
                    Position = new Vector3(anchorPosition.X + rotatedX, anchorPosition.Y + rotatedY, 0f),
                    Facing = (float)(baseFacingDeg + member.FacingOffset),
                    Context = DecorationContext.WallAdjacent
                });
            }
        }

        internal static DungeonVignette PickWeightedVignette(List<DungeonVignette> vignettes, System.Random rng)
        {
            var total = vignettes.Sum(v => v.Weight);
            if (total <= 0)
                return vignettes[0];

            var roll = rng.Next(total);
            var cumulative = 0;
            foreach (var vignette in vignettes)
            {
                cumulative += vignette.Weight;
                if (roll < cumulative)
                    return vignette;
            }

            return vignettes[^1];
        }

        /// <summary>
        /// How far inside a stamped structure footprint's face a flush-anchored placement sits --
        /// the "against the building wall" tolerance the WallFlush anchoring contract promises (see
        /// <see cref="DecorationAnchoring.WallFlush"/>: hand-built flush cargo has median
        /// building-architecture distance ~0).
        /// </summary>
        internal const float FlushWallGap = 0.4f;

        /// <summary>
        /// Cardinal direction from a room tile toward an adjacent stamped structure footprint cell
        /// (deterministic probe order), or null when no cardinal neighbor is stamped structure --
        /// the eligibility/geometry anchor for <see cref="DecorationAnchoring.WallFlush"/> entries.
        /// Deliberately stricter than <see cref="IsStructureAdjacent"/> (which accepts diagonals):
        /// a flush placement needs a real FACE to sit against, not mere corner proximity.
        /// </summary>
        internal static (int Dx, int Dy)? FlushStructureDirection((int X, int Y) tile, ResolvedLayout layout)
        {
            var stamped = layout?.StampedStructureTiles;
            var placeable = layout?.PlaceableStructureCells;
            var hasStamped = stamped is { Count: > 0 };
            var hasPlaceable = placeable is { Count: > 0 };
            if (!hasStamped && !hasPlaceable)
                return null;

            foreach (var (dx, dy) in CardinalDirections)
            {
                var neighbor = (tile.X + dx, tile.Y + dy);
                if ((hasStamped && stamped.Contains(neighbor)) ||
                    (hasPlaceable && placeable.Contains(neighbor)))
                    return (dx, dy);
            }

            return null;
        }

        /// <summary>
        /// Builds one flush-anchored placement: the item stands <see cref="FlushWallGap"/> inside
        /// the shared face between its room tile and the adjacent stamped structure cell, bearing =
        /// that face's outward normal (cardinal, away from the structure, into the open) -- cargo
        /// stacked against a tower wall, never floating mid-room.
        /// </summary>
        private static PlannedDecoration BuildFlushStructurePlacement(
            (int X, int Y) tile, (int Dx, int Dy) structureDir, string resref, DecorationContext context)
        {
            var flatTile = TileCenter(tile.X, tile.Y);
            var position = new Vector3(
                flatTile.X + structureDir.Dx * (TileHalf - FlushWallGap),
                flatTile.Y + structureDir.Dy * (TileHalf - FlushWallGap),
                0f);

            return new PlannedDecoration
            {
                Resref = resref,
                Position = position,
                Facing = CardinalFacing(-structureDir.Dx, -structureDir.Dy),
                Context = context
            };
        }

        private static PlannedDecoration BuildWallHuggingPlacement(
            (int X, int Y) tile, (float Dx, float Dy) wallDir, string resref, DecorationContext context)
        {
            var flatTile = TileCenter(tile.X, tile.Y);
            var position = new Vector3(
                flatTile.X + wallDir.Dx * WallOffset,
                flatTile.Y + wallDir.Dy * WallOffset,
                0f);
            // Face away from the wall, into the room — hand-built wall-hugging pieces consistently
            // orient into open space, never into the wall.
            var facing = (float)(Math.Atan2(-wallDir.Dy, -wallDir.Dx) * (180.0 / Math.PI));

            return new PlannedDecoration
            {
                Resref = resref,
                Position = position,
                Facing = facing,
                Context = context
            };
        }

        /// <summary>
        /// Cardinal bearing (degrees) for a direction vector, quantized by dominant component --
        /// the urban placement grammar's bearing rule (hand-built city dressing is 73%
        /// cardinal-aligned: objects face the walls/roads/facades they belong to, not a random
        /// heading). Shares <see cref="QuantizeDirection"/>'s bucketing so a placement's bearing can
        /// never disagree with the run axis it was grouped under.
        /// </summary>
        internal static float CardinalFacing(float dx, float dy)
        {
            return QuantizeDirection(dx, dy) switch
            {
                0 => 0f,
                1 => 180f,
                2 => 90f,
                _ => 270f
            };
        }

        /// <summary>
        /// Road-margin facing rule (urban grammar): a placement standing on a tile that does NOT
        /// itself carry the road, but cardinally borders a road-carrying tile, faces INTO the street
        /// (the market-row look -- kiosks/planters/benches set back from the lane, fronting it).
        /// Returns false for on-road tiles (lamp-family fixtures keep their wall-derived cardinal
        /// bearing) and for tiles with no cardinal road neighbor. Deterministic: cardinal probe
        /// order breaks ties.
        /// </summary>
        private static bool TryGetRoadFacing((int X, int Y) tile, ResolvedLayout layout, string roadCrosser, out float facing)
        {
            facing = 0f;
            if (string.IsNullOrEmpty(roadCrosser) || TileCarriesRoadEdge(tile, layout, roadCrosser))
                return false;

            foreach (var (dx, dy) in CardinalDirections)
            {
                if (TileCarriesRoadEdge((tile.X + dx, tile.Y + dy), layout, roadCrosser))
                {
                    facing = CardinalFacing(dx, dy);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Builds one wall-hugging placement under the urban grammar: same wall-offset position as
        /// <see cref="BuildWallHuggingPlacement"/>, but the bearing is road-facing when the tile
        /// borders the carved street (see <see cref="TryGetRoadFacing"/>) and cardinal-quantized
        /// into-the-room otherwise -- never the raw diagonal a corner tile's averaged wall direction
        /// produces.
        /// </summary>
        private static PlannedDecoration BuildUrbanWallPlacement(
            (int X, int Y) tile, (float Dx, float Dy) wallDir, string resref, DecorationContext context,
            ResolvedLayout layout, string roadCrosser)
        {
            var flatTile = TileCenter(tile.X, tile.Y);
            var position = new Vector3(
                flatTile.X + wallDir.Dx * WallOffset,
                flatTile.Y + wallDir.Dy * WallOffset,
                0f);

            var facing = TryGetRoadFacing(tile, layout, roadCrosser, out var roadFacing)
                ? roadFacing
                : CardinalFacing(-wallDir.Dx, -wallDir.Dy);

            return new PlannedDecoration
            {
                Resref = resref,
                Position = position,
                Facing = facing,
                Context = context
            };
        }

        /// <summary>
        /// Road-integrity gate (urban grammar): a tile whose own edges carry the road crosser is
        /// part of the clear walkway ribbon -- only lamp-family entries flagged
        /// <see cref="DungeonDecorationEntry.AllowOnRoadSurface"/> may stand there. Returns the
        /// eligible subset for the tile (the full list off-road, the flagged subset on-road --
        /// possibly empty, in which case the tile hosts nothing).
        /// </summary>
        private static List<DungeonDecorationEntry> RoadSurfaceEligible(
            List<DungeonDecorationEntry> entries, (int X, int Y) tile, ResolvedLayout layout, string roadCrosser)
        {
            if (entries == null || !TileCarriesRoadEdge(tile, layout, roadCrosser))
                return entries;

            return entries.Where(e => e.AllowOnRoadSurface).ToList();
        }

        /// <summary>
        /// Buckets a (possibly diagonal, corner-tile) wall direction into one of four cardinal run
        /// axes by dominant component: 0 = +X wall, 1 = -X wall, 2 = +Y wall, 3 = -Y wall. Ties
        /// (exact diagonals) resolve to the X-axis bucket.
        /// </summary>
        internal static int QuantizeDirection(float dx, float dy)
        {
            if (MathF.Abs(dx) >= MathF.Abs(dy))
                return dx >= 0 ? 0 : 1;
            return dy >= 0 ? 2 : 3;
        }

        /// <summary>
        /// Resolves the placement context a wall-eligible tile falls into (StructureAdjacent within
        /// one cell of a stamped OpenSetPiece building footprint when the palette curates that bucket
        /// -- see <see cref="IsStructureAdjacent"/>; else CorridorSide for corridor-like rooms OR a
        /// tile within one cell of a carved road edge -- see <see cref="IsRoadAdjacent"/>; else
        /// WallAdjacent; upgraded to DoorwayFlank near a transition) and the curated palette entries
        /// for that bucket, falling back to WallAdjacent when a family never curated the more
        /// specific bucket so a sparse palette still decorates rather than going silent. The
        /// StructureAdjacent branch is additionally gated on the palette actually curating that
        /// bucket: many non-city tilesets stamp OpenSetPieces too, and without the gate their
        /// structure-adjacent tiles would silently reroute out of their curated CorridorSide/
        /// WallAdjacent buckets the moment the enum value existed. Returns false (no eligible entries
        /// at all) when even the WallAdjacent fallback is empty. Shared by both the pass-1
        /// eligibility count and the wall-run assembly so they can never resolve a tile's
        /// context/entries differently from each other.
        /// </summary>
        private static bool TryResolveContext(
            (int X, int Y) tile, bool isCorridorLike, ResolvedLayout layout, string roadCrosser,
            Dictionary<DecorationContext, List<DungeonDecorationEntry>> byContext,
            out DecorationContext context, out List<DungeonDecorationEntry> entries)
        {
            var structureCurated = byContext.TryGetValue(DecorationContext.StructureAdjacent, out var structureEntries) &&
                                   structureEntries.Count > 0;

            // Priority: road-side beats structure-side -- a city building's street frontage tile is
            // usually BOTH road- and structure-adjacent, and giving StructureAdjacent precedence
            // measured it absorbing over half of ALL placements on fcx01 at 32x32 (1573/2853,
            // collapsing the street-furniture CorridorSide bucket 1163 -> 244); with road first, the
            // street keeps its streetlight/kiosk dressing and StructureAdjacent dresses the
            // off-street building flanks.
            context = isCorridorLike || IsRoadAdjacent(tile, layout, roadCrosser)
                ? DecorationContext.CorridorSide
                : structureCurated && IsStructureAdjacent(tile, layout)
                    ? DecorationContext.StructureAdjacent
                    : DecorationContext.WallAdjacent;
            if (IsNearDoorway(tile, layout))
                context = DecorationContext.DoorwayFlank;

            if (byContext.TryGetValue(context, out entries) && entries.Count > 0)
                return true;

            if (context != DecorationContext.WallAdjacent &&
                byContext.TryGetValue(DecorationContext.WallAdjacent, out entries) && entries.Count > 0)
            {
                context = DecorationContext.WallAdjacent;
                return true;
            }

            entries = null;
            return false;
        }

        /// <summary>
        /// Finds the room tile closest (Euclidean, tile-grid distance) to <paramref name="center"/>,
        /// excluding <paramref name="center"/> itself and any excluded tile — used to anchor a
        /// RoomCenter decoration on a real neighboring tile rather than sharing CenterTile's own cell.
        /// Ties broken by List order, so this is deterministic given a fixed room.Tiles ordering.
        /// </summary>
        private static (int X, int Y)? NearestOtherTile(
            (int X, int Y) center, List<(int X, int Y)> tiles, HashSet<(int X, int Y)> excluded)
        {
            (int X, int Y)? best = null;
            var bestDistSq = int.MaxValue;

            foreach (var tile in tiles)
            {
                if (tile == center || excluded.Contains(tile))
                    continue;

                var dx = tile.X - center.X;
                var dy = tile.Y - center.Y;
                var distSq = dx * dx + dy * dy;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    best = tile;
                }
            }

            return best;
        }

        private static HashSet<(int X, int Y)> BuildExclusionSet(ResolvedLayout layout)
        {
            var excluded = new HashSet<(int X, int Y)>();
            foreach (var transition in layout.Transitions)
            {
                excluded.Add(transition.Tile);
                if (transition.Style is TransitionStyle.Door or TransitionStyle.GroupExit)
                {
                    excluded.Add(transition.DoorCell);
                    excluded.Add(transition.DoorwayCell);
                }
            }

            return excluded;
        }

        private static (int MinX, int MaxX, int MinY, int MaxY) BoundingBox(List<(int X, int Y)> tiles)
        {
            var minX = int.MaxValue;
            var maxX = int.MinValue;
            var minY = int.MaxValue;
            var maxY = int.MinValue;

            foreach (var (x, y) in tiles)
            {
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }

            return (minX, maxX, minY, maxY);
        }

        /// <summary>
        /// Direction (unit-ish vector) pointing from a room tile toward its nearest "wall" — any of
        /// the four cardinal neighbors that is NOT part of this room's own tile set (either a real
        /// solid wall, or a corridor/foreign-room gap, both reasonable wall-hugging anchors for set
        /// dressing). Corner tiles average their two-plus wall directions into a diagonal. Returns
        /// null for a fully interior tile (every cardinal neighbor is in-room).
        /// </summary>
        private static (float Dx, float Dy)? NearestWallDirection((int X, int Y) tile, HashSet<(int X, int Y)> tileSet)
        {
            var directions = CardinalDirections;
            float sumX = 0, sumY = 0;
            var found = false;
            (int Dx, int Dy) first = default;

            foreach (var (dx, dy) in directions)
            {
                var neighbor = (tile.X + dx, tile.Y + dy);
                if (tileSet.Contains(neighbor))
                    continue;

                if (!found)
                {
                    first = (dx, dy);
                    found = true;
                }

                sumX += dx;
                sumY += dy;
            }

            if (!found)
                return null;

            var length = MathF.Sqrt(sumX * sumX + sumY * sumY);
            if (length < 0.1f)
                return (first.Dx, first.Dy);

            return (sumX / length, sumY / length);
        }

        private static bool IsNearDoorway((int X, int Y) tile, ResolvedLayout layout)
        {
            foreach (var transition in layout.Transitions)
            {
                if (Chebyshev(tile, transition.Tile) <= 1)
                    return true;

                if (transition.Style is TransitionStyle.Door or TransitionStyle.GroupExit)
                {
                    if (Chebyshev(tile, transition.DoorCell) <= 1)
                        return true;
                    if (Chebyshev(tile, transition.DoorwayCell) <= 1)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// True when <paramref name="tile"/> itself, or any of its 8 Chebyshev-1 neighbor cells, carries
        /// <paramref name="roadCrosser"/> on at least one edge (see LayoutRoadCarver, which writes the
        /// composed tileset's RoadCrosser -- e.g. fcx01's "Routes" -- into the layout's shared
        /// EdgeCrosserGrid). Matches the hand-built-vs-generated decoration-to-road adjacency
        /// methodology used by the tile-composition statistics harness (within-1-tile of a road edge),
        /// so a room tile that reads visually as "along the street" gets routed to the CorridorSide
        /// decoration bucket (see TryResolveContext) regardless of the owning room's own shape.
        /// Returns false immediately when the tileset never declared a RoadCrosser (roadCrosser empty)
        /// or the layout carries no crosser grid (defensive; MacroLayout always allocates one, but a
        /// hand-crafted ResolvedLayout in a unit test may not).
        /// </summary>
        internal static bool IsRoadAdjacent((int X, int Y) tile, ResolvedLayout layout, string roadCrosser)
        {
            if (string.IsNullOrEmpty(roadCrosser) || layout?.Crossers == null)
                return false;

            var crossers = layout.Crossers;

            bool IsRoadTile(int x, int y)
            {
                if (x < 0 || y < 0 || x >= crossers.Width || y >= crossers.Height)
                    return false;

                for (var slot = 0; slot < 4; slot++)
                {
                    if (string.Equals(crossers.GetEdge(x, y, slot), roadCrosser, System.StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                return false;
            }

            for (var dx = -1; dx <= 1; dx++)
            for (var dy = -1; dy <= 1; dy++)
            {
                if (IsRoadTile(tile.X + dx, tile.Y + dy))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// True when <paramref name="tile"/> sits within Chebyshev distance 1 of a stamped
        /// OpenSetPiece structure footprint cell (see ResolvedLayout.StampedStructureTiles) -- the
        /// tile reads visually as a building's frontage/flank, so decoration curated as
        /// StructureAdjacent (building lamps, frontage container stacks) anchors against the
        /// structure rather than free-standing along an unrelated room divider. Mirrors
        /// <see cref="IsRoadAdjacent"/>'s within-1-tile convention.
        /// </summary>
        internal static bool IsStructureAdjacent((int X, int Y) tile, ResolvedLayout layout)
        {
            // Placeable frontage buildings (see BuildingFrontagePlanner /
            // ResolvedLayout.PlaceableStructureCells) count as structure anchors alongside stamped
            // tile footprints: the hand-built evidence stacks flush cargo and frontage dressing
            // against swd_build* placeable bases exactly as against tile towers.
            return IsWithin1(tile, layout?.StampedStructureTiles) ||
                   IsWithin1(tile, layout?.PlaceableStructureCells);
        }

        private static bool IsWithin1((int X, int Y) tile, HashSet<(int X, int Y)> cells)
        {
            if (cells == null || cells.Count == 0)
                return false;

            for (var dx = -1; dx <= 1; dx++)
            for (var dy = -1; dy <= 1; dy++)
            {
                if (cells.Contains((tile.X + dx, tile.Y + dy)))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// True when this one tile itself carries <paramref name="roadCrosser"/> on any edge -- the
        /// radius-0 companion to <see cref="IsRoadAdjacent"/>, used by courtyard placement so an
        /// arrangement never stands ON the street surface while road-ADJACENT interior space (the
        /// only interior generated city plazas have -- see CourtyardInteriorClearance) stays usable.
        /// </summary>
        internal static bool TileCarriesRoadEdge((int X, int Y) tile, ResolvedLayout layout, string roadCrosser)
        {
            if (string.IsNullOrEmpty(roadCrosser) || layout?.Crossers == null)
                return false;
            if (tile.X < 0 || tile.Y < 0 || tile.X >= layout.Crossers.Width || tile.Y >= layout.Crossers.Height)
                return false;

            for (var slot = 0; slot < 4; slot++)
            {
                if (string.Equals(layout.Crossers.GetEdge(tile.X, tile.Y, slot), roadCrosser, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static int Chebyshev((int X, int Y) a, (int X, int Y) b)
        {
            return Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
        }

        private static Vector3 TileCenter(int tileX, int tileY)
        {
            return new Vector3(tileX * TileSize + TileHalf, tileY * TileSize + TileHalf, 0f);
        }

        private static string PickWeighted(List<DungeonDecorationEntry> entries, System.Random rng)
        {
            var total = entries.Sum(e => e.Weight);
            if (total <= 0)
                return entries[0].Resref;

            var roll = rng.Next(total);
            var cumulative = 0;

            foreach (var entry in entries)
            {
                cumulative += entry.Weight;
                if (roll < cumulative)
                    return entry.Resref;
            }

            return entries[^1].Resref;
        }
    }
}
