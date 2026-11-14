using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Character
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
