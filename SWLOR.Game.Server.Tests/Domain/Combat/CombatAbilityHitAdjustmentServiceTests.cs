using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;
namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The ability hit adjustment part of <c>Combat</c>: the critical rate and its roll, the shared hit and critical adjustment, the
/// sundered target reads, the ranged defense reduction rider and the effects of an evaded Force ability.
/// </summary>
[TestFixture]
public class CombatAbilityHitAdjustmentServiceTests : CombatAbilityHitTestBase
{
    // ------------------------------------------------------------------ critical rate and roll

    [Test]
    public void TheAbilityCriticalRateAddsEverySourceThroughTheRule()
    {
        _skillCritical = 2;
        _skillSelected[StatType.AbilityCriticalRatePercentAdjustment] = 3;
        _longRangeCritical = 4;
        _targetStatusCritical = 5;
        _sideCritical = 6;
        _backCritical = 7;

        _adjustment.GetAbilityCriticalRate(Attacker, SkillType.Rifle, false, 1, Defender)
            .Should().Be(CombatRules.CalculateAbilityCriticalChance(1 + 2 + 3 + 4 + 5 + 6 + 7));
        _log.Should().Contain($"Selected {Attacker} {SkillType.Rifle} {StatType.AbilityCriticalRatePercentAdjustmentSkillType} {StatType.AbilityCriticalRatePercentAdjustment} True");

        _invalid.Add(Gone);
        _adjustment.GetAbilityCriticalRate(Gone, SkillType.Rifle, false).Should().Be(0);
    }

    [Test]
    public void TheTwinBladeAreaThrowingAndPositionalCriticalBonusesNeedTheirConditions()
    {
        Stat(Attacker, StatType.TwinBladeAreaAbilityCriticalRatePercentAdjustment, 10);
        Stat(Attacker, StatType.ThrowingAbilityCriticalRateToBleedingOrDisorientedTargetPercentAdjustment, 20);
        Stat(Attacker, StatType.CriticalRateAgainstTargetNotFacingAttackerPercentAdjustment, 40);
        Stat(Attacker, StatType.CriticalRateAgainstSunderedTargetPercentAdjustment, 80);
        var none = CombatRules.CalculateAbilityCriticalChance(0);

        _adjustment.GetAbilityCriticalRate(Attacker, SkillType.TwinBlade, false, 0, Defender).Should().Be(none, "not an area ability");
        _adjustment.GetAbilityCriticalRate(Attacker, SkillType.TwinBlade, true, 0, Defender).Should().Be(CombatRules.CalculateAbilityCriticalChance(10));
        _adjustment.GetAbilityCriticalRate(Attacker, SkillType.Throwing, true, 0, Defender).Should().Be(none, "the target neither bleeds nor is disoriented");
        _statuses.Add((Defender, typeof(DisorientedStatusEffect)));
        _adjustment.GetAbilityCriticalRate(Attacker, SkillType.Throwing, true, 0, Defender).Should().Be(CombatRules.CalculateAbilityCriticalChance(20));
        _statuses.Clear();
        _categories.Add((Defender, StatusEffectCategory.Bleeding));
        _adjustment.GetAbilityCriticalRate(Attacker, SkillType.Throwing, false, 0, Defender).Should().Be(CombatRules.CalculateAbilityCriticalChance(20));
        _adjustment.GetAbilityCriticalRate(Attacker, SkillType.Throwing, false).Should().Be(none, "no defender");

        _categories.Clear();
        _notFacing = true;
        _adjustment.GetAbilityCriticalRate(Attacker, SkillType.Rifle, false, 0, Defender).Should().Be(CombatRules.CalculateAbilityCriticalChance(40));
        _adjustment.GetAbilityCriticalRate(Attacker, SkillType.Rifle, false).Should().Be(none, "no defender to face");
        _notFacing = false;
        _statuses.Add((Defender, typeof(SunderStatusEffect)));
        _adjustment.GetAbilityCriticalRate(Attacker, SkillType.Rifle, false, 0, Defender).Should().Be(CombatRules.CalculateAbilityCriticalChance(80));
    }

