using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The ability and attack bonuses of <c>Combat</c>: the one-shot next-ability and next-attack bonuses and the adjustments an
/// ability reads from its user's stats.
/// </summary>
[TestFixture]
public class CombatAbilityBonusServiceTests : CombatAbilityBonusTestBase
{
    [Test]
    public void TheNextAbilityIsFreeOnlyForTheStoredSkillAndOnlyOnce()
    {
        _service.HasNextAbilityNoStaminaCost(Attacker, SkillType.Invalid).Should().BeFalse();
        _service.HasNextAbilityNoStaminaCost(Attacker, SkillType.Rifle).Should().BeFalse();
        _service.ConsumeNextAbilityNoStaminaCost(Attacker, SkillType.Rifle).Should().BeFalse();

        Store(Attacker, StatType.NextAbilityNoStaminaCostSkillType, (int)SkillType.Rifle, StatType.NextAbilityNoStaminaCostSkillType);
        _service.HasNextAbilityNoStaminaCost(Attacker, SkillType.Pistol).Should().BeFalse();
        _service.HasNextAbilityNoStaminaCost(Attacker, SkillType.Rifle).Should().BeTrue();
        _service.ConsumeNextAbilityNoStaminaCost(Attacker, SkillType.Rifle).Should().BeTrue();
        _service.HasNextAbilityNoStaminaCost(Attacker, SkillType.Rifle).Should().BeFalse("consumed");
    }

    [Test]
    public void TheNextSkillAbilityStaminaDiscountsCountOnlyTheSourcesThatMatchAndConsumeOnlyThose()
    {
        // A global discount, a rifle discount, a hostile-only discount and a pistol discount, each its own source.
        _modifiers.Replace(Attacker, StatType.NextSkillAbilityStaminaCostAdjustment, -1, 30, "global");
        _modifiers.Replace(Attacker, StatType.NextSkillAbilityStaminaCostAdjustment, -2, 30, "rifle");
        _modifiers.Replace(Attacker, StatType.NextSkillAbilityStaminaCostAdjustmentSkillType, (int)SkillType.Rifle, 30, "rifle");
        _modifiers.Replace(Attacker, StatType.NextSkillAbilityStaminaCostAdjustment, -4, 30, "hostile");
        _modifiers.Replace(Attacker, StatType.NextSkillAbilityStaminaCostAdjustmentHostileOnly, 1, 30, "hostile");
        _modifiers.Replace(Attacker, StatType.NextSkillAbilityStaminaCostAdjustment, -8, 30, "pistol");
        _modifiers.Replace(Attacker, StatType.NextSkillAbilityStaminaCostAdjustmentSkillType, (int)SkillType.Pistol, 30, "pistol");

        _service.GetNextSkillAbilityStaminaCostAdjustment(Attacker, SkillType.Rifle).Should().Be(-3);
        _service.GetNextSkillAbilityStaminaCostAdjustment(Attacker, SkillType.Rifle, true).Should().Be(-7);
        _service.GetNextSkillAbilityStaminaCostAdjustment(Attacker, SkillType.Invalid, true).Should().Be(0);

        _service.ConsumeNextSkillAbilityStaminaCostAdjustment(Attacker, SkillType.Rifle).Should().Be(-3);
        _service.GetNextSkillAbilityStaminaCostAdjustment(Attacker, SkillType.Rifle, true).Should().Be(-4, "the hostile discount stays");
        _service.ConsumeNextSkillAbilityStaminaCostAdjustment(Attacker, SkillType.Pistol, true).Should().Be(-12);
        _service.GetNextSkillAbilityStaminaCostAdjustment(Attacker, SkillType.Pistol, true).Should().Be(0);
    }

