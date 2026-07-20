using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SWLOR.Game.Server.Service.AreaGenerationService
{
    /// <summary>
    /// Plans the structural BUILDING-PLACEABLE FRONTAGE pass for urban tilesets that declare
    /// <see cref="DungeonTilesetProfile.FrontageBuildings"/>: skyscraper/tower placeables erected
    /// along open-area perimeter edges and street margins so streets and plazas read as canyons
    /// walled by building mass -- the hand-built promenade-family mechanism.
    ///
    /// Evidence (_scratch_decor/r11_mine_buildings.py over pw_ar_narpromena / pw_ar_narscorpd /
    /// pw_ar_nsshipyard): the 12x12 flagship promenade walls its plaza with 30 swd_build*
    /// placeables standing on flat cobble (ZERO building tiles) -- swd_build007 rows at 9.8-10.1m
    /// center pitch (one per 10m tile edge, footprints overlapping laterally into a continuous
    /// wall), 100% cardinal-quantized bearings, centers concentrated on tile boundaries
    /// (x/y mod 10 at 9/0), building NN median 9.9m. Model mix is dominant-plus-accent:
    /// build007 carries ~46-61%% of a wall line with build001/003/004/005/006 and the swd2_elev002
    /// elevator tower as accents (7 distinct models on the 12x12 flagship).
    ///
    /// Placement contract (the walkable-clearance gate):
    ///  - a building's anchor cell is always a NON-WALKABLE margin cell (not a room tile, not a
    ///    stamped structure tile, not a transition/door cell),
    ///  - its face sits <see cref="FaceIntrusion"/> proud of the open-cell boundary, and its
    ///    footprint may penetrate any walkable cell by at most <see cref="MaxOpenIntrusion"/>
    ///    (min-dimension of the overlap rectangle), so no street lane or room interior is ever
    ///    blocked,
    ///  - bearing = the fronted face's outward normal, cardinal-quantized.
    ///
    /// Composition: occupied margin cells are published as
    /// <see cref="ResolvedLayout.PlaceableStructureCells"/>, so WallFlush cargo and
    /// structure-frontage dressing anchor against placeable buildings exactly as against stamped
    /// tile structures (hand-built narscorpd stacks its flush cargo against swd_build bases). At
    /// 12x12-20x20 this is the primary canyon mechanism; at 24-32 it complements the tile-block
    /// mechanism, walling the margins the tile blocks leave open.
    ///
    /// Determinism: seeded from layout.Seed with pass-local salts, independent of the decoration
    /// planner's main RNG stream, so the existing dressing mechanisms keep their exact sequences.
    /// </summary>
    public static class BuildingFrontagePlanner
    {
        private const float TileSize = 10f;
        private const float TileHalf = 5f;

        /// <summary>How far the building face sits PROUD of the open-cell boundary (into the
        /// walkable cell) -- flush with the cobble edge like the hand-built facades, small enough
        /// that WallFlush cargo (0.4m inside the boundary) and wall-run dressing (1.5m inside)
        /// stand against the face rather than inside it.</summary>
        internal const float FaceIntrusion = 0.6f;

        /// <summary>Maximum min-dimension penetration of a building footprint into any walkable
        /// (room) cell -- the walkable-clearance contract. 2.6m off a 10m tile leaves a 7.4m lane
        /// and never reaches a carved road ribbon's center strip.</summary>
        internal const float MaxOpenIntrusion = 2.6f;

        /// <summary>Maximum min-dimension penetration into a transition/door cell -- door frames
        /// and exit anchors stay visually clear.</summary>
        private const float MaxExcludedIntrusion = 0.5f;

        /// <summary>Min-dimension coverage at which a non-walkable cell under a building footprint
        /// counts as occupied structure (published to PlaceableStructureCells and skipped as a
        /// later frontage anchor).</summary>
        private const float OccupiedCoverage = 3.0f;

        /// <summary>Chance each further slot of a frontage run repeats the run's dominant model
        /// rather than re-rolling the weighted mix -- hand-built wall lines are dominant-model runs
        /// (build007 x4-5 consecutive) with occasional accents.</summary>
        internal const double DominantShare = 0.7;

        /// <summary>Wall mounts float this far off the face plane (proud of the face) so sign
        /// panels never z-fight the facade art.</summary>
        internal const float MountProudOffset = 0.35f;

        /// <summary>Ceiling on the per-face-slot chance of hanging a facade mount; the effective
        /// chance is min(this, mount budget / slot count) -- see <see cref="MountBudgetShare"/>.
        /// Wide faces roll a second mount below.</summary>
        internal const double MountChance = 0.85;

        /// <summary>
        /// Facade-mount budget as a fraction of the rest of the plan (ground dressing +
        /// frontage buildings): mounts target ~0.22x the other placements, converging the
        /// elevated share M/(rest+M) on ~0.18 -- the middle of the hand-built dense-city
        /// elevated band (0.13-0.23 of decoratives above Z 0.5m across velundr/narscorpd/
        /// nsshipyard/ns_comrcial_ka/narshadaar_promi). A flat per-slot chance alone overshot at
        /// 24-32 (measured 0.24-0.37), because tile-block faces multiply with area while ground
        /// dressing grows with room supply; the budget keeps every size inside the band.
        /// </summary>
        internal const double MountBudgetShare = 0.22;

        /// <summary>Faces at least this wide roll a second, independently-jittered mount.</summary>
        internal const float WideFaceWidth = 18f;

        private const int FrontageSeedSalt = unchecked((int)0x0BF0A11E);
        private const int MountSeedSalt = unchecked((int)0x00FACADE);

        private static readonly (int Dx, int Dy)[] CardinalDirections =
        {
            (1, 0), (-1, 0), (0, 1), (0, -1)
        };

        /// <summary>One planned frontage building plus its face geometry (for the facade-mount
        /// pass and the benchmark's enclosure/alignment metrics).</summary>
        public sealed class FrontagePlacement
        {
            public PlannedDecoration Decoration { get; set; }
            /// <summary>Midpoint of the fronted open/margin boundary edge (world units).</summary>
            public Vector2 FaceCenter { get; set; }
            /// <summary>Outward normal of the fronted face (toward the open cell).</summary>
            public (int Dx, int Dy) Outward { get; set; }
            public float FaceWidth { get; set; }
            public float Depth { get; set; }
            /// <summary>The margin cell the building anchors on.</summary>
            public (int X, int Y) AnchorCell { get; set; }
        }

        public sealed class FrontageResult
        {
            public List<FrontagePlacement> Placements { get; } = new();
            /// <summary>Non-walkable cells covered by placed building mass (see
            /// <see cref="ResolvedLayout.PlaceableStructureCells"/>).</summary>
            public HashSet<(int X, int Y)> OccupiedCells { get; } = new();
        }

        /// <summary>
        /// Plans the frontage pass. Deterministic for (layout.Seed, tileset). Returns an empty
        /// result when the tileset declares no frontage buildings.
        /// </summary>
        public static FrontageResult PlanFrontage(
            ResolvedLayout layout, DungeonTilesetProfile tileset, HashSet<(int X, int Y)> excluded,
            string roadCrosser)
        {
            var result = new FrontageResult();
            var entries = tileset?.FrontageBuildings;
            if (layout == null || entries == null || entries.Count == 0)
                return result;

            var openCells = new HashSet<(int X, int Y)>();
            var frontableCells = new HashSet<(int X, int Y)>();
            foreach (var room in layout.Rooms)
            foreach (var tile in room.Tiles)
            {
                openCells.Add(tile);
                if (!room.IsSetPiece)
                    frontableCells.Add(tile);
            }

            var stamped = layout.StampedStructureTiles ?? new HashSet<(int X, int Y)>();
            excluded ??= new HashSet<(int X, int Y)>();

            // Candidate anchors: margin cells with at least one cardinal frontable-open neighbor,
            // grouped into runs (shared face direction, same face line, contiguous along it) --
            // the flush-line composition the hand-built canyon walls use.
            var candidates = new List<((int X, int Y) Cell, (int Dx, int Dy) Dir)>();
            for (var y = 0; y < layout.Height; y++)
            for (var x = 0; x < layout.Width; x++)
            {
                var cell = (X: x, Y: y);
                if (openCells.Contains(cell) || stamped.Contains(cell) || excluded.Contains(cell))
                    continue;
                // Road integrity: a margin cell whose own edges carry the road crosser is where a
                // carved lane runs off the walkable grid (toward a transition or the map rim) --
                // a building there would wall the road off. The lane stays clear.
                if (DungeonDecorationPlanner.TileCarriesRoadEdge(cell, layout, roadCrosser))
                    continue;

                foreach (var dir in CardinalDirections)
                {
                    var fronted = (X: x + dir.Dx, Y: y + dir.Dy);
                    if (!frontableCells.Contains(fronted) || excluded.Contains(fronted))
                        continue;

                    candidates.Add((cell, dir));
                }
            }

            // Deterministic run grouping: for a horizontal outward normal the run advances along Y
            // (a vertical face line), and vice versa. Runs sort by (direction, face line, slot).
            var runs = candidates
                .GroupBy(c => (c.Dir, Line: c.Dir.Dx != 0 ? c.Cell.X : c.Cell.Y))
                .OrderBy(g => g.Key.Dir.Dx).ThenBy(g => g.Key.Dir.Dy).ThenBy(g => g.Key.Line)
                .Select(g => g.OrderBy(c => c.Dir.Dx != 0 ? c.Cell.Y : c.Cell.X).ToList())
                .ToList();

            var rng = new System.Random(layout.Seed ^ FrontageSeedSalt);
            var usage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var run in runs)
            {
                BuildingFrontageEntry dominant = null;

                foreach (var (cell, dir) in run)
                {
                    if (result.OccupiedCells.Contains(cell))
                        continue;

                    var fitting = entries
                        .Where(e => e.MaxPerArea <= 0 || usage.GetValueOrDefault(e.Resref) < e.MaxPerArea)
                        .Where(e => Fits(e, cell, dir, layout, openCells, stamped, excluded))
                        .ToList();
                    if (fitting.Count == 0)
                        continue;

                    dominant ??= PickWeighted(fitting, rng);
                    var entry = fitting.Contains(dominant) && rng.NextDouble() < DominantShare
                        ? dominant
                        : PickWeighted(fitting, rng);

                    Place(entry, cell, dir, result);
                    usage[entry.Resref] = usage.GetValueOrDefault(entry.Resref) + 1;
                }
            }

            return result;
        }

        /// <summary>Axis-aligned footprint rectangle for an entry anchored at (cell, dir): center
        /// on the face's inward normal at depth/2 - FaceIntrusion behind the boundary.</summary>
        private static (float MinX, float MinY, float MaxX, float MaxY) Footprint(
            BuildingFrontageEntry entry, (int X, int Y) cell, (int Dx, int Dy) dir)
        {
            var face = FaceCenter(cell, dir);
            var centerX = face.X - dir.Dx * (entry.Depth / 2f - FaceIntrusion);
            var centerY = face.Y - dir.Dy * (entry.Depth / 2f - FaceIntrusion);
            var halfX = dir.Dx != 0 ? entry.Depth / 2f : entry.FaceWidth / 2f;
            var halfY = dir.Dx != 0 ? entry.FaceWidth / 2f : entry.Depth / 2f;
            return (centerX - halfX, centerY - halfY, centerX + halfX, centerY + halfY);
        }

        private static Vector2 FaceCenter((int X, int Y) cell, (int Dx, int Dy) dir)
        {
            return new Vector2(
                cell.X * TileSize + TileHalf + dir.Dx * TileHalf,
                cell.Y * TileSize + TileHalf + dir.Dy * TileHalf);
        }

        /// <summary>Min-dimension of the overlap rectangle between a footprint and one grid cell
        /// (0 when disjoint) -- the penetration metric of the walkable-clearance contract.</summary>
        internal static float CellPenetration(
            (float MinX, float MinY, float MaxX, float MaxY) box, int cellX, int cellY)
        {
            var overlapX = Math.Min(box.MaxX, cellX * TileSize + TileSize) - Math.Max(box.MinX, cellX * TileSize);
            var overlapY = Math.Min(box.MaxY, cellY * TileSize + TileSize) - Math.Max(box.MinY, cellY * TileSize);
            if (overlapX <= 0f || overlapY <= 0f)
                return 0f;
            return Math.Min(overlapX, overlapY);
        }

        private static bool Fits(
            BuildingFrontageEntry entry, (int X, int Y) cell, (int Dx, int Dy) dir,
            ResolvedLayout layout, HashSet<(int X, int Y)> openCells,
            HashSet<(int X, int Y)> stamped, HashSet<(int X, int Y)> excluded)
        {
            var box = Footprint(entry, cell, dir);
            var minCellX = (int)MathF.Floor(box.MinX / TileSize);
            var maxCellX = (int)MathF.Floor((box.MaxX - 0.01f) / TileSize);
            var minCellY = (int)MathF.Floor(box.MinY / TileSize);
            var maxCellY = (int)MathF.Floor((box.MaxY - 0.01f) / TileSize);

            for (var cy = minCellY; cy <= maxCellY; cy++)
            for (var cx = minCellX; cx <= maxCellX; cx++)
            {
                // Cells beyond the grid are free margin -- hand-built rim buildings overhang the
                // area edge (flagship edge distances 0.2-0.8m).
                if (cx < 0 || cy < 0 || cx >= layout.Width || cy >= layout.Height)
                    continue;

                var penetration = CellPenetration(box, cx, cy);
                if (penetration <= 0f)
                    continue;

                if (openCells.Contains((cx, cy)) && penetration > MaxOpenIntrusion)
                    return false;
                if (excluded.Contains((cx, cy)) && penetration > MaxExcludedIntrusion)
                    return false;
                if (stamped.Contains((cx, cy)) && penetration > MaxOpenIntrusion)
                    return false;
            }

            return true;
        }

        private static void Place(
            BuildingFrontageEntry entry, (int X, int Y) cell, (int Dx, int Dy) dir, FrontageResult result)
        {
            var face = FaceCenter(cell, dir);
            var centerX = face.X - dir.Dx * (entry.Depth / 2f - FaceIntrusion);
            var centerY = face.Y - dir.Dy * (entry.Depth / 2f - FaceIntrusion);

            result.Placements.Add(new FrontagePlacement
            {
                Decoration = new PlannedDecoration
                {
                    Resref = entry.Resref,
                    Position = new Vector3(centerX, centerY, 0f),
                    Facing = DungeonDecorationPlanner.CardinalFacing(dir.Dx, dir.Dy),
                    Context = DecorationContext.BuildingFrontage
                },
                FaceCenter = face,
                Outward = dir,
                FaceWidth = entry.FaceWidth,
                Depth = entry.Depth,
                AnchorCell = cell
            });

            // Publish covered margin cells as structure so flush cargo/frontage dressing anchor
            // against the facade and later runs cannot double-place on them.
            var box = Footprint(entry, cell, dir);
            var minCellX = (int)MathF.Floor(box.MinX / TileSize);
            var maxCellX = (int)MathF.Floor((box.MaxX - 0.01f) / TileSize);
            var minCellY = (int)MathF.Floor(box.MinY / TileSize);
            var maxCellY = (int)MathF.Floor((box.MaxY - 0.01f) / TileSize);
            for (var cy = minCellY; cy <= maxCellY; cy++)
            for (var cx = minCellX; cx <= maxCellX; cx++)
            {
                if (cx < 0 || cy < 0)
                    continue;

                if (CellPenetration(box, cx, cy) >= OccupiedCoverage)
                    result.OccupiedCells.Add((cx, cy));
            }

            result.OccupiedCells.Add(cell);
        }

        /// <summary>
        /// Plans the wall-mounted facade dressing pass: sign/holo placeables hung on building
        /// faces -- every placed frontage building's face plus every stamped structure tile face
        /// that fronts an open room cell -- at the entry's mined height band, slightly proud of
        /// the face, bearing = the face's outward normal. See
        /// <see cref="DungeonTilesetProfile.FacadeMounts"/>.
        /// </summary>
        public static List<PlannedDecoration> PlanFacadeMounts(
            ResolvedLayout layout, DungeonTilesetProfile tileset, FrontageResult frontage,
            int restOfPlanCount)
        {
            var plan = new List<PlannedDecoration>();
            var entries = tileset?.FacadeMounts;
            if (layout == null || entries == null || entries.Count == 0)
                return plan;

            // Every mountable face slot, in deterministic order: frontage-building faces first
            // (placement order), then stamped tile-structure faces fronting open room cells
            // (cell order). Collected up front so the budgeted per-slot chance (see
            // MountBudgetShare) is known before any RNG draw.
            var slots = new List<(Vector2 FaceCenter, (int Dx, int Dy) Outward, float FaceWidth, float Proud)>();
            if (frontage != null)
            {
                foreach (var placement in frontage.Placements)
                    slots.Add((placement.FaceCenter, placement.Outward, placement.FaceWidth,
                        FaceIntrusion + MountProudOffset));
            }

            var stamped = layout.StampedStructureTiles;
            if (stamped is { Count: > 0 })
            {
                var openCells = new HashSet<(int X, int Y)>();
                foreach (var room in layout.Rooms)
                {
                    if (room.IsSetPiece)
                        continue;
                    foreach (var tile in room.Tiles)
                        openCells.Add(tile);
                }

                foreach (var cell in stamped.OrderBy(c => c.Y).ThenBy(c => c.X))
                foreach (var dir in CardinalDirections)
                {
                    if (!openCells.Contains((cell.X + dir.Dx, cell.Y + dir.Dy)))
                        continue;

                    slots.Add((FaceCenter(cell, dir), dir, TileSize, MountProudOffset));
                }
            }

            var opportunities = slots.Sum(s => s.FaceWidth >= WideFaceWidth ? 2 : 1);
            if (opportunities == 0)
                return plan;

            var budget = MountBudgetShare * Math.Max(restOfPlanCount, 0);
            var chance = Math.Min(MountChance, budget / opportunities);
            if (chance <= 0.0)
                return plan;

            var rng = new System.Random(layout.Seed ^ MountSeedSalt);
            foreach (var slot in slots)
                MountOnFace(plan, entries, slot.FaceCenter, slot.Outward, slot.FaceWidth, slot.Proud, chance, rng);

            return plan;
        }

        private static void MountOnFace(
            List<PlannedDecoration> plan, List<FacadeMountEntry> entries,
            Vector2 faceCenter, (int Dx, int Dy) outward, float faceWidth, float proud,
            double chance, System.Random rng)
        {
            var mounts = faceWidth >= WideFaceWidth ? 2 : 1;
            for (var i = 0; i < mounts; i++)
            {
                if (rng.NextDouble() >= chance)
                    continue;

                var entry = PickWeighted(entries, rng);
                var jitterRange = Math.Max(0f, faceWidth / 2f - 1.5f);
                var jitter = (float)(rng.NextDouble() * 2.0 - 1.0) * Math.Min(3.5f, jitterRange);
                var alongX = outward.Dx == 0 ? 1f : 0f;
                var alongY = outward.Dx == 0 ? 0f : 1f;
                var z = entry.MinHeight + (float)rng.NextDouble() * (entry.MaxHeight - entry.MinHeight);

                plan.Add(new PlannedDecoration
                {
                    Resref = entry.Resref,
                    Position = new Vector3(
                        faceCenter.X + outward.Dx * proud + alongX * jitter,
                        faceCenter.Y + outward.Dy * proud + alongY * jitter,
                        z),
                    Facing = DungeonDecorationPlanner.CardinalFacing(outward.Dx, outward.Dy),
                    Context = DecorationContext.FacadeMount
                });
            }
        }

        private static BuildingFrontageEntry PickWeighted(List<BuildingFrontageEntry> entries, System.Random rng)
        {
            var total = entries.Sum(e => e.Weight);
            if (total <= 0)
                return entries[0];

            var roll = rng.Next(total);
            var cumulative = 0;
            foreach (var entry in entries)
            {
                cumulative += entry.Weight;
                if (roll < cumulative)
                    return entry;
            }

            return entries[^1];
        }

        private static FacadeMountEntry PickWeighted(List<FacadeMountEntry> entries, System.Random rng)
        {
            var total = entries.Sum(e => e.Weight);
            if (total <= 0)
                return entries[0];

            var roll = rng.Next(total);
            var cumulative = 0;
            foreach (var entry in entries)
            {
                cumulative += entry.Weight;
                if (roll < cumulative)
                    return entry;
            }

            return entries[^1];
        }
    }
}

