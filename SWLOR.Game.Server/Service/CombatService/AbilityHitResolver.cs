using System.Linq;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;

namespace SWLOR.Game.Server.Service.CombatService
{
    public static class AbilityHitResolver
    {
        public static void ApplyAbilityImpactEffects(uint activator, AbilityImpactSummary summary)
        {
            if (!GetIsObjectValid(activator) || summary == null || summary.ImpactedTargetCount <= 0)
                return;

            switch (summary.SkillType)
            {
                case SkillType.Throwing:
                    AbilityImpactEffects.ApplyThrowingAreaAbilityImpactEffects(activator, summary);
                    break;
                case SkillType.Saberstaff:
                    AbilityImpactEffects.ApplyAreaAbilityImpactEffects(activator, summary);
                    break;
                case SkillType.Spear:
                    AbilityImpactEffects.ApplySpearAbilityImpactEffects(activator, summary);
                    break;
                case SkillType.TwinBlade:
                    AbilityImpactEffects.ApplyTwinBladeAbilityImpactEffects(activator, summary);
                    break;
            }
        }

        public static int CalculateAbilityCriticalRating(
            uint attacker,
            SkillType skillType,
            bool isAreaAbility,
            int criticalRateAdjustment = 0,
            uint defender = OBJECT_INVALID)
        {
            if (!GetIsObjectValid(attacker))
                return 0;

            var criticalRate = criticalRateAdjustment;
            criticalRate += CombatDamageCalculator.GetSkillCriticalRatePercentAdjustment(attacker, skillType);
            criticalRate += AbilityHitResolver.GetAbilityHitOrCriticalAdjustment(
                attacker,
                skillType,
                PerkType.Invalid,
                StatType.AbilityCriticalRatePercentAdjustmentSkillType,
                StatType.AbilityCriticalRatePercentAdjustmentPerkType,
                StatType.AbilityCriticalRatePercentAdjustmentSecondaryPerkType,
                StatType.AbilityCriticalRatePercentAdjustment,
                false);

            if (isAreaAbility && skillType == SkillType.TwinBlade)
            {
                criticalRate += Stat.GetStatAdjustment(attacker, StatType.TwinBladeAreaAbilityCriticalRatePercentAdjustment);
            }

            if (skillType == SkillType.Throwing &&
                GetIsObjectValid(defender) &&
                (StatusEffect.HasStatusEffect(defender, typeof(DisorientedStatusEffect)) ||
                 StatusEffect.HasStatusEffectCategory(defender, StatusEffectCategory.Bleeding)))
            {
                criticalRate += Stat.GetStatAdjustment(attacker, StatType.ThrowingAbilityCriticalRateToBleedingOrDisorientedTargetPercentAdjustment);
            }

            if (GetIsObjectValid(defender) && AbilityHitResolver.IsTargetNotFacingAttacker(attacker, defender))
            {
                criticalRate += Stat.GetStatAdjustment(attacker, StatType.CriticalRateAgainstTargetNotFacingAttackerPercentAdjustment);
            }

            criticalRate += AbilityHitResolver.GetCriticalRateAgainstSunderedTargetAdjustment(attacker, defender);
            criticalRate += AttackConditionBonuses.GetTargetStatusCriticalRateAdjustment(attacker, defender);
            criticalRate += SideCriticalEffects.GetSideAttackCriticalRateAdjustment(attacker, defender, skillType);

            return criticalRate > 0 && Random.D100(1) <= criticalRate
                ? CombatDamageCalculator.StandardCriticalRating
                : 0;
        }

