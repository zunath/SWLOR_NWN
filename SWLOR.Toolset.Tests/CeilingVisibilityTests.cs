using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// The viewport hides an interior tileset's ceilings by dropping mesh nodes whose MDL
    /// <c>tilefade</c> flag is non-zero (see <c>GlAreaControl.ShowCeilings</c>). These cover the two
    /// facts that rule depends on: the flag survives into <see cref="RenderMesh"/>, and it really does
    /// separate overhead geometry from floors and walls.
    /// </summary>
    public class CeilingVisibilityTests
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

        private static ResourceIndex BuildIndex() =>
            ResourceIndex.FromHakBuilderConfig(
                Path.Combine(RepoRoot, "Build", "hakbuilder.json"),
                Path.Combine(RepoRoot, "SWLOR_Haks"));

        /// <summary>
        /// Every mesh the tileset itself calls a ceiling must be flagged, and nothing at floor level
        /// may be - otherwise hiding ceilings would either leave the room sealed or take its floor.
        /// </summary>
        [Test]
        public void InteriorTileMeshes_FlagTheirCeilingsAndOnlyTheirCeilings()
        {
            var index = BuildIndex();
            var cache = new TileModelCache(index);
            new TilesetCatalog(index).TryGetTileset("zsf01", out var tileset).Should().BeTrue();

            var meshes = tileset!.Tiles
                .Select(tile => tile.Model)
                .Where(model => !string.IsNullOrWhiteSpace(model))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(cache.GetOrBuild)
                .Where(model => model != null)
                .SelectMany(model => model!.Meshes)
                .ToList();

            meshes.Should().NotBeEmpty();

            // BioWare's own spelling in this tileset. Named nodes are the unambiguous cases; the flag
            // covers more than these (upper wall bands, roof slabs), which is exactly why the renderer
            // reads the flag rather than the name.
            var ceilings = meshes.Where(m => m.NodeName.StartsWith("ceilling", StringComparison.OrdinalIgnoreCase)).ToList();
            ceilings.Should().NotBeEmpty("zsf01 is an interior tileset and its tiles carry ceiling nodes");
            ceilings.Should().OnlyContain(m => m.TileFade != 0, "a ceiling is what tilefade marks");

            // Nothing flagged may reach the floor, or hiding ceilings would take the ground out from
            // under the builder. Height is read as the node's own Z plus its transform's translation:
            // these tiles' node transforms are translations, so that is their world height.
            foreach (var mesh in meshes.Where(m => m.TileFade != 0))
            {
                var lowest = float.MaxValue;
                for (var i = 2; i < mesh.Positions.Length; i += 3)
                    lowest = MathF.Min(lowest, mesh.Positions[i] + mesh.Transform.M43);

                lowest.Should().BeGreaterThan(
                    -0.01f,
                    "'{0}' is flagged as overhead, so none of it should sit at or below the tile floor",
                    mesh.NodeName);
            }
        }

        /// <summary>
        /// An exterior tileset flags overhead geometry too - ttw01's canopy shell - which is why the
        /// hide pass is gated on the tileset being interior rather than applied everywhere.
        /// </summary>
        [Test]
        public void ExteriorTilesetsAlsoFlagOverheadGeometry()
        {
            var index = BuildIndex();
            var cache = new TileModelCache(index);

            var canopy = cache.GetOrBuild("ttw01_a03_01");
            canopy.Should().NotBeNull();
            canopy!.Meshes.Should().Contain(
                mesh => mesh.NodeName.Equals("treefol_01", StringComparison.OrdinalIgnoreCase) && mesh.TileFade != 0,
                "the forest canopy carries the same flag an interior ceiling does");
        }

        [TestCase("cz220shipbreakin", true)]
        [TestCase("kashyyykpaths", false)]
        public void SceneRecordsWhetherItsTilesetIsInterior(string areaResRef, bool expected)
        {
            var index = BuildIndex();
            var workspace = new ModuleWorkspace(CorpusLocator.ModuleDirectory);
            var (are, git, _) = workspace.LoadArea(areaResRef);

            var scene = AreaSceneBuilder.Build(
                are, git, new TilesetCatalog(index), new TileModelCache(index));

            scene.IsInteriorTileset.Should().Be(expected);
        }

        /// <summary>
        /// An area whose tileset cannot be resolved must not be treated as an interior: hiding
        /// geometry on a guess is worse than showing all of it.
        /// </summary>
        [Test]
        public void UnresolvableTilesetIsNotTreatedAsInterior()
        {
            var index = BuildIndex();
            var workspace = new ModuleWorkspace(CorpusLocator.ModuleDirectory);
            var (are, git, _) = workspace.LoadArea("cz220shipbreakin");

            are.Tileset = "no_such_tileset";

            var scene = AreaSceneBuilder.Build(
                are, git, new TilesetCatalog(index), new TileModelCache(index));

            scene.IsInteriorTileset.Should().BeFalse();
        }
    }
}
