using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// Describes an attack-delay reduction that expires after a limited number of matching
    /// attack attempts. The native swing scheduler uses this contract to avoid scheduling
    /// more accelerated attacks than the effect can cover.
    /// </summary>
    public interface ILimitedAttackDelayReductionStatusEffect : IStatusEffect
    {
        int AttackDelayReductionPercent { get; }
        int RemainingAttacks { get; }
        bool AppliesToSkill(SkillType skillType);
    }
}
