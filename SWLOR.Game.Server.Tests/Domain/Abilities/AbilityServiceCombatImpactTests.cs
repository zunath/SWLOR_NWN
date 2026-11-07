using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.ResistanceService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Tests.Domain.Abilities;

/// <summary>
/// One hit of a hostile impact: the order of its steps, the damage calculation for players, creatures and unscaled
/// payloads, the status effects it applies, and the Dark Force conversion.
/// </summary>
[TestFixture]
public class AbilityServiceCombatImpactTests : AbilityServiceTestBase
{
    private sealed class ControlEffect : ProbeEffect
    {
        public ControlEffect()
        {
            CategoriesValue = StatusEffectCategory.Control;
        }
    }

    private sealed class SecondEffect : ProbeEffect
    {
        public SecondEffect()
        {
            CategoriesValue = StatusEffectCategory.Debuff;
        }
    }

    private int Impact(
        AbilityDetail? ability = null,
        int baseDamage = 10,
        int duration = 0,
        Type? statusEffect = null,
        bool isArea = false,
        uint target = Target,
        bool useNpcScaling = false,
        bool unscaled = false,
        bool resolvesHit = true,
        bool awardsCombatPoints = true,
        bool canCritical = true,
        IEnumerable<Type>? additional = null,
        Func<IStatusEffect>? factory = null,
        Func<uint, int>? percent = null,
        Func<uint, int>? baseAdjustment = null,
        ResistanceType resistance = ResistanceType.Invalid,
        VisualEffect targetVisual = VisualEffect.None,
        VisualEffect areaVisual = VisualEffect.None,
        Action<uint>? beforeImpact = null,
        Action<uint>? afterHit = null,
        Func<uint, StatusEffectCategory?>? riders = null,
        bool playImpactAnimation = false,
        Animation impactAnimation = Animation.Invalid,
        int enmityBonus = 0,
        CombatDamageType damageType = CombatDamageType.Physical)
    {
        if (ability != null)
            Service.BeginAbilityImpact(Caster, ability);
        return Service.ApplyCombatImpact(
            Caster, target, Bench.At(AbilityBench.TheArea, Vector3.Zero), SkillType.Force, baseDamage, duration, statusEffect!, isArea,
            additional!, factory!, damageType, resistance, targetVisual, areaVisual, percent!, baseAdjustment!, impactAnimation, enmityBonus,
            afterHit!, riders!, useNPCStatScaling: useNpcScaling, awardsCombatPoints: awardsCombatPoints, playImpactAnimation: playImpactAnimation,
            resolvesHit: resolvesHit, canCritical: canCritical, useUnscaledDamage: unscaled, beforeImpact: beforeImpact!);
    }

    // ----- Order -----

    [Test]
    public void A_tracked_hit_runs_its_steps_in_order()
    {
        var ability = Hostile();
        ability.DamageImpactVisualEffect = VisualEffect.Vfx_Fnf_Sound_Burst;
        Service.BeginAbilityImpact(Caster, ability);
        Service.StatusAppliedBonus = 0;
        Bench.Log.Clear();

        var dealt = Service.ApplyHostileCombatImpact(Caster, Target, SkillType.Force, 25, CombatDamageType.Force, typeof(ControlEffect), 6);

        dealt.Should().Be(25);
        Bench.Log.Should().Equal(
            $"HealingScope:{Caster}:{Target}",
            "IsTargetAttackingAlly",
            $"BattleTheme:{Target}:{Caster}",
            $"AddCombatPoint:{Caster}:{Target}:{SkillType.Force}:3",
            "ConsumeSameTargetPressure:25",
            "DamageDealtEffects:25:True:True",
            $"NotifyDamage:{Caster}:{Target}:25:{CombatDamageType.Force}",
            $"ModifyEnmity:{Caster}:{Target}:100",
            $"ApplyStatus:{Caster}:{Target}:ControlEffect:6:{CombatDamageType.Force}",
            $"CapstoneProof:{Caster}:{Target}:Blast",
            $"ImpactRiders:{Target}:25:True:False:True:Control:True:False",
            $"Effect:Vfx:{VisualEffect.Vfx_Fnf_Sound_Burst}:{Target}",
            "HealingScopeEnd");
        LogOf("TempHpFeedback", "DamageReflection", "AssignCommand").Should().BeEmpty("a tracked hit queues its damage");
        Service.GetActiveAbilityImpactSummary(Caster).ImpactedTargetCount.Should().Be(1);
        Service.GetActiveAbilityImpactSummary(Caster).AttributedDamage.Should().Be(25);
    }

    [Test]
    public void The_queued_direct_damage_of_a_tracked_hit_resolves_its_lifecycle_before_each_effect()
    {
        Service.BeginAbilityImpact(Caster, Hostile());
        Service.ApplyHostileCombatImpact(Caster, Target, SkillType.Force, 25, CombatDamageType.Force);
        Service.ApplyHostileCombatImpact(Caster, Other, SkillType.Force, 5, CombatDamageType.Force);
        Service.EndAbilityImpact(Caster);
        Bench.Log.Clear();

        Bench.RunDeferred();

        Bench.Log.Should().Equal(
            $"RecorderAbility:{Caster}:Blast",
            $"NotifyPreDamage:{Caster}:{Target}:25:{CombatDamageType.Force}",
            $"TempHpFeedback:{Caster}:{Target}:25",
            $"DamageReflection:{Caster}:{Target}:25:{CombatDamageType.Force}",
            $"Effect:Damage:25:{CombatDamageType.Force.GetNWScriptDamageType()}:{Target}",
            $"NotifyPreDamage:{Caster}:{Other}:5:{CombatDamageType.Force}",
            $"TempHpFeedback:{Caster}:{Other}:5",
            $"DamageReflection:{Caster}:{Other}:5:{CombatDamageType.Force}",
            $"Effect:Damage:5:{CombatDamageType.Force.GetNWScriptDamageType()}:{Other}");
    }

