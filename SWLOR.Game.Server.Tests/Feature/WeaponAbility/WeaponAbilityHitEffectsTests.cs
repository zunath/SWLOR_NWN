using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.AbilityDefinition.WeaponAbility;
using SWLOR.Game.Server.Tests.Support;
using static SWLOR.Game.Server.Tests.Support.CombatTranscriptBench;

namespace SWLOR.Game.Server.Tests.Feature.WeaponAbility;

/// <summary>The resource restore and drain and the target modifiers of each landed hit, with fake services.</summary>
[TestFixture]
[NonParallelizable]
public class WeaponAbilityHitEffectsTests
{
    private WeaponAbilityPartBench _bench = null!;
    private WeaponAbilityHitEffects _part = null!;

    [SetUp]
    public void Build()
    {
        _bench = new WeaponAbilityPartBench();
        _part = _bench.Part<WeaponAbilityHitEffects>();
    }

    [TearDown]
    public void Remove() => _bench.Dispose();

    private static WeaponAbilityProfile Restoring() => new()
    {
        RestoreBothResourcesBelowThresholdPercent = 30,
        RestoreFPIfResourcesBelow = 5,
        RestoreStaminaIfResourcesBelow = 8
    };

    [Test]
    public void ResourceRestoreDoesNothingWhileTheResourcesAreAboveTheThreshold()
    {
        var text = _bench.Run(() => _part.ApplyResourceConditionalRestore(Restoring(), Attacker));

        text.Should().NotContain("RestoreFP").And.NotContain("RestoreStamina");
    }

    [Test]
    public void ResourceRestoreGivesBothPoolsWhenBothAreAtOrBelowTheThreshold()
    {
        _bench.Leaf<ICombatResourcesService>().Script(nameof(ICombatResourcesService.IsCurrentFPAndStaminaAtOrBelowPercent), _ => true);
        _bench.Leaf<IStatService>()
            .Script(nameof(IStatService.RestoreFP), _ => 5)
            .Script(nameof(IStatService.RestoreStamina), _ => 8);

        var text = _bench.Run(() => _part.ApplyResourceConditionalRestore(Restoring(), Attacker));

        text.Should().Contain("RestoreFP(Attacker, 5").And.Contain("RestoreStamina(Attacker, 8")
            .And.Contain("ApplyAbilityRestoredFPEffects").And.Contain("ApplyAbilityRestoredBothResourcesEffects");
    }

    [Test]
    public void ResourceRestoreSkipsTheBothResourcesEffectWhenOnlyFPCameBack()
    {
        _bench.Leaf<ICombatResourcesService>().Script(nameof(ICombatResourcesService.IsCurrentFPAndStaminaAtOrBelowPercent), _ => true);
        _bench.Leaf<IStatService>().Script(nameof(IStatService.RestoreFP), _ => 5);

        var text = _bench.Run(() => _part.ApplyResourceConditionalRestore(Restoring(), Attacker));

        text.Should().Contain("ApplyAbilityRestoredFPEffects").And.NotContain("ApplyAbilityRestoredBothResourcesEffects");
    }

    [Test]
    public void ResourceDrainNeedsTheActivatorAtOrAboveTheThresholdInTheMatchingPool()
    {
        var profile = new WeaponAbilityProfile
        {
            DrainTargetResourceAboveThresholdPercent = 50,
            DrainTargetFPIfActivatorFPAboveThreshold = 6,
            DrainTargetStaminaIfActivatorStaminaAboveThreshold = 7
        };
        _bench.Leaf<IStatService>()
            .Script(nameof(IStatService.GetMaxFP), _ => 100).Script(nameof(IStatService.GetCurrentFP), _ => 60)
            .Script(nameof(IStatService.GetMaxStamina), _ => 100).Script(nameof(IStatService.GetCurrentStamina), _ => 40);

        var text = _bench.Run(() => _part.ApplyResourceConditionalDrain(profile, Attacker, Defender));

        text.Should().Contain("ReduceFP(Defender, 6").And.NotContain("ReduceStamina");
    }

    [Test]
    public void ResourceDrainIsOffWithoutAThreshold()
    {
        var profile = new WeaponAbilityProfile { DrainTargetFPIfActivatorFPAboveThreshold = 6 };

        _bench.Run(() => _part.ApplyResourceConditionalDrain(profile, Attacker, Defender)).Should().NotContain("ReduceFP");
    }

    [Test]
    public void TargetModifiersAreWrittenOnlyForASettingWithADuration()
    {
        var profile = new WeaponAbilityProfile
        {
            TargetAttackPercent = -10, TargetAttackDurationSeconds = 8,
            TargetAbilityHitChancePercent = -5, TargetAbilityHitChanceDurationSeconds = 0
        };

        var text = _bench.Run(() => _part.ApplyTargetTemporaryModifiers(profile, Defender));

        text.Should().Contain("Replace(Defender, AttackPercentAdjustment, -10, 8, GeneratedWeaponAbilityTarget:AttackPercentAdjustment)")
            .And.NotContain("AbilityHitChancePercentAdjustment");
    }
}
