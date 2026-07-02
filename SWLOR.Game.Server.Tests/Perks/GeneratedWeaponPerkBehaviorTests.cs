using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Tests.Perks;

public class GeneratedWeaponPerkBehaviorTests
{
    [Test]
    public void GeneratedWeaponTraitPerks_EmitRepresentativeStatBonuses()
    {
        AssertSourceStat("VibrobladePerkDefinition.cs", StatType.AutoAttackDamageBonusChance, "10");
        AssertSourceStat("VibrobladePerkDefinition.cs", StatType.AutoAttackDamageBonus, "8");
        AssertSourceStat("VibrobladePerkDefinition.cs", StatType.RiotBladeSecondaryDamageBonus, "12");
        AssertSourceContains("VibrobladePerkDefinition.cs", "EquipmentPredicates.HasOffHandShield(creature) ? 35 : 0");

        AssertSourceStat("VibroknifePerkDefinition.cs", StatType.CriticalBleedingStatusDurationExtensionSeconds, "6");
        AssertSourceStat("VibroknifePerkDefinition.cs", StatType.CriticalBleedingStatusDurationExtensionCooldownSeconds, "8");
        AssertSourceStat("VibroknifePerkDefinition.cs", StatType.HostileAbilitySequenceWindowSeconds, "30");
        AssertSourceStat("VibroknifePerkDefinition.cs", StatType.HostileAbilitySequenceNextAttackBleedDurationSeconds, "30");
        AssertSourceStat("VibroknifePerkDefinition.cs", StatType.SameTargetHostileAbilityHitCountRequired, "3");
        AssertSourceStat("VibroknifePerkDefinition.cs", StatType.SameTargetHostileAbilityStaminaRestore, "4");
        AssertSourceStat("VibroknifePerkDefinition.cs", StatType.DamageToSourceAppliedStatusTargetCategory, "(int)StatusEffectCategory.Debuff");
        AssertSourceStat("VibroknifePerkDefinition.cs", StatType.DamageToSourceAppliedStatusTargetPercentAdjustment, "10");

        AssertSourceStat("HeavyVibrobladePerkDefinition.cs", StatType.HeavyVibrobladeOffenseEssenceHunter, "1");
        AssertSourceStat("HeavyVibrobladePerkDefinition.cs", StatType.HeavyVibrobladeOffenseSoulAscension, "1");
        AssertSourceStat("HeavyVibrobladePerkDefinition.cs", StatType.HeavyVibrobladeDefenseDamageDealtHPPercentRestore, "1");

        AssertSourceStat("LightsaberPerkDefinition.cs", StatType.DeflectionNearbyAllyGuard, "10");
        AssertSourceStat("LightsaberPerkDefinition.cs", StatType.DeflectionNearbyAllyGuardDurationSeconds, "30");
        AssertSourceStat("LightsaberPerkDefinition.cs", StatType.WardSharedDamageNextSkillAbilityDamageBonus, "10");
        AssertSourceStat("LightsaberPerkDefinition.cs", StatType.WardSharedDamageNextSkillAbilityWindowSeconds, "30");

        AssertSourceStat("RiflePerkDefinition.cs", StatType.IdleSkillAbilitySkillType, "(int)SkillType.Rifle");
        AssertSourceStat("RiflePerkDefinition.cs", StatType.IdleSkillAbilityDamageBonus, "14");
        AssertSourceStat("RiflePerkDefinition.cs", StatType.IdleSkillAbilityHitChancePercentAdjustment, "8");
        AssertSourceStat("RiflePerkDefinition.cs", StatType.IdleSkillAbilityCriticalDamagePercentAdjustment, "15");
        AssertSourceStat("RiflePerkDefinition.cs", StatType.RepeatedTargetDamageBonusPerHit, "3");
        AssertSourceStat("RiflePerkDefinition.cs", StatType.RepeatedTargetDamageBonusMax, "15");
        AssertSourceStat("RiflePerkDefinition.cs", StatType.RepeatedTargetDamageDurationSeconds, "30");
        AssertSourceStat("RiflePerkDefinition.cs", StatType.AutoAttackSuppressionStackChance, "15");
        AssertSourceStat("RiflePerkDefinition.cs", StatType.AutoAttackSuppressionStackDurationSeconds, "30");
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
        AssertSourceStat("ThrowingPerkDefinition.cs", StatType.BleedingTargetAbilityBleedDurationExtensionSeconds, "6");
        AssertSourceStat("ThrowingPerkDefinition.cs", StatType.TargetLowHPStatusDamageThresholdPercent, "50");
        AssertSourceStat("ThrowingPerkDefinition.cs", StatType.TargetLowHPStatusDamageStatusCategory, "(int)StatusEffectCategory.Bleeding");
        AssertSourceStat("ThrowingPerkDefinition.cs", StatType.TargetLowHPStatusDamagePercentAdjustment, "20");

        AssertSourceStat("TwinBladePerkDefinition.cs", StatType.BleedingTargetAbilityBleedSpreadChance, "35");
        AssertSourceStat("TwinBladePerkDefinition.cs", StatType.BleedingTargetAbilityBleedSpreadDurationSeconds, "30");
        AssertSourceStat("TwinBladePerkDefinition.cs", StatType.DefeatedBleedingEnemyNearbyBleedDurationSeconds, "30");
        AssertSourceStat("TwinBladePerkDefinition.cs", StatType.TargetStatusCriticalRateStatusCategory, "(int)StatusEffectCategory.Bleeding");

        AssertSourceStat("SaberstaffPerkDefinition.cs", StatType.AbilityStaminaCostFPRestorePercentSkillType, "(int)SkillType.Saberstaff");
        AssertSourceStat("SaberstaffPerkDefinition.cs", StatType.AbilityStaminaCostFPRestorePercent, "35");
        AssertSourceStat("SaberstaffPerkDefinition.cs", StatType.AbilityFPCostStaminaRestorePercentSkillType, "(int)SkillType.Force");
        AssertSourceStat("SaberstaffPerkDefinition.cs", StatType.AbilityFPCostStaminaRestorePercent, "35");
        AssertSourceStat("SaberstaffPerkDefinition.cs", StatType.HighFPAndStaminaAbilityDamageBonus, "12");
        AssertSourceStat("SaberstaffPerkDefinition.cs", StatType.AbilityRestoredFPHastePercentAdjustment, "10");
        AssertSourceStat("SaberstaffPerkDefinition.cs", StatType.AreaAbilityAfterDeflectionDamagePercentAdjustment, "10");
        AssertSourceStat("SaberstaffPerkDefinition.cs", StatType.AbilityGrantedAttackDeflectionFPRestore, "2");

        AssertSourceStat("KatarPerkDefinition.cs", StatType.Guard, "35");
        AssertSourceStat("KatarPerkDefinition.cs", StatType.GuardDamageReductionPercentAdjustment, "10");
        AssertSourceStat("KatarPerkDefinition.cs", StatType.GuardedHitNextSkillAbilityDamageBonus, "10");
        AssertSourceStat("KatarPerkDefinition.cs", StatType.GuardedHitNextKatarAbilityDamageBonus, "35");
        AssertSourceStat("KatarPerkDefinition.cs", StatType.KatarIronGuardCoveringClaws, "1");
        AssertSourceStat("KatarPerkDefinition.cs", StatType.LowHPGuard, "25");
        AssertSourceStat("KatarPerkDefinition.cs", StatType.StatusAppliedRequiredCategory, "(int)StatusEffectCategory.Control");
        AssertSourceStat("KatarPerkDefinition.cs", StatType.StatusAppliedSelfEnmityPercentAdjustment, "15");
        AssertSourceStat("KatarPerkDefinition.cs", StatType.OutgoingDebuffDurationPercentAdjustment, "20");
        AssertSourceStat("KatarPerkDefinition.cs", StatType.DamageTakenNextSkillAbilityDamageBonus, "20");

        AssertSourceStat("StaffPerkDefinition.cs", StatType.CriticalDamageTargetStatusCategory, "(int)StatusEffectCategory.Control");
        AssertSourceStat("StaffPerkDefinition.cs", StatType.StatusAppliedNextSkillAbilityDamageBonus, "26");
        AssertSourceStat("StaffPerkDefinition.cs", StatType.StaffSentinelGuard, "1");
        AssertSourceStat("StaffPerkDefinition.cs", StatType.StaffSentinelGuardingStep, "1");
        AssertSourceStat("StaffPerkDefinition.cs", StatType.AbilityUsedAttackDeflection, "8");

        AssertSourceStat("PistolPerkDefinition.cs", StatType.RangedAbilityHitNearTargetDamageDealtPercentAdjustment, "-10");
        AssertSourceStat("PistolPerkDefinition.cs", StatType.AutoAttackCycleCriticalRateRequiredCount, "4");
        AssertSourceStat("PistolPerkDefinition.cs", StatType.CriticalDamageHighHPTargetPercentAdjustment, "15");

        AssertSourceStat("SpearPerkDefinition.cs", StatType.CostlyAbilityDamageBonus, "14");
        AssertSourceStat("SpearPerkDefinition.cs", StatType.CostlyAbilityHitStaminaRestore, "3");
        AssertSourceStat("SpearPerkDefinition.cs", StatType.AbilityDamageToSourceAppliedStatusTargetCategory, "(int)StatusEffectCategory.Control");
        AssertSourceStat("SpearPerkDefinition.cs", StatType.AbilityDamageToSourceAppliedStatusTargetPercentAdjustment, "10");
        AssertSourceStat("VibrobladePerkDefinition.cs", StatType.ShieldEquippedPhysicalDefensePercentAdjustment, "12");
    }

