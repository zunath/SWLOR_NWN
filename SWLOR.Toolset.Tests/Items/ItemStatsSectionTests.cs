using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Items;
using SWLOR.Toolset.Domain.GameData.TwoDa;
using SWLOR.Toolset.Editors.Behaviors;
using SWLOR.Toolset.Editors.Items;

namespace SWLOR.Toolset.Tests.Items
{
    /// <summary>The Stats tab: family/role-driven group visibility, cell values, and write-back.</summary>
    [TestFixture]
    public class ItemStatsSectionTests
    {
        private static string AdrenHarnessPath =>
            Path.Combine(CorpusLocator.ModuleDirectory, "uti", "adren_harness.uti.json");

        private static string Sw2DaDirectory =>
            Path.Combine(CorpusLocator.RepositoryRoot, "SWLOR_Haks", "sw_2da");

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
            defense.Cells.Single(cell => cell.Label == "Physical Defense").Number.Should().Be(26);
            defense.Cells.Single(cell => cell.Label == "Force Defense").Number.Should().Be(21);

            var vitals = section.Groups.Single(group => group.Group == ItemStatGroup.Vitals);
            vitals.Cells.Single(cell => cell.Label == "HP").Number.Should().Be(92);
            vitals.Cells.Single(cell => cell.Label == "STM Regen").Number.Should().Be(1);

            var resistance = section.Groups.Single(group => group.Group == ItemStatGroup.Resistance);
            resistance.Cells.Single(cell => cell.Label == "Fire Resistance").Number.Should().BeNull();
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

            fireResistance.Number = 12;
            store.GetPropertyValue(133, 1).Should().Be(12);

            fireResistance.Number = null;
            store.GetPropertyValue(133, 1).Should().BeNull("a cleared box removes the property");

            fireResistance.Number = 12;
            store.GetPropertyValue(133, 1).Should().Be(12);

            fireResistance.Number = 0;
            store.GetPropertyValue(133, 1).Should().Be(0,
                "zero is a real stored CostValue now - only clearing the box removes the property");
        }

        [Test]
        public void ArmorShowsNeitherEnhancementLevelNorWeaponDamageType()
        {
            // Corpus-verified: of EnhancementLevel's 812 entries and WeaponDamageType's 311, not
            // one sits on an armor - they belong to enhancement modules and weapons respectively.
            var section = OpenArmorSection(OpenStore());

            var combat = section.Groups.SingleOrDefault(group => group.Group == ItemStatGroup.Combat);
            if (combat == null)
                return;

            combat.Cells.Should().NotContain(cell => cell.Label == "Enhancement Level");
            combat.ExclusiveChoices.Should().NotContain(choice => choice.Label == "Weapon Damage Type");
        }

        [Test]
        public void AWeaponStillShowsBoth()
        {
            var section = new ItemStatsSectionViewModel(
                OpenStore(), (_, mutation) => { mutation(); return true; });
            section.Rebuild(ItemFamily.MeleeWeapon, ItemRoleCatalog.CustomId);

            var combat = section.Groups.Single(group => group.Group == ItemStatGroup.Combat);
            combat.Cells.Should().Contain(cell => cell.Label == "Enhancement Level");
            combat.ExclusiveChoices.Should().Contain(choice => choice.Label == "Weapon Damage Type");
        }

        [Test]
        public void GroupsAreDealtIntoTwoBalancedColumns()
        {
            var section = OpenArmorSection(OpenStore());

            var columns = section.LeftColumn.Concat(section.RightColumn).ToList();
            columns.Should().BeEquivalentTo(section.Groups, "every group lands in exactly one column");

            static int Rows(IEnumerable<ItemStatGroupViewModel> column) =>
                column.Sum(group => group.Cells.Count + group.EntryLists.Count + group.ExclusiveChoices.Count);

            var tallest = Math.Max(Rows(section.LeftColumn), Rows(section.RightColumn));
            var shortest = Math.Min(Rows(section.LeftColumn), Rows(section.RightColumn));
            (tallest - shortest).Should().BeLessThan(tallest,
                "the columns are packed independently rather than one holding everything");
        }