    [Test]
    public void TheNextSkillAbilityBonusesGoOnlyToTheStoredSkillAndItsRequiredCategory()
    {
        var group = StatType.NextSkillAbilitySkillType;
        _service.ConsumeNextSkillAbilityBonuses(Attacker, SkillType.Invalid).Should().Be((0, 0, 0));
        _service.ConsumeNextSkillAbilityBonuses(Attacker, SkillType.Rifle).Should().Be((0, 0, 0), "nothing stored");

        Store(Attacker, StatType.NextSkillAbilitySkillType, (int)SkillType.Rifle, group);
        Store(Attacker, StatType.NextSkillAbilityDamageBonus, 6, group);
        Store(Attacker, StatType.NextSkillAbilityCriticalRatePercentAdjustment, 10, group);
        Store(Attacker, StatType.NextSkillAbilityDefenseIgnorePercentAdjustment, 20, group);
        Store(Attacker, StatType.NextSkillAbilityRequiredCategory, (int)StatusEffectCategory.Control, group);

        _service.ConsumeNextSkillAbilityBonuses(Attacker, SkillType.Pistol, StatusEffectCategory.Control).Should().Be((0, 0, 0));
        _service.ConsumeNextSkillAbilityBonuses(Attacker, SkillType.Rifle, StatusEffectCategory.Debuff).Should().Be((0, 0, 0), "it waits for a control ability");
        _service.ConsumeNextSkillAbilityBonuses(Attacker, SkillType.Rifle, StatusEffectCategory.Control | StatusEffectCategory.Debuff)
            .Should().Be((6, 10, 20));
        Stored(Attacker, StatType.NextSkillAbilityRequiredCategory, group).Should().Be(0);
        Stored(Attacker, StatType.NextSkillAbilitySkillType, group).Should().Be(0);
        _service.ConsumeNextSkillAbilityBonuses(Attacker, SkillType.Rifle, StatusEffectCategory.Control).Should().Be((0, 0, 0));

        Store(Attacker, StatType.NextSkillAbilitySkillType, (int)SkillType.Rifle, group);
        Store(Attacker, StatType.NextSkillAbilityDamageBonus, 3, group);
        _service.ConsumeNextSkillAbilityBonuses(Attacker, SkillType.Rifle).Should().Be((3, 0, 0), "no category is required");
    }

    [Test]
    public void ThePersistentNextAbilityCriticalRateMatchesItsSkillOrAnyRangedSkillAndIsConsumedWithItsTracker()
    {
        var group = StatType.PersistentNextSkillAbilityCriticalRateSkillType;
        Store(Attacker, StatType.PersistentNextSkillAbilityCriticalRatePercentAdjustment, 15, group);
        Store(Attacker, StatType.PersistentNextSkillAbilityCriticalRateSkillType, (int)SkillType.Pistol, group);

        _service.GetPersistentNextSkillAbilityCriticalRateBonus(Attacker, SkillType.Rifle).Should().Be(0);
        _service.ConsumePersistentNextSkillAbilityCriticalRateBonus(Attacker, SkillType.Rifle).Should().Be(0);
        Removed().Should().BeEmpty();
        _service.GetPersistentNextSkillAbilityCriticalRateBonus(Attacker, SkillType.Pistol).Should().Be(15);

        _service.ConsumePersistentNextSkillAbilityCriticalRateBonus(Attacker, SkillType.Pistol).Should().Be(15);
        Removed().Should().Equal($"{Attacker} {nameof(CriticalRateStackTrackerStatusEffect)}");
        Stored(Attacker, StatType.PersistentNextSkillAbilityCriticalRateSkillType, group).Should().Be(0);
        _service.GetPersistentNextSkillAbilityCriticalRateBonus(Attacker, SkillType.Pistol).Should().Be(0);

        Store(Attacker, StatType.PersistentNextSkillAbilityCriticalRatePercentAdjustment, 8, group);
        Store(Attacker, StatType.PersistentNextSkillAbilityCriticalRateRangedOnly, 1, group);
        _service.GetPersistentNextSkillAbilityCriticalRateBonus(Attacker, SkillType.Vibroblade).Should().Be(0, "ranged only");
        _service.ConsumePersistentNextSkillAbilityCriticalRateBonus(Attacker, SkillType.Rifle).Should().Be(8);
        Stored(Attacker, StatType.PersistentNextSkillAbilityCriticalRateRangedOnly, group).Should().Be(0);
    }