    [Test]
    public void TheCriticalRatingRollsOnceAgainstThePositiveRateAndNotAtAll()
    {
        _skillCritical = 30;
        var rate = CombatRules.CalculateAbilityCriticalChance(30);
        rate.Should().BeGreaterThan(0);

        var seed = Enumerable.Range(1, 500).First(s => Rolls(s, 1)[0] <= rate);
        var rolls = Rolls(seed, 2);
        _adjustment.CalculateAbilityCriticalRating(Attacker, SkillType.Rifle, false, 0, Defender).Should().Be(CombatRules.StandardCriticalRating);
        Random.D100(1).Should().Be(rolls[1], "the rating drew exactly one roll");

        seed = Enumerable.Range(1, 500).First(s => Rolls(s, 1)[0] > rate);
        rolls = Rolls(seed, 2);
        _adjustment.CalculateAbilityCriticalRating(Attacker, SkillType.Rifle, false, 0, Defender).Should().Be(0);
        Random.D100(1).Should().Be(rolls[1]);

        _invalid.Add(Gone);
        rolls = Rolls(3, 1);
        _adjustment.CalculateAbilityCriticalRating(Gone, SkillType.Rifle, false).Should().Be(0);
        Random.D100(1).Should().Be(rolls[0], "a rate of zero draws nothing");
    }

    [Test]
    public void ThePerkTargetingBranchOfTheSharedAdjustmentAddsTheTargetedBonus()
    {
        // Both callers pass false today; the branch is kept as it was.
        var method = typeof(CombatAbilityHitAdjustmentService).GetMethod("GetAbilityHitOrCriticalAdjustment", BindingFlags.Public | BindingFlags.Instance)!;
        _targeted = 9;
        _skillSelected[StatType.AbilityHitChancePercentAdjustment] = 1;

        method.Invoke(_adjustment, new object[]
        {
            Attacker, SkillType.Rifle, PerkType.DoubleShot, StatType.AbilityHitChancePercentAdjustmentSkillType,
            StatType.AbilityHitChancePercentAdjustmentPerkType, StatType.AbilityHitChancePercentAdjustmentSecondaryPerkType,
            StatType.AbilityHitChancePercentAdjustment, true, Defender
        }).Should().Be(1 + 9);
        method.Invoke(_adjustment, new object[]
        {
            Attacker, SkillType.Rifle, PerkType.DoubleShot, StatType.AbilityHitChancePercentAdjustmentSkillType,
            StatType.AbilityHitChancePercentAdjustmentPerkType, StatType.AbilityHitChancePercentAdjustmentSecondaryPerkType,
            StatType.AccuracyPercentAdjustment, false, Defender
        }).Should().Be(0, "only the hit chance and critical rate stats read the long-range bonus");
    }

    // ------------------------------------------------------------------ evaded Force ability

    [Test]
    public void AnEvadedForceAbilityGrantsForceDefenseAndStaminaOnItsCooldown()
    {
        Stat(Defender, StatType.ForceAbilityEvadedForceDefensePercentAdjustment, 15);
        Stat(Defender, StatType.ForceAbilityEvadedDurationSeconds, 6);
        Stat(Defender, StatType.ForceAbilityEvadedStaminaRestore, 3);
        Stat(Defender, StatType.ForceAbilityEvadedCooldownSeconds, 12);
        _service.SetAbilityHitResolutionOverride(false);
        try
        {
            _service.TryResolveAbilityHit(Attacker, Defender, SkillType.Force, PerkType.Invalid, out _).Should().BeFalse();
            _log.Should().ContainInOrder(
                $"Trigger {Defender} {StatType.ForceAbilityEvadedForceDefensePercentAdjustment} 12",
                $"Replace {Defender} {StatType.ForceDefensePercentAdjustment} 15 6 {StatType.ForceAbilityEvadedForceDefensePercentAdjustment}",
                $"Stamina {Defender} {StatType.ForceAbilityEvadedStaminaRestore} 3");

            _log.Clear();
            _triggerReady = false;
            _service.TryResolveAbilityHit(Attacker, Defender, SkillType.Force, PerkType.Invalid, out _);
            _log.Should().NotContain(line => line.StartsWith("Replace") || line.StartsWith("Stamina"), "on cooldown");

            _log.Clear();
            _triggerReady = true;
            Stat(Defender, StatType.ForceAbilityEvadedForceDefensePercentAdjustment, 0);
            _service.TryResolveAbilityHit(Attacker, Defender, SkillType.Force, PerkType.Invalid, out _);
            _log.Should().Contain($"Stamina {Defender} {StatType.ForceAbilityEvadedStaminaRestore} 3");
            _log.Should().NotContain(line => line.StartsWith("Replace"));

            _log.Clear();
            Stat(Defender, StatType.ForceAbilityEvadedForceDefensePercentAdjustment, 15);
            Stat(Defender, StatType.ForceAbilityEvadedStaminaRestore, 0);
            _service.TryResolveAbilityHit(Attacker, Defender, SkillType.Force, PerkType.Invalid, out _);
            _log.Should().Contain(line => line.StartsWith("Replace"));
            _log.Should().NotContain(line => line.StartsWith("Stamina"));

            _log.Clear();
            Stat(Defender, StatType.ForceAbilityEvadedForceDefensePercentAdjustment, 0);
            _service.TryResolveAbilityHit(Attacker, Defender, SkillType.Force, PerkType.Invalid, out _);
            _log.Should().NotContain(line => line.StartsWith("Trigger"), "nothing to grant");

            Stat(Defender, StatType.ForceAbilityEvadedStaminaRestore, 3);
            Stat(Defender, StatType.ForceAbilityEvadedDurationSeconds, 0);
            _service.TryResolveAbilityHit(Attacker, Defender, SkillType.Force, PerkType.Invalid, out _);
            _log.Should().NotContain(line => line.StartsWith("Trigger"), "no duration");
        }
        finally
        {
            _service.SetAbilityHitResolutionOverride(null);
        }
    }

