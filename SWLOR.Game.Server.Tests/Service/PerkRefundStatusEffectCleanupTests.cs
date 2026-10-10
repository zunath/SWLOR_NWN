using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Beastmaster;
using SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

public class PerkRefundStatusEffectCleanupTests
{
    [Test]
    public void SourceOwnedRefunds_KeepEachAbilityAndPreserveUnrelatedHaste()
    {
        var snapRush = new SnapRushTechniqueAbilityDefinition().BuildAbilities()[FeatType.SnapRushTechnique];
        var secondTechnique = new AbilityDetail
        {
            EffectiveLevelPerkType = PerkType.CombatAnalyzer,
            SourceOwnedStatusEffectTypesRemovedOnPerkRefund = new List<Type> { typeof(Hasten1StatusEffect) }
        };
        var hasten = new HastenAbilityDefinition().BuildAbilities()[FeatType.Hasten1];
        hasten.SourceOwnedStatusEffectTypesRemovedOnPerkRefund.Add(typeof(Hasten1StatusEffect));

        var refunds = Perk.GetSourceOwnedStatusEffectRefunds(
            new[] { snapRush, secondTechnique, snapRush, hasten }, PerkType.CombatAnalyzer);
        refunds.Should().Equal(new[]
        {
            (snapRush, typeof(Hasten1StatusEffect)),
            (secondTechnique, typeof(Hasten1StatusEffect))
        }, "sharing a status class must not discard its ability ownership or include another perk");

        var snapHaste = new Hasten1StatusEffect { OriginatingAbility = snapRush };
        var secondHaste = new Hasten1StatusEffect { OriginatingAbility = secondTechnique };
        var beastmasterHaste = new Hasten1StatusEffect { OriginatingAbility = hasten };
        var untrackedHaste = new Hasten1StatusEffect();
        var alliedHaste = new Hasten1StatusEffect { OriginatingAbility = snapRush };
        foreach (var effect in new[] { snapHaste, secondHaste, beastmasterHaste, untrackedHaste })
            effect.ApplyEffect(1, 2, 15);
        alliedHaste.ApplyEffect(3, 2, 15);
        var effects = new[] { snapHaste, secondHaste, beastmasterHaste, untrackedHaste, alliedHaste };

        refunds.SelectMany(refund => StatusEffect.GetSourceOwnedStatusEffects(
                effects, refund.StatusEffectType, 1, refund.Ability))
            .Should().Equal(new IStatusEffect[] { snapHaste, secondHaste });
    }

    [Test]
    public void SourceOwnedCleanup_CachedTargetsPreserveUnrelatedHasteAndStats()
    {
        var snapRush = new AbilityDetail();
        var hasten = new AbilityDetail();
        var snapHaste = new Hasten1StatusEffect { OriginatingAbility = snapRush };
        var beastmasterHaste = new Hasten1StatusEffect { OriginatingAbility = hasten };
        var untrackedHaste = new Hasten1StatusEffect();
        var alliedHaste = new Hasten1StatusEffect { OriginatingAbility = snapRush };
        var trackers = new Dictionary<IStatusEffect, CreatureStatusEffect>();
        foreach (var effect in new[] { snapHaste, beastmasterHaste, untrackedHaste, alliedHaste })
        {
            var tracker = new CreatureStatusEffect();
            effect.ApplyEffect(effect == alliedHaste ? 9u : 7u, 2, 15);
            tracker.Add(effect);
            trackers.Add(effect, tracker);
        }

        foreach (var tracker in trackers.Values)
            foreach (var effect in StatusEffect.GetSourceOwnedStatusEffects(
                         tracker.GetAllEffects(), typeof(Hasten1StatusEffect), 7, snapRush))
                tracker.Remove(effect);

        trackers[snapHaste].GetAllEffects().Should().BeEmpty();
        trackers[snapHaste].StatGroup.Stats[StatType.AttackDelayReductionPercent].Should().Be(0);
        foreach (var effect in new[] { beastmasterHaste, untrackedHaste, alliedHaste })
        {
            trackers[effect].GetAllEffects().Should().Equal(new IStatusEffect[] { effect });
            trackers[effect].StatGroup.Stats[StatType.AttackDelayReductionPercent].Should().Be(15,
                "removing an owned buff from a logged-out target must preserve other targets' unrelated payloads");
        }
    }