    [Test]
    public void GeneratedWeaponStances_EmitBibleDrivenStatusStats()
    {
        var debilitating = new DebilitatingStanceStatusEffect();
        AssertStatusStat(debilitating, StatType.AttackPercentAdjustment, -10);
        AssertStatusStat(debilitating, StatType.DamageDealtHamstringSkillType, (int)SkillType.Vibroknife);
        AssertStatusStat(debilitating, StatType.DamageDealtHamstringChance, 100);
        AssertStatusStat(debilitating, StatType.DamageDealtHamstringDurationSeconds, 30);

        var berserker = new BerserkerStanceStatusEffect();
        berserker.ApplyEffect(1, 1, -1);
        AssertStatusStat(berserker, StatType.AttackPercentAdjustment, 15);
        AssertStatusStat(berserker, StatType.AttackDelayReductionPercent, 10);
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

        var infiniteConduit = new InfiniteConduitStatusEffect();
        AssertStatusStat(infiniteConduit, StatType.AbilityStaminaCostFPRestorePercentSkillType, (int)SkillType.Saberstaff);
        AssertStatusStat(infiniteConduit, StatType.AbilityStaminaCostFPRestorePercent, 50);
        AssertStatusStat(infiniteConduit, StatType.AbilityFPCostStaminaRestorePercentSkillType, (int)SkillType.Force);
        AssertStatusStat(infiniteConduit, StatType.AbilityFPCostStaminaRestorePercent, 50);
        AssertStatusStat(infiniteConduit, StatType.HighFPAndStaminaAbilityDamageBonusThresholdPercent, 70);
        AssertStatusStat(infiniteConduit, StatType.HighFPAndStaminaAbilityDamageBonus, 20);
    }