    [Test]
    public void An_untracked_hit_lands_its_damage_at_once_and_applies_its_reflection_after_the_notifications()
    {
        var dealt = Service.ApplyHostileCombatImpact(Caster, Target, SkillType.Force, 25, CombatDamageType.Force, effectDamageType: DamageType.Fire);

        dealt.Should().Be(25);
        Bench.Log.Should().ContainInOrder(
            $"AddCombatPoint:{Caster}:{Target}:{SkillType.Force}:3",
            $"NotifyPreDamage:{Caster}:{Target}:25:{CombatDamageType.Force}",
            $"TempHpFeedback:{Caster}:{Target}:25",
            $"AssignCommand:{Caster}",
            "ConsumeSameTargetPressure:25",
            "DamageDealtEffects:25:True:True",
            $"NotifyDamage:{Caster}:{Target}:25:{CombatDamageType.Force}",
            $"DamageReflection:{Caster}:{Target}:25:{CombatDamageType.Force}",
            $"ModifyEnmity:{Caster}:{Target}:100");

        Bench.RunDeferred();
        LogOf("Effect:Damage").Should().Equal($"Effect:Damage:25:{DamageType.Fire}:{Target}");
    }

    [Test]
    public void A_hit_can_withhold_its_combat_points_and_a_zero_damage_hit_skips_every_damage_step()
    {
        Service.ApplyHostileCombatImpact(Caster, Target, SkillType.Force, 0, CombatDamageType.Force, awardsCombatPoints: false);

        LogOf("AddCombatPoint", "NotifyPreDamage", "NotifyDamage", "DamageDealtEffects", "ConsumeSameTargetPressure").Should().BeEmpty();
        Bench.Log.Should().Contain($"ModifyEnmity:{Caster}:{Target}:100");
    }

    [Test]
    public void A_technique_rolls_its_hit_procs_once_per_target_across_the_cast()
    {
        var ability = Hostile(skill: SkillType.Vibroblade);
        ability.MeleeTechnique = new MeleeTechniqueDetail();
        Service.BeginAbilityImpact(Caster, ability);

        Service.ApplyHostileCombatImpact(Caster, Target, SkillType.Vibroblade, 5, CombatDamageType.Physical);
        Service.ApplyHostileCombatImpact(Caster, Target, SkillType.Vibroblade, 5, CombatDamageType.Physical);

        LogOf("DamageDealtEffects").Should().Equal("DamageDealtEffects:5:True:True", "DamageDealtEffects:5:True:False");
    }

    [Test]
    public void The_first_damaging_hit_consumes_a_status_applied_bonus_once()
    {
        var ability = Hostile();
        Service.StatusAppliedBonus = 9;
        ability.DealsDeferredDamage = true;
        Service.BeginAbilityImpact(Caster, ability);
        Service.ApplyCapturedAbilityDamageBonus(Caster, 10).Should().Be(19);

        Service.ApplyHostileCombatImpact(Caster, Target, SkillType.Force, 5, CombatDamageType.Force);
        Service.ApplyHostileCombatImpact(Caster, Other, SkillType.Force, 5, CombatDamageType.Force);

        LogOf("ConsumeStatusAppliedBonus").Should().ContainSingle();
        Service.ApplyCapturedAbilityDamageBonus(Caster, 10).Should().Be(10);
    }

    [Test]
    public void A_status_rider_of_the_hit_counts_as_applied_status()
    {
        Service.ApplyHostileCombatImpact(Caster, Target, SkillType.Force, 0, CombatDamageType.Force,
            beforeSuccessfulImpactRiders: _ => StatusEffectCategory.Control);

        LogOf("ImpactRiders").Should().Equal($"ImpactRiders:{Target}:0:True:False:True:Control:True:False");
    }

    [Test]
    public void The_authored_visual_wins_over_the_hit_visual_and_needs_damage_or_a_status()
    {
        Service.ApplyHostileCombatImpact(Caster, Target, SkillType.Force, 0, CombatDamageType.Force, targetVisualEffect: VisualEffect.Vfx_Fnf_Sound_Burst);
        LogOf("Effect:").Should().BeEmpty("the hit neither damaged nor applied anything");

        Service.ApplyHostileCombatImpact(Caster, Target, SkillType.Force, 5, CombatDamageType.Force, targetVisualEffect: VisualEffect.Vfx_Fnf_Sound_Burst);
        Bench.Log.Should().Contain($"Effect:Vfx:{VisualEffect.Vfx_Fnf_Sound_Burst}:{Target}");
        Bench.Log.Clear();

        var ability = Hostile();
        ability.SuccessfulImpactVisualEffect = VisualEffect.Vfx_Fnf_Sound_Burst;
        Service.BeginAbilityImpact(Caster, ability);
        Service.ApplyHostileCombatImpact(Caster, Target, SkillType.Force, 5, CombatDamageType.Force, targetVisualEffect: VisualEffect.Vfx_Dur_Aura_Blue);
        LogOf("Effect:").Should().Equal($"Effect:Vfx:{VisualEffect.Vfx_Fnf_Sound_Burst}:{Target}");
    }

