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
        perkSource.Should().Contain("Combat.RefreshStatDrivenTrackerEffects(creature);");
        perkSource.Should().Contain("RemoveStatusEffectsOnPerkRefund(player, perkType);");
        perksViewModelSource.Should().Contain("Perk.RemoveStatusEffectsOnPerkRefund(target, selectedPerk);");
        rebuildViewModelSource.Should().Contain("Perk.RemoveStatusEffectsOnPerkRefund(Player, type);");

        var combatSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Combat.cs"));
        combatSource.Should().Contain("_autoAttackCycleCriticalCounts.Remove(creature);");
        combatSource.Should().Contain("typeof(AttackCycleTrackerStatusEffect)");
        combatSource.Should().Contain("typeof(CriticalRateStackTrackerStatusEffect)");
        combatSource.Should().Contain("StatType.NonCriticalAbilityNextSkillAbilityCriticalRatePercentAdjustment) <= 0");
        combatSource.Should().Contain("typeof(SteadyAimReadyStatusEffect)");
        combatSource.Should().Contain("typeof(IdleSkillAbilityReadyStatusEffect)");
        combatSource.Should().NotContain("typeof(PatienceReadyStatusEffect)");
        combatSource.Should().NotContain("typeof(OpeningAutoAttackReadyStatusEffect)");
    }

    [Test]
    public void CharacterFullRebuild_RemovesUndefinedPerksWithoutLookingUpDetails()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "CharacterFullRebuildViewModel.cs"));

        source.Should().Contain("var allPerks = Perk.GetAllPerks();");
        source.Should().Contain("if (!allPerks.TryGetValue(type, out var perkDetail))");
        source.Should().Contain("dbPlayer.Perks.Remove(type);");
        source.Should().Contain("Removed undefined perk during full rebuild");
        source.Should().Contain("PlayerInitialization.ResetFeatsToBaseline(Player);");
    }

    [Test]
    public void PlayerInitialization_ClearsFeatListBeforeRestoringBaselineFeats()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "PlayerInitialization.cs"));

        source.Should().Contain("for (var currentFeat = numberOfFeats - 1; currentFeat >= 0; currentFeat--)");
        source.Should().Contain("CreaturePlugin.RemoveFeat(player, CreaturePlugin.GetFeatByIndex(player, currentFeat));");
        source.Should().Contain("public static void ResetFeatsToBaseline(uint player)");
        source.Should().Contain("ClearFeats(player);");
        source.Should().Contain("GrantBasicFeats(player);");
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
