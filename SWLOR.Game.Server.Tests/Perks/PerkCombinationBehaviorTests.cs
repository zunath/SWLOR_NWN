using System.Collections;
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Tests.Perks;

public class PerkCombinationBehaviorTests
{
    private const uint Creature = 61250;
    private const BindingFlags PrivateStatic = BindingFlags.Static | BindingFlags.NonPublic;

    [TearDown]
    public void Cleanup() => ((IDictionary)typeof(TemporaryStatModifier)
        .GetField("_modifiers", PrivateStatic)!.GetValue(null)!).Remove(Creature);

    [Test]
    public void IndependentDamageProcs_PreserveTheirIndividualExpectedDamage()
    {
        var sources = new[]
        {
            Source((StatType.AutoAttackDamageBonus, 10), (StatType.AutoAttackDamageBonusChance, 15)),
            Source((StatType.AutoAttackDamageBonus, 8), (StatType.AutoAttackDamageBonusChance, 15))
        };
        var total = 0;
        for (var first = 1; first <= 100; first++)
        for (var second = 1; second <= 100; second++)
        {
            var rolls = new Queue<int>(new[] { first, second });
            total += Combat.CalculateAutoAttackProcDamage(sources, () => rolls.Dequeue());
        }
        (total / 10000d).Should().Be(2.7, "independent 15% procs must not become one 30% proc of their combined damage");
    }

    [Test]
    public void PayloadPouch_IsAThrowingSplashAndDoesNotIncreaseSavageReflexesChance()
    {
        var pouch = PerkSource<ThrowingPerkDefinition>("PayloadPouch", PerkType.PayloadPouch);
        var reflexes = PerkSource<VibrobladePerkDefinition>("SavageReflexes", PerkType.SavageReflexes);
        pouch[StatType.AutoAttackSplashChance].Should().Be(15);
        pouch[StatType.AutoAttackSplashDamage].Should().Be(8);
        pouch[StatType.AutoAttackSplashRadiusMeters].Should().Be(3);
        pouch[StatType.AutoAttackSplashMaximumTargets].Should().Be(5);
        foreach (var skill in Enum.GetValues<SkillType>())
            Combat.CanTriggerAutoAttackSplash(pouch, skill).Should().Be(skill == SkillType.Throwing);
        Combat.CalculateAutoAttackProcDamage(new[] { pouch, reflexes }, () => 16).Should().Be(0);
        Combat.CalculateAutoAttackProcDamage(new[] { pouch, reflexes }, () => 15).Should().Be(10);
    }

    [Test]
    public void SplashChance_IsBeneficialAndUsesMaximumAggregation()
    {
        Stat.IsBeneficialStatAdjustment(StatType.AutoAttackSplashChance, 15).Should().BeTrue();
        Stat.IsBeneficialStatAdjustment(StatType.AutoAttackSplashChance, 0).Should().BeFalse();
        Stat.IsBeneficialStatAdjustment(StatType.AutoAttackSplashChance, -15).Should().BeFalse();
        Stat.GetStatTypeAggregation(StatType.AutoAttackSplashChance).Should().Be(StatTypeAggregation.Maximum);
    }

    [Test]
    public void SplashTargetBudget_ExcludesThePrimaryBeforeLimitingSecondaryTargets()
    {
        foreach (var candidates in new[]
                 {
                     new uint[] { 99, 1, 2, 3, 4, 5 },
                     new uint[] { 1, 2, 99, 2, 3, 4, 5 },
                     new uint[] { 1, 2, 3, 4, 5 }
                 })
        {
            Combat.SelectAutoAttackSplashSecondaryTargets(candidates, 99, 5)
                .Should().Equal(new uint[] { 1, 2, 3, 4 },
                    "the primary receives its own hit and cannot consume or duplicate any of the four splash slots");
        }
        Combat.SelectAutoAttackSplashSecondaryTargets(new uint[] { 99, 1, 2 }, 99, 1).Should().BeEmpty();
        Combat.SelectAutoAttackSplashSecondaryTargets(new uint[] { 99, 1, 2 }, 99, 0).Should().BeEmpty();
    }

