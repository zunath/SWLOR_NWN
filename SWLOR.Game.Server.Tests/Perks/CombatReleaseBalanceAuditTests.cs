using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Tests.Perks;

public class CombatReleaseBalanceAuditTests
{
    private const int SkillPointCap = 400;
    private const int DefaultWeaponDeflectionCap = 50;

    private static readonly PerkCategoryType[] WeaponPackages =
    {
        PerkCategoryType.VibrobladeDefense,
        PerkCategoryType.VibrobladeOffense,
        PerkCategoryType.VibroknifeShadow,
        PerkCategoryType.VibroknifeSaboteur,
        PerkCategoryType.LightsaberDefense,
        PerkCategoryType.LightsaberOffense,
        PerkCategoryType.HeavyVibrobladeDefense,
        PerkCategoryType.HeavyVibrobladeOffense,
        PerkCategoryType.SpearDamage,
        PerkCategoryType.SpearDisabler,
        PerkCategoryType.TwinBladeCyclone,
        PerkCategoryType.TwinBladeDuelist,
        PerkCategoryType.SaberstaffConduit,
        PerkCategoryType.SaberstaffTempest,
        PerkCategoryType.KatarIronGuard,
        PerkCategoryType.KatarVenomCurrent,
        PerkCategoryType.StaffCrusher,
        PerkCategoryType.StaffSentinel,
        PerkCategoryType.PistolGunslinger,
        PerkCategoryType.PistolSkirmisher,
        PerkCategoryType.RifleMarksman,
        PerkCategoryType.RiflePacification,
        PerkCategoryType.ThrowingBombardier,
        PerkCategoryType.ThrowingDeadeye
    };

    private static readonly PerkCategoryType[] SupportPackages =
    {
        PerkCategoryType.ForceAlter,
        PerkCategoryType.ForceControl,
        PerkCategoryType.ForceSense,
        PerkCategoryType.General,
        PerkCategoryType.Leadership,
        PerkCategoryType.LeadershipVanguardCommand,
        PerkCategoryType.LeadershipFieldSteward,
        PerkCategoryType.DevicesGrenadier,
        PerkCategoryType.DevicesFieldEngineer,
        PerkCategoryType.DevicesFieldSupport,
        PerkCategoryType.DevicesAssaultGadgets,
        PerkCategoryType.FirstAidTraumaMedic,
        PerkCategoryType.FirstAidCombatPharmacology,
        PerkCategoryType.BeastMasteryTraining,
        PerkCategoryType.BeastMasteryIncubation,
        PerkCategoryType.BeastDamage,
        PerkCategoryType.BeastTank,
        PerkCategoryType.BeastBalanced,
        PerkCategoryType.BeastBruiser,
        PerkCategoryType.BeastEvasion,
        PerkCategoryType.BeastForce,
        PerkCategoryType.Mimicry,
        PerkCategoryType.EspionageInfiltrator,
        PerkCategoryType.EspionageSaboteur
    };

    private static readonly PerkCategoryType[] UtilityPackages =
    {
        PerkCategoryType.EspionageTradecraft
    };

    private static readonly StatType[] DirectDamagePercentStats =
    {
        StatType.AttackPercentAdjustment,
        StatType.ForceAttackPercentAdjustment,
        StatType.DamageDealtPercentAdjustment,
        StatType.WeaponAndForceDamageDealtPercentAdjustment,
        StatType.TargetLowHPDamagePercentAdjustment,
        StatType.TargetLowHPStatusDamagePercentAdjustment,
        StatType.DamageToSunderedTargetPercentAdjustment,
        StatType.DamageToBleedingTargetPercentAdjustment,
        StatType.DamageToDebuffedTargetPercentAdjustment,
        StatType.DamageToSourceAppliedStatusTargetPercentAdjustment,
        StatType.AbilityDamageToSourceAppliedStatusTargetPercentAdjustment,
        StatType.DamageToPoisonedOrDisorientedTargetPercentAdjustment,
        StatType.DamageToWeakenedOrHamstringTargetPercentAdjustment,
        StatType.DamageToControlTargetPercentAdjustment,
        StatType.DamageToDisorientedDazedTargetPercentAdjustment,
        StatType.RangedDamageToNearbyTargetPercentAdjustment,
        StatType.HighFPAndStaminaAttackPercentAdjustment,
        StatType.HighFPAndStaminaAbilityDamagePercentAdjustment,
        StatType.AttackToBleedingTargetPercentAdjustment,
        StatType.TwinBladeSingleTargetAbilityDamagePercentAdjustment,
        StatType.TwinBladeAreaAbilityDamagePercentAdjustment,
        StatType.ThrowingAreaAbilityDamagePercentAdjustment,
        StatType.SingleTargetPhysicalAbilityDamagePercentAdjustment,
        StatType.SkillAreaAbilityDamagePercentAdjustment,
        StatType.SkillAbilityDamagePercentAdjustment,
        StatType.DarkForceTargetLowHPDamagePercentAdjustment,
        StatType.BeaconPulseDamagePercentAdjustment,
        StatType.AssaultGadgetDamagePercentAdjustment,
        StatType.SideAttackDamagePercentAdjustment,
        StatType.RepeatedTargetDamagePercentPerHit,
        StatType.DamageToStatusSourcePercentAdjustment,
        StatType.HitPointSpendAbilityDamagePercentAdjustment,
        StatType.LowHPAttackPercentAdjustment,
        StatType.StatusAppliedSelfForceAttackPercentAdjustment,
        StatType.HostileAbilityForceAttackPercentPerStack,
        StatType.AreaAbilityAfterDeflectionDamagePercentAdjustment,
        StatType.BackAttackDamagePercentAdjustment,
        StatType.PoisonBonus,
        StatType.TrapBonus,
        StatType.MimicryPotencyPercent
    };

