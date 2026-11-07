using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;
using CombatFacade = SWLOR.Game.Server.Service.Combat;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The pure combat rules, called directly as the combat simulator and the entity constructors call them, and the
/// forwarders that keep every member on <c>Combat</c>: same name, signature and defaults, and each passes its own
/// parameters, in order, to the rule of the same name.
/// </summary>
[TestFixture]
public class CombatRulesTests
{
    private const BindingFlags Statics = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly;

    private static TDelegate Rule<TDelegate>(string name)
        where TDelegate : Delegate =>
        (typeof(CombatRules).GetMethod(name, Statics) ?? throw new MissingMethodException(nameof(CombatRules), name))
        .CreateDelegate<TDelegate>();

    private static StatAdjustmentSource Source(params (StatType Stat, int Value)[] stats) =>
        new("test", stats.ToDictionary(stat => stat.Stat, stat => stat.Value));

    [Test]
    public void TheDamageRangeScalesWithTheAttackRatioAndCriticalRating()
    {
        CombatRules.CalculateDamageRange(100, 100, 20, 100, 20, 0).Should().Be((70, 100));
        CombatRules.CalculateDamageRange(100, 100, 20, 100, 20, 2).Should().Be((100, 125));
        CombatRules.CalculateDamageRange(100, 100, 20, 100, 20, 3).Should().Be((100, 150));
        CombatRules.CalculateDamageRange(1000, 100, 20, 100, 20, 0).Should().Be((253, 362), "the ratio is capped at 3.625");
        CombatRules.CalculateDamageRange(1, 100, 20, 1000, 20, 0).Should().Be((1, 1), "the ratio floor still deals at least 1");
        CombatRules.CalculateDamageRange(100, 100, 20, 0, 20, 0).Should().Be(CombatRules.CalculateDamageRange(100, 100, 20, 1, 20, 0));
    }

    [Test]
    public void TheDamageRangeCapsTheStatDeltaByTheWeaponRank()
    {
        CombatRules.CalculateDamageRange(100, 100, 40, 100, 0, 0, 2).Should().Be((77, 110), "+14 is capped at 8 + 2");
        CombatRules.CalculateDamageRange(100, 100, 0, 100, 40, 0, 2).Should().Be((68, 98), "-14 is capped at -2");
        CombatRules.CalculateDamageRange(100, 0, 0, 100, 10, 0).Should().Be((-2, -3), "no floor when the attacker has no DMG");
    }

    [Test]
    public void TheHitRateIsClampedBetweenTheFloorAndTheCap()
    {
        CombatRules.CalculateHitRate(120, 100, 0).Should().Be(85);
        CombatRules.CalculateHitRate(99, 100, 0).Should().Be(74, "half of -1 rounds down");
        CombatRules.CalculateHitRate(0, 100, -10).Should().Be(CombatRules.MinimumHitRate);
        CombatRules.CalculateHitRate(0, 100, 0, 35).Should().Be(35);
        CombatRules.CalculateHitRate(200, 0, 0).Should().Be(CombatRules.MaximumHitRate);
    }

    [Test]
    public void TheCriticalRateAddsSkillAndCappedStatBonuses()
    {
        CombatRules.CalculateCriticalRate(10, 10, 0, 0).Should().Be(5);
        CombatRules.CalculateCriticalRate(10, 10, 50, 0).Should().Be(10);
        CombatRules.CalculateCriticalRate(30, 10, 0, 0).Should().Be(8, "the stat bonus is capped at 3");
        CombatRules.CalculateCriticalRate(10, 10, -10, 0).Should().Be(5);
        CombatRules.CalculateCriticalRate(10, 10, 0, -100).Should().Be(CombatRules.MinimumCriticalRate);
        CombatRules.CalculateCriticalRate(10, 10, 0, 100).Should().Be(CombatRules.MaximumCriticalRate);
        CombatRules.CalculateAbilityCriticalChance(10).Should().Be(15);
        CombatRules.CalculateAbilityCriticalChance(-100).Should().Be(CombatRules.MinimumCriticalRate);
        CombatRules.CalculateAbilityCriticalChance(1000).Should().Be(CombatRules.MaximumCriticalRate);
        CombatRules.ClampCriticalDamagePercentAdjustment(500).Should().Be(CombatRules.MaximumCriticalDamagePercentAdjustment);
        CombatRules.ClampCriticalDamagePercentAdjustment(50).Should().Be(50);
    }