    // ----- Rolling the hit -----

    [Test]
    public void A_miss_reports_the_miss_awards_one_point_and_applies_the_miss_enmity()
    {
        Service.HitResolves = false;
        Service.HitRate = 40;
        Bench.TechniqueAccuracy = 3;
        Bench.Recording = true;
        var ability = Hostile();
        Service.BeginAbilityImpact(Caster, ability);
        Bench.Log.Clear();

        var dealt = Service.ApplyCombatImpact(Caster, Target, Bench.Nowhere(), SkillType.Force, 10, 0, null!, false, hitChancePercentAdjustment: 5);

        dealt.Should().Be(0);
        Bench.Log.Should().Equal(
            "ConsumeNextAbilityDamageBonus",
            "ConsumeNextSkillAbilityBonuses:None",
            "GetPersistentCriticalRate",
            "ConsumeGuardedHitBonuses",
            "GetStatusAppliedBonus",
            $"HealingScope:{Caster}:{Target}",
            $"TrackHostileActivity:{Caster}",
            $"TrackDefensiveEntry:{Target}:{Caster}",
            $"BattleTheme:{Target}:{Caster}",
            $"ResolveHit:5:-1:{AbilityType.Invalid}:3",
            $"RecorderAttack:{Caster}:{Target}:4:40:Blast",
            $"Nearby:{Target}:Blast:4:40:60",
            $"AddCombatPoint:{Caster}:{Target}:{SkillType.Force}:1",
            $"ModifyEnmity:{Caster}:{Target}:1",
            "HealingScopeEnd");
    }

    [Test]
    public void A_hit_reports_the_hit_before_it_calculates_damage()
    {
        var ability = Hostile();
        Service.BeginAbilityImpact(Caster, ability);
        Bench.Log.Clear();

        Service.ApplyCombatImpact(Caster, Target, Bench.Nowhere(), SkillType.Force, 10, 0, null!, false);

        Bench.Log.Should().ContainInOrder(
            $"ResolveHit:0:-1:{AbilityType.Invalid}:0",
            $"Nearby:{Target}:Blast:1:75:60",
            $"GetAttack:{Caster}:{AbilityType.Might}:{SkillType.Force}");
    }

    [Test]
    public void An_invalid_technique_impact_stops_before_anything_happens()
    {
        Bench.TechniqueImpactValid = false;

        Impact(Hostile()).Should().Be(0);

        LogOf("ResolveHit", "BattleTheme", "AddCombatPoint", "ModifyEnmity").Should().BeEmpty();
    }

    [Test]
    public void A_weapon_ability_does_not_roll_because_its_carrier_attack_already_did()
    {
        var ability = Hostile(skill: SkillType.Vibroblade);
        ability.ActivationType = AbilityActivationType.Weapon;

        Impact(ability, baseDamage: 0).Should().BeGreaterThanOrEqualTo(0);

        LogOf("ResolveHit").Should().BeEmpty();
    }

    [Test]
    public void Without_a_roll_the_impact_never_rolls()
    {
        Impact(Hostile(), resolvesHit: false);

        LogOf("ResolveHit").Should().BeEmpty();
        Bench.Log.Should().NotContain(entry => entry.StartsWith("Nearby:"));
    }

    // ----- Damage -----

    [Test]
    public void The_player_damage_path_runs_in_order_and_adds_every_bonus()
    {
        Service.CriticalRating = 2;
        Service.WeaponDamage = 7;
        Service.NextAbilityDamageBonus = 4;
        Service.IdleBonuses = (3, 0, 11);
        var ability = Hostile();
        ability.DealsDeferredDamage = true;
        ability.MeleeTechnique = null!;
        Service.BeginAbilityImpact(Caster, ability);
        Bench.Log.Clear();

        var dealt = Service.ApplyCombatImpact(Caster, Target, Bench.Nowhere(), SkillType.Force, 10, 0, null!, false, criticalRatePercentAdjustment: 6);

        dealt.Should().Be(10 + 7 + 3 + 4 + 2, "base, weapon, idle, captured and the force affinity of the bench");
        Bench.Log.Should().ContainInOrder(
            $"GetAttack:{Caster}:{AbilityType.Might}:{SkillType.Force}",
            $"GetDefense:{Target}:{CombatDamageType.Physical}:{CombatDamageType.Physical.GetDefenseAbilityType()}",
            "DefenseIgnore:20:0",
            "CriticalRating:False:6",
            "DamageRoll:40:24:10:20:10:2",
            "CriticalDamage:24:2:11",
            "DealtModifiers:26:0:0",
            "ForceConversion:26",
            "TypedLeadership:26",
            $"Resistance:{Target}:{CombatDamageType.Physical}:26",
            "TakenModifiers:26:True",
            "CriticalFeedback:Blast",
            "CriticalEffects:26:2:True");
        Service.GetActiveAbilityImpactSummary(Caster).CriticalHitCount.Should().Be(1);
    }

    [Test]
    public void A_downgraded_critical_sends_its_feedback_and_counts_as_no_critical()
    {
        Service.CriticalRating = 0;
        Service.CriticalDowngraded = true;

        Impact(Hostile());

        LogOf("CriticalDowngradeFeedback").Should().ContainSingle();
        LogOf("CriticalFeedback", "CriticalEffects").Should().BeEmpty();
    }

