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
/// The ability area impact part of <c>Combat</c>: the damage riders (Foggy Mind drain, bleed refresh, spread and splash, fragmentation, the
/// skill riders), the area pulse, the fragmentation adjustment and the area ability hit sequence.
/// </summary>
[TestFixture]
public class CombatAbilityAreaImpactServiceTests : CombatAbilityImpactTestBase
{
    protected override object PrivateTarget => _areaImpactPart;

    [Test]
    public void ANonDamagingImpactRunsNoDamageRider()
    {
        _statuses.Add((Defender, typeof(FoggyMindStatusEffect)));
        Stat(StatType.AbilityResourceDrainFoggyMindFP, 2);
        Stat(StatType.KatarVenomCurrentSecondStrikeDamageBonus, 5);
        Private("ApplyAbilityDamageRiders", Attacker, Defender, Hostile(SkillType.Katar), SkillType.Katar, CombatDamageType.Physical, 0);
        _log.Should().BeEmpty();
    }

    [Test]
    public void TheFoggyMindDrainNeedsAHostileAbilityAndAFoggyTarget()
    {
        Stat(StatType.AbilityResourceDrainFoggyMindFP, 2);
        Stat(StatType.AbilityResourceDrainFoggyMindStamina, 3);
        Private("ApplyFoggyMindResourceDrain", Attacker, Defender, Hostile());
        _statuses.Add((Defender, typeof(FoggyMindStatusEffect)));
        Private("ApplyFoggyMindResourceDrain", Attacker, Defender, new AbilityDetail());
        Private("ApplyFoggyMindResourceDrain", Attacker, Defender, null!);
        _invalid.Add(Defender);
        Private("ApplyFoggyMindResourceDrain", Attacker, Defender, Hostile());
        _log.Should().BeEmpty();

        _invalid.Clear();
        Private("ApplyFoggyMindResourceDrain", Attacker, Defender, Hostile());
        Stat(StatType.AbilityResourceDrainFoggyMindFP, 0);
        Private("ApplyFoggyMindResourceDrain", Attacker, Defender, Hostile());
        Stat(StatType.AbilityResourceDrainFoggyMindStamina, 0);
        Private("ApplyFoggyMindResourceDrain", Attacker, Defender, Hostile());
        _log.Should().Equal(
            $"LoseFP {Defender} {StatType.AbilityResourceDrainFoggyMindFP} 2",
            $"LoseStamina {Defender} {StatType.AbilityResourceDrainFoggyMindStamina} 3",
            $"LoseStamina {Defender} {StatType.AbilityResourceDrainFoggyMindStamina} 3");
    }

    [Test]
    public void TheDamageRidersRunDrainRefreshSpreadSplashAndFragmentationBeforeTheSkillRiders()
    {
        Random.SetSeed(7);
        _statuses.Add((Defender, typeof(FoggyMindStatusEffect)));
        _statuses.Add((Defender, typeof(PoisonStatusEffect)));
        Stat(StatType.AbilityResourceDrainFoggyMindFP, 2);
        _sourceStatuses.Add((Defender, typeof(BleedStatusEffect), Attacker));
        _skillSelected = 4;
        _categories.Add((Defender, StatusEffectCategory.Bleeding));
        Stat(StatType.BleedingTargetAbilityBleedSpreadChance, 100);
        Stat(StatType.BleedingTargetAbilityBleedSpreadDurationSeconds, 12);
        _impactSources[StatType.BleedingTargetAbilitySplashDamage] = new[]
        {
            Source("splash", (StatType.BleedingTargetAbilitySplashChance, 100), (StatType.BleedingTargetAbilitySplashDamage, 3),
                (StatType.BleedingTargetAbilitySplashRadiusMeters, 4), (StatType.BleedingTargetAbilitySplashMaximumTargets, 1),
                (StatType.AbilityDamageToBleedingTargetSkillType, (int)SkillType.Katar))
        };
        _sequence = new AbilityImpactSequence();
        _areaImpact = true;
        _permanent[(Attacker, StatType.AreaAbilityFragmentationDamage)] = 3;
        _permanent[(Attacker, StatType.AreaAbilityFragmentationDurationSeconds)] = 9;
        _permanent[(Attacker, StatType.AreaAbilityFragmentationPulseSeconds)] = 3;
        Stat(StatType.KatarVenomCurrentSecondStrikeDamageBonus, 5);
        _nearby.Add(Other);

        Private("ApplyAbilityDamageRiders", Attacker, Defender, Hostile(SkillType.Katar), SkillType.Katar, CombatDamageType.Physical, 10);

        _log.Should().Equal(
            $"LoseFP {Defender} {StatType.AbilityResourceDrainFoggyMindFP} 2",
            $"SkillSelected {Attacker} {SkillType.Katar} {StatType.AbilityDamageToBleedingTargetSkillType} {StatType.BleedingTargetAbilityBleedDurationExtensionSeconds} False",
            $"Extend {Defender} BleedStatusEffect {Attacker} 4",
            $"Apply {Attacker}>{Other} BleedStatusEffect 12 {CombatDamageType.Physical}",
            $"Triggered {Attacker} {Other} 3 {CombatDamageType.Physical} {SkillType.Katar} False null False",
            $"Apply {Attacker}>{Defender} FragmentationStatusEffect 9 {CombatDamageType.Physical}",
            $"Triggered {Attacker} {Defender} 5 {CombatDamageType.Physical} {SkillType.Invalid} False null False",
            $"Apply {Attacker}>{Defender} BleedStatusEffect 30 {CombatDamageType.Physical}");
    }

