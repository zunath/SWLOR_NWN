using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.AbilityDefinition.WeaponAbility;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Tests.Support;
using static SWLOR.Game.Server.Tests.Support.CombatTranscriptBench;

namespace SWLOR.Game.Server.Tests.Feature.WeaponAbility;

/// <summary>What a profile adds to damage, critical rate and the impact status, with fake services.</summary>
[TestFixture]
[NonParallelizable]
public class WeaponAbilityDamageModifiersTests
{
    private WeaponAbilityPartBench _bench = null!;
    private WeaponAbilityDamageModifiers _part = null!;

    [SetUp]
    public void Build()
    {
        _bench = new WeaponAbilityPartBench();
        _part = _bench.Part<WeaponAbilityDamageModifiers>();
    }

    [TearDown]
    public void Remove() => _bench.Dispose();

    private int BaseAdjustment(WeaponAbilityProfile profile, WeaponAbilityProfile.ActivationIdleBonusSnapshot? snapshot = null)
    {
        var result = 0;
        _bench.Run(() => result = _part.GetBaseDamageAdjustment(profile, Attacker, Defender, snapshot!));
        return result;
    }

    [Test]
    public void IdleBonusAppliesOnlyAfterTheIdleWindowHasPassed()
    {
        var profile = new WeaponAbilityProfile { IdleWindowSeconds = 3f, ExtraDamageIfIdle = 5 };

        _bench.Leaf<ICombatActivityService>().Script(nameof(ICombatActivityService.GetLastCompletedOffensiveActivityAt), _ => DateTime.UtcNow.AddSeconds(-1));
        BaseAdjustment(profile).Should().Be(0, "the caster acted one second ago, inside the three second window");

        _bench.Leaf<ICombatActivityService>().Script(nameof(ICombatActivityService.GetLastCompletedOffensiveActivityAt), _ => DateTime.UtcNow.AddSeconds(-30));
        BaseAdjustment(profile).Should().Be(5);
    }

    [Test]
    public void ASnapshotTakenAtActivationDecidesTheIdleBonusInsteadOfTheLiveState()
    {
        var profile = new WeaponAbilityProfile { IdleWindowSeconds = 3f, ExtraDamageIfIdle = 5 };
        _bench.Leaf<ICombatActivityService>().Script(nameof(ICombatActivityService.GetLastCompletedOffensiveActivityAt), _ => DateTime.UtcNow.AddSeconds(-30));

        BaseAdjustment(profile, new WeaponAbilityProfile.ActivationIdleBonusSnapshot { HasSnapshot = true, DamageBonus = 0 })
            .Should().Be(0, "the caster was not idle when the ability was activated");
        BaseAdjustment(profile, new WeaponAbilityProfile.ActivationIdleBonusSnapshot { HasSnapshot = true, DamageBonus = 7 })
            .Should().Be(7);
    }

    [Test]
    public void RecentTargetBonusNeedsTheTargetToHaveBeenHitInsideTheWindow()
    {
        var profile = new WeaponAbilityProfile { ExtraDamageIfRecentTarget = 4, RecentTargetWindowSeconds = 6f };

        BaseAdjustment(profile).Should().Be(0);

        _bench.Leaf<ICombatActivityService>().Script(nameof(ICombatActivityService.HasRecentDamageTarget), _ => true);
        BaseAdjustment(profile).Should().Be(4);
    }

    [Test]
    public void RecentTargetBonusIsOffWhenTheWindowIsZero()
    {
        _bench.Leaf<ICombatActivityService>().Script(nameof(ICombatActivityService.HasRecentDamageTarget), _ => true);

        BaseAdjustment(new WeaponAbilityProfile { ExtraDamageIfRecentTarget = 4, RecentTargetWindowSeconds = 0f }).Should().Be(0);
    }

