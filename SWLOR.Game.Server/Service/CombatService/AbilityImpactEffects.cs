using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;

namespace SWLOR.Game.Server.Service.CombatService
{
    public static class AbilityImpactEffects
    {
        internal static void ApplyThrowingAreaAbilityImpactEffects(uint activator, AbilityImpactSummary summary)
        {
            if (!summary.IsAreaAbility)
                return;

            var staminaThreshold = Stat.GetStatAdjustment(activator, StatType.ThrowingAreaAbilityMinTargetsStaminaRestoreThreshold);
            var staminaRestore = Stat.GetStatAdjustment(activator, StatType.ThrowingAreaAbilityMinTargetsStaminaRestore);
            if (staminaThreshold > 0 && staminaRestore > 0 && summary.ImpactedTargetCount >= staminaThreshold)
            {
                Stat.RestoreStamina(activator, staminaRestore);
            }

            var attackPerTarget = Stat.GetStatAdjustment(activator, StatType.ThrowingAreaAbilityAttackPercentPerTarget);
            var attackDuration = Stat.GetStatAdjustment(activator, StatType.ThrowingAreaAbilityAttackDurationSeconds);
            var attackMax = Stat.GetStatAdjustment(activator, StatType.ThrowingAreaAbilityAttackPercentMax);
            if (attackPerTarget > 0 && attackDuration > 0 && attackMax > 0)
            {
                AbilityImpactEffects.ApplyStackingAttackBoost(activator, attackPerTarget, attackMax, attackDuration, summary.ImpactedTargetCount);
            }
        }

        internal static void ApplyAreaAbilityImpactEffects(uint activator, AbilityImpactSummary summary)
        {
            if (!summary.IsAreaAbility)
                return;

            var restoreThreshold = Stat.GetStatAdjustment(activator, StatType.AreaAbilityMinTargetsResourceRestoreThreshold);
            var fpRestore = Stat.GetStatAdjustment(activator, StatType.AreaAbilityFPRestore);
            var staminaRestore = Stat.GetStatAdjustment(activator, StatType.AreaAbilityStaminaRestore);
            var restoreCooldown = Stat.GetStatAdjustment(activator, StatType.AreaAbilityResourceRestoreCooldownSeconds);
            if (restoreThreshold > 0 &&
                summary.ImpactedTargetCount >= restoreThreshold &&
                CombatStatTriggers.TryUseStatTrigger(activator, StatType.AreaAbilityFPRestore, restoreCooldown))
            {
                if (fpRestore > 0)
                {
                    Stat.RestoreFP(activator, fpRestore);
                    AbilityRecoveryEffects.ApplyAbilityRestoredFPEffects(activator);
                }
                if (staminaRestore > 0)
                    Stat.RestoreStamina(activator, staminaRestore);

                if (fpRestore > 0 && staminaRestore > 0)
                    AbilityRecoveryEffects.ApplyAbilityRestoredBothResourcesEffects(activator);
            }

            var buffThreshold = Stat.GetStatAdjustment(activator, StatType.AreaAbilityMinTargetsBuffThreshold);
            if (buffThreshold <= 0 || summary.ImpactedTargetCount < buffThreshold)
                return;

            var duration = Stat.GetStatAdjustment(activator, StatType.AreaAbilityBuffDurationSeconds);
            if (duration <= 0)
                return;

            var haste = Stat.GetStatAdjustment(activator, StatType.AreaAbilityHastePercentAdjustment);
            if (haste != 0)
            {
                TemporaryStatModifier.Replace(
                    activator,
                    StatType.AttackDelayReductionPercent,
                    haste,
                    duration,
                    StatType.AttackDelayReductionPercent);
            }

            var deflection = Stat.GetStatAdjustment(activator, StatType.AreaAbilityAttackDeflection);
            if (deflection != 0)
            {
                TemporaryStatModifier.Replace(
                    activator,
                    StatType.AttackDeflection,
                    deflection,
                    duration,
                    StatType.AttackDeflection);
                AbilityGrantedDeflectionEffects.ApplyAbilityGrantedAttackDeflectionEffects(activator);
            }
        }

