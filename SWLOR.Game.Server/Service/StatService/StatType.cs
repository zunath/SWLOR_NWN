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
        /// Flat percent chance to deflect an attack while wielding a weapon and no shield.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AttackDeflection = 7,

        /// <summary>
        /// Flat Stamina restored when the creature successfully deflects an attack.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DeflectionStaminaRestore = 8,

        /// <summary>
        /// Flat FP restored when the creature successfully deflects an attack.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DeflectionFPRestore = 9,

        /// <summary>
        /// Percent of maximum Stamina restored when the creature successfully deflects an attack.
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
        /// Percent reduction to attack delay. Total reduction is capped by combat delay logic.
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
        /// Percent reduction to ability recast delay. Recast service caps the effective reduction.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AbilityRecastReductionPercent = 35,

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
        /// Temporary percent Evasion adjustment applied after a successful ranged attack deflection.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DeflectionEvasionPercentAdjustment = 47,

        /// <summary>
        /// Temporary percent Enmity adjustment paired with the deflection Evasion effect.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        DeflectionEvasionEnmityPercentAdjustment = 48,

        /// <summary>
        /// Temporary percent Enmity adjustment applied after a successful ranged attack deflection.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DeflectionEnmityPercentAdjustment = 49,

        /// <summary>
        /// Temporary percent Physical Defense adjustment applied after a successful ranged attack deflection.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DeflectionDefensePercentAdjustment = 50,

        /// <summary>
        /// Temporary percent Force Defense adjustment applied after a successful ranged attack deflection.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
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
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        CriticalTargetDefensePercentAdjustment = 83,

        /// <summary>
        /// Duration in seconds for CriticalTargetDefensePercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
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
        /// Legacy stat retained for migration compatibility. No gameplay reads this value.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DeprecatedLowHPTemporaryHPTrigger = 102,

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
        /// Percent damage adjustment against nearby targets whose current attack target is not the attacker.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DamageToNearbyNonTargetingTargetPercentAdjustment = 106,

        /// <summary>
        /// Flat FP restored by auto-attacks, subject to AutoAttackFPRestoreCooldownSeconds.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AutoAttackFPRestore = 107,

        /// <summary>
        /// Cooldown in seconds for AutoAttackFPRestore.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
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
        /// RecastGroup id reduced when the creature defeats an enemy.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        DefeatedEnemyRecastReductionGroup = 115,

        /// <summary>
        /// Seconds removed from DefeatedEnemyRecastReductionGroup when the creature defeats an enemy.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        DefeatedEnemyRecastReductionSeconds = 116,

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
        /// Flat damage bonus granted to the next auto-attack after a pistol ability is used.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        PistolAbilityUsedNextAutoAttackDamageBonus = 119,

        /// <summary>
        /// Duration in seconds for PistolAbilityUsedNextAutoAttackDamageBonus to remain available.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        PistolAbilityUsedNextAutoAttackDamageDurationSeconds = 120,

        /// <summary>
        /// Flat damage bonus granted to the next auto-attack after a throwing ability is used.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        ThrowingAbilityUsedNextAutoAttackDamageBonus = 121,

        /// <summary>
        /// Duration in seconds for ThrowingAbilityUsedNextAutoAttackDamageBonus to remain available.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        ThrowingAbilityUsedNextAutoAttackDamageDurationSeconds = 122,

        /// <summary>
        /// Temporary percent Evasion adjustment applied after a pistol ability is used.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        PistolAbilityUsedEvasionPercentAdjustment = 123,

        /// <summary>
        /// Duration in seconds for PistolAbilityUsedEvasionPercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        PistolAbilityUsedEvasionDurationSeconds = 124,

        /// <summary>
        /// Temporary percent Evasion adjustment applied after a twin blade ability is used.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        TwinBladeAbilityUsedEvasionPercentAdjustment = 125,

        /// <summary>
        /// Duration in seconds for TwinBladeAbilityUsedEvasionPercentAdjustment.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        TwinBladeAbilityUsedEvasionDurationSeconds = 126,

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
        /// Minimum number of impacted targets required for a saberstaff area ability to restore resources.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        SaberstaffAreaAbilityMinTargetsResourceRestoreThreshold = 136,

        /// <summary>
        /// Flat FP restored when a saberstaff area ability meets the resource restore target threshold.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        SaberstaffAreaAbilityFPRestore = 137,

        /// <summary>
        /// Flat Stamina restored when a saberstaff area ability meets the resource restore target threshold.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        SaberstaffAreaAbilityStaminaRestore = 138,

        /// <summary>
        /// Cooldown in seconds for saberstaff area ability resource restoration.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        SaberstaffAreaAbilityResourceRestoreCooldownSeconds = 139,

        /// <summary>
        /// Minimum number of impacted targets required for a saberstaff area ability to apply temporary buffs.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        SaberstaffAreaAbilityMinTargetsBuffThreshold = 140,

        /// <summary>
        /// Temporary percent attack delay reduction applied when a saberstaff area ability meets the buff threshold.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        SaberstaffAreaAbilityHastePercentAdjustment = 141,

        /// <summary>
        /// Temporary flat attack deflection chance applied when a saberstaff area ability meets the buff threshold.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        SaberstaffAreaAbilityAttackDeflection = 142,

        /// <summary>
        /// Duration in seconds for saberstaff area ability haste and attack deflection buffs.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        SaberstaffAreaAbilityBuffDurationSeconds = 143,

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
        /// Legacy cooldown stat for twin blade area ability Stamina restoration. Prefer TwinBladeAreaAbilityCooldownStaminaRestoreCooldownSeconds for current combat hooks.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        TwinBladeAreaAbilityStaminaRestoreCooldownSeconds = 153,

        /// <summary>
        /// Temporary flat attack deflection chance applied after a twin blade single-target ability is used.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        TwinBladeSingleTargetAbilityAttackDeflection = 154,

        /// <summary>
        /// Duration in seconds for TwinBladeSingleTargetAbilityAttackDeflection.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        TwinBladeSingleTargetAbilityAttackDeflectionDurationSeconds = 155,

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
        /// PerkType value for the ability that can consume the deflection-triggered next-ability damage bonus.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        DeflectionNextAbilityDamageBonusPerkType = 171,

        /// <summary>
        /// Flat damage added to the next matching ability after the deflection trigger succeeds.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DeflectionNextAbilityDamageBonus = 172,

        /// <summary>
        /// Duration in seconds for DeflectionNextAbilityDamageBonus.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        DeflectionNextAbilityDamageBonusDurationSeconds = 173,

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
        /// Base seconds added to Vanguard Command shout durations before Social scaling.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        VanguardCommandDurationBonusBaseSeconds = 195,

        /// <summary>
        /// Maximum seconds added to Vanguard Command shout durations after Social scaling.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        VanguardCommandDurationBonusMaximumSeconds = 196,

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
        /// Grenade radius bonus in tenths of a meter.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        GrenadeRadiusBonusTenths = 201,

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
        /// Temporary Attack Deflection granted after a Light Guardian power is used.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        LightGuardianPowerAttackDeflection = 211,

        /// <summary>
        /// Duration in seconds for LightGuardianPowerAttackDeflection.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        LightGuardianPowerAttackDeflectionDurationSeconds = 212,

        /// <summary>
        /// Percent adjustment applied to Med Kit, Kolto Mist, Emergency Triage, and Infusion healing.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        FirstAidMedicalHealingPercentAdjustment = 213,

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
        /// PerkType id for the next specific ability modifier granted after guarding a hit.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        GuardedHitNextMatchingAbilityPerkType = 219,

        /// <summary>
        /// Flat damage bonus for the next matching perk ability after guarding a hit.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        GuardedHitNextMatchingAbilityDamageBonus = 220,

        /// <summary>
        /// Flat stamina cost adjustment for the next matching perk ability after guarding a hit.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenNegative)]
        GuardedHitNextMatchingAbilityStaminaCostAdjustment = 221,

        /// <summary>
        /// Duration in seconds for guarded-hit next matching perk ability modifiers.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        GuardedHitNextMatchingAbilityWindowSeconds = 222,

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
        /// Maximum Attack Deflection chance allowed before temporary, explicit override effects.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        AttackDeflectionChanceCap = 227,

        /// <summary>
        /// Flat percent chance to deflect an attack while equipped with a shield.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
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
        /// Flat damage dealt back to the attacker when the creature successfully guards a hit.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        GuardRetaliationDamage = 232,

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
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DeflectionNextSkillAbilityDamageBonus = 238,

        /// <summary>
        /// Critical chance bonus for the next matching skill ability after deflecting an attack.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
        DeflectionNextSkillAbilityCriticalRatePercentAdjustment = 239,

        /// <summary>
        /// Non-zero when deflection grants no activation delay to the next matching skill ability.
        /// </summary>
        [StatType(StatTypeCategory.BeneficialWhenPositive)]
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
        /// SkillType id required before a critical hit grants no activation delay to the next ability.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        CriticalNextAbilityNoDelayTriggerSkillType = 244,

        /// <summary>
        /// SkillType id that receives no activation delay after the critical-hit trigger.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        CriticalNextAbilityNoDelaySkillType = 245,

        /// <summary>
        /// Duration in seconds for CriticalNextAbilityNoDelaySkillType.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        CriticalNextAbilityNoDelayDurationSeconds = 246,

        /// <summary>
        /// Cooldown in seconds for the critical-hit no-delay trigger.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        CriticalNextAbilityNoDelayCooldownSeconds = 247,

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
        /// PerkType value whose ability can apply Knockdown when it critically hits.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        CriticalAbilityKnockdownPerkType = 281,

        /// <summary>
        /// Duration in seconds for CriticalAbilityKnockdownPerkType.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        CriticalAbilityKnockdownDurationSeconds = 282,

        /// <summary>
        /// Duration in seconds for Bleed applied by Explosive Toss abilities.
        /// </summary>
        [StatType(StatTypeCategory.NonBeneficial)]
        ExplosiveTossBleedDurationSeconds = 283,

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
        PhysicalDamageDealtHPPercentRestore = 373
    }

    public class StatTypeAttribute : Attribute
    {
        public StatTypeCategory Category { get; }

        public StatTypeAttribute(StatTypeCategory category)
        {
            Category = category;
        }
    }
}