    [Test]
    public void DamageDealtHamstringStats_AreDeclaredAndConsumedBySharedCombat()
    {
        Stat.GetStatTypeCategory(StatType.DamageDealtHamstringSkillType).Should().Be(StatTypeCategory.NonBeneficial);
        Stat.GetStatTypeCategory(StatType.DamageDealtHamstringChance).Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.DamageDealtHamstringDurationSeconds).Should().Be(StatTypeCategory.NonBeneficial);

        var root = FindRepositoryRoot();
        var combatSource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "Combat.cs"));
        combatSource.Should().Contain("ApplyDamageDealtHamstringEffect(attacker, defender, skillType, damageType)");
        combatSource.Should().Contain("StatType.DamageDealtHamstringSkillType");
        combatSource.Should().Contain("typeof(HamstringStatusEffect)");
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
        Stat.GetStatTypeCategory(StatType.BleedingTargetAbilityBleedSpreadChance)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.AvoidedAttackNextAutoAttackNoDelaySkillType)
            .Should().Be(StatTypeCategory.NonBeneficial);
        Stat.GetStatTypeCategory(StatType.AutoAttackSuppressionStackChance)
            .Should().Be(StatTypeCategory.BeneficialWhenPositive);
        Stat.GetStatTypeCategory(StatType.SuppressionStackDamageBonusAdjustment)
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

        var root = FindRepositoryRoot();
        var combatSource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "Combat.cs"));
        combatSource.Should().Contain("ApplyCriticalBleedingStatusDurationExtension(attacker, defender)");
        combatSource.Should().Contain("ApplyHostileAbilitySequenceEffects(activator, feat, ability)");
        combatSource.Should().Contain("ApplySameTargetHostileAbilityHitEffects(activator, target, ability)");
        combatSource.Should().Contain("ApplyNextDamageDealtBleedEffect(attacker, defender, damageType)");
        combatSource.Should().Contain("ApplyBleedingTargetAbilityBleedSpread(attacker, defender, skillType, damageType)");
        combatSource.Should().Contain("ApplyRangedHitSuppressionStack(activator, target, skillType, damageType)");
        combatSource.Should().Contain("ApplyAvoidedAttackNextAutoAttackNoDelay(creature)");
        combatSource.Should().Contain("ApplyBleedingStatusExpiredEffects(uint source)");
        combatSource.Should().Contain("ApplyCostlyAbilityHitEffects(activator, target, ability, skillType)");
        combatSource.Should().Contain("ApplyDeflectionNearbyAllyGuard(creature)");
        combatSource.Should().Contain("ApplyAbilityGrantedAttackDeflectionEffects(activator)");
        combatSource.Should().Contain("ApplyTargetLowHPStatusDamageModifier(attacker, defender, damage)");
        combatSource.Should().Contain("ApplyDamageTakenShareToStatusSource(defender, attacker, damage, damageType)");
        combatSource.Should().Contain("IdleSkillAbilityCriticalDamagePercentAdjustment");
        combatSource.Should().Contain("TargetHasSourceAppliedStatusCategory(defender, attacker, category)");

        var bleedSource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "StatusEffectDefinition", "BleedStatusEffect.cs"));
        var suppressionSource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "StatusEffectDefinition", "SuppressionStatusEffect.cs"));
        bleedSource.Should().Contain("WasNaturallyExpired");
        combatSource.Should().Contain("typeof(HemorrhageStatusEffect)");
        File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "StatusEffectDefinition", "LacerationStatusEffect.cs")).Should().BeFalse();
        File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "StatusEffectDefinition", "RupturedStatusEffect.cs")).Should().BeFalse();
        suppressionSource.Should().Contain("StatusEffectStackType.UnlimitedStacking");
    }

    [Test]
    public void GeneratedWeaponActiveProfiles_EmitConditionalAndTemporaryRiders()
    {
        var root = FindRepositoryRoot();

        AssertAbilitySourceContains(root, "Saberstaff", "FocusedArcAbilityDefinition.cs", "HighResourceExtraDamageThresholdPercent = 60");
        AssertAbilitySourceContains(root, "Saberstaff", "FocusedArcAbilityDefinition.cs", "ExtraDamageIfHighResources = 10");
        AssertAbilitySourceContains(root, "Saberstaff", "GuardedChannelAbilityDefinition.cs", "SelfStatResourceAboveThresholdPercent = 40");
        AssertAbilitySourceContains(root, "Saberstaff", "SeverFocusAbilityDefinition.cs", "DrainTargetResourceAboveThresholdPercent = 80");
        AssertAbilitySourceContains(root, "Saberstaff", "InfiniteConduitAbilityDefinition.cs", "typeof(InfiniteConduitStatusEffect)");
        AssertAbilitySourceContains(root, "Lightsaber", "WardBondAbilityDefinition.cs", "FriendlyTargetStatusEffectFactory = () => new WardBondStatusEffect(45, 12, 12, 0, 8.0f)");
        AssertAbilitySourceContains(root, "Lightsaber", "SaberStormAbilityDefinition.cs", "ExtraDamageTargetStatusEffect = typeof(SunderStatusEffect)");
        AssertAbilitySourceContains(root, "Lightsaber", "SaberStormAbilityDefinition.cs", "ExtraDamageIfTargetStatusEffect = 30");
        AssertAbilitySourceContains(root, "Lightsaber", "SaberStormAbilityDefinition.cs", "ConditionalTargetStatusEffect = typeof(SunderStatusEffect)");
        AssertAbilitySourceContains(root, "Lightsaber", "SaberStormAbilityDefinition.cs", "AbilityTargetingShapeType.None");
        AssertAbilitySourceDoesNotContain(root, "Lightsaber", "SaberStormAbilityDefinition.cs", "AbilityTargetingShapeType.Sphere");
        AssertAbilitySourceContains(root, "Katar", "SteelShoulderAbilityDefinition.cs", "FriendlyTargetStatusEffectFactory = () => new GuardedStatusEffect(50, 5.0f)");
        AssertAbilitySourceContains(root, "Katar", "SteelShoulderAbilityDefinition.cs", "FriendlyTargetStatusPersistsUntilBroken = true");
        AssertAbilitySourceContains(root, "Katar", "TagInAbilityDefinition.cs", "RequiresGuardedTarget = true");
        AssertAbilitySourceContains(root, "Katar", "TagInAbilityDefinition.cs", "FriendlyTargetTemporaryHPPercent = 15");
        AssertAbilitySourceContains(root, "Katar", "TagInAbilityDefinition.cs", "SelfGuardPercent = 20");
        AssertAbilitySourceContains(root, "Katar", "WhirlingGuardAbilityDefinition.cs", "SelfStatusAlsoAppliesToGuardedTarget = true");
        AssertAbilitySourceContains(root, "Vibroblade", "RiotBladeAbilityDefinition.cs", "StatType.RiotBladeSecondaryDamageBonus");
        AssertAbilitySourceContains(root, "HeavyVibroblade", "HeavyVibrobladeActiveAbilityDefinitionBase.cs", "ApplyEssenceHunter");
        AssertAbilitySourceContains(root, "HeavyVibroblade", "SoulBurstAbilityDefinition.cs", "afterSuccessfulHit");
        AssertAbilitySourceContains(root, "Pistol", "LastWordAbilityDefinition.cs", "TemporaryAvoidedAttackNextAutoAttackNoDelaySkillType = (int)SkillType.Pistol");
        AssertAbilitySourceContains(root, "Rifle", "SuppressingShotAbilityDefinition.cs", "ApplySuppressionStackOnHit = true");
        AssertAbilitySourceContains(root, "Rifle", "SuppressiveLineAbilityDefinition.cs", "SuppressionDisorientedRequiredStacks = 2");
        AssertAbilitySourceContains(root, "Rifle", "KillBoxAbilityDefinition.cs", "TemporarySuppressionStackDamageBonusAdjustment = 3");
        AssertAbilitySourceContains(root, "TwinBlade", "TempestBloomAbilityDefinition.cs", "TemporaryAreaAbilityFragmentationDamage = 8");
        AssertAbilitySourceContains(root, "Throwing", "RainOfSteelAbilityDefinition.cs", "TemporaryAreaAbilityFragmentationPulseSeconds = 6");
        AssertAbilitySourceContains(root, "Saberstaff", "SaberCycloneAbilityDefinition.cs", "TemporarySaberstaffAreaAbilityAttackDeflection = 8");
        AssertAbilitySourceContains(root, "Staff", "ShelterCircleAbilityDefinition.cs", "NearbyPartyStatusEffect = typeof(ShelterCircleStatusEffect)");
        AssertAbilitySourceContains(root, "Staff", "ShelterCircleAbilityDefinition.cs", "NearbyPartyStatusIncludesSelf = true");
        AssertAbilitySourceContains(root, "Staff", "UnmovingCenterAbilityDefinition.cs", "SelfKnockdownDazedImmunityDurationSeconds = 45");
        AssertAbilitySourceContains(root, "Spear", "CripplingDefenseAbilityDefinition.cs", "TemporaryCostlyAbilityExposedDurationSeconds = 30");
        AssertAbilitySourceContains(root, "Staff", "WorldbreakerAbilityDefinition.cs", "RequiredTargetStatusCategoryForConditionalStatus = StatusEffectCategory.Control");
        AssertAbilitySourceContains(root, "Vibroknife", "VitalRuptureAbilityDefinition.cs", "RequiredTargetStatusCategoryForConditionalStatus = StatusEffectCategory.Bleeding");
        AssertAbilitySourceContains(root, "Vibroknife", "RuptureStrikeAbilityDefinition.cs", "ConsumeBleedIntoHemorrhage = true");
        AssertAbilitySourceContains(root, "TwinBlade", "RedBloomAbilityDefinition.cs", "SpreadHemorrhageFromTarget = true");
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
        source.Should().Contain("AbilityTargeting.ValidateFriendlyTarget(activator, target, false)");
        source.Should().Contain("GuardedStatusEffect.IsActiveGuardedBySource(target, activator)");
        source.Should().Contain("ApplyFriendlyTargetEffects(activator, target)");
        source.Should().Contain("statusEffect.ReassignSource(activator)");
        source.Should().Contain("return profile.ValidateFriendlyTargetStatus(activator, target)");

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

        AssertAbilitySourceContains(root, "Lightsaber", "WardBondAbilityDefinition.cs", "FriendlyTargetStatusEffectFactory = () => new WardBondStatusEffect(45, 12, 12, 0, 8.0f)");
        AssertAbilitySourceContains(root, "Lightsaber", "WardBondAbilityDefinition.cs", "Animation.DoubleStrike,\r\n                8.0f,");
        AssertAbilitySourceContains(root, "Lightsaber", "GuardianMasterAbilityDefinition.cs", "FriendlyTargetStatusEffectFactory = () => new WardBondStatusEffect(50, 15, 15, 15, 8.0f)");
        AssertAbilitySourceContains(root, "Lightsaber", "GuardianMasterAbilityDefinition.cs", "Animation.DoubleStrike,\r\n                8.0f,");

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

    private static void AssertSourceStat(string fileName, StatType statType, string valueExpression)
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "PerkDefinition",
            fileName));

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
