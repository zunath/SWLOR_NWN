#nullable disable
using System.Collections.Generic;
using SWLOR.Toolset.Domain.AreaGeneration.Tileset;

namespace SWLOR.Toolset.Domain.AreaGeneration.Layouts
{
    /// <summary>
    /// Shared fence-line post-pass: carves straight, one-cell-wide Fence edge-crosser lines through
    /// open space (e.g. a fenced-off maintenance checkpoint in tds01 Sewers, a courtyard partition in
    /// vmr01). Unlike LayoutAccentChannelCarver's bands, a fence line never repaints corner terrain --
    /// every corner on both sides of the line stays this layout's own open terrain the whole time.
    /// That also means the shared corner-graph connectivity check
    /// (LayoutCornerUtils.IsConnectedWithLinks) is blind to a Fence barrier -- both sides still read as
    /// plain open corners -- so this pass runs its own cell-level (not corner-level) connectivity
    /// check instead, using the same tentative-commit / verify / revert shape
    /// LayoutAccentChannelCarver's band carving uses: a candidate run's crossers are set, cell-level
    /// reachability from a fixed open-cell anchor is compared before and after, and the whole run is
    /// undone if it reduced reachability (some cells that were reachable became unreachable) -- e.g.
    /// two independently-safe lines whose end caps happen to meet at a shared corner and jointly seal
    /// a small pocket, which a per-line-only margin check cannot see. A run also requires both cells
    /// one step past its ends ("margin" cells) to already be fully open and crosser-free before it is
    /// even attempted, and reserves them afterward so no later line can carve directly onto an earlier
    /// line's own walk-around gap -- a cheap pre-filter that keeps the authoritative flood-fill check
    /// rare rather than load-bearing on its own.
    ///
    /// Vocabulary this relies on (verified offline against tds01/vmr01 .set data): flat, ungrouped,
    /// fully-open-cornered tiles carrying a Fence crosser on exactly one opposite pair of edges (a
    /// straight run segment, e.g. tds01 TILE56/145/146) or exactly one edge (an end cap, e.g. tds01
    /// TILE59/142) -- the same "chain of shared crosser edges, resolved per-cell from its own local
    /// key" shape LayoutTunnelCarver's Corridor chain and LayoutAccentChannelCarver's Bridge span use,
    /// minus the corner repaint. TileResolver's rotation search means a single physical straight-run
    /// tile (Top+Bottom Fence) also resolves the Left+Right orientation, so one capability probe
    /// covers both axes.
    ///
    /// Scans the whole grid for a candidate run rather than scoping to a single room's Tiles (mirroring
    /// LayoutAccentChannelCarver's own random-attempt shape): some layout styles cap chamber size well
    /// below what a room-scoped search would need (WarrenLayout hard-caps chambers at 5 corners, the
    /// same reason BridgeChannelTests documents Warren as unusable for AccentChannels), and a fence
    /// spanning a chamber's mouth into its connecting corridor reads fine thematically (a checkpoint
    /// fence).
    ///
    /// Runs after LayoutTransitionAssignment (so a fence line can avoid a room's already-anchored
    /// transition tile) and before LayoutGroupStamper, whose CorridorInsert classifier can optionally
    /// splice a FenceDoor/InteriorFenceDoor/ExteriorFenceDoor group gate into a straight body segment
    /// this pass carved, when a tileset profile configures one via SetPieces.
    ///
    /// Runs a fully independent pass per terrain: MacroLayoutParameters.OpenTerrain (the primary
    /// terrain, always) and, when districts are active, MacroLayoutParameters.SecondaryOpenTerrain too
    /// (e.g. vmr01's InteriorFenceDoor gate needs a Floor-terrain fence run, alongside
    /// ExteriorFenceDoor's Plaza-terrain one) -- each pass's IsClearCell/LabelComponents only ever read
    /// or write cells already homogeneous in that single pass's own terrain, so the two passes never
    /// interact or need to know about each other's crossers.
    /// </summary>
    internal static class LayoutFenceCarver
    {
        private const string FenceCrosser = "Fence";
        private const int MinLength = 2;
        private const int MaxLength = 5;
        private const int MaxAttempts = 500;

        internal static void CarveFences(
            MacroLayout layout, MacroLayoutParameters parameters, TilesetModel tileset, System.Random random)
        {
            if (parameters.FenceLines <= 0) return;
            if (tileset == null) return;

            // Primary pass (original v1 scope, fully back-compat). A second, independent pass for
            // SecondaryOpenTerrain follows when districts are active and the tileset separately
            // covers Fence vocabulary against that terrain too (e.g. vmr01's InteriorFenceDoor against
            // Floor, alongside ExteriorFenceDoor against Plaza) -- each pass only ever touches cells of
            // its own terrain, so the two never interact.
            CarveFencesForTerrain(layout, parameters, tileset, random, parameters.OpenTerrain);
            if (!string.IsNullOrEmpty(parameters.SecondaryOpenTerrain))
                CarveFencesForTerrain(layout, parameters, tileset, random, parameters.SecondaryOpenTerrain);
        }

