using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.AreaGenerationService;
using SWLOR.Game.Server.Service.AreaGenerationService.Tileset;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

public class TileResolverTests
{
    private const string Wall = "Wall";
    private const string Floor = "Floor";

    /// <summary>
    /// Minimal synthetic tileset: one tile per Wall/Floor combination of the 4 corners (16 tiles),
    /// plus a few duplicate-corner tiles so candidate lists sometimes have more than one entry.
    /// All tiles are ungrouped, edge/door/height-free, matching the v1 TileResolver scope.
    /// </summary>
    private static TilesetModel BuildFixtureTileset(bool includeAllWallCombo = true)
    {
        var tileset = new TilesetModel
        {
            Resref = "tst01",
            Name = "Synthetic Test Tileset",
            Terrains = new List<string> { Wall, Floor },
            DefaultTerrain = Wall,
            FloorTerrain = Floor
        };

        var nextTileId = 0;

        for (var combo = 0; combo < 16; combo++)
        {
            var tl = (combo & 8) != 0 ? Floor : Wall;
            var tr = (combo & 4) != 0 ? Floor : Wall;
            var br = (combo & 2) != 0 ? Floor : Wall;
            var bl = (combo & 1) != 0 ? Floor : Wall;

            if (!includeAllWallCombo && tl == Wall && tr == Wall && br == Wall && bl == Wall)
                continue;

            tileset.Tiles.Add(NewTile(nextTileId++, tl, tr, br, bl));
        }

        // Duplicates so a couple of corner combos have multiple candidates to choose among.
        if (includeAllWallCombo)
            tileset.Tiles.Add(NewTile(nextTileId++, Wall, Wall, Wall, Wall));

        tileset.Tiles.Add(NewTile(nextTileId++, Floor, Floor, Floor, Floor));
        tileset.Tiles.Add(NewTile(nextTileId, Floor, Floor, Floor, Floor));

        return tileset;
    }

    private static TileRecord NewTile(int tileId, string tl, string tr, string br, string bl)
    {
        return new TileRecord
        {
            TileId = tileId,
            Corners = new[] { tl, tr, br, bl },
            CornerHeights = new[] { 0, 0, 0, 0 },
            Edges = new[] { "", "", "", "" },
            GroupIndex = -1
        };
    }

    private static TileRecord FindTile(TilesetModel tileset, int tileId)
    {
        foreach (var tile in tileset.Tiles)
        {
            if (tile.TileId == tileId)
                return tile;
        }

        throw new InvalidOperationException($"Tile {tileId} not found in fixture.");
    }

    private static MacroLayoutParameters DefaultParameters()
    {
        return new MacroLayoutParameters
        {
            Width = 16,
            Height = 16,
            SolidTerrain = Wall,
            OpenTerrain = Floor,
            MinRooms = 4,
            MaxRooms = 6
        };
    }

    [Test]
    public void TryResolve_GeneratedLayout_SucceedsAndMatchesCorners()
    {
        var tileset = BuildFixtureTileset();
        var layout = MacroLayoutGenerator.Generate(DefaultParameters(), new Random(7));
        layout.Seed = 7;

        var success = TileResolver.TryResolve(tileset, layout, new Random(99), out var resolved, out var failureReason);

        success.Should().BeTrue(failureReason);
        resolved.Should().NotBeNull();
        resolved.TilesetResref.Should().Be(tileset.Resref);
        resolved.Seed.Should().Be(7);
        resolved.Width.Should().Be(layout.Corners.Width);
        resolved.Height.Should().Be(layout.Corners.Height);
        resolved.Rooms.Should().BeSameAs(layout.Rooms);

        for (var y = 0; y < resolved.Height; y++)
        {
            for (var x = 0; x < resolved.Width; x++)
            {
                var expectedTl = layout.Corners.Labels[x, y + 1];
                var expectedTr = layout.Corners.Labels[x + 1, y + 1];
                var expectedBr = layout.Corners.Labels[x + 1, y];
                var expectedBl = layout.Corners.Labels[x, y];

                var resolvedTile = resolved.GetTile(x, y);
                var tileRecord = FindTile(tileset, resolvedTile.TileId);

                tileRecord.GetCornerAt(resolvedTile.Orientation, CornerSlot.TopLeft).Should().Be(expectedTl, $"cell ({x},{y}) TL");
                tileRecord.GetCornerAt(resolvedTile.Orientation, CornerSlot.TopRight).Should().Be(expectedTr, $"cell ({x},{y}) TR");
                tileRecord.GetCornerAt(resolvedTile.Orientation, CornerSlot.BottomRight).Should().Be(expectedBr, $"cell ({x},{y}) BR");
                tileRecord.GetCornerAt(resolvedTile.Orientation, CornerSlot.BottomLeft).Should().Be(expectedBl, $"cell ({x},{y}) BL");

                resolvedTile.Height.Should().Be(0);
            }
        }
    }

    [Test]
    public void TryResolve_SameInputsSameSeed_IsDeterministic()
    {
        var tileset = BuildFixtureTileset();
        var layout = MacroLayoutGenerator.Generate(DefaultParameters(), new Random(11));
        layout.Seed = 11;

        TileResolver.TryResolve(tileset, layout, new Random(42), out var resolvedA, out _);
        TileResolver.TryResolve(tileset, layout, new Random(42), out var resolvedB, out _);

        resolvedA.Should().NotBeNull();
        resolvedB.Should().NotBeNull();
        resolvedA.Tiles.Should().HaveCount(resolvedB.Tiles.Length);

        for (var i = 0; i < resolvedA.Tiles.Length; i++)
        {
            resolvedA.Tiles[i].TileId.Should().Be(resolvedB.Tiles[i].TileId, $"tile index {i}");
            resolvedA.Tiles[i].Orientation.Should().Be(resolvedB.Tiles[i].Orientation, $"tile index {i}");
        }
    }

    [Test]
    public void TryResolve_MissingCornerCombination_FailsWithFirstUnresolvableCell()
    {
        // Tile (0,0) always touches corners (0,1),(1,1),(1,0),(0,0), all of which remain solid by
        // construction (border ring plus the mandatory >=1 solid-corner gap around rooms/corridors).
        // Omitting the all-Wall tile guarantees resolution fails there first.
        var tileset = BuildFixtureTileset(includeAllWallCombo: false);
        var layout = MacroLayoutGenerator.Generate(DefaultParameters(), new Random(3));
        layout.Seed = 3;

        var success = TileResolver.TryResolve(tileset, layout, new Random(1), out var resolved, out var failureReason);

        success.Should().BeFalse();
        resolved.Should().BeNull();
        failureReason.Should().Contain("(0,0)");
        failureReason.Should().Contain("TL=Wall");
        failureReason.Should().Contain("BL=Wall");
    }
}
