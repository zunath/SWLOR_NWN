using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.GameData.Tilesets;
using SWLOR.Toolset.Domain.GameData.Tlk;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// <see cref="TilePaletteBuilder"/> against the real tilesets, resolved the way the toolset
    /// resolves them (layered resource index, hak precedence, TLK-supplied group names). The floors
    /// asserted here are measured values rounded down, and they exist to catch a regression that
    /// quietly empties a category rather than to pin exact counts.
    /// </summary>
    /// <remarks>
    /// Needs an NWN:EE install and a checked-out SWLOR_Haks submodule; without the haks the .set
    /// files are absent and these tests fail the same way every other corpus test in this suite does.
    /// </remarks>
    [TestFixture]
    [Category("Corpus")]
    public class TilePaletteCorpusTests
    {
        private static string RepoRoot
        {
            get
            {
                var current = new DirectoryInfo(AppContext.BaseDirectory);
                while (current != null)
                {
                    if (File.Exists(Path.Combine(current.FullName, "Build", "hakbuilder.json")) &&
                        Directory.Exists(Path.Combine(current.FullName, "SWLOR_Haks")))
                        return current.FullName;

                    current = current.Parent;
                }

                throw new DirectoryNotFoundException(
                    "Could not locate the repository root (Build/hakbuilder.json + SWLOR_Haks) from the test context.");
            }
        }

        private static readonly Lazy<Fixture> Shared =
            new(Fixture.Build, LazyThreadSafetyMode.ExecutionAndPublication);

        private static Fixture Data => Shared.Value;

        /// <summary>The layered resource stack plus TLK, built once - scanning it is several seconds.</summary>
        private sealed class Fixture
        {
            public required TilesetCatalog Tilesets { get; init; }
            public required TlkService Tlk { get; init; }

            public static Fixture Build()
            {
                var installPath = NwnInstallLocator.Locate(null)
                    ?? throw new DirectoryNotFoundException(
                        "No NWN:EE install was found; the base tilesets and dialog.tlk only exist there.");

                var baseLayer = KeyBifCatalog.Load(Path.Combine(installPath, "data"));
                var index = ResourceIndex.FromHakBuilderConfig(
                    Path.Combine(RepoRoot, "Build", "hakbuilder.json"),
                    Path.Combine(RepoRoot, "SWLOR_Haks"),
                    baseLayer);
                index.EnsureInitialized();

                // Tileset group strrefs in this corpus are base dialog.tlk refs (ttd01's are 8xxx and
                // 63xxx), so an empty custom TLK stands in when the haks submodule has no sw_tlk.
                var swTlkPath = Path.Combine(RepoRoot, "SWLOR_Haks", "sw_tlk", "sw_tlk.tlk.json");
                var baseTlkPath = Path.Combine(installPath, "lang", "en", "data", "dialog.tlk");
                var tlk = File.Exists(swTlkPath)
                    ? TlkService.Load(swTlkPath, baseTlkPath)
                    : new TlkService(
                        TlkJsonFile.Parse("{\"language\":0,\"entries\":[]}"),
                        SWLOR.NWN.Formats.Tlk.TlkReader.Read(baseTlkPath));

                return new Fixture { Tilesets = new TilesetCatalog(index), Tlk = tlk };
            }

            public (TilePalette Palette, List<string> Problems) Build(string resref)
            {
                Tilesets.TryGetTileset(resref, out var tileset)
                    .Should().BeTrue(because: $"'{resref}.set' must resolve through the resource index");

                var problems = new List<string>();
                return (TilePaletteBuilder.Build(tileset, Tlk.GetString, problems.Add), problems);
            }
        }

        /// <summary>The entries of one named category, or an empty list when it was not built.</summary>
        private static IReadOnlyList<TilePaletteEntry> CategoryOf(TilePalette palette, string name) =>
            palette.Categories.FirstOrDefault(category => category.Name == name)?.Entries
            ?? Array.Empty<TilePaletteEntry>();

        /// <summary>
        /// Every named arrangement the .set declares, whichever heading it was filed under.
        /// </summary>
        /// <remarks>
        /// The palette files single-row arrangements as Features and the rest as Groups, the way
        /// Aurora does. That is a decision about presentation - both come from the same [GROUPn]
        /// blocks - so the tests that ask "did every group survive the build" have to ask about both.
        /// </remarks>
        private static IReadOnlyList<TilePaletteEntry> NamedArrangementsOf(TilePalette palette) =>
            CategoryOf(palette, TilePaletteBuilder.FeaturesCategoryName)
                .Concat(CategoryOf(palette, TilePaletteBuilder.GroupsCategoryName))
                .ToList();

        /// <summary>
        /// Measured floors, rounded down: shp02 44 groups / 579 tiles, ttd01 53 / 388.
        /// </summary>
        [Test]
        [TestCase("shp02", 40, 570)]
        [TestCase("ttd01", 50, 380)]
        public void A_Real_Tileset_Yields_Its_Categories(string resref, int minimumGroups, int minimumTiles)
        {
            var (palette, _) = Data.Build(resref);

            palette.IsEmpty.Should().BeFalse();
            palette.Categories.Select(category => category.Name)
                .Should().Equal("Terrain", "Features", "Groups", "All tiles");

            var groups = NamedArrangementsOf(palette);
            var tiles = CategoryOf(palette, TilePaletteBuilder.AllTilesCategoryName);

            groups.Count.Should().BeGreaterThanOrEqualTo(minimumGroups);
            tiles.Count.Should().BeGreaterThanOrEqualTo(minimumTiles);

            groups.Should().OnlyContain(entry => entry.Label.Length > 0);
            groups.Should().OnlyContain(entry => entry.TileIds.Count == entry.Rows * entry.Columns);
            tiles.Should().OnlyContain(entry => entry.Rows == 1 && entry.Columns == 1);
        }

        /// <summary>
        /// The Terrain category is the brush half of this palette: terrain brushes (vertex paints),
        /// crosser brushes (edge paints) and the eraser file together there, matching the reference
        /// toolset's Terrain tree. Every entry must be one the painter can actually resolve - a
        /// declared terrain backed by a solid tile, or a declared crosser some tile carries. An
        /// entry the tileset cannot satisfy would arm a brush whose every click did nothing at all.
        /// </summary>
        [Test]
        [TestCase("shp02")]
        [TestCase("ttd01")]
        public void Terrain_Entries_Name_A_Terrain_Or_Crosser_The_Tileset_Declares(string resref)
        {
            var (palette, _) = Data.Build(resref);
            Data.Tilesets.TryGetTileset(resref, out var tileset).Should().BeTrue();

            var brushes = CategoryOf(palette, TilePaletteBuilder.TerrainCategoryName);
            brushes.Should().NotBeEmpty(because: $"'{resref}' declares {tileset.Terrains.Count} terrains");

            var declaredTerrains = tileset.Terrains.Select(terrain => terrain.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var declaredCrossers = tileset.Crossers.Select(crosser => crosser.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            brushes.Should().OnlyContain(entry => entry.Terrain == null || entry.Crosser == null,
                "an entry is one brush, never a terrain and a crosser at once");
            brushes.Should().OnlyContain(entry => entry.Terrain != null || entry.Crosser != null,
                "every entry in the brush category paints something");
            brushes.Where(entry => entry.Terrain != null).Should().NotBeEmpty();
            brushes.Should().OnlyContain(entry =>
                entry.Terrain == null || declaredTerrains.Contains(entry.Terrain!));
            brushes.Should().OnlyContain(entry =>
                entry.Crosser == null || entry.Crosser.Length == 0 || declaredCrossers.Contains(entry.Crosser!));
            brushes.Should().OnlyContain(entry => entry.Rows == 1 && entry.Columns == 1);
            brushes.Where(entry => entry.Terrain != null)
                .Should().OnlyContain(entry => entry.TileIds.Count == 1);
        }

        /// <summary>
        /// A terrain brush's representative tile is what its thumbnail renders, and it has to be the
        /// solid form of that terrain - four matching corners, no edge crossers - or the palette
        /// would advertise the brush with a picture of a wall.
        /// </summary>
        [Test]
        [TestCase("shp02")]
        [TestCase("ttd01")]
        public void A_Terrain_Entry_Points_At_A_Solid_Tile(string resref)
        {
            var (palette, _) = Data.Build(resref);
            Data.Tilesets.TryGetTileset(resref, out var tileset).Should().BeTrue();

            foreach (var entry in CategoryOf(palette, TilePaletteBuilder.TerrainCategoryName))
            {
                if (entry.Crosser != null)
                {
                    // A crosser brush's representative tile must actually carry the crosser it
                    // advertises (the eraser, painting "nothing", has no representative at all).
                    if (entry.Crosser.Length > 0)
                    {
                        var carrier = tileset.Tiles[entry.TileIds[0]];
                        new[] { carrier.Top, carrier.Right, carrier.Bottom, carrier.Left }
                            .Should().Contain(edge => string.Equals(
                                    edge, entry.Crosser, StringComparison.OrdinalIgnoreCase),
                                because: $"'{entry.Label}' is advertised by a tile carrying it");
                    }

                    continue;
                }

                var tile = tileset.Tiles[entry.TileIds[0]];

                tile.TopLeft.Should().BeEquivalentTo(entry.Terrain);
                tile.TopRight.Should().BeEquivalentTo(entry.Terrain);
                tile.BottomLeft.Should().BeEquivalentTo(entry.Terrain);
                tile.BottomRight.Should().BeEquivalentTo(entry.Terrain);

                new[] { tile.Top, tile.Right, tile.Bottom, tile.Left }
                    .Should().OnlyContain(crosser => crosser.Length == 0,
                        because: $"'{entry.Label}' is meant to fill a cell, not carry a feature across it");
            }
        }

        /// <summary>
        /// Every tile in the corpus declares a model (measured: 579/579 for shp02, 388/388 for ttd01,
        /// 37,146/37,146 across all 70 .set files), so a missing preview means either a real tileset
        /// regression or a parser one - both worth failing over.
        /// </summary>
        [Test]
        [TestCase("shp02")]
        [TestCase("ttd01")]
        public void Every_Palette_Entry_Can_Be_Previewed(string resref)
        {
            var (palette, _) = Data.Build(resref);

            var previewless = palette.Categories
                .SelectMany(category => category.Entries)
                // The eraser paints "nothing" and so has nothing to preview - the one entry with
                // no representative tile by design.
                .Where(entry => entry.Label != TilePaletteBuilder.EraserLabel)
                .Where(entry => entry.PreviewModelResRef.Length == 0)
                .Select(entry => entry.Label)
                .ToList();

            previewless.Should().BeEmpty(
                because: $"every {resref} tile declares a Model; without one no thumbnail can render. " +
                         $"Previewless: {string.Join(", ", previewless.Take(5))}");
        }

        /// <summary>
        /// Tile ids must stay usable as Tile_ID values, which is what makes "All tiles" paintable
        /// at all: the entry's single id is its index into the tileset's own tile list.
        /// </summary>
        [Test]
        public void All_Tiles_Ids_Are_Tile_List_Indices()
        {
            Data.Tilesets.TryGetTileset("ttd01", out var tileset).Should().BeTrue();
            var palette = TilePaletteBuilder.Build(tileset, Data.Tlk.GetString);

            var tiles = palette.Categories.Single(category => category.Name == "All tiles").Entries;

            tiles.Count.Should().Be(tileset.Tiles.Count);
            for (var tileId = 0; tileId < tiles.Count; tileId++)
            {
                tiles[tileId].TileIds.Should().Equal(tileId);
                tiles[tileId].PreviewModelResRef.Should().Be(tileset.Tiles[tileId].Model);
            }
        }

        /// <summary>
        /// A group's label is its own name from the .set, so supplying a TLK cannot change it.
        /// </summary>
        /// <remarks>
        /// Asserted on tmi (sw_t_modint2) because tmi is the tileset that broke: its first six groups are
        /// AverageTwoWide/AverageFrontDoor/AverageElevator/PoorTwoWide/PoorFrontDoor/PoorElevator and its
        /// strrefs are 63552/1/2/63552/1/2 - pointers copied in from another tileset. Following them
        /// renamed all six to "Bath"/"Barbarians"/"Bard" twice over.
        /// </remarks>
        [Test]
        public void Group_Labels_Come_From_The_Set_And_A_Tlk_Cannot_Change_Them()
        {
            var (withTlk, _) = Data.Build("tmi");
            Data.Tilesets.TryGetTileset("tmi", out var tileset).Should().BeTrue();
            var withoutTlk = TilePaletteBuilder.Build(tileset);

            var a = NamedArrangementsOf(withTlk).Select(entry => entry.Label).ToList();
            var b = NamedArrangementsOf(withoutTlk).Select(entry => entry.Label).ToList();

            a.Should().Equal(b, "the .set's own names decide the label, with or without a TLK");
            a.Should().Contain("AverageFrontDoor").And.NotContain("Barbarians");
        }

        /// <summary>
        /// No two groups of a tileset may share a label. This is the property the failure actually
        /// violated - six distinct groups all reading "Bath" identify none of them - and it holds across
        /// the whole corpus, so a future change to the label rule cannot quietly reintroduce it.
        /// </summary>
        [Test]
        public void No_Tileset_Has_Two_Groups_With_The_Same_Label()
        {
            var offenders = new List<string>();

            foreach (var name in Data.Tilesets.GetTilesetNames())
            {
                if (!Data.Tilesets.TryGetTileset(name, out var tileset))
                    continue;

                var groups = NamedArrangementsOf(TilePaletteBuilder.Build(tileset));

                if (groups.Count == 0)
                    continue;

                var duplicated = groups
                    .GroupBy(entry => entry.Label, StringComparer.OrdinalIgnoreCase)
                    .Where(group => group.Count() > 1)
                    .Select(group => $"{name}: '{group.Key}' x{group.Count()}")
                    .ToList();

                offenders.AddRange(duplicated);
            }

            offenders.Should().BeEmpty();
        }

        /// <summary>
        /// The whole corpus in one pass: nothing may throw, and the only groups the builder is allowed
        /// to reject are genuinely unplaceable ones. -1 slots (90 groups corpus-wide) are holes in a
        /// non-rectangular group, not corruption, so they must NOT cost those groups their palette entry.
        /// </summary>
        [Test]
        public void Every_Tileset_Builds_A_Palette_Without_Losing_Its_Groups()
        {
            var names = Data.Tilesets.GetTilesetNames();
            names.Count.Should().BeGreaterThan(50, because: "the sw_t_* tileset corpus should be present");

            var problems = new List<string>();
            var built = 0;

            foreach (var resref in names)
            {
                if (!Data.Tilesets.TryGetTileset(resref, out var tileset))
                    continue;

                var perTileset = new List<string>();
                var palette = TilePaletteBuilder.Build(tileset, Data.Tlk.GetString, perTileset.Add);

                palette.IsEmpty.Should().BeFalse(because: $"'{resref}' declares {tileset.TileCount} tiles");
                if (tileset.Groups.Count > 0)
                {
                    NamedArrangementsOf(palette).Count.Should().Be(
                        tileset.Groups.Count - perTileset.Count,
                        because: $"every '{resref}' group is either emitted or explained");
                }

                problems.AddRange(perTileset.Select(problem => $"{resref}: {problem}"));
                built++;
            }

            built.Should().BeGreaterThan(50);
            problems.Should().BeEmpty(
                because: "no tileset in the corpus contains a group the builder should reject; a -1 slot " +
                         $"is a hole, not an out-of-range index. Rejected:\n{string.Join("\n", problems.Take(10))}");
        }
    }
}