    [Test]
    public void RicochetToss_GrantsItsFullPayloadAndOnlyRollsForThrownHitsAgainstBleedingTargets()
    {
        var source = PerkSource<ThrowingPerkDefinition>("RicochetToss", PerkType.RicochetToss);
        source[StatType.BleedingTargetAbilitySplashDamage].Should().Be(12);
        source[StatType.BleedingTargetAbilitySplashRadiusMeters].Should().Be(5);
        source[StatType.BleedingTargetAbilitySplashMaximumTargets].Should().Be(1);
        foreach (var skill in Enum.GetValues<SkillType>())
        foreach (var bleeding in new[] { false, true })
        {
            var procs = Enumerable.Range(1, 100).Count(roll =>
                Combat.CanTriggerBleedingTargetAbilitySplash(source, skill, bleeding, roll));
            procs.Should().Be(skill == SkillType.Throwing && bleeding ? 25 : 0);
        }
        var flurry = PerkSource<ThrowingPerkDefinition>("FlurryBleed", PerkType.FlurryBleed);
        Combat.CanTriggerBleedingTargetAbilitySplash(flurry, SkillType.Throwing, true, 1)
            .Should().BeFalse("a shared skill selector must not confer another perk's proc");
    }

    [TestCase(SkillType.HeavyVibroblade, -100, 1)]
    [TestCase(SkillType.HeavyVibroblade, -100, 500)]
    [TestCase(SkillType.Force, -100, 500)]
    [TestCase(SkillType.HeavyVibroblade, -150, 500)]
    public void ExplicitZeroDamageAbilities_BypassWeaponDamageAndAllLaterBonuses(
        SkillType skill, int adjustment, int rolledDamage)
    {
        var damage = Combat.ApplyDamageDealtModifiers(1, 2, rolledDamage, skill, CombatDamageType.Physical,
            isAbilityDamage: true, canApplyRandomFlatBonuses: true, isLandedAttack: true, ability: null,
            targetStatusDamagePercentAdjustment: out var targetAdjustment,
            abilityDamagePercentAdjustment: adjustment, lowHPAbilityDamagePercentAdjustment: 500);
        damage.Should().Be(0);
        targetAdjustment.Should().Be(0);
    }

    [Test]
    public void SingleAdditionalDamageTarget_ExcludesThePrimaryBeforeSpendingTheSlot()
    {
        Combat.SelectSecondaryDamageTargets(new uint[] { 99, 99, 1, 1, 2 }, 99, 1)
            .Should().Equal(new uint[] { 1 });
        Combat.SelectSecondaryDamageTargets(new uint[] { 99 }, 99, 1).Should().BeEmpty();
        Combat.SelectSecondaryDamageTargets(new uint[] { 99, 1 }, 99, 0).Should().BeEmpty();
    }

    [Test]
    public void AvoidedAttackDiscounts_KeepTheirAuthoredScopesAndConsumeIndependently()
    {
        StoreDiscount("staff", PerkSource<StaffPerkDefinition>("FlowingDefense", PerkType.FlowingDefense));
        StoreDiscount("ranged", PerkSource<PistolPerkDefinition>("EvasiveReload", PerkType.EvasiveReload));
        StoreDiscount("global", PerkSource<SpearPerkDefinition>("OpportunistsFlow", PerkType.OpportunistsFlow));

        Combat.GetNextSkillAbilityStaminaCostAdjustment(Creature, SkillType.Staff, true).Should().Be(-7);
        Combat.GetNextSkillAbilityStaminaCostAdjustment(Creature, SkillType.Rifle, true).Should().Be(-7);
        Combat.GetNextSkillAbilityStaminaCostAdjustment(Creature, SkillType.Throwing, true).Should().Be(-7);
        Combat.GetNextSkillAbilityStaminaCostAdjustment(Creature, SkillType.Force, true).Should().Be(-4);
        Combat.GetNextSkillAbilityStaminaCostAdjustment(Creature, SkillType.Staff, false).Should().Be(0);
        Combat.GetNextSkillAbilityStaminaCostAdjustment(Creature, SkillType.Invalid, true).Should().Be(0);

        ConsumeMatchingDiscounts(SkillType.Rifle);
        Combat.GetNextSkillAbilityStaminaCostAdjustment(Creature, SkillType.Staff, true).Should().Be(-3);
        Combat.GetNextSkillAbilityStaminaCostAdjustment(Creature, SkillType.Pistol, true).Should().Be(0);
        Combat.GetNextSkillAbilityStaminaCostAdjustment(Creature, SkillType.Force, true).Should().Be(0);
        ConsumeMatchingDiscounts(SkillType.Staff);
        Combat.GetNextSkillAbilityStaminaCostAdjustment(Creature, SkillType.Staff, true).Should().Be(0);
    }

