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
                        Radoub.Formats.Tlk.TlkReader.Read(baseTlkPath));

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

        /// <summary>
        /// Measured floors, rounded down: shp02 44 groups / 579 tiles, ttd01 53 / 388.
        /// </summary>
        [Test]
        [TestCase("shp02", 40, 570)]
        [TestCase("ttd01", 50, 380)]
        public void A_Real_Tileset_Yields_Both_Categories(string resref, int minimumGroups, int minimumTiles)
        {
            var (palette, _) = Data.Build(resref);

            palette.IsEmpty.Should().BeFalse();
            palette.Categories.Select(category => category.Name).Should().Equal("Groups", "All tiles");

            var groups = palette.Categories[0].Entries;
            var tiles = palette.Categories[1].Entries;

            groups.Count.Should().BeGreaterThanOrEqualTo(minimumGroups);
            tiles.Count.Should().BeGreaterThanOrEqualTo(minimumTiles);

            groups.Should().OnlyContain(entry => entry.Label.Length > 0);
            groups.Should().OnlyContain(entry => entry.TileIds.Count == entry.Rows * entry.Columns);
            tiles.Should().OnlyContain(entry => entry.Rows == 1 && entry.Columns == 1);
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
        /// ttd01 carries strrefs on 39 of its 53 groups, so with a TLK supplied its labels must read
        /// as localized names rather than as the .set's internal identifiers ("Ruin01_2x2").
        /// </summary>
        [Test]
        public void Group_Labels_Use_The_Tlk_When_The_Tileset_Supplies_StrRefs()
        {
            var (localized, _) = Data.Build("ttd01");
            Data.Tilesets.TryGetTileset("ttd01", out var tileset).Should().BeTrue();
            var raw = TilePaletteBuilder.Build(tileset);

            var localizedLabels = localized.Categories[0].Entries.Select(entry => entry.Label).ToList();
            var rawLabels = raw.Categories[0].Entries.Select(entry => entry.Label).ToList();

            localizedLabels.Should().HaveSameCount(rawLabels);
            localizedLabels.Zip(rawLabels).Count(pair => pair.First != pair.Second)
                .Should().BeGreaterThanOrEqualTo(30,
                    because: "39 of ttd01's 53 groups declare a strref that the base dialog.tlk resolves");
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
                    palette.Categories[0].Entries.Count.Should().Be(
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