    [Test]
    public void DamageAdjustmentsKeepTheirFloorsAndCaps()
    {
        CombatRules.ApplyOrdinaryAttackDamageMultiplier(100, 25).Should().Be(125);
        CombatRules.ApplyOrdinaryAttackDamageMultiplier(100, -150).Should().Be(0);
        CombatRules.ApplyOrdinaryAttackDamageMultiplier(-5, 10).Should().Be(0);
        CombatRules.ApplyOrdinaryAttackDamageMultiplier(int.MaxValue, 100).Should().Be(int.MaxValue);

        CombatRules.CapOutgoingDamageBonus(0, 50, 5).Should().Be(0);
        CombatRules.CapOutgoingDamageBonus(100, 250, 5).Should().Be(205);
        CombatRules.CapOutgoingDamageBonus(100, 150, 0).Should().Be(150);
        CombatRules.CapOutgoingDamageBonus(100, -50, 0).Should().Be(1);
        CombatRules.CapOutgoingDamageBonus(100, 250, 0, 50).Should().Be(150);

        CombatRules.ApplyCombinedDamageTakenAdjustment(0, -50, -50).Should().Be(0);
        CombatRules.ApplyCombinedDamageTakenAdjustment(100, 0, 0).Should().Be(100);
        CombatRules.ApplyCombinedDamageTakenAdjustment(100, -50, -50).Should().Be(50);
        CombatRules.ApplyCombinedDamageTakenAdjustment(1, -50, -50).Should().Be(1);

        CombatRules.ApplyPercentDamageAdjustment(100, 50).Should().Be(150);
        CombatRules.ApplyPercentDamageAdjustment(100, -50).Should().Be(50);
        CombatRules.ApplyPercentDamageAdjustment(1, -95).Should().Be(1);
        CombatRules.ApplyPercentDamageAdjustment(0, 50).Should().Be(0);
        CombatRules.ApplyPercentDamageAdjustment(100, 0).Should().Be(100);
        CombatRules.ApplyPercentDamageAdjustment(100, -200).Should().Be(CombatRules.ApplyPercentDamageAdjustment(100, -95));

        CombatRules.ApplyTargetHPDamageAdjustment(100, 20, 100, 25, 50).Should().Be(150);
        CombatRules.ApplyTargetHPDamageAdjustment(100, 30, 100, 25, 50).Should().Be(100);
        CombatRules.ApplyTargetHPDamageAdjustment(100, 20, 100, 25, 0).Should().Be(100);
        CombatRules.ApplyTargetHPDamageAdjustment(100, 20, 0, 25, 50).Should().Be(100);

        CombatRules.ApplyDefenseIgnore(100, 25).Should().Be(75);
        CombatRules.ApplyDefenseIgnore(100, 150).Should().Be(0);
        CombatRules.ApplyDefenseIgnore(0, 25).Should().Be(0);
        CombatRules.ApplyDefenseIgnore(100, 0).Should().Be(100);

        CombatRules.ApplyTriggeredDamageTargetAdjustment(100, -50, null).Should().Be((50, -50));
        CombatRules.ApplyTriggeredDamageTargetAdjustment(100, -50, 0).Should().Be((100, 0));
        CombatRules.ApplyTriggeredDamageTargetAdjustment(100, -50, 20).Should().Be((100, 20));

        CombatRules.GetIncomingPhysicalToForceConversionPortion(100, 50).Should().Be(50);
        CombatRules.GetIncomingPhysicalToForceConversionPortion(5, 50).Should().Be(3);
        CombatRules.GetIncomingPhysicalToForceConversionPortion(0, 50).Should().Be(0);
        CombatRules.GetIncomingPhysicalToForceConversionPortion(10, 0).Should().Be(0);
        CombatRules.GetIncomingPhysicalToForceConversionPortion(10, 150).Should().Be(10);
    }

    [Test]
    public void AutoAttackProcsAndSplashesReadTheirOwnSource()
    {
        var proc = Source((StatType.AutoAttackDamageBonusChance, 50), (StatType.AutoAttackDamageBonus, 10));
        CombatRules.CalculateAutoAttackProcDamage(new[] { proc, proc }, () => 30).Should().Be(20);
        CombatRules.CalculateAutoAttackProcDamage(new[] { proc }, () => 60).Should().Be(0);
        CombatRules.CalculateAutoAttackProcDamage(new[] { Source((StatType.AutoAttackDamageBonus, 10)) }, () => 1).Should().Be(0);

        var splash = Source(
            (StatType.AutoAttackSplashDamage, 5),
            (StatType.AutoAttackSplashChance, 20),
            (StatType.AutoAttackSplashRadiusMeters, 3),
            (StatType.AutoAttackSplashMaximumTargets, 2));
        CombatRules.CanTriggerAutoAttackSplash(splash, SkillType.Rifle).Should().BeTrue();
        CombatRules.CanTriggerAutoAttackSplash(splash, SkillType.FirstAid).Should().BeFalse();
        CombatRules.CanTriggerAutoAttackSplash(
            Source((StatType.AutoAttackSplashDamage, 5), (StatType.AutoAttackSplashChance, 20),
                (StatType.AutoAttackSplashRadiusMeters, 3), (StatType.AutoAttackSplashMaximumTargets, 1)),
            SkillType.Rifle).Should().BeFalse();
        CombatRules.CanTriggerAutoAttackSplash(
            Source((StatType.AutoAttackSplashDamage, 5), (StatType.AutoAttackSplashChance, 20),
                (StatType.AutoAttackSplashRadiusMeters, 3), (StatType.AutoAttackSplashMaximumTargets, 2),
                (StatType.AutoAttackSplashSkillType, (int)SkillType.Pistol)),
            SkillType.Rifle).Should().BeFalse();

        CombatRules.SelectAutoAttackSplashSecondaryTargets(new uint[] { 1, 2, 2, 3, 4 }, 2, 3).Should().Equal(1u, 3u);
        CombatRules.SelectSecondaryDamageTargets(new uint[] { 1, 1, 3 }, 2, 5).Should().Equal(1u, 3u);
        CombatRules.SelectSecondaryDamageTargets(new uint[] { 1, 3 }, 2, -1).Should().BeEmpty();

        var bleeding = Source(
            (StatType.BleedingTargetAbilitySplashChance, 40),
            (StatType.BleedingTargetAbilitySplashDamage, 5),
            (StatType.BleedingTargetAbilitySplashRadiusMeters, 3),
            (StatType.BleedingTargetAbilitySplashMaximumTargets, 2),
            (StatType.AbilityDamageToBleedingTargetSkillType, (int)SkillType.Throwing));
        CombatRules.CanTriggerBleedingTargetAbilitySplash(bleeding, SkillType.Throwing, true, 40).Should().BeTrue();
        CombatRules.CanTriggerBleedingTargetAbilitySplash(bleeding, SkillType.Throwing, true, 41).Should().BeFalse();
        CombatRules.CanTriggerBleedingTargetAbilitySplash(bleeding, SkillType.Throwing, true, 0).Should().BeFalse();
        CombatRules.CanTriggerBleedingTargetAbilitySplash(bleeding, SkillType.Throwing, false, 10).Should().BeFalse();
        CombatRules.CanTriggerBleedingTargetAbilitySplash(bleeding, SkillType.Rifle, true, 10).Should().BeFalse();
    }

