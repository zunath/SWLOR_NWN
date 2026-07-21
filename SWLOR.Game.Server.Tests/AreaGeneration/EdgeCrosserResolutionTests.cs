using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.AreaGenerationService;
using SWLOR.Game.Server.Service.AreaGenerationService.Tileset;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

/// <summary>
/// Phase 1 of "full tile viability": edge crossers (Corridor, Doorway, Bridge, Fence, Alley, ...) are
/// now first-class in the corner+edge resolver, alongside the pre-existing crosser-free corner match.
/// These tests pin two things: (1) back-compat — a fully blank <see cref="MacroLayout.Crossers"/> grid
/// (the only kind any layout style produces today) must resolve byte-identically to the pre-crosser
/// resolver and must never select a crosser or door-slot tile; (2) the new behavior — a layout that
/// actually requests crosser edges resolves to tiles whose oriented edges match, and door-slot crosser
/// tiles are gated to Doorway-expecting keys only.
/// </summary>
public class EdgeCrosserResolutionTests
{
    private static TilesetModel LoadTileset(string tilesetResref) => TilesetTestSource.LoadTileset(tilesetResref);

    // ------------------------------------------------------------------
    // 1. Back-compat regression: blank Crossers must resolve exactly like the pre-crosser resolver.
    // ------------------------------------------------------------------

    [TestCase("tdt01")]
    [TestCase("tds01")]
    [TestCase("zsf01")]
    [TestCase("vmr01")]
    public void BlankCrossers_ResolvesDeterministicallyAndNeverSelectsCrosserOrDoorTiles(string tilesetResref)
    {
        var model = LoadTileset(tilesetResref);

        foreach (var seed in new[] { 5, 17, 4001 })
        {
            ResolvedLayout Run()
            {
                // DoorTransitions=false: this test isolates TileResolver's corner+edge matching stage.
                // With it enabled (the real-generation default), TileDoorPlanner legitimately substitutes
                // door-slot/crosser tiles for transitions after the initial resolve (see
                // TileDoorPlannerTests) — that is sanctioned, unrelated behavior this test must not
                // mistake for a corner+edge-matching leak.
                var macro = MacroLayoutGenerator.Generate(new MacroLayoutParameters
                {
                    Width = 16,
                    Height = 16,
                    SolidTerrain = model.DefaultTerrain,
                    OpenTerrain = model.FloorTerrain,
                    MinRooms = 4,
                    MaxRooms = 8,
                    DoorTransitions = false
                }, new Random(seed));
                macro.Seed = seed;

                // Every layout style today leaves Crossers fully blank — confirm that before trusting
                // the "blank crossers" premise of this test.
                for (var x = 0; x < macro.Corners.Width; x++)
                for (var y = 0; y < macro.Corners.Height; y++)
                foreach (var slot in new[] { EdgeSlot.Top, EdgeSlot.Right, EdgeSlot.Bottom, EdgeSlot.Left })
                {
                    macro.Crossers.GetEdge(x, y, slot).Should().BeEmpty(
                        $"{tilesetResref} seed {seed}: no layout style emits crossers yet, so Crossers must stay blank");
                }

                TileResolver.TryResolve(model, macro, new Random(seed * 97 + 3), out var resolved, out var failure)
                    .Should().BeTrue($"{tilesetResref} seed {seed}: {failure}");
                return resolved;
            }

            var a = Run();
            var b = Run();

            a.Tiles.Should().HaveCount(b.Tiles.Length);
            for (var i = 0; i < a.Tiles.Length; i++)
            {
                a.Tiles[i].TileId.Should().Be(b.Tiles[i].TileId, $"{tilesetResref} seed {seed} tile {i}");
                a.Tiles[i].Orientation.Should().Be(b.Tiles[i].Orientation, $"{tilesetResref} seed {seed} tile {i}");
            }

            // No leakage: a blank-crosser layout must never resolve to a tile carrying any edge crosser,
            // nor to a door-slot tile (door-slot tiles remain TileDoorPlanner's inventory here).
            foreach (var tile in a.Tiles)
            {
                var record = model.Tiles[tile.TileId];
                record.HasAnyCrosser.Should().BeFalse(
                    $"{tilesetResref} seed {seed}: blank-crosser cell resolved to a crosser tile (tile {tile.TileId})");
                record.Doors.Should().BeEmpty(
                    $"{tilesetResref} seed {seed}: blank-crosser cell resolved to a door-slot tile (tile {tile.TileId})");
            }
        }
    }

    // ------------------------------------------------------------------
    // 2. Edge matching: a hand-built horizontal Corridor run on zsf01 resolves to the corridor tile at
    //    the correct orientation, and adjacent cells agree on the shared edge.
    // ------------------------------------------------------------------