        internal static void ApplySpearAbilityImpactEffects(uint activator, AbilityImpactSummary summary)
        {
            if (!summary.IsAreaAbility)
                return;

            var minimumTargets = Stat.GetStatAdjustment(activator, StatType.SpearDamageCripplingDefenseMinimumTargets);
            var staminaRestore = Stat.GetStatAdjustment(activator, StatType.SpearDamageCripplingDefenseStaminaRestore);
            if (minimumTargets > 0 &&
                staminaRestore > 0 &&
                summary.ImpactedTargetCount >= minimumTargets)
            {
                Stat.RestoreStamina(activator, staminaRestore);
            }
        }

        internal static void ApplyTwinBladeAbilityImpactEffects(uint activator, AbilityImpactSummary summary)
        {
            if (summary.IsSingleTargetAbility)
            {
                var staminaRestore = Stat.GetStatAdjustment(activator, StatType.TwinBladeSingleTargetAbilityStaminaRestore);
                var cooldown = Stat.GetStatAdjustment(activator, StatType.TwinBladeSingleTargetAbilityStaminaRestoreCooldownSeconds);
                if (staminaRestore > 0 && CombatStatTriggers.TryUseStatTrigger(activator, StatType.TwinBladeSingleTargetAbilityStaminaRestore, cooldown))
                {
                    Stat.RestoreStamina(activator, staminaRestore);
                }
            }

            if (!summary.IsAreaAbility)
                return;

            AbilityImpactEffects.ApplyTwinBladeSweepingAdvance(activator, summary);

            var hasteThreshold = Stat.GetStatAdjustment(activator, StatType.TwinBladeAreaAbilityMinTargetsHasteThreshold);
            var hastePercent = Stat.GetStatAdjustment(activator, StatType.TwinBladeAreaAbilityHastePercentAdjustment);
            var hasteDuration = Stat.GetStatAdjustment(activator, StatType.TwinBladeAreaAbilityHasteDurationSeconds);
            var hasteMax = Stat.GetStatAdjustment(activator, StatType.TwinBladeAreaAbilityHastePercentMax);
            if (hasteThreshold > 0 &&
                hastePercent > 0 &&
                hasteDuration > 0 &&
                hasteMax > 0 &&
                summary.ImpactedTargetCount >= hasteThreshold)
            {
                var stacksGained = AbilityImpactEffects.ApplyStackingHasteBoost(activator, hastePercent, hasteMax, hasteDuration, 1);
                var staminaOnStack = Stat.GetStatAdjustment(activator, StatType.TwinBladeAreaAbilityStaminaRestoreOnHasteStack);
                if (staminaOnStack > 0 && stacksGained > 0)
                {
                    Stat.RestoreStamina(activator, staminaOnStack * stacksGained);
                }
            }

            var staminaPerTarget = Stat.GetStatAdjustment(activator, StatType.TwinBladeAreaAbilityStaminaRestorePerTarget);
            var staminaMax = Stat.GetStatAdjustment(activator, StatType.TwinBladeAreaAbilityStaminaRestoreMax);
            if (staminaPerTarget > 0 && staminaMax > 0)
            {
                Stat.RestoreStamina(activator, Math.Min(staminaMax, staminaPerTarget * summary.ImpactedTargetCount));
            }

            var cooldownStaminaPerTarget = Stat.GetStatAdjustment(activator, StatType.TwinBladeAreaAbilityCooldownStaminaRestorePerTarget);
            var cooldownStaminaMax = Stat.GetStatAdjustment(activator, StatType.TwinBladeAreaAbilityCooldownStaminaRestoreMax);
            var areaStaminaCooldown = Stat.GetStatAdjustment(activator, StatType.TwinBladeAreaAbilityCooldownStaminaRestoreCooldownSeconds);
            if (cooldownStaminaPerTarget > 0 &&
                cooldownStaminaMax > 0 &&
                CombatStatTriggers.TryUseStatTrigger(activator, StatType.TwinBladeAreaAbilityCooldownStaminaRestorePerTarget, areaStaminaCooldown))
            {
                Stat.RestoreStamina(activator, Math.Min(cooldownStaminaMax, cooldownStaminaPerTarget * summary.ImpactedTargetCount));
            }
        }

