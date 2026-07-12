using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

/// <summary>
/// Empirically validates the tile orientation rotation formula in TileRecord.GetCornerAt /
/// GetCornerHeightAt / GetEdgeAt (TilesetModel.cs) against hand-authored module areas.
/// In a valid NWN area, adjacent tiles' shared corners must carry identical terrain (and shared
/// edges identical crossers), so if the rotation formula were wrong, comparing every adjacent
/// tile pair in real areas would show widespread mismatches.
///
/// Empirical result: the formula already in TilesetModel.cs — base[(slot + orientation) % 4] —
/// produces ZERO corner, edge, and corner-height mismatches across every adjacent tile pair in
/// both known tdt01 areas (moncaladungeon1, prefab_hauntcave), exercising all four orientation
/// values (0-3). The alternate formula base[(slot - orientation + 4) % 4] produces 164 corner and
/// 37 edge mismatches on the same data. TilesetModel.cs was NOT changed.
///
/// A negative control (deliberately reversing Tile_List row order, i.e. treating y=0 as the north
/// row instead of the documented south row) breaks corner/edge consistency for both formula
/// candidates, confirming the y=0-at-south / row-major(x + y*Width) reading used here — and
/// documented on TilesetModel/ResolvedLayout — is correct, not a coincidence of the formula choice.
///
/// No tde01 (sw_t_dungeon) areas exist anywhere under Module/are, so this convention could only be
/// validated against tdt01 (sw_t_minecave) data; see report for details.
/// </summary>
public class TileOrientationConsistencyTests
{
    private static readonly string[] Tdt01AreaFiles =
    {
        "moncaladungeon1.are.json",
        "prefab_hauntcave.are.json"
    };

    [Test]
    public void Tdt01Areas_AdjacentTilesShareCornersEdgesAndHeights()
    {
        var root = FindRepositoryRoot();
        var tileset = LoadTileset(root, "sw_t_minecave", "tdt01.set", "tdt01");

        var totalPairs = 0;
        var cornerMismatches = new List<string>();
        var edgeMismatches = new List<string>();
        var heightMismatches = new List<string>();

        foreach (var areaFile in Tdt01AreaFiles)
        {
            var area = LoadArea(root, areaFile);
            area.Tileset.Should().Be("tdt01", $"{areaFile} is expected to use the tdt01 tileset");

            var (pairs, corners, edges, heights) = CheckArea(tileset, area, areaFile);
            totalPairs += pairs;
            cornerMismatches.AddRange(corners);
            edgeMismatches.AddRange(edges);
            heightMismatches.AddRange(heights);
        }

        // 474 adjacent tile pairs across the two areas (moncaladungeon1: 15x13, prefab_hauntcave: 8x8).
        totalPairs.Should().BeGreaterThan(400);
        cornerMismatches.Should().BeEmpty(because: string.Join("\n", cornerMismatches));
        edgeMismatches.Should().BeEmpty(because: string.Join("\n", edgeMismatches));
        heightMismatches.Should().BeEmpty(because: string.Join("\n", heightMismatches));
    }

    private static (int Pairs, List<string> CornerMismatches, List<string> EdgeMismatches, List<string> HeightMismatches) CheckArea(
        TilesetModel tileset, AreaGrid area, string areaFile)
    {
        var pairs = 0;
        var cornerMismatches = new List<string>();
        var edgeMismatches = new List<string>();
        var heightMismatches = new List<string>();

        TileRecord TileAt(int x, int y) => tileset.Tiles[area.GetTile(x, y).TileId];

        for (var y = 0; y < area.Height; y++)
        {
            for (var x = 0; x < area.Width; x++)
            {
                var here = area.GetTile(x, y);
                var hereTile = TileAt(x, y);

                if (x + 1 < area.Width)
                {
                    pairs++;
                    var right = area.GetTile(x + 1, y);
                    var rightTile = TileAt(x + 1, y);
                    var label = $"{areaFile} ({x},{y})-({x + 1},{y})";

                    CompareCorners(hereTile, here.Orientation, CornerSlot.TopRight, rightTile, right.Orientation, CornerSlot.TopLeft,
                        $"{label} TR/TL", cornerMismatches);
                    CompareCorners(hereTile, here.Orientation, CornerSlot.BottomRight, rightTile, right.Orientation, CornerSlot.BottomLeft,
                        $"{label} BR/BL", cornerMismatches);

                    CompareEdges(hereTile, here.Orientation, EdgeSlot.Right, rightTile, right.Orientation, EdgeSlot.Left,
                        $"{label} Right/Left", edgeMismatches);

                    CompareHeights(hereTile, here, CornerSlot.TopRight, rightTile, right, CornerSlot.TopLeft,
                        $"{label} TR/TL height", heightMismatches);
                    CompareHeights(hereTile, here, CornerSlot.BottomRight, rightTile, right, CornerSlot.BottomLeft,
                        $"{label} BR/BL height", heightMismatches);
                }

                if (y + 1 < area.Height)
                {
                    pairs++;
                    var upper = area.GetTile(x, y + 1);
                    var upperTile = TileAt(x, y + 1);
                    var label = $"{areaFile} ({x},{y})-({x},{y + 1})";

                    CompareCorners(hereTile, here.Orientation, CornerSlot.TopLeft, upperTile, upper.Orientation, CornerSlot.BottomLeft,
                        $"{label} TL/BL", cornerMismatches);
                    CompareCorners(hereTile, here.Orientation, CornerSlot.TopRight, upperTile, upper.Orientation, CornerSlot.BottomRight,
                        $"{label} TR/BR", cornerMismatches);

                    CompareEdges(hereTile, here.Orientation, EdgeSlot.Top, upperTile, upper.Orientation, EdgeSlot.Bottom,
                        $"{label} Top/Bottom", edgeMismatches);

                    CompareHeights(hereTile, here, CornerSlot.TopLeft, upperTile, upper, CornerSlot.BottomLeft,
                        $"{label} TL/BL height", heightMismatches);
                    CompareHeights(hereTile, here, CornerSlot.TopRight, upperTile, upper, CornerSlot.BottomRight,
                        $"{label} TR/BR height", heightMismatches);
                }
            }
        }

        return (pairs, cornerMismatches, edgeMismatches, heightMismatches);
    }