    [Test]
    public void HandBuiltCorridorRun_ResolvesMatchingEdgesOnZsf01()
    {
        var model = LoadTileset("zsf01");

        const int width = 4;
        const int height = 4;
        const int corridorRow = 2;

        // All-solid corners (never opened) — this test only exercises edge/crosser matching, not room
        // carving, so the macro layout is built by hand rather than through MacroLayoutGenerator.
        var corners = new CornerTerrainGrid(width, height, model.DefaultTerrain);
        var layout = new MacroLayout(corners) { Seed = 1 };

        // A full-width Corridor row: every vertical edge in the row (both map borders and the internal
        // shared edges) is Corridor, so every cell in the row expects Left=Corridor, Right=Corridor,
        // Top=Bottom=blank — the "straight through" pattern zsf01's TILE6 provides.
        for (var x = 0; x < width; x++)
            layout.Crossers.SetEdge(x, corridorRow, EdgeSlot.Right, "Corridor");
        layout.Crossers.SetEdge(0, corridorRow, EdgeSlot.Left, "Corridor");

        var success = TileResolver.TryResolve(model, layout, new Random(123), out var resolved, out var failureReason);
        success.Should().BeTrue(failureReason);

        for (var x = 0; x < width; x++)
        {
            var resolvedTile = resolved.GetTile(x, corridorRow);
            var record = model.Tiles[resolvedTile.TileId];

            record.GetEdgeAt(resolvedTile.Orientation, EdgeSlot.Left).Should().BeEquivalentTo("Corridor", $"cell ({x},{corridorRow}) Left");
            record.GetEdgeAt(resolvedTile.Orientation, EdgeSlot.Right).Should().BeEquivalentTo("Corridor", $"cell ({x},{corridorRow}) Right");
            record.GetEdgeAt(resolvedTile.Orientation, EdgeSlot.Top).Should().BeEmpty($"cell ({x},{corridorRow}) Top");
            record.GetEdgeAt(resolvedTile.Orientation, EdgeSlot.Bottom).Should().BeEmpty($"cell ({x},{corridorRow}) Bottom");

            record.GetCornerAt(resolvedTile.Orientation, CornerSlot.TopLeft).Should().Be(model.DefaultTerrain);
            record.GetCornerAt(resolvedTile.Orientation, CornerSlot.TopRight).Should().Be(model.DefaultTerrain);
            record.GetCornerAt(resolvedTile.Orientation, CornerSlot.BottomRight).Should().Be(model.DefaultTerrain);
            record.GetCornerAt(resolvedTile.Orientation, CornerSlot.BottomLeft).Should().Be(model.DefaultTerrain);
        }

        // Adjacent cells agree on the shared edge (the storage is shared, but assert via the resolved
        // tiles themselves rather than trusting the grid alone).
        for (var x = 0; x < width - 1; x++)
        {
            var left = resolved.GetTile(x, corridorRow);
            var leftRecord = model.Tiles[left.TileId];
            var right = resolved.GetTile(x + 1, corridorRow);
            var rightRecord = model.Tiles[right.TileId];

            leftRecord.GetEdgeAt(left.Orientation, EdgeSlot.Right)
                .Should().BeEquivalentTo(rightRecord.GetEdgeAt(right.Orientation, EdgeSlot.Left),
                    $"cells ({x},{corridorRow})/({x + 1},{corridorRow}) must agree on their shared edge");
        }

        // Rows above/below the corridor stay ordinary all-solid, blank-edge cells and still resolve.
        for (var y = 0; y < height; y++)
        {
            if (y == corridorRow) continue;
            for (var x = 0; x < width; x++)
            {
                var tile = resolved.GetTile(x, y);
                model.Tiles[tile.TileId].HasAnyCrosser.Should().BeFalse($"cell ({x},{y}) is outside the corridor row");
            }
        }
    }

    [Test]
    public void HandBuiltCorridorRun_IsDeterministicPerSeed()
    {
        var model = LoadTileset("zsf01");

        ResolvedLayout Build(int seed)
        {
            const int width = 4;
            const int height = 4;
            const int corridorRow = 2;

            var corners = new CornerTerrainGrid(width, height, model.DefaultTerrain);
            var layout = new MacroLayout(corners) { Seed = seed };

            for (var x = 0; x < width; x++)
                layout.Crossers.SetEdge(x, corridorRow, EdgeSlot.Right, "Corridor");
            layout.Crossers.SetEdge(0, corridorRow, EdgeSlot.Left, "Corridor");

            TileResolver.TryResolve(model, layout, new Random(seed), out var resolved, out var failure).Should().BeTrue(failure);
            return resolved;
        }

        var a = Build(777);
        var b = Build(777);

        for (var i = 0; i < a.Tiles.Length; i++)
        {
            a.Tiles[i].TileId.Should().Be(b.Tiles[i].TileId, $"tile {i}");
            a.Tiles[i].Orientation.Should().Be(b.Tiles[i].Orientation, $"tile {i}");
        }
    }