        private static void CarveFencesForTerrain(
            MacroLayout layout, MacroLayoutParameters parameters, TilesetModel tileset, System.Random random, string open)
        {
            if (string.IsNullOrEmpty(open)) return;

            // Zero-config capability probe: only a tileset whose current open terrain resolves both
            // the straight-run and end-cap shapes can ever place a valid fence line. Skips silently
            // (never fails generation) on tilesets without Fence vocabulary (e.g. tdt01, zsf01).
            var hasStraightRun = TileResolver.HasCandidate(
                tileset, open, open, open, open, FenceCrosser, string.Empty, FenceCrosser, string.Empty);
            var hasEndCap = TileResolver.HasCandidate(
                tileset, open, open, open, open, FenceCrosser, string.Empty, string.Empty, string.Empty);
            if (!hasStraightRun || !hasEndCap) return;

            var corners = layout.Corners;
            var crossers = layout.Crossers;
            var width = corners.Width;
            var height = corners.Height;

            // Cells a fence line's own body must never claim: room path anchors (2x2 around each
            // center tile, same convention LayoutAccentChannelCarver uses) and every already-anchored
            // transition, plus (once placed) every prior line's own margin cells so a later line can
            // never carve its body directly onto an earlier line's walk-around gap.
            var claimed = new HashSet<(int X, int Y)>();
            foreach (var room in layout.Rooms)
            {
                var (cx, cy) = room.CenterTile;
                claimed.Add((cx, cy));
                claimed.Add((cx + 1, cy));
                claimed.Add((cx, cy + 1));
                claimed.Add((cx + 1, cy + 1));
            }
            foreach (var transition in layout.Transitions)
                claimed.Add(transition.Tile);

            var placed = 0;
            var attempts = 0;

            while (placed < parameters.FenceLines && attempts < MaxAttempts)
            {
                attempts++;

                var horizontal = random.Next(2) == 0; // true: chain steps in X (Left/Right edges); false: steps in Y (Top/Bottom edges)
                var length = random.Next(MinLength, MaxLength + 1);

                (int X, int Y) start;
                (int Dx, int Dy) step;

                if (horizontal)
                {
                    if (width < length + 2) continue; // no room for both margin cells
                    var y = random.Next(0, height);
                    var x0 = random.Next(1, width - length);
                    start = (x0, y);
                    step = (1, 0);
                }
                else
                {
                    if (height < length + 2) continue;
                    var x = random.Next(0, width);
                    var y0 = random.Next(1, height - length);
                    start = (x, y0);
                    step = (0, 1);
                }

                if (!TryBuildChain(corners, crossers, claimed, open, start, step, length, out var chain))
                    continue;

                // A single fixed anchor only guards the one component containing it -- a layout can
                // have several disconnected fully-open components already (e.g. islands a 1-wide open
                // lane doesn't bridge at the whole-cell granularity), and two lines that are each safe
                // in isolation can still jointly split a DIFFERENT component at their shared corner.
                // Label every component before, tentatively commit, relabel, and require every
                // before-component to still map to exactly one after-component (Fence only ever
                // removes edges, so an after-component can only be a subset of a before-component --
                // this rejects the case where one before-component splits into more than one).
                var beforeLabels = LabelComponents(corners, crossers, open, width, height);

                for (var i = 0; i + 1 < chain.Count; i++)
                {
                    var a = chain[i];
                    if (horizontal) crossers.SetEdge(a.X, a.Y, EdgeSlot.Right, FenceCrosser);
                    else crossers.SetEdge(a.X, a.Y, EdgeSlot.Top, FenceCrosser);
                }

                var afterLabels = LabelComponents(corners, crossers, open, width, height);
                var splitDetected = AnyComponentSplit(beforeLabels, afterLabels);

                if (splitDetected)
                {
                    // Revert: this run's placement split a component that was reachable before -- a
                    // rare multi-fence interaction (e.g. two independently-safe lines meeting at a
                    // shared corner and jointly sealing a small pocket) the per-line margin check
                    // alone cannot see. Undo and let the caller try a different placement.
                    for (var i = 0; i + 1 < chain.Count; i++)
                    {
                        var a = chain[i];
                        if (horizontal) crossers.SetEdge(a.X, a.Y, EdgeSlot.Right, string.Empty);
                        else crossers.SetEdge(a.X, a.Y, EdgeSlot.Top, string.Empty);
                    }
                    continue;
                }

                var before = (X: start.X - step.Dx, Y: start.Y - step.Dy);
                var after = (X: start.X + step.Dx * length, Y: start.Y + step.Dy * length);
                claimed.Add(before);
                claimed.Add(after);

                placed++;
            }
        }

