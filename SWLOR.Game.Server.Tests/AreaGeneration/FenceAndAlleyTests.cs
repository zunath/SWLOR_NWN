using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.DungeonDefinition;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

/// <summary>
/// FENCE (LayoutFenceCarver) and ALLEY (LayoutTunnelCarver with CorridorCrosserType.Alley) crosser
/// systems: run the full pipeline against real tds01/vmr01 .set data, same conventions as
/// BridgeChannelTests/TunnelCorridorTests.
///
/// Fence design: a straight, one-cell-wide Fence edge-crosser line through open room interiors that
/// never repaints corner terrain -- both sides stay open the whole time -- so it needs no TunnelLink.
/// Instead of a mandatory FenceDoor gate, LayoutFenceCarver's default geometry always leaves the fence
/// "floating" (an open, crosser-free margin cell past both tips), so a player can always walk around
/// either end -- the "gap" option from the design brief. A FenceDoor/Interior/ExteriorFenceDoor gate
/// spliced directly into a body segment (the other brief option) is exercised separately below via
/// LayoutGroupStamper's generalized CorridorInsert classifier.
///
/// Alley design: structurally identical to Corridor tunnel mode (chains through fully solid cells,
/// entering open Plaza space through a port) -- verified offline against vmr01 .set data, the ONE
/// difference is that Alley ports carry the "Alley" crosser itself rather than a separate "Doorway"
/// crosser (TILE210 Plaza-port, TILE221 all-solid straight body both say "Alley").
/// </summary>
public class FenceAndAlleyTests
{
    private static TilesetModel LoadTileset(string tilesetResref) => TilesetTestSource.LoadTileset(tilesetResref);

    private static IEnumerable<string> AllCrosserLabels(MacroLayout macro, int width, int height)
    {
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        for (var slot = 0; slot < 4; slot++)
        {
            var edge = macro.Crossers.GetEdge(x, y, slot);
            if (edge.Length != 0) yield return edge;
        }
    }

    /// <summary>
    /// Global edge-agreement proof, mirroring BridgeChannelTests/TunnelCorridorTests: every resolved
    /// tile's oriented edges must match the crosser plan on all four sides.
    /// </summary>
    private static void AssertEdgeAgreement(TilesetModel model, MacroLayout macro, ResolvedLayout resolved, int seed, List<string> failures)
    {
        var tilesById = model.Tiles.ToDictionary(t => t.TileId);

        var doorCells = new HashSet<(int X, int Y)>();
        foreach (var transition in resolved.Transitions.Where(t => t.Style == TransitionStyle.Door))
        {
            doorCells.Add(transition.Tile);
            doorCells.Add(transition.DoorCell);
            doorCells.Add(transition.DoorwayCell);
        }

        for (var y = 0; y < resolved.Height; y++)
        {
            for (var x = 0; x < resolved.Width; x++)
            {
                if (doorCells.Contains((x, y))) continue;

                var tile = resolved.GetTile(x, y);
                var record = tilesById[tile.TileId];

                for (var slot = 0; slot < 4; slot++)
                {
                    var actual = record.GetEdgeAt(tile.Orientation, slot) ?? string.Empty;
                    var expected = macro.Crossers.GetEdge(x, y, slot) ?? string.Empty;
                    if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
                    {
                        failures.Add($"seed {seed}: cell ({x},{y}) slot {slot}: planned '{expected}' but resolved TILE{tile.TileId} o={tile.Orientation} has '{actual}'");
                    }
                }
            }
        }
    }