    private static readonly StatType[] FlatDamageStats =
    {
        StatType.AutoAttackDamageBonus,
        StatType.NextAutoAttackDamageBonus,
        StatType.NextAbilityDamageBonus,
        StatType.CriticalNextAbilityDamageBonus,
        StatType.AbilityDamageFlatAdjustment,
        StatType.GuardedHitNextSkillAbilityDamageBonus,
        StatType.NextSkillAbilityDamageBonus,
        StatType.DeflectionNextSkillAbilityDamageBonus,
        StatType.OpeningAutoAttackDamageBonus,
        StatType.CurrentAutoAttackDamageBonus,
        StatType.AbilityUsedNextSkillAutoAttackDamageBonus,
        StatType.NextSkillAutoAttackDamageBonus,
        StatType.RiotBladeSecondaryDamageBonus,
        StatType.SavageCleaveSecondaryDamageBonus,
        StatType.EarthshatterDamageBonus,
        StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageBonus,
        StatType.GuardedHitPulseDMG,
        StatType.GuardedHitNextAttackDMGBonus,
        StatType.GuardedHitSecondaryNextAttackDMGBonus,
        StatType.KatarVenomCurrentSecondStrikeDamageBonus,
        StatType.LightsaberOffenseAreaDamageBonus,
        StatType.LightsaberOffenseDebuffedTargetDamageBonus,
        StatType.LightsaberOffenseSingleTargetSplashDamage,
        StatType.LightsaberOffenseSurgeStrikeDamageBonus,
        StatType.PistolDamageToDisorientedKnockdownOrTranquilizedTargetBonus,
        StatType.PistolSkirmisherRicochetDamageBonus,
        StatType.SaberstaffConduitFlareDamageBonus,
        StatType.SpearDamageBreachStrike,
        StatType.StaffCrusherFinisherDamageBonus,
        StatType.ThrowingBombardierClusterStormDamageBonus,
        StatType.ThrowingBombardierSaturationTossDamage,
        StatType.ThrowingDeadeyeRicochetDamageBonus,
        StatType.TwinBladeDuelistReversalCutDamageBonus,
        StatType.GuardedHitNextSkillAbilityExposedDamageBonus,
        StatType.RangedAttackDamageFlatAdjustment,
        StatType.HighFPAndStaminaAbilityDamageBonus,
        StatType.AbilityDamageToBleedingTargetBonus,
        StatType.StatusAppliedNextSkillAbilityDamageBonus,
        StatType.StatusAppliedNextAttackDamageBonus,
        StatType.AvoidedAttackNextSkillAbilityDamageBonus,
        StatType.DamageTakenNextSkillAbilityDamageBonus,
        StatType.CostlyAbilityDamageBonus,
        StatType.SameTargetPressureWeaponAbilityDamageBonus,
        StatType.AreaAbilityFragmentationDamage,
        StatType.RepeatedTargetDamageBonusPerHit,
        StatType.MeleeRepeatedTargetDamageBonusPerHit,
        StatType.RangedRepeatedTargetDamageBonusPerHit
    };

    private static readonly StatType[] CriticalRateStats =
    {
        StatType.CriticalRatePercentAdjustment,
        StatType.RangedCriticalRatePercentAdjustment,
        StatType.StaffCriticalRatePercentAdjustment,
        StatType.DeflectionNextSkillAbilityCriticalRatePercentAdjustment,
        StatType.NextSkillAbilityCriticalRatePercentAdjustment,
        StatType.ThrowingAbilityCriticalRateToBleedingOrDisorientedTargetPercentAdjustment,
        StatType.CriticalRateAgainstTargetNotFacingAttackerPercentAdjustment,
        StatType.CriticalRateAgainstSunderedTargetPercentAdjustment,
        StatType.OpeningAutoAttackCriticalRatePercentAdjustment,
        StatType.AbilityCriticalRatePercentAdjustment,
        StatType.BeaconPulseCriticalRatePercentAdjustment,
        StatType.AssaultGadgetCriticalRatePercentAdjustment,
        StatType.DeflectionNextAutoAttackCriticalRatePercentAdjustment,
        StatType.NextAutoAttackCriticalRatePercentAdjustment,
        StatType.SideAttackCriticalRatePercentAdjustment,
        StatType.LightsaberOffenseCenteringAccuracyPercent,
        StatType.LowHPCriticalRatePercentAdjustment,
        StatType.StatusAppliedNextSkillAbilityCriticalRatePercentAdjustment,
        StatType.TargetStatusCriticalRatePercentAdjustment,
        StatType.RangedAutoAttackCycleCriticalRatePercentAdjustment,
        StatType.NonCriticalAbilityNextSkillAbilityCriticalRatePercentAdjustment,
        StatType.BackAttackCriticalRatePercentAdjustment,
        StatType.RangedAbilityLongRangeCriticalRatePercentAdjustment
    };

    private static readonly StatType[] CriticalDamageStats =
    {
        StatType.CriticalDamagePercentAdjustment,
        StatType.StaffCriticalDamagePercentAdjustment,
        StatType.RangedCriticalDamagePercentAdjustment,
        StatType.CriticalDamageHighHPTargetPercentAdjustment,
        StatType.CriticalDamageTargetStatusPercentAdjustment,
        StatType.IdleSkillAbilityCriticalDamagePercentAdjustment,
        StatType.OpeningAutoAttackCriticalDamagePercentAdjustment
    };

