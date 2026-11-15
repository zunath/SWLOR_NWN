using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Abilities;

/// <summary>
/// The tracker of one impact: when it starts, which bonuses it consumes and in what order, what it records, how it ends,
/// and how a recurring impact shares it.
/// </summary>
[TestFixture]
public class AbilityServiceImpactTrackingTests : AbilityServiceTestBase
{
    private static WeaponAbilityTraitActivation Traits(bool committed = true, int accuracy = 6, int cadence = 1) =>
        new(new PairedRangedAbilityTraitsSnapshot(Guid.Empty, true, true, cadence, accuracy), 5) { IsCommitted = committed };

    [Test]
    public void An_impact_needs_a_valid_activator_and_an_ability()
    {
        Bench.Invalid.Add(Other);

        Lifecycle.BeginAbilityImpact(Other, Hostile());
        Lifecycle.BeginAbilityImpact(Caster, null!);

        ImpactState.GetActiveAbilityImpactSummary(Other).Should().BeNull();
        ImpactState.GetActiveAbilityImpactSummary(Caster).Should().BeNull();
    }

    [Test]
    public void A_weapon_impact_consumes_its_bonuses_in_order_when_it_starts()
    {
        var ability = Hostile(skill: SkillType.Vibroblade);
        ability.ActivationType = AbilityActivationType.Weapon;
        Bench.NextAbilityDamageBonus = 1;
        Bench.NextSkillBonuses = (2, 3, 4);
        Bench.PersistentCriticalRate = 5;
        Bench.QueuedBonuses = (6, 7, 8);
        Bench.GuardedHitBonuses = (9, 10, 11);
        Bench.StatusAppliedBonus = 12;

        Lifecycle.BeginAbilityImpact(Caster, ability);

        LogOf("Consume", "GetPersistent", "GetStatusApplied").Should().Equal(
            "ConsumeNextAbilityDamageBonus",
            "ConsumeNextSkillAbilityBonuses:None",
            "GetPersistentCriticalRate",
            "ConsumeQueuedWeaponAbilityBonuses",
            "ConsumeGuardedHitBonuses",
            "GetStatusAppliedBonus");
        HostileImpact.ApplyCapturedAbilityDamageBonus(Caster, 100).Should().Be(100 + 1 + 2 + 9 + 12 + 6);
    }

    [Test]
    public void A_control_only_impact_keeps_every_bonus_armed()
    {
        Lifecycle.BeginAbilityImpact(Caster, Hostile());

        LogOf("Consume").Should().BeEmpty();
        HostileImpact.ApplyCapturedAbilityDamageBonus(Caster, 0).Should().Be(0, "a control payload stays zero");
    }

    [Test]
    public void A_deferred_damage_impact_consumes_its_bonuses_when_it_starts_and_a_support_impact_skips_the_hostile_ones()
    {
        var ability = new AbilityDetail { Name = "Mend", SkillType = SkillType.Force, DealsDeferredDamage = true };

        Lifecycle.BeginAbilityImpact(Caster, ability);

        LogOf("Consume", "GetPersistent", "GetStatusApplied").Should().Equal(
            "ConsumeNextAbilityDamageBonus", "ConsumeNextSkillAbilityBonuses:None", "GetPersistentCriticalRate");
    }

    [Test]
    public void The_status_categories_of_the_impact_reach_the_next_skill_bonuses()
    {
        var ability = Hostile();
        ability.DealsDeferredDamage = true;
        ability.ResolveImpactStatusCategories = _ => SWLOR.Game.Server.Domain.Character.StatusEffectCategory.Debuff;

        Lifecycle.BeginAbilityImpact(Caster, ability);

        Bench.Log.Should().Contain("ConsumeNextSkillAbilityBonuses:Debuff");
    }

    [Test]
    public void A_captured_bonus_of_a_control_impact_is_consumed_once_by_the_first_damaging_payload()
    {
        Bench.NextAbilityDamageBonus = 4;
        Lifecycle.BeginAbilityImpact(Caster, Hostile());

        Lifecycle.CaptureRepeatedAbilityImpact(Caster, () => { });
        LogOf("ConsumeNextAbilityDamageBonus").Should().BeEmpty();

        Lifecycle.CaptureRepeatedAbilityImpact(Caster, () => { }, baseDamage: 10);
        Lifecycle.CaptureRepeatedAbilityImpact(Caster, () => { }, baseDamage: 10);
        LogOf("ConsumeNextAbilityDamageBonus").Should().ContainSingle();
    }

