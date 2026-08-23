using System;
using System.IO;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Feature.AbilityDefinition.Rifle;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public sealed class RifleSuppressionFeedbackTests
{
    [Test]
    public void SuppressionStatuses_ExposePlayerVisibleIdentity()
    {
        new KillBoxStatusEffect(4).Name.Should().Be("Kill Box");
        new KillBoxStatusEffect(4).Icon.Should().Be(EffectIconType.SuppressionStanceStatusEffect);
        new KillBoxStatusEffect(4).StackingType.Should().Be(StatusEffectStackType.StackFromMultipleSources);
        new KillBoxStatusEffect(4).Should().BeAssignableTo<IRangedHitSuppressionSource>();
        new OverwatchStatusEffect().Name.Should().Be("Overwatch");
        new OverwatchStatusEffect().Icon.Should().Be(EffectIconType.TacticalUplinkStatusEffect);
        new ContainmentNetStatusEffect(-10).DamageAdjustmentPercent.Should().Be(-10);
        new ContainmentNetStatusEffect(-10).StackingType.Should().Be(StatusEffectStackType.StackFromMultipleSources);
        new ContainmentNetStatusEffect(-10).StatGroup.Stats[StatType.DamageDealtPercentAdjustment].Should().Be(-10);
        new SustainedFireStatusEffect(3, 5, 9).Stacks.Should().Be(3);
        new SustainedFireStatusEffect(3, 5, 9).DamageBonus.Should().Be(9);
        ContainmentNetStatusEffect.ShouldRemainActive(3, 3, -10).Should().BeTrue();
        ContainmentNetStatusEffect.ShouldRemainActive(2, 3, -10).Should().BeFalse();
    }

    [Test]
    public void RifleSuppressionBranches_KeepFeedbackAndTargetingContracts()
    {
        var root = FindRepositoryRoot();
        var combat = File.ReadAllText(Path.Combine(root, "SWLOR.Game.Server", "Service", "Combat.cs"));
        var killBox = File.ReadAllText(Path.Combine(root, "SWLOR.Game.Server", "Feature", "AbilityDefinition", "Rifle", "KillBoxAbilityDefinition.cs"));
        var native = File.ReadAllText(Path.Combine(root, "SWLOR.Game.Server", "Native", "ResolveAttackRoll.cs"));

        combat.Should().Contain("Sustained Fire {state.Stacks}/{maxStacks}");
        combat.Should().Contain("Pinning Fire: Suppression applied.");
        combat.Should().Contain("new OverwatchStatusEffect()");
        combat.Should().Contain("new ContainmentNetStatusEffect(adjustment)");
        combat.Should().Contain("new SustainedFireStatusEffect(state.Stacks, maxStacks, stackBonus)");
        combat.Should().NotContain("GetSuppressionDamageDealtToOtherTargetsAdjustment");
        combat.Should().Contain("SuppressionStackDamageDealtRequiredStacks");
        combat.Should().Contain("SuppressionStackDamageDealtPercentAdjustment");
        combat.Should().Contain("public static bool ApplySuppressionStack(");
        combat.Should().Contain("if (applied && GetIsPC(attacker))");
        combat.Should().Contain("ContainmentNetStatusEffect.ShouldRemainActive(");
        combat.Should().Contain("OfType<IRangedHitSuppressionSource>()");
        combat.Should().NotContain("OfType<KillBoxStatusEffect>()");
        var suppressionStatus = File.ReadAllText(Path.Combine(root, "SWLOR.Game.Server", "Feature", "StatusEffectDefinition", "SuppressionStatusEffect.cs"));
        var statusEffectService = File.ReadAllText(Path.Combine(root, "SWLOR.Game.Server", "Service", "StatusEffect.cs"));
        suppressionStatus.Should().Contain("IStatusEffectRemovedHandler");
        suppressionStatus.Should().Contain("Combat.ReconcileContainmentNetStatus(Source, creature);");
        suppressionStatus.Should().NotContain("DelayCommand");
        statusEffectService.Should().Contain("NotifyStatusEffectRemoved(creature, statusEffect);");
        killBox.Should().Contain("StatusEffectFactory = () => new KillBoxStatusEffect()");
        killBox.Should().Contain("8.0f");
        killBox.Should().Contain("AbilityTargetingFlags.HarmsEnemies");
        killBox.Should().NotContain("OriginOnSelf");
        native.Should().Contain("ConsumeSuppressionRangedAttackAccuracyAdjustment");
    }

    [Test]
    public void KillBox_IsAimedEnemyOnlyGroundCircle()
    {
        var ability = new KillBoxAbilityDefinition().BuildAbilities()[FeatType.KillBox1];
        ability.RequiresLocationTarget.Should().BeTrue();
        ability.Targeting.Shape.Should().Be(AbilityTargetingShapeType.Sphere);
        ability.Targeting.SizeX.Should().Be(8f);
        ability.Targeting.Flags.Should().HaveFlag(AbilityTargetingFlags.HarmsEnemies);
        ability.Targeting.Flags.Should().NotHaveFlag(AbilityTargetingFlags.OriginOnSelf);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