    [Test]
    public void TheBleedSpreadAndSplashDrawOneRollEachInThatOrderAndOnlyWhenTheyCanTrigger()
    {
        var rolls = Rolls(11, 4);
        _categories.Add((Defender, StatusEffectCategory.Bleeding));
        Stat(StatType.BleedingTargetAbilityBleedSpreadChance, rolls[0]);
        Stat(StatType.BleedingTargetAbilityBleedSpreadDurationSeconds, 12);
        _impactSources[StatType.BleedingTargetAbilitySplashDamage] = new[]
        {
            Source("splash", (StatType.BleedingTargetAbilitySplashChance, rolls[1] - 1), (StatType.BleedingTargetAbilitySplashDamage, 3),
                (StatType.BleedingTargetAbilitySplashRadiusMeters, 4), (StatType.BleedingTargetAbilitySplashMaximumTargets, 1),
                (StatType.AbilityDamageToBleedingTargetSkillType, (int)SkillType.Katar))
        };
        _sequence = new AbilityImpactSequence();
        _nearby.Add(Other);

        Private("ApplyAbilityDamageRiders", Attacker, Defender, Hostile(SkillType.Katar, area: true), SkillType.Katar, CombatDamageType.Physical, 10);

        Lines("Apply").Should().Equal(new[] { $"Apply {Attacker}>{Other} BleedStatusEffect 12 {CombatDamageType.Physical}" },
            "the spread's roll was its chance");
        Lines("Triggered").Should().BeEmpty("the splash's roll was one above its chance");
        Random.D100(1).Should().Be(rolls[2], "the spread and the splash drew one roll each");

        // The splash rider was consumed by the sequence, so a second impact draws no splash roll; the spread draws again.
        Rolls(11, 4);
        Private("ApplyAbilityDamageRiders", Attacker, Defender, Hostile(SkillType.Katar, area: true), SkillType.Katar, CombatDamageType.Physical, 10);
        Random.D100(1).Should().Be(rolls[1], "only the spread drew");

        // A non-physical hit, a non-weapon skill or no chance draws nothing for the spread.
        Rolls(11, 4);
        _sequence = new AbilityImpactSequence();
        _impactSources.Clear();
        Private("ApplyAbilityDamageRiders", Attacker, Defender, Hostile(SkillType.Force), SkillType.Force, CombatDamageType.Force, 10);
        Private("ApplyAbilityDamageRiders", Attacker, Defender, Hostile(SkillType.Katar), SkillType.Katar, CombatDamageType.Fire, 10);
        Stat(StatType.BleedingTargetAbilityBleedSpreadChance, 0);
        Private("ApplyAbilityDamageRiders", Attacker, Defender, Hostile(SkillType.Katar), SkillType.Katar, CombatDamageType.Physical, 10);
        Random.D100(1).Should().Be(rolls[0], "nothing drew");
    }

