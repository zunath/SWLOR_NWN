using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The damage bonuses an ability hit reads before its damage: the base bonus, the high resource bonus and the costly ability bonus.
    /// </summary>
    public interface ICombatAbilityDamageBonusService
    {
        int GetAbilityImpactBaseDamageBonus(
            uint activator,
            uint target,
            AbilityDetail ability,
            SkillType skillType);

        int GetHighResourceAbilityDamageBonus(uint activator, AbilityDetail ability = null);

        int GetCostlyAbilityDamageBonus(
            uint activator,
            AbilityDetail ability,
            SkillType skillType);
    }
}
