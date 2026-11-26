using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.NWN.API.Testing;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;
using SWLOR.Game.Server.Composition;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The ability impact stay-part of <c>Combat</c>: the riders of a successful ability hit in the order they run, the attacking ally check and
/// the forget of a creature across the parts. The weapon riders, status-applied, hit riders, area impact and damage bonus parts are scripted at the
/// leaves, not at the part boundary, so each ride goes through the real part.
/// </summary>
[TestFixture]
public class CombatAbilityImpactServiceTests : CombatAbilityImpactTestBase
{
    protected override object PrivateTarget => _service;

    [Test]
    public void NoRiderRunsWithoutAValidActivatorTargetAndAbility()
    {
        Riders(null!, SkillType.Rifle);
        _invalid.Add(Gone);
        _service.ApplySuccessfulAbilityImpactRiders(Gone, Defender, Hostile(), SkillType.Rifle, CombatDamageType.Physical, 10, true, null!,
            Array.Empty<Type>(), true, true, StatusEffectCategory.Debuff);
        Riders(Hostile(), SkillType.Rifle, target: Gone);
        _log.Should().BeEmpty();
    }

    [Test]
    public void AnImpactThatAlreadyRanItsHitRidersOnThisTargetRunsNoSecondSet()
    {
        var weapon = new WeaponShotProfile(501, SkillType.Rifle, 10, CombatDamageType.Physical, 90, 90, AbilityType.Perception, "1");
        using var profile = new WeaponImpactProfile(Attacker, weapon, new WeaponImpactActivation());

        Riders(Hostile(), SkillType.Rifle);
        Lines("Record").Should().ContainSingle();
        _log.Clear();
        Riders(Hostile(), SkillType.Rifle);
        _log.Should().BeEmpty("the profile's activation already ran the hit riders on this target");
        Riders(Hostile(), SkillType.Rifle, target: Other);
        Lines("Record").Should().Equal($"Record {Attacker} {Other} {SkillType.Rifle}");
    }

    // Every rider of a successful hit is configured, so each one writes to the log; the log must keep the order the riders ran
    // in as static members of Combat: record, party buff, first hit count, source stacks, next auto-attack, same-target hits,
    // the status riders (the skill's, the perk category's, the guarded-hit exposed), the status-applied effects, the target
    // status effects, the near-target effects, the costly ability effects, the damage riders, the area pulse and the area hit
    // sequence.