    [TestCase(100, 500, 15, 215)]
    [TestCase(100, 180, 15, 195)]
    [TestCase(1, 3, 15, 17)]
    [TestCase(0, 100, 15, 0)]
    public void DamageBonusCap_PreservesFlatDamage(int baseline, int percentageAdjusted, int flat, int expected) =>
        Combat.CapOutgoingDamageBonus(baseline, percentageAdjusted, flat).Should().Be(expected);

    [TestCase(500, -50, -80, 150)]
    [TestCase(225, -50, -80, 68)]
    [TestCase(1000, 0, -95, 150)]
    [TestCase(500, -50, -20, 400)]
    [TestCase(1000, 0, 20, 1200)]
    [TestCase(0, -50, -80, 0)]
    [TestCase(1, -95, -95, 1)]
    [TestCase(2, -95, 0, 2)]
    public void CombinedReductionCap_DoesNotUndoGuardOrDamageSplitting(
        int damageAfterPriorStages, int targetReduction, int genericReduction, int expected) =>
        Combat.ApplyCombinedDamageTakenAdjustment(damageAfterPriorStages, targetReduction, genericReduction)
            .Should().Be(expected);

    [Test]
    public void AllMitigationPairs_RespectTheBudgetWithoutWeakeningIndependentGuard()
    {
        for (var target = 0; target >= -95; target--)
        for (var generic = 0; generic >= -95; generic--)
        {
            var targetDamage = 10000 * (100 + Math.Max(target, -85)) / 100;
            var ordinary = Combat.ApplyCombinedDamageTakenAdjustment(targetDamage, target, generic);
            var guarded = Combat.ApplyCombinedDamageTakenAdjustment(targetDamage * 45 / 100, target, generic);
            ordinary.Should().BeGreaterThanOrEqualTo(1500);
            guarded.Should().BeLessThanOrEqualTo((int)Math.Ceiling(ordinary * .45),
                "Guard is a separate 55% reduction, including when other reductions reach their cap");
        }
    }

    [TestCase(8, -50, -80, 4, 2)]
    [TestCase(100, -50, -80, 50, 15)]
    [TestCase(100, -95, -80, 15, 15)]
    [TestCase(100, 20, -50, 120, 60)]
    public void TriggeredDamage_HonorsTypedTargetModifiersAndTheSharedReductionBudget(
        int damage, int typedAdjustment, int genericAdjustment, int typedDamage, int expected)
    {
        var result = Combat.ApplyTriggeredDamageTargetAdjustment(damage, typedAdjustment, null);
        result.Damage.Should().Be(typedDamage);
        Combat.ApplyCombinedDamageTakenAdjustment(result.Damage, result.Adjustment, genericAdjustment)
            .Should().Be(expected);
    }

    [TestCase(-50)]
    [TestCase(0)]
    public void ConvertedDamage_PreservesAnAlreadyAppliedTargetAdjustmentIncludingZero(int priorAdjustment)
    {
        var result = Combat.ApplyTriggeredDamageTargetAdjustment(50, -80, priorAdjustment);
        result.Damage.Should().Be(50, "the originating hit already applied its target-status modifier");
        result.Adjustment.Should().Be(priorAdjustment);
    }