    [Test]
    public void An_impact_that_cannot_critical_skips_the_rating()
    {
        Service.CriticalRating = 5;

        Impact(Hostile(), canCritical: false);

        LogOf("CriticalRating", "CriticalFeedback").Should().BeEmpty();
    }

    [Test]
    public void A_control_only_impact_deals_no_damage_and_leaves_its_bonuses_armed()
    {
        Service.NextAbilityDamageBonus = 4;

        Impact(Hostile(), baseDamage: 0).Should().Be(0);

        LogOf("ConsumeNextAbilityDamageBonus", "DamageRoll", "DealtModifiers").Should().BeEmpty();
        LogOf("ImpactRiders").Should().Equal($"ImpactRiders:{Target}:0:False:False:True:None:True:False");
    }

    [Test]
    public void The_authored_adjustments_only_apply_to_a_damaging_impact()
    {
        var percentCalls = 0;
        Impact(Hostile(), baseDamage: 0, percent: _ => { percentCalls++; return 50; });
        percentCalls.Should().Be(0);
        Service.EndAbilityImpact(Caster);
        Bench.Log.Clear();

        Impact(Hostile(), baseDamage: 10, percent: _ => 50, baseAdjustment: _ => 5, unscaled: true);

        Bench.Log.Should().Contain("DealtModifiers:15:50:0");
    }

    [Test]
    public void The_low_hp_bonus_of_a_dark_force_ability_reads_the_target()
    {
        var ability = Hostile();
        ability.TriggersDarkForceConversion = true;
        Service.Stats[(Caster, StatType.DarkForceTargetLowHPDamageThresholdPercent)] = 50;
        Service.Stats[(Caster, StatType.DarkForceTargetLowHPDamagePercentAdjustment)] = 30;
        Bench.Hp[Target] = 40;

        Impact(ability, unscaled: true);

        Bench.Log.Should().Contain("DealtModifiers:10:0:30");
        Bench.Log.Clear();
        Service.EndAbilityImpact(Caster);

        Bench.Hp[Target] = 90;
        Impact(ability, unscaled: true);
        Bench.Log.Should().Contain("DealtModifiers:10:0:0", "the target is above the threshold");
    }

    [Test]
    public void The_unscaled_path_uses_only_the_base_and_captured_damage()
    {
        Service.CombatReadiness = 50;
        Service.WeaponDamage = 99;

        Impact(Hostile(), baseDamage: 10, unscaled: true).Should().Be(15);

        Bench.Log.Should().ContainInOrder("DealtModifiers:10:0:0", "ForceConversion:15", "TypedLeadership:15", $"Resistance:{Target}:{CombatDamageType.Physical}:15", "TakenModifiers:15:True");
        LogOf("GetAttack", "DamageRoll").Should().BeEmpty();
    }

    [Test]
    public void Unscaled_damage_keeps_its_authored_type()
    {
        Service.IncludesWeaponDamage = true;
        Service.WeaponDamageType = CombatDamageType.Electrical;

        Impact(Hostile(), unscaled: true);
        Bench.Log.Should().Contain($"Resistance:{Target}:{CombatDamageType.Physical}:10");
        Bench.Log.Clear();
        Service.EndAbilityImpact(Caster);

        Impact(Hostile());
        Bench.Log.Should().Contain($"Resistance:{Target}:{CombatDamageType.Electrical}:12", "the bench's force affinity adds 2");
    }

    [Test]
    public void A_volley_partitions_the_damage_after_the_modifiers()
    {
        var shot = new WeaponShotProfile(900, SkillType.Pistol, 8, CombatDamageType.Fire, 0, 0, AbilityType.Perception, string.Empty);
        using var profile = new WeaponImpactProfile(Caster, shot, new WeaponImpactActivation(), volleyIndex: 1, volleyCount: 2);

        Impact(Hostile(), baseDamage: 10).Should().Be(14, "the second of two shots takes two thirds of 20: 10, the weapon's 8 and the bench's force affinity of 2");
    }

    [Test]
    public void A_critical_of_a_volley_notifies_once_for_the_activation()
    {
        Service.CriticalRating = 1;
        var shot = new WeaponShotProfile(900, SkillType.Pistol, 8, CombatDamageType.Fire, 0, 0, AbilityType.Perception, string.Empty);
        var activation = new WeaponImpactActivation();
        Service.BeginAbilityImpact(Caster, Hostile());
        using (new WeaponImpactProfile(Caster, shot, activation))
        {
            Service.ApplyCombatImpact(Caster, Target, Bench.Nowhere(), SkillType.Force, 10, 0, null!, false);
            Service.ApplyCombatImpact(Caster, Other, Bench.Nowhere(), SkillType.Force, 10, 0, null!, false);
        }

        LogOf("CriticalFeedback").Should().HaveCount(2);
        LogOf("CriticalEffects").Should().ContainSingle();
    }

    [Test]
    public void A_technique_critical_notifies_once_for_the_cast()
    {
        Service.CriticalRating = 1;
        var ability = Hostile();
        ability.MeleeTechnique = new MeleeTechniqueDetail();
        Service.BeginAbilityImpact(Caster, ability);

        Service.ApplyCombatImpact(Caster, Target, Bench.Nowhere(), SkillType.Force, 10, 0, null!, false);
        Service.ApplyCombatImpact(Caster, Other, Bench.Nowhere(), SkillType.Force, 10, 0, null!, false);

        LogOf("CriticalEffects").Should().ContainSingle();
    }

