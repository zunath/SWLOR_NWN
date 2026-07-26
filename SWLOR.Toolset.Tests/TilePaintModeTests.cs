using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.GameData.Tilesets;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// What each tile paint mode offers, and that a real tileset can actually serve both.
    /// </summary>
    /// <remarks>
    /// The two modes answer different questions: Auto picks a terrain and lets the tileset choose the
    /// tile - which is how Aurora works and why an area laid in it reads as continuous ground - while
    /// Manual picks the tile itself.
    /// </remarks>
    public class TilePaintModeTests
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
                    {
                        return current.FullName;
                    }

                    current = current.Parent;
                }

                throw new DirectoryNotFoundException("Could not locate the repository root from the test context.");
            }
        }

        private static TilePalette BuildPalette(params string[] categoryNames)
        {
            var entry = new TilePaletteEntry("x", new[] { 0 }, 1, 1, string.Empty);
            var categories = categoryNames
                .Select(name => new TilePaletteCategory(name, new[] { entry }))
                .ToList();

            // TilePalette's constructor is internal to the Domain assembly, so shape a real one instead.
            return TilePaletteFromCategories(categories);
        }

        private static TilePalette TilePaletteFromCategories(IReadOnlyList<TilePaletteCategory> categories)
        {
            var ctor = typeof(TilePalette).GetConstructor(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                binder: null, new[] { typeof(IReadOnlyList<TilePaletteCategory>) }, modifiers: null);

            return (TilePalette)ctor!.Invoke(new object[] { categories });
        }

        [Test]
        public void Auto_OffersTerrainAndGroupsButNotIndividualTiles()
        {
            var palette = BuildPalette(
                TilePaletteBuilder.TerrainCategoryName,
                TilePaletteBuilder.GroupsCategoryName,
                TilePaletteBuilder.AllTilesCategoryName);

            var offered = TilePaintModes.CategoriesFor(palette, TilePaintMode.Auto).Select(c => c.Name);

            offered.Should().Equal(TilePaletteBuilder.TerrainCategoryName, TilePaletteBuilder.GroupsCategoryName);
        }

        [Test]
        public void Manual_OffersIndividualTilesAndGroupsButNotTerrain()
        {
            var palette = BuildPalette(
                TilePaletteBuilder.TerrainCategoryName,
                TilePaletteBuilder.GroupsCategoryName,
                TilePaletteBuilder.AllTilesCategoryName);

            var offered = TilePaintModes.CategoriesFor(palette, TilePaintMode.Manual).Select(c => c.Name);

            offered.Should().Equal(TilePaletteBuilder.GroupsCategoryName, TilePaletteBuilder.AllTilesCategoryName);
        }

        /// <summary>
        /// Groups belong to both. A group is a fixed arrangement of several tiles, so it is neither
        /// solved nor a single tile, and hiding it in a mode would only mean switching out of that mode
        /// to reach it.
        /// </summary>
        [TestCase(TilePaintMode.Auto)]
        [TestCase(TilePaintMode.Manual)]
        public void Groups_AreOfferedInBothModes(TilePaintMode mode)
        {
            TilePaintModes.Offers(TilePaletteBuilder.GroupsCategoryName, mode).Should().BeTrue();
        }

        /// <summary>
        /// A real tileset serves both modes. Without this the filtering could be perfectly correct and
        /// still leave one mode showing an empty panel for every tileset the module actually uses.
        /// </summary>
        [Test]
        public void ARealTileset_HasSomethingToOfferInEitherMode()
        {
            var index = ResourceIndex.FromHakBuilderConfig(
                Path.Combine(RepoRoot, "Build", "hakbuilder.json"), Path.Combine(RepoRoot, "SWLOR_Haks"));
            var catalog = new TilesetCatalog(index);

            if (!catalog.TryGetTileset("zsf01", out var tileset))
            {
                Assert.Ignore("The sci-fi base tileset did not resolve; skipping.");
                return;
            }

            var palette = TilePaletteBuilder.Build(tileset, resolveStrRef: null, reportProblem: null);

            TilePaintModes.CategoriesFor(palette, TilePaintMode.Auto)
                .Should().NotBeEmpty("Auto needs terrain to paint");
            TilePaintModes.CategoriesFor(palette, TilePaintMode.Manual)
                .Should().NotBeEmpty("Manual needs individual tiles to stamp");
        }

        /// <summary>
        /// Auto's entries carry a terrain and Manual's do not - the difference the click acts on. An
        /// entry with no terrain is stamped literally; one with a terrain is handed to the solver.
        /// </summary>
        [Test]
        public void OnlyAutosEntriesCarryATerrain()
        {
            var index = ResourceIndex.FromHakBuilderConfig(
                Path.Combine(RepoRoot, "Build", "hakbuilder.json"), Path.Combine(RepoRoot, "SWLOR_Haks"));
            var catalog = new TilesetCatalog(index);

            if (!catalog.TryGetTileset("zsf01", out var tileset))
            {
                Assert.Ignore("The sci-fi base tileset did not resolve; skipping.");
                return;
            }

            var palette = TilePaletteBuilder.Build(tileset, resolveStrRef: null, reportProblem: null);

            var terrain = palette.Categories
                .FirstOrDefault(c => c.Name == TilePaletteBuilder.TerrainCategoryName);
            terrain.Should().NotBeNull();
            terrain!.Entries.Should().OnlyContain(entry => entry.Terrain != null);

            var allTiles = palette.Categories
                .FirstOrDefault(c => c.Name == TilePaletteBuilder.AllTilesCategoryName);
            allTiles.Should().NotBeNull();
            allTiles!.Entries.Should().OnlyContain(entry => entry.Terrain == null);
        }
    }
}
