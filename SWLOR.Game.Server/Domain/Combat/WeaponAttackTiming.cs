using System.Linq;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;
using static SWLOR.Game.Server.Composition.GameServices;
using SWLOR.Game.Server.Composition;

namespace SWLOR.Game.Server.Domain.Combat
{
    public static class WeaponAttackTiming
    {
        public static SkillType GetTimingSkill(uint attacker)
        {
            var main = GetItemInSlot(InventorySlot.RightHand, attacker);
            if (!EquipmentPredicates.HasDualWield(attacker) || GetWeaponRanged(main))
                return GameServices.CombatQueries.GetEquippedWeaponSkillType(attacker);

            var off = EquipmentPredicates.GetOffhandAttackWeapon(attacker);
            return SelectTimingSkill(attacker, Skill.GetSkillTypeByBaseItem(GetBaseItemType(main)),
                Skill.GetSkillTypeByBaseItem(GetBaseItemType(off)));
        }

        public static SkillType SelectTimingSkill(uint attacker, SkillType mainSkill, SkillType offSkill)
        {
            if (mainSkill == offSkill)
                return mainSkill;

            var mainNoDelay = GameServices.CombatAttackTiming.HasNextAutoAttackNoDelay(attacker, mainSkill);
            var offNoDelay = GameServices.CombatAttackTiming.HasNextAutoAttackNoDelay(attacker, offSkill);
            if (mainNoDelay != offNoDelay)
                return offNoDelay ? offSkill : mainSkill;

            StatusEffect.TryGetLimitedAttackDelayReduction(attacker, mainSkill, out var mainReduction, out _);
            StatusEffect.TryGetLimitedAttackDelayReduction(attacker, offSkill, out var offReduction, out _);
            return offReduction > mainReduction ? offSkill : mainSkill;
        }

        public static LimitedAttackTimingBudget GetLimitedBudget(
            uint attacker, SkillType timingSkill, SkillType mainSkill, SkillType offSkill, bool noDelay)
        {
            if (GameServices.CombatAttackTiming.IsAttackDelayReductionSuppressed(attacker))
                return default;

            var effects = StatusEffect.GetCreatureStatusEffects(attacker).GetAllEffects();
            var budgets = noDelay
                ? effects.OfType<ILimitedAttackNoDelayStatusEffect>()
                    .Where(effect => effect.RemainingAttacks > 0 && effect.AppliesToSkill(timingSkill))
                    .Select(effect => new LimitedAttackTimingBudget(effect.RemainingAttacks,
                        effect.AppliesToSkill(mainSkill), offSkill != SkillType.Invalid && effect.AppliesToSkill(offSkill)))
                : effects.OfType<ILimitedAttackDelayReductionStatusEffect>()
                    .Where(effect => effect.RemainingAttacks > 0 && effect.AttackDelayReductionPercent > 0 && effect.AppliesToSkill(timingSkill))
                    .Select(effect => new LimitedAttackTimingBudget(effect.RemainingAttacks,
                        effect.AppliesToSkill(mainSkill), offSkill != SkillType.Invalid && effect.AppliesToSkill(offSkill)));
            var attacksPerCycle = offSkill == SkillType.Invalid ? 1 : 2;
            return budgets.OrderBy(budget => budget.RollLimit(attacksPerCycle)).FirstOrDefault();
        }

        public static LimitedAttackTimingBudget GetTemporaryNoDelayBudget(
            uint attacker, SkillType timingSkill, SkillType mainSkill, SkillType offSkill)
        {
            if (!GameServices.CombatAttackTiming.HasTemporaryNextAutoAttackNoDelay(attacker, timingSkill))
                return default;

            return new LimitedAttackTimingBudget(1,
                GameServices.CombatAttackTiming.HasTemporaryNextAutoAttackNoDelay(attacker, mainSkill),
                offSkill != SkillType.Invalid && GameServices.CombatAttackTiming.HasTemporaryNextAutoAttackNoDelay(attacker, offSkill));
        }
    }
}