    [Test]
    public void HealingBudgetsAndCapsStayWithinTheDamageDealt()
    {
        CombatRules.CalculateMaxHPHealingBudget(200, 15).Should().Be(30);
        CombatRules.CalculateMaxHPHealingBudget(-200, 15).Should().Be(0);
        CombatRules.CalculateMaxHPHealingBudget(int.MaxValue, 100).Should().Be(int.MaxValue);
        CombatRules.CalculateDamageEligibleForHealing(50, 30).Should().Be(30);
        CombatRules.CalculateDamageEligibleForHealing(50, 30, 10).Should().Be(20);
        CombatRules.CalculateDamageEligibleForHealing(-5, 30).Should().Be(0);
        CombatRules.CalculateCappedDamageDerivedHealingAmount(100, 20, 50).Should().Be(30);
        CombatRules.CalculateCappedDamageDerivedHealingAmount(100, 0, 10).Should().Be(10);
        CombatRules.CalculateCappedDamageDerivedHealingAmount(0, 0, 10).Should().Be(0);
        CombatRules.CalculateCappedDamageDerivedHealingAmount(100, 0, 0).Should().Be(0);
        CombatRules.CalculateHitPointsSpentThroughTemporaryHitPoints(50, 20).Should().Be(30);
        CombatRules.CalculateHitPointsSpentThroughTemporaryHitPoints(10, 20).Should().Be(0);
        CombatRules.CalculateHitPointsSpentThroughTemporaryHitPoints(50, -5).Should().Be(50);
        CombatRules.CalculateHitPointSpendStaminaRestore(100, 10, 20, 20).Should().Be(10);
        CombatRules.CalculateHitPointSpendStaminaRestore(100, 10, 10, 20).Should().Be(5);
        CombatRules.CalculateHitPointSpendStaminaRestore(100, 10, 0, 20).Should().Be(0);
        CombatRules.CalculateHitPointSpendStaminaRestore(0, 10, 20, 20).Should().Be(0);
        CombatRules.CalculateAbilityHitStaminaRestore(10, 2, 20).Should().Be(7);
        CombatRules.CalculateAbilityHitStaminaRestore(10, 2, -5).Should().Be(0);

        var restore = Rule<Func<int, int, int>>("CalculateResourceRestoreFromCost");
        restore(100, 50).Should().Be(50);
        restore(100, 200).Should().Be(95);
        restore(0, 50).Should().Be(0);
        restore(100, 0).Should().Be(0);
    }

