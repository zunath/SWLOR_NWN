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

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The ability impact stay-part of <c>Combat</c>: the riders of a successful ability hit in the order they run, the status riders by
/// weapon skill, the party buff, the status-applied effects, the Spear Disabler suppression riders, the status-applied stamina drain and
/// the forget of a creature across the parts. The hit riders, area impact and damage bonus parts are scripted at the leaves, not at the
/// part boundary, so each ride goes through the real part.
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
    public void ThePartyBuffNeedsADamagingOrStatusHitAndAFullConfiguration()
    {
        var ability = Hostile();
        _service.ApplyHostileAbilityPartyBuff(Attacker, ability, 10, false);
        Stat(StatType.HostileAbilityPartyBuffAccuracyPercent, 5);
        Stat(StatType.HostileAbilityPartyBuffDurationSeconds, 30);
        Stat(StatType.HostileAbilityPartyBuffCooldownSeconds, 12);
        Stat(StatType.HostileAbilityPartyBuffNameStrRef, 99);
        _service.ApplyHostileAbilityPartyBuff(Attacker, ability, 10, false);
        _log.Should().BeEmpty("the icon is missing");

        Stat(StatType.HostileAbilityPartyBuffIcon, (int)EffectIconType.AttackIncrease);
        _service.ApplyHostileAbilityPartyBuff(Attacker, ability, 0, false);
        _service.ApplyHostileAbilityPartyBuff(Attacker, new AbilityDetail(), 10, true);
        _service.ApplyHostileAbilityPartyBuff(Attacker, null!, 10, true);
        _log.Should().BeEmpty("a miss or a friendly ability buffs nobody");

        _service.ApplyHostileAbilityPartyBuff(Attacker, ability, 0, true);
        Lines("Apply").Should().ContainSingle().Which.Should().StartWith($"Apply {Attacker}>{Attacker} HostileAbilityPartyBuffStatusEffect");
    }

    [Test]
    public void ThePartyBuffReachesTheFriendlyTargetsOnceASequenceAndOnlyOffCooldown()
    {
        var ability = Hostile();
        Stat(StatType.HostileAbilityPartyBuffDamagePercent, 5);
        Stat(StatType.HostileAbilityPartyBuffDurationSeconds, 30);
        Stat(StatType.HostileAbilityPartyBuffCooldownSeconds, 12);
        Stat(StatType.HostileAbilityPartyBuffNameStrRef, 99);
        Stat(StatType.HostileAbilityPartyBuffIcon, (int)EffectIconType.DamageIncrease);
        _shape.AddRange(new[] { Attacker, Ally, Other });
        _hitPoints[Ally] = 20;
        _hitPoints[Other] = 20;
        _partyMembers.Add(Ally);
        _sequence = new AbilityImpactSequence();

        _service.ApplyHostileAbilityPartyBuff(Attacker, ability, 10, false);
        Lines("Apply").Select(line => line.Split(' ')[1]).Should().Equal($"{Attacker}>{Attacker}", $"{Attacker}>{Ally}");

        _log.Clear();
        _service.ApplyHostileAbilityPartyBuff(Attacker, ability, 10, false);
        _log.Should().BeEmpty("the sequence already buffed the party");

        _sequence = null;
        _blockedTriggers.Add(StatType.HostileAbilityPartyBuffDamagePercent);
        _service.ApplyHostileAbilityPartyBuff(Attacker, ability, 10, false);
        Lines("Apply").Should().BeEmpty("the buff is on cooldown");
        Lines("Trigger").Should().ContainSingle();
    }

    [Test]
    public void AHeavyVibrobladeDefenseAbilityAddsEnmityAndCrushingBlow()
    {
        Stat(StatType.HeavyVibrobladeDefenseAbilityEnmityBonus, 30);
        Stat(StatType.HeavyVibrobladeDefenseAbilityCrushingBlow, 1);
        _perkTypes = true;
        Riders(Hostile(SkillType.HeavyVibroblade), SkillType.HeavyVibroblade);
        Lines("Enmity").Should().BeEmpty("the ability is not a Defense trigger");

        _heavyTrigger = true;
        Riders(Hostile(SkillType.HeavyVibroblade), SkillType.HeavyVibroblade);
        _perkTypes = false;
        Stat(StatType.HeavyVibrobladeDefenseAbilityEnmityBonus, 0);
        Riders(Hostile(SkillType.HeavyVibroblade), SkillType.HeavyVibroblade);
        Lines("Enmity").Should().Equal($"Enmity {Attacker} {Defender} 30");
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Defender} CrushingBlowStatusEffect 30 {CombatDamageType.Physical}");
    }

    [Test]
    public void AForceImpactThatAppliedADarkStatusCollapsesWill()
    {
        Riders(Hostile(SkillType.Force), SkillType.Force, primary: typeof(NightmareField1StatusEffect));
        Stat(StatType.DarkManipulatorCollapseWill, 1);
        Riders(Hostile(SkillType.Force), SkillType.Force, primary: typeof(DazedStatusEffect));
        Lines("Apply").Should().BeEmpty();

        Riders(Hostile(SkillType.Force), SkillType.Force, primary: typeof(DazedStatusEffect), additional: new[] { typeof(EclipseOfResolve1StatusEffect) });
        Lines("Apply").Should().Equal(
            $"Apply {Attacker}>{Defender} ExposedStatusEffect 30 {CombatDamageType.Force}",
            $"Apply {Attacker}>{Defender} ForceErosionStatusEffect 30 {CombatDamageType.Force}");
    }

    [Test]
    public void AKatarImpactOnAPoisonedTargetSpreadsThePoison()
    {
        Riders(Hostile(SkillType.Katar), SkillType.Katar, damage: 0);
        _statuses.Add((Defender, typeof(PoisonStatusEffect)));
        Riders(Hostile(SkillType.Katar), SkillType.Katar, damage: 0);
        Lines("SpreadPoison").Should().Equal($"SpreadPoison {Attacker} {Defender}");
    }

    [Test]
    public void ALightsaberOffenseImpactSundersDisorientsDisruptsAndPurifiesInThatOrder()
    {
        Stat(StatType.LightsaberOffenseSunderDurationSeconds, 10);
        Stat(StatType.LightsaberOffenseDisorientedDurationSeconds, 6);
        Stat(StatType.LightsaberOffenseSingleTargetForceDisruptionDurationSeconds, 8);
        Stat(StatType.LightsaberOffensePurify, 1);
        Stat(StatType.LightsaberOffensePurifyCooldownSeconds, 30);
        var harmful = WithSource(new DazedStatusEffect(), Other);
        Effects(Attacker, new HostileAbilityPartyBuffStatusEffect(5, 0, 1, EffectIconType.DamageIncrease), harmful);

        Riders(new AbilityDetail { SkillType = SkillType.Lightsaber }, SkillType.Lightsaber);
        Lines("Apply").Should().BeEmpty("a friendly ability carries no offense riders");

        Riders(Hostile(SkillType.Lightsaber), SkillType.Lightsaber);
        _log.Where(line => line.StartsWith("Apply", StringComparison.Ordinal) || line.StartsWith("Remove", StringComparison.Ordinal) || line.StartsWith("Trigger", StringComparison.Ordinal))
            .Should().Equal(
                $"Apply {Attacker}>{Defender} SunderStatusEffect -15 10 {CombatDamageType.Physical}",
                $"Apply {Attacker}>{Defender} DisorientedStatusEffect 6 {ResistanceType.Mind}",
                $"Apply {Attacker}>{Defender} ForceDisruptionStatusEffect 8 {CombatDamageType.Force}",
                $"Trigger {Attacker} {StatType.LightsaberOffensePurify} 30",
                $"Remove {Attacker} DazedStatusEffect {Other} False",
                $"Apply {Attacker}>{Defender} DazedStatusEffect 30 {CombatDamageType.Force}");
    }

    [Test]
    public void ALightsaberSunderIsNotAppliedOverAnEqualOrStrongerSunder()
    {
        Stat(StatType.LightsaberOffenseSunderDurationSeconds, 10);
        Effects(Defender, new SunderStatusEffect(15));
        Riders(Hostile(SkillType.Lightsaber, area: true), SkillType.Lightsaber);
        Effects(Defender, new SunderStatusEffect(10));
        Riders(Hostile(SkillType.Lightsaber, area: true), SkillType.Lightsaber);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Defender} SunderStatusEffect -15 10 {CombatDamageType.Physical}");
        ((bool)Private("HasSunderPenaltyAtLeast", Other, 1)!).Should().BeFalse("a creature with no Sunder has no penalty");
    }

    [Test]
    public void APurifyNeedsItsTriggerAndAHarmfulStatusToTransfer()
    {
        Stat(StatType.LightsaberOffensePurify, 1);
        Effects(Attacker, new HostileAbilityPartyBuffStatusEffect(5, 0, 1, EffectIconType.DamageIncrease));
        Private("ApplyLightsaberOffensePurify", Attacker, Defender);
        Lines("Remove").Should().BeEmpty("a buff is not a debuff, a control or a bleed");

        Effects(Attacker, new DazedStatusEffect());
        _blockedTriggers.Add(StatType.LightsaberOffensePurify);
        Private("ApplyLightsaberOffensePurify", Attacker, Defender);
        Lines("Remove").Should().BeEmpty("Purify is on cooldown");

        Stat(StatType.LightsaberOffensePurify, 0);
        _log.Clear();
        Private("ApplyLightsaberOffensePurify", Attacker, Defender);
        _log.Should().BeEmpty();
    }

    [Test]
    public void APistolSkirmisherDisorientsOnlyFromAnAreaOrCloseAbility()
    {
        Stat(StatType.PistolSkirmisherDisorientedDurationSeconds, 5);
        Riders(new AbilityDetail { SkillType = SkillType.Pistol }, SkillType.Pistol, damage: 0);
        Riders(Hostile(SkillType.Pistol, maxRange: 10f), SkillType.Pistol, damage: 0);
        Riders(Hostile(SkillType.Pistol, maxRange: 5f), SkillType.Pistol, damage: 0);
        Riders(Hostile(SkillType.Pistol, area: true, maxRange: 10f), SkillType.Pistol, damage: 0);
        Lines("Apply").Should().Equal(
            $"Apply {Attacker}>{Defender} DisorientedStatusEffect 5 {ResistanceType.Mind}",
            $"Apply {Attacker}>{Defender} DisorientedStatusEffect 5 {ResistanceType.Mind}");
    }

    [Test]
    public void ARiflePacificationImpactNeutralizesOverwatchesAndPinsByRank()
    {
        Stat(StatType.RiflePacificationNeutralizingShot, 1);
        Stat(StatType.RiflePacificationOverwatch, 1);
        Stat(StatType.RiflePacificationPinningFireRank, 2);
        Riders(Hostile(SkillType.Rifle), SkillType.Rifle, damage: 0);
        _log.Where(line => line.StartsWith("Apply", StringComparison.Ordinal) || line.StartsWith("RemoveBeneficial", StringComparison.Ordinal))
            .Should().Equal(
                $"RemoveBeneficial {Defender} False",
                $"Apply {Attacker}>{Defender} DisorientedStatusEffect 30 {ResistanceType.Mind}",
                $"Apply {Attacker}>{Defender} FoggyMindStatusEffect foggy 30 {ResistanceType.Mind}",
                $"Apply {Attacker}>{Defender} KnockdownStatusEffect 6 {ResistanceType.Trauma}");
        _engine.CallsTo("AssignCommand").Select(call => (uint)call.Arguments[0]!).Should().Equal(Defender);

        _log.Clear();
        Stat(StatType.RiflePacificationNeutralizingShot, 0);
        Stat(StatType.RiflePacificationOverwatch, 0);
        Stat(StatType.RiflePacificationPinningFireRank, 1);
        Riders(Hostile(SkillType.Rifle), SkillType.Rifle, damage: 0);
        Stat(StatType.RiflePacificationPinningFireRank, 0);
        Riders(Hostile(SkillType.Rifle), SkillType.Rifle, damage: 0);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Defender} DazedStatusEffect 30 {ResistanceType.Mind}");
    }

    [Test]
    public void ASaberstaffImpactRunsTheConduitThenTheTempestRider()
    {
        Stat(StatType.SaberstaffConduitAreaConduitFlare, 1);
        Stat(StatType.SaberstaffConduitFlareForceDisruptionDurationSeconds, 4);
        Stat(StatType.SaberstaffTempestForceGyre, 1);
        Stat(StatType.SaberstaffTempestForceGyreDurationSeconds, 6);
        Riders(Hostile(SkillType.Saberstaff, area: true), SkillType.Saberstaff, damage: 0);
        Riders(Hostile(SkillType.Saberstaff), SkillType.Saberstaff, damage: 0);
        Riders(new AbilityDetail { SkillType = SkillType.Saberstaff, IsAreaAbility = true }, SkillType.Saberstaff, damage: 0);
        Lines("Apply").Should().Equal(
            $"Apply {Attacker}>{Defender} ForceDisruptionStatusEffect 4 {CombatDamageType.Force}",
            $"Apply {Attacker}>{Defender} ForceErosionStatusEffect 6 {CombatDamageType.Force}",
            $"Apply {Attacker}>{Defender} ForceDisruptionStatusEffect 4 {CombatDamageType.Force}",
            $"Apply {Attacker}>{Defender} ForceErosionStatusEffect 6 {CombatDamageType.Force}");

        _log.Clear();
        Stat(StatType.SaberstaffConduitFlareForceDisruptionDurationSeconds, 0);
        Stat(StatType.SaberstaffTempestForceGyreDurationSeconds, 0);
        Riders(Hostile(SkillType.Saberstaff, area: true), SkillType.Saberstaff, damage: 0);
        Stat(StatType.SaberstaffConduitAreaConduitFlare, 0);
        Stat(StatType.SaberstaffTempestForceGyre, 0);
        Riders(Hostile(SkillType.Saberstaff, area: true), SkillType.Saberstaff, damage: 0);
        Lines("Apply").Should().BeEmpty();
    }

    [Test]
    public void ASpearImpactRunsTheDamageThenTheDisablerRiders()
    {
        Stat(StatType.SpearDamageBreachStrike, 1);
        Stat(StatType.SpearDamageCripplingDefense, 1);
        Stat(StatType.SpearDisablerForceNullification, 1);
        Stat(StatType.SpearDisablerForcebane, 1);
        Stat(StatType.SpearDisablerFractureStrike, 1);
        Riders(Hostile(SkillType.Spear, area: true), SkillType.Spear, damage: 0, primary: typeof(ForceDisruptionStatusEffect));
        Lines("Apply").Should().Equal(
            $"Apply {Attacker}>{Defender} BreachStatusEffect 30 {CombatDamageType.Physical}",
            $"Apply {Attacker}>{Defender} CrippledDefenseStatusEffect 45 {CombatDamageType.Physical}",
            $"Apply {Attacker}>{Defender} ForceDisruptionStatusEffect 30 {CombatDamageType.Force}",
            $"Apply {Attacker}>{Defender} ForcebaneStatusEffect 45 {CombatDamageType.Force}",
            $"Apply {Attacker}>{Defender} FracturedFocusStatusEffect 30 {CombatDamageType.Force}");

        _log.Clear();
        Riders(Hostile(SkillType.Spear), SkillType.Spear, damage: 0, primary: typeof(ForceSuppressionStatusEffect));
        Lines("Apply").Should().Equal(
            $"Apply {Attacker}>{Defender} BreachStatusEffect 30 {CombatDamageType.Physical}",
            $"Apply {Attacker}>{Defender} ForcebaneStatusEffect 45 {CombatDamageType.Force}");

        _log.Clear();
        _stats.Clear();
        Riders(Hostile(SkillType.Spear, area: true), SkillType.Spear, damage: 0, primary: typeof(ForceDisruptionStatusEffect));
        Lines("Apply").Should().BeEmpty();
    }

    [Test]
    public void TheSpearDisablerSuppressionRidersNeedValidCreaturesAndRunFractureBeforeForcebane()
    {
        Stat(StatType.SpearDisablerFractureStrike, 1);
        Stat(StatType.SpearDisablerForcebane, 1);
        _invalid.Add(Gone);
        _service.ApplySpearDisablerSuppressionRiders(Gone, Defender);
        _service.ApplySpearDisablerSuppressionRiders(Attacker, Gone);
        _log.Should().BeEmpty();

        _service.ApplySpearDisablerSuppressionRiders(Attacker, Defender);
        Stat(StatType.SpearDisablerFractureStrike, 0);
        Stat(StatType.SpearDisablerForcebane, 0);
        _service.ApplySpearDisablerSuppressionRiders(Attacker, Defender);
        _log.Should().Equal(
            $"Apply {Attacker}>{Defender} FracturedFocusStatusEffect 30 {CombatDamageType.Force}",
            $"Apply {Attacker}>{Defender} ForcebaneStatusEffect 45 {CombatDamageType.Force}");
    }

    [Test]
    public void AStaffThrowingTwinBladeAndVibroknifeImpactApplyTheirStatuses()
    {
        Stat(StatType.StaffCrusherFinisherDazedDurationSeconds, 3);
        Riders(Hostile(SkillType.Staff), SkillType.Staff, damage: 0);
        Stat(StatType.ThrowingDeadeyeMarkingToss, 1);
        Riders(Hostile(SkillType.Throwing, area: true), SkillType.Throwing, damage: 0);
        Riders(Hostile(SkillType.Throwing), SkillType.Throwing, damage: 0);
        _reversalCut = true;
        _temporary[StatType.TwinBladeDuelistReversalCutDazedDurationSeconds] = 4;
        Riders(Hostile(SkillType.TwinBlade), SkillType.TwinBlade, damage: 0);
        Riders(Hostile(SkillType.TwinBlade), SkillType.TwinBlade, damage: 0);
        _reversalCut = false;
        Riders(Hostile(SkillType.TwinBlade), SkillType.TwinBlade, damage: 0);
        Stat(StatType.VibroknifeShadowMarkedForDeath, 1);
        Riders(Hostile(SkillType.Vibroknife, area: true), SkillType.Vibroknife, damage: 0);
        Riders(Hostile(SkillType.Vibroknife), SkillType.Vibroknife, damage: 0);

        Lines("Apply").Should().Equal(
            $"Apply {Attacker}>{Defender} DazedStatusEffect 3 {ResistanceType.Mind}",
            $"Apply {Attacker}>{Defender} MarkingTossStatusEffect 30 {CombatDamageType.Physical}",
            $"Apply {Attacker}>{Defender} DazedStatusEffect 4 {ResistanceType.Mind}",
            $"Apply {Attacker}>{Defender} MarkedForDeathStatusEffect 30 {CombatDamageType.Physical}");
        Lines("Consume").Should().Equal(
            $"Consume {Attacker} {StatType.TwinBladeDuelistReversalCutDazedDurationSeconds} {StatType.TwinBladeDuelistReversalCut}",
            $"Consume {Attacker} {StatType.TwinBladeDuelistReversalCutDazedDurationSeconds} {StatType.TwinBladeDuelistReversalCut}");
    }

    [Test]
    public void AVibroknifeSaboteurImpactCoatsAndSapsADebuffedTargetOffCooldown()
    {
        Stat(StatType.VibroknifeSaboteurToxicCoatingRank, 1);
        Stat(StatType.VibroknifeSaboteurSapVitalityRank, 3);
        Riders(new AbilityDetail { SkillType = SkillType.Vibroknife }, SkillType.Vibroknife, damage: 0, primary: typeof(DazedStatusEffect));
        _log.Where(line => !line.StartsWith("Record", StringComparison.Ordinal)).Should().Equal(
            $"Apply {Attacker}>{Defender} ToxinStatusEffect 30 {CombatDamageType.Poison}",
            $"Trigger {Defender} {StatType.VibroknifeSaboteurSapVitalityRank} 6",
            $"LoseStamina {Defender} {StatType.VibroknifeSaboteurSapVitalityRank} 3");

        _log.Clear();
        _blockedTriggers.Add(StatType.VibroknifeSaboteurSapVitalityRank);
        Riders(new AbilityDetail { SkillType = SkillType.Vibroknife }, SkillType.Vibroknife, damage: 0, primary: typeof(DazedStatusEffect));
        Riders(new AbilityDetail { SkillType = SkillType.Vibroknife }, SkillType.Vibroknife, damage: 0, primary: typeof(ExposedStatusEffect));
        Stat(StatType.VibroknifeSaboteurToxicCoatingRank, 0);
        Stat(StatType.VibroknifeSaboteurSapVitalityRank, 0);
        Riders(new AbilityDetail { SkillType = SkillType.Vibroknife }, SkillType.Vibroknife, damage: 0, primary: typeof(DazedStatusEffect));
        Lines("LoseStamina").Should().BeEmpty();
        Lines("Trigger").Should().ContainSingle("the second application did not apply a debuff, and the third has no rank");
    }

    [Test]
    public void ThePerkCategoryEnmityStatusNeedsAMatchingAbilityAndAFullConfiguration()
    {
        Stat(StatType.AbilityUsedPerkCategoryTargetEnmityToSourcePercentAdjustment, 25);
        Riders(new AbilityDetail(), SkillType.Invalid, damage: 0);
        _perkCategory = true;
        Riders(new AbilityDetail(), SkillType.Invalid, damage: 0);
        Lines("Apply").Should().BeEmpty("no duration");

        Stat(StatType.AbilityUsedPerkCategoryTargetEnmityToSourceDurationSeconds, 9);
        Riders(new AbilityDetail(), SkillType.Invalid, damage: 0);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Defender} CoveringClawsStatusEffect 9 {CombatDamageType.Physical}");
    }

    [Test]
    public void TheGuardedHitExposedStatusIsConsumedOnlyForItsSkillAndOnlyWithADuration()
    {
        _temporary[StatType.GuardedHitNextSkillAbilityStatusSkillType] = (int)SkillType.Spear;
        Riders(new AbilityDetail(), SkillType.Rifle, damage: 0);
        Lines("Consume").Should().BeEmpty();

        Riders(new AbilityDetail(), SkillType.Spear, damage: 0);
        Lines("Consume").Should().ContainSingle();
        Lines("Apply").Should().BeEmpty("the stored duration was zero");

        _temporary[StatType.GuardedHitNextSkillAbilityStatusSkillType] = (int)SkillType.Spear;
        _temporary[StatType.GuardedHitNextSkillAbilityExposedDurationSeconds] = 6;
        Riders(new AbilityDetail(), SkillType.Spear, damage: 0);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Defender} ExposedStatusEffect 6 {CombatDamageType.Physical}");
    }

    [Test]
    public void TheStatusAppliedEffectsNeedTheRequiredCategory()
    {
        Stat(StatType.StatusAppliedNextSkillAbilityDamageBonus, 3);
        Riders(new AbilityDetail(), SkillType.Invalid, damage: 0, applied: StatusEffectCategory.Debuff);
        Stat(StatType.StatusAppliedRequiredCategory, (int)StatusEffectCategory.Control);
        Riders(new AbilityDetail(), SkillType.Invalid, damage: 0, applied: StatusEffectCategory.Debuff);
        Lines("NextSkill").Should().BeEmpty();

        Riders(new AbilityDetail(), SkillType.Invalid, damage: 0, applied: StatusEffectCategory.Debuff | StatusEffectCategory.Control);
        Lines("NextSkill").Should().ContainSingle();
    }

    [Test]
    public void TheStatusAppliedEffectsGrantTheNextSkillAndAttackBonusesShowTheCueAndRunTheSelfTargetAndAllyStagesInOrder()
    {
        Stat(StatType.StatusAppliedRequiredCategory, (int)StatusEffectCategory.Debuff);
        Stat(StatType.StatusAppliedNextSkillAbilitySkillType, (int)SkillType.Katar);
        Stat(StatType.StatusAppliedNextSkillAbilityDamageBonus, 3);
        Stat(StatType.StatusAppliedNextSkillAbilityCriticalRatePercentAdjustment, 5);
        Stat(StatType.StatusAppliedNextSkillAbilityWindowSeconds, 8);
        Stat(StatType.StatusAppliedNextAttackDamageBonus, 6);
        Stat(StatType.StatusAppliedNextAttackWindowSeconds, 10);
        _temporary[StatType.NextAttackStatusAppliedDMGBonus] = 2;
        Stat(StatType.StatusAppliedSelfDurationSeconds, 12);
        Stat(StatType.StatusAppliedSelfAttackDeflection, 1);
        Stat(StatType.StatusAppliedSelfDefensePercentAdjustment, 5);
        Stat(StatType.StatusAppliedSelfEvasionPercentAdjustment, 6);
        Stat(StatType.StatusAppliedSelfForceAttackPercentAdjustment, 7);
        Stat(StatType.StatusAppliedSelfHastePercentAdjustment, 8);
        Stat(StatType.StatusAppliedSelfEnmityPercentAdjustment, 9);
        Stat(StatType.StatusAppliedSelfStaminaRestore, 4);
        Stat(StatType.StatusAppliedTargetDurationSeconds, 5);
        Stat(StatType.StatusAppliedTargetPhysicalDefensePercentAdjustment, -10);
        Stat(StatType.StatusAppliedTargetAccuracyPercentAdjustment, -15);
        Stat(StatType.StatusAppliedTargetAttackingAllyEnmityToSourcePercentAdjustment, 40);
        Stat(StatType.StatusAppliedTargetAttackingAllyEnmityDurationSeconds, 20);

        Riders(new AbilityDetail(), SkillType.Invalid, damage: 0, applied: StatusEffectCategory.Debuff, attackingAlly: true);

        var deflection = StatRules.GetGrantedDeflectionStatType(StatType.StatusAppliedSelfAttackDeflection);
        _log.Where(line => !line.StartsWith("Record", StringComparison.Ordinal)).Should().Equal(
            $"NextSkill {Attacker} {SkillType.Katar} 3 5 8 0 {StatusEffectCategory.None}",
            $"Cue {Attacker} True 6 {ReadyCue.Damage("Charged Blows", 6)}",
            $"NextAttack {Attacker} 6 10",
            $"Replace {Attacker} {deflection} 1 12 {StatType.StatusAppliedSelfAttackDeflection}",
            $"Replace {Attacker} {StatType.PhysicalDefensePercentAdjustment} 5 12 {StatType.StatusAppliedSelfDefensePercentAdjustment}",
            $"Replace {Attacker} {StatType.EvasionPercentAdjustment} 6 12 {StatType.StatusAppliedSelfEvasionPercentAdjustment}",
            $"Replace {Attacker} {StatType.ForceAttackPercentAdjustment} 7 12 {StatType.StatusAppliedSelfForceAttackPercentAdjustment}",
            $"Replace {Attacker} {StatType.AttackDelayReductionPercent} 8 12 {StatType.StatusAppliedSelfHastePercentAdjustment}",
            $"Replace {Attacker} {StatType.EnmityPercentAdjustment} 9 12 {StatType.StatusAppliedSelfEnmityPercentAdjustment}",
            $"HitStamina {Attacker} {StatType.StatusAppliedSelfStaminaRestore} 4",
            $"Replace {Defender} {StatType.PhysicalDefensePercentAdjustment} -10 5 {StatType.StatusAppliedTargetPhysicalDefensePercentAdjustment}",
            $"Replace {Defender} {StatType.AccuracyPercentAdjustment} -15 5 {StatType.StatusAppliedTargetAccuracyPercentAdjustment}",
            $"Apply {Attacker}>{Defender} ChallengeEnmityStatusEffect 40 20");
    }

    [Test]
    public void TheReadyCueShowsOnlyWithANextAttackWindowAndTheSelfAndTargetStagesNeedTheirDurations()
    {
        Stat(StatType.StatusAppliedRequiredCategory, (int)StatusEffectCategory.Debuff);
        Stat(StatType.StatusAppliedNextAttackDamageBonus, 6);
        Stat(StatType.StatusAppliedSelfDefensePercentAdjustment, 5);
        Stat(StatType.StatusAppliedSelfStaminaRestore, 4);
        Stat(StatType.StatusAppliedTargetPhysicalDefensePercentAdjustment, -10);
        Stat(StatType.StatusAppliedSelfDurationSeconds, 12);
        Stat(StatType.StatusAppliedNextAttackWindowSeconds, 10);

        Riders(new AbilityDetail(), SkillType.Invalid, damage: 0, applied: StatusEffectCategory.Debuff);
        Lines("Cue").Should().Equal(new[] { $"Cue {Attacker} False 6 {ReadyCue.Damage("Charged Blows", 6)}" }, "nothing was armed before");
        Lines("Replace").Should().Equal(
            new[] { $"Replace {Attacker} {StatType.PhysicalDefensePercentAdjustment} 5 12 {StatType.StatusAppliedSelfDefensePercentAdjustment}" },
            "the target stage has no duration and the zero self stats are skipped");

        _log.Clear();
        Stat(StatType.StatusAppliedNextAttackWindowSeconds, 0);
        Stat(StatType.StatusAppliedSelfDurationSeconds, 0);
        Stat(StatType.StatusAppliedSelfStaminaRestore, 0);
        Stat(StatType.StatusAppliedTargetDurationSeconds, 5);
        _invalid.Add(Gone);
        Riders(new AbilityDetail(), SkillType.Invalid, damage: 0, applied: StatusEffectCategory.Debuff, target: Defender);
        Lines("Cue").Should().BeEmpty();
        Lines("Replace").Should().Equal(
            $"Replace {Defender} {StatType.PhysicalDefensePercentAdjustment} -10 5 {StatType.StatusAppliedTargetPhysicalDefensePercentAdjustment}");
        Lines("NextAttack").Should().Equal(new[] { $"NextAttack {Attacker} 6 0" }, "the grant still runs and keeps its own window check");

        _log.Clear();
        Private("ApplyStatusAppliedTargetEffects", Attacker, Gone);
        Private("ReplaceTemporaryStat", Attacker, StatType.Invalid, 5, 5, StatType.Invalid);
        Private("ReplaceTemporaryStat", Attacker, StatType.EvasionPercentAdjustment, 5, 0, StatType.Invalid);
        _log.Should().BeEmpty();
    }

    [Test]
    public void TheAttackingAllyEnmityDoesNotReplaceAStrongerChallengeFromTheSameSource()
    {
        Stat(StatType.StatusAppliedTargetAttackingAllyEnmityToSourcePercentAdjustment, 40);
        Private("ApplyStatusAppliedTargetAttackingAllyEnmity", Attacker, Defender);
        Lines("Apply").Should().BeEmpty("no duration");

        Stat(StatType.StatusAppliedTargetAttackingAllyEnmityDurationSeconds, 20);
        _invalid.Add(Gone);
        Private("ApplyStatusAppliedTargetAttackingAllyEnmity", Attacker, Gone);
        Effects(Defender,
            WithSource(new ChallengeEnmityStatusEffect(60), Attacker),
            WithSource(new SunderStatusEffect(5), Attacker));
        Private("ApplyStatusAppliedTargetAttackingAllyEnmity", Attacker, Defender);
        Lines("Apply").Should().BeEmpty("the activator's own Challenge is stronger");

        Effects(Defender, WithSource(new ChallengeEnmityStatusEffect(60), Other), WithSource(new ChallengeEnmityStatusEffect(30), Attacker));
        Private("ApplyStatusAppliedTargetAttackingAllyEnmity", Attacker, Defender);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Defender} ChallengeEnmityStatusEffect 40 20");
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
    public void TheTargetStatusEffectNeedsAHostileAbilityAValidTargetAndTheRequiredCategory()
    {
        Stat(StatType.AbilityTargetStatusPhysicalDefensePercentAdjustment, -10);
        Stat(StatType.AbilityTargetStatusDurationSeconds, 5);
        Private("ApplyAbilityTargetStatusEffects", Attacker, Defender, Hostile());
        Stat(StatType.AbilityTargetStatusRequiredCategory, (int)StatusEffectCategory.Bleeding);
        Private("ApplyAbilityTargetStatusEffects", Attacker, Defender, Hostile());
        _categories.Add((Defender, StatusEffectCategory.Bleeding));
        Private("ApplyAbilityTargetStatusEffects", Attacker, Defender, new AbilityDetail());
        Private("ApplyAbilityTargetStatusEffects", Attacker, Defender, null!);
        _invalid.Add(Defender);
        Private("ApplyAbilityTargetStatusEffects", Attacker, Defender, Hostile());
        _log.Should().BeEmpty();

        _invalid.Clear();
        Private("ApplyAbilityTargetStatusEffects", Attacker, Defender, Hostile());
        _log.Should().Equal($"Replace {Defender} {StatType.PhysicalDefensePercentAdjustment} -10 5 {StatType.AbilityTargetStatusPhysicalDefensePercentAdjustment}");
    }

    [Test]
    public void TheStatusAppliedStaminaDrainShowsWhatTheTargetActuallyLost()
    {
        Stat(StatType.StatusAppliedTargetStaminaDrainRequiredCategory, (int)StatusEffectCategory.Debuff);
        Stat(StatType.StatusAppliedTargetStaminaDrain, 6);
        Stat(StatType.StatusAppliedTargetStaminaDrainCooldownSeconds, 12);
        _stamina[Defender] = 4;
        _staminaDrained = 4;

        _service.ApplyStatusAppliedTargetStaminaDrain(Attacker, Defender, StatusEffectCategory.Debuff | StatusEffectCategory.Control);

        _log.Should().Equal(
            $"Trigger {Attacker} {StatType.StatusAppliedTargetStaminaDrain} 12",
            $"Stamina {Defender}",
            $"Attribute {Defender} {StatType.StatusAppliedTargetStaminaDrain}",
            $"ReduceStamina {Defender} 6",
            $"Stamina {Defender}");
        FloatingText().Should().Equal($"{Defender} {SWLOR.Game.Server.Domain.Foundation.ColorToken.Combat("-4 STM")}");
    }

    [Test]
    public void TheStatusAppliedStaminaDrainNeedsAnotherValidCreatureTheCategoryAndItsTrigger()
    {
        _invalid.Add(Gone);
        _service.ApplyStatusAppliedTargetStaminaDrain(Gone, Defender, StatusEffectCategory.Debuff);
        _service.ApplyStatusAppliedTargetStaminaDrain(Attacker, Gone, StatusEffectCategory.Debuff);
        _service.ApplyStatusAppliedTargetStaminaDrain(Attacker, Attacker, StatusEffectCategory.Debuff);
        _service.ApplyStatusAppliedTargetStaminaDrain(Attacker, Defender, StatusEffectCategory.Debuff);
        Stat(StatType.StatusAppliedTargetStaminaDrainRequiredCategory, (int)StatusEffectCategory.Debuff);
        Stat(StatType.StatusAppliedTargetStaminaDrain, 6);
        _service.ApplyStatusAppliedTargetStaminaDrain(Attacker, Defender, StatusEffectCategory.Debuff);
        Stat(StatType.StatusAppliedTargetStaminaDrainCooldownSeconds, 12);
        _service.ApplyStatusAppliedTargetStaminaDrain(Attacker, Defender, StatusEffectCategory.Control);
        _log.Should().BeEmpty();

        _blockedTriggers.Add(StatType.StatusAppliedTargetStaminaDrain);
        _service.ApplyStatusAppliedTargetStaminaDrain(Attacker, Defender, StatusEffectCategory.Debuff);
        _log.Should().Equal($"Trigger {Attacker} {StatType.StatusAppliedTargetStaminaDrain} 12");
        FloatingText().Should().BeEmpty();
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
        Private("ApplyAreaAbilityTargetHitSequenceEffects", Attacker, Defender, ability, SkillType.Throwing);
        Private("ApplyAreaAbilityTargetHitSequenceEffects", Other, Third, ability, SkillType.Throwing);

        _service.ForgetImpactState(Defender);

        SameTargetCounts().Keys.Should().Equal((Other, Third));
        HitSequences().Keys.Cast<(uint, uint)>().Should().Equal((Other, Third));
        _service.ForgetImpactState(Third);
        SameTargetCounts().Should().BeEmpty();
        HitSequences().Count.Should().Be(0);
    }
}