    private static readonly StatType[] HasteStats =
    {
        StatType.AttackDelayReductionPercent,
        StatType.OffhandAttackDelayReductionPercent,
        StatType.DefeatedEnemyAttackDelayReductionPercent,
        StatType.AreaAbilityHastePercentAdjustment,
        StatType.AreaAbilityHastePerStack,
        StatType.DamageDealtAttackDelayReductionPercent,
        StatType.PredatorsMarkHastePercentPerStack,
        StatType.KatarToxicRushHastePercentPerStack,
        StatType.SideAttackDelayReductionPercent,
        StatType.AbilityRestoredBothResourcesHastePercentAdjustment,
        StatType.StatusAppliedSelfHastePercentAdjustment,
        StatType.AbilityRestoredFPHastePercentAdjustment,
        StatType.CriticalHitSelfHastePercentAdjustment,
        StatType.HostileAbilityRecastDelayPercentAdjustment
    };

    private static readonly StatType[] DefenseStats =
    {
        StatType.DefensePercentAdjustment,
        StatType.PhysicalDefensePercentAdjustment,
        StatType.ForceDefensePercentAdjustment,
        StatType.EvasionPercentAdjustment,
        StatType.RangedEvasionPercentAdjustment,
        StatType.DeflectionEvasionPercentAdjustment,
        StatType.DeflectionDefensePercentAdjustment,
        StatType.DeflectionForceDefensePercentAdjustment,
        StatType.LowHPPhysicalDefensePercentAdjustment,
        StatType.LowHPEvasionPercentAdjustment,
        StatType.LowHPTemporaryHPPercent,
        StatType.LowHPNoSaveTemporaryHPPercent,
        StatType.FatalDamageTemporaryHPPercent,
        StatType.LightGuardianPowerAttackDeflection,
        StatType.LightGuardianTemporaryHPReflectiveBarrier,
        StatType.DeviceShieldTemporaryHPPercentAdjustment,
        StatType.FieldSupportPhysicalDefensePercent,
        StatType.FieldSupportPhysicalAndForceDamageReductionPercent,
        StatType.IncomingCriticalHitDowngradeToMinimumDamage,
        StatType.PhysicalDamageImmunity,
        StatType.PhysicalDamageTakenPercentAdjustment,
        StatType.ForceDamageTakenPercentAdjustment,
        StatType.RangedPhysicalDamageTakenPercentAdjustment,
        StatType.DamageTakenFromStatusSourcePercentAdjustment,
        StatType.DamageTakenFromStatusSourcePartyPercentAdjustment,
        StatType.PhysicalAbilityDamageTakenPercentAdjustment,
        StatType.HitPointSpendTemporaryHPPercentOfSpentHP,
        StatType.AvoidedAttackAccuracyPercentAdjustment,
        StatType.StatusAppliedSelfAttackDeflection,
        StatType.StatusAppliedSelfDefensePercentAdjustment,
        StatType.StatusAppliedSelfEvasionPercentAdjustment,
        StatType.AreaAbilityUsedEvasionPercentAdjustment,
        StatType.AbilityUsedRangedDeflection,
        StatType.CriticalHitSelfEvasionPercentAdjustment,
        StatType.AbilityUsedNearbyAllyDefensePercentAdjustment,
        StatType.AbilityUsedNearbyAllyForceDefensePercentAdjustment,
        StatType.WardTargetPhysicalDefensePercentAdjustment,
        StatType.WardTargetForceDefensePercentAdjustment,
        StatType.WardAbilityDefensePercentAdjustment,
        StatType.WardAbilityForceDefensePercentAdjustment,
        StatType.ForceDamageTakenForceDefense,
        StatType.AbilityUsedMovementSpeedPercentAdjustment,
        StatType.DeflectionNearbyAllyGuard,
        StatType.RangedAbilityHitNearTargetDamageDealtPercentAdjustment
    };

    private static readonly StatType[] SustainStats =
    {
        StatType.DamageDealtHPPercentRestore,
        StatType.PhysicalDamageDealtHPPercentRestore,
        StatType.CriticalHPPercentOfDamageRestore,
        StatType.DefeatedEnemyHPPercentRestore,
        StatType.DarkForceDamageHPPercentRestore,
        StatType.LowHPDamageDealtHPPercentRestore,
        StatType.HealingReceivedPercentAdjustment,
        StatType.OutgoingAbilityHealingPercentAdjustment,
        StatType.HPRegen,
        StatType.FPRegen,
        StatType.StaminaRegen,
        StatType.FPRestorePercentAdjustment,
        StatType.CriticalStaminaRestore,
        StatType.DefeatedEnemyStaminaRestore,
        StatType.DefeatedEnemyFPRestore,
        StatType.AutoAttackStaminaRestore,
        StatType.AutoAttackFPRestore,
        StatType.DamageDealtStaminaRestore,
        StatType.BeastBalancedAbilityStaminaRestore,
        StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreBasePercent,
        StatType.LowFPAndStaminaIntervalFPRestore,
        StatType.LowFPAndStaminaIntervalStaminaRestore,
        StatType.AbilityStaminaCostFPRestorePercent,
        StatType.AbilityFPCostStaminaRestorePercent,
        StatType.CriticalHitSequenceStaminaRestore,
        StatType.HostileAbilityFPRestore,
        StatType.HostileAbilityStaminaRestore,
        StatType.CostlyAbilityHitStaminaRestore,
        StatType.AbilityGrantedAttackDeflectionFPRestore
    };