    [Test]
    public void The_damage_of_a_committed_trait_is_added_once_per_weapon_budget()
    {
        var traits = new WeaponAbilityTraitActivation(new PairedRangedAbilityTraitsSnapshot(Guid.Empty, true, true, 1, 0), 5)
        {
            IsCommitted = true,
            PrimaryWeapon = 900
        };
        Service.BeginAbilityImpact(Caster, Hostile(skill: SkillType.Pistol), weaponTraits: traits);

        Service.ApplyCombatImpact(Caster, Target, Bench.Nowhere(), SkillType.Pistol, 10, 0, null!, false).Should().Be(15);
        Service.ApplyCombatImpact(Caster, Other, Bench.Nowhere(), SkillType.Pistol, 10, 0, null!, false).Should().Be(10);
    }

    [Test]
    public void The_npc_path_scales_the_damage_from_its_rank_and_attacks_from_its_own_level()
    {
        Service.NpcStats = new NPCStats { Level = 40, Attack = 8 };
        Service.NpcStats.Skills[SkillType.Force] = 20;
        Service.Stats[(Caster, StatType.Attack)] = 2;

        var dealt = Impact(Hostile(), baseDamage: 10, useNpcScaling: true);

        dealt.Should().Be(10 + 3 + 2, "ceil(20 * 0.15) is the scaled flat damage and the bench's force affinity adds 2");
        Bench.Log.Should().ContainInOrder(
            "GetNpcAttack:40:10:2",
            $"GetDefense:{Target}:{CombatDamageType.Physical}:{CombatDamageType.Physical.GetDefenseAbilityType()}",
            "DamageRoll:40:13:10:20:10:0");
    }

    [Test]
    public void An_npc_critical_runs_the_same_feedback_as_a_player_critical()
    {
        Service.NpcStats = new NPCStats { Level = 40 };
        Service.NpcStats.Skills[SkillType.Force] = 20;
        Service.CriticalRating = 2;
        Service.CriticalDowngraded = true;

        Impact(Hostile(), useNpcScaling: true);

        Bench.Log.Should().ContainInOrder(
            "CriticalDowngradeFeedback",
            "CriticalDamage:13:2:0",
            "CriticalFeedback:Blast",
            "CriticalEffects:15:2:True");
        Service.GetActiveAbilityImpactSummary(Caster).CriticalHitCount.Should().Be(1);
    }

    [Test]
    public void An_npc_volley_partitions_the_damage_after_the_modifiers()
    {
        Service.NpcStats = new NPCStats { Level = 40 };
        Service.NpcStats.Skills[SkillType.Force] = 20;
        var shot = new WeaponShotProfile(900, SkillType.Pistol, 8, CombatDamageType.Fire, 0, 0, AbilityType.Perception, string.Empty);
        using var profile = new WeaponImpactProfile(Caster, shot, new WeaponImpactActivation(), volleyIndex: 0, volleyCount: 2);

        Impact(Hostile(), baseDamage: 10, useNpcScaling: true).Should().Be(5, "the first of two shots takes a third of 15");
    }

    [Test]
    public void A_player_is_never_scaled_as_a_creature()
    {
        Bench.Players.Add(Caster);

        Impact(Hostile(), useNpcScaling: true);

        LogOf("GetNpcAttack").Should().BeEmpty();
        Bench.Log.Should().Contain($"GetAttack:{Caster}:{AbilityType.Might}:{SkillType.Force}");
    }

    [Test]
    public void An_npc_with_no_skill_rank_derives_it_from_its_highest_relevant_stat()
    {
        Service.NpcStats = new NPCStats { ForceAttack = 40 };
        Service.NpcStats.Defenses[CombatDamageType.Physical] = 30;
        Service.NpcStats.Resistances[ResistanceType.Fire] = 10;

        // The offense bonus of a force ability is 40, so the rank is 20 and ceil(20 * 0.15) is 3 (the bench's force affinity adds 2).
        Impact(Hostile(), baseDamage: 10, useNpcScaling: true).Should().Be(15);
        Service.EndAbilityImpact(Caster);

        Service.NpcStats = new NPCStats { Attack = 40 };
        Service.NpcStats.Defenses[CombatDamageType.Force] = 100;
        // No physical defense: the highest listed defense gives rank 50 and ceil(50 * 0.15) is 8.
        Impact(Hostile(), baseDamage: 10, useNpcScaling: true).Should().Be(20);
        Service.EndAbilityImpact(Caster);

        Service.NpcStats = new NPCStats();
        Service.NpcStats.Resistances[ResistanceType.Fire] = 200;
        // Fire resistance answers for a fire hit: rank 100 caps there, and ceil(100 * 0.15f) is 16 in single precision.
        Impact(Hostile(), baseDamage: 10, useNpcScaling: true, damageType: CombatDamageType.Fire).Should().Be(28);
    }

    [Test]
    public void The_npc_attack_modifiers_scale_the_attack_up()
    {
        Service.NpcStats = new NPCStats { Level = 10 };
        Service.NpcStats.Skills[SkillType.Force] = 20;
        Service.Stats[(Caster, StatType.AttackPercentAdjustment)] = 10;
        Service.Stats[(Caster, StatType.ForceAttackPercentAdjustment)] = 15;
        Service.Stats[(Caster, StatType.HighFPAndStaminaAttackThresholdPercent)] = 50;
        Service.Stats[(Caster, StatType.HighFPAndStaminaAttackPercentAdjustment)] = 25;

        Impact(Hostile(), useNpcScaling: true);

        Bench.Log.Should().Contain(entry => entry.StartsWith("DamageRoll:60:"), "40 raised by 50 percent");
    }