    [Test]
    public void TheGuardedHitBonusesOfTheNextAttackAreConsumedTogether()
    {
        var group = StatType.NextAttackGuardedHitDMGBonus;
        Store(Attacker, StatType.NextAttackGuardedHitDMGBonus, 7, group);
        Store(Attacker, StatType.NextAttackGuardedHitEnmityBonus, 40, group);
        Store(Attacker, StatType.NextAttackGuardedHitCriticalRatePercentAdjustment, 11, group);

        _service.ConsumeNextAttackGuardedHitBonuses(Attacker).Should().Be((7, 11, 40));
        _service.ConsumeNextAttackGuardedHitBonuses(Attacker).Should().Be((0, 0, 0));

        Store(Attacker, StatType.NextAttackGuardedHitDMGBonus, 2, group);
        Store(Attacker, StatType.NextAttackGuardedHitCriticalRatePercentAdjustment, 3, group);
        _service.ConsumeNextAttackGuardedHitAutoAttackBonuses(Attacker).Should().Be((2, 0));
        _service.ConsumeNextAttackGuardedHitCriticalRateBonus(Attacker).Should().Be(3);
    }

    [Test]
    public void TheStatusAppliedNextAttackDamageBonusIsReadAndConsumed()
    {
        Store(Attacker, StatType.NextAttackStatusAppliedDMGBonus, 5, StatType.NextAttackStatusAppliedDMGBonus);

        _service.GetStatusAppliedNextAttackDamageBonus(Attacker).Should().Be(5);
        _service.ConsumeStatusAppliedNextAttackDamageBonus(Attacker).Should().Be(5);
        _service.GetStatusAppliedNextAttackDamageBonus(Attacker).Should().Be(0);
    }

    [Test]
    public void TheGuardedHitEnmityGoesToAValidDefenderOnlyWhenPositive()
    {
        _invalid.Add(Gone);
        _service.ApplyNextAttackGuardedHitEnmityBonus(Gone, Defender, 10);
        _service.ApplyNextAttackGuardedHitEnmityBonus(Attacker, Gone, 10);
        _service.ApplyNextAttackGuardedHitEnmityBonus(Attacker, Defender, 0);
        _enmityCalls.Calls.Should().BeEmpty();

        _service.ApplyNextAttackGuardedHitEnmityBonus(Attacker, Defender, 10);
        _enmityCalls.Calls.Single().Arguments.Should().Equal(Attacker, Defender, 10);
    }

    [Test]
    public void TheStatValueGrantsReadTheirPerkAndSkillAndHandThemToTheAttackTimingPart()
    {
        _service.GrantNextAbilityDamageBonus(Attacker, (int)PerkType.DoubleShot, 5, 12);
        _service.GrantNextAbilityDamageBonus(Attacker, -3, 5, 12);
        _service.GrantNextSkillAbilityBonuses(Attacker, (int)SkillType.Rifle, 4, 9, 6);
        _service.GrantNextSkillAbilityBonuses(Attacker, 0, 4, 9, 6);

        _timingCalls.Calls.Select(call => $"{call.Member} {string.Join(",", call.Arguments)}").Should().Equal(
            $"GrantNextAbilityDamageBonus {Attacker},{PerkType.DoubleShot},5,12",
            $"GrantNextAbilityDamageBonus {Attacker},{PerkType.Invalid},5,12",
            $"GrantNextSkillAbilityBonuses {Attacker},{SkillType.Rifle},4,9,6,0,{StatusEffectCategory.None}",
            $"GrantNextSkillAbilityBonuses {Attacker},{SkillType.Invalid},4,9,6,0,{StatusEffectCategory.None}");
    }

    [Test]
    public void TheNextAbilityDamageAndStaminaBonusesAreKeptPerPerk()
    {
        var group = $"{nameof(PerkType)}:{(int)PerkType.DoubleShot}";
        _modifiers.Replace(Attacker, StatType.NextAbilityDamageBonus, 6, 30, group);
        _modifiers.Replace(Attacker, StatType.NextAbilityStaminaCostAdjustment, -2, 30, group);

        _service.ConsumeNextAbilityDamageBonus(Attacker, PerkType.Invalid).Should().Be(0);
        _service.GetNextAbilityStaminaCostAdjustment(Attacker, PerkType.Invalid).Should().Be(0);
        _service.ConsumeNextAbilityStaminaCostAdjustment(Attacker, PerkType.Invalid).Should().Be(0);
        _service.GetNextAbilityStaminaCostAdjustment(Attacker, PerkType.DoubleShot).Should().Be(-2);
        _service.ConsumeNextAbilityStaminaCostAdjustment(Attacker, PerkType.DoubleShot).Should().Be(-2);
        _service.GetNextAbilityStaminaCostAdjustment(Attacker, PerkType.DoubleShot).Should().Be(0);
        _service.ConsumeNextAbilityDamageBonus(Attacker, PerkType.DoubleShot).Should().Be(6);
        _service.ConsumeNextAbilityDamageBonus(Attacker, PerkType.DoubleShot).Should().Be(0);
    }