    [Test]
    public void SourceOwnedCleanup_ReconnectedSourceRetainsItsAbilityIdentity()
    {
        var snapRush = new AbilityDetail();
        var hasten = new AbilityDetail();
        var snapHaste = new Hasten1StatusEffect { OriginatingAbility = snapRush };
        var beastmasterHaste = new Hasten1StatusEffect { OriginatingAbility = hasten };
        foreach (var effect in new[] { snapHaste, beastmasterHaste })
        {
            effect.ApplyEffect(7, 7, 15);
            effect.ReassignSource(11);
        }

        snapHaste.OriginatingAbility.Should().BeSameAs(snapRush);
        beastmasterHaste.OriginatingAbility.Should().BeSameAs(hasten);
        StatusEffect.GetSourceOwnedStatusEffects(
                new[] { snapHaste, beastmasterHaste }, typeof(Hasten1StatusEffect), 7, snapRush)
            .Should().BeEmpty("the old creature handle no longer owns the restored self effects");
        StatusEffect.GetSourceOwnedStatusEffects(
                new[] { snapHaste, beastmasterHaste }, typeof(Hasten1StatusEffect), 11, snapRush)
            .Should().Equal(new IStatusEffect[] { snapHaste });
    }

    [Test]
    public void SourceOwnedRefundPath_PassesTheOriginatingAbilityToCleanup()
    {
        var syntax = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(
            FindRepositoryRoot().FullName, "SWLOR.Game.Server", "Service", "Perk.cs"))).GetRoot();
        var method = syntax.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single(node => node.Identifier.ValueText == "RemoveStatusEffectsOnPerkRefund");
        var cleanup = method.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Single(node => node.Expression.ToString() == "StatusEffect.RemoveStatusEffectsFromAllTargetsBySource");
        cleanup.ArgumentList.Arguments.Select(argument => argument.ToString()).Should()
            .Equal("creature", "sourceOwnedStatusEffect.StatusEffectType", "false", "sourceOwnedStatusEffect.Ability");
    }

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
        perkSource.Should().Contain("SourceOwnedStatusEffectTypesRemovedOnPerkRefund");
        perkSource.Should().Contain("StatusEffect.RemoveStatusEffectsFromAllTargetsBySource(");
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
        combatSource.Should().Contain("typeof(OpeningAttackReadyStatusEffect)");
        combatSource.Should().Contain("typeof(IdleSkillAbilityReadyStatusEffect)");
        combatSource.Should().NotContain("typeof(PatienceReadyStatusEffect)");
        combatSource.Should().NotContain("typeof(OpeningAutoAttackReadyStatusEffect)");
    }

    [Test]
    public void SourceOwnedPerkRefundCleanup_RemovesEffectsFromLoggedOutTargets()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "StatusEffect.cs")).Replace("\r\n", "\n");
        var cleanupBody = source.Substring(
            source.IndexOf("public static void RemoveStatusEffectsFromAllTargetsBySource(", StringComparison.Ordinal),
            source.IndexOf("private static void RemoveStatusEffectsFromAllTargetsWhenSourceExits", StringComparison.Ordinal) -
            source.IndexOf("public static void RemoveStatusEffectsFromAllTargetsBySource(", StringComparison.Ordinal));

        cleanupBody.Should().Contain("foreach (var loggedOutEffects in _loggedOutPlayerEffects.Values)");
        cleanupBody.Should().Contain("statusEffectType.IsAssignableFrom(effect.GetType())");
        cleanupBody.Should().Contain("effect.Source == source");
        cleanupBody.Should().Contain("loggedOutEffects.Effects.Remove(effect);");

        var method = CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Single(node => node.Identifier.ValueText == "RemoveStatusEffectsFromAllTargetsBySource");
        var ownershipFilters = method.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(node => node.Expression.ToString() == "GetSourceOwnedStatusEffects").ToArray();
        ownershipFilters.Should().HaveCount(2, "both active and logged-out targets need the same ownership filter");
        ownershipFilters.Should().OnlyContain(node => node.ArgumentList.Arguments.Count == 4 &&
            node.ArgumentList.Arguments[3].Expression.ToString() == "originatingAbility");
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