    [Test]
    public void DeflectingReturnReflectsAShareOfTheAttackCappedByTheDeflector()
    {
        CombatRules.GetRangedDeflectionReflectionAmount(100, 50, 40, 100).Should().Be(40);
        CombatRules.GetRangedDeflectionReflectionAmount(100, 50, 40, 0).Should().Be(50);
        CombatRules.GetRangedDeflectionReflectionAmount(0, 50, 40, 100).Should().Be(0);
        CombatRules.GetRangedDeflectionReflectionAmount(100, 0, 40, 100).Should().Be(0);
        CombatRules.GetRangedDeflectionReflectionRates(30, 50, 5, 3, 10, 0, 0).Should().Be((40, 50));
        CombatRules.GetRangedDeflectionReflectionRates(30, 50, 2, 3, 10, 0, 0).Should().Be((30, 50));
        CombatRules.GetRangedDeflectionReflectionRates(30, 50, 5, 3, 10, 70, 0).Should().Be((70, 50));
        CombatRules.GetRangedDeflectionReflectionRates(30, 50, 5, 3, 10, 70, 60).Should().Be((70, 60));
        CombatRules.GetRangedDeflectionReflectionRates(-30, -50, 5, 0, 10, 0, 0).Should().Be((0, 0));
        typeof(CombatRules).GetField("DeflectingReturnCooldownSeconds", Statics)!.GetRawConstantValue().Should().Be(6);

        var name = Rule<Func<DeflectionSource, string>>("GetDeflectionResultName");
        name(DeflectionSource.Melee).Should().Be("melee deflect");
        name(DeflectionSource.Ranged).Should().Be("ranged deflect");
        name(DeflectionSource.Shield).Should().Be("shield deflect");
        name((DeflectionSource)999).Should().Be("deflect");
    }

    [Test]
    public void SkillClassificationFollowsTheSkillAttributes()
    {
        CombatRules.IsWeaponSkillType(SkillType.Rifle).Should().BeTrue();
        CombatRules.IsWeaponSkillType(SkillType.FirstAid).Should().BeFalse();
        CombatRules.IsWeaponSkillType(SkillType.Invalid).Should().BeFalse();
        CombatRules.IsRangedDamageSkill(SkillType.Devices).Should().BeTrue();
        CombatRules.IsRangedDamageSkill(SkillType.Vibroblade).Should().BeFalse();
        CombatRules.IsRangedWeaponSkill(SkillType.TwinPistol).Should().BeTrue();
        CombatRules.IsRangedWeaponSkill(SkillType.Devices).Should().BeFalse();
        CombatRules.IsMeleeWeaponSkill(SkillType.Vibroblade).Should().BeTrue();
        CombatRules.IsMeleeWeaponSkill(SkillType.Pistol).Should().BeFalse();
        CombatRules.GetWeaponEngagementRange(SkillType.Pistol).Should().Be(CombatRules.RangedWeaponEngagementRange);
        CombatRules.GetWeaponEngagementRange(SkillType.Vibroblade).Should().Be(CombatRules.MeleeWeaponEngagementRange);
        CombatRules.CanWeaponSkillTriggerAbility(SkillType.FirstAid, SkillType.Rifle).Should().BeFalse();
        CombatRules.CanWeaponSkillTriggerAbility(SkillType.Pistol, SkillType.Rifle).Should().BeTrue();
        CombatRules.CanWeaponSkillTriggerAbility(SkillType.FirstAid, SkillType.FirstAid).Should().BeTrue();

        Rule<Func<SkillType, SkillType, bool>>("SkillTypeMatches")(SkillType.Rifle, SkillType.Invalid).Should().BeFalse();
        Rule<Func<SkillType, SkillType, bool>>("SkillTypeMatches")(SkillType.Rifle, SkillType.Rifle).Should().BeTrue();
        Rule<Func<SkillType, SkillType, bool>>("SkillTypeMatchesOrGlobal")(SkillType.Rifle, SkillType.Invalid).Should().BeTrue();
        Rule<Func<SkillType, SkillType, bool>>("SkillTypeMatchesOrGlobal")(SkillType.Rifle, SkillType.Pistol).Should().BeFalse();

        CombatRules.AbilityUsedTriggerSkillMatches(SkillType.Rifle, SkillType.Rifle, false, false).Should().BeTrue();
        CombatRules.AbilityUsedTriggerSkillMatches(SkillType.Rifle, SkillType.Invalid, false, true).Should().BeFalse();
        CombatRules.AbilityUsedTriggerSkillMatches(SkillType.Pistol, SkillType.Invalid, true, true).Should().BeTrue();
        CombatRules.AbilityUsedTriggerSkillMatches(SkillType.Pistol, SkillType.Invalid, true, false).Should().BeFalse();
        CombatRules.AbilityUsedTriggerSkillMatches(SkillType.Vibroblade, SkillType.Invalid, true, true).Should().BeFalse();

        CombatRules.MatchesNextSkillAbilityCriticalRateSkill(SkillType.Rifle, SkillType.Invalid, true).Should().BeTrue();
        CombatRules.MatchesNextSkillAbilityCriticalRateSkill(SkillType.Vibroblade, SkillType.Invalid, true).Should().BeFalse();
        CombatRules.MatchesNextSkillAbilityCriticalRateSkill(SkillType.Rifle, SkillType.Rifle, false).Should().BeTrue();
        CombatRules.MatchesNextSkillAbilityCriticalRateSkill(SkillType.Rifle, SkillType.Invalid, false).Should().BeFalse();
    }