    [Test]
    public void TheTargetedAbilityAdjustmentsReadTheirPerkStatsFromTheQueriesPart()
    {
        // The scripted queries part answers a targeted adjustment with (adjustment stat % 7) + 1, so each stat is told apart.
        int Targeted(StatType adjustment) => (int)adjustment % 7 + 1;

        _service.GetAbilityDamageFlatAdjustment(Attacker, PerkType.DoubleShot, SkillType.Rifle)
            .Should().Be(Targeted(StatType.AbilityDamageFlatAdjustment) + 100);
        _service.GetAbilityStaminaCostFlatAdjustment(Attacker, PerkType.DoubleShot).Should().Be(Targeted(StatType.AbilityStaminaCostFlatAdjustment));
        _service.GetAbilityRecastDelayFlatAdjustment(Attacker, PerkType.DoubleShot).Should().Be(Targeted(StatType.AbilityRecastDelayFlatAdjustment));
        _queriesCalls.Calls.Where(call => call.Member == nameof(ICombatQueriesService.GetTargetedAbilityAdjustment))
            .Select(call => string.Join(",", call.Arguments)).Should().Equal(
                $"{Attacker},{PerkType.DoubleShot},{StatType.AbilityDamageFlatAdjustmentPerkType},{StatType.AbilityDamageFlatAdjustmentSecondaryPerkType},{StatType.AbilityDamageFlatAdjustment}",
                $"{Attacker},{PerkType.DoubleShot},{StatType.AbilityStaminaCostFlatAdjustmentPerkType},{StatType.AbilityStaminaCostFlatAdjustmentSecondaryPerkType},{StatType.AbilityStaminaCostFlatAdjustment}",
                $"{Attacker},{PerkType.DoubleShot},{StatType.AbilityRecastDelayFlatAdjustmentPerkType},{StatType.AbilityRecastDelayFlatAdjustmentPerkType},{StatType.AbilityRecastDelayFlatAdjustment}");
    }

    [Test]
    public void TheStatusCategoryBonusesNeedTheSkillAndAnAppliedRequiredCategory()
    {
        _invalid.Add(Gone);
        _service.GetAbilityStatusCategoryDamageBonus(Gone, SkillType.Rifle, StatusEffectCategory.Control).Should().Be(0);
        _service.GetAbilityStatusCategoryDamageBonus(Attacker, SkillType.Invalid, StatusEffectCategory.Control).Should().Be(0);
        _service.GetAbilityStatusCategoryDamageBonus(Attacker, SkillType.Rifle, StatusEffectCategory.None).Should().Be(0);

        Stat(Attacker, StatType.AbilityStatusCategoryDamageBonus, 8);
        Stat(Attacker, StatType.AbilityStatusCategoryHitChancePercentAdjustment, 6);
        Stat(Attacker, StatType.AbilityStatusCategoryBonusSkillType, (int)SkillType.Pistol);
        _service.GetAbilityStatusCategoryDamageBonus(Attacker, SkillType.Rifle, StatusEffectCategory.Control).Should().Be(0, "pistol only");

        Stat(Attacker, StatType.AbilityStatusCategoryBonusSkillType, (int)SkillType.Rifle);
        _service.GetAbilityStatusCategoryDamageBonus(Attacker, SkillType.Rifle, StatusEffectCategory.Control).Should().Be(0, "no category configured");

        Stat(Attacker, StatType.AbilityStatusCategoryBonusRequiredCategory, (int)StatusEffectCategory.Control);
        _service.GetAbilityStatusCategoryDamageBonus(Attacker, SkillType.Rifle, StatusEffectCategory.Debuff).Should().Be(0);
        _service.GetAbilityStatusCategoryDamageBonus(Attacker, SkillType.Rifle, StatusEffectCategory.Control).Should().Be(8);
        _service.GetAbilityStatusCategoryHitChancePercentAdjustment(Attacker, SkillType.Rifle, StatusEffectCategory.Control).Should().Be(6);
    }

