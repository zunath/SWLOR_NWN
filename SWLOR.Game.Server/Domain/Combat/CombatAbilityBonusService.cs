using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Domain.Abilities;
using static SWLOR.Game.Server.Domain.Combat.CombatRules;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The ability and attack bonuses of <c>Combat</c>: the one-shot next-ability and next-attack bonuses, and the flat,
    /// status, stamina, recast and defense-ignore adjustments an ability reads from its user's stats. It holds no state of
    /// its own: what it stores lives in the temporary stat modifiers. The opening auto-attack and the queued weapon
    /// ability snapshots are <see cref="CombatQueuedAbilityBonusService"/>.
    /// </summary>
    public class CombatAbilityBonusService : ICombatAbilityBonusService
    {
        private readonly ICombatQueriesService _queries;
        private readonly ICombatResourcesService _resources;
        private readonly ICombatActivityService _combatActivity;
        private readonly ICombatAttackTimingService _attackTiming;
        private readonly IStatService _stat;
        private readonly IStatusEffectService _statusEffect;
        private readonly ITemporaryStatModifierService _temporaryStatModifier;
        private readonly IEnmityChangeService _enmityChange;
        private readonly IDBService _db;

        public CombatAbilityBonusService(
            ICombatQueriesService queries,
            ICombatResourcesService resources,
            ICombatActivityService combatActivity,
            ICombatAttackTimingService attackTiming,
            IStatService stat,
            IStatusEffectService statusEffect,
            ITemporaryStatModifierService temporaryStatModifier,
            IEnmityChangeService enmityChange,
            IDBService db)
        {
            _queries = queries;
            _resources = resources;
            _combatActivity = combatActivity;
            _attackTiming = attackTiming;
            _stat = stat;
            _statusEffect = statusEffect;
            _temporaryStatModifier = temporaryStatModifier;
            _enmityChange = enmityChange;
            _db = db;
        }

        public bool HasNextAbilityNoStaminaCost(uint creature, SkillType skillType)
        {
            if (skillType == SkillType.Invalid)
                return false;

            var storedSkillType = GetSkillTypeFromStat(_temporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.NextAbilityNoStaminaCostSkillType,
                StatType.NextAbilityNoStaminaCostSkillType));

            return storedSkillType == skillType;
        }

        public bool ConsumeNextAbilityNoStaminaCost(uint creature, SkillType skillType)
        {
            if (!HasNextAbilityNoStaminaCost(creature, skillType))
                return false;

            _temporaryStatModifier.Consume(
                creature,
                StatType.NextAbilityNoStaminaCostSkillType,
                StatType.NextAbilityNoStaminaCostSkillType);

            return true;
        }

        public int GetNextSkillAbilityStaminaCostAdjustment(
            uint creature, SkillType skillType, bool isHostileAbility = false)
        {
            var adjustment = 0;
            foreach (var source in _temporaryStatModifier.GetStatSources(creature, StatType.NextSkillAbilityStaminaCostAdjustment))
            {
                if (MatchesStaminaDiscount(source, skillType, isHostileAbility))
                    adjustment += source[StatType.NextSkillAbilityStaminaCostAdjustment];
            }
            return adjustment;
        }

        public int ConsumeNextSkillAbilityStaminaCostAdjustment(
            uint creature, SkillType skillType, bool isHostileAbility = false)
        {
            var adjustment = 0;
            foreach (var source in _temporaryStatModifier.GetStatSources(creature, StatType.NextSkillAbilityStaminaCostAdjustment))
            {
                if (MatchesStaminaDiscount(source, skillType, isHostileAbility))
                    adjustment += _temporaryStatModifier.ConsumeSource(creature, StatType.NextSkillAbilityStaminaCostAdjustment, source);
            }
            return adjustment;
        }

        /// <param name="abilityStatusCategories">Status categories the ability applies; a bonus stored with
        /// a required category (Breaker Reversal: Control) waits for an ability that applies it.</param>
        public (int DamageBonus, int CriticalRatePercentAdjustment, int DefenseIgnorePercentAdjustment) ConsumeNextSkillAbilityBonuses(
            uint creature,
            SkillType skillType,
            StatusEffectCategory abilityStatusCategories = StatusEffectCategory.None)
        {
            if (skillType == SkillType.Invalid)
                return (0, 0, 0);

            var storedSkillType = GetSkillTypeFromStat(_temporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.NextSkillAbilitySkillType,
                StatType.NextSkillAbilitySkillType));
            if (!SkillTypeMatches(skillType, storedSkillType))
                return (0, 0, 0);

            var requiredCategory = GetStatusEffectCategoryFromStat(_temporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.NextSkillAbilityRequiredCategory,
                StatType.NextSkillAbilitySkillType));
            if (requiredCategory != StatusEffectCategory.None &&
                (abilityStatusCategories & requiredCategory) == 0)
            {
                return (0, 0, 0);
            }

            var damageBonus = _temporaryStatModifier.Consume(
                creature,
                StatType.NextSkillAbilityDamageBonus,
                StatType.NextSkillAbilitySkillType);
            var criticalRate = _temporaryStatModifier.Consume(
                creature,
                StatType.NextSkillAbilityCriticalRatePercentAdjustment,
                StatType.NextSkillAbilitySkillType);
            var defenseIgnore = _temporaryStatModifier.Consume(
                creature,
                StatType.NextSkillAbilityDefenseIgnorePercentAdjustment,
                StatType.NextSkillAbilitySkillType);
            _temporaryStatModifier.Consume(
                creature,
                StatType.NextSkillAbilityRequiredCategory,
                StatType.NextSkillAbilitySkillType);
            _temporaryStatModifier.Consume(
                creature,
                StatType.NextSkillAbilitySkillType,
                StatType.NextSkillAbilitySkillType);

            return (damageBonus, criticalRate, defenseIgnore);
        }

        public int ConsumePersistentNextSkillAbilityCriticalRateBonus(uint creature, SkillType skillType)
        {
            if (!PersistentNextSkillAbilityCriticalRateMatches(creature, skillType))
                return 0;

            var criticalRate = _temporaryStatModifier.Consume(
                creature,
                StatType.PersistentNextSkillAbilityCriticalRatePercentAdjustment,
                StatType.PersistentNextSkillAbilityCriticalRateSkillType);
            _temporaryStatModifier.Consume(
                creature,
                StatType.PersistentNextSkillAbilityCriticalRateSkillType,
                StatType.PersistentNextSkillAbilityCriticalRateSkillType);
            _temporaryStatModifier.Consume(
                creature,
                StatType.PersistentNextSkillAbilityCriticalRateRangedOnly,
                StatType.PersistentNextSkillAbilityCriticalRateSkillType);
            _statusEffect.RemoveStatusEffect(creature, typeof(CriticalRateStackTrackerStatusEffect), false);
            return criticalRate;
        }

        public int GetPersistentNextSkillAbilityCriticalRateBonus(uint creature, SkillType skillType)
        {
            return PersistentNextSkillAbilityCriticalRateMatches(creature, skillType)
                ? _temporaryStatModifier.GetStatAdjustment(
                    creature,
                    StatType.PersistentNextSkillAbilityCriticalRatePercentAdjustment,
                    StatType.PersistentNextSkillAbilityCriticalRateSkillType)
                : 0;
        }

        private bool PersistentNextSkillAbilityCriticalRateMatches(uint creature, SkillType skillType)
        {
            var storedSkillType = GetSkillTypeFromStat(_temporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.PersistentNextSkillAbilityCriticalRateSkillType,
                StatType.PersistentNextSkillAbilityCriticalRateSkillType));
            var rangedOnly = _temporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.PersistentNextSkillAbilityCriticalRateRangedOnly,
                StatType.PersistentNextSkillAbilityCriticalRateSkillType) > 0;
            return MatchesNextSkillAbilityCriticalRateSkill(skillType, storedSkillType, rangedOnly);
        }

        public (int DMGBonus, int CriticalRatePercentAdjustment, int EnmityBonus) ConsumeNextAttackGuardedHitBonuses(
            uint creature)
        {
            var attackBonuses = ConsumeNextAttackGuardedHitAutoAttackBonuses(creature);
            var criticalRate = ConsumeNextAttackGuardedHitCriticalRateBonus(creature);

            return (attackBonuses.DMGBonus, criticalRate, attackBonuses.EnmityBonus);
        }

        public (int DMGBonus, int EnmityBonus) ConsumeNextAttackGuardedHitAutoAttackBonuses(uint creature)
        {
            var dmgBonus = _temporaryStatModifier.Consume(
                creature,
                StatType.NextAttackGuardedHitDMGBonus,
                StatType.NextAttackGuardedHitDMGBonus);
            var enmityBonus = _temporaryStatModifier.Consume(
                creature,
                StatType.NextAttackGuardedHitEnmityBonus,
                StatType.NextAttackGuardedHitDMGBonus);

            return (dmgBonus, enmityBonus);
        }

        public int ConsumeNextAttackGuardedHitCriticalRateBonus(uint creature)
        {
            return _temporaryStatModifier.Consume(
                creature,
                StatType.NextAttackGuardedHitCriticalRatePercentAdjustment,
                StatType.NextAttackGuardedHitDMGBonus);
        }

        public int ConsumeStatusAppliedNextAttackDamageBonus(uint creature)
        {
            return _temporaryStatModifier.Consume(
                creature,
                StatType.NextAttackStatusAppliedDMGBonus,
                StatType.NextAttackStatusAppliedDMGBonus);
        }

        public int GetStatusAppliedNextAttackDamageBonus(uint creature)
        {
            return _temporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.NextAttackStatusAppliedDMGBonus,
                StatType.NextAttackStatusAppliedDMGBonus);
        }

        public void ApplyNextAttackGuardedHitEnmityBonus(
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

            _enmityChange.ModifyEnmity(attacker, defender, enmityBonus);
        }

        public void GrantNextAbilityDamageBonus(uint creature, int perkTypeValue, int bonus, int durationSeconds)
        {
            var perkType = GetPerkTypeFromStat(perkTypeValue);
            _attackTiming.GrantNextAbilityDamageBonus(creature, perkType, bonus, durationSeconds);
        }

        public void GrantNextSkillAbilityBonuses(
            uint creature,
            int skillTypeValue,
            int damageBonus,
            int criticalRatePercentAdjustment,
            int durationSeconds)
        {
            var skillType = GetSkillTypeFromStat(skillTypeValue);
            _attackTiming.GrantNextSkillAbilityBonuses(creature, skillType, damageBonus, criticalRatePercentAdjustment, durationSeconds);
        }

        public int ConsumeNextAbilityDamageBonus(uint creature, PerkType perkType)
        {
            if (perkType == PerkType.Invalid)
                return 0;

            return _temporaryStatModifier.Consume(
                creature,
                StatType.NextAbilityDamageBonus,
                GetPerkTypeGroup(perkType));
        }

        public int GetNextAbilityStaminaCostAdjustment(uint creature, PerkType perkType)
        {
            if (perkType == PerkType.Invalid)
                return 0;

            return _temporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.NextAbilityStaminaCostAdjustment,
                GetPerkTypeGroup(perkType));
        }

        public int ConsumeNextAbilityStaminaCostAdjustment(uint creature, PerkType perkType)
        {
            if (perkType == PerkType.Invalid)
                return 0;

            return _temporaryStatModifier.Consume(
                creature,
                StatType.NextAbilityStaminaCostAdjustment,
                GetPerkTypeGroup(perkType));
        }

        public int GetAbilityDamageFlatAdjustment(uint creature, PerkType perkType, SkillType skillType)
        {
            var adjustment = _queries.GetTargetedAbilityAdjustment(
                creature,
                perkType,
                StatType.AbilityDamageFlatAdjustmentPerkType,
                StatType.AbilityDamageFlatAdjustmentSecondaryPerkType,
                StatType.AbilityDamageFlatAdjustment);

            adjustment += _queries.GetRangedAttackDamageFlatAdjustment(creature, skillType);

            return adjustment;
        }

        public int GetAbilityStatusCategoryDamageBonus(
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

        public int GetAbilityStatusCategoryHitChancePercentAdjustment(
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

        private int GetAbilityStatusCategoryAdjustment(
            uint creature,
            SkillType skillType,
            StatusEffectCategory appliedCategories,
            StatType adjustmentStatType)
        {
            if (!GetIsObjectValid(creature) || skillType == SkillType.Invalid || appliedCategories == StatusEffectCategory.None)
                return 0;

            var requiredSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                creature,
                StatType.AbilityStatusCategoryBonusSkillType));
            if (!SkillTypeMatches(skillType, requiredSkillType))
                return 0;

            var requiredCategories = GetStatusEffectCategoryFromStat(_stat.GetStatAdjustment(
                creature,
                StatType.AbilityStatusCategoryBonusRequiredCategory));
            if (requiredCategories == StatusEffectCategory.None ||
                (appliedCategories & requiredCategories) == 0)
            {
                return 0;
            }

            return _stat.GetStatAdjustment(creature, adjustmentStatType);
        }

        public int GetAbilityStaminaCostFlatAdjustment(uint creature, PerkType perkType)
        {
            return _queries.GetTargetedAbilityAdjustment(
                creature,
                perkType,
                StatType.AbilityStaminaCostFlatAdjustmentPerkType,
                StatType.AbilityStaminaCostFlatAdjustmentSecondaryPerkType,
                StatType.AbilityStaminaCostFlatAdjustment);
        }

        public int GetAbilityStaminaCostFlatAdjustment(uint creature, AbilityDetail ability)
        {
            if (ability == null)
                return 0;

            var skillType = _queries.GetAbilitySkillType(creature, ability);
            var adjustment = GetAbilityStaminaCostFlatAdjustment(creature, ability.EffectiveLevelPerkType);
            if (ability.IsHostileAbility)
            {
                adjustment += _stat.GetStatAdjustment(creature, StatType.HostileAbilityStaminaCostFlatAdjustment);
                adjustment += _resources.GetNonCriticalRangedAbilityStaminaCostFlatAdjustment(creature, ability);
            }

            var flatSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                creature,
                StatType.SkillAbilityStaminaCostFlatAdjustmentSkillType));
            if (SkillTypeMatches(skillType, flatSkillType))
            {
                adjustment += _stat.GetStatAdjustment(creature, StatType.SkillAbilityStaminaCostFlatAdjustment);
            }

            var highResourceSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                creature,
                StatType.HighResourceAbilityStaminaCostSkillType));
            var threshold = _stat.GetStatAdjustment(creature, StatType.HighResourceAbilityStaminaCostThresholdPercent);
            if (threshold > 0 &&
                SkillTypeMatches(skillType, highResourceSkillType) &&
                _resources.IsCurrentFPAtOrAbovePercent(creature, threshold))
            {
                adjustment += _stat.GetStatAdjustment(creature, StatType.HighResourceAbilityStaminaCostAdjustment);
            }

            return adjustment;
        }

        public int GetAbilityStatusDurationPercentAdjustment(
            uint creature,
            PerkType perkType,
            SkillType skillType,
            Type primaryStatusEffect,
            IEnumerable<Type> additionalStatusEffects,
            Func<IStatusEffect> statusEffectFactory)
        {
            var adjustment = _queries.GetTargetedAbilityAdjustment(
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

        private int GetIdleStatusDurationPercentAdjustment(
            uint creature,
            SkillType skillType,
            Type primaryStatusEffect,
            IEnumerable<Type> additionalStatusEffects,
            Func<IStatusEffect> statusEffectFactory)
        {
            var requiredSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(
                creature,
                StatType.IdleStatusDurationPercentAdjustmentSkillType));
            if (!SkillTypeMatches(skillType, requiredSkillType))
                return 0;

            var requiredIdleSeconds = _stat.GetStatAdjustment(creature, StatType.IdleStatusDurationRequiredIdleSeconds);
            if (requiredIdleSeconds <= 0 || _combatActivity.HasRecentAttackActivity(creature, requiredIdleSeconds))
                return 0;

            var requiredCategory = GetStatusEffectCategoryFromStat(_stat.GetStatAdjustment(
                creature,
                StatType.IdleStatusDurationRequiredCategory));
            if (requiredCategory != 0 &&
                !StatusContextHasCategory(primaryStatusEffect, additionalStatusEffects, statusEffectFactory, requiredCategory))
            {
                return 0;
            }

            return _stat.GetStatAdjustment(creature, StatType.IdleStatusDurationPercentAdjustment);
        }

        private bool StatusContextHasCategory(
            Type primaryStatusEffect,
            IEnumerable<Type> additionalStatusEffects,
            Func<IStatusEffect> statusEffectFactory,
            StatusEffectCategory category)
        {
            if (_queries.AbilityAppliedAnyStatusCategory(primaryStatusEffect, additionalStatusEffects, category))
                return true;

            var statusEffect = statusEffectFactory?.Invoke();
            return statusEffect != null && (statusEffect.Categories & category) != 0;
        }

        public int GetAbilityDefenseIgnorePercentAdjustment(uint creature, PerkType perkType, SkillType skillType, uint defender)
        {
            var adjustment = _queries.GetTargetedAbilityAdjustment(
                creature,
                perkType,
                StatType.AbilityDefenseIgnorePercentAdjustmentPerkType,
                StatType.AbilityDefenseIgnorePercentAdjustmentSecondaryPerkType,
                StatType.AbilityDefenseIgnorePercentAdjustment);
            adjustment += _queries.GetRangedAttackDefenseIgnorePercentAdjustment(creature, skillType);

            var exposedOrSunderedSkillType = GetSkillTypeFromStat(
                _stat.GetStatAdjustment(creature, StatType.AbilityDefenseIgnoreExposedOrSunderedSkillType));
            if (SkillTypeMatches(skillType, exposedOrSunderedSkillType) &&
                GetIsObjectValid(defender) &&
                _statusEffect.HasStatusEffect(
                    defender,
                    typeof(ExposedStatusEffect),
                    typeof(ExposeWeakPointStatusEffect),
                    typeof(SunderStatusEffect)))
            {
                adjustment += _stat.GetStatAdjustment(creature, StatType.AbilityDefenseIgnoreExposedOrSunderedPercentAdjustment);
            }

            if (GetIsObjectValid(defender) &&
                _statusEffect.HasStatusEffect(
                    defender,
                    typeof(ForceDisruptionStatusEffect),
                    typeof(FoggyMindStatusEffect)))
            {
                adjustment += _stat.GetStatAdjustment(
                    creature,
                    StatType.AbilityDefenseIgnoreForceDisruptionOrFoggyMindPercentAdjustment);
            }

            return adjustment;
        }

        public float GetAbilityRecastDelayFlatAdjustment(uint creature, PerkType perkType)
        {
            return _queries.GetTargetedAbilityAdjustment(
                creature,
                perkType,
                StatType.AbilityRecastDelayFlatAdjustmentPerkType,
                StatType.AbilityRecastDelayFlatAdjustmentPerkType,
                StatType.AbilityRecastDelayFlatAdjustment);
        }

        public float ApplyAbilityRecastDelayModifiers(
            uint activator,
            AbilityDetail ability,
            float recastDelaySeconds)
        {
            if (!GetIsObjectValid(activator) || ability?.IsHostileAbility != true || recastDelaySeconds <= 0f)
                return recastDelaySeconds;

            var adjustment = _stat.GetStatAdjustment(activator, StatType.HostileAbilityRecastDelayPercentAdjustment);
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

        public int GetAbilityDamageBonus(uint creature, SkillType skill)
        {
            var level = 0;
            if (!GetIsPC(creature))
            {
                var npcStats = _stat.GetNPCStats(creature);
                level = npcStats.Level;
            }
            else
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = _db.Get<Player>(playerId);

                var pcSkill = dbPlayer.Skills[skill];
                level = pcSkill.Rank;
            }


            return (int)(0.15f * level);
        }
    }
}
