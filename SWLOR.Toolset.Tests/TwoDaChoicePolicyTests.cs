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
        [TestCase("*****")]
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
        [TestCase("(Null Human)")]
        public void PlaceholderLabelsAreNeverSelectable(string label)
        {
            TwoDaChoicePolicy.IsSelectableLabel(label).Should().BeFalse();
        }

        [TestCase("Human")]
        [TestCase("Outsider")]
        [TestCase("Nullifier")]
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
                    "4 MissingStringRef another_model TST01 missing_strref **** 1 1 1\r\n" +
                    "5 MissingVisibility another_model TST01 missing_visibility 124 1 **** 1\r\n");
                File.WriteAllText(
                    Path.Combine(scratch, "genericdoors.2da"),
                    "2DA V2.0\r\n\r\nLabel StrRef ModelName BlockSight VisibleModel SoundAppType Name\r\n" +
                    "0 GenericDoor 123 generic_model 1 1 1 123\r\n" +
                    "1 User002 **** **** **** **** **** ****\r\n" +
                    "2 MissingModel 123 **** 1 1 1 123\r\n" +
                    "3 Transition 124 transition_model 0 0 **** 124\r\n" +
                    "4 MissingVisibility 125 missing_visibility 1 **** 1 125\r\n");
                var service = new DoorTypeService(
                    new TwoDaService(scratch),
                    new TlkService(TlkJsonFile.Parse("{\"language\":0,\"entries\":[]}")));

                service.GetAll().Should().ContainSingle()
                    .Which.Should().Be(new DoorTypeRow(0, "RealDoor", "RealDoor", "real_model"));
                service.GetGenericAll().Should().HaveCount(2);
                service.GetGeneric(0).Should().Match<GenericDoorRow>(row =>
                    row.Label == "GenericDoor" && row.Model == "generic_model" && row.VisibleModel);
                service.GetGeneric(3).Should().Match<GenericDoorRow>(row =>
                    row.Label == "Transition" && row.Model == "transition_model" && !row.VisibleModel);
            }
            finally
            {
                Directory.Delete(scratch, recursive: true);
            }
        }

        [Test]
        public void DoorChoicesFailClosedWhenVisibilityColumnIsMissing()
        {
            var scratch = Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                $"door-visibility-{Guid.NewGuid():N}");
            Directory.CreateDirectory(scratch);
            try
            {
                File.WriteAllText(
                    Path.Combine(scratch, "doortypes.2da"),
                    "2DA V2.0\r\n\r\nLabel Model StringRefGame\r\n" +
                    "0 RealDoor real_model 123\r\n");
                File.WriteAllText(
                    Path.Combine(scratch, "genericdoors.2da"),
                    "2DA V2.0\r\n\r\nLabel ModelName Name\r\n" +
                    "0 GenericDoor generic_model 123\r\n");
                var service = new DoorTypeService(
                    new TwoDaService(scratch),
                    new TlkService(TlkJsonFile.Parse("{\"language\":0,\"entries\":[]}")));

                service.GetAll().Should().BeEmpty();
                service.GetGenericAll().Should().BeEmpty();
            }
            finally
            {
                Directory.Delete(scratch, recursive: true);
            }
        }

        [Test]
        public void RequiredValidityColumnsRejectSentinelValues()
        {
            var scratch = Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                $"two-da-required-{Guid.NewGuid():N}");
            Directory.CreateDirectory(scratch);
            try
            {
                File.WriteAllText(
                    Path.Combine(scratch, "soundset.2da"),
                    "2DA V2.0\r\n\r\nLABEL STRREF RESREF\r\n" +
                    "0 Working 123 real_soundset\r\n" +
                    "1 LooksReal 124 ****\r\n");
                var service = new TwoDaLookupService(
                    new TwoDaService(scratch),
                    new TlkService(TlkJsonFile.Parse("{\"language\":0,\"entries\":[]}")));

                service.GetRows(TwoDaLookupTables.SoundSet).Should().ContainSingle()
                    .Which.Id.Should().Be(0);
            }
            finally
            {
                Directory.Delete(scratch, recursive: true);
            }
        }

        [Test]
        public void SpecializedLookupsApplyTheSharedPolicyAndRequiredMetadata()
        {
            var scratch = Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                $"specialized-two-da-choice-{Guid.NewGuid():N}");
            Directory.CreateDirectory(scratch);
            try
            {
                File.WriteAllText(
                    Path.Combine(scratch, "appearance.2da"),
                    "2DA V2.0\r\n\r\nLABEL STRING_REF NAME RACE MODELTYPE PORTRAIT\r\n" +
                    "0 RealCreature **** real_name c_real F real_portrait\r\n" +
                    "1 Bio_reserved 123 reserved_name c_reserved F reserved_portrait\r\n" +
                    "2 MissingModel **** missing_name **** F missing_portrait\r\n" +
                    "3 \"(Null Human)\" **** null_human c_invsguy F ****\r\n");
                File.WriteAllText(
                    Path.Combine(scratch, "placeables.2da"),
                    "2DA V2.0\r\n\r\nLabel StrRef ModelName\r\n" +
                    "0 RealPlaceable **** plc_real\r\n" +
                    "1 DELETED 123 plc_deleted\r\n" +
                    "2 MissingModel **** ****\r\n");
                File.WriteAllText(
                    Path.Combine(scratch, "waypoint.2da"),
                    "2DA V2.0\r\n\r\nLABEL RESREF STRREF\r\n" +
                    "0 RealWaypoint wpt_real ****\r\n" +
                    "1 USER wpt_user 123\r\n" +
                    "2 MissingModel **** ****\r\n");
                File.WriteAllText(
                    Path.Combine(scratch, "portraits.2da"),
                    "2DA V2.0\r\n\r\nBaseResRef Sex Race InanimateType\r\n" +
                    "0 portrait_real 0 0 ****\r\n" +
                    "1 NULL6 0 0 ****\r\n");
                File.WriteAllText(
                    Path.Combine(scratch, "ambientsound.2da"),
                    "2DA V2.0\r\n\r\nDescription Resource DisplayName\r\n" +
                    "0 **** sound_real ****\r\n" +
                    "1 123 UNUSED_42 ****\r\n");
                File.WriteAllText(
                    Path.Combine(scratch, "baseitems.2da"),
                    "2DA V2.0\r\n\r\nlabel Name ItemClass ModelType StorePanel EquipableSlots\r\n" +
                    "0 RealItem **** realclass 0 4 0\r\n" +
                    "1 INVALID_ITEM 123 invalidclass 0 4 0\r\n" +
                    "2 MissingClass **** **** 0 4 0\r\n");

                var twoDa = new TwoDaService(scratch);
                var tlk = new TlkService(TlkJsonFile.Parse("{\"language\":0,\"entries\":[]}"));

                new AppearanceService(twoDa, tlk).GetAll().Select(row => row.Label)
                    .Should().Equal("RealCreature");
                new PlaceableAppearanceService(twoDa, tlk).GetAll().Select(row => row.Label)
                    .Should().Equal("RealPlaceable");
                new WaypointAppearanceService(twoDa, tlk).GetAll().Select(row => row.Label)
                    .Should().Equal("RealWaypoint");
                new PortraitService(twoDa).GetAll().Select(row => row.BaseResRef)
                    .Should().Equal("portrait_real");
                new SoundService(twoDa, tlk).GetAll().Select(row => row.Resource)
                    .Should().Equal("sound_real");
                new BaseItemRowService(twoDa).All.Select(row => row.Label)
                    .Should().Equal("RealItem");
            }
            finally
            {
                Directory.Delete(scratch, recursive: true);
            }
        }

        [Test]
        public void PlaceableRenderLookupRetainsModelBearingRowsWithBlankLabels()
        {
            var scratch = Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                $"placeable-render-{Guid.NewGuid():N}");
            Directory.CreateDirectory(scratch);
            try
            {
                File.WriteAllText(
                    Path.Combine(scratch, "placeables.2da"),
                    "2DA V2.0\r\n\r\nLabel StrRef ModelName\r\n" +
                    "0 Named **** plc_named\r\n" +
                    "1 **** **** plc_unlabeled\r\n");
                var service = new PlaceableAppearanceService(
                    new TwoDaService(scratch),
                    new TlkService(TlkJsonFile.Parse("{\"language\":0,\"entries\":[]}")));

                service.GetAll().Select(row => row.Id).Should().Equal(0);
                service.TryGet(1, out var renderRow).Should().BeTrue();
                renderRow.ModelName.Should().Be("plc_unlabeled");
                renderRow.DisplayName.Should().Be("plc_unlabeled");
            }
            finally
            {
                Directory.Delete(scratch, recursive: true);
            }
        }

        [Test]
        public void MissingOrMalformedDoorTablesFailClosed()
        {
            var scratch = Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                $"door-missing-{Guid.NewGuid():N}");
            Directory.CreateDirectory(scratch);
            try
            {
                File.WriteAllText(Path.Combine(scratch, "doortypes.2da"), "not a 2da");
                var service = new DoorTypeService(
                    new TwoDaService(scratch),
                    new TlkService(TlkJsonFile.Parse("{\"language\":0,\"entries\":[]}")));

                service.GetAll().Should().BeEmpty();
                service.GetGenericAll().Should().BeEmpty();
            }
            finally
            {
                Directory.Delete(scratch, recursive: true);
            }
        }
    }
}
