using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.GameData.Tilesets;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// End-to-end coverage for the WP7.3 new-area wizard's write path: it must produce an area
    /// triplet that loads back as a real, solid-filled area and is registered in module.ifo. Runs
    /// against a THROWAWAY module fixture (the repo's real template files copied into a temp
    /// directory), so the repository module is never written to.
    /// </summary>
    public class NewAreaWriterTests
    {
        private string _moduleRoot = string.Empty;

        private static string RepoRoot
        {
            get
            {
                var c = new DirectoryInfo(AppContext.BaseDirectory);
                while (c != null)
                {
                    if (File.Exists(Path.Combine(c.FullName, "Build", "hakbuilder.json")) &&
                        Directory.Exists(Path.Combine(c.FullName, "SWLOR_Haks")))
                        return c.FullName;
                    c = c.Parent;
                }
                throw new DirectoryNotFoundException("repo root not found");
            }
        }

        [SetUp]
        public void CreateFixtureModule()
        {
            var source = CorpusLocator.ModuleDirectory;
            _moduleRoot = Path.Combine(Path.GetTempPath(), "swlor_wp73_" + Guid.NewGuid().ToString("N"));

            foreach (var folder in new[] { "are", "git", "gic", "ifo", "utc" })
                Directory.CreateDirectory(Path.Combine(_moduleRoot, folder));

            File.Copy(Path.Combine(source, "are", "area_template.are.json"),
                Path.Combine(_moduleRoot, "are", "area_template.are.json"));
            File.Copy(Path.Combine(source, "git", "area_template.git.json"),
                Path.Combine(_moduleRoot, "git", "area_template.git.json"));
            File.Copy(Path.Combine(source, "gic", "area_template.gic.json"),
                Path.Combine(_moduleRoot, "gic", "area_template.gic.json"));
            File.Copy(Path.Combine(source, "ifo", "module.ifo.json"),
                Path.Combine(_moduleRoot, "ifo", "module.ifo.json"));
        }

        [TearDown]
        public void RemoveFixtureModule()
        {
            try
            {
                if (Directory.Exists(_moduleRoot))
                    Directory.Delete(_moduleRoot, recursive: true);
            }
            catch
            {
                // A leftover temp directory must never fail the run.
            }
        }

        /// <summary>Resolves real tilesets, so the fill tile comes from genuine .set corner rules.</summary>
        private static NewAreaWriter.TilesetResolver? RealTilesets()
        {
            var installPath = NwnInstallLocator.Locate();
            if (installPath == null)
                return null;

            var index = ResourceIndex.FromHakBuilderConfig(
                Path.Combine(RepoRoot, "Build", "hakbuilder.json"),
                Path.Combine(RepoRoot, "SWLOR_Haks"),
                KeyBifCatalog.Load(Path.Combine(installPath, "data")));
            var catalog = new TilesetCatalog(index);
            return (string resRef, out TilesetDefinition tileset) => catalog.TryGetTileset(resRef, out tileset);
        }

        [Test]
        public void TryCreate_WritesALoadableSolidArea_AndRegistersIt()
        {
            var resolver = RealTilesets();
            if (resolver == null)
            {
                Assert.Ignore("No local NWN:EE install; the fill tile needs real tileset data.");
                return;
            }

            var workspace = new ModuleWorkspace(_moduleRoot);

            NewAreaWriter.TryCreate(workspace, resolver, "wp73_new", "WP73 New", "tms01", 3, 2, null, out var error)
                .Should().BeTrue(error);

            // The triplet exists and loads back as a real area.
            var (are, _, _) = workspace.LoadArea("wp73_new");
            are.Width.Should().Be(3);
            are.Height.Should().Be(2);
            are.Tileset.Should().Be("tms01");
            are.Tag.Should().Be("wp73_new");
            are.Name.Text.Should().Be("WP73 New");
            are.Tiles.Should().HaveCount(6, "the grid is filled to width*height");

            // Every cell carries the same solid fill tile, and that tile really is uniform terrain -
            // i.e. the new area is a plain walkable floor, not an arbitrary legal tile.
            resolver("tms01", out var tileset).Should().BeTrue();
            var first = AreaTiles.At(are, 0, 0)!.Value;
            var corners = new[]
            {
                TileCorner.NorthWest, TileCorner.NorthEast, TileCorner.SouthWest, TileCorner.SouthEast
            }.Select(c => TileAdjacency.WorldCornerTerrain(tileset.Tiles[first.TileId], first.Orientation, c)).ToList();
            corners.Distinct(StringComparer.OrdinalIgnoreCase).Should().ContainSingle("the fill tile is a single uniform terrain");

            for (var row = 0; row < 2; row++)
            for (var col = 0; col < 3; col++)
                AreaTiles.At(are, col, row).Should().Be(first, "every cell gets the same fill");

            // And the module now lists it.
            IfoDocument.Load(Path.Combine(_moduleRoot, "ifo", "module.ifo.json"))
                .AreaResRefs.Should().Contain("wp73_new");
        }

        [Test]
        public void TryCreate_RejectsDuplicateResRef_WithoutTouchingTheExistingArea()
        {
            var resolver = RealTilesets();
            if (resolver == null)
            {
                Assert.Ignore("No local NWN:EE install; the fill tile needs real tileset data.");
                return;
            }

            var workspace = new ModuleWorkspace(_moduleRoot);
            NewAreaWriter.TryCreate(workspace, resolver, "wp73_new", "First", "tms01", 2, 2, null, out _)
                .Should().BeTrue();
            var bytesAfterFirst = File.ReadAllBytes(workspace.GetResourcePath(ResourceType.Area, "wp73_new"));

            NewAreaWriter.TryCreate(workspace, resolver, "wp73_new", "Second", "tms01", 8, 8, null, out var error)
                .Should().BeFalse("the resref is taken");
            error.Should().Contain("already exists");

            File.ReadAllBytes(workspace.GetResourcePath(ResourceType.Area, "wp73_new"))
                .Should().Equal(bytesAfterFirst, "a rejected create must not modify the existing area");
        }

        [Test]
        public void TryCreate_NormalizesResRefCase()
        {
            var resolver = RealTilesets();
            if (resolver == null)
            {
                Assert.Ignore("No local NWN:EE install; the fill tile needs real tileset data.");
                return;
            }

            var workspace = new ModuleWorkspace(_moduleRoot);

            // NWN resrefs are case-insensitive and conventionally lowercase, so mixed-case input is
            // normalized rather than rejected - the files land under the lowercase name.
            NewAreaWriter.TryCreate(workspace, resolver, "WP73_Mixed", "Mixed", "tms01", 2, 2, null, out var error)
                .Should().BeTrue(error);

            File.Exists(workspace.GetResourcePath(ResourceType.Area, "wp73_mixed")).Should().BeTrue();
            workspace.EnumerateAreaResRefs().Should().Contain("wp73_mixed");
        }

        [TestCase("", "resref must be rejected when blank")]
        [TestCase("has spaces", "spaces are not allowed")]
        [TestCase("way_too_long_resref_name", "resrefs are capped at 16 characters")]
        [TestCase("bad-dash", "punctuation other than underscore is not allowed")]
        public void TryCreate_RejectsInvalidResRefs(string resRef, string why)
        {
            var workspace = new ModuleWorkspace(_moduleRoot);

            NewAreaWriter.TryCreate(workspace, RealTilesets(), resRef, "Name", "tms01", 2, 2, null, out var error)
                .Should().BeFalse(why);
            error.Should().NotBeEmpty();
        }

        [Test]
        public void TryCreate_RejectsOutOfRangeDimensions()
        {
            var workspace = new ModuleWorkspace(_moduleRoot);

            NewAreaWriter.TryCreate(workspace, RealTilesets(), "wp73_big", "Too big", "tms01", 33, 2, null, out var error)
                .Should().BeFalse();
            error.Should().Contain("between 1 and 32");

            NewAreaWriter.TryCreate(workspace, RealTilesets(), "wp73_zero", "Zero", "tms01", 0, 2, null, out _)
                .Should().BeFalse();
        }
    }
}