    [Test]
    public void A_triggering_weapon_fixes_the_damage_and_type_of_the_impact()
    {
        const uint weapon = 900;
        var ability = Hostile(skill: SkillType.Vibroblade);
        Bench.WeaponDamage = 30;
        Bench.WeaponDamageType = CombatDamageType.Electrical;

        Lifecycle.BeginAbilityImpact(Caster, ability, triggeringWeapon: weapon);

        Bench.Log.Should().ContainInOrder($"GetAbilityDMG:{Caster}:{weapon}:{SkillType.Vibroblade}", $"GetWeaponDamageType:{weapon}");
        var summary = ImpactState.GetActiveAbilityImpactSummary(Caster);
        summary.TriggeringWeaponDamage.Should().Be(30);
        summary.TriggeringWeaponDamageType.Should().Be(CombatDamageType.Electrical);
    }

    [Test]
    public void Explicit_weapon_values_win_over_the_triggering_weapon()
    {
        Bench.WeaponDamage = 30;

        Lifecycle.BeginAbilityImpact(Caster, Hostile(), triggeringWeapon: 900, triggeringWeaponDamage: 12, triggeringWeaponDamageType: CombatDamageType.Fire);

        LogOf("GetAbilityDMG", "GetWeaponDamageType").Should().BeEmpty();
        var summary = ImpactState.GetActiveAbilityImpactSummary(Caster);
        summary.TriggeringWeaponDamage.Should().Be(12);
        summary.TriggeringWeaponDamageType.Should().Be(CombatDamageType.Fire);
    }

    [Test]
    public void Without_a_weapon_the_impact_captures_the_technique_values_first()
    {
        Bench.CapturedWeaponDamage = 44;
        Bench.CapturedWeaponDamageType = CombatDamageType.Ice;
        Bench.WeaponDamage = 30;

        Lifecycle.BeginAbilityImpact(Caster, Hostile());

        var summary = ImpactState.GetActiveAbilityImpactSummary(Caster);
        summary.TriggeringWeaponDamage.Should().Be(44);
        summary.TriggeringWeaponDamageType.Should().Be(CombatDamageType.Ice);
    }

    [Test]
    public void A_sweep_activation_makes_the_single_target_technique_an_area_for_this_cast()
    {
        Bench.SweepActivation = true;
        var ability = Hostile(skill: SkillType.Vibroblade);
        ability.MeleeTechnique = new MeleeTechniqueDetail();

        Lifecycle.BeginAbilityImpact(Caster, ability);

        ImpactState.IsAreaImpact(Caster, ability).Should().BeTrue();
        ImpactState.IsSingleTargetImpact(Caster, ability).Should().BeFalse();
        ability.IsSingleTargetAbility.Should().BeTrue("the definition is not touched");
    }

    [Test]
    public void An_ability_without_a_technique_keeps_its_authored_shape()
    {
        var ability = Hostile();
        Lifecycle.BeginAbilityImpact(Caster, ability);

        ImpactState.IsAreaImpact(Caster, ability).Should().BeFalse();
        ImpactState.IsSingleTargetImpact(Caster, ability).Should().BeTrue();
        ImpactState.IsAreaImpact(null!, ability).Should().BeFalse();
        ImpactState.IsAreaImpact(Other, null!).Should().BeFalse();
        ImpactState.IsSingleTargetImpact(Other, null!).Should().BeFalse();
    }

    [Test]
    public void Weapon_traits_ride_the_sequence_and_their_accuracy_counts_once_committed()
    {
        var traits = Traits(committed: false);
        Lifecycle.BeginAbilityImpact(Caster, Hostile(), weaponTraits: traits);

        ImpactState.GetAbilityImpactSequence(Caster).WeaponTraits.Should().BeSameAs(traits);
        ImpactState.GetWeaponTraitAccuracy(Caster).Should().Be(0);

        traits.IsCommitted = true;
        ImpactState.GetWeaponTraitAccuracy(Caster).Should().Be(6);
        ImpactState.GetWeaponTraitAccuracy(Other).Should().Be(0);
    }

