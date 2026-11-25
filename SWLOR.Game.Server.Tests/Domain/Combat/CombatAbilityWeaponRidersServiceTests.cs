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
/// The weapon riders part of <c>Combat</c>: the riders of each weapon family, the Spear Disabler suppression riders, the perk category enmity
/// status and the guarded hit exposed status. The stay-part dispatches to it by skill and the tests ride through that dispatch.
/// </summary>
[TestFixture]
public class CombatAbilityWeaponRidersServiceTests : CombatAbilityImpactTestBase
{
    protected override object PrivateTarget => _weaponRiders;

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
        _weaponRiders.ApplySpearDisablerSuppressionRiders(Gone, Defender);
        _weaponRiders.ApplySpearDisablerSuppressionRiders(Attacker, Gone);
        _log.Should().BeEmpty();

        _weaponRiders.ApplySpearDisablerSuppressionRiders(Attacker, Defender);
        Stat(StatType.SpearDisablerFractureStrike, 0);
        Stat(StatType.SpearDisablerForcebane, 0);
        _weaponRiders.ApplySpearDisablerSuppressionRiders(Attacker, Defender);
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
    public void ItHoldsTheEightServicesItsRidersReadAndNoLazy()
    {
        var constructor = typeof(CombatAbilityWeaponRidersService).GetConstructors().Should().ContainSingle().Subject;

        constructor.GetParameters().Select(parameter => parameter.ParameterType).Should().Equal(
            typeof(ICombatQueriesService), typeof(ICombatResourcesService), typeof(ICombatDefeatedEnemyEffectsService), typeof(IStatService),
            typeof(IStatusEffectService), typeof(IStatusEffectApplicationService), typeof(ITemporaryStatModifierService), typeof(IEnmityService));
        constructor.GetParameters().Should().NotContain(parameter => parameter.ParameterType.IsGenericType, "no Lazy<> is added");
    }

    [Test]
    public void ItHoldsNoStateAndOwnsNoHandler()
    {
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        typeof(CombatAbilityWeaponRidersService).GetFields(All).Where(field => !field.FieldType.IsInterface).Should().BeEmpty("it holds services only");
        typeof(CombatAbilityWeaponRidersService).GetMethods(All).SelectMany(method => method.GetCustomAttributes<NWNEventHandler>()).Should().BeEmpty();
    }

    [Test]
    public void TheStayPartNoLongerDeclaresTheWeaponRidersAndKeepsTheOrchestrator()
    {
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        var moved = typeof(ICombatAbilityWeaponRidersService).GetMethods().Select(method => method.Name).ToArray();

        moved.Should().HaveCount(18).And.Contain("ApplySpearDisablerSuppressionRiders");
        typeof(ICombatAbilityImpactService).GetMethods().Select(method => method.Name).Should().NotIntersectWith(moved);
        typeof(CombatAbilityImpactService).GetMethods(All).Select(method => method.Name).Should().NotIntersectWith(moved);
        typeof(CombatAbilityImpactService).GetMethods(All).Select(method => method.Name)
            .Should().Contain("ApplySuccessfulAbilityImpactRiders").And.Contain("ApplyAbilityStatusRiders");
    }

    [Test]
    public void TheContainerHoldsOnePartThatIsNotTheStayPart()
    {
        using var facades = new FacadeContainer();
        using var installed = facades.Install();

        var part = facades.Get<ICombatAbilityWeaponRidersService>();

        part.Should().BeOfType<CombatAbilityWeaponRidersService>();
        part.Should().NotBeSameAs(facades.Get<ICombatAbilityImpactService>());
        GameServices.CombatAbilityWeaponRiders.Should().BeSameAs(part);
        facades.Get<ICombatAbilityWeaponRidersService>().Should().BeSameAs(part, "it is one singleton");
    }
}
