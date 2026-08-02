using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Coverage for <see cref="TextureAlphaPolicy"/>: which textures have to be cut out rather than
    /// drawn solid, decided from the alpha channel because most that need it carry no TXI.
    /// </summary>
    public class TextureAlphaPolicyTests
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

        private static TextureImage Image(int width, int height, Func<int, byte> alphaAt)
        {
            var pixels = new byte[width * height * 4];
            for (var i = 0; i < width * height; i++)
            {
                pixels[i * 4] = 200;
                pixels[i * 4 + 1] = 200;
                pixels[i * 4 + 2] = 200;
                pixels[i * 4 + 3] = alphaAt(i);
            }

            return new TextureImage
            {
                Width = width,
                Height = height,
                Pixels = pixels,
                SourceFormat = TextureSourceFormat.Tga
            };
        }

        [Test]
        public void FullyOpaqueTexture_NeedsNoCutoff()
        {
            TextureAlphaPolicy.RequiresCutoff(Image(16, 16, _ => 255)).Should().BeFalse();
        }

        [Test]
        public void HalfTransparentTexture_NeedsACutoff()
        {
            TextureAlphaPolicy.RequiresCutoff(Image(16, 16, i => (byte)(i % 2 == 0 ? 0 : 255))).Should().BeTrue();
        }

        /// <summary>
        /// A handful of stray transparent texels - a DXT block artefact, a bad pixel - must not start
        /// punching holes in a surface that is meant to be solid.
        /// </summary>
        [Test]
        public void ATraceOfTransparency_NeedsNoCutoff()
        {
            // 2 transparent texels out of 1024 is under the one-percent threshold.
            TextureAlphaPolicy.RequiresCutoff(Image(32, 32, i => (byte)(i < 2 ? 0 : 255))).Should().BeFalse();
        }

        [TestCase(0, 4)]
        [TestCase(4, 0)]
        public void DegenerateDimensions_NeedNoCutoff(int width, int height)
        {
            var image = new TextureImage
            {
                Width = width,
                Height = height,
                Pixels = Array.Empty<byte>(),
                SourceFormat = TextureSourceFormat.Tga
            };

            TextureAlphaPolicy.RequiresCutoff(image).Should().BeFalse();
        }

        [Test]
        public void NullImage_NeedsNoCutoff()
        {
            TextureAlphaPolicy.RequiresCutoff(null).Should().BeFalse();
        }

        /// <summary>A pixel buffer too short for its own dimensions is read as opaque, not indexed past its end.</summary>
        [Test]
        public void TruncatedPixelBuffer_NeedsNoCutoffAndDoesNotThrow()
        {
            var image = new TextureImage
            {
                Width = 16,
                Height = 16,
                Pixels = new byte[16],
                SourceFormat = TextureSourceFormat.Tga
            };

            Action act = () => TextureAlphaPolicy.RequiresCutoff(image);

            act.Should().NotThrow();
            TextureAlphaPolicy.RequiresCutoff(image).Should().BeFalse();
        }

        /// <summary>
        /// The real case: the tileset grating that drew as solid black must be recognised as cut-out,
        /// and the solid floor textures beside it must not be.
        /// </summary>
        /// <remarks>
        /// Reads the actual game textures rather than a synthetic stand-in, because the point of the
        /// policy is what it decides about the artwork that shipped. zsf01_bridge is the floor grate of
        /// zsf01_d05_01, laid 62 times in cz220shipbreakin.
        /// </remarks>
        [Test]
        public void TheTilesetGrating_IsCutOutAndTheFloorsAreNot()
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

            var grating = TextureLoader.Load(index, "zsf01_bridge");
            grating.Should().NotBeNull("zsf01_bridge ships in the sci-fi base tileset hak");
            TextureAlphaPolicy.RequiresCutoff(grating).Should().BeTrue(
                "the floor grate is see-through, and drawing it solid turns 62 tiles black");

            foreach (var solid in new[] { "zsf01_rock3", "zsf01_stonfloor2", "zsf01_stonegld" })
            {
                var image = TextureLoader.Load(index, solid);
                if (image == null)
                    continue;

                var transparent = Enumerable.Range(0, image.Width * image.Height)
                    .Count(index => image.Pixels[index * 4 + 3] < 128);
                var transparentShare = (double)transparent / (image.Width * image.Height);
                TextureAlphaPolicy.RequiresCutoff(image).Should().BeFalse(
                    $"'{solid}' is a solid surface and must not be punched through " +
                    $"(decoded transparent share: {transparentShare:P2})");
            }
        }
    }
}
