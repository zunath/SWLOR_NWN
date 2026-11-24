using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The weapon family riders of an ability activation: the Heavy Vibroblade, Beast Mastery, Vibroknife, Pistol and Lightsaber
    /// riders that run for the skill the ability uses.
    /// </summary>
    public interface ICombatAbilityWeaponActivationService
    {
        void ApplyAbilityActivatedRiders(uint activator, uint target, AbilityDetail ability, SkillType skillType);
    }
}
