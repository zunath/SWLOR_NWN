using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Items;
using SWLOR.Toolset.Editors.Behaviors;
using SWLOR.Toolset.Editors.Items;

namespace SWLOR.Toolset.Tests.Items
{
    /// <summary>
    /// <see cref="ItemPropertyEntryListViewModel"/> and <see cref="ItemPropertyEntryViewModel"/> in
    /// isolation, against fake subtype choices rather than a real 2da - the add/remove/write-back
    /// mechanics themselves are what is under test here, not any one property's real subtype table.
    /// </summary>
    [TestFixture]
    public class ItemPropertyEntryListTests
    {
        // adren_harness carries Physical/Force Defense, HP, STMRegen, and an Armor skill requirement -
        // no FoodBonus (106) of any subtype - so it is a clean slate for exercising the FoodBonus list.
        private static string AdrenHarnessPath =>
            Path.Combine(CorpusLocator.ModuleDirectory, "uti", "adren_harness.uti.json");

        private static ItemValueStore OpenStore() =>
            new(UtiDocument.Load(AdrenHarnessPath).Fields);

        private static readonly IReadOnlyList<BehaviorChoice> FakeFoodTypes = new[]
        {
            new BehaviorChoice(5, "Spice Cake"),
            new BehaviorChoice(9, "Bantha Steak")
        };

        private static ItemPropertyEntryListViewModel OpenFoodBonusList(ItemValueStore store) =>
            new(ItemMultiEntryCatalog.ByPropertyId(106)!, store, (_, mutation) => { mutation(); return true; },
                FakeFoodTypes);

        [Test]
        public void WatermarkFallsBackToTheLabelWhenNoSearchNounIsDeclared()
        {
            var list = OpenFoodBonusList(OpenStore());

            list.AddWatermark.Should().Be("Search Food Bonus");
        }

        [Test]
        public void AddingAChoiceWritesTheStoreAddsAnEntryAndDropsItFromAddChoices()
        {
            var store = OpenStore();
            var list = OpenFoodBonusList(store);

            list.AddChoices.Select(choice => (int)choice.Value).Should().BeEquivalentTo(new[] { 5, 9 });

            list.AddCommand.Execute(new BehaviorChoiceViewModel(FakeFoodTypes[0]));

            store.GetPropertyValue(106, 5).Should().Be(1, "AddCommand writes an initial value of 1");
            list.Entries.Should().ContainSingle(entry => entry.SubtypeId == 5);
            list.Entries.Single(entry => entry.SubtypeId == 5).SubtypeDisplay.Should().Be("Spice Cake");

            list.AddChoices.Select(choice => (int)choice.Value).Should().BeEquivalentTo(new[] { 9 },
                "subtype 5 is already present and must not be offered again");
        }

        [Test]
        public void AddingANoCostPropertyNormalizesItsStoredTableAndValue()
        {
            var document = UtiDocument.Load(AdrenHarnessPath);
            var store = new ItemValueStore(document.Fields);
            var choices = new[] { new BehaviorChoice(2, "Astromech") };
            var list = new ItemPropertyEntryListViewModel(
                ItemMultiEntryCatalog.ByPropertyId(122)!, store,
                (_, mutation) => { mutation(); return true; }, choices);

            list.AddCommand.Execute(new BehaviorChoiceViewModel(choices[0]));

            store.GetPropertyValue(122, 2).Should().Be(0);
            var added = document.PropertiesList.Single(entry =>
                entry.Get("PropertyName").GetInteger() == 122 &&
                entry.Get("Subtype").GetInteger() == 2);
            added.Get("CostTable").GetInteger().Should().Be(0,
                "Aurora stores zero when itempropdef.2da declares no cost table");
            added.Get("CostValue").GetInteger().Should().Be(0,
                "a no-cost property is a subtype marker rather than a numeric value");

            var row = list.Entries.Single();
            row.HasEditableValue.Should().BeFalse();
            row.Number = 7;
            row.Number.Should().Be(0);
            store.GetPropertyValue(122, 2).Should().Be(0);
        }

        [Test]
        public void RemoveCommandClearsTheStoreAndDropsTheEntry()
        {
            var store = OpenStore();
            var list = OpenFoodBonusList(store);
            list.AddCommand.Execute(new BehaviorChoiceViewModel(FakeFoodTypes[0]));

            var entry = list.Entries.Single(e => e.SubtypeId == 5);
            entry.RemoveCommand.Execute(null);

            store.GetPropertyValue(106, 5).Should().BeNull();
            list.Entries.Should().BeEmpty();
            list.AddChoices.Select(choice => (int)choice.Value).Should().BeEquivalentTo(new[] { 5, 9 },
                "removing the entry puts its subtype back on offer");
        }

        [Test]
        public void EditingAnEntrysNumberRoundTrips()
        {
            var store = OpenStore();
            var list = OpenFoodBonusList(store);
            list.AddCommand.Execute(new BehaviorChoiceViewModel(FakeFoodTypes[0]));
            var entry = list.Entries.Single(e => e.SubtypeId == 5);

            entry.Number = 7;

            store.GetPropertyValue(106, 5).Should().Be(7);
            entry.Number.Should().Be(7);
            list.Entries.Should().ContainSingle();
        }

        [Test]
        public void ReloadRebuildsEntriesFromWhateverTheStoreHoldsNow()
        {
            var store = OpenStore();
            var list = OpenFoodBonusList(store);

            store.SetPropertyValue(106, 9, 45, 4);
            list.Entries.Should().BeEmpty("nothing has told the list to re-read the store yet");

            list.Reload();

            list.Entries.Should().ContainSingle(entry => entry.SubtypeId == 9 && entry.Number == 4);
        }
    }
}