    [Test]
    public void A_landed_hit_completes_the_traits_every_time_and_earns_cadence_once()
    {
        var traits = Traits();
        Lifecycle.BeginAbilityImpact(Caster, Hostile(skill: SkillType.Pistol), weaponTraits: traits);

        Service.RecordSuccessfulWeaponAbilityHit(Caster, Target, SkillType.Force);
        LogOf("Trait").Should().BeEmpty("only a weapon skill hit counts");

        Service.RecordSuccessfulWeaponAbilityHit(Caster, Target, SkillType.Pistol);
        Service.RecordSuccessfulWeaponAbilityHit(Caster, Other, SkillType.Pistol);

        LogOf("Trait").Should().Equal($"TraitCompleteHit:{Caster}", $"TraitRecordHit:{Caster}", $"TraitCompleteHit:{Caster}");
    }

    [Test]
    public void A_hit_of_uncommitted_traits_records_nothing()
    {
        Lifecycle.BeginAbilityImpact(Caster, Hostile(skill: SkillType.Pistol), weaponTraits: Traits(committed: false));

        Service.RecordSuccessfulWeaponAbilityHit(Caster, Target, SkillType.Pistol);

        LogOf("Trait").Should().BeEmpty();
    }

    [Test]
    public void The_remaining_healing_hp_of_a_target_subtracts_what_is_already_queued()
    {
        Bench.Hp[Target] = 50;
        Bench.Plot.Add(Other);
        Bench.Invalid.Add(600);

        ImpactState.GetRemainingDamageTargetHP(Caster, 600).Should().Be(0);
        ImpactState.GetRemainingDamageTargetHP(Caster, Other).Should().Be(0);
        ImpactState.GetRemainingDamageTargetHP(Caster, Target).Should().Be(50);

        Lifecycle.BeginAbilityImpact(Caster, Hostile());
        ImpactState.TryQueueTrackedDamageEffect(Caster, Target, 20, DamageType.Fire).Should().BeTrue();

        ImpactState.GetRemainingDamageTargetHP(Caster, Target).Should().Be(30);
    }

    [Test]
    public void Stat_sources_are_read_live_without_an_impact()
    {
        ImpactState.GetAbilityImpactStatSources(Caster, StatType.Attack).Should().BeEmpty();
        Bench.StatSourceReads.Should().Be(1);

        Lifecycle.BeginAbilityImpact(Caster, Hostile());
        ImpactState.GetAbilityImpactStatSources(Caster, StatType.Attack).Should().BeEmpty();
        ImpactState.GetAbilityImpactStatSources(Caster, StatType.Attack).Should().BeEmpty();
        Bench.StatSourceReads.Should().Be(3, "an empty answer is not cached");

        var sources = new[] { new StatAdjustmentSource("source", new Dictionary<StatType, int>()) };
        Bench.StatSources = sources;
        ImpactState.GetAbilityImpactStatSources(Caster, StatType.Attack).Should().BeSameAs(sources);
        Bench.StatSources = Array.Empty<StatAdjustmentSource>();
        ImpactState.GetAbilityImpactStatSources(Caster, StatType.Attack).Should().BeSameAs(sources, "one impact keeps the sources it read");
        Bench.StatSourceReads.Should().Be(4);
    }

    [Test]
    public void An_area_pulse_triggers_once_per_sequence()
    {
        ImpactState.TryTriggerAreaAbilityPulse(Caster).Should().BeFalse("no impact is active");

        Lifecycle.BeginAbilityImpact(Caster, Hostile());

        ImpactState.TryTriggerAreaAbilityPulse(Caster).Should().BeTrue();
        ImpactState.TryTriggerAreaAbilityPulse(Caster).Should().BeFalse();
    }

    [Test]
    public void Ending_an_impact_that_never_started_returns_an_empty_summary()
    {
        var summary = Lifecycle.EndAbilityImpact(Caster);

        summary.ImpactedTargetCount.Should().Be(0);
        ImpactState.GetLastCompletedAbilityImpactSummary(Caster).Should().BeNull();
        Bench.Log.Should().BeEmpty();
    }

    [Test]
    public void A_hostile_impact_with_a_critical_ends_in_order()
    {
        var ability = Hostile();
        Lifecycle.BeginAbilityImpact(Caster, ability);
        Bench.CriticalRating = 3;
        ImpactState.GetActiveAbilityImpactSummary(Caster).CriticalHitCount = 1;
        Bench.Log.Clear();

        var summary = Lifecycle.EndAbilityImpact(Caster);

        Bench.Log.Should().Equal(
            $"RefundCritical:{Caster}",
            $"ConsumePersistentCriticalRate:{Caster}:{SkillType.Force}",
            $"NotifyAttackAttempt:{Caster}:{SkillType.Force}",
            $"TechniqueComplete:{Caster}:0");
        ImpactState.GetActiveAbilityImpactSummary(Caster).Should().BeNull();
        ImpactState.GetLastCompletedAbilityImpactSummary(Caster).Should().BeSameAs(summary);
    }

