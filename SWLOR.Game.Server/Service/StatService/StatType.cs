namespace SWLOR.Game.Server.Service.StatService
{
    public enum StatType
    {
        /// <summary>
        /// No stat. Used as a sentinel when no valid stat type applies.
        /// </summary>
        Invalid = 0,

        /// <summary>
        /// Percent adjustment applied to final Attack. Positive values increase Attack; negative values reduce it.
        /// </summary>
        AttackPercentAdjustment = 1,

        /// <summary>
        /// Percent adjustment applied to all final Defense calculations before damage-type-specific adjustments.
        /// </summary>
        DefensePercentAdjustment = 2,

        /// <summary>
        /// Percent adjustment applied to final Defense when defending against physical damage.
        /// </summary>
        PhysicalDefensePercentAdjustment = 3,

        /// <summary>
        /// Percent adjustment applied to final Defense when defending against Force damage.
        /// </summary>
        ForceDefensePercentAdjustment = 4,

        /// <summary>
        /// Percent adjustment applied to final Accuracy.
        /// </summary>
        AccuracyPercentAdjustment = 5,

        /// <summary>
        /// Percent adjustment applied to final Evasion.
        /// </summary>
        EvasionPercentAdjustment = 6,

        /// <summary>
        /// Flat percent chance added to ranged attack deflection while wielding a valid deflecting weapon.
        /// </summary>
        AttackDeflection = 7,

        /// <summary>
        /// Flat Stamina restored when the creature successfully deflects a ranged attack.
        /// </summary>
        DeflectionStaminaRestore = 8,

        /// <summary>
        /// Flat FP restored when the creature successfully deflects a ranged attack.
        /// </summary>
        DeflectionFPRestore = 9,

        /// <summary>
        /// Percent of maximum Stamina restored when the creature successfully deflects a ranged attack.
        /// </summary>
        DeflectionStaminaRestorePercent = 10,

        /// <summary>
        /// Percent adjustment applied to FP costs. Positive values increase cost; negative values reduce it.
        /// </summary>
        FPCostPercentAdjustment = 11,

        /// <summary>
        /// Flat adjustment applied to FP costs after percent adjustment.
        /// </summary>
        FPCostFlatAdjustment = 12,

        /// <summary>
        /// Flat bonus added to Attack before percent adjustments.
        /// </summary>
        Attack = 13,

        /// <summary>
        /// Flat bonus added to all Defense calculations before damage-type-specific bonuses and percent adjustments.
        /// </summary>
        Defense = 14,

        /// <summary>
        /// Flat bonus added to Defense against physical damage.
        /// </summary>
        PhysicalDefense = 15,

        /// <summary>
        /// Flat bonus added to Defense against Force damage.
        /// </summary>
        ForceDefense = 16,

        /// <summary>
        /// Flat bonus added to Accuracy before percent adjustments.
        /// </summary>
        Accuracy = 17,

        /// <summary>
        /// Flat bonus added to Evasion before percent adjustments.
        /// </summary>
        Evasion = 18,

        /// <summary>
        /// Flat bonus added to maximum FP.
        /// </summary>
        MaxFP = 19,

        /// <summary>
        /// Flat bonus added to maximum Stamina.
        /// </summary>
        MaxStamina = 20,

        /// <summary>
        /// Flat bonus added to Defense against fire damage.
        /// </summary>
        FireDefense = 21,

        /// <summary>
        /// Flat bonus added to Defense against poison damage.
        /// </summary>
        PoisonDefense = 22,

        /// <summary>
        /// Flat bonus added to Defense against electrical damage.
        /// </summary>
        ElectricalDefense = 23,

        /// <summary>
        /// Flat bonus added to Defense against ice damage.
        /// </summary>
        IceDefense = 24,

        /// <summary>
        /// Percent adjustment applied to incoming damage. Positive values increase damage taken; negative values reduce it.
        /// </summary>
        DamageTakenPercentAdjustment = 25,

        /// <summary>
        /// Flat adjustment applied to incoming damage after percent adjustment.
        /// </summary>
        DamageTakenFlatAdjustment = 26,

        /// <summary>
        /// Percent reduction to attack delay. Total reduction is capped by combat delay logic.
        /// </summary>
        AttackDelayReductionPercent = 27,

        /// <summary>
        /// Flat seconds added to ability activation delay. Negative values reduce activation time.
        /// </summary>
        ActivationDelayFlatAdjustment = 28,

        /// <summary>
        /// Percent adjustment to earned skill experience.
        /// </summary>
        ExperiencePercentAdjustment = 29,

        /// <summary>
        /// Temporary bonus language rank used when checking whether a listener understands speech.
        /// </summary>
        LanguageComprehension = 30,

        /// <summary>
        /// Flat HP regenerated by effects that read HP regeneration stats.
        /// </summary>
        HPRegen = 31,

        /// <summary>
        /// Flat FP regenerated by effects that read FP regeneration stats.
        /// </summary>
        FPRegen = 32,

        /// <summary>
        /// Flat Stamina regenerated by effects that read Stamina regeneration stats.
        /// </summary>
        StaminaRegen = 33,

        /// <summary>
        /// Flat regeneration bonus applied during rest regeneration effects.
        /// </summary>
        RestRegen = 34,

        /// <summary>
        /// Percent reduction to ability recast delay. Recast service caps the effective reduction.
        /// </summary>
        AbilityRecastReductionPercent = 35,

        /// <summary>
        /// Percent adjustment applied to creature movement speed.
        /// </summary>
        MovementSpeedPercentAdjustment = 36,

        /// <summary>
        /// If greater than zero, piloting module effectiveness can use Willpower instead of Perception when Willpower is higher.
        /// </summary>
        UseWillpowerForPilotingModuleEffectiveness = 37,

        /// <summary>
        /// Percent-point adjustment added to SWLOR critical hit chance.
        /// </summary>
        CriticalRatePercentAdjustment = 38,

        /// <summary>
        /// Staff-only override for the ability score used to calculate weapon damage. Stores AbilityType as value plus one.
        /// </summary>
        StaffDamageAbilityOverride = 39,

        /// <summary>
        /// Staff-only override for the ability score used to calculate weapon accuracy. Stores AbilityType as value plus one.
        /// </summary>
        StaffAccuracyAbilityOverride = 40,

        /// <summary>
        /// Multiplier for the attacker's positive Might modifier added as staff damage bonus.
        /// </summary>
        StaffMightModifierDamageMultiplier = 41,

        /// <summary>
        /// Flat bonus added to Will saving throws.
        /// </summary>
        WillSavingThrow = 42,

        /// <summary>
        /// Flat bonus added to Fortitude saving throws.
        /// </summary>
        FortitudeSavingThrow = 43,

        /// <summary>
        /// Flat bonus added to Reflex saving throws.
        /// </summary>
        ReflexSavingThrow = 44,

        /// <summary>
        /// Percent adjustment applied to damage after a successful SWLOR critical hit.
        /// </summary>
        CriticalDamagePercentAdjustment = 45,

        /// <summary>
        /// Percent adjustment applied to generated enmity.
        /// </summary>
        EnmityPercentAdjustment = 46,

        /// <summary>
        /// Temporary percent Evasion adjustment applied after a successful ranged attack deflection.
        /// </summary>
        DeflectionEvasionPercentAdjustment = 47,

        /// <summary>
        /// Temporary percent Enmity adjustment paired with the deflection Evasion effect.
        /// </summary>
        DeflectionEvasionEnmityPercentAdjustment = 48,

        /// <summary>
        /// Temporary percent Enmity adjustment applied after a successful ranged attack deflection.
        /// </summary>
        DeflectionEnmityPercentAdjustment = 49,

        /// <summary>
        /// Temporary percent Physical Defense adjustment applied after a successful ranged attack deflection.
        /// </summary>
        DeflectionDefensePercentAdjustment = 50,

        /// <summary>
        /// Temporary percent Force Defense adjustment applied after a successful ranged attack deflection.
        /// </summary>
        DeflectionForceDefensePercentAdjustment = 51,

        /// <summary>
        /// Target current HP threshold percent required for low-HP bonus damage to apply.
        /// </summary>
        TargetLowHPDamageThresholdPercent = 52,

        /// <summary>
        /// Percent damage adjustment applied when damaging a target at or below the low-HP threshold.
        /// </summary>
        TargetLowHPDamagePercentAdjustment = 53,

        /// <summary>
        /// Percent chance for an auto-attack to add AutoAttackDamageBonus damage.
        /// </summary>
        AutoAttackDamageBonusChance = 54,

        /// <summary>
        /// Flat damage added to an auto-attack when AutoAttackDamageBonusChance succeeds.
        /// </summary>
        AutoAttackDamageBonus = 55,

        /// <summary>
        /// Flat Stamina restored after a critical hit, subject to CriticalStaminaRestoreCooldownSeconds.
        /// </summary>
        CriticalStaminaRestore = 56,

        /// <summary>
        /// Cooldown in seconds for CriticalStaminaRestore.
        /// </summary>
        CriticalStaminaRestoreCooldownSeconds = 57,

        /// <summary>
        /// Percent of critical-hit damage removed from the target's FP.
        /// </summary>
        CriticalTargetFPLossPercentOfDamage = 58,

        /// <summary>
        /// Percent damage adjustment against targets affected by Sunder.
        /// </summary>
        DamageToSunderedTargetPercentAdjustment = 59,

        /// <summary>
        /// Percent damage adjustment against targets affected by Bleed or Hemorrhage.
        /// </summary>
        DamageToBleedingTargetPercentAdjustment = 60,

        /// <summary>
        /// Percent damage adjustment against targets with any status effect categorized as a debuff.
        /// </summary>
        DamageToDebuffedTargetPercentAdjustment = 61,

        /// <summary>
        /// Percent damage adjustment against targets affected by Poison, Toxin, or Disoriented.
        /// </summary>
        DamageToPoisonedOrDisorientedTargetPercentAdjustment = 62,

        /// <summary>
        /// Percent damage adjustment against targets affected by Weakened or Hamstring.
        /// </summary>
        DamageToWeakenedOrHamstringTargetPercentAdjustment = 63,

        /// <summary>
        /// Percent damage adjustment against targets with any status effect categorized as control.
        /// </summary>
        DamageToControlTargetPercentAdjustment = 64,

        /// <summary>
        /// Percent damage adjustment against targets affected by Disoriented or Dazed.
        /// </summary>
        DamageToDisorientedDazedTargetPercentAdjustment = 65,

        /// <summary>
        /// Percent ability damage adjustment against targets affected by Knockdown or Blind.
        /// </summary>
        AbilityDamageToKnockdownOrBlindTargetPercentAdjustment = 66,

        /// <summary>
        /// Flat Stamina restored when the creature defeats an enemy.
        /// </summary>
        DefeatedEnemyStaminaRestore = 67,

        /// <summary>
        /// Percent of maximum HP restored when the creature defeats an enemy.
        /// </summary>
        DefeatedEnemyHPPercentRestore = 68,

        /// <summary>
        /// Temporary percent Attack adjustment applied when the creature defeats an enemy.
        /// </summary>
        DefeatedEnemyAttackPercentAdjustment = 69,

        /// <summary>
        /// Duration in seconds for DefeatedEnemyAttackPercentAdjustment.
        /// </summary>
        DefeatedEnemyAttackDurationSeconds = 70,

        /// <summary>
        /// Temporary percent attack delay reduction applied when the creature defeats an enemy.
        /// </summary>
        DefeatedEnemyAttackDelayReductionPercent = 71,

        /// <summary>
        /// Duration in seconds for DefeatedEnemyAttackDelayReductionPercent.
        /// </summary>
        DefeatedEnemyAttackDelayReductionDurationSeconds = 72,

        /// <summary>
        /// Temporary percent Physical Defense adjustment applied to nearby party members when the creature defeats an enemy.
        /// </summary>
        DefeatedEnemyNearbyAllyPhysicalDefensePercentAdjustment = 73,

        /// <summary>
        /// Duration in seconds for DefeatedEnemyNearbyAllyPhysicalDefensePercentAdjustment.
        /// </summary>
        DefeatedEnemyNearbyAllyPhysicalDefenseDurationSeconds = 74,

        /// <summary>
        /// Percent of damage dealt restored to the attacker as HP.
        /// </summary>
        DamageDealtHPPercentRestore = 75,

        /// <summary>
        /// Percent of critical-hit damage restored to the attacker as HP.
        /// </summary>
        CriticalHPPercentOfDamageRestore = 76,

        /// <summary>
        /// Percent chance for an auto-attack to restore AutoAttackStaminaRestore Stamina.
        /// </summary>
        AutoAttackStaminaRestoreChance = 77,

        /// <summary>
        /// Flat Stamina restored when AutoAttackStaminaRestoreChance succeeds.
        /// </summary>
        AutoAttackStaminaRestore = 78,

        /// <summary>
        /// Temporary percent Accuracy adjustment applied to the attacker after a critical hit.
        /// </summary>
        CriticalAccuracyPercentAdjustment = 79,

        /// <summary>
        /// Duration in seconds for CriticalAccuracyPercentAdjustment.
        /// </summary>
        CriticalAccuracyDurationSeconds = 80,

        /// <summary>
        /// Temporary percent Evasion adjustment applied to the target after the attacker lands a critical hit.
        /// </summary>
        CriticalTargetEvasionPercentAdjustment = 81,

        /// <summary>
        /// Duration in seconds for CriticalTargetEvasionPercentAdjustment.
        /// </summary>
        CriticalTargetEvasionDurationSeconds = 82,

        /// <summary>
        /// Percent Defense adjustment applied to the target through Exposed after the attacker lands a critical hit.
        /// </summary>
        CriticalTargetDefensePercentAdjustment = 83,

        /// <summary>
        /// Duration in seconds for CriticalTargetDefensePercentAdjustment.
        /// </summary>
        CriticalTargetDefenseDurationSeconds = 84,

        /// <summary>
        /// Percent chance for an auto-attack to apply AutoAttackTargetAccuracyPercentAdjustment to the target.
        /// </summary>
        AutoAttackTargetAccuracyPercentAdjustmentChance = 85,

        /// <summary>
        /// Temporary percent Accuracy adjustment applied to the target when the auto-attack accuracy trigger succeeds.
        /// </summary>
        AutoAttackTargetAccuracyPercentAdjustment = 86,

        /// <summary>
        /// Duration in seconds for AutoAttackTargetAccuracyPercentAdjustment.
        /// </summary>
        AutoAttackTargetAccuracyPercentAdjustmentDurationSeconds = 87,

        /// <summary>
        /// Temporary percent Attack adjustment applied to a defender after taking damage.
        /// </summary>
        DamageTakenAttackPercentAdjustment = 88,

        /// <summary>
        /// Duration in seconds for DamageTakenAttackPercentAdjustment.
        /// </summary>
        DamageTakenAttackDurationSeconds = 89,

        /// <summary>
        /// HP threshold percent that must be crossed before LowHPPhysicalDefensePercentAdjustment can trigger.
        /// </summary>
        LowHPPhysicalDefenseThresholdPercent = 90,

        /// <summary>
        /// Temporary percent Physical Defense adjustment applied when the low-HP physical defense trigger fires.
        /// </summary>
        LowHPPhysicalDefensePercentAdjustment = 91,

        /// <summary>
        /// Duration in seconds for LowHPPhysicalDefensePercentAdjustment.
        /// </summary>
        LowHPPhysicalDefenseDurationSeconds = 92,

        /// <summary>
        /// Cooldown in seconds for the low-HP physical defense trigger.
        /// </summary>
        LowHPPhysicalDefenseCooldownSeconds = 93,

        /// <summary>
        /// HP threshold percent that must be crossed before LowHPEvasionPercentAdjustment can trigger.
        /// </summary>
        LowHPEvasionThresholdPercent = 94,

        /// <summary>
        /// Temporary percent Evasion adjustment applied when the low-HP evasion trigger fires.
        /// </summary>
        LowHPEvasionPercentAdjustment = 95,

        /// <summary>
        /// Duration in seconds for LowHPEvasionPercentAdjustment.
        /// </summary>
        LowHPEvasionDurationSeconds = 96,

        /// <summary>
        /// Cooldown in seconds for the low-HP evasion trigger.
        /// </summary>
        LowHPEvasionCooldownSeconds = 97,

        /// <summary>
        /// HP threshold percent that must be crossed before temporary HP with a Fortitude save can trigger.
        /// </summary>
        LowHPTemporaryHPThresholdPercent = 98,

        /// <summary>
        /// Percent of maximum HP granted as temporary HP when the low-HP Fortitude-save trigger succeeds.
        /// </summary>
        LowHPTemporaryHPPercent = 99,

        /// <summary>
        /// Duration in seconds for LowHPTemporaryHPPercent temporary HP.
        /// </summary>
        LowHPTemporaryHPDurationSeconds = 100,

        /// <summary>
        /// Cooldown in seconds for the low-HP Fortitude-save temporary HP trigger.
        /// </summary>
        LowHPTemporaryHPCooldownSeconds = 101,

        /// <summary>
        /// Fortitude save DC required for the low-HP temporary HP trigger. A value of zero or less skips the save.
        /// </summary>
        LowHPTemporaryHPFortitudeSaveDC = 102,

        /// <summary>
        /// Flat Stamina restored on critical hits against poisoned targets.
        /// </summary>
        CriticalPoisonedTargetStaminaRestore = 103,

        /// <summary>
        /// Percent chance to add DamageToPoisonedTargetFlatBonus damage against poisoned targets.
        /// </summary>
        DamageToPoisonedTargetFlatBonusChance = 104,

        /// <summary>
        /// Flat damage added against poisoned targets when DamageToPoisonedTargetFlatBonusChance succeeds.
        /// </summary>
        DamageToPoisonedTargetFlatBonus = 105,

        /// <summary>
        /// Percent damage adjustment against nearby targets whose current attack target is not the attacker.
        /// </summary>
        DamageToNearbyNonTargetingTargetPercentAdjustment = 106,

        /// <summary>
        /// Flat FP restored by auto-attacks, subject to AutoAttackFPRestoreCooldownSeconds.
        /// </summary>
        AutoAttackFPRestore = 107,

        /// <summary>
        /// Cooldown in seconds for AutoAttackFPRestore.
        /// </summary>
        AutoAttackFPRestoreCooldownSeconds = 108,

        /// <summary>
        /// Percent of maximum FP and Stamina both required to enable HighFPAndStaminaAttackPercentAdjustment.
        /// </summary>
        HighFPAndStaminaAttackThresholdPercent = 109,

        /// <summary>
        /// Percent Attack adjustment applied while both current FP and Stamina meet the high-resource threshold.
        /// </summary>
        HighFPAndStaminaAttackPercentAdjustment = 110,

        /// <summary>
        /// HP threshold percent that must be crossed before no-save temporary HP can trigger.
        /// </summary>
        LowHPNoSaveTemporaryHPThresholdPercent = 111,

        /// <summary>
        /// Percent of maximum HP granted as temporary HP when the no-save low-HP trigger succeeds.
        /// </summary>
        LowHPNoSaveTemporaryHPPercent = 112,

        /// <summary>
        /// Duration in seconds for LowHPNoSaveTemporaryHPPercent temporary HP.
        /// </summary>
        LowHPNoSaveTemporaryHPDurationSeconds = 113,

        /// <summary>
        /// Cooldown in seconds for the no-save low-HP temporary HP trigger.
        /// </summary>
        LowHPNoSaveTemporaryHPCooldownSeconds = 114,

        /// <summary>
        /// RecastGroup id reduced when the creature defeats an enemy.
        /// </summary>
        DefeatedEnemyRecastReductionGroup = 115,

        /// <summary>
        /// Seconds removed from DefeatedEnemyRecastReductionGroup when the creature defeats an enemy.
        /// </summary>
        DefeatedEnemyRecastReductionSeconds = 116,

        /// <summary>
        /// Internal temporary flat damage bonus consumed by the next auto-attack.
        /// </summary>
        NextAutoAttackDamageBonus = 117,

        /// <summary>
        /// Internal temporary SkillType value that causes the next matching ability activation delay to become zero, then is consumed.
        /// </summary>
        NextAttackNoDelay = 118,

        /// <summary>
        /// Flat damage bonus granted to the next auto-attack after a pistol ability is used.
        /// </summary>
        PistolAbilityUsedNextAutoAttackDamageBonus = 119,

        /// <summary>
        /// Duration in seconds for PistolAbilityUsedNextAutoAttackDamageBonus to remain available.
        /// </summary>
        PistolAbilityUsedNextAutoAttackDamageDurationSeconds = 120,

        /// <summary>
        /// Flat damage bonus granted to the next auto-attack after a throwing ability is used.
        /// </summary>
        ThrowingAbilityUsedNextAutoAttackDamageBonus = 121,

        /// <summary>
        /// Duration in seconds for ThrowingAbilityUsedNextAutoAttackDamageBonus to remain available.
        /// </summary>
        ThrowingAbilityUsedNextAutoAttackDamageDurationSeconds = 122,

        /// <summary>
        /// Temporary percent Evasion adjustment applied after a pistol ability is used.
        /// </summary>
        PistolAbilityUsedEvasionPercentAdjustment = 123,

        /// <summary>
        /// Duration in seconds for PistolAbilityUsedEvasionPercentAdjustment.
        /// </summary>
        PistolAbilityUsedEvasionDurationSeconds = 124,

        /// <summary>
        /// Temporary percent Evasion adjustment applied after a twin blade ability is used.
        /// </summary>
        TwinBladeAbilityUsedEvasionPercentAdjustment = 125,

        /// <summary>
        /// Duration in seconds for TwinBladeAbilityUsedEvasionPercentAdjustment.
        /// </summary>
        TwinBladeAbilityUsedEvasionDurationSeconds = 126,

        /// <summary>
        /// Primary RecastGroup id that can trigger an ability-used recast reduction.
        /// </summary>
        AbilityUsedRecastReductionTriggerGroup = 127,

        /// <summary>
        /// Secondary RecastGroup id that can trigger an ability-used recast reduction.
        /// </summary>
        AbilityUsedRecastReductionSecondaryTriggerGroup = 128,

        /// <summary>
        /// RecastGroup id whose active recast is reduced when the trigger group matches the used ability.
        /// </summary>
        AbilityUsedRecastReductionTargetGroup = 129,

        /// <summary>
        /// Seconds removed from AbilityUsedRecastReductionTargetGroup when the ability-used trigger fires.
        /// </summary>
        AbilityUsedRecastReductionSeconds = 130,

        /// <summary>
        /// Minimum number of impacted targets required for a throwing area ability to restore Stamina.
        /// </summary>
        ThrowingAreaAbilityMinTargetsStaminaRestoreThreshold = 131,

        /// <summary>
        /// Flat Stamina restored when a throwing area ability meets the minimum target threshold.
        /// </summary>
        ThrowingAreaAbilityMinTargetsStaminaRestore = 132,

        /// <summary>
        /// Temporary percent Attack gained per impacted target from throwing area abilities.
        /// </summary>
        ThrowingAreaAbilityAttackPercentPerTarget = 133,

        /// <summary>
        /// Duration in seconds for ThrowingAreaAbilityAttackPercentPerTarget stacks.
        /// </summary>
        ThrowingAreaAbilityAttackDurationSeconds = 134,

        /// <summary>
        /// Maximum total temporary Attack percent allowed from ThrowingAreaAbilityAttackPercentPerTarget stacks.
        /// </summary>
        ThrowingAreaAbilityAttackPercentMax = 135,

        /// <summary>
        /// Minimum number of impacted targets required for a saberstaff area ability to restore resources.
        /// </summary>
        SaberstaffAreaAbilityMinTargetsResourceRestoreThreshold = 136,

        /// <summary>
        /// Flat FP restored when a saberstaff area ability meets the resource restore target threshold.
        /// </summary>
        SaberstaffAreaAbilityFPRestore = 137,

        /// <summary>
        /// Flat Stamina restored when a saberstaff area ability meets the resource restore target threshold.
        /// </summary>
        SaberstaffAreaAbilityStaminaRestore = 138,

        /// <summary>
        /// Cooldown in seconds for saberstaff area ability resource restoration.
        /// </summary>
        SaberstaffAreaAbilityResourceRestoreCooldownSeconds = 139,

        /// <summary>
        /// Minimum number of impacted targets required for a saberstaff area ability to apply temporary buffs.
        /// </summary>
        SaberstaffAreaAbilityMinTargetsBuffThreshold = 140,

        /// <summary>
        /// Temporary percent attack delay reduction applied when a saberstaff area ability meets the buff threshold.
        /// </summary>
        SaberstaffAreaAbilityHastePercentAdjustment = 141,

        /// <summary>
        /// Temporary flat attack deflection chance applied when a saberstaff area ability meets the buff threshold.
        /// </summary>
        SaberstaffAreaAbilityAttackDeflection = 142,

        /// <summary>
        /// Duration in seconds for saberstaff area ability haste and attack deflection buffs.
        /// </summary>
        SaberstaffAreaAbilityBuffDurationSeconds = 143,

        /// <summary>
        /// Minimum number of impacted targets required for a twin blade area ability to gain haste.
        /// </summary>
        TwinBladeAreaAbilityMinTargetsHasteThreshold = 144,

        /// <summary>
        /// Temporary percent attack delay reduction gained per twin blade area ability haste stack.
        /// </summary>
        TwinBladeAreaAbilityHastePercentAdjustment = 145,

        /// <summary>
        /// Duration in seconds for TwinBladeAreaAbilityHastePercentAdjustment stacks.
        /// </summary>
        TwinBladeAreaAbilityHasteDurationSeconds = 146,

        /// <summary>
        /// Maximum total temporary attack delay reduction allowed from TwinBladeAreaAbilityHastePercentAdjustment stacks.
        /// </summary>
        TwinBladeAreaAbilityHastePercentMax = 147,

        /// <summary>
        /// Flat Stamina restored per impacted target by twin blade area abilities.
        /// </summary>
        TwinBladeAreaAbilityStaminaRestorePerTarget = 148,

        /// <summary>
        /// Maximum Stamina restored by TwinBladeAreaAbilityStaminaRestorePerTarget.
        /// </summary>
        TwinBladeAreaAbilityStaminaRestoreMax = 149,

        /// <summary>
        /// Percent chance for twin blade area abilities to use SWLOR's standard critical rating.
        /// </summary>
        TwinBladeAreaAbilityCriticalRatePercentAdjustment = 150,

        /// <summary>
        /// Flat Stamina restored when a twin blade single-target ability is used, subject to cooldown.
        /// </summary>
        TwinBladeSingleTargetAbilityStaminaRestore = 151,

        /// <summary>
        /// Cooldown in seconds for TwinBladeSingleTargetAbilityStaminaRestore.
        /// </summary>
        TwinBladeSingleTargetAbilityStaminaRestoreCooldownSeconds = 152,

        /// <summary>
        /// Legacy cooldown stat for twin blade area ability Stamina restoration. Prefer TwinBladeAreaAbilityCooldownStaminaRestoreCooldownSeconds for current combat hooks.
        /// </summary>
        TwinBladeAreaAbilityStaminaRestoreCooldownSeconds = 153,

        /// <summary>
        /// Temporary flat attack deflection chance applied after a twin blade single-target ability is used.
        /// </summary>
        TwinBladeSingleTargetAbilityAttackDeflection = 154,

        /// <summary>
        /// Duration in seconds for TwinBladeSingleTargetAbilityAttackDeflection.
        /// </summary>
        TwinBladeSingleTargetAbilityAttackDeflectionDurationSeconds = 155,

        /// <summary>
        /// Percent chance, after taking damage from a recent target, to make the next ability activation delay zero.
        /// </summary>
        DamageTakenRecentTargetNextAbilityNoDelayChance = 156,

        /// <summary>
        /// Seconds a damaged target relationship remains recent for DamageTakenRecentTargetNextAbilityNoDelayChance.
        /// </summary>
        DamageTakenRecentTargetWindowSeconds = 157,

        /// <summary>
        /// Flat Stamina restored for each twin blade area haste stack successfully gained.
        /// </summary>
        TwinBladeAreaAbilityStaminaRestoreOnHasteStack = 158,

        /// <summary>
        /// Flat Stamina restored per impacted target by twin blade area abilities when the cooldown-gated restore trigger succeeds.
        /// </summary>
        TwinBladeAreaAbilityCooldownStaminaRestorePerTarget = 159,

        /// <summary>
        /// Maximum Stamina restored by TwinBladeAreaAbilityCooldownStaminaRestorePerTarget.
        /// </summary>
        TwinBladeAreaAbilityCooldownStaminaRestoreMax = 160,

        /// <summary>
        /// Cooldown in seconds for TwinBladeAreaAbilityCooldownStaminaRestorePerTarget.
        /// </summary>
        TwinBladeAreaAbilityCooldownStaminaRestoreCooldownSeconds = 161,

        /// <summary>
        /// HP threshold percent that must be crossed before granting a no-Stamina-cost ability charge.
        /// </summary>
        LowHPNextAbilityNoStaminaCostThresholdPercent = 162,

        /// <summary>
        /// SkillType value for the no-Stamina-cost ability charge granted by the low-HP trigger.
        /// </summary>
        LowHPNextAbilityNoStaminaCostSkillType = 163,

        /// <summary>
        /// Duration in seconds for the low-HP no-Stamina-cost ability charge.
        /// </summary>
        LowHPNextAbilityNoStaminaCostDurationSeconds = 164,

        /// <summary>
        /// Cooldown in seconds for LowHPNextAbilityNoStaminaCostSkillType.
        /// </summary>
        LowHPNextAbilityNoStaminaCostCooldownSeconds = 165,

        /// <summary>
        /// Temporary SkillType value that makes the next matching ability cost 0 Stamina.
        /// </summary>
        NextAbilityNoStaminaCostSkillType = 166,

        /// <summary>
        /// PerkType value for the ability that can consume the critical-hit next-ability damage bonus.
        /// </summary>
        CriticalNextAbilityDamageBonusPerkType = 167,

        /// <summary>
        /// Flat damage added to the next matching ability after the critical-hit trigger succeeds.
        /// </summary>
        CriticalNextAbilityDamageBonus = 168,

        /// <summary>
        /// Duration in seconds for CriticalNextAbilityDamageBonus.
        /// </summary>
        CriticalNextAbilityDamageBonusDurationSeconds = 169,

        /// <summary>
        /// Cooldown in seconds for CriticalNextAbilityDamageBonus.
        /// </summary>
        CriticalNextAbilityDamageBonusCooldownSeconds = 170,

        /// <summary>
        /// PerkType value for the ability that can consume the deflection-triggered next-ability damage bonus.
        /// </summary>
        DeflectionNextAbilityDamageBonusPerkType = 171,

        /// <summary>
        /// Flat damage added to the next matching ability after the deflection trigger succeeds.
        /// </summary>
        DeflectionNextAbilityDamageBonus = 172,

        /// <summary>
        /// Duration in seconds for DeflectionNextAbilityDamageBonus.
        /// </summary>
        DeflectionNextAbilityDamageBonusDurationSeconds = 173,

        /// <summary>
        /// Temporary flat damage added to the next ability matching its grouped PerkType.
        /// </summary>
        NextAbilityDamageBonus = 174,

        /// <summary>
        /// Percent chance for a critical hit from the target's side to restore Stamina.
        /// </summary>
        CriticalSideAttackStaminaRestoreChance = 175,

        /// <summary>
        /// Flat Stamina restored when CriticalSideAttackStaminaRestoreChance succeeds.
        /// </summary>
        CriticalSideAttackStaminaRestore = 176,

        /// <summary>
        /// Percent Defense adjustment applied to the target through Exposed after a single-target ability critical hit.
        /// </summary>
        SingleTargetCriticalTargetDefensePercentAdjustment = 177,

        /// <summary>
        /// Duration in seconds for SingleTargetCriticalTargetDefensePercentAdjustment.
        /// </summary>
        SingleTargetCriticalTargetDefenseDurationSeconds = 178,

        /// <summary>
        /// SkillType value required before CriticalStaminaRestore can trigger. Invalid or 0 allows any skill.
        /// </summary>
        CriticalStaminaRestoreSkillType = 179,

        /// <summary>
        /// SkillType value required before CriticalNextAbilityDamageBonus can trigger. Invalid or 0 allows any skill.
        /// </summary>
        CriticalNextAbilityDamageBonusTriggerSkillType = 180,

        /// <summary>
        /// SkillType value required for DamageTakenRecentTargetNextAbilityNoDelayChance to grant a no-delay ability charge.
        /// </summary>
        DamageTakenRecentTargetNextAbilityNoDelaySkillType = 181
    }
}