        [Test]
        public void SecondaryGroupsAreSimplyNotShown()
        {
            var section = OpenArmorSection(OpenStore());

            // There is no "not used by this base type" section anymore - a stat outside the
            // family's primary groups is simply absent from Groups, with nothing to expand.
            section.Groups.Should().NotContain(group => group.Group == ItemStatGroup.Crafting);
            section.Groups.Should().NotContain(group => group.Group == ItemStatGroup.Droid);
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
        public void WeaponCombatGroupExposesTheWeaponDamageTypeExclusiveChoice()
        {
            var section = new ItemStatsSectionViewModel(
                OpenStore(), (_, mutation) => { mutation(); return true; });

            section.Rebuild(ItemFamily.MeleeWeapon, ItemRoleCatalog.CustomId);

            var combat = section.Groups.Single(group => group.Group == ItemStatGroup.Combat);
            combat.ExclusiveChoices.Select(choice => choice.Label).Should().BeEquivalentTo(new[] { "Weapon Damage Type" });
            combat.Cells.Should().NotContain(cell => cell.Label.StartsWith("Weapon Damage Type"));
            combat.Cells.Should().NotContain(cell => cell.Label.StartsWith("Damage Stat"));
        }

        [Test]
        public void WeaponDamageTypeExclusiveChoicePicksWritesAndClears()
        {
            var store = OpenStore();
            var section = new ItemStatsSectionViewModel(store, (_, mutation) => { mutation(); return true; });
            section.Rebuild(ItemFamily.MeleeWeapon, ItemRoleCatalog.CustomId);

            var choice = section.Groups.Single(group => group.Group == ItemStatGroup.Combat)
                .ExclusiveChoices.Single(c => c.Label == "Weapon Damage Type");

            choice.Selected.Should().Be(choice.Options[0], "nothing is stored yet, so 'none' is selected");

            var fire = new BehaviorChoiceViewModel(new BehaviorChoice(3, "Fire"));
            var electrical = new BehaviorChoiceViewModel(new BehaviorChoice(5, "Electrical"));
            var withRealOptions = new ItemStatGroupViewModel(
                ItemStatGroup.Combat, ItemStatVisibility.CombatStatsFor(ItemFamily.MeleeWeapon), store,
                (_, mutation) => { mutation(); return true; }, null, null,
                new[]
                {
                    new ItemExclusiveChoiceViewModel(
                        ItemMultiEntryCatalog.ByPropertyId(134)!, store,
                        (_, mutation) => { mutation(); return true; }, new[] { fire.Choice, electrical.Choice })
                });
            var realChoice = withRealOptions.ExclusiveChoices.Single();

            realChoice.Selected = realChoice.Options.Single(o => o.Value == 3);
            store.GetPropertyValue(134, 3).Should().Be(0, "WeaponDamageType always stores CostValue 0");

            realChoice.Selected = realChoice.Options.Single(o => o.Value == 5);
            store.GetPropertyValue(134, 3).Should().BeNull("switching subtype clears the old one, not adds a second");
            store.GetPropertyValue(134, 5).Should().Be(0);

            realChoice.Selected = realChoice.Options[0];
            store.HasProperty(134).Should().BeFalse("the leading 'none' option clears the property");
        }

        [Test]
        public void MaximumReflectsTheRealCostTableWhenOneIsSupplied()
        {
            var ranges = new ItemCostTableRanges(new TwoDaService(Sw2DaDirectory));
            var section = new ItemStatsSectionViewModel(
                OpenStore(), (_, mutation) => { mutation(); return true; }, costTableMax: ranges.MaxFor);
            section.Rebuild(ItemFamily.Armor, ItemRoleCatalog.CustomId);

            var defense = section.Groups.Single(group => group.Group == ItemStatGroup.Defense);
            var physicalDefense = defense.Cells.Single(cell => cell.Label == "Physical Defense");

            // Defense's CostTableId is 35 -> iprp_defense.2da, whose highest row is 1000.
            physicalDefense.Maximum.Should().Be(1000);
        }

        [Test]
        public void MaximumFallsBackToTheDefaultWhenNoCostTableResolverIsSupplied()
        {
            var section = OpenArmorSection(OpenStore());

            var defense = section.Groups.Single(group => group.Group == ItemStatGroup.Defense);
            defense.Cells.Single(cell => cell.Label == "Physical Defense").Maximum
                .Should().Be(ItemCostTableRanges.DefaultMax);
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
