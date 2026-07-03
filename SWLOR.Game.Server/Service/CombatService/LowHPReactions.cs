using System.Linq;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Service.CombatService
{
    public static class LowHPReactions
    {
        internal static void HealFromDamage(uint creature, int damage, int percent)
        {
            if (damage <= 0 || percent <= 0)
                return;

            var amount = Math.Max(1, (int)Math.Ceiling(damage * (percent / 100f)));
            amount = Stat.ApplyHealingReceivedAdjustment(creature, amount);
            ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), creature);
        }

        internal static void ApplyLowHPPhysicalDefenseEffect(uint defender, int damage)
        {
            var threshold = Stat.GetStatAdjustment(defender, StatType.LowHPPhysicalDefenseThresholdPercent);
            var defensePercent = Stat.GetStatAdjustment(defender, StatType.LowHPPhysicalDefensePercentAdjustment);
            var duration = Stat.GetStatAdjustment(defender, StatType.LowHPPhysicalDefenseDurationSeconds);
            var cooldown = Stat.GetStatAdjustment(defender, StatType.LowHPPhysicalDefenseCooldownSeconds);

            if (threshold <= 0 ||
                defensePercent == 0 ||
                duration <= 0 ||
                !DamageModifierPipeline.DidCrossHPThreshold(defender, damage, threshold) ||
                !CombatStatTriggers.TryUseStatTrigger(defender, StatType.LowHPPhysicalDefensePercentAdjustment, cooldown))
                return;

            TemporaryStatModifier.Replace(
                defender,
                StatType.PhysicalDefensePercentAdjustment,
                defensePercent,
                duration,
                StatType.LowHPPhysicalDefensePercentAdjustment);
        }

        internal static void ApplyLowHPEvasionEffect(uint defender, int damage)
        {
            var threshold = Stat.GetStatAdjustment(defender, StatType.LowHPEvasionThresholdPercent);
            var evasionPercent = Stat.GetStatAdjustment(defender, StatType.LowHPEvasionPercentAdjustment);
            var duration = Stat.GetStatAdjustment(defender, StatType.LowHPEvasionDurationSeconds);
            var cooldown = Stat.GetStatAdjustment(defender, StatType.LowHPEvasionCooldownSeconds);

            if (threshold <= 0 ||
                evasionPercent == 0 ||
                duration <= 0 ||
                !DamageModifierPipeline.DidCrossHPThreshold(defender, damage, threshold) ||
                !CombatStatTriggers.TryUseStatTrigger(defender, StatType.LowHPEvasionPercentAdjustment, cooldown))
                return;

            TemporaryStatModifier.Replace(
                defender,
                StatType.EvasionPercentAdjustment,
                evasionPercent,
                duration,
                StatType.LowHPEvasionPercentAdjustment);
        }

        internal static void ApplyLowHPNextAbilityNoStaminaCostEffect(uint defender, int damage)
        {
            var threshold = Stat.GetStatAdjustment(defender, StatType.LowHPNextAbilityNoStaminaCostThresholdPercent);
            var skillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(defender, StatType.LowHPNextAbilityNoStaminaCostSkillType));
            var duration = Stat.GetStatAdjustment(defender, StatType.LowHPNextAbilityNoStaminaCostDurationSeconds);
            var cooldown = Stat.GetStatAdjustment(defender, StatType.LowHPNextAbilityNoStaminaCostCooldownSeconds);

            if (threshold <= 0 ||
                skillType == SkillType.Invalid ||
                duration <= 0 ||
                !DamageModifierPipeline.DidCrossHPThreshold(defender, damage, threshold) ||
                !CombatStatTriggers.TryUseStatTrigger(defender, StatType.LowHPNextAbilityNoStaminaCostSkillType, cooldown))
                return;

            TemporaryStatModifier.Replace(
                defender,
                StatType.NextAbilityNoStaminaCostSkillType,
                (int)skillType,
                duration,
                StatType.NextAbilityNoStaminaCostSkillType);
        }

        internal static void ApplyLowHPTemporaryHPEffect(uint defender, int damage)
        {
            var threshold = Stat.GetStatAdjustment(defender, StatType.LowHPTemporaryHPThresholdPercent);
            var temporaryHPPercent = Stat.GetStatAdjustment(defender, StatType.LowHPTemporaryHPPercent);
            var duration = Stat.GetStatAdjustment(defender, StatType.LowHPTemporaryHPDurationSeconds);
            var cooldown = Stat.GetStatAdjustment(defender, StatType.LowHPTemporaryHPCooldownSeconds);

            if (threshold <= 0 ||
                temporaryHPPercent <= 0 ||
                duration <= 0 ||
                !DamageModifierPipeline.DidCrossHPThreshold(defender, damage, threshold) ||
                !CombatStatTriggers.TryUseStatTrigger(defender, StatType.LowHPTemporaryHPPercent, cooldown))
                return;

            var temporaryHP = Math.Max(1, (int)Math.Ceiling(GetMaxHitPoints(defender) * (temporaryHPPercent / 100f)));
            ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(temporaryHP), defender, duration);
        }

        internal static void ApplyLowHPTemporaryHPBeforeFatalDamage(uint defender, int damage)
        {
            if (!GetIsObjectValid(defender) || GetIsDead(defender) || damage <= 0)
                return;

            var threshold = Stat.GetStatAdjustment(defender, StatType.LowHPTemporaryHPThresholdPercent);
            var temporaryHPPercent = Stat.GetStatAdjustment(defender, StatType.LowHPTemporaryHPPercent);
            var duration = Stat.GetStatAdjustment(defender, StatType.LowHPTemporaryHPDurationSeconds);
            var cooldown = Stat.GetStatAdjustment(defender, StatType.LowHPTemporaryHPCooldownSeconds);
            if (threshold <= 0 || temporaryHPPercent <= 0 || duration <= 0)
                return;

            var maxHP = GetMaxHitPoints(defender);
            var currentHP = GetCurrentHitPoints(defender);
            if (maxHP <= 0 || currentHP <= 0)
                return;

            var thresholdHP = maxHP * (threshold / 100f);
            var projectedHP = currentHP - damage;
            if (currentHP < thresholdHP || projectedHP >= thresholdHP || projectedHP > 0)
                return;

            if (!CombatStatTriggers.TryUseStatTrigger(defender, StatType.LowHPTemporaryHPPercent, cooldown))
                return;

            var temporaryHP = Math.Max(1, (int)Math.Ceiling(maxHP * (temporaryHPPercent / 100f)));
            ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(temporaryHP), defender, duration);
        }

        internal static void ApplyLowHPNoSaveTemporaryHPEffect(uint defender, int damage)
        {
            var threshold = Stat.GetStatAdjustment(defender, StatType.LowHPNoSaveTemporaryHPThresholdPercent);
            var temporaryHPPercent = Stat.GetStatAdjustment(defender, StatType.LowHPNoSaveTemporaryHPPercent);
            var duration = Stat.GetStatAdjustment(defender, StatType.LowHPNoSaveTemporaryHPDurationSeconds);
            var cooldown = Stat.GetStatAdjustment(defender, StatType.LowHPNoSaveTemporaryHPCooldownSeconds);

            if (threshold <= 0 ||
                temporaryHPPercent <= 0 ||
                duration <= 0 ||
                !DamageModifierPipeline.DidCrossHPThreshold(defender, damage, threshold) ||
                !CombatStatTriggers.TryUseStatTrigger(defender, StatType.LowHPNoSaveTemporaryHPPercent, cooldown))
                return;

            var temporaryHP = Math.Max(1, (int)Math.Ceiling(GetMaxHitPoints(defender) * (temporaryHPPercent / 100f)));
            ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(temporaryHP), defender, duration);
        }

        internal static void ApplyLowHPGuardEffect(uint defender, int damage)
        {
            LowHPReactions.ApplyLowHPGuardEffect(defender, damage, defender);
        }

        public static void ApplyLowHPGuardEffectFromProtectedTarget(uint guardRecipient, uint protectedTarget, int damage)
        {
            LowHPReactions.ApplyLowHPGuardEffect(protectedTarget, damage, guardRecipient);
        }

        internal static void ApplyLowHPGuardEffect(uint thresholdCreature, int damage, uint guardRecipient)
        {
            if (!GetIsObjectValid(thresholdCreature) || !GetIsObjectValid(guardRecipient))
                return;

            var threshold = Stat.GetStatAdjustment(guardRecipient, StatType.LowHPGuardThresholdPercent);
            var guardChance = Stat.GetStatAdjustment(guardRecipient, StatType.LowHPGuard);
            var duration = Stat.GetStatAdjustment(guardRecipient, StatType.LowHPGuardDurationSeconds);
            var cooldown = Stat.GetStatAdjustment(guardRecipient, StatType.LowHPGuardCooldownSeconds);

            if (threshold <= 0 ||
                guardChance <= 0 ||
                duration <= 0 ||
                !DamageModifierPipeline.DidCrossHPThreshold(thresholdCreature, damage, threshold) ||
                !CombatStatTriggers.TryUseStatTrigger(guardRecipient, StatType.LowHPGuard, cooldown))
                return;

            TemporaryStatModifier.Replace(
                guardRecipient,
                StatType.Guard,
                guardChance,
                duration,
                StatType.LowHPGuard);

            if (GetIsPC(guardRecipient))
                FloatingTextStringOnCreature(ColorToken.Combat("Guardian Reflexes"), guardRecipient, false);

            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Ac_Bonus), guardRecipient);
        }

    }
}
