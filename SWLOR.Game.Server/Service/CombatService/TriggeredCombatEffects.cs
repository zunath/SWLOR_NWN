using System.Linq;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.CombatService
{
    public static class TriggeredCombatEffects
    {
        public static int ApplyTriggeredDamage(
            uint activator,
            uint target,
            int damage,
            CombatDamageType damageType,
            SkillType skillType = SkillType.Invalid)
        {
            if (damage <= 0)
                return 0;

            damage = Resistance.ApplyResistanceToDamage(target, damageType, damage);
            if (damage <= 0)
                return 0;

            damage = CombatDamageCalculator.ApplyDamageTakenModifiers(target, damage, activator, damageType);
            if (damage <= 0)
                return 0;

            var effectDamageType = damageType.GetNWScriptDamageType();
            if (!Ability.TryQueueTrackedDamageEffect(activator, target, damage, effectDamageType))
            {
                AssignCommand(
                    activator,
                    () => ApplyEffectToObject(
                        DurationType.Instant,
                        EffectDamage(damage, effectDamageType),
                        target));
            }

            DamageDealtEffects.ApplyDamageDealtEffects(activator, target, damage, skillType, damageType, CombatDamageDeliveryType.Triggered);
            StatusEffect.NotifyDamageStatusEffects(activator, target, damage, damageType, CombatDamageDeliveryType.Triggered);
            return damage;
        }

        internal static void ApplyGuardiansResolve(uint activator)
        {
            var shieldPercent = Stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeDefenseGuardiansResolveShieldPercent);
            var duration = Stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeDefenseGuardiansResolveDurationSeconds);
            var cooldown = Stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeDefenseGuardiansResolveCooldownSeconds);
            if (shieldPercent <= 0 || duration <= 0 || !CombatStatTriggers.TryUseStatTrigger(activator, StatType.HeavyVibrobladeDefenseGuardiansResolveShieldPercent, cooldown))
                return;

            var shieldAmount = Math.Max(1, (int)Math.Ceiling(GetMaxHitPoints(activator) * (shieldPercent / 100f)));
            ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(shieldAmount), activator, duration);
            StatusEffect.ApplyStatusEffect(activator, activator, new GuardiansResolveStatusEffect(shieldAmount), duration);
        }

        internal static void ApplyHeavyVibrobladeActivatedEffects(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (AbilityImpactEffects.AbilityMatchesAnyPerkTypeStat(
                    activator,
                    ability,
                    StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerPrimaryPerkType,
                    StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerSecondaryPerkType,
                    StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerTertiaryPerkType,
                    StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerQuaternaryPerkType,
                    StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerQuinaryPerkType,
                    StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerSenaryPerkType))
            {
                AbilityRecoveryEffects.ApplyNextAutoAttackDamageBonus(
                    activator,
                    StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageBonus,
                    StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageDurationSeconds);
            }

            if (AbilityImpactEffects.AbilityMatchesAnyPerkTypeStat(
                    activator,
                    ability,
                    StatType.HeavyVibrobladeDefenseGuardiansResolveTriggerPrimaryPerkType,
                    StatType.HeavyVibrobladeDefenseGuardiansResolveTriggerSecondaryPerkType,
                    StatType.HeavyVibrobladeDefenseGuardiansResolveTriggerTertiaryPerkType,
                    StatType.HeavyVibrobladeDefenseGuardiansResolveTriggerQuaternaryPerkType))
            {
                TriggeredCombatEffects.ApplyGuardiansResolve(activator);
            }

        }

        internal static void ApplyBeastBalancedAbilityRecovery(uint activator, AbilityDetail ability)
        {
            if (!AbilityImpactEffects.AbilityMatchesPerkCategoryStat(
                    activator,
                    ability,
                    StatType.BeastBalancedAbilityStaminaRestoreCategoryId))
            {
                return;
            }

            var staminaRestore = Stat.GetStatAdjustment(activator, StatType.BeastBalancedAbilityStaminaRestore);
            var cooldown = Stat.GetStatAdjustment(activator, StatType.BeastBalancedAbilityStaminaRestoreCooldownSeconds);
            if (staminaRestore <= 0 || !CombatStatTriggers.TryUseStatTrigger(activator, StatType.BeastBalancedAbilityStaminaRestore, cooldown))
                return;

            Stat.RestoreStamina(activator, staminaRestore);

            var master = GetMaster(activator);
            if (GetIsObjectValid(master))
            {
                Stat.RestoreStamina(master, staminaRestore);
            }
        }

        internal static void ApplyVibroknifeShadowActivatedEffects(
            uint activator,
            AbilityDetail ability)
        {
            var rank = Stat.GetStatAdjustment(activator, StatType.VibroknifeShadowEvasiveCombatRank);
            if (rank <= 0 || !ability.IsAreaAbility && !ability.IsSingleTargetAbility)
                return;

            var evasion = rank >= 2 ? 20 : 10;
            var enmity = rank >= 2 ? -25 : -15;
            StatusEffect.ApplyStatusEffect(
                activator,
                activator,
                new EvasiveCombatStatusEffect(-15, evasion, enmity),
                8f);
        }

        internal static void ApplyPistolSkirmisherActivatedEffects(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            var duration = Stat.GetStatAdjustment(activator, StatType.PistolSkirmisherEvasiveAbilityDurationSeconds);
            if (duration <= 0 || !ability.IsAreaAbility && !ability.IsSingleTargetAbility)
                return;

            var evasion = Stat.GetStatAdjustment(activator, StatType.PistolSkirmisherEvasiveAbilityEvasionPercent);
            if (evasion != 0)
            {
                StatusEffect.ApplyStatusEffect(
                    activator,
                    activator,
                    new SnapRollStatusEffect(evasion),
                    duration);
            }

            var nextDamage = Stat.GetStatAdjustment(activator, StatType.PistolSkirmisherEvasiveAbilityNextAttackDamageBonus);
            if (nextDamage > 0)
            {
                AbilityImpactEffects.GrantNextSkillAbilityBonuses(activator, SkillType.Pistol, nextDamage, 0, duration);
            }

            var reduction = Stat.GetStatAdjustment(activator, StatType.PistolSkirmisherEvasiveAbilityEnmityReductionPercent);
            if (reduction > 0 && GetIsObjectValid(target))
            {
                Enmity.ReduceEnmity(activator, target, reduction);
            }
        }

        internal static void ApplyLightsaberOffenseActivatedEffects(uint activator, uint target)
        {
            TriggeredCombatEffects.ApplyLightsaberOffenseCentering(activator, target);
            TriggeredCombatEffects.ApplyLightsaberOffenseSecondWind(activator);
        }

        internal static void ApplyLightsaberOffenseCentering(uint activator, uint target)
        {
            var accuracy = Stat.GetStatAdjustment(activator, StatType.LightsaberOffenseCenteringAccuracyPercent);
            var duration = Stat.GetStatAdjustment(activator, StatType.LightsaberOffenseCenteringDurationSeconds);
            var cooldown = Stat.GetStatAdjustment(activator, StatType.LightsaberOffenseCenteringCooldownSeconds);
            if (accuracy <= 0 ||
                duration <= 0 ||
                !CombatStatTriggers.TryUseStatTrigger(activator, StatType.LightsaberOffenseCenteringAccuracyPercent, cooldown))
            {
                return;
            }

            StatusEffect.ApplyStatusEffect(activator, activator, new CenteringStatusEffect(accuracy), duration);

            var enmityReduction = Stat.GetStatAdjustment(activator, StatType.LightsaberOffenseCenteringEnmityReductionPercent);
            if (enmityReduction > 0 && GetIsObjectValid(target))
            {
                Enmity.ReduceEnmity(activator, target, enmityReduction);
            }
        }

        internal static void ApplyLightsaberOffenseSecondWind(uint activator)
        {
            var thresholdPercent = Stat.GetStatAdjustment(activator, StatType.LightsaberOffenseSecondWindThresholdPercent);
            var basePercent = Stat.GetStatAdjustment(activator, StatType.LightsaberOffenseSecondWindStaminaRestoreBasePercent);
            if (thresholdPercent <= 0 || basePercent <= 0)
                return;

            var maximumStamina = Stat.GetMaxStamina(activator);
            if (maximumStamina <= 0 ||
                Stat.GetCurrentStamina(activator) > maximumStamina * (thresholdPercent / 100f))
            {
                return;
            }

            var cooldown = Stat.GetStatAdjustment(activator, StatType.LightsaberOffenseSecondWindCooldownSeconds);
            if (!CombatStatTriggers.TryUseStatTrigger(activator, StatType.LightsaberOffenseSecondWindStaminaRestoreBasePercent, cooldown))
                return;

            var percent = basePercent;
            var scalingAbility = AbilityImpactEffects.GetAbilityTypeFromStatPlusOne(Stat.GetStatAdjustment(
                activator,
                StatType.LightsaberOffenseSecondWindScalingAbility));
            if (scalingAbility != AbilityType.Invalid)
            {
                percent += Math.Max(0, GetAbilityScore(activator, scalingAbility));
            }

            var maximumPercent = Stat.GetStatAdjustment(activator, StatType.LightsaberOffenseSecondWindStaminaRestoreMaximumPercent);
            if (maximumPercent > 0)
            {
                percent = Math.Min(percent, maximumPercent);
            }

            Stat.RestoreStamina(activator, Math.Max(1, (int)Math.Ceiling(maximumStamina * (percent / 100f))));
        }

        internal static void ApplyLightsaberDefenseActivatedEffects(uint activator)
        {
            var attackDeflection = Stat.GetStatAdjustment(activator, StatType.LightsaberDefenseGuardiansInfluenceAttackDeflection);
            if (attackDeflection <= 0)
                return;

            foreach (var friendly in AbilityTargeting.GetFriendlyTargetsNearLocation(activator, GetLocation(activator), 5f, false))
            {
                TemporaryStatModifier.Replace(
                    friendly,
                    StatType.AttackDeflection,
                    attackDeflection,
                    12f,
                    StatType.LightsaberDefenseGuardiansInfluenceAttackDeflection);
            }
        }

        internal static void ApplyLightsaberWardActivatedEffects(
            uint activator,
            AbilityDetail ability)
        {
            if (!AbilityImpactEffects.AbilityMatchesPerkCategoryStat(
                    activator,
                    ability,
                    StatType.WardAbilityDefenseCategoryId))
            {
                return;
            }

            var defense = Stat.GetStatAdjustment(activator, StatType.WardAbilityDefensePercentAdjustment);
            var forceDefense = Stat.GetStatAdjustment(activator, StatType.WardAbilityForceDefensePercentAdjustment);
            var duration = Stat.GetStatAdjustment(activator, StatType.WardAbilityDefenseDurationSeconds);
            if (duration <= 0 || defense == 0 && forceDefense == 0)
                return;

            if (defense != 0)
            {
                TemporaryStatModifier.Replace(
                    activator,
                    StatType.PhysicalDefensePercentAdjustment,
                    defense,
                    duration,
                    StatType.WardAbilityDefensePercentAdjustment);
                TemporaryStatModifier.Replace(
                    activator,
                    StatType.WardTargetPhysicalDefensePercentAdjustment,
                    defense,
                    duration,
                    StatType.WardAbilityDefensePercentAdjustment);
            }

            if (forceDefense != 0)
            {
                TemporaryStatModifier.Replace(
                    activator,
                    StatType.ForceDefensePercentAdjustment,
                    forceDefense,
                    duration,
                    StatType.WardAbilityForceDefensePercentAdjustment);
                TemporaryStatModifier.Replace(
                    activator,
                    StatType.WardTargetForceDefensePercentAdjustment,
                    forceDefense,
                    duration,
                    StatType.WardAbilityForceDefensePercentAdjustment);
            }
        }

        internal static void ApplySaberstaffConduitActivatedEffects(
            uint activator,
            AbilityDetail ability)
        {
            if (ability.IsHostileAbility ||
                Stat.GetStatAdjustment(activator, StatType.SaberstaffConduitForceLens) <= 0)
            {
                return;
            }

            foreach (var friendly in AbilityTargeting.GetFriendlyTargetsNearLocation(activator, GetLocation(activator), 5f))
            {
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(ForceLensStatusEffect), 30f);
            }
        }

        internal static void ApplyAbilityUsedPerkCategorySelfDefense(
            uint activator,
            AbilityDetail ability)
        {
            if (!AbilityImpactEffects.AbilityMatchesPerkCategoryStat(
                    activator,
                    ability,
                    StatType.AbilityUsedPerkCategorySelfDefenseCategoryId))
            {
                return;
            }

            var evasion = Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategorySelfEvasionPercentAdjustment);
            var defense = Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategorySelfDefensePercentAdjustment);
            var forceDefense = Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategorySelfForceDefensePercentAdjustment);
            var duration = Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategorySelfDefenseDurationSeconds);
            if (duration <= 0 || evasion == 0 && defense == 0 && forceDefense == 0)
                return;

            var cooldown = Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategorySelfDefenseCooldownSeconds);
            if (!CombatStatTriggers.TryUseStatTrigger(
                    activator,
                    StatType.AbilityUsedPerkCategorySelfDefensePercentAdjustment,
                    cooldown))
            {
                return;
            }

            StatusEffect.ApplyStatusEffect(
                activator,
                activator,
                new SelfDefensiveStatsStatusEffect(
                    evasion,
                    defense,
                    forceDefense,
                    "Guarding Step",
                    EffectIconType.GuardingStepStatusEffect),
                duration);
        }

        internal static void ApplyAbilityUsedPerkCategoryNearbyAllyAttackDeflection(
            uint activator,
            AbilityDetail ability)
        {
            if (!AbilityImpactEffects.AbilityMatchesPerkCategoryStat(
                    activator,
                    ability,
                    StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflectionCategoryId))
            {
                return;
            }

            var attackDeflection = Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflection);
            var duration = Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflectionDurationSeconds);
            var selfEnmity = Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflectionSelfEnmityPercentAdjustment);
            if (attackDeflection <= 0 || duration <= 0)
                return;

            foreach (var friendly in AbilityTargeting.GetFriendlyTargetsNearLocation(activator, GetLocation(activator), 5f))
            {
                StatusEffect.ApplyStatusEffect(
                    activator,
                    friendly,
                    new NearbyAllyAttackDeflectionStatusEffect(
                        attackDeflection,
                        selfEnmity,
                        "Sentinel Guard",
                        EffectIconType.SentinelGuardStatusEffect),
                    duration);
            }
        }

    }
}
