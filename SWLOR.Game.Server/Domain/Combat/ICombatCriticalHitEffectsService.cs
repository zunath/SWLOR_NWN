using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// What a critical hit sets off, the riders of a non-critical ability, and the forget of the critical sequence the combat
    /// cleanup makes.
    /// </summary>
    public interface ICombatCriticalHitEffectsService
    {
        void ApplyCriticalHitEffects(
            uint attacker,
            uint defender,
            int damage,
            int criticalRating,
            bool isSingleTargetImpact = false,
            SkillType skillType = SkillType.Invalid);

        void ApplyNonCriticalAbilityEffects(uint activator, uint target, SkillType skillType);

        void ForgetCriticalHitSequenceState(uint creature);
    }
}
