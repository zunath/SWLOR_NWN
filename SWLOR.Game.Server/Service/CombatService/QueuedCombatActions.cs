using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;

namespace SWLOR.Game.Server.Service.CombatService
{
    public static class QueuedCombatActions
    {
        public static bool CanConsumeNextAbilityNoDelay(AbilityDetail ability)
        {
            return ability?.IsHostileAbility == true;
        }

        public static bool ConsumeNextAbilityNoDelay(uint creature, AbilityDetail ability)
        {
            if (!QueuedCombatActions.CanConsumeNextAbilityNoDelay(ability))
                return false;

            var skillType = QueuedCombatActions.GetAbilitySkillType(creature, ability);
            return QueuedCombatActions.ConsumeNextAbilityNoDelay(creature, skillType);
        }

        internal static bool ConsumeNextAbilityNoDelay(uint creature, SkillType skillType)
        {
            if (skillType == SkillType.Invalid)
                return false;

            var storedSkillType = AbilityImpactEffects.GetSkillTypeFromStat(TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.NextAttackNoDelay,
                StatType.NextAttackNoDelay));
            if (storedSkillType != skillType)
                return false;

            TemporaryStatModifier.Consume(
                creature,
                StatType.NextAttackNoDelay,
                StatType.NextAttackNoDelay);

            return true;
        }

        public static bool HasNextAutoAttackNoDelay(uint creature, SkillType skillType)
        {
            if (skillType == SkillType.Invalid)
                return false;

            var storedSkillType = AbilityImpactEffects.GetSkillTypeFromStat(TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.NextAutoAttackNoDelaySkillType,
                StatType.NextAutoAttackNoDelaySkillType));