    [Test]
    public void TheAbilityStaminaCostAddsTheHostileSkillAndHighResourceAdjustments()
    {
        _service.GetAbilityStaminaCostFlatAdjustment(Attacker, (AbilityDetail)null!).Should().Be(0);
        var perk = (int)StatType.AbilityStaminaCostFlatAdjustment % 7 + 1;
        var plain = new AbilityDetail { SkillType = SkillType.Rifle, EffectiveLevelPerkType = PerkType.DoubleShot };
        _service.GetAbilityStaminaCostFlatAdjustment(Attacker, plain).Should().Be(perk);

        var hostile = new AbilityDetail { SkillType = SkillType.Rifle, EffectiveLevelPerkType = PerkType.DoubleShot, IsHostileAbility = true };
        Stat(Attacker, StatType.HostileAbilityStaminaCostFlatAdjustment, -1);
        _service.GetAbilityStaminaCostFlatAdjustment(Attacker, hostile).Should().Be(perk - 1 + 300);

        Stat(Attacker, StatType.SkillAbilityStaminaCostFlatAdjustmentSkillType, (int)SkillType.Rifle);
        Stat(Attacker, StatType.SkillAbilityStaminaCostFlatAdjustment, -10);
        Stat(Attacker, StatType.HighResourceAbilityStaminaCostSkillType, (int)SkillType.Rifle);
        Stat(Attacker, StatType.HighResourceAbilityStaminaCostAdjustment, -20);
        _service.GetAbilityStaminaCostFlatAdjustment(Attacker, plain).Should().Be(perk - 10, "no threshold is configured");
        Stat(Attacker, StatType.HighResourceAbilityStaminaCostThresholdPercent, 75);
        _service.GetAbilityStaminaCostFlatAdjustment(Attacker, plain).Should().Be(perk - 10, "FP is low");
        _highFP = true;
        _service.GetAbilityStaminaCostFlatAdjustment(Attacker, plain).Should().Be(perk - 30);
        _resourcesCalls.Calls.Last().Arguments.Should().Equal(Attacker, 75);

        var pistol = new AbilityDetail { SkillType = SkillType.Pistol, EffectiveLevelPerkType = PerkType.DoubleShot };
        _service.GetAbilityStaminaCostFlatAdjustment(Attacker, pistol).Should().Be(perk, "both skill adjustments are the rifle's");
    }

    [Test]
    public void TheIdleStatusDurationBonusNeedsTheSkillIdleTimeAndTheRequiredCategory()
    {
        var targeted = (int)StatType.AbilityStatusDurationPercentAdjustment % 7 + 1;
        int Duration(SkillType skill, Func<IStatusEffect>? factory = null) => _service.GetAbilityStatusDurationPercentAdjustment(
            Attacker, PerkType.DoubleShot, skill, typeof(StunnedStatusEffect), Array.Empty<Type>(), factory!);

        Duration(SkillType.Rifle).Should().Be(targeted, "no idle bonus is configured");
        Stat(Attacker, StatType.IdleStatusDurationPercentAdjustmentSkillType, (int)SkillType.Rifle);
        Stat(Attacker, StatType.IdleStatusDurationPercentAdjustment, 50);
        Duration(SkillType.Rifle).Should().Be(targeted, "no idle time is configured");

        Stat(Attacker, StatType.IdleStatusDurationRequiredIdleSeconds, 6);
        _recentAttack = true;
        Duration(SkillType.Rifle).Should().Be(targeted, "the attacker fought recently");
        _activityCalls.Calls.Last().Arguments.Should().Equal(Attacker, 6f);

        _recentAttack = false;
        Duration(SkillType.Rifle).Should().Be(targeted + 50, "no category is required");
        Duration(SkillType.Pistol).Should().Be(targeted);

        Stat(Attacker, StatType.IdleStatusDurationRequiredCategory, (int)StatusEffectCategory.Control);
        Duration(SkillType.Rifle).Should().Be(targeted, "the ability applies no control status");
        Duration(SkillType.Rifle, () => null!).Should().Be(targeted);
        Duration(SkillType.Rifle, () => new BurnStatusEffect()).Should().Be(targeted, "a burn is not a control status");
        Duration(SkillType.Rifle, () => new StunnedStatusEffect()).Should().Be(targeted + 50);
        _abilityAppliedCategory = true;
        Duration(SkillType.Rifle).Should().Be(targeted + 50, "the ability itself applies a control status");
    }

