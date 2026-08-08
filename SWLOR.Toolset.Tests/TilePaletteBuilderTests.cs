using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Tilesets;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Hermetic coverage for <see cref="TilePaletteBuilder"/>: category shape, group labelling
    /// through the TLK, the skip-and-report path for a group that cannot be placed, and the
    /// row-major ordering the placement code depends on. Everything here uses a hand-built
    /// <see cref="TilesetDefinition"/> so it needs no NWN install and no haks checkout.
    /// </summary>
    public class TilePaletteBuilderTests
    {
        private static TileDefinition Tile(string model, string terrain = "Floor") => new()
        {
            Model = model,
            TopLeft = terrain,
            TopRight = terrain,
            BottomLeft = terrain,
            BottomRight = terrain
        };

        private static TilesetDefinition Tileset(
            IEnumerable<TileDefinition>? tiles = null,
            IEnumerable<TileGroupDefinition>? groups = null,
            string name = "TST01") => new()
        {
            Name = name,
            Tiles = (tiles ?? Enumerable.Empty<TileDefinition>()).ToArray(),
            Groups = (groups ?? Enumerable.Empty<TileGroupDefinition>()).ToArray()
        };

        private static TilesetDefinition FourTiles(params TileGroupDefinition[] groups) =>
            Tileset(
                new[] { Tile("tst01_a01_01"), Tile("tst01_a02_01"), Tile("tst01_a03_01"), Tile("tst01_a04_01") },
                groups);

        private static TilePaletteCategory CategoryNamed(TilePalette palette, string name) =>
            palette.Categories.Single(category => category.Name == name);

        /// <summary>
        /// Every named arrangement the tileset declares, whichever heading it was filed under.
        /// </summary>
        /// <remarks>
        /// Single-row arrangements are filed as Features and the rest as Groups, matching Aurora. Both
        /// come from the same [GROUPn] blocks, so the tests below - which are about labelling and
        /// validation, not about presentation - ask for both rather than naming a heading.
        /// </remarks>
        private static IReadOnlyList<TilePaletteEntry> NamedArrangements(TilePalette palette) =>
            palette.Categories
                .Where(category => category.Name is TilePaletteBuilder.FeaturesCategoryName
                    or TilePaletteBuilder.GroupsCategoryName)
                .SelectMany(category => category.Entries)
                .ToList();

        [Test]
        public void A_Tileset_With_Groups_And_Tiles_Yields_Its_Arrangements_Then_All_Tiles()
        {
            // 1x2, so it is a feature rather than a group - see TilePaletteBuilder.IsFeature.
            var singleRow = TilePaletteBuilder.Build(
                FourTiles(new TileGroupDefinition("Hut", 1, 2, null, new[] { 0, 1 })));

            singleRow.IsEmpty.Should().BeFalse();
            singleRow.Categories.Select(category => category.Name)
                .Should().Equal(new[] { "Features", "All tiles" },
                    "a single-row arrangement is a feature, and named arrangements lead the palette");

            var block = TilePaletteBuilder.Build(
                FourTiles(new TileGroupDefinition("Hall", 2, 2, null, new[] { 0, 1, 2, 3 })));

            block.Categories.Select(category => category.Name)
                .Should().Equal(new[] { "Groups", "All tiles" },
                    "an arrangement spanning rows is a group");
        }

        /// <summary>
        /// The .set's own name wins over its strref, which is the opposite of the usual preference for a
        /// localized label. See <see cref="TilePaletteBuilder"/> for the measurement behind it: custom
        /// tilesets carry strrefs copied in from another tileset, so following them renamed six distinct
        /// groups of sw_t_modint2 to "Bath", "Barbarians", "Bard", "Bath", "Barbarians", "Bard".
        /// </summary>
        [Test]
        public void A_Group_Label_Prefers_Its_Own_Name_Over_A_Resolved_StrRef()
        {
            var palette = TilePaletteBuilder.Build(
                FourTiles(new TileGroupDefinition("AverageFrontDoor", 1, 1, 1, new[] { 0 })),
                strRef => strRef == 1 ? "Barbarians" : null);

            NamedArrangements(palette).Single().Label.Should().Be("AverageFrontDoor");
        }

        /// <summary>
        /// A stale strref is not merely ignored - it must not be able to collapse distinct groups onto one
        /// label, which is the failure a builder actually saw.
        /// </summary>
        [Test]
        public void Groups_Sharing_A_Stale_StrRef_Keep_Their_Distinct_Names()
        {
            var groups = new[]
            {
                new TileGroupDefinition("AverageTwoWide", 1, 1, 63552, new[] { 0 }),
                new TileGroupDefinition("PoorTwoWide", 1, 1, 63552, new[] { 1 }),
                new TileGroupDefinition("RichTwoWide", 1, 1, 63552, new[] { 2 })
            };

            var palette = TilePaletteBuilder.Build(FourTiles(groups), _ => "Bath");

            NamedArrangements(palette).Select(entry => entry.Label)
                .Should().Equal("AverageTwoWide", "PoorTwoWide", "RichTwoWide");
        }

        /// <summary>
        /// The .set files repeat group names - tbx78 has three "room2x1" - and a palette a builder picks
        /// from cannot show three identical entries.
        /// </summary>
        [Test]
        public void Groups_Sharing_A_Name_Are_Numbered()
        {
            var groups = new[]
            {
                new TileGroupDefinition("room2x1", 1, 1, null, new[] { 0 }),
                new TileGroupDefinition("room2x1", 1, 1, null, new[] { 1 }),
                new TileGroupDefinition("ROOM2X1", 1, 1, null, new[] { 2 })
            };

            var palette = TilePaletteBuilder.Build(FourTiles(groups));

            // The counter is shared case-insensitively, but each label keeps the casing its author used -
            // renaming someone's "ROOM2X1" to lower case would be a second, unasked-for change.
            NamedArrangements(palette).Select(entry => entry.Label)
                .Should().Equal("room2x1", "room2x1 (2)", "ROOM2X1 (3)");
        }

        /// <summary>
        /// The strref is still the fallback for a group with no name of its own, and a TLK that throws or
        /// answers blank must not take the label down with it.
        /// </summary>
        [Test]
        public void A_Nameless_Group_Falls_Back_To_Its_StrRef()
        {
            var groups = new[]
            {
                new TileGroupDefinition("", 1, 1, 63655, new[] { 0 }),
                new TileGroupDefinition("", 1, 1, 63656, new[] { 1 }),
                new TileGroupDefinition("", 1, 1, 63657, new[] { 2 })
            };

            var palette = TilePaletteBuilder.Build(
                FourTiles(groups),
                strRef => strRef switch
                {
                    63655 => "Ruined Building",
                    63656 => "   ",
                    _ => throw new InvalidOperationException("no tlk")
                });

            NamedArrangements(palette).Select(entry => entry.Label)
                .Should().Equal("Ruined Building", "Group 1", "Group 2");
        }

        [Test]
        public void A_Nameless_Group_Still_Gets_A_Label()
        {
            var palette = TilePaletteBuilder.Build(
                FourTiles(
                    new TileGroupDefinition("First", 1, 1, null, new[] { 0 }),
                    new TileGroupDefinition("", 1, 1, null, new[] { 1 })));

            NamedArrangements(palette).Last().Label.Should().Be("Group 1");
        }

        [Test]
        public void A_Groups_TileIds_Keep_Their_Row_Major_Order()
        {
            // tde01's "Stairs - Down, Lava (2x2)" lists its tiles as 76, 77, 74, 75 - the bottom row
            // before the top one. The .set's Tile0..Tile{n-1} keys are already the row-major order the
            // placement code walks, so the builder must hand them over untouched rather than sorting.
            var palette = TilePaletteBuilder.Build(
                FourTiles(new TileGroupDefinition("Stairs", 2, 2, null, new[] { 3, 2, 1, 0 })));

            var entry = NamedArrangements(palette).Single();
            entry.TileIds.Should().Equal(3, 2, 1, 0);
            entry.Rows.Should().Be(2);
            entry.Columns.Should().Be(2);
        }

        [Test]
        public void A_Group_With_No_Tiles_Is_Skipped_And_Reported()
        {
            var problems = new List<string>();

            var palette = TilePaletteBuilder.Build(
                FourTiles(new TileGroupDefinition("Hollow", 0, 0, null, Array.Empty<int>())),
                reportProblem: problems.Add);

            palette.Categories.Select(category => category.Name).Should().Equal("All tiles");
            problems.Should().ContainSingle().Which.Should().Contain("Hollow").And.Contain("no tiles");
        }

        [Test]
        public void A_Group_Pointing_Outside_The_Tile_List_Is_Skipped_And_Reported()
        {
            var problems = new List<string>();

            var palette = TilePaletteBuilder.Build(
                FourTiles(
                    new TileGroupDefinition("Good", 1, 1, null, new[] { 2 }),
                    new TileGroupDefinition("PastTheEnd", 1, 2, null, new[] { 1, 4 }),
                    new TileGroupDefinition("Negative", 1, 2, null, new[] { 1, -7 })),
                reportProblem: problems.Add);

            NamedArrangements(palette).Select(entry => entry.Label).Should().Equal("Good");
            problems.Should().HaveCount(2);
            problems[0].Should().Contain("PastTheEnd").And.Contain("4").And.Contain("skipped");
            problems[1].Should().Contain("Negative").And.Contain("-7");
        }

        [Test]
        public void A_Group_Whose_Rectangle_Does_Not_Match_Its_Slot_Count_Is_Skipped_And_Reported()
        {
            var problems = new List<string>();

            var palette = TilePaletteBuilder.Build(
                FourTiles(new TileGroupDefinition("Lopsided", 2, 2, null, new[] { 0, 1, 2 })),
                reportProblem: problems.Add);

            palette.Categories.Select(category => category.Name).Should().Equal("All tiles");
            problems.Should().ContainSingle().Which.Should().Contain("Lopsided").And.Contain("2x2");
        }

        /// <summary>
        /// -1 is how the corpus declares a non-rectangular group (a hole in the bounding rectangle),
        /// not corruption, so it survives into TileIds where the placement code can honor it.
        /// </summary>
        [Test]
        public void A_Group_Hole_Survives_As_A_Negative_One_Slot()
        {
            var problems = new List<string>();

            var palette = TilePaletteBuilder.Build(
                FourTiles(new TileGroupDefinition("Docked", 2, 2, null, new[] { 0, 1, 2, -1 })),
                reportProblem: problems.Add);

            var entry = NamedArrangements(palette).Single();
            entry.TileIds.Should().Equal(0, 1, 2, -1);
            entry.PreviewModelResRef.Should().Be("tst01_a01_01");
            problems.Should().BeEmpty();
        }

        [Test]
        public void A_Group_That_Is_Nothing_But_Holes_Is_Skipped_And_Reported()
        {
            var problems = new List<string>();

            var palette = TilePaletteBuilder.Build(
                FourTiles(new TileGroupDefinition("Phantom", 1, 2, null, new[] { -1, -1 })),
                reportProblem: problems.Add);

            palette.Categories.Select(category => category.Name).Should().Equal("All tiles");
            problems.Should().ContainSingle().Which.Should().Contain("Phantom").And.Contain("empty slots");
        }

        [Test]
        public void A_Groups_Preview_Model_Comes_From_Its_First_Real_Tile()
        {
            var palette = TilePaletteBuilder.Build(
                FourTiles(new TileGroupDefinition("Leading Hole", 1, 3, null, new[] { -1, 2, 3 })));

            NamedArrangements(palette).Single()
                .PreviewModelResRef.Should().Be("tst01_a03_01");
        }

        [Test]
        public void All_Tiles_Is_In_Tile_Id_Order_With_One_Id_Per_Entry()
        {
            var palette = TilePaletteBuilder.Build(FourTiles());

            var entries = CategoryNamed(palette, "All tiles").Entries;

            entries.Should().HaveCount(4);
            entries.SelectMany(entry => entry.TileIds).Should().Equal(new[] { 0, 1, 2, 3 },
                "the tile's index in [TILES] is the Tile_ID an area's Tile_List stores");
            entries.Should().OnlyContain(entry => entry.Rows == 1 && entry.Columns == 1);
            entries.Select(entry => entry.Label)
                .Should().Equal("tst01_a01_01", "tst01_a02_01", "tst01_a03_01", "tst01_a04_01");
            entries.Select(entry => entry.PreviewModelResRef)
                .Should().Equal("tst01_a01_01", "tst01_a02_01", "tst01_a03_01", "tst01_a04_01");
        }

        [Test]
        public void A_Tile_With_No_Model_Falls_Back_To_Its_Id_And_Has_No_Preview()
        {
            var palette = TilePaletteBuilder.Build(
                Tileset(new[] { Tile("tst01_a01_01"), Tile("") }));

            var entries = CategoryNamed(palette, "All tiles").Entries;

            entries[1].Label.Should().Be("Tile 1");
            entries[1].PreviewModelResRef.Should().BeEmpty();
        }

        [Test]
        public void A_Tileset_With_No_Groups_Omits_The_Groups_Category_Entirely()
        {
            var palette = TilePaletteBuilder.Build(FourTiles());

            palette.Categories.Select(category => category.Name).Should().Equal(new[] { "All tiles" },
                "a header with nothing under it is worse than no header");
        }

        [Test]
        public void A_Tileset_With_No_Tiles_Has_Neither_Category()
        {
            // With no tiles there is nothing a group could reference either, so this is Empty rather
            // than a Groups-only palette.
            var palette = TilePaletteBuilder.Build(
                Tileset(groups: new[] { new TileGroupDefinition("Hut", 1, 1, null, new[] { 0 }) }));

            palette.IsEmpty.Should().BeTrue();
            palette.Categories.Should().BeEmpty();
        }

        [Test]
        public void No_Tileset_Yields_The_Empty_Palette_Rather_Than_Throwing()
        {
            var palette = TilePaletteBuilder.Build(null);

            palette.Should().BeSameAs(TilePalette.Empty);
            palette.IsEmpty.Should().BeTrue();
            palette.Categories.Should().BeEmpty();
        }

        [Test]
        public void Building_Without_A_TlkOrProblemSink_Is_Safe()
        {
            var act = () => TilePaletteBuilder.Build(
                FourTiles(
                    new TileGroupDefinition("Ok", 1, 1, 63655, new[] { 0 }),
                    new TileGroupDefinition("Broken", 1, 1, null, new[] { 99 })));

            act.Should().NotThrow();
            NamedArrangements(act()).Single().Label.Should().Be("Ok");
        }
    }
}