    private static readonly StatType[] ControlStats =
    {
        StatType.ActivationDelayFlatAdjustment,
        StatType.ForceAbilityActivationDisabled,
        StatType.CriticalTargetFPLossPercentOfDamage,
        StatType.CriticalTargetStaminaLossPercentOfDamage,
        StatType.CriticalTargetDefensePercentAdjustment,
        StatType.CriticalTargetEvasionPercentAdjustment,
        StatType.AutoAttackTargetAccuracyPercentAdjustment,
        StatType.OutgoingDebuffDurationPercentAdjustment,
        StatType.OutgoingControlDurationPercentAdjustment,
        StatType.OutgoingForceDisruptionDurationPercentAdjustment,
        StatType.OutgoingForceDisruptionForceDefensePercentAdjustment,
        StatType.OutgoingBleedingDurationBonusSeconds,
        StatType.OutgoingBleedingDamagePercentAdjustment,
        StatType.OutgoingPoisonAttackPercentAdjustment,
        StatType.OutgoingDisorientedAttackPercentAdjustment,
        StatType.OutgoingDisorientedEvasionPercentAdjustment,
        StatType.DamageDealtForceErosionFPLossPerTick,
        StatType.DamageDealtForceErosionStaminaLossPerTick,
        StatType.AbilityDefenseIgnorePercentAdjustment,
        StatType.AbilityDefenseIgnoreExposedOrSunderedPercentAdjustment,
        StatType.CriticalNextSkillAbilityDefenseIgnorePercentAdjustment,
        StatType.NextSkillAbilityDefenseIgnorePercentAdjustment,
        StatType.FieldEngineerAreaEvasionPenaltyPercent,
        StatType.SparkLightningPressureEvasionPenaltyPercent,
        StatType.BleedingTargetAbilityBleedDurationExtensionSeconds,
        StatType.BleedingTargetAbilityBleedSpreadChance,
        StatType.AutoAttackSuppressionStackChance,
        StatType.RangedHitSuppressionStackDurationSeconds,
        StatType.AutoAttackSuppressionStackEvasionPenaltyPercent,
        StatType.RangedHitSuppressionStackEvasionPenaltyPercent,
        StatType.SuppressionStackEvasionPenaltyPercentAdjustment,
        StatType.HitChanceAgainstSunderedTargetPercentAdjustment,
        StatType.AbilityResourceDrainFoggyMindFP,
        StatType.AbilityResourceDrainFoggyMindStamina,
        StatType.AbilityDefenseIgnoreForceDisruptionOrFoggyMindPercentAdjustment,
        StatType.StatusAppliedTargetPhysicalDefensePercentAdjustment,
        StatType.StatusAppliedTargetAccuracyPercentAdjustment,
        StatType.AbilityTargetStatusPhysicalDefensePercentAdjustment,
        StatType.RangedAttackAccuracyAgainstSuppressionStackPercentAdjustment,
        StatType.SuppressionStackDamageDealtPercentAdjustment,
        StatType.DefenseIgnoreHitPhysicalDefensePercentAdjustment,
        StatType.AreaAbilityTargetHitSequenceExposedDurationSeconds,
        StatType.IdleStatusDurationPercentAdjustment,
        StatType.RangedAbilityTargetDefenseReductionPercent
    };

    [Test]
    public void CuratedReleaseArchetypes_AreLegalAndStayWithinHardReleaseGates()
    {
        var packages = BuildPackages();
        var archetypes = BuildCuratedArchetypes(packages);
        var failures = new List<string>();

        foreach (var archetype in archetypes)
        {
            if (archetype.Profile.Cost > SkillPointCap)
            {
                failures.Add($"{archetype.Name}: costs {archetype.Profile.Cost} SP, above {SkillPointCap}.");
                continue;
            }

            if (archetype.Profile.MeleeDeflection >= DefaultWeaponDeflectionCap)
            {
                failures.Add($"{archetype.Name}: permanent Melee Deflection is {archetype.Profile.MeleeDeflection}; cap access must stay temporary.");
            }

            if (archetype.Profile.RangedDeflection >= DefaultWeaponDeflectionCap)
            {
                failures.Add($"{archetype.Name}: permanent Ranged Deflection is {archetype.Profile.RangedDeflection}; cap access must stay temporary.");
            }

            if (IsCompoundReleaseBlocker(archetype.Profile))
            {
                failures.Add($"{archetype.Name}: compound budget blocker. {Describe(archetype.Profile)}");
            }
        }

        TestContext.Out.WriteLine("Curated archetype budget scan:");
        foreach (var archetype in archetypes.OrderByDescending(x => x.Profile.OffenseScore))
        {
            TestContext.Out.WriteLine($"{archetype.Name}: {Describe(archetype.Profile)}");
        }

        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
    }

    [Test]
    public void FullPackageEnumeration_ReportsLegalOutliersWithoutPermanentDeflectionCapAccess()
    {
        var packages = BuildPackages()
            .Values
            .Where(x => WeaponPackages.Contains(x.Category) ||
                        SupportPackages.Contains(x.Category) ||
                        UtilityPackages.Contains(x.Category))
            .Where(x => x.Cost > 0)
            .OrderBy(x => x.Cost)
            .ThenBy(x => x.Name)
            .ToArray();

        var legalProfiles = EnumerateLegalFrontierProfiles(packages, SkillPointCap).ToArray();
        legalProfiles.Should().NotBeEmpty("the release audit needs legal package combinations to inspect");

        var capViolations = legalProfiles
            .Where(x => x.MeleeDeflection >= DefaultWeaponDeflectionCap ||
                        x.RangedDeflection >= DefaultWeaponDeflectionCap)
            .OrderByDescending(x => Math.Max(x.MeleeDeflection, x.RangedDeflection))
            .ThenByDescending(x => x.OffenseScore)
            .Take(20)
            .ToArray();

        var compoundOutliers = legalProfiles
            .Where(IsCompoundReleaseBlocker)
            .OrderByDescending(x => x.OffenseScore)
            .ThenByDescending(x => x.SustainScore)
            .ThenByDescending(x => x.DefenseScore)
            .Take(20)
            .ToArray();

        TestContext.Out.WriteLine("Top legal offense profiles:");
        foreach (var profile in legalProfiles.OrderByDescending(x => x.OffenseScore).Take(12))
        {
            TestContext.Out.WriteLine(Describe(profile));
        }

        TestContext.Out.WriteLine("Top legal sustain profiles:");
        foreach (var profile in legalProfiles.OrderByDescending(x => x.SustainScore).Take(12))
        {
            TestContext.Out.WriteLine(Describe(profile));
        }

        TestContext.Out.WriteLine("Top compound outlier profiles for manual review:");
        foreach (var profile in compoundOutliers)
        {
            TestContext.Out.WriteLine(Describe(profile));
        }

        capViolations.Should().BeEmpty(string.Join(Environment.NewLine, capViolations.Select(Describe)));
    }