    [Test]
    public void TheRidersOfASuccessfulHitRunInTheirOrder()
    {
        // An area ability that also counts as single-target, so Throwing's single-target status rider runs too.
        var ability = Hostile(SkillType.Throwing, area: true);
        ability.IsSingleTargetAbility = true;
        _areaImpact = true;
        _sequence = new AbilityImpactSequence();
        _nearby.AddRange(new[] { Other, Third });
        // Party buff.
        Stat(StatType.HostileAbilityPartyBuffDamagePercent, 5);
        Stat(StatType.HostileAbilityPartyBuffDurationSeconds, 30);
        Stat(StatType.HostileAbilityPartyBuffCooldownSeconds, 12);
        Stat(StatType.HostileAbilityPartyBuffNameStrRef, 1234);
        Stat(StatType.HostileAbilityPartyBuffIcon, (int)EffectIconType.DamageIncrease);
        // Next auto-attack without delay.
        Stat(StatType.HostileAbilityHitNextAutoAttackNoDelayAllSkills, 1);
        Stat(StatType.HostileAbilityHitNextAutoAttackNoDelayDurationSeconds, 6);
        // Same-target hits.
        Stat(StatType.SameTargetHostileAbilityHitCountRequired, 1);
        Stat(StatType.SameTargetHostileAbilityStaminaRestore, 2);
        // Throwing status rider, the perk category's Covering Claws and the guarded-hit Exposed.
        Stat(StatType.ThrowingDeadeyeMarkingToss, 1);
        _perkCategory = true;
        Stat(StatType.AbilityUsedPerkCategoryTargetEnmityToSourcePercentAdjustment, 25);
        Stat(StatType.AbilityUsedPerkCategoryTargetEnmityToSourceDurationSeconds, 9);
        _temporary[StatType.GuardedHitNextSkillAbilityStatusSkillType] = (int)SkillType.Throwing;
        _temporary[StatType.GuardedHitNextSkillAbilityExposedDurationSeconds] = 4;
        // Status-applied effects.
        Stat(StatType.StatusAppliedRequiredCategory, (int)StatusEffectCategory.Debuff);
        Stat(StatType.StatusAppliedNextSkillAbilitySkillType, (int)SkillType.Throwing);
        Stat(StatType.StatusAppliedNextSkillAbilityDamageBonus, 3);
        Stat(StatType.StatusAppliedNextSkillAbilityWindowSeconds, 8);
        // Target status effects.
        Stat(StatType.AbilityTargetStatusRequiredCategory, (int)StatusEffectCategory.Bleeding);
        Stat(StatType.AbilityTargetStatusPhysicalDefensePercentAdjustment, -10);
        Stat(StatType.AbilityTargetStatusDurationSeconds, 5);
        _categories.Add((Defender, StatusEffectCategory.Bleeding));
        // Near-target effects.
        Stat(StatType.RangedAbilityHitNearTargetRangeMeters, 5);
        Stat(StatType.RangedAbilityHitNearTargetDamageDealtPercentAdjustment, -10);
        Stat(StatType.RangedAbilityHitNearTargetDurationSeconds, 6);
        Stat(StatType.RangedAbilityHitNearTargetStatusEffectNameStrRef, 77);
        Stat(StatType.RangedAbilityHitNearTargetStatusEffectIcon, (int)EffectIconType.DamageDecrease);
        Stat(StatType.RangedAbilityHitNearTargetStatusEffectCleanseTypes, (int)StatusEffectCleanseType.Purify);
        Stat(StatType.RangedAbilityHitNearTargetStatusEffectResistanceType, (int)ResistanceType.Mind);
        // Costly ability effects.
        _costState = new CombatAbilityStaminaCostState { Cost = 20 };
        Stat(StatType.CostlyAbilityStatusMinimumStaminaCost, 10);
        Stat(StatType.CostlyAbilityExposedDurationSeconds, 7);
        // Damage riders: Foggy Mind drain and the area fragmentation, then Throwing's cluster storm.
        _statuses.Add((Defender, typeof(FoggyMindStatusEffect)));
        Stat(StatType.AbilityResourceDrainFoggyMindFP, 2);
        _permanent[(Attacker, StatType.AreaAbilityFragmentationDamage)] = 3;
        _permanent[(Attacker, StatType.AreaAbilityFragmentationDurationSeconds)] = 9;
        _permanent[(Attacker, StatType.AreaAbilityFragmentationPulseSeconds)] = 3;
        Stat(StatType.ThrowingBombardierClusterStormDamageBonus, 4);
        Stat(StatType.ThrowingBombardierClusterStormMaximumTargets, 1);
        // Area pulse.
        _sources[(Attacker, StatType.AreaAbilityPulseDamage)] = new[]
        {
            Source("pulse", (StatType.AreaAbilityPulseDamage, 6), (StatType.AreaAbilityPulseRadiusMeters, 4))
        };
        // Area hit sequence.
        Stat(StatType.AreaAbilityTargetHitSequenceSkillType, (int)SkillType.Throwing);
        Stat(StatType.AreaAbilityTargetHitSequenceCountRequired, 1);
        Stat(StatType.AreaAbilityTargetHitSequenceWindowSeconds, 6);
        Stat(StatType.AreaAbilityTargetHitSequenceExposedDurationSeconds, 5);

        Riders(ability, SkillType.Throwing, damage: 10, statusApplied: true, firstHit: true, applied: StatusEffectCategory.Debuff);

        _log.Where(line => !line.StartsWith("Stamina ", StringComparison.Ordinal)).Should().Equal(
            $"Record {Attacker} {Defender} {SkillType.Throwing}",
            $"Trigger {Attacker} {StatType.HostileAbilityPartyBuffDamagePercent} 12",
            $"Apply {Attacker}>{Attacker} HostileAbilityPartyBuffStatusEffect {new HostileAbilityPartyBuffStatusEffect(5, 0, 1234, EffectIconType.DamageIncrease).Name} 30",
            $"FirstHitCount {Attacker} {SkillType.Throwing}",
            $"SourceStack {Attacker} {Defender}",
            $"NoDelay {Attacker} 6",
            $"HitStamina {Attacker} {StatType.SameTargetHostileAbilityStaminaRestore} 2 Throwing",
            $"Apply {Attacker}>{Defender} MarkingTossStatusEffect 30 {CombatDamageType.Physical}",
            $"Apply {Attacker}>{Defender} CoveringClawsStatusEffect 9 {CombatDamageType.Physical}",
            $"Consume {Attacker} {StatType.GuardedHitNextSkillAbilityExposedDurationSeconds} {StatType.GuardedHitNextSkillAbilityExposedDurationSeconds}",
            $"Apply {Attacker}>{Defender} ExposedStatusEffect 4 {CombatDamageType.Physical}",
            $"NextSkill {Attacker} {SkillType.Throwing} 3 0 8 0 {StatusEffectCategory.None}",
            $"NextAttack {Attacker} 0 0",
            $"Replace {Defender} {StatType.PhysicalDefensePercentAdjustment} -10 5 {StatType.AbilityTargetStatusPhysicalDefensePercentAdjustment}",
            $"Apply {Attacker}>{Defender} DamageDealtAdjustmentStatusEffect {new DamageDealtAdjustmentStatusEffect(-10, 77, EffectIconType.DamageDecrease, StatusEffectCleanseType.Purify, ResistanceType.Mind).Name} DamageDecrease Purify Mind 6 {CombatDamageType.Physical}",
            $"Apply {Attacker}>{Defender} ExposedStatusEffect 7 {CombatDamageType.Physical}",
            $"LoseFP {Defender} {StatType.AbilityResourceDrainFoggyMindFP} 2",
            $"Apply {Attacker}>{Defender} FragmentationStatusEffect 9 {CombatDamageType.Physical}",
            $"Triggered {Attacker} {Other} 4 {CombatDamageType.Physical} {SkillType.Invalid} False null False",
            $"Pulse {Attacker}",
            $"Triggered {Attacker} {Other} 6 {CombatDamageType.Physical} {SkillType.Invalid} False null False",
            $"Triggered {Attacker} {Third} 6 {CombatDamageType.Physical} {SkillType.Invalid} False null False",
            $"Apply {Attacker}>{Defender} ExposedStatusEffect 5 {CombatDamageType.Physical}");
        Visuals().Should().Equal(VisualEffect.Vfx_Fnf_Howl_War_Cry);
    }