        public static bool TryResolveAbilityHit(
            uint attacker,
            uint defender,
            SkillType skillType,
            PerkType perkType,
            out int hitRate,
            int hitChancePercentAdjustment = 0,
            int skillLevelOverride = -1,
            AbilityType statOverride = AbilityType.Invalid)
        {
            hitRate = 100;
            if (!GetIsObjectValid(attacker) ||
                !GetIsObjectValid(defender) ||
                skillType == SkillType.Invalid)
                return true;

            var accuracy = AbilityHitResolver.GetAbilityAccuracy(attacker, defender, skillType, skillLevelOverride, statOverride);
            var evasion = Stat.GetEvasion(defender, SkillType.Invalid, skillType);
            evasion = SideCriticalEffects.ApplySideAttackEvasionIgnore(attacker, defender, skillType, evasion);

            var modifier = hitChancePercentAdjustment + AbilityHitResolver.GetAbilityHitOrCriticalAdjustment(
                attacker,
                skillType,
                perkType,
                StatType.AbilityHitChancePercentAdjustmentSkillType,
                StatType.AbilityHitChancePercentAdjustmentPerkType,
                StatType.AbilityHitChancePercentAdjustmentSecondaryPerkType,
                StatType.AbilityHitChancePercentAdjustment,
                false);
            modifier += AbilityImpactEffects.GetTargetedAbilityAdjustment(
                attacker,
                perkType,
                StatType.AbilityHitChancePercentAdjustmentPerkType,
                StatType.AbilityHitChancePercentAdjustmentSecondaryPerkType,
                StatType.TargetedAbilityHitChancePercentAdjustment);
            modifier += AbilityHitResolver.GetPhysicalAndForceAbilityHitChanceAdjustment(attacker, skillType);
            modifier += AbilityHitResolver.GetIncomingAbilityHitChanceAdjustment(defender, skillType);
            modifier += SideCriticalEffects.GetSideAttackHitChanceAdjustment(attacker, defender, skillType);
            modifier += AbilityRecoveryEffects.GetIdleAbilityHitChanceAdjustment(attacker, skillType);
            modifier += AbilityHitResolver.GetSuppressionAbilityHitChanceAdjustment(attacker, defender);
            modifier += AbilityHitResolver.GetHitChanceAgainstSunderedTargetAdjustment(attacker, defender);
            if (skillType == SkillType.Force)
            {
                modifier += Perk.GetForceAffinityHitChanceAdjustment(attacker, perkType);
            }

            hitRate = CombatFormula.CalculateHitRate(accuracy, evasion, modifier);
            var isHit = Random.D100(1) <= hitRate;
            if (!isHit && skillType == SkillType.Force)
            {
                AbilityHitResolver.ApplyForceAbilityEvadedEffects(defender);
            }

            return isHit;
        }

        internal static int GetAbilityAccuracy(
            uint attacker,
            uint defender,
            SkillType skillType,
            int skillLevelOverride = -1,
            AbilityType statOverride = AbilityType.Invalid)
        {
            var weapon = AbilityHitResolver.GetRelevantSkillWeapon(attacker, skillType);
            var accuracy = Stat.GetAccuracy(attacker, weapon, statOverride, skillType, skillLevelOverride);
            return DamageModifierPipeline.ApplyStatusSourceAccuracyModifiers(attacker, defender, accuracy);
        }

        internal static uint GetRelevantSkillWeapon(uint creature, SkillType skillType)
        {
            var rightHand = GetItemInSlot(InventorySlot.RightHand, creature);
            if (GetIsObjectValid(rightHand) &&
                (skillType == SkillType.Invalid ||
                 Skill.GetSkillTypeByBaseItem((BaseItem)GetBaseItemType(rightHand)) == skillType ||
                 skillType == SkillType.Force))
                return rightHand;

            var leftHand = GetItemInSlot(InventorySlot.LeftHand, creature);
            if (GetIsObjectValid(leftHand))
                return leftHand;

            return skillType == SkillType.BeastMastery
                ? AbilityHitResolver.GetCreatureNaturalWeapon(creature)
                : rightHand;
        }

        internal static uint GetCreatureNaturalWeapon(uint creature)
        {
            var creatureRight = GetItemInSlot(InventorySlot.CreatureRight, creature);
            if (GetIsObjectValid(creatureRight))
                return creatureRight;

            var creatureLeft = GetItemInSlot(InventorySlot.CreatureLeft, creature);
            if (GetIsObjectValid(creatureLeft))
                return creatureLeft;

            return GetItemInSlot(InventorySlot.CreatureBite, creature);
        }

        internal static int GetAbilityHitOrCriticalAdjustment(
            uint creature,
            SkillType skillType,
            PerkType perkType,
            StatType skillTypeStat,
            StatType primaryPerkStat,
            StatType secondaryPerkStat,
            StatType adjustmentStat,
            bool includePerkTargeting)
        {
            var adjustment = 0;
            var requiredSkillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(creature, skillTypeStat));
            if (AbilityImpactEffects.SkillTypeMatches(skillType, requiredSkillType))
            {
                adjustment += Stat.GetStatAdjustment(creature, adjustmentStat);
            }

            if (includePerkTargeting)
            {
                adjustment += AbilityImpactEffects.GetTargetedAbilityAdjustment(
                    creature,
                    perkType,
                    primaryPerkStat,
                    secondaryPerkStat,
                    adjustmentStat);
            }

            return adjustment;
        }