            return storedSkillType == skillType;
        }

        public static bool ConsumeNextAutoAttackNoDelay(uint creature, SkillType skillType)
        {
            if (!QueuedCombatActions.HasNextAutoAttackNoDelay(creature, skillType))
                return false;

            TemporaryStatModifier.Consume(
                creature,
                StatType.NextAutoAttackNoDelaySkillType,
                StatType.NextAutoAttackNoDelaySkillType);

            return true;
        }

        public static int ConsumeNextAutoAttackCriticalRateBonus(uint creature, SkillType skillType)
        {
            if (skillType == SkillType.Invalid)
                return 0;

            var storedSkillType = AbilityImpactEffects.GetSkillTypeFromStat(TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.NextAutoAttackCriticalRateSkillType,
                StatType.NextAutoAttackCriticalRateSkillType));
            if (!AbilityImpactEffects.SkillTypeMatches(skillType, storedSkillType))
                return 0;

            var criticalRate = TemporaryStatModifier.Consume(
                creature,
                StatType.NextAutoAttackCriticalRatePercentAdjustment,
                StatType.NextAutoAttackCriticalRateSkillType);
            TemporaryStatModifier.Consume(
                creature,
                StatType.NextAutoAttackCriticalRateSkillType,
                StatType.NextAutoAttackCriticalRateSkillType);

            return criticalRate;
        }

        public static void GrantNextAutoAttackNoDelay(uint creature, SkillType skillType, int durationSeconds)
        {
            if (!GetIsObjectValid(creature) || skillType == SkillType.Invalid || durationSeconds <= 0)
                return;

            TemporaryStatModifier.Replace(
                creature,
                StatType.NextAutoAttackNoDelaySkillType,
                (int)skillType,
                durationSeconds,
                StatType.NextAutoAttackNoDelaySkillType);
        }

        public static void GrantNextAutoAttackCriticalRateBonus(
            uint creature,
            SkillType skillType,
            int criticalRatePercentAdjustment,
            int durationSeconds)
        {
            if (!GetIsObjectValid(creature) ||
                skillType == SkillType.Invalid ||
                criticalRatePercentAdjustment == 0 ||
                durationSeconds <= 0)
                return;

            TemporaryStatModifier.Replace(
                creature,
                StatType.NextAutoAttackCriticalRateSkillType,
                (int)skillType,
                durationSeconds,
                StatType.NextAutoAttackCriticalRateSkillType);
            TemporaryStatModifier.Replace(
                creature,
                StatType.NextAutoAttackCriticalRatePercentAdjustment,
                criticalRatePercentAdjustment,
                durationSeconds,
                StatType.NextAutoAttackCriticalRateSkillType);
        }

        public static void GrantNextAbilityNoDelay(uint creature, int skillTypeValue, int durationSeconds)
        {
            var skillType = AbilityImpactEffects.GetSkillTypeFromStat(skillTypeValue);
            QueuedCombatActions.GrantNextAbilityNoDelay(creature, skillType, durationSeconds);
        }

        public static void GrantNextAbilityNoDelay(uint creature, SkillType skillType, int durationSeconds)
        {
            if (!GetIsObjectValid(creature) || skillType == SkillType.Invalid || durationSeconds <= 0)
                return;

            TemporaryStatModifier.Replace(
                creature,
                StatType.NextAttackNoDelay,
                (int)skillType,
                durationSeconds,
                StatType.NextAttackNoDelay);
        }

        public static SkillType GetAbilitySkillType(uint creature, AbilityDetail ability)
        {
            if (ability == null || ability.SkillType != SkillType.Invalid)
                return ability?.SkillType ?? SkillType.Invalid;

            return QueuedCombatActions.GetEquippedWeaponSkillType(creature);
        }

        public static SkillType GetEquippedWeaponSkillType(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return SkillType.Invalid;

            var rightHand = GetItemInSlot(InventorySlot.RightHand, creature);
            if (GetIsObjectValid(rightHand))
            {
                var rightSkillType = Skill.GetSkillTypeByBaseItem((BaseItem)GetBaseItemType(rightHand));
                if (rightSkillType != SkillType.Invalid)
                    return rightSkillType;
            }

            var leftHand = GetItemInSlot(InventorySlot.LeftHand, creature);
            if (!GetIsObjectValid(leftHand))
                return SkillType.Invalid;

            return Skill.GetSkillTypeByBaseItem((BaseItem)GetBaseItemType(leftHand));
        }

        public static bool HasNextAbilityNoStaminaCost(uint creature, SkillType skillType)
        {
            if (skillType == SkillType.Invalid)
                return false;

            var storedSkillType = AbilityImpactEffects.GetSkillTypeFromStat(TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.NextAbilityNoStaminaCostSkillType,
                StatType.NextAbilityNoStaminaCostSkillType));

            return storedSkillType == skillType;
        }

        public static bool ConsumeNextAbilityNoStaminaCost(uint creature, SkillType skillType)
        {
            if (!QueuedCombatActions.HasNextAbilityNoStaminaCost(creature, skillType))
                return false;

            TemporaryStatModifier.Consume(
                creature,
                StatType.NextAbilityNoStaminaCostSkillType,
                StatType.NextAbilityNoStaminaCostSkillType);

            return true;
        }

        public static int GetNextSkillAbilityStaminaCostAdjustment(uint creature, SkillType skillType)
        {
            if (skillType == SkillType.Invalid)
                return 0;

            var storedSkillType = AbilityImpactEffects.GetSkillTypeFromStat(TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.NextSkillAbilityStaminaCostAdjustmentSkillType,
                StatType.NextSkillAbilityStaminaCostAdjustmentSkillType));

            return storedSkillType == skillType
                ? TemporaryStatModifier.GetStatAdjustment(
                    creature,
                    StatType.NextSkillAbilityStaminaCostAdjustment,
                    StatType.NextSkillAbilityStaminaCostAdjustmentSkillType)
                : 0;
        }

        public static int ConsumeNextSkillAbilityStaminaCostAdjustment(uint creature, SkillType skillType)
        {
            var adjustment = QueuedCombatActions.GetNextSkillAbilityStaminaCostAdjustment(creature, skillType);
            if (adjustment == 0)
                return 0;

            TemporaryStatModifier.Consume(
                creature,
                StatType.NextSkillAbilityStaminaCostAdjustment,
                StatType.NextSkillAbilityStaminaCostAdjustmentSkillType);
            TemporaryStatModifier.Consume(
                creature,
                StatType.NextSkillAbilityStaminaCostAdjustmentSkillType,
                StatType.NextSkillAbilityStaminaCostAdjustmentSkillType);

            return adjustment;
        }

        public static (int DamageBonus, int CriticalRatePercentAdjustment, int DefenseIgnorePercentAdjustment) ConsumeNextSkillAbilityBonuses(
            uint creature,
            SkillType skillType)
        {
            if (skillType == SkillType.Invalid)
                return (0, 0, 0);

            var storedSkillType = AbilityImpactEffects.GetSkillTypeFromStat(TemporaryStatModifier.GetStatAdjustment(
                creature,
                StatType.NextSkillAbilitySkillType,
                StatType.NextSkillAbilitySkillType));
            if (!AbilityImpactEffects.SkillTypeMatches(skillType, storedSkillType))
                return (0, 0, 0);

            var damageBonus = TemporaryStatModifier.Consume(
                creature,
                StatType.NextSkillAbilityDamageBonus,
                StatType.NextSkillAbilitySkillType);
            var criticalRate = TemporaryStatModifier.Consume(
                creature,
                StatType.NextSkillAbilityCriticalRatePercentAdjustment,
                StatType.NextSkillAbilitySkillType);
            var defenseIgnore = TemporaryStatModifier.Consume(
                creature,
                StatType.NextSkillAbilityDefenseIgnorePercentAdjustment,
                StatType.NextSkillAbilitySkillType);
            TemporaryStatModifier.Consume(
                creature,
                StatType.NextSkillAbilitySkillType,
                StatType.NextSkillAbilitySkillType);

            return (damageBonus, criticalRate, defenseIgnore);
        }

    }
}