    [Test]
    public void TheBleedSpreadReachesAtLeastOneTargetAndNeedsABleedingTarget()
    {
        Stat(StatType.BleedingTargetAbilityBleedSpreadChance, 100);
        Stat(StatType.BleedingTargetAbilityBleedSpreadDurationSeconds, 12);
        Stat(StatType.BleedingTargetAbilityBleedSpreadMaxTargets, 0);
        _nearby.AddRange(new[] { Other, Third });
        Private("ApplyBleedingTargetAbilityBleedSpread", Attacker, Defender, SkillType.Katar, CombatDamageType.Physical);
        _log.Should().BeEmpty("the target is not bleeding");

        _categories.Add((Defender, StatusEffectCategory.Bleeding));
        Private("ApplyBleedingTargetAbilityBleedSpread", Attacker, Defender, SkillType.Katar, CombatDamageType.Physical);
        Stat(StatType.BleedingTargetAbilityBleedSpreadMaxTargets, 2);
        Lines("Apply").Should().Equal(new[] { $"Apply {Attacker}>{Other} BleedStatusEffect 12 {CombatDamageType.Physical}" },
            "no maximum means one target");
        _log.Clear();
        Private("ApplyBleedingTargetAbilityBleedSpread", Attacker, Defender, SkillType.Katar, CombatDamageType.Physical);
        Lines("Apply").Should().Equal(
            $"Apply {Attacker}>{Other} BleedStatusEffect 12 {CombatDamageType.Physical}",
            $"Apply {Attacker}>{Third} BleedStatusEffect 12 {CombatDamageType.Physical}");

        _log.Clear();
        Stat(StatType.BleedingTargetAbilityBleedSpreadDurationSeconds, 0);
        Private("ApplyBleedingTargetAbilityBleedSpread", Attacker, Defender, SkillType.Katar, CombatDamageType.Physical);
        _log.Should().BeEmpty();
    }

    [Test]
    public void TheBleedRefreshExtendsOnlyTheActivatorsBleedByTheSelectedExtension()
    {
        _skillSelected = 6;
        Private("ApplyBleedingTargetAbilityBleedRefresh", Attacker, Defender, SkillType.Throwing);
        _log.Should().BeEmpty("the target has no bleed from the attacker");

        _sourceStatuses.Add((Defender, typeof(BleedStatusEffect), Attacker));
        _skillSelected = 0;
        Private("ApplyBleedingTargetAbilityBleedRefresh", Attacker, Defender, SkillType.Throwing);
        Lines("Extend").Should().BeEmpty();
        _skillSelected = 6;
        Private("ApplyBleedingTargetAbilityBleedRefresh", Attacker, Defender, SkillType.Throwing);
        Lines("Extend").Should().Equal($"Extend {Defender} BleedStatusEffect {Attacker} 6");
    }

    [Test]
    public void TheBleedSplashSkipsSourcesThatCannotTriggerOrThatTheSequenceConsumed()
    {
        _impactSources[StatType.BleedingTargetAbilitySplashDamage] = new[]
        {
            Source("wrongSkill", (StatType.BleedingTargetAbilitySplashChance, 100), (StatType.BleedingTargetAbilitySplashDamage, 3),
                (StatType.BleedingTargetAbilitySplashRadiusMeters, 4), (StatType.BleedingTargetAbilitySplashMaximumTargets, 1),
                (StatType.AbilityDamageToBleedingTargetSkillType, (int)SkillType.Spear)),
            Source("splash", (StatType.BleedingTargetAbilitySplashChance, 100), (StatType.BleedingTargetAbilitySplashDamage, 3),
                (StatType.BleedingTargetAbilitySplashRadiusMeters, 4), (StatType.BleedingTargetAbilitySplashMaximumTargets, 2),
                (StatType.AbilityDamageToBleedingTargetSkillType, (int)SkillType.Katar))
        };
        _nearby.AddRange(new[] { Other, Third });
        Private("ApplyBleedingTargetAbilitySplash", Attacker, Defender, SkillType.Katar, CombatDamageType.Physical);
        _log.Should().BeEmpty("the target is not bleeding");

        _categories.Add((Defender, StatusEffectCategory.Bleeding));
        Private("ApplyBleedingTargetAbilitySplash", Attacker, Defender, SkillType.Katar, CombatDamageType.Physical);
        _log.Should().BeEmpty("without an impact sequence nothing can consume the rider");

        _sequence = new AbilityImpactSequence();
        Private("ApplyBleedingTargetAbilitySplash", Attacker, Defender, SkillType.Katar, CombatDamageType.Physical);
        Private("ApplyBleedingTargetAbilitySplash", Attacker, Defender, SkillType.Katar, CombatDamageType.Physical);
        _log.Should().Equal(
            $"Triggered {Attacker} {Other} 3 {CombatDamageType.Physical} {SkillType.Katar} False null False",
            $"Triggered {Attacker} {Third} 3 {CombatDamageType.Physical} {SkillType.Katar} False null False");
    }