    // ------------------------------------------------------------------
    // 3. Doorway-with-door-slot gating: a crosser tile that also carries door slots must only be a
    //    candidate under a key whose edge part actually contains a Doorway crosser.
    // ------------------------------------------------------------------

    [Test]
    public void DoorSlotCrosserTile_OnTdt01_OnlyMatchesKeysContainingDoorway()
    {
        var model = LoadTileset("tdt01");

        // Find a flat, ungrouped tile that has BOTH a door slot and at least one crosser edge — the
        // exact category the gating rule targets. tdt01 is known (see TileDoorPlannerTests) to have
        // usable flat, ungrouped door-slot tiles.
        var candidateTiles = model.Tiles
            .Where(t => t.GroupIndex == -1 &&
                        t.Doors.Count != 0 &&
                        t.HasAnyCrosser &&
                        t.CornerHeights.All(h => h == 0))
            .ToList();

        candidateTiles.Should().NotBeEmpty("tdt01 should have at least one flat, ungrouped, door-slot tile with a crosser edge");

        var probed = false;

        foreach (var tile in candidateTiles)
        {
            for (var orientation = 0; orientation < 4; orientation++)
            {
                var top = tile.GetEdgeAt(orientation, EdgeSlot.Top);
                var right = tile.GetEdgeAt(orientation, EdgeSlot.Right);
                var bottom = tile.GetEdgeAt(orientation, EdgeSlot.Bottom);
                var left = tile.GetEdgeAt(orientation, EdgeSlot.Left);

                var tl = tile.GetCornerAt(orientation, CornerSlot.TopLeft);
                var tr = tile.GetCornerAt(orientation, CornerSlot.TopRight);
                var br = tile.GetCornerAt(orientation, CornerSlot.BottomRight);
                var bl = tile.GetCornerAt(orientation, CornerSlot.BottomLeft);

                var hasDoorwayEdge =
                    IsDoorway(top) || IsDoorway(right) || IsDoorway(bottom) || IsDoorway(left);

                if (!hasDoorwayEdge)
                    continue; // the negative case (must NOT leak in) is covered by the sibling test below.

                // This exact tile/orientation is itself proof the key resolves (it's a member of the
                // qualifying set), so this must always be true.
                TileResolver.HasCandidate(model, tl, tr, br, bl, top, right, bottom, left).Should().BeTrue(
                    $"tile {tile.TileId} orientation {orientation} has a Doorway edge and should be a valid candidate for its own key");
                probed = true;
            }
        }

        probed.Should().BeTrue("expected to find at least one door-slot crosser tile/orientation whose edges include Doorway");
    }

    /// <summary>
    /// Synthetic fixture isolating the gating rule itself, independent of which real tilesets happen to
    /// carry which combination today. Confirmed against real data first (see
    /// <see cref="DoorSlotCrosserTile_OnTdt01_OnlyMatchesKeysContainingDoorway"/>): every real ungrouped
    /// flat door-slot tile with a crosser also carries a Doorway crosser (rotation can't remove it once
    /// present), so a real-data-only test could never exercise the "door + crosser, but never Doorway"
    /// branch — tdt01's own such tiles (e.g. TILE67: Doors=1, edges Corridor/Corridor, no Doorway) all
    /// turn out to be GROUP members, excluded upstream by the ungrouped check regardless of gating. This
    /// fixture exercises the gating rule directly instead of depending on that accident of real data.
    /// </summary>
    private static TilesetModel BuildDoorGatingFixture()
    {
        var tileset = new TilesetModel { Resref = "gate_fixture", DefaultTerrain = "Wall", FloorTerrain = "Floor" };

        // Ordinary solid tile: no crosser, no door — must resolve the plain blank key.
        tileset.Tiles.Add(new TileRecord
        {
            TileId = 0,
            Corners = new[] { "Wall", "Wall", "Wall", "Wall" },
            CornerHeights = new[] { 0, 0, 0, 0 },
            Edges = new[] { "", "", "", "" },
            GroupIndex = -1,
            PathNode = "A"
        });

        // Crosser (Corridor) door tile with NO Doorway edge anywhere — must be excluded from EVERY key,
        // including one matching its own Corridor/Corridor pattern exactly, per the gating rule.
        tileset.Tiles.Add(new TileRecord
        {
            TileId = 1,
            Corners = new[] { "Wall", "Wall", "Wall", "Wall" },
            CornerHeights = new[] { 0, 0, 0, 0 },
            Edges = new[] { "Corridor", "", "Corridor", "" },
            GroupIndex = -1,
            PathNode = "A",
            Doors = new List<TileDoorRecord> { new() }
        });

        // Crosser+Doorway door tile — must be a candidate ONLY for keys whose edge part contains Doorway.
        tileset.Tiles.Add(new TileRecord
        {
            TileId = 2,
            Corners = new[] { "Wall", "Wall", "Wall", "Wall" },
            CornerHeights = new[] { 0, 0, 0, 0 },
            Edges = new[] { "Doorway", "", "", "" },
            GroupIndex = -1,
            PathNode = "A",
            Doors = new List<TileDoorRecord> { new() }
        });

        return tileset;
    }

