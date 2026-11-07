using System.Collections.Generic;
using System.Linq;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.ActivityService;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatRecorderService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using ObjectPlugin = SWLOR.NWN.API.NWNX.ObjectPlugin;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;

namespace SWLOR.Game.Server.Service
{
    public static partial class Combat
    {
        private static readonly Dictionary<uint, HostileAbilitySequenceState> _hostileAbilitySequenceStates = new();

        private sealed class HostileAbilitySequenceState
        {
            public FeatType LastFeat { get; init; }
            public DateTime LastUse { get; init; }
        }

        public static void ApplyAbilityActivatedEffects(
            uint activator,
            uint target,
            FeatType feat,
            AbilityDetail ability,
            AbilityImpactSummary summary)
        {
            if (!GetIsObjectValid(activator) || ability == null || summary == null)
                return;

            ApplyAbilityUsedRecastReduction(activator, ability);
            ApplyAbilityUsedNextSkillAutoAttackDamageBonus(activator, ability);
            ApplyAbilityUsedNextSkillFPCostAdjustment(activator, ability);
            ApplyAbilityUsedMasterAbilityHitChance(activator);
            ApplyForceFPCostActivatedEffects(activator, ability);

            var skillType = ResolveActivatedAbilitySkillType(activator, ability, summary);
            var isSingleTargetAbility = summary.IsSingleTargetAbility ||
                summary.SkillType == SkillType.Invalid &&
                ability.IsHostileAbility &&
                ability.IsSingleTargetAbility;

            ApplyAbilityUsedSkillEvasion(activator, ability);
            ApplyHostileAbilityUsedEvasion(activator, ability, skillType);
            ApplyCostlyAbilityUsedEvasion(activator, ability, skillType);
            ApplyAbilityUsedSkillRangedDeflection(activator, ability);
            ApplyAbilityUsedMovementSpeed(activator, ability, skillType);
            ApplyAbilityUsedSkillAttackDeflection(activator, ability);
            ApplyAbilityUsedPerkCategoryAttackDeflection(activator, ability);
            ApplySingleTargetAbilityUsedAttackDeflection(activator, ability, isSingleTargetAbility);
            var isAreaAbility = Ability.IsAreaImpact(summary, ability);
            ApplyAreaAbilityUsedEvasion(activator, ability, skillType, isAreaAbility);
            ApplyAreaAbilityUsedRewards(activator, ability, isAreaAbility);
            ApplyHostileAbilityForceAttack(activator, ability);
            ApplyHostileAbilityFPSpendForceAttack(activator, ability);
            ApplyAbilityUsedNearbyAllyDefense(activator);
            ApplyAbilityUsedPerkCategoryNearbyAllyAttackDeflection(activator, ability);
            ApplyAbilityUsedPerkCategorySelfDefense(activator, ability);
            ApplyHostileAbilityUsedAttackAdjustment(activator, ability);
            ApplyAbilityActivatedRiders(activator, target, ability, skillType);
            ApplyHostileAbilitySequenceEffects(activator, feat, ability);
            ApplyHostileAbilityResourceRestoreEffects(activator, ability);

            if (ability.ActivationType != AbilityActivationType.Weapon)
                TrackCombatAbilityUse(activator, ability);
        }

        public static void TrackQueuedWeaponAbilityUse(uint activator, AbilityDetail ability)
        {
            TrackCombatAbilityUse(activator, ability);
        }

        private static SkillType ResolveActivatedAbilitySkillType(
            uint activator,
            AbilityDetail ability,
            AbilityImpactSummary summary)
        {
            return summary.SkillType != SkillType.Invalid
                ? summary.SkillType
                : GetAbilitySkillType(activator, ability);
        }

        private static void ApplyAreaAbilityUsedRewards(uint activator, AbilityDetail ability, bool isAreaAbility)
        {
            if (ability?.IsHostileAbility != true || !isAreaAbility)
                return;

            var grantedDeflection = false;
            foreach (var source in Stat.GetStatSources(activator, StatType.AreaAbilityUsedAttackDeflection))
            {
                var deflection = source[StatType.AreaAbilityUsedAttackDeflection];
                var duration = source[StatType.AreaAbilityUsedAttackDeflectionDurationSeconds];
                if (deflection <= 0 || duration <= 0)
                    continue;

                TemporaryStatModifier.Replace(activator,
                    Stat.GetGrantedDeflectionStatType(StatType.AreaAbilityUsedAttackDeflection),
                    deflection, duration, source.GetModifierGroup(StatType.AreaAbilityUsedAttackDeflection));
                grantedDeflection = true;
            }
            if (grantedDeflection)
                ApplyAbilityGrantedAttackDeflectionEffects(activator,
                    Stat.GetStatTypeDeflectionSource(StatType.AreaAbilityUsedAttackDeflection));

            var restore = Stat.GetStatAdjustment(activator, StatType.AreaAbilityUsedFPRestore);
            if (restore > 0 && RestoreFPFromStat(activator, StatType.AreaAbilityUsedFPRestore, restore) > 0)
                ApplyAbilityRestoredFPEffects(activator);
        }