    [Test]
    public void A_hostile_impact_without_a_critical_applies_the_non_critical_effects()
    {
        Lifecycle.BeginAbilityImpact(Caster, Hostile());
        Bench.Log.Clear();

        Lifecycle.EndAbilityImpact(Caster);

        Bench.Log.Should().Equal(
            $"NonCriticalEffects:{Caster}:{SkillType.Force}",
            $"NotifyAttackAttempt:{Caster}:{SkillType.Force}",
            $"TechniqueComplete:{Caster}:0");
    }

    [Test]
    public void An_impact_that_is_not_an_attack_attempt_only_refunds_a_critical()
    {
        Lifecycle.BeginAbilityImpact(Caster, Hostile(), countsAsAttackAttempt: false);
        ImpactState.GetActiveAbilityImpactSummary(Caster).CriticalHitCount = 1;
        Bench.Log.Clear();

        Lifecycle.EndAbilityImpact(Caster);

        Bench.Log.Should().Equal($"RefundCritical:{Caster}", $"TechniqueComplete:{Caster}:0");
    }

    [Test]
    public void A_support_impact_ends_with_no_combat_effects()
    {
        Lifecycle.BeginAbilityImpact(Caster, new AbilityDetail { Name = "Mend" });
        Bench.Log.Clear();

        Lifecycle.EndAbilityImpact(Caster);

        Bench.Log.Should().Equal($"TechniqueComplete:{Caster}:0");
    }

    [Test]
    public void The_last_completed_summary_can_be_cleared()
    {
        Lifecycle.BeginAbilityImpact(Caster, Hostile());
        Lifecycle.EndAbilityImpact(Caster);

        ImpactState.ClearLastCompletedAbilityImpactSummary(Caster);

        ImpactState.GetLastCompletedAbilityImpactSummary(Caster).Should().BeNull();
    }

    [Test]
    public void Queued_damage_lands_when_the_impact_ends_inside_a_command_of_the_activator()
    {
        ImpactState.TryQueueTrackedDamageEffect(Caster, Target, 5, DamageType.Fire).Should().BeFalse("no impact is active");
        Lifecycle.BeginAbilityImpact(Caster, Hostile());
        ImpactState.TryQueueTrackedDamageEffect(Caster, Target, 20, DamageType.Fire).Should().BeTrue();
        ImpactState.TryQueueTrackedDamageEffect(Caster, Other, 0, DamageType.Fire).Should().BeTrue("zero damage is dropped by the tracker");
        ImpactState.GetActiveAbilityImpactSummary(Caster).AttributedDamage.Should().Be(20);
        LogOf("Effect:").Should().BeEmpty();

        Lifecycle.EndAbilityImpact(Caster);

        Bench.Log.Should().Contain($"AssignCommand:{Caster}");
        LogOf("Effect:").Should().BeEmpty("the command has not run");
        Bench.RunDeferred();
        LogOf("Effect:").Should().Equal($"Effect:Damage:20:{DamageType.Fire}:{Target}");
        ImpactState.GetRemainingDamageTargetHP(Caster, Target).Should().Be(100);
    }

    [Test]
    public void Queued_damage_of_a_target_that_is_gone_when_the_command_runs_is_dropped()
    {
        Lifecycle.BeginAbilityImpact(Caster, Hostile());
        ImpactState.TryQueueTrackedDamageEffect(Caster, Target, 20, DamageType.Fire);
        Lifecycle.EndAbilityImpact(Caster);

        Bench.Invalid.Add(Target);
        Bench.RunDeferred();

        LogOf("Effect:").Should().BeEmpty();
    }

    [Test]
    public void Aborting_discards_the_queued_damage_and_completes_nothing()
    {
        Lifecycle.AbortAbilityImpact(Caster);
        Lifecycle.BeginAbilityImpact(Caster, Hostile());
        ImpactState.TryQueueTrackedDamageEffect(Caster, Target, 20, DamageType.Fire);
        Bench.Log.Clear();

        Lifecycle.AbortAbilityImpact(Caster);

        Bench.Log.Should().BeEmpty();
        ImpactState.GetActiveAbilityImpactSummary(Caster).Should().BeNull();
        ImpactState.GetLastCompletedAbilityImpactSummary(Caster).Should().BeNull();
    }