    [Test]
    public void APistolSingleTargetImpactRicochetsOffCooldownToEveryNearbyTargetButTheFirst()
    {
        Stat(StatType.PistolSkirmisherRicochetDamageBonus, 4);
        Stat(StatType.PistolSkirmisherRicochetMaximumTargets, 3);
        Stat(StatType.PistolSkirmisherRicochetCooldownSeconds, 9);
        _nearby.AddRange(new[] { Defender, Other });
        _hitPoints[Defender] = 30;

        Private("ApplyAbilityDamageRiders", Attacker, Defender, Hostile(SkillType.Pistol), SkillType.Pistol, CombatDamageType.Physical, 10);
        Private("ApplyAbilityDamageRiders", Attacker, Defender, Hostile(SkillType.Pistol, area: true), SkillType.Pistol, CombatDamageType.Physical, 10);
        _blockedTriggers.Add(StatType.PistolSkirmisherRicochetDamageBonus);
        Private("ApplyAbilityDamageRiders", Attacker, Defender, Hostile(SkillType.Pistol), SkillType.Pistol, CombatDamageType.Physical, 10);
        _log.Should().Equal(
            $"Trigger {Attacker} {StatType.PistolSkirmisherRicochetDamageBonus} 9",
            $"Triggered {Attacker} {Other} 4 {CombatDamageType.Physical} {SkillType.Invalid} False null False",
            $"Trigger {Attacker} {StatType.PistolSkirmisherRicochetDamageBonus} 9");

        _log.Clear();
        Stat(StatType.PistolSkirmisherRicochetMaximumTargets, 0);
        Private("ApplyRicochetDamage", Attacker, Defender, CombatDamageType.Physical, StatType.PistolSkirmisherRicochetDamageBonus,
            StatType.PistolSkirmisherRicochetMaximumTargets, StatType.PistolSkirmisherRicochetCooldownSeconds);
        _log.Should().BeEmpty();
    }

    [Test]
    public void AThrowingImpactRicochetsFromASingleTargetAndStormsAndTossesFromAnArea()
    {
        Stat(StatType.ThrowingDeadeyeRicochetDamageBonus, 2);
        Stat(StatType.ThrowingDeadeyeRicochetMaximumTargets, 2);
        Stat(StatType.ThrowingBombardierClusterStormDamageBonus, 4);
        Stat(StatType.ThrowingBombardierClusterStormMaximumTargets, 2);
        Stat(StatType.ThrowingBombardierSaturationToss, 1);
        Stat(StatType.ThrowingBombardierSaturationTossDurationSeconds, 6);
        Stat(StatType.ThrowingBombardierSaturationTossDamage, 3);
        Stat(StatType.ThrowingBombardierSaturationTossPulseSeconds, 2);
        _sequence = new AbilityImpactSequence();
        _nearby.AddRange(new[] { Other, Third });

        Private("ApplyAbilityDamageRiders", Attacker, Defender, Hostile(SkillType.Throwing), SkillType.Throwing, CombatDamageType.Physical, 10);
        Private("ApplyAbilityDamageRiders", Attacker, Defender, Hostile(SkillType.Throwing, area: true), SkillType.Throwing, CombatDamageType.Fire, 10);
        Private("ApplyAbilityDamageRiders", Attacker, Defender, Hostile(SkillType.Throwing, area: true), SkillType.Throwing, CombatDamageType.Fire, 10);

        _log.Should().Equal(
            $"Triggered {Attacker} {Other} 2 {CombatDamageType.Physical} {SkillType.Invalid} False null False",
            $"Triggered {Attacker} {Third} 2 {CombatDamageType.Physical} {SkillType.Invalid} False null False",
            $"Triggered {Attacker} {Other} 4 {CombatDamageType.Fire} {SkillType.Invalid} False null False",
            $"Triggered {Attacker} {Third} 4 {CombatDamageType.Fire} {SkillType.Invalid} False null False",
            $"Apply {Attacker}>{Defender} SaturationTossStatusEffect 6 {CombatDamageType.Fire}",
            $"Apply {Attacker}>{Defender} SaturationTossStatusEffect 6 {CombatDamageType.Fire}");
        Visuals().Should().Equal(VisualEffect.Vfx_Dur_Aura_Fire, VisualEffect.Vfx_Imp_Flame_S, VisualEffect.Vfx_Dur_Aura_Fire, VisualEffect.Vfx_Imp_Flame_S);
    }