    [Test]
    public void SelectedStatAdjustmentsAndDiscountsMatchTheirSkill()
    {
        var sources = new[]
        {
            Source((StatType.NextSkillAbilityStaminaCostAdjustmentSkillType, (int)SkillType.Rifle), (StatType.AbilityCriticalRatePercentAdjustment, 10)),
            Source((StatType.AbilityCriticalRatePercentAdjustment, 5)),
        };
        CombatRules.SumSkillSelectedStatAdjustment(sources, SkillType.Rifle, StatType.NextSkillAbilityStaminaCostAdjustmentSkillType, StatType.AbilityCriticalRatePercentAdjustment)
            .Should().Be(10);
        CombatRules.SumSkillSelectedStatAdjustment(sources, SkillType.Rifle, StatType.NextSkillAbilityStaminaCostAdjustmentSkillType, StatType.AbilityCriticalRatePercentAdjustment, true)
            .Should().Be(15);
        CombatRules.SumSkillSelectedStatAdjustment(sources, SkillType.Pistol, StatType.NextSkillAbilityStaminaCostAdjustmentSkillType, StatType.AbilityCriticalRatePercentAdjustment, true)
            .Should().Be(5);

        CombatRules.MatchesStaminaDiscount(Source(), SkillType.Invalid, true).Should().BeFalse();
        CombatRules.MatchesStaminaDiscount(Source(), SkillType.Vibroblade, false).Should().BeTrue();
        CombatRules.MatchesStaminaDiscount(Source((StatType.NextSkillAbilityStaminaCostAdjustmentHostileOnly, 1)), SkillType.Rifle, false).Should().BeFalse();
        CombatRules.MatchesStaminaDiscount(Source((StatType.NextSkillAbilityStaminaCostAdjustmentRangedOnly, 1)), SkillType.Vibroblade, true).Should().BeFalse();
        CombatRules.MatchesStaminaDiscount(Source((StatType.NextSkillAbilityStaminaCostAdjustmentSkillType, (int)SkillType.Rifle)), SkillType.Pistol, true).Should().BeFalse();

        CombatRules.CanConsumeNextAbilityNoDelay(null).Should().BeFalse();
        CombatRules.CanConsumeNextAbilityNoDelay(new AbilityDetail { IsHostileAbility = true }).Should().BeFalse();
        CombatRules.CanConsumeNextAbilityNoDelay(new AbilityDetail { IsHostileAbility = true, SkillType = SkillType.Rifle }).Should().BeTrue();
        Rule<Func<AbilityDetail, int, bool, bool>>("IsSuccessfulHostileAbilityImpact")(null, 10, true).Should().BeFalse();
        Rule<Func<AbilityDetail, int, bool, bool>>("IsSuccessfulHostileAbilityImpact")(new AbilityDetail { IsHostileAbility = true }, 10, false).Should().BeTrue();
        Rule<Func<AbilityDetail, int, bool, bool>>("IsSuccessfulHostileAbilityImpact")(new AbilityDetail { IsHostileAbility = true }, 0, true).Should().BeTrue();
        Rule<Func<AbilityDetail, int, bool, bool>>("IsSuccessfulHostileAbilityImpact")(new AbilityDetail { IsHostileAbility = true }, 0, false).Should().BeFalse();
        Rule<Func<AbilityDetail, int, bool, bool>>("IsSuccessfulHostileAbilityImpact")(new AbilityDetail(), 10, true).Should().BeFalse();
    }

