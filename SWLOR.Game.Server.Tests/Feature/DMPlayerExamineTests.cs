using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Feature;

public class DMPlayerExamineTests
{
    [TestCase(0, 2, 1, "")]
    [TestCase(7, 3, 2, "shield")]
    public void PerkControls_RepublishCurrentCriteriaWithoutReloadingOrResettingThem(
        int category, int sortOrder, int status, string search)
    {
        var model = new DMPlayerExamineViewModel
        {
            TopTabId = 2,
            BottomTabId = -1,
            SelectedPerkCategoryId = category,
            SelectedPerkSortOrderId = sortOrder,
            SelectedPerkStatusId = status,
            PerkSearchText = search
        };
        typeof(DMPlayerExamineViewModel).GetField("_selectedTabId", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(model, 2);
        typeof(DMPlayerExamineViewModel).GetField("_initialized", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(model, true);
        var notifications = new List<string>();
        model.PropertyChanged += (_, change) => notifications.Add(change.PropertyName!);

        typeof(DMPlayerExamineViewModel).GetMethod("RefreshTabInputs", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(model, null);

        notifications.Should().BeEquivalentTo(new[]
        {
            nameof(model.TopTabId), nameof(model.BottomTabId), nameof(model.SelectedPerkCategoryId),
            nameof(model.SelectedPerkSortOrderId), nameof(model.SelectedPerkStatusId), nameof(model.PerkSearchText)
        });
        model.SelectedPerkCategoryId.Should().Be(category);
        model.SelectedPerkSortOrderId.Should().Be(sortOrder);
        model.SelectedPerkStatusId.Should().Be(status);
        model.PerkSearchText.Should().Be(search);
    }

    [Test]
    public void Skills_CombineCategoryAndSearch_UsingTheTargetsCharacterType()
    {
        var skills = new Dictionary<SkillType, SkillAttribute>
        {
            [SkillType.Force] = Skill("Force", SkillCategoryType.Combat, CharacterType.ForceSensitive),
            [SkillType.Armor] = Skill("Armor", SkillCategoryType.Combat),
            [SkillType.Piloting] = Skill("Piloting", SkillCategoryType.Utility)
        };
        var target = new Player { CharacterType = CharacterType.Standard };

        DMPlayerExamineListFilter.Skills(skills, target, 0, "").Select(x => x.Key)
            .Should().Equal(SkillType.Armor, SkillType.Piloting);
        DMPlayerExamineListFilter.Skills(skills, target, (int)SkillCategoryType.Combat, "  ARM  ")
            .Select(x => x.Key).Should().Equal(SkillType.Armor);
        DMPlayerExamineListFilter.Skills(skills, target, (int)SkillCategoryType.Utility, "ARM")
            .Should().BeEmpty();
        target.CharacterType = CharacterType.ForceSensitive;
        DMPlayerExamineListFilter.Skills(skills, target, 0, "force")
            .Select(x => x.Key).Should().Equal(SkillType.Force);
    }

    [Test]
    public void Perks_ApplyOwnedMaxedAndBuyableFiltersToTargetRanks()
    {
        var perks = Perks();
        var ranks = new Dictionary<PerkType, int> { [PerkType.AlphaRhythm] = 1, [PerkType.Antitoxin] = 2 };
        var checkedRanks = new List<int>();
        bool CanBuy(PerkDetail detail, int rank)
        {
            checkedRanks.Add(rank);
            return detail.Name == "Alpha" && rank == 1;
        }

        DMPlayerExamineListFilter.Perks(perks, ranks, 0, "", 1, 0, CanBuy)
            .Select(x => x.Key).Should().Equal(PerkType.AlphaRhythm, PerkType.Antitoxin);
        DMPlayerExamineListFilter.Perks(perks, ranks, 0, "", 3, 0, CanBuy)
            .Select(x => x.Key).Should().Equal(PerkType.Antitoxin);
        checkedRanks.Should().BeEmpty();
        DMPlayerExamineListFilter.Perks(perks, ranks, 0, "", 2, 0, CanBuy)
            .Select(x => x.Key).Should().Equal(PerkType.AlphaRhythm);
        checkedRanks.Should().BeEquivalentTo(new[] { 1, 2, 0 });
    }

    [Test]
    public void Perks_CombineCategorySearchAndSortBeforeDisplayingRows()
    {
        var perks = Perks();
        var ranks = new Dictionary<PerkType, int>();
        DMPlayerExamineListFilter.Perks(perks, ranks, (int)PerkCategoryType.General, "  A ", 0, 1, (_, _) => false)
            .Select(x => x.Value.Name).Should().Equal("Beta", "Alpha");
        DMPlayerExamineListFilter.Perks(perks, ranks, (int)PerkCategoryType.Piloting, "Alpha", 0, 0, (_, _) => false)
            .Should().BeEmpty();
    }

    [Test]
    public void PerkSkillSort_UsesNextRankAndHandlesSavedRanksBeyondTheDefinition()
    {
        var perks = Perks();
        var ranks = new Dictionary<PerkType, int> { [PerkType.AlphaRhythm] = 1, [PerkType.Antitoxin] = 99 };
        DMPlayerExamineListFilter.Perks(perks, ranks, 0, "", 0, 2, (_, _) => false)
            .Select(x => x.Value.Name).Should().Equal("Gamma", "Alpha", "Beta");
        DMPlayerExamineListFilter.Perks(perks, ranks, 0, "", 0, 3, (_, _) => false)
            .Select(x => x.Value.Name).Should().Equal("Beta", "Alpha", "Gamma");
    }

    [Test]
    public void RetiredOwnedPerks_RemainInspectableWithoutBecomingBuyable()
    {
        var perks = Perks();
        perks[PerkType.AlphaRhythm].IsActive = false;
        perks[PerkType.Antitoxin].IsActive = false;
        var ranks = new Dictionary<PerkType, int> { [PerkType.AlphaRhythm] = 1 };

        DMPlayerExamineListFilter.Perks(perks, ranks, 0, "", 0, 0, (_, _) => true)
            .Select(x => x.Value.Name).Should().Equal("Alpha", "Gamma");
        DMPlayerExamineListFilter.Perks(perks, ranks, 0, "", 1, 0, (_, _) => true)
            .Select(x => x.Value.Name).Should().Equal("Alpha");
        DMPlayerExamineListFilter.Perks(perks, ranks, 0, "", 2, 0, (_, _) => true)
            .Select(x => x.Value.Name).Should().Equal("Gamma");
    }

    [TestCase("status-id", true)]
    [TestCase("status-id:Native:Slow", true)]
    [TestCase("other-id:Native:Slow", false)]
    [TestCase("status-id-other", false)]
    [TestCase("", false)]
    public void NativePayloads_AreDeduplicatedOnlyForTheirManagedEffect(string tag, bool expected)
    {
        var method = typeof(DMPlayerExamineViewModel).GetMethod("IsManagedEffectTag", BindingFlags.NonPublic | BindingFlags.Static)!;
        method.Invoke(null, new object[] { tag, new HashSet<string> { "status-id" } }).Should().Be(expected);
    }

    [TestCase(-1f, "Permanent")]
    [TestCase(0f, "00:00:00")]
    [TestCase(0.2f, "00:00:01")]
    [TestCase(3661f, "01:01:01")]
    [TestCase(90061f, "1.01:01:01")]
    public void EffectDuration_PreservesHoursDaysAndPermanentLifetimes(float seconds, string expected)
    {
        var method = typeof(DMPlayerExamineViewModel).GetMethod("FormatDuration", BindingFlags.NonPublic | BindingFlags.Static)!;
        method.Invoke(null, new object[] { seconds }).Should().Be(expected);
    }

    private static SkillAttribute Skill(string name, SkillCategoryType category, CharacterType restriction = CharacterType.Invalid) =>
        new(category, name, 50, true, "Description", false, false, false, characterTypeRestriction: restriction);

    private static Dictionary<PerkType, PerkDetail> Perks() => new()
    {
        [PerkType.AlphaRhythm] = Perk("Alpha", PerkCategoryType.General, 1, 20),
        [PerkType.Antitoxin] = Perk("Beta", PerkCategoryType.General, 10, 30),
        [PerkType.ApexBite] = Perk("Gamma", PerkCategoryType.Piloting, 5, 40)
    };

    private static PerkDetail Perk(string name, PerkCategoryType category, int firstRank, int secondRank) => new()
    {
        Name = name,
        Category = category,
        IsActive = true,
        PerkLevels = new Dictionary<int, PerkLevel>
        {
            [1] = new() { Requirements = new List<IPerkRequirement> { new PerkRequirementSkill(SkillType.Armor, firstRank) } },
            [2] = new() { Requirements = new List<IPerkRequirement> { new PerkRequirementSkill(SkillType.Armor, secondRank) } }
        }
    };
}
