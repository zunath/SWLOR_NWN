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

        private static ItemCostTableRanges OpenCostTables() =>
            new(new TwoDaService(Sw2DaDirectory));

        [Test]
        public void ReadOnlySummaryUsesTheItemEditorsStatCatalog()
        {
            var item = UtiDocument.Load(Path.Combine(
                CorpusLocator.ModuleDirectory, "uti", "vnpcrbot1.uti.json")).Fields;

            var groups = ItemStatSummary.Build(item);
            var combat = groups.Single(group => group.Title == "Combat");

            combat.Entries.Should().Contain(entry =>
                entry.Label == "DMG" && entry.Value == "6");
            combat.Entries.Should().Contain(entry =>
                entry.Label == "Delay" && entry.Value == "30");
            groups.SelectMany(group => group.Entries).Should().NotContain(entry =>
                entry.Label.Contains("ammo", StringComparison.OrdinalIgnoreCase),
                "engine-only unlimited ammunition remains behind the scenes");
            ItemStatSummary.Compact(groups).Should().Contain("DMG").And.Contain("Delay");
        }

        [Test]
        public void CompactSummaryKeepsItsRemainingStatCountSeparate()
        {
            var groups = new[]
            {
                new ItemStatSummaryGroup("Defense", new[]
                {
                    new ItemStatSummaryEntry("Force Defense", "3"),
                    new ItemStatSummaryEntry("Physical Defense", "2")
                }),
                new ItemStatSummaryGroup("Vitals", new[]
                {
                    new ItemStatSummaryEntry("FP", "1"),
                    new ItemStatSummaryEntry("FP Regen", "1"),
                    new ItemStatSummaryEntry("HP", "20"),
                    new ItemStatSummaryEntry("STM", "4")
                })
            };

            var summary = ItemStatSummary.CompactParts(groups);

            summary.Primary.Should().Contain("Force Defense 3")
                .And.Contain("FP Regen 1")
                .And.NotContain("HP 20");
            summary.Overflow.Should().Be("+2 more");
            summary.HasOverflow.Should().BeTrue();
            summary.Text.Should().EndWith("+2 more");
        }

        private static ItemStatsSectionViewModel OpenArmorSection(ItemValueStore store)
        {
            var section = new ItemStatsSectionViewModel(
                store, (_, mutation) => { mutation(); return true; }, costTables: OpenCostTables());
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
        public void FractionalAndOutOfRangeStatValuesAreRefused()
        {
            var store = OpenStore();
            var section = OpenArmorSection(store);
            var physicalDefense = section.Groups
                .Single(group => group.Group == ItemStatGroup.Defense)
                .Cells.Single(cell => cell.Label == "Physical Defense");

            physicalDefense.Number = 26.5m;
            physicalDefense.Number.Should().Be(26);
            store.GetPropertyValue(94, 1).Should().Be(26);

            physicalDefense.Number = physicalDefense.Maximum + 1m;
            physicalDefense.Number.Should().Be(26);
            store.GetPropertyValue(94, 1).Should().Be(26);
        }

        [Test]
        public void OrdinaryNumericEditDoesNotRepackTheStatColumns()
        {
            var section = OpenArmorSection(OpenStore());
            var collectionChanges = 0;
            section.LeftColumn.CollectionChanged += (_, _) => collectionChanges++;
            section.RightColumn.CollectionChanged += (_, _) => collectionChanges++;
            var physicalDefense = section.Groups
                .Single(group => group.Group == ItemStatGroup.Defense)
                .Cells.Single(cell => cell.Label == "Physical Defense");

            physicalDefense.Number = 27;

            collectionChanges.Should().Be(0, "a value edit does not change card height or membership");
        }

        [Test]
        public void ClearingTheLastStoredValueRemovesAStoredOnlyGroupImmediately()
        {
            var store = OpenStore();
            var section = new ItemStatsSectionViewModel(
                store, (_, mutation) => { mutation(); return true; }, costTables: OpenCostTables());
            section.Rebuild(ItemFamily.Miscellaneous, ItemRoleCatalog.CustomId);

            var defense = section.Groups.Single(group => group.Group == ItemStatGroup.Defense);
            defense.Cells.Single(cell => cell.Label == "Physical Defense").Number = null;
            defense.Cells.Single(cell => cell.Label == "Force Defense").Number = null;

            section.Groups.Should().NotContain(group => group.Group == ItemStatGroup.Defense);
            section.LeftColumn.Concat(section.RightColumn)
                .OfType<ItemStatGroupViewModel>()
                .Should().NotContain(group => group.Group == ItemStatGroup.Defense);
        }

        [Test]
        public void ExternalReloadRecomputesStoredOnlyGroups()
        {
            var store = OpenStore();
            var section = new ItemStatsSectionViewModel(
                store, (_, mutation) => { mutation(); return true; });
            section.Rebuild(ItemFamily.Miscellaneous, ItemRoleCatalog.CustomId);
            section.Groups.Should().Contain(group => group.Group == ItemStatGroup.Defense);

            store.ClearProperty(94);
            section.ReloadFromDocument();

            section.Groups.Should().NotContain(group => group.Group == ItemStatGroup.Defense);
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
            columns.OfType<ItemStatGroupViewModel>()
                .Should().BeEquivalentTo(section.Groups, "every group lands in exactly one column");

            // The engine sweep is dealt in alongside the groups, so a column can hold either kind.
            static int Rows(IEnumerable<object> column) =>
                column.OfType<ItemStatGroupViewModel>()
                    .Sum(group => group.Cells.Count + group.EntryLists.Count + group.ExclusiveChoices.Count)
                + column.OfType<ItemEngineLegacySectionViewModel>().Sum(engine => engine.Entries.Count + 1);

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

            // Miscellaneous has no primary groups of its own, so the role contributes Droid and the
            // rest is whatever this blueprint stores - it holds HP and STM Regen (Vitals) and both
            // defenses. Its RequiresSkill entry belongs to the Requirements tab and adds no group.
            section.Groups.Select(group => group.Group).Should().BeEquivalentTo(new[]
            {
                ItemStatGroup.Droid, ItemStatGroup.Vitals, ItemStatGroup.Defense
            });
        }

        [Test]
        public void AGroupTheFamilyWouldHideIsShownAnywayWhenTheItemStoresAValueInIt()
        {
            // Hiding a group the item has a value in would leave that value invisible but still
            // saved - so a stored value always wins over the family's usual surface.
            var section = new ItemStatsSectionViewModel(
                OpenStore(), (_, mutation) => { mutation(); return true; });

            section.Rebuild(ItemFamily.Miscellaneous, ItemRoleCatalog.CustomId);

            section.Groups.Select(group => group.Group).Should().BeEquivalentTo(new[]
            {
                ItemStatGroup.Vitals, ItemStatGroup.Defense
            });
            section.Groups.Single(group => group.Group == ItemStatGroup.Defense)
                .Cells.Should().Contain(cell => cell.Label.StartsWith("Physical Defense"));
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
        public void EveryResolvedCostTableIsOfferedAsItsRealRows()
        {
            // iprp_delay starts at row 11 and labels that row "110". A number box therefore offered
            // rows 0-10, which do not exist, and displayed a row index as though it were the delay.
            var ranges = new ItemCostTableRanges(new TwoDaService(Sw2DaDirectory));
            var section = new ItemStatsSectionViewModel(
                OpenStore(), (_, mutation) => { mutation(); return true; }, costTables: ranges);
            section.Rebuild(ItemFamily.MeleeWeapon, ItemRoleCatalog.CustomId);

            var combat = section.Groups.Single(group => group.Group == ItemStatGroup.Combat);
            var delay = combat.Cells.Single(cell => cell.Label == "Delay");

            delay.HasOptions.Should().BeTrue("Delay's rows are codes, not quantities");
            delay.Options.Should().NotContain(option => option.Value < 11, "rows 0-10 are blank");
            delay.Options.Should().Contain(option => option.Value == 11 && option.Label == "110");

            var damage = combat.Cells.Single(cell => cell.Label == "DMG");
            damage.HasOptions.Should().BeTrue("numeric ladders also list only real table rows");
            damage.Options.Should().Contain(option => option.Value == 1 && option.Label == "1");
        }

        [Test]
        public void CombatAndVitalStatsUseActualNumericOptions()
        {
            var ranges = new ItemCostTableRanges(new TwoDaService(Sw2DaDirectory));
            var section = new ItemStatsSectionViewModel(
                OpenStore(), (_, mutation) => { mutation(); return true; }, costTables: ranges);
            section.Rebuild(ItemFamily.Armor, ItemRoleCatalog.CustomId);

            var combat = section.Groups.Single(group => group.Group == ItemStatGroup.Combat);
            foreach (var label in new[] { "Attack", "Force Attack" })
            {
                var cell = combat.Cells.Single(candidate => candidate.Label == label);
                cell.HasOptions.Should().BeTrue();
                cell.Options.Should().Contain(option => option.Value == 0 && option.Label == "0");
                cell.Options.Should().Contain(option => option.Value == 100 && option.Label == "100");
            }

            var vitals = section.Groups.Single(group => group.Group == ItemStatGroup.Vitals);
            foreach (var label in new[] { "HP", "FP", "STM" })
            {
                var cell = vitals.Cells.Single(candidate => candidate.Label == label);
                cell.HasOptions.Should().BeTrue();
                cell.Options.Should().NotContain(option => option.Value == 0, "row 0 is blank in the vitality tables");
                cell.Options.Should().Contain(option => option.Value == 1 && option.Label == "1");
                cell.Options.Should().Contain(option => option.Value == 1000 && option.Label == "1000");
            }

            foreach (var label in new[] { "FP Regen", "STM Regen" })
            {
                var cell = vitals.Cells.Single(candidate => candidate.Label == label);
                cell.HasOptions.Should().BeTrue();
                cell.Options.Should().Contain(option => option.Value == 0 && option.Label == "0");
                cell.Options.Should().Contain(option => option.Value == 100 && option.Label == "100");
            }
        }

        [Test]
        public void CodedStatCanBeClearedFromItsDropdown()
        {
            var store = OpenStore();
            var ranges = new ItemCostTableRanges(new TwoDaService(Sw2DaDirectory));
            var section = new ItemStatsSectionViewModel(
                store, (_, mutation) => { mutation(); return true; }, costTables: ranges);
            section.Rebuild(ItemFamily.MeleeWeapon, ItemRoleCatalog.CustomId);
            var delay = section.Groups.Single(group => group.Group == ItemStatGroup.Combat)
                .Cells.Single(cell => cell.Label == "Delay");

            delay.SelectedOption = delay.Options.First();
            store.HasProperty(98).Should().BeTrue();

            delay.SelectedOption = delay.SelectableOptions[0];

            delay.Number.Should().BeNull();
            store.HasProperty(98).Should().BeFalse();
            delay.SelectableOptions[0].Label.Should().Contain("none");
        }

        [Test]
        public void ResistanceDropdownShowsActualAmountAndWritesEncodedCostValue()
        {
            var store = OpenStore();
            var ranges = new ItemCostTableRanges(new TwoDaService(Sw2DaDirectory));
            var section = new ItemStatsSectionViewModel(
                store, (_, mutation) => { mutation(); return true; }, costTables: ranges);
            section.Rebuild(ItemFamily.Armor, ItemRoleCatalog.CustomId);
            var fireResistance = section.Groups
                .Single(group => group.Group == ItemStatGroup.Resistance)
                .Cells.Single(cell => cell.Label == "Fire Resistance");

            var vulnerability = fireResistance.Options.Single(option => option.Label == "-1");
            vulnerability.Value.Should().Be(101, "negative amounts are encoded as non-negative 2DA row ids");

            fireResistance.SelectedOption = vulnerability;

            fireResistance.SelectedOption.Should().Be(vulnerability);
            fireResistance.Number.Should().Be(101);
            store.GetPropertyValue(133, 1).Should().Be(101);
        }

        [Test]
        public void MaximumReflectsTheRealCostTableWhenOneIsSupplied()
        {
            var ranges = new ItemCostTableRanges(new TwoDaService(Sw2DaDirectory));
            var section = new ItemStatsSectionViewModel(
                OpenStore(), (_, mutation) => { mutation(); return true; }, costTables: ranges);
            section.Rebuild(ItemFamily.Armor, ItemRoleCatalog.CustomId);

            var defense = section.Groups.Single(group => group.Group == ItemStatGroup.Defense);
            var physicalDefense = defense.Cells.Single(cell => cell.Label == "Physical Defense");

            // Defense's CostTableId is 35 -> iprp_defense.2da, whose highest row is 1000.
            physicalDefense.Maximum.Should().Be(1000);
        }

        [Test]
        public void MaximumFallsBackToTheDefaultWhenNoCostTableResolverIsSupplied()
        {
            var section = new ItemStatsSectionViewModel(
                OpenStore(), (_, mutation) => { mutation(); return true; });
            section.Rebuild(ItemFamily.Armor, ItemRoleCatalog.CustomId);

            var defense = section.Groups.Single(group => group.Group == ItemStatGroup.Defense);
            defense.Cells.Single(cell => cell.Label == "Physical Defense").Maximum
                .Should().Be(ItemCostTableRanges.DefaultMax);
        }

        [Test]
        public void GameResourceReloadRebuildsSubtypeChoicesAndCostTableModels()
        {
            IReadOnlyList<BehaviorChoice> choices = new[] { new BehaviorChoice(3, "Fire") };
            var section = new ItemStatsSectionViewModel(
                OpenStore(),
                (_, mutation) => { mutation(); return true; },
                resolveChoices: _ => choices,
                costTables: new ItemCostTableRanges(new TwoDaService(Sw2DaDirectory)));
            section.Rebuild(ItemFamily.MeleeWeapon, ItemRoleCatalog.CustomId);

            var originalCombat = section.Groups.Single(group => group.Group == ItemStatGroup.Combat);
            originalCombat.ExclusiveChoices.Single().Options.Should().Contain(option => option.Display == "Fire");
            originalCombat.Cells.Single(cell => cell.Label == "Delay").Maximum
                .Should().NotBe(ItemCostTableRanges.DefaultMax);

            choices = new[] { new BehaviorChoice(5, "Electrical") };
            section.ReloadGameResources(costTables: null);

            var refreshedCombat = section.Groups.Single(group => group.Group == ItemStatGroup.Combat);
            refreshedCombat.ExclusiveChoices.Single().Options.Should().Contain(option => option.Display == "Electrical");
            refreshedCombat.ExclusiveChoices.Single().Options.Should().NotContain(option => option.Display == "Fire");
            refreshedCombat.Cells.Single(cell => cell.Label == "Delay").Maximum
                .Should().Be(ItemCostTableRanges.DefaultMax);
        }

        [Test]
        public void MissingCostTableMetadataKeepsTheStoredValueReadOnly()
        {
            var store = OpenStore();
            var editCount = 0;
            var section = new ItemStatsSectionViewModel(store, (_, mutation) =>
            {
                editCount++;
                mutation();
                return true;
            });
            section.Rebuild(ItemFamily.Armor, ItemRoleCatalog.CustomId);
            var physicalDefense = section.Groups
                .Single(group => group.Group == ItemStatGroup.Defense)
                .Cells.Single(cell => cell.Label == "Physical Defense");

            physicalDefense.HasOptions.Should().BeFalse();
            physicalDefense.LookupUnavailableMessage.Should().Contain("read-only");

            physicalDefense.Number = 99;

            physicalDefense.Number.Should().Be(26);
            store.GetPropertyValue(94, 1).Should().Be(26);
            editCount.Should().Be(0);
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

        [Test]
        public void ADropdownNeverOffersABlankRowOrARepeatedLabel()
        {
            var ranges = new ItemCostTableRanges(new TwoDaService(Sw2DaDirectory));
            var section = new ItemStatsSectionViewModel(
                OpenStore(), (_, mutation) => { mutation(); return true; }, costTables: ranges);
            section.Rebuild(ItemFamily.Armor, ItemRoleCatalog.CustomId);

            var cells = section.Groups.SelectMany(group => group.Cells).ToList();
            cells.Should().NotBeEmpty();

            foreach (var cell in cells.Where(cell => cell.HasOptions))
            {
                // 2DA blanks are a run of asterisks and are not selectable CostValues.
                cell.Options.Should().NotContain(
                    option => option.Label.Length == 0 || option.Label.All(character => character == '*'),
                    $"{cell.Label} offered a blank row");

                // Every listed semantic amount or coded label must identify exactly one stored row.
                cell.Options.Select(option => option.Label).Should().OnlyHaveUniqueItems(
                    $"{cell.Label} offered the same label twice");
            }
        }
    }
}
