using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Service;

public class PerkRefundStatusEffectCleanupTests
{
    [Test]
    public void ConfigureToggle_MarksStatusForPerkRefundCleanup()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "AbilityDefinition",
            "WeaponActiveAbilityDefinitionBase.cs"));

        source.Should().Contain(".RemoveStatusEffectOnPerkRefund(type)");
    }

    [Test]
    public void CustomToggleAbilities_MarkStatusForPerkRefundCleanup()
    {
        var root = FindRepositoryRoot();
        var abilityRoot = Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "AbilityDefinition");
        var failures = Directory
            .EnumerateFiles(abilityRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.EndsWith("ActiveAbilityDefinitionBase.cs", StringComparison.Ordinal))
            .Where(path =>
            {
                var source = File.ReadAllText(path);
                return source.Contains("ToggleSelfStatus(", StringComparison.Ordinal) &&
                       !source.Contains("RemoveStatusEffectOnPerkRefund(", StringComparison.Ordinal);
            })
            .Select(path => Path.GetRelativePath(root.FullName, path))
            .OrderBy(path => path)
            .ToList();

        failures.Should().BeEmpty("permanent self-toggle abilities must declare the status to remove when their perk is refunded");
    }

    [Test]
    public void PerkRefundPaths_RemoveMarkedStatusEffects()
    {
        var root = FindRepositoryRoot();
        var perkSource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "Perk.cs"));
        var perksViewModelSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "PerksViewModel.cs"));
        var rebuildViewModelSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "CharacterFullRebuildViewModel.cs"));

        perkSource.Should().Contain("StatusEffectTypesRemovedOnPerkRefund");
        perkSource.Should().Contain("StatusEffect.RemoveStatusEffect(creature, statusEffectType, false);");
        perkSource.Should().Contain("RemoveStatusEffectsOnPerkRefund(player, perkType);");
        perksViewModelSource.Should().Contain("Perk.RemoveStatusEffectsOnPerkRefund(target, selectedPerk);");
        rebuildViewModelSource.Should().Contain("Perk.RemoveStatusEffectsOnPerkRefund(Player, type);");
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