    [Test]
    public void StatValuesTurnIntoTheEnumsTheyName()
    {
        Rule<Func<int, SkillType>>("GetSkillTypeFromStat")((int)SkillType.Rifle).Should().Be(SkillType.Rifle);
        Rule<Func<int, SkillType>>("GetSkillTypeFromStat")(0).Should().Be(SkillType.Invalid);
        Rule<Func<int, SkillType>>("GetSkillTypeFromStat")(99999).Should().Be(SkillType.Invalid);
        Rule<Func<int, RecastGroup>>("GetRecastGroupFromStat")((int)RecastGroup.Flash).Should().Be(RecastGroup.Flash);
        Rule<Func<int, RecastGroup>>("GetRecastGroupFromStat")(-1).Should().Be(RecastGroup.Invalid);
        Rule<Func<int, StatusEffectCategory>>("GetStatusEffectCategoryFromStat")(5).Should().Be(StatusEffectCategory.Debuff | StatusEffectCategory.Bleeding);
        Rule<Func<int, StatusEffectCategory>>("GetStatusEffectCategoryFromStat")(0).Should().Be(StatusEffectCategory.None);
        Rule<Func<int, EffectIconType>>("GetEffectIconTypeFromStat")((int)EffectIconType.Regenerate).Should().Be(EffectIconType.Regenerate);
        Rule<Func<int, EffectIconType>>("GetEffectIconTypeFromStat")(0).Should().Be(EffectIconType.Invalid);
        Rule<Func<int, StatusEffectCleanseType>>("GetStatusEffectCleanseTypeFromStat")(9).Should().Be(StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet);
        Rule<Func<int, StatusEffectCleanseType>>("GetStatusEffectCleanseTypeFromStat")(16).Should().Be(StatusEffectCleanseType.None);
        Rule<Func<int, StatusEffectCleanseType>>("GetStatusEffectCleanseTypeFromStat")(-1).Should().Be(StatusEffectCleanseType.None);
        Rule<Func<int, ResistanceType>>("GetResistanceTypeFromStat")((int)ResistanceType.Fire).Should().Be(ResistanceType.Fire);
        Rule<Func<int, ResistanceType>>("GetResistanceTypeFromStat")(0).Should().Be(ResistanceType.Invalid);
        Rule<Func<int, CombatDamageType>>("GetCombatDamageTypeFromStat")((int)CombatDamageType.Force).Should().Be(CombatDamageType.Force);
        Rule<Func<int, CombatDamageType>>("GetCombatDamageTypeFromStat")(999).Should().Be(CombatDamageType.Invalid);
        Rule<Func<int, AbilityType>>("GetAbilityTypeFromStatPlusOne")(1).Should().Be((AbilityType)0);
        Rule<Func<int, AbilityType>>("GetAbilityTypeFromStatPlusOne")(0).Should().Be(AbilityType.Invalid);
        Rule<Func<int, AbilityType>>("GetAbilityTypeFromStatPlusOne")(9999).Should().Be(AbilityType.Invalid);
        Rule<Func<int, PerkType>>("GetPerkTypeFromStat")((int)PerkType.AlphaRhythm).Should().Be(PerkType.AlphaRhythm);
        Rule<Func<int, PerkType>>("GetPerkTypeFromStat")(0).Should().Be(PerkType.Invalid);
        Rule<Func<PerkType, string>>("GetPerkTypeGroup")(PerkType.AlphaRhythm).Should().Be("PerkType:1");

        Rule<Func<CombatDamageType, StatType>>("GetDamageReflectionStat")(CombatDamageType.Force).Should().Be(StatType.ForceDamageReflectionPercentAdjustment);
        Rule<Func<CombatDamageType, StatType>>("GetDamageReflectionStat")(CombatDamageType.Physical).Should().Be(StatType.PhysicalDamageReflectionPercentAdjustment);
        Rule<Func<CombatDamageType, StatType>>("GetDamageReflectionStat")(CombatDamageType.Fire).Should().Be(StatType.ElementalDamageReflectionPercentAdjustment);
        Rule<Func<StatusEffectCategory, Type>>("GetSourceStatusStackEffectType")(StatusEffectCategory.Infection).Should().Be(typeof(InfectionStatusEffect));
        Rule<Func<StatusEffectCategory, Type>>("GetSourceStatusStackEffectType")(StatusEffectCategory.Debuff).Should().BeNull();
    }

    [Test]
    public void TargetedStatGroupsAndHarmfulStatusesAreRecognised()
    {
        var group = new StatGroup();
        group.Stats[StatType.AbilityCriticalRatePercentAdjustment] = 12;
        group.Stats[StatType.NextSkillAbilityStaminaCostAdjustmentSkillType] = (int)PerkType.AlphaRhythm;
        Rule<Func<StatGroup, PerkType, StatType, StatType, StatType, int>>("GetTargetedStatGroupAdjustment")(group, PerkType.AlphaRhythm, StatType.NextSkillAbilityStaminaCostAdjustmentSkillType,
            StatType.AutoAttackSplashSkillType, StatType.AbilityCriticalRatePercentAdjustment).Should().Be(12);
        Rule<Func<StatGroup, PerkType, StatType, StatType, StatType, int>>("GetTargetedStatGroupAdjustment")(group, PerkType.Antitoxin, StatType.NextSkillAbilityStaminaCostAdjustmentSkillType,
            StatType.AutoAttackSplashSkillType, StatType.AbilityCriticalRatePercentAdjustment).Should().Be(0);
        Rule<Func<StatGroup, PerkType, StatType, StatType, StatType, int>>("GetTargetedStatGroupAdjustment")(null!, PerkType.AlphaRhythm, StatType.NextSkillAbilityStaminaCostAdjustmentSkillType,
            StatType.AutoAttackSplashSkillType, StatType.AbilityCriticalRatePercentAdjustment).Should().Be(0);

        Rule<Func<IStatusEffect, bool>>("IsTransferableHarmfulStatus")(null).Should().BeFalse();
        Rule<Func<IStatusEffect, bool>>("IsTransferableHarmfulStatus")(Status(StatusEffectCategory.Bleeding)).Should().BeTrue();
        Rule<Func<IStatusEffect, bool>>("IsTransferableHarmfulStatus")(Status(StatusEffectCategory.Buff)).Should().BeFalse();
    }

    private static IStatusEffect Status(StatusEffectCategory categories)
    {
        var effect = ScriptedProxy.Create<IStatusEffect>(out var proxy);
        proxy.On("get_Categories", _ => categories);
        return effect;
    }

