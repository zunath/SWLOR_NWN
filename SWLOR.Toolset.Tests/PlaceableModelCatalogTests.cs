using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Tlk;
using SWLOR.Toolset.Domain.GameData.TwoDa;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// The Appearance tab's model grid, against the real placeables.2da. Its whole point is keeping
    /// the rows a dropdown cannot show, so it has to survive a table that is mostly holes.
    /// </summary>
    public class PlaceableModelCatalogTests
    {
        private static PlaceableModelCatalog? TryCreate()
        {
            var haks = Path.Combine(
                Directory.GetParent(CorpusLocator.ModuleDirectory)!.FullName, "SWLOR_Haks");

            if (!Directory.Exists(Path.Combine(haks, "sw_2da")))
                return null;

            var twoDa = new TwoDaService(Path.Combine(haks, "sw_2da"));
            var tlk = TlkService.Load(Path.Combine(haks, "sw_tlk", "sw_tlk.tlk.json"));
            return new PlaceableModelCatalog(twoDa, tlk);
        }

        [Test]
        public void GetAll_OverTheRealTable_KeepsEveryRowThatHasAModel()
        {
            var catalog = TryCreate();
            if (catalog == null)
                Assert.Ignore("SWLOR_Haks not available from the test context.");

            var rows = catalog!.GetAll();

            rows.Should().NotBeEmpty();
            rows.Should().OnlyContain(row => !string.IsNullOrEmpty(row.ModelName),
                "a row with no model is not pickable and must be dropped");
            rows.Should().OnlyContain(row => !string.IsNullOrEmpty(row.DisplayName),
                "an unlabelled row falls back to its model resref, so a caption is always present");
            rows.Count(row => !row.HasLabel).Should().BeGreaterThan(0,
                "most of the table has no label - that is why the grid exists");
        }

        [Test]
        public void Search_MatchesLabelOrModelResRef_AndTolleratesEveryRow()
        {
            var catalog = TryCreate();
            if (catalog == null)
                Assert.Ignore("SWLOR_Haks not available from the test context.");

            catalog!.Search(null).Should().HaveCount(catalog.GetAll().Count);
            catalog.Search("   ").Should().HaveCount(catalog.GetAll().Count);

            var sample = catalog.GetAll().First(row => row.HasLabel);
            catalog.Search(sample.DisplayName).Should().Contain(row => row.Id == sample.Id);
            catalog.Search(sample.ModelName).Should().Contain(row => row.Id == sample.Id);
        }
    }
}
