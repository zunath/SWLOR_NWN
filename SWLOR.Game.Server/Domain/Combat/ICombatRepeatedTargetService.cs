using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// Repeated-target and same-target pressure state and effects for combat: the three repeated-target damage stacks,
    /// Spotter's Rhythm, and the per-attacker tables behind them.
    /// </summary>
    public interface ICombatRepeatedTargetService : ICombatCreatureStateOwner
    {
        int ApplyRepeatedTargetDamageModifier(
            uint attacker,
            uint defender,
            SkillType skillType,
            int damage,
            bool isAbilityDamage,
            out int flatBonus);

        int ApplyMeleeRepeatedTargetDamageModifier(
            uint attacker,
            uint defender,
            SkillType skillType,
            int damage,
            bool isAbilityDamage);

        int ApplyRangedRepeatedTargetDamageModifier(
            uint attacker,
            uint defender,
            SkillType skillType,
            int damage);

        void ClearRangedRepeatedTargetDamageTracker(uint creature);

        void ApplySameTargetPressureDamageEffects(uint attacker, uint defender, SkillType skillType);

        int GetSameTargetPressureWeaponAbilityDamageBonus(
            uint attacker,
            uint target,
            SkillType skillType);

        void ConsumeSameTargetPressureWeaponAbilityDamageBonus(
            uint attacker,
            uint target,
            SkillType skillType,
            int damage);

        /// <summary>Forgets every repeated-target and pressure state of the creature.</summary>
        void ForgetDamageModifierState(uint creature);
    }
}
