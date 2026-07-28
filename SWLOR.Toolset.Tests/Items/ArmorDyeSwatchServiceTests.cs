using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Editors.Items;

namespace SWLOR.Toolset.Tests.Items
{
    /// <summary>
    /// Coverage for <see cref="ArmorDyeSwatchService"/>'s graceful-degrade contract: no resource
    /// layer, or a resource layer that cannot resolve the shared <c>pal_cloth01</c>/<c>pal_leath01</c>/
    /// <c>pal_armor01</c> palette textures, both yield null (the caller renders a neutral chip)
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
            service.GetColor(ArmorDyeSwatchService.DyeMaterial.Metal, 23).Should().BeNull();
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
                service.GetColor(ArmorDyeSwatchService.DyeMaterial.Metal, index).Should().BeNull();
        }
    }
}
