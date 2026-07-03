using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.CombatService
{
    public static class WeaponAbilityImpactEffects
    {
        internal static void ApplyHeavyVibrobladeDefenseImpactRiders(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (!AbilityImpactEffects.AbilityMatchesHeavyVibrobladeDefenseAbilityTrigger(activator, ability))
                return;

            var enmityBonus = Stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeDefenseAbilityEnmityBonus);
            if (enmityBonus > 0)
            {
                Enmity.ModifyEnmity(activator, target, enmityBonus);
            }

            if (AbilityImpactEffects.AbilityMatchesAnyPerkTypeStat(
                    activator,
                    ability,
                    StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerPrimaryPerkType,
                    StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerSecondaryPerkType,
                    StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerTertiaryPerkType,
                    StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerQuaternaryPerkType,
                    StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerQuinaryPerkType,
                    StatType.HeavyVibrobladeDefenseAbilityCrushingBlowTriggerSenaryPerkType) &&
                Stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeDefenseAbilityCrushingBlow) > 0)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(CrushingBlowStatusEffect), 30f, CombatDamageType.Physical);
            }
        }

        internal static void ApplyForceDarkImpactRiders(
            uint activator,
            uint target,
            Type primaryStatusEffect,
            IEnumerable<Type> additionalStatusEffects)
        {
            if (Stat.GetStatAdjustment(activator, StatType.DarkManipulatorCollapseWill) <= 0 ||
                !WeaponStatusImpactEffects.AbilityAppliedAnyStatus(
                    primaryStatusEffect,
                    additionalStatusEffects,
                    typeof(NightmareField1StatusEffect),
                    typeof(EclipseOfResolve1StatusEffect)))
            {
                return;
            }

            StatusEffect.ApplyStatusEffect(activator, target, typeof(ExposedStatusEffect), 30f, CombatDamageType.Force);
            StatusEffect.ApplyStatusEffect(activator, target, typeof(ForceErosionStatusEffect), 30f, CombatDamageType.Force);
        }

        internal static void ApplyLightsaberOffenseImpactRiders(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (!ability.IsHostileAbility)
                return;

            var sunderDuration = Stat.GetStatAdjustment(activator, StatType.LightsaberOffenseSunderDurationSeconds);
            if (sunderDuration > 0)
            {
                WeaponAbilityImpactEffects.ApplyLightsaberOffenseSunder(activator, target, sunderDuration);
            }

            var disorientedDuration = Stat.GetStatAdjustment(activator, StatType.LightsaberOffenseDisorientedDurationSeconds);
            if (disorientedDuration > 0)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(DisorientedStatusEffect), disorientedDuration, ResistanceType.Mind);
            }

            if (ability.IsSingleTargetAbility)
            {
                var disruptionDuration = Stat.GetStatAdjustment(activator, StatType.LightsaberOffenseSingleTargetForceDisruptionDurationSeconds);
                if (disruptionDuration > 0)
                {
                    StatusEffect.ApplyStatusEffect(activator, target, typeof(ForceDisruptionStatusEffect), disruptionDuration, CombatDamageType.Force);
                }
            }

            WeaponAbilityImpactEffects.ApplyLightsaberOffensePurify(activator, target);
        }

        internal static void ApplyLightsaberOffenseSunder(uint activator, uint target, int duration)
        {
            const int DefensePenaltyPercent = 15;

            if (WeaponAbilityImpactEffects.HasSunderPenaltyAtLeast(target, DefensePenaltyPercent))
                return;

            StatusEffect.ApplyStatusEffect(
                activator,
                target,
                new SunderStatusEffect(DefensePenaltyPercent),
                duration,
                CombatDamageType.Physical);
        }

        internal static void ApplyNextDamageDealtBleedEffect(
            uint attacker,
            uint defender,
            CombatDamageType damageType)
        {
            var duration = TemporaryStatModifier.Consume(
                attacker,
                StatType.NextDamageDealtBleedDurationSeconds,
                StatType.NextDamageDealtBleedDurationSeconds);
            if (duration <= 0)
                return;

            StatusEffect.ApplyStatusEffect(attacker, defender, typeof(BleedStatusEffect), duration, damageType);
        }

        internal static void ApplyBleedingTargetAbilityBleedRefresh(uint attacker, uint defender, SkillType skillType)
        {
            if (!StatusEffect.HasStatusEffect(defender, typeof(BleedStatusEffect), attacker))
                return;

            var requiredSkillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.AbilityDamageToBleedingTargetSkillType));
            if (!AbilityImpactEffects.SkillTypeMatches(skillType, requiredSkillType))
                return;

            var extensionSeconds = Stat.GetStatAdjustment(
                attacker,
                StatType.BleedingTargetAbilityBleedDurationExtensionSeconds);
            if (extensionSeconds <= 0)
                return;

            StatusEffect.ExtendStatusEffectDuration(defender, typeof(BleedStatusEffect), attacker, extensionSeconds);
        }

        internal static void ApplyBleedingTargetAbilityBleedSpread(
            uint attacker,
            uint defender,
            SkillType skillType,
            CombatDamageType damageType)
        {
            if (!damageType.IsPhysicalDamageType() ||
                !AbilityImpactEffects.IsWeaponSkillType(skillType) ||
                !StatusEffect.HasStatusEffectCategory(defender, StatusEffectCategory.Bleeding))
            {
                return;
            }

            var chance = Stat.GetStatAdjustment(attacker, StatType.BleedingTargetAbilityBleedSpreadChance);
            var duration = Stat.GetStatAdjustment(attacker, StatType.BleedingTargetAbilityBleedSpreadDurationSeconds);
            if (chance <= 0 || duration <= 0 || Random.D100(1) > chance)
                return;

            var maximumTargets = Stat.GetStatAdjustment(attacker, StatType.BleedingTargetAbilityBleedSpreadMaxTargets);
            maximumTargets = maximumTargets <= 0 ? 1 : maximumTargets;

            foreach (var nearby in AbilityTargeting.GetHostileTargetsNearLocation(
                         attacker,
                         GetLocation(defender),
                         5f,
                         maximumTargets,
                         defender))
            {
                StatusEffect.ApplyStatusEffect(attacker, nearby, typeof(BleedStatusEffect), duration, damageType);
            }
        }

        internal static void ApplyAutoAttackSuppressionStack(
            uint attacker,
            uint defender,
            SkillType skillType,
            CombatDamageType damageType)
        {
            if (!CombatSkillType.IsRangedWeaponSkill(skillType))
                return;

            var chance = Stat.GetStatAdjustment(attacker, StatType.AutoAttackSuppressionStackChance);
            var duration = Stat.GetStatAdjustment(attacker, StatType.AutoAttackSuppressionStackDurationSeconds);
            if (chance <= 0 || duration <= 0 || Random.D100(1) > chance)
                return;

            WeaponAbilityImpactEffects.ApplySuppressionStack(
                attacker,
                defender,
                Stat.GetStatAdjustment(attacker, StatType.AutoAttackSuppressionStackEvasionPenaltyPercent),
                duration,
                damageType);
        }

        internal static void ApplyRangedHitSuppressionStack(
            uint attacker,
            uint defender,
            SkillType skillType,
            CombatDamageType damageType)
        {
            if (!CombatSkillType.IsRangedWeaponSkill(skillType))
                return;

            var duration = Stat.GetStatAdjustment(attacker, StatType.RangedHitSuppressionStackDurationSeconds);
            if (duration <= 0)
                return;

            WeaponAbilityImpactEffects.ApplySuppressionStack(
                attacker,
                defender,
                Stat.GetStatAdjustment(attacker, StatType.RangedHitSuppressionStackEvasionPenaltyPercent),
                duration,
                damageType);
        }

        public static void ApplySuppressionStack(
            uint attacker,
            uint defender,
            int evasionPenaltyPercent,
            int durationSeconds,
            CombatDamageType damageType)
        {
            if (!GetIsObjectValid(attacker) ||
                !GetIsObjectValid(defender) ||
                durationSeconds <= 0)
            {
                return;
            }

            var adjustedEvasionPenaltyPercent = Math.Max(
                0,
                evasionPenaltyPercent +
                Stat.GetStatAdjustment(attacker, StatType.SuppressionStackEvasionPenaltyPercentAdjustment));

            StatusEffect.ApplyStatusEffect(
                attacker,
                defender,
                new SuppressionStatusEffect(adjustedEvasionPenaltyPercent),
                durationSeconds,
                damageType);
        }

        public static int GetSuppressionStackCount(uint target, uint source = OBJECT_INVALID)
        {
            if (!GetIsObjectValid(target))
                return 0;

            return StatusEffect.GetCreatureStatusEffects(target)
                .GetAllEffects()
                .OfType<SuppressionStatusEffect>()
                .Count(effect => !GetIsObjectValid(source) || effect.Source == source);
        }

        internal static int GetSuppressionDamageDealtToOtherTargetsAdjustment(uint attacker, uint defender)
        {
            if (!GetIsObjectValid(attacker) || !GetIsObjectValid(defender))
                return 0;

            var adjustment = 0;
            foreach (var group in StatusEffect.GetCreatureStatusEffects(attacker)
                         .GetAllEffects()
                         .OfType<SuppressionStatusEffect>()
                         .Where(effect => GetIsObjectValid(effect.Source) && effect.Source != defender)
                         .GroupBy(effect => effect.Source))
            {
                var requiredStacks = Stat.GetStatAdjustment(
                    group.Key,
                    StatType.SuppressionStackDamageDealtToOtherTargetsRequiredStacks);
                var percentAdjustment = Stat.GetStatAdjustment(
                    group.Key,
                    StatType.SuppressionStackDamageDealtToOtherTargetsPercentAdjustment);
                if (requiredStacks > 0 && percentAdjustment != 0 && group.Count() >= requiredStacks)
                {
                    adjustment += percentAdjustment;
                }
            }

            return adjustment;
        }

        internal static int GetDamageToSourceAppliedStatusTargetAdjustment(uint attacker, uint defender)
        {
            var category = AbilityImpactEffects.GetStatusEffectCategoryFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.DamageToSourceAppliedStatusTargetCategory));
            var adjustment = Stat.GetStatAdjustment(attacker, StatType.DamageToSourceAppliedStatusTargetPercentAdjustment);
            if (category == 0 || adjustment == 0 || !WeaponAbilityImpactEffects.TargetHasSourceAppliedStatusCategory(defender, attacker, category))
                return 0;

            return adjustment;
        }

        internal static int GetAbilityDamageToSourceAppliedStatusTargetAdjustment(
            uint attacker,
            uint defender,
            SkillType skillType)
        {
            var requiredSkillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.AbilityDamageToSourceAppliedStatusTargetSkillType));
            if (requiredSkillType != SkillType.Invalid && !AbilityImpactEffects.SkillTypeMatches(skillType, requiredSkillType))
                return 0;

            var category = AbilityImpactEffects.GetStatusEffectCategoryFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.AbilityDamageToSourceAppliedStatusTargetCategory));
            var adjustment = Stat.GetStatAdjustment(attacker, StatType.AbilityDamageToSourceAppliedStatusTargetPercentAdjustment);
            if (category == 0 || adjustment == 0 || !WeaponAbilityImpactEffects.TargetHasSourceAppliedStatusCategory(defender, attacker, category))
                return 0;

            return adjustment;
        }

        internal static bool TargetHasSourceAppliedStatusCategory(
            uint defender,
            uint source,
            StatusEffectCategory category)
        {
            if (!GetIsObjectValid(defender) || !GetIsObjectValid(source) || category == 0)
                return false;

            foreach (var effect in StatusEffect.GetCreatureStatusEffects(defender).GetAllEffects())
            {
                if (effect.Source == source && (effect.Categories & category) != 0)
                    return true;
            }

            return false;
        }

        internal static bool TargetHasAnyStatusEffectCategory(uint creature, StatusEffectCategory category)
        {
            if (!GetIsObjectValid(creature) || category == 0)
                return false;

            return StatusEffect.GetCreatureStatusEffects(creature)
                .GetAllEffects()
                .Any(effect => (effect.Categories & category) != 0);
        }

        internal static void ApplyFrenzySlashHasteRefresh(uint attacker)
        {
            var duration = Stat.GetStatAdjustment(attacker, StatType.FrenzySlashHasteRefreshDurationSeconds);
            if (duration <= 0)
                return;

            TemporaryStatModifier.Refresh(
                attacker,
                StatType.AttackDelayReductionPercent,
                duration,
                StatType.AttackDelayReductionPercent);
        }

        internal static bool HasSunderPenaltyAtLeast(uint target, int defensePenaltyPercent)
        {
            return StatusEffect.GetCreatureStatusEffects(target)
                .GetAllEffects()
                .OfType<SunderStatusEffect>()
                .Select(effect => Math.Abs(effect.StatGroup.Stats.GetValueOrDefault(StatType.PhysicalDefensePercentAdjustment)))
                .DefaultIfEmpty(0)
                .Max() >= defensePenaltyPercent;
        }

        internal static void ApplyLightsaberOffensePurify(uint activator, uint target)
        {
            if (Stat.GetStatAdjustment(activator, StatType.LightsaberOffensePurify) <= 0)
                return;

            var cooldown = Stat.GetStatAdjustment(activator, StatType.LightsaberOffensePurifyCooldownSeconds);
            if (!CombatStatTriggers.TryUseStatTrigger(activator, StatType.LightsaberOffensePurify, cooldown))
                return;

            var effect = StatusEffect.GetCreatureStatusEffects(activator)
                .GetAllEffects()
                .FirstOrDefault(IsTransferableHarmfulStatus);
            if (effect == null)
                return;

            var transferred = effect.Clone();
            StatusEffect.RemoveStatusEffect(activator, effect.GetType(), effect.Source, false);
            StatusEffect.ApplyStatusEffect(activator, target, transferred, 30f, CombatDamageType.Force);
        }

        internal static bool IsTransferableHarmfulStatus(IStatusEffect effect)
        {
            return effect != null &&
                   (effect.Categories & (StatusEffectCategory.Debuff | StatusEffectCategory.Control | StatusEffectCategory.Bleeding)) != 0;
        }

        internal static void ApplyAbilityUsedPerkCategoryTargetEnmityToSourceStatus(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (!AbilityImpactEffects.AbilityMatchesPerkCategoryStat(
                    activator,
                    ability,
                    StatType.AbilityUsedPerkCategoryTargetEnmityToSourceCategoryId))
            {
                return;
            }

            var enmity = Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategoryTargetEnmityToSourcePercentAdjustment);
            var duration = Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategoryTargetEnmityToSourceDurationSeconds);
            if (enmity <= 0 || duration <= 0)
                return;

            StatusEffect.ApplyStatusEffect(
                activator,
                target,
                new EnmityToStatusSourceStatusEffect(
                    enmity,
                    "Covering Claws",
                    EffectIconType.CoveringClawsStatusEffect),
                duration,
                CombatDamageType.Physical);
        }

    }
}
