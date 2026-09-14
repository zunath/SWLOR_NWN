using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.StatusEffectService
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