    [Test]
    public void ARiderSetWithoutHitProcsOrAFirstHitBonusSkipsTheSourceStacksAndTheFirstHitCount()
    {
        var suppressed = Hostile();
        suppressed.SuppressesSourceStatusStackRiders = true;

        Riders(Hostile(), SkillType.Rifle, allowsHitProcs: false);
        Riders(suppressed, SkillType.Rifle);
        Riders(new AbilityDetail { SkillType = SkillType.Rifle }, SkillType.Rifle, firstHit: true);
        Riders(Hostile(), SkillType.Rifle, firstHit: false);

        Lines("SourceStack").Should().ContainSingle("only the last hit allows procs from a hostile ability that keeps its stacks");
        Lines("FirstHitCount").Should().ContainSingle("the non-hostile hit still counts the first hit it was given");
        Lines("Record").Should().HaveCount(4);
    }

    [Test]
    public void ATargetIsAttackingAnAllyThroughItsAttackTargetOrItsEnmityTable()
    {
        _service.IsTargetAttackingAllyForStatusAppliedRiders(Attacker, Defender).Should().BeFalse("the activator has no rider that needs it");
        _log.Should().BeEmpty();

        Stat(StatType.StatusAppliedTargetAttackingAllyEnmityToSourcePercentAdjustment, 40);
        _attackTarget = Ally;
        _partyMembers.Add(Ally);
        _service.IsTargetAttackingAllyForStatusAppliedRiders(Attacker, Defender).Should().BeTrue();
        _log.Should().Equal($"InParty {Attacker} {Ally}");

        _log.Clear();
        _attackTarget = NWScript.OBJECT_INVALID;
        _highestEnmity = Ally;
        _service.IsAttackingAllyOf(Defender, Attacker).Should().BeTrue();
        _log.Should().Equal($"HighestEnmity {Defender}", $"InParty {Attacker} {Ally}");

        _highestEnmity = Attacker;
        _service.IsAttackingAllyOf(Defender, Attacker).Should().BeFalse("the creature itself is not its ally");
        _highestEnmity = NWScript.OBJECT_INVALID;
        _service.IsAttackingAllyOf(Defender, Attacker).Should().BeFalse("nothing is attacked");
        _attackTarget = Other;
        _service.IsAttackingAllyOf(Defender, Attacker).Should().BeFalse("the attacked creature is not in the party");
        _invalid.Add(Gone);
        _service.IsAttackingAllyOf(Gone, Attacker).Should().BeFalse();
        _service.IsAttackingAllyOf(Defender, Gone).Should().BeFalse();
    }

    [Test]
    public void ForgettingACreatureDropsEveryHitCountAndSequenceItIsPartOf()
    {
        SameTargetCounts()[(Attacker, Defender)] = 1;
        SameTargetCounts()[(Defender, Other)] = 2;
        SameTargetCounts()[(Other, Third)] = 3;
        _areaImpact = true;
        Stat(StatType.AreaAbilityTargetHitSequenceSkillType, (int)SkillType.Throwing);
        Stat(StatType.AreaAbilityTargetHitSequenceCountRequired, 3);
        Stat(StatType.AreaAbilityTargetHitSequenceWindowSeconds, 6);
        Stat(StatType.AreaAbilityTargetHitSequenceExposedDurationSeconds, 5);
        Stat(Other, StatType.AreaAbilityTargetHitSequenceSkillType, (int)SkillType.Throwing);
        Stat(Other, StatType.AreaAbilityTargetHitSequenceCountRequired, 3);
        Stat(Other, StatType.AreaAbilityTargetHitSequenceWindowSeconds, 6);
        Stat(Other, StatType.AreaAbilityTargetHitSequenceExposedDurationSeconds, 5);
        var ability = Hostile(SkillType.Throwing, area: true);
        _areaImpactPart.ApplyAreaAbilityTargetHitSequenceEffects(Attacker, Defender, ability, SkillType.Throwing);
        _areaImpactPart.ApplyAreaAbilityTargetHitSequenceEffects(Other, Third, ability, SkillType.Throwing);

        _service.ForgetImpactState(Defender);

        SameTargetCounts().Keys.Should().Equal((Other, Third));
        HitSequences().Keys.Cast<(uint, uint)>().Should().Equal((Other, Third));
        _service.ForgetImpactState(Third);
        SameTargetCounts().Should().BeEmpty();
        HitSequences().Count.Should().Be(0);
    }
}