        private static bool TryBuildChain(
            CornerTerrainGrid corners, EdgeCrosserGrid crossers, HashSet<(int X, int Y)> claimed, string open,
            (int X, int Y) start, (int Dx, int Dy) step, int length, out List<(int X, int Y)> run)
        {
            run = null;

            var before = (X: start.X - step.Dx, Y: start.Y - step.Dy);
            if (!IsClearCell(corners, crossers, open, before)) return false;

            var body = new List<(int X, int Y)>(length);
            for (var i = 0; i < length; i++)
            {
                var cell = (X: start.X + step.Dx * i, Y: start.Y + step.Dy * i);
                if (claimed.Contains(cell)) return false;
                if (!IsClearCell(corners, crossers, open, cell)) return false;
                body.Add(cell);
            }

            var after = (X: start.X + step.Dx * length, Y: start.Y + step.Dy * length);
            if (!IsClearCell(corners, crossers, open, after)) return false;

            run = body;
            return true;
        }

        private static bool IsClearCell(CornerTerrainGrid corners, EdgeCrosserGrid crossers, string open, (int X, int Y) cell)
        {
            if (cell.X < 0 || cell.Y < 0 || cell.X >= corners.Width || cell.Y >= corners.Height) return false;
            if (!LayoutCornerUtils.IsTileFullyOpen(corners, cell.X, cell.Y, open)) return false;

            for (var slot = 0; slot < 4; slot++)
                if (crossers.GetEdge(cell.X, cell.Y, slot).Length != 0) return false;

            return true;
        }

        /// <summary>
        /// Labels every fully-open cell with its cell-level (not corner-level) connected-component id,
        /// treating a "Fence" crosser edge as an impassable wall between two otherwise-open cells --
        /// the real walkability model Fence tiles impose. Used to detect whether tentatively committing
        /// a candidate run splits any existing component (see CarveFences).
        /// </summary>
        private static Dictionary<(int X, int Y), int> LabelComponents(
            CornerTerrainGrid corners, EdgeCrosserGrid crossers, string open, int width, int height)
        {
            var labels = new Dictionary<(int X, int Y), int>();
            var nextLabel = 0;

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var start = (X: x, Y: y);
                    if (labels.ContainsKey(start)) continue;
                    if (!LayoutCornerUtils.IsTileFullyOpen(corners, x, y, open)) continue;

                    var queue = new Queue<(int X, int Y)>();
                    labels[start] = nextLabel;
                    queue.Enqueue(start);

                    while (queue.Count > 0)
                    {
                        var (cx, cy) = queue.Dequeue();

                        foreach (var (dx, dy, slot) in Steps)
                        {
                            var next = (X: cx + dx, Y: cy + dy);
                            if (next.X < 0 || next.Y < 0 || next.X >= width || next.Y >= height) continue;
                            if (labels.ContainsKey(next)) continue;
                            if (!LayoutCornerUtils.IsTileFullyOpen(corners, next.X, next.Y, open)) continue;
                            if (string.Equals(crossers.GetEdge(cx, cy, slot), FenceCrosser, System.StringComparison.OrdinalIgnoreCase)) continue;

                            labels[next] = nextLabel;
                            queue.Enqueue(next);
                        }
                    }

                    nextLabel++;
                }
            }

            return labels;
        }

        /// <summary>
        /// True when some "before" component maps to more than one distinct "after" component -- Fence
        /// only ever removes edges, so an after-component can only ever be a subset of a
        /// before-component; a before-component spanning two or more after-components is exactly the
        /// "this run severed a previously-connected area" case CarveFences must reject.
        /// </summary>
        private static bool AnyComponentSplit(
            Dictionary<(int X, int Y), int> beforeLabels, Dictionary<(int X, int Y), int> afterLabels)
        {
            var mapping = new Dictionary<int, int>();
            foreach (var (cell, beforeLabel) in beforeLabels)
            {
                var afterLabel = afterLabels[cell];
                if (mapping.TryGetValue(beforeLabel, out var expectedAfter))
                {
                    if (expectedAfter != afterLabel) return true;
                }
                else
                {
                    mapping[beforeLabel] = afterLabel;
                }
            }

            return false;
        }

        private static readonly (int Dx, int Dy, int Slot)[] Steps =
        {
            (1, 0, EdgeSlot.Right), (-1, 0, EdgeSlot.Left), (0, 1, EdgeSlot.Top), (0, -1, EdgeSlot.Bottom)
        };
    }
}