    [Test]
    public void SelfAppliedSubdualControl_PreservesItsSixtySecondDuration()
    {
        var categories = new KnockdownStatusEffect().Categories;
        StatusEffect.ClampHardCrowdControlDurationTicks(categories, 60, 1f, isSelfApplied: true)
            .Should().Be(60, "subdual applies its knockdown from the defeated player to themselves");
        StatusEffect.ClampHardCrowdControlDurationTicks(categories, 60, 1f, isSelfApplied: false)
            .Should().Be(30, "externally applied combat control must retain its budget");
    }

    [TestCase(10, 100, 100, 0)]
    [TestCase(0, 80, 100, 0)]
    [TestCase(20, 95, 100, 5)]
    [TestCase(20, 0, 100, 0)]
    [TestCase(20, 80, 100, 20)]
    public void HealingRiders_RequireAnActualPositiveHeal(int amount, int currentHP, int maxHP, int expected) =>
        Stat.CalculateEffectiveHealingAmount(amount, currentHP, maxHP).Should().Be(expected);

    [TestCase(44, 1f, 30)]
    [TestCase(90, 1f, 30)]
    [TestCase(9, 1f, 9)]
    [TestCase(15, 3f, 10)]
    [TestCase(15, 4f, 7)]
    [TestCase(-1, 1f, -1)]
    public void HardControl_FinalDurationIsBoundedAfterAllModifiers(int ticks, float frequency, int expected)
    {
        StatusEffect.ClampHardCrowdControlDurationTicks(
            StatusEffectCategory.Debuff | StatusEffectCategory.Control | StatusEffectCategory.HardCrowdControl,
            ticks, frequency).Should().Be(expected);
        StatusEffect.ClampHardCrowdControlDurationTicks(StatusEffectCategory.Debuff, ticks, frequency)
            .Should().Be(ticks, "soft debuff duration bonuses retain their value");
    }

    [Test]
    public void DisruptionExpert_ExtendsOnlyAbilityDisruptionWhileIronGripExtendsDebuffs()
    {
        var expert = PerkSource<SpearPerkDefinition>("DisruptionExpert", PerkType.DisruptionExpert);
        var ironGrip = PerkSource<KatarPerkDefinition>("IronGrip", PerkType.IronGrip);
        foreach (var effect in new StatusEffectBase[] { new FoggyMindStatusEffect(), new ForceDisruptionStatusEffect(),
                     new EclipseOfResolve1StatusEffect(), new FracturedFocusStatusEffect() })
            StatusEffect.GetOutgoingDurationPercentAdjustment(effect.Categories, stat => expert[stat] + ironGrip[stat])
                .Should().Be(45, effect.Name);
        foreach (var effect in new StatusEffectBase[] { new DazedStatusEffect(), new StunnedStatusEffect(),
                     new ImmobilizedStatusEffect(), new BleedStatusEffect(), new PoisonStatusEffect() })
            StatusEffect.GetOutgoingDurationPercentAdjustment(effect.Categories, stat => expert[stat] + ironGrip[stat])
                .Should().Be(20, effect.Name + " is a debuff but is not ability disruption");
    }