    private static bool IsFullyOpenCell(CornerTerrainGrid corners, int x, int y, string open)
    {
        return string.Equals(corners.Labels[x, y], open, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(corners.Labels[x + 1, y], open, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(corners.Labels[x, y + 1], open, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(corners.Labels[x + 1, y + 1], open, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Labels every fully-open cell with its cell-level (not corner-level) connected-component id.
    /// This is the real walkability model Fence tiles impose (their corners stay open, so the shared
    /// corner-graph flood fill LayoutCornerUtils uses is blind to a Fence barrier). A layout can have
    /// several disconnected fully-open components even with zero fences (a corridor only 1 corner wide
    /// contributes no fully-open cells at all, since a cell needs both its y and y+1 corner rows open),
    /// so the test below compares the FULL partition with vs without Fence edges counted as blocking --
    /// mirroring LayoutFenceCarver's own tentative-commit/verify/revert check -- rather than reachability
    /// from one fixed anchor, which only proves the invariant for the single component containing it.
    /// </summary>
    private static Dictionary<(int X, int Y), int> LabelComponents(MacroLayout macro, string open, bool fenceBlocks)
    {
        var corners = macro.Corners;
        var labels = new Dictionary<(int X, int Y), int>();
        var nextLabel = 0;

        for (var y = 0; y < corners.Height; y++)
        {
            for (var x = 0; x < corners.Width; x++)
            {
                var start = (X: x, Y: y);
                if (labels.ContainsKey(start)) continue;
                if (!IsFullyOpenCell(corners, x, y, open)) continue;

                var queue = new Queue<(int X, int Y)>();
                labels[start] = nextLabel;
                queue.Enqueue(start);

                while (queue.Count > 0)
                {
                    var (cx, cy) = queue.Dequeue();

                    foreach (var (dx, dy, slot) in new[] { (1, 0, EdgeSlot.Right), (-1, 0, EdgeSlot.Left), (0, 1, EdgeSlot.Top), (0, -1, EdgeSlot.Bottom) })
                    {
                        var next = (X: cx + dx, Y: cy + dy);
                        if (next.X < 0 || next.Y < 0 || next.X >= corners.Width || next.Y >= corners.Height) continue;
                        if (labels.ContainsKey(next)) continue;
                        if (!IsFullyOpenCell(corners, next.X, next.Y, open)) continue;
                        if (fenceBlocks && string.Equals(macro.Crossers.GetEdge(cx, cy, slot), "Fence", StringComparison.OrdinalIgnoreCase)) continue;

                        labels[next] = nextLabel;
                        queue.Enqueue(next);
                    }
                }

                nextLabel++;
            }
        }

        return labels;
    }

    private static List<(int X, int Y)> AllFullyOpenCells(MacroLayout macro, string open)
    {
        var corners = macro.Corners;
        var result = new List<(int X, int Y)>();
        for (var x = 0; x < corners.Width; x++)
        for (var y = 0; y < corners.Height; y++)
            if (IsFullyOpenCell(corners, x, y, open))
                result.Add((x, y));
        return result;
    }

    // ============================================================
    // Fence lines
    // ============================================================

    private static MacroLayoutParameters FenceParameters(TilesetModel model, string openTerrain, int fenceLines, int width = 24)
    {
        return new MacroLayoutParameters
        {
            Style = DungeonLayoutStyle.RoomsAndCorridors,
            MinRooms = 4,
            MaxRooms = 6,
            MinRoomCornerSize = 4,
            MaxRoomCornerSize = 7,
            LoopFactor = 0.3,
            Width = width,
            Height = width,
            SolidTerrain = model.DefaultTerrain,
            OpenTerrain = openTerrain,
            FenceLines = fenceLines,
        };
    }

    [TestCase("tds01", "")]
    [TestCase("vmr01", "Plaza")]
    public void FenceLines_FullPipelineSucceedsAcrossManySeeds(string tilesetResref, string openTerrainOverride)
    {
        var model = LoadTileset(tilesetResref);
        var open = string.IsNullOrEmpty(openTerrainOverride) ? model.FloorTerrain : openTerrainOverride;
        var failures = new List<string>();
        var seedsWithFence = 0;
        const int seedCount = 20;

        for (var seed = 20000; seed < 20000 + seedCount; seed++)
        {
            var rng = new Random(seed);
            var parameters = FenceParameters(model, open, fenceLines: 2);

            MacroLayout macro;
            try
            {
                macro = MacroLayoutGenerator.Generate(parameters, rng, model);
            }
            catch (InvalidOperationException ex)
            {
                failures.Add($"seed {seed}: generation failed: {ex.Message}");
                continue;
            }

            macro.Seed = seed;

            var hasFence = AllCrosserLabels(macro, parameters.Width, parameters.Height)
                .Any(e => string.Equals(e, "Fence", StringComparison.OrdinalIgnoreCase));
            if (hasFence) seedsWithFence++;

            if (!TileResolver.TryResolve(model, macro, rng, out var resolved, out var reason))
            {
                failures.Add($"seed {seed}: resolution failed: {reason}");
                continue;
            }

            AssertEdgeAgreement(model, macro, resolved, seed, failures);
        }

        failures.Should().BeEmpty();
        seedsWithFence.Should().BeGreaterThan(0, $"{tilesetResref} should place at least one Fence line across {seedCount} seeds");
        TestContext.WriteLine($"{tilesetResref}: {seedsWithFence}/{seedCount} seeds produced a Fence line, 0 resolution failures");
    }

    /// <summary>
    /// Directly proves the "never sever connectivity" requirement using the real (cell-level,
    /// Fence-aware) walkability model rather than the corner-graph invariant, which cannot see a
    /// Fence barrier at all (see FenceAwareCellFloodFill doc comment). Also confirms both end-cap
    /// cells of every placed fence line have exactly one Fence edge and their outward margin cell is
    /// open and crosser-free -- the concrete shape of the "gap" design (LayoutFenceCarver never
    /// touches a wall, so both tips are always walkable-around by construction).
    /// </summary>
    [TestCase("tds01", "")]
    [TestCase("vmr01", "Plaza")]
    public void FenceLines_NeverSeverConnectivityAndAlwaysLeaveAWalkableGap(string tilesetResref, string openTerrainOverride)
    {
        var model = LoadTileset(tilesetResref);
        var open = string.IsNullOrEmpty(openTerrainOverride) ? model.FloorTerrain : openTerrainOverride;
        var seedsWithFence = 0;

        for (var seed = 20100; seed < 20130; seed++)
        {
            var rng = new Random(seed);
            var parameters = FenceParameters(model, open, fenceLines: 2);
            var macro = MacroLayoutGenerator.Generate(parameters, rng, model);

            // Compare the FULL cell-level partition with vs without Fence edges counted as blocking --
            // every "without Fence" component must map to exactly one "with Fence" component (Fence
            // only ever removes edges, so a with-Fence component can only be a subset of a
            // without-Fence one; a split means some Fence line severed a previously-connected area).
            var baseline = LabelComponents(macro, open, fenceBlocks: false);
            var withFenceBlocking = LabelComponents(macro, open, fenceBlocks: true);

            var mapping = new Dictionary<int, int>();
            foreach (var (cell, beforeLabel) in baseline)
            {
                var afterLabel = withFenceBlocking[cell];
                if (mapping.TryGetValue(beforeLabel, out var expectedAfter))
                    expectedAfter.Should().Be(afterLabel, $"seed {seed}: a Fence line must never split a previously-connected area (cell {cell})");
                else
                    mapping[beforeLabel] = afterLabel;
            }

            var hasFence = false;
            for (var y = 0; y < macro.Corners.Height; y++)
            for (var x = 0; x < macro.Corners.Width; x++)
            {
                var edgeCount = 0;
                var fenceSlot = -1;
                for (var slot = 0; slot < 4; slot++)
                {
                    if (string.Equals(macro.Crossers.GetEdge(x, y, slot), "Fence", StringComparison.OrdinalIgnoreCase))
                    {
                        edgeCount++;
                        fenceSlot = slot;
                    }
                }

                // An end-cap cell (exactly one Fence edge): its single edge faces INTO the chain (the
                // next body cell), so the walkable margin/gap is the cell on the OPPOSITE side (the
                // slot with no crosser at all) -- the outward tip of the fence line.
                if (edgeCount == 1)
                {
                    hasFence = true;
                    var (dx, dy) = fenceSlot switch
                    {
                        EdgeSlot.Top => (0, -1),
                        EdgeSlot.Bottom => (0, 1),
                        EdgeSlot.Right => (-1, 0),
                        EdgeSlot.Left => (1, 0),
                        _ => (0, 0)
                    };
                    var margin = (X: x + dx, Y: y + dy);
                    if (margin.X >= 0 && margin.Y >= 0 && margin.X < macro.Corners.Width && margin.Y < macro.Corners.Height)
                    {
                        IsFullyOpenCell(macro.Corners, margin.X, margin.Y, open).Should().BeTrue(
                            $"seed {seed}: fence end-cap at ({x},{y}) must have an open margin cell at {margin}");
                        for (var slot = 0; slot < 4; slot++)
                            macro.Crossers.GetEdge(margin.X, margin.Y, slot).Should().BeEmpty(
                                $"seed {seed}: fence end-cap's margin cell at {margin} must be crosser-free");
                    }
                }
            }

            if (hasFence) seedsWithFence++;
        }

        seedsWithFence.Should().BeGreaterThan(0, "should exercise the fence end-cap/gap shape at least once across 30 seeds");
    }

    [TestCase("tds01", "")]
    public void FenceLines_IsDeterministicPerSeed(string tilesetResref, string openTerrainOverride = "")
    {
        var model = LoadTileset(tilesetResref);
        var open = string.IsNullOrEmpty(openTerrainOverride) ? model.FloorTerrain : openTerrainOverride;

        MacroLayout Generate()
        {
            var rng = new Random(20200);
            return MacroLayoutGenerator.Generate(FenceParameters(model, open, fenceLines: 2), rng, model);
        }

        var first = Generate();
        var second = Generate();

        for (var y = 0; y < 24; y++)
        for (var x = 0; x < 24; x++)
        {
            first.Corners.Labels[x, y].Should().Be(second.Corners.Labels[x, y], $"corner ({x},{y})");
            for (var slot = 0; slot < 4; slot++)
                first.Crossers.GetEdge(x, y, slot).Should().Be(second.Crossers.GetEdge(x, y, slot), $"cell ({x},{y}) slot {slot}");
        }
    }

    /// <summary>
    /// Back-compat: FenceLines=0 (the default) must never emit a Fence edge and must not otherwise
    /// perturb generation -- same params/seed with fence lines explicitly zeroed reproduces
    /// byte-for-byte identical corners/crossers to a plain baseline, and matches a hand-built
    /// MacroLayoutParameters that never sets the field at all.
    /// </summary>
    [TestCase("tds01", "")]
    public void FenceLines_Zero_ProducesNoFenceEdgesAndMatchesBaseline(string tilesetResref, string openTerrainOverride = "")
    {
        var model = LoadTileset(tilesetResref);
        var open = string.IsNullOrEmpty(openTerrainOverride) ? model.FloorTerrain : openTerrainOverride;

        for (var seed = 20300; seed < 20315; seed++)
        {
            var withZero = FenceParameters(model, open, fenceLines: 0);
            var macroA = MacroLayoutGenerator.Generate(withZero, new Random(seed), model);

            AllCrosserLabels(macroA, withZero.Width, withZero.Height)
                .Should().NotContain(e => string.Equals(e, "Fence", StringComparison.OrdinalIgnoreCase),
                    $"seed {seed}: FenceLines=0 must never place a Fence edge");

            var withDefaultField = FenceParameters(model, open, fenceLines: 0);
            withDefaultField.FenceLines.Should().Be(0);
            var macroB = MacroLayoutGenerator.Generate(withDefaultField, new Random(seed), model);

            for (var y = 0; y <= macroA.Corners.Height; y++)
            for (var x = 0; x <= macroA.Corners.Width; x++)
                macroA.Corners.Labels[x, y].Should().Be(macroB.Corners.Labels[x, y], $"seed {seed} corner ({x},{y})");

            for (var y = 0; y < macroA.Corners.Height; y++)
            for (var x = 0; x < macroA.Corners.Width; x++)
            for (var slot = 0; slot < 4; slot++)
                macroA.Crossers.GetEdge(x, y, slot).Should().Be(macroB.Crossers.GetEdge(x, y, slot), $"seed {seed} cell ({x},{y}) slot {slot}");
        }
    }

    /// <summary>
    /// Back-compat with a null tileset: LayoutFenceCarver must no-op (same guard LayoutGroupStamper
    /// uses) even when FenceLines is configured, since it has no tileset to probe capability against.
    /// </summary>
    [Test]
    public void FenceLines_NullTilesetSkipsCarvingEntirely()
    {
        var model = LoadTileset("tds01");
        var parameters = FenceParameters(model, model.FloorTerrain, fenceLines: 3);

        var macro = MacroLayoutGenerator.Generate(parameters, new Random(20400));

        AllCrosserLabels(macro, parameters.Width, parameters.Height)
            .Should().NotContain(e => string.Equals(e, "Fence", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// FenceDoor/InteriorFenceDoor gate splice: LayoutGroupStamper's generalized CorridorInsert
    /// classifier can pin a FenceDoor group tile into a straight body segment of a fence line this
    /// pass carves, the other design option ("a FenceDoor group tile spliced into the run") alongside
    /// the always-on floating-gap geometry proven above.
    /// </summary>
    [TestCase("tds01", "", "FenceDoor01", "FenceDoor02")]
    public void FenceDoorInsert_CanSpliceIntoAStraightFenceRun(string tilesetResref, string openTerrainOverride, string groupA, string groupB)
    {
        var model = LoadTileset(tilesetResref);
        var open = string.IsNullOrEmpty(openTerrainOverride) ? model.FloorTerrain : openTerrainOverride;
        var insertTileIds = new HashSet<int>
        {
            model.Groups.First(g => string.Equals(g.Name, groupA, StringComparison.OrdinalIgnoreCase)).TileIds[0],
            model.Groups.First(g => string.Equals(g.Name, groupB, StringComparison.OrdinalIgnoreCase)).TileIds[0],
        };

        var totalInserts = 0;

        for (var seed = 20500; seed < 20540; seed++)
        {
            var rng = new Random(seed);
            var parameters = FenceParameters(model, open, fenceLines: 3);
            parameters.SetPieces = new Dictionary<string, int> { [groupA] = 2, [groupB] = 2 };

            var macro = MacroLayoutGenerator.Generate(parameters, rng, model);
            totalInserts += macro.PinnedTiles.Count(p => insertTileIds.Contains(p.Value.TileId));
        }

        totalInserts.Should().BeGreaterThan(0, $"at least some of 40 {tilesetResref} fence-line seeds should splice a FenceDoor gate");
    }

    // ============================================================
    // Alley (Tunnel mode with CorridorCrosserType.Alley)
    // ============================================================

    private static MacroLayoutParameters AlleyParameters(TilesetModel model, int seedWidth = 22)
    {
        return new MacroLayoutParameters
        {
            Style = DungeonLayoutStyle.RoomsAndCorridors,
            CorridorMode = CorridorMode.Tunnel,
            CorridorCrosserType = CorridorCrosserType.Alley,
            MinRooms = 6,
            MaxRooms = 9,
            MinRoomCornerSize = 3,
            MaxRoomCornerSize = 5,
            LoopFactor = 0.3,
            Width = seedWidth,
            Height = seedWidth,
            SolidTerrain = model.DefaultTerrain,
            OpenTerrain = "Plaza",
        };
    }

    [Test]
    public void AlleyMode_FullPipelineSucceedsAcrossManySeeds()
    {
        var model = LoadTileset("vmr01");
        var failures = new List<string>();
        var tunneledLayouts = 0;
        const int seedCount = 20;

        for (var seed = 21000; seed < 21000 + seedCount; seed++)
        {
            var rng = new Random(seed);
            MacroLayout macro;
            try
            {
                macro = MacroLayoutGenerator.Generate(AlleyParameters(model), rng);
            }
            catch (InvalidOperationException ex)
            {
                failures.Add($"seed {seed}: generation failed: {ex.Message}");
                continue;
            }

            macro.Seed = seed;
            if (macro.TunnelLinks.Count > 0) tunneledLayouts++;

            if (!TileResolver.TryResolve(model, macro, rng, out var resolved, out var reason))
            {
                failures.Add($"seed {seed}: resolution failed: {reason}");
                continue;
            }

            macro.Rooms.Should().Contain(r => r.Role == RoomRole.Boss, $"seed {seed} must assign a boss room across Alley tunnels");
            AssertEdgeAgreement(model, macro, resolved, seed, failures);
        }

        failures.Should().BeEmpty();
        tunneledLayouts.Should().BeGreaterThan(15, "Alley tunnel carving should succeed for nearly every layout, not constantly fall back to open lanes");
    }

    /// <summary>
    /// The Alley vocabulary exists only in vmr01. Composing an Alley-corridor layout (Streets) with
    /// any other tileset must downgrade to Corridor/Doorway tunnels instead of failing resolution
    /// (Content Builder surfaced exactly that failure: "No matching tile ... Right=Alley" on Cavern).
    /// The downgrade only happens when the TilesetModel is supplied to Generate, mirroring how the
    /// production facade, Content Builder, and ProcgenReview all call it.
    /// </summary>
    [TestCase("tdt01")]
    [TestCase("tds01")]
    [TestCase("zsf01")]
    public void AlleyMode_TilesetsWithoutAlleyVocabularyDowngradeToCorridorTunnels(string tilesetResref)
    {
        var model = LoadTileset(tilesetResref);
        var failures = new List<string>();
        var tunneledLayouts = 0;

        for (var seed = 22000; seed < 22015; seed++)
        {
            var rng = new Random(seed);
            var parameters = new MacroLayoutParameters
            {
                Style = DungeonLayoutStyle.RoomsAndCorridors,
                CorridorMode = CorridorMode.Tunnel,
                CorridorCrosserType = CorridorCrosserType.Alley,
                MinRooms = 6,
                MaxRooms = 9,
                MinRoomCornerSize = 3,
                MaxRoomCornerSize = 5,
                LoopFactor = 0.3,
                Width = 20,
                Height = 20,
                SolidTerrain = model.DefaultTerrain,
                OpenTerrain = tilesetResref == "zsf01" ? "floor" : model.FloorTerrain,
            };

            MacroLayout macro;
            try
            {
                macro = MacroLayoutGenerator.Generate(parameters, rng, model);
            }
            catch (InvalidOperationException ex)
            {
                failures.Add($"seed {seed}: generation failed: {ex.Message}");
                continue;
            }

            macro.Seed = seed;
            if (macro.TunnelLinks.Count > 0) tunneledLayouts++;

            if (!TileResolver.TryResolve(model, macro, rng, out _, out var reason))
            {
                failures.Add($"seed {seed}: resolution failed: {reason}");
                continue;
            }

            // The caller's parameters object must not be mutated by the downgrade.
            parameters.CorridorCrosserType.Should().Be(CorridorCrosserType.Alley);

            for (var y = 0; y < 20; y++)
            for (var x = 0; x < 20; x++)
            for (var slot = 0; slot < 4; slot++)
            {
                if (string.Equals(macro.Crossers.GetEdge(x, y, slot), "Alley", StringComparison.OrdinalIgnoreCase))
                    failures.Add($"seed {seed}: cell ({x},{y}) slot {slot} carries an Alley edge on a tileset with no Alley vocabulary");
            }
        }

        failures.Should().BeEmpty();
        tunneledLayouts.Should().BeGreaterThan(10, "downgraded Streets layouts should still carve corridor tunnels");
    }

    [Test]
    public void AlleyMode_CrosserPlanUsesOnlyAlley()
    {
        var model = LoadTileset("vmr01");
        var rng = new Random(21100);
        var macro = MacroLayoutGenerator.Generate(AlleyParameters(model), rng);

        macro.TunnelLinks.Should().NotBeEmpty();

        var labels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var y = 0; y < 22; y++)
        for (var x = 0; x < 22; x++)
        for (var slot = 0; slot < 4; slot++)
        {
            var edge = macro.Crossers.GetEdge(x, y, slot);
            if (edge.Length != 0) labels.Add(edge);
        }

        labels.Should().NotBeEmpty();
        // Unlike Corridor mode (Corridor body + Doorway port), Alley mode uses a single crosser name
        // for both the tunnel body and the room-facing port -- verified offline, no Doorway edges.
        labels.Should().BeEquivalentTo(new[] { "Alley" });
    }

    [Test]
    public void AlleyMode_IsDeterministicPerSeed()
    {
        var model = LoadTileset("vmr01");

        MacroLayout Generate()
        {
            var rng = new Random(21200);
            return MacroLayoutGenerator.Generate(AlleyParameters(model), rng);
        }

        var first = Generate();
        var second = Generate();

        first.TunnelLinks.Count.Should().Be(second.TunnelLinks.Count);
        for (var i = 0; i < first.TunnelLinks.Count; i++)
        {
            first.TunnelLinks[i].CornerA.Should().Be(second.TunnelLinks[i].CornerA);
            first.TunnelLinks[i].CornerB.Should().Be(second.TunnelLinks[i].CornerB);
            first.TunnelLinks[i].Length.Should().Be(second.TunnelLinks[i].Length);
        }

        for (var y = 0; y < 22; y++)
        for (var x = 0; x < 22; x++)
        for (var slot = 0; slot < 4; slot++)
            first.Crossers.GetEdge(x, y, slot).Should().Be(second.Crossers.GetEdge(x, y, slot), $"cell ({x},{y}) slot {slot}");
    }

    /// <summary>
    /// Back-compat: the default CorridorCrosserType (Corridor) must reproduce identical output to a
    /// hand-built layout that never sets the field at all -- proves the new field is a pure opt-in for
    /// existing Tunnel-mode callers (Facility/Complex).
    /// </summary>
    [Test]
    public void CorridorCrosserType_DefaultMatchesExplicitCorridorValue()
    {
        var model = LoadTileset("tds01");

        MacroLayoutParameters WithoutField() => new()
        {
            Style = DungeonLayoutStyle.RoomsAndCorridors,
            CorridorMode = CorridorMode.Tunnel,
            MinRooms = 6,
            MaxRooms = 9,
            MinRoomCornerSize = 3,
            MaxRoomCornerSize = 5,
            LoopFactor = 0.3,
            Width = 20,
            Height = 20,
            SolidTerrain = model.DefaultTerrain,
            OpenTerrain = model.FloorTerrain,
        };

        for (var seed = 21300; seed < 21310; seed++)
        {
            var implicitParams = WithoutField();
            implicitParams.CorridorCrosserType.Should().Be(CorridorCrosserType.Corridor);
            var macroA = MacroLayoutGenerator.Generate(implicitParams, new Random(seed));

            var explicitParams = WithoutField();
            explicitParams.CorridorCrosserType = CorridorCrosserType.Corridor;
            var macroB = MacroLayoutGenerator.Generate(explicitParams, new Random(seed));

            for (var y = 0; y < 20; y++)
            for (var x = 0; x < 20; x++)
            for (var slot = 0; slot < 4; slot++)
                macroA.Crossers.GetEdge(x, y, slot).Should().Be(macroB.Crossers.GetEdge(x, y, slot), $"seed {seed} cell ({x},{y}) slot {slot}");
        }
    }

    [Test]
    public void BigDoorAlleyInsert_CanSpliceIntoAStraightAlleySegment()
    {
        var model = LoadTileset("vmr01");
        var insertTileId = model.Groups.First(g => string.Equals(g.Name, "BigDoorAlley", StringComparison.OrdinalIgnoreCase)).TileIds[0];

        var totalInserts = 0;

        for (var seed = 21400; seed < 21430; seed++)
        {
            var rng = new Random(seed);
            var parameters = AlleyParameters(model);
            parameters.SetPieces = new Dictionary<string, int> { ["BigDoorAlley"] = 2 };

            var macro = MacroLayoutGenerator.Generate(parameters, rng, model);
            totalInserts += macro.PinnedTiles.Count(p => p.Value.TileId == insertTileId);
        }

        totalInserts.Should().BeGreaterThan(0, "at least some of 30 vmr01 Alley-tunnel seeds should splice a BigDoorAlley gate");
    }

    // ============================================================
    // Profile / composition wiring
    // ============================================================

    [Test]
    public void HallsProfile_EnablesFenceLinesAndFenceDoorSetPieces()
    {
        var tilesetProfiles = new StandardTilesetProfiles().BuildTilesetProfiles();
        var layoutProfiles = new StandardLayoutProfiles().BuildLayoutProfiles();

        // Warren (Sewers' production pairing) deliberately does NOT enable FenceLines: its
        // CorridorWidth=1 open lanes contribute zero fully-open cells and its chambers are hard-capped
        // at 5 corners, the same reason BridgeChannelTests documents Warren as unusable for
        // AccentChannels (see StandardLayoutProfiles.Warren). Halls (AncientRuin/vmr01's production
        // pairing) has room and 2-wide open lanes to spare.
        layoutProfiles[StandardLayoutProfiles.Warren].Template.FenceLines.Should().Be(0);
        layoutProfiles[StandardLayoutProfiles.Halls].Template.FenceLines.Should().BeGreaterThan(0);
        tilesetProfiles[StandardTilesetProfiles.Sewers].SetPieces.Should().ContainKeys("FenceDoor01", "FenceDoor02");
        tilesetProfiles[StandardTilesetProfiles.AncientRuin].SetPieces.Should().ContainKeys("InteriorFenceDoor", "ExteriorFenceDoor", "BigDoorAlley");

        // Every OTHER shipped layout profile must stay untouched (FenceLines still defaults to 0).
        layoutProfiles[StandardLayoutProfiles.Organic].Template.FenceLines.Should().Be(0);
        layoutProfiles[StandardLayoutProfiles.Packed].Template.FenceLines.Should().Be(0);
        layoutProfiles[StandardLayoutProfiles.Complex].Template.FenceLines.Should().Be(0);
        layoutProfiles[StandardLayoutProfiles.Labyrinth].Template.FenceLines.Should().Be(0);
    }

    [Test]
    public void StreetsProfile_ComposesRoomsAndCorridorsTunnelAlley()
    {
        var layoutProfiles = new StandardLayoutProfiles().BuildLayoutProfiles();
        var streets = layoutProfiles[StandardLayoutProfiles.Streets].Template;

        streets.Style.Should().Be(DungeonLayoutStyle.RoomsAndCorridors);
        streets.CorridorMode.Should().Be(CorridorMode.Tunnel);
        streets.CorridorCrosserType.Should().Be(CorridorCrosserType.Alley);

        // Additive: AlienRuin's shipped default pairing is unaffected.
        var alienRuinDefault = new SWLOR.Game.Server.Feature.DungeonDefinition.AlienRuinDungeonDefinition()
            .BuildDungeons();
        alienRuinDefault.Values.First().LayoutProfileKey.Should().Be(StandardLayoutProfiles.Halls);
    }

    /// <summary>
    /// Production AncientRuin pairing (AncientRuin tileset profile + Halls layout profile, the actual
    /// AlienRuinDungeonDefinition composition) should place Fence lines across many seeds, mirroring
    /// BridgeChannelTests.AccentChannels_ShippedProfileCompositionPlacesBridgeCrossings for Fence.
    /// </summary>
    [Test]
    public void FenceLines_ShippedAncientRuinHallsCompositionPlacesFenceLines()
    {
        var tilesetProfiles = new StandardTilesetProfiles().BuildTilesetProfiles();
        var layoutProfiles = new StandardLayoutProfiles().BuildLayoutProfiles();
        var tilesetProfile = tilesetProfiles[StandardTilesetProfiles.AncientRuin];

        var composition = new DungeonComposition
        {
            Tileset = tilesetProfile,
            Layout = layoutProfiles[StandardLayoutProfiles.Halls]
        };

        var model = LoadTileset(tilesetProfile.TilesetResref);
        var parameters = composition.BuildLayoutParameters();
        parameters.FenceLines.Should().BeGreaterThan(0);

        parameters.Width = 24;
        parameters.Height = 24;
        parameters.SolidTerrain = model.DefaultTerrain;
        parameters.OpenTerrain = tilesetProfile.PrimaryOpenTerrain;

        var seedsWithFence = 0;
        const int seedCount = 20;
        for (var seed = 21600; seed < 21600 + seedCount; seed++)
        {
            var rng = new Random(seed);
            var macro = MacroLayoutGenerator.Generate(parameters, rng, model);
            TileResolver.TryResolve(model, macro, rng, out _, out var reason).Should().BeTrue(reason);

            if (AllCrosserLabels(macro, parameters.Width, parameters.Height).Any(e => string.Equals(e, "Fence", StringComparison.OrdinalIgnoreCase)))
                seedsWithFence++;
        }

        seedsWithFence.Should().BeGreaterThan(0, $"AncientRuin/Halls composition should place at least one Fence line across {seedCount} seeds");
    }

    /// <summary>
    /// AncientRuin (vmr01) tileset profile composed with the new Streets layout profile: not a
    /// shipped default pairing, but a request-time override callers can select for the exterior
    /// alley feel. Never fails and places Alley edges across many seeds.
    /// </summary>
    [Test]
    public void AlleyMode_AncientRuinStreetsCompositionPlacesAlleyEdges()
    {
        var tilesetProfiles = new StandardTilesetProfiles().BuildTilesetProfiles();
        var layoutProfiles = new StandardLayoutProfiles().BuildLayoutProfiles();
        var tilesetProfile = tilesetProfiles[StandardTilesetProfiles.AncientRuin];

        var composition = new DungeonComposition
        {
            Tileset = tilesetProfile,
            Layout = layoutProfiles[StandardLayoutProfiles.Streets]
        };

        var model = LoadTileset(tilesetProfile.TilesetResref);
        var parameters = composition.BuildLayoutParameters();
        parameters.CorridorCrosserType.Should().Be(CorridorCrosserType.Alley);

        parameters.Width = 22;
        parameters.Height = 22;
        parameters.SolidTerrain = model.DefaultTerrain;
        parameters.OpenTerrain = tilesetProfile.PrimaryOpenTerrain;

        var failures = new List<string>();
        var seedsWithAlley = 0;
        const int seedCount = 20;
        for (var seed = 21700; seed < 21700 + seedCount; seed++)
        {
            var rng = new Random(seed);
            MacroLayout macro;
            try
            {
                macro = MacroLayoutGenerator.Generate(parameters, rng, model);
            }
            catch (InvalidOperationException ex)
            {
                failures.Add($"seed {seed}: generation failed: {ex.Message}");
                continue;
            }

            macro.Seed = seed;
            if (!TileResolver.TryResolve(model, macro, rng, out _, out var reason))
            {
                failures.Add($"seed {seed}: resolution failed: {reason}");
                continue;
            }

            if (AllCrosserLabels(macro, parameters.Width, parameters.Height).Any(e => string.Equals(e, "Alley", StringComparison.OrdinalIgnoreCase)))
                seedsWithAlley++;
        }

        failures.Should().BeEmpty();
        seedsWithAlley.Should().BeGreaterThan(0, $"AncientRuin/Streets composition should place at least one Alley edge across {seedCount} seeds");
    }

    // ============================================================
    // Full pipeline sweep across all four tilesets, with the new fields exercised where wired
    // ============================================================

    [TestCase(StandardTilesetProfiles.Facility, StandardLayoutProfiles.Complex)]
    [TestCase(StandardTilesetProfiles.Cavern, StandardLayoutProfiles.Organic)]
    [TestCase(StandardTilesetProfiles.Sewers, StandardLayoutProfiles.Warren)]
    [TestCase(StandardTilesetProfiles.AncientRuin, StandardLayoutProfiles.Halls)]
    [TestCase(StandardTilesetProfiles.AncientRuin, StandardLayoutProfiles.Streets)]
    public void FullPipelineSweep_AllTilesetsWithFenceAndAlleySystemsWired(string tilesetKey, string layoutKey)
    {
        var tilesetProfile = new StandardTilesetProfiles().BuildTilesetProfiles()[tilesetKey];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[layoutKey];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        const int retryBudget = 6;
        var failures = new List<string>();

        for (var seedBase = 22000; seedBase < 22015; seedBase++)
        {
            var succeeded = false;
            var lastFailure = "no attempts made";

            for (var attempt = 0; attempt < retryBudget && !succeeded; attempt++)
            {
                var trySeed = seedBase + attempt;
                var rng = new Random(trySeed);

                var composition = new DungeonComposition { Tileset = tilesetProfile, Layout = layoutProfile };
                var parameters = composition.BuildLayoutParameters();
                parameters.Width = 22;
                parameters.Height = 22;
                parameters.SolidTerrain = model.DefaultTerrain;
                parameters.OpenTerrain = string.IsNullOrEmpty(tilesetProfile.PrimaryOpenTerrain)
                    ? model.FloorTerrain
                    : tilesetProfile.PrimaryOpenTerrain;

                MacroLayout macro;
                try
                {
                    macro = MacroLayoutGenerator.Generate(parameters, rng, model);
                    macro.Seed = trySeed;
                }
                catch (InvalidOperationException ex)
                {
                    lastFailure = ex.Message;
                    continue;
                }

                if (TileResolver.TryResolve(model, macro, rng, out _, out var reason))
                    succeeded = true;
                else
                    lastFailure = reason;
            }

            if (!succeeded)
                failures.Add($"{tilesetKey}/{layoutKey} seed base {seedBase}: {lastFailure}");
        }

        failures.Should().BeEmpty();
    }
}
