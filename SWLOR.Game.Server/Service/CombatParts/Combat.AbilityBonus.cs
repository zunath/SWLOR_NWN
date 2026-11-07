using System.Collections.Generic;
using System.Linq;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.ActivityService;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatRecorderService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using ObjectPlugin = SWLOR.NWN.API.NWNX.ObjectPlugin;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;

namespace SWLOR.Game.Server.Service
{
    public static partial class Combat
    {
        public static int PrepareOpeningAutoAttack(uint attacker, SkillType skillType)
        {
            if (!GetIsObjectValid(attacker) || !IsWeaponSkillType(skillType))
                return 0;

            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(attacker, StatType.OpeningAutoAttackSkillType));
            if (!SkillTypeMatchesOrGlobal(skillType, requiredSkillType))
                return 0;

            var idleSeconds = Stat.GetStatAdjustment(attacker, StatType.OpeningAutoAttackIdleSeconds);
            if (idleSeconds <= 0)
                return 0;

            var lastActivity = GetLastCompletedOffensiveActivityAt(attacker);
            if (lastActivity != default &&
                (Clock.UtcNow - lastActivity).TotalSeconds < idleSeconds)
                return 0;

            TrackCombatActivity(attacker);
            StatusEffect.RemoveStatusEffect(attacker, typeof(OpeningAttackReadyStatusEffect), false);

            var damageBonus = Stat.GetStatAdjustment(attacker, StatType.OpeningAutoAttackDamageBonus);
            if (damageBonus != 0)
            {
                TemporaryStatModifier.Replace(
                    attacker,
                    StatType.CurrentAutoAttackDamageBonus,
                    damageBonus,
                    6,
                    StatType.CurrentAutoAttackDamageBonus);

            }

            var criticalDamageBonus = Stat.GetStatAdjustment(
                attacker,
                StatType.OpeningAutoAttackCriticalDamagePercentAdjustment);
            if (criticalDamageBonus != 0)
            {
                TemporaryStatModifier.Replace(
                    attacker,
                    StatType.CurrentAutoAttackCriticalDamagePercentAdjustment,
                    criticalDamageBonus,
                    6,
                    StatType.CurrentAutoAttackCriticalDamagePercentAdjustment);
            }

            return Stat.GetStatAdjustment(attacker, StatType.OpeningAutoAttackCriticalRatePercentAdjustment);
        }

        public static void PrepareQueuedWeaponAbilityOpeningAttackAtActivation(uint attacker, SkillType skillType)
        {
            if (!GetIsObjectValid(attacker) || skillType == SkillType.Invalid)
                return;

            // Discard any stale per-swing payload before capturing this queued activation. The
            // opening condition is evaluated before the queued ability records its own activity.
            ClearQueuedWeaponAbilityAttemptBonuses(attacker);
            var criticalRate = PrepareOpeningAutoAttack(attacker, skillType);
            var damageBonus = TemporaryStatModifier.Consume(
                attacker,
                StatType.CurrentAutoAttackDamageBonus,
                StatType.CurrentAutoAttackDamageBonus);
            var criticalDamage = ConsumeOpeningAutoAttackCriticalDamageAdjustment(attacker);
            StoreQueuedWeaponAbilityActivationOpeningBonuses(
                attacker,
                skillType,
                damageBonus,
                criticalRate,
                criticalDamage);
        }

        public static int ConsumeOpeningAutoAttackCriticalDamageAdjustment(uint attacker)
        {
            return TemporaryStatModifier.Consume(
                attacker,
                StatType.CurrentAutoAttackCriticalDamagePercentAdjustment,
                StatType.CurrentAutoAttackCriticalDamagePercentAdjustment);
        }

        public static void ClearOpeningAutoAttackModifiers(uint attacker)
        {
            TemporaryStatModifier.Consume(
                attacker,
                StatType.CurrentAutoAttackDamageBonus,
                StatType.CurrentAutoAttackDamageBonus);
            TemporaryStatModifier.Consume(
                attacker,
                StatType.CurrentAutoAttackCriticalDamagePercentAdjustment,
                StatType.CurrentAutoAttackCriticalDamagePercentAdjustment);
            ClearQueuedWeaponAbilityAttemptBonuses(attacker);
        }

        public static bool HasNextAbilityNoStaminaCost(uint creature, SkillType skillType)
        {
            if (skillType == SkillType.Invalid)
                return false;

            var storedSkillType = GetSkillTypeFromStat(TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.NextAbilityNoStaminaCostSkillType,
                StatType.NextAbilityNoStaminaCostSkillType));

