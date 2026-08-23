using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class PerksWindowTests
{
    [Test]
    public void SortOptions_AreEmbeddedInThePerksWindowLayout()
    {
        var root = FindRepositoryRoot();
        var definitionSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "PerksDefinition.cs"));
        var viewModelSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "PerksViewModel.cs"));

        definitionSource.Should().Contain(".AddOption(\"Alphabetical (A-Z)\", 0)");
        definitionSource.Should().Contain(".AddOption(\"Alphabetical (Z-A)\", 1)");
        definitionSource.Should().Contain(".AddOption(\"Skill Level (Asc)\", 2)");
        definitionSource.Should().Contain(".AddOption(\"Skill Level (Desc)\", 3)");
        definitionSource.Should().NotContain("model => model.SortOptions");
        viewModelSource.Should().NotContain("public GuiBindingList<GuiComboEntry> SortOptions");
        viewModelSource.Should().NotContain("SortAlphabeticalAscending");
    }

    [Test]
    public void DefaultSort_IsSkillLevelAscendingForEveryPerkMode()
    {
        var root = FindRepositoryRoot();
        var viewModelSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "PerksViewModel.cs"));

        viewModelSource.Should().Contain(
            "SelectedSortOrderId = (int)PerkSortOrder.SkillLevelAscending;");
        viewModelSource.Should().NotContain(
            "SelectedSortOrderId = (int)PerkSortOrder.AlphabeticalAscending;");
    }

    [Test]
    public void SelectedPerkDetails_ShowVisibleRecastGroupAfterCategory()
    {
        var root = FindRepositoryRoot();
        var viewModelSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "PerksViewModel.cs"));
        var perkSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Perk.cs"));
        var recastSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Recast.cs"));

        viewModelSource.Should().Contain("selectedDetails += $\"[{categoryDetail.Name}]\\n\";");
        viewModelSource.Should().Contain("var recastGroupText = BuildRecastGroupText(detail);");
        viewModelSource.Should().Contain("selectedDetails += recastGroupText + \"\\n\";");
        viewModelSource.Should().Contain("Perk.GetActiveAbilityRecastGroup(detail.Type)");
        viewModelSource.Should().Contain("Recast.IsRecastGroupVisible(recastGroup)");
        viewModelSource.Should().Contain("Recast.GetRecastGroupDisplayName(recastGroup)");
        viewModelSource.Should().Contain("recastGroup == RecastGroup.Invalid");
        viewModelSource.Should().NotContain("Recast Groups");
        viewModelSource.Should().NotContain("string.Join");
        perkSource.Should().Contain("private static readonly Dictionary<PerkType, RecastGroup> _activeAbilityRecastGroupByPerk");
        perkSource.Should().Contain("public static RecastGroup GetActiveAbilityRecastGroup(PerkType perkType)");
        recastSource.Should().Contain("private static readonly Dictionary<RecastGroup, string> _recastNames");
        recastSource.Should().Contain("private static readonly HashSet<RecastGroup> _visibleRecastGroups");

        var categoryIndex = viewModelSource.IndexOf("selectedDetails += $\"[{categoryDetail.Name}]\\n\";");
        var recastIndex = viewModelSource.IndexOf("var recastGroupText = BuildRecastGroupText(detail);");
        var currentUpgradeIndex = viewModelSource.IndexOf("if (currentUpgrade != null)");

        categoryIndex.Should().BeLessThan(recastIndex);
        recastIndex.Should().BeLessThan(currentUpgradeIndex);
    }

    [Test]
    public void ForceAffinity_IsProminentAndExplainsEachSelectedForcePerk()
    {
        var root = FindRepositoryRoot();
        var definitionSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "PerksDefinition.cs"));
        var viewModelSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "PerksViewModel.cs"));

        definitionSource.Should().Contain("model => model.IsForceAffinityVisible");
        definitionSource.Should().Contain("model => model.ForceAffinityHeading");
        definitionSource.Should().Contain("model => model.ForceAffinityExplanation");
        definitionSource.Should().Contain("model => model.ForceAffinityColor");

        viewModelSource.Should().Contain("dbPlayer.CharacterType == CharacterType.ForceSensitive");
        viewModelSource.Should().Contain("FORCE AFFINITY: {affinityLabel}");
        viewModelSource.Should().Contain("Owning any rank of a Light power contributes +1; a Dark power contributes -1.");
        viewModelSource.Should().Contain("detail.ForceAffinityType.Value == ForceAffinityType.Light ? \"LIGHT\" : \"DARK\"");
        viewModelSource.Should().Contain("{alignment}-ALIGNED FORCE POWER");
        viewModelSource.Should().Contain("UNIVERSAL FORCE POWER");
        viewModelSource.Should().Contain("Perk.GetForceAffinityMagnitudeMultiplier(Player, detail.Type)");
        viewModelSource.Should().Contain("Perk.GetForceAffinityHitChanceAdjustment(Player, detail.Type)");
        viewModelSource.Should().Contain("additional ranks do not add more affinity");
    }

    [Test]
    public void NativeStealthMode_IsAddedAndRemovedAsAModeToggle()
    {
        var root = FindRepositoryRoot();
        var viewModelSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "PerksViewModel.cs"));

        viewModelSource.Should().NotContain("PerkType.Stealth");
        viewModelSource.Should().Contain("Perk.GetPerkDetails(perkType).HotBarActionModes");
        viewModelSource.Should().Contain("AddModeToggleToHotBar(actionMode);");
        viewModelSource.Should().Contain("PlayerQuickBarSlot.ToggleMode((int)mode)");
        viewModelSource.Should().Contain("foreach (var actionMode in perkDetail.HotBarActionModes)");
        viewModelSource.Should().Contain("RemoveModeToggleFromHotBar(actionMode);");

        var matcher = typeof(PerksViewModel).GetMethod(
            "IsModeHotBarSlot",
            BindingFlags.Static | BindingFlags.NonPublic)!;
        var stealthMode = new QuickBarSlot
        {
            ObjectType = QuickBarSlotType.ModeToggle,
            INTParam1 = (int)ActionMode.Stealth,
        };
        var stealthFeat = new QuickBarSlot
        {
            ObjectType = QuickBarSlotType.Feat,
            INTParam1 = (int)ActionMode.Stealth,
        };
        var detectMode = new QuickBarSlot
        {
            ObjectType = QuickBarSlotType.ModeToggle,
            INTParam1 = (int)ActionMode.Detect,
        };

        matcher.Invoke(null, new object[] { stealthMode, ActionMode.Stealth }).Should().Be(true);
        matcher.Invoke(null, new object[] { stealthFeat, ActionMode.Stealth }).Should().Be(false);
        matcher.Invoke(null, new object[] { detectMode, ActionMode.Stealth }).Should().Be(false);
    }

    [Test]
    public void SkillLevelSort_UnlearnedPerksUseFirstRequiredSkillLevel()
    {
        var detail = BuildPerkDetail((1, 10), (2, 20), (3, 30));

        GetRequiredSkillLevelSortOrder(detail, 0).Should().Be(10);
    }

    [Test]
    public void SkillLevelSort_InProgressPerksUseNextRequiredSkillLevel()
    {
        var detail = BuildPerkDetail((1, 10), (2, 20), (3, 30));

        GetRequiredSkillLevelSortOrder(detail, 1).Should().Be(20);
    }

    [Test]
    public void SkillLevelSort_MaxedPerksUseLastLearnedSkillRequirement()
    {
        var detail = BuildPerkDetail((1, 10), (2, 20), (3, 30));

        GetRequiredSkillLevelSortOrder(detail, 3).Should().Be(30);
    }

    [Test]
    public void SkillLevelSort_MaxedPerksWalkBackToLastSkillRequirement()
    {
        var detail = BuildPerkDetail((1, 10), (2, 0), (3, 0));

        GetRequiredSkillLevelSortOrder(detail, 3).Should().Be(10);
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }

    private static PerkDetail BuildPerkDetail(params (int Level, int SkillRank)[] levels)
    {
        var detail = new PerkDetail();

        foreach (var (level, skillRank) in levels)
        {
            var perkLevel = new PerkLevel();
            if (skillRank > 0)
            {
                perkLevel.Requirements.Add(new PerkRequirementSkill(SkillType.Leadership, skillRank));
            }

            detail.PerkLevels[level] = perkLevel;
        }

        return detail;
    }

    private static int GetRequiredSkillLevelSortOrder(PerkDetail detail, int rank)
    {
        return (int)typeof(PerksViewModel)
            .GetMethod(
                "GetRequiredSkillLevelSortOrder",
                BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, new object[] { detail, rank })!;
    }
}