    [TestCase(40, 100, 100, 100, 0)]
    [TestCase(100, 40, 100, 100, 0)]
    [TestCase(100, 100, 0, 100, 0)]
    public void The_high_resource_attack_bonus_needs_both_resources_above_the_threshold(int fp, int stamina, int maxFp, int maxStamina, int percent)
    {
        Service.NpcStats = new NPCStats();
        Service.NpcStats.Skills[SkillType.Force] = 20;
        Service.CurrentFP = fp;
        Service.CurrentStamina = stamina;
        Service.MaxFP = maxFp;
        Service.MaxStamina = maxStamina;
        Service.Stats[(Caster, StatType.HighFPAndStaminaAttackThresholdPercent)] = 50;
        Service.Stats[(Caster, StatType.HighFPAndStaminaAttackPercentAdjustment)] = 25;

        Impact(Hostile(), useNpcScaling: true);

        Bench.Log.Should().Contain(entry => entry.StartsWith("DamageRoll:40:"));
    }

    [Test]
    public void A_negative_npc_attack_modifier_lowers_the_attack_but_never_below_one()
    {
        Service.NpcStats = new NPCStats();
        Service.NpcStats.Skills[SkillType.Devices] = 20;
        Service.Stats[(Caster, StatType.AttackPercentAdjustment)] = -200;

        Service.ApplyCombatImpact(Caster, Target, Bench.Nowhere(), SkillType.Devices, 10, 0, null!, false, useNPCStatScaling: true);

        Bench.Log.Should().Contain(entry => entry.StartsWith("DamageRoll:1:"));
    }

    // ----- Status effects -----

    [Test]
    public void A_factory_replaces_the_primary_status_and_additional_statuses_apply_once_each()
    {
        var ability = Hostile();
        Service.BeginAbilityImpact(Caster, ability);
        Bench.Log.Clear();

        Service.ApplyHostileCombatImpact(Caster, Target, SkillType.Force, 0, CombatDamageType.Force, typeof(ProbeEffect), 6,
            new[] { typeof(SecondEffect), typeof(SecondEffect), null! },
            () => new ControlEffect());

        LogOf("ApplyStatus").Should().Equal(
            $"ApplyStatus:{Caster}:{Target}:ControlEffect:6:{CombatDamageType.Force}",
            $"ApplyStatus:{Caster}:{Target}:SecondEffect:6:{CombatDamageType.Force}");
    }

    [Test]
    public void A_primary_status_repeated_in_the_additional_list_applies_once()
    {
        Service.ApplyHostileCombatImpact(Caster, Target, SkillType.Force, 0, CombatDamageType.Force, typeof(SecondEffect), 6,
            new[] { typeof(SecondEffect) });

        LogOf("ApplyStatus").Should().ContainSingle();
        LogOf("ImpactRiders").Should().Equal($"ImpactRiders:{Target}:0:True:False:True:Debuff:True:False");
    }

    [Test]
    public void A_valid_resistance_type_is_what_the_status_rolls_against()
    {
        Service.ApplyHostileCombatImpact(Caster, Target, SkillType.Force, 0, CombatDamageType.Force, typeof(SecondEffect), 6,
            statusEffectFactory: null!, statusResistanceType: ResistanceType.Mind);
        Service.ApplyHostileCombatImpact(Caster, Other, SkillType.Force, 0, CombatDamageType.Force, null!, 6,
            statusEffectFactory: () => new ControlEffect(), statusResistanceType: ResistanceType.Mind);

        LogOf("ApplyStatus").Should().Equal(
            $"ApplyStatus:{Caster}:{Target}:SecondEffect:6:{ResistanceType.Mind}",
            $"ApplyStatus:{Caster}:{Other}:ControlEffect:6:{ResistanceType.Mind}");
    }

    [Test]
    public void A_status_that_does_not_land_reports_nothing_applied()
    {
        Bench.StatusApplies = false;

        Service.ApplyHostileCombatImpact(Caster, Target, SkillType.Force, 0, CombatDamageType.Force, typeof(SecondEffect), 6,
            statusEffectFactory: null!);
        Service.ApplyHostileCombatImpact(Caster, Other, SkillType.Force, 0, CombatDamageType.Force, null!, 6,
            statusEffectFactory: () => new ControlEffect());
        Service.ApplyHostileCombatImpact(Caster, Other, SkillType.Force, 0, CombatDamageType.Force, null!, 6,
            statusEffectFactory: () => null!);

        LogOf("ImpactRiders").Should().OnlyContain(entry => entry.Contains(":False:False:True:None:"));
    }

    [Test]
    public void A_status_with_no_duration_is_not_applied_and_the_duration_adjustment_keeps_at_least_one()
    {
        Service.ApplyHostileCombatImpact(Caster, Target, SkillType.Force, 0, CombatDamageType.Force, typeof(SecondEffect), 0);
        LogOf("ApplyStatus").Should().BeEmpty();

        Service.StatusDurationAdjustment = 50;
        Service.ApplyHostileCombatImpact(Caster, Target, SkillType.Force, 0, CombatDamageType.Force, typeof(SecondEffect), 6);
        LogOf("ApplyStatus").Last().Should().Contain(":9:");

        Service.StatusDurationAdjustment = -200;
        Service.ApplyHostileCombatImpact(Caster, Target, SkillType.Force, 0, CombatDamageType.Force, typeof(SecondEffect), 6);
        LogOf("ApplyStatus").Last().Should().Contain(":1:");
    }

