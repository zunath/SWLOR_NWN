using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editors.Items;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Tests.Items
{
    /// <summary>Reads and round-trip writes against a real corpus file's PropertiesList.</summary>
    [TestFixture]
    public class ItemValueStoreTests
    {
        private static string AdrenHarnessPath =>
            Path.Combine(CorpusLocator.ModuleDirectory, "uti", "adren_harness.uti.json");

        [Test]
        public void GetPropertyValue_ReadsEveryEntryStoredInTheCorpusFile()
        {
            var store = new ItemValueStore(UtiDocument.Load(AdrenHarnessPath).Fields);

            store.GetPropertyValue(94, 1).Should().Be(26, "subtype 1 is Physical defense");
            store.GetPropertyValue(94, 2).Should().Be(21, "subtype 2 is Force defense");
            store.GetPropertyValue(90, -1).Should().Be(92, "HP has no subtype table");
            store.GetPropertyValue(90, 0).Should().Be(92, "-1 and 0 both mean 'no subtype' here");
            store.GetPropertyValue(120, -1).Should().Be(1, "STMRegen has no subtype table");
            store.GetPropertyValue(131, 6).Should().Be(45, "subtype 6 is the Armor skill");

            store.GetPropertyValue(999, -1).Should().BeNull();
            store.HasProperty(94).Should().BeTrue();
            store.HasProperty(999).Should().BeFalse();
            store.Properties.Should().HaveCount(5);
        }

        [Test]
        public void SetPropertyValue_UpdatesAnExistingEntryWithoutDisturbingItsSiblingSubtype()
        {
            var store = new ItemValueStore(UtiDocument.Load(AdrenHarnessPath).Fields);

            store.SetPropertyValue(94, 1, 35, 30);

            store.GetPropertyValue(94, 1).Should().Be(30);
            store.GetPropertyValue(94, 2).Should().Be(21, "updating one subtype must not disturb the other");
            store.Properties.Should().HaveCount(5);
        }

        [Test]
        public void SetPropertyValue_AddsANewEntryWithTheCorpusFieldTypesAndSentinels()
        {
            var document = UtiDocument.Load(AdrenHarnessPath);
            var store = new ItemValueStore(document.Fields);

            store.SetPropertyValue(133, 1, 54, 10);

            store.GetPropertyValue(133, 1).Should().Be(10);
            store.HasProperty(133).Should().BeTrue();
            store.Properties.Should().HaveCount(6);

            var reloaded = UtiDocument.Parse(document.ToBytes());
            var newEntry = reloaded.PropertiesList.Single(entry =>
                entry.Get("PropertyName").GetInteger() == 133);

            newEntry.Get("PropertyName").Type.Should().Be(GffFieldType.Word);
            newEntry.Get("Subtype").Type.Should().Be(GffFieldType.Word);
            newEntry.Get("Subtype").GetInteger().Should().Be(1);
            newEntry.Get("CostTable").Type.Should().Be(GffFieldType.Byte);
            newEntry.Get("CostTable").GetInteger().Should().Be(54);
            newEntry.Get("CostValue").Type.Should().Be(GffFieldType.Word);
            newEntry.Get("CostValue").GetInteger().Should().Be(10);
            newEntry.Get("Param1").Type.Should().Be(GffFieldType.Byte);
            newEntry.Get("Param1").GetInteger().Should().Be(255);
            newEntry.Get("Param1Value").Type.Should().Be(GffFieldType.Byte);
            newEntry.Get("Param1Value").GetInteger().Should().Be(0);
            newEntry.Get("ChanceAppear").Type.Should().Be(GffFieldType.Byte);
            newEntry.Get("ChanceAppear").GetInteger().Should().Be(100);

            new ItemValueStore(reloaded.Fields).GetPropertyValue(133, 1).Should().Be(10);
        }

        [Test]
        public void SetPropertyValue_OnlyNullRemovesTheEntry()
        {
            var store = new ItemValueStore(UtiDocument.Load(AdrenHarnessPath).Fields);

            store.SetPropertyValue(94, 1, 35, null);
            store.HasProperty(94).Should().BeTrue("Force defense (subtype 2) is untouched");
            store.GetPropertyValue(94, 1).Should().BeNull();
            store.Properties.Should().HaveCount(4);

            store.SetPropertyValue(120, -1, 45, 0);
            store.HasProperty(120).Should().BeTrue(
                "zero is a real stored CostValue - subtype-keyed properties like WeaponDamageType " +
                "legitimately store 0, so only null may remove");
            store.GetPropertyValue(120, -1).Should().Be(0);
            store.Properties.Should().HaveCount(4);
        }

        [Test]
        public void SetExclusiveProperty_WritesACostValueZeroEntry()
        {
            var store = new ItemValueStore(UtiDocument.Load(AdrenHarnessPath).Fields);

            store.SetExclusiveProperty(134, 3, 0);

            store.HasProperty(134).Should().BeTrue();
            store.GetPropertyValue(134, 3).Should().Be(0, "WeaponDamageType's real stored value is 0, not absent");
        }

        [Test]
        public void SetExclusiveProperty_ReplacesAnyExistingEntryOfThatPropertyRatherThanAddingASecondOne()
        {
            var store = new ItemValueStore(UtiDocument.Load(AdrenHarnessPath).Fields);
            store.SetExclusiveProperty(134, 3, 0);

            store.SetExclusiveProperty(134, 5, 0);

            store.Properties.Count(p => p.PropertyId == 134).Should().Be(1);
            store.GetPropertyValue(134, 5).Should().Be(0);
            store.GetPropertyValue(134, 3).Should().BeNull("the old subtype's entry is gone, not left behind");
        }

        [Test]
        public void ClearProperty_RemovesEveryEntryOfThatPropertyRegardlessOfSubtype()
        {
            var store = new ItemValueStore(UtiDocument.Load(AdrenHarnessPath).Fields);
            store.SetExclusiveProperty(134, 3, 0);

            store.ClearProperty(134);

            store.HasProperty(134).Should().BeFalse();
        }
    }
}