    [Test]
    public void CrossSkillSupportFrontier_HasNoCompoundGodProfiles()
    {
        var packages = BuildPackages();
        var supportPackages = SupportPackages
            .Concat(UtilityPackages)
            .Select(category => packages[category])
            .Where(package => package.Cost > 0)
            .OrderBy(package => package.Cost)
            .ThenBy(package => package.Name)
            .ToArray();
        var failures = new List<string>();

        foreach (var weaponCategory in WeaponPackages)
        {
            var weapon = packages[weaponCategory];
            var weaponOnly = Combine(new[] { weapon });
            if (IsCompoundReleaseBlocker(weaponOnly))
                failures.Add(Describe(weaponOnly));

            foreach (var supportProfile in EnumerateLegalFrontierProfiles(
                         supportPackages,
                         SkillPointCap - weapon.Cost))
            {
                var combined = AddPackage(supportProfile, weapon);
                if (combined.Cost <= SkillPointCap && IsCompoundReleaseBlocker(combined))
                    failures.Add(Describe(combined));
            }
        }

        failures
            .Distinct(StringComparer.Ordinal)
            .Should()
            .BeEmpty(
                "every active weapon package combined with the full legal cross-skill support frontier must retain a meaningful offense/defense/sustain/control tradeoff");
    }

    [Test]
    public void ReleaseAuditScope_IncludesEveryWeaponAndSupportPackage()
    {
        var packages = BuildPackages();
        var missing = WeaponPackages
            .Concat(SupportPackages)
            .Concat(UtilityPackages)
            .Where(category => !packages.ContainsKey(category))
            .ToArray();

        missing.Should().BeEmpty("the release audit must cover weapons, Force, Devices, Leadership, First Aid, Beast Mastery, Mimicry, Espionage, and Armor");
    }

    [Test]
    public void ReleaseAuditScoring_IncludesMimicryAndEspionageCombatPayloads()
    {
        var packages = BuildPackages();

        packages[PerkCategoryType.Mimicry].OffenseScore.Should().Be(15,
            "max-rank Combat Analyzer grants 15% Mimicry potency");
        packages[PerkCategoryType.EspionageInfiltrator].OffenseScore.Should().Be(13,
            "max-rank Back Attack grants 8% damage and 5% critical rate");
        packages[PerkCategoryType.EspionageSaboteur].OffenseScore.Should().Be(40,
            "Saboteur contributes 30% Venom damage and 10% trap damage across distinct payloads");
        packages[PerkCategoryType.EspionageTradecraft].OffenseScore.Should().Be(0,
            "Tradecraft is explicitly audited non-combat utility");
        packages[PerkCategoryType.EspionageTradecraft].SupportPackageCount.Should().Be(0,
            "non-combat disguise utility must not inflate compound combat-support scores");

        packages[PerkCategoryType.RifleMarksman].Stats[StatType.RangedAbilityLongRangeCriticalRatePercentAdjustment]
            .Should().Be(8);
        packages[PerkCategoryType.RifleMarksman].Stats[StatType.RangedAbilityTargetDefenseReductionPercent]
            .Should().Be(10);
        CriticalRateStats.Should().Contain(StatType.RangedAbilityLongRangeCriticalRatePercentAdjustment);
        ControlStats.Should().Contain(StatType.RangedAbilityTargetDefenseReductionPercent);
    }

    private static bool IsCompoundReleaseBlocker(ReleaseProfile profile)
    {
        if (profile.OffenseScore < 175)
            return false;

        return profile.SustainScore >= 80 ||
               profile.DefenseScore >= 160 ||
               profile.ControlScore >= 110 ||
               profile.SupportPackageCount >= 3;
    }

    private static IReadOnlyDictionary<PerkCategoryType, AuditPackage> BuildPackages()
    {
        var packages = BuildPerksWithout2daLookup()
            .Where(x => x.Detail.IsActive)
            .GroupBy(x => x.Detail.Category)
            .ToDictionary(
                x => x.Key,
                x => BuildPackage(x.Key, x.ToArray()));

        return packages;
    }

    private static AuditPackage BuildPackage(PerkCategoryType category, IReadOnlyCollection<PerkRecord> perks)
    {
        var cost = 0;
        var stats = new Dictionary<StatType, int>();
        foreach (var perk in perks)
        {
            cost += perk.Detail.PerkLevels.Values.Sum(x => x.Price);

            var maxLevel = perk.Detail.PerkLevels
                .OrderByDescending(x => x.Key)
                .First()
                .Value;

            foreach (var statBonus in maxLevel.StatBonuses)
            {
                var value = ResolveAuditStatValue(perk.Type, statBonus);
                stats[statBonus.Stat] = stats.GetValueOrDefault(statBonus.Stat) + value;
            }
        }

        return new AuditPackage(
            category,
            GetCategoryName(category),
            cost,
            stats,
            Sum(stats, StatType.MeleeDeflection),
            Sum(stats, StatType.RangedDeflection),
            Sum(stats, StatType.ShieldDeflection),
            Sum(stats, StatType.Guard),
            ScoreOffense(stats),
            ScoreDefense(stats),
            ScoreSustain(stats),
            ScoreControl(stats),
            SupportPackages.Contains(category) ? 1 : 0);
    }

