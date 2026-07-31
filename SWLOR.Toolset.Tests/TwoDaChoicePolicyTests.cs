using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Tlk;
using SWLOR.Toolset.Domain.GameData.TwoDa;

namespace SWLOR.Toolset.Tests
{
    public sealed class TwoDaChoicePolicyTests
    {
        [TestCase("")]
        [TestCase("****")]
        [TestCase("DELETED")]
        [TestCase("USER")]
        [TestCase("Unused")]
        [TestCase("UNUSED_42")]
        [TestCase("INVALID_RACE")]
        [TestCase("Bio_reserved")]
        [TestCase("cep reserved")]
        [TestCase("Padding")]
        [TestCase("NULL6")]
        public void PlaceholderLabelsAreNeverSelectable(string label)
        {
            TwoDaChoicePolicy.IsSelectableLabel(label).Should().BeFalse();
        }

        [TestCase("Human")]
        [TestCase("Outsider")]
        [TestCase("User Interface")]
        public void ContentLabelsRemainSelectable(string label)
        {
            TwoDaChoicePolicy.IsSelectableLabel(label).Should().BeTrue();
        }

        [Test]
        public void DeclaredValidityColumnsExcludeStructurallyIncompleteRaceRows()
        {
            var scratch = Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                $"two-da-choice-{Guid.NewGuid():N}");
            Directory.CreateDirectory(scratch);
            try
            {
                File.WriteAllText(
                    Path.Combine(scratch, "racialtypes.2da"),
                    "2DA V2.0\r\n\r\nLabel Name Constant\r\n" +
                    "0 Human 34 RACIAL_TYPE_HUMAN\r\n" +
                    "1 DELETED **** ****\r\n" +
                    "2 USER **** ****\r\n" +
                    "3 Bio_reserved **** ****\r\n" +
                    "4 INVALID_RACE **** ****\r\n" +
                    "5 Looks_Real 123 ****\r\n");
                var service = new TwoDaLookupService(
                    new TwoDaService(scratch),
                    new TlkService(TlkJsonFile.Parse("{\"language\":0,\"entries\":[]}")));

                service.GetRows(new TwoDaLookupTable(
                        "racialtypes",
                        "Label",
                        "Name",
                        ["Constant"]))
                    .Should().ContainSingle()
                    .Which.Should().Be(new TwoDaLookupRow(0, "Human", "Human"));
            }
            finally
            {
                Directory.Delete(scratch, recursive: true);
            }
        }

        [Test]
        public void MissingValidityColumnFailsClosed()
        {
            var scratch = Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                $"two-da-choice-{Guid.NewGuid():N}");
            Directory.CreateDirectory(scratch);
            try
            {
                File.WriteAllText(
                    Path.Combine(scratch, "racialtypes.2da"),
                    "2DA V2.0\r\n\r\nLabel Name\r\n0 Human 34\r\n");
                var service = new TwoDaLookupService(
                    new TwoDaService(scratch),
                    new TlkService(TlkJsonFile.Parse("{\"language\":0,\"entries\":[]}")));

                service.GetRows(new TwoDaLookupTable(
                        "racialtypes",
                        "Label",
                        "Name",
                        ["Constant"]))
                    .Should().BeEmpty();
            }
            finally
            {
                Directory.Delete(scratch, recursive: true);
            }
        }
    }
}
