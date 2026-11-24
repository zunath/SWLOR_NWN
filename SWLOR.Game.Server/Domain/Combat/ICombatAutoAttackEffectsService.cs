using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// What an auto-attack adds: the damage modifiers of each attack (with their resource restores, hamstring, status stacks and
    /// cycle damage), the suppressed bonuses, and the source status stacks an ability hit also applies.
    /// </summary>
    public interface ICombatAutoAttackEffectsService
    {
        int ApplyAutoAttackDamageModifiers(uint attacker, uint defender, int damage, SkillType skillType = SkillType.Invalid);

        void ConsumeSuppressedAutoAttackDamageBonuses(uint attacker, SkillType skillType);

        void ApplySourceStatusStackEffects(uint attacker, uint defender);
    }
}