    // ------------------------------------------------------------------ riders

    [Test]
    public void ARangedAbilityLowersTheTargetsPhysicalDefense()
    {
        Stat(Attacker, StatType.RangedAbilityTargetDefenseReductionPercent, 20);
        Stat(Attacker, StatType.RangedAbilityTargetDefenseReductionDurationSeconds, 9);

        _adjustment.ApplyRangedAbilityTargetDefenseReduction(Attacker, Defender, SkillType.Rifle);
        _log.Should().Equal($"Replace {Defender} {StatType.PhysicalDefensePercentAdjustment} -20 9 {StatType.RangedAbilityTargetDefenseReductionPercent}");

        _log.Clear();
        _adjustment.ApplyRangedAbilityTargetDefenseReduction(Attacker, Defender, SkillType.Vibroblade);
        _invalid.Add(Gone);
        _adjustment.ApplyRangedAbilityTargetDefenseReduction(Gone, Defender, SkillType.Rifle);
        _adjustment.ApplyRangedAbilityTargetDefenseReduction(Attacker, Gone, SkillType.Rifle);
        Stat(Attacker, StatType.RangedAbilityTargetDefenseReductionDurationSeconds, 0);
        _adjustment.ApplyRangedAbilityTargetDefenseReduction(Attacker, Defender, SkillType.Rifle);
        Stat(Attacker, StatType.RangedAbilityTargetDefenseReductionDurationSeconds, 9);
        Stat(Attacker, StatType.RangedAbilityTargetDefenseReductionPercent, 0);
        _adjustment.ApplyRangedAbilityTargetDefenseReduction(Attacker, Defender, SkillType.Rifle);
        _log.Should().BeEmpty();
    }

    [Test]
    public void TheSunderedTargetBonusesNeedAValidSunderedTarget()
    {
        Stat(Attacker, StatType.HitChanceAgainstSunderedTargetPercentAdjustment, 5);
        Stat(Attacker, StatType.CriticalRateAgainstSunderedTargetPercentAdjustment, 6);

        _adjustment.GetHitChanceAgainstSunderedTargetAdjustment(Attacker, Defender).Should().Be(0);
        _adjustment.GetCriticalRateAgainstSunderedTargetAdjustment(Attacker, Defender).Should().Be(0);
        _statuses.Add((Defender, typeof(SunderStatusEffect)));
        _adjustment.GetHitChanceAgainstSunderedTargetAdjustment(Attacker, Defender).Should().Be(5);
        _adjustment.GetCriticalRateAgainstSunderedTargetAdjustment(Attacker, Defender).Should().Be(6);
        _invalid.Add(Defender);
        _adjustment.GetHitChanceAgainstSunderedTargetAdjustment(Attacker, Defender).Should().Be(0);
        _adjustment.GetCriticalRateAgainstSunderedTargetAdjustment(Attacker, Defender).Should().Be(0);
    }

    [Test]
    public void TheContainerBuildsTheHitAdjustmentPartWithItsInjectedNeighbours()
    {
        _facades.Get<ICombatAbilityHitAdjustmentService>().Should().BeOfType<CombatAbilityHitAdjustmentService>();
    }
}
