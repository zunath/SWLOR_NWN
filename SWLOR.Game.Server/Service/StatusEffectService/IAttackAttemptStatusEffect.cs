using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.StatusEffectService
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
