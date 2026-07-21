using System.IO;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.AreaGenerationService;
using SWLOR.Game.Server.Service.AreaGenerationService.Tileset;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

public class TilesetSetParserTests
{
    [Test]
    public void Tdt01_ParsesGeneralTerrainsCrossersAndGroups()
    {
        var root = FindRepositoryRoot();
        var contents = File.ReadAllText(Path.Combine(root.FullName, "SWLOR_Haks", "sw_t_minecave", "tdt01.set"));

        var model = TilesetSetParser.Parse("tdt01", contents);

        model.Tiles.Should().NotBeEmpty();
        model.Tiles.Should().HaveCount(159);

        model.Terrains.Should().Equal("Floor", "Wall", "Water");
        model.Crossers.Should().Equal("Corridor", "Doorway", "Bridge");

        model.FloorTerrain.Should().NotBeNullOrEmpty();
        model.DefaultTerrain.Should().NotBeNullOrEmpty();
        model.FloorTerrain.Should().Be("Floor");
        model.DefaultTerrain.Should().Be("Wall");
        model.BorderTerrain.Should().Be("Wall");
        model.IsInterior.Should().BeTrue();
        model.HasHeightTransition.Should().BeFalse();

        model.Groups.Should().HaveCount(28);

        // [GROUP6] Name=StairsDown_2x2, Rows=2, Columns=2, Tile0=73 Tile1=74 Tile2=71 Tile3=72
        var stairsDown = model.Groups[6];
        stairsDown.Name.Should().Be("StairsDown_2x2");
        stairsDown.Rows.Should().Be(2);
        stairsDown.Columns.Should().Be(2);
        stairsDown.TileIds.Should().Equal(73, 74, 71, 72);

        // Every tile referenced by a group (excluding -1 empty slots) should carry that group's index
        // (or an earlier group's index, if it was already claimed).
        foreach (var tileId in stairsDown.TileIds)
        {
            if (tileId < 0)
                continue;

            model.Tiles[tileId].GroupIndex.Should().BeGreaterThanOrEqualTo(0);
        }

        // [GROUP10] Platform03_2x2 has an explicit -1 (empty) slot in the raw file.
        var platform03 = model.Groups[10];
        platform03.Name.Should().Be("Platform03_2x2");
        platform03.TileIds.Should().Equal(99, -1, 97, 98);
    }

    [Test]
    public void Tdt01_SpotCheckTile7CornersEdgesAndDoors()
    {
        var root = FindRepositoryRoot();
        var contents = File.ReadAllText(Path.Combine(root.FullName, "SWLOR_Haks", "sw_t_minecave", "tdt01.set"));

        var model = TilesetSetParser.Parse("tdt01", contents);

        // [TILE7]: TopLeft=Wall, TopRight=Wall, BottomLeft=Floor, BottomRight=Wall,
        // Top=Doorway, Right=Doorway, Bottom/Left empty, Doors=2.
        var tile = model.Tiles[7];
        tile.Model.Should().Be("tdt01_a14_01");
        tile.Corners.Should().Equal("Wall", "Wall", "Wall", "Floor");
        tile.CornerHeights.Should().Equal(0, 0, 0, 0);
        tile.Edges.Should().Equal("Doorway", "Doorway", "", "");
        tile.Doors.Should().HaveCount(2);
    }

    [Test]
    public void Tde01_ParsesExpectedTileTerrainAndGroupCounts()
    {
        var root = FindRepositoryRoot();
        var contents = File.ReadAllText(Path.Combine(root.FullName, "SWLOR_Haks", "sw_t_dungeon", "tde01.set"));

        var model = TilesetSetParser.Parse("tde01", contents);

        model.Tiles.Should().HaveCount(1092);
        model.Terrains.Should().HaveCount(7);
        model.Terrains.Should().Equal("Wall", "Floor", "Lava", "Water", "Sewer", "Ice", "Pit");
        model.Crossers.Should().Equal("Bridge", "Corridor", "Fence", "Doorway", "Ramp", "MazeMosaic");
        model.Groups.Should().HaveCount(60);

        model.FloorTerrain.Should().Be("Floor");
        model.DefaultTerrain.Should().Be("Wall");
        model.HasHeightTransition.Should().BeTrue();
    }

    [Test]
    public void Tde01_SpotCheckTile500HeightOnBottomRightCornerOnly()
    {
        var root = FindRepositoryRoot();
        var contents = File.ReadAllText(Path.Combine(root.FullName, "SWLOR_Haks", "sw_t_dungeon", "tde01.set"));

        var model = TilesetSetParser.Parse("tde01", contents);

        // [TILE500]: all corners Floor, BottomRightHeight=1, all other heights 0.
        var tile = model.Tiles[500];
        tile.Model.Should().Be("zde01_w01_01");
        tile.Corners.Should().Equal("Floor", "Floor", "Floor", "Floor");
        tile.CornerHeights.Should().Equal(0, 0, 1, 0);
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
}
