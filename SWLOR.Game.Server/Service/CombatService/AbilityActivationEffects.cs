using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.CombatService
{
    public static class AbilityActivationEffects
    {
        public static void ApplyAbilityActivatedEffects(
            uint activator,
            uint target,
            FeatType feat,
            AbilityDetail ability,
            AbilityImpactSummary summary)
        {
            if (!GetIsObjectValid(activator) || ability == null || summary == null)
                return;

            AbilityUseEffects.ApplyAbilityUsedRecastReduction(activator, ability);
            AbilityUseEffects.ApplyAbilityUsedNextSkillAutoAttackDamageBonus(activator, ability);
            AbilityUseEffects.ApplyAbilityUsedNextSkillFPCostAdjustment(activator, ability);
            HitPointSpendEffects.ApplyAbilityUsedMasterAbilityHitChance(activator);
            HitPointSpendEffects.ApplyForceFPCostActivatedEffects(activator, ability);

            var skillType = AbilityActivationEffects.ResolveActivatedAbilitySkillType(activator, ability, summary);
            var isSingleTargetAbility = summary.IsSingleTargetAbility ||
                summary.SkillType == SkillType.Invalid &&
                ability.IsHostileAbility &&
                ability.IsSingleTargetAbility;

            AbilityUseEffects.ApplyAbilityUsedSkillEvasion(activator, ability);
            AbilityUseEffects.ApplyAbilityUsedSkillRangedEvasion(activator, ability);
            AbilityActivationEffects.ApplyAbilityUsedMovementSpeed(activator, ability, skillType);
            AbilityUseEffects.ApplyAbilityUsedSkillAttackDeflection(activator, ability);
            AbilityUseEffects.ApplyAbilityUsedPerkCategoryAttackDeflection(activator, ability);
            AbilityUseEffects.ApplySingleTargetAbilityUsedAttackDeflection(activator, ability, isSingleTargetAbility);
            AbilityActivationEffects.ApplyAreaAbilityUsedEvasion(activator, ability, skillType);
            AbilityActivationEffects.ApplyHostileAbilityForceAttack(activator, ability);
            AbilityActivationEffects.ApplyAbilityUsedNearbyAllyDefense(activator);
            TriggeredCombatEffects.ApplyAbilityUsedPerkCategoryNearbyAllyAttackDeflection(activator, ability);
            TriggeredCombatEffects.ApplyAbilityUsedPerkCategorySelfDefense(activator, ability);
            AbilityActivationEffects.ApplyAbilityActivatedRiders(activator, target, ability, skillType);
            AbilityUseEffects.ApplyHostileAbilitySequenceEffects(activator, feat, ability);
            AbilityUseEffects.ApplyHostileAbilityResourceRestoreEffects(activator, ability);

            AbilityUseEffects.TrackCombatAbilityUse(activator, ability);
        }

        internal static SkillType ResolveActivatedAbilitySkillType(
            uint activator,
            AbilityDetail ability,
            AbilityImpactSummary summary)
        {
            return summary.SkillType != SkillType.Invalid
                ? summary.SkillType
                : QueuedCombatActions.GetAbilitySkillType(activator, ability);
        }

        internal static void ApplyAreaAbilityUsedEvasion(
            uint activator,
            AbilityDetail ability,
            SkillType skillType)
        {
            if (ability == null || !ability.IsAreaAbility)
                return;

            var requiredSkillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.AreaAbilityUsedEvasionPercentAdjustmentSkillType));
            if (!AbilityImpactEffects.SkillTypeMatches(skillType, requiredSkillType))
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

        internal static void ApplyAbilityUsedMovementSpeed(
            uint activator,
            AbilityDetail ability,
            SkillType skillType)
        {
            if (ability == null || !ability.IsHostileAbility)
                return;

            var requiredSkillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(
                activator,
                StatType.AbilityUsedMovementSpeedPercentAdjustmentSkillType));
            if (!AbilityImpactEffects.SkillTypeMatches(skillType, requiredSkillType))
                return;

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

        internal static void ApplyHostileAbilityForceAttack(uint activator, AbilityDetail ability)
        {
            if (ability == null || !ability.IsHostileAbility)
                return;

            var forceAttack = Stat.GetStatAdjustment(activator, StatType.HostileAbilityForceAttackPercentPerStack);
            var duration = Stat.GetStatAdjustment(activator, StatType.HostileAbilityForceAttackDurationSeconds);
            var maximum = Stat.GetStatAdjustment(activator, StatType.HostileAbilityForceAttackPercentMax);
            if (forceAttack == 0 || duration <= 0 || maximum <= 0)
                return;

            TemporaryStatModifier.AddCapped(
                activator,
                StatType.ForceAttackPercentAdjustment,
                forceAttack,
                duration,
                maximum,
                StatType.HostileAbilityForceAttackPercentPerStack,
                1);
        }

        internal static void ApplyAbilityUsedNearbyAllyDefense(uint activator)
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

        internal static void ApplyAbilityActivatedRiders(
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
                    TriggeredCombatEffects.ApplyHeavyVibrobladeActivatedEffects(activator, target, ability);
                    break;
                case SkillType.BeastMastery:
                    TriggeredCombatEffects.ApplyBeastBalancedAbilityRecovery(activator, ability);
                    break;
                case SkillType.Vibroknife:
                    TriggeredCombatEffects.ApplyVibroknifeShadowActivatedEffects(activator, ability);
                    break;
                case SkillType.Pistol:
                    TriggeredCombatEffects.ApplyPistolSkirmisherActivatedEffects(activator, target, ability);
                    break;
                case SkillType.Lightsaber:
                    TriggeredCombatEffects.ApplyLightsaberOffenseActivatedEffects(activator, target);
                    TriggeredCombatEffects.ApplyLightsaberDefenseActivatedEffects(activator);
                    TriggeredCombatEffects.ApplyLightsaberWardActivatedEffects(activator, ability);
                    break;
                case SkillType.Saberstaff:
                    TriggeredCombatEffects.ApplySaberstaffConduitActivatedEffects(activator, ability);
                    break;
            }
        }

    }
}