    private static int ResolveAuditStatValue(PerkType perkType, PerkStatBonus statBonus)
    {
        var conditionalValue = (perkType, statBonus.Stat) switch
        {
            (PerkType.DualWield, StatType.OffhandAttackDelayReductionPercent) => 30,
            (PerkType.RapidShot, StatType.AttackDelayReductionPercent) => 30,
            (PerkType.RapidShot, StatType.AutoAttackStaminaRestoreChance) => 10,
            (PerkType.RapidShot, StatType.AutoAttackStaminaRestore) => 2,
            (PerkType.GuardiansRiposte, StatType.DeflectionNextSkillAbilityDamageBonus) => 10,
            (PerkType.GuardiansRiposte, StatType.DeflectionNextSkillAbilityDamageBonusWindowSeconds) => 18,
            (PerkType.Alacrity, StatType.ShieldDeflectionStaminaRestore) => 4,
            (PerkType.Alacrity, StatType.ShieldDeflectionStaminaRestoreCooldownSeconds) => 6,
            (PerkType.Bulwark, StatType.ShieldDeflection) => 35,
            (PerkType.ShieldTraining, StatType.DeflectionEvasionPercentAdjustment) => 3,
            (PerkType.ShieldTraining, StatType.DeflectionEvasionEnmityPercentAdjustment) => 3,
            (PerkType.ShieldTraining, StatType.DeflectionRecastReductionGroupId) => (int)RecastGroup.ShieldBash,
            (PerkType.ShieldTraining, StatType.DeflectionRecastReductionSeconds) => 2,
            (PerkType.ConduitTraining, StatType.AutoAttackFPRestore) => 3,
            (PerkType.ConduitTraining, StatType.AutoAttackFPRestoreCooldownSeconds) => 4,
            (PerkType.CriticalWard, StatType.IncomingCriticalHitDowngradeCooldownMilliseconds) => 12000,
            (PerkType.UnbreakableWill, StatType.MeleeDeflection) => 8,
            (PerkType.VampiricFury, StatType.CriticalHPPercentOfDamageRestore) => 25,
            (PerkType.BodyguardsResolve, StatType.DamageTakenPercentAdjustment) => -10,
            _ => 0
        };

        if (conditionalValue != 0)
            return conditionalValue;

        return statBonus.Calculate(0);
    }

    private static IReadOnlyCollection<ReleaseArchetype> BuildCuratedArchetypes(IReadOnlyDictionary<PerkCategoryType, AuditPackage> packages)
    {
        return new[]
        {
            Archetype(packages, "Single weapon specialist", PerkCategoryType.VibrobladeOffense),
            Archetype(packages, "Two weapon-line hybrid", PerkCategoryType.VibrobladeOffense, PerkCategoryType.HeavyVibrobladeOffense),
            Archetype(packages, "Three weapon-line combat maximizer", PerkCategoryType.HeavyVibrobladeOffense, PerkCategoryType.SpearDamage, PerkCategoryType.StaffCrusher),
            Archetype(packages, "Weapon plus Leadership", PerkCategoryType.VibrobladeOffense, PerkCategoryType.LeadershipVanguardCommand),
            Archetype(packages, "Weapon plus Force support", PerkCategoryType.LightsaberOffense, PerkCategoryType.ForceControl, PerkCategoryType.ForceSense),
            Archetype(packages, "Weapon plus Devices support", PerkCategoryType.RifleMarksman, PerkCategoryType.DevicesFieldSupport),
            Archetype(packages, "Weapon plus First Aid sustain", PerkCategoryType.HeavyVibrobladeOffense, PerkCategoryType.FirstAidTraumaMedic),
            Archetype(packages, "Weapon plus Beast pressure", PerkCategoryType.SpearDamage, PerkCategoryType.BeastDamage),
            Archetype(packages, "Weapon plus Mimicry", PerkCategoryType.VibrobladeOffense, PerkCategoryType.Mimicry),
            Archetype(packages, "Weapon plus Espionage Infiltrator", PerkCategoryType.VibroknifeShadow, PerkCategoryType.EspionageInfiltrator),
            Archetype(packages, "Weapon plus Espionage Saboteur", PerkCategoryType.VibroknifeSaboteur, PerkCategoryType.EspionageSaboteur),
            Archetype(packages, "Poison trap and Mimicry payload stack", PerkCategoryType.VibroknifeSaboteur, PerkCategoryType.EspionageSaboteur, PerkCategoryType.Mimicry, PerkCategoryType.General),
            Archetype(packages, "Stealth burst cross-skill stack", PerkCategoryType.VibroknifeShadow, PerkCategoryType.EspionageInfiltrator, PerkCategoryType.Mimicry, PerkCategoryType.LeadershipVanguardCommand),
            Archetype(packages, "Cross-resource sustain engine", PerkCategoryType.SaberstaffConduit, PerkCategoryType.ForceControl, PerkCategoryType.ForceSense, PerkCategoryType.FirstAidTraumaMedic),
            Archetype(packages, "Damage-healing sustain engine", PerkCategoryType.HeavyVibrobladeOffense, PerkCategoryType.HeavyVibrobladeDefense, PerkCategoryType.SaberstaffConduit, PerkCategoryType.FirstAidTraumaMedic, PerkCategoryType.LeadershipFieldSteward),
            Archetype(packages, "Deflection reflection support stack", PerkCategoryType.LightsaberOffense, PerkCategoryType.StaffSentinel, PerkCategoryType.DevicesFieldSupport, PerkCategoryType.LeadershipFieldSteward),
            Archetype(packages, "Cross-skill control stack", PerkCategoryType.SpearDisabler, PerkCategoryType.RiflePacification, PerkCategoryType.DevicesGrenadier, PerkCategoryType.EspionageSaboteur, PerkCategoryType.Mimicry),
            Archetype(packages, "High-MGT damage stack", PerkCategoryType.HeavyVibrobladeOffense, PerkCategoryType.SpearDamage, PerkCategoryType.StaffCrusher, PerkCategoryType.LeadershipVanguardCommand),
            Archetype(packages, "High-PER crit stack", PerkCategoryType.PistolGunslinger, PerkCategoryType.RifleMarksman, PerkCategoryType.ThrowingDeadeye, PerkCategoryType.LeadershipVanguardCommand),
            Archetype(packages, "Melee Deflection stack", PerkCategoryType.StaffSentinel, PerkCategoryType.TwinBladeDuelist, PerkCategoryType.HeavyVibrobladeDefense),
            Archetype(packages, "Ranged Deflection stack", PerkCategoryType.LightsaberOffense, PerkCategoryType.SaberstaffTempest),
            Archetype(packages, "Shield Deflection stack", PerkCategoryType.VibrobladeDefense, PerkCategoryType.DevicesFieldSupport, PerkCategoryType.LeadershipFieldSteward),
            Archetype(packages, "Guard tank stack", PerkCategoryType.KatarIronGuard, PerkCategoryType.HeavyVibrobladeDefense, PerkCategoryType.LeadershipFieldSteward),
            Archetype(packages, "Sustain tank", PerkCategoryType.HeavyVibrobladeOffense, PerkCategoryType.HeavyVibrobladeDefense, PerkCategoryType.FirstAidTraumaMedic, PerkCategoryType.LeadershipFieldSteward),
            Archetype(packages, "High-control/debuff stack", PerkCategoryType.SpearDisabler, PerkCategoryType.VibroknifeSaboteur, PerkCategoryType.RiflePacification, PerkCategoryType.DevicesGrenadier),
            Archetype(packages, "Positional low-uptime build", PerkCategoryType.SpearDamage, PerkCategoryType.VibroknifeShadow),
            Archetype(packages, "Positional high-uptime build", PerkCategoryType.SpearDamage, PerkCategoryType.VibroknifeShadow, PerkCategoryType.KatarVenomCurrent)
        };
    }

