using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Tests.Support;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Decoded textures come out with red in the red channel.
    /// </summary>
    /// <remarks>
    /// DDS block and pixel formats encode channels independently of the canonical RGBA surface. The
    /// shared decoder normalizes those channels; this corpus check keeps that contract observable with
    /// red-dominant artwork instead of relying on grayscale samples.
    /// </remarks>
    public class TextureChannelOrderTests
    {
        private static ResourceIndex? BuildIndex()
        {
            var installPath = NwnInstallLocator.Locate(Environment.GetEnvironmentVariable("NWN_INSTALL_PATH"));
            if (installPath == null)
                return null;

            var dataDirectory = Path.Combine(installPath, "data");
            if (!File.Exists(Path.Combine(dataDirectory, "nwn_base.key")))
                return null;

            return ResourceIndex.FromHakBuilderConfig(
                Path.Combine(ToolsetCorpusPaths.RepositoryRoot ?? throw new DirectoryNotFoundException(
                    "Set SWLOR_TEST_REPOSITORY_ROOT to the SWLOR source fixture root."), "Build", "hakbuilder.json"),
                ToolsetCorpusPaths.HaksRoot ?? throw new DirectoryNotFoundException(
                    "Set SWLOR_TEST_HAKS_ROOT to the SWLOR HAK fixture root."),
                KeyBifCatalog.Load(dataDirectory));
        }

        /// <summary>
        /// Skips the test when <paramref name="resRef"/> is not in the resource index at all.
        /// </summary>
        /// <remarks>
        /// These cases read SWLOR's own artwork, which lives in the SWLOR_Haks submodule. The fixture
        /// already skips when there is no NWN:EE install, but a checkout with the submodule
        /// uninitialised - a fresh clone, or any git worktree, which does not populate submodules - still
        /// built an index (the base game resolves fine) and then failed on a null image, reporting a
        /// texture-decoding regression where the truth was that the texture was never there.
        /// <para>
        /// Deliberately a presence check rather than skipping whenever the image is null: a texture that
        /// <b>is</b> in the index and fails to decode is exactly the regression these tests exist to
        /// catch, and must still fail.
        /// </para>
        /// </remarks>
        private static void RequireCorpusTexture(ResourceIndex index, string resRef)
        {
            foreach (var extension in new[] { "tga", "dds", "plt" })
            {
                if (index.TryLookup(new ResourceIdentity(resRef, ResourceIdentity.TypeFromExtension(extension)), out _))
                    return;
            }

            Assert.Ignore(
                $"'{resRef}' is not in the resource index - the SWLOR_Haks submodule is not populated " +
                "in this checkout, so there is no module artwork to decode.");
        }

        private static (int R, int G, int B)? AverageColour(ResourceIndex index, string resRef)
        {
            var image = TextureLoader.Load(index, resRef);
            if (image == null)
                return null;

            long r = 0, g = 0, b = 0;
            var texels = image.Width * image.Height;
            for (var i = 0; i < texels; i++)
            {
                r += image.Pixels[i * 4];
                g += image.Pixels[i * 4 + 1];
                b += image.Pixels[i * 4 + 2];
            }

            return ((int)(r / texels), (int)(g / texels), (int)(b / texels));
        }

        /// <summary>
        /// A standard DDS keeps its hue. chewyrug is the wookiee pelt on PLC_JR1 - brown fur, so red
        /// dominant. It decoded to rgb(31,49,74) and drew as a blue pelt, which is what exposed this.
        /// </summary>
        [Test]
        public void AStandardDdsKeepsItsHue()
        {
            var index = BuildIndex();
            if (index == null)
            {
                Assert.Ignore("No local NWN:EE installation was found; skipping.");
                return;
            }

            RequireCorpusTexture(index, "chewyrug");

            var colour = AverageColour(index, "chewyrug");
            colour.Should().NotBeNull("chewyrug ships with the module's placeable artwork");

            var (r, g, b) = colour!.Value;
            r.Should().BeGreaterThan(b,
                "brown fur is red-dominant; red below blue means the channels were exchanged");
            r.Should().BeGreaterThan(g);
        }

        /// <summary>
        /// BioWare's own DDS variant is unchanged by the fix. It was already right, because doing the
        /// swap while copying and doing it afterwards are the same swap - so this is the half of the
        /// corpus that must not move.
        /// </summary>
        [TestCase("zsf01_pipe")]
        [TestCase("zsf01_stonegld")]
        public void ABiowareVariantDdsIsUnchanged(string resRef)
        {
            var index = BuildIndex();
            if (index == null)
            {
                Assert.Ignore("No local NWN:EE installation was found; skipping.");
                return;
            }

            RequireCorpusTexture(index, resRef);

            var colour = AverageColour(index, resRef);
            colour.Should().NotBeNull();

            var (r, _, b) = colour!.Value;
            r.Should().BeGreaterThanOrEqualTo(b,
                "these tileset surfaces are rust and warm grey, never blue-dominant");
        }
    }
}
