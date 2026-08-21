using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.Service.StatService
{
    public enum StatType
    {
        /// <summary>
        /// No stat. Used as a sentinel when no valid stat type applies.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        Invalid = 0,

        /// <summary>
        /// Percent adjustment applied to final Attack. Positive values increase Attack; negative values reduce it.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AttackPercentAdjustment = 1,

        /// <summary>
        /// Percent adjustment applied to all final Defense calculations before damage-type-specific adjustments.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DefensePercentAdjustment = 2,

        /// <summary>
        /// Percent adjustment applied to final Defense when defending against physical damage.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        PhysicalDefensePercentAdjustment = 3,

        /// <summary>
        /// Percent adjustment applied to final Defense when defending against Force damage.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        ForceDefensePercentAdjustment = 4,

        /// <summary>
        /// Percent adjustment applied to final Accuracy.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AccuracyPercentAdjustment = 5,

        /// <summary>
        /// Percent adjustment applied to final Evasion.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        EvasionPercentAdjustment = 6,

        /// <summary>
        /// Flat percent chance to deflect a hostile melee weapon auto-attack while wielding a weapon and no shield.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive, deflectionSource: DeflectionSource.Melee)]
        MeleeDeflection = 7,

        /// <summary>
        /// Flat Stamina restored when the creature successfully uses Melee Deflection.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive, deflectionSource: DeflectionSource.Melee)]
        MeleeDeflectionStaminaRestore = 8,

        /// <summary>
        /// Flat FP restored when the creature successfully uses Ranged Deflection.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive, deflectionSource: DeflectionSource.Ranged)]
        DeflectionFPRestore = 9,

        /// <summary>
        /// Percent of maximum Stamina restored after any successful melee, ranged, or shield deflection.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DeflectionStaminaRestorePercent = 10,

        /// <summary>
        /// Percent adjustment applied to FP costs. Positive values increase cost; negative values reduce it.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenNegative)]
        FPCostPercentAdjustment = 11,

        /// <summary>
        /// Flat adjustment applied to FP costs after percent adjustment.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenNegative)]
        FPCostFlatAdjustment = 12,

        /// <summary>
        /// Flat bonus added to Attack before percent adjustments.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        Attack = 13,

        /// <summary>
        /// Flat bonus added to all Defense calculations before damage-type-specific bonuses and percent adjustments.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        Defense = 14,

        /// <summary>
        /// Flat bonus added to Defense against physical damage.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        PhysicalDefense = 15,

        /// <summary>
        /// Flat bonus added to Defense against Force damage.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        ForceDefense = 16,

        /// <summary>
        /// Flat bonus added to Accuracy before percent adjustments.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        Accuracy = 17,

        /// <summary>
        /// Flat bonus added to Evasion before percent adjustments.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        Evasion = 18,

        /// <summary>
        /// Flat bonus added to maximum FP.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        MaxFP = 19,

        /// <summary>
        /// Flat bonus added to maximum Stamina.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        MaxStamina = 20,

        /// <summary>
        /// Flat bonus added to Defense against fire damage.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        FireDefense = 21,

        /// <summary>
        /// Flat bonus added to Defense against poison damage.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        PoisonDefense = 22,

        /// <summary>
        /// Flat bonus added to Defense against electrical damage.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        ElectricalDefense = 23,

        /// <summary>
        /// Flat bonus added to Defense against ice damage.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        IceDefense = 24,

        /// <summary>
        /// Percent adjustment applied to incoming damage. Positive values increase damage taken; negative values reduce it.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenNegative)]
        DamageTakenPercentAdjustment = 25,

        /// <summary>
        /// Flat adjustment applied to incoming damage after percent adjustment.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenNegative)]
        DamageTakenFlatAdjustment = 26,

        /// <summary>
        /// Percent reduction to attack delay. Negative values increase attack delay. Total adjustment is capped by combat delay logic.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AttackDelayReductionPercent = 27,

        /// <summary>
        /// Flat seconds added to ability activation delay. Negative values reduce activation time.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenNegative)]
        ActivationDelayFlatAdjustment = 28,

        /// <summary>
        /// Percent adjustment to earned skill experience.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        ExperiencePercentAdjustment = 29,

        /// <summary>
        /// Temporary bonus language rank used when checking whether a listener understands speech.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        LanguageComprehension = 30,

        /// <summary>
        /// Flat HP regenerated by effects that read HP regeneration stats.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        HPRegen = 31,

        /// <summary>
        /// Flat FP regenerated by effects that read FP regeneration stats.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        FPRegen = 32,

        /// <summary>
        /// Flat Stamina regenerated by effects that read Stamina regeneration stats.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        StaminaRegen = 33,

        /// <summary>
        /// Flat regeneration bonus applied during rest regeneration effects.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        RestRegen = 34,

        /// <summary>
        /// Percent adjustment applied to creature movement speed.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        MovementSpeedPercentAdjustment = 36,

        /// <summary>
        /// If greater than zero, piloting module effectiveness can use Willpower instead of Perception when Willpower is higher.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        UseWillpowerForPilotingModuleEffectiveness = 37,

        /// <summary>
        /// Percent-point adjustment added to SWLOR critical hit chance.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        CriticalRatePercentAdjustment = 38,

        /// <summary>
        /// Staff-only override for the ability score used to calculate weapon damage. Stores AbilityType as value plus one.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        StaffDamageAbilityOverride = 39,

        /// <summary>
        /// Staff-only override for the ability score used to calculate weapon accuracy. Stores AbilityType as value plus one.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        StaffAccuracyAbilityOverride = 40,

        /// <summary>
        /// Multiplier for the attacker's positive Might modifier added as staff damage bonus.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        StaffMightModifierDamageMultiplier = 41,

        /// <summary>
        /// Flat bonus added to Mind resistance.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        MindResistance = 42,

        /// <summary>
        /// Flat bonus added to Trauma resistance.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        TraumaResistance = 43,

        /// <summary>
        /// Flat bonus added to Mobility resistance.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        MobilityResistance = 44,

        /// <summary>
        /// Percent adjustment applied to damage after a successful SWLOR critical hit.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        CriticalDamagePercentAdjustment = 45,

        /// <summary>
        /// Percent adjustment applied to generated enmity.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        EnmityPercentAdjustment = 46,

        /// <summary>
        /// Temporary percent Evasion adjustment applied after a successful Shield Deflection.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive, deflectionSource: DeflectionSource.Shield)]
        DeflectionEvasionPercentAdjustment = 47,

        /// <summary>
        /// Temporary percent Enmity adjustment paired with the deflection Evasion effect.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive, deflectionSource: DeflectionSource.Shield)]
        DeflectionEvasionEnmityPercentAdjustment = 48,

        /// <summary>
        /// Temporary percent Enmity adjustment applied after a successful ranged attack deflection.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive, deflectionSource: DeflectionSource.Ranged)]
        DeflectionEnmityPercentAdjustment = 49,

        /// <summary>
        /// Temporary percent Physical Defense adjustment applied after a successful ranged attack deflection.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive, deflectionSource: DeflectionSource.Ranged)]
        DeflectionDefensePercentAdjustment = 50,

        /// <summary>
        /// Temporary percent Force Defense adjustment applied after a successful ranged attack deflection.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive, deflectionSource: DeflectionSource.Ranged)]
        DeflectionForceDefensePercentAdjustment = 51,

        /// <summary>
        /// Target current HP threshold percent required for low-HP bonus damage to apply.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        TargetLowHPDamageThresholdPercent = 52,

        /// <summary>
        /// Percent damage adjustment applied when damaging a target at or below the low-HP threshold.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        TargetLowHPDamagePercentAdjustment = 53,

        /// <summary>
        /// Percent chance for an auto-attack to add AutoAttackDamageBonus damage.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AutoAttackDamageBonusChance = 54,

        /// <summary>
        /// Flat damage added to an auto-attack when AutoAttackDamageBonusChance succeeds.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AutoAttackDamageBonus = 55,

        /// <summary>
        /// Flat Stamina restored after a critical hit, subject to CriticalStaminaRestoreCooldownSeconds.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        CriticalStaminaRestore = 56,

        /// <summary>
        /// Cooldown in seconds for CriticalStaminaRestore.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        CriticalStaminaRestoreCooldownSeconds = 57,

        /// <summary>
        /// Percent of critical-hit damage removed from the target's FP.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        CriticalTargetFPLossPercentOfDamage = 58,

        /// <summary>
        /// Percent damage adjustment against targets affected by Sunder.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DamageToSunderedTargetPercentAdjustment = 59,

        /// <summary>
        /// Percent damage adjustment against targets affected by Bleed or Hemorrhage.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DamageToBleedingTargetPercentAdjustment = 60,

        /// <summary>
        /// Percent damage adjustment against targets with any status effect categorized as a debuff.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DamageToDebuffedTargetPercentAdjustment = 61,

        /// <summary>
        /// Percent damage adjustment against targets affected by Poison, Toxin, or Disoriented.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DamageToPoisonedOrDisorientedTargetPercentAdjustment = 62,

        /// <summary>
        /// Percent damage adjustment against targets affected by Weakened or Hamstring.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DamageToWeakenedOrHamstringTargetPercentAdjustment = 63,

        /// <summary>
        /// Percent damage adjustment against targets with any status effect categorized as control.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DamageToControlTargetPercentAdjustment = 64,

        /// <summary>
        /// Percent damage adjustment against targets affected by Disoriented or Dazed.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DamageToDisorientedDazedTargetPercentAdjustment = 65,

        /// <summary>
        /// Percent ability damage adjustment against targets affected by Knockdown or Blind.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AbilityDamageToKnockdownOrBlindTargetPercentAdjustment = 66,

        /// <summary>
        /// Flat Stamina restored when the creature defeats an enemy.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DefeatedEnemyStaminaRestore = 67,

        /// <summary>
        /// Percent of maximum HP restored when the creature defeats an enemy.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DefeatedEnemyHPPercentRestore = 68,

        /// <summary>
        /// Temporary percent Attack adjustment applied when the creature defeats an enemy.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DefeatedEnemyAttackPercentAdjustment = 69,

        /// <summary>
        /// Duration in seconds for DefeatedEnemyAttackPercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        DefeatedEnemyAttackDurationSeconds = 70,

        /// <summary>
        /// Temporary percent attack delay reduction applied when the creature defeats an enemy.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DefeatedEnemyAttackDelayReductionPercent = 71,

        /// <summary>
        /// Duration in seconds for DefeatedEnemyAttackDelayReductionPercent.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DefeatedEnemyAttackDelayReductionDurationSeconds = 72,

        /// <summary>
        /// Temporary percent Physical Defense adjustment applied to nearby party members when the creature defeats an enemy.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DefeatedEnemyNearbyAllyPhysicalDefensePercentAdjustment = 73,

        /// <summary>
        /// Duration in seconds for DefeatedEnemyNearbyAllyPhysicalDefensePercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        DefeatedEnemyNearbyAllyPhysicalDefenseDurationSeconds = 74,

        /// <summary>
        /// Percent of damage dealt restored to the attacker as HP.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DamageDealtHPPercentRestore = 75,

        /// <summary>
        /// Percent of critical-hit damage restored to the attacker as HP.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        CriticalHPPercentOfDamageRestore = 76,

        /// <summary>
        /// Percent chance for an auto-attack to restore AutoAttackStaminaRestore Stamina.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AutoAttackStaminaRestoreChance = 77,

        /// <summary>
        /// Flat Stamina restored when AutoAttackStaminaRestoreChance succeeds.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AutoAttackStaminaRestore = 78,

        /// <summary>
        /// Temporary percent Accuracy adjustment applied to the attacker after a critical hit.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        CriticalAccuracyPercentAdjustment = 79,

        /// <summary>
        /// Duration in seconds for CriticalAccuracyPercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        CriticalAccuracyDurationSeconds = 80,

        /// <summary>
        /// Temporary percent Evasion adjustment applied to the target after the attacker lands a critical hit.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        CriticalTargetEvasionPercentAdjustment = 81,

        /// <summary>
        /// Duration in seconds for CriticalTargetEvasionPercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        CriticalTargetEvasionDurationSeconds = 82,

        /// <summary>
        /// Percent Defense adjustment applied to the target through Exposed after the attacker lands a critical hit.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenNegative)]
        CriticalTargetDefensePercentAdjustment = 83,

        /// <summary>
        /// Duration in seconds for CriticalTargetDefensePercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        CriticalTargetDefenseDurationSeconds = 84,

        /// <summary>
        /// Percent chance for an auto-attack to apply AutoAttackTargetAccuracyPercentAdjustment to the target.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AutoAttackTargetAccuracyPercentAdjustmentChance = 85,

        /// <summary>
        /// Temporary percent Accuracy adjustment applied to the target when the auto-attack accuracy trigger succeeds.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AutoAttackTargetAccuracyPercentAdjustment = 86,

        /// <summary>
        /// Duration in seconds for AutoAttackTargetAccuracyPercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AutoAttackTargetAccuracyPercentAdjustmentDurationSeconds = 87,

        /// <summary>
        /// Temporary percent Attack adjustment applied to a defender after taking damage.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DamageTakenAttackPercentAdjustment = 88,

        /// <summary>
        /// Duration in seconds for DamageTakenAttackPercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        DamageTakenAttackDurationSeconds = 89,

        /// <summary>
        /// HP threshold percent that must be crossed before LowHPPhysicalDefensePercentAdjustment can trigger.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        LowHPPhysicalDefenseThresholdPercent = 90,

        /// <summary>
        /// Temporary percent Physical Defense adjustment applied when the low-HP physical defense trigger fires.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        LowHPPhysicalDefensePercentAdjustment = 91,

        /// <summary>
        /// Duration in seconds for LowHPPhysicalDefensePercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        LowHPPhysicalDefenseDurationSeconds = 92,

        /// <summary>
        /// Cooldown in seconds for the low-HP physical defense trigger.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        LowHPPhysicalDefenseCooldownSeconds = 93,

        /// <summary>
        /// HP threshold percent that must be crossed before LowHPEvasionPercentAdjustment can trigger.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        LowHPEvasionThresholdPercent = 94,

        /// <summary>
        /// Temporary percent Evasion adjustment applied when the low-HP evasion trigger fires.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        LowHPEvasionPercentAdjustment = 95,

        /// <summary>
        /// Duration in seconds for LowHPEvasionPercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        LowHPEvasionDurationSeconds = 96,

        /// <summary>
        /// Cooldown in seconds for the low-HP evasion trigger.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        LowHPEvasionCooldownSeconds = 97,

        /// <summary>
        /// HP threshold percent that must be crossed before temporary HP can trigger.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        LowHPTemporaryHPThresholdPercent = 98,

        /// <summary>
        /// Percent of maximum HP granted as temporary HP when the low-HP trigger succeeds.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        LowHPTemporaryHPPercent = 99,

        /// <summary>
        /// Duration in seconds for LowHPTemporaryHPPercent temporary HP.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        LowHPTemporaryHPDurationSeconds = 100,

        /// <summary>
        /// Cooldown in seconds for the low-HP temporary HP trigger.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        LowHPTemporaryHPCooldownSeconds = 101,

        /// <summary>
        /// Flat Stamina restored on critical hits against poisoned targets.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        CriticalPoisonedTargetStaminaRestore = 103,

        /// <summary>
        /// Percent chance to add DamageToPoisonedTargetFlatBonus damage against poisoned targets.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DamageToPoisonedTargetFlatBonusChance = 104,

        /// <summary>
        /// Flat damage added against poisoned targets when DamageToPoisonedTargetFlatBonusChance succeeds.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DamageToPoisonedTargetFlatBonus = 105,

        /// <summary>
        /// Percent damage adjustment for ranged weapon attacks against nearby targets.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        RangedDamageToNearbyTargetPercentAdjustment = 106,

        /// <summary>
        /// Flat FP restored by auto-attacks, subject to AutoAttackFPRestoreCooldownSeconds.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AutoAttackFPRestore = 107,

        /// <summary>
        /// Cooldown in seconds for AutoAttackFPRestore.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AutoAttackFPRestoreCooldownSeconds = 108,

        /// <summary>
        /// Percent of maximum FP and Stamina both required to enable HighFPAndStaminaAttackPercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        HighFPAndStaminaAttackThresholdPercent = 109,

        /// <summary>
        /// Percent Attack adjustment applied while both current FP and Stamina meet the high-resource threshold.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        HighFPAndStaminaAttackPercentAdjustment = 110,

        /// <summary>
        /// HP threshold percent that must be crossed before no-save temporary HP can trigger.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        LowHPNoSaveTemporaryHPThresholdPercent = 111,

        /// <summary>
        /// Percent of maximum HP granted as temporary HP when the no-save low-HP trigger succeeds.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        LowHPNoSaveTemporaryHPPercent = 112,

        /// <summary>
        /// Duration in seconds for LowHPNoSaveTemporaryHPPercent temporary HP.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        LowHPNoSaveTemporaryHPDurationSeconds = 113,

        /// <summary>
        /// Cooldown in seconds for the no-save low-HP temporary HP trigger.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        LowHPNoSaveTemporaryHPCooldownSeconds = 114,

        /// <summary>
        /// Internal temporary flat damage bonus consumed by the next auto-attack.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        NextAutoAttackDamageBonus = 117,

        /// <summary>
        /// Internal temporary SkillType value that causes the next matching ability activation delay to become zero, then is consumed.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        NextAttackNoDelay = 118,

        /// <summary>
        /// Primary RecastGroup id that can trigger an ability-used recast reduction.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityUsedRecastReductionTriggerGroup = 127,

        /// <summary>
        /// Secondary RecastGroup id that can trigger an ability-used recast reduction.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityUsedRecastReductionSecondaryTriggerGroup = 128,

        /// <summary>
        /// RecastGroup id whose active recast is reduced when the trigger group matches the used ability.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityUsedRecastReductionTargetGroup = 129,

        /// <summary>
        /// Seconds removed from AbilityUsedRecastReductionTargetGroup when the ability-used trigger fires.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityUsedRecastReductionSeconds = 130,

        /// <summary>
        /// Minimum number of impacted targets required for a throwing area ability to restore Stamina.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        ThrowingAreaAbilityMinTargetsStaminaRestoreThreshold = 131,

        /// <summary>
        /// Flat Stamina restored when a throwing area ability meets the minimum target threshold.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        ThrowingAreaAbilityMinTargetsStaminaRestore = 132,

        /// <summary>
        /// Temporary percent Attack gained per impacted target from throwing area abilities.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        ThrowingAreaAbilityAttackPercentPerTarget = 133,

        /// <summary>
        /// Duration in seconds for ThrowingAreaAbilityAttackPercentPerTarget stacks.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        ThrowingAreaAbilityAttackDurationSeconds = 134,

        /// <summary>
        /// Maximum total temporary Attack percent allowed from ThrowingAreaAbilityAttackPercentPerTarget stacks.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        ThrowingAreaAbilityAttackPercentMax = 135,

        /// <summary>
        /// Minimum number of impacted targets required for an area ability to restore resources.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AreaAbilityMinTargetsResourceRestoreThreshold = 136,

        /// <summary>
        /// Flat FP restored when an area ability meets the resource restore target threshold.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AreaAbilityFPRestore = 137,

        /// <summary>
        /// Flat Stamina restored when an area ability meets the resource restore target threshold.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AreaAbilityStaminaRestore = 138,

        /// <summary>
        /// Cooldown in seconds for area ability resource restoration.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AreaAbilityResourceRestoreCooldownSeconds = 139,

        /// <summary>
        /// Minimum number of impacted targets required for an area ability to apply temporary buffs.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AreaAbilityMinTargetsBuffThreshold = 140,

        /// <summary>
        /// Temporary percent attack delay reduction applied when an area ability meets the buff threshold.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AreaAbilityHastePercentAdjustment = 141,

        /// <summary>
        /// Temporary Ranged Deflection applied when an area ability meets the buff threshold.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive, deflectionSource: DeflectionSource.Ranged)]
        AreaAbilityAttackDeflection = 142,

        /// <summary>
        /// Duration in seconds for area ability haste and attack deflection buffs.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AreaAbilityBuffDurationSeconds = 143,

        /// <summary>
        /// Minimum number of impacted targets required for a twin blade area ability to gain haste.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        TwinBladeAreaAbilityMinTargetsHasteThreshold = 144,

        /// <summary>
        /// Temporary percent attack delay reduction gained per twin blade area ability haste stack.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        TwinBladeAreaAbilityHastePercentAdjustment = 145,

        /// <summary>
        /// Duration in seconds for TwinBladeAreaAbilityHastePercentAdjustment stacks.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        TwinBladeAreaAbilityHasteDurationSeconds = 146,

        /// <summary>
        /// Maximum total temporary attack delay reduction allowed from TwinBladeAreaAbilityHastePercentAdjustment stacks.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        TwinBladeAreaAbilityHastePercentMax = 147,

        /// <summary>
        /// Flat Stamina restored per impacted target by twin blade area abilities.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        TwinBladeAreaAbilityStaminaRestorePerTarget = 148,

        /// <summary>
        /// Maximum Stamina restored by TwinBladeAreaAbilityStaminaRestorePerTarget.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        TwinBladeAreaAbilityStaminaRestoreMax = 149,

        /// <summary>
        /// Percent chance for twin blade area abilities to use SWLOR's standard critical rating.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        TwinBladeAreaAbilityCriticalRatePercentAdjustment = 150,

        /// <summary>
        /// Flat Stamina restored when a twin blade single-target ability is used, subject to cooldown.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        TwinBladeSingleTargetAbilityStaminaRestore = 151,

        /// <summary>
        /// Cooldown in seconds for TwinBladeSingleTargetAbilityStaminaRestore.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        TwinBladeSingleTargetAbilityStaminaRestoreCooldownSeconds = 152,

        /// <summary>
        /// Percent chance, after taking damage from a recent target, to make the next ability activation delay zero.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DamageTakenRecentTargetNextAbilityNoDelayChance = 156,

        /// <summary>
        /// Seconds a damaged target relationship remains recent for DamageTakenRecentTargetNextAbilityNoDelayChance.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        DamageTakenRecentTargetWindowSeconds = 157,

        /// <summary>
        /// Flat Stamina restored for each twin blade area haste stack successfully gained.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        TwinBladeAreaAbilityStaminaRestoreOnHasteStack = 158,

        /// <summary>
        /// Flat Stamina restored per impacted target by twin blade area abilities when the cooldown-gated restore trigger succeeds.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        TwinBladeAreaAbilityCooldownStaminaRestorePerTarget = 159,

        /// <summary>
        /// Maximum Stamina restored by TwinBladeAreaAbilityCooldownStaminaRestorePerTarget.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        TwinBladeAreaAbilityCooldownStaminaRestoreMax = 160,

        /// <summary>
        /// Cooldown in seconds for TwinBladeAreaAbilityCooldownStaminaRestorePerTarget.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        TwinBladeAreaAbilityCooldownStaminaRestoreCooldownSeconds = 161,

        /// <summary>
        /// HP threshold percent that must be crossed before granting a no-Stamina-cost ability charge.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        LowHPNextAbilityNoStaminaCostThresholdPercent = 162,

        /// <summary>
        /// SkillType value for the no-Stamina-cost ability charge granted by the low-HP trigger.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        LowHPNextAbilityNoStaminaCostSkillType = 163,

        /// <summary>
        /// Duration in seconds for the low-HP no-Stamina-cost ability charge.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        LowHPNextAbilityNoStaminaCostDurationSeconds = 164,

        /// <summary>
        /// Cooldown in seconds for LowHPNextAbilityNoStaminaCostSkillType.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        LowHPNextAbilityNoStaminaCostCooldownSeconds = 165,

        /// <summary>
        /// Temporary SkillType value that makes the next matching ability cost 0 Stamina.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        NextAbilityNoStaminaCostSkillType = 166,

        /// <summary>
        /// PerkType value for the ability that can consume the critical-hit next-ability damage bonus.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        CriticalNextAbilityDamageBonusPerkType = 167,

        /// <summary>
        /// Flat damage added to the next matching ability after the critical-hit trigger succeeds.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        CriticalNextAbilityDamageBonus = 168,

        /// <summary>
        /// Duration in seconds for CriticalNextAbilityDamageBonus.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        CriticalNextAbilityDamageBonusDurationSeconds = 169,

        /// <summary>
        /// Cooldown in seconds for CriticalNextAbilityDamageBonus.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        CriticalNextAbilityDamageBonusCooldownSeconds = 170,

        /// <summary>
        /// Temporary flat damage added to the next ability matching its grouped PerkType.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        NextAbilityDamageBonus = 174,

        /// <summary>
        /// Percent chance for a critical hit from the target's side to restore Stamina.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        CriticalSideAttackStaminaRestoreChance = 175,

        /// <summary>
        /// Flat Stamina restored when CriticalSideAttackStaminaRestoreChance succeeds.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        CriticalSideAttackStaminaRestore = 176,

        /// <summary>
        /// Percent Defense adjustment applied to the target through Exposed after a single-target ability critical hit.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        SingleTargetCriticalTargetDefensePercentAdjustment = 177,

        /// <summary>
        /// Duration in seconds for SingleTargetCriticalTargetDefensePercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        SingleTargetCriticalTargetDefenseDurationSeconds = 178,

        /// <summary>
        /// SkillType value required before CriticalStaminaRestore can trigger. Invalid or 0 allows any skill.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        CriticalStaminaRestoreSkillType = 179,

        /// <summary>
        /// SkillType value required before CriticalNextAbilityDamageBonus can trigger. Invalid or 0 allows any skill.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        CriticalNextAbilityDamageBonusTriggerSkillType = 180,

        /// <summary>
        /// SkillType value required for DamageTakenRecentTargetNextAbilityNoDelayChance to grant a no-delay ability charge.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        DamageTakenRecentTargetNextAbilityNoDelaySkillType = 181,

        /// <summary>
        /// Percent adjustment applied to final Force Attack calculations.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        ForceAttackPercentAdjustment = 182,

        /// <summary>
        /// Percent Attack adjustment against targets affected by Bleed or Hemorrhage.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AttackToBleedingTargetPercentAdjustment = 183,

        /// <summary>
        /// Primary PerkType value that receives AbilityDamageFlatAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityDamageFlatAdjustmentPerkType = 184,

        /// <summary>
        /// Secondary PerkType value that receives AbilityDamageFlatAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityDamageFlatAdjustmentSecondaryPerkType = 185,

        /// <summary>
        /// Flat damage adjustment applied to matching abilities.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AbilityDamageFlatAdjustment = 186,

        /// <summary>
        /// Primary PerkType value that receives AbilityStaminaCostFlatAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityStaminaCostFlatAdjustmentPerkType = 187,

        /// <summary>
        /// Secondary PerkType value that receives AbilityStaminaCostFlatAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityStaminaCostFlatAdjustmentSecondaryPerkType = 188,

        /// <summary>
        /// Flat Stamina cost adjustment applied to matching abilities.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenNegative)]
        AbilityStaminaCostFlatAdjustment = 189,

        /// <summary>
        /// Flat FP restored when the creature defeats an enemy.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DefeatedEnemyFPRestore = 190,

        /// <summary>
        /// Percent adjustment applied to incoming direct healing.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        HealingReceivedPercentAdjustment = 191,

        /// <summary>
        /// Percent of maximum HP granted as temporary HP when incoming damage would be fatal.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        FatalDamageTemporaryHPPercent = 192,

        /// <summary>
        /// Duration in seconds for FatalDamageTemporaryHPPercent temporary HP.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        FatalDamageTemporaryHPDurationSeconds = 193,

        /// <summary>
        /// Bonus range in meters applied to Leadership command abilities and auras.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        LeadershipCommandRadiusBonusMeters = 194,

        /// <summary>
        /// Base seconds added to non-capstone Leadership command buff durations before Social scaling.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        LeadershipCommandDurationBonusBaseSeconds = 195,

        /// <summary>
        /// Maximum seconds added to non-capstone Leadership command buff durations after Social scaling.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        LeadershipCommandDurationBonusMaximumSeconds = 196,

        /// <summary>
        /// Triage Protocol tier applied by Field Steward shouts.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        FieldStewardTriageProtocolLevel = 197,

        /// <summary>
        /// Seconds added to Field Steward shout durations.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        FieldStewardDurationBonusSeconds = 198,

        /// <summary>
        /// Percent adjustment applied to Devices shield temporary HP effects.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DeviceShieldTemporaryHPPercentAdjustment = 199,

        /// <summary>
        /// Seconds added to Devices shield temporary HP effects.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        DeviceShieldDurationBonusSeconds = 200,

        /// <summary>
        /// Explosive blast radius bonus in tenths of a meter for grenade, Remote Charge, and Overload Barrage blasts.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        BlastRadiusBonusTenths = 201,

        /// <summary>
        /// Percentage-point bonus applied to grenade control-effect potency.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        GrenadeControlPotencyBonus = 202,

        /// <summary>
        /// Source ability score used to scale FatalDamageTemporaryHPPercent.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        FatalDamageTemporaryHPScalingAbilityScore = 203,

        /// <summary>
        /// Flat FP restored when a damaging dark Force power lands.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        DarkForceDamageFPRestore = 204,

        /// <summary>
        /// Percent of maximum HP paid when a damaging dark Force power lands.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        DarkForceDamageHPCostPercent = 205,

        /// <summary>
        /// Percent of maximum HP paid when a damaging dark Force power lands against a low-HP target.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        DarkForceDamageLowTargetHPCostPercent = 206,

        /// <summary>
        /// Target HP threshold for DarkForceDamageLowTargetHPCostPercent.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        DarkForceDamageLowTargetHPThresholdPercent = 207,

        /// <summary>
        /// Percent adjustment to physical damage taken by the creature.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenNegative)]
        PhysicalDamageTakenPercentAdjustment = 208,

        /// <summary>
        /// Percent adjustment to Throwing skill damage taken by the creature.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenNegative)]
        ThrowingDamageTakenPercentAdjustment = 209,

        /// <summary>
        /// Percent of dark Force damage restored as HP when a damaging dark Force power lands.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DarkForceDamageHPPercentRestore = 210,

        /// <summary>
        /// Temporary Ranged Deflection granted after a Light Guardian power is used.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive, deflectionSource: DeflectionSource.Ranged)]
        LightGuardianPowerAttackDeflection = 211,

        /// <summary>
        /// Duration in seconds for LightGuardianPowerAttackDeflection.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        LightGuardianPowerAttackDeflectionDurationSeconds = 212,

        /// <summary>
        /// Percent adjustment applied to direct, area, and periodic healing caused by abilities.
        /// Damage-derived healing and Resuscitation I's fixed one-HP restoration are excluded.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        OutgoingAbilityHealingPercentAdjustment = 213,

        /// <summary>
        /// Percent adjustment applied to stim pack effect durations.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        StimPackDurationPercentAdjustment = 214,

        /// <summary>
        /// SkillType id for the next skill ability modifier granted after guarding a hit.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        GuardedHitNextSkillAbilitySkillType = 215,

        /// <summary>
        /// Critical chance bonus for the next matching skill ability after guarding a hit.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        GuardedHitNextSkillAbilityCriticalRatePercentAdjustment = 216,

        /// <summary>
        /// Flat damage bonus for the next matching skill ability after guarding a hit.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        GuardedHitNextSkillAbilityDamageBonus = 217,

        /// <summary>
        /// Duration in seconds for guarded-hit next skill ability modifiers.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        GuardedHitNextSkillAbilityWindowSeconds = 218,

        /// <summary>
        /// Internal temporary SkillType id consumed by the next matching ability.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        NextSkillAbilitySkillType = 223,

        /// <summary>
        /// Internal temporary flat damage bonus consumed by the next matching skill ability.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        NextSkillAbilityDamageBonus = 224,

        /// <summary>
        /// Internal temporary critical chance bonus consumed by the next matching skill ability.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        NextSkillAbilityCriticalRatePercentAdjustment = 225,

        /// <summary>
        /// Internal temporary stamina cost adjustment consumed by the next matching perk ability.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenNegative)]
        NextAbilityStaminaCostAdjustment = 226,

        /// <summary>
        /// Flat bonus added to the default Melee Deflection chance cap.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive, deflectionSource: DeflectionSource.Melee)]
        MeleeDeflectionChanceCap = 227,

        /// <summary>
        /// Flat percent chance to deflect an attack while equipped with a shield.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive, deflectionSource: DeflectionSource.Shield)]
        ShieldDeflection = 228,

        /// <summary>
        /// Flat percent chance to guard an incoming weapon hit.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        Guard = 229,

        /// <summary>
        /// Percent-point adjustment to guard's base damage reduction.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        GuardDamageReductionPercentAdjustment = 230,

        /// <summary>
        /// Flat Stamina restored when the creature successfully guards a hit.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        GuardStaminaRestore = 231,

        /// <summary>
        /// DMG rating resolved through the combat damage range when the creature successfully guards a hit.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        GuardRetaliationDMG = 232,

        /// <summary>
        /// Percent adjustment applied to enmity generated by successful guarded hits.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        GuardEnmityPercentAdjustment = 233,

        /// <summary>
        /// HP threshold for granting temporary Guard chance after damage taken.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        LowHPGuardThresholdPercent = 234,

        /// <summary>
        /// Temporary Guard chance granted after crossing the configured low-HP threshold.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        LowHPGuard = 235,

        /// <summary>
        /// Duration in seconds for temporary low-HP Guard chance.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        LowHPGuardDurationSeconds = 236,

        /// <summary>
        /// Cooldown in seconds for the low-HP Guard trigger.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        LowHPGuardCooldownSeconds = 237,

        /// <summary>
        /// Flat damage bonus for the next matching skill ability after deflecting an attack.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive, deflectionSource: DeflectionSource.Ranged)]
        DeflectionNextSkillAbilityDamageBonus = 238,

        /// <summary>
        /// Critical chance bonus for the next matching skill ability after deflecting an attack.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive, deflectionSource: DeflectionSource.Ranged)]
        DeflectionNextSkillAbilityCriticalRatePercentAdjustment = 239,

        /// <summary>
        /// Non-zero when deflection grants no activation delay to the next matching skill ability.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive, deflectionSource: DeflectionSource.Ranged)]
        DeflectionNextSkillAbilityNoDelay = 240,

        /// <summary>
        /// Duration in seconds for DeflectionNextSkillAbilityDamageBonus.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        DeflectionNextSkillAbilityDamageBonusWindowSeconds = 241,

        /// <summary>
        /// Duration in seconds for DeflectionNextSkillAbilityCriticalRatePercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        DeflectionNextSkillAbilityCriticalRateWindowSeconds = 242,

        /// <summary>
        /// Duration in seconds for DeflectionNextSkillAbilityNoDelay.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        DeflectionNextSkillAbilityNoDelayWindowSeconds = 243,

        /// <summary>
        /// SkillType id required before a critical hit grants limited Haste.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        CriticalHitLimitedHasteTriggerSkillType = 244,

        /// <summary>
        /// Percent Haste granted by a matching critical hit.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        CriticalHitLimitedHastePercentAdjustment = 245,

        /// <summary>
        /// Duration in seconds for CriticalHitLimitedHastePercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        CriticalHitLimitedHasteDurationSeconds = 246,

        /// <summary>
        /// Number of direct attacks that retain the critical-hit Haste effect.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        CriticalHitLimitedHasteAttackCount = 247,

        /// <summary>
        /// Percent adjustment applied to FP restored to the creature.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        FPRestorePercentAdjustment = 248,

        /// <summary>
        /// Primary PerkType value that receives AbilityStatusDurationPercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityStatusDurationPercentAdjustmentPerkType = 249,

        /// <summary>
        /// Secondary PerkType value that receives AbilityStatusDurationPercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityStatusDurationPercentAdjustmentSecondaryPerkType = 250,

        /// <summary>
        /// Percent status duration adjustment applied to matching abilities.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AbilityStatusDurationPercentAdjustment = 251,

        /// <summary>
        /// Non-zero prevents the creature from activating Force abilities.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenNegative)]
        ForceAbilityActivationDisabled = 252,

        /// <summary>
        /// Percent duration adjustment applied to debuffs inflicted by the creature.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        OutgoingDebuffDurationPercentAdjustment = 253,

        /// <summary>
        /// Percent duration adjustment applied to Force Disruption effects inflicted by the creature.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        OutgoingForceDisruptionDurationPercentAdjustment = 254,

        /// <summary>
        /// Force Defense adjustment added to Force Disruption effects inflicted by the creature.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenNegative)]
        OutgoingForceDisruptionForceDefensePercentAdjustment = 255,

        /// <summary>
        /// Flat seconds added to bleeding effects inflicted by the creature.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        OutgoingBleedingDurationBonusSeconds = 256,

        /// <summary>
        /// Percent damage adjustment applied to bleeding ticks from effects inflicted by the creature.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        OutgoingBleedingDamagePercentAdjustment = 257,

        /// <summary>
        /// Attack adjustment added to Poison effects inflicted by the creature.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenNegative)]
        OutgoingPoisonAttackPercentAdjustment = 258,

        /// <summary>
        /// Attack adjustment added to Disoriented effects inflicted by the creature.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenNegative)]
        OutgoingDisorientedAttackPercentAdjustment = 259,

        /// <summary>
        /// Evasion adjustment added to Disoriented effects inflicted by the creature.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenNegative)]
        OutgoingDisorientedEvasionPercentAdjustment = 260,

        /// <summary>
        /// Attack adjustment applied after the creature's tranquilizer effect ends.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenNegative)]
        TranquilizeExpiredAttackPercentAdjustment = 261,

        /// <summary>
        /// Duration in seconds for TranquilizeExpiredAttackPercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        TranquilizeExpiredAttackDurationSeconds = 262,

        /// <summary>
        /// Base percent chance to restore Stamina when receiving HP healing.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        HealingReceivedStaminaRestoreChance = 263,

        /// <summary>
        /// AbilityType value plus one used to scale HealingReceivedStaminaRestoreChance.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        HealingReceivedStaminaRestoreChanceScalingAbility = 264,

        /// <summary>
        /// Maximum percent chance allowed for HealingReceivedStaminaRestoreChance after scaling.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        HealingReceivedStaminaRestoreChanceMaximum = 265,

        /// <summary>
        /// Flat Stamina restored when the healing-received Stamina trigger succeeds.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        HealingReceivedStaminaRestore = 266,

        /// <summary>
        /// Temporary Attack adjustment applied when receiving HP healing.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        HealingReceivedAttackPercentAdjustment = 267,

        /// <summary>
        /// Duration in seconds for HealingReceivedAttackPercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        HealingReceivedAttackDurationSeconds = 268,

        /// <summary>
        /// Duration in seconds for Force Erosion effects inflicted when dealing damage.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        DamageDealtForceErosionDurationSeconds = 269,

        /// <summary>
        /// FP removed each tick from Force Erosion effects inflicted when dealing damage.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DamageDealtForceErosionFPLossPerTick = 270,

        /// <summary>
        /// Primary PerkType value that receives AbilityDefenseIgnorePercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityDefenseIgnorePercentAdjustmentPerkType = 271,

        /// <summary>
        /// Secondary PerkType value that receives AbilityDefenseIgnorePercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityDefenseIgnorePercentAdjustmentSecondaryPerkType = 272,

        /// <summary>
        /// Percent of target Defense ignored by matching abilities.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AbilityDefenseIgnorePercentAdjustment = 273,

        /// <summary>
        /// SkillType value whose abilities ignore Defense against Exposed or Sundered targets.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityDefenseIgnoreExposedOrSunderedSkillType = 274,

        /// <summary>
        /// Percent of target Defense ignored by matching skill abilities against Exposed or Sundered targets.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AbilityDefenseIgnoreExposedOrSunderedPercentAdjustment = 275,

        /// <summary>
        /// Percent of target Defense ignored by the next matching skill ability after a critical hit.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        CriticalNextSkillAbilityDefenseIgnorePercentAdjustment = 276,

        /// <summary>
        /// Duration in seconds for CriticalNextSkillAbilityDefenseIgnorePercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        CriticalNextSkillAbilityDefenseIgnoreDurationSeconds = 277,

        /// <summary>
        /// Internal temporary Defense ignore percent consumed by the next matching skill ability.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        NextSkillAbilityDefenseIgnorePercentAdjustment = 278,

        /// <summary>
        /// Critical chance bonus for Throwing abilities against Bleeding or Disoriented targets.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        ThrowingAbilityCriticalRateToBleedingOrDisorientedTargetPercentAdjustment = 279,

        /// <summary>
        /// Stamina restored when critically hitting a target marked by the attacker.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        CriticalMarkedTargetStaminaRestore = 280,

        /// <summary>
        /// Critical chance bonus against targets not facing the attacker.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        CriticalRateAgainstTargetNotFacingAttackerPercentAdjustment = 284,

        /// <summary>
        /// Non-zero causes incoming critical hits to become normal hits that roll minimum damage.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        IncomingCriticalHitDowngradeToMinimumDamage = 285,

        /// <summary>
        /// Attack percent gained per nearby creature matching NearbyStatusTargetAttackStatusCategory.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        NearbyStatusTargetAttackPercentPerTarget = 286,

        /// <summary>
        /// Search radius in meters for NearbyStatusTargetAttackPercentPerTarget.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        NearbyStatusTargetAttackRadiusMeters = 287,

        /// <summary>
        /// Maximum attack percent granted by NearbyStatusTargetAttackPercentPerTarget.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        NearbyStatusTargetAttackPercentMaximum = 288,

        /// <summary>
        /// StatusEffectCategory value counted by NearbyStatusTargetAttackPercentPerTarget.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        NearbyStatusTargetAttackStatusCategory = 289,

        /// <summary>
        /// Radius in meters for spreading Poison when a Poisoned enemy is defeated.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        PoisonedDefeatedEnemySpreadRadiusMeters = 290,

        /// <summary>
        /// Duration in seconds for Poison spread from a defeated Poisoned enemy.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        PoisonedDefeatedEnemySpreadDurationSeconds = 291,

        /// <summary>
        /// SkillType value required for an opening auto-attack bonus.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        OpeningAutoAttackSkillType = 292,

        /// <summary>
        /// Critical chance bonus applied to an opening auto-attack.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        OpeningAutoAttackCriticalRatePercentAdjustment = 293,

        /// <summary>
        /// Flat damage bonus applied to an opening auto-attack.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        OpeningAutoAttackDamageBonus = 294,

        /// <summary>
        /// Seconds without combat activity required before the next auto-attack is considered an opening attack.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        OpeningAutoAttackIdleSeconds = 295,

        /// <summary>
        /// Internal one-shot flat damage bonus prepared for the current auto-attack.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        CurrentAutoAttackDamageBonus = 296,

        /// <summary>
        /// Chance to restore Stamina when taking melee attack damage.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        MeleeDamageTakenStaminaRestoreChance = 297,

        /// <summary>
        /// Stamina restored when MeleeDamageTakenStaminaRestoreChance succeeds.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        MeleeDamageTakenStaminaRestore = 298,

        /// <summary>
        /// Temporary Evasion percent adjustment applied when the melee-damage-taken trigger succeeds.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        MeleeDamageTakenEvasionPercentAdjustment = 299,

        /// <summary>
        /// Duration in seconds for MeleeDamageTakenEvasionPercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        MeleeDamageTakenEvasionDurationSeconds = 300,

        /// <summary>
        /// SkillType value receiving AbilityHitChancePercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityHitChancePercentAdjustmentSkillType = 301,

        /// <summary>
        /// Hit chance modifier applied to matching abilities.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AbilityHitChancePercentAdjustment = 302,

        /// <summary>
        /// Primary PerkType value that receives AbilityHitChancePercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityHitChancePercentAdjustmentPerkType = 303,

        /// <summary>
        /// Secondary PerkType value that receives AbilityHitChancePercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityHitChancePercentAdjustmentSecondaryPerkType = 304,

        /// <summary>
        /// Primary PerkType value that receives AbilityRecastDelayFlatAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityRecastDelayFlatAdjustmentPerkType = 305,

        /// <summary>
        /// Flat seconds added to matching ability recast delays. Use negative values to reduce recast time.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenNegative)]
        AbilityRecastDelayFlatAdjustment = 306,

        /// <summary>
        /// Temporary Force Defense adjustment granted after evading a Force ability.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        ForceAbilityEvadedForceDefensePercentAdjustment = 307,

        /// <summary>
        /// Duration in seconds for ForceAbilityEvadedForceDefensePercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        ForceAbilityEvadedDurationSeconds = 308,

        /// <summary>
        /// Stamina restored after evading a Force ability.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        ForceAbilityEvadedStaminaRestore = 309,

        /// <summary>
        /// Cooldown in seconds for the Force ability evasion trigger.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        ForceAbilityEvadedCooldownSeconds = 310,

        /// <summary>
        /// SkillType value of incoming abilities affected by IncomingAbilityHitChancePercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        IncomingAbilityHitChancePercentAdjustmentSkillType = 311,

        /// <summary>
        /// Hit chance modifier applied to incoming matching abilities.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenNegative)]
        IncomingAbilityHitChancePercentAdjustment = 312,

        /// <summary>
        /// SkillType value that must be used before granting a next auto-attack damage bonus.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityUsedNextSkillAutoAttackDamageBonusTriggerSkillType = 313,

        /// <summary>
        /// SkillType value whose next auto-attack receives the damage bonus.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityUsedNextSkillAutoAttackDamageBonusSkillType = 314,

        /// <summary>
        /// Flat damage bonus granted to the next matching auto-attack after using the trigger ability type.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AbilityUsedNextSkillAutoAttackDamageBonus = 315,

        /// <summary>
        /// Duration in seconds for AbilityUsedNextSkillAutoAttackDamageBonus.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityUsedNextSkillAutoAttackDamageWindowSeconds = 316,

        /// <summary>
        /// SkillType value that must be used before granting a next ability FP cost adjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityUsedNextSkillFPCostAdjustmentTriggerSkillType = 317,

        /// <summary>
        /// SkillType value whose next ability receives the FP cost adjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityUsedNextSkillFPCostAdjustmentSkillType = 318,

        /// <summary>
        /// Flat FP cost adjustment granted to the next matching ability.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenNegative)]
        AbilityUsedNextSkillFPCostAdjustment = 319,

        /// <summary>
        /// Duration in seconds for AbilityUsedNextSkillFPCostAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityUsedNextSkillFPCostAdjustmentWindowSeconds = 320,

        /// <summary>
        /// Temporary SkillType value for the next auto-attack damage bonus.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        NextSkillAutoAttackDamageBonusSkillType = 321,

        /// <summary>
        /// Temporary flat damage bonus consumed by the next matching auto-attack.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        NextSkillAutoAttackDamageBonus = 322,

        /// <summary>
        /// Temporary SkillType value for the next ability FP cost adjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        NextAbilityFPCostAdjustmentSkillType = 323,

        /// <summary>
        /// Temporary flat FP cost adjustment consumed by the next matching ability.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenNegative)]
        NextAbilityFPCostAdjustment = 324,

        /// <summary>
        /// SkillType value whose abilities receive a Stamina cost adjustment above the configured resource threshold.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        HighResourceAbilityStaminaCostSkillType = 325,

        /// <summary>
        /// Current FP percent required before HighResourceAbilityStaminaCostAdjustment applies.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        HighResourceAbilityStaminaCostThresholdPercent = 326,

        /// <summary>
        /// Flat Stamina cost adjustment while the high-resource threshold is met.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenNegative)]
        HighResourceAbilityStaminaCostAdjustment = 327,

        /// <summary>
        /// SkillType value whose next ability receives an avoided-attack Stamina cost adjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AvoidedAttackNextSkillAbilitySkillType = 328,

        /// <summary>
        /// Flat Stamina cost adjustment granted after dodging or deflecting an attack.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenNegative)]
        AvoidedAttackNextSkillAbilityStaminaCostAdjustment = 329,

        /// <summary>
        /// Duration in seconds for AvoidedAttackNextSkillAbilityStaminaCostAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AvoidedAttackNextSkillAbilityWindowSeconds = 330,

        /// <summary>
        /// Temporary SkillType value for the next ability Stamina cost adjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        NextSkillAbilityStaminaCostAdjustmentSkillType = 331,

        /// <summary>
        /// Temporary flat Stamina cost adjustment consumed by the next matching skill ability.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenNegative)]
        NextSkillAbilityStaminaCostAdjustment = 332,

        /// <summary>
        /// SkillType value that can receive idle-time ability bonuses.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        IdleSkillAbilitySkillType = 333,

        /// <summary>
        /// Seconds since the last combat ability required before idle-time ability bonuses apply.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        IdleSkillAbilityRequiredIdleSeconds = 334,

        /// <summary>
        /// Flat damage bonus applied to a matching ability after sufficient idle time.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        IdleSkillAbilityDamageBonus = 335,

        /// <summary>
        /// Hit chance bonus applied to a matching ability after sufficient idle time.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        IdleSkillAbilityHitChancePercentAdjustment = 336,

        /// <summary>
        /// SkillType value that can receive side-attack bonuses.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        SideAttackSkillType = 337,

        /// <summary>
        /// Damage percent adjustment applied to matching attacks from the side.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        SideAttackDamagePercentAdjustment = 338,

        /// <summary>
        /// Hit chance modifier applied to matching attacks from the side.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        SideAttackHitChancePercentAdjustment = 339,

        /// <summary>
        /// Critical chance modifier applied to matching attacks from the side.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        SideAttackCriticalRatePercentAdjustment = 340,

        /// <summary>
        /// Stamina restored after dealing damage with a matching side attack.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        SideAttackStaminaRestore = 341,

        /// <summary>
        /// Cooldown in seconds for SideAttackStaminaRestore.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        SideAttackStaminaRestoreCooldownSeconds = 342,

        /// <summary>
        /// Temporary attack delay reduction granted after dealing damage with a matching side attack.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        SideAttackDelayReductionPercent = 343,

        /// <summary>
        /// Duration in seconds for SideAttackDelayReductionPercent.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        SideAttackDelayReductionDurationSeconds = 344,

        /// <summary>
        /// Base chance for matching side attacks to ignore part of the target's Evasion.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        SideAttackEvasionIgnoreChance = 345,

        /// <summary>
        /// AbilityType value plus one used to scale SideAttackEvasionIgnoreChance.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        SideAttackEvasionIgnoreChanceScalingAbility = 346,

        /// <summary>
        /// Maximum chance for SideAttackEvasionIgnoreChance after scaling.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        SideAttackEvasionIgnoreChanceMaximum = 347,

        /// <summary>
        /// Percent of target Evasion ignored when the matching side-attack trigger succeeds.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        SideAttackEvasionIgnorePercent = 348,

        /// <summary>
        /// SkillType value that can trigger auto-attack cycle damage.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AutoAttackCycleDamageSkillType = 349,

        /// <summary>
        /// Number of matching auto-attacks required before auto-attack cycle damage triggers.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AutoAttackCycleRequiredCount = 350,

        /// <summary>
        /// Flat damage dealt by auto-attack cycle damage.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AutoAttackCycleDamage = 351,

        /// <summary>
        /// Radius in meters for auto-attack cycle damage target selection.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AutoAttackCycleRadiusMeters = 352,

        /// <summary>
        /// SkillType value receiving AbilityCriticalRatePercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityCriticalRatePercentAdjustmentSkillType = 353,

        /// <summary>
        /// Critical chance modifier applied to matching abilities.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AbilityCriticalRatePercentAdjustment = 354,

        /// <summary>
        /// Primary PerkType value that receives AbilityCriticalRatePercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityCriticalRatePercentAdjustmentPerkType = 355,

        /// <summary>
        /// Secondary PerkType value that receives AbilityCriticalRatePercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityCriticalRatePercentAdjustmentSecondaryPerkType = 356,

        /// <summary>
        /// Internal one-shot flag causing the current incoming attack to roll minimum normal damage.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        CurrentIncomingAttackMinimumDamage = 357,

        /// <summary>
        /// Hit chance modifier applied only to abilities matching AbilityHitChancePercentAdjustmentPerkType.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        TargetedAbilityHitChancePercentAdjustment = 358,

        /// <summary>
        /// Percent adjustment applied to incoming Force damage. Positive values increase damage taken; negative values reduce it.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenNegative)]
        ForceDamageTakenPercentAdjustment = 359,

        /// <summary>
        /// Percent adjustment applied to incoming ranged physical damage. Positive values increase damage taken; negative values reduce it.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenNegative)]
        RangedPhysicalDamageTakenPercentAdjustment = 360,

        /// <summary>
        /// Percent damage adjustment applied by a status recipient when damaging that status effect's source.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DamageToStatusSourcePercentAdjustment = 361,

        /// <summary>
        /// Percent Defense adjustment applied by a status recipient when defending against that status effect's source.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DefenseAgainstStatusSourcePercentAdjustment = 362,

        /// <summary>
        /// Percent incoming damage adjustment applied by a status recipient when damaged by that status effect's source.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenNegative)]
        DamageTakenFromStatusSourcePercentAdjustment = 363,

        /// <summary>
        /// Percent incoming damage adjustment applied by a status recipient when damaged by that status effect source's party.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenNegative)]
        DamageTakenFromStatusSourcePartyPercentAdjustment = 364,

        /// <summary>
        /// Percent of incoming Force damage reflected back to the attacker after mitigation.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        ForceDamageReflectionPercentAdjustment = 365,

        /// <summary>
        /// Percent of incoming elemental damage reflected back to the attacker after mitigation.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        ElementalDamageReflectionPercentAdjustment = 366,

        /// <summary>
        /// Percent adjustment applied to incoming physical damage-over-time ticks. Positive values increase damage taken; negative values reduce it.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenNegative)]
        PhysicalDamageOverTimeTakenPercentAdjustment = 367,

        /// <summary>
        /// Force alignment affinity. Negative values favor Dark-side abilities; positive values favor Light-side abilities.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        ForceAffinity = 368,

        /// <summary>
        /// Percent enmity adjustment applied by a status recipient when generating enmity toward that status effect's source.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenNegative)]
        EnmityToStatusSourcePercentAdjustment = 369,

        /// <summary>
        /// Percent Accuracy adjustment applied by a status recipient when attacking that status effect's source.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AccuracyToStatusSourcePercentAdjustment = 370,

        /// <summary>
        /// Percent reduction to offhand weapon delay. Total reduction is capped by combat delay logic.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        OffhandAttackDelayReductionPercent = 371,

        /// <summary>
        /// Percent adjustment applied to outgoing damage.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DamageDealtPercentAdjustment = 372,

        /// <summary>
        /// Percent of physical damage dealt restored as HP.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        PhysicalDamageDealtHPPercentRestore = 373,

        /// <summary>
        /// Percent damage adjustment applied to single-target Twin Blade abilities.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        TwinBladeSingleTargetAbilityDamagePercentAdjustment = 374,

        /// <summary>
        /// Percent damage adjustment applied to area Twin Blade abilities.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        TwinBladeAreaAbilityDamagePercentAdjustment = 375,

        /// <summary>
        /// SkillType value whose auto-attacks restore SkillAutoAttackFPRestore FP.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        SkillAutoAttackFPRestoreSkillType = 376,

        /// <summary>
        /// Flat FP restored by matching skill auto-attacks.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        SkillAutoAttackFPRestore = 377,

        /// <summary>
        /// SkillType value whose Stamina ability costs restore FP by percent.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial, StatTypeAggregation.Maximum)]
        AbilityStaminaCostFPRestorePercentSkillType = 378,

        /// <summary>
        /// Percent of Stamina spent on matching abilities restored as FP.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AbilityStaminaCostFPRestorePercent = 379,

        /// <summary>
        /// SkillType value whose FP ability costs restore Stamina by percent.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial, StatTypeAggregation.Maximum)]
        AbilityFPCostStaminaRestorePercentSkillType = 380,

        /// <summary>
        /// Percent of FP spent on matching abilities restored as Stamina.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AbilityFPCostStaminaRestorePercent = 381,

        /// <summary>
        /// SkillType value whose abilities receive a flat Stamina cost adjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        SkillAbilityStaminaCostFlatAdjustmentSkillType = 382,

        /// <summary>
        /// Flat Stamina cost adjustment for matching skill abilities.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenNegative)]
        SkillAbilityStaminaCostFlatAdjustment = 383,

        /// <summary>
        /// SkillType value whose repeated damage against the same target receives RepeatedTargetDamagePercentPerHit.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        RepeatedTargetDamageSkillType = 384,

        /// <summary>
        /// Percent damage adjustment gained for each repeated hit against the same target.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        RepeatedTargetDamagePercentPerHit = 385,

        /// <summary>
        /// Maximum percent damage adjustment from RepeatedTargetDamagePercentPerHit stacks.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        RepeatedTargetDamagePercentMax = 386,

        /// <summary>
        /// Percent Evasion adjustment applied only against incoming ranged attacks.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        RangedEvasionPercentAdjustment = 387,

        /// <summary>
        /// Percent damage adjustment applied to area Throwing abilities.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        ThrowingAreaAbilityDamagePercentAdjustment = 388,

        /// <summary>
        /// Percent adjustment applied to Stamina ability costs. Positive values increase cost; negative values reduce it.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenNegative)]
        AbilityStaminaCostPercentAdjustment = 389,

        /// <summary>
        /// Percent accuracy adjustment applied to Field Engineer beacon pulses.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        BeaconPulseAccuracyPercentAdjustment = 390,

        /// <summary>
        /// Percent critical chance adjustment applied to Field Engineer beacon pulses.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        BeaconPulseCriticalRatePercentAdjustment = 391,

        /// <summary>
        /// Percent damage adjustment applied to Field Engineer beacon pulses.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        BeaconPulseDamagePercentAdjustment = 392,

        /// <summary>
        /// Radius bonus in meters applied to Field Engineer beacon pulse target selection.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        BeaconPulseRangeBonusMeters = 393,

        /// <summary>
        /// Percent accuracy adjustment applied only to Assault Gadget abilities.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AssaultGadgetAccuracyPercentAdjustment = 394,

        /// <summary>
        /// Percent critical chance adjustment applied only to Assault Gadget abilities.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AssaultGadgetCriticalRatePercentAdjustment = 395,

        /// <summary>
        /// Percent damage adjustment applied only to Assault Gadget abilities.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AssaultGadgetDamagePercentAdjustment = 396,

        /// <summary>
        /// Cooldown in seconds for fatal damage temporary HP triggers.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        FatalDamageTemporaryHPCooldownSeconds = 397,

        /// <summary>
        /// Percent chance to restore Stamina when damaging a bleeding target.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DamageDealtBleedingTargetStaminaRestoreChance = 398,

        /// <summary>
        /// Flat Stamina restored when damaging a bleeding target.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DamageDealtBleedingTargetStaminaRestore = 399,

        /// <summary>
        /// Percent chance for auto-attacks to restore Stamina to the attacker's master.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AutoAttackMasterStaminaRestoreChance = 400,

        /// <summary>
        /// Flat Stamina restored to the attacker's master by auto-attacks.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AutoAttackMasterStaminaRestore = 401,

        /// <summary>
        /// Percent chance for auto-attacks to restore FP to the attacker's master.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AutoAttackMasterFPRestoreChance = 402,

        /// <summary>
        /// Flat FP restored to the attacker's master by auto-attacks.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AutoAttackMasterFPRestore = 403,

        /// <summary>
        /// Percent chance for melee attackers to suffer poison damage when damaging this creature.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        MeleeDamageTakenPoisonDamageChance = 404,

        /// <summary>
        /// Flat poison damage dealt to melee attackers.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        MeleeDamageTakenPoisonDamage = 405,

        /// <summary>
        /// AbilityType plus one used to scale MeleeDamageTakenPoisonDamage.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        MeleeDamageTakenPoisonDamageScalingAbility = 406,

        /// <summary>
        /// Percent chance to restore Stamina when avoiding an attack.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AvoidedAttackStaminaRestoreChance = 407,

        /// <summary>
        /// Flat Stamina restored when avoiding an attack.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AvoidedAttackStaminaRestore = 408,

        /// <summary>
        /// Percent ability hit chance adjustment granted to the activator's master when the activator uses an ability.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AbilityUsedMasterAbilityHitChancePercentAdjustment = 409,

        /// <summary>
        /// Duration in seconds for AbilityUsedMasterAbilityHitChancePercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityUsedMasterAbilityHitChanceDurationSeconds = 410,

        /// <summary>
        /// Percent chance bonus applied when marking a target for rare loot.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        RareItemFindChance = 411,

        /// <summary>
        /// Percent chance for the next avoided attack to restore Stamina and consume the granting status.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AvoidedAttackSingleStaminaRestoreChance = 412,

        /// <summary>
        /// Flat Stamina restored by the next avoided attack before consuming the granting status.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AvoidedAttackSingleStaminaRestore = 413,

        /// <summary>
        /// Percent of incoming damage redirected to the source of the granting status effect.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DamageTakenRedirectToStatusSourcePercent = 414,

        /// <summary>
        /// Percent hit chance adjustment applied only to physical weapon and Force abilities.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        PhysicalAndForceAbilityHitChancePercentAdjustment = 417,

        /// <summary>
        /// Percent outgoing damage adjustment applied only to weapon and Force damage.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        WeaponAndForceDamageDealtPercentAdjustment = 418,

        /// <summary>
        /// Current rank of the Rallying Standard Leadership aura.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        RallyingStandardAuraLevel = 419,

        /// <summary>
        /// Current rank of the Coordinated Focus Leadership aura.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        CoordinatedFocusAuraLevel = 420,

        /// <summary>
        /// Current rank of the Charge Order Leadership aura.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        ChargeOrderAuraLevel = 421,

        /// <summary>
        /// Current rank of the Watchful Presence Leadership aura.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        WatchfulPresenceAuraLevel = 422,

        /// <summary>
        /// Current rank of the Steady Formation Leadership aura.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        SteadyFormationAuraLevel = 423,

        /// <summary>
        /// Current rank of the Field Recovery Leadership aura.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        FieldRecoveryAuraLevel = 424,

        /// <summary>
        /// HP threshold percent below which damage dealt restores HP.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        LowHPDamageDealtHPRestoreThresholdPercent = 425,

        /// <summary>
        /// Percent of damage dealt restored as HP while below LowHPDamageDealtHPRestoreThresholdPercent.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        LowHPDamageDealtHPPercentRestore = 426,

        /// <summary>
        /// Treats incoming physical damage as fully immune. Normal percent mitigation should use PhysicalDamageTakenPercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        PhysicalDamageImmunity = 427,

        /// <summary>
        /// Internal temporary SkillType value that causes the next matching auto-attack to use the default minimum delay, then is consumed.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        NextAutoAttackNoDelaySkillType = 428,

        /// <summary>
        /// SkillType id required before a critical hit grants minimum delay to the next auto-attack.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        CriticalNextAutoAttackNoDelayTriggerSkillType = 429,

        /// <summary>
        /// SkillType id whose next auto-attack receives the default minimum delay after the critical-hit trigger.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        CriticalNextAutoAttackNoDelaySkillType = 430,

        /// <summary>
        /// Duration in seconds for CriticalNextAutoAttackNoDelaySkillType.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        CriticalNextAutoAttackNoDelayDurationSeconds = 431,

        /// <summary>
        /// Cooldown in seconds for the critical-hit auto-attack minimum-delay trigger.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        CriticalNextAutoAttackNoDelayCooldownSeconds = 432,

        /// <summary>
        /// If greater than zero, creature movement speed is reduced to zero.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        MovementSpeedDisabled = 433,

        /// <summary>
        /// Percent adjustment applied to FP costs for damaging Dark Force conversion powers.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenNegative)]
        DarkForceConversionFPCostPercentAdjustment = 434,

        /// <summary>
        /// Percent adjustment applied to incoming physical ability damage.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenNegative)]
        PhysicalAbilityDamageTakenPercentAdjustment = 435,

        /// <summary>
        /// Percent damage adjustment applied to single-target physical abilities.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        SingleTargetPhysicalAbilityDamagePercentAdjustment = 436,

        /// <summary>
        /// Percent adjustment applied to activated ability damage and healing. Does not affect recast delays.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        CombatReadinessPercent = 437,

        /// <summary>
        /// Evasion percent penalty applied by pressure marks from Force Spark and Force Lightning.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        SparkLightningPressureEvasionPenaltyPercent = 438,

        /// <summary>
        /// Force damage taken penalty applied by pressure marks while the target is below the configured HP threshold.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        SparkLightningPressureLowHPForceDamageTakenPercent = 439,

        /// <summary>
        /// HP threshold percent for SparkLightningPressureLowHPForceDamageTakenPercent.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        SparkLightningPressureLowHPThresholdPercent = 440,

        /// <summary>
        /// Duration in seconds for pressure marks from Force Spark and Force Lightning.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        SparkLightningPressureDurationSeconds = 441,

        /// <summary>
        /// Target HP threshold percent for dark force ability damage bonuses.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        DarkForceTargetLowHPDamageThresholdPercent = 442,

        /// <summary>
        /// Percent damage adjustment applied to dark force abilities against targets below the configured HP threshold.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DarkForceTargetLowHPDamagePercentAdjustment = 443,

        /// <summary>
        /// Enables Devices Field Engineer area effects to reveal hidden enemies in their affected area.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        FieldEngineerAreaRevealHidden = 444,

        /// <summary>
        /// Evasion percent penalty applied by Devices Field Engineer area effects.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        FieldEngineerAreaEvasionPenaltyPercent = 445,

        /// <summary>
        /// Duration in seconds for FieldEngineerAreaEvasionPenaltyPercent.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        FieldEngineerAreaEvasionPenaltyDurationSeconds = 446,

        /// <summary>
        /// Enables Power Cell to apply Power Surge to its initial target.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        PowerCellInitialTargetPowerSurge = 447,

        /// <summary>
        /// Enables Field Support ally abilities to apply Overclock Routine.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        FieldSupportAllyOverclockRoutine = 449,

        /// <summary>
        /// Enables Assault Gadget damage to apply Tactical Uplink.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AssaultGadgetTacticalUplink = 450,

        /// <summary>
        /// Enables Control healing powers to apply Serene Focus.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        ControlHealingSereneFocus = 451,

        /// <summary>
        /// Enables Control healing powers to trigger the Force Mend cleanse and bonus heal.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        ControlHealingForceMend = 471,

        /// <summary>
        /// Enables defeated-enemy Force restoration and accuracy from Cruel Momentum.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        CruelMomentum = 452,

        /// <summary>
        /// Enables low-HP ally splash healing from Harmonic Restoration.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        HarmonicRestoration = 453,

        /// <summary>
        /// Enables FP-spend restoration and Force accuracy from Force Convergence.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        ForceConvergence = 454,

        /// <summary>
        /// Enables FP-spend Defense and Evasion from Precognition.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        ForcePrecognition = 455,

        /// <summary>
        /// Secondary-area damage added to Riot Blade.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        RiotBladeSecondaryDamageBonus = 456,

        /// <summary>
        /// Secondary-target damage added to Savage Cleave.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        SavageCleaveSecondaryDamageBonus = 457,

        /// <summary>
        /// Stamina restored per secondary target hit by Savage Cleave.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        SavageCleaveSecondaryTargetStaminaRestore = 458,

        /// <summary>
        /// Maximum Stamina restored from SavageCleaveSecondaryTargetStaminaRestore per activation.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        SavageCleaveSecondaryTargetStaminaRestoreMaximum = 459,

        /// <summary>
        /// Damage added to Earthshatter.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        EarthshatterDamageBonus = 461,

        /// <summary>
        /// Enmity added to Earthshatter.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        EarthshatterEnmityBonus = 462,

        /// <summary>
        /// Haste percent granted by Predator's Mark follow-up hits.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        PredatorsMarkHastePercentPerStack = 463,

        /// <summary>
        /// Ability hit chance percent granted by Predator's Mark follow-up hits.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        PredatorsMarkAbilityHitChancePercentPerStack = 464,

        /// <summary>
        /// Duration in seconds for Predator's Mark follow-up stacks.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        PredatorsMarkFollowUpDurationSeconds = 465,

        /// <summary>
        /// Maximum Predator's Mark follow-up stacks.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        PredatorsMarkFollowUpMaximumStacks = 466,

        /// <summary>
        /// Damage taken from the beast that applied Predator's Mark.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        PredatorsMarkDamageTakenFromBeastPercent = 472,

        /// <summary>
        /// Duration in seconds for Predator's Mark.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        PredatorsMarkDurationSeconds = 825,

        /// <summary>
        /// Physical Defense percent applied by Field Support ally-buff riders.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        FieldSupportPhysicalDefensePercent = 467,

        /// <summary>
        /// Duration in seconds for FieldSupportPhysicalDefensePercent.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        FieldSupportPhysicalDefenseDurationSeconds = 468,

        /// <summary>
        /// Physical and Force damage reduction percent applied by Field Support ally-buff riders.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        FieldSupportPhysicalAndForceDamageReductionPercent = 469,

        /// <summary>
        /// Duration in seconds for FieldSupportPhysicalAndForceDamageReductionPercent.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        FieldSupportPhysicalAndForceDamageReductionDurationSeconds = 470,

        /// <summary>
        /// Stamina restored to a balanced beast and its master after the beast uses a balanced active ability.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        BeastBalancedAbilityStaminaRestore = 473,

        /// <summary>
        /// Cooldown in seconds for BeastBalancedAbilityStaminaRestore.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        BeastBalancedAbilityStaminaRestoreCooldownSeconds = 474,

        /// <summary>
        /// Coagulant rank applied by Combat Pharmacology stim effects.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        CombatPharmacologyStimCoagulantRank = 475,

        /// <summary>
        /// Enables Trauma Medic healing and treatment abilities to apply Emergency Sealant.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        TraumaMedicEmergencySealant = 476,

        /// <summary>
        /// Enables Nightmare Field and Eclipse of Resolve to apply Exposed and Force Erosion.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DarkManipulatorCollapseWill = 477,

        /// <summary>
        /// Enables Light Guardian sense powers to apply Courageous Resolve.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        LightGuardianSenseResolve = 478,

        /// <summary>
        /// Enables Light Guardian temporary HP powers to apply Reflective Barrier.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        LightGuardianTemporaryHPReflectiveBarrier = 479,

        /// <summary>
        /// Next auto-attack damage granted after using a Heavy Vibroblade Defense ability.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        HeavyVibrobladeDefenseAbilityNextAutoAttackDamageBonus = 480,

        /// <summary>
        /// Duration in seconds for HeavyVibrobladeDefenseAbilityNextAutoAttackDamageBonus.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        HeavyVibrobladeDefenseAbilityNextAutoAttackDamageDurationSeconds = 481,

        /// <summary>
        /// Enmity added by Heavy Vibroblade Defense ability hits.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        HeavyVibrobladeDefenseAbilityEnmityBonus = 482,

        /// <summary>
        /// Enables Heavy Vibroblade Defense attacks to apply Crushing Blow.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        HeavyVibrobladeDefenseAbilityCrushingBlow = 483,

        /// <summary>
        /// Marks active Heavy Vibroblade Defense physical-defense or damage-reduction buffs.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        HeavyVibrobladeDefenseRecoveryWindow = 484,

        /// <summary>
        /// Percent of combat damage restored as HP while HeavyVibrobladeDefenseRecoveryWindow is active.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        HeavyVibrobladeDefenseDamageDealtHPPercentRestore = 485,

        /// <summary>
        /// Base maximum Stamina percent restored after spending HP on a Heavy Vibroblade Offense ability.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        HeavyVibrobladeOffenseHitPointSpendStaminaRestoreBasePercent = 486,

        /// <summary>
        /// Ability score used to scale HeavyVibrobladeOffenseHitPointSpendStaminaRestoreBasePercent.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        HeavyVibrobladeOffenseHitPointSpendStaminaRestoreScalingAbility = 487,

        /// <summary>
        /// Maximum Stamina percent for HeavyVibrobladeOffenseHitPointSpendStaminaRestoreBasePercent.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        HeavyVibrobladeOffenseHitPointSpendStaminaRestoreMaximumPercent = 488,

        /// <summary>
        /// Cooldown in seconds for HeavyVibrobladeOffenseHitPointSpendStaminaRestoreBasePercent.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        HeavyVibrobladeOffenseHitPointSpendStaminaRestoreCooldownSeconds = 489,

        /// <summary>
        /// Enables Heavy Vibroblade Offense weapon abilities to apply Essence Drain.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        HeavyVibrobladeOffenseEssenceHunter = 493,

        /// <summary>
        /// Enables Soul Sacrifice after spending HP on a Heavy Vibroblade Offense ability.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        HeavyVibrobladeOffenseHitPointSpendSoulSacrifice = 490,

        /// <summary>
        /// Window in seconds after spending HP during which Soul Ascension can trigger.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        HeavyVibrobladeOffenseHitPointSpendWindowSeconds = 491,

        /// <summary>
        /// Enables Soul Ascension on defeated enemies while the HP-spend window is active.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        HeavyVibrobladeOffenseSoulAscension = 492,

        /// <summary>
        /// Percent of maximum HP granted as Guardian's Resolve Temporary HP.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        HeavyVibrobladeDefenseGuardiansResolveShieldPercent = 494,

        /// <summary>
        /// Duration in seconds for HeavyVibrobladeDefenseGuardiansResolveShieldPercent.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        HeavyVibrobladeDefenseGuardiansResolveDurationSeconds = 495,

        /// <summary>
        /// Cooldown in seconds for HeavyVibrobladeDefenseGuardiansResolveShieldPercent.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        HeavyVibrobladeDefenseGuardiansResolveCooldownSeconds = 496,

        /// <summary>
        /// Duration in seconds for Exposed on the next matching skill ability after guarding a hit.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        GuardedHitNextSkillAbilityExposedDurationSeconds = 497,

        /// <summary>
        /// Enmity-to-source percent adjustment applied to targets hit by matching category abilities.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AbilityUsedPerkCategoryTargetEnmityToSourcePercentAdjustment = 498,

        /// <summary>
        /// Physical DMG rating dealt by a standalone pulse after guarding a hostile hit.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        GuardedHitPulseDMG = 499,

        /// <summary>
        /// Secondary hit damage added to Venom Current single-target abilities.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        KatarVenomCurrentSecondStrikeDamageBonus = 500,

        /// <summary>
        /// Radius in meters for Venom Current poison spread.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        KatarVenomCurrentPoisonSpreadRadiusMeters = 501,

        /// <summary>
        /// Duration in seconds for poison spread by Venom Current abilities.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        KatarVenomCurrentPoisonSpreadDurationSeconds = 502,

        /// <summary>
        /// Haste percent per Toxic Rush stack.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        KatarToxicRushHastePercentPerStack = 503,

        /// <summary>
        /// Attack percent per Toxic Rush stack.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        KatarToxicRushAttackPercentPerStack = 504,

        /// <summary>
        /// Maximum Toxic Rush stacks.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        KatarToxicRushMaximumStacks = 505,

        /// <summary>
        /// Duration in seconds for Toxic Rush stacks.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        KatarToxicRushDurationSeconds = 506,

        /// <summary>
        /// Bolster Resolve rank applied by Field Steward recovery commands.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        LeadershipFieldStewardBolsterResolveRank = 507,

        /// <summary>
        /// Mark Target rank applied by Vanguard offensive commands.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        LeadershipVanguardMarkTargetRank = 508,

        /// <summary>
        /// Damage added to Lightsaber Offense area abilities.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        LightsaberOffenseAreaDamageBonus = 509,

        /// <summary>
        /// Accuracy percent granted by Lightsaber Offense Centering.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        LightsaberOffenseCenteringAccuracyPercent = 510,

        /// <summary>
        /// Duration in seconds for LightsaberOffenseCenteringAccuracyPercent.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        LightsaberOffenseCenteringDurationSeconds = 511,

        /// <summary>
        /// Percent of max HP restored when Lightsaber Defense self-sustain triggers.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        LightsaberDefenseSelfRestorePercent = 512,

        /// <summary>
        /// Duration in seconds for Guardian's Influence.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        LightsaberDefenseGuardiansInfluenceDurationSeconds = 513,

        /// <summary>
        /// Damage added to Lightsaber Offense single-target abilities against debuffed enemies.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        LightsaberOffenseDebuffedTargetDamageBonus = 514,

        /// <summary>
        /// Enables Lightsaber Offense abilities to remove one harmful effect from the attacker.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        LightsaberOffensePurify = 515,

        /// <summary>
        /// Splash damage added around single-target Lightsaber Offense hits.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        LightsaberOffenseSingleTargetSplashDamage = 516,

        /// <summary>
        /// Damage added to Lightsaber Offense abilities after moving.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        LightsaberOffenseSurgeStrikeDamageBonus = 517,

        /// <summary>
        /// Damage added to Pistol attacks against Disoriented, Knockdown, or Tranquilized targets.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        PistolDamageToDisorientedKnockdownOrTranquilizedTargetBonus = 518,

        /// <summary>
        /// Duration in seconds for Disoriented from Skirmisher close-range abilities.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        PistolSkirmisherDisorientedDurationSeconds = 519,

        /// <summary>
        /// Damage added to Skirmisher ricochet bounces.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        PistolSkirmisherRicochetDamageBonus = 520,

        /// <summary>
        /// Maximum secondary targets for Skirmisher ricochet bounces.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        PistolSkirmisherRicochetMaximumTargets = 521,

        /// <summary>
        /// Cooldown in seconds for Skirmisher ricochet bounces.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        PistolSkirmisherRicochetCooldownSeconds = 522,

        /// <summary>
        /// Evasion percent granted by Skirmisher evasive abilities.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        PistolSkirmisherEvasiveAbilityEvasionPercent = 523,

        /// <summary>
        /// Duration in seconds for PistolSkirmisherEvasiveAbilityEvasionPercent.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        PistolSkirmisherEvasiveAbilityDurationSeconds = 524,

        /// <summary>
        /// Current enmity reduction percent from Skirmisher evasive abilities.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        PistolSkirmisherEvasiveAbilityEnmityReductionPercent = 525,

        /// <summary>
        /// Next pistol attack damage after Skirmisher evasive abilities.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        PistolSkirmisherEvasiveAbilityNextAttackDamageBonus = 526,

        /// <summary>
        /// Enables Pacification control shots to remove a beneficial effect.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        RiflePacificationNeutralizingShot = 528,

        /// <summary>
        /// Enables Pacification control shots to interrupt and apply Foggy Mind.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        RiflePacificationOverwatch = 529,

        /// <summary>
        /// Pinning Fire rank applied by Pacification control shots.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        RiflePacificationPinningFireRank = 530,

        /// <summary>
        /// Enables Conduit area abilities to apply Conduit Flare.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        SaberstaffConduitAreaConduitFlare = 531,

        /// <summary>
        /// Enables Tempest spinning attacks to apply Force Gyre.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        SaberstaffTempestForceGyre = 532,

        /// <summary>
        /// Force Attack percent granted whenever FP is actually restored.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        RestoredFPForceAttackPercentAdjustment = 533,

        /// <summary>
        /// Enables Spear Damage attacks to apply Breach.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        SpearDamageBreachStrike = 534,

        /// <summary>
        /// Enables Spear Damage area abilities to apply Crippling Defense.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        SpearDamageCripplingDefense = 535,

        /// <summary>
        /// Enables Spear Disabler attacks to apply Force Nullification.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        SpearDisablerForceNullification = 536,

        /// <summary>
        /// Enables Spear Disabler attacks to apply Forcebane.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        SpearDisablerForcebane = 537,

        /// <summary>
        /// Enables Spear Disabler suppression abilities to apply Fractured Focus.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        SpearDisablerFractureStrike = 538,

        /// <summary>
        /// Enables Spear Damage stances to apply Improved Attentiveness.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        SpearDamageImprovedAttentiveness = 539,

        /// <summary>
        /// Evasion granted to self after using an ability from a matching perk category.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AbilityUsedPerkCategorySelfEvasionPercentAdjustment = 540,

        /// <summary>
        /// Melee Deflection granted to nearby allies after using an ability from a matching perk category.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive, deflectionSource: DeflectionSource.Melee)]
        AbilityUsedPerkCategoryNearbyAllyAttackDeflection = 541,

        /// <summary>
        /// Damage added to hostile Staff abilities.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        StaffCrusherFinisherDamageBonus = 542,

        /// <summary>
        /// Damage added to Bombardier secondary explosives.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        ThrowingBombardierClusterStormDamageBonus = 543,

        /// <summary>
        /// Enables Deadeye single-target throws to apply Marking Toss.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        ThrowingDeadeyeMarkingToss = 544,

        /// <summary>
        /// Damage added to Deadeye ricochet tosses.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        ThrowingDeadeyeRicochetDamageBonus = 545,

        /// <summary>
        /// Maximum secondary targets for Deadeye ricochet tosses.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        ThrowingDeadeyeRicochetMaximumTargets = 546,

        /// <summary>
        /// Enables Bombardier control areas to leave Saturation Toss damage.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        ThrowingBombardierSaturationToss = 547,

        /// <summary>
        /// Enables Duelist retaliatory abilities to apply Reversal Cut.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        TwinBladeDuelistReversalCut = 548,

        /// <summary>
        /// Enables Cyclone area abilities to apply Sweeping Advance.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        TwinBladeCycloneSweepingAdvance = 549,

        /// <summary>
        /// Evasive Combat rank applied by Vibroknife Shadow evasive abilities.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        VibroknifeShadowEvasiveCombatRank = 551,

        /// <summary>
        /// Enables Vibroknife Shadow single-target abilities to apply Marked for Death.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        VibroknifeShadowMarkedForDeath = 552,

        /// <summary>
        /// Sap Vitality rank applied by Vibroknife Saboteur control abilities.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        VibroknifeSaboteurSapVitalityRank = 553,

        /// <summary>
        /// Toxic Coating rank applied by Vibroknife Saboteur strike abilities.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        VibroknifeSaboteurToxicCoatingRank = 554,

        /// <summary>
        /// Damage added to the next matching skill ability that applies Exposed after guarding a hit.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        GuardedHitNextSkillAbilityExposedDamageBonus = 555,

        /// <summary>
        /// Percent current enmity reduction granted by Lightsaber Offense Centering.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        LightsaberOffenseCenteringEnmityReductionPercent = 556,

        /// <summary>
        /// Cooldown in seconds for Lightsaber Offense Centering.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        LightsaberOffenseCenteringCooldownSeconds = 557,

        /// <summary>
        /// Ranged Deflection granted to nearby allies by Guardian's Influence.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive, deflectionSource: DeflectionSource.Ranged)]
        LightsaberDefenseGuardiansInfluenceAttackDeflection = 558,

        /// <summary>
        /// Duration in seconds for Sunder from Lightsaber Offense area abilities.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        LightsaberOffenseAreaSunderDurationSeconds = 559,

        /// <summary>
        /// Cooldown in seconds for Lightsaber Offense Purify.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        LightsaberOffensePurifyCooldownSeconds = 560,

        /// <summary>
        /// Force Disruption duration in seconds from Lightsaber Offense single-target abilities.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        LightsaberOffenseSingleTargetForceDisruptionDurationSeconds = 561,

        /// <summary>
        /// Stamina threshold percent required for Lightsaber Offense Second Wind.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        LightsaberOffenseSecondWindThresholdPercent = 562,

        /// <summary>
        /// Base maximum Stamina percent restored by Lightsaber Offense Second Wind.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        LightsaberOffenseSecondWindStaminaRestoreBasePercent = 563,

        /// <summary>
        /// Ability score used to scale Lightsaber Offense Second Wind.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        LightsaberOffenseSecondWindScalingAbility = 564,

        /// <summary>
        /// Maximum Stamina percent restored by Lightsaber Offense Second Wind.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        LightsaberOffenseSecondWindStaminaRestoreMaximumPercent = 565,

        /// <summary>
        /// Cooldown in seconds for Lightsaber Offense Second Wind.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        LightsaberOffenseSecondWindCooldownSeconds = 566,

        /// <summary>
        /// Duration in seconds for Disoriented from Lightsaber Offense area abilities.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        LightsaberOffenseAreaDisorientedDurationSeconds = 567,

        /// <summary>
        /// Cooldown in seconds for category-triggered self defensive stat riders.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityUsedPerkCategorySelfDefenseCooldownSeconds = 568,

        /// <summary>
        /// Duration in seconds for Dazed from hostile Staff abilities.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        StaffCrusherFinisherDazedDurationSeconds = 569,

        /// <summary>
        /// Maximum secondary explosions created by Bombardier area abilities.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        ThrowingBombardierClusterStormMaximumTargets = 570,

        /// <summary>
        /// Duration in seconds for Saturation Toss area pulses.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        ThrowingBombardierSaturationTossDurationSeconds = 571,

        /// <summary>
        /// Damage dealt by Saturation Toss area pulses.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        ThrowingBombardierSaturationTossDamage = 572,

        /// <summary>
        /// Pulse interval in seconds for Saturation Toss.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        ThrowingBombardierSaturationTossPulseSeconds = 573,

        /// <summary>
        /// Damage added to the next Twin Blade Duelist ability after taking damage.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        TwinBladeDuelistReversalCutDamageBonus = 574,

        /// <summary>
        /// Duration in seconds for Dazed from Reversal Cut.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        TwinBladeDuelistReversalCutDazedDurationSeconds = 575,

        /// <summary>
        /// Window in seconds for the next Twin Blade Duelist ability from Reversal Cut.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        TwinBladeDuelistReversalCutWindowSeconds = 576,

        /// <summary>
        /// Minimum targets hit required for Sweeping Advance.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        TwinBladeCycloneSweepingAdvanceMinimumTargets = 577,

        /// <summary>
        /// Stamina restored by Sweeping Advance.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        TwinBladeCycloneSweepingAdvanceStaminaRestore = 578,

        /// <summary>
        /// Haste percent granted by Sweeping Advance.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        TwinBladeCycloneSweepingAdvanceHastePercent = 579,

        /// <summary>
        /// Duration in seconds for Sweeping Advance.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        TwinBladeCycloneSweepingAdvanceDurationSeconds = 580,

        /// <summary>
        /// Damage added by Conduit Flare.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        SaberstaffConduitFlareDamageBonus = 581,

        /// <summary>
        /// Force Disruption duration in seconds from Conduit Flare.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        SaberstaffConduitFlareForceDisruptionDurationSeconds = 582,

        /// <summary>
        /// Force Erosion duration in seconds from Force Gyre.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        SaberstaffTempestForceGyreDurationSeconds = 583,

        /// <summary>
        /// Stamina restored when Crippling Defense affects enough targets.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        SpearDamageCripplingDefenseStaminaRestore = 584,

        /// <summary>
        /// Minimum targets required for Crippling Defense stamina restoration.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        SpearDamageCripplingDefenseMinimumTargets = 585,

        /// <summary>
        /// SkillType value of abilities that trigger AbilityUsedEvasionPercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityUsedEvasionPercentAdjustmentSkillType = 586,

        /// <summary>
        /// Temporary percent Evasion adjustment applied after using a matching ability.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AbilityUsedEvasionPercentAdjustment = 587,

        /// <summary>
        /// Duration in seconds for AbilityUsedEvasionPercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityUsedEvasionDurationSeconds = 588,

        /// <summary>
        /// SkillType value of abilities that trigger AbilityUsedRangedDeflection.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityUsedRangedDeflectionSkillType = 826,

        /// <summary>
        /// Temporary Ranged Deflection granted after using a matching ability.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive, deflectionSource: DeflectionSource.Ranged)]
        AbilityUsedRangedDeflection = 827,

        /// <summary>
        /// Duration in seconds for AbilityUsedRangedDeflection.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityUsedRangedDeflectionDurationSeconds = 828,

        /// <summary>
        /// SkillType value of single-target abilities that trigger SingleTargetAbilityAttackDeflection.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        SingleTargetAbilityAttackDeflectionSkillType = 589,

        /// <summary>
        /// Temporary Melee Deflection granted after using a matching single-target ability.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive, deflectionSource: DeflectionSource.Melee)]
        SingleTargetAbilityAttackDeflection = 590,

        /// <summary>
        /// Duration in seconds for SingleTargetAbilityAttackDeflection.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        SingleTargetAbilityAttackDeflectionDurationSeconds = 591,

        /// <summary>
        /// Primary PerkType value whose ability triggers HeavyVibrobladeDefenseAbilityNextAutoAttackDamageBonus.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerPrimaryPerkType = 592,

        /// <summary>
        /// Primary PerkType value whose ability triggers HeavyVibrobladeDefenseAbilityCrushingBlow.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        HeavyVibrobladeDefenseAbilityCrushingBlowTriggerPrimaryPerkType = 593,

        /// <summary>
        /// Primary PerkType value whose ability triggers HeavyVibrobladeDefenseGuardiansResolveShieldPercent.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        HeavyVibrobladeDefenseGuardiansResolveTriggerPrimaryPerkType = 594,

        /// <summary>
        /// Secondary PerkType value whose ability triggers HeavyVibrobladeDefenseGuardiansResolveShieldPercent.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        HeavyVibrobladeDefenseGuardiansResolveTriggerSecondaryPerkType = 595,

        /// <summary>
        /// Tertiary PerkType value whose ability triggers HeavyVibrobladeDefenseGuardiansResolveShieldPercent.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        HeavyVibrobladeDefenseGuardiansResolveTriggerTertiaryPerkType = 596,

        /// <summary>
        /// Quaternary PerkType value whose ability triggers HeavyVibrobladeDefenseGuardiansResolveShieldPercent.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        HeavyVibrobladeDefenseGuardiansResolveTriggerQuaternaryPerkType = 597,

        /// <summary>
        /// Cooldown in seconds for MeleeDeflectionStaminaRestore.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial, deflectionSource: DeflectionSource.Melee)]
        MeleeDeflectionStaminaRestoreCooldownSeconds = 598,

        /// <summary>
        /// Cooldown in seconds for CriticalHPPercentOfDamageRestore.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        CriticalHPPercentOfDamageRestoreCooldownSeconds = 599,

        /// <summary>
        /// Secondary PerkType value whose ability triggers HeavyVibrobladeDefenseAbilityNextAutoAttackDamageBonus.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerSecondaryPerkType = 600,

        /// <summary>
        /// Tertiary PerkType value whose ability triggers HeavyVibrobladeDefenseAbilityNextAutoAttackDamageBonus.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerTertiaryPerkType = 601,

        /// <summary>
        /// Quaternary PerkType value whose ability triggers HeavyVibrobladeDefenseAbilityNextAutoAttackDamageBonus.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerQuaternaryPerkType = 602,

        /// <summary>
        /// Quinary PerkType value whose ability triggers HeavyVibrobladeDefenseAbilityNextAutoAttackDamageBonus.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerQuinaryPerkType = 603,

        /// <summary>
        /// Senary PerkType value whose ability triggers HeavyVibrobladeDefenseAbilityNextAutoAttackDamageBonus.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerSenaryPerkType = 604,

        /// <summary>
        /// Secondary PerkType value whose ability triggers HeavyVibrobladeDefenseAbilityCrushingBlow.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        HeavyVibrobladeDefenseAbilityCrushingBlowTriggerSecondaryPerkType = 605,

        /// <summary>
        /// Tertiary PerkType value whose ability triggers HeavyVibrobladeDefenseAbilityCrushingBlow.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        HeavyVibrobladeDefenseAbilityCrushingBlowTriggerTertiaryPerkType = 606,

        /// <summary>
        /// Quaternary PerkType value whose ability triggers HeavyVibrobladeDefenseAbilityCrushingBlow.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        HeavyVibrobladeDefenseAbilityCrushingBlowTriggerQuaternaryPerkType = 607,

        /// <summary>
        /// Quinary PerkType value whose ability triggers HeavyVibrobladeDefenseAbilityCrushingBlow.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        HeavyVibrobladeDefenseAbilityCrushingBlowTriggerQuinaryPerkType = 608,

        /// <summary>
        /// Senary PerkType value whose ability triggers HeavyVibrobladeDefenseAbilityCrushingBlow.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        HeavyVibrobladeDefenseAbilityCrushingBlowTriggerSenaryPerkType = 609,

        /// <summary>
        /// Flat bonus added to Disruption resistance.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DisruptionResistance = 610,

        /// <summary>
        /// Cooldown in milliseconds for IncomingCriticalHitDowngradeToMinimumDamage.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        IncomingCriticalHitDowngradeCooldownMilliseconds = 614,

        /// <summary>
        /// SkillType value for deflection-triggered next-skill ability bonuses. Unset applies to any skill.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial, deflectionSource: DeflectionSource.Ranged)]
        DeflectionNextSkillAbilitySkillType = 615,

        /// <summary>
        /// Percent critical damage adjustment applied to Staff critical hits.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        StaffCriticalDamagePercentAdjustment = 616,

        /// <summary>
        /// Percent critical rate adjustment applied to Staff attacks and abilities.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        StaffCriticalRatePercentAdjustment = 617,

        /// <summary>
        /// Percent defense adjustment applied to the target on Staff critical hits.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenNegative)]
        StaffCriticalTargetDefensePercentAdjustment = 618,

        /// <summary>
        /// Duration in seconds for StaffCriticalTargetDefensePercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        StaffCriticalTargetDefenseDurationSeconds = 619,

        /// <summary>
        /// Percent critical damage adjustment applied to ranged weapon skill critical hits.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        RangedCriticalDamagePercentAdjustment = 620,

        /// <summary>
        /// Primary PerkType value whose Twin Blade ability can consume Reversal Cut.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        TwinBladeDuelistReversalCutTriggerPrimaryPerkType = 621,

        /// <summary>
        /// Secondary PerkType value whose Twin Blade ability can consume Reversal Cut.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        TwinBladeDuelistReversalCutTriggerSecondaryPerkType = 622,

        /// <summary>
        /// Tertiary PerkType value whose Twin Blade ability can consume Reversal Cut.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        TwinBladeDuelistReversalCutTriggerTertiaryPerkType = 623,

        /// <summary>
        /// Multiplier for the attacker's positive Might modifier added as weapon damage bonus.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        WeaponMightModifierDamageMultiplier = 624,

        /// <summary>
        /// Flat damage added to ranged weapon attacks and ranged combat abilities.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        RangedAttackDamageFlatAdjustment = 625,

        /// <summary>
        /// Percent of target Defense ignored by ranged weapon attacks and ranged combat abilities.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        RangedAttackDefenseIgnorePercentAdjustment = 626,

        /// <summary>
        /// Cooldown in seconds for DeflectionFPRestore.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial, deflectionSource: DeflectionSource.Ranged)]
        DeflectionFPRestoreCooldownSeconds = 627,

        /// <summary>
        /// SkillType id whose next auto-attack receives a critical chance bonus after deflecting an attack.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial, deflectionSource: DeflectionSource.Ranged)]
        DeflectionNextAutoAttackCriticalRateSkillType = 630,

        /// <summary>
        /// Critical chance bonus for the next matching auto-attack after deflecting an attack.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive, deflectionSource: DeflectionSource.Ranged)]
        DeflectionNextAutoAttackCriticalRatePercentAdjustment = 631,

        /// <summary>
        /// Duration in seconds for DeflectionNextAutoAttackCriticalRatePercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        DeflectionNextAutoAttackCriticalRateWindowSeconds = 632,

        /// <summary>
        /// Internal temporary SkillType value consumed by the next matching auto-attack critical chance bonus.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        NextAutoAttackCriticalRateSkillType = 633,

        /// <summary>
        /// Internal temporary critical chance bonus consumed by the next matching auto-attack.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        NextAutoAttackCriticalRatePercentAdjustment = 634,

        /// <summary>
        /// Cooldown in seconds for AvoidedAttackStaminaRestore.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AvoidedAttackStaminaRestoreCooldownSeconds = 635,

        /// <summary>
        /// SkillType id required before direct damage dealt restores Stamina. Invalid or 0 allows any skill.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        DamageDealtStaminaRestoreSkillType = 636,

        /// <summary>
        /// Flat Stamina restored after dealing direct damage, subject to DamageDealtStaminaRestoreCooldownSeconds.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DamageDealtStaminaRestore = 637,

        /// <summary>
        /// Cooldown in seconds for DamageDealtStaminaRestore.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        DamageDealtStaminaRestoreCooldownSeconds = 638,

        /// <summary>
        /// SkillType id required before direct damage dealt grants AttackDelayReductionPercent.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        DamageDealtAttackDelayReductionSkillType = 639,

        /// <summary>
        /// Attack delay reduction granted after dealing direct damage.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DamageDealtAttackDelayReductionPercent = 640,

        /// <summary>
        /// Duration in seconds for DamageDealtAttackDelayReductionPercent.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        DamageDealtAttackDelayReductionDurationSeconds = 641,

        /// <summary>
        /// Percent of critical hit damage also removed from the target's Stamina.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        CriticalTargetStaminaLossPercentOfDamage = 642,

        /// <summary>
        /// Flat Stamina removed each Force Erosion tick.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DamageDealtForceErosionStaminaLossPerTick = 643,

        /// <summary>
        /// Duration in seconds for Sunder from hostile Lightsaber Offense abilities.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        LightsaberOffenseSunderDurationSeconds = 644,

        /// <summary>
        /// Duration in seconds for Disoriented from hostile Lightsaber Offense abilities.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        LightsaberOffenseDisorientedDurationSeconds = 645,

        /// <summary>
        /// SkillType id required before direct damage can inflict Hamstring. Invalid or 0 allows any skill.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        DamageDealtHamstringSkillType = 646,

        /// <summary>
        /// Percent chance for direct damage to inflict Hamstring.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DamageDealtHamstringChance = 647,

        /// <summary>
        /// Duration in seconds for Hamstring inflicted by direct damage.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        DamageDealtHamstringDurationSeconds = 648,

        /// <summary>
        /// SkillType id required before hostile abilities trigger AbilityUsedAttackDeflection. Invalid or 0 allows any skill.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityUsedAttackDeflectionSkillType = 649,

        /// <summary>
        /// Temporary Melee Deflection granted after using a matching hostile ability.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive, deflectionSource: DeflectionSource.Melee)]
        AbilityUsedAttackDeflection = 650,

        /// <summary>
        /// Duration in seconds for AbilityUsedAttackDeflection.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityUsedAttackDeflectionDurationSeconds = 651,

        /// <summary>
        /// Seconds added to the attacker's Bleed and Hemorrhage effects on a target after a critical hit.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        CriticalBleedingStatusDurationExtensionSeconds = 652,

        /// <summary>
        /// Cooldown in seconds for CriticalBleedingStatusDurationExtensionSeconds.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        CriticalBleedingStatusDurationExtensionCooldownSeconds = 653,

        /// <summary>
        /// Seconds between two different hostile abilities required to grant HostileAbilitySequenceNextAttackBleedDurationSeconds.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        HostileAbilitySequenceWindowSeconds = 654,

        /// <summary>
        /// Bleed duration granted to the next damaging attack after two different hostile abilities are used within the sequence window.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        HostileAbilitySequenceNextAttackBleedDurationSeconds = 655,

        /// <summary>
        /// Temporary Bleed duration consumed by the next direct damage dealt.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        NextDamageDealtBleedDurationSeconds = 656,

        /// <summary>
        /// Number of hostile ability hits against the same target required to trigger SameTargetHostileAbilityStaminaRestore.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        SameTargetHostileAbilityHitCountRequired = 657,

        /// <summary>
        /// Stamina restored when SameTargetHostileAbilityHitCountRequired is reached.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        SameTargetHostileAbilityStaminaRestore = 658,

        /// <summary>
        /// Flat Stamina cost adjustment granted to the next matching skill ability when a bleeding effect naturally expires.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenNegative)]
        BleedingStatusExpiredNextSkillAbilityStaminaCostAdjustment = 659,

        /// <summary>
        /// SkillType id required for BleedingStatusExpiredNextSkillAbilityStaminaCostAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        BleedingStatusExpiredNextSkillAbilitySkillType = 660,

        /// <summary>
        /// Duration in seconds for BleedingStatusExpiredNextSkillAbilityStaminaCostAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        BleedingStatusExpiredNextSkillAbilityWindowSeconds = 661,

        /// <summary>
        /// Haste granted when a hostile ability restores both FP and Stamina.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AbilityRestoredBothResourcesHastePercentAdjustment = 662,

        /// <summary>
        /// Duration in seconds for AbilityRestoredBothResourcesHastePercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityRestoredBothResourcesHasteDurationSeconds = 663,

        /// <summary>
        /// FP restored after using a hostile ability.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        HostileAbilityFPRestore = 664,

        /// <summary>
        /// Stamina restored after using a hostile ability.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        HostileAbilityStaminaRestore = 665,

        /// <summary>
        /// Flat hostile ability damage bonus granted while FP and Stamina are both above HighFPAndStaminaAbilityDamageBonusThresholdPercent.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        HighFPAndStaminaAbilityDamageBonus = 666,

        /// <summary>
        /// Percent of maximum FP and Stamina both required to enable HighFPAndStaminaAbilityDamageBonus.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial, StatTypeAggregation.Maximum)]
        HighFPAndStaminaAbilityDamageBonusThresholdPercent = 667,

        /// <summary>
        /// FP restored on the regeneration interval while FP and Stamina are both below LowFPAndStaminaIntervalThresholdPercent.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        LowFPAndStaminaIntervalFPRestore = 668,

        /// <summary>
        /// Stamina restored on the regeneration interval while FP and Stamina are both below LowFPAndStaminaIntervalThresholdPercent.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        LowFPAndStaminaIntervalStaminaRestore = 669,

        /// <summary>
        /// Percent of maximum FP and Stamina both required below before low-resource interval restoration triggers.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        LowFPAndStaminaIntervalThresholdPercent = 670,

        /// <summary>
        /// Critical-hit count required within CriticalHitSequenceWindowSeconds to trigger CriticalHitSequenceStaminaRestore.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        CriticalHitSequenceCountRequired = 671,

        /// <summary>
        /// Seconds allowed between critical hits for CriticalHitSequenceCountRequired.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        CriticalHitSequenceWindowSeconds = 672,

        /// <summary>
        /// Stamina restored when the critical-hit sequence completes.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        CriticalHitSequenceStaminaRestore = 673,

        /// <summary>
        /// Seconds added to the attacker's Bleed on the target when a matching ability hits a bleeding target.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        BleedingTargetAbilityBleedDurationExtensionSeconds = 674,

        /// <summary>
        /// Duration in seconds used to refresh generated Frenzy Slash haste stacks.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        FrenzySlashHasteRefreshDurationSeconds = 675,

        /// <summary>
        /// SkillType id required before AbilityDamageToBleedingTargetBonus applies.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityDamageToBleedingTargetSkillType = 676,

        /// <summary>
        /// Flat ability damage bonus against bleeding targets.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AbilityDamageToBleedingTargetBonus = 677,

        /// <summary>
        /// Percent chance for a physical ability against a bleeding target to spread Bleed.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        BleedingTargetAbilityBleedSpreadChance = 678,

        /// <summary>
        /// Bleed duration applied by BleedingTargetAbilityBleedSpreadChance.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        BleedingTargetAbilityBleedSpreadDurationSeconds = 679,

        /// <summary>
        /// Maximum nearby targets affected by BleedingTargetAbilityBleedSpreadChance.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        BleedingTargetAbilityBleedSpreadMaxTargets = 680,

        /// <summary>
        /// Duration used to refresh ability-used Ranged Deflection after avoiding an attack.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AvoidedAttackAbilityUsedRangedDeflectionRefreshDurationSeconds = 681,

        /// <summary>
        /// SkillType value whose next auto-attack is quickened after avoiding an attack.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AvoidedAttackNextAutoAttackNoDelaySkillType = 682,

        /// <summary>
        /// Duration for AvoidedAttackNextAutoAttackNoDelaySkillType.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AvoidedAttackNextAutoAttackNoDelayDurationSeconds = 683,

        /// <summary>
        /// Percent chance for auto-attacks to add a Suppression stack.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AutoAttackSuppressionStackChance = 684,

        /// <summary>
        /// Duration of Suppression stacks added by auto-attacks.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AutoAttackSuppressionStackDurationSeconds = 685,

        /// <summary>
        /// Evasion penalty percent carried by Suppression stacks added by auto-attacks.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AutoAttackSuppressionStackEvasionPenaltyPercent = 686,

        /// <summary>
        /// Duration of Suppression stacks added by ranged hits.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        RangedHitSuppressionStackDurationSeconds = 687,

        /// <summary>
        /// Evasion penalty percent carried by Suppression stacks added by ranged hits.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        RangedHitSuppressionStackEvasionPenaltyPercent = 688,

        /// <summary>
        /// Extra Evasion penalty percent added per source-owned Suppression stack.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        SuppressionStackEvasionPenaltyPercentAdjustment = 689,

        /// <summary>
        /// Damage percent adjustment for abilities which spend hit points.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        HitPointSpendAbilityDamagePercentAdjustment = 690,

        /// <summary>
        /// Temporary hit point percent of the spent HP granted after spending HP on an ability.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        HitPointSpendTemporaryHPPercentOfSpentHP = 691,

        /// <summary>
        /// Duration in seconds for HitPointSpendTemporaryHPPercentOfSpentHP.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        HitPointSpendTemporaryHPDurationSeconds = 692,

        /// <summary>
        /// HP threshold percent for LowHPAttackPercentAdjustment and LowHPCriticalRatePercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        LowHPAttackThresholdPercent = 693,

        /// <summary>
        /// Attack percent adjustment while below LowHPAttackThresholdPercent.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        LowHPAttackPercentAdjustment = 694,

        /// <summary>
        /// Critical rate adjustment while below LowHPAttackThresholdPercent.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        LowHPCriticalRatePercentAdjustment = 695,

        /// <summary>
        /// FP drained by hostile abilities against targets affected by Foggy Mind.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AbilityResourceDrainFoggyMindFP = 696,

        /// <summary>
        /// Stamina drained by hostile abilities against targets affected by Foggy Mind.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AbilityResourceDrainFoggyMindStamina = 697,

        /// <summary>
        /// Defense ignore percent against targets affected by Force Disruption or Foggy Mind.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AbilityDefenseIgnoreForceDisruptionOrFoggyMindPercentAdjustment = 698,

        /// <summary>
        /// Temporary Accuracy percent adjustment after avoiding an attack.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AvoidedAttackAccuracyPercentAdjustment = 699,

        /// <summary>
        /// Duration in seconds for AvoidedAttackAccuracyPercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AvoidedAttackAccuracyDurationSeconds = 700,

        /// <summary>
        /// StatusEffectCategory flags required for status-applied stat riders.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial, StatTypeAggregation.BitwiseOr)]
        StatusAppliedRequiredCategory = 701,

        /// <summary>
        /// SkillType value whose next ability receives status-applied bonuses.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        StatusAppliedNextSkillAbilitySkillType = 702,

        /// <summary>
        /// Flat damage bonus granted to the next matching skill ability after applying a matching status.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        StatusAppliedNextSkillAbilityDamageBonus = 703,

        /// <summary>
        /// Critical rate bonus granted to the next matching skill ability after applying a matching status.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        StatusAppliedNextSkillAbilityCriticalRatePercentAdjustment = 704,

        /// <summary>
        /// Duration in seconds for status-applied next ability bonuses.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        StatusAppliedNextSkillAbilityWindowSeconds = 705,

        /// <summary>
        /// Temporary Melee Deflection granted after applying a matching status.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive, deflectionSource: DeflectionSource.Melee)]
        StatusAppliedSelfAttackDeflection = 706,

        /// <summary>
        /// Temporary Defense percent granted after applying a matching status.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        StatusAppliedSelfDefensePercentAdjustment = 707,

        /// <summary>
        /// Temporary Evasion percent granted after applying a matching status.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        StatusAppliedSelfEvasionPercentAdjustment = 708,

        /// <summary>
        /// Temporary Force Attack percent granted after applying a matching status.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        StatusAppliedSelfForceAttackPercentAdjustment = 709,

        /// <summary>
        /// Temporary Haste percent granted after applying a matching status.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        StatusAppliedSelfHastePercentAdjustment = 710,

        /// <summary>
        /// Temporary Enmity percent granted after applying a matching status.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        StatusAppliedSelfEnmityPercentAdjustment = 711,

        /// <summary>
        /// Duration in seconds for status-applied self stat riders.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        StatusAppliedSelfDurationSeconds = 712,

        /// <summary>
        /// Temporary Physical Defense percent applied to a target after applying a matching status.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenNegative)]
        StatusAppliedTargetPhysicalDefensePercentAdjustment = 713,

        /// <summary>
        /// Temporary Accuracy percent applied to a target after applying a matching status.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenNegative)]
        StatusAppliedTargetAccuracyPercentAdjustment = 714,

        /// <summary>
        /// Duration in seconds for status-applied target stat riders.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        StatusAppliedTargetDurationSeconds = 715,

        /// <summary>
        /// StatusEffectCategory flags required on the target for ability target-status riders.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityTargetStatusRequiredCategory = 716,

        /// <summary>
        /// Temporary Physical Defense percent applied by abilities against targets with AbilityTargetStatusRequiredCategory.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenNegative)]
        AbilityTargetStatusPhysicalDefensePercentAdjustment = 717,

        /// <summary>
        /// Duration in seconds for AbilityTargetStatusPhysicalDefensePercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityTargetStatusDurationSeconds = 718,

        /// <summary>
        /// SkillType value whose area abilities trigger AreaAbilityUsedEvasionPercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AreaAbilityUsedEvasionPercentAdjustmentSkillType = 719,

        /// <summary>
        /// Temporary Evasion percent granted after using a matching area ability.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AreaAbilityUsedEvasionPercentAdjustment = 720,

        /// <summary>
        /// Duration in seconds for AreaAbilityUsedEvasionPercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AreaAbilityUsedEvasionDurationSeconds = 721,

        /// <summary>
        /// Bleed duration applied to nearby enemies after defeating a bleeding enemy.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DefeatedBleedingEnemyNearbyBleedDurationSeconds = 722,

        /// <summary>
        /// StatusEffectCategory flags required on the target for target-status critical rate.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        TargetStatusCriticalRateStatusCategory = 723,

        /// <summary>
        /// Critical rate adjustment against targets matching TargetStatusCriticalRateStatusCategory.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        TargetStatusCriticalRatePercentAdjustment = 724,

        /// <summary>
        /// Force Attack percent granted per hostile ability use.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        HostileAbilityForceAttackPercentPerStack = 725,

        /// <summary>
        /// Duration in seconds for HostileAbilityForceAttackPercentPerStack.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        HostileAbilityForceAttackDurationSeconds = 726,

        /// <summary>
        /// Maximum total Force Attack percent from HostileAbilityForceAttackPercentPerStack.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        HostileAbilityForceAttackPercentMax = 727,

        /// <summary>
        /// SkillType value whose area abilities can receive recent-deflection damage.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial, deflectionSource: DeflectionSource.Ranged)]
        AreaAbilityAfterDeflectionDamagePercentAdjustmentSkillType = 728,

        /// <summary>
        /// Damage percent granted to matching area abilities after a recent deflection.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive, deflectionSource: DeflectionSource.Ranged)]
        AreaAbilityAfterDeflectionDamagePercentAdjustment = 729,

        /// <summary>
        /// Recent deflection window for AreaAbilityAfterDeflectionDamagePercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial, deflectionSource: DeflectionSource.Ranged)]
        AreaAbilityAfterDeflectionWindowSeconds = 730,

        /// <summary>
        /// Haste granted after a combat ability restores FP.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AbilityRestoredFPHastePercentAdjustment = 731,

        /// <summary>
        /// Duration in seconds for AbilityRestoredFPHastePercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityRestoredFPHasteDurationSeconds = 732,

        /// <summary>
        /// Required ranged auto-attack count before the cross-skill ranged cycle grants bonus
        /// Critical Rate. Any ranged weapon skill advances the cycle.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        RangedAutoAttackCycleCriticalRateRequiredCount = 734,

        /// <summary>
        /// Critical rate adjustment granted on the ranged auto-attack that completes the cycle.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        RangedAutoAttackCycleCriticalRatePercentAdjustment = 735,

        /// <summary>
        /// SkillType value whose non-critical abilities build next-ability Critical Rate.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        NonCriticalAbilityNextSkillAbilityCriticalRateSkillType = 736,

        /// <summary>
        /// Critical rate added after a non-critical matching ability.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        NonCriticalAbilityNextSkillAbilityCriticalRatePercentAdjustment = 737,

        /// <summary>
        /// Maximum Critical Rate from NonCriticalAbilityNextSkillAbilityCriticalRatePercentAdjustment stacks.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        NonCriticalAbilityNextSkillAbilityCriticalRateMax = 738,

        /// <summary>
        /// Duration in seconds for non-critical next ability Critical Rate stacks.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        NonCriticalAbilityNextSkillAbilityCriticalRateWindowSeconds = 739,

        /// <summary>
        /// Target HP threshold percent for CriticalDamageHighHPTargetPercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        CriticalDamageHighHPTargetThresholdPercent = 740,

        /// <summary>
        /// Critical damage percent adjustment against targets above CriticalDamageHighHPTargetThresholdPercent.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        CriticalDamageHighHPTargetPercentAdjustment = 741,

        /// <summary>
        /// Ability hit chance adjustment against source-suppressed targets after they use a combat ability.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AbilityHitChanceAgainstSuppressionStackPercentAdjustment = 742,

        /// <summary>
        /// Source-owned Suppression stack count required before a target's damage to other targets is adjusted.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        SuppressionStackDamageDealtToOtherTargetsRequiredStacks = 743,

        /// <summary>
        /// Damage percent adjustment for suppressed targets attacking someone other than the Suppression source.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenNegative)]
        SuppressionStackDamageDealtToOtherTargetsPercentAdjustment = 744,

        /// <summary>
        /// Physical Defense percent applied when an ability with defense ignore hits.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenNegative)]
        DefenseIgnoreHitPhysicalDefensePercentAdjustment = 745,

        /// <summary>
        /// Duration in seconds for DefenseIgnoreHitPhysicalDefensePercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        DefenseIgnoreHitPhysicalDefenseDurationSeconds = 746,

        /// <summary>
        /// Fragmentation damage dealt by matching area abilities.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AreaAbilityFragmentationDamage = 747,

        /// <summary>
        /// Duration in seconds for fragmentation applied by matching area abilities.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AreaAbilityFragmentationDurationSeconds = 748,

        /// <summary>
        /// Pulse interval in seconds for fragmentation applied by matching area abilities.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AreaAbilityFragmentationPulseSeconds = 749,

        /// <summary>
        /// SkillType value whose area abilities count toward area target-hit sequences.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AreaAbilityTargetHitSequenceSkillType = 750,

        /// <summary>
        /// Required hit count within AreaAbilityTargetHitSequenceWindowSeconds.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AreaAbilityTargetHitSequenceCountRequired = 751,

        /// <summary>
        /// Window in seconds for AreaAbilityTargetHitSequenceCountRequired.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AreaAbilityTargetHitSequenceWindowSeconds = 752,

        /// <summary>
        /// Exposed duration applied when an area target-hit sequence completes.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AreaAbilityTargetHitSequenceExposedDurationSeconds = 753,

        /// <summary>
        /// SkillType value whose idle status durations can be adjusted.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        IdleStatusDurationPercentAdjustmentSkillType = 754,

        /// <summary>
        /// Duration percent adjustment for matching status effects after an idle window.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        IdleStatusDurationPercentAdjustment = 755,

        /// <summary>
        /// Idle seconds required for IdleStatusDurationPercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        IdleStatusDurationRequiredIdleSeconds = 756,

        /// <summary>
        /// StatusEffectCategory flags required for IdleStatusDurationPercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        IdleStatusDurationRequiredCategory = 757,

        /// <summary>
        /// Evasion granted after landing a critical hit.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        CriticalHitSelfEvasionPercentAdjustment = 758,

        /// <summary>
        /// Duration in seconds for CriticalHitSelfEvasionPercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        CriticalHitSelfEvasionDurationSeconds = 759,

        /// <summary>
        /// Defense granted to nearby allies after using a matching ability.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AbilityUsedNearbyAllyDefensePercentAdjustment = 760,

        /// <summary>
        /// Force Defense granted to nearby allies after using a matching ability.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AbilityUsedNearbyAllyForceDefensePercentAdjustment = 761,

        /// <summary>
        /// Duration in seconds for ability-used nearby ally defense riders.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityUsedNearbyAllyDefenseDurationSeconds = 762,

        /// <summary>
        /// StatusEffectCategory flags required for conditional critical damage.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        CriticalDamageTargetStatusCategory = 763,

        /// <summary>
        /// Critical damage percent adjustment against targets matching CriticalDamageTargetStatusCategory.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        CriticalDamageTargetStatusPercentAdjustment = 764,

        /// <summary>
        /// Haste granted after landing a critical hit.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        CriticalHitSelfHastePercentAdjustment = 765,

        /// <summary>
        /// Duration in seconds for CriticalHitSelfHastePercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        CriticalHitSelfHasteDurationSeconds = 766,

        /// <summary>
        /// SkillType value whose hostile abilities grant temporary movement speed.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityUsedMovementSpeedPercentAdjustmentSkillType = 767,

        /// <summary>
        /// Movement speed granted after using a matching hostile ability.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AbilityUsedMovementSpeedPercentAdjustment = 768,

        /// <summary>
        /// Duration in seconds for AbilityUsedMovementSpeedPercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityUsedMovementSpeedDurationSeconds = 769,

        /// <summary>
        /// Damage dealt percent applied to close targets hit by a ranged ability.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenNegative)]
        RangedAbilityHitNearTargetDamageDealtPercentAdjustment = 770,

        /// <summary>
        /// Range in meters for RangedAbilityHitNearTargetDamageDealtPercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        RangedAbilityHitNearTargetRangeMeters = 771,

        /// <summary>
        /// Duration in seconds for RangedAbilityHitNearTargetDamageDealtPercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        RangedAbilityHitNearTargetDurationSeconds = 772,

        /// <summary>
        /// SkillType value whose next ability gains damage after the creature takes damage.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        DamageTakenNextSkillAbilitySkillType = 773,

        /// <summary>
        /// Flat damage bonus granted to the next matching ability after the creature takes damage.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DamageTakenNextSkillAbilityDamageBonus = 774,

        /// <summary>
        /// Window in seconds for DamageTakenNextSkillAbilityDamageBonus.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        DamageTakenNextSkillAbilityWindowSeconds = 775,

        /// <summary>
        /// Flat damage bonus granted to the next matching ability after avoiding an attack.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AvoidedAttackNextSkillAbilityDamageBonus = 776,

        /// <summary>
        /// SkillType value whose costly hostile ability hits can restore Stamina.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        CostlyAbilityHitStaminaRestoreSkillType = 777,

        /// <summary>
        /// Stamina restored when a costly matching hostile ability successfully hits.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        CostlyAbilityHitStaminaRestore = 779,

        /// <summary>
        /// SkillType value whose costly abilities gain flat damage.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        CostlyAbilityDamageBonusSkillType = 780,

        /// <summary>
        /// Flat damage added to costly matching hostile abilities.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        CostlyAbilityDamageBonus = 781,

        /// <summary>
        /// FP restored when an ability grants Ranged Deflection.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive, deflectionSource: DeflectionSource.Ranged)]
        AbilityGrantedAttackDeflectionFPRestore = 782,

        /// <summary>
        /// Cooldown in seconds for AbilityGrantedAttackDeflectionFPRestore.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial, deflectionSource: DeflectionSource.Ranged)]
        AbilityGrantedAttackDeflectionFPRestoreCooldownSeconds = 783,

        /// <summary>
        /// Guard granted to one nearby ally after the creature deflects an attack.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive, deflectionSource: DeflectionSource.Melee)]
        DeflectionNearbyAllyGuard = 784,

        /// <summary>
        /// Duration in seconds for DeflectionNearbyAllyGuard.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        DeflectionNearbyAllyGuardDurationSeconds = 785,

        /// <summary>
        /// Critical damage percent adjustment applied to a matching ability after sufficient idle time.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        IdleSkillAbilityCriticalDamagePercentAdjustment = 786,

        /// <summary>
        /// Target HP percent threshold for conditional status-target damage.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        TargetLowHPStatusDamageThresholdPercent = 787,

        /// <summary>
        /// StatusEffectCategory flags required for conditional low-HP status-target damage.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        TargetLowHPStatusDamageStatusCategory = 788,

        /// <summary>
        /// Damage percent adjustment against targets matching TargetLowHPStatusDamageStatusCategory below the HP threshold.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        TargetLowHPStatusDamagePercentAdjustment = 789,

        /// <summary>
        /// StatusEffectCategory flags required for damage against statuses applied by the attacker.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        DamageToSourceAppliedStatusTargetCategory = 790,

        /// <summary>
        /// Damage percent adjustment against targets with matching statuses applied by the attacker.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DamageToSourceAppliedStatusTargetPercentAdjustment = 791,

        /// <summary>
        /// Optional SkillType value required before ability damage against source-applied statuses applies.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityDamageToSourceAppliedStatusTargetSkillType = 792,

        /// <summary>
        /// StatusEffectCategory flags required for ability damage against statuses applied by the attacker.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityDamageToSourceAppliedStatusTargetCategory = 793,

        /// <summary>
        /// Ability damage percent adjustment against targets with matching statuses applied by the attacker.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AbilityDamageToSourceAppliedStatusTargetPercentAdjustment = 794,

        /// <summary>
        /// Percent adjustment applied to final physical Defense only while a shield is equipped.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        ShieldEquippedPhysicalDefensePercentAdjustment = 795,

        /// <summary>
        /// Percent of incoming damage shared to the source of the granting status effect without consuming the status.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DamageTakenShareToStatusSourcePercent = 796,

        /// <summary>
        /// Damage bonus granted to the source's next matching skill ability when their guarded ally is hit.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        GuardedAllyHitNextSkillAbilityDamageBonus = 797,

        /// <summary>
        /// Duration in seconds for GuardedAllyHitNextSkillAbilityDamageBonus.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        GuardedAllyHitNextSkillAbilityWindowSeconds = 798,

        /// <summary>
        /// Damage bonus granted to the source's next matching skill ability when shared ward damage is triggered.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        WardSharedDamageNextSkillAbilityDamageBonus = 799,

        /// <summary>
        /// Duration in seconds for WardSharedDamageNextSkillAbilityDamageBonus.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        WardSharedDamageNextSkillAbilityWindowSeconds = 800,

        /// <summary>
        /// SkillType value whose abilities applying a matching status category receive AbilityStatusCategoryDamageBonus and AbilityStatusCategoryHitChancePercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityStatusCategoryBonusSkillType = 801,

        /// <summary>
        /// StatusEffectCategory flags required before ability status-category bonuses apply.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityStatusCategoryBonusRequiredCategory = 802,

        /// <summary>
        /// Flat damage bonus for abilities applying AbilityStatusCategoryBonusRequiredCategory.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AbilityStatusCategoryDamageBonus = 803,

        /// <summary>
        /// Hit chance percent adjustment for abilities applying AbilityStatusCategoryBonusRequiredCategory.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AbilityStatusCategoryHitChancePercentAdjustment = 804,

        /// <summary>
        /// Flat damage bonus gained for each repeated hit against the same target.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        RepeatedTargetDamageBonusPerHit = 805,

        /// <summary>
        /// Maximum flat damage bonus from RepeatedTargetDamageBonusPerHit stacks.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        RepeatedTargetDamageBonusMax = 806,

        /// <summary>
        /// Duration in seconds before repeated-target damage stacks expire.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        RepeatedTargetDamageDurationSeconds = 807,

        /// <summary>
        /// FP restoration temporarily granted while AbilityUsedAttackDeflection is active.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive, deflectionSource: DeflectionSource.Melee)]
        AbilityUsedAttackDeflectionFPRestore = 808,

        /// <summary>
        /// PerkCategoryType value whose abilities trigger AbilityUsedPerkCategoryAttackDeflection.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityUsedPerkCategoryAttackDeflectionCategoryType = 809,

        /// <summary>
        /// Temporary Melee Deflection granted after using an ability from AbilityUsedPerkCategoryAttackDeflectionCategoryType.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive, deflectionSource: DeflectionSource.Melee)]
        AbilityUsedPerkCategoryAttackDeflection = 810,

        /// <summary>
        /// Duration in seconds for AbilityUsedPerkCategoryAttackDeflection.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityUsedPerkCategoryAttackDeflectionDurationSeconds = 811,

        /// <summary>
        /// FP restoration temporarily granted while AbilityUsedPerkCategoryAttackDeflection is active.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive, deflectionSource: DeflectionSource.Melee)]
        AbilityUsedPerkCategoryAttackDeflectionFPRestore = 812,

        /// <summary>
        /// Percent chance for ability item requirements to preserve the required stim pack.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        StimPackPreserveChance = 813,

        /// <summary>
        /// SkillType value whose costly hostile abilities can apply a status rider.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        CostlyAbilityStatusSkillType = 814,

        /// <summary>
        /// Exposed duration applied when a matching costly hostile ability hits.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        CostlyAbilityExposedDurationSeconds = 815,

        /// <summary>
        /// Stamina restored to the source when applying a matching status category.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        StatusAppliedSelfStaminaRestore = 816,

        /// <summary>
        /// Hit chance percent adjustment against targets affected by Sunder.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        HitChanceAgainstSunderedTargetPercentAdjustment = 817,

        /// <summary>
        /// Critical rate percent adjustment against targets affected by Sunder.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        CriticalRateAgainstSunderedTargetPercentAdjustment = 818,

        /// <summary>
        /// SkillType value whose area abilities receive SkillAreaAbilityDamagePercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        SkillAreaAbilityDamagePercentAdjustmentSkillType = 819,

        /// <summary>
        /// Damage percent adjustment for matching area abilities.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        SkillAreaAbilityDamagePercentAdjustment = 820,

        /// <summary>
        /// Duration percent adjustment applied to outgoing control status effects.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        OutgoingControlDurationPercentAdjustment = 821,

        /// <summary>
        /// Recast delay percent adjustment for hostile abilities.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenNegative)]
        HostileAbilityRecastDelayPercentAdjustment = 822,

        /// <summary>
        /// Physical Defense percent adjustment granted to the source's active ward target.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        WardTargetPhysicalDefensePercentAdjustment = 823,

        /// <summary>
        /// Force Defense percent adjustment granted to the source's active ward target.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        WardTargetForceDefensePercentAdjustment = 824,

        /// <summary>
        /// Physical Defense percent granted to the source and active ward target after using a Ward ability.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        WardAbilityDefensePercentAdjustment = 829,

        /// <summary>
        /// Force Defense percent granted to the source and active ward target after using a Ward ability.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        WardAbilityForceDefensePercentAdjustment = 830,

        /// <summary>
        /// Duration in seconds for Ward ability defense riders.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        WardAbilityDefenseDurationSeconds = 831,

        /// <summary>
        /// Force Defense granted after taking Force damage.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        ForceDamageTakenForceDefense = 832,

        /// <summary>
        /// Duration in seconds for ForceDamageTakenForceDefense.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        ForceDamageTakenForceDefenseDurationSeconds = 833,

        /// <summary>
        /// PerkCategoryType value whose abilities trigger BeastBalancedAbilityStaminaRestore.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        BeastBalancedAbilityStaminaRestoreCategoryId = 834,

        /// <summary>
        /// PerkCategoryType value whose abilities trigger target enmity-to-source status.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityUsedPerkCategoryTargetEnmityToSourceCategoryId = 835,

        /// <summary>
        /// PerkCategoryType value whose abilities trigger nearby ally Melee Deflection.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityUsedPerkCategoryNearbyAllyAttackDeflectionCategoryId = 836,

        /// <summary>
        /// PerkCategoryType value whose abilities trigger Ward ability defense riders.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        WardAbilityDefenseCategoryId = 837,

        /// <summary>
        /// PerkCategoryType value whose abilities trigger self defensive stat riders.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityUsedPerkCategorySelfDefenseCategoryId = 838,

        /// <summary>
        /// Duration in seconds for target enmity-to-source status from matching category abilities.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityUsedPerkCategoryTargetEnmityToSourceDurationSeconds = 839,

        /// <summary>
        /// SkillType value whose next ability can receive guarded-hit status riders.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        GuardedHitNextSkillAbilityStatusSkillType = 841,

        /// <summary>
        /// Duration in seconds for nearby ally Melee Deflection from matching category abilities.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityUsedPerkCategoryNearbyAllyAttackDeflectionDurationSeconds = 843,

        /// <summary>
        /// Physical Defense granted to self after using an ability from a matching perk category.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AbilityUsedPerkCategorySelfDefensePercentAdjustment = 844,

        /// <summary>
        /// Force Defense granted to self after using an ability from a matching perk category.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AbilityUsedPerkCategorySelfForceDefensePercentAdjustment = 845,

        /// <summary>
        /// Duration in seconds for category-triggered self defensive stat riders.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityUsedPerkCategorySelfDefenseDurationSeconds = 846,

        /// <summary>
        /// Enmity percent granted to self when applying nearby ally Melee Deflection.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AbilityUsedPerkCategoryNearbyAllyAttackDeflectionSelfEnmityPercentAdjustment = 847,

        /// <summary>
        /// SkillType value whose direct damage builds same-target pressure.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        SameTargetPressureBuildSkillType = 848,

        /// <summary>
        /// Seconds of direct damage against one target required to ready same-target pressure.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        SameTargetPressureBuildSeconds = 849,

        /// <summary>
        /// Seconds allowed between matching direct damage hits while building same-target pressure.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        SameTargetPressureGraceSeconds = 850,

        /// <summary>
        /// Seconds the same-target pressure ready state remains available.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        SameTargetPressureReadyDurationSeconds = 851,

        /// <summary>
        /// Flat damage bonus for the next hostile weapon ability against the same pressure target.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        SameTargetPressureWeaponAbilityDamageBonus = 852,

        /// <summary>
        /// RecastGroup value whose active cooldown is reduced when a deflection succeeds.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial, deflectionSource: DeflectionSource.Shield)]
        DeflectionRecastReductionGroupId = 853,

        /// <summary>
        /// Seconds removed from the configured recast group when a deflection succeeds.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive, deflectionSource: DeflectionSource.Shield)]
        DeflectionRecastReductionSeconds = 854,

        /// <summary>
        /// Restricts RepeatedTargetDamage bonuses to auto-attacks when greater than zero.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        RepeatedTargetDamageAutoAttackOnly = 855,

        /// <summary>
        /// Percent damage adjustment applied to outgoing Poison damage.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        PoisonDamageDealtPercentAdjustment = 856,

        /// <summary>
        /// SkillType value whose auto-attacks apply Hamstring.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AutoAttackHamstringSkillType = 857,

        /// <summary>
        /// Duration in seconds for Hamstring applied by auto-attacks.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AutoAttackHamstringDurationSeconds = 858,

        /// <summary>
        /// Attack percent adjustment granted after using a hostile ability.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        HostileAbilityUsedAttackPercentAdjustment = 859,

        /// <summary>
        /// Duration in seconds for HostileAbilityUsedAttackPercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        HostileAbilityUsedAttackPercentAdjustmentDurationSeconds = 860,

        /// <summary>
        /// Maximum Attack percent adjustment from repeated hostile ability uses.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        HostileAbilityUsedAttackPercentAdjustmentMaximum = 861,

        /// <summary>
        /// Flat damage bonus applied to the first matching hostile ability hits in combat.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        FirstHostileAbilityHitDamageBonus = 862,

        /// <summary>
        /// Number of hostile ability hits (stacks) that receive FirstHostileAbilityHitDamageBonus before the window is exhausted.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        FirstHostileAbilityHitMaximumCount = 863,

        /// <summary>
        /// Seconds after the FirstHostileAbilityHitMaximumCount stacks are exhausted before the stacks recharge. 0 recharges only on leaving combat.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        FirstHostileAbilityHitCooldownSeconds = 886,

        /// <summary>
        /// Flat Stamina restored by the first attack in combat.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        FirstCombatAttackStaminaRestore = 864,

        /// <summary>
        /// Cooldown in seconds for FirstCombatAttackStaminaRestore.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        FirstCombatAttackStaminaRestoreCooldownSeconds = 865,

        /// <summary>
        /// StatusEffectCategory value required on a source-applied target status for ability flat damage.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        AbilityDamageToSourceAppliedStatusTargetBonusCategory = 866,

        /// <summary>
        /// Flat ability damage added against targets with a matching source-applied status.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AbilityDamageToSourceAppliedStatusTargetBonus = 867,

        /// <summary>
        /// StatusEffectCategory value required before source status stacks are applied.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        SourceStatusStackRequiredCategory = 868,

        /// <summary>
        /// StatusEffectCategory value identifying the source-owned stack effect to apply.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        SourceStatusStackAppliedCategory = 869,

        /// <summary>
        /// Maximum stack count for source-owned status stacks.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        SourceStatusStackMaximum = 870,

        /// <summary>
        /// Duration in seconds for source-owned status stacks.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        SourceStatusStackDurationSeconds = 871,

        /// <summary>
        /// SkillType value whose successful hostile ability hits grant no-delay auto-attacks.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        HostileAbilityHitNextAutoAttackNoDelaySkillType = 872,

        /// <summary>
        /// Duration in seconds for HostileAbilityHitNextAutoAttackNoDelaySkillType.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        HostileAbilityHitNextAutoAttackNoDelayDurationSeconds = 873,

        /// <summary>
        /// StatusEffectCategory value required on source-applied target statuses for auto-attack cycle damage.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        SourceStatusAutoAttackCycleRequiredCategory = 874,

        /// <summary>
        /// SkillType value whose auto-attacks count toward source-status cycle damage.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        SourceStatusAutoAttackCycleSkillType = 875,

        /// <summary>
        /// Required auto-attack count for source-status cycle damage.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        SourceStatusAutoAttackCycleRequiredCount = 876,

        /// <summary>
        /// Flat damage dealt by source-status auto-attack cycles.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        SourceStatusAutoAttackCycleDamage = 877,

        /// <summary>
        /// CombatDamageType value used by source-status auto-attack cycle damage.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        SourceStatusAutoAttackCycleDamageType = 878,

        /// <summary>
        /// StatusEffectCategory value whose application drains target Stamina.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        StatusAppliedTargetStaminaDrainRequiredCategory = 879,

        /// <summary>
        /// Flat Stamina drained when applying a matching status.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        StatusAppliedTargetStaminaDrain = 880,

        /// <summary>
        /// Cooldown in seconds for StatusAppliedTargetStaminaDrain.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        StatusAppliedTargetStaminaDrainCooldownSeconds = 881,

        /// <summary>
        /// StatusEffectCategory value that receives source-owned healing received adjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        SourceStatusHealingReceivedRequiredCategory = 882,

        /// <summary>
        /// Healing received percent adjustment from matching source-owned statuses.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenNegative)]
        SourceStatusHealingReceivedPercentAdjustment = 883,

        /// <summary>
        /// Flat direct damage added against targets with a matching status category or from stealth.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DirectDamageToStatusCategoryOrStealthBonus = 884,

        /// <summary>
        /// StatusEffectCategory value for DirectDamageToStatusCategoryOrStealthBonus.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        DirectDamageToStatusCategoryOrStealthBonusCategory = 885,

        /// <summary>
        /// Percent of incoming physical damage converted to Force damage before mitigation (Saber Ward / Perfect Aegis).
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        IncomingPhysicalToForceConversionPercent = 887,

        /// <summary>
        /// Defense percent granted per Embattled stack.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        EmbattledStackDefensePercent = 904,

        /// <summary>
        /// Force Defense percent granted per Embattled stack.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        EmbattledStackForceDefensePercent = 905,

        /// <summary>
        /// Maximum number of Embattled stacks that may be held.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        EmbattledMaxStacks = 906,

        /// <summary>
        /// Embattled stack count at or above which high-stack bonuses apply.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        EmbattledHighStackThreshold = 907,

        /// <summary>
        /// Mobility Resistance granted while at or above the Embattled high-stack threshold.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        EmbattledHighStackMobilityResistance = 908,

        /// <summary>
        /// Additional Deflecting Return reflection percent while at or above the Embattled high-stack threshold.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        EmbattledHighStackDeflectionReflectionBonusPercent = 909,

        /// <summary>
        /// Percent of a deflected ranged attack's pre-mitigation damage reflected back to its source.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        RangedDeflectionReflectionPercent = 894,

        /// <summary>
        /// Cap on Deflecting Return reflected damage, expressed as a percent of normal main-hand weapon damage.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        RangedDeflectionReflectionCapPercent = 895,

        /// <summary>
        /// Force Attack percent granted for spending at least the minimum FP on a hostile combat ability (Overpower).
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        HostileAbilityFPSpendForceAttackPercent = 896,

        /// <summary>
        /// Maximum stacked Force Attack percent from Overpower's FP-spend trigger.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        HostileAbilityFPSpendForceAttackMaxPercent = 897,

        /// <summary>
        /// Minimum FP cost required to trigger Overpower's Force Attack bonus.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        HostileAbilityFPSpendForceAttackMinFPCost = 898,

        /// <summary>
        /// Duration in seconds of Overpower's Force Attack bonus.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        HostileAbilityFPSpendForceAttackDurationSeconds = 899,

        /// <summary>
        /// FP restored when landing an auto-attack on a target afflicted by the attacker's Sunder (High Ground).
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AutoAttackSunderedTargetFPRestore = 900,

        /// <summary>
        /// Attack percent granted while current FP is below the low-FP threshold (Focus Shift).
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        LowFPAttackPercentAdjustment = 901,

        /// <summary>
        /// FP percent below which the low-FP Attack bonus applies.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        LowFPAttackThresholdPercent = 902,

        /// <summary>
        /// While positive, the wearer's hostile weapon auto-attacks deal Force damage instead of their normal type (Imbuement Stance).
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        StanceHostileAutoAttackForceConversion = 903,

        /// <summary>
        /// FP cost consumed by each hostile auto-attack while Imbuement Stance is active.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        StanceHostileAutoAttackFPCost = 916,

        /// <summary>
        /// Percent chance for a mimicked trait to inflict Bleed on damage dealt.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DamageDealtBleedChance = 910,

        /// <summary>
        /// Percent chance for a mimicked trait to inflict Freezing on damage dealt.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DamageDealtFreezingChance = 911,

        /// <summary>
        /// Percent chance for a mimicked trait to inflict Shock on damage dealt.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DamageDealtShockChance = 912,

        /// <summary>
        /// Percent chance for a mimicked trait to inflict Sunder on damage dealt.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DamageDealtSunderChance = 913,

        /// <summary>
        /// Percent chance for a mimicked trait to inflict Hemorrhage on damage dealt.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DamageDealtHemorrhageChance = 914,

        /// <summary>
        /// Percent bonus to the direct damage of mimicked techniques (combat analyzer potency).
        /// Granted by Combat Analyzer ranks, the Overclocked Analyzer capstone's Overload, and
        /// damage-type loadout set bonuses; read by the Mimicry technique impact as a damage-percent adjustment.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        MimicryPotencyPercent = 915,

        /// <summary>
        /// Percent chance for a mimicked trait to inflict Poison on damage dealt.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DamageDealtPoisonChance = 917,

        /// <summary>
        /// Percent of incoming physical damage reflected back to the attacker after mitigation.
        /// The physical counterpart to <see cref="ForceDamageReflectionPercentAdjustment"/> and
        /// <see cref="ElementalDamageReflectionPercentAdjustment"/>.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        PhysicalDamageReflectionPercentAdjustment = 918,

        /// <summary>
        /// Flat bonus to the stealth side of the opposed stealth detection check.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        Stealth = 919,

        /// <summary>
        /// Flat bonus to the detection side of the opposed stealth detection check.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        Detection = 920,

        /// <summary>
        /// Increases the effect strength of traps placed by the creature.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        TrapBonus = 921,

        /// <summary>
        /// Improves the creature's ability to disarm hostile traps.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        TrapDisarm = 922,

        /// <summary>
        /// Increases the potency of weapon poisons applied by the creature.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        PoisonBonus = 923,

        /// <summary>
        /// Improves the creature's ability to slice locks, terminals, and lockboxes.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        Lockpicking = 924,

        /// <summary>
        /// Legacy stealth-effectiveness multiplier slot retained for stable enum serialization.
        /// New stealth ranks contribute flat <see cref="Stealth"/> instead.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        StealthEffectivenessPercent = 925,

        /// <summary>
        /// Percent by which stamina drain while stealthed is slowed.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        StealthStaminaDrainReductionPercent = 926,

        /// <summary>
        /// Percent adjustment to how long weapon poison coatings applied by the creature last.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        PoisonCoatingDurationPercent = 927,

        /// <summary>
        /// Percent adjustment applied to melee weapon damage dealt while the attacker is behind the target.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        BackAttackDamagePercentAdjustment = 928,

        /// <summary>
        /// Critical rate percent adjustment applied to melee weapon attacks made while the attacker is behind the target.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        BackAttackCriticalRatePercentAdjustment = 929,

        /// <summary>
        /// Additional traps the creature may keep active at the same time beyond the base allowance.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AdditionalTrapCapacity = 930,

        /// <summary>
        /// Defense reduction percent applied as Exposed when the creature lands a back attack.
        /// Consumed on the next landed back attack.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        BackAttackExposedPercent = 931,

        /// <summary>
        /// Duration in seconds for the Exposed effect applied by BackAttackExposedPercent.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        BackAttackExposedDurationSeconds = 932,

        /// <summary>
        /// Percent reduction to the time a placed trap takes to arm.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        TrapPlacementSpeedPercent = 933,

        /// <summary>
        /// Additional metres of range at which the creature notices concealed traps.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        TrapDetectionRangeBonus = 934,

        /// <summary>
        /// Additional disguise identities the player may keep on file beyond the base allowance.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AdditionalDisguiseSlots = 935,

        /// <summary>
        /// Percent reduction to the delay between disguise activations. Deactivation is unaffected.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DisguiseSwapCooldownReductionPercent = 936,

        /// <summary>
        /// Overrides the duration in seconds of Poison inflicted by the shared damage-dealt Poison proc.
        /// A value of zero uses the proc's normal duration.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DamageDealtPoisonDurationSeconds = 937,

        /// <summary>
        /// Final override for Deflecting Return's reflected-damage percent. A positive value replaces
        /// the normal perk value and Embattled bonus instead of stacking with them.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        RangedDeflectionReflectionOverridePercent = 938,

        /// <summary>
        /// Final override for Deflecting Return's weapon-damage cap percent. A positive value replaces
        /// the normal perk cap instead of stacking with it.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        RangedDeflectionReflectionCapOverridePercent = 939,

        /// <summary>
        /// SkillType value whose activated ability damage receives SkillAbilityDamagePercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        SkillAbilityDamagePercentAdjustmentSkillType = 940,

        /// <summary>
        /// Percent adjustment to activated ability damage for the matching skill. This participates in
        /// the shared outgoing-damage cap rather than multiplying damage after the cap is applied.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        SkillAbilityDamagePercentAdjustment = 941,

        /// <summary>
        /// SkillType value whose sufficiently costly hostile abilities grant temporary Evasion.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        CostlyAbilityUsedEvasionPercentAdjustmentSkillType = 942,

        /// <summary>
        /// Minimum final Stamina cost required to grant CostlyAbilityUsedEvasionPercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        CostlyAbilityUsedEvasionMinimumStaminaCost = 943,

        /// <summary>
        /// Temporary Evasion percent granted after using a matching sufficiently costly hostile ability.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        CostlyAbilityUsedEvasionPercentAdjustment = 944,

        /// <summary>
        /// Duration in seconds of CostlyAbilityUsedEvasionPercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        CostlyAbilityUsedEvasionDurationSeconds = 945,

        /// <summary>
        /// SkillType value for an independent second guarded-hit next-ability bonus channel.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        GuardedHitSecondaryNextSkillAbilitySkillType = 946,

        /// <summary>
        /// Critical-rate adjustment stored by the secondary guarded-hit next-ability channel.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        GuardedHitSecondaryNextSkillAbilityCriticalRatePercentAdjustment = 947,

        /// <summary>
        /// Flat damage stored by the secondary guarded-hit next-ability channel.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        GuardedHitSecondaryNextSkillAbilityDamageBonus = 948,

        /// <summary>
        /// Duration in seconds of the secondary guarded-hit next-ability channel.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        GuardedHitSecondaryNextSkillAbilityWindowSeconds = 949,

        /// <summary>
        /// Radius in meters of a guarded-hit pulse around the defender. Zero limits the pulse to
        /// the original attacker.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        GuardedHitPulseRadiusMeters = 950,

        /// <summary>
        /// Percent of the guarded hit's incoming damage generated as Enmity toward each additional
        /// enemy hit by the guarded-hit pulse. The original attacker receives normal Guard Enmity.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        GuardedHitPulseEnmityPercentOfIncomingDamage = 951,

        /// <summary>
        /// Optional SkillType restriction for the independent hostile-ability Evasion channel.
        /// Invalid applies the channel to every hostile combat ability.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        HostileAbilityUsedEvasionPercentAdjustmentSkillType = 952,

        /// <summary>
        /// Temporary Evasion percent granted after using a hostile ability that passes the optional
        /// skill restriction.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        HostileAbilityUsedEvasionPercentAdjustment = 953,

        /// <summary>
        /// Duration in seconds of HostileAbilityUsedEvasionPercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        HostileAbilityUsedEvasionDurationSeconds = 954,

        /// <summary>
        /// SkillType value whose activated abilities grant temporary Evasion through an independent
        /// second footwork channel.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        SecondaryAbilityUsedEvasionPercentAdjustmentSkillType = 955,

        /// <summary>
        /// Temporary Evasion percent granted by the independent second footwork channel.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        SecondaryAbilityUsedEvasionPercentAdjustment = 956,

        /// <summary>
        /// Duration in seconds of SecondaryAbilityUsedEvasionPercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        SecondaryAbilityUsedEvasionDurationSeconds = 957,

        /// <summary>
        /// Flat Stamina cost adjustment applied to every hostile combat ability.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenNegative)]
        HostileAbilityStaminaCostFlatAdjustment = 958,

        /// <summary>
        /// Minimum final Stamina cost required for CostlyAbilityDamageBonus.
        /// Kept separate from other costly-ability riders so thresholds do not add together.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        CostlyAbilityDamageMinimumStaminaCost = 959,

        /// <summary>
        /// Minimum final Stamina cost required for CostlyAbilityHitStaminaRestore.
        /// Kept separate from other costly-ability riders so thresholds do not add together.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        CostlyAbilityHitStaminaRestoreMinimumStaminaCost = 960,

        /// <summary>
        /// Minimum final Stamina cost required for CostlyAbilityExposedDurationSeconds.
        /// Kept separate from other costly-ability riders so thresholds do not add together.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        CostlyAbilityStatusMinimumStaminaCost = 961,

        /// <summary>
        /// Movement speed percent adjustment applied only while the creature is stealthed.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        StealthMovementSpeedPercentAdjustment = 962,

        /// <summary>
        /// DMG granted to the next attack after guarding a hit, regardless of skill line.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        GuardedHitNextAttackDMGBonus = 963,

        /// <summary>
        /// Critical-rate adjustment granted to the next attack after guarding a hit, regardless of skill line.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        GuardedHitNextAttackCriticalRatePercentAdjustment = 964,

        /// <summary>
        /// Duration in seconds of the cross-skill guarded-hit next-attack bonus.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        GuardedHitNextAttackWindowSeconds = 965,

        /// <summary>
        /// Internal temporary DMG consumed by the next hostile ability or landed auto attack.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        NextAttackGuardedHitDMGBonus = 966,

        /// <summary>
        /// Internal temporary critical-rate adjustment consumed by the next hostile ability or
        /// landed auto attack.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        NextAttackGuardedHitCriticalRatePercentAdjustment = 967,

        /// <summary>
        /// DMG granted by an independent second cross-skill guarded-hit next-attack channel.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        GuardedHitSecondaryNextAttackDMGBonus = 968,

        /// <summary>
        /// Flat Enmity granted by the independent second cross-skill guarded-hit next-attack channel.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        GuardedHitSecondaryNextAttackEnmityBonus = 969,

        /// <summary>
        /// Duration in seconds of the independent second cross-skill guarded-hit next-attack channel.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        GuardedHitSecondaryNextAttackWindowSeconds = 970,

        /// <summary>
        /// Internal temporary flat Enmity consumed by the next hostile ability or landed auto attack.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        NextAttackGuardedHitEnmityBonus = 971,

        /// <summary>
        /// Number of melee auto-attacks required before the cross-skill melee cycle deals bonus damage.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        MeleeAutoAttackCycleRequiredCount = 972,

        /// <summary>
        /// Flat DMG added by the cross-skill melee auto-attack cycle.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        MeleeAutoAttackCycleDamage = 973,

        /// <summary>
        /// Flat DMG gained per consecutive melee auto-attack against the same target.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        MeleeRepeatedTargetDamageBonusPerHit = 974,

        /// <summary>
        /// Maximum flat DMG from consecutive melee auto-attacks against the same target.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        MeleeRepeatedTargetDamageBonusMax = 975,

        /// <summary>
        /// Effect icon displayed while the consecutive melee auto-attack bonus is active.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        MeleeRepeatedTargetDamageStatusEffectIcon = 976,

        /// <summary>
        /// Flat DMG granted to the next hostile ability or landed auto attack after applying a matching status.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        StatusAppliedNextAttackDamageBonus = 977,

        /// <summary>
        /// Duration in seconds of the status-applied next-attack bonus.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        StatusAppliedNextAttackWindowSeconds = 978,

        /// <summary>
        /// Internal temporary DMG consumed by the next hostile ability or landed auto attack
        /// after applying a matching status.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        NextAttackStatusAppliedDMGBonus = 979,

        /// <summary>
        /// SkillType required for the skill-damage bleeding-target Stamina restore channel.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        SkillDamageBleedingTargetStaminaRestoreSkillType = 980,

        /// <summary>
        /// Percent chance for matching skill damage to restore Stamina against a bleeding target.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        SkillDamageBleedingTargetStaminaRestoreChance = 981,

        /// <summary>
        /// Flat Stamina restored by matching skill damage against a bleeding target.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        SkillDamageBleedingTargetStaminaRestore = 982,

        /// <summary>
        /// Cooldown in seconds for the skill-damage bleeding-target Stamina restore channel.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        SkillDamageBleedingTargetStaminaRestoreCooldownSeconds = 983,

        /// <summary>
        /// SkillType required for the skill-ability bleeding-target Stamina restore channel.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        SkillAbilityBleedingTargetStaminaRestoreSkillType = 984,

        /// <summary>
        /// Percent chance for a matching skill ability to restore Stamina against a bleeding target.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        SkillAbilityBleedingTargetStaminaRestoreChance = 985,

        /// <summary>
        /// Flat Stamina restored by a matching skill ability against a bleeding target.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        SkillAbilityBleedingTargetStaminaRestore = 986,

        /// <summary>
        /// Cooldown in seconds for the skill-ability bleeding-target Stamina restore channel.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        SkillAbilityBleedingTargetStaminaRestoreCooldownSeconds = 987,

        /// <summary>
        /// Percent critical-rate adjustment applied to ranged weapon attacks and abilities.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        RangedCriticalRatePercentAdjustment = 988,

        /// <summary>
        /// Internal temporary partner to NextAttackNoDelay: the percent (1-99) the armed next
        /// matching ability's activation delay is reduced by. Absent means the armed buff removes
        /// the delay entirely.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        NextAttackDelayReductionPercent = 989,

        /// <summary>
        /// Flat DMG gained per consecutive ranged hit against the same target. Any ranged weapon
        /// skill builds and benefits from the stacks.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        RangedRepeatedTargetDamageBonusPerHit = 990,

        /// <summary>
        /// Maximum flat DMG from consecutive ranged hits against the same target.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        RangedRepeatedTargetDamageBonusMax = 991,

        /// <summary>
        /// Seconds without a qualifying ranged hit before the consecutive-hit stacks expire.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        RangedRepeatedTargetDamageDurationSeconds = 992,

        /// <summary>
        /// When enabled, landing any hostile ability grants minimum delay to the next auto-attack,
        /// regardless of the weapon skill used for that attack.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        HostileAbilityHitNextAutoAttackNoDelayAllSkills = 993,

        /// <summary>
        /// Internal temporary flag that causes the next auto-attack from any weapon skill to use
        /// the default minimum delay, then is consumed.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        NextAutoAttackNoDelayAllSkills = 994,

        /// <summary>
        /// Flat percent chance to deflect a hostile ranged weapon auto-attack while wielding a weapon and no shield.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive, deflectionSource: DeflectionSource.Ranged)]
        RangedDeflection = 995,

        /// <summary>
        /// Flat bonus added to the default Ranged Deflection chance cap.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive, deflectionSource: DeflectionSource.Ranged)]
        RangedDeflectionChanceCap = 996,

        /// <summary>
        /// Flat Stamina restored when the creature successfully uses Shield Deflection.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive, deflectionSource: DeflectionSource.Shield)]
        ShieldDeflectionStaminaRestore = 997,

        /// <summary>
        /// Cooldown in seconds for ShieldDeflectionStaminaRestore.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial, deflectionSource: DeflectionSource.Shield)]
        ShieldDeflectionStaminaRestoreCooldownSeconds = 998,

        /// <summary>
        /// Flat FP restored when the creature successfully uses Melee Deflection.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive, deflectionSource: DeflectionSource.Melee)]
        MeleeDeflectionFPRestore = 999,

        /// <summary>
        /// Cooldown in seconds for MeleeDeflectionFPRestore.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial, deflectionSource: DeflectionSource.Melee)]
        MeleeDeflectionFPRestoreCooldownSeconds = 1000,

        /// <summary>
        /// Percent hostile ability damage bonus granted while FP and Stamina are both above
        /// HighFPAndStaminaAbilityDamagePercentAdjustmentThresholdPercent.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        HighFPAndStaminaAbilityDamagePercentAdjustment = 1001,

        /// <summary>
        /// Percent of maximum FP and Stamina both required to enable
        /// HighFPAndStaminaAbilityDamagePercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial, StatTypeAggregation.Maximum)]
        HighFPAndStaminaAbilityDamagePercentAdjustmentThresholdPercent = 1002,

        /// <summary>
        /// Duration in seconds for RestoredFPForceAttackPercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        RestoredFPForceAttackDurationSeconds = 1003,

        /// <summary>
        /// Attack percent granted whenever Stamina is actually restored.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        RestoredStaminaAttackPercentAdjustment = 1004,

        /// <summary>
        /// Duration in seconds for RestoredStaminaAttackPercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        RestoredStaminaAttackDurationSeconds = 1005,

    }

    public class StatTypeAttribute : Attribute
    {
        public StatTypeCategory Category { get; }
        public StatTypeAggregation Aggregation { get; }
        public DeflectionSource DeflectionSource { get; }

        public StatTypeAttribute(
            StatTypeCategory category,
            StatTypeAggregation aggregation = StatTypeAggregation.Additive,
            DeflectionSource deflectionSource = DeflectionSource.None)
        {
            Category = category;
            Aggregation = aggregation;
            DeflectionSource = deflectionSource;
        }
    }

    public enum StatTypeAggregation
    {
        Additive = 0,
        BitwiseOr = 1,
        Maximum = 2
    }
}
