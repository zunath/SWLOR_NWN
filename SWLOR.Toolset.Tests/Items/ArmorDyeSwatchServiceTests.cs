using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Editors.Items;

namespace SWLOR.Toolset.Tests.Items
{
    /// <summary>
    /// Coverage for <see cref="ArmorDyeSwatchService"/>'s shared appearance-palette contract: no
    /// resource layer, or one that cannot resolve Aurora's armor/skin/hair/tattoo palette textures,
    /// yields null (the caller renders an unavailable state rather than inventing a color)
    /// rather than throwing. Color-accurate sampling against the real palette artwork is not
    /// verifiable from this repo: those TGAs ship only in the base-game BIF, never in SWLOR_Haks -
    /// see <c>RenderPipelineTests.TextureLoader_LoadPlt_ForKnownCorpusTexture_DecodesToReportedDimensions</c>
    /// for the same corpus limitation on the PLT decode path this service reuses.
    /// </summary>
    [TestFixture]
    public class ArmorDyeSwatchServiceTests
    {
        private static string HakBuilderConfigPath =>
            Path.Combine(CorpusLocator.RepositoryRoot, "Build", "hakbuilder.json");

        private static string HaksDirectory =>
            Path.Combine(CorpusLocator.RepositoryRoot, "SWLOR_Haks");

        private static ResourceIndex BuildHakOnlyIndex() =>
            ResourceIndex.FromHakBuilderConfig(HakBuilderConfigPath, HaksDirectory);

        [Test]
        public void NoResourceIndex_DegradesToNullForEveryMaterial()
        {
            var service = new ArmorDyeSwatchService(null);

            service.GetColor(ArmorDyeSwatchService.DyeMaterial.Cloth, 23).Should().BeNull();
            service.GetColor(ArmorDyeSwatchService.DyeMaterial.Leather, 23).Should().BeNull();
            service.GetColor(ArmorDyeSwatchService.DyeMaterial.Metal1, 23).Should().BeNull();
            service.GetColor(ArmorDyeSwatchService.DyeMaterial.Metal2, 23).Should().BeNull();
            service.GetColor(ArmorDyeSwatchService.DyeMaterial.Skin, 23).Should().BeNull();
            service.GetColor(ArmorDyeSwatchService.DyeMaterial.Hair, 23).Should().BeNull();
            service.GetColor(ArmorDyeSwatchService.DyeMaterial.Tattoo, 23).Should().BeNull();
        }

        [Test]
        public void HakOnlyIndex_DegradesToNullRatherThanThrowing()
        {
            // The dye palette TGAs ship only in the base game, so a hak-only index (this repo's test
            // corpus) never resolves them - the neutral-chip fallback this service exists to enable.
            var service = new ArmorDyeSwatchService(BuildHakOnlyIndex());

            service.GetColor(ArmorDyeSwatchService.DyeMaterial.Cloth, 23).Should().BeNull();
        }

        [Test]
        public void RepeatedLookupsForTheSameMaterialDoNotThrowOrDeadlock()
        {
            // Exercises the palette cache's repeat-lookup path (still a graceful-degrade miss here).
            var service = new ArmorDyeSwatchService(BuildHakOnlyIndex());

            for (var index = 0; index < 5; index++)
                service.GetColor(ArmorDyeSwatchService.DyeMaterial.Metal2, index).Should().BeNull();
        }

        [Test]
        public void MetalChannelsSampleTheirSeparateAuroraPalettes()
        {
            var directory = Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                "armor-dye-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                File.WriteAllBytes(
                    Path.Combine(directory, "pal_armor01.tga"),
                    SolidColorTga(10, 20, 30));
                File.WriteAllBytes(
                    Path.Combine(directory, "pal_armor02.tga"),
                    SolidColorTga(40, 50, 60));
                var index = new ResourceIndex(
                    null,
                    new[] { new ResourceIndex.HakLayer("fixture", directory) });
                var service = new ArmorDyeSwatchService(index);

                service.GetColor(ArmorDyeSwatchService.DyeMaterial.Metal1, 0)
                    .Should().Be(((byte)10, (byte)20, (byte)30));
                service.GetColor(ArmorDyeSwatchService.DyeMaterial.Metal2, 0)
                    .Should().Be(((byte)40, (byte)50, (byte)60));
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Test]
        public void CreatureChannelsUseTheRenderersAuroraPalettes()
        {
            var directory = Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                "creature-color-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                File.WriteAllBytes(Path.Combine(directory, "pal_skin01.tga"), SolidColorTga(10, 20, 30));
                File.WriteAllBytes(Path.Combine(directory, "pal_hair01.tga"), SolidColorTga(40, 50, 60));
                File.WriteAllBytes(Path.Combine(directory, "pal_tattoo01.tga"), SolidColorTga(70, 80, 90));
                var index = new ResourceIndex(
                    null,
                    new[] { new ResourceIndex.HakLayer("fixture", directory) });
                var service = new ArmorDyeSwatchService(index);

                service.GetColor(ArmorDyeSwatchService.DyeMaterial.Skin, 0)
                    .Should().Be(((byte)10, (byte)20, (byte)30));
                service.GetColor(ArmorDyeSwatchService.DyeMaterial.Hair, 0)
                    .Should().Be(((byte)40, (byte)50, (byte)60));
                service.GetColor(ArmorDyeSwatchService.DyeMaterial.Tattoo, 0)
                    .Should().Be(((byte)70, (byte)80, (byte)90));
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Test]
        public void FindsTheClosestRenderedPaletteColor()
        {
            var directory = Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                "closest-dye-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                File.WriteAllBytes(
                    Path.Combine(directory, "pal_cloth01.tga"),
                    VerticalColorsTga(
                        ((byte)10, (byte)20, (byte)30),
                        ((byte)40, (byte)50, (byte)60),
                        ((byte)70, (byte)80, (byte)90)));
                var index = new ResourceIndex(
                    null,
                    new[] { new ResourceIndex.HakLayer("fixture", directory) });
                var service = new ArmorDyeSwatchService(index);
                var target = service.GetColor(ArmorDyeSwatchService.DyeMaterial.Cloth, 1);

                target.Should().NotBeNull();
                service.FindClosestColorIndex(
                        ArmorDyeSwatchService.DyeMaterial.Cloth,
                        target!.Value)
                    .Should().Be(1);
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        private static byte[] SolidColorTga(byte red, byte green, byte blue)
        {
            var bytes = new byte[21];
            bytes[2] = 2;
            System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(12, 2), 1);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(14, 2), 1);
            bytes[16] = 24;
            bytes[18] = blue;
            bytes[19] = green;
            bytes[20] = red;
            return bytes;
        }

        private static byte[] VerticalColorsTga(params (byte R, byte G, byte B)[] colors)
        {
            var bytes = new byte[18 + colors.Length * 3];
            bytes[2] = 2;
            System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(12, 2), 1);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(
                bytes.AsSpan(14, 2),
                checked((ushort)colors.Length));
            bytes[16] = 24;
            for (var index = 0; index < colors.Length; index++)
            {
                var offset = 18 + index * 3;
                bytes[offset] = colors[index].B;
                bytes[offset + 1] = colors[index].G;
                bytes[offset + 2] = colors[index].R;
            }

            return bytes;
        }
    }
}
