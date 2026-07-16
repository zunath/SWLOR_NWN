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
        /// (layout.Seed, tileset, detail, densityPercent) always produces an identical plan, in the
        /// same order. Returns an empty plan when the merged palette (tileset family palette + theme
        /// accents) is empty or densityPercent is 0 (the toggle-off case).
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
        public static List<PlannedDecoration> Plan(ResolvedLayout layout, DungeonTilesetProfile tileset, DungeonDetail detail, int densityPercent)
        {
            var plan = new List<PlannedDecoration>();
            if (layout == null || detail == null || densityPercent <= 0)
                return plan;

            var palette = MergePalette(tileset, detail);
            if (palette.Count == 0)
                return plan;

            var densityFraction = densityPercent / 100.0;
            var targetCount = detail.DecorationBaseDensity * layout.Width * layout.Height * densityFraction;
            if (targetCount <= 0)
                return plan;

            var byContext = palette
                .GroupBy(d => d.Context)
                .ToDictionary(g => g.Key, g => g.ToList());

            var vignettes = tileset?.Vignettes ?? new List<DungeonVignette>();
            var roadCrosser = tileset?.RoadCrosser ?? string.Empty;

            var excluded = BuildExclusionSet(layout);

            // Courtyards only exist for palettes that curate BOTH courtyard buckets -- everything
            // below (anchor search, budget share, per-room roll) is skipped entirely otherwise, so a
            // palette without courtyards keeps its exact pre-courtyard budget split and RNG sequence.
            byContext.TryGetValue(DecorationContext.CourtyardCenter, out var courtyardCenterEntries);
            byContext.TryGetValue(DecorationContext.Courtyard, out var courtyardRingEntries);
            var courtyardsCurated = courtyardCenterEntries is { Count: > 0 } && courtyardRingEntries is { Count: > 0 };

            // Precompute each non-set-piece room's shape classification once — reused across every
            // pass so they can never drift out of sync with each other.
            var rooms = new List<(LayoutRoom Room, bool IsCorridorLike, HashSet<(int X, int Y)> TileSet, (int X, int Y)? CourtyardAnchor)>();
            foreach (var room in layout.Rooms)
            {
                if (room.IsSetPiece || room.Tiles.Count == 0)
                    continue;

                var (minX, maxX, minY, maxY) = BoundingBox(room.Tiles);
                var spanX = maxX - minX + 1;
                var spanY = maxY - minY + 1;
                var isCorridorLike = Math.Min(spanX, spanY) <= CorridorLikeMaxSpan;
                var roomTileSet = new HashSet<(int X, int Y)>(room.Tiles);
                var courtyardAnchor = courtyardsCurated && !isCorridorLike
                    ? FindCourtyardAnchor(room, roomTileSet, excluded, layout, roadCrosser)
                    : null;
                rooms.Add((room, isCorridorLike, roomTileSet, courtyardAnchor));
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
            var courtyardEligibleRoomCount = rooms.Count(r => r.CourtyardAnchor != null);
            foreach (var (room, isCorridorLike, tileSet, _) in rooms)
            {
                if (!isCorridorLike && room.Tiles.Count >= MinCenterpieceRoomTiles &&
                    byContext.TryGetValue(DecorationContext.RoomCenter, out var centerEntriesProbe) &&
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

                    if (TryResolveContext(tile, isCorridorLike, layout, roadCrosser, byContext, out _, out _))
                    {
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
            // Strictly zero unless the palette curates courtyards AND a room can host one -- see
            // CourtyardTargetShare's doc comment (palettes without courtyards keep the exact
            // pre-courtyard budget split).
            var courtyardTarget = courtyardsCurated && courtyardEligibleRoomCount > 0
                ? targetCount * CourtyardTargetShare
                : 0.0;
            var wallTarget = Math.Max(0.0, targetCount - centerTarget - doorwayFlankTarget - vignetteTarget - courtyardTarget);

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

            var rng = new System.Random(layout.Seed ^ SeedSalt);
            var consumedTiles = new HashSet<(int X, int Y)>();

            // PASS 2a: doorway flank pairs, in deterministic transition order, BEFORE the per-room pass
            // so consumedTiles is fully populated before any room decides its own wall-run placements.
            byContext.TryGetValue(DecorationContext.DoorwayFlank, out var doorwayEntries);
            var doorwayFallback = doorwayEntries is { Count: > 0 } ? doorwayEntries : byContext.GetValueOrDefault(DecorationContext.WallAdjacent);
            foreach (var pair in doorwayFlankPairs)
            {
                if (rng.NextDouble() >= doorwayFlankProbability || doorwayFallback == null || doorwayFallback.Count == 0)
                    continue;

                var resref = PickWeighted(doorwayFallback, rng);
                foreach (var tile in new[] { pair.A, pair.B })
                {
                    var roomIndex = tileToRoom[tile];
                    var wallDir = NearestWallDirection(tile, rooms[roomIndex].TileSet);
                    if (wallDir == null)
                        continue;

                    plan.Add(BuildWallHuggingPlacement(tile, wallDir.Value, resref, DecorationContext.DoorwayFlank));
                    consumedTiles.Add(tile);
                }
            }

            // PASS 2b: per-room centerpiece, vignette, then rhythmic wall/corridor runs.
            var motifCache = new Dictionary<(int RoomId, DecorationContext Context), List<string>>();

            foreach (var (room, isCorridorLike, tileSet, courtyardAnchor) in rooms)
            {
                (int X, int Y)? centerpieceAnchor = null;

                // Courtyard first: when one lands, it IS this room's interior arrangement -- the
                // plain single-item centerpiece roll is skipped for the room (its centerpiece slot is
                // the courtyard's own center) so the two interior mechanisms never double-dress the
                // same plaza.
                var courtyardPlaced = false;
                if (courtyardAnchor != null && courtyardProbability > 0 && rng.NextDouble() < courtyardProbability)
                {
                    courtyardPlaced = PlanCourtyard(
                        plan, room, tileSet, courtyardAnchor.Value, courtyardCenterEntries, courtyardRingEntries,
                        excluded, consumedTiles, layout, roadCrosser, rng);
                    if (courtyardPlaced)
                        centerpieceAnchor = courtyardAnchor;
                }

                if (!courtyardPlaced && !isCorridorLike && room.Tiles.Count >= MinCenterpieceRoomTiles &&
                    byContext.TryGetValue(DecorationContext.RoomCenter, out var centerEntries) &&
                    centerEntries.Count > 0 &&
                    rng.NextDouble() < centerProbability)
                {
                    // Never the CenterTile itself — that cell is reserved for boss/treasure/exit
                    // content placement (see DungeonContentPlacer.PopulateBossRoom/PlaceExit) — so
                    // pick the nearest OTHER room tile to stand the centerpiece on instead.
                    var anchor = NearestOtherTile(room.CenterTile, room.Tiles, excluded);
                    if (anchor != null)
                    {
                        centerpieceAnchor = anchor;
                        var resref = PickWeighted(centerEntries, rng);
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
                    }
                }

                if (vignettes.Count > 0 && rng.NextDouble() < vignetteProbability)
                {
                    var anchor = FindVignetteAnchor(room, tileSet, excluded, consumedTiles, centerpieceAnchor);
                    if (anchor != null)
                    {
                        var vignette = PickWeightedVignette(vignettes, rng);
                        var wallDir = NearestWallDirection(anchor.Value, tileSet)!.Value;
                        PlaceVignette(plan, anchor.Value, wallDir, vignette);
                        consumedTiles.Add(anchor.Value);
                    }
                }

                PlaceWallRuns(plan, room, tileSet, isCorridorLike, layout, roadCrosser, byContext, excluded,
                    consumedTiles, centerpieceAnchor, wallProbability, rng, motifCache);
            }

            return plan;
        }

        /// <summary>
        /// Merges the tileset family's bulk palette (see DungeonTilesetProfile.Decorations) with the
        /// theme's small accent list (DungeonDetail.Decorations) into one weighted pool per
        /// DecorationContext. The tileset supplies the visual bulk; the theme layers a few
        /// genuinely-theme-flavored extras on top — neither source alone need be exhaustive.
        /// </summary>
        private static List<DungeonDecorationEntry> MergePalette(DungeonTilesetProfile tileset, DungeonDetail detail)
        {
            var merged = new List<DungeonDecorationEntry>();
            if (tileset?.Decorations != null)
                merged.AddRange(tileset.Decorations);
            if (detail?.Decorations != null)
                merged.AddRange(detail.Decorations);
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
            List<(LayoutRoom Room, bool IsCorridorLike, HashSet<(int X, int Y)> TileSet, (int X, int Y)? CourtyardAnchor)> rooms)
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
            Dictionary<(int RoomId, DecorationContext Context), List<string>> motifCache)
        {
            if (wallProbability <= 0)
                return;

            // Bucket this room's eligible tiles by (context, quantized wall direction) — each bucket is
            // one straight run. Iterate room.Tiles in its own stored (deterministic) order so bucket
            // membership order, and therefore the whole pass, stays reproducible per seed.
            var runs = new Dictionary<(DecorationContext Context, int Direction), List<(int X, int Y)>>();

            foreach (var tile in room.Tiles)
            {
                if (excluded.Contains(tile) || tile == room.CenterTile || tile == centerpieceAnchor || consumedTiles.Contains(tile))
                    continue;

                var wallDir = NearestWallDirection(tile, tileSet);
                if (wallDir == null)
                    continue;

                if (!TryResolveContext(tile, isCorridorLike, layout, roadCrosser, byContext, out var context, out _))
                    continue;

                var direction = QuantizeDirection(wallDir.Value.Dx, wallDir.Value.Dy);
                var key = (context, direction);
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

            foreach (var ((context, direction), tiles) in runs.OrderBy(kv => kv.Key.Direction).ThenBy(kv => kv.Key.Context))
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
                var jitterRange = Math.Max(1, spacing);
                var i = rng.Next(spacing);
                var segmentLength = 0;

                while (i < ordered.Count)
                {
                    var tile = ordered[i];
                    var wallDir = NearestWallDirection(tile, tileSet);
                    if (wallDir != null)
                    {
                        var resref = PickResrefUnderRoomCap(motifEntries, secondaryMotifEntries, entries, resrefUsageCounts, context, rng);
                        if (resref != null)
                        {
                            plan.Add(BuildWallHuggingPlacement(tile, wallDir.Value, resref, context));
                            segmentLength++;
                        }
                    }

                    var step = Math.Max(1, spacing + rng.Next(-jitterRange, jitterRange + 1));
                    if (segmentLength >= MaxRunSegmentLength)
                    {
                        // Force a real gap before starting the next segment — this is what makes a
                        // long wall read as a few distinct dressed clusters instead of one continuous
                        // run (or, worst case, a run that wraps the room's entire perimeter).
                        step += spacing * (RunSegmentGapExtraSteps + 1);
                        segmentLength = 0;
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
        /// resort so a heavily-capped room still keeps dressing rather than stopping early. Returns
        /// null (place nothing at this tile) only once every candidate for this room+context is at cap.
        /// </summary>
        private static string PickResrefUnderRoomCap(
            List<DungeonDecorationEntry> motifEntries, List<DungeonDecorationEntry> secondaryMotifEntries,
            List<DungeonDecorationEntry> fallbackEntries,
            Dictionary<(DecorationContext Context, string Resref), int> resrefUsageCounts,
            DecorationContext context, System.Random rng)
        {
            bool UnderCap(DungeonDecorationEntry e) =>
                resrefUsageCounts.GetValueOrDefault((context, e.Resref)) < MaxSameResrefPerRoomContext;

            var available = motifEntries.Where(UnderCap).ToList();
            if (available.Count == 0)
                available = secondaryMotifEntries.Where(UnderCap).ToList();
            if (available.Count == 0)
                available = fallbackEntries.Where(UnderCap).ToList();
            if (available.Count == 0)
                return null;

            var resref = PickWeighted(available, rng);
            resrefUsageCounts[(context, resref)] = resrefUsageCounts.GetValueOrDefault((context, resref)) + 1;
            return resref;
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
            ResolvedLayout layout, string roadCrosser, System.Random rng)
        {
            var centerResref = PickWeighted(centerEntries, rng);
            var center = TileCenter(anchor.X, anchor.Y);

            // Room-area scaling: a 5x5-tile room rings 4-5 items, a 9x9 plaza rings the full 8.
            var ringCount = Math.Clamp(3 + room.Tiles.Count / 16, CourtyardMinRingItems, CourtyardMaxRingItems);
            var radius = CourtyardBaseRadius + (float)(rng.NextDouble() * CourtyardRadiusJitter);

            // Mixed-resref ring motif: 2-3 distinct resrefs cycled around the ring (weighted sample
            // without replacement), matching the hand-built mixed-composition evidence. A palette
            // with a single curated ring resref still works (the hand-built sample includes one
            // all-light-pole ring too).
            var motifSize = Math.Min(3, ringEntries.Select(e => e.Resref).Distinct().Count());
            var pool = new List<DungeonDecorationEntry>(ringEntries);
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

            if (members.Count < 3)
                return false;

            plan.Add(new PlannedDecoration
            {
                Resref = centerResref,
                Position = center,
                Facing = (float)(rng.NextDouble() * 360.0),
                Context = DecorationContext.CourtyardCenter
            });
            plan.AddRange(members);

            consumedTiles.Add(anchor);
            foreach (var tile in memberTiles)
                consumedTiles.Add(tile);

            return true;
        }

        /// <summary>
        /// Finds the first tile in a room (in stored, deterministic order) eligible to anchor a
        /// vignette: not excluded/CenterTile/centerpiece/already consumed, with a real wall direction.
        /// </summary>
        internal static (int X, int Y)? FindVignetteAnchor(
            LayoutRoom room, HashSet<(int X, int Y)> tileSet, HashSet<(int X, int Y)> excluded,
            HashSet<(int X, int Y)> consumedTiles, (int X, int Y)? centerpieceAnchor)
        {
            foreach (var tile in room.Tiles)
            {
                if (excluded.Contains(tile) || tile == room.CenterTile || tile == centerpieceAnchor || consumedTiles.Contains(tile))
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
            var stamped = layout?.StampedStructureTiles;
            if (stamped == null || stamped.Count == 0)
                return false;

            for (var dx = -1; dx <= 1; dx++)
            for (var dy = -1; dy <= 1; dy++)
            {
                if (stamped.Contains((tile.X + dx, tile.Y + dy)))
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
