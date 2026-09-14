using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Editors.Items;

namespace SWLOR.Toolset.Tests.Items
{
    [TestFixture]
    public class ArmorPartCatalogTests
    {
        [Test]
        public void CatalogEnumeratesSparseAndExtendedPartsForTheRequestedSideOnly()
        {
            var scratch = Path.Combine(Path.GetTempPath(), "swlor-armor-parts-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(scratch);
            try
            {
                File.WriteAllBytes(Path.Combine(scratch, "pmh0_bicepl001.mdl"), Array.Empty<byte>());
                File.WriteAllBytes(Path.Combine(scratch, "pfh0_bicepl270.mdl"), Array.Empty<byte>());
                File.WriteAllBytes(Path.Combine(scratch, "pmh0_bicepr002.mdl"), Array.Empty<byte>());
                File.WriteAllBytes(Path.Combine(scratch, "pmh0_bicepl_bad.mdl"), Array.Empty<byte>());
                File.WriteAllBytes(Path.Combine(scratch, "helm_309.mdl"), Array.Empty<byte>());

                var resources = new ResourceIndex(
                    baseLayer: null,
                    hakLayersInOrder: new[] { new ResourceIndex.HakLayer("fixture", scratch) });
                var catalog = new ArmorPartCatalog(resources);

                catalog.Numbers("bicepl").Should().Equal(1, 270);
                catalog.Numbers("bicepr").Should().Equal(2);
                catalog.NumbersForModelPrefix("helm_").Should().Equal(309);
            }
            finally
            {
                Directory.Delete(scratch, recursive: true);
            }
        }

        [Test]
        public void WithNonePrependsTheEngineNoModelValueOnce()
        {
            ArmorPartCatalog.WithNone(new[] { 1, 3, 7 }).Should().Equal(0, 1, 3, 7);
            ArmorPartCatalog.WithNone(new[] { 0, 1, 3 }).Should().Equal(0, 1, 3);
            ArmorPartCatalog.WithNone(Array.Empty<int>()).Should().BeEmpty();
        }
    }
}