    [Test]
    public void TheClusterStormNeedsItsConfigurationAndASequenceAndTheTossNeedsItsStatusToApply()
    {
        Stat(StatType.ThrowingBombardierClusterStormDamageBonus, 4);
        _nearby.Add(Other);
        Private("ApplyClusterStormDamage", Attacker, Defender, CombatDamageType.Physical);
        Stat(StatType.ThrowingBombardierClusterStormMaximumTargets, 2);
        Private("ApplyClusterStormDamage", Attacker, Defender, CombatDamageType.Physical);
        _log.Should().BeEmpty("there is no impact sequence");

        Stat(StatType.ThrowingBombardierSaturationToss, 1);
        Private("ApplySaturationToss", Attacker, Defender);
        Stat(StatType.ThrowingBombardierSaturationTossDurationSeconds, 6);
        Stat(StatType.ThrowingBombardierSaturationTossDamage, 3);
        Stat(StatType.ThrowingBombardierSaturationTossPulseSeconds, 2);
        _statusApplies = false;
        Private("ApplySaturationToss", Attacker, Defender);
        Lines("Apply").Should().ContainSingle();
        Visuals().Should().BeEmpty("the toss did not apply");
    }

    [Test]
    public void AKatarSingleTargetImpactStrikesAgainAndBleedsAPoisonedTarget()
    {
        Stat(StatType.KatarVenomCurrentSecondStrikeDamageBonus, 5);
        Private("ApplyAbilityDamageRiders", Attacker, Defender, Hostile(SkillType.Katar), SkillType.Katar, CombatDamageType.Physical, 10);
        Private("ApplyAbilityDamageRiders", Attacker, Defender, Hostile(SkillType.Katar, area: true), SkillType.Katar, CombatDamageType.Physical, 10);
        _log.Should().Equal($"Triggered {Attacker} {Defender} 5 {CombatDamageType.Physical} {SkillType.Invalid} False null False");
    }

    [Test]
    public void ThePulseSourcesNeedTheFirstTargetOfAHostileAreaImpact()
    {
        var sources = new[] { Source("pulse", (StatType.AreaAbilityPulseDamage, 6), (StatType.AreaAbilityPulseRadiusMeters, 4)) };
        _sources[(Attacker, StatType.AreaAbilityPulseDamage)] = sources;
        ((IReadOnlyList<StatAdjustmentSource>)Private("GetAreaAbilityPulseSources", Attacker, Hostile(), true)!).Should().BeEmpty();
        _areaImpact = true;
        ((IReadOnlyList<StatAdjustmentSource>)Private("GetAreaAbilityPulseSources", Attacker, Hostile(), false)!).Should().BeEmpty();
        ((IReadOnlyList<StatAdjustmentSource>)Private("GetAreaAbilityPulseSources", Attacker, new AbilityDetail(), true)!).Should().BeEmpty();
        ((IReadOnlyList<StatAdjustmentSource>)Private("GetAreaAbilityPulseSources", Attacker, null!, true)!).Should().BeEmpty();
        ((IReadOnlyList<StatAdjustmentSource>)Private("GetAreaAbilityPulseSources", Attacker, Hostile(), true)!).Should().BeSameAs(sources);
    }

