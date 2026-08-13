using FluentAssertions;
using NUnit.Framework;
using System.Text.RegularExpressions;
using SWLOR.Game.Server.Feature.AbilityDefinition.Katar;
using SWLOR.Game.Server.Feature.AbilityDefinition.Pistol;
using SWLOR.Game.Server.Feature.AbilityDefinition.Saberstaff;
using SWLOR.Game.Server.Feature.AbilityDefinition.Spear;
using SWLOR.Game.Server.Feature.AbilityDefinition.Throwing;
using SWLOR.Game.Server.Feature.AbilityDefinition.Vibroknife;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class GeneratedWeaponPerkBehaviorTests
{
    [Test]
    public void SaberCyclone_IsASelfBuffWithoutHostileAreaTargeting()
    {
        var ability = new SaberCycloneAbilityDefinition().BuildAbilities()[FeatType.SaberCyclone1];

        ability.IsHostileAbility.Should().BeFalse();
        ability.IsAreaAbility.Should().BeFalse();
        ability.RequiresTarget.Should().BeFalse();
        ability.Targeting.Should().BeNull();
    }

    [Test]
    public void ForceSheath_RanksUseMeaningfulDamageSteps()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "AbilityDefinition",
            "Lightsaber",
            "ForceSheathAbilityDefinition.cs"));
        var matches = Regex.Matches(
            source,
            @"builder\.Create\(FeatType\.ForceSheath(?<rank>\d).*?SkillType\.Lightsaber,\s*(?<damage>\d+),",
            RegexOptions.Singleline);
        var damageByRank = matches.ToDictionary(
            match => int.Parse(match.Groups["rank"].Value),
            match => int.Parse(match.Groups["damage"].Value));

        damageByRank.Should().Equal(
            new Dictionary<int, int>
            {
                [1] = 12,
                [2] = 17,
                [3] = 23,
                [4] = 30
            });
        (source.Split("IsQueuedWeaponAbility = true").Length - 1).Should().Be(4);
        (source.Split("DamageType = CombatDamageType.Force").Length - 1).Should().Be(4);
    }

    [Test]
    public void GeneratedWeaponTraitPerks_EmitRepresentativeStatBonuses()
    {
        AssertSourceStat("VibrobladePerkDefinition.cs", StatType.AutoAttackDamageBonusChance, "15");
        AssertSourceStat("VibrobladePerkDefinition.cs", StatType.AutoAttackDamageBonus, "10");
        AssertSourceStat("VibrobladePerkDefinition.cs", StatType.MeleeRepeatedTargetDamageBonusPerHit, "3");
        AssertSourceStat("VibrobladePerkDefinition.cs", StatType.MeleeRepeatedTargetDamageBonusMax, "15");
        AssertSourceStat("VibrobladePerkDefinition.cs", StatType.MeleeRepeatedTargetDamageStatusEffectIcon, "(int)EffectIconType.RundownStatusEffect");
        AssertSourceStat("VibrobladePerkDefinition.cs", StatType.MeleeAutoAttackCycleRequiredCount, "3");
        AssertSourceStat("VibrobladePerkDefinition.cs", StatType.MeleeAutoAttackCycleDamage, "10");
        AssertSourceContains("VibrobladePerkDefinition.cs", "EquipmentPredicates.HasOffHandShield(creature) ? 35 : 0");

        AssertSourceStat("VibroknifePerkDefinition.cs", StatType.SourceStatusHealingReceivedRequiredCategory, "(int)StatusEffectCategory.Venom");
        AssertSourceStat("VibroknifePerkDefinition.cs", StatType.SourceStatusHealingReceivedPercentAdjustment, "-15");
        AssertSourceStat("VibroknifePerkDefinition.cs", StatType.SourceStatusStackRequiredCategory, "(int)StatusEffectCategory.Venom");
        AssertSourceStat("VibroknifePerkDefinition.cs", StatType.SourceStatusStackAppliedCategory, "(int)StatusEffectCategory.Infection");
        AssertSourceStat("VibroknifePerkDefinition.cs", StatType.SourceStatusStackMaximum, "5");
        AssertSourceStat("VibroknifePerkDefinition.cs", StatType.AbilityDamageToSourceAppliedStatusTargetBonus, "12");
        AssertSourceStat("VibroknifePerkDefinition.cs", StatType.SourceStatusAutoAttackCycleDamageType, "(int)CombatDamageType.Poison");
        AssertSourceStat("VibroknifePerkDefinition.cs", StatType.DirectDamageToStatusCategoryOrStealthBonusCategory, "(int)StatusEffectCategory.Incapacitating");
        AssertSourceStat("VibroknifePerkDefinition.cs", StatType.HostileAbilityUsedAttackPercentAdjustment, "5");
        AssertSourceStat("VibroknifePerkDefinition.cs", StatType.StatusAppliedTargetStaminaDrainRequiredCategory, "(int)StatusEffectCategory.StaminaDrainTrigger");
        AssertSourceStat("VibroknifePerkDefinition.cs", StatType.HostileAbilityHitNextAutoAttackNoDelayAllSkills, "1");

        AssertSourceStat("HeavyVibrobladePerkDefinition.cs", StatType.HeavyVibrobladeOffenseEssenceHunter, "1");
        AssertSourceStat("HeavyVibrobladePerkDefinition.cs", StatType.HeavyVibrobladeOffenseSoulAscension, "1");
        AssertSourceStat("HeavyVibrobladePerkDefinition.cs", StatType.HeavyVibrobladeDefenseDamageDealtHPPercentRestore, "1");

        AssertSourceStat("RiflePerkDefinition.cs", StatType.IdleSkillAbilitySkillType, "(int)SkillType.Rifle");
        AssertSourceStat("RiflePerkDefinition.cs", StatType.IdleSkillAbilityDamageBonus, "14");
        AssertSourceStat("RiflePerkDefinition.cs", StatType.IdleSkillAbilityHitChancePercentAdjustment, "8");
        AssertSourceStat("RiflePerkDefinition.cs", StatType.IdleSkillAbilityCriticalDamagePercentAdjustment, "15");
        AssertSourceStat("RiflePerkDefinition.cs", StatType.SameTargetPressureBuildSkillType, "(int)SkillType.Rifle");
        AssertSourceStat("RiflePerkDefinition.cs", StatType.SameTargetPressureBuildSeconds, "12");
        AssertSourceStat("RiflePerkDefinition.cs", StatType.SameTargetPressureGraceSeconds, "6");
        AssertSourceStat("RiflePerkDefinition.cs", StatType.SameTargetPressureReadyDurationSeconds, "9");
        AssertSourceStat("RiflePerkDefinition.cs", StatType.SameTargetPressureWeaponAbilityDamageBonus, "15");
        AssertSourceStat("RiflePerkDefinition.cs", StatType.RangedRepeatedTargetDamageBonusPerHit, "3");
        AssertSourceStat("RiflePerkDefinition.cs", StatType.RangedRepeatedTargetDamageBonusMax, "15");
        AssertSourceStat("RiflePerkDefinition.cs", StatType.RangedRepeatedTargetDamageDurationSeconds, "30");
        AssertSourceStat("RiflePerkDefinition.cs", StatType.AutoAttackSuppressionStackChance, "15");
        AssertSourceStat("RiflePerkDefinition.cs", StatType.AutoAttackSuppressionStackDurationSeconds, "30");
        AssertSourceStat("RiflePerkDefinition.cs", StatType.AutoAttackSuppressionStackEvasionPenaltyPercent, "5");
        AssertSourceStat("RiflePerkDefinition.cs", StatType.RangedHitSuppressionStackEvasionPenaltyPercent, "5");
        AssertSourceStat("RiflePerkDefinition.cs", StatType.AbilityHitChanceAgainstSuppressionStackPercentAdjustment, "10");
        AssertSourceStat("RiflePerkDefinition.cs", StatType.DefenseIgnoreHitPhysicalDefensePercentAdjustment, "-10");

        AssertSourceStat("ThrowingPerkDefinition.cs", StatType.ThrowingBombardierClusterStormDamageBonus, "10");
        AssertSourceStat("ThrowingPerkDefinition.cs", StatType.ThrowingBombardierClusterStormMaximumTargets, "1");
        AssertSourceStat("ThrowingPerkDefinition.cs", StatType.AreaAbilityFragmentationDamage, "7");
        AssertSourceStat("ThrowingPerkDefinition.cs", StatType.AreaAbilityTargetHitSequenceCountRequired, "2");
        AssertSourceStat("ThrowingPerkDefinition.cs", StatType.BleedingStatusExpiredNextSkillAbilityStaminaCostAdjustment, "-3");
        AssertSourceStat("ThrowingPerkDefinition.cs", StatType.BleedingStatusExpiredNextSkillAbilitySkillType, "(int)SkillType.Throwing");
        AssertSourceStat("ThrowingPerkDefinition.cs", StatType.BleedingStatusExpiredNextSkillAbilityWindowSeconds, "30");
        AssertSourceStat("ThrowingPerkDefinition.cs", StatType.AbilityDamageToBleedingTargetSkillType, "(int)SkillType.Throwing");
        AssertSourceStat("ThrowingPerkDefinition.cs", StatType.AbilityDamageToBleedingTargetBonus, "12");
        AssertSourceStat("ThrowingPerkDefinition.cs", StatType.SkillAbilityBleedingTargetStaminaRestoreSkillType, "(int)SkillType.Throwing");
        AssertSourceStat("ThrowingPerkDefinition.cs", StatType.SkillAbilityBleedingTargetStaminaRestoreChance, "100");
        AssertSourceStat("ThrowingPerkDefinition.cs", StatType.SkillAbilityBleedingTargetStaminaRestore, "2");
        AssertSourceStat("ThrowingPerkDefinition.cs", StatType.BleedingTargetAbilityBleedDurationExtensionSeconds, "6");
        AssertSourceStat("ThrowingPerkDefinition.cs", StatType.TargetLowHPStatusDamageThresholdPercent, "50");
        AssertSourceStat("ThrowingPerkDefinition.cs", StatType.TargetLowHPStatusDamageStatusCategory, "(int)StatusEffectCategory.Bleeding");
        AssertSourceStat("ThrowingPerkDefinition.cs", StatType.TargetLowHPStatusDamagePercentAdjustment, "20");

        AssertSourceStat("TwinBladePerkDefinition.cs", StatType.BleedingTargetAbilityBleedSpreadChance, "35");
        AssertSourceStat("TwinBladePerkDefinition.cs", StatType.BleedingTargetAbilityBleedSpreadDurationSeconds, "30");
        AssertSourceStat("TwinBladePerkDefinition.cs", StatType.DefeatedBleedingEnemyNearbyBleedDurationSeconds, "30");
        AssertSourceStat("TwinBladePerkDefinition.cs", StatType.TargetStatusCriticalRateStatusCategory, "(int)StatusEffectCategory.Bleeding");
        AssertSourceStat("TwinBladePerkDefinition.cs", StatType.SkillDamageBleedingTargetStaminaRestoreSkillType, "(int)SkillType.TwinBlade");
        AssertSourceStat("TwinBladePerkDefinition.cs", StatType.SkillDamageBleedingTargetStaminaRestoreChance, "100");
        AssertSourceStat("TwinBladePerkDefinition.cs", StatType.SkillDamageBleedingTargetStaminaRestore, "1");
        AssertSourceStat("TwinBladePerkDefinition.cs", StatType.SkillDamageBleedingTargetStaminaRestoreCooldownSeconds, "4");

        AssertSourceStat("SaberstaffPerkDefinition.cs", StatType.AbilityStaminaCostFPRestorePercentSkillType, "(int)SkillType.Saberstaff");
        AssertSourceStat("SaberstaffPerkDefinition.cs", StatType.AbilityStaminaCostFPRestorePercent, "35");
        AssertSourceStat("SaberstaffPerkDefinition.cs", StatType.AbilityFPCostStaminaRestorePercentSkillType, "(int)SkillType.Force");
        AssertSourceStat("SaberstaffPerkDefinition.cs", StatType.AbilityFPCostStaminaRestorePercent, "35");
        AssertSourceStat("SaberstaffPerkDefinition.cs", StatType.RestoredFPForceAttackPercentAdjustment, "8");
        AssertSourceStat("SaberstaffPerkDefinition.cs", StatType.RestoredFPForceAttackDurationSeconds, "30");
        AssertSourceStat("SaberstaffPerkDefinition.cs", StatType.RestoredStaminaAttackPercentAdjustment, "8");
        AssertSourceStat("SaberstaffPerkDefinition.cs", StatType.RestoredStaminaAttackDurationSeconds, "30");
        AssertSourceStat("SaberstaffPerkDefinition.cs", StatType.HighFPAndStaminaAbilityDamageBonus, "12");
        AssertSourceStat("SaberstaffPerkDefinition.cs", StatType.HighFPAndStaminaAbilityDamagePercentAdjustmentThresholdPercent, "60");
        AssertSourceStat("SaberstaffPerkDefinition.cs", StatType.HighFPAndStaminaAbilityDamagePercentAdjustment, "8");
        AssertSourceStat("SaberstaffPerkDefinition.cs", StatType.AbilityRestoredFPHastePercentAdjustment, "10");
        AssertSourceStat("SaberstaffPerkDefinition.cs", StatType.AreaAbilityAfterDeflectionDamagePercentAdjustment, "10");
        AssertSourceStat("SaberstaffPerkDefinition.cs", StatType.AbilityGrantedAttackDeflectionFPRestore, "2");

        AssertSourceStat("KatarPerkDefinition.cs", StatType.Guard, "35");
        AssertSourceStat("KatarPerkDefinition.cs", StatType.GuardDamageReductionPercentAdjustment, "10");
        AssertSourceStat("KatarPerkDefinition.cs", StatType.GuardedHitNextAttackDMGBonus, "10");
        AssertSourceStat("KatarPerkDefinition.cs", StatType.GuardedHitNextSkillAbilityStatusSkillType, "(int)SkillType.Katar");
        AssertSourceStat("KatarPerkDefinition.cs", StatType.GuardedHitNextSkillAbilityExposedDamageBonus, "35");
        AssertSourceStat("KatarPerkDefinition.cs", StatType.AbilityUsedPerkCategoryTargetEnmityToSourceCategoryId, "(int)PerkCategoryType.KatarIronGuard");
        AssertSourceStat("KatarPerkDefinition.cs", StatType.AbilityUsedPerkCategoryTargetEnmityToSourcePercentAdjustment, "25");
        AssertSourceStat("KatarPerkDefinition.cs", StatType.GuardedHitPulseDMG, "15");
        AssertSourceStat("KatarPerkDefinition.cs", StatType.GuardedHitPulseRadiusMeters, "5");
        AssertSourceStat("KatarPerkDefinition.cs", StatType.GuardedHitPulseEnmityPercentOfIncomingDamage, "100");
        AssertSourceStat("KatarPerkDefinition.cs", StatType.GuardedHitSecondaryNextAttackDMGBonus, "8");
        AssertSourceStat("KatarPerkDefinition.cs", StatType.GuardedHitSecondaryNextAttackEnmityBonus, "40");
        AssertSourceStat("KatarPerkDefinition.cs", StatType.GuardedHitSecondaryNextAttackWindowSeconds, "30");
        AssertSourceStat("KatarPerkDefinition.cs", StatType.LowHPGuard, "25");
        AssertSourceStat("KatarPerkDefinition.cs", StatType.StatusAppliedRequiredCategory, "(int)StatusEffectCategory.Control");
        AssertSourceStat("KatarPerkDefinition.cs", StatType.StatusAppliedSelfEnmityPercentAdjustment, "15");
        AssertSourceStat("KatarPerkDefinition.cs", StatType.OutgoingDebuffDurationPercentAdjustment, "20");
        AssertSourceStat("KatarPerkDefinition.cs", StatType.DamageTakenNextSkillAbilityDamageBonus, "20");

        AssertSourceStat("StaffPerkDefinition.cs", StatType.CriticalDamageTargetStatusCategory, "(int)StatusEffectCategory.Control");
        AssertSourceStat("StaffPerkDefinition.cs", StatType.StatusAppliedNextAttackDamageBonus, "26");
        AssertSourceStat("StaffPerkDefinition.cs", StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflectionCategoryId, "(int)PerkCategoryType.StaffSentinel");
        AssertSourceStat("StaffPerkDefinition.cs", StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflection, "8");
        AssertSourceStat("StaffPerkDefinition.cs", StatType.AbilityUsedPerkCategorySelfDefenseCategoryId, "(int)PerkCategoryType.StaffSentinel");
        AssertSourceStat("StaffPerkDefinition.cs", StatType.AbilityUsedPerkCategorySelfEvasionPercentAdjustment, "25");
        AssertSourceStat("StaffPerkDefinition.cs", StatType.AbilityUsedAttackDeflection, "8");

        AssertSourceStat("PistolPerkDefinition.cs", StatType.RangedAbilityHitNearTargetDamageDealtPercentAdjustment, "-10");
        AssertSourceStat("PistolPerkDefinition.cs", StatType.SecondaryAbilityUsedEvasionPercentAdjustmentSkillType, "(int)SkillType.Pistol");
        AssertSourceStat("PistolPerkDefinition.cs", StatType.SecondaryAbilityUsedEvasionPercentAdjustment, "8");
        AssertSourceStat("PistolPerkDefinition.cs", StatType.SecondaryAbilityUsedEvasionDurationSeconds, "30");
        AssertSourceStat("PistolPerkDefinition.cs", StatType.AbilityUsedRangedEvasionPercentAdjustmentSkillType, "(int)SkillType.Pistol");
        AssertSourceStat("PistolPerkDefinition.cs", StatType.AbilityUsedRangedEvasionPercentAdjustment, "12");
        AssertSourceStat("PistolPerkDefinition.cs", StatType.AbilityUsedRangedEvasionDurationSeconds, "30");
        AssertSourceStat("PistolPerkDefinition.cs", StatType.RangedAutoAttackCycleCriticalRateRequiredCount, "4");
        AssertSourceStat("PistolPerkDefinition.cs", StatType.CriticalDamageHighHPTargetPercentAdjustment, "15");

        AssertSourceStat("SpearPerkDefinition.cs", StatType.CostlyAbilityDamageBonus, "14");
        AssertSourceStat("SpearPerkDefinition.cs", StatType.CostlyAbilityDamageMinimumStaminaCost, "8");
        AssertSourceStat("SpearPerkDefinition.cs", StatType.ForceDamageTakenForceDefense, "5");
        AssertSourceStat("SpearPerkDefinition.cs", StatType.ForceDamageTakenForceDefenseDurationSeconds, "30");
        AssertSourceStat("SpearPerkDefinition.cs", StatType.CostlyAbilityHitStaminaRestore, "3");
        AssertSourceStat("SpearPerkDefinition.cs", StatType.CostlyAbilityHitStaminaRestoreMinimumStaminaCost, "8");
        AssertSourceStat("SpearPerkDefinition.cs", StatType.AbilityDamageToSourceAppliedStatusTargetCategory, "(int)StatusEffectCategory.Control");
        AssertSourceStat("SpearPerkDefinition.cs", StatType.AbilityDamageToSourceAppliedStatusTargetPercentAdjustment, "10");
        AssertSourceStat("SpearPerkDefinition.cs", StatType.AbilityUsedEvasionPercentAdjustmentSkillType, "(int)SkillType.Spear");
        AssertSourceStat("SpearPerkDefinition.cs", StatType.AbilityUsedEvasionPercentAdjustment, "10");
        AssertSourceStat("SpearPerkDefinition.cs", StatType.AbilityUsedEvasionDurationSeconds, "30");
        AssertSourceStat("SpearPerkDefinition.cs", StatType.CostlyAbilityUsedEvasionMinimumStaminaCost, "8");
        var spearSource = ReadPerkDefinition("SpearPerkDefinition.cs");
        spearSource.Should().NotContain("StatType.CostlyAbilityDamageBonusSkillType");
        spearSource.Should().NotContain("StatType.CostlyAbilityUsedEvasionPercentAdjustmentSkillType");
        spearSource.Should().NotContain("StatType.CostlyAbilityHitStaminaRestoreSkillType");
        spearSource.Should().NotContain("StatType.CostlyAbilityHitMinimumStaminaCost");
        AssertSourceStat("VibrobladePerkDefinition.cs", StatType.ShieldEquippedPhysicalDefensePercentAdjustment, "12");
    }

    [Test]
    public void PathogenStrike_ExtendsCasterOwnedVenomAndInfectionWithoutConsumingThem()
    {
        var root = FindRepositoryRoot();
        var pathogenSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "AbilityDefinition",
            "Vibroknife",
            "PathogenStrikeAbilityDefinition.cs"));
        var generatedAbilitySource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "AbilityDefinition",
            "WeaponActiveAbilityDefinitionBase.cs")).Replace("\r\n", "\n");

        pathogenSource.Should().Contain("SourceStatusEffectsToExtend = new[] { typeof(VenomStatusEffect), typeof(InfectionStatusEffect) }");
        pathogenSource.Should().NotContain("ConsumeSourceStatusEffectsOnHit = true");
        generatedAbilitySource.Should().Contain("StatusEffect.ExtendStatusEffectDuration(\n                        target,\n                        statusEffectType,\n                        activator,");
    }

    [Test]
    public void SpottersRhythm_UsesSameTargetPressureInsteadOfIdleAbilityStats()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "PerkDefinition",
            "RiflePerkDefinition.cs"));
        var start = source.IndexOf("private void SpottersRhythm()", StringComparison.Ordinal);
        var end = source.IndexOf("private void SuppressiveLine()", start, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0);
        end.Should().BeGreaterThan(start);

        var spottersRhythm = source[start..end];
        spottersRhythm.Should().Contain("After maintaining Rifle fire on the same target for 12 seconds");
        spottersRhythm.Should().Contain("gain Spotter's Rhythm for 9 seconds");
        spottersRhythm.Should().Contain("hostile weapon ability against that target");
        spottersRhythm.Should().Contain("StatType.SameTargetPressureBuildSkillType");
        spottersRhythm.Should().Contain("StatType.SameTargetPressureWeaponAbilityDamageBonus");
        spottersRhythm.Should().NotContain("IdleSkillAbility");
        spottersRhythm.Should().NotContain("hostile ranged ability");
    }

    [Test]
    public void GeneratedWeaponStances_EmitBibleDrivenStatusStats()
    {
        var assassins = new AssassinsStanceStatusEffect();
        AssertStatusStat(assassins, StatType.PoisonDamageDealtPercentAdjustment, 20);
        AssertStatusStat(assassins, StatType.AttackPercentAdjustment, -10);

        var shadowflow = new ShadowflowStanceStatusEffect();
        AssertStatusStat(shadowflow, StatType.AutoAttackHamstringSkillType, (int)SkillType.Vibroknife);
        AssertStatusStat(shadowflow, StatType.AutoAttackHamstringDurationSeconds, 18);
        AssertStatusStat(shadowflow, StatType.DefensePercentAdjustment, -20);

        var berserker = new BerserkerStanceStatusEffect();
        berserker.ApplyEffect(1, 1, -1);
        AssertStatusStat(berserker, StatType.AttackPercentAdjustment, 25);
        AssertStatusStat(berserker, StatType.AttackDelayReductionPercent, 15);
        AssertStatusStat(berserker, StatType.PhysicalDefensePercentAdjustment, -20);
        AssertStatusStat(berserker, StatType.ForceDefensePercentAdjustment, -20);

        var soulDevourer = new SoulDevourerStatusEffect();
        AssertStatusStat(soulDevourer, StatType.AttackPercentAdjustment, 25);
        AssertStatusStat(soulDevourer, StatType.CriticalRatePercentAdjustment, 10);

        var flurry = new FlurryStanceStatusEffect();
        AssertStatusStat(flurry, StatType.DefensePercentAdjustment, -10);

        var suppression = new SuppressionStanceStatusEffect();
        AssertStatusStat(suppression, StatType.RangedHitSuppressionStackDurationSeconds, 30);
        AssertStatusStat(suppression, StatType.RangedCriticalDamagePercentAdjustment, -10);

        var vigor = new VigorStanceStatusEffect();
        AssertStatusStat(vigor, StatType.HostileAbilityStaminaCostFlatAdjustment, 2);
        AssertStatusStat(vigor, StatType.DamageDealtPercentAdjustment, 10);
        AssertStatusStat(vigor, StatType.HostileAbilityUsedEvasionPercentAdjustment, 8);
        AssertStatusStat(vigor, StatType.HostileAbilityUsedEvasionDurationSeconds, 30);
        AssertStatusStat(vigor, StatType.SkillAbilityStaminaCostFlatAdjustmentSkillType, 0);
        AssertStatusStat(vigor, StatType.SkillAbilityDamagePercentAdjustmentSkillType, 0);
        AssertStatusStat(vigor, StatType.HostileAbilityUsedEvasionPercentAdjustmentSkillType, 0);

        var scrapper = new ScrapperStanceStatusEffect();
        AssertStatusStat(scrapper, StatType.OutgoingControlDurationPercentAdjustment, 20);
        AssertStatusStat(scrapper, StatType.HostileAbilityRecastDelayPercentAdjustment, 10);

        var infiniteConduit = new InfiniteConduitStatusEffect();
        AssertStatusStat(infiniteConduit, StatType.AbilityStaminaCostFPRestorePercentSkillType, (int)SkillType.Saberstaff);
        AssertStatusStat(infiniteConduit, StatType.AbilityStaminaCostFPRestorePercent, 50);
        AssertStatusStat(infiniteConduit, StatType.AbilityFPCostStaminaRestorePercentSkillType, (int)SkillType.Force);
        AssertStatusStat(infiniteConduit, StatType.AbilityFPCostStaminaRestorePercent, 50);
        AssertStatusStat(infiniteConduit, StatType.HighFPAndStaminaAbilityDamageBonusThresholdPercent, 70);
        AssertStatusStat(infiniteConduit, StatType.HighFPAndStaminaAbilityDamageBonus, 20);

        var restoredFPForceAttack = new RestoredFPForceAttackStatusEffect(8);
        AssertStatusStat(restoredFPForceAttack, StatType.ForceAttackPercentAdjustment, 8);
        restoredFPForceAttack.Icon.Should().Be(EffectIconType.RestoredFPForceAttackStatusEffect);

        var restoredStaminaAttack = new RestoredStaminaAttackStatusEffect(8);
        AssertStatusStat(restoredStaminaAttack, StatType.AttackPercentAdjustment, 8);
        restoredStaminaAttack.Icon.Should().Be(EffectIconType.RestoredStaminaAttackStatusEffect);

        var restoredFPHaste = new RestoredFPHasteStatusEffect(10);
        AssertStatusStat(restoredFPHaste, StatType.AttackDelayReductionPercent, 10);
        restoredFPHaste.Icon.Should().Be(EffectIconType.RestoredFPHasteStatusEffect);

        var hostileAbilityForceAttack = new HostileAbilityForceAttackStatusEffect(15);
        AssertStatusStat(hostileAbilityForceAttack, StatType.ForceAttackPercentAdjustment, 15);
        hostileAbilityForceAttack.Icon.Should().Be(EffectIconType.HostileAbilityForceAttackStatusEffect);

        var guardedChannel = new GuardedChannelStatusEffect(20);
        AssertStatusStat(guardedChannel, StatType.PhysicalDefensePercentAdjustment, 20);
        guardedChannel.Icon.Should().Be(EffectIconType.GuardedChannelStatusEffect);

        var guardianReflexes = new GuardianReflexesStatusEffect(25);
        AssertStatusStat(guardianReflexes, StatType.Guard, 25);
        guardianReflexes.Icon.Should().Be(EffectIconType.GuardianReflexesStatusEffect);
    }

    [Test]
    public void VigorThrust_BakesEvasionIntoEachRanksBaseStaminaCost()
    {
        var abilities = new VigorThrustAbilityDefinition().BuildAbilities();
        var expected = new[]
        {
            (FeatType.VigorThrust1, Stamina: 3),
            (FeatType.VigorThrust2, Stamina: 5),
            (FeatType.VigorThrust3, Stamina: 8),
            (FeatType.VigorThrust4, Stamina: 12),
        };

        foreach (var (feat, stamina) in expected)
        {
            var ability = abilities[feat];
            ability.SkillType.Should().Be(SkillType.Spear);
            ability.Requirements
                .OfType<AbilityRequirementStamina>()
                .Should().ContainSingle()
                .Which.RequiredSTM.Should().Be(stamina);
        }

        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot().FullName,
            "SWLOR.Game.Server",
            "Feature",
            "AbilityDefinition",
            "Spear",
            "VigorThrustAbilityDefinition.cs"));
        source.Should().Contain("SelfEvasionPercent = 6");
        source.Should().Contain("SelfEvasionPercent = 8");
        source.Should().Contain("SelfEvasionPercent = 10");
        source.Should().Contain("SelfEvasionPercent = 12");
        source.Should().Contain("SelfStatDurationSeconds = 30");
    }

    [Test]
    public void EvasiveFootwork_ExposesTheTriggeredEvasionAsAVisibleStatus()
    {
        var status = new EvasiveFootworkStatusEffect(10);
        AssertStatusStat(status, StatType.EvasionPercentAdjustment, 10);
        status.Name.Should().Be("Evasive Footwork");
        status.Icon.Should().Be(EffectIconType.EvasiveFootworkStatusEffect);
        status.StackingType.Should().Be(StatusEffectStackType.Disabled,
            "Mobile and Lateral Footwork should refresh the shared buff rather than stack it");

        var clone = status.Clone();
        AssertStatusStat(clone, StatType.EvasionPercentAdjustment, 10);
    }

    [Test]
    public void AutoAttackHamstringStats_AreDeclaredAndConsumedBySharedCombat()
    {
        Stat.GetStatTypeCategory(StatType.AutoAttackHamstringSkillType).Should().Be(StatTypeCategory.NonBeneficial);
        Stat.GetStatTypeCategory(StatType.AutoAttackHamstringDurationSeconds).Should().Be(StatTypeCategory.NonBeneficial);

        var root = FindRepositoryRoot();
        var combatSource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "Combat.cs"));
        combatSource.Should().Contain("ApplyAutoAttackHamstringEffect(attacker, defender, skillType, CombatDamageType.Physical)");
        combatSource.Should().Contain("StatType.AutoAttackHamstringSkillType");
        combatSource.Should().Contain("typeof(HamstringStatusEffect)");
    }

    [Test]
    public void ReportedQueuedWeaponAndPistolAbilityTargeting_MatchesActivationBehavior()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root, "SWLOR_Haks", "sw_2da", "feat.2da");
        var spellRows = Read2da(root, "SWLOR_Haks", "sw_2da", "spells.2da");

        var pathogenStrike = new PathogenStrikeAbilityDefinition().BuildAbilities();
        foreach (var feat in new[] { FeatType.PathogenStrike1, FeatType.PathogenStrike2, FeatType.PathogenStrike3, FeatType.PathogenStrike4 })
        {
            pathogenStrike[feat].IsHostileAbility.Should().BeTrue();
            pathogenStrike[feat].RequiresTarget.Should().BeFalse();
            AssertFeatSpellTargeting(featRows, spellRows, feat, "1", "****", "M", "0x03", "0");
        }

        var virulentBlade = new VirulentBladeAbilityDefinition().BuildAbilities();
        foreach (var feat in new[] { FeatType.VirulentBlade1, FeatType.VirulentBlade2, FeatType.VirulentBlade3 })
        {
            virulentBlade[feat].IsHostileAbility.Should().BeTrue();
            virulentBlade[feat].RequiresTarget.Should().BeFalse();
            AssertFeatSpellTargeting(featRows, spellRows, feat, "1", "****", "M", "0x03", "0");
        }

        var guardCounter = new GuardCounterAbilityDefinition().BuildAbilities();
        foreach (var feat in new[] { FeatType.GuardCounter1, FeatType.GuardCounter2, FeatType.GuardCounter3 })
        {
            guardCounter[feat].ActivationType.Should().Be(AbilityActivationType.Weapon);
            guardCounter[feat].IsHostileAbility.Should().BeTrue();
            guardCounter[feat].RequiresTarget.Should().BeFalse();
            AssertFeatSpellTargeting(featRows, spellRows, feat, "1", "****", "P", "0x01", "0");
        }

        var guardCounterSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "AbilityDefinition",
            "Katar",
            "GuardCounterAbilityDefinition.cs"));
        (guardCounterSource.Split("IsQueuedWeaponAbility = true").Length - 1).Should().Be(3);
        guardCounterSource.Should().Contain("ExtraDamageIfRecentGuardedHit = 8");
        guardCounterSource.Should().Contain("ExtraDamageIfRecentGuardedHit = 12");
        guardCounterSource.Should().Contain("ExtraDamageIfRecentGuardedHit = 17");
        guardCounterSource.Should().Contain("RequireRecentGuardedHitForConditionalStatus = true");
        guardCounterSource.Should().Contain("ConditionalTargetStatusEffect = typeof(DazedStatusEffect)");

        var explosiveToss = new ExplosiveTossAbilityDefinition().BuildAbilities();
        foreach (var feat in new[]
                 {
                     FeatType.ExplosiveToss1,
                     FeatType.ExplosiveToss2,
                     FeatType.ExplosiveToss3,
                     FeatType.ExplosiveToss4
                 })
        {
            explosiveToss[feat].ActivationType.Should().Be(AbilityActivationType.Casted);
            explosiveToss[feat].IsHostileAbility.Should().BeTrue();
            explosiveToss[feat].RequiresTarget.Should().BeFalse(
                "the Bible places Explosive Toss at a location, not on a required target object");
            explosiveToss[feat].RequiresLocationTarget.Should().BeTrue();

            var featRow = featRows[(int)feat];
            featRow["TARGETSELF"].Should().Be("****");
            featRow["HostileFeat"].Should().Be("1");

            var spellRow = spellRows[int.Parse(featRow["SPELLID"])];
            spellRow["TargetShape"].Should().Be("sphere");
            spellRow["TargetSizeX"].Should().Be("5");
            spellRow["TargetFlags"].Should().Be("1");
        }

        var generatorSource = File.ReadAllText(Path.Combine(root.FullName, "tools", "GenerateWeaponArchetypeImplementation.py"));
        generatorSource.Should().Contain("(?:this|it) deals weapon DMG");
        generatorSource.Should().Contain("def has_explicit_area_target_point(lowered):");
        generatorSource.Should().Contain("is_explicitly_aimed = has_explicit_area_target_point(lowered)");
        generatorSource.Should().Contain("\"AbilityTargetingFlags.HarmsEnemies\" if is_explicitly_aimed else",
            "explicitly placed spheres must not be regenerated as caster-centered areas");

        var cripplingSlice = new CripplingSliceAbilityDefinition().BuildAbilities();
        foreach (var feat in new[] { FeatType.CripplingSlice1, FeatType.CripplingSlice2, FeatType.CripplingSlice3 })
        {
            cripplingSlice[feat].IsHostileAbility.Should().BeTrue();
            cripplingSlice[feat].RequiresTarget.Should().BeTrue();
            AssertFeatSpellTargeting(featRows, spellRows, feat, "****", "1", "M", "0x03", "0");
        }

        var shadowflow = new ShadowflowStanceAbilityDefinition().BuildAbilities()[FeatType.ShadowflowStance1];
        shadowflow.IsHostileAbility.Should().BeFalse();
        shadowflow.RequiresTarget.Should().BeFalse();

        var gambler = new GamblerStanceAbilityDefinition().BuildAbilities()[FeatType.GamblerStance1];
        gambler.IsHostileAbility.Should().BeFalse();
        gambler.RequiresTarget.Should().BeFalse();

        foreach (var feat in new[]
        {
            FeatType.FlurryStance1,
            FeatType.GamblerStance1,
            FeatType.LaceratorStance1,
            FeatType.OrdnanceStance1,
            FeatType.ScrapperStance1,
            FeatType.ShadowflowStance1,
            FeatType.SuppressionStance1,
            FeatType.VigorStance1
        })
        {
            AssertFeatSpellTargeting(featRows, spellRows, feat, "1", "****", "P", "0x01", "0");
        }
    }

    [Test]
    public void EventDrivenWeaponPromises_AreDeclaredAndConsumedBySharedCombat()
    {
        Stat.GetStatTypeCategory(StatType.CriticalBleedingStatusDurationExtensionSeconds)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.HostileAbilitySequenceWindowSeconds)
            .Should().Be(StatTypeCategory.NonBeneficial);
        Stat.GetStatTypeCategory(StatType.NextDamageDealtBleedDurationSeconds)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.SameTargetHostileAbilityStaminaRestore)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.BleedingStatusExpiredNextSkillAbilityStaminaCostAdjustment)
            .Should().Be(StatTypeCategory.BeneficialWhenNegative);
        Stat.GetStatTypeCategory(StatType.AbilityRestoredFPHastePercentAdjustment)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.HighFPAndStaminaAbilityDamageBonus)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.HighFPAndStaminaAbilityDamagePercentAdjustment)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.BleedingTargetAbilityBleedSpreadChance)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.AvoidedAttackNextAutoAttackNoDelaySkillType)
            .Should().Be(StatTypeCategory.NonBeneficial);
        Stat.GetStatTypeCategory(StatType.AutoAttackSuppressionStackChance)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.SuppressionStackEvasionPenaltyPercentAdjustment)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.SameTargetPressureBuildSkillType)
            .Should().Be(StatTypeCategory.NonBeneficial);
        Stat.GetStatTypeCategory(StatType.SameTargetPressureBuildSeconds)
            .Should().Be(StatTypeCategory.NonBeneficial);
        Stat.GetStatTypeCategory(StatType.SameTargetPressureGraceSeconds)
            .Should().Be(StatTypeCategory.NonBeneficial);
        Stat.GetStatTypeCategory(StatType.SameTargetPressureReadyDurationSeconds)
            .Should().Be(StatTypeCategory.NonBeneficial);
        Stat.GetStatTypeCategory(StatType.SameTargetPressureWeaponAbilityDamageBonus)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.CostlyAbilityDamageBonus)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.DeflectionNearbyAllyGuard)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.RangedAbilityHitNearTargetDamageDealtPercentAdjustment)
            .Should().Be(StatTypeCategory.BeneficialWhenNegative);
        Stat.GetStatTypeCategory(StatType.IdleSkillAbilityCriticalDamagePercentAdjustment)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.TargetLowHPStatusDamageStatusCategory)
            .Should().Be(StatTypeCategory.NonBeneficial);
        Stat.GetStatTypeCategory(StatType.TargetLowHPStatusDamagePercentAdjustment)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.DamageToSourceAppliedStatusTargetPercentAdjustment)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.AbilityDamageToSourceAppliedStatusTargetPercentAdjustment)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.DamageTakenShareToStatusSourcePercent)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.GuardedAllyHitNextSkillAbilityDamageBonus)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.WardSharedDamageNextSkillAbilityDamageBonus)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.ShieldEquippedPhysicalDefensePercentAdjustment)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.HitChanceAgainstSunderedTargetPercentAdjustment)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.CriticalRateAgainstSunderedTargetPercentAdjustment)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.SkillAreaAbilityDamagePercentAdjustment)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.OutgoingControlDurationPercentAdjustment)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.HostileAbilityRecastDelayPercentAdjustment)
            .Should().Be(StatTypeCategory.BeneficialWhenNegative);
        Stat.GetStatTypeCategory(StatType.WardTargetPhysicalDefensePercentAdjustment)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.WardTargetForceDefensePercentAdjustment)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.WardAbilityDefensePercentAdjustment)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.WardAbilityForceDefensePercentAdjustment)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.WardAbilityDefenseDurationSeconds)
            .Should().Be(StatTypeCategory.NonBeneficial);
        Stat.GetStatTypeCategory(StatType.WardAbilityDefenseCategoryId)
            .Should().Be(StatTypeCategory.NonBeneficial);
        Stat.GetStatTypeCategory(StatType.AbilityUsedRangedEvasionPercentAdjustmentSkillType)
            .Should().Be(StatTypeCategory.NonBeneficial);
        Stat.GetStatTypeCategory(StatType.AbilityUsedRangedEvasionPercentAdjustment)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.AbilityUsedRangedEvasionDurationSeconds)
            .Should().Be(StatTypeCategory.NonBeneficial);
        Stat.GetStatTypeCategory(StatType.ForceDamageTakenForceDefense)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.ForceDamageTakenForceDefenseDurationSeconds)
            .Should().Be(StatTypeCategory.NonBeneficial);
        Stat.GetStatTypeCategory(StatType.AbilityUsedPerkCategoryTargetEnmityToSourceCategoryId)
            .Should().Be(StatTypeCategory.NonBeneficial);
        Stat.GetStatTypeCategory(StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflectionCategoryId)
            .Should().Be(StatTypeCategory.NonBeneficial);
        Stat.GetStatTypeCategory(StatType.AbilityUsedPerkCategorySelfDefenseCategoryId)
            .Should().Be(StatTypeCategory.NonBeneficial);
        Stat.GetStatTypeCategory(StatType.PoisonDamageDealtPercentAdjustment)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.SourceStatusStackAppliedCategory)
            .Should().Be(StatTypeCategory.NonBeneficial);
        Stat.GetStatTypeCategory(StatType.SourceStatusStackMaximum)
            .Should().Be(StatTypeCategory.NonBeneficial);
        Stat.GetStatTypeCategory(StatType.HostileAbilityHitNextAutoAttackNoDelaySkillType)
            .Should().Be(StatTypeCategory.NonBeneficial);
        Stat.GetStatTypeCategory(StatType.HostileAbilityHitNextAutoAttackNoDelayAllSkills)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.NextAutoAttackNoDelayAllSkills)
            .Should().Be(StatTypeCategory.NonBeneficial);
        Stat.GetStatTypeCategory(StatType.SourceStatusAutoAttackCycleDamage)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.StatusAppliedTargetStaminaDrain)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.SourceStatusHealingReceivedPercentAdjustment)
            .Should().Be(StatTypeCategory.BeneficialWhenNegative);
        Stat.GetStatTypeCategory(StatType.DirectDamageToStatusCategoryOrStealthBonus)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.SkillAbilityDamagePercentAdjustmentSkillType)
            .Should().Be(StatTypeCategory.NonBeneficial);
        Stat.GetStatTypeCategory(StatType.SkillAbilityDamagePercentAdjustment)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.CostlyAbilityUsedEvasionPercentAdjustmentSkillType)
            .Should().Be(StatTypeCategory.NonBeneficial);
        Stat.GetStatTypeCategory(StatType.CostlyAbilityUsedEvasionPercentAdjustment)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.CostlyAbilityDamageMinimumStaminaCost)
            .Should().Be(StatTypeCategory.NonBeneficial);
        Stat.GetStatTypeCategory(StatType.CostlyAbilityHitStaminaRestoreMinimumStaminaCost)
            .Should().Be(StatTypeCategory.NonBeneficial);
        Stat.GetStatTypeCategory(StatType.CostlyAbilityStatusMinimumStaminaCost)
            .Should().Be(StatTypeCategory.NonBeneficial);
        Stat.GetStatTypeCategory(StatType.GuardedHitSecondaryNextSkillAbilitySkillType)
            .Should().Be(StatTypeCategory.NonBeneficial);
        Stat.GetStatTypeCategory(StatType.GuardedHitSecondaryNextSkillAbilityDamageBonus)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.GuardedHitSecondaryNextAttackDMGBonus)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.GuardedHitSecondaryNextAttackEnmityBonus)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.GuardedHitSecondaryNextAttackWindowSeconds)
            .Should().Be(StatTypeCategory.NonBeneficial);
        Stat.GetStatTypeCategory(StatType.GuardedHitPulseRadiusMeters)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.GuardedHitPulseEnmityPercentOfIncomingDamage)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.HostileAbilityUsedEvasionPercentAdjustmentSkillType)
            .Should().Be(StatTypeCategory.NonBeneficial);
        Stat.GetStatTypeCategory(StatType.HostileAbilityUsedEvasionPercentAdjustment)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);

        var root = FindRepositoryRoot();
        var combatSource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "Combat.cs"));
        var abilitySource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "Ability.cs"));
        var usePerkFeatSource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "UsePerkFeat.cs"));
        var nativeAttackSource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Native", "ResolveAttackRoll.cs"));
        var statusEffectSource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "StatusEffect.cs"));
        var generatorSource = File.ReadAllText(Path.Combine(root.FullName, "tools", "GenerateWeaponArchetypeImplementation.py"));
        combatSource.Should().Contain("ApplyCriticalBleedingStatusDurationExtension(attacker, defender)");
        combatSource.Should().Contain("StatType.HighFPAndStaminaAbilityDamagePercentAdjustmentThresholdPercent");
        combatSource.Should().Contain("StatType.HighFPAndStaminaAbilityDamagePercentAdjustment");
        combatSource.Should().Contain("new HostileAbilityForceAttackStatusEffect(total)");
        combatSource.Should().Contain("new RestoredFPHasteStatusEffect(haste)");
        combatSource.Should().Contain("new RestoredFPForceAttackStatusEffect(forceAttack)");
        combatSource.Should().Contain("new RestoredStaminaAttackStatusEffect(attack)");
        combatSource.Should().NotContain("SaberstaffConduitForceLens",
            "shared resource restoration must remain stat-driven rather than checking a specific perk identity");
        generatorSource.Should().Contain(
            "add_stat(stats, \"HighFPAndStaminaAbilityDamagePercentAdjustmentThresholdPercent\"");
        generatorSource.Should().Contain(
            "add_stat(stats, \"HighFPAndStaminaAbilityDamagePercentAdjustment\"");
        combatSource.Should().Contain("ApplyHostileAbilitySequenceEffects(activator, feat, ability)");
        combatSource.Should().Contain("ApplySameTargetHostileAbilityHitEffects(activator, target, ability)");
        combatSource.Should().Contain("ApplyNextDamageDealtBleedEffect(attacker, defender, damageType)");
        combatSource.Should().Contain("ApplyBleedingTargetAbilityBleedSpread(activator, target, skillType, damageType)");
        combatSource.Should().Contain("ApplyRangedHitSuppressionStack(attacker, defender, skillType, damageType)");
        combatSource.Should().Contain("ApplySameTargetPressureDamageEffects(attacker, defender, skillType)");
        combatSource.Should().Contain("GetSameTargetPressureWeaponAbilityDamageBonus");
        combatSource.Should().Contain("ConsumeSameTargetPressureWeaponAbilityDamageBonus");
        combatSource.Should().Contain("typeof(SpottersRhythmStatusEffect)");
        var consumePressureIndex = abilitySource.IndexOf("ConsumeSameTargetPressureWeaponAbilityDamageBonus", StringComparison.Ordinal);
        var applyDamageDealtIndex = abilitySource.IndexOf("Combat.ApplyDamageDealtEffects(", StringComparison.Ordinal);
        consumePressureIndex.Should().BeGreaterThanOrEqualTo(0);
        applyDamageDealtIndex.Should().BeGreaterThanOrEqualTo(0);
        consumePressureIndex.Should().BeLessThan(applyDamageDealtIndex);
        abilitySource.Should().Contain("isAbilityDamage: true");
        combatSource.Should().Contain("SuppressionStackEvasionPenaltyPercentAdjustment");
        combatSource.Should().Contain("if (adjustedEvasionPenaltyPercent <= 0)");
        combatSource.Should().Contain("TrackSuppressionAbilityUse(activator, now)");
        combatSource.Should().Contain("GetSuppressionAbilityHitChanceAdjustment(attacker, defender, skillType)");
        combatSource.Should().Contain("!IsRangedWeaponSkill(skillType)");
        combatSource.Should().Contain("_pendingSuppressionAbilityUses.TryGetValue(key, out var state)");
        combatSource.Should().Contain("state.Expiration <= DateTime.UtcNow");
        combatSource.Should().Contain("HasCurrentSuppressionAbilityUseStack(attacker, defender, state.SuppressionEffectIds)");
        combatSource.Should().Contain("effects.Max(effect => effect.DurationTicks * effect.Frequency)");
        combatSource.Should().Contain("SuppressionEffectIds = effects.Select(effect => effect.Id).ToHashSet()");
        combatSource.Should().Contain("ApplySkillAreaAbilityDamageModifier(");
        combatSource.Should().Contain("ApplySkillAbilityDamageModifier(");
        combatSource.Should().Contain("ApplyHostileAbilityUsedEvasion(");
        combatSource.Should().Contain("ApplyCostlyAbilityUsedEvasion(");
        combatSource.Should().Contain("StatType.GuardedHitSecondaryNextSkillAbilitySkillType");
        combatSource.Should().Contain("ApplyGuardedHitRetaliationPulse(");
        combatSource.Should().Contain("ColorToken.Combat($\"Counter Ready: +{selected.DamageBonus} DMG{criticalText}\")");
        combatSource.Should().Contain("GetHitChanceAgainstSunderedTargetAdjustment(");
        combatSource.Should().Contain("GetCriticalRateAgainstSunderedTargetAdjustment(");
        combatSource.Should().Contain("ApplyAbilityRecastDelayModifiers(");
        combatSource.Should().Contain("ApplyLightsaberWardActivatedEffects(activator, ability)");
        combatSource.Should().Contain("StatType.WardAbilityDefensePercentAdjustment");
        combatSource.Should().Contain("StatType.WardTargetPhysicalDefensePercentAdjustment");
        combatSource.Should().Contain("ApplyForceDamageTakenEffects(defender)");
        combatSource.Should().Contain("new ForceWardingStatusEffect(forceDefense)");
        combatSource.Should().Contain("StatType.GuardedHitPulseDMG");
        combatSource.Should().NotContain("GuardRetaliationDMGBonusSkillType");
        combatSource.Should().Contain("AbilityMatchesPerkCategoryStat(");
        combatSource.Should().Contain("StatType.AbilityUsedPerkCategoryTargetEnmityToSourceCategoryId");
        combatSource.Should().Contain("StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflectionCategoryId");
        combatSource.Should().Contain("StatType.AbilityUsedPerkCategorySelfDefenseCategoryId");
        combatSource.Should().NotContain("KatarIronGuardPulseDamageBonus");
        combatSource.Should().NotContain("StaffSentinelGuardCategoryId");
        combatSource.Should().Contain("StatType.RangedEvasionPercentAdjustment");
        usePerkFeatSource.Should().Contain("Combat.ApplyAbilityRecastDelayModifiers(");
        nativeAttackSource.Should().Contain("Combat.GetHitChanceAgainstSunderedTargetAdjustment(");
        nativeAttackSource.Should().Contain("Combat.GetCriticalRateAgainstSunderedTargetAdjustment(");
        statusEffectSource.Should().Contain("OutgoingControlDurationPercentAdjustment");
        generatorSource.Should().Contain("EXPLICIT_RECAST_SHORT_NAMES");
        generatorSource.Should().Contain("Missing explicit recast short name");
        generatorSource.Should().Contain("\"SameTargetPressureBuildSkillType\"");
        generatorSource.Should().Contain("\"SameTargetPressureReadyDurationSeconds\"");
        generatorSource.Should().Contain("gain Spotter's Rhythm for (\\d+) seconds");
        generatorSource.Should().Contain("Unable to parse Suppressing Shot suppression stack Evasion");
        generatorSource.Should().Contain("\"GuardedHitSecondaryNextAttackEnmityBonus\"");
        generatorSource.Should().Contain("\"CostlyAbilityUsedEvasionPercentAdjustmentSkillType\"");
        generatorSource.Should().Contain("\"HostileAbilityStaminaCostFlatAdjustment\"");
        generatorSource.Should().Contain("\"HostileAbilityUsedEvasionPercentAdjustment\"");
        combatSource.Should().Contain("ApplyAvoidedAttackNextAutoAttackNoDelay(creature)");
        combatSource.Should().Contain("ApplyBleedingStatusExpiredEffects(uint source)");
        combatSource.Should().Contain("ApplyCostlyAbilityHitEffects(activator, target, ability, skillType)");
        combatSource.Should().Contain("ApplyDeflectionNearbyAllyGuard(creature, source)");
        combatSource.Should().Contain("ApplyAbilityGrantedAttackDeflectionEffects(activator, source)");
        combatSource.Should().Contain("ApplyTargetLowHPStatusDamageModifier(attacker, defender, damage)");
        combatSource.Should().Contain("ApplyDamageTakenShareToStatusSource(defender, attacker, damage, damageType)");
        combatSource.Should().Contain("IdleSkillAbilityCriticalDamagePercentAdjustment");
        combatSource.Should().Contain("TargetHasSourceAppliedStatusCategory(defender, attacker, category)");
        combatSource.Should().Contain("ApplySourceStatusStackEffects(attacker, defender)");
        combatSource.Should().Contain("ApplyHostileAbilityHitNextAutoAttackNoDelay(activator, ability)");
        combatSource.Should().Contain("GrantNextAutoAttackNoDelay(activator, duration)");
        combatSource.Should().Contain("StatType.NextAutoAttackNoDelayAllSkills");
        combatSource.Should().Contain("var appliesToSkill = skillType != SkillType.Invalid && storedSkillType == skillType");
        combatSource.Should().Contain("if (appliesToSkill)");
        combatSource.Should().Contain("First Strike ready: {maximumCount} {stackLabel} (+{damageBonus} DMG each).");
        combatSource.Should().Contain("First Strike deals +{damageBonus} DMG ({remaining} {stackLabel} remaining{rechargeText}).");
        combatSource.Should().Contain("First Strike +{damageBonus} DMG ({remaining} {stackLabel} remaining)");
        combatSource.Should().Contain("First Strike is recharging ({remainingSeconds} seconds remaining).");

        var firstStrikeStart = combatSource.IndexOf(
            "private static int GetFirstHostileAbilityHitDamageBonus(",
            StringComparison.Ordinal);
        var firstStrikeEnd = combatSource.IndexOf(
            "private static int GetAbilityDamageToSourceAppliedStatusTargetAdjustment(",
            firstStrikeStart,
            StringComparison.Ordinal);
        firstStrikeEnd.Should().BeGreaterThan(firstStrikeStart);
        var firstStrikeSource = combatSource[firstStrikeStart..firstStrikeEnd];
        firstStrikeSource.Should().Contain("ability?.IsHostileAbility != true");
        firstStrikeSource.Should().NotContain("SkillType.",
            "the Bible grants First Strike to any hostile combat ability, including cross-skill abilities");
        firstStrikeSource.Should().Contain("SendMessageToPC(attacker, feedback);");
        firstStrikeSource.Should().Contain("Count = 0");
        firstStrikeSource.Should().Contain("LastHit = DateTime.MinValue");
        firstStrikeSource.Should().Contain("First Strike ready: Attacker={Attacker}");
        firstStrikeSource.Should().Contain("First Strike stack consumed: Attacker={Attacker}");
        firstStrikeSource.Should().Contain("First Strike recharged: Attacker={Attacker}");
        firstStrikeSource.Should().Contain("First Strike reset after combat: Attacker={Attacker}");

        var firstCombatAttackStart = combatSource.IndexOf(
            "private static void ApplyFirstCombatAttackStaminaRestore(",
            StringComparison.Ordinal);
        var firstCombatAttackEnd = combatSource.IndexOf(
            "private static void ApplyAutoAttackHamstringEffect(",
            firstCombatAttackStart,
            StringComparison.Ordinal);
        firstCombatAttackEnd.Should().BeGreaterThan(firstCombatAttackStart);
        var firstCombatAttackSource = combatSource[firstCombatAttackStart..firstCombatAttackEnd];
        firstCombatAttackSource.Should().Contain("_firstCombatAttackConsumed.Add(attacker)");
        firstCombatAttackSource.Should().NotContain("_lastCombatActivity",
            "a prolonged combat entry can remain active through hostile attempts and incoming attacks");

        var damageDealtEffectsStart = combatSource.IndexOf(
            "public static void ApplyDamageDealtEffects(",
            StringComparison.Ordinal);
        var damageDealtEffectsEnd = combatSource.IndexOf(
            "private static void ApplyHeavyVibrobladeDefenseDamageRecovery(",
            damageDealtEffectsStart,
            StringComparison.Ordinal);
        damageDealtEffectsEnd.Should().BeGreaterThan(damageDealtEffectsStart);
        var damageDealtEffectsSource = combatSource[damageDealtEffectsStart..damageDealtEffectsEnd];
        damageDealtEffectsSource.IndexOf("TrackCombatActivity(attacker);", StringComparison.Ordinal)
            .Should().BeLessThan(
                damageDealtEffectsSource.IndexOf("ApplyFirstCombatAttackStaminaRestore(attacker);", StringComparison.Ordinal),
                "combat entry must reset the consumed state before the opening landed attack evaluates Venatic Recovery");

        var trackCombatActivityStart = combatSource.IndexOf(
            "private static void TrackCombatActivity(",
            StringComparison.Ordinal);
        var trackCombatActivityEnd = combatSource.IndexOf(
            "public static void TrackAttackActivity(",
            trackCombatActivityStart,
            StringComparison.Ordinal);
        trackCombatActivityEnd.Should().BeGreaterThan(trackCombatActivityStart);
        var trackCombatActivitySource = combatSource[trackCombatActivityStart..trackCombatActivityEnd];
        trackCombatActivitySource.Should().Contain("ReportCombatEntryIfNeeded(creature, now);");
        trackCombatActivitySource.IndexOf("ReportCombatEntryIfNeeded(creature, now);", StringComparison.Ordinal)
            .Should().BeLessThan(
                trackCombatActivitySource.IndexOf("_lastCombatActivity[creature] = now;", StringComparison.Ordinal),
                "combat entry must be evaluated before the activity timestamp is refreshed");

        var hostileAbilityActivityStart = combatSource.IndexOf(
            "public static void TrackHostileAbilityActivity(",
            StringComparison.Ordinal);
        var hostileAbilityActivityEnd = combatSource.IndexOf(
            "public static void TrackHostileDefensiveCombatEntryActivity(",
            hostileAbilityActivityStart,
            StringComparison.Ordinal);
        hostileAbilityActivityEnd.Should().BeGreaterThan(hostileAbilityActivityStart);
        var hostileAbilityActivitySource = combatSource[hostileAbilityActivityStart..hostileAbilityActivityEnd];
        hostileAbilityActivitySource.Should().Contain("ReportCombatEntryIfNeeded(creature, now);");
        hostileAbilityActivitySource.Should().Contain("_lastHostileAbilityAttemptActivity[creature] = now;");
        hostileAbilityActivitySource.Should().NotContain("_lastCombatActivity[creature] = now;",
            "hostile attempts must not suppress landed-opening riders such as Venatic Recovery");

        var defensiveCombatEntryActivityStart = hostileAbilityActivityEnd;
        var defensiveCombatEntryActivityEnd = combatSource.IndexOf(
            "private static void ReportCombatEntryIfNeeded(",
            defensiveCombatEntryActivityStart,
            StringComparison.Ordinal);
        defensiveCombatEntryActivityEnd.Should().BeGreaterThan(defensiveCombatEntryActivityStart);
        var defensiveCombatEntryActivitySource = combatSource[defensiveCombatEntryActivityStart..defensiveCombatEntryActivityEnd];
        defensiveCombatEntryActivitySource.Should().Contain("ReportCombatEntryIfNeeded(creature, now);");
        defensiveCombatEntryActivitySource.Should().Contain("_lastHostileIncomingActivity[creature] = now;");
        defensiveCombatEntryActivitySource.Should().Contain("GetIsReactionTypeHostile(attacker, creature)");
        defensiveCombatEntryActivitySource.Should().NotContain("_lastCombatActivity[creature] = now;",
            "incoming hostile actions must not suppress Venatic Recovery on the defender's retaliation");

        var reportCombatEntryStart = defensiveCombatEntryActivityEnd;
        var reportCombatEntryEnd = combatSource.IndexOf(
            "private static bool HasRecentCombatEntryActivity(",
            reportCombatEntryStart,
            StringComparison.Ordinal);
        reportCombatEntryEnd.Should().BeGreaterThan(reportCombatEntryStart);
        var reportCombatEntrySource = combatSource[reportCombatEntryStart..reportCombatEntryEnd];
        reportCombatEntrySource.Should().Contain("HasRecentCombatEntryActivity(creature, now)");
        reportCombatEntrySource.Should().Contain("_firstCombatAttackConsumed.Remove(creature);");
        reportCombatEntrySource.Should().Contain("ReportFirstStrikeCombatEntry(creature, now);");
        combatSource.Should().Contain("TrackHostileDefensiveCombatEntryActivity(defender, attacker);");
        combatSource.Should().Contain("TrackHostileDefensiveCombatEntryActivity(creature, attacker);");
        nativeAttackSource.Should().Contain("Combat.TrackAvoidedAttack(defender.m_idSelf, attacker.m_idSelf);");
        nativeAttackSource.Should().Contain("var wasSuccessfulBeforeDefensiveEffects = IsSuccessfulAttackResult(");
        nativeAttackSource.Should().Contain("wasSuccessfulBeforeDefensiveEffects &&");
        var defensiveEffectsIndex = nativeAttackSource.IndexOf(
            "attacker.ResolveDefensiveEffects(defender, isHit ? 1 : 0);",
            StringComparison.Ordinal);
        defensiveEffectsIndex.Should().BeGreaterThan(-1);
        nativeAttackSource.IndexOf(
                "Combat.TrackAvoidedAttack(defender.m_idSelf, attacker.m_idSelf);",
                defensiveEffectsIndex,
                StringComparison.Ordinal)
            .Should().BeGreaterThan(defensiveEffectsIndex,
                "concealment can convert a provisional hit into an avoided attack");

        var hostileCombatImpactStart = abilitySource.IndexOf(
            "private static int ApplyHostileCombatImpact(",
            StringComparison.Ordinal);
        var hostileCombatImpactEnd = abilitySource.IndexOf(
            "private static bool ShouldResolveCombatImpactHit(",
            hostileCombatImpactStart,
            StringComparison.Ordinal);
        hostileCombatImpactEnd.Should().BeGreaterThan(hostileCombatImpactStart);
        var hostileCombatImpactSource = abilitySource[hostileCombatImpactStart..hostileCombatImpactEnd];
        hostileCombatImpactSource.Should().Contain("Combat.TrackHostileAbilityActivity(activator);");
        hostileCombatImpactSource.Should().Contain("Combat.TrackHostileDefensiveCombatEntryActivity(target, activator);");
        hostileCombatImpactSource.IndexOf("Combat.TrackHostileAbilityActivity(activator);", StringComparison.Ordinal)
            .Should().BeLessThan(
                hostileCombatImpactSource.IndexOf("Combat.TryResolveAbilityHit(", StringComparison.Ordinal),
                "a missed opening cast still enters combat and must report First Strike readiness");
        hostileCombatImpactSource.IndexOf("Combat.TrackHostileDefensiveCombatEntryActivity(target, activator);", StringComparison.Ordinal)
            .Should().BeLessThan(
                hostileCombatImpactSource.IndexOf("Combat.TryResolveAbilityHit(", StringComparison.Ordinal),
                "the defender must enter combat even when the opening hostile cast misses");
        hostileCombatImpactSource.Should().Contain("firstHostileAbilityHitDamageBonusApplied: true");
        abilitySource.Should().Contain("bool firstHostileAbilityHitDamageBonusApplied = false");
        combatSource.Should().Contain("if (firstHostileAbilityHitDamageBonusApplied)");
        combatSource.Should().Contain("ApplyHostileAbilityUsedAttackAdjustment(activator, ability)");
        combatSource.Should().Contain("public static void ApplyStatusAppliedTargetStaminaDrain(");
        combatSource.Should().Contain("TryUseStatTrigger(activator, StatType.StatusAppliedTargetStaminaDrain, cooldown)");
        var staminaDrainMethodStart = combatSource.IndexOf(
            "public static void ApplyStatusAppliedTargetStaminaDrain(",
            StringComparison.Ordinal);
        var staminaDrainMethodEnd = combatSource.IndexOf(
            "private static void ApplyAreaAbilityTargetHitSequenceEffects(",
            staminaDrainMethodStart,
            StringComparison.Ordinal);
        staminaDrainMethodEnd.Should().BeGreaterThan(staminaDrainMethodStart);
        var staminaDrainMethod = combatSource[staminaDrainMethodStart..staminaDrainMethodEnd];
        var staminaBeforeIndex = staminaDrainMethod.IndexOf(
            "var staminaBefore = Stat.GetCurrentStamina(target)",
            StringComparison.Ordinal);
        var reduceStaminaIndex = staminaDrainMethod.IndexOf(
            "Stat.ReduceStamina(target, staminaDrain)",
            StringComparison.Ordinal);
        var staminaDrainedIndex = staminaDrainMethod.IndexOf(
            "var staminaDrained = Math.Max(0, staminaBefore - Stat.GetCurrentStamina(target))",
            StringComparison.Ordinal);
        var feedbackIndex = staminaDrainMethod.IndexOf(
            "ColorToken.Combat($\"-{staminaDrained} STM\")",
            StringComparison.Ordinal);
        staminaBeforeIndex.Should().BeGreaterThanOrEqualTo(0);
        reduceStaminaIndex.Should().BeGreaterThan(staminaBeforeIndex);
        staminaDrainedIndex.Should().BeGreaterThan(reduceStaminaIndex);
        feedbackIndex.Should().BeGreaterThan(staminaDrainedIndex);
        staminaDrainMethod.Should().NotContain("ColorToken.Combat($\"-{staminaDrain} STM\")");
        statusEffectSource.Should().Contain("Combat.ApplyStatusAppliedTargetStaminaDrain(source, creature, statusEffect.Categories)");
        usePerkFeatSource.Should().Contain("public static bool InterruptAbilityActivation(uint activator)");
        usePerkFeatSource.Should().Contain("activation.Ability.ChannelInterruptAction?.Invoke(activator)");
        combatSource.Should().Contain("StatusEffectCategory.Infection => typeof(InfectionStatusEffect)");
        abilitySource.Should().Contain("beforeSuccessfulImpactRiders?.Invoke(target)");
        abilitySource.Should().Contain("Combat.ApplySuccessfulAbilityImpactRiders(");
        abilitySource.Should().Contain("beforeSuccessfulImpactRiders");

        var bleedSource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "StatusEffectDefinition", "BleedStatusEffect.cs"));
        var venomSource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "StatusEffectDefinition", "VenomStatusEffect.cs"));
        var infectionSource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "StatusEffectDefinition", "InfectionStatusEffect.cs"));
        var suppressionSource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "StatusEffectDefinition", "SuppressionStatusEffect.cs"));
        bleedSource.Should().Contain("WasNaturallyExpired");
        venomSource.Should().Contain("EffectDamage(damageAmount, CombatDamageType.Poison.GetNWScriptDamageType())");
        venomSource.Should().Contain("var baseDamage = CalculateBaseDamagePerTick(_damageBonusPercent)");
        venomSource.Should().Contain("Combat.ApplyDamageTypeDealtModifiers(source, baseDamage, CombatDamageType.Poison)");
        infectionSource.Should().Contain("public int Stacks");
        infectionSource.Should().Contain("DamagePerStack * Math.Max(1, Stacks)");
        infectionSource.Should().Contain("EffectDamage(damageAmount, CombatDamageType.Poison.GetNWScriptDamageType())");
        combatSource.Should().Contain("typeof(HemorrhageStatusEffect)");
        File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "StatusEffectDefinition", "LacerationStatusEffect.cs")).Should().BeFalse();
        File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "StatusEffectDefinition", "RupturedStatusEffect.cs")).Should().BeFalse();
        suppressionSource.Should().Contain("StatusEffectStackType.UnlimitedStacking");
        new SuppressionStatusEffect(4).StatGroup.Stats[StatType.EvasionPercentAdjustment].Should().Be(-4);
    }

    [Test]
    public void GeneratedWeaponActiveProfiles_EmitConditionalAndTemporaryRiders()
    {
        var root = FindRepositoryRoot();

        AssertAbilitySourceContains(root, "Saberstaff", "FocusedArcAbilityDefinition.cs", "HighResourceExtraDamageThresholdPercent = 60");
        AssertAbilitySourceContains(root, "Saberstaff", "FocusedArcAbilityDefinition.cs", "ExtraDamageIfHighResources = 10");
        AssertAbilitySourceContains(root, "Saberstaff", "GuardedChannelAbilityDefinition.cs", "SelfStatResourceAboveThresholdPercent = 40");
        AssertAbilitySourceContains(root, "Saberstaff", "GuardedChannelAbilityDefinition.cs", "SelfStatDurationSeconds = 30");
        AssertAbilitySourceContains(root, "Saberstaff", "GuardedChannelAbilityDefinition.cs", "SelfStatusEffectFactory = () => new GuardedChannelStatusEffect");
        AssertAbilitySourceContains(root, "Saberstaff", "SeverFocusAbilityDefinition.cs", "DrainTargetResourceAboveThresholdPercent = 80");
        AssertAbilitySourceContains(root, "Saberstaff", "InfiniteConduitAbilityDefinition.cs", "typeof(InfiniteConduitStatusEffect)");
        AssertAbilitySourceContains(root, "Katar", "SteelShoulderAbilityDefinition.cs", "FriendlyTargetStatusEffectFactory = () => new GuardedStatusEffect(50, 5.0f)");
        AssertAbilitySourceContains(root, "Katar", "SteelShoulderAbilityDefinition.cs", "FriendlyTargetStatusPersistsUntilBroken = true");
        AssertAbilitySourceContains(root, "Katar", "TagInAbilityDefinition.cs", "RequiresGuardedTarget = true");
        AssertAbilitySourceContains(root, "Katar", "TagInAbilityDefinition.cs", "FriendlyTargetTemporaryHPPercent = 15");
        AssertAbilitySourceContains(root, "Katar", "TagInAbilityDefinition.cs", "SelfGuardPercent = 20");
        AssertAbilitySourceContains(root, "Katar", "WhirlingGuardAbilityDefinition.cs", "SelfStatusAlsoAppliesToGuardedTarget = true");
        AssertAbilitySourceContains(root, "Vibroblade", "RiotBladeAbilityDefinition.cs", "FeatType.RiotBlade4");
        AssertAbilitySourceContains(root, "HeavyVibroblade", "HeavyVibrobladeActiveAbilityDefinitionBase.cs", "ApplyEssenceHunter");
        AssertAbilitySourceContains(root, "HeavyVibroblade", "SoulBurstAbilityDefinition.cs", "afterSuccessfulHit");
        AssertAbilitySourceContains(root, "Pistol", "LastWordAbilityDefinition.cs", "TemporaryAvoidedAttackNextAutoAttackNoDelaySkillType = (int)SkillType.Pistol");
        AssertAbilitySourceContains(root, "Rifle", "SuppressingShotAbilityDefinition.cs", "ApplySuppressionStackOnHit = true");
        AssertAbilitySourceContains(root, "Rifle", "SuppressiveLineAbilityDefinition.cs", "SuppressionDisorientedRequiredStacks = 2");
        AssertAbilitySourceContains(root, "Rifle", "KillBoxAbilityDefinition.cs", "TemporarySuppressionStackEvasionPenaltyPercentAdjustment = 3");
        AssertAbilitySourceContains(root, "TwinBlade", "TempestBloomAbilityDefinition.cs", "TemporaryAreaAbilityFragmentationDamage = 8");
        AssertAbilitySourceContains(root, "Throwing", "RainOfSteelAbilityDefinition.cs", "TemporaryAreaAbilityFragmentationPulseSeconds = 6");
        AssertAbilitySourceContains(root, "Throwing", "ExplosiveTossAbilityDefinition.cs", "typeof(BurnStatusEffect)");
        AssertAbilitySourceContains(root, "Throwing", "ExplosiveTossAbilityDefinition.cs", "Spell.ExplosiveToss4");
        AssertAbilitySourceContains(root, "Saberstaff", "SaberCycloneAbilityDefinition.cs", "TemporaryAreaAbilityAttackDeflection = 8");
        AssertAbilitySourceContains(root, "Saberstaff", "CircleSlashAbilityDefinition.cs", "SelfRangedDeflection = 8");
        AssertAbilitySourceContains(root, "Staff", "ShelterCircleAbilityDefinition.cs", "NearbyPartyStatusEffect = typeof(ShelterCircleStatusEffect)");
        AssertAbilitySourceContains(root, "Staff", "ShelterCircleAbilityDefinition.cs", "NearbyPartyStatusIncludesSelf = true");
        AssertAbilitySourceContains(root, "Staff", "UnmovingCenterAbilityDefinition.cs", "SelfKnockdownDazedImmunityDurationSeconds = 45");
        AssertAbilitySourceContains(root, "Spear", "CripplingDefenseAbilityDefinition.cs", "TemporaryCostlyAbilityExposedDurationSeconds = 30");
        AssertAbilitySourceContains(root, "Spear", "CripplingDefenseAbilityDefinition.cs", "TemporaryCostlyAbilityStatusMinimumStaminaCost = 8");
        AssertAbilitySourceDoesNotContain(root, "Spear", "CripplingDefenseAbilityDefinition.cs", "TemporaryCostlyAbilityStatusSkillType");
        var generatorSource = File.ReadAllText(Path.Combine(root.FullName, "tools", "GenerateWeaponArchetypeImplementation.py"));
        generatorSource.Should().Contain("def add_high_stm_exposed_properties():");
        generatorSource.Split("if \"high-stm abilities also inflict exposed\" in lowered:")
            .Should().HaveCount(2, "the high-STM Exposed rule must have one canonical case-insensitive branch");
        AssertAbilitySourceContains(root, "Staff", "WorldbreakerAbilityDefinition.cs", "RequiredTargetStatusCategoryForConditionalStatus = StatusEffectCategory.Control");
        AssertAbilitySourceContains(root, "Vibroknife", "PathogenStrikeAbilityDefinition.cs", "SourceStatusEffectsToExtend = new[] { typeof(VenomStatusEffect), typeof(InfectionStatusEffect) }");
        AssertAbilitySourceContains(root, "Vibroknife", "BackstabAbilityDefinition.cs", "ExtraDamageIfBehindFeedbackLabel = \"Backstab\"");
        AssertAbilitySourceContains(root, "Vibroknife", "ViralCascadeAbilityDefinition.cs", "ExtraDamageSourceStatusEffect = typeof(VenomStatusEffect)");
        AssertAbilitySourceContains(root, "Vibroknife", "ViralCascadeAbilityDefinition.cs", "ExtraDamageSourceStackStatusEffect = typeof(InfectionStatusEffect)");
        AssertAbilitySourceContains(root, "Vibroknife", "ViralCascadeAbilityDefinition.cs", "ConsumeSourceStatusEffectsOnHit = true");
        AssertAbilitySourceContains(root, "Vibroknife", "ViralCascadeAbilityDefinition.cs", "SuppressSourceStatusStackRiders = true");
        AssertAbilitySourceContains(root, "TwinBlade", "RedBloomAbilityDefinition.cs", "SpreadHemorrhageFromTarget = true");

        var weaponBaseSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "AbilityDefinition",
            "WeaponActiveAbilityDefinitionBase.cs"));
        weaponBaseSource.Should().Contain("UsePerkFeat.InterruptAbilityActivation(target)");
        weaponBaseSource.Should().Contain("Extended {extendedCount} {statusLabel} by {SourceStatusExtensionSeconds}s");
        weaponBaseSource.Should().Contain("public bool HasImmediateSelfStatusEffect()");
        weaponBaseSource.Should().Contain("SelfStatusEffectFactory != null && SelfStatDurationSeconds <= 0",
            "resource-gated status factories must be applied by ApplySelfModifiers after the gate, not as permanent immediate statuses");
        weaponBaseSource.Should().Contain("if (profile.HasImmediateSelfStatusEffect())");
    }

    [Test]
    public void GeneratedFriendlyTargetAbilities_ValidateStatusBeforeReplacingExistingLinks()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "AbilityDefinition",
            "WeaponActiveAbilityDefinitionBase.cs"));

        source.Should().Contain("public string ValidateFriendlyTargetStatus(uint activator, uint target)");
        source.Should().Contain("target = ResolveFriendlyTarget(activator, target);");
        source.Should().Contain("public uint ResolveFriendlyTarget(uint activator, uint target)");
        source.Should().Contain("You do not have an active Guarded target within range.");
        source.Should().Contain("return GuardedStatusEffect.GetActiveGuardedTarget(activator);");
        source.Should().Contain("AbilityTargeting.ValidateFriendlyTarget(activator, target, false)");
        source.Should().Contain("GuardedStatusEffect.IsActiveGuardedBySource(target, activator)");
        source.Should().Contain("ApplyFriendlyTargetEffects(activator, target, temporaryHPEffectKey)");
        source.Should().Contain("statusEffect.ReassignSource(activator)");
        source.Should().Contain("if (!profile.RequiresGuardedTarget)");
        source.Should().Contain("return profile.ValidateFriendlyTargetStatus(activator, target)");
        source.Should().NotContain("profileFactory");

        var canApplyIndex = source.IndexOf("var canApply = statusEffect.CanApply(target)", StringComparison.Ordinal);
        var removeExistingLinkIndex = source.IndexOf(
            "StatusEffect.RemoveStatusEffectFromAllTargetsBySource(statusEffect.GetType(), activator, false)",
            StringComparison.Ordinal);
        canApplyIndex.Should().BeGreaterThanOrEqualTo(0);
        removeExistingLinkIndex.Should().BeGreaterThan(canApplyIndex);
    }

    [Test]
    public void FriendlyProtectionLinks_MatchBibleRangesAndPreventStacking()
    {
        var root = FindRepositoryRoot();

        AssertAbilitySourceContains(root, "Katar", "SteelShoulderAbilityDefinition.cs", "builder.Create(FeatType.TwinGuardStance1, PerkType.TwinGuardStance)");
        AssertAbilitySourceContains(root, "Katar", "SteelShoulderAbilityDefinition.cs", "FriendlyTargetStatusEffectFactory = () => new GuardedStatusEffect(50, 5.0f)");
        AssertAbilitySourceContains(root, "Katar", "SteelShoulderAbilityDefinition.cs", "FriendlyTargetStatusPersistsUntilBroken = true");
        AssertAbilitySourceContains(root, "Katar", "SteelShoulderAbilityDefinition.cs", "Animation.DoubleStrike,\r\n                0.0f,");
        AssertAbilitySourceContains(root, "Katar", "TagInAbilityDefinition.cs", "builder.Create(FeatType.TwinIntercept1, PerkType.TwinIntercept)");
        AssertAbilitySourceContains(root, "Katar", "TagInAbilityDefinition.cs", "RequiresGuardedTarget = true");
        AssertAbilitySourceContains(root, "Katar", "TagInAbilityDefinition.cs", "FriendlyTargetTemporaryHPPercent = 15");
        AssertAbilitySourceContains(root, "Katar", "TagInAbilityDefinition.cs", "SelfGuardPercent = 20");
        AssertAbilitySourceContains(root, "Katar", "WhirlingGuardAbilityDefinition.cs", "SelfStatusAlsoAppliesToGuardedTarget = true");
        AssertAbilitySourceContains(root, "Katar", "AdamantineGuardAbilityDefinition.cs", "typeof(AdamantineGuardStatusEffect)");
        AssertAbilitySourceDoesNotContain(root, "Katar", "AdamantineGuardAbilityDefinition.cs", "FriendlyTargetStatusEffectFactory");

        AssertStatusSourceContains(root, "WardBondStatusEffect.cs", "StatusEffect.HasStatusEffect(creature, typeof(GuardedStatusEffect))");
        AssertStatusSourceContains(root, "WardBondStatusEffect.cs", "Only one ward or guard link can protect a target.");
        AssertStatusSourceContains(root, "WardBondStatusEffect.cs", "StatType.WardTargetPhysicalDefensePercentAdjustment");
        AssertStatusSourceContains(root, "WardBondStatusEffect.cs", "StatType.WardTargetForceDefensePercentAdjustment");
        AssertStatusSourceContains(root, "WardBondStatusEffect.cs", "SourceWardDefenseRefreshSeconds");
        AssertStatusSourceContains(root, "GuardedStatusEffect.cs", "StatusEffect.HasStatusEffect(creature, typeof(GuardedStatusEffect), Source)");
        AssertStatusSourceContains(root, "GuardedStatusEffect.cs", "Party.IsInParty(Source, creature)");
        AssertStatusSourceContains(root, "GuardedStatusEffect.cs", "Only one ward or guard link can protect a target.");
        AssertStatusSourceContains(root, "GuardedStatusEffect.cs", "PlayerName.GetColoredDisplayName(guarded, source)");
        AssertStatusSourceContains(root, "GuardedStatusEffect.cs", "PlayerName.GetColoredDisplayName(source, guarded)");
        AssertStatusSourceContains(root, "GuardedStatusEffect.cs", "TemporaryStatModifier.GetStatAdjustment(source, StatType.Guard, GuardShareGroup)");
        AssertStatusSourceContains(root, "GuardedStatusEffect.cs", "Combat.ApplyLowHPGuardEffectFromProtectedTarget(Source, defender, damage)");

        var combatSource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "Combat.cs"));
        var statusEffectSource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "StatusEffect.cs"));
        combatSource.Should().Contain("StatusEffect.OnGuardedHit(defender, attacker, preventedDamage)");
        statusEffectSource.Should().Contain("OfType<IGuardedHitStatusEffect>()");

        var steelShoulder = new SteelShoulderAbilityDefinition().BuildAbilities()[FeatType.TwinGuardStance1];
        steelShoulder.RequiresTarget.Should().BeTrue();

        var tagIn = new TagInAbilityDefinition().BuildAbilities()[FeatType.TwinIntercept1];
        tagIn.IsSingleTargetAbility.Should().BeTrue();
        tagIn.RequiresTarget.Should().BeFalse();

        var featRows = Read2da(root, "SWLOR_Haks", "sw_2da", "feat.2da");
        var tagInFeat = featRows[(int)FeatType.TwinIntercept1];
        tagInFeat["TARGETSELF"].Should().Be("1",
            "Tag In activates immediately and resolves the current Guarded ally on the server");
        tagInFeat["HostileFeat"].Should().Be("****");

        var generator = File.ReadAllText(Path.Combine(root.FullName, "tools", "GenerateWeaponArchetypeImplementation.py"));
        generator.Should().Contain("is_automatic_guarded_target_active(row[\"Description\"])",
            "regeneration must preserve Tag In's no-cursor feat metadata");
    }

    [Test]
    public void Flash_IsAStatusAndEnmityAreaWithoutWeaponDamage()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "AbilityDefinition",
            "HeavyVibroblade",
            "FlashAbilityDefinition.cs"));

        source.Should().Contain("statusEffectFactory: () => new FlashStatusEffect(20)");
        source.Should().Contain("damagePercentAdjustment: _ => -100",
            "the weapon-skill combat pipeline otherwise adds weapon damage even when base damage is zero");
        source.Should().Contain("enmityBonus: 650");
        source.Should().Contain("canCritical: false");
    }

    [Test]
    public void ConditionalInterruptionAbilities_AreGeneratedAsTargetActivityPayoffs()
    {
        var root = FindRepositoryRoot();
        var spearSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "AbilityDefinition",
            "Spear",
            "InterruptionStrikeAbilityDefinition.cs"));
        var pistolSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "AbilityDefinition",
            "Pistol",
            "InterruptingShotAbilityDefinition.cs"));

        spearSource.Should().Contain("TargetUsingAbilityDrainFP = 4");
        spearSource.Should().Contain("TargetUsingAbilityDrainStamina = 4");
        spearSource.Should().NotContain("DrainFPOnHit = 4");
        pistolSource.Should().Contain("TargetUsingAbilityStatusEffect = typeof(DisorientedStatusEffect)");
        pistolSource.Should().Contain("TargetUsingAbilityStatusDurationSeconds = 30");
    }

    private static string ReadPerkDefinition(string fileName)
    {
        var root = FindRepositoryRoot();
        return File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "PerkDefinition",
            fileName));
    }

    private static void AssertSourceStat(string fileName, StatType statType, string valueExpression)
    {
        var source = ReadPerkDefinition(fileName);

        source.Should().Contain($".IncreasesStat(StatType.{statType}, {valueExpression})");
    }

    private static void AssertSourceContains(string fileName, string expectedSource)
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "PerkDefinition",
            fileName));

        source.Should().Contain(expectedSource);
    }

    private static void AssertAbilitySourceContains(
        DirectoryInfo root,
        string skillFolder,
        string fileName,
        string expectedSource)
    {
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "AbilityDefinition",
            skillFolder,
            fileName));

        source.Should().Contain(expectedSource);
    }

    private static void AssertAbilitySourceDoesNotContain(
        DirectoryInfo root,
        string skillFolder,
        string fileName,
        string unexpectedSource)
    {
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "AbilityDefinition",
            skillFolder,
            fileName));

        source.Should().NotContain(unexpectedSource);
    }

    private static void AssertStatusSourceContains(
        DirectoryInfo root,
        string fileName,
        string expectedSource)
    {
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "StatusEffectDefinition",
            fileName));

        source.Should().Contain(expectedSource);
    }

    private static void AssertStatusStat(IStatusEffect statusEffect, StatType statType, int value)
    {
        statusEffect.StatGroup.Stats[statType].Should().Be(value);
    }

    private static void AssertFeatSpellTargeting(
        IReadOnlyDictionary<int, Dictionary<string, string>> featRows,
        IReadOnlyDictionary<int, Dictionary<string, string>> spellRows,
        FeatType featType,
        string targetSelf,
        string hostileFeat,
        string range,
        string targetType,
        string hostileSetting)
    {
        var featRow = featRows[(int)featType];
        featRow["TARGETSELF"].Should().Be(targetSelf);
        featRow["HostileFeat"].Should().Be(hostileFeat);

        var spellRow = spellRows[int.Parse(featRow["SPELLID"])];
        spellRow["Range"].Should().Be(range);
        spellRow["TargetType"].Should().Be(targetType);
        spellRow["HostileSetting"].Should().Be(hostileSetting);
        spellRow["TargetShape"].Should().Be("****");
        spellRow["TargetFlags"].Should().Be("****");
    }

    private static Dictionary<int, Dictionary<string, string>> Read2da(
        DirectoryInfo root,
        params string[] segments)
    {
        var path = Path.Combine(new[] { root.FullName }.Concat(segments).ToArray());
        var lines = File.ReadAllLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
        var header = lines[1].Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
        var result = new Dictionary<int, Dictionary<string, string>>();

        foreach (var line in lines.Skip(2))
        {
            var cells = line.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
            if (!int.TryParse(cells[0], out var row))
                continue;

            var values = new Dictionary<string, string>();
            for (var index = 0; index < header.Length && index + 1 < cells.Length; index++)
            {
                values[header[index]] = cells[index + 1];
            }

            result[row] = values;
        }

        return result;
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("repository root should be discoverable from the test directory");
        return directory!;
    }
}