    [Test]
    public void GatingFixture_PlainBlankKey_ResolvesOnlyToTheDoorlessCrosserFreeTile()
    {
        var tileset = BuildDoorGatingFixture();

        TileResolver.HasCandidate(tileset, "Wall", "Wall", "Wall", "Wall", "", "", "", "").Should().BeTrue(
            "the ordinary crosser-free, door-free tile must still satisfy the blank key");

        var layout = BuildSingleCellLayout("Wall", "Wall", "Wall", "Wall", "", "", "", "");
        TileResolver.TryResolve(tileset, layout, new Random(1), out var resolved, out var failure).Should().BeTrue(failure);
        resolved.GetTile(0, 0).TileId.Should().Be(0, "only the crosser-free tile is eligible for the blank key");
    }

    [Test]
    public void GatingFixture_CorridorKeyWithoutDoorway_HasNoCandidate()
    {
        var tileset = BuildDoorGatingFixture();

        // TileId 1 is the ONLY tile whose base edges are Corridor/Corridor, but it carries a door slot
        // and no Doorway crosser — the gating rule must exclude it from this key entirely, leaving no
        // candidate at all (not "falls back to some other tile": there IS no other tile shaped this way).
        TileResolver.HasCandidate(tileset, "Wall", "Wall", "Wall", "Wall", "Corridor", "", "Corridor", "").Should().BeFalse(
            "a door-slot crosser tile with no Doorway edge must never be a candidate for any key, including its own");

        var layout = BuildSingleCellLayout("Wall", "Wall", "Wall", "Wall", "Corridor", "", "Corridor", "");
        TileResolver.TryResolve(tileset, layout, new Random(1), out _, out var failure).Should().BeFalse();
        failure.Should().Contain("Corridor", "the failure message should surface the unmatched non-blank edges");
    }

    [Test]
    public void GatingFixture_DoorwayKey_ResolvesOnlyToTheDoorwayCrosserDoorTile()
    {
        var tileset = BuildDoorGatingFixture();

        TileResolver.HasCandidate(tileset, "Wall", "Wall", "Wall", "Wall", "Doorway", "", "", "").Should().BeTrue(
            "the Doorway-crosser door tile must be a candidate for a key whose edge part contains Doorway");

        var layout = BuildSingleCellLayout("Wall", "Wall", "Wall", "Wall", "Doorway", "", "", "");
        TileResolver.TryResolve(tileset, layout, new Random(1), out var resolved, out var failure).Should().BeTrue(failure);
        resolved.GetTile(0, 0).TileId.Should().Be(2, "only the Doorway-crosser door tile is eligible for a Doorway-bearing key");
    }

    private static MacroLayout BuildSingleCellLayout(
        string tl, string tr, string br, string bl,
        string top, string right, string bottom, string left)
    {
        var corners = new CornerTerrainGrid(1, 1, tl);
        corners.Labels[0, 1] = tl;
        corners.Labels[1, 1] = tr;
        corners.Labels[1, 0] = br;
        corners.Labels[0, 0] = bl;

        var layout = new MacroLayout(corners) { Seed = 1 };
        layout.Crossers.SetEdge(0, 0, EdgeSlot.Top, top);
        layout.Crossers.SetEdge(0, 0, EdgeSlot.Right, right);
        layout.Crossers.SetEdge(0, 0, EdgeSlot.Bottom, bottom);
        layout.Crossers.SetEdge(0, 0, EdgeSlot.Left, left);
        return layout;
    }

    private static bool IsDoorway(string edge)
    {
        return string.Equals(edge, "Doorway", StringComparison.OrdinalIgnoreCase);
    }
}