    [Test]
    public void HardControl_RefreshAndExtensionCannotPostponeTheImmunityWindow()
    {
        var started = new DateTime(2026, 9, 12, 12, 0, 0, DateTimeKind.Utc);
        var hard = new DurationProbe(StatusEffectCategory.HardCrowdControl);
        hard.ApplyEffect(0, 0, 30);
        typeof(StatusEffectBase).GetField("_lastRun", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(hard, started);
        var nearExpiry = started.AddSeconds(28.5);
        hard.ExtendDurationTicks(30);
        hard.SetDurationTicks(30);
        hard.GetRemainingDurationSeconds(nearExpiry).Should().Be(1.5f);
        StatusEffect.ClampConvertedControlDurationTicks(6, 1f, hard.GetRemainingDurationSeconds(nearExpiry))
            .Should().Be(1, "late Daze-to-Knockdown conversion must not start a fresh six-second timer");
        StatusEffect.ClampConvertedControlDurationTicks(30, 1f, hard.GetRemainingDurationSeconds(started.AddSeconds(30)))
            .Should().Be(0, "expired control cannot be revived by conversion");

        var soft = new DurationProbe(StatusEffectCategory.Debuff);
        soft.ApplyEffect(0, 0, 30);
        soft.ExtendDurationTicks(6);
        soft.DurationTicks.Should().Be(36);
        soft.SetDurationTicks(45);
        soft.DurationTicks.Should().Be(45, "Bleed, Venom and Infection refresh/extension retain their intended behavior");
    }

    [TestCase(6, 1f, 20f, 6)]
    [TestCase(6, 1f, 3.5f, 3)]
    [TestCase(6, 1f, .5f, 0)]
    [TestCase(10, 3f, 5.9f, 1)]
    public void ControlConversion_PreservesTheEarlierExpiration(int requested, float frequency, float remaining, int expected) =>
        StatusEffect.ClampConvertedControlDurationTicks(requested, frequency, remaining).Should().Be(expected);

    private sealed class DurationProbe(StatusEffectCategory categories) : StatusEffectBase
    {
        public DurationProbe() : this(StatusEffectCategory.None) { }

        public override string Name => "Duration probe";
        public override SWLOR.NWN.API.NWScript.Enum.EffectIconType Icon => default;
        public override StatusEffectCategory Categories => categories;
    }

    private static StatAdjustmentSource PerkSource<T>(string method, PerkType type) where T : new()
    {
        var definition = new T();
        typeof(T).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(definition, null);
        var builder = typeof(T).GetField("_builder", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(definition)!;
        var perks = (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
            .GetField("_perks", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(builder)!;
        return new StatAdjustmentSource(type.ToString(), perks[type].PerkLevels[1].StatBonuses
            .ToDictionary(bonus => bonus.Stat, bonus => bonus.Calculate(0)));
    }

    private static StatAdjustmentSource Source(params (StatType Stat, int Amount)[] stats) =>
        new("test", stats.ToDictionary(stat => stat.Stat, stat => stat.Amount));

    private static void StoreDiscount(string group, StatAdjustmentSource perk)
    {
        AddTemporary(group, StatType.NextSkillAbilityStaminaCostAdjustment,
            perk[StatType.AvoidedAttackNextSkillAbilityStaminaCostAdjustment]);
        AddTemporary(group, StatType.NextSkillAbilityStaminaCostAdjustmentSkillType,
            perk[StatType.AvoidedAttackNextSkillAbilitySkillType]);
        AddTemporary(group, StatType.NextSkillAbilityStaminaCostAdjustmentRangedOnly,
            perk[StatType.AvoidedAttackNextSkillAbilityRangedOnly]);
        AddTemporary(group, StatType.NextSkillAbilityStaminaCostAdjustmentHostileOnly, 1);
    }

    private static void AddTemporary(string group, StatType stat, int amount) =>
        typeof(TemporaryStatModifier).GetMethod("AddInternal", PrivateStatic)!
            .Invoke(null, new object[] { Creature, stat, amount, 30f, group, false });

    private static void ConsumeMatchingDiscounts(SkillType skill)
    {
        foreach (var source in TemporaryStatModifier.GetStatSources(Creature, StatType.NextSkillAbilityStaminaCostAdjustment))
        {
            if (Combat.MatchesStaminaDiscount(source, skill, true))
                typeof(TemporaryStatModifier).GetMethod("ConsumeSourceInternal", PrivateStatic)!
                    .Invoke(null, new object[] { Creature, StatType.NextSkillAbilityStaminaCostAdjustment, source, false });
        }
    }
}