    [Test]
    public void The_defense_ignore_of_the_active_impact_accumulates()
    {
        ImpactState.AddActiveAbilityDefenseIgnorePercentAdjustment(Caster, 10);
        Lifecycle.BeginAbilityImpact(Caster, Hostile());

        ImpactState.AddActiveAbilityDefenseIgnorePercentAdjustment(Caster, 0);
        ImpactState.AddActiveAbilityDefenseIgnorePercentAdjustment(Caster, 25);

        var impact = AbilityInternals.TrackedImpacts(Service)[Caster]!;
        AbilityInternals.TrackedImpactType.GetProperty("NextAbilityDefenseIgnorePercentAdjustment")!.GetValue(impact).Should().Be(25);
    }

    [Test]
    public void The_force_affinity_reads_apply_only_to_an_active_force_impact()
    {
        Bench.ForceAffinityMultiplier = 1.5f;
        Bench.ForceAffinityRestorativeMultiplier = 1.25f;

        Magnitude.GetActiveForceAffinityMagnitudeMultiplier(Caster).Should().Be(1f);
        Magnitude.GetActiveForceAffinityRestorativeMagnitudeMultiplier(Caster).Should().Be(1f);
        Magnitude.ApplyActiveForceAffinityRestorativeMagnitude(Caster, 10).Should().Be(10);
        Magnitude.ApplyActiveForceAffinityMagnitude(Caster, 10).Should().Be(10);
        Magnitude.ApplyActiveForceAffinityMagnitude(Caster, 10f).Should().Be(10f);

        Lifecycle.BeginAbilityImpact(Caster, Hostile(skill: SkillType.Devices));
        Magnitude.GetActiveForceAffinityMagnitudeMultiplier(Caster).Should().Be(1f);
        Lifecycle.EndAbilityImpact(Caster);

        Lifecycle.BeginAbilityImpact(Caster, Hostile());
        Magnitude.GetActiveForceAffinityMagnitudeMultiplier(Caster).Should().Be(1.5f);
        Magnitude.GetActiveForceAffinityRestorativeMagnitudeMultiplier(Caster).Should().Be(1.25f);
        Magnitude.ApplyActiveForceAffinityRestorativeMagnitude(Caster, 10).Should().Be(11);
        Magnitude.ApplyActiveForceAffinityMagnitude(Caster, 10).Should().Be(12);
        Magnitude.ApplyActiveForceAffinityMagnitude(Caster, 10f).Should().Be(12.5f);
    }

    [TestCase(0, 100, 100)]
    [TestCase(25, 100, 125)]
    [TestCase(25, 3, 4)]
    [TestCase(25, 0, 0)]
    [TestCase(25, -4, -4)]
    public void Combat_readiness_scales_a_positive_magnitude_up(int readiness, int amount, int expected)
    {
        Bench.CombatReadiness = readiness;

        Magnitude.ApplyCombatReadinessMagnitude(Caster, amount).Should().Be(expected);
    }

    [Test]
    public void Combat_readiness_scales_an_activated_ability_only_during_its_impact()
    {
        Bench.CombatReadiness = 50;

        Magnitude.ApplyCombatReadinessToActivatedAbilityMagnitude(Caster, 10).Should().Be(10);

        Lifecycle.BeginAbilityImpact(Caster, Hostile());
        Magnitude.ApplyCombatReadinessToActivatedAbilityMagnitude(Caster, 10).Should().Be(15);
    }

    [TestCase(0, 0, 1, 100)]
    [TestCase(50, 0, 1, 150)]
    [TestCase(-50, 0, 1, 100)]
    [TestCase(0, 0, 2, 33)]
    [TestCase(0, 1, 2, 67)]
    public void Hostile_enmity_is_the_base_plus_the_bonus_split_across_a_volley(int bonus, int index, int count, int expected)
    {
        HostileImpact.CalculateHostileAbilityEnmity(bonus, index, count).Should().Be(expected);
    }

