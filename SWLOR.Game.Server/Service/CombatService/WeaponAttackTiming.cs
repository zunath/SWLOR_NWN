using System.Linq;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.CombatService
{
    public static class WeaponAttackTiming
    {
        public static SkillType GetTimingSkill(uint attacker)
        {
            var main = GetItemInSlot(InventorySlot.RightHand, attacker);
            if (!EquipmentPredicates.HasDualWield(attacker) || GetWeaponRanged(main))
                return Combat.GetEquippedWeaponSkillType(attacker);

            var off = GetItemInSlot(InventorySlot.LeftHand, attacker);
            return SelectTimingSkill(attacker, Skill.GetSkillTypeByBaseItem(GetBaseItemType(main)),
                Skill.GetSkillTypeByBaseItem(GetBaseItemType(off)));
        }

        public static SkillType SelectTimingSkill(uint attacker, SkillType mainSkill, SkillType offSkill)
        {
            if (mainSkill == offSkill)
                return mainSkill;

            var mainNoDelay = Combat.HasNextAutoAttackNoDelay(attacker, mainSkill);
            var offNoDelay = Combat.HasNextAutoAttackNoDelay(attacker, offSkill);
            if (mainNoDelay != offNoDelay)
                return offNoDelay ? offSkill : mainSkill;

            StatusEffect.TryGetLimitedAttackDelayReduction(attacker, mainSkill, out var mainReduction, out _);
            StatusEffect.TryGetLimitedAttackDelayReduction(attacker, offSkill, out var offReduction, out _);
            return offReduction > mainReduction ? offSkill : mainSkill;
        }

        public static LimitedAttackTimingBudget GetLimitedBudget(
            uint attacker, SkillType timingSkill, SkillType mainSkill, SkillType offSkill, bool noDelay)
        {
            if (Combat.IsAttackDelayReductionSuppressed(attacker))
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
            if (!Combat.HasTemporaryNextAutoAttackNoDelay(attacker, timingSkill))
                return default;

            return new LimitedAttackTimingBudget(1,
                Combat.HasTemporaryNextAutoAttackNoDelay(attacker, mainSkill),
                offSkill != SkillType.Invalid && Combat.HasTemporaryNextAutoAttackNoDelay(attacker, offSkill));
        }
    }
}
