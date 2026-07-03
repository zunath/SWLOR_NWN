using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.CombatService
{
    public static class DamageDealtEffects
    {
        public static void ApplyDamageDealtEffects(
            uint attacker,
            uint defender,
            int damage,
            SkillType skillType = SkillType.Invalid,
            CombatDamageType damageType = CombatDamageType.Physical,
            CombatDamageDeliveryType deliveryType = CombatDamageDeliveryType.Direct)
        {
            if (damage <= 0)
                return;

            CombatActivity.TrackCombatActivity(attacker);
            CombatActivity.TrackRecentDamageTarget(attacker, defender);

            var appliesDirectDamageEffects = deliveryType == CombatDamageDeliveryType.Direct;
            if (!appliesDirectDamageEffects)
                return;

            DamageDealtEffects.ApplySideAttackDamageEffects(attacker, defender, skillType, damage);
            DamageDealtEffects.ApplyDamageDealtStaminaRestore(attacker, skillType);
            DamageDealtEffects.ApplyDamageDealtAttackDelayReduction(attacker, skillType);
            DamageDealtEffects.ApplyPredatorsMarkEffects(attacker, defender, skillType);
            DamageDealtEffects.ApplyDamageDealtForceErosionEffect(attacker, defender, deliveryType);
            DamageDealtEffects.ApplyDamageDealtHamstringEffect(attacker, defender, skillType, damageType);
            WeaponAbilityImpactEffects.ApplyNextDamageDealtBleedEffect(attacker, defender, damageType);
            WeaponAbilityImpactEffects.ApplyAutoAttackSuppressionStack(attacker, defender, skillType, damageType);
            WeaponAbilityImpactEffects.ApplyRangedHitSuppressionStack(attacker, defender, skillType, damageType);
            DamageDealtEffects.ApplyBleedingTargetStaminaRestore(attacker, defender);
            WeaponAbilityImpactEffects.ApplyBleedingTargetAbilityBleedRefresh(attacker, defender, skillType);
            WeaponAbilityImpactEffects.ApplyBleedingTargetAbilityBleedSpread(attacker, defender, skillType, damageType);
            WeaponStatusImpactEffects.ApplyToxicRushDamageDealtEffects(attacker, defender, deliveryType);
            DamageDealtEffects.ApplyHeavyVibrobladeDefenseDamageRecovery(attacker, damage);
            WeaponAbilityImpactEffects.ApplyFrenzySlashHasteRefresh(attacker);

            var hpRestorePercent = Stat.GetStatAdjustment(attacker, StatType.DamageDealtHPPercentRestore);
            if (hpRestorePercent > 0)
            {
                LowHPReactions.HealFromDamage(attacker, damage, hpRestorePercent);
            }

            if (damageType.IsPhysicalDamageType())
            {
                hpRestorePercent = Stat.GetStatAdjustment(attacker, StatType.PhysicalDamageDealtHPPercentRestore);
                if (hpRestorePercent > 0)
                {
                    LowHPReactions.HealFromDamage(attacker, damage, hpRestorePercent);
                }
            }

            DamageDealtEffects.ApplyLowHPDamageDealtHPRestore(attacker, damage);
        }

        internal static void ApplyHeavyVibrobladeDefenseDamageRecovery(uint attacker, int damage)
        {
            if (Stat.GetStatAdjustment(attacker, StatType.HeavyVibrobladeDefenseRecoveryWindow) <= 0)
                return;

            var hpRestorePercent = Stat.GetStatAdjustment(attacker, StatType.HeavyVibrobladeDefenseDamageDealtHPPercentRestore);
            if (hpRestorePercent <= 0)
                return;

            LowHPReactions.HealFromDamage(attacker, damage, hpRestorePercent);
        }

        internal static void ApplyPredatorsMarkEffects(uint attacker, uint defender, SkillType skillType)
        {
            if (skillType != SkillType.BeastMastery ||
                !GetIsObjectValid(attacker) ||
                !GetIsObjectValid(defender) ||
                GetIsDead(attacker) ||
                GetIsDead(defender))
            {
                return;
            }

            if (StatusEffect.HasStatusEffect(defender, typeof(PredatorsMark1StatusEffect), attacker))
            {
                DamageDealtEffects.ApplyPredatorsMarkFollowUp(attacker);
                return;
            }

            var damageTakenFromBeastPercent = Stat.GetStatAdjustment(attacker, StatType.PredatorsMarkDamageTakenFromBeastPercent);
            var durationSeconds = Stat.GetStatAdjustment(attacker, StatType.PredatorsMarkDurationSeconds);
            if (damageTakenFromBeastPercent <= 0 || durationSeconds <= 0)
                return;

            StatusEffect.ApplyStatusEffect(
                attacker,
                defender,
                new PredatorsMark1StatusEffect(damageTakenFromBeastPercent),
                durationSeconds,
                ResistanceType.Trauma);
        }

        internal static void ApplyPredatorsMarkFollowUp(uint attacker)
        {
            var hastePercent = Stat.GetStatAdjustment(attacker, StatType.PredatorsMarkHastePercentPerStack);
            var abilityHitChancePercent = Stat.GetStatAdjustment(attacker, StatType.PredatorsMarkAbilityHitChancePercentPerStack);
            var durationSeconds = Stat.GetStatAdjustment(attacker, StatType.PredatorsMarkFollowUpDurationSeconds);
            var maximumStacks = Stat.GetStatAdjustment(attacker, StatType.PredatorsMarkFollowUpMaximumStacks);

            if (durationSeconds <= 0 || maximumStacks <= 0)
                return;

            if (hastePercent > 0)
            {
                TemporaryStatModifier.AddCapped(
                    attacker,
                    StatType.AttackDelayReductionPercent,
                    hastePercent,
                    durationSeconds,
                    hastePercent * maximumStacks,
                    StatType.PredatorsMarkHastePercentPerStack,
                    1);
            }

            if (abilityHitChancePercent <= 0)
                return;

            TemporaryStatModifier.AddCapped(
                attacker,
                StatType.AbilityHitChancePercentAdjustment,
                abilityHitChancePercent,
                durationSeconds,
                abilityHitChancePercent * maximumStacks,
                StatType.PredatorsMarkAbilityHitChancePercentPerStack,
                1);
            TemporaryStatModifier.Replace(
                attacker,
                StatType.AbilityHitChancePercentAdjustmentSkillType,
                (int)SkillType.BeastMastery,
                durationSeconds,
                StatType.PredatorsMarkAbilityHitChancePercentPerStack);
        }

        internal static void ApplyLowHPDamageDealtHPRestore(uint attacker, int damage)
        {
            var threshold = Stat.GetStatAdjustment(attacker, StatType.LowHPDamageDealtHPRestoreThresholdPercent);
            var hpRestorePercent = Stat.GetStatAdjustment(attacker, StatType.LowHPDamageDealtHPPercentRestore);
            if (threshold <= 0 || hpRestorePercent <= 0)
                return;

            var maxHP = GetMaxHitPoints(attacker);
            if (maxHP <= 0 || GetCurrentHitPoints(attacker) >= maxHP * (threshold / 100f))
                return;

            LowHPReactions.HealFromDamage(attacker, damage, hpRestorePercent);
        }

        internal static void ApplyBleedingTargetStaminaRestore(uint attacker, uint defender)
        {
            if (!GetIsObjectValid(defender) ||
                !StatusEffect.HasStatusEffectCategory(defender, StatusEffectCategory.Bleeding))
                return;

            var chance = Stat.GetStatAdjustment(attacker, StatType.DamageDealtBleedingTargetStaminaRestoreChance);
            var staminaRestore = Stat.GetStatAdjustment(attacker, StatType.DamageDealtBleedingTargetStaminaRestore);
            if (chance <= 0 || staminaRestore <= 0 || Random.D100(1) > chance)
                return;

            Stat.RestoreStamina(attacker, staminaRestore);
        }

        internal static void ApplyDamageDealtForceErosionEffect(
            uint attacker,
            uint defender,
            CombatDamageDeliveryType deliveryType)
        {
            if (deliveryType != CombatDamageDeliveryType.Direct)
                return;

            var duration = Stat.GetStatAdjustment(attacker, StatType.DamageDealtForceErosionDurationSeconds);
            if (duration <= 0)
                return;

            var fpLossPerTick = Stat.GetStatAdjustment(attacker, StatType.DamageDealtForceErosionFPLossPerTick);
            var staminaLossPerTick = Stat.GetStatAdjustment(attacker, StatType.DamageDealtForceErosionStaminaLossPerTick);
            StatusEffect.ApplyStatusEffect(
                attacker,
                defender,
                new ForceErosionStatusEffect(fpLossPerTick, staminaLossPerTick),
                duration,
                CombatDamageType.Physical);
        }

        internal static void ApplyDamageDealtHamstringEffect(
            uint attacker,
            uint defender,
            SkillType skillType,
            CombatDamageType damageType)
        {
            if (!GetIsObjectValid(defender))
                return;

            var requiredSkillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.DamageDealtHamstringSkillType));
            var chance = Stat.GetStatAdjustment(attacker, StatType.DamageDealtHamstringChance);
            var duration = Stat.GetStatAdjustment(attacker, StatType.DamageDealtHamstringDurationSeconds);

            if (chance <= 0 ||
                duration <= 0 ||
                !AbilityImpactEffects.SkillTypeMatches(skillType, requiredSkillType) ||
                Random.D100(1) > chance)
            {
                return;
            }

            StatusEffect.ApplyStatusEffect(
                attacker,
                defender,
                typeof(HamstringStatusEffect),
                duration,
                damageType);
        }

        internal static void ApplySideAttackDamageEffects(uint attacker, uint defender, SkillType skillType, int damage)
        {
            if (damage <= 0 || !SideCriticalEffects.IsMatchingSideAttack(attacker, defender, skillType))
                return;

            var staminaRestore = Stat.GetStatAdjustment(attacker, StatType.SideAttackStaminaRestore);
            var staminaCooldown = Stat.GetStatAdjustment(attacker, StatType.SideAttackStaminaRestoreCooldownSeconds);
            if (staminaRestore > 0 && CombatStatTriggers.TryUseStatTrigger(attacker, StatType.SideAttackStaminaRestore, staminaCooldown))
            {
                Stat.RestoreStamina(attacker, staminaRestore);
            }

            var delayReduction = Stat.GetStatAdjustment(attacker, StatType.SideAttackDelayReductionPercent);
            var duration = Stat.GetStatAdjustment(attacker, StatType.SideAttackDelayReductionDurationSeconds);
            if (delayReduction != 0 && duration > 0)
            {
                TemporaryStatModifier.Replace(
                    attacker,
                    StatType.AttackDelayReductionPercent,
                    delayReduction,
                    duration,
                StatType.SideAttackDelayReductionPercent);
            }
        }

        internal static void ApplyDamageDealtStaminaRestore(uint attacker, SkillType skillType)
        {
            var requiredSkillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.DamageDealtStaminaRestoreSkillType));
            var staminaRestore = Stat.GetStatAdjustment(attacker, StatType.DamageDealtStaminaRestore);
            var cooldown = Stat.GetStatAdjustment(attacker, StatType.DamageDealtStaminaRestoreCooldownSeconds);

            if (staminaRestore <= 0 ||
                !AbilityImpactEffects.SkillTypeMatches(skillType, requiredSkillType) ||
                !CombatStatTriggers.TryUseStatTrigger(attacker, StatType.DamageDealtStaminaRestore, cooldown))
            {
                return;
            }

            Stat.RestoreStamina(attacker, staminaRestore);
        }

        internal static void ApplyDamageDealtAttackDelayReduction(uint attacker, SkillType skillType)
        {
            var requiredSkillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(
                attacker,
                StatType.DamageDealtAttackDelayReductionSkillType));
            var delayReduction = Stat.GetStatAdjustment(attacker, StatType.DamageDealtAttackDelayReductionPercent);
            var duration = Stat.GetStatAdjustment(attacker, StatType.DamageDealtAttackDelayReductionDurationSeconds);

            if (delayReduction == 0 ||
                duration <= 0 ||
                !AbilityImpactEffects.SkillTypeMatches(skillType, requiredSkillType))
            {
                return;
            }

            TemporaryStatModifier.Replace(
                attacker,
                StatType.AttackDelayReductionPercent,
                delayReduction,
                duration,
                StatType.DamageDealtAttackDelayReductionPercent);
        }

    }
}
