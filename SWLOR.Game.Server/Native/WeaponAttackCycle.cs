using NWN.Native.API;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Native
{
    /// <summary>
    /// Resolves both equipped melee weapons within the existing animation and delay gate.
    /// NWN chooses the off-hand once CurrentAttack reaches OnHandAttacks + AdditionalAttacks
    /// + BonusEffectAttacks. Merely increasing ResolveAttack's count does not select both hands.
    /// </summary>
    public static class WeaponAttackCycle
    {
        public static SkillType SelectTimingSkill(uint attacker, SkillType mainSkill, SkillType offSkill)
        {
            if (mainSkill == offSkill)
                return mainSkill;

            var mainNoDelay = StatusEffect.TryGetLimitedAttackNoDelay(attacker, mainSkill, out _) ||
                              Combat.HasTemporaryNextAutoAttackNoDelay(attacker, mainSkill);
            var offNoDelay = StatusEffect.TryGetLimitedAttackNoDelay(attacker, offSkill, out _) ||
                             Combat.HasTemporaryNextAutoAttackNoDelay(attacker, offSkill);
            if (mainNoDelay != offNoDelay)
                return offNoDelay ? offSkill : mainSkill;

            StatusEffect.TryGetLimitedAttackDelayReduction(attacker, mainSkill, out var mainReduction, out _);
            StatusEffect.TryGetLimitedAttackDelayReduction(attacker, offSkill, out var offReduction, out _);
            return offReduction > mainReduction ? offSkill : mainSkill;
        }

        public static int PrepareDualWieldAttacks(CNWSCombatRound round, int attacks)
        {
            attacks = Math.Clamp(attacks, 2, Combat.MaxAttacksPerSwing * 2);
            // GetAttack has 50 slots; leave the final slot available for the engine's
            // post-roll cursor. Ordinary rounds restart well before reaching this bound.
            attacks = Math.Min(attacks, 49 - round.m_nCurrentAttack);
            if (attacks < 2)
                return 0;

            // An odd final roll can result from a single remaining no-delay charge.
            round.m_nOnHandAttacks = round.m_nCurrentAttack + (attacks + 1) / 2;
            round.m_nOffHandAttacks = attacks / 2;
            round.m_nAdditionalAttacks = 0;
            round.m_nBonusEffectAttacks = 0;
            round.m_nOffHandAttacksTaken = 0;
            round.m_nExtraAttacksTaken = 0;
            return attacks;
        }
    }
}
