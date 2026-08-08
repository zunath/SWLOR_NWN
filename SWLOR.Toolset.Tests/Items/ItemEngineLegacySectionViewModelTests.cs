using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Items;
using SWLOR.Toolset.Editors.Items;

namespace SWLOR.Toolset.Tests.Items
{
    /// <summary>
    /// <see cref="ItemEngineLegacySectionViewModel"/>: the flat, add-affordance-free sweep over
    /// whichever base-game engine properties (<see cref="ItemEngineLegacyCatalog"/>) an item actually
    /// carries.
    /// </summary>
    [TestFixture]
    public class ItemEngineLegacySectionViewModelTests
    {
        private static string AdrenHarnessPath =>
            Path.Combine(CorpusLocator.ModuleDirectory, "uti", "adren_harness.uti.json");

        // zomb_bite carries AttackBonus (56, no subtype table) and Damage (16, IPRP_DAMAGETYPE
        // subtype 1 = Physical) among its PropertiesList entries - verified against the corpus.
        private static string ZombBitePath =>
            Path.Combine(CorpusLocator.ModuleDirectory, "uti", "zomb_bite.uti.json");

        private static ItemValueStore OpenStore(string path) =>
            new(UtiDocument.Load(path).Fields);

        [Test]
        public void HasEntriesIsFalseWhenTheItemCarriesNoEngineLegacyProperty()
        {
            var section = new ItemEngineLegacySectionViewModel(
                OpenStore(AdrenHarnessPath), (_, mutation) => { mutation(); return true; });

            section.HasEntries.Should().BeFalse();
            section.Entries.Should().BeEmpty();
        }

        [Test]
        public void CorpusItemWithEngineLegacyPropertiesShowsThemWithResolvedLabels()
        {
            var section = new ItemEngineLegacySectionViewModel(
                OpenStore(ZombBitePath), (_, mutation) => { mutation(); return true; },
                resolveSubtypeChoices: key => key == "item.subtypes:IPRP_DAMAGETYPE"
                    ? new[] { new BehaviorChoice(1, "Physical") }
                    : Array.Empty<BehaviorChoice>());

            section.HasEntries.Should().BeTrue();
            section.Entries.Should().Contain(entry => entry.SubtypeDisplay == "AttackBonus");
            section.Entries.Should().Contain(entry => entry.SubtypeDisplay == "Damage (Physical)");
        }

        [Test]
        public void EditingAnEntryRoundTripsAndRemovingOneRebuildsTheRowSet()
        {
            var store = OpenStore(ZombBitePath);
            var section = new ItemEngineLegacySectionViewModel(store, (_, mutation) => { mutation(); return true; });

            var attackBonus = section.Entries.Single(entry => entry.SubtypeDisplay == "AttackBonus");
            attackBonus.Number = 9;
            store.GetPropertyValue(56, 0).Should().Be(9);

            var countBefore = section.Entries.Count;
            attackBonus.RemoveCommand.Execute(null);

            store.GetPropertyValue(56, 0).Should().BeNull();
            section.Entries.Should().HaveCount(countBefore - 1);
            section.Entries.Should().NotContain(entry => entry.SubtypeDisplay == "AttackBonus");
            section.HasEntries.Should().BeTrue("Damage and the OnHit rows are still present");
        }

        [Test]
        public void FractionalEntryValueIsRefusedRatherThanTruncated()
        {
            var store = OpenStore(ZombBitePath);
            var section = new ItemEngineLegacySectionViewModel(
                store, (_, mutation) => { mutation(); return true; });
            var attackBonus = section.Entries.Single(entry => entry.SubtypeDisplay == "AttackBonus");
            var original = attackBonus.Number;

            attackBonus.Number = original + 0.5m;

            attackBonus.Number.Should().Be(original);
            store.GetPropertyValue(56, 0).Should().Be((int?)original);
        }
    }
}