    [Test]
    public void Hostile_enmity_uses_the_volley_of_the_explicit_weapon_profile()
    {
        HostileImpact.ApplyHostileAbilityEnmity(Caster, Target, 50);
        Bench.Log.Should().Contain($"ModifyEnmity:{Caster}:{Target}:150");
        Bench.Log.Clear();

        var shot = new WeaponShotProfile(900, SkillType.Pistol, 10, CombatDamageType.Fire, 0, 0, AbilityType.Perception, string.Empty);
        using (new WeaponImpactProfile(Caster, shot, new WeaponImpactActivation(), volleyIndex: 1, volleyCount: 2))
            HostileImpact.ApplyHostileAbilityEnmity(Caster, Target);

        Bench.Log.Should().Contain($"ModifyEnmity:{Caster}:{Target}:67");
    }

    [Test]
    public void The_weapon_damage_type_prefers_the_explicit_profile_then_keeps_an_authored_element()
    {
        var shot = new WeaponShotProfile(900, SkillType.Pistol, 10, CombatDamageType.Fire, 0, 0, AbilityType.Perception, string.Empty);
        using (new WeaponImpactProfile(Caster, shot, new WeaponImpactActivation()))
            HostileImpact.GetWeaponAbilityDamageType(Caster, SkillType.Pistol, CombatDamageType.Physical).Should().Be(CombatDamageType.Fire);

        Bench.IncludesWeaponDamage = true;
        Bench.WeaponDamageType = CombatDamageType.Electrical;
        HostileImpact.GetWeaponAbilityDamageType(Caster, SkillType.Pistol, CombatDamageType.Ice).Should().Be(CombatDamageType.Ice);
        HostileImpact.GetWeaponAbilityDamageType(Caster, SkillType.Pistol, CombatDamageType.Physical).Should().Be(CombatDamageType.Electrical);

        Bench.IncludesWeaponDamage = false;
        HostileImpact.GetWeaponAbilityDamageType(Caster, SkillType.Pistol, CombatDamageType.Physical).Should().Be(CombatDamageType.Physical);
    }

    [Test]
    public void The_weapon_damage_type_of_an_active_impact_is_the_one_it_captured()
    {
        Bench.IncludesWeaponDamage = true;
        Lifecycle.BeginAbilityImpact(Caster, Hostile(skill: SkillType.Pistol), triggeringWeaponDamage: 3, triggeringWeaponDamageType: CombatDamageType.Fire);
        Bench.WeaponDamageType = CombatDamageType.Electrical;

        HostileImpact.GetWeaponAbilityDamageType(Caster, SkillType.Pistol, CombatDamageType.Physical).Should().Be(CombatDamageType.Fire);
    }

    // ----- Visual effects -----

    [Test]
    public void A_successful_impact_visual_plays_once_per_recipient()
    {
        var ability = Hostile();
        ability.SuccessfulImpactVisualEffect = VisualEffect.Vfx_Fnf_Sound_Burst;
        Service.PlaySuccessfulImpactVisualEffect(Caster, Target);
        LogOf("Effect:").Should().BeEmpty("no impact is active");

        Lifecycle.BeginAbilityImpact(Caster, ability);
        Service.GetSuccessfulImpactVisualEffect(Caster).Should().Be(VisualEffect.Vfx_Fnf_Sound_Burst);

        Service.PlaySuccessfulImpactVisualEffect(Caster, Target);
        Service.PlaySuccessfulImpactVisualEffect(Caster, Target);
        Service.PlaySuccessfulImpactVisualEffect(Caster, Other);

        LogOf("Effect:").Should().Equal(
            $"Effect:Vfx:{VisualEffect.Vfx_Fnf_Sound_Burst}:{Target}",
            $"Effect:Vfx:{VisualEffect.Vfx_Fnf_Sound_Burst}:{Other}");
    }

    [Test]
    public void A_captured_visual_outlives_the_impact_and_a_concealed_recipient_sees_it_alone()
    {
        var plugin = new FakePlayerPlugin();
        using var pluginScope = PlayerPluginRuntime.Override(plugin);
        var ability = Hostile();
        ability.SuccessfulImpactVisualEffect = VisualEffect.Vfx_Fnf_Sound_Burst;
        Lifecycle.BeginAbilityImpact(Caster, ability);
        var play = Service.CaptureSuccessfulImpactVisualEffect(Caster);
        Lifecycle.EndAbilityImpact(Caster);
        Bench.Players.Add(Target);
        Bench.Concealed.Add(Target);

        play(Target);
        play(Target);
        play(Other);

        plugin.CallsTo("ApplyInstantVisualEffectToObject").Should().ContainSingle();
        LogOf("Effect:").Should().Equal($"Effect:Vfx:{VisualEffect.Vfx_Fnf_Sound_Burst}:{Other}");
    }