    private static ReleaseArchetype Archetype(
        IReadOnlyDictionary<PerkCategoryType, AuditPackage> packages,
        string name,
        params PerkCategoryType[] categories)
    {
        return new ReleaseArchetype(name, Combine(packages, name, categories));
    }

    private static IEnumerable<ReleaseProfile> EnumerateLegalFrontierProfiles(IReadOnlyList<AuditPackage> packages, int maximumCost)
    {
        var emptyProfile = new ReleaseProfile(
            "Empty",
            Array.Empty<string>(),
            0,
            new Dictionary<StatType, int>(),
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0);
        var frontier = new Dictionary<EnumerationKey, ReleaseProfile>
        {
            [GetEnumerationKey(emptyProfile)] = emptyProfile
        };

        foreach (var package in packages)
        {
            var additions = new List<ReleaseProfile>();
            foreach (var profile in frontier.Values)
            {
                if (profile.Cost + package.Cost > maximumCost)
                    continue;

                additions.Add(AddPackage(profile, package));
            }

            foreach (var profile in additions)
            {
                var key = GetEnumerationKey(profile);
                if (!frontier.TryGetValue(key, out var existing) ||
                    profile.Cost < existing.Cost ||
                    profile.Cost == existing.Cost && profile.PackageNames.Count < existing.PackageNames.Count)
                {
                    frontier[key] = profile;
                }
            }
        }

        return frontier.Values.Where(x => x.Cost > 0);
    }

    private static ReleaseProfile AddPackage(ReleaseProfile profile, AuditPackage package)
    {
        var packageNames = profile.PackageNames
            .Concat(new[] { package.Name })
            .OrderBy(x => x)
            .ToArray();

        return new ReleaseProfile(
            string.Join(" + ", packageNames),
            packageNames,
            profile.Cost + package.Cost,
            new Dictionary<StatType, int>(),
            profile.MeleeDeflection + package.MeleeDeflection,
            profile.RangedDeflection + package.RangedDeflection,
            profile.ShieldDeflection + package.ShieldDeflection,
            profile.Guard + package.Guard,
            profile.OffenseScore + package.OffenseScore,
            profile.DefenseScore + package.DefenseScore,
            profile.SustainScore + package.SustainScore,
            profile.ControlScore + package.ControlScore,
            profile.SupportPackageCount + package.SupportPackageCount);
    }

    private static EnumerationKey GetEnumerationKey(ReleaseProfile profile)
    {
        return new EnumerationKey(
            Math.Min(profile.MeleeDeflection, DefaultWeaponDeflectionCap),
            Math.Min(profile.RangedDeflection, DefaultWeaponDeflectionCap),
            Math.Min(profile.OffenseScore, 175),
            Math.Min(profile.DefenseScore, 160),
            Math.Min(profile.SustainScore, 80),
            Math.Min(profile.ControlScore, 110),
            Math.Min(profile.SupportPackageCount, 3));
    }

    private static ReleaseProfile Combine(
        IReadOnlyDictionary<PerkCategoryType, AuditPackage> packages,
        string name,
        IReadOnlyCollection<PerkCategoryType> categories)
    {
        var auditPackages = categories
            .Select(category => packages[category])
            .ToArray();

        return Combine(auditPackages, name);
    }

    private static ReleaseProfile Combine(IReadOnlyCollection<AuditPackage> packages, string name = "")
    {
        var stats = new Dictionary<StatType, int>();
        foreach (var package in packages)
        {
            foreach (var (stat, value) in package.Stats)
            {
                stats[stat] = stats.GetValueOrDefault(stat) + value;
            }
        }

        var cost = packages.Sum(x => x.Cost);
        var packageNames = packages.Select(x => x.Name).OrderBy(x => x).ToArray();
        return new ReleaseProfile(
            string.IsNullOrWhiteSpace(name) ? string.Join(" + ", packageNames) : name,
            packageNames,
            cost,
            stats,
            Sum(stats, StatType.MeleeDeflection),
            Sum(stats, StatType.RangedDeflection),
            Sum(stats, StatType.ShieldDeflection),
            Sum(stats, StatType.Guard),
            ScoreOffense(stats),
            ScoreDefense(stats),
            ScoreSustain(stats),
            ScoreControl(stats),
            packages.Count(x => SupportPackages.Contains(x.Category)));
    }