    [Test]
    public void AttackDelayFollowsTheWeaponsAndTheSwingFloor()
    {
        CombatRules.CalculateAttackDelayMilliseconds(220, 0, 15, 0).Should().Be(3117);
        CombatRules.CalculateEffectiveAttackDelay(CombatRules.CalculateAttackDelayMilliseconds(220, 0, 15, 0)).Should().Be(1367);
        CombatRules.CalculateAttackDelayMilliseconds(0, 0, 0, 0).Should().Be(0);
        CombatRules.CalculateAttackDelayMilliseconds(220, 220, 0, 0).Should().Be(5583, "a pair pays the base cadence once");
        CombatRules.CalculateAttackDelayMilliseconds(220, 180, 20, 0)
            .Should().Be(CombatRules.CalculateAttackDelayMilliseconds(180, 220, 20, 0), "hand order does not change the haste factor");
        CombatRules.CalculateAttackDelayMilliseconds(220, 180, 20, 10)
            .Should().BeLessThan(CombatRules.CalculateAttackDelayMilliseconds(220, 180, 20, 0), "the offhand reduction shortens the off hand");
        CombatRules.CalculateAttackDelayMilliseconds(220, 0, -80, 0).Should().BeGreaterThan(CombatRules.CalculateAttackDelayMilliseconds(220, 0, 0, 0));
        CombatRules.CalculateEffectiveAttackDelay(1000).Should().Be(CombatRules.BaseAttackDelayMilliseconds);
        CombatRules.CalculateEffectiveAttackDelay(5000, true).Should().Be(CombatRules.MinimumAttackDelayMilliseconds);
        CombatRules.CalculateEffectiveAttackDelay(2000, false).Should().Be(CombatRules.MinimumAttackDelayMilliseconds);
        CombatRules.CalculateAttackSwingDelay(500).Should().Be(CombatRules.BaseAttackDelayMilliseconds);
        CombatRules.CalculateAttackSwingDelay(2500).Should().Be(2500);

        CombatRules.CalculateAttacksPerSwing(0, 0.5f, out var noDebt).Should().Be(1);
        noDebt.Should().Be(0f);
        CombatRules.CalculateAttacksPerSwing(875, 0f, out var even).Should().Be(2);
        even.Should().Be(0f);
        CombatRules.CalculateAttacksPerSwing(100, 0f, out var capped).Should().Be(CombatRules.MaxAttacksPerSwing);
        capped.Should().Be(CombatRules.MaxAttacksPerSwing);

        CombatRules.CapAttacksPerSwingForLimitedAttackEffect(3, 1, 0).Should().Be(1);
        CombatRules.CapAttacksPerSwingForLimitedAttackEffect(3, 1, 2).Should().Be(2);
        CombatRules.CapAttacksPerSwingForLimitedAttackEffect(3, 1, 0, 1).Should().Be(2);
        CombatRules.CapAttacksPerSwingForLimitedAttackEffect(0, 5, 9).Should().Be(1);
    }

    [Test]
    public void DefaultDefensesHoldEveryDefenseTypeInEnumOrder()
    {
        var defenses = CombatRules.CreateDefaultDefenseValues();
        defenses.Keys.Should().Equal(CombatDamageType.Physical, CombatDamageType.Force);
        defenses.Values.Should().OnlyContain(value => value == 0);
        CombatRules.CreateDefaultDefenseValues(7).Values.Should().OnlyContain(value => value == 7);
        CombatRules.CreateDefaultDefenseValues().Should().NotBeSameAs(defenses);

        var catalog = new CombatDamageCatalogService(new InMemoryDBService(), null!);
        defenses.Keys.Should().Equal(catalog.GetDefenseDamageTypes(), "the rule and the catalog name the same types in the same order");
        CombatFacade.CreateDefaultDefenseValues(3).Should().Equal(CombatRules.CreateDefaultDefenseValues(3));
    }

    [Test]
    public void CombatRepeatsEveryRuleConstant()
    {
        var facade = typeof(CombatFacade).GetFields(Statics).Where(field => field.IsLiteral).ToDictionary(field => field.Name);
        foreach (var constant in typeof(CombatRules).GetFields(Statics).Where(field => field.IsLiteral && !field.IsPrivate))
        {
            facade.Should().ContainKey(constant.Name);
            facade[constant.Name].FieldType.Should().Be(constant.FieldType, constant.Name);
            facade[constant.Name].GetRawConstantValue().Should().Be(constant.GetRawConstantValue(), constant.Name);
        }
    }

    [Test]
    public void CombatKeepsAForwarderWithTheSameSignatureForEveryRule()
    {
        var rules = typeof(CombatRules).GetMethods(Statics).Where(method => !method.IsPrivate && !method.IsSpecialName).ToArray();
        rules.Should().NotBeEmpty();

        foreach (var rule in rules)
        {
            var parameters = rule.GetParameters();
            var forwarder = typeof(CombatFacade).GetMethod(rule.Name, Statics, parameters.Select(parameter => parameter.ParameterType).ToArray());
            forwarder.Should().NotBeNull($"Combat keeps {rule.Name} for its callers");
            forwarder!.ReturnType.Should().Be(rule.ReturnType, rule.Name);
            if (rule.IsPublic)
                forwarder.IsPublic.Should().BeTrue($"{rule.Name} is public on Combat");

            var forwardedParameters = forwarder.GetParameters();
            for (var i = 0; i < parameters.Length; i++)
            {
                forwardedParameters[i].Name.Should().Be(parameters[i].Name, rule.Name);
                forwardedParameters[i].HasDefaultValue.Should().Be(parameters[i].HasDefaultValue, $"{rule.Name}({parameters[i].Name})");
                if (parameters[i].HasDefaultValue)
                    forwardedParameters[i].DefaultValue.Should().Be(parameters[i].DefaultValue, $"{rule.Name}({parameters[i].Name})");
            }
        }
    }

