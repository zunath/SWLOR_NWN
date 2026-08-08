using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// A model's UVs land on the part of its texture the artist meant.
    /// </summary>
    /// <remarks>
    /// NWN stores DDS rows top-down and TGA rows bottom-up, and samples both with the same bottom-up
    /// UVs, so the decoders have to be brought into one convention before anything else touches them.
    /// Getting it wrong flips every DDS-textured surface vertically - which is invisible unless the
    /// texture's top and bottom actually differ, and so survived until a texture turned up with a
    /// black strip along one edge.
    /// </remarks>
    public class TextureOrientationCorpusTests
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

        /// <summary>
        /// The wookiee rug is brown fur, not a black shape. Its texture carries a black strip near one
        /// edge; sampled upside down, 15% of the mesh's area landed on it and the head rendered solid
        /// black. Right way up it is 4%, which is the shadowed underside the strip is there for.
        /// </summary>
        [Test]
        public void ADdsTexturedModelSamplesTheArtistsSideOfItsTexture()
        {
            var installPath = NwnInstallLocator.Locate();
            if (installPath == null)
            {
                Assert.Ignore("No local NWN:EE installation was found; skipping.");
                return;
            }

            var index = ResourceIndex.FromHakBuilderConfig(
                Path.Combine(RepoRoot, "Build", "hakbuilder.json"),
                Path.Combine(RepoRoot, "SWLOR_Haks"),
                KeyBifCatalog.Load(Path.Combine(installPath, "data")));

            var identity = new ResourceIdentity("PLC_JR1", ResourceIdentity.TypeFromExtension("mdl"));
            if (!index.TryLookup(identity, out var handle))
            {
                Assert.Ignore("PLC_JR1 did not resolve; skipping.");
                return;
            }

            var mesh = MdlMeshBuilder.Build(new MdlReader().Parse(handle.GetBytes())).Meshes.Single();
            var image = TextureLoader.Load(index, mesh.TextureName);
            image.Should().NotBeNull();

            double darkArea = 0, totalArea = 0;
            for (var face = 0; face + 2 < mesh.Indices.Length; face += 3)
            {
                var i0 = mesh.Indices[face];
                var i1 = mesh.Indices[face + 1];
                var i2 = mesh.Indices[face + 2];
                if (i0 * 2 + 1 >= mesh.TexCoords.Length || i2 * 2 + 1 >= mesh.TexCoords.Length)
                    continue;

                var a = new Vector3(mesh.Positions[i0 * 3], mesh.Positions[i0 * 3 + 1], mesh.Positions[i0 * 3 + 2]);
                var b = new Vector3(mesh.Positions[i1 * 3], mesh.Positions[i1 * 3 + 1], mesh.Positions[i1 * 3 + 2]);
                var c = new Vector3(mesh.Positions[i2 * 3], mesh.Positions[i2 * 3 + 1], mesh.Positions[i2 * 3 + 2]);
                var area = Vector3.Cross(b - a, c - a).Length() * 0.5f;

                var u = (mesh.TexCoords[i0 * 2] + mesh.TexCoords[i1 * 2] + mesh.TexCoords[i2 * 2]) / 3f;
                var v = (mesh.TexCoords[i0 * 2 + 1] + mesh.TexCoords[i1 * 2 + 1] + mesh.TexCoords[i2 * 2 + 1]) / 3f;
                u -= MathF.Floor(u);
                v -= MathF.Floor(v);

                // The convention the renderers use: NWN UVs run bottom-up over a top-down image.
                var x = Math.Clamp((int)(u * image!.Width), 0, image.Width - 1);
                var y = Math.Clamp((int)((1f - v) * image.Height), 0, image.Height - 1);
                var offset = (y * image.Width + x) * 4;
                var brightness = (image.Pixels[offset] + image.Pixels[offset + 1] + image.Pixels[offset + 2]) / 3;

                totalArea += area;
                if (brightness < 20)
                    darkArea += area;
            }

            totalArea.Should().BeGreaterThan(0);
            (darkArea / totalArea).Should().BeLessThan(0.08,
                "the pelt is brown fur; a large dark share means it is being sampled upside down");
        }
    }
}
