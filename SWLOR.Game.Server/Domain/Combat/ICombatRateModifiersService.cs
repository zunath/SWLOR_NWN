using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// Critical rate and positional rate modifiers for combat: the auto-attack and target-status critical rate, the
    /// auto-attack cycle critical tracker, and the side and back attack adjustments to damage, hit chance, critical rate
    /// and evasion.
    /// </summary>
    public interface ICombatRateModifiersService : ICombatCreatureStateOwner
    {
        int GetAutoAttackCriticalRateAdjustment(
            uint attacker,
            uint defender,
            int cycleCriticalRateAdjustment);

        int GetTargetStatusCriticalRateAdjustment(uint attacker, uint defender);

        int PrepareAutoAttackCycleCriticalRate(uint attacker, SkillType skillType);

        void ClearAutoAttackCycleCriticalRateTracker(uint creature);

        /// <summary>Forgets the creature's auto-attack cycle count, without touching the tracker status effect.</summary>
        void ForgetAutoAttackCycleCriticalRateCount(uint creature);

        int ApplySideAttackDamageModifier(uint attacker, uint defender, SkillType skillType, int damage);

        int GetSideAttackHitChanceAdjustment(uint attacker, uint defender, SkillType skillType);

        int GetSideAttackCriticalRateAdjustment(uint attacker, uint defender, SkillType skillType);

        int ApplyBackAttackDamageModifier(uint attacker, uint defender, SkillType skillType, int damage);

        int GetBackAttackCriticalRateAdjustment(uint attacker, uint defender, SkillType skillType);

        int ApplySideAttackEvasionIgnore(uint attacker, uint defender, SkillType skillType, int evasion);
    }
}
