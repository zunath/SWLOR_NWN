using FluentAssertions;
using NUnit.Framework;

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
}