    [Test]
    public void The_status_categories_of_an_impact_combine_every_source()
    {
        Bench.TypeCategories = StatusEffectCategory.Debuff;

        Service.GetCombatImpactStatusCategories(null!, null!, null!).Should().Be(StatusEffectCategory.None);

        var categories = Service.GetCombatImpactStatusCategories(typeof(SecondEffect), new[] { typeof(SecondEffect) }, () => new ControlEffect());
        categories.HasFlag(StatusEffectCategory.Debuff).Should().BeTrue();
        categories.HasFlag(StatusEffectCategory.Control).Should().BeTrue();

        Service.GetCombatImpactStatusCategories(null!, null!, () => null!).Should().Be(StatusEffectCategory.None);
    }

    // ----- Targets -----

    [Test]
    public void A_single_target_impact_needs_a_valid_target_and_still_plays_its_animation()
    {
        Bench.Invalid.Add(Target);
        var ability = Hostile();
        ability.ImpactAnimationType = Animation.ThrowGrenade;

        Impact(ability, playImpactAnimation: true).Should().Be(0);

        LogOf("BattleTheme", "ModifyEnmity").Should().BeEmpty();
        LogOf("NamedRelease").Should().Equal($"NamedRelease:{Caster}");
    }

    [Test]
    public void A_friendly_target_is_skipped_and_the_impact_reports_no_target()
    {
        Bench.Hostile.Clear();

        Impact(Hostile()).Should().Be(0);

        Bench.Log.Should().Contain($"Nearby:{Caster}:Blast:no target:60");
    }

    [Test]
    public void An_area_impact_visits_only_hostiles_it_can_see_and_applies_its_area_visual_first()
    {
        Bench.InShape.AddRange(new[] { Target, Other, Caster });
        Bench.NoLineOfSight.Add(Other);
        var captured = new List<uint>();

        var dealt = Impact(Hostile(), isArea: true, areaVisual: VisualEffect.Vfx_Fnf_Sound_Burst, unscaled: true, beforeImpact: captured.Add);

        dealt.Should().Be(10);
        captured.Should().Equal(Target);
        Bench.Log.Should().ContainInOrder($"EffectAt:Vfx:{VisualEffect.Vfx_Fnf_Sound_Burst}", $"BattleTheme:{Target}:{Caster}");
        LogOf("ModifyEnmity").Should().ContainSingle();
        Service.GetActiveAbilityImpactSummary(Caster).IsAreaAbility.Should().BeTrue();
    }

    [Test]
    public void An_area_impact_centred_on_nothing_skips_its_visual()
    {
        Bench.Invalid.Add(Target);
        Service.BeginAbilityImpact(Caster, Hostile());

        Service.ApplyCombatImpact(Caster, Target, Bench.Nowhere(), SkillType.Force, 10, 0, null!, true,
            areaVisualEffect: VisualEffect.Vfx_Fnf_Sound_Burst);

        LogOf("EffectAt").Should().BeEmpty();
        Bench.Log.Should().Contain($"Nearby:{Caster}:Blast:no target:60");
    }

    [Test]
    public void An_area_marked_single_target_counts_as_an_area_when_it_says_so()
    {
        Service.BeginAbilityImpact(Caster, Hostile());

        Service.ApplyCombatImpact(Caster, Target, Bench.Nowhere(), SkillType.Force, 10, 0, null!, false, isAreaImpact: true);

        Service.GetActiveAbilityImpactSummary(Caster).IsAreaAbility.Should().BeTrue();
    }

    [Test]
    public void The_after_hit_hook_runs_once_per_successful_target()
    {
        var hits = new List<uint>();

        Impact(Hostile(), afterHit: hits.Add);

        hits.Should().Equal(Target);
    }

    // ----- Dark Force -----

    [Test]
    public void The_dark_force_conversion_runs_once_per_impact_and_keeps_the_caster_alive()
    {
        var ability = Hostile();
        ability.TriggersDarkForceConversion = true;
        Service.Stats[(Caster, StatType.DarkForceDamageHPPercentRestore)] = 10;
        Service.Stats[(Caster, StatType.DarkForceDamageFPRestore)] = 5;
        Service.Stats[(Caster, StatType.DarkForceDamageHPCostPercent)] = 50;
        Bench.Hp[Caster] = 30;
        Bench.MaxHp[Caster] = 100;
        Bench.Recording = true;
        Service.BeginAbilityImpact(Caster, ability);

        Service.ApplyHostileCombatImpact(Caster, Target, SkillType.Force, 25, CombatDamageType.Force);
        Service.ApplyHostileCombatImpact(Caster, Other, SkillType.Force, 25, CombatDamageType.Force);

        LogOf("DamageHealing").Should().Equal($"DamageHealing:{Caster}:25:10", $"DamageHealing:{Caster}:25:10");
        LogOf("RestoreFP").Should().ContainSingle().Which.Should().Be($"RestoreFP:{Caster}:5");
        LogOf("RecorderStat").Should().ContainSingle();
        LogOf("RecorderHitPointCost").Should().ContainSingle().Which.Should().Be($"RecorderHitPointCost:{Caster}:29");

        Bench.RunDeferred();
        Bench.Log.Should().Contain($"Effect:Damage:29:{DamageType.Force}:{Caster}");
    }