        private static void ApplyAreaAbilityUsedEvasion(
            uint activator,
            AbilityDetail ability,
            SkillType skillType,
            bool isAreaAbility)
        {
            if (ability?.IsHostileAbility != true || !isAreaAbility)
                return;

            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.AreaAbilityUsedEvasionPercentAdjustmentSkillType));
            if (!SkillTypeMatchesOrGlobal(skillType, requiredSkillType))
                return;

            var evasion = Stat.GetStatAdjustment(activator, StatType.AreaAbilityUsedEvasionPercentAdjustment);
            var duration = Stat.GetStatAdjustment(activator, StatType.AreaAbilityUsedEvasionDurationSeconds);
            if (evasion == 0 || duration <= 0)
                return;

            TemporaryStatModifier.Replace(
                activator,
                StatType.EvasionPercentAdjustment,
                evasion,
                duration,
                StatType.AreaAbilityUsedEvasionPercentAdjustment);
        }

        private static void ApplyHostileAbilityUsedEvasion(
            uint activator,
            AbilityDetail ability,
            SkillType skillType)
        {
            if (ability?.IsHostileAbility != true)
                return;

            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.HostileAbilityUsedEvasionPercentAdjustmentSkillType));
            if (!SkillTypeMatchesOrGlobal(skillType, requiredSkillType))
                return;

            ApplyAbilityUsedEvasion(
                activator,
                StatType.HostileAbilityUsedEvasionPercentAdjustment,
                StatType.HostileAbilityUsedEvasionDurationSeconds);
        }

        private static void ApplyCostlyAbilityUsedEvasion(
            uint activator,
            AbilityDetail ability,
            SkillType skillType)
        {
            if (ability?.IsHostileAbility != true ||
                !TryGetAbilityStaminaCostState(activator, ability, out var costState))
            {
                return;
            }

            var requiredSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.CostlyAbilityUsedEvasionPercentAdjustmentSkillType));
            var minimumCost = Stat.GetStatAdjustment(
                activator,
                StatType.CostlyAbilityUsedEvasionMinimumStaminaCost);
            if (!SkillTypeMatchesOrGlobal(skillType, requiredSkillType) ||
                minimumCost <= 0 ||
                costState.Cost < minimumCost)
            {
                return;
            }

            ApplyAbilityUsedEvasion(
                activator,
                StatType.CostlyAbilityUsedEvasionPercentAdjustment,
                StatType.CostlyAbilityUsedEvasionDurationSeconds);
        }

        private static void ApplyAbilityUsedMovementSpeed(
            uint activator,
            AbilityDetail ability,
            SkillType skillType)
        {
            if (ability == null || !ability.IsHostileAbility)
                return;

            if (!AbilityUsedTriggerSkillMatches(
                    activator,
                    ability,
                    skillType,
                    StatType.AbilityUsedMovementSpeedPercentAdjustmentSkillType,
                    StatType.AbilityUsedMovementSpeedRangedOnly))
            {
                return;
            }

            var movementSpeed = Stat.GetStatAdjustment(activator, StatType.AbilityUsedMovementSpeedPercentAdjustment);
            var duration = Stat.GetStatAdjustment(activator, StatType.AbilityUsedMovementSpeedDurationSeconds);
            if (movementSpeed == 0 || duration <= 0)
                return;

            TemporaryStatModifier.Replace(
                activator,
                StatType.MovementSpeedPercentAdjustment,
                movementSpeed,
                duration,
                StatType.AbilityUsedMovementSpeedPercentAdjustment);
        }

        private static void ApplyHostileAbilityForceAttack(uint activator, AbilityDetail ability)
        {
            if (ability == null || !ability.IsHostileAbility)
                return;

            var forceAttack = Stat.GetStatAdjustment(activator, StatType.HostileAbilityForceAttackPercentPerStack);
            var duration = Stat.GetStatAdjustment(activator, StatType.HostileAbilityForceAttackDurationSeconds);
            var maximum = Stat.GetStatAdjustment(activator, StatType.HostileAbilityForceAttackPercentMax);
            if (forceAttack == 0 || duration <= 0 || maximum <= 0)
                return;

            var current = StatusEffect.GetStatusEffect(
                activator,
                typeof(HostileAbilityForceAttackStatusEffect)) as HostileAbilityForceAttackStatusEffect;
            var total = Math.Min(maximum, (current?.ForceAttack ?? 0) + forceAttack);

            StatusEffect.ApplyStatusEffect(
                activator,
                activator,
                new HostileAbilityForceAttackStatusEffect(total),
                duration);
        }

        /// <summary>
        /// Grants stacking Force Attack when the activated hostile ability costs at least the
        /// configured minimum FP. Drives the Lightsaber Severance "Overpower" trait.
        /// </summary>
        private static void ApplyHostileAbilityFPSpendForceAttack(uint activator, AbilityDetail ability)
        {
            if (ability == null || !ability.IsHostileAbility)
                return;

            var forceAttack = Stat.GetStatAdjustment(activator, StatType.HostileAbilityFPSpendForceAttackPercent);
            var duration = Stat.GetStatAdjustment(activator, StatType.HostileAbilityFPSpendForceAttackDurationSeconds);
            var maximum = Stat.GetStatAdjustment(activator, StatType.HostileAbilityFPSpendForceAttackMaxPercent);
            var minimumFPCost = Stat.GetStatAdjustment(activator, StatType.HostileAbilityFPSpendForceAttackMinFPCost);
            if (forceAttack <= 0 || duration <= 0 || maximum <= 0 || minimumFPCost <= 0)
                return;

            var fpCost = ability.Requirements
                .OfType<AbilityRequirementFP>()
                .Sum(x => x.RequiredFP);
            if (fpCost < minimumFPCost)
                return;

            TemporaryStatModifier.AddCapped(
                activator,
                StatType.ForceAttackPercentAdjustment,
                forceAttack,
                duration,
                maximum,
                StatType.HostileAbilityFPSpendForceAttackPercent,
                1,
                refreshExistingStacks: true);
        }

        private static void ApplyHostileAbilityUsedAttackAdjustment(uint activator, AbilityDetail ability)
        {
            if (ability == null || !ability.IsHostileAbility)
                return;

            var attack = Stat.GetStatAdjustment(activator, StatType.HostileAbilityUsedAttackPercentAdjustment);
            var duration = Stat.GetStatAdjustment(
                activator,
                StatType.HostileAbilityUsedAttackPercentAdjustmentDurationSeconds);
            var maximum = Stat.GetStatAdjustment(
                activator,
                StatType.HostileAbilityUsedAttackPercentAdjustmentMaximum);
            if (attack <= 0 || duration <= 0 || maximum <= 0)
                return;

            TemporaryStatModifier.AddCapped(
                activator,
                StatType.AttackPercentAdjustment,
                attack,
                duration,
                maximum,
                StatType.HostileAbilityUsedAttackPercentAdjustment,
                1,
                refreshExistingStacks: true);

            var currentTotal = TemporaryStatModifier.GetStatAdjustment(
                activator,
                StatType.AttackPercentAdjustment,
                StatType.HostileAbilityUsedAttackPercentAdjustment);
            if (currentTotal > 0)
            {
                StatusEffect.ApplyStatusEffect(
                    activator,
                    activator,
                    new ButchersTempoStatusEffect(currentTotal),
                    duration);
            }
        }

        private static void ApplyAbilityUsedNearbyAllyDefense(uint activator)
        {
            var defense = Stat.GetStatAdjustment(activator, StatType.AbilityUsedNearbyAllyDefensePercentAdjustment);
            var forceDefense = Stat.GetStatAdjustment(activator, StatType.AbilityUsedNearbyAllyForceDefensePercentAdjustment);
            var duration = Stat.GetStatAdjustment(activator, StatType.AbilityUsedNearbyAllyDefenseDurationSeconds);
            if (duration <= 0 || defense == 0 && forceDefense == 0)
                return;

            foreach (var friendly in AbilityTargeting.GetFriendlyTargetsNearLocation(activator, GetLocation(activator), 5f, false))
            {
                if (defense != 0)
                {
                    TemporaryStatModifier.Replace(
                        friendly,
                        StatType.PhysicalDefensePercentAdjustment,
                        defense,
                        duration,
                        StatType.AbilityUsedNearbyAllyDefensePercentAdjustment);
                }

                if (forceDefense != 0)
                {
                    TemporaryStatModifier.Replace(
                        friendly,
                        StatType.ForceDefensePercentAdjustment,
                        forceDefense,
                        duration,
                        StatType.AbilityUsedNearbyAllyDefensePercentAdjustment);
                }
            }
        }

        private static void ApplyAbilityActivatedRiders(
            uint activator,
            uint target,
            AbilityDetail ability,
            SkillType skillType)
        {
            if (ability == null)
                return;

            switch (skillType)
            {
                case SkillType.HeavyVibroblade:
                    ApplyHeavyVibrobladeActivatedEffects(activator, target, ability);
                    break;
                case SkillType.BeastMastery:
                    ApplyBeastBalancedAbilityRecovery(activator, ability);
                    break;
                case SkillType.Vibroknife:
                    ApplyVibroknifeShadowActivatedEffects(activator, ability);
                    break;
                case SkillType.Pistol:
                    ApplyPistolSkirmisherActivatedEffects(activator, target, ability);
                    break;
                case SkillType.Lightsaber:
                    ApplyLightsaberOffenseActivatedEffects(activator, target);
                    ApplyLightsaberDefenseActivatedEffects(activator);
                    ApplyLightsaberWardActivatedEffects(activator, ability);
                    break;
            }
        }

        private static void ApplyGuardiansResolve(uint activator)
        {
            var shieldPercent = Stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeDefenseGuardiansResolveShieldPercent);
            var duration = Stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeDefenseGuardiansResolveDurationSeconds);
            var cooldown = Stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeDefenseGuardiansResolveCooldownSeconds);
            if (shieldPercent <= 0 || duration <= 0 || !TryUseStatTrigger(activator, StatType.HeavyVibrobladeDefenseGuardiansResolveShieldPercent, cooldown))
                return;

            // The pool can be smaller than requested under the temporary HP cap or at the engine's HP limit.
            using var recorderAttribution = CombatRecorder.BeginStatTriggerAttribution(
                activator, StatType.HeavyVibrobladeDefenseGuardiansResolveShieldPercent);
            var shieldAmount = TemporaryHitPointEffects.ApplyFlat(
                activator,
                TemporaryHitPointEffectKey.GuardiansResolve,
                GameMath.PercentOf(GetMaxHitPoints(activator), shieldPercent),
                duration);
            if (shieldAmount <= 0)
                return;

            var poolGeneration = TemporaryHitPointEffects.GetGrantGeneration(activator, TemporaryHitPointEffectKey.GuardiansResolve);
            StatusEffect.ApplyStatusEffect(activator, activator, new GuardiansResolveStatusEffect(shieldAmount, poolGeneration), duration);
        }

        private static void ApplyHeavyVibrobladeActivatedEffects(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (AbilityMatchesAnyPerkTypeStat(
                    activator,
                    ability,
                    StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerPrimaryPerkType,
                    StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerSecondaryPerkType,
                    StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerTertiaryPerkType,
                    StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerQuaternaryPerkType,
                    StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerQuinaryPerkType,
                    StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageTriggerSenaryPerkType))
            {
                ApplyNextAutoAttackDamageBonus(
                    activator,
                    StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageBonus,
                    StatType.HeavyVibrobladeDefenseAbilityNextAutoAttackDamageDurationSeconds);
            }

            if (AbilityMatchesAnyPerkTypeStat(
                    activator,
                    ability,
                    StatType.HeavyVibrobladeDefenseGuardiansResolveTriggerPrimaryPerkType,
                    StatType.HeavyVibrobladeDefenseGuardiansResolveTriggerSecondaryPerkType,
                    StatType.HeavyVibrobladeDefenseGuardiansResolveTriggerTertiaryPerkType,
                    StatType.HeavyVibrobladeDefenseGuardiansResolveTriggerQuaternaryPerkType))
            {
                ApplyGuardiansResolve(activator);
            }

        }

        private static void ApplyBeastBalancedAbilityRecovery(uint activator, AbilityDetail ability)
        {
            if (!AbilityMatchesPerkCategoryStat(
                    activator,
                    ability,
                    StatType.BeastBalancedAbilityStaminaRestoreCategoryId))
            {
                return;
            }

            var staminaRestore = Stat.GetStatAdjustment(activator, StatType.BeastBalancedAbilityStaminaRestore);
            var cooldown = Stat.GetStatAdjustment(activator, StatType.BeastBalancedAbilityStaminaRestoreCooldownSeconds);
            if (staminaRestore <= 0 || !TryUseStatTrigger(activator, StatType.BeastBalancedAbilityStaminaRestore, cooldown))
                return;

            RestoreStaminaFromStat(activator, StatType.BeastBalancedAbilityStaminaRestore, staminaRestore);

            var master = GetMaster(activator);
            if (GetIsObjectValid(master))
            {
                RestoreStaminaFromStat(master, StatType.BeastBalancedAbilityStaminaRestore, staminaRestore);
            }
        }

        private static void ApplyVibroknifeShadowActivatedEffects(
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

        private static void ApplyPistolSkirmisherActivatedEffects(
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
                TemporaryStatModifier.Replace(
                    activator,
                    StatType.EvasionPercentAdjustment,
                    evasion,
                    duration,
                    StatType.PistolSkirmisherEvasiveAbilityEvasionPercent);
            }

            var nextDamage = Stat.GetStatAdjustment(activator, StatType.PistolSkirmisherEvasiveAbilityNextAttackDamageBonus);
            if (nextDamage > 0)
            {
                GrantNextSkillAbilityBonuses(activator, SkillType.Pistol, nextDamage, 0, duration);
            }

            var reduction = Stat.GetStatAdjustment(activator, StatType.PistolSkirmisherEvasiveAbilityEnmityReductionPercent);
            if (reduction > 0 && GetIsObjectValid(target))
            {
                Enmity.ReduceEnmity(activator, target, reduction);
            }
        }

        private static void ApplyLightsaberOffenseActivatedEffects(uint activator, uint target)
        {
            ApplyLightsaberOffenseCentering(activator, target);
            ApplyLightsaberOffenseSecondWind(activator);
        }

        private static void ApplyLightsaberOffenseCentering(uint activator, uint target)
        {
            var accuracy = Stat.GetStatAdjustment(activator, StatType.LightsaberOffenseCenteringAccuracyPercent);
            var duration = Stat.GetStatAdjustment(activator, StatType.LightsaberOffenseCenteringDurationSeconds);
            var cooldown = Stat.GetStatAdjustment(activator, StatType.LightsaberOffenseCenteringCooldownSeconds);
            if (accuracy <= 0 ||
                duration <= 0 ||
                !TryUseStatTrigger(activator, StatType.LightsaberOffenseCenteringAccuracyPercent, cooldown))
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

        private static void ApplyLightsaberOffenseSecondWind(uint activator)
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
            if (!TryUseStatTrigger(activator, StatType.LightsaberOffenseSecondWindStaminaRestoreBasePercent, cooldown))
                return;

            var percent = basePercent;
            var scalingAbility = GetAbilityTypeFromStatPlusOne(Stat.GetStatAdjustment(
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

            RestoreStaminaFromStat(activator, StatType.LightsaberOffenseSecondWindStaminaRestoreBasePercent,
                GameMath.PercentOf(maximumStamina, percent));
        }

        private static void ApplyLightsaberDefenseActivatedEffects(uint activator)
        {
            var sourceStatType = StatType.LightsaberDefenseGuardiansInfluenceAttackDeflection;
            var attackDeflection = Stat.GetStatAdjustment(activator, sourceStatType);
            var deflectionStatType = Stat.GetGrantedDeflectionStatType(sourceStatType);
            if (deflectionStatType == StatType.Invalid || attackDeflection <= 0)
                return;

            foreach (var friendly in AbilityTargeting.GetFriendlyTargetsNearLocation(activator, GetLocation(activator), 5f, false))
            {
                TemporaryStatModifier.Replace(
                    friendly,
                    deflectionStatType,
                    attackDeflection,
                    12f,
                    sourceStatType);
            }
        }

        private static void ApplyLightsaberWardActivatedEffects(
            uint activator,
            AbilityDetail ability)
        {
            if (!AbilityMatchesPerkCategoryStat(
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

        private static void ApplyAbilityUsedPerkCategorySelfDefense(
            uint activator,
            AbilityDetail ability)
        {
            if (!AbilityMatchesPerkCategoryStat(
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
            if (!TryUseStatTrigger(
                    activator,
                    StatType.AbilityUsedPerkCategorySelfDefensePercentAdjustment,
                    cooldown))
            {
                return;
            }

            StatusEffect.ApplyStatusEffect(
                activator,
                activator,
                new GuardingStepStatusEffect(evasion, defense, forceDefense),
                duration);
        }

        private static void ApplyAbilityUsedPerkCategoryNearbyAllyAttackDeflection(
            uint activator,
            AbilityDetail ability)
        {
            if (!AbilityMatchesPerkCategoryStat(
                    activator,
                    ability,
                    StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflectionCategoryId))
            {
                return;
            }

            var attackDeflection = Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflection);
            var deflectionStatType = Stat.GetGrantedDeflectionStatType(
                StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflection);
            var duration = Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflectionDurationSeconds);
            var selfEnmity = Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategoryNearbyAllyAttackDeflectionSelfEnmityPercentAdjustment);
            if (deflectionStatType == StatType.Invalid || attackDeflection <= 0 || duration <= 0)
                return;

            foreach (var friendly in AbilityTargeting.GetFriendlyTargetsNearLocation(activator, GetLocation(activator), 5f))
            {
                StatusEffect.ApplyStatusEffect(
                    activator,
                    friendly,
                    new SentinelGuardStatusEffect(attackDeflection, selfEnmity, deflectionStatType),
                    duration);
            }
        }

        /// <param name="hitPointsSpent">HP actually paid. Nothing triggers when no HP was paid.</param>
        /// <param name="hitPointCost">Full HP cost before the payment was clipped at 1 HP; 0 when it was paid in full.</param>
        public static void ApplyHitPointSpendAbilityEffects(uint activator, int hitPointsSpent, int hitPointCost = 0)
        {
            if (hitPointsSpent <= 0)
                return;

            var window = Math.Max(1, Stat.GetStatAdjustment(
                activator,
                StatType.HeavyVibrobladeOffenseHitPointSpendWindowSeconds));

            if (Stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeOffenseHitPointSpendSoulSacrifice) > 0)
            {
                StatusEffect.ApplyStatusEffect(activator, activator, typeof(SoulSacrificeStatusEffect), 30f);
            }

            if (Stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeOffenseSoulAscension) > 0)
            {
                TemporaryStatModifier.Replace(
                    activator,
                    StatType.HeavyVibrobladeOffenseSoulAscension,
                    1,
                    window,
                    StatType.HeavyVibrobladeOffenseHitPointSpendWindowSeconds);
            }

            ApplyHitPointSpendStaminaRestore(activator, hitPointsSpent, hitPointCost);
            ApplyHitPointSpendTemporaryHitPoints(activator, hitPointsSpent);
        }

        private static void ApplyHitPointSpendTemporaryHitPoints(uint activator, int hitPointsSpent)
        {
            if (hitPointsSpent <= 0)
                return;

            var percent = Stat.GetStatAdjustment(activator, StatType.HitPointSpendTemporaryHPPercentOfSpentHP);
            var duration = Stat.GetStatAdjustment(activator, StatType.HitPointSpendTemporaryHPDurationSeconds);
            if (percent <= 0 || duration <= 0)
                return;

            var temporaryHP = GameMath.PercentOf(hitPointsSpent, percent);
            using var recorderAttribution = CombatRecorder.BeginStatTriggerAttribution(activator, StatType.HitPointSpendTemporaryHPPercentOfSpentHP);
            TemporaryHitPointEffects.ApplyFlat(activator, "HIT_POINT_SPEND", temporaryHP, duration);
        }

        private static void ApplyHitPointSpendStaminaRestore(uint activator, int hitPointsSpent, int hitPointCost)
        {
            var basePercent = Stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreBasePercent);
            if (basePercent <= 0)
                return;

            var cooldown = Stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreCooldownSeconds);
            if (!TryUseStatTrigger(activator, StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreBasePercent, cooldown))
                return;

            var percent = basePercent;
            var scalingAbility = GetAbilityTypeFromStatPlusOne(Stat.GetStatAdjustment(
                activator,
                StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreScalingAbility));
            if (scalingAbility != AbilityType.Invalid)
            {
                percent += Math.Max(0, GetAbilityScore(activator, scalingAbility));
            }

            var maximum = Stat.GetStatAdjustment(activator, StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreMaximumPercent);
            if (maximum > 0)
            {
                percent = Math.Min(maximum, percent);
            }

            RestoreStaminaFromStat(
                activator,
                StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreBasePercent,
                CalculateHitPointSpendStaminaRestore(Stat.GetMaxStamina(activator), percent, hitPointsSpent, hitPointCost));
        }

        private static void ApplyForceFPCostActivatedEffects(uint activator, AbilityDetail ability)
        {
            if (!Ability.IsForcePower(activator, ability) ||
                !ability.Requirements.OfType<AbilityRequirementFP>().Any(x => x.RequiredFP > 0))
            {
                return;
            }

            if (Stat.GetStatAdjustment(activator, StatType.ForcePrecognition) > 0 &&
                TryUseStatTrigger(activator, StatType.ForcePrecognition, 12))
            {
                StatusEffect.ApplyStatusEffect(activator, activator, typeof(DangerSenseStatusEffect), 30f);
            }

            if (Stat.GetStatAdjustment(activator, StatType.ForceConvergence) > 0 &&
                TryUseStatTrigger(activator, StatType.ForceConvergence, 45))
            {
                StatusEffect.ApplyStatusEffect(activator, activator, typeof(ForceConvergenceStatusEffect), 30f);
            }
        }

        private static void ApplyAbilityUsedMasterAbilityHitChance(uint activator)
        {
            var master = GetMaster(activator);
            if (!GetIsObjectValid(master))
                return;

            var adjustment = Stat.GetStatAdjustment(activator, StatType.AbilityUsedMasterAbilityHitChancePercentAdjustment);
            var duration = Stat.GetStatAdjustment(activator, StatType.AbilityUsedMasterAbilityHitChanceDurationSeconds);
            if (adjustment == 0 || duration <= 0)
                return;

            TemporaryStatModifier.Replace(
                master,
                StatType.AbilityHitChancePercentAdjustment,
                adjustment,
                duration,
                StatType.AbilityUsedMasterAbilityHitChancePercentAdjustment);
        }

        private static void ApplyAbilityUsedRecastReduction(uint activator, AbilityDetail ability)
        {
            ApplyAbilityUsedRecastReduction(activator, ability?.RecastGroup ?? RecastGroup.Invalid);
        }

        private static void ApplyAbilityUsedRecastReduction(uint activator, RecastGroup activatedRecastGroup)
        {
            var triggerGroup = GetRecastGroupFromStat(Stat.GetStatAdjustment(activator, StatType.AbilityUsedRecastReductionTriggerGroup));
            var secondaryTriggerGroup = GetRecastGroupFromStat(Stat.GetStatAdjustment(activator, StatType.AbilityUsedRecastReductionSecondaryTriggerGroup));
            if (activatedRecastGroup == RecastGroup.Invalid ||
                activatedRecastGroup != triggerGroup &&
                activatedRecastGroup != secondaryTriggerGroup)
                return;

            var targetGroup = GetRecastGroupFromStat(Stat.GetStatAdjustment(activator, StatType.AbilityUsedRecastReductionTargetGroup));
            var seconds = Stat.GetStatAdjustment(activator, StatType.AbilityUsedRecastReductionSeconds);
            if (targetGroup == RecastGroup.Invalid || seconds <= 0)
                return;

            Recast.ReduceRecastDelay(activator, targetGroup, seconds);
        }

        private static void ApplyAbilityUsedNextSkillAutoAttackDamageBonus(uint activator, AbilityDetail ability)
        {
            var triggerSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedNextSkillAutoAttackDamageBonusTriggerSkillType));
            var abilitySkillType = GetAbilitySkillType(activator, ability);
            if (!SkillTypeMatches(abilitySkillType, triggerSkillType))
                return;

            var targetSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedNextSkillAutoAttackDamageBonusSkillType));
            var damageBonus = Stat.GetStatAdjustment(activator, StatType.AbilityUsedNextSkillAutoAttackDamageBonus);
            var duration = Stat.GetStatAdjustment(activator, StatType.AbilityUsedNextSkillAutoAttackDamageWindowSeconds);
            GrantNextSkillAutoAttackDamageBonus(activator, targetSkillType, damageBonus, duration);
        }

        private static void ApplyAbilityUsedNextSkillFPCostAdjustment(uint activator, AbilityDetail ability)
        {
            var triggerSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedNextSkillFPCostAdjustmentTriggerSkillType));
            var abilitySkillType = GetAbilitySkillType(activator, ability);
            if (!SkillTypeMatches(abilitySkillType, triggerSkillType))
                return;

            var targetSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedNextSkillFPCostAdjustmentSkillType));
            var adjustment = Stat.GetStatAdjustment(activator, StatType.AbilityUsedNextSkillFPCostAdjustment);
            var duration = Stat.GetStatAdjustment(activator, StatType.AbilityUsedNextSkillFPCostAdjustmentWindowSeconds);
            GrantNextAbilityFPCostAdjustment(activator, targetSkillType, adjustment, duration);
        }

        private static void ApplyAbilityUsedSkillEvasion(uint activator, AbilityDetail ability)
        {
            ApplyAbilityUsedSkillEvasionChannel(
                activator,
                ability,
                StatType.AbilityUsedEvasionPercentAdjustmentSkillType,
                StatType.AbilityUsedEvasionPercentAdjustment,
                StatType.AbilityUsedEvasionDurationSeconds);
            ApplyAbilityUsedSkillEvasionChannel(
                activator,
                ability,
                StatType.SecondaryAbilityUsedEvasionPercentAdjustmentSkillType,
                StatType.SecondaryAbilityUsedEvasionPercentAdjustment,
                StatType.SecondaryAbilityUsedEvasionDurationSeconds,
                StatType.SecondaryAbilityUsedEvasionRangedOnly);
        }

        private static void ApplyAbilityUsedSkillEvasionChannel(
            uint activator,
            AbilityDetail ability,
            StatType skillTypeStat,
            StatType evasionStat,
            StatType durationStat,
            StatType? rangedOnlyStat = null)
        {
            if (!AbilityUsedTriggerSkillMatches(
                    activator,
                    ability,
                    GetAbilitySkillType(activator, ability),
                    skillTypeStat,
                    rangedOnlyStat))
            {
                return;
            }

            var evasionPercent = Stat.GetStatAdjustment(
                activator,
                evasionStat);
            var duration = Stat.GetStatAdjustment(
                activator,
                durationStat);
            if (evasionPercent == 0 || duration <= 0)
                return;

            StatusEffect.ApplyStatusEffect(
                activator,
                activator,
                new EvasiveFootworkStatusEffect(evasionPercent),
                duration);
        }

        private static void ApplyAbilityUsedSkillRangedDeflection(uint activator, AbilityDetail ability)
        {
            if (ability?.IsHostileAbility != true)
                return;

            if (!AbilityUsedTriggerSkillMatches(
                    activator,
                    ability,
                    GetAbilitySkillType(activator, ability),
                    StatType.AbilityUsedRangedDeflectionSkillType,
                    StatType.AbilityUsedRangedDeflectionRangedOnly))
            {
                return;
            }

            var deflection = Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedRangedDeflection);
            var duration = Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedRangedDeflectionDurationSeconds);
            var statusEffectIcon = GetEffectIconTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedRangedDeflectionStatusEffectIcon));
            if (deflection <= 0 || duration <= 0 || statusEffectIcon == EffectIconType.Invalid)
                return;

            if (StatusEffect.ApplyStatusEffect(
                    activator,
                    activator,
                    new RangedDeflectionStatusEffect(deflection, statusEffectIcon),
                    duration))
            {
                var source = Stat.GetStatTypeDeflectionSource(StatType.AbilityUsedRangedDeflection);
                ApplyAbilityGrantedAttackDeflectionEffects(activator, source);
            }
        }

        private static void ApplySingleTargetAbilityUsedAttackDeflection(
            uint activator,
            AbilityDetail ability,
            bool isSingleTargetAbility)
        {
            if (!isSingleTargetAbility)
                return;

            var triggerSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.SingleTargetAbilityAttackDeflectionSkillType));
            var abilitySkillType = GetAbilitySkillType(activator, ability);
            if (!SkillTypeMatches(abilitySkillType, triggerSkillType))
                return;

            ApplyAbilityUsedAttackDeflection(
                activator,
                StatType.SingleTargetAbilityAttackDeflection,
                StatType.SingleTargetAbilityAttackDeflectionDurationSeconds);
        }

        private static void ApplyAbilityUsedSkillAttackDeflection(uint activator, AbilityDetail ability)
        {
            if (ability == null || !ability.IsHostileAbility)
                return;

            var triggerSkillType = GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedAttackDeflectionSkillType));
            var abilitySkillType = GetAbilitySkillType(activator, ability);
            if (!SkillTypeMatches(abilitySkillType, triggerSkillType))
                return;

            ApplyAbilityUsedAttackDeflection(
                activator,
                StatType.AbilityUsedAttackDeflection,
                StatType.AbilityUsedAttackDeflectionDurationSeconds,
                StatType.AbilityUsedAttackDeflectionFPRestore);
        }

        private static void ApplyAbilityUsedPerkCategoryAttackDeflection(uint activator, AbilityDetail ability)
        {
            var categoryValue = Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedPerkCategoryAttackDeflectionCategoryType);
            if (!Perk.IsPerkInCategory(ability?.EffectiveLevelPerkType ?? PerkType.Invalid, categoryValue))
            {
                return;
            }

            ApplyAbilityUsedAttackDeflection(
                activator,
                StatType.AbilityUsedPerkCategoryAttackDeflection,
                StatType.AbilityUsedPerkCategoryAttackDeflectionDurationSeconds,
                StatType.AbilityUsedPerkCategoryAttackDeflectionFPRestore);
        }

        private static void ApplyHostileAbilitySequenceEffects(
            uint activator,
            FeatType feat,
            AbilityDetail ability)
        {
            if (!GetIsObjectValid(activator) || ability == null || !ability.IsHostileAbility)
                return;

            var windowSeconds = Stat.GetStatAdjustment(activator, StatType.HostileAbilitySequenceWindowSeconds);
            var bleedDuration = Stat.GetStatAdjustment(
                activator,
                StatType.HostileAbilitySequenceNextAttackBleedDurationSeconds);
            if (windowSeconds <= 0 || bleedDuration <= 0)
            {
                _hostileAbilitySequenceStates[activator] = new HostileAbilitySequenceState
                {
                    LastFeat = feat,
                    LastUse = Clock.UtcNow
                };
                return;
            }

            var now = Clock.UtcNow;
            if (_hostileAbilitySequenceStates.TryGetValue(activator, out var state) &&
                state.LastFeat != feat &&
                (now - state.LastUse).TotalSeconds <= windowSeconds)
            {
                TemporaryStatModifier.Replace(
                    activator,
                    StatType.NextDamageDealtBleedDurationSeconds,
                    bleedDuration,
                    windowSeconds,
                    StatType.NextDamageDealtBleedDurationSeconds);
            }

            _hostileAbilitySequenceStates[activator] = new HostileAbilitySequenceState
            {
                LastFeat = feat,
                LastUse = now
            };
        }

        private static void ApplyHostileAbilityResourceRestoreEffects(uint activator, AbilityDetail ability)
        {
            if (ability?.IsHostileAbility != true)
                return;

            var fpRestore = Stat.GetStatAdjustment(activator, StatType.HostileAbilityFPRestore);
            var staminaRestore = Stat.GetStatAdjustment(activator, StatType.HostileAbilityStaminaRestore);

            var restoredFP = fpRestore > 0
                ? RestoreFPFromStat(activator, StatType.HostileAbilityFPRestore, fpRestore)
                : 0;
            if (restoredFP > 0)
                ApplyAbilityRestoredFPEffects(activator);

            var restoredStamina = staminaRestore > 0
                ? RestoreStaminaFromStat(activator, StatType.HostileAbilityStaminaRestore, staminaRestore)
                : 0;

            if (restoredFP > 0 && restoredStamina > 0)
                ApplyAbilityRestoredBothResourcesEffects(activator);
        }

        private static void ApplyNextAutoAttackDamageBonus(
            uint activator,
            StatType bonusStatType,
            StatType durationStatType)
        {
            var bonus = Stat.GetStatAdjustment(activator, bonusStatType);
            var duration = Stat.GetStatAdjustment(activator, durationStatType);
            if (bonus == 0 || duration <= 0)
                return;

            TemporaryStatModifier.Replace(
                activator,
                StatType.NextAutoAttackDamageBonus,
                bonus,
                duration,
                StatType.NextAutoAttackDamageBonus);
        }

        private static void ApplyAbilityUsedEvasion(
            uint activator,
            StatType evasionStatType,
            StatType durationStatType,
            StatType targetStatType = StatType.EvasionPercentAdjustment)
        {
            var evasionPercent = Stat.GetStatAdjustment(activator, evasionStatType);
            var duration = Stat.GetStatAdjustment(activator, durationStatType);
            if (evasionPercent == 0 || duration <= 0)
                return;

            TemporaryStatModifier.Replace(
                activator,
                targetStatType,
                evasionPercent,
                duration,
                evasionStatType);
        }

        private static void ForgetActivationState(uint creature)
        {
            _hostileAbilitySequenceStates.Remove(creature);
        }
    }
}