    private static int ScoreOffense(IReadOnlyDictionary<StatType, int> stats)
    {
        var damagePercent = SumBeneficial(stats, DirectDamagePercentStats);
        var flatDamage = SumBeneficial(stats, FlatDamageStats) / 2;
        var crit = SumBeneficial(stats, CriticalRateStats) + SumBeneficial(stats, CriticalDamageStats) / 2;
        var haste = SumBeneficial(stats, HasteStats);
        var mightScaling = SumBeneficial(stats, StatType.WeaponMightModifierDamageMultiplier) * 12 +
                           SumBeneficial(stats, StatType.StaffMightModifierDamageMultiplier) * 6;

        return damagePercent + flatDamage + crit + haste + mightScaling;
    }

    private static int ScoreDefense(IReadOnlyDictionary<StatType, int> stats)
    {
        return SumBeneficial(stats, DefenseStats) +
               SumBeneficial(stats, StatType.MeleeDeflection) * 2 +
               SumBeneficial(stats, StatType.RangedDeflection) * 2 +
               SumBeneficial(stats, StatType.ShieldDeflection) * 2 +
               SumBeneficial(stats, StatType.Guard) +
               SumBeneficial(stats, StatType.GuardDamageReductionPercentAdjustment) * 2;
    }

    private static int ScoreSustain(IReadOnlyDictionary<StatType, int> stats)
    {
        return SumBeneficial(stats, SustainStats);
    }

    private static int ScoreControl(IReadOnlyDictionary<StatType, int> stats)
    {
        return SumBeneficial(stats, ControlStats) +
               SumBeneficial(stats, StatType.ForceAbilityActivationDisabled) * 20;
    }

    private static int Sum(IReadOnlyDictionary<StatType, int> stats, params StatType[] statTypes)
    {
        return statTypes.Sum(stat => stats.GetValueOrDefault(stat));
    }

    private static int SumBeneficial(IReadOnlyDictionary<StatType, int> stats, params StatType[] statTypes)
    {
        return statTypes.Sum(stat => GetBeneficialMagnitude(stat, stats.GetValueOrDefault(stat)));
    }

    private static int GetBeneficialMagnitude(StatType stat, int value)
    {
        return Stat.GetStatTypeCategory(stat) switch
        {
            StatTypeCategory.BeneficialWhenPositive => Math.Max(value, 0),
            StatTypeCategory.BeneficialWhenNegative => Math.Max(-value, 0),
            _ => 0
        };
    }

    private static string Describe(ReleaseProfile profile)
    {
        return $"{profile.Name}: SP={profile.Cost}, Off={profile.OffenseScore}, Def={profile.DefenseScore}, Sustain={profile.SustainScore}, Control={profile.ControlScore}, MeleeDef={profile.MeleeDeflection}, RangedDef={profile.RangedDeflection}, ShieldDef={profile.ShieldDeflection}, Guard={profile.Guard}, SupportPkgs={profile.SupportPackageCount}, Packages=[{string.Join(", ", profile.PackageNames)}]";
    }

    private static IReadOnlyCollection<PerkRecord> BuildPerksWithout2daLookup()
    {
        var result = new List<PerkRecord>();
        var definitionTypes = typeof(IPerkListDefinition).Assembly
            .GetTypes()
            .Where(x => !x.IsAbstract && typeof(IPerkListDefinition).IsAssignableFrom(x))
            .OrderBy(x => x.FullName)
            .ToArray();

        foreach (var definitionType in definitionTypes)
        {
            var definition = Activator.CreateInstance(definitionType)!;
            foreach (var method in definitionType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                         .Where(x => x.ReturnType == typeof(void) && x.GetParameters().Length == 0 && !x.Name.Contains('<'))
                         .OrderBy(x => x.MetadataToken))
            {
                method.Invoke(definition, null);
            }

            var builder = definitionType
                .GetField("_builder", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(definition)!;

            var perks = (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
                .GetField("_perks", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(builder)!;

            result.AddRange(perks.Select(x => new PerkRecord(x.Key, x.Value)));
        }

        return result;
    }

    private static string GetCategoryName(PerkCategoryType category)
    {
        var field = typeof(PerkCategoryType).GetField(category.ToString())!;
        var attribute = (PerkCategoryAttribute)field
            .GetCustomAttributes(typeof(PerkCategoryAttribute), false)
            .Single();

        return attribute.Name;
    }

    private sealed record PerkRecord(PerkType Type, PerkDetail Detail);

    private sealed record AuditPackage(
        PerkCategoryType Category,
        string Name,
        int Cost,
        IReadOnlyDictionary<StatType, int> Stats,
        int MeleeDeflection,
        int RangedDeflection,
        int ShieldDeflection,
        int Guard,
        int OffenseScore,
        int DefenseScore,
        int SustainScore,
        int ControlScore,
        int SupportPackageCount);

    private sealed record ReleaseArchetype(string Name, ReleaseProfile Profile);

    private readonly record struct EnumerationKey(
        int MeleeDeflection,
        int RangedDeflection,
        int OffenseScore,
        int DefenseScore,
        int SustainScore,
        int ControlScore,
        int SupportPackageCount);

    private sealed record ReleaseProfile(
        string Name,
        IReadOnlyCollection<string> PackageNames,
        int Cost,
        IReadOnlyDictionary<StatType, int> Stats,
        int MeleeDeflection,
        int RangedDeflection,
        int ShieldDeflection,
        int Guard,
        int OffenseScore,
        int DefenseScore,
        int SustainScore,
        int ControlScore,
        int SupportPackageCount);
}
