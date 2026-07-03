using System.Linq;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.CombatService
{
    public static class DamageModifierPipeline
    {
        private const int MaximumNormalDamageReductionPercent = 95;

        internal static void ApplyRecentDamageTargetHitEffects(uint defender, uint attacker)
        {
            if (!GetIsObjectValid(attacker) || !GetIsObjectValid(defender) || attacker == defender)
                return;

            var chance = Stat.GetStatAdjustment(defender, StatType.DamageTakenRecentTargetNextAbilityNoDelayChance);
            var window = Stat.GetStatAdjustment(defender, StatType.DamageTakenRecentTargetWindowSeconds);
            var skillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(defender, StatType.DamageTakenRecentTargetNextAbilityNoDelaySkillType));
            if (chance <= 0 || window <= 0 || skillType == SkillType.Invalid)
                return;

            if (!CombatActivity.HasRecentDamageTarget(defender, attacker, window) ||
                Random.D100(1) > chance)
                return;

            TemporaryStatModifier.Replace(
                defender,
                StatType.NextAttackNoDelay,
                (int)skillType,
                window,
                StatType.NextAttackNoDelay);
        }

        internal static bool DidCrossHPThreshold(uint creature, int damage, int thresholdPercent)
        {
            var maxHP = GetMaxHitPoints(creature);
            var currentHP = GetCurrentHitPoints(creature);
            if (maxHP <= 0 || currentHP <= 0)
                return false;

            var thresholdHP = maxHP * (thresholdPercent / 100f);
            var previousHP = currentHP + damage;
            return previousHP >= thresholdHP && currentHP < thresholdHP;
        }

        internal static void HealPercentOfMaxHP(uint creature, int percent)
        {
            if (percent <= 0)
                return;

            var amount = Math.Max(1, (int)Math.Ceiling(GetMaxHitPoints(creature) * (percent / 100f)));
            amount = Stat.ApplyHealingReceivedAdjustment(creature, amount);
            ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), creature);
        }

        internal static int ApplyOutgoingDamageModifier(uint attacker, int damage)
        {
            var adjustment = Stat.GetStatAdjustment(attacker, StatType.DamageDealtPercentAdjustment);
            if (adjustment == 0)
                return damage;

            return DamageModifierPipeline.ApplyPercentDamageAdjustment(damage, adjustment);
        }

        internal static int ApplyWeaponAndForceDamageModifier(
            uint attacker,
            int damage,
            SkillType skillType,
            CombatDamageType damageType)
        {
            if (!AbilityImpactEffects.IsWeaponOrForceDamage(skillType, damageType))
                return damage;

            var adjustment = Stat.GetStatAdjustment(attacker, StatType.WeaponAndForceDamageDealtPercentAdjustment);
            if (adjustment == 0)
                return damage;

            return DamageModifierPipeline.ApplyPercentDamageAdjustment(damage, adjustment);
        }

        internal static int ApplyTargetLowHPDamageModifier(uint attacker, uint defender, int damage)
        {
            var threshold = Stat.GetStatAdjustment(attacker, StatType.TargetLowHPDamageThresholdPercent);
            var adjustment = Stat.GetStatAdjustment(attacker, StatType.TargetLowHPDamagePercentAdjustment);

            if (threshold > 0 && adjustment != 0)
            {
                var maxHP = GetMaxHitPoints(defender);
                if (maxHP > 0 && GetCurrentHitPoints(defender) <= maxHP * (threshold / 100f))
                    damage = DamageModifierPipeline.ApplyPercentDamageAdjustment(damage, adjustment);
            }

            return DamageModifierPipeline.ApplyTargetLowHPStatusDamageModifier(attacker, defender, damage);
        }

        internal static int ApplyTargetLowHPStatusDamageModifier(uint attacker, uint defender, int damage)
        {
            var threshold = Stat.GetStatAdjustment(attacker, StatType.TargetLowHPStatusDamageThresholdPercent);
            var adjustment = Stat.GetStatAdjustment(attacker, StatType.TargetLowHPStatusDamagePercentAdjustment);
            var category = AbilityImpactEffects.GetStatusEffectCategoryFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.TargetLowHPStatusDamageStatusCategory));

            if (threshold <= 0 || adjustment == 0 || category == 0 || !WeaponAbilityImpactEffects.TargetHasAnyStatusEffectCategory(defender, category))
                return damage;

            var maxHP = GetMaxHitPoints(defender);
            if (maxHP <= 0 || GetCurrentHitPoints(defender) > maxHP * (threshold / 100f))
                return damage;

            return DamageModifierPipeline.ApplyPercentDamageAdjustment(damage, adjustment);
        }

        public static int GetNearbyStatusTargetAttackAdjustment(uint creature)
        {
            var percentPerTarget = Stat.GetStatAdjustment(creature, StatType.NearbyStatusTargetAttackPercentPerTarget);
            var radius = Stat.GetStatAdjustment(creature, StatType.NearbyStatusTargetAttackRadiusMeters);
            var maximum = Stat.GetStatAdjustment(creature, StatType.NearbyStatusTargetAttackPercentMaximum);
            var categoryValue = Stat.GetStatAdjustment(creature, StatType.NearbyStatusTargetAttackStatusCategory);
            if (percentPerTarget == 0 || radius <= 0 || categoryValue <= 0)
                return 0;

            var category = (StatusEffectCategory)categoryValue;
            var count = 0;
            var location = GetLocation(creature);
            var target = GetFirstObjectInShape(Shape.Sphere, radius, location, true);
            while (GetIsObjectValid(target))
            {
                if (target != creature &&
                    GetIsReactionTypeHostile(target, creature) &&
                    StatusEffect.HasStatusEffectCategory(target, category))
                {
                    count++;
                }

                target = GetNextObjectInShape(Shape.Sphere, radius, location, true);
            }

            var adjustment = count * percentPerTarget;
            return maximum > 0
                ? Math.Min(maximum, adjustment)
                : adjustment;
        }

        internal static int ApplyTargetStatusDamageModifiers(
            uint attacker,
            uint defender,
            int damage,
            SkillType skillType,
            CombatDamageType damageType,
            bool isAbilityDamage,
            bool canApplyRandomFlatBonuses)
        {
            var adjustment = 0;
            adjustment += CombatStatTriggers.GetStatusSourceStatAdjustment(
                attacker,
                defender,
                StatType.DamageToStatusSourcePercentAdjustment);
            adjustment += CombatStatTriggers.GetStatusSourceStatAdjustment(
                defender,
                attacker,
                StatType.DamageTakenFromStatusSourcePercentAdjustment);
            adjustment += CombatStatTriggers.GetStatusSourcePartyStatAdjustment(
                defender,
                attacker,
                StatType.DamageTakenFromStatusSourcePartyPercentAdjustment);

            if (StatusEffect.HasStatusEffect(defender, typeof(SunderStatusEffect)))
                adjustment += Stat.GetStatAdjustment(attacker, StatType.DamageToSunderedTargetPercentAdjustment);

            if (StatusEffect.HasStatusEffectCategory(defender, StatusEffectCategory.Bleeding))
            {
                adjustment += Stat.GetStatAdjustment(attacker, StatType.DamageToBleedingTargetPercentAdjustment);
                if (isAbilityDamage &&
                    AbilityImpactEffects.SkillTypeMatches(
                        skillType,
                        AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(
                            attacker,
                            StatType.AbilityDamageToBleedingTargetSkillType))))
                {
                    damage += Stat.GetStatAdjustment(attacker, StatType.AbilityDamageToBleedingTargetBonus);
                }
            }

            if (StatusEffect.HasStatusEffectCategory(defender, StatusEffectCategory.Debuff))
                adjustment += Stat.GetStatAdjustment(attacker, StatType.DamageToDebuffedTargetPercentAdjustment);

            adjustment += WeaponAbilityImpactEffects.GetDamageToSourceAppliedStatusTargetAdjustment(attacker, defender);
            if (isAbilityDamage)
                adjustment += WeaponAbilityImpactEffects.GetAbilityDamageToSourceAppliedStatusTargetAdjustment(attacker, defender, skillType);

            if (isAbilityDamage &&
                skillType == SkillType.Katar &&
                StatusEffect.HasStatusEffect(defender, typeof(PoisonStatusEffect), typeof(DisorientedStatusEffect)))
            {
                adjustment += Stat.GetStatAdjustment(attacker, StatType.DamageToPoisonedOrDisorientedTargetPercentAdjustment);
            }

            if (canApplyRandomFlatBonuses &&
                skillType == SkillType.Katar &&
                StatusEffect.HasStatusEffect(defender, typeof(PoisonStatusEffect)))
            {
                var flatBonusChance = Stat.GetStatAdjustment(attacker, StatType.DamageToPoisonedTargetFlatBonusChance);
                var flatBonus = Stat.GetStatAdjustment(attacker, StatType.DamageToPoisonedTargetFlatBonus);
                if (flatBonusChance > 0 && flatBonus != 0 && Random.D100(1) <= flatBonusChance)
                {
                    damage += flatBonus;
                }
            }

            if (StatusEffect.HasStatusEffect(defender, typeof(WeakenedStatusEffect), typeof(HamstringStatusEffect)))
                adjustment += Stat.GetStatAdjustment(attacker, StatType.DamageToWeakenedOrHamstringTargetPercentAdjustment);

            if (StatusEffect.HasStatusEffectCategory(defender, StatusEffectCategory.Control))
                adjustment += Stat.GetStatAdjustment(attacker, StatType.DamageToControlTargetPercentAdjustment);

            adjustment += WeaponAbilityImpactEffects.GetSuppressionDamageDealtToOtherTargetsAdjustment(attacker, defender);

            if (isAbilityDamage &&
                AbilityImpactEffects.IsCurrentFPAndStaminaAtOrAbovePercent(
                    attacker,
                    Stat.GetStatAdjustment(attacker, StatType.HighFPAndStaminaAttackThresholdPercent)))
            {
                adjustment += Stat.GetStatAdjustment(attacker, StatType.HighFPAndStaminaAttackPercentAdjustment);
            }

            if (skillType == SkillType.Rifle &&
                StatusEffect.HasStatusEffect(defender, typeof(DisorientedStatusEffect), typeof(DazedStatusEffect), typeof(TranquilizedStatusEffect)))
            {
                adjustment += Stat.GetStatAdjustment(attacker, StatType.DamageToDisorientedDazedTargetPercentAdjustment);
            }

            if (damageType.IsPhysicalDamageType())
                adjustment += Stat.GetStatAdjustment(defender, StatType.PhysicalDamageTakenPercentAdjustment);

            if (isAbilityDamage && damageType.IsPhysicalDamageType())
                adjustment += Stat.GetStatAdjustment(defender, StatType.PhysicalAbilityDamageTakenPercentAdjustment);

            if (damageType == CombatDamageType.Force)
                adjustment += Stat.GetStatAdjustment(defender, StatType.ForceDamageTakenPercentAdjustment);

            if (damageType.IsPhysicalDamageType() && CombatSkillType.IsRangedDamageSkill(skillType))
                adjustment += Stat.GetStatAdjustment(defender, StatType.RangedPhysicalDamageTakenPercentAdjustment);

            if (skillType == SkillType.Throwing)
                adjustment += Stat.GetStatAdjustment(defender, StatType.ThrowingDamageTakenPercentAdjustment);

            if (CombatSkillType.IsRangedWeaponSkill(skillType) && DamageModifierPipeline.IsNearbyTargetWithinDistance(attacker, defender, 8f))
                adjustment += Stat.GetStatAdjustment(attacker, StatType.RangedDamageToNearbyTargetPercentAdjustment);

            if (skillType == SkillType.Pistol &&
                StatusEffect.HasStatusEffect(
                    defender,
                    typeof(DisorientedStatusEffect),
                    typeof(KnockdownStatusEffect),
                    typeof(TranquilizedStatusEffect)))
            {
                damage += Stat.GetStatAdjustment(
                    attacker,
                    StatType.PistolDamageToDisorientedKnockdownOrTranquilizedTargetBonus);
            }

            if (isAbilityDamage &&
                skillType == SkillType.Staff &&
                StatusEffect.HasStatusEffect(defender, typeof(KnockdownStatusEffect), typeof(BlindStatusEffect)))
            {
                adjustment += Stat.GetStatAdjustment(attacker, StatType.AbilityDamageToKnockdownOrBlindTargetPercentAdjustment);
            }

            if (adjustment == 0)
                return damage;

            return DamageModifierPipeline.ApplyPercentDamageAdjustment(damage, adjustment);
        }

        public static int ApplyTwinBladeAbilityShapeDamageModifier(
            uint attacker,
            SkillType skillType,
            int damage,
            bool isSingleTargetAbility,
            bool isAreaAbility)
        {
            if (damage <= 0 || skillType != SkillType.TwinBlade)
                return damage;

            var adjustment = 0;
            if (isSingleTargetAbility)
            {
                adjustment += Stat.GetStatAdjustment(
                    attacker,
                    StatType.TwinBladeSingleTargetAbilityDamagePercentAdjustment);
            }

            if (isAreaAbility)
            {
                adjustment += Stat.GetStatAdjustment(
                    attacker,
                    StatType.TwinBladeAreaAbilityDamagePercentAdjustment);
            }

            if (adjustment == 0)
                return damage;

            return DamageModifierPipeline.ApplyPercentDamageAdjustment(damage, adjustment);
        }

        public static int ApplyThrowingAbilityShapeDamageModifier(
            uint attacker,
            SkillType skillType,
            int damage,
            bool isAreaAbility)
        {
            if (damage <= 0 || skillType != SkillType.Throwing || !isAreaAbility)
                return damage;

            var adjustment = Stat.GetStatAdjustment(
                attacker,
                StatType.ThrowingAreaAbilityDamagePercentAdjustment);
            if (adjustment == 0)
                return damage;

            return DamageModifierPipeline.ApplyPercentDamageAdjustment(damage, adjustment);
        }

        public static int ApplyPhysicalAbilityShapeDamageModifier(
            uint attacker,
            CombatDamageType damageType,
            int damage,
            bool isSingleTargetAbility)
        {
            if (damage <= 0 || !isSingleTargetAbility || !damageType.IsPhysicalDamageType())
                return damage;

            var adjustment = Stat.GetStatAdjustment(
                attacker,
                StatType.SingleTargetPhysicalAbilityDamagePercentAdjustment);
            if (adjustment == 0)
                return damage;

            return DamageModifierPipeline.ApplyPercentDamageAdjustment(damage, adjustment);
        }

        public static int ApplySkillAreaAbilityDamageModifier(
            uint attacker,
            SkillType skillType,
            int damage,
            bool isAreaAbility)
        {
            if (damage <= 0 || !isAreaAbility)
                return damage;

            var requiredSkillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.SkillAreaAbilityDamagePercentAdjustmentSkillType));
            if (!AbilityImpactEffects.SkillTypeMatches(skillType, requiredSkillType))
                return damage;

            var adjustment = Stat.GetStatAdjustment(
                attacker,
                StatType.SkillAreaAbilityDamagePercentAdjustment);
            if (adjustment == 0)
                return damage;

            return DamageModifierPipeline.ApplyPercentDamageAdjustment(damage, adjustment);
        }

        public static int ApplyAreaAbilityAfterDeflectionDamageModifier(
            uint attacker,
            SkillType skillType,
            int damage,
            bool isAreaAbility)
        {
            if (damage <= 0 || !isAreaAbility)
                return damage;

            var requiredSkillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.AreaAbilityAfterDeflectionDamagePercentAdjustmentSkillType));
            if (!AbilityImpactEffects.SkillTypeMatches(skillType, requiredSkillType))
                return damage;

            var window = Stat.GetStatAdjustment(attacker, StatType.AreaAbilityAfterDeflectionWindowSeconds);
            if (window <= 0 || !CombatActivity.HasRecentDeflection(attacker, window))
                return damage;

            var adjustment = Stat.GetStatAdjustment(attacker, StatType.AreaAbilityAfterDeflectionDamagePercentAdjustment);
            return DamageModifierPipeline.ApplyPercentDamageAdjustment(damage, adjustment);
        }

        internal static int ApplyPercentDamageAdjustment(int damage, int adjustment)
        {
            if (damage <= 0 || adjustment == 0)
                return damage;

            adjustment = Math.Max(adjustment, -MaximumNormalDamageReductionPercent);
            return Math.Max(1, damage + (int)Math.Ceiling(damage * (adjustment / 100f)));
        }

        public static bool HasDamageImmunity(uint defender, CombatDamageType damageType)
        {
            if (!GetIsObjectValid(defender))
                return false;

            return damageType.IsPhysicalDamageType() &&
                   Stat.GetStatAdjustment(defender, StatType.PhysicalDamageImmunity) > 0;
        }

        internal static int ApplyRepeatedTargetDamageModifier(
            uint attacker,
            uint defender,
            SkillType skillType,
            int damage)
        {
            if (damage <= 0 || !GetIsObjectValid(attacker) || !GetIsObjectValid(defender) || attacker == defender)
                return damage;

            var requiredSkillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(attacker, StatType.RepeatedTargetDamageSkillType));
            var percentPerHit = Stat.GetStatAdjustment(attacker, StatType.RepeatedTargetDamagePercentPerHit);
            var maxPercent = Stat.GetStatAdjustment(attacker, StatType.RepeatedTargetDamagePercentMax);
            var bonusPerHit = Stat.GetStatAdjustment(attacker, StatType.RepeatedTargetDamageBonusPerHit);
            var maxBonus = Stat.GetStatAdjustment(attacker, StatType.RepeatedTargetDamageBonusMax);
            var durationSeconds = Stat.GetStatAdjustment(attacker, StatType.RepeatedTargetDamageDurationSeconds);
            var hasPercentBonus = percentPerHit > 0 && maxPercent > 0;
            var hasFlatBonus = bonusPerHit > 0 && maxBonus > 0;
            if (!AbilityImpactEffects.SkillTypeMatches(skillType, requiredSkillType) || (!hasPercentBonus && !hasFlatBonus))
            {
                CombatState.ClearRepeatedTargetDamage(attacker);
                return damage;
            }

            var maxStacks = 1;
            if (hasPercentBonus)
                maxStacks = Math.Max(maxStacks, (int)Math.Ceiling(maxPercent / (float)percentPerHit));
            if (hasFlatBonus)
                maxStacks = Math.Max(maxStacks, (int)Math.Ceiling(maxBonus / (float)bonusPerHit));

            var stacks = CombatState.TrackRepeatedTargetDamageHit(
                attacker,
                defender,
                durationSeconds,
                maxStacks);

            if (hasPercentBonus)
            {
                var adjustment = Math.Min(maxPercent, stacks * percentPerHit);
                damage += (int)Math.Ceiling(damage * (adjustment / 100f));
            }

            if (hasFlatBonus)
            {
                damage += Math.Min(maxBonus, stacks * bonusPerHit);
            }

            return damage;
        }

        public static int ApplyStatusSourceDefenseModifiers(uint attacker, uint defender, int defense)
        {
            if (defense <= 0)
                return defense;

            var adjustment = CombatStatTriggers.GetStatusSourceStatAdjustment(
                defender,
                attacker,
                StatType.DefenseAgainstStatusSourcePercentAdjustment);

            return adjustment == 0
                ? defense
                : Math.Max(0, defense + (int)Math.Ceiling(defense * (adjustment / 100f)));
        }

        public static int ApplyStatusSourceAccuracyModifiers(uint attacker, uint defender, int accuracy)
        {
            if (accuracy <= 0)
                return accuracy;

            var adjustment = CombatStatTriggers.GetStatusSourceStatAdjustment(
                attacker,
                defender,
                StatType.AccuracyToStatusSourcePercentAdjustment);

            return adjustment == 0
                ? accuracy
                : Math.Max(1, accuracy + (int)Math.Ceiling(accuracy * (adjustment / 100f)));
        }

        internal static bool IsNearbyTargetWithinDistance(uint attacker, uint defender, float distance)
        {
            return GetIsObjectValid(attacker) &&
                   GetIsObjectValid(defender) &&
                   GetArea(attacker) == GetArea(defender) &&
                   GetDistanceBetween(attacker, defender) <= distance;
        }

    }
}
