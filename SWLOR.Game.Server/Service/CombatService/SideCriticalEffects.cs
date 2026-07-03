using System.Linq;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.CombatService
{
    public static class SideCriticalEffects
    {
        public static int ApplySideAttackDamageModifier(uint attacker, uint defender, SkillType skillType, int damage)
        {
            if (damage <= 0 || !SideCriticalEffects.IsMatchingSideAttack(attacker, defender, skillType))
                return damage;

            var adjustment = Stat.GetStatAdjustment(attacker, StatType.SideAttackDamagePercentAdjustment);
            return adjustment == 0
                ? damage
                : Math.Max(0, damage + (int)Math.Ceiling(damage * (adjustment / 100f)));
        }

        public static int GetSideAttackHitChanceAdjustment(uint attacker, uint defender, SkillType skillType)
        {
            return SideCriticalEffects.IsMatchingSideAttack(attacker, defender, skillType)
                ? Stat.GetStatAdjustment(attacker, StatType.SideAttackHitChancePercentAdjustment)
                : 0;
        }

        public static int GetSideAttackCriticalRateAdjustment(uint attacker, uint defender, SkillType skillType)
        {
            return SideCriticalEffects.IsMatchingSideAttack(attacker, defender, skillType)
                ? Stat.GetStatAdjustment(attacker, StatType.SideAttackCriticalRatePercentAdjustment)
                : 0;
        }

        public static int ApplySideAttackEvasionIgnore(uint attacker, uint defender, SkillType skillType, int evasion)
        {
            if (evasion <= 0 || !SideCriticalEffects.IsMatchingSideAttack(attacker, defender, skillType))
                return evasion;

            var chance = Stat.GetStatAdjustment(attacker, StatType.SideAttackEvasionIgnoreChance);
            if (chance <= 0)
                return evasion;

            var scalingAbility = AbilityImpactEffects.GetAbilityTypeFromStatPlusOne(
                Stat.GetStatAdjustment(attacker, StatType.SideAttackEvasionIgnoreChanceScalingAbility));
            if (scalingAbility != AbilityType.Invalid)
            {
                chance += Math.Max(0, GetAbilityScore(attacker, scalingAbility));
            }

            var chanceMaximum = Stat.GetStatAdjustment(attacker, StatType.SideAttackEvasionIgnoreChanceMaximum);
            if (chanceMaximum > 0)
            {
                chance = Math.Min(chance, chanceMaximum);
            }

            var ignorePercent = Stat.GetStatAdjustment(attacker, StatType.SideAttackEvasionIgnorePercent);
            if (ignorePercent <= 0 || Random.D100(1) > chance)
                return evasion;

            return Math.Max(0, evasion - (int)Math.Ceiling(evasion * (Math.Min(100, ignorePercent) / 100f)));
        }

        internal static bool IsMatchingSideAttack(uint attacker, uint defender, SkillType skillType)
        {
            var requiredSkillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(attacker, StatType.SideAttackSkillType));
            return AbilityImpactEffects.SkillTypeMatches(skillType, requiredSkillType) && SideCriticalEffects.IsAttackerBesideTarget(attacker, defender);
        }

        public static void ApplyCriticalHitEffects(
            uint attacker,
            uint defender,
            int damage,
            int criticalRating,
            bool isSingleTargetImpact = false,
            SkillType skillType = SkillType.Invalid)
        {
            if (criticalRating <= 0 || damage <= 0)
                return;

            var staminaRestore = Stat.GetStatAdjustment(attacker, StatType.CriticalStaminaRestore);
            var staminaRestoreSkillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(attacker, StatType.CriticalStaminaRestoreSkillType));
            var staminaRestoreCooldown = Stat.GetStatAdjustment(attacker, StatType.CriticalStaminaRestoreCooldownSeconds);
            if (staminaRestore > 0 &&
                AbilityImpactEffects.SkillTypeMatches(skillType, staminaRestoreSkillType) &&
                CombatStatTriggers.TryUseStatTrigger(attacker, StatType.CriticalStaminaRestore, staminaRestoreCooldown))
            {
                Stat.RestoreStamina(attacker, staminaRestore);
            }

            SideCriticalEffects.ApplyCriticalNextAbilityDamageBonus(attacker, skillType);
            SideCriticalEffects.ApplyCriticalNextSkillAbilityDefenseIgnore(attacker, skillType);
            SideCriticalEffects.ApplyCriticalNextAbilityNoDelay(attacker, skillType);
            SideCriticalEffects.ApplyCriticalNextAutoAttackNoDelay(attacker, skillType);
            SideCriticalEffects.ApplyCriticalSideAttackStaminaRestore(attacker, defender);
            SideCriticalEffects.ApplyCriticalBleedingStatusDurationExtension(attacker, defender);
            SideCriticalEffects.ApplyCriticalHitSequenceEffects(attacker);
            SideCriticalEffects.ApplyCriticalHitSelfEffects(attacker);

            var poisonedTargetStaminaRestore = Stat.GetStatAdjustment(attacker, StatType.CriticalPoisonedTargetStaminaRestore);
            if (poisonedTargetStaminaRestore > 0 && StatusEffect.HasStatusEffect(defender, typeof(PoisonStatusEffect)))
            {
                Stat.RestoreStamina(attacker, poisonedTargetStaminaRestore);
            }

            var markedTargetStaminaRestore = Stat.GetStatAdjustment(attacker, StatType.CriticalMarkedTargetStaminaRestore);
            if (markedTargetStaminaRestore > 0 && StatusEffect.HasStatusEffect(defender, typeof(MarkingTossStatusEffect), attacker))
            {
                Stat.RestoreStamina(attacker, markedTargetStaminaRestore);
            }

            var targetFPLossPercent = Stat.GetStatAdjustment(attacker, StatType.CriticalTargetFPLossPercentOfDamage);
            if (targetFPLossPercent > 0)
            {
                var fpLoss = Math.Max(1, (int)Math.Ceiling(damage * (targetFPLossPercent / 100f)));
                Stat.ReduceFP(defender, fpLoss);
            }

            var targetStaminaLossPercent = Stat.GetStatAdjustment(attacker, StatType.CriticalTargetStaminaLossPercentOfDamage);
            if (targetStaminaLossPercent > 0)
            {
                var staminaLoss = Math.Max(1, (int)Math.Ceiling(damage * (targetStaminaLossPercent / 100f)));
                Stat.ReduceStamina(defender, staminaLoss);
            }

            var hpRestorePercent = Stat.GetStatAdjustment(attacker, StatType.CriticalHPPercentOfDamageRestore);
            var hpRestoreCooldown = Stat.GetStatAdjustment(attacker, StatType.CriticalHPPercentOfDamageRestoreCooldownSeconds);
            if (hpRestorePercent > 0 &&
                CombatStatTriggers.TryUseStatTrigger(attacker, StatType.CriticalHPPercentOfDamageRestore, hpRestoreCooldown))
            {
                LowHPReactions.HealFromDamage(attacker, damage, hpRestorePercent);
            }

            var accuracyPercent = Stat.GetStatAdjustment(attacker, StatType.CriticalAccuracyPercentAdjustment);
            var accuracyDuration = Stat.GetStatAdjustment(attacker, StatType.CriticalAccuracyDurationSeconds);
            if (accuracyPercent != 0 && accuracyDuration > 0)
            {
                TemporaryStatModifier.Replace(
                    attacker,
                    StatType.AccuracyPercentAdjustment,
                    accuracyPercent,
                    accuracyDuration,
                    StatType.CriticalAccuracyPercentAdjustment);
            }

            var targetEvasionPercent = Stat.GetStatAdjustment(attacker, StatType.CriticalTargetEvasionPercentAdjustment);
            var targetEvasionDuration = Stat.GetStatAdjustment(attacker, StatType.CriticalTargetEvasionDurationSeconds);
            if (targetEvasionPercent != 0 && targetEvasionDuration > 0)
            {
                TemporaryStatModifier.Replace(
                    defender,
                    StatType.EvasionPercentAdjustment,
                    targetEvasionPercent,
                    targetEvasionDuration,
                    StatType.CriticalTargetEvasionPercentAdjustment);
            }

            var targetDefensePercent = Stat.GetStatAdjustment(attacker, StatType.CriticalTargetDefensePercentAdjustment);
            var targetDefenseDuration = Stat.GetStatAdjustment(attacker, StatType.CriticalTargetDefenseDurationSeconds);
            if (skillType == SkillType.Staff)
            {
                targetDefensePercent += Stat.GetStatAdjustment(attacker, StatType.StaffCriticalTargetDefensePercentAdjustment);
                targetDefenseDuration = Math.Max(
                    targetDefenseDuration,
                    Stat.GetStatAdjustment(attacker, StatType.StaffCriticalTargetDefenseDurationSeconds));
            }
            if (targetDefensePercent != 0 && targetDefenseDuration > 0)
            {
                StatusEffect.ApplyStatusEffect(
                    attacker,
                    defender,
                    new ExposedStatusEffect(targetDefensePercent),
                    targetDefenseDuration,
                    CombatDamageType.Physical);
            }

            if (isSingleTargetImpact)
            {
                SideCriticalEffects.ApplySingleTargetCriticalTargetDefenseEffect(attacker, defender);
            }
        }

        internal static void ApplyCriticalHitSelfEffects(uint attacker)
        {
            var evasion = Stat.GetStatAdjustment(attacker, StatType.CriticalHitSelfEvasionPercentAdjustment);
            var evasionDuration = Stat.GetStatAdjustment(attacker, StatType.CriticalHitSelfEvasionDurationSeconds);
            if (evasion != 0 && evasionDuration > 0)
            {
                TemporaryStatModifier.Replace(
                    attacker,
                    StatType.EvasionPercentAdjustment,
                    evasion,
                    evasionDuration,
                    StatType.CriticalHitSelfEvasionPercentAdjustment);
            }

            var haste = Stat.GetStatAdjustment(attacker, StatType.CriticalHitSelfHastePercentAdjustment);
            var hasteDuration = Stat.GetStatAdjustment(attacker, StatType.CriticalHitSelfHasteDurationSeconds);
            if (haste != 0 && hasteDuration > 0)
            {
                TemporaryStatModifier.Replace(
                    attacker,
                    StatType.AttackDelayReductionPercent,
                    haste,
                    hasteDuration,
                    StatType.CriticalHitSelfHastePercentAdjustment);
            }
        }

        public static void ApplyNonCriticalAbilityEffects(uint activator, uint target, SkillType skillType)
        {
            if (!GetIsObjectValid(activator) || skillType == SkillType.Invalid)
                return;

            var requiredSkillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.NonCriticalAbilityNextSkillAbilityCriticalRateSkillType));
            var criticalRate = Stat.GetStatAdjustment(
                activator,
                StatType.NonCriticalAbilityNextSkillAbilityCriticalRatePercentAdjustment);
            var maximum = Stat.GetStatAdjustment(
                activator,
                StatType.NonCriticalAbilityNextSkillAbilityCriticalRateMax);
            var duration = Stat.GetStatAdjustment(
                activator,
                StatType.NonCriticalAbilityNextSkillAbilityCriticalRateWindowSeconds);
            if (!AbilityImpactEffects.SkillTypeMatches(skillType, requiredSkillType) ||
                criticalRate <= 0 ||
                maximum <= 0 ||
                duration <= 0)
            {
                return;
            }

            TemporaryStatModifier.Replace(
                activator,
                StatType.NextSkillAbilitySkillType,
                (int)skillType,
                duration,
                StatType.NextSkillAbilitySkillType);
            TemporaryStatModifier.AddCapped(
                activator,
                StatType.NextSkillAbilityCriticalRatePercentAdjustment,
                criticalRate,
                duration,
                maximum,
                StatType.NextSkillAbilitySkillType,
                1);
        }

        internal static void ApplyCriticalBleedingStatusDurationExtension(uint attacker, uint defender)
        {
            var extensionSeconds = Stat.GetStatAdjustment(attacker, StatType.CriticalBleedingStatusDurationExtensionSeconds);
            if (extensionSeconds <= 0 ||
                !GetIsObjectValid(defender) ||
                !StatusEffect.HasStatusEffect(defender, typeof(BleedStatusEffect), attacker) &&
                !StatusEffect.HasStatusEffect(defender, typeof(HemorrhageStatusEffect), attacker))
            {
                return;
            }

            var cooldownSeconds = Stat.GetStatAdjustment(
                attacker,
                StatType.CriticalBleedingStatusDurationExtensionCooldownSeconds);
            if (!CombatStatTriggers.TryUseStatTrigger(attacker, StatType.CriticalBleedingStatusDurationExtensionSeconds, cooldownSeconds))
                return;

            StatusEffect.ExtendStatusEffectDuration(defender, typeof(BleedStatusEffect), attacker, extensionSeconds);
            StatusEffect.ExtendStatusEffectDuration(defender, typeof(HemorrhageStatusEffect), attacker, extensionSeconds);
        }

        internal static void ApplyCriticalHitSequenceEffects(uint attacker)
        {
            var requiredCount = Stat.GetStatAdjustment(attacker, StatType.CriticalHitSequenceCountRequired);
            var windowSeconds = Stat.GetStatAdjustment(attacker, StatType.CriticalHitSequenceWindowSeconds);
            var staminaRestore = Stat.GetStatAdjustment(attacker, StatType.CriticalHitSequenceStaminaRestore);
            if (requiredCount <= 0 || windowSeconds <= 0 || staminaRestore <= 0)
                return;

            if (CombatState.TrackCriticalHitSequence(attacker, requiredCount, windowSeconds))
            {
                Stat.RestoreStamina(attacker, staminaRestore);
            }
        }

        internal static void ApplyCriticalNextSkillAbilityDefenseIgnore(uint attacker, SkillType skillType)
        {
            if (skillType == SkillType.Invalid)
                return;

            var defenseIgnore = Stat.GetStatAdjustment(attacker, StatType.CriticalNextSkillAbilityDefenseIgnorePercentAdjustment);
            var duration = Stat.GetStatAdjustment(attacker, StatType.CriticalNextSkillAbilityDefenseIgnoreDurationSeconds);
            if (defenseIgnore <= 0 || duration <= 0)
                return;

            TemporaryStatModifier.Replace(
                attacker,
                StatType.NextSkillAbilitySkillType,
                (int)skillType,
                duration,
                StatType.NextSkillAbilitySkillType);

            TemporaryStatModifier.Replace(
                attacker,
                StatType.NextSkillAbilityDefenseIgnorePercentAdjustment,
                defenseIgnore,
                duration,
                StatType.NextSkillAbilitySkillType);
        }

        internal static void ApplyCriticalNextAbilityDamageBonus(uint attacker, SkillType skillType)
        {
            var triggerSkillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(attacker, StatType.CriticalNextAbilityDamageBonusTriggerSkillType));
            if (!AbilityImpactEffects.SkillTypeMatches(skillType, triggerSkillType))
                return;

            var perkType = AbilityImpactEffects.GetPerkTypeFromStat(Stat.GetStatAdjustment(attacker, StatType.CriticalNextAbilityDamageBonusPerkType));
            var bonus = Stat.GetStatAdjustment(attacker, StatType.CriticalNextAbilityDamageBonus);
            var duration = Stat.GetStatAdjustment(attacker, StatType.CriticalNextAbilityDamageBonusDurationSeconds);
            var cooldown = Stat.GetStatAdjustment(attacker, StatType.CriticalNextAbilityDamageBonusCooldownSeconds);
            if (perkType == PerkType.Invalid || bonus == 0 || duration <= 0)
                return;

            if (CombatStatTriggers.TryUseStatTrigger(attacker, StatType.CriticalNextAbilityDamageBonus, cooldown))
            {
                AbilityImpactEffects.GrantNextAbilityDamageBonus(attacker, perkType, bonus, duration);
            }
        }

        internal static void ApplyCriticalNextAbilityNoDelay(uint attacker, SkillType skillType)
        {
            var triggerSkillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(attacker, StatType.CriticalNextAbilityNoDelayTriggerSkillType));
            if (!AbilityImpactEffects.SkillTypeMatches(skillType, triggerSkillType))
                return;

            var noDelaySkillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(attacker, StatType.CriticalNextAbilityNoDelaySkillType));
            var duration = Stat.GetStatAdjustment(attacker, StatType.CriticalNextAbilityNoDelayDurationSeconds);
            var cooldown = Stat.GetStatAdjustment(attacker, StatType.CriticalNextAbilityNoDelayCooldownSeconds);
            if (noDelaySkillType == SkillType.Invalid || duration <= 0)
                return;

            if (CombatStatTriggers.TryUseStatTrigger(attacker, StatType.CriticalNextAbilityNoDelaySkillType, cooldown))
            {
                QueuedCombatActions.GrantNextAbilityNoDelay(attacker, noDelaySkillType, duration);
            }
        }

        internal static void ApplyCriticalNextAutoAttackNoDelay(uint attacker, SkillType skillType)
        {
            var triggerSkillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(attacker, StatType.CriticalNextAutoAttackNoDelayTriggerSkillType));
            if (!AbilityImpactEffects.SkillTypeMatches(skillType, triggerSkillType))
                return;

            var noDelaySkillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(attacker, StatType.CriticalNextAutoAttackNoDelaySkillType));
            var duration = Stat.GetStatAdjustment(attacker, StatType.CriticalNextAutoAttackNoDelayDurationSeconds);
            var cooldown = Stat.GetStatAdjustment(attacker, StatType.CriticalNextAutoAttackNoDelayCooldownSeconds);
            if (noDelaySkillType == SkillType.Invalid || duration <= 0)
                return;

            if (CombatStatTriggers.TryUseStatTrigger(attacker, StatType.CriticalNextAutoAttackNoDelaySkillType, cooldown))
            {
                QueuedCombatActions.GrantNextAutoAttackNoDelay(attacker, noDelaySkillType, duration);
            }
        }

        internal static void ApplyCriticalSideAttackStaminaRestore(uint attacker, uint defender)
        {
            var chance = Stat.GetStatAdjustment(attacker, StatType.CriticalSideAttackStaminaRestoreChance);
            var staminaRestore = Stat.GetStatAdjustment(attacker, StatType.CriticalSideAttackStaminaRestore);
            if (chance <= 0 || staminaRestore <= 0 || !SideCriticalEffects.IsAttackerBesideTarget(attacker, defender))
                return;

            if (Random.D100(1) <= chance)
            {
                Stat.RestoreStamina(attacker, staminaRestore);
            }
        }

        internal static void ApplySingleTargetCriticalTargetDefenseEffect(uint attacker, uint defender)
        {
            var targetDefensePercent = Stat.GetStatAdjustment(attacker, StatType.SingleTargetCriticalTargetDefensePercentAdjustment);
            var targetDefenseDuration = Stat.GetStatAdjustment(attacker, StatType.SingleTargetCriticalTargetDefenseDurationSeconds);
            if (targetDefensePercent == 0 || targetDefenseDuration <= 0)
                return;

            StatusEffect.ApplyStatusEffect(
                attacker,
                defender,
                new ExposedStatusEffect(targetDefensePercent),
                targetDefenseDuration,
                CombatDamageType.Physical);
        }

        public static bool IsAttackerBesideTarget(uint attacker, uint defender)
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

            return angleDegrees >= 45.0 && angleDegrees <= 135.0;
        }

    }
}
