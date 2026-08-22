using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Placed creatures draw as themselves when the caller can compose them.
    /// </summary>
    /// <remarks>
    /// The scene builder resolves placeables, doors and waypoints from 2DA lookups it owns, but a
    /// humanoid creature is assembled from a skeleton and a dozen body parts by a composer that lives
    /// in the app layer. Without a resolver from the caller every creature fell through to the kind's
    /// marker - the red pyramid a builder sees where they just placed an NPC.
    /// </remarks>
    public class CreatureModelTests
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

        private static (ResourceIndex Index, TilesetCatalog Catalog, TileModelCache Models) Fixture()
        {
            var index = ResourceIndex.FromHakBuilderConfig(
                Path.Combine(RepoRoot, "Build", "hakbuilder.json"), Path.Combine(RepoRoot, "SWLOR_Haks"));
            return (index, new TilesetCatalog(index), new TileModelCache(index));
        }

        /// <summary>A stand-in for the app layer's composer: any resref resolves to the same geometry.</summary>
        private static RenderModel StubModel() => new()
        {
            Name = "stub",
            Meshes = new List<RenderMesh>
            {
                new()
                {
                    NodeName = "body",
                    TextureName = "stub",
                    Positions = new[] { 0f, 0f, 0f, 1f, 0f, 0f, 0f, 1f, 0f },
                    Normals = Array.Empty<float>(),
                    TexCoords = Array.Empty<float>(),
                    Indices = new[] { 0, 1, 2 },
                    Transform = System.Numerics.Matrix4x4.Identity
                }
            }
        };

        private static (AreDocumentPair Pair, string ResRef) AreaWithCreatures()
        {
            var workspace = new ModuleWorkspace(CorpusLocator.ModuleDirectory);
            foreach (var resRef in workspace.EnumerateAreaResRefs())
            {
                var (are, git, _) = workspace.LoadArea(resRef);
                if (git.Creatures.Count > 0)
                    return (new AreDocumentPair(are, git), resRef);
            }

            return (null!, string.Empty);
        }

        private sealed record AreDocumentPair(
            Domain.Documents.AreDocument Are, Domain.Documents.GitDocument Git);

        [Test]
        public void WithAResolver_PlacedCreaturesCarryTheirModel()
        {
            var (_, catalog, models) = Fixture();
            var (pair, resRef) = AreaWithCreatures();
            if (pair == null)
            {
                Assert.Ignore("No corpus area carries creatures; skipping.");
                return;
            }

            var asked = new List<JsonGffStruct>();
            var scene = AreaSceneBuilder.Build(
                pair.Are, pair.Git, catalog, models,
                resolveCreatureModel: instance => { asked.Add(instance); return StubModel(); });

            var creatures = scene.Instances.Where(i => i.Kind == InstanceMarkerKind.Creature).ToList();
            creatures.Should().NotBeEmpty($"'{resRef}' was chosen because it has creatures");
            creatures.Should().OnlyContain(c => c.Model != null,
                "a creature with no model draws as the red marker pyramid");
            asked.Should().NotBeEmpty("the builder must ask the caller to compose each creature");
        }

        /// <summary>
        /// Without a resolver nothing changes - a caller that cannot compose creatures still gets the
        /// marker, which is what every non-editor consumer of the builder relies on.
        /// </summary>
        [Test]
        public void WithoutAResolver_CreaturesStillFallBackToTheirMarker()
        {
            var (_, catalog, models) = Fixture();
            var (pair, _) = AreaWithCreatures();
            if (pair == null)
            {
                Assert.Ignore("No corpus area carries creatures; skipping.");
                return;
            }

            var scene = AreaSceneBuilder.Build(pair.Are, pair.Git, catalog, models);

            scene.Instances.Where(i => i.Kind == InstanceMarkerKind.Creature)
                .Should().OnlyContain(c => c.Model == null);
        }

        /// <summary>
        /// The resolver is asked for every embedded instance, because placements sharing a blueprint
        /// may carry different appearance and body-part overrides.
        /// </summary>
        [Test]
        public void TheResolverIsAskedPerInstance_SoTheCallerMustCache()
        {
            var (_, catalog, models) = Fixture();
            var (pair, _) = AreaWithCreatures();
            if (pair == null)
            {
                Assert.Ignore("No corpus area carries creatures; skipping.");
                return;
            }

            var asked = new List<JsonGffStruct>();
            AreaSceneBuilder.Build(
                pair.Are, pair.Git, catalog, models,
                resolveCreatureModel: instance => { asked.Add(instance); return StubModel(); });

            asked.Count.Should().Be(pair.Git.Creatures.Count);
            asked.Should().Equal(pair.Git.Creatures);
        }
    }
}
