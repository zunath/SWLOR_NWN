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
    /// The queued ability bonuses of <c>Combat</c>: the opening auto-attack, the queued weapon ability snapshots and the
    /// idle activation bonuses, kept as temporary stat modifiers. It holds no state of its own.
    /// </summary>
    public class CombatQueuedAbilityBonusService : ICombatQueuedAbilityBonusService
    {
        private readonly ICombatQueriesService _queries;
        private readonly ICombatActivityService _combatActivity;
        private readonly IStatService _stat;
        private readonly IStatusEffectService _statusEffect;
        private readonly ITemporaryStatModifierService _temporaryStatModifier;
        private readonly ICombatAbilityBonusService _abilityBonus;

        public CombatQueuedAbilityBonusService(
            ICombatQueriesService queries,
            ICombatActivityService combatActivity,
            IStatService stat,
            IStatusEffectService statusEffect,
            ITemporaryStatModifierService temporaryStatModifier,
            ICombatAbilityBonusService abilityBonus)
        {
            _queries = queries;
            _combatActivity = combatActivity;
            _stat = stat;
            _statusEffect = statusEffect;
            _temporaryStatModifier = temporaryStatModifier;
            _abilityBonus = abilityBonus;
        }

        public int PrepareOpeningAutoAttack(uint attacker, SkillType skillType)
        {
            if (!GetIsObjectValid(attacker) || !IsWeaponSkillType(skillType))
                return 0;

            var requiredSkillType = GetSkillTypeFromStat(_stat.GetStatAdjustment(attacker, StatType.OpeningAutoAttackSkillType));
            if (!SkillTypeMatchesOrGlobal(skillType, requiredSkillType))
                return 0;

            var idleSeconds = _stat.GetStatAdjustment(attacker, StatType.OpeningAutoAttackIdleSeconds);
            if (idleSeconds <= 0)
                return 0;

            var lastActivity = _combatActivity.GetLastCompletedOffensiveActivityAt(attacker);
            if (lastActivity != default &&
                (Clock.UtcNow - lastActivity).TotalSeconds < idleSeconds)
                return 0;

            _combatActivity.TrackCombatActivity(attacker);
            _statusEffect.RemoveStatusEffect(attacker, typeof(OpeningAttackReadyStatusEffect), false);

            var damageBonus = _stat.GetStatAdjustment(attacker, StatType.OpeningAutoAttackDamageBonus);
            if (damageBonus != 0)
            {
                _temporaryStatModifier.Replace(
                    attacker,
                    StatType.CurrentAutoAttackDamageBonus,
                    damageBonus,
                    6,
                    StatType.CurrentAutoAttackDamageBonus);

            }

            var criticalDamageBonus = _stat.GetStatAdjustment(
                attacker,
                StatType.OpeningAutoAttackCriticalDamagePercentAdjustment);
            if (criticalDamageBonus != 0)
            {
                _temporaryStatModifier.Replace(
                    attacker,
                    StatType.CurrentAutoAttackCriticalDamagePercentAdjustment,
                    criticalDamageBonus,
                    6,
                    StatType.CurrentAutoAttackCriticalDamagePercentAdjustment);
            }

            return _stat.GetStatAdjustment(attacker, StatType.OpeningAutoAttackCriticalRatePercentAdjustment);
        }

        public void PrepareQueuedWeaponAbilityOpeningAttackAtActivation(uint attacker, SkillType skillType)
        {
            if (!GetIsObjectValid(attacker) || skillType == SkillType.Invalid)
                return;

            // Discard any stale per-swing payload before capturing this queued activation. The
            // opening condition is evaluated before the queued ability records its own activity.
            ClearQueuedWeaponAbilityAttemptBonuses(attacker);
            var criticalRate = PrepareOpeningAutoAttack(attacker, skillType);
            var damageBonus = _temporaryStatModifier.Consume(
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

        public int ConsumeOpeningAutoAttackCriticalDamageAdjustment(uint attacker)
        {
            return _temporaryStatModifier.Consume(
                attacker,
                StatType.CurrentAutoAttackCriticalDamagePercentAdjustment,
                StatType.CurrentAutoAttackCriticalDamagePercentAdjustment);
        }

        public void ClearOpeningAutoAttackModifiers(uint attacker)
        {
            _temporaryStatModifier.Consume(
                attacker,
                StatType.CurrentAutoAttackDamageBonus,
                StatType.CurrentAutoAttackDamageBonus);
            _temporaryStatModifier.Consume(
                attacker,
                StatType.CurrentAutoAttackCriticalDamagePercentAdjustment,
                StatType.CurrentAutoAttackCriticalDamagePercentAdjustment);
            ClearQueuedWeaponAbilityAttemptBonuses(attacker);
        }

        public void StoreQueuedWeaponAbilityCriticalRateBonus(
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

        private void StoreQueuedWeaponAbilityActivationOpeningBonuses(
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

            var storedSkillType = GetSkillTypeFromStat(_temporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType));
            if (SkillTypeMatches(skillType, storedSkillType))
            {
                damageBonus += _temporaryStatModifier.GetStatAdjustment(
                    creature,
                    StatType.QueuedWeaponAbilityIdleDamageBonus,
                    StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
                criticalRatePercentAdjustment += _temporaryStatModifier.GetStatAdjustment(
                    creature,
                    StatType.QueuedWeaponAbilityActivationCriticalRatePercentAdjustment,
                    StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
                criticalDamagePercentAdjustment = Math.Max(
                    criticalDamagePercentAdjustment,
                    _temporaryStatModifier.GetStatAdjustment(
                        creature,
                        StatType.QueuedWeaponAbilityIdleCriticalDamagePercentAdjustment,
                        StatType.QueuedWeaponAbilityActivationCriticalRateSkillType));
            }

            _temporaryStatModifier.Replace(
                creature,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType,
                (int)skillType,
                30,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
            _temporaryStatModifier.Replace(
                creature,
                StatType.QueuedWeaponAbilityIdleDamageBonus,
                damageBonus,
                30,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
            _temporaryStatModifier.Replace(
                creature,
                StatType.QueuedWeaponAbilityActivationCriticalRatePercentAdjustment,
                criticalRatePercentAdjustment,
                30,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
            _temporaryStatModifier.Replace(
                creature,
                StatType.QueuedWeaponAbilityIdleCriticalDamagePercentAdjustment,
                criticalDamagePercentAdjustment,
                30,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
        }

        private void StoreQueuedWeaponAbilityAttemptBonuses(
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

            var storedSkillType = GetSkillTypeFromStat(_temporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.QueuedWeaponAbilityCriticalRateSkillType,
                StatType.QueuedWeaponAbilityCriticalRateSkillType));
            if (SkillTypeMatches(skillType, storedSkillType))
            {
                damageBonus += _temporaryStatModifier.GetStatAdjustment(
                    creature,
                    StatType.QueuedWeaponAbilityDamageBonus,
                    StatType.QueuedWeaponAbilityCriticalRateSkillType);
                criticalRatePercentAdjustment += _temporaryStatModifier.GetStatAdjustment(
                    creature,
                    StatType.QueuedWeaponAbilityCriticalRatePercentAdjustment,
                    StatType.QueuedWeaponAbilityCriticalRateSkillType);
                criticalDamagePercentAdjustment += _temporaryStatModifier.GetStatAdjustment(
                    creature,
                    StatType.QueuedWeaponAbilityCriticalDamagePercentAdjustment,
                    StatType.QueuedWeaponAbilityCriticalRateSkillType);
            }

            _temporaryStatModifier.Replace(
                creature,
                StatType.QueuedWeaponAbilityCriticalRateSkillType,
                (int)skillType,
                6,
                StatType.QueuedWeaponAbilityCriticalRateSkillType);
            _temporaryStatModifier.Replace(
                creature,
                StatType.QueuedWeaponAbilityCriticalRatePercentAdjustment,
                criticalRatePercentAdjustment,
                6,
                StatType.QueuedWeaponAbilityCriticalRateSkillType);
            _temporaryStatModifier.Replace(
                creature,
                StatType.QueuedWeaponAbilityDamageBonus,
                damageBonus,
                6,
                StatType.QueuedWeaponAbilityCriticalRateSkillType);
            _temporaryStatModifier.Replace(
                creature,
                StatType.QueuedWeaponAbilityCriticalDamagePercentAdjustment,
                criticalDamagePercentAdjustment,
                6,
                StatType.QueuedWeaponAbilityCriticalRateSkillType);
        }

        public void StoreQueuedWeaponAbilityActivationCriticalRateBonus(
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

            _temporaryStatModifier.Replace(
                creature,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType,
                (int)skillType,
                30,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
            _temporaryStatModifier.Replace(
                creature,
                StatType.QueuedWeaponAbilityActivationCriticalRatePercentAdjustment,
                criticalRatePercentAdjustment,
                30,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
        }

        public void StoreQueuedWeaponAbilityActivationIdleBonuses(uint creature, SkillType skillType)
        {
            if (!GetIsObjectValid(creature) || skillType == SkillType.Invalid)
                return;

            var idleBonuses = _combatActivity.GetIdleSkillAbilityBonuses(creature, skillType);
            if (idleBonuses == default)
                return;

            _temporaryStatModifier.Replace(
                creature,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType,
                (int)skillType,
                30,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
            _temporaryStatModifier.Replace(
                creature,
                StatType.QueuedWeaponAbilityIdleDamageBonus,
                idleBonuses.DamageBonus,
                30,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
            _temporaryStatModifier.Replace(
                creature,
                StatType.QueuedWeaponAbilityIdleHitChancePercentAdjustment,
                idleBonuses.HitChancePercentAdjustment,
                30,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
            _temporaryStatModifier.Replace(
                creature,
                StatType.QueuedWeaponAbilityIdleCriticalDamagePercentAdjustment,
                idleBonuses.CriticalDamagePercentAdjustment,
                30,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
        }

        public void StoreWeaponAbilityActivationIdleBonuses(
            uint creature,
            bool isIdle,
            int damageBonus,
            int criticalRatePercentAdjustment,
            int defenseIgnorePercent)
        {
            ClearWeaponAbilityActivationIdleBonuses(creature);
            if (!GetIsObjectValid(creature))
                return;

            _temporaryStatModifier.Replace(creature, StatType.WeaponAbilityActivationIdleSnapshot, 1, 30,
                StatType.WeaponAbilityActivationIdleSnapshot);
            _temporaryStatModifier.Replace(creature, StatType.WeaponAbilityActivationIdleDamageBonus,
                isIdle ? damageBonus : 0, 30, StatType.WeaponAbilityActivationIdleSnapshot);
            _temporaryStatModifier.Replace(creature, StatType.WeaponAbilityActivationIdleCriticalRatePercentAdjustment,
                isIdle ? criticalRatePercentAdjustment : 0, 30, StatType.WeaponAbilityActivationIdleSnapshot);
            _temporaryStatModifier.Replace(creature, StatType.WeaponAbilityActivationIdleDefenseIgnorePercent,
                isIdle ? defenseIgnorePercent : 0, 30, StatType.WeaponAbilityActivationIdleSnapshot);
        }

        public bool HasWeaponAbilityActivationIdleSnapshot(uint creature)
        {
            return _temporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.WeaponAbilityActivationIdleSnapshot,
                StatType.WeaponAbilityActivationIdleSnapshot) > 0;
        }

        public int GetWeaponAbilityActivationIdleDamageBonus(uint creature) =>
            _temporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.WeaponAbilityActivationIdleDamageBonus,
                StatType.WeaponAbilityActivationIdleSnapshot);

        public int GetWeaponAbilityActivationIdleCriticalRateBonus(uint creature) =>
            _temporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.WeaponAbilityActivationIdleCriticalRatePercentAdjustment,
                StatType.WeaponAbilityActivationIdleSnapshot);

        public int GetWeaponAbilityActivationIdleDefenseIgnorePercent(uint creature) =>
            _temporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.WeaponAbilityActivationIdleDefenseIgnorePercent,
                StatType.WeaponAbilityActivationIdleSnapshot);

        public void ClearWeaponAbilityActivationIdleBonuses(uint creature)
        {
            _temporaryStatModifier.Consume(creature, StatType.WeaponAbilityActivationIdleDamageBonus,
                StatType.WeaponAbilityActivationIdleSnapshot);
            _temporaryStatModifier.Consume(creature, StatType.WeaponAbilityActivationIdleCriticalRatePercentAdjustment,
                StatType.WeaponAbilityActivationIdleSnapshot);
            _temporaryStatModifier.Consume(creature, StatType.WeaponAbilityActivationIdleDefenseIgnorePercent,
                StatType.WeaponAbilityActivationIdleSnapshot);
            _temporaryStatModifier.Consume(creature, StatType.WeaponAbilityActivationIdleSnapshot,
                StatType.WeaponAbilityActivationIdleSnapshot);
        }

        /// <param name="ability">The queued ability. Its status categories add category accuracy
        /// (Scrapper Control: "your control abilities gain Ability Accuracy"), which ability rolls get
        /// in ApplyCombatImpact but the native weapon roll would otherwise miss.</param>
        public int GetQueuedWeaponAbilityActivationHitChanceAdjustment(
            uint creature,
            SkillType skillType,
            AbilityDetail ability = null)
        {
            var activationSkillType = GetSkillTypeFromStat(_temporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType));
            var idleHitChanceAdjustment = SkillTypeMatches(skillType, activationSkillType)
                ? _temporaryStatModifier.GetStatAdjustment(
                    creature,
                    StatType.QueuedWeaponAbilityIdleHitChancePercentAdjustment,
                    StatType.QueuedWeaponAbilityActivationCriticalRateSkillType)
                : 0;

            var statusCategoryHitChanceAdjustment = _abilityBonus.GetAbilityStatusCategoryHitChancePercentAdjustment(
                creature,
                skillType,
                ability?.ResolveImpactStatusCategories?.Invoke(creature) ?? StatusEffectCategory.None);

            // Queued abilities use the native weapon roll instead of TryResolveAbilityHit.
            return _queries.GetPhysicalAndForceAbilityHitChanceAdjustment(creature, skillType) + idleHitChanceAdjustment +
                   statusCategoryHitChanceAdjustment;
        }

        public void ClearQueuedWeaponAbilityActivationBonuses(uint creature)
        {
            _temporaryStatModifier.Consume(
                creature,
                StatType.QueuedWeaponAbilityIdleDamageBonus,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
            _temporaryStatModifier.Consume(
                creature,
                StatType.QueuedWeaponAbilityIdleHitChancePercentAdjustment,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
            _temporaryStatModifier.Consume(
                creature,
                StatType.QueuedWeaponAbilityIdleCriticalDamagePercentAdjustment,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
            _temporaryStatModifier.Consume(
                creature,
                StatType.QueuedWeaponAbilityActivationCriticalRatePercentAdjustment,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
            _temporaryStatModifier.Consume(
                creature,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
        }

        public (
            int DamageBonus,
            int CriticalRatePercentAdjustment,
            int CriticalDamagePercentAdjustment) ConsumeQueuedWeaponAbilityBonuses(
                uint creature,
                SkillType skillType)
        {
            var damageBonus = 0;
            var criticalRate = 0;
            var criticalDamage = 0;
            var storedSkillType = GetSkillTypeFromStat(_temporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.QueuedWeaponAbilityCriticalRateSkillType,
                StatType.QueuedWeaponAbilityCriticalRateSkillType));
            if (SkillTypeMatches(skillType, storedSkillType))
            {
                damageBonus = _temporaryStatModifier.Consume(
                    creature,
                    StatType.QueuedWeaponAbilityDamageBonus,
                    StatType.QueuedWeaponAbilityCriticalRateSkillType);
                criticalRate += _temporaryStatModifier.Consume(
                    creature,
                    StatType.QueuedWeaponAbilityCriticalRatePercentAdjustment,
                    StatType.QueuedWeaponAbilityCriticalRateSkillType);
                criticalDamage = _temporaryStatModifier.Consume(
                    creature,
                    StatType.QueuedWeaponAbilityCriticalDamagePercentAdjustment,
                    StatType.QueuedWeaponAbilityCriticalRateSkillType);
                _temporaryStatModifier.Consume(
                    creature,
                    StatType.QueuedWeaponAbilityCriticalRateSkillType,
                    StatType.QueuedWeaponAbilityCriticalRateSkillType);
            }

            var activationSkillType = GetSkillTypeFromStat(_temporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType,
                StatType.QueuedWeaponAbilityActivationCriticalRateSkillType));
            if (SkillTypeMatches(skillType, activationSkillType))
            {
                damageBonus += _temporaryStatModifier.Consume(
                    creature,
                    StatType.QueuedWeaponAbilityIdleDamageBonus,
                    StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
                criticalRate += _temporaryStatModifier.Consume(
                    creature,
                    StatType.QueuedWeaponAbilityActivationCriticalRatePercentAdjustment,
                    StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
                _temporaryStatModifier.Consume(
                    creature,
                    StatType.QueuedWeaponAbilityIdleHitChancePercentAdjustment,
                    StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
                criticalDamage = Math.Max(criticalDamage, _temporaryStatModifier.Consume(
                    creature,
                    StatType.QueuedWeaponAbilityIdleCriticalDamagePercentAdjustment,
                    StatType.QueuedWeaponAbilityActivationCriticalRateSkillType));
                _temporaryStatModifier.Consume(
                    creature,
                    StatType.QueuedWeaponAbilityActivationCriticalRateSkillType,
                    StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
            }

            return (damageBonus, criticalRate, criticalDamage);
        }

        public void ClearQueuedWeaponAbilityAttemptBonuses(uint creature)
        {
            _temporaryStatModifier.Consume(
                creature,
                StatType.QueuedWeaponAbilityDamageBonus,
                StatType.QueuedWeaponAbilityCriticalRateSkillType);
            _temporaryStatModifier.Consume(
                creature,
                StatType.QueuedWeaponAbilityCriticalRatePercentAdjustment,
                StatType.QueuedWeaponAbilityCriticalRateSkillType);
            _temporaryStatModifier.Consume(
                creature,
                StatType.QueuedWeaponAbilityCriticalDamagePercentAdjustment,
                StatType.QueuedWeaponAbilityCriticalRateSkillType);
            _temporaryStatModifier.Consume(
                creature,
                StatType.QueuedWeaponAbilityCriticalRateSkillType,
                StatType.QueuedWeaponAbilityCriticalRateSkillType);
        }
    }
}
