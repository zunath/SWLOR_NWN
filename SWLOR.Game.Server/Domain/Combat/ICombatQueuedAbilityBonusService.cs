using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// Opening auto-attack, queued weapon ability and idle activation bonuses for combat: what an ability queued behind an
    /// auto-attack stores, reads and consumes.
    /// </summary>
    public interface ICombatQueuedAbilityBonusService
    {
        int PrepareOpeningAutoAttack(uint attacker, SkillType skillType);

        void PrepareQueuedWeaponAbilityOpeningAttackAtActivation(uint attacker, SkillType skillType);

        int ConsumeOpeningAutoAttackCriticalDamageAdjustment(uint attacker);

        void ClearOpeningAutoAttackModifiers(uint attacker);

        void StoreQueuedWeaponAbilityCriticalRateBonus(
            uint creature,
            SkillType skillType,
            int criticalRatePercentAdjustment);

        void StoreQueuedWeaponAbilityActivationCriticalRateBonus(
            uint creature,
            SkillType skillType,
            int criticalRatePercentAdjustment);

        void StoreQueuedWeaponAbilityActivationIdleBonuses(uint creature, SkillType skillType);

        void StoreWeaponAbilityActivationIdleBonuses(
            uint creature,
            bool isIdle,
            int damageBonus,
            int criticalRatePercentAdjustment,
            int defenseIgnorePercent);

        bool HasWeaponAbilityActivationIdleSnapshot(uint creature);

        int GetWeaponAbilityActivationIdleDamageBonus(uint creature);

        int GetWeaponAbilityActivationIdleCriticalRateBonus(uint creature);

        int GetWeaponAbilityActivationIdleDefenseIgnorePercent(uint creature);

        void ClearWeaponAbilityActivationIdleBonuses(uint creature);

        /// <param name="ability">The queued ability. Its status categories add category accuracy
        /// (Scrapper Control: "your control abilities gain Ability Accuracy"), which ability rolls get
        /// in ApplyCombatImpact but the native weapon roll would otherwise miss.</param>
        int GetQueuedWeaponAbilityActivationHitChanceAdjustment(
            uint creature,
            SkillType skillType,
            AbilityDetail ability = null);

        void ClearQueuedWeaponAbilityActivationBonuses(uint creature);

        (int DamageBonus, int CriticalRatePercentAdjustment, int CriticalDamagePercentAdjustment) ConsumeQueuedWeaponAbilityBonuses(
            uint creature,
            SkillType skillType);

        void ClearQueuedWeaponAbilityAttemptBonuses(uint creature);
    }
}
