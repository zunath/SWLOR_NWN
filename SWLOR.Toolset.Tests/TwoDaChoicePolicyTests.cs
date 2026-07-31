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
        [TestCase("User002")]
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

        [Test]
        public void DoorChoicesExcludeSentinelsAndStructurallyIncompleteRows()
        {
            var scratch = Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                $"door-choice-{Guid.NewGuid():N}");
            Directory.CreateDirectory(scratch);
            try
            {
                File.WriteAllText(
                    Path.Combine(scratch, "doortypes.2da"),
                    "2DA V2.0\r\n\r\nLabel Model TileSet TemplateResRef StringRefGame BlockSight VisibleModel SoundAppType\r\n" +
                    "0 RealDoor real_model TST01 real_template 123 1 1 1\r\n" +
                    "1 Reserved47 **** **** **** **** **** **** ****\r\n" +
                    "2 User002 **** **** **** **** **** **** ****\r\n" +
                    "3 MissingModel **** TST01 missing_model 123 1 1 1\r\n" +
                    "4 MissingStringRef another_model TST01 missing_strref **** 1 1 1\r\n");
                File.WriteAllText(
                    Path.Combine(scratch, "genericdoors.2da"),
                    "2DA V2.0\r\n\r\nLabel StrRef ModelName BlockSight VisibleModel SoundAppType Name\r\n" +
                    "0 GenericDoor 123 generic_model 1 1 1 123\r\n" +
                    "1 User002 **** **** **** **** **** ****\r\n" +
                    "2 MissingModel 123 **** 1 1 1 123\r\n");
                var service = new DoorTypeService(
                    new TwoDaService(scratch),
                    new TlkService(TlkJsonFile.Parse("{\"language\":0,\"entries\":[]}")));

                service.GetAll().Should().ContainSingle()
                    .Which.Should().Be(new DoorTypeRow(0, "RealDoor", "RealDoor", "real_model"));
                service.GetGenericAll().Should().ContainSingle()
                    .Which.Should().Match<GenericDoorRow>(row =>
                        row.Id == 0 && row.Label == "GenericDoor" && row.Model == "generic_model");
            }
            finally
            {
                Directory.Delete(scratch, recursive: true);
            }
        }
    }
}
