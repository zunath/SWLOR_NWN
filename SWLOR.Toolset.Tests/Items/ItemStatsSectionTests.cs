using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editors.Items;
using SWLOR.Toolset.Editors.Items;

namespace SWLOR.Toolset.Tests.Items
{
    /// <summary>The Stats tab: family/role-driven group visibility, cell values, and write-back.</summary>
    [TestFixture]
    public class ItemStatsSectionTests
    {
        private static string AdrenHarnessPath =>
            Path.Combine(CorpusLocator.ModuleDirectory, "uti", "adren_harness.uti.json");

        private static ItemValueStore OpenStore() =>
            new(UtiDocument.Load(AdrenHarnessPath).Fields);

        private static ItemStatsSectionViewModel OpenArmorSection(ItemValueStore store)
        {
            var section = new ItemStatsSectionViewModel(store, (_, mutation) => { mutation(); return true; });
            section.Rebuild(ItemFamily.Armor, ItemRoleCatalog.CustomId);
            return section;
        }

        [Test]
        public void ArmorPrimariesMatchTheWornArmorSet()
        {
            var section = OpenArmorSection(OpenStore());

            section.Groups.Select(group => group.Group).Should().BeEquivalentTo(new[]
            {
                ItemStatGroup.Defense, ItemStatGroup.Resistance, ItemStatGroup.Vitals,
                ItemStatGroup.Combat, ItemStatGroup.Utility
            });
        }

        [Test]
        public void DefenseAndVitalsShowTheStoredValuesAndAnAbsentStatIsBlank()
        {
            var section = OpenArmorSection(OpenStore());

            var defense = section.Groups.Single(group => group.Group == ItemStatGroup.Defense);
            defense.Cells.Single(cell => cell.Label == "Physical Defense").Value.Should().Be("26");
            defense.Cells.Single(cell => cell.Label == "Force Defense").Value.Should().Be("21");

            var vitals = section.Groups.Single(group => group.Group == ItemStatGroup.Vitals);
            vitals.Cells.Single(cell => cell.Label == "HP").Value.Should().Be("92");
            vitals.Cells.Single(cell => cell.Label == "STM Regen").Value.Should().Be("1");

            var resistance = section.Groups.Single(group => group.Group == ItemStatGroup.Resistance);
            resistance.Cells.Single(cell => cell.Label == "Fire Resistance").Value.Should().Be(string.Empty);
        }

        [Test]
        public void CombatGroupDropsWeaponOnlyStatsForArmorButKeepsThemForAWeapon()
        {
            var section = OpenArmorSection(OpenStore());

            var armorCombat = section.Groups.Single(group => group.Group == ItemStatGroup.Combat);
            armorCombat.Cells.Should().NotContain(cell => cell.Label == "DMG");
            armorCombat.Cells.Should().NotContain(cell => cell.Label == "Delay");

            section.Rebuild(ItemFamily.MeleeWeapon, ItemRoleCatalog.CustomId);

            var weaponCombat = section.Groups.Single(group => group.Group == ItemStatGroup.Combat);
            weaponCombat.Cells.Should().Contain(cell => cell.Label == "DMG");
            weaponCombat.Cells.Should().Contain(cell => cell.Label == "Delay");
        }

        [Test]
        public void WritingAValueRoundTripsAndClearingItRemovesTheProperty()
        {
            var store = OpenStore();
            var section = OpenArmorSection(store);
            var fireResistance = section.Groups
                .Single(group => group.Group == ItemStatGroup.Resistance)
                .Cells.Single(cell => cell.Label == "Fire Resistance");

            fireResistance.Value = "12";
            store.GetPropertyValue(133, 1).Should().Be(12);

            fireResistance.Value = string.Empty;
            store.GetPropertyValue(133, 1).Should().BeNull("an empty box clears the property");

            fireResistance.Value = "12";
            store.GetPropertyValue(133, 1).Should().Be(12);

            fireResistance.Value = "0";
            store.GetPropertyValue(133, 1).Should().BeNull("zero removes the property just like the store itself does");
        }

