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
    /// <summary>The Requirements tab: skill categories, the stat block, and the perk/race gates.</summary>
    [TestFixture]
    public class ItemRequirementsSectionTests
    {
        private static string AdrenHarnessPath =>
            Path.Combine(CorpusLocator.ModuleDirectory, "uti", "adren_harness.uti.json");

        private static ItemValueStore OpenStore() =>
            new(UtiDocument.Load(AdrenHarnessPath).Fields);

        private static string Sw2DaDirectory =>
            Path.Combine(CorpusLocator.RepositoryRoot, "SWLOR_Haks", "sw_2da");

        private static ItemRequirementsSectionViewModel OpenSection(ItemValueStore store) =>
            new(store, (_, mutation) => { mutation(); return true; },
                costTables: new ItemCostTableRanges(new TwoDaService(Sw2DaDirectory)));

        [Test]
        public void ArmorSkillRequirementShowsTheStoredLevel()
        {
            var section = OpenSection(OpenStore());

            var combat = section.Groups.Single(group => group.Title == "Combat skills");
            combat.Cells.Single(cell => cell.Label == "Armor").Number.Should().Be(45);
        }

        [Test]
        public void SkillCategoriesArePresentAndLanguagesIsExcluded()
        {
            var section = OpenSection(OpenStore());

            var titles = section.Groups.Select(group => group.Title).ToList();
            titles.Should().Contain(new[] { "Combat skills", "Crafting skills", "Utility skills" });
            titles.Should().NotContain("Languages");
        }

        [Test]
        public void OtherGroupsCoverStatAndTheStatGroupHasNoOtherRequirementsCardAnymore()
        {
            var section = OpenSection(OpenStore());

            var stat = section.Groups.Single(group => group.Title == "Required stat");
            stat.Cells.Should().HaveCount(6);
            stat.Cells.Select(cell => cell.Label).Should().BeEquivalentTo(new[]
            {
                "Might", "Perception", "Vitality", "Agility", "Willpower", "Social"
            });

            // Perk (100) and Race (64) moved off the single-cell "Other requirements" card and onto
            // their own entry lists - there is no group left with just those two in it.
            section.Groups.Should().NotContain(group => group.Title == "Other requirements");
        }

        [Test]
        public void PerkAndRaceSurfaceAsEntryListsInsteadOfTheOldSingleCell()
        {
            var section = OpenSection(OpenStore());

            section.EntryLists.Select(list => list.Label).Should().BeEquivalentTo(new[]
            {
                "Required Perk", "Required Race"
            });
        }

        [Test]
        public void PerkAndRaceAddSearchBoxesNameWhatTheySearch()
        {
            var section = OpenSection(OpenStore());

            section.EntryLists.Single(list => list.Label == "Required Perk").AddWatermark.Should().Be("Search Perks");
            section.EntryLists.Single(list => list.Label == "Required Race").AddWatermark.Should().Be("Search Races");
        }

        [Test]
        public void GameResourceReloadRebuildsSubtypeChoicesAndCostTableModels()
        {
            IReadOnlyList<BehaviorChoice> choices = new[] { new BehaviorChoice(12, "Weapon Focus") };
            var section = new ItemRequirementsSectionViewModel(
                OpenStore(),
                (_, mutation) => { mutation(); return true; },
                resolveChoices: _ => choices,
                costTables: new ItemCostTableRanges(new TwoDaService(Sw2DaDirectory)));

            section.EntryLists.Single(list => list.Label == "Required Perk").AddChoices
                .Should().Contain(choice => choice.Display == "Weapon Focus");
            section.Groups.Single(group => group.Title == "Combat skills")
                .Cells.Single(cell => cell.Label == "Armor").Maximum
                .Should().NotBe(ItemCostTableRanges.DefaultMax);

            choices = new[] { new BehaviorChoice(34, "Martial Arts") };
            section.ReloadGameResources(costTables: null);

            var perks = section.EntryLists.Single(list => list.Label == "Required Perk").AddChoices;
            perks.Should().Contain(choice => choice.Display == "Martial Arts");
            perks.Should().NotContain(choice => choice.Display == "Weapon Focus");
            section.Groups.Single(group => group.Title == "Combat skills")
                .Cells.Single(cell => cell.Label == "Armor").Maximum
                .Should().Be(ItemCostTableRanges.DefaultMax);
        }

        [Test]
        public void AddingAPerkRequirementRoundTripsAsSubtypeAndLevel()
        {
            var store = OpenStore();
            var section = OpenSection(store);

            var perkChoices = new[]
            {
                new BehaviorChoice(12, "Weapon Focus"),
                new BehaviorChoice(34, "Martial Arts")
            };
            var perkList = new ItemPropertyEntryListViewModel(
                ItemMultiEntryCatalog.ByPropertyId(100)!, store, (_, mutation) => { mutation(); return true; },
                perkChoices);

            perkList.AddCommand.Execute(new BehaviorChoiceViewModel(perkChoices[0]));

            store.GetPropertyValue(100, 12).Should().Be(1, "AddCommand writes an initial value of 1");
            var entry = perkList.Entries.Single(e => e.SubtypeId == 12);
            entry.SubtypeDisplay.Should().Be("Weapon Focus");

            entry.Number = 3;
            store.GetPropertyValue(100, 12).Should().Be(3, "the required level round-trips through Number");
        }

        [Test]
        public void WritingACraftingSkillRequirementRoundTrips()
        {
            var store = OpenStore();
            var section = OpenSection(store);

            var smithery = section.Groups
                .Single(group => group.Title == "Crafting skills")
                .Cells.Single(cell => cell.Label == "Smithery");

            smithery.Number.Should().BeNull("the corpus item has no Smithery requirement");

            smithery.Number = 20;

            store.GetPropertyValue(131, 9).Should().Be(20, "Smithery is SkillType 9");

            smithery.Number = null;
            store.GetPropertyValue(131, 9).Should().BeNull();
        }
    }
}
