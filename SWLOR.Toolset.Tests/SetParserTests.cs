using System.Collections.Concurrent;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Tilesets;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Coverage for <see cref="SetFileParser"/>: every .set tileset file under the SWLOR_Haks
    /// corpus must parse without throwing, spot-checked values from tde01.set must come back
    /// exactly as they appear in the file, and the parser must tolerate the messier corners of
    /// hand-edited INI (unknown sections, comments, blank keys, mixed line endings, duplicate
    /// keys, mismatched case).
    /// </summary>
    public class SetParserTests
    {
        private static string HaksDirectory
        {
            get
            {
                var current = new DirectoryInfo(AppContext.BaseDirectory);
                while (current != null)
                {
                    var candidate = Path.Combine(current.FullName, "SWLOR_Haks");
                    if (Directory.Exists(candidate))
                        return candidate;

                    current = current.Parent;
                }

                throw new DirectoryNotFoundException(
                    "Could not locate the repository SWLOR_Haks directory from the test context.");
            }
        }

        private static IEnumerable<string> EnumerateSetFiles()
        {
            var haksDirectory = HaksDirectory;
            foreach (var tilesetDirectory in Directory.EnumerateDirectories(haksDirectory, "sw_t_*"))
            foreach (var file in Directory.EnumerateFiles(tilesetDirectory, "*.set"))
                yield return file;
        }

        private static string Tde01Path =>
            Path.Combine(HaksDirectory, "sw_t_dungeon", "tde01.set");

        [Test]
        public void EveryTilesetSetFile_ParsesWithoutExceptionAndHasTiles()
        {
            var files = EnumerateSetFiles().ToList();
            files.Count.Should().BeGreaterThan(50, "the sw_t_* tileset corpus should be present");

            var failures = new ConcurrentBag<string>();
            var processed = 0;

            Parallel.ForEach(files, file =>
            {
                try
                {
                    var tileset = SetFileParser.ParseFile(file);
                    if (tileset.TileCount <= 0)
                        failures.Add($"{file}: parsed but TileCount was {tileset.TileCount}");
                }
                catch (Exception ex)
                {
                    failures.Add($"{file}: {ex.GetType().Name}: {ex.Message}");
                }

                Interlocked.Increment(ref processed);
            });

            processed.Should().Be(files.Count);
            failures.Should().BeEmpty(
                $"all {files.Count} .set files under sw_t_* should parse cleanly with at least one " +
                $"tile. {failures.Count} failed. First failures:\n{string.Join("\n", failures.Take(10))}");
        }

        [Test]
        public void Tde01_GeneralSection_MatchesFileContents()
        {
            var tileset = SetFileParser.ParseFile(Tde01Path);

            tileset.Name.Should().Be("TDE01");
            tileset.UnlocalizedName.Should().Be("Dungeon*");
            tileset.Interior.Should().BeTrue();
            tileset.HasHeightTransition.Should().BeTrue();
            tileset.Transition.Should().Be(3);
            tileset.Border.Should().Be("Wall");
            tileset.Floor.Should().Be("Floor");
        }

        [Test]
        public void Tde01_Terrains_MatchFileContents()
        {
            var tileset = SetFileParser.ParseFile(Tde01Path);

            tileset.Terrains.Should().HaveCount(7);
            tileset.Terrains[0].Name.Should().Be("Wall");
            tileset.Terrains[0].StrRef.Should().Be(63301);

            // CROSSER5 has no StrRef, only an UnlocalizedName - exercises the optional-StrRef path.
            tileset.Crossers.Should().HaveCount(6);
            tileset.Crossers[5].Name.Should().Be("MazeMosaic");
            tileset.Crossers[5].StrRef.Should().BeNull();
            tileset.Crossers[5].UnlocalizedName.Should().Be("Maze - Mosaic");
        }

        [Test]
        public void Tde01_Tile6_HasExpectedModelCornersAndDoor()
        {
            var tileset = SetFileParser.ParseFile(Tde01Path);

            tileset.TileCount.Should().Be(1092);

            var tile6 = tileset.Tiles[6];
            tile6.Model.Should().Be("tde01_a13_01");
            tile6.TopLeft.Should().Be("Floor");
            tile6.TopRight.Should().Be("Wall");
            tile6.BottomLeft.Should().Be("Floor");
            tile6.BottomRight.Should().Be("Wall");
            tile6.Right.Should().Be("Doorway");

            // [TILE6] declares Doors=1 and is followed by exactly one [TILE6DOOR0] block.
            tile6.DoorsRaw.Should().Be(1);
            tile6.Doors.Should().HaveCount(1);
            tile6.Doors[0].X.Should().Be(5.00);
            tile6.Doors[0].Orientation.Should().Be(-90.0);
        }

        [Test]
        public void Tde01_Groups_MatchFileContents()
        {
            var tileset = SetFileParser.ParseFile(Tde01Path);

            tileset.Groups.Should().HaveCount(60);
            tileset.Groups[0].Name.Should().Be("Treasure 1");
            tileset.Groups[0].Rows.Should().Be(1);
            tileset.Groups[0].Columns.Should().Be(1);
            tileset.Groups[0].TileIndices.Should().Equal(108);

            var stairsDownLava = tileset.Groups[7];
            stairsDownLava.Name.Should().Be("Stairs - Down, Lava (2x2)");
            stairsDownLava.Rows.Should().Be(2);
            stairsDownLava.Columns.Should().Be(2);
            stairsDownLava.TileIndices.Should().Equal(76, 77, 74, 75);
        }

        [Test]
        public void Parse_ToleratesCommentsBlankKeysUnknownSectionsAndDuplicateKeys()
        {
            // Deliberately messy: a leading comment, mixed CRLF/LF line endings, an unknown
            // section that must not crash the parser, a lowercase section name ("[grass]") to
            // exercise case-insensitive section lookup, a duplicate "Name" key within [GENERAL]
            // (last-wins is the documented policy), and blank edge-crosser values on the tile.
            const string content =
                "; a leading comment\r\n" +
                "[GENERAL]\n" +
                "Name=FIRST\r\n" +
                "Name=SECOND\n" +
                "Interior=1\r\n" +
                "\r\n" +
                "[SOME UNKNOWN SECTION]\n" +
                "SomeUnknownKey=SomeUnknownValue\r\n" +
                "\n" +
                "[grass]\r\n" +
                "Grass=0\n" +
                "\r\n" +
                "[TERRAIN TYPES]\r\n" +
                "Count=0\n" +
                "\n" +
                "[CROSSER TYPES]\r\n" +
                "Count=0\n" +
                "\n" +
                "[PRIMARY RULES]\r\n" +
                "Count=0\n" +
                "\n" +
                "[SECONDARY RULES]\r\n" +
                "Count=0\n" +
                "\n" +
                "[TILES]\r\n" +
                "Count=1\n" +
                "\n" +
                "[TILE0]\r\n" +
                "Model=test_a01_01\n" +
                "WalkMesh=msb01\r\n" +
                "TopLeft=Wall\n" +
                "TopLeftHeight=0\r\n" +
                "TopRight=Wall\n" +
                "TopRightHeight=0\r\n" +
                "BottomLeft=Wall\n" +
                "BottomLeftHeight=0\r\n" +
                "BottomRight=Wall\n" +
                "Top=\r\n" +
                "Right=\n" +
                "Bottom=\r\n" +
                "Left=\n" +
                "MainLight1=1\r\n" +
                "MainLight2=1\n" +
                "SourceLight1=1\r\n" +
                "SourceLight2=1\n" +
                "AnimLoop1=1\r\n" +
                "AnimLoop2=1\n" +
                "AnimLoop3=1\r\n" +
                "Doors=0\n" +
                "Sounds=0\r\n" +
                "PathNode=A\n" +
                "Orientation=0\r\n";

            var tileset = SetFileParser.Parse(content);

            tileset.Name.Should().Be("SECOND", "duplicate keys within a section are last-wins");
            tileset.Interior.Should().BeTrue();
            tileset.HasGrass.Should().BeFalse("the lowercase [grass] section must resolve case-insensitively");
            tileset.TileCount.Should().Be(1);

            var tile = tileset.Tiles[0];
            tile.Model.Should().Be("test_a01_01");
            tile.Top.Should().Be("", "a blank key should parse as an empty crosser, not null or a crash");
            tile.Doors.Should().BeEmpty();
        }
    }
}
