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
        public void GetAll_OverTheRealTable_KeepsEverySelectableRowThatHasAModel()
        {
            var catalog = TryCreate();
            if (catalog == null)
                Assert.Ignore("SWLOR_Haks not available from the test context.");

            var rows = catalog!.GetAll();

            rows.Should().NotBeEmpty();
            rows.Should().OnlyContain(row => !string.IsNullOrEmpty(row.ModelName),
                "a row with no model is not pickable and must be dropped");
            rows.Should().OnlyContain(row => TwoDaChoicePolicy.IsSelectableLabel(row.ModelName),
                "reserved model resrefs are engine slots rather than builder choices");
            rows.Should().OnlyContain(row => !string.IsNullOrEmpty(row.DisplayName),
                "an unlabelled row falls back to its model resref, so a caption is always present");
            rows.Select(row => row.Id).Should().OnlyHaveUniqueItems(
                "each selectable 2DA row must appear exactly once in the gallery");
        }

        [Test]
        public void GetAll_FiltersSentinelsFromLabelsAndModelResRefs()
        {
            var scratch = Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                $"placeable-model-policy-{Guid.NewGuid():N}");
            Directory.CreateDirectory(scratch);
            try
            {
                File.WriteAllText(
                    Path.Combine(scratch, "placeables.2da"),
                    "2DA V2.0\r\n\r\nLabel StrRef ModelName\r\n" +
                    "0 Real_Placeable **** real_model\r\n" +
                    "1 **** **** unlabeled_model\r\n" +
                    "2 **** **** bio_reserved\r\n" +
                    "3 CEP_RESERVED **** reserved_model\r\n" +
                    "4 Real_No_Model **** ****\r\n" +
                    "5 User002 **** user_model\r\n");
                var catalog = new PlaceableModelCatalog(
                    new TwoDaService(scratch),
                    new TlkService(TlkJsonFile.Parse("{\"language\":0,\"entries\":[]}")));

                catalog.GetAll().Select(row => row.Id).Should().Equal(0, 1);
                catalog.GetAll()[1].Should().Be(
                    new PlaceableModelRow(1, "unlabeled_model", "unlabeled_model", false));
            }
            finally
            {
                Directory.Delete(scratch, recursive: true);
            }
        }

        [TestCase("StrRef ModelName", "**** real_model")]
        [TestCase("Label StrRef", "Real_Placeable ****")]
        public void GetAll_FailsClosedWhenDeclaredMetadataIsMissing(string columns, string row)
        {
            var scratch = Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                $"placeable-model-metadata-{Guid.NewGuid():N}");
            Directory.CreateDirectory(scratch);
            try
            {
                File.WriteAllText(
                    Path.Combine(scratch, "placeables.2da"),
                    $"2DA V2.0\r\n\r\n{columns}\r\n0 {row}\r\n");
                var catalog = new PlaceableModelCatalog(
                    new TwoDaService(scratch),
                    new TlkService(TlkJsonFile.Parse("{\"language\":0,\"entries\":[]}")));

                catalog.GetAll().Should().BeEmpty(
                    "a gallery cannot prove rows are selectable without every declared metadata column");
                catalog.TryGet(0, out _).Should().BeFalse();
            }
            finally
            {
                Directory.Delete(scratch, recursive: true);
            }
        }

        [Test]
        public void Search_MatchesLabelOrModelResRef_AndToleratesEveryRow()
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

        [Test]
        public void MissingPlaceablesTableDegradesToAnEmptyCatalog()
        {
            var empty2Da = Path.Combine(
                Path.GetTempPath(), "swlor-missing-placeables-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(empty2Da);
            try
            {
                var haks = Path.Combine(
                    Directory.GetParent(CorpusLocator.ModuleDirectory)!.FullName, "SWLOR_Haks");
                var tlkPath = Path.Combine(haks, "sw_tlk", "sw_tlk.tlk.json");
                if (!File.Exists(tlkPath))
                    Assert.Ignore("SWLOR_Haks TLK is not available from the test context.");

                var catalog = new PlaceableModelCatalog(
                    new TwoDaService(empty2Da),
                    TlkService.Load(tlkPath));

                var read = () => catalog.GetAll();
                read.Should().NotThrow();
                catalog.GetAll().Should().BeEmpty();
                catalog.Search("anything").Should().BeEmpty();
                catalog.TryGet(1, out _).Should().BeFalse();
            }
            finally
            {
                Directory.Delete(empty2Da, recursive: true);
            }
        }
    }
}
