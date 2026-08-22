using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.StatusEffectService
{
    /// <summary>
    /// Describes a no-delay attack effect that expires after a limited number of matching
    /// attempts. The native swing scheduler uses the remaining count to avoid pre-scheduling
    /// more accelerated attacks than the effect can cover.
    /// </summary>
    public interface ILimitedAttackNoDelayStatusEffect : IStatusEffect
    {
        int RemainingAttacks { get; }
        bool AppliesToSkill(SkillType skillType);
    }
}