    [Test]
    public void A_low_target_replaces_the_hp_cost_of_the_conversion()
    {
        var ability = Hostile();
        ability.TriggersDarkForceConversion = true;
        Service.Stats[(Caster, StatType.DarkForceDamageHPCostPercent)] = 50;
        Service.Stats[(Caster, StatType.DarkForceDamageLowTargetHPThresholdPercent)] = 50;
        Service.Stats[(Caster, StatType.DarkForceDamageLowTargetHPCostPercent)] = 10;
        Bench.Hp[Target] = 20;
        Bench.Recording = true;
        Service.BeginAbilityImpact(Caster, ability);

        Service.ApplyHostileCombatImpact(Caster, Target, SkillType.Force, 25, CombatDamageType.Force);

        LogOf("RecorderHitPointCost").Should().Equal($"RecorderHitPointCost:{Caster}:10");
    }

    [Test]
    public void A_status_only_hit_of_a_dark_force_ability_still_converts()
    {
        var ability = Hostile();
        ability.TriggersDarkForceConversion = true;
        Service.Stats[(Caster, StatType.DarkForceDamageFPRestore)] = 5;
        Service.BeginAbilityImpact(Caster, ability);

        Service.ApplyHostileCombatImpact(Caster, Target, SkillType.Force, 0, CombatDamageType.Force, typeof(SecondEffect), 6);

        LogOf("RestoreFP").Should().ContainSingle();
    }

    [Test]
    public void An_ability_without_the_conversion_never_converts()
    {
        Service.Stats[(Caster, StatType.DarkForceDamageFPRestore)] = 5;
        Service.BeginAbilityImpact(Caster, Hostile());

        Service.ApplyHostileCombatImpact(Caster, Target, SkillType.Force, 25, CombatDamageType.Force, typeof(SecondEffect), 6);

        LogOf("RestoreFP", "DamageHealing").Should().BeEmpty();
    }

    [Test]
    public void The_public_dark_force_restoration_needs_damage_a_valid_caster_and_a_percent()
    {
        Service.ApplyDarkForceDamageRestoration(Caster, 0);
        Bench.Invalid.Add(Other);
        Service.ApplyDarkForceDamageRestoration(Other, 10);
        Service.ApplyDarkForceDamageRestoration(Caster, 10);
        LogOf("DamageHealing").Should().BeEmpty();

        Service.Stats[(Caster, StatType.DarkForceDamageHPPercentRestore)] = 10;
        Service.ApplyDarkForceDamageRestoration(Caster, 10);
        LogOf("DamageHealing").Should().Equal($"DamageHealing:{Caster}:10:10");
    }

    // ----- Animation -----

    [Test]
    public void A_queued_weapon_ability_plays_no_scripted_animation()
    {
        var ability = Hostile();
        ability.ActivationType = AbilityActivationType.Weapon;
        ability.ImpactAnimationType = Animation.ThrowGrenade;
        Service.BeginAbilityImpact(Caster, ability);
        Bench.Log.Clear();

        Service.PlayAbilityImpactAnimation(Caster);

        LogOf("NamedPlay", "NamedRelease", "AssignCommand").Should().BeEmpty();
    }

    [Test]
    public void An_ability_with_no_impact_animation_plays_nothing()
    {
        Service.BeginAbilityImpact(Caster, Hostile());
        Bench.Log.Clear();

        Service.PlayAbilityImpactAnimation(Caster);

        LogOf("NamedPlay", "NamedRelease", "AssignCommand").Should().BeEmpty();
    }

    [Test]
    public void An_authored_impact_clip_plays_on_a_player()
    {
        Bench.Players.Add(Caster);
        var ability = Hostile();
        ability.ImpactAnimationType = Animation.ThrowGrenade;
        ability.AuthoredImpactAnimation = new AnimationClip("clip", 1f);
        Service.BeginAbilityImpact(Caster, ability);
        Bench.Log.Clear();

        Service.PlayAbilityImpactAnimation(Caster);

        LogOf("NamedPlay", "NamedRelease", "AssignCommand").Should().Equal($"NamedPlay:{Caster}");
    }

    [TestCase(0.5f, "", "")]
    [TestCase(0f, "source", "replacement")]
    [TestCase(0f, "", "")]
    public void A_native_impact_animation_releases_the_named_clip_then_runs_as_a_command(float immediate, string source, string replacement)
    {
        var ability = Hostile();
        ability.ImpactAnimationType = Animation.ThrowGrenade;
        ability.ImmediateNativeImpactAnimationDuration = immediate;
        ability.ImpactAnimationSourceAnimationName = source;
        ability.ImpactAnimationReplacementAnimationName = replacement;
        Service.BeginAbilityImpact(Caster, ability);
        Bench.Log.Clear();

        Service.PlayAbilityImpactAnimation(Caster);

        LogOf("NamedRelease", "AssignCommand").Should().Equal($"NamedRelease:{Caster}", $"AssignCommand:{Caster}");
        Bench.RunDeferred();
    }

    [Test]
    public void An_explicit_impact_animation_wins_over_the_one_the_ability_declares()
    {
        Impact(Hostile(), impactAnimation: Animation.ThrowGrenade, playImpactAnimation: true);

        Bench.Log.Should().Contain($"NamedRelease:{Caster}");
    }
}
