using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// The area viewport shares one <see cref="TileModelCache"/> across every open area, and its
    /// misses are filled from <see cref="System.Collections.Concurrent.ConcurrentDictionary.GetOrAdd"/>,
    /// which deliberately does not serialise its factory. That makes the parse itself the thing that
    /// has to be safe to run twice at once.
    /// </summary>
    /// <remarks>
    /// It was not. The cache held one <c>MdlReader</c>, and <c>MdlBinaryReader</c> keeps the model's
    /// data block, its pointer base and the live <c>BinaryReader</c> in fields - so two tiles parsing
    /// together read each other's bytes. The damage was silent and area-wide: cz220shipbreakin drew
    /// enormous garbage triangles across the whole map (vertices read at another model's pointer
    /// base), untextured black slabs (mesh headers landing on the wrong bytes) and magenta fallback
    /// tiles (a read past the end throwing, and the null being cached).
    /// </remarks>
    public class TileModelCacheConcurrencyTests
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

        /// <summary>Every distinct tile model of the interior tileset the corruption was first seen on.</summary>
        private static IReadOnlyList<string> ZsfTileModels(ResourceIndex index)
        {
            new TilesetCatalog(index).TryGetTileset("zsf01", out var tileset).Should().BeTrue();
            return tileset!.Tiles
                .Select(tile => tile.Model)
                .Where(model => !string.IsNullOrWhiteSpace(model))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        [Test]
        public void ParallelGetOrBuild_ProducesTheSameGeometryAsSerialGetOrBuild()
        {
            var index = BuildIndex();
            var models = ZsfTileModels(index);
            models.Should().NotBeEmpty();

            // The reference: one model at a time, so no two parses can overlap.
            var serial = new TileModelCache(index);
            var expected = models.ToDictionary(
                resRef => resRef,
                resRef => Fingerprint(serial.GetOrBuild(resRef)),
                StringComparer.OrdinalIgnoreCase);

            expected.Values.Should().NotContain(
                (string?)null, "every zsf01 tile model resolves when parsed one at a time");

            // Several passes: a race that only shows sometimes is still a race, and one pass over a
            // 70-tile tileset is a small window.
            for (var pass = 0; pass < 4; pass++)
            {
                var cache = new TileModelCache(index);
                var actual = new System.Collections.Concurrent.ConcurrentDictionary<string, string?>(
                    StringComparer.OrdinalIgnoreCase);

                Parallel.ForEach(
                    models,
                    new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                    resRef => actual[resRef] = Fingerprint(cache.GetOrBuild(resRef)));

                foreach (var (resRef, fingerprint) in expected)
                {
                    actual[resRef].Should().Be(
                        fingerprint,
                        "pass {0}: '{1}' must parse to the same geometry however many models parse alongside it",
                        pass,
                        resRef);
                }
            }
        }

        /// <summary>
        /// Mesh count, triangle count, texture names and the geometry's bounding box - enough that any
        /// of the three observed corruptions (garbage vertices, wrong texture, failed parse) changes it.
        /// </summary>
        private static string? Fingerprint(RenderModel? model)
        {
            if (model == null)
                return null;

            var minX = float.MaxValue;
            var maxX = float.MinValue;
            var minZ = float.MaxValue;
            var maxZ = float.MinValue;

            foreach (var mesh in model.Meshes)
            {
                for (var i = 0; i + 2 < mesh.Positions.Length; i += 3)
                {
                    minX = MathF.Min(minX, mesh.Positions[i]);
                    maxX = MathF.Max(maxX, mesh.Positions[i]);
                    minZ = MathF.Min(minZ, mesh.Positions[i + 2]);
                    maxZ = MathF.Max(maxZ, mesh.Positions[i + 2]);
                }
            }

            var meshes = string.Join(
                "|",
                model.Meshes.Select(mesh =>
                    $"{mesh.NodeName}:{mesh.TextureName}:{mesh.TriangleCount}:{mesh.TileFade}"));

            return $"{model.Name}[{model.Meshes.Count}] x[{minX:F3},{maxX:F3}] z[{minZ:F3},{maxZ:F3}] {meshes}";
        }
    }
}