    [Test]
    public void A_player_only_visual_is_for_a_living_player_and_never_for_nothing()
    {
        var plugin = new FakePlayerPlugin();
        using var pluginScope = PlayerPluginRuntime.Override(plugin);
        Bench.Players.Add(Caster);
        Bench.Dms.Add(Other);
        Bench.Players.Add(Other);
        Bench.Invalid.Add(600);

        Service.ShowVisualEffectToPlayer(Caster, Target, VisualEffect.None);
        Service.ShowVisualEffectToPlayer(Caster, 600, VisualEffect.Vfx_Fnf_Sound_Burst);
        Service.ShowVisualEffectToPlayer(Target, Target, VisualEffect.Vfx_Fnf_Sound_Burst);
        Service.ShowVisualEffectToPlayer(Other, Target, VisualEffect.Vfx_Fnf_Sound_Burst);
        plugin.CallsTo("ApplyInstantVisualEffectToObject").Should().BeEmpty();

        Service.ShowVisualEffectToPlayer(Caster, Target, VisualEffect.Vfx_Fnf_Sound_Burst);
        plugin.CallsTo("ApplyInstantVisualEffectToObject").Should().ContainSingle();
    }

    // ----- Recurring impacts -----

    [Test]
    public void A_recurring_impact_without_an_origin_is_the_action_itself()
    {
        Action<uint> action = _ => { };

        Lifecycle.CaptureRepeatedAbilityImpact(Caster, action).Should().BeSameAs(action);

        var nullAction = () => Lifecycle.CaptureRepeatedAbilityImpact(Caster, (Action)null!);
        nullAction.Should().Throw<ArgumentNullException>();
        var nullPayload = () => Lifecycle.CaptureRepeatedAbilityImpact<uint>(Caster, null!);
        nullPayload.Should().Throw<ArgumentNullException>();
        var nullBatch = () => Lifecycle.CaptureRepeatedAbilityImpactBatch<uint>(Caster, null!);
        nullBatch.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void A_recurring_pulse_tracks_its_own_impact_then_restores_the_one_it_interrupted()
    {
        var ability = Hostile();
        Lifecycle.BeginAbilityImpact(Caster, ability);
        var pulses = new List<AbilityImpactSummary>();
        var pulse = Lifecycle.CaptureRepeatedAbilityImpact<uint>(Caster, target =>
        {
            pulses.Add(ImpactState.GetActiveAbilityImpactSummary(Caster));
            Bench.Log.Add($"Pulse:{target}");
        });
        var origin = ImpactState.GetActiveAbilityImpactSummary(Caster);
        Bench.Recording = true;
        Bench.Log.Clear();

        pulse(Target);

        pulses.Should().ContainSingle().Which.Should().NotBeSameAs(origin);
        Bench.Log.Should().ContainInOrder(
            $"RecorderAbility:{Caster}:Blast",
            $"Pulse:{Target}",
            $"TechniqueComplete:{Caster}:0",
            $"RecorderRecurring:{Caster}:Blast",
            $"ApplyAbilityImpactEffects:{Caster}:0");
        LogOf("NotifyAttackAttempt").Should().BeEmpty("a pulse is not an attack attempt");
        ImpactState.GetActiveAbilityImpactSummary(Caster).Should().BeSameAs(origin);
    }

    [Test]
    public void A_recurring_pulse_needs_a_living_activator_unless_it_was_told_otherwise()
    {
        Lifecycle.BeginAbilityImpact(Caster, Hostile());
        var runs = 0;
        var living = Lifecycle.CaptureRepeatedAbilityImpact(Caster, () => runs++);
        var corpse = Lifecycle.CaptureRepeatedAbilityImpact(Caster, () => runs++, requiresLivingActivator: false);
        Lifecycle.EndAbilityImpact(Caster);
        Bench.Hp[Caster] = 0;

        living();
        runs.Should().Be(0);

        corpse();
        runs.Should().Be(1);

        Bench.Invalid.Add(Caster);
        corpse();
        runs.Should().Be(1);
    }

    [Test]
    public void A_pulse_that_fails_aborts_its_impact_and_restores_the_interrupted_one()
    {
        Lifecycle.BeginAbilityImpact(Caster, Hostile());
        var pulse = Lifecycle.CaptureRepeatedAbilityImpact(Caster, () => throw new InvalidOperationException("boom"));
        var origin = ImpactState.GetActiveAbilityImpactSummary(Caster);

        var run = () => pulse();

        run.Should().Throw<InvalidOperationException>();
        ImpactState.GetActiveAbilityImpactSummary(Caster).Should().BeSameAs(origin);
    }

    [Test]
    public void A_pulse_inherits_the_captured_bonus_until_one_of_them_hits_a_target()
    {
        Bench.NextAbilityDamageBonus = 7;
        var ability = Hostile();
        Lifecycle.BeginAbilityImpact(Caster, ability);
        var seen = new List<int>();
        var hit = false;
        var pulse = Lifecycle.CaptureRepeatedAbilityImpact(Caster, () =>
        {
            seen.Add(HostileImpact.ApplyCapturedAbilityDamageBonus(Caster, 10));
            if (hit)
                RecordTarget(Target);
        }, baseDamage: 10);
        Lifecycle.EndAbilityImpact(Caster);

        pulse();
        hit = true;
        pulse();
        pulse();

        seen.Should().Equal(17, 17, 10);
    }

    [Test]
    public void A_retained_capture_gives_every_portion_the_same_bonus()
    {
        Bench.NextAbilityDamageBonus = 7;
        Lifecycle.BeginAbilityImpact(Caster, Hostile());
        var seen = new List<int>();
        var portion = Lifecycle.CaptureRepeatedAbilityImpact<uint>(Caster, target =>
        {
            seen.Add(HostileImpact.ApplyCapturedAbilityDamageBonus(Caster, 10));
            RecordTarget(target);
        }, baseDamage: 10, retainCapturedBonuses: true);
        Lifecycle.EndAbilityImpact(Caster);

        portion(Target);
        portion(Other);

        seen.Should().Equal(17, 17);
    }

    [Test]
    public void A_batch_shares_one_impact_and_completes_on_its_last_arrival()
    {
        Bench.Recording = true;
        Lifecycle.BeginAbilityImpact(Caster, Hostile());
        var interrupted = ImpactState.GetActiveAbilityImpactSummary(Caster);
        var seen = new List<AbilityImpactSummary>();
        var batch = Lifecycle.CaptureRepeatedAbilityImpactBatch<uint>(Caster, target =>
        {
            seen.Add(ImpactState.GetActiveAbilityImpactSummary(Caster));
            RecordTarget(target);
        }, baseDamage: 10);
        var apply = batch(2);
        Bench.Log.Clear();

        apply(Target);
        ImpactState.GetActiveAbilityImpactSummary(Caster).Should().BeSameAs(interrupted);
        LogOf("TechniqueComplete").Should().BeEmpty();

        apply(Other);

        seen.Should().HaveCount(2);
        seen[0].Should().BeSameAs(seen[1]);
        seen[0].ImpactedTargetCount.Should().Be(2);
        Bench.Log.Should().ContainInOrder(
            $"TechniqueComplete:{Caster}:2",
            $"RecorderRecurring:{Caster}:Blast",
            $"ApplyAbilityImpactEffects:{Caster}:2");
        ImpactState.GetActiveAbilityImpactSummary(Caster).Should().BeSameAs(interrupted);
    }

    [Test]
    public void A_batch_without_an_origin_applies_its_payloads_as_they_arrive()
    {
        var seen = new List<uint>();
        var apply = Lifecycle.CaptureRepeatedAbilityImpactBatch<uint>(Caster, seen.Add)(2);
        Bench.Hp[Caster] = 0;

        apply(Target);
        apply(Other);

        seen.Should().Equal(Target, Other);
    }

    [Test]
    public void A_batch_of_a_dead_activator_applies_nothing()
    {
        Lifecycle.BeginAbilityImpact(Caster, Hostile());
        var seen = new List<uint>();
        var apply = Lifecycle.CaptureRepeatedAbilityImpactBatch<uint>(Caster, seen.Add)(1);
        Lifecycle.EndAbilityImpact(Caster);
        Bench.Hp[Caster] = 0;

        apply(Target);

        seen.Should().BeEmpty();
    }

    private void RecordTarget(uint target)
    {
        var impact = AbilityInternals.TrackedImpacts(Service)[Caster]!;
        AbilityInternals.TrackedImpactType.GetMethod("RecordTarget")!.Invoke(impact, new object[] { target });
    }
}