        internal static void ApplyTwinBladeSweepingAdvance(uint activator, AbilityImpactSummary summary)
        {
            if (Stat.GetStatAdjustment(activator, StatType.TwinBladeCycloneSweepingAdvance) <= 0)
                return;

            var minimumTargets = Stat.GetStatAdjustment(activator, StatType.TwinBladeCycloneSweepingAdvanceMinimumTargets);
            if (minimumTargets <= 0 || summary.ImpactedTargetCount < minimumTargets)
                return;

            var staminaRestore = Stat.GetStatAdjustment(activator, StatType.TwinBladeCycloneSweepingAdvanceStaminaRestore);
            if (staminaRestore > 0)
            {
                Stat.RestoreStamina(activator, staminaRestore);
            }

            var hastePercent = Stat.GetStatAdjustment(activator, StatType.TwinBladeCycloneSweepingAdvanceHastePercent);
            var duration = Stat.GetStatAdjustment(activator, StatType.TwinBladeCycloneSweepingAdvanceDurationSeconds);
            if (hastePercent > 0 && duration > 0)
            {
                TemporaryStatModifier.Replace(
                    activator,
                    StatType.AttackDelayReductionPercent,
                    hastePercent,
                    duration,
                    StatType.TwinBladeCycloneSweepingAdvanceHastePercent);
            }
        }

        internal static int ApplyStackingHasteBoost(
            uint activator,
            int hastePercent,
            int maxHastePercent,
            int durationSeconds,
            int requestedStacks)
        {
            return TemporaryStatModifier.AddCapped(
                activator,
                StatType.AttackDelayReductionPercent,
                hastePercent,
                durationSeconds,
                maxHastePercent,
                StatType.TwinBladeAreaAbilityHastePercentAdjustment,
                requestedStacks);
        }

        internal static void ApplyStackingAttackBoost(
            uint activator,
            int attackPercent,
            int maxAttackPercent,
            int durationSeconds,
            int requestedStacks)
        {
            TemporaryStatModifier.AddCapped(
                activator,
                StatType.AttackPercentAdjustment,
                attackPercent,
                durationSeconds,
                maxAttackPercent,
                StatType.ThrowingAreaAbilityAttackPercentPerTarget,
                requestedStacks);
        }

        internal static RecastGroup GetRecastGroupFromStat(int value)
        {
            return value > 0 && Enum.IsDefined(typeof(RecastGroup), value)
                ? (RecastGroup)value
                : RecastGroup.Invalid;
        }

        internal static SkillType GetSkillTypeFromStat(int value)
        {
            return value > 0 && Enum.IsDefined(typeof(SkillType), value)
                ? (SkillType)value
                : SkillType.Invalid;
        }

        internal static StatusEffectCategory GetStatusEffectCategoryFromStat(int value)
        {
            return value > 0
                ? (StatusEffectCategory)value
                : 0;
        }

        internal static bool AbilityMatchesHeavyVibrobladeDefenseAbilityTrigger(
            uint creature,
            AbilityDetail ability)
        {
            return AbilityImpactEffects.AbilityMatchesAnyPerkTypeStat(
                       creature,
                       ability,
                       StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerPrimaryPerkType,
                       StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerSecondaryPerkType,
                       StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerTertiaryPerkType,
                       StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerQuaternaryPerkType,
                       StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerQuinaryPerkType,
                       StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerSenaryPerkType) ||
                   AbilityImpactEffects.AbilityMatchesAnyPerkTypeStat(
                       creature,
                       ability,
                       StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerPrimaryPerkType,
                       StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerSecondaryPerkType,
                       StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerTertiaryPerkType,
                       StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerQuaternaryPerkType,
                       StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerQuinaryPerkType,
                       StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerSenaryPerkType);
        }

        internal static bool AbilityMatchesReversalCutTrigger(uint creature, AbilityDetail ability)
        {
            return AbilityImpactEffects.AbilityMatchesAnyPerkTypeStat(
                creature,
                ability,
                StatType.TwinBladeDuelistReversalCutTriggerPrimaryPerkType,
                StatType.TwinBladeDuelistReversalCutTriggerSecondaryPerkType,
                StatType.TwinBladeDuelistReversalCutTriggerTertiaryPerkType);
        }

        internal static bool AbilityMatchesAnyPerkTypeStat(
            uint creature,
            AbilityDetail ability,
            params StatType[] statTypes)
        {
            var perkType = ability?.EffectiveLevelPerkType ?? PerkType.Invalid;
            if (perkType == PerkType.Invalid)
                return false;

            foreach (var statType in statTypes)
            {
                if (perkType == AbilityImpactEffects.GetPerkTypeFromStat(Stat.GetStatAdjustment(creature, statType)))
                    return true;
            }

            return false;
        }