    [Test]
    public void EveryForwarderReturnsWhatItsRuleReturnsForTheSameArguments()
    {
        static object? Argument(Type type) => type switch
        {
            _ when type == typeof(int) => 40,
            _ when type == typeof(uint) => 2u,
            _ when type == typeof(float) || type == typeof(float).MakeByRefType() => 0.25f,
            _ when type == typeof(bool) => true,
            _ when type == typeof(int?) => (int?)5,
            _ when type == typeof(SkillType) => SkillType.Rifle,
            _ when type == typeof(StatType) => StatType.AutoAttackDamageBonus,
            _ when type == typeof(CombatDamageType) => CombatDamageType.Force,
            _ when type == typeof(StatusEffectCategory) => StatusEffectCategory.Bleeding,
            _ when type == typeof(PerkType) => PerkType.AlphaRhythm,
            _ when type == typeof(DeflectionSource) => DeflectionSource.Ranged,
            _ when type == typeof(AbilityDetail) => new AbilityDetail { IsHostileAbility = true, SkillType = SkillType.Rifle },
            _ when type == typeof(IStatusEffect) => Status(StatusEffectCategory.Debuff),
            _ when type == typeof(StatGroup) => new StatGroup(),
            _ when type == typeof(StatAdjustmentSource) => Source((StatType.AutoAttackDamageBonusChance, 50), (StatType.AutoAttackDamageBonus, 10)),
            _ when type == typeof(IEnumerable<StatAdjustmentSource>) || type == typeof(IReadOnlyList<StatAdjustmentSource>) =>
                new[] { Source((StatType.AutoAttackDamageBonusChance, 50), (StatType.AutoAttackDamageBonus, 10)) },
            _ when type == typeof(IEnumerable<uint>) => new uint[] { 1, 2, 3, 2 },
            _ when type == typeof(Func<int>) => new Func<int>(() => 1),
            _ => throw new NotSupportedException($"No test argument for {type}"),
        };

        var rules = typeof(CombatRules).GetMethods(Statics).Where(method => !method.IsPrivate && !method.IsSpecialName).ToArray();
        foreach (var rule in rules)
        {
            var types = rule.GetParameters().Select(parameter => parameter.ParameterType).ToArray();
            var forwarder = typeof(CombatFacade).GetMethod(rule.Name, Statics, types)!;
            var ruleArguments = types.Select(Argument).ToArray();
            var forwarderArguments = (object?[])ruleArguments.Clone();

            var expected = rule.Invoke(null, ruleArguments);
            var actual = forwarder.Invoke(null, forwarderArguments);

            if (expected is System.Collections.IEnumerable sequence and not string)
                actual.Should().BeEquivalentTo(sequence, options => options.WithStrictOrdering(), rule.Name);
            else
                actual.Should().Be(expected, rule.Name);
            for (var i = 0; i < types.Length; i++)
            {
                if (types[i].IsByRef)
                    forwarderArguments[i].Should().Be(ruleArguments[i], $"{rule.Name} writes its out value back");
            }
        }
    }

    [Test]
    public void EachForwarderPassesItsParametersInOrderToTheRuleOfTheSameName()
    {
        var parts = new[] { "Combat.Rules.cs", "Combat.DamageCatalog.cs" };
        var forwarders = CombatSource.Paths()
            .Where(path => parts.Contains(Path.GetFileName(path)))
            .Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path)))
            .SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>())
            .Where(method => method.ExpressionBody?.Expression is InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "CombatRules" } }
            })
            .ToArray();
        forwarders.Length.Should().Be(typeof(CombatRules).GetMethods(Statics).Count(method => !method.IsPrivate && !method.IsSpecialName));

        foreach (var forwarder in forwarders)
        {
            var call = (InvocationExpressionSyntax)forwarder.ExpressionBody!.Expression;
            ((MemberAccessExpressionSyntax)call.Expression).Name.Identifier.Text.Should().Be(forwarder.Identifier.Text);
            var arguments = call.ArgumentList.Arguments
                .Select(argument => (argument.RefKindKeyword.Text + " " + argument.Expression).Trim())
                .ToArray();
            var parameters = forwarder.ParameterList.Parameters
                .Select(parameter => (parameter.Modifiers.ToString() + " " + parameter.Identifier.Text).Trim())
                .ToArray();
            arguments.Should().Equal(parameters, forwarder.Identifier.Text);
        }
    }
}
