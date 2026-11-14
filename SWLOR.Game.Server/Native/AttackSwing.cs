using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Native
{
    /// <summary>
    /// The skills, hand cycle and delays one native attack swing resolves with. The attack action works them out before
    /// it checks the swing delay and hands them to the steps that resolve the round.
    /// </summary>
    internal readonly record struct AttackSwing(
        SkillType AttackSkillType,
        SkillType MainSkill,
        SkillType OffSkill,
        bool IsDualWieldCycle,
        bool IsTwinPistolCycle,
        bool UseDefaultMinimumDelay,
        int EffectiveAttackDelay,
        int UnbuffedAttackDelay,
        int EffectiveDelayWithoutLimitedReduction,
        int LimitedDelayReductionRemainingAttacks,
        int LimitedNoDelayRemainingAttacks);
}