    [Test]
    public void ThePulseRunsOncePerTriggerAndSkipsIncompleteSources()
    {
        _areaImpact = true;
        _nearby.Add(Other);
        _sources[(Attacker, StatType.AreaAbilityPulseDamage)] = new[]
        {
            Source("noRadius", (StatType.AreaAbilityPulseDamage, 6)),
            Source("pulse", (StatType.AreaAbilityPulseDamage, 6), (StatType.AreaAbilityPulseRadiusMeters, 4)),
            Source("noDamage", (StatType.AreaAbilityPulseRadiusMeters, 4)),
            Source("second", (StatType.AreaAbilityPulseDamage, 2), (StatType.AreaAbilityPulseRadiusMeters, 2))
        };

        Private("ApplyAreaAbilityPulse", Attacker, Defender, Hostile(area: true), true);
        _pulseReady = false;
        Private("ApplyAreaAbilityPulse", Attacker, Defender, Hostile(area: true), true);
        _log.Should().Equal(
            $"Pulse {Attacker}",
            $"Triggered {Attacker} {Other} 6 {CombatDamageType.Physical} {SkillType.Invalid} False null False",
            $"Triggered {Attacker} {Other} 2 {CombatDamageType.Physical} {SkillType.Invalid} False null False",
            $"Pulse {Attacker}");
        Visuals().Should().Equal(VisualEffect.Vfx_Fnf_Howl_War_Cry);

        _log.Clear();
        _sources[(Attacker, StatType.AreaAbilityPulseDamage)] = new[] { Source("noRadius", (StatType.AreaAbilityPulseDamage, 6)) };
        Private("ApplyAreaAbilityPulse", Attacker, Defender, Hostile(area: true), true);
        _log.Should().BeEmpty("no source can pulse, so the trigger is not spent");
    }

    [Test]
    public void TheFragmentationFiltersPersistentAndTemporarySourcesByTheirOwnSkill()
    {
        _permanent[(Attacker, StatType.AreaAbilityFragmentationSkillType)] = (int)SkillType.Throwing;
        _permanent[(Attacker, StatType.AreaAbilityFragmentationDamage)] = 3;
        _temporary[StatType.AreaAbilityFragmentationSkillType] = (int)SkillType.Staff;
        _temporary[StatType.AreaAbilityFragmentationDamage] = 4;

        ((int)Private("GetAreaAbilityFragmentationStatAdjustment", Attacker, SkillType.Throwing, StatType.AreaAbilityFragmentationDamage)!).Should().Be(3);
        ((int)Private("GetAreaAbilityFragmentationStatAdjustment", Attacker, SkillType.Staff, StatType.AreaAbilityFragmentationDamage)!).Should().Be(4);
        ((int)Private("GetAreaAbilityFragmentationStatAdjustment", Attacker, SkillType.Rifle, StatType.AreaAbilityFragmentationDamage)!).Should().Be(0);
        _permanent.Remove((Attacker, StatType.AreaAbilityFragmentationSkillType));
        _temporary.Remove(StatType.AreaAbilityFragmentationSkillType);
        ((int)Private("GetAreaAbilityFragmentationStatAdjustment", Attacker, SkillType.Rifle, StatType.AreaAbilityFragmentationDamage)!).Should().Be(7);
    }

    [Test]
    public void TheFragmentationNeedsAnAreaImpactOnAValidTargetAndAFullConfiguration()
    {
        _permanent[(Attacker, StatType.AreaAbilityFragmentationDamage)] = 3;
        _permanent[(Attacker, StatType.AreaAbilityFragmentationDurationSeconds)] = 9;
        Private("ApplyAreaAbilityFragmentation", Attacker, Defender, Hostile(area: true), SkillType.Rifle, CombatDamageType.Physical);
        _areaImpact = true;
        Private("ApplyAreaAbilityFragmentation", Attacker, Defender, null!, SkillType.Rifle, CombatDamageType.Physical);
        Private("ApplyAreaAbilityFragmentation", Attacker, Defender, Hostile(area: true), SkillType.Rifle, CombatDamageType.Physical);
        _permanent[(Attacker, StatType.AreaAbilityFragmentationPulseSeconds)] = 3;
        _invalid.Add(Defender);
        Private("ApplyAreaAbilityFragmentation", Attacker, Defender, Hostile(area: true), SkillType.Rifle, CombatDamageType.Physical);
        _log.Should().BeEmpty();

        _invalid.Clear();
        Private("ApplyAreaAbilityFragmentation", Attacker, Defender, Hostile(area: true), SkillType.Rifle, CombatDamageType.Sonic);
        _log.Should().Equal($"Apply {Attacker}>{Defender} FragmentationStatusEffect 9 {CombatDamageType.Sonic}");
    }

