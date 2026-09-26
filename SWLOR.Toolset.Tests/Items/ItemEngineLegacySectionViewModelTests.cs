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

        // zomb_bite carries Damage (16, IPRP_DAMAGETYPE subtype 1 = Physical) and OnHit (48) among
        // its PropertiesList entries - verified against the corpus.
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
            section.Entries.Should().Contain(entry => entry.SubtypeDisplay == "Damage (Physical)");
            section.Entries.Should().Contain(entry => entry.SubtypeDisplay.StartsWith("OnHit"));
        }

        [Test]
        public void EditingAnEntryRoundTripsAndRemovingOneRebuildsTheRowSet()
        {
            var store = OpenStore(ZombBitePath);
            var section = new ItemEngineLegacySectionViewModel(
                store, (_, mutation) => { mutation(); return true; },
                resolveSubtypeChoices: key => key == "item.subtypes:IPRP_DAMAGETYPE"
                    ? new[] { new BehaviorChoice(1, "Physical") }
                    : Array.Empty<BehaviorChoice>());

            var damage = section.Entries.Single(entry => entry.SubtypeDisplay == "Damage (Physical)");
            damage.Number = 9;
            store.GetPropertyValue(16, 1).Should().Be(9);

            var countBefore = section.Entries.Count;
            damage.RemoveCommand.Execute(null);

            store.GetPropertyValue(16, 1).Should().BeNull();
            section.Entries.Should().HaveCount(countBefore - 1);
            section.Entries.Should().NotContain(entry => entry.SubtypeDisplay == "Damage (Physical)");
            section.HasEntries.Should().BeTrue("the OnHit rows are still present");
        }

        [Test]
        public void FractionalEntryValueIsRefusedRatherThanTruncated()
        {
            var store = OpenStore(ZombBitePath);
            var section = new ItemEngineLegacySectionViewModel(
                store, (_, mutation) => { mutation(); return true; },
                resolveSubtypeChoices: key => key == "item.subtypes:IPRP_DAMAGETYPE"
                    ? new[] { new BehaviorChoice(1, "Physical") }
                    : Array.Empty<BehaviorChoice>());
            var damage = section.Entries.Single(entry => entry.SubtypeDisplay == "Damage (Physical)");
            var original = damage.Number;

            damage.Number = original + 0.5m;

            damage.Number.Should().Be(original);
            store.GetPropertyValue(16, 1).Should().Be((int?)original);
        }
    }
}