        internal static bool AbilityMatchesPerkCategoryStat(
            uint creature,
            AbilityDetail ability,
            StatType categoryStatType)
        {
            var perkType = ability?.EffectiveLevelPerkType ?? PerkType.Invalid;
            if (perkType == PerkType.Invalid)
                return false;

            var categoryValue = Stat.GetStatAdjustment(creature, categoryStatType);
            return categoryValue > 0 && Perk.IsPerkInCategory(perkType, categoryValue);
        }

        internal static AbilityType GetAbilityTypeFromStatPlusOne(int value)
        {
            var abilityValue = value - 1;
            return value > 0 && Enum.IsDefined(typeof(AbilityType), abilityValue)
                ? (AbilityType)abilityValue
                : AbilityType.Invalid;
        }

        internal static bool IsCurrentFPAtOrAbovePercent(uint creature, int thresholdPercent)
        {
            var maxFP = Stat.GetMaxFP(creature);
            if (maxFP <= 0)
                return false;

            return Stat.GetCurrentFP(creature) >= maxFP * (thresholdPercent / 100f);
        }

        public static bool IsCurrentFPAndStaminaAtOrAbovePercent(uint creature, int thresholdPercent)
        {
            if (thresholdPercent <= 0)
                return false;

            var maxFP = Stat.GetMaxFP(creature);
            var maxStamina = Stat.GetMaxStamina(creature);
            if (maxFP <= 0 || maxStamina <= 0)
                return false;

            return Stat.GetCurrentFP(creature) >= maxFP * (thresholdPercent / 100f) &&
                   Stat.GetCurrentStamina(creature) >= maxStamina * (thresholdPercent / 100f);
        }

        public static bool IsCurrentFPAndStaminaAtOrBelowPercent(uint creature, int thresholdPercent)
        {
            if (thresholdPercent <= 0)
                return false;

            var maxFP = Stat.GetMaxFP(creature);
            var maxStamina = Stat.GetMaxStamina(creature);
            if (maxFP <= 0 || maxStamina <= 0)
                return false;

            return Stat.GetCurrentFP(creature) <= maxFP * (thresholdPercent / 100f) &&
                   Stat.GetCurrentStamina(creature) <= maxStamina * (thresholdPercent / 100f);
        }

        internal static bool SkillTypeMatches(SkillType actualSkillType, SkillType requiredSkillType)
        {
            return requiredSkillType != SkillType.Invalid && actualSkillType == requiredSkillType;
        }

        internal static bool IsWeaponOrForceDamage(SkillType skillType, CombatDamageType damageType)
        {
            return skillType == SkillType.Force ||
                   damageType == CombatDamageType.Force ||
                   AbilityImpactEffects.IsWeaponSkillType(skillType);
        }

        internal static bool IsWeaponOrForceAbility(SkillType skillType)
        {
            return skillType == SkillType.Force || AbilityImpactEffects.IsWeaponSkillType(skillType);
        }

        public static int GetCombatImpactWeaponDamage(uint activator, SkillType skillType)
        {
            if (!AbilityImpactEffects.IsWeaponSkillType(skillType))
                return 0;

            var weapon = AbilityImpactEffects.GetCombatImpactWeapon(activator);
            return GetIsObjectValid(weapon)
                ? Item.GetDMG(weapon)
                : 0;
        }

        internal static uint GetCombatImpactWeapon(uint activator)
        {
            var rightHand = GetItemInSlot(InventorySlot.RightHand, activator);
            if (AbilityImpactEffects.IsCombatImpactWeapon(rightHand))
                return rightHand;

            var leftHand = GetItemInSlot(InventorySlot.LeftHand, activator);
            if (AbilityImpactEffects.IsCombatImpactWeapon(leftHand))
                return leftHand;

            return OBJECT_INVALID;
        }

        internal static bool IsCombatImpactWeapon(uint item)
        {
            return GetIsObjectValid(item) &&
                   Skill.GetSkillTypeByBaseItem((BaseItem)GetBaseItemType(item)) != SkillType.Invalid;
        }

        public static bool IsWeaponSkillType(SkillType skillType)
        {
            return skillType != SkillType.Invalid &&
                   skillType.GetAttribute<SkillType, SkillAttribute>().CombatPointCategory == CombatPointCategoryType.Weapon;
        }