    [Test]
    public void LowHitPointBonusFollowsTheThresholdPercent()
    {
        // The engine fake gives every creature 100 of 200 hit points, which is 50 percent.
        BaseAdjustment(new WeaponAbilityProfile { ExtraDamageIfTargetLowHP = 9, TargetLowHPThresholdPercent = 60 }).Should().Be(9);
        BaseAdjustment(new WeaponAbilityProfile { ExtraDamageIfTargetLowHP = 9, TargetLowHPThresholdPercent = 40 }).Should().Be(0);
    }

    [Test]
    public void BleedingBonusAsksTheTargetForTheBleedingCategory()
    {
        var profile = new WeaponAbilityProfile { ExtraDamageIfTargetBleeding = 6 };

        BaseAdjustment(profile).Should().Be(0);

        _bench.Leaf<IStatusEffectService>().Script(nameof(IStatusEffectService.HasStatusEffectCategory), _ => true);
        BaseAdjustment(profile).Should().Be(6);
    }

    [Test]
    public void GuardedHitStatusReplacesTheImpactStatusOnlyAfterAGuardedHit()
    {
        var profile = new WeaponAbilityProfile
        {
            RecentGuardedHitWindowSeconds = 8f,
            RecentGuardedHitStatusEffect = typeof(DisorientedStatusEffect),
            RecentGuardedHitStatusDurationSeconds = 12
        };

        _part.ResolveImpactStatus(profile, Attacker, typeof(BleedStatusEffect), 30).Should().Be((typeof(BleedStatusEffect), 30));

        _bench.Leaf<ICombatDefenseMemoryService>().Script(nameof(ICombatDefenseMemoryService.HasRecentGuardedHit), _ => true);
        _part.ResolveImpactStatus(profile, Attacker, typeof(BleedStatusEffect), 30).Should().Be((typeof(DisorientedStatusEffect), 12));
    }

    [Test]
    public void CriticalRateGrowsWhenTheTargetWasNotHitRecently()
    {
        var profile = new WeaponAbilityProfile { CriticalRatePercentAdjustment = 5, CriticalRateIfNotRecentTarget = 10, NotRecentTargetWindowSeconds = 6f };
        var rate = 0;

        _bench.Run(() => rate = _part.GetCriticalRateAdjustment(profile, Attacker, Defender));
        rate.Should().Be(15);

        _bench.Leaf<ICombatActivityService>().Script(nameof(ICombatActivityService.HasRecentDamageTarget), _ => true);
        _bench.Run(() => rate = _part.GetCriticalRateAdjustment(profile, Attacker, Defender));
        rate.Should().Be(5);
    }

    [Test]
    public void DefenseIgnoreAddsTheIdleShareOnlyWhileIdle()
    {
        var profile = new WeaponAbilityProfile { IdleWindowSeconds = 3f, DefenseIgnorePercent = 10, DefenseIgnoreIfIdle = 15 };

        _bench.Leaf<ICombatActivityService>().Script(nameof(ICombatActivityService.GetLastCompletedOffensiveActivityAt), _ => DateTime.UtcNow.AddSeconds(-1));
        _part.GetDefenseIgnorePercent(profile, Attacker).Should().Be(10);

        _bench.Leaf<ICombatActivityService>().Script(nameof(ICombatActivityService.GetLastCompletedOffensiveActivityAt), _ => DateTime.UtcNow.AddSeconds(-30));
        _part.GetDefenseIgnorePercent(profile, Attacker).Should().Be(25);
    }

    [Test]
    public void ActivationStoresNothingForAProfileWithoutIdleSettings()
    {
        var text = _bench.Run(() => _part.PrepareActivationIdleBonuses(new WeaponAbilityProfile(), Attacker));

        text.Should().Contain("ClearWeaponAbilityActivationIdleBonuses").And.NotContain("StoreWeaponAbilityActivationIdleBonuses");
    }

    [Test]
    public void ActivationStoresTheIdleBonusesWhenTheProfileHasAnIdleWindow()
    {
        var text = _bench.Run(() => _part.PrepareActivationIdleBonuses(
            new WeaponAbilityProfile { IdleWindowSeconds = 3f, ExtraDamageIfIdle = 5 }, Attacker));

        text.Should().Contain("StoreWeaponAbilityActivationIdleBonuses");
    }
}