            return storedSkillType == skillType;
        }

        public static bool ConsumeNextAbilityNoStaminaCost(uint creature, SkillType skillType)
        {
            if (!HasNextAbilityNoStaminaCost(creature, skillType))
                return false;

            TemporaryStatModifier.Consume(
                creature,
                StatType.NextAbilityNoStaminaCostSkillType,
                StatType.NextAbilityNoStaminaCostSkillType);

            return true;
        }

        public static int GetNextSkillAbilityStaminaCostAdjustment(
            uint creature, SkillType skillType, bool isHostileAbility = false)
        {
            var adjustment = 0;
            foreach (var source in TemporaryStatModifier.GetStatSources(creature, StatType.NextSkillAbilityStaminaCostAdjustment))
            {
                if (MatchesStaminaDiscount(source, skillType, isHostileAbility))
                    adjustment += source[StatType.NextSkillAbilityStaminaCostAdjustment];
            }
            return adjustment;
        }

        public static int ConsumeNextSkillAbilityStaminaCostAdjustment(
            uint creature, SkillType skillType, bool isHostileAbility = false)
        {
            var adjustment = 0;
            foreach (var source in TemporaryStatModifier.GetStatSources(creature, StatType.NextSkillAbilityStaminaCostAdjustment))
            {
                if (MatchesStaminaDiscount(source, skillType, isHostileAbility))
                    adjustment += TemporaryStatModifier.ConsumeSource(creature, StatType.NextSkillAbilityStaminaCostAdjustment, source);
            }
            return adjustment;
        }

        /// <param name="abilityStatusCategories">Status categories the ability applies; a bonus stored with
        /// a required category (Breaker Reversal: Control) waits for an ability that applies it.</param>
        public static (int DamageBonus, int CriticalRatePercentAdjustment, int DefenseIgnorePercentAdjustment) ConsumeNextSkillAbilityBonuses(
            uint creature,
            SkillType skillType,
            StatusEffectCategory abilityStatusCategories = StatusEffectCategory.None)
        {
            if (skillType == SkillType.Invalid)
                return (0, 0, 0);

            var storedSkillType = GetSkillTypeFromStat(TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.NextSkillAbilitySkillType,
                StatType.NextSkillAbilitySkillType));
            if (!SkillTypeMatches(skillType, storedSkillType))
                return (0, 0, 0);

            var requiredCategory = GetStatusEffectCategoryFromStat(TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.NextSkillAbilityRequiredCategory,
                StatType.NextSkillAbilitySkillType));
            if (requiredCategory != StatusEffectCategory.None &&
                (abilityStatusCategories & requiredCategory) == 0)
            {
                return (0, 0, 0);
            }

            var damageBonus = TemporaryStatModifier.Consume(
                creature,
                StatType.NextSkillAbilityDamageBonus,
                StatType.NextSkillAbilitySkillType);
            var criticalRate = TemporaryStatModifier.Consume(
                creature,
                StatType.NextSkillAbilityCriticalRatePercentAdjustment,
                StatType.NextSkillAbilitySkillType);
            var defenseIgnore = TemporaryStatModifier.Consume(
                creature,
                StatType.NextSkillAbilityDefenseIgnorePercentAdjustment,
                StatType.NextSkillAbilitySkillType);
            TemporaryStatModifier.Consume(
                creature,
                StatType.NextSkillAbilityRequiredCategory,
                StatType.NextSkillAbilitySkillType);
            TemporaryStatModifier.Consume(
                creature,
                StatType.NextSkillAbilitySkillType,
                StatType.NextSkillAbilitySkillType);

            return (damageBonus, criticalRate, defenseIgnore);
        }

        public static int ConsumePersistentNextSkillAbilityCriticalRateBonus(uint creature, SkillType skillType)
        {
            if (!PersistentNextSkillAbilityCriticalRateMatches(creature, skillType))
                return 0;

            var criticalRate = TemporaryStatModifier.Consume(
                creature,
                StatType.PersistentNextSkillAbilityCriticalRatePercentAdjustment,
                StatType.PersistentNextSkillAbilityCriticalRateSkillType);
            TemporaryStatModifier.Consume(
                creature,
                StatType.PersistentNextSkillAbilityCriticalRateSkillType,
                StatType.PersistentNextSkillAbilityCriticalRateSkillType);
            TemporaryStatModifier.Consume(
                creature,
                StatType.PersistentNextSkillAbilityCriticalRateRangedOnly,
                StatType.PersistentNextSkillAbilityCriticalRateSkillType);
            StatusEffect.RemoveStatusEffect(creature, typeof(CriticalRateStackTrackerStatusEffect), false);
            return criticalRate;
        }

        public static int GetPersistentNextSkillAbilityCriticalRateBonus(uint creature, SkillType skillType)
        {
            return PersistentNextSkillAbilityCriticalRateMatches(creature, skillType)
                ? TemporaryStatModifier.GetStatAdjustment(
                    creature,
                    StatType.PersistentNextSkillAbilityCriticalRatePercentAdjustment,
                    StatType.PersistentNextSkillAbilityCriticalRateSkillType)
                : 0;
        }

        private static bool PersistentNextSkillAbilityCriticalRateMatches(uint creature, SkillType skillType)
        {
            var storedSkillType = GetSkillTypeFromStat(TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.PersistentNextSkillAbilityCriticalRateSkillType,
                StatType.PersistentNextSkillAbilityCriticalRateSkillType));
            var rangedOnly = TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.PersistentNextSkillAbilityCriticalRateRangedOnly,
                StatType.PersistentNextSkillAbilityCriticalRateSkillType) > 0;
            return MatchesNextSkillAbilityCriticalRateSkill(skillType, storedSkillType, rangedOnly);
        }

        public static void StoreQueuedWeaponAbilityCriticalRateBonus(
            uint creature,
            SkillType skillType,
            int criticalRatePercentAdjustment)
        {
            StoreQueuedWeaponAbilityAttemptBonuses(
                creature,
                skillType,
                0,
                criticalRatePercentAdjustment,
                0);
        }

        private static void StoreQueuedWeaponAbilityActivationOpeningBonuses(
            uint creature,
            SkillType skillType,
            int damageBonus,
            int criticalRatePercentAdjustment,
            int criticalDamagePercentAdjustment)
        {
            if (!GetIsObjectValid(creature) ||
                skillType == SkillType.Invalid ||
                (damageBonus == 0 &&
                 criticalRatePercentAdjustment == 0 &&
                 criticalDamagePercentAdjustment == 0))
                return;

            var storedSkillType = GetSkillTypeFromStat(TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType));
            if (SkillTypeMatches(skillType, storedSkillType))
            {
                damageBonus += TemporaryStatModifier.GetStatAdjustment(
                    creature,
                    StatType.QueuedWeaponAbilityIdleDamageBonus,
                    StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
                criticalRatePercentAdjustment += TemporaryStatModifier.GetStatAdjustment(
                    creature,
                    StatType.QueuedWeaponAbilityActivationCriticalRatePercentAdjustment,
                    StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
                criticalDamagePercentAdjustment = Math.Max(
                    criticalDamagePercentAdjustment,
                    TemporaryStatModifier.GetStatAdjustment(
                        creature,
                        StatType.QueuedWeaponAbilityIdleCriticalDamagePercentAdjustment,
                        StatType.QueuedWeaponAbilityActivationCriticalRateSkillType));
            }

            TemporaryStatModifier.Replace(
                creature,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType,
                (int)skillType,
                30,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
            TemporaryStatModifier.Replace(
                creature,
                StatType.QueuedWeaponAbilityIdleDamageBonus,
                damageBonus,
                30,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
            TemporaryStatModifier.Replace(
                creature,
                StatType.QueuedWeaponAbilityActivationCriticalRatePercentAdjustment,
                criticalRatePercentAdjustment,
                30,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
            TemporaryStatModifier.Replace(
                creature,
                StatType.QueuedWeaponAbilityIdleCriticalDamagePercentAdjustment,
                criticalDamagePercentAdjustment,
                30,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
        }

        private static void StoreQueuedWeaponAbilityAttemptBonuses(
            uint creature,
            SkillType skillType,
            int damageBonus,
            int criticalRatePercentAdjustment,
            int criticalDamagePercentAdjustment)
        {
            if (!GetIsObjectValid(creature) ||
                skillType == SkillType.Invalid ||
                (damageBonus == 0 &&
                 criticalRatePercentAdjustment == 0 &&
                 criticalDamagePercentAdjustment == 0))
                return;

            var storedSkillType = GetSkillTypeFromStat(TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.QueuedWeaponAbilityCriticalRateSkillType,
                StatType.QueuedWeaponAbilityCriticalRateSkillType));
            if (SkillTypeMatches(skillType, storedSkillType))
            {
                damageBonus += TemporaryStatModifier.GetStatAdjustment(
                    creature,
                    StatType.QueuedWeaponAbilityDamageBonus,
                    StatType.QueuedWeaponAbilityCriticalRateSkillType);
                criticalRatePercentAdjustment += TemporaryStatModifier.GetStatAdjustment(
                    creature,
                    StatType.QueuedWeaponAbilityCriticalRatePercentAdjustment,
                    StatType.QueuedWeaponAbilityCriticalRateSkillType);
                criticalDamagePercentAdjustment += TemporaryStatModifier.GetStatAdjustment(
                    creature,
                    StatType.QueuedWeaponAbilityCriticalDamagePercentAdjustment,
                    StatType.QueuedWeaponAbilityCriticalRateSkillType);
            }

            TemporaryStatModifier.Replace(
                creature,
                StatType.QueuedWeaponAbilityCriticalRateSkillType,
                (int)skillType,
                6,
                StatType.QueuedWeaponAbilityCriticalRateSkillType);
            TemporaryStatModifier.Replace(
                creature,
                StatType.QueuedWeaponAbilityCriticalRatePercentAdjustment,
                criticalRatePercentAdjustment,
                6,
                StatType.QueuedWeaponAbilityCriticalRateSkillType);
            TemporaryStatModifier.Replace(
                creature,
                StatType.QueuedWeaponAbilityDamageBonus,
                damageBonus,
                6,
                StatType.QueuedWeaponAbilityCriticalRateSkillType);
            TemporaryStatModifier.Replace(
                creature,
                StatType.QueuedWeaponAbilityCriticalDamagePercentAdjustment,
                criticalDamagePercentAdjustment,
                6,
                StatType.QueuedWeaponAbilityCriticalRateSkillType);
        }

        public static void StoreQueuedWeaponAbilityActivationCriticalRateBonus(
            uint creature,
            SkillType skillType,
            int criticalRatePercentAdjustment)
        {
            if (!GetIsObjectValid(creature) ||
                skillType == SkillType.Invalid ||
                criticalRatePercentAdjustment <= 0)
            {
                return;
            }

            TemporaryStatModifier.Replace(
                creature,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType,
                (int)skillType,
                30,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
            TemporaryStatModifier.Replace(
                creature,
                StatType.QueuedWeaponAbilityActivationCriticalRatePercentAdjustment,
                criticalRatePercentAdjustment,
                30,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
        }

        public static void StoreQueuedWeaponAbilityActivationIdleBonuses(uint creature, SkillType skillType)
        {
            if (!GetIsObjectValid(creature) || skillType == SkillType.Invalid)
                return;

            var idleBonuses = GetIdleSkillAbilityBonuses(creature, skillType);
            if (idleBonuses == default)
                return;

            TemporaryStatModifier.Replace(
                creature,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType,
                (int)skillType,
                30,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
            TemporaryStatModifier.Replace(
                creature,
                StatType.QueuedWeaponAbilityIdleDamageBonus,
                idleBonuses.DamageBonus,
                30,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
            TemporaryStatModifier.Replace(
                creature,
                StatType.QueuedWeaponAbilityIdleHitChancePercentAdjustment,
                idleBonuses.HitChancePercentAdjustment,
                30,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
            TemporaryStatModifier.Replace(
                creature,
                StatType.QueuedWeaponAbilityIdleCriticalDamagePercentAdjustment,
                idleBonuses.CriticalDamagePercentAdjustment,
                30,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
        }

        public static void StoreWeaponAbilityActivationIdleBonuses(
            uint creature,
            bool isIdle,
            int damageBonus,
            int criticalRatePercentAdjustment,
            int defenseIgnorePercent)
        {
            ClearWeaponAbilityActivationIdleBonuses(creature);
            if (!GetIsObjectValid(creature))
                return;

            TemporaryStatModifier.Replace(creature, StatType.WeaponAbilityActivationIdleSnapshot, 1, 30,
                StatType.WeaponAbilityActivationIdleSnapshot);
            TemporaryStatModifier.Replace(creature, StatType.WeaponAbilityActivationIdleDamageBonus,
                isIdle ? damageBonus : 0, 30, StatType.WeaponAbilityActivationIdleSnapshot);
            TemporaryStatModifier.Replace(creature, StatType.WeaponAbilityActivationIdleCriticalRatePercentAdjustment,
                isIdle ? criticalRatePercentAdjustment : 0, 30, StatType.WeaponAbilityActivationIdleSnapshot);
            TemporaryStatModifier.Replace(creature, StatType.WeaponAbilityActivationIdleDefenseIgnorePercent,
                isIdle ? defenseIgnorePercent : 0, 30, StatType.WeaponAbilityActivationIdleSnapshot);
        }

        public static bool HasWeaponAbilityActivationIdleSnapshot(uint creature)
        {
            return TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.WeaponAbilityActivationIdleSnapshot,
                StatType.WeaponAbilityActivationIdleSnapshot) > 0;
        }

        public static int GetWeaponAbilityActivationIdleDamageBonus(uint creature) =>
            TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.WeaponAbilityActivationIdleDamageBonus,
                StatType.WeaponAbilityActivationIdleSnapshot);

        public static int GetWeaponAbilityActivationIdleCriticalRateBonus(uint creature) =>
            TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.WeaponAbilityActivationIdleCriticalRatePercentAdjustment,
                StatType.WeaponAbilityActivationIdleSnapshot);

        public static int GetWeaponAbilityActivationIdleDefenseIgnorePercent(uint creature) =>
            TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.WeaponAbilityActivationIdleDefenseIgnorePercent,
                StatType.WeaponAbilityActivationIdleSnapshot);

        public static void ClearWeaponAbilityActivationIdleBonuses(uint creature)
        {
            TemporaryStatModifier.Consume(creature, StatType.WeaponAbilityActivationIdleDamageBonus,
                StatType.WeaponAbilityActivationIdleSnapshot);
            TemporaryStatModifier.Consume(creature, StatType.WeaponAbilityActivationIdleCriticalRatePercentAdjustment,
                StatType.WeaponAbilityActivationIdleSnapshot);
            TemporaryStatModifier.Consume(creature, StatType.WeaponAbilityActivationIdleDefenseIgnorePercent,
                StatType.WeaponAbilityActivationIdleSnapshot);
            TemporaryStatModifier.Consume(creature, StatType.WeaponAbilityActivationIdleSnapshot,
                StatType.WeaponAbilityActivationIdleSnapshot);
        }

        /// <param name="ability">The queued ability. Its status categories add category accuracy
        /// (Scrapper Control: "your control abilities gain Ability Accuracy"), which ability rolls get
        /// in ApplyCombatImpact but the native weapon roll would otherwise miss.</param>
        public static int GetQueuedWeaponAbilityActivationHitChanceAdjustment(
            uint creature,
            SkillType skillType,
            AbilityDetail ability = null)
        {
            var activationSkillType = GetSkillTypeFromStat(TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType));
            var idleHitChanceAdjustment = SkillTypeMatches(skillType, activationSkillType)
                ? TemporaryStatModifier.GetStatAdjustment(
                    creature,
                    StatType.QueuedWeaponAbilityIdleHitChancePercentAdjustment,
                    StatType.QueuedWeaponAbilityActivationCriticalRateSkillType)
                : 0;

            var statusCategoryHitChanceAdjustment = GetAbilityStatusCategoryHitChancePercentAdjustment(
                creature,
                skillType,
                ability?.ResolveImpactStatusCategories?.Invoke(creature) ?? StatusEffectCategory.None);

            // Queued abilities use the native weapon roll instead of TryResolveAbilityHit.
            return GetPhysicalAndForceAbilityHitChanceAdjustment(creature, skillType) + idleHitChanceAdjustment +
                   statusCategoryHitChanceAdjustment;
        }

        public static void ClearQueuedWeaponAbilityActivationBonuses(uint creature)
        {
            TemporaryStatModifier.Consume(
                creature,
                StatType.QueuedWeaponAbilityIdleDamageBonus,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
            TemporaryStatModifier.Consume(
                creature,
                StatType.QueuedWeaponAbilityIdleHitChancePercentAdjustment,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
            TemporaryStatModifier.Consume(
                creature,
                StatType.QueuedWeaponAbilityIdleCriticalDamagePercentAdjustment,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
            TemporaryStatModifier.Consume(
                creature,
                StatType.QueuedWeaponAbilityActivationCriticalRatePercentAdjustment,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
            TemporaryStatModifier.Consume(
                creature,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
        }

        public static (
            int DamageBonus,
            int CriticalRatePercentAdjustment,
            int CriticalDamagePercentAdjustment) ConsumeQueuedWeaponAbilityBonuses(
                uint creature,
                SkillType skillType)
        {
            var damageBonus = 0;
            var criticalRate = 0;
            var criticalDamage = 0;
            var storedSkillType = GetSkillTypeFromStat(TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.QueuedWeaponAbilityCriticalRateSkillType,
                StatType.QueuedWeaponAbilityCriticalRateSkillType));
            if (SkillTypeMatches(skillType, storedSkillType))
            {
                damageBonus = TemporaryStatModifier.Consume(
                    creature,
                    StatType.QueuedWeaponAbilityDamageBonus,
                    StatType.QueuedWeaponAbilityCriticalRateSkillType);
                criticalRate += TemporaryStatModifier.Consume(
                    creature,
                    StatType.QueuedWeaponAbilityCriticalRatePercentAdjustment,
                    StatType.QueuedWeaponAbilityCriticalRateSkillType);
                criticalDamage = TemporaryStatModifier.Consume(
                    creature,
                    StatType.QueuedWeaponAbilityCriticalDamagePercentAdjustment,
                    StatType.QueuedWeaponAbilityCriticalRateSkillType);
                TemporaryStatModifier.Consume(
                    creature,
                    StatType.QueuedWeaponAbilityCriticalRateSkillType,
                    StatType.QueuedWeaponAbilityCriticalRateSkillType);
            }

            var activationSkillType = GetSkillTypeFromStat(TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType));
            if (SkillTypeMatches(skillType, activationSkillType))
            {
                damageBonus += TemporaryStatModifier.Consume(
                    creature,
                    StatType.QueuedWeaponAbilityIdleDamageBonus,
                    StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
                criticalRate += TemporaryStatModifier.Consume(
                    creature,
                    StatType.QueuedWeaponAbilityActivationCriticalRatePercentAdjustment,
                    StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
                TemporaryStatModifier.Consume(
                    creature,
                    StatType.QueuedWeaponAbilityIdleHitChancePercentAdjustment,
                    StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
                criticalDamage = Math.Max(criticalDamage, TemporaryStatModifier.Consume(
                    creature,
                    StatType.QueuedWeaponAbilityIdleCriticalDamagePercentAdjustment,
                    StatType.QueuedWeaponAbilityActivationCriticalRateSkillType));
                TemporaryStatModifier.Consume(
                    creature,
                    StatType.QueuedWeaponAbilityActivationCriticalRateSkillType,
                    StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
            }

            return (damageBonus, criticalRate, criticalDamage);
        }

        public static void ClearQueuedWeaponAbilityAttemptBonuses(uint creature)
        {
            TemporaryStatModifier.Consume(
                creature,
                StatType.QueuedWeaponAbilityDamageBonus,
                StatType.QueuedWeaponAbilityCriticalRateSkillType);
            TemporaryStatModifier.Consume(
                creature,
                StatType.QueuedWeaponAbilityCriticalRatePercentAdjustment,
                StatType.QueuedWeaponAbilityCriticalRateSkillType);
            TemporaryStatModifier.Consume(
                creature,
                StatType.QueuedWeaponAbilityCriticalDamagePercentAdjustment,
                StatType.QueuedWeaponAbilityCriticalRateSkillType);
            TemporaryStatModifier.Consume(
                creature,
                StatType.QueuedWeaponAbilityCriticalRateSkillType,
                StatType.QueuedWeaponAbilityCriticalRateSkillType);
        }

        public static (int DMGBonus, int CriticalRatePercentAdjustment, int EnmityBonus) ConsumeNextAttackGuardedHitBonuses(
            uint creature)
        {
            var attackBonuses = ConsumeNextAttackGuardedHitAutoAttackBonuses(creature);
            var criticalRate = ConsumeNextAttackGuardedHitCriticalRateBonus(creature);

            return (attackBonuses.DMGBonus, criticalRate, attackBonuses.EnmityBonus);
        }

        public static (int DMGBonus, int EnmityBonus) ConsumeNextAttackGuardedHitAutoAttackBonuses(uint creature)
        {
            var dmgBonus = TemporaryStatModifier.Consume(
                creature,
                StatType.NextAttackGuardedHitDMGBonus,
                StatType.NextAttackGuardedHitDMGBonus);
            var enmityBonus = TemporaryStatModifier.Consume(
                creature,
                StatType.NextAttackGuardedHitEnmityBonus,
                StatType.NextAttackGuardedHitDMGBonus);

            return (dmgBonus, enmityBonus);
        }

        public static int ConsumeNextAttackGuardedHitCriticalRateBonus(uint creature)
        {
            return TemporaryStatModifier.Consume(
                creature,
                StatType.NextAttackGuardedHitCriticalRatePercentAdjustment,
                StatType.NextAttackGuardedHitDMGBonus);
        }

        public static int ConsumeStatusAppliedNextAttackDamageBonus(uint creature)
        {
            return TemporaryStatModifier.Consume(
                creature,
                StatType.NextAttackStatusAppliedDMGBonus,
                StatType.NextAttackStatusAppliedDMGBonus);
        }

        public static int GetStatusAppliedNextAttackDamageBonus(uint creature)
        {
            return TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.NextAttackStatusAppliedDMGBonus,
                StatType.NextAttackStatusAppliedDMGBonus);
        }

        public static void ApplyNextAttackGuardedHitEnmityBonus(
            uint attacker,
            uint defender,
            int enmityBonus)
        {
            if (!GetIsObjectValid(attacker) ||
                !GetIsObjectValid(defender) ||
                enmityBonus <= 0)
            {
                return;
            }

            Enmity.ModifyEnmity(attacker, defender, enmityBonus);
        }

        public static void GrantNextAbilityDamageBonus(uint creature, int perkTypeValue, int bonus, int durationSeconds)
        {
            var perkType = GetPerkTypeFromStat(perkTypeValue);
            GrantNextAbilityDamageBonus(creature, perkType, bonus, durationSeconds);
        }

        public static void GrantNextSkillAbilityBonuses(
            uint creature,
            int skillTypeValue,
            int damageBonus,
            int criticalRatePercentAdjustment,
            int durationSeconds)
        {
            var skillType = GetSkillTypeFromStat(skillTypeValue);
            GrantNextSkillAbilityBonuses(creature, skillType, damageBonus, criticalRatePercentAdjustment, durationSeconds);
        }

        public static int ConsumeNextAbilityDamageBonus(uint creature, PerkType perkType)
        {
            if (perkType == PerkType.Invalid)
                return 0;

            return TemporaryStatModifier.Consume(
                creature,
                StatType.NextAbilityDamageBonus,
                GetPerkTypeGroup(perkType));
        }

        public static int GetNextAbilityStaminaCostAdjustment(uint creature, PerkType perkType)
        {
            if (perkType == PerkType.Invalid)
                return 0;

            return TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.NextAbilityStaminaCostAdjustment,
                GetPerkTypeGroup(perkType));
        }

        public static int ConsumeNextAbilityStaminaCostAdjustment(uint creature, PerkType perkType)
        {
            if (perkType == PerkType.Invalid)
                return 0;

            return TemporaryStatModifier.Consume(
                creature,
                StatType.NextAbilityStaminaCostAdjustment,
                GetPerkTypeGroup(perkType));
        }

        public static int GetAbilityDamageFlatAdjustment(uint creature, PerkType perkType, SkillType skillType)
        {
            var adjustment = GetTargetedAbilityAdjustment(
                creature,
                perkType,
                StatType.AbilityDamageFlatAdjustmentPerkType,
                StatType.AbilityDamageFlatAdjustmentSecondaryPerkType,
                StatType.AbilityDamageFlatAdjustment);

            adjustment += GetRangedAttackDamageFlatAdjustment(creature, skillType);

            return adjustment;
        }

        public static int GetAbilityStatusCategoryDamageBonus(
            uint creature,
            SkillType skillType,
            StatusEffectCategory appliedCategories)
        {
            return GetAbilityStatusCategoryAdjustment(
                creature,
                skillType,
                appliedCategories,
                StatType.AbilityStatusCategoryDamageBonus);
        }

        public static int GetAbilityStatusCategoryHitChancePercentAdjustment(
            uint creature,
            SkillType skillType,
            StatusEffectCategory appliedCategories)
        {
            return GetAbilityStatusCategoryAdjustment(
                creature,
                skillType,
                appliedCategories,
                StatType.AbilityStatusCategoryHitChancePercentAdjustment);
        }

        private static int GetAbilityStatusCategoryAdjustment(
            uint creature,
            SkillType skillType,
            StatusEffectCategory appliedCategories,
            StatType adjustmentStatType)
        {
            if (!GetIsObjectValid(creature) || skillType == SkillType.Invalid || appliedCategories == StatusEffectCategory.None)
                return 0;

            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                creature,
                StatType.AbilityStatusCategoryBonusSkillType));
            if (!SkillTypeMatches(skillType, requiredSkillType))
                return 0;

            var requiredCategories = GetStatusEffectCategoryFromStat(Stat.GetStatAdjustment(
                creature,
                StatType.AbilityStatusCategoryBonusRequiredCategory));
            if (requiredCategories == StatusEffectCategory.None ||
                (appliedCategories & requiredCategories) == 0)
            {
                return 0;
            }

            return Stat.GetStatAdjustment(creature, adjustmentStatType);
        }

        public static int GetAbilityStaminaCostFlatAdjustment(uint creature, PerkType perkType)
        {
            return GetTargetedAbilityAdjustment(
                creature,
                perkType,
                StatType.AbilityStaminaCostFlatAdjustmentPerkType,
                StatType.AbilityStaminaCostFlatAdjustmentSecondaryPerkType,
                StatType.AbilityStaminaCostFlatAdjustment);
        }

        public static int GetAbilityStaminaCostFlatAdjustment(uint creature, AbilityDetail ability)
        {
            if (ability == null)
                return 0;

            var skillType = GetAbilitySkillType(creature, ability);
            var adjustment = GetAbilityStaminaCostFlatAdjustment(creature, ability.EffectiveLevelPerkType);
            if (ability.IsHostileAbility)
            {
                adjustment += Stat.GetStatAdjustment(creature, StatType.HostileAbilityStaminaCostFlatAdjustment);
                adjustment += GetNonCriticalRangedAbilityStaminaCostFlatAdjustment(creature, ability);
            }

            var flatSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                creature,
                StatType.SkillAbilityStaminaCostFlatAdjustmentSkillType));
            if (SkillTypeMatches(skillType, flatSkillType))
            {
                adjustment += Stat.GetStatAdjustment(creature, StatType.SkillAbilityStaminaCostFlatAdjustment);
            }

            var highResourceSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                creature,
                StatType.HighResourceAbilityStaminaCostSkillType));
            var threshold = Stat.GetStatAdjustment(creature, StatType.HighResourceAbilityStaminaCostThresholdPercent);
            if (threshold > 0 &&
                SkillTypeMatches(skillType, highResourceSkillType) &&
                IsCurrentFPAtOrAbovePercent(creature, threshold))
            {
                adjustment += Stat.GetStatAdjustment(creature, StatType.HighResourceAbilityStaminaCostAdjustment);
            }

            return adjustment;
        }

        public static int GetAbilityStatusDurationPercentAdjustment(
            uint creature,
            PerkType perkType,
            SkillType skillType,
            Type primaryStatusEffect,
            IEnumerable<Type> additionalStatusEffects,
            Func<IStatusEffect> statusEffectFactory)
        {
            var adjustment = GetTargetedAbilityAdjustment(
                creature,
                perkType,
                StatType.AbilityStatusDurationPercentAdjustmentPerkType,
                StatType.AbilityStatusDurationPercentAdjustmentSecondaryPerkType,
                StatType.AbilityStatusDurationPercentAdjustment);
            adjustment += GetIdleStatusDurationPercentAdjustment(
                creature,
                skillType,
                primaryStatusEffect,
                additionalStatusEffects,
                statusEffectFactory);

            return adjustment;
        }

        private static int GetIdleStatusDurationPercentAdjustment(
            uint creature,
            SkillType skillType,
            Type primaryStatusEffect,
            IEnumerable<Type> additionalStatusEffects,
            Func<IStatusEffect> statusEffectFactory)
        {
            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                creature,
                StatType.IdleStatusDurationPercentAdjustmentSkillType));
            if (!SkillTypeMatches(skillType, requiredSkillType))
                return 0;

            var requiredIdleSeconds = Stat.GetStatAdjustment(creature, StatType.IdleStatusDurationRequiredIdleSeconds);
            if (requiredIdleSeconds <= 0 || HasRecentAttackActivity(creature, requiredIdleSeconds))
                return 0;

            var requiredCategory = GetStatusEffectCategoryFromStat(Stat.GetStatAdjustment(
                creature,
                StatType.IdleStatusDurationRequiredCategory));
            if (requiredCategory != 0 &&
                !StatusContextHasCategory(primaryStatusEffect, additionalStatusEffects, statusEffectFactory, requiredCategory))
            {
                return 0;
            }

            return Stat.GetStatAdjustment(creature, StatType.IdleStatusDurationPercentAdjustment);
        }

        private static bool StatusContextHasCategory(
            Type primaryStatusEffect,
            IEnumerable<Type> additionalStatusEffects,
            Func<IStatusEffect> statusEffectFactory,
            StatusEffectCategory category)
        {
            if (AbilityAppliedAnyStatusCategory(primaryStatusEffect, additionalStatusEffects, category))
                return true;

            var statusEffect = statusEffectFactory?.Invoke();
            return statusEffect != null && (statusEffect.Categories & category) != 0;
        }

        public static int GetAbilityDefenseIgnorePercentAdjustment(uint creature, PerkType perkType, SkillType skillType, uint defender)
        {
            var adjustment = GetTargetedAbilityAdjustment(
                creature,
                perkType,
                StatType.AbilityDefenseIgnorePercentAdjustmentPerkType,
                StatType.AbilityDefenseIgnorePercentAdjustmentSecondaryPerkType,
                StatType.AbilityDefenseIgnorePercentAdjustment);
            adjustment += GetRangedAttackDefenseIgnorePercentAdjustment(creature, skillType);

            var exposedOrSunderedSkillType = GetSkillTypeFromStat(
                Stat.GetStatAdjustment(creature, StatType.AbilityDefenseIgnoreExposedOrSunderedSkillType));
            if (SkillTypeMatches(skillType, exposedOrSunderedSkillType) &&
                GetIsObjectValid(defender) &&
                StatusEffect.HasStatusEffect(
                    defender,
                    typeof(ExposedStatusEffect),
                    typeof(ExposeWeakPointStatusEffect),
                    typeof(SunderStatusEffect)))
            {
                adjustment += Stat.GetStatAdjustment(creature, StatType.AbilityDefenseIgnoreExposedOrSunderedPercentAdjustment);
            }

            if (GetIsObjectValid(defender) &&
                StatusEffect.HasStatusEffect(
                    defender,
                    typeof(ForceDisruptionStatusEffect),
                    typeof(FoggyMindStatusEffect)))
            {
                adjustment += Stat.GetStatAdjustment(
                    creature,
                    StatType.AbilityDefenseIgnoreForceDisruptionOrFoggyMindPercentAdjustment);
            }

            return adjustment;
        }

        public static float GetAbilityRecastDelayFlatAdjustment(uint creature, PerkType perkType)
        {
            return GetTargetedAbilityAdjustment(
                creature,
                perkType,
                StatType.AbilityRecastDelayFlatAdjustmentPerkType,
                StatType.AbilityRecastDelayFlatAdjustmentPerkType,
                StatType.AbilityRecastDelayFlatAdjustment);
        }

        public static float ApplyAbilityRecastDelayModifiers(
            uint activator,
            AbilityDetail ability,
            float recastDelaySeconds)
        {
            if (!GetIsObjectValid(activator) || ability?.IsHostileAbility != true || recastDelaySeconds <= 0f)
                return recastDelaySeconds;

            var adjustment = Stat.GetStatAdjustment(activator, StatType.HostileAbilityRecastDelayPercentAdjustment);
            if (adjustment == 0)
                return recastDelaySeconds;

            return Math.Max(0f, recastDelaySeconds + recastDelaySeconds * (adjustment / 100f));
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
    }
}