        internal static PerkType GetPerkTypeFromStat(int value)
        {
            return value > 0 && Enum.IsDefined(typeof(PerkType), value)
                ? (PerkType)value
                : PerkType.Invalid;
        }

        internal static int GetTargetedAbilityAdjustment(
            uint creature,
            PerkType perkType,
            StatType primaryPerkStatType,
            StatType secondaryPerkStatType,
            StatType adjustmentStatType)
        {
            if (perkType == PerkType.Invalid)
                return 0;

            var adjustment = Perk.GetTargetedStatBonus(
                creature,
                perkType,
                primaryPerkStatType,
                secondaryPerkStatType,
                adjustmentStatType);

            foreach (var effect in StatusEffect.GetCreatureStatusEffects(creature).GetAllEffects())
            {
                adjustment += AbilityImpactEffects.GetTargetedStatGroupAdjustment(
                    effect.StatGroup,
                    perkType,
                    primaryPerkStatType,
                    secondaryPerkStatType,
                    adjustmentStatType);
            }

            return adjustment;
        }

        internal static int GetTargetedStatGroupAdjustment(
            StatGroup statGroup,
            PerkType perkType,
            StatType primaryPerkStatType,
            StatType secondaryPerkStatType,
            StatType adjustmentStatType)
        {
            if (statGroup == null)
                return 0;

            var primaryPerk = AbilityImpactEffects.GetPerkTypeFromStat(statGroup.Stats[primaryPerkStatType]);
            var secondaryPerk = AbilityImpactEffects.GetPerkTypeFromStat(statGroup.Stats[secondaryPerkStatType]);
            if (perkType != primaryPerk && perkType != secondaryPerk)
                return 0;

            return statGroup.Stats[adjustmentStatType];
        }

        internal static void GrantNextAbilityDamageBonus(uint creature, PerkType perkType, int bonus, int durationSeconds)
        {
            if (!GetIsObjectValid(creature) || perkType == PerkType.Invalid || bonus == 0 || durationSeconds <= 0)
                return;

            TemporaryStatModifier.Replace(
                creature,
                StatType.NextAbilityDamageBonus,
                bonus,
                durationSeconds,
                AbilityImpactEffects.GetPerkTypeGroup(perkType));
        }

        internal static void GrantNextAbilityStaminaCostAdjustment(
            uint creature,
            PerkType perkType,
            int adjustment,
            int durationSeconds)
        {
            if (!GetIsObjectValid(creature) || perkType == PerkType.Invalid || adjustment == 0 || durationSeconds <= 0)
                return;

            TemporaryStatModifier.Replace(
                creature,
                StatType.NextAbilityStaminaCostAdjustment,
                adjustment,
                durationSeconds,
                AbilityImpactEffects.GetPerkTypeGroup(perkType));
        }

        public static void GrantNextSkillAbilityBonuses(
            uint creature,
            SkillType skillType,
            int damageBonus,
            int criticalRatePercentAdjustment,
            int durationSeconds,
            int defenseIgnorePercentAdjustment = 0)
        {
            if (!GetIsObjectValid(creature) ||
                durationSeconds <= 0 ||
                damageBonus == 0 && criticalRatePercentAdjustment == 0 && defenseIgnorePercentAdjustment == 0)
                return;

            TemporaryStatModifier.Replace(
                creature,
                StatType.NextSkillAbilitySkillType,
                (int)skillType,
                durationSeconds,
                StatType.NextSkillAbilitySkillType);

            if (damageBonus != 0)
            {
                TemporaryStatModifier.Replace(
                    creature,
                    StatType.NextSkillAbilityDamageBonus,
                    damageBonus,
                    durationSeconds,
                    StatType.NextSkillAbilitySkillType);
            }

            if (criticalRatePercentAdjustment != 0)
            {
                TemporaryStatModifier.Replace(
                    creature,
                    StatType.NextSkillAbilityCriticalRatePercentAdjustment,
                    criticalRatePercentAdjustment,
                    durationSeconds,
                    StatType.NextSkillAbilitySkillType);
            }

            if (defenseIgnorePercentAdjustment != 0)
            {
                TemporaryStatModifier.Replace(
                    creature,
                    StatType.NextSkillAbilityDefenseIgnorePercentAdjustment,
                    defenseIgnorePercentAdjustment,
                    durationSeconds,
                    StatType.NextSkillAbilitySkillType);
            }
        }