    [Test]
    public void TheAreaHitSequenceCountsHitsOnATargetWithinItsWindowAndExposesAtTheRequiredCount()
    {
        _areaImpact = true;
        var ability = Hostile(SkillType.Throwing, area: true);
        Stat(StatType.AreaAbilityTargetHitSequenceSkillType, (int)SkillType.Throwing);
        Stat(StatType.AreaAbilityTargetHitSequenceCountRequired, 3);
        Stat(StatType.AreaAbilityTargetHitSequenceWindowSeconds, 6);
        Stat(StatType.AreaAbilityTargetHitSequenceExposedDurationSeconds, 5);

        Private("ApplyAreaAbilityTargetHitSequenceEffects", Attacker, Defender, ability, SkillType.Throwing);
        _now = _now.AddSeconds(6);
        Private("ApplyAreaAbilityTargetHitSequenceEffects", Attacker, Defender, ability, SkillType.Throwing);
        HitSequences().Count.Should().Be(1);
        Lines("Apply").Should().BeEmpty();
        Private("ApplyAreaAbilityTargetHitSequenceEffects", Attacker, Defender, ability, SkillType.Throwing);
        Lines("Apply").Should().ContainSingle().Which.Should().Be($"Apply {Attacker}>{Defender} ExposedStatusEffect 5 {CombatDamageType.Physical}",
            "a hit at exactly six seconds is inside the window, so the count reached two and the next hit made three");
        HitSequences().Count.Should().Be(0, "the sequence is spent");

        // A fresh sequence: the window lapses between the first two hits, so the count starts again at one.
        _log.Clear();
        Private("ApplyAreaAbilityTargetHitSequenceEffects", Attacker, Other, ability, SkillType.Throwing);
        _now = _now.AddSeconds(7);
        Private("ApplyAreaAbilityTargetHitSequenceEffects", Attacker, Other, ability, SkillType.Throwing);
        Lines("Apply").Should().BeEmpty("the window lapsed, so the count started again at one");

        Private("ApplyAreaAbilityTargetHitSequenceEffects", Attacker, Other, ability, SkillType.Throwing);
        Private("ApplyAreaAbilityTargetHitSequenceEffects", Attacker, Other, ability, SkillType.Throwing);
        Lines("Apply").Should().Equal($"Apply {Attacker}>{Other} ExposedStatusEffect 5 {CombatDamageType.Physical}");
        HitSequences().Count.Should().Be(0, "the sequence is spent");
    }

    [TestCase(StatType.AreaAbilityTargetHitSequenceSkillType, (int)SkillType.Staff)]
    [TestCase(StatType.AreaAbilityTargetHitSequenceCountRequired, 0)]
    [TestCase(StatType.AreaAbilityTargetHitSequenceWindowSeconds, 0)]
    [TestCase(StatType.AreaAbilityTargetHitSequenceExposedDurationSeconds, 0)]
    public void TheAreaHitSequenceNeedsEveryPartOfItsConfiguration(StatType missing, int value)
    {
        _areaImpact = true;
        Stat(StatType.AreaAbilityTargetHitSequenceSkillType, (int)SkillType.Throwing);
        Stat(StatType.AreaAbilityTargetHitSequenceCountRequired, 1);
        Stat(StatType.AreaAbilityTargetHitSequenceWindowSeconds, 6);
        Stat(StatType.AreaAbilityTargetHitSequenceExposedDurationSeconds, 5);
        Stat(missing, value);
        Private("ApplyAreaAbilityTargetHitSequenceEffects", Attacker, Defender, Hostile(SkillType.Throwing, area: true), SkillType.Throwing);
        _log.Should().BeEmpty();
        HitSequences().Count.Should().Be(0);
    }

    [Test]
    public void TheAreaHitSequenceNeedsAnAreaImpactOnAValidTarget()
    {
        Stat(StatType.AreaAbilityTargetHitSequenceSkillType, (int)SkillType.Throwing);
        Stat(StatType.AreaAbilityTargetHitSequenceCountRequired, 1);
        Stat(StatType.AreaAbilityTargetHitSequenceWindowSeconds, 6);
        Stat(StatType.AreaAbilityTargetHitSequenceExposedDurationSeconds, 5);
        Private("ApplyAreaAbilityTargetHitSequenceEffects", Attacker, Defender, Hostile(SkillType.Throwing, area: true), SkillType.Throwing);
        _areaImpact = true;
        Private("ApplyAreaAbilityTargetHitSequenceEffects", Attacker, Defender, null!, SkillType.Throwing);
        _invalid.Add(Defender);
        Private("ApplyAreaAbilityTargetHitSequenceEffects", Attacker, Defender, Hostile(SkillType.Throwing, area: true), SkillType.Throwing);
        _log.Should().BeEmpty();
    }
}