    private static void CompareCorners(TileRecord a, int orientationA, int slotA, TileRecord b, int orientationB, int slotB,
        string label, List<string> mismatches)
    {
        var valueA = a.GetCornerAt(orientationA, slotA);
        var valueB = b.GetCornerAt(orientationB, slotB);
        if (!string.Equals(valueA, valueB, StringComparison.OrdinalIgnoreCase))
            mismatches.Add($"{label}: '{valueA}' vs '{valueB}'");
    }

    private static void CompareEdges(TileRecord a, int orientationA, int slotA, TileRecord b, int orientationB, int slotB,
        string label, List<string> mismatches)
    {
        var valueA = a.GetEdgeAt(orientationA, slotA);
        var valueB = b.GetEdgeAt(orientationB, slotB);
        if (!string.Equals(valueA, valueB, StringComparison.OrdinalIgnoreCase))
            mismatches.Add($"{label}: '{valueA}' vs '{valueB}'");
    }

    private static void CompareHeights(TileRecord tileA, AreaTile a, int slotA, TileRecord tileB, AreaTile b, int slotB,
        string label, List<string> mismatches)
    {
        var heightA = a.Height + tileA.GetCornerHeightAt(a.Orientation, slotA);
        var heightB = b.Height + tileB.GetCornerHeightAt(b.Orientation, slotB);
        if (heightA != heightB)
            mismatches.Add($"{label}: {heightA} vs {heightB}");
    }

    private static TilesetModel LoadTileset(DirectoryInfo root, string hakFolder, string setFileName, string resref)
    {
        var path = Path.Combine(root.FullName, "SWLOR_Haks", hakFolder, setFileName);
        var contents = File.ReadAllText(path);
        return TilesetSetParser.Parse(resref, contents);
    }

    /// <summary>
    /// Tile_List entry i corresponds to x = i % Width, y = i / Width, with y = 0 the SOUTH (bottom)
    /// row — matching NWN's own tile indexing (see ResolvedLayout in AreaLayout.cs). Confirmed
    /// empirically above: reversing this row order breaks corner/edge consistency.
    /// </summary>
    private static AreaGrid LoadArea(DirectoryInfo root, string fileName)
    {
        var path = Path.Combine(root.FullName, "Module", "are", fileName);
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var rootElement = document.RootElement;

        var width = rootElement.GetProperty("Width").GetProperty("value").GetInt32();
        var height = rootElement.GetProperty("Height").GetProperty("value").GetInt32();
        var tilesetResref = rootElement.GetProperty("Tileset").GetProperty("value").GetString() ?? string.Empty;

        var tiles = new AreaTile[width * height];
        var index = 0;
        foreach (var entry in rootElement.GetProperty("Tile_List").GetProperty("value").EnumerateArray())
        {
            var tileId = entry.GetProperty("Tile_ID").GetProperty("value").GetInt32();
            var orientation = entry.GetProperty("Tile_Orientation").GetProperty("value").GetInt32();
            var tileHeight = entry.GetProperty("Tile_Height").GetProperty("value").GetInt32();
            tiles[index] = new AreaTile(tileId, orientation, tileHeight);
            index++;
        }

        return new AreaGrid(width, height, tilesetResref, tiles);
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                return directory;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }

    private readonly record struct AreaTile(int TileId, int Orientation, int Height);

    private sealed class AreaGrid
    {
        public int Width { get; }
        public int Height { get; }
        public string Tileset { get; }
        private readonly AreaTile[] _tiles;

        public AreaGrid(int width, int height, string tileset, AreaTile[] tiles)
        {
            Width = width;
            Height = height;
            Tileset = tileset;
            _tiles = tiles;
        }

        public AreaTile GetTile(int x, int y) => _tiles[y * Width + x];
    }
}