        public static void GrantNextSkillAbilityStaminaCostAdjustment(
            uint creature,
            SkillType skillType,
            int adjustment,
            int durationSeconds)
        {
            if (!GetIsObjectValid(creature) ||
                skillType == SkillType.Invalid ||
                adjustment == 0 ||
                durationSeconds <= 0)
                return;

            TemporaryStatModifier.Replace(
                creature,
                StatType.NextSkillAbilityStaminaCostAdjustmentSkillType,
                (int)skillType,
                durationSeconds,
                StatType.NextSkillAbilityStaminaCostAdjustmentSkillType);
            TemporaryStatModifier.Replace(
                creature,
                StatType.NextSkillAbilityStaminaCostAdjustment,
                adjustment,
                durationSeconds,
                StatType.NextSkillAbilityStaminaCostAdjustmentSkillType);
        }

        public static void ApplyBleedingStatusExpiredEffects(uint source)
        {
            if (!GetIsObjectValid(source))
                return;

            var adjustment = Stat.GetStatAdjustment(
                source,
                StatType.BleedingStatusExpiredNextSkillAbilityStaminaCostAdjustment);
            var skillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(
                source,
                StatType.BleedingStatusExpiredNextSkillAbilitySkillType));
            var windowSeconds = Stat.GetStatAdjustment(
                source,
                StatType.BleedingStatusExpiredNextSkillAbilityWindowSeconds);

            AbilityImpactEffects.GrantNextSkillAbilityStaminaCostAdjustment(source, skillType, adjustment, windowSeconds);
        }

        internal static void GrantNextSkillAutoAttackDamageBonus(
            uint creature,
            SkillType skillType,
            int damageBonus,
            int durationSeconds)
        {
            if (!GetIsObjectValid(creature) ||
                skillType == SkillType.Invalid ||
                damageBonus == 0 ||
                durationSeconds <= 0)
                return;

            TemporaryStatModifier.Replace(
                creature,
                StatType.NextSkillAutoAttackDamageBonusSkillType,
                (int)skillType,
                durationSeconds,
                StatType.NextSkillAutoAttackDamageBonusSkillType);
            TemporaryStatModifier.Replace(
                creature,
                StatType.NextSkillAutoAttackDamageBonus,
                damageBonus,
                durationSeconds,
                StatType.NextSkillAutoAttackDamageBonusSkillType);
        }

        internal static void GrantNextAbilityFPCostAdjustment(
            uint creature,
            SkillType skillType,
            int adjustment,
            int durationSeconds)
        {
            if (!GetIsObjectValid(creature) ||
                skillType == SkillType.Invalid ||
                adjustment == 0 ||
                durationSeconds <= 0)
                return;

            TemporaryStatModifier.Replace(
                creature,
                StatType.NextAbilityFPCostAdjustmentSkillType,
                (int)skillType,
                durationSeconds,
                StatType.NextAbilityFPCostAdjustmentSkillType);
            TemporaryStatModifier.Replace(
                creature,
                StatType.NextAbilityFPCostAdjustment,
                adjustment,
                durationSeconds,
                StatType.NextAbilityFPCostAdjustmentSkillType);
        }

        internal static string GetPerkTypeGroup(PerkType perkType)
        {
            return $"{nameof(PerkType)}:{(int)perkType}";
        }

        /// <summary>
        /// Return a damage bonus equal to 0.15 of the player's relevant skill or an NPC's level.
        /// This helps abilities as the player progresses.
        ///
        /// Global scaling on gear is closer to 0.25 DMG per player skill level so low tier abilities will still
        /// become less useful over time, and get replaced by higher tier ones.  But they will have some utility still.
        /// </summary>
        /// <returns> 0.15 * the player's rank in the specified skill, or the level for NPCs.</returns>

        public static int GetAbilityDamageBonus(uint creature, SkillType skill)
        {
            var level = 0;
            if (!GetIsPC(creature))
            {
                var npcStats = Stat.GetNPCStats(creature);
                level = npcStats.Level;
            }
            else
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = DB.Get<Player>(playerId);

                var pcSkill = dbPlayer.Skills[skill];
                level = pcSkill.Rank;
            }


            return (int)(0.15f * level);
        }

        /// <summary>
        /// On module heartbeat, clear a PC's saved combat facing if they are no longer in combat.
    }
}