        internal static int GetIncomingAbilityHitChanceAdjustment(uint defender, SkillType skillType)
        {
            var requiredSkillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(
                defender,
                StatType.IncomingAbilityHitChancePercentAdjustmentSkillType));

            return AbilityImpactEffects.SkillTypeMatches(skillType, requiredSkillType)
                ? Stat.GetStatAdjustment(defender, StatType.IncomingAbilityHitChancePercentAdjustment)
                : 0;
        }

        internal static int GetSuppressionAbilityHitChanceAdjustment(uint attacker, uint defender)
        {
            return WeaponAbilityImpactEffects.GetSuppressionStackCount(defender, attacker) > 0
                ? Stat.GetStatAdjustment(attacker, StatType.AbilityHitChanceAgainstSuppressionStackPercentAdjustment)
                : 0;
        }

        public static int GetHitChanceAgainstSunderedTargetAdjustment(uint attacker, uint defender)
        {
            return GetIsObjectValid(defender) && StatusEffect.HasStatusEffect(defender, typeof(SunderStatusEffect))
                ? Stat.GetStatAdjustment(attacker, StatType.HitChanceAgainstSunderedTargetPercentAdjustment)
                : 0;
        }

        public static int GetCriticalRateAgainstSunderedTargetAdjustment(uint attacker, uint defender)
        {
            return GetIsObjectValid(defender) && StatusEffect.HasStatusEffect(defender, typeof(SunderStatusEffect))
                ? Stat.GetStatAdjustment(attacker, StatType.CriticalRateAgainstSunderedTargetPercentAdjustment)
                : 0;
        }

        internal static int GetPhysicalAndForceAbilityHitChanceAdjustment(uint attacker, SkillType skillType)
        {
            return AbilityImpactEffects.IsWeaponOrForceAbility(skillType)
                ? Stat.GetStatAdjustment(attacker, StatType.PhysicalAndForceAbilityHitChancePercentAdjustment)
                : 0;
        }

        internal static void ApplyForceAbilityEvadedEffects(uint defender)
        {
            var forceDefense = Stat.GetStatAdjustment(defender, StatType.ForceAbilityEvadedForceDefensePercentAdjustment);
            var duration = Stat.GetStatAdjustment(defender, StatType.ForceAbilityEvadedDurationSeconds);
            var staminaRestore = Stat.GetStatAdjustment(defender, StatType.ForceAbilityEvadedStaminaRestore);
            var cooldown = Stat.GetStatAdjustment(defender, StatType.ForceAbilityEvadedCooldownSeconds);
            if (forceDefense == 0 && staminaRestore <= 0 ||
                duration <= 0 ||
                !CombatStatTriggers.TryUseStatTrigger(defender, StatType.ForceAbilityEvadedForceDefensePercentAdjustment, cooldown))
                return;

            if (forceDefense != 0)
            {
                TemporaryStatModifier.Replace(
                    defender,
                    StatType.ForceDefensePercentAdjustment,
                    forceDefense,
                    duration,
                    StatType.ForceAbilityEvadedForceDefensePercentAdjustment);
            }

            if (staminaRestore > 0)
            {
                Stat.RestoreStamina(defender, staminaRestore);
            }
        }

        public static int ApplyDefenseIgnore(int defense, int defenseIgnorePercent)
        {
            if (defense <= 0 || defenseIgnorePercent <= 0)
                return defense;

            return Math.Max(0, defense - (int)Math.Ceiling(defense * (Math.Min(100, defenseIgnorePercent) / 100f)));
        }

        public static bool IsTargetNotFacingAttacker(uint attacker, uint defender)
        {
            if (!GetIsObjectValid(attacker) ||
                !GetIsObjectValid(defender) ||
                GetArea(attacker) != GetArea(defender))
                return false;

            var defenderPosition = GetPosition(defender);
            var attackerPosition = GetPosition(attacker);
            var deltaX = attackerPosition.X - defenderPosition.X;
            var deltaY = attackerPosition.Y - defenderPosition.Y;
            var distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
            if (distance <= 0.001)
                return false;

            var facingRadians = GetFacing(defender) * Math.PI / 180.0;
            var forwardX = Math.Cos(facingRadians);
            var forwardY = Math.Sin(facingRadians);
            var dot = Math.Clamp((forwardX * deltaX + forwardY * deltaY) / distance, -1.0, 1.0);
            var angleDegrees = Math.Acos(dot) * 180.0 / Math.PI;

            return angleDegrees > 90.0;
        }

    }
}
