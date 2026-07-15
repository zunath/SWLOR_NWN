using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

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
    /// planters, crates, wall clutter — instead of bare geometry. Runs on the layout's own seeded
    /// RNG stream (Seed) the same way DungeonContentPlacer.Populate seeds its own ambient/boss/
    /// treasure content, so a given seed always plans the identical set of decorations.
    ///
    /// Evidence: curated per-theme palettes and base densities were mined from ~440 hand-built
    /// SWLOR reference areas (placeable resref frequency, density-per-tile, and an edge-vs-center
    /// tile-local position proxy) — see the decoration_evidence/ scratchpad data and per-theme
    /// DungeonDefinition doc comments for the specific reference areas each palette draws from.
    ///
    /// Exclusions (never decorated):
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

        /// <summary>A room counts as corridor-like when its shorter bounding-box axis is this narrow.</summary>
        private const int CorridorLikeMaxSpan = 2;

        /// <summary>Salt XORed into the layout seed so this pass draws a different RNG stream than
        /// DungeonContentPlacer's tier-scaled content pass (which uses seed ^ (tier * 397)).</summary>
        private const int SeedSalt = 0x0EC0;

        private static readonly (int Dx, int Dy)[] CardinalDirections =
        {
            (1, 0), (-1, 0), (0, 1), (0, -1)
        };

        /// <summary>
        /// Plans the decoration pass for a resolved layout. Deterministic: identical
        /// (layout.Seed, detail, densityPercent) always produces an identical plan, in the same order.
        /// Returns an empty plan when the theme has no curated palette or densityPercent is 0 (the
        /// toggle-off case).
        ///
        /// Calibration: DungeonDetail.DecorationBaseDensity is evidence-derived as placeables PER TOTAL
        /// AREA TILE (layout.Width * layout.Height) from the hand-built reference areas — see
        /// decoration_evidence/mine_evidence.py's own "density: decorative placeables per tile (area
        /// Width*Height)" convention. The eligible tile POOL this planner can actually decorate (room
        /// perimeter cells with a curated palette bucket) is a much smaller fraction of the total area
        /// (corridors carved outside LayoutRooms, interior room tiles, and every excluded cell are never
        /// eligible — see the class doc comment). Applying baseDensity directly as a per-eligible-tile
        /// coin flip therefore collapses the realized count to a small fraction of the evidence target.
        /// Plan() instead runs two passes over the layout: PASS 1 (no RNG) counts the real eligible pool
        /// size for both the wall/corridor/doorway "hugging" placements and the RoomCenter centerpiece
        /// slots; PASS 2 derives a per-slot probability (targetCount / eligibleCount, capped at 1) that
        /// makes the EXPECTED realized count converge on baseDensity * totalTiles * (densityPercent/100),
        /// then rolls the same RNG stream the original single-pass algorithm did to actually place.
        /// </summary>
        public static List<PlannedDecoration> Plan(ResolvedLayout layout, DungeonDetail detail, int densityPercent)
        {
            var plan = new List<PlannedDecoration>();
            if (layout == null || detail == null || detail.Decorations.Count == 0 || densityPercent <= 0)
                return plan;

            var densityFraction = densityPercent / 100.0;
            var targetCount = detail.DecorationBaseDensity * layout.Width * layout.Height * densityFraction;
            if (targetCount <= 0)
                return plan;

            var byContext = detail.Decorations
                .GroupBy(d => d.Context)
                .ToDictionary(g => g.Key, g => g.ToList());

            var excluded = BuildExclusionSet(layout);

            // Precompute each non-set-piece room's shape classification once — reused by both the
            // eligibility count (pass 1) and the actual placement rolls (pass 2) so they can never
            // drift out of sync with each other.
            var rooms = new List<(LayoutRoom Room, bool IsCorridorLike, HashSet<(int X, int Y)> TileSet)>();
            foreach (var room in layout.Rooms)
            {
                if (room.IsSetPiece || room.Tiles.Count == 0)
                    continue;

                var (minX, maxX, minY, maxY) = BoundingBox(room.Tiles);
                var spanX = maxX - minX + 1;
                var spanY = maxY - minY + 1;
                rooms.Add((room, Math.Min(spanX, spanY) <= CorridorLikeMaxSpan, new HashSet<(int X, int Y)>(room.Tiles)));
            }

            // PASS 1: count the eligible pool for each bucket, ignoring RNG entirely — the centerpiece
            // anchor's one-tile exclusion from the wall pool is intentionally not modeled here (it can
            // remove at most one tile per qualifying room, negligible against the pool this normalizes).
            var wallEligibleCount = 0;
            var centerEligibleRoomCount = 0;
            foreach (var (room, isCorridorLike, tileSet) in rooms)
            {
                if (!isCorridorLike && room.Tiles.Count >= MinCenterpieceRoomTiles &&
                    byContext.TryGetValue(DecorationContext.RoomCenter, out var centerEntriesProbe) &&
                    centerEntriesProbe.Count > 0 &&
                    NearestOtherTile(room.CenterTile, room.Tiles, excluded) != null)
                    centerEligibleRoomCount++;

                foreach (var tile in room.Tiles)
                {
                    if (excluded.Contains(tile) || tile == room.CenterTile)
                        continue;

                    if (NearestWallDirection(tile, tileSet) == null)
                        continue;

                    if (TryResolveContext(tile, isCorridorLike, layout, byContext, out _, out _))
                        wallEligibleCount++;
                }
            }

            var centerTarget = targetCount * CenterpieceTargetShare;
            var wallTarget = targetCount - centerTarget;
            var wallProbability = wallEligibleCount > 0 ? Math.Min(1.0, wallTarget / wallEligibleCount) : 0.0;
            var centerProbability = centerEligibleRoomCount > 0 ? Math.Min(0.95, centerTarget / centerEligibleRoomCount) : 0.0;

            var rng = new System.Random(layout.Seed ^ SeedSalt);

            // PASS 2: the actual placement rolls, over the same room/tile order pass 1 used.
            foreach (var (room, isCorridorLike, tileSet) in rooms)
            {
                (int X, int Y)? centerpieceAnchor = null;

                if (!isCorridorLike && room.Tiles.Count >= MinCenterpieceRoomTiles &&
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

                foreach (var tile in room.Tiles)
                {
                    if (excluded.Contains(tile) || tile == room.CenterTile || tile == centerpieceAnchor)
                        continue;

                    var wallDir = NearestWallDirection(tile, tileSet);
                    if (wallDir == null)
                        continue; // fully interior tile with no adjacent solid/foreign edge in this room

                    if (!TryResolveContext(tile, isCorridorLike, layout, byContext, out var context, out var entries))
                        continue;

                    if (rng.NextDouble() >= wallProbability)
                        continue;

                    var resrefPick = PickWeighted(entries, rng);
                    var flatTile = TileCenter(tile.X, tile.Y);
                    var (dx, dy) = wallDir.Value;
                    var position = new Vector3(
                        flatTile.X + dx * WallOffset,
                        flatTile.Y + dy * WallOffset,
                        0f);
                    // Face away from the wall, into the room — hand-built wall-hugging pieces
                    // consistently orient into open space, never into the wall.
                    var facing = (float)(Math.Atan2(-dy, -dx) * (180.0 / Math.PI));

                    plan.Add(new PlannedDecoration
                    {
                        Resref = resrefPick,
                        Position = position,
                        Facing = facing,
                        Context = context
                    });
                }
            }

            return plan;
        }

        /// <summary>
        /// Resolves the placement context a wall-eligible tile falls into (CorridorSide for
        /// corridor-like rooms, else WallAdjacent, upgraded to DoorwayFlank near a transition) and the
        /// curated palette entries for that bucket, falling back to WallAdjacent when a theme never
        /// curated the more specific bucket so a sparse palette still decorates rather than going
        /// silent. Returns false (no eligible entries at all) when even the WallAdjacent fallback is
        /// empty. Shared by both the pass-1 eligibility count and the pass-2 placement roll so they can
        /// never resolve a tile's context/entries differently from each other.
        /// </summary>
        private static bool TryResolveContext(
            (int X, int Y) tile, bool isCorridorLike, ResolvedLayout layout,
            Dictionary<DecorationContext, List<DungeonDecorationEntry>> byContext,
            out DecorationContext context, out List<DungeonDecorationEntry> entries)
        {
            context = isCorridorLike ? DecorationContext.CorridorSide : DecorationContext.WallAdjacent;
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