    [Test]
    public void TheAbilityDefenseIgnoreAddsTheRangedExposedAndDisruptedTargetAdjustments()
    {
        var targeted = (int)StatType.AbilityDefenseIgnorePercentAdjustment % 7 + 1;
        int Ignore(uint defender) => _service.GetAbilityDefenseIgnorePercentAdjustment(Attacker, PerkType.DoubleShot, SkillType.Rifle, defender);
        Stat(Attacker, StatType.AbilityDefenseIgnoreExposedOrSunderedPercentAdjustment, 10);
        Stat(Attacker, StatType.AbilityDefenseIgnoreForceDisruptionOrFoggyMindPercentAdjustment, 20);

        Ignore(Defender).Should().Be(targeted + 200, "the target has neither status");
        _statuses.Add((Defender, typeof(SunderStatusEffect)));
        Ignore(Defender).Should().Be(targeted + 200, "the exposed bonus needs its skill selector");
        Stat(Attacker, StatType.AbilityDefenseIgnoreExposedOrSunderedSkillType, (int)SkillType.Rifle);
        Ignore(Defender).Should().Be(targeted + 210);
        _statuses.Add((Defender, typeof(FoggyMindStatusEffect)));
        Ignore(Defender).Should().Be(targeted + 230);
        _service.GetAbilityDefenseIgnorePercentAdjustment(Attacker, PerkType.DoubleShot, SkillType.Pistol, Defender)
            .Should().Be(targeted + 220, "the exposed bonus is the rifle's");
        _invalid.Add(Gone);
        Ignore(Gone).Should().Be(targeted + 200);

        _statusCalls.Calls.Where(call => call.Member == nameof(IStatusEffectService.HasStatusEffect))
            .Select(call => string.Join(",", ((Type[])call.Arguments[1]!).Select(type => type.Name))).Distinct().Should().Equal(
                "ForceDisruptionStatusEffect,FoggyMindStatusEffect",
                "ExposedStatusEffect,ExposeWeakPointStatusEffect,SunderStatusEffect");
    }

    [Test]
    public void TheHostileRecastDelayAdjustmentScalesTheDelayAndNeverGoesBelowZero()
    {
        var hostile = new AbilityDetail { IsHostileAbility = true };
        _service.ApplyAbilityRecastDelayModifiers(Attacker, hostile, 10f).Should().Be(10f, "no adjustment");
        Stat(Attacker, StatType.HostileAbilityRecastDelayPercentAdjustment, -25);
        _service.ApplyAbilityRecastDelayModifiers(Attacker, hostile, 10f).Should().Be(7.5f);
        _service.ApplyAbilityRecastDelayModifiers(Attacker, new AbilityDetail(), 10f).Should().Be(10f, "not hostile");
        _service.ApplyAbilityRecastDelayModifiers(Attacker, null!, 10f).Should().Be(10f);
        _service.ApplyAbilityRecastDelayModifiers(Attacker, hostile, 0f).Should().Be(0f);
        _invalid.Add(Gone);
        _service.ApplyAbilityRecastDelayModifiers(Gone, hostile, 10f).Should().Be(10f);
        Stat(Attacker, StatType.HostileAbilityRecastDelayPercentAdjustment, -150);
        _service.ApplyAbilityRecastDelayModifiers(Attacker, hostile, 10f).Should().Be(0f);
    }

    [Test]
    public void TheAbilityDamageBonusIsFifteenHundredthsOfThePlayersSkillOrTheNpcsLevel()
    {
        _service.GetAbilityDamageBonus(Attacker, SkillType.Rifle).Should().Be(6, "0.15 of level 40");

        _players.Add(Attacker);
        var player = new Player { Id = PlayerId };
        player.Skills[SkillType.Rifle] = new PlayerSkill { Rank = 47 };
        _db.Set(player);
        _service.GetAbilityDamageBonus(Attacker, SkillType.Rifle).Should().Be(7, "0.15 of rank 47");
        _db.Reads.Should().Contain(PlayerId);
    }
}
