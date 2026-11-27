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
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The weapon activation part of <c>Combat</c>: the Heavy Vibroblade, Beast Mastery, Vibroknife, Pistol and Lightsaber riders
/// that run for the skill the activated ability uses.
/// </summary>
[TestFixture]
public class CombatAbilityWeaponActivationServiceTests : CombatAbilityActivationTestBase
{
    [Test]
    public void TheActivatedRidersRunOnlyForTheirSkillAndNeedAnAbility()
    {
        Stat(Activator, StatType.VibroknifeShadowEvasiveCombatRank, 1);
        var ability = Hostile();

        Private(_weaponActivation, "ApplyAbilityActivatedRiders", Activator, Target, ability, SkillType.Rifle);
        _log.Should().BeEmpty();
        typeof(CombatAbilityWeaponActivationService).GetMethod("ApplyAbilityActivatedRiders", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(_weaponActivation, new object?[] { Activator, Target, null, SkillType.Vibroknife });
        _log.Should().BeEmpty("no ability");

        Private(_weaponActivation, "ApplyAbilityActivatedRiders", Activator, Target, ability, SkillType.Vibroknife);
        _log.Should().Equal($"Apply {Activator}>{Activator} EvasiveCombatStatusEffect(-15,10,-15) 8");
    }

    [Test]
    public void TheHeavyVibrobladeRidersGrantTheNextAutoAttackBonusAndGuardiansResolveForTheirPerks()
    {
        var ability = Hostile();
        Stat(Activator, StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageBonus, 6);
        Stat(Activator, StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageDurationSeconds, 9);
        Stat(Activator, StatType.HeavyVibrobladeDefenseGuardiansResolveShieldPercent, 10);
        Stat(Activator, StatType.HeavyVibrobladeDefenseGuardiansResolveDurationSeconds, 8);
        Stat(Activator, StatType.HeavyVibrobladeDefenseGuardiansResolveCooldownSeconds, 30);

        Private(_weaponActivation, "ApplyAbilityActivatedRiders", Activator, Target, ability, SkillType.HeavyVibroblade);
        _log.Should().BeEmpty("neither rider's perks match");
        _asked.Should().Equal(
            $"PerkTypes {Activator} {string.Join(",", StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerPrimaryPerkType, StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerSecondaryPerkType, StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerTertiaryPerkType, StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerQuaternaryPerkType, StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerQuinaryPerkType, StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerSenaryPerkType)}",
            $"PerkTypes {Activator} {string.Join(",", StatType.HeavyVibrobladeDefenseGuardiansResolveTriggerPrimaryPerkType, StatType.HeavyVibrobladeDefenseGuardiansResolveTriggerSecondaryPerkType, StatType.HeavyVibrobladeDefenseGuardiansResolveTriggerTertiaryPerkType, StatType.HeavyVibrobladeDefenseGuardiansResolveTriggerQuaternaryPerkType)}");

        _matchingPerkTypes.Add(StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerPrimaryPerkType);
        _matchingPerkTypes.Add(StatType.HeavyVibrobladeDefenseGuardiansResolveTriggerPrimaryPerkType);
        _hitPoints[Activator] = (100, 100);
        Private(_weaponActivation, "ApplyAbilityActivatedRiders", Activator, Target, ability, SkillType.HeavyVibroblade);

        _log.Take(3).Should().Equal(
            $"Replace {Activator} {StatType.NextAutoAttackDamageBonus} 6 9 {StatType.NextAutoAttackDamageBonus}",
            $"Trigger {Activator} {StatType.HeavyVibrobladeDefenseGuardiansResolveShieldPercent} 30",
            $"Attribute {Activator} {StatType.HeavyVibrobladeDefenseGuardiansResolveShieldPercent}");
        var shield = SWLOR.Game.Server.Feature.AbilityDefinition.TemporaryHitPointEffects.GetGrantedAmount(
            Activator, SWLOR.Game.Server.Domain.Combat.TemporaryHitPointEffectKey.GuardiansResolve);
        var generation = SWLOR.Game.Server.Feature.AbilityDefinition.TemporaryHitPointEffects.GetGrantGeneration(
            Activator, SWLOR.Game.Server.Domain.Combat.TemporaryHitPointEffectKey.GuardiansResolve);
        shield.Should().Be(10, "10% of 100 HP fits under every cap");
        generation.Should().BeGreaterThan(0);
        _log.Skip(3).Should().Equal($"Apply {Activator}>{Activator} GuardiansResolveStatusEffect({shield},{generation}) 8");
    }

    [Test]
    public void TheNextAutoAttackBonusAndGuardiansResolveNeedTheirStats()
    {
        Private(_offenseEffects, "ApplyNextAutoAttackDamageBonus", Activator, StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageBonus,
            StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageDurationSeconds);
        Stat(Activator, StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageBonus, -3);
        Private(_offenseEffects, "ApplyNextAutoAttackDamageBonus", Activator, StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageBonus,
            StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageDurationSeconds);
        _log.Should().BeEmpty("no bonus, then no duration");

        Stat(Activator, StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageDurationSeconds, 4);
        Private(_offenseEffects, "ApplyNextAutoAttackDamageBonus", Activator, StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageBonus,
            StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageDurationSeconds);
        _log.Should().Equal($"Replace {Activator} {StatType.NextAutoAttackDamageBonus} -3 4 {StatType.NextAutoAttackDamageBonus}");

        _log.Clear();
        Stat(Activator, StatType.HeavyVibrobladeDefenseGuardiansResolveShieldPercent, 10);
        Private(_weaponActivation, "ApplyGuardiansResolve", Activator);
        Stat(Activator, StatType.HeavyVibrobladeDefenseGuardiansResolveShieldPercent, 0);
        Stat(Activator, StatType.HeavyVibrobladeDefenseGuardiansResolveDurationSeconds, 8);
        Private(_weaponActivation, "ApplyGuardiansResolve", Activator);
        _log.Should().BeEmpty("no duration, then no shield");

        Stat(Activator, StatType.HeavyVibrobladeDefenseGuardiansResolveShieldPercent, 10);
        _triggerReady = false;
        Private(_weaponActivation, "ApplyGuardiansResolve", Activator);
        _log.Should().Equal($"Trigger {Activator} {StatType.HeavyVibrobladeDefenseGuardiansResolveShieldPercent} 0");

        // A creature with no hit points gets no pool, so no status follows the grant.
        _log.Clear();
        _triggerReady = true;
        Private(_weaponActivation, "ApplyGuardiansResolve", Activator);
        _log.Should().Equal(
            $"Trigger {Activator} {StatType.HeavyVibrobladeDefenseGuardiansResolveShieldPercent} 0",
            $"Attribute {Activator} {StatType.HeavyVibrobladeDefenseGuardiansResolveShieldPercent}");
    }

    [Test]
    public void TheBalancedRecoveryRestoresTheBeastAndItsMasterWhenTheCategoryMatchesAndTheTriggerIsReady()
    {
        var ability = Hostile();
        Stat(Activator, StatType.BeastBalancedAbilityStaminaRestore, 5);
        Stat(Activator, StatType.BeastBalancedAbilityStaminaRestoreCooldownSeconds, 12);

        Private(_weaponActivation, "ApplyAbilityActivatedRiders", Activator, Target, ability, SkillType.BeastMastery);
        _log.Should().BeEmpty("the category does not match");
        _asked.Should().Equal($"PerkCategory {Activator} {StatType.BeastBalancedAbilityStaminaRestoreCategoryId}");

        _matchingPerkCategories.Add(StatType.BeastBalancedAbilityStaminaRestoreCategoryId);
        _triggerReady = false;
        Private(_weaponActivation, "ApplyBeastBalancedAbilityRecovery", Activator, ability);
        _log.Should().Equal($"Trigger {Activator} {StatType.BeastBalancedAbilityStaminaRestore} 12");

        _log.Clear();
        _triggerReady = true;
        Private(_weaponActivation, "ApplyBeastBalancedAbilityRecovery", Activator, ability);
        _log.Should().Equal(
            new[]
            {
                $"Trigger {Activator} {StatType.BeastBalancedAbilityStaminaRestore} 12",
                $"RestoreStamina {Activator} {StatType.BeastBalancedAbilityStaminaRestore} 5",
            },
            "a beast with no master restores only itself");

        _log.Clear();
        _masters[Activator] = Master;
        Private(_weaponActivation, "ApplyBeastBalancedAbilityRecovery", Activator, ability);
        _log.Should().Equal(
            $"Trigger {Activator} {StatType.BeastBalancedAbilityStaminaRestore} 12",
            $"RestoreStamina {Activator} {StatType.BeastBalancedAbilityStaminaRestore} 5",
            $"RestoreStamina {Master} {StatType.BeastBalancedAbilityStaminaRestore} 5");

        _log.Clear();
        Stat(Activator, StatType.BeastBalancedAbilityStaminaRestore, 0);
        Private(_weaponActivation, "ApplyBeastBalancedAbilityRecovery", Activator, ability);
        _log.Should().BeEmpty("no restore");
    }

    [TestCase(1, 10, -15)]
    [TestCase(2, 20, -25)]
    public void EvasiveCombatScalesWithItsRankForAreaOrSingleTargetAbilities(int rank, int evasion, int enmity)
    {
        Stat(Activator, StatType.VibroknifeShadowEvasiveCombatRank, rank);

        Private(_weaponActivation, "ApplyVibroknifeShadowActivatedEffects", Activator, new AbilityDetail { Name = "Buff" });
        _log.Should().BeEmpty("neither an area nor a single target ability");

        Private(_weaponActivation, "ApplyVibroknifeShadowActivatedEffects", Activator, new AbilityDetail { Name = "Fan", IsAreaAbility = true });
        _log.Should().Equal($"Apply {Activator}>{Activator} EvasiveCombatStatusEffect(-15,{evasion},{enmity}) 8");

        _log.Clear();
        Stat(Activator, StatType.VibroknifeShadowEvasiveCombatRank, 0);
        Private(_weaponActivation, "ApplyVibroknifeShadowActivatedEffects", Activator, Hostile());
        _log.Should().BeEmpty("no rank");
    }

    [Test]
    public void TheSkirmisherRidersGrantEvasionTheNextPistolAbilityAndAnEnmityDropOnAValidTarget()
    {
        Stat(Activator, StatType.PistolSkirmisherEvasiveAbilityDurationSeconds, 6);
        Stat(Activator, StatType.PistolSkirmisherEvasiveAbilityEvasionPercent, 10);
        Stat(Activator, StatType.PistolSkirmisherEvasiveAbilityNextAttackDamageBonus, 4);
        Stat(Activator, StatType.PistolSkirmisherEvasiveAbilityEnmityReductionPercent, 20);

        Private(_weaponActivation, "ApplyAbilityActivatedRiders", Activator, Target, new AbilityDetail { Name = "Buff" }, SkillType.Pistol);
        _log.Should().BeEmpty("neither an area nor a single target ability");

        Private(_weaponActivation, "ApplyAbilityActivatedRiders", Activator, Target, Hostile(), SkillType.Pistol);
        _log.Should().Equal(
            $"Replace {Activator} {StatType.EvasionPercentAdjustment} 10 6 {StatType.PistolSkirmisherEvasiveAbilityEvasionPercent}",
            $"NextSkill {Activator} {SkillType.Pistol} 4 0 6 0 {StatusEffectCategory.None}",
            $"ReduceEnmity {Activator} {Target} 20");

        _log.Clear();
        _invalid.Add(Target);
        Stat(Activator, StatType.PistolSkirmisherEvasiveAbilityEvasionPercent, 0);
        Stat(Activator, StatType.PistolSkirmisherEvasiveAbilityNextAttackDamageBonus, 0);
        Private(_weaponActivation, "ApplyPistolSkirmisherActivatedEffects", Activator, Target, Hostile());
        _invalid.Clear();
        Stat(Activator, StatType.PistolSkirmisherEvasiveAbilityEnmityReductionPercent, 0);
        Private(_weaponActivation, "ApplyPistolSkirmisherActivatedEffects", Activator, Target, Hostile());
        Stat(Activator, StatType.PistolSkirmisherEvasiveAbilityDurationSeconds, 0);
        Stat(Activator, StatType.PistolSkirmisherEvasiveAbilityEvasionPercent, 10);
        Private(_weaponActivation, "ApplyPistolSkirmisherActivatedEffects", Activator, Target, Hostile());
        _log.Should().BeEmpty("an invalid target, no reduction, then no duration");
    }

    [Test]
    public void CenteringNeedsItsStatsAndAReadyTriggerAndDropsEnmityOnAValidTarget()
    {
        Stat(Activator, StatType.LightsaberOffenseCenteringAccuracyPercent, 8);
        Stat(Activator, StatType.LightsaberOffenseCenteringDurationSeconds, 6);
        Stat(Activator, StatType.LightsaberOffenseCenteringCooldownSeconds, 20);
        Stat(Activator, StatType.LightsaberOffenseCenteringEnmityReductionPercent, 15);

        Private(_weaponActivation, "ApplyLightsaberOffenseCentering", Activator, Target);
        _log.Should().Equal(
            $"Trigger {Activator} {StatType.LightsaberOffenseCenteringAccuracyPercent} 20",
            $"Apply {Activator}>{Activator} CenteringStatusEffect(8) 6",
            $"ReduceEnmity {Activator} {Target} 15");

        _log.Clear();
        _invalid.Add(Target);
        Private(_weaponActivation, "ApplyLightsaberOffenseCentering", Activator, Target);
        _invalid.Clear();
        Stat(Activator, StatType.LightsaberOffenseCenteringEnmityReductionPercent, 0);
        Private(_weaponActivation, "ApplyLightsaberOffenseCentering", Activator, Target);
        _log.Where(line => line.StartsWith("ReduceEnmity", StringComparison.Ordinal)).Should().BeEmpty("an invalid target, then no reduction");

        _log.Clear();
        _triggerReady = false;
        Private(_weaponActivation, "ApplyLightsaberOffenseCentering", Activator, Target);
        _log.Should().Equal($"Trigger {Activator} {StatType.LightsaberOffenseCenteringAccuracyPercent} 20");

        _log.Clear();
        Stat(Activator, StatType.LightsaberOffenseCenteringDurationSeconds, 0);
        Private(_weaponActivation, "ApplyLightsaberOffenseCentering", Activator, Target);
        Stat(Activator, StatType.LightsaberOffenseCenteringDurationSeconds, 6);
        Stat(Activator, StatType.LightsaberOffenseCenteringAccuracyPercent, 0);
        Private(_weaponActivation, "ApplyLightsaberOffenseCentering", Activator, Target);
        _log.Should().BeEmpty("no duration, then no accuracy");
    }

    [Test]
    public void SecondWindRestoresAPercentOfMaximumStaminaScaledByItsAbilityAndCappedAtLowStamina()
    {
        Stat(Activator, StatType.LightsaberOffenseSecondWindThresholdPercent, 30);
        Stat(Activator, StatType.LightsaberOffenseSecondWindStaminaRestoreBasePercent, 10);
        Stat(Activator, StatType.LightsaberOffenseSecondWindCooldownSeconds, 60);
        Stat(Activator, StatType.LightsaberOffenseSecondWindScalingAbility, (int)AbilityType.Agility + 1);
        Stat(Activator, StatType.LightsaberOffenseSecondWindStaminaRestoreMaximumPercent, 14);
        _abilityScores[AbilityType.Agility] = 6;
        _maxStamina[Activator] = 200;
        _stamina[Activator] = 60;

        Private(_weaponActivation, "ApplyLightsaberOffenseSecondWind", Activator);
        _log.Should().Equal(
            new[]
            {
                $"Trigger {Activator} {StatType.LightsaberOffenseSecondWindStaminaRestoreBasePercent} 60",
                $"RestoreStamina {Activator} {StatType.LightsaberOffenseSecondWindStaminaRestoreBasePercent} 28",
            },
            "10% plus the Agility score of 6 is capped at 14% of 200");

        _log.Clear();
        _abilityScores[AbilityType.Agility] = -3;
        Stat(Activator, StatType.LightsaberOffenseSecondWindStaminaRestoreMaximumPercent, 0);
        Private(_weaponActivation, "ApplyLightsaberOffenseSecondWind", Activator);
        _log.Last().Should().Be(
            $"RestoreStamina {Activator} {StatType.LightsaberOffenseSecondWindStaminaRestoreBasePercent} 20",
            "a negative score adds nothing and no maximum caps nothing");

        _log.Clear();
        Stat(Activator, StatType.LightsaberOffenseSecondWindScalingAbility, 0);
        _abilityScores[AbilityType.Agility] = 6;
        Private(_weaponActivation, "ApplyLightsaberOffenseSecondWind", Activator);
        _log.Last().Should().Be(
            $"RestoreStamina {Activator} {StatType.LightsaberOffenseSecondWindStaminaRestoreBasePercent} 20",
            "no scaling ability");

        _log.Clear();
        _triggerReady = false;
        Private(_weaponActivation, "ApplyLightsaberOffenseSecondWind", Activator);
        _log.Should().Equal($"Trigger {Activator} {StatType.LightsaberOffenseSecondWindStaminaRestoreBasePercent} 60");

        _log.Clear();
        _triggerReady = true;
        _stamina[Activator] = 61;
        Private(_weaponActivation, "ApplyLightsaberOffenseSecondWind", Activator);
        _maxStamina[Activator] = 0;
        _stamina[Activator] = 0;
        Private(_weaponActivation, "ApplyLightsaberOffenseSecondWind", Activator);
        _maxStamina[Activator] = 200;
        Stat(Activator, StatType.LightsaberOffenseSecondWindStaminaRestoreBasePercent, 0);
        Private(_weaponActivation, "ApplyLightsaberOffenseSecondWind", Activator);
        Stat(Activator, StatType.LightsaberOffenseSecondWindStaminaRestoreBasePercent, 10);
        Stat(Activator, StatType.LightsaberOffenseSecondWindThresholdPercent, 0);
        Private(_weaponActivation, "ApplyLightsaberOffenseSecondWind", Activator);
        _log.Should().BeEmpty("above the threshold, no maximum stamina, no base percent, then no threshold");
    }

    [Test]
    public void GuardiansInfluenceGrantsItsDeflectionToNearbyPartyMembersButNotTheActivator()
    {
        _inShape.AddRange(new[] { Activator, Ally, Bystander });
        _party.Add(Ally);
        _grantedDeflections[StatType.LightsaberDefenseGuardiansInfluenceAttackDeflection] = StatType.MeleeDeflection;

        Private(_weaponActivation, "ApplyLightsaberDefenseActivatedEffects", Activator);
        _log.Should().BeEmpty("no deflection");

        Stat(Activator, StatType.LightsaberDefenseGuardiansInfluenceAttackDeflection, 5);
        Private(_weaponActivation, "ApplyLightsaberDefenseActivatedEffects", Activator);
        _log.Should().Equal($"Replace {Ally} {StatType.MeleeDeflection} 5 12 {StatType.LightsaberDefenseGuardiansInfluenceAttackDeflection}");

        _log.Clear();
        _grantedDeflections.Clear();
        Private(_weaponActivation, "ApplyLightsaberDefenseActivatedEffects", Activator);
        _log.Should().BeEmpty("no granted deflection type");
    }

    [Test]
    public void TheWardGrantsEachConfiguredDefenseAndItsTargetCopyForTheCategory()
    {
        var ability = Hostile();
        Stat(Activator, StatType.WardAbilityForceDefensePercentAdjustment, 3);
        Stat(Activator, StatType.WardAbilityDefenseDurationSeconds, 6);

        Private(_weaponActivation, "ApplyLightsaberWardActivatedEffects", Activator, ability);
        _log.Should().BeEmpty("the category does not match");

        _matchingPerkCategories.Add(StatType.WardAbilityDefenseCategoryId);
        Private(_weaponActivation, "ApplyLightsaberWardActivatedEffects", Activator, ability);
        _log.Should().Equal(
            $"Replace {Activator} {StatType.ForceDefensePercentAdjustment} 3 6 {StatType.WardAbilityForceDefensePercentAdjustment}",
            $"Replace {Activator} {StatType.WardTargetForceDefensePercentAdjustment} 3 6 {StatType.WardAbilityForceDefensePercentAdjustment}");

        _log.Clear();
        Stat(Activator, StatType.WardAbilityForceDefensePercentAdjustment, 0);
        Private(_weaponActivation, "ApplyLightsaberWardActivatedEffects", Activator, ability);
        Stat(Activator, StatType.WardAbilityDefensePercentAdjustment, 2);
        Stat(Activator, StatType.WardAbilityDefenseDurationSeconds, 0);
        Private(_weaponActivation, "ApplyLightsaberWardActivatedEffects", Activator, ability);
        _log.Should().BeEmpty("no defense, then no duration");
    }

    // ------------------------------------------------------------------ construction

    [Test]
    public void TheConstructorOnlyAssignsAndTheContainerBuildsOneInstanceWithTheConvertedParts()
    {
        _log.Should().BeEmpty();
        _asked.Should().BeEmpty();
        _engine.Calls.Should().BeEmpty("the constructor makes no engine call");

        var part = _facades.Get<ICombatAbilityWeaponActivationService>();
        part.Should().BeOfType<CombatAbilityWeaponActivationService>();
        _facades.Get<ICombatAbilityWeaponActivationService>().Should().BeSameAs(part);
        const BindingFlags Instance = BindingFlags.NonPublic | BindingFlags.Instance;
        typeof(CombatAbilityWeaponActivationService).GetField("_offenseEffects", Instance)!.GetValue(part)
            .Should().BeSameAs(_facades.Get<ICombatAbilityOffenseEffectsService>());
        typeof(CombatAbilityWeaponActivationService).GetField("_enmityChange", Instance)!.GetValue(part)
            .Should().BeSameAs(_facades.Get<IEnmityChangeService>());
        typeof(CombatAbilityWeaponActivationService).GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            .Should().BeEmpty("the part holds no static state");
        typeof(CombatAbilityWeaponActivationService).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Should().OnlyContain(field => field.IsInitOnly, "the part holds no state of its own");
    }
}