        [Test]
        public void GarbageInputRefusesTheWriteAndRestoresTheShownValue()
        {
            var store = OpenStore();
            var section = OpenArmorSection(store);
            var fireResistance = section.Groups
                .Single(group => group.Group == ItemStatGroup.Resistance)
                .Cells.Single(cell => cell.Label == "Fire Resistance");

            fireResistance.Value = "12";

            fireResistance.Value = "abc";

            store.GetPropertyValue(133, 1).Should().Be(12, "the invalid input must never reach the store");
            fireResistance.Value.Should().Be("12", "the shown value is put back to what is actually stored");
        }

        [Test]
        public void SecondaryGroupsAreSummarizedButBuiltOnlyOnceExpanded()
        {
            var section = OpenArmorSection(OpenStore());

            section.HasSecondary.Should().BeTrue();
            section.SecondarySummary.Should().Contain("Crafting");
            section.SecondaryGroups.Should().BeEmpty("nothing is built until the builder expands it");

            section.IsSecondaryExpanded = true;

            section.SecondaryGroups.Select(group => group.Group).Should().BeEquivalentTo(new[]
            {
                ItemStatGroup.Crafting, ItemStatGroup.Bonuses, ItemStatGroup.Droid,
                ItemStatGroup.Incubation, ItemStatGroup.Npc, ItemStatGroup.Enhancements
            });
        }

        [Test]
        public void MiscellaneousWithDroidPartRoleUnlocksTheDroidGroup()
        {
            var section = new ItemStatsSectionViewModel(
                OpenStore(), (_, mutation) => { mutation(); return true; });

            section.Rebuild(ItemFamily.Miscellaneous, ItemRoleCatalog.DroidPartId);

            section.Groups.Select(group => group.Group).Should().BeEquivalentTo(new[] { ItemStatGroup.Droid });
        }

        [Test]
        public void MiscellaneousWithMealRoleExposesTheFoodBonusEntryListUnderBonuses()
        {
            var section = new ItemStatsSectionViewModel(
                OpenStore(), (_, mutation) => { mutation(); return true; });

            section.Rebuild(ItemFamily.Miscellaneous, ItemRoleCatalog.MealId);

            var bonuses = section.Groups.Single(group => group.Group == ItemStatGroup.Bonuses);
            bonuses.EntryLists.Select(list => list.Label).Should().BeEquivalentTo(new[] { "Food Bonus" });
        }

        [Test]
        public void EssencePrimariesIncludeAnEnhancementsGroupWithAllSevenEnhancementLists()
        {
            var section = new ItemStatsSectionViewModel(
                OpenStore(), (_, mutation) => { mutation(); return true; });

            section.Rebuild(ItemFamily.Essence, ItemRoleCatalog.ComponentId);

            var enhancements = section.Groups.Single(group => group.Group == ItemStatGroup.Enhancements);
            enhancements.EntryLists.Select(list => list.Label).Should().BeEquivalentTo(new[]
            {
                "Armor Enhancement", "Weapon Enhancement", "Structure Enhancement", "Food Enhancement",
                "Starship Enhancement", "Module Enhancement", "Droid Enhancement"
            });
        }

        [Test]
        public void ArmorPrimariesDoNotIncludeTheEnhancementsGroup()
        {
            var section = OpenArmorSection(OpenStore());

            section.Groups.Should().NotContain(group => group.Group == ItemStatGroup.Enhancements);
        }

        [Test]
        public void EngineSectionIsBuiltOnRebuildAndReloadedByReloadFromDocument()
        {
            var section = OpenArmorSection(OpenStore());

            section.Engine.Should().NotBeNull();
            section.Engine!.HasEntries.Should().BeFalse("adren_harness carries no engine-legacy properties");

            section.ReloadFromDocument();
            section.Engine!.HasEntries.Should().BeFalse();
        }
    }
}
