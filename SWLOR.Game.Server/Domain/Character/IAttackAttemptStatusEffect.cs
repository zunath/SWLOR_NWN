using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// Receives one notification when an originating hostile attack attempt finishes resolving.
    /// Multi-hit and area abilities still produce only one notification.
    /// </summary>
    public interface IAttackAttemptStatusEffect : IStatusEffect
    {
        void OnAttackAttemptedEffect(
            uint attacker,
            SkillType skillType,
            AbilityImpactSummary abilityImpact);
    }
}
