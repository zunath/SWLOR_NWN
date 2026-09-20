using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public static class LightGuardianPowerSupport
    {
        public static void ApplyDeflectivePresence(uint activator, uint target)
        {
            if (!GetIsObjectValid(activator))
                return;

            var sourceStatType = StatType.LightGuardianPowerAttackDeflection;
            var attackDeflection = Stat.GetStatAdjustment(activator, sourceStatType);
            var duration = Stat.GetStatAdjustment(activator, StatType.LightGuardianPowerAttackDeflectionDurationSeconds);
            var deflectionStatType = Stat.GetGrantedDeflectionStatType(sourceStatType);
            if (deflectionStatType == StatType.Invalid || attackDeflection == 0 || duration <= 0)
                return;

            TemporaryStatModifier.Replace(
                target,
                deflectionStatType,
                attackDeflection,
                duration,
                sourceStatType);
        }

        public static void ApplyTemporaryHPPowerRiders(uint activator, uint target, float durationSeconds)
        {
            if (!GetIsObjectValid(activator) || !GetIsObjectValid(target))
                return;

            ApplyDeflectivePresence(activator, target);

            // Guardian Ward pools replace one another regardless of caster. Clear any rider tied
            // to the replaced pool before conditionally applying the new caster's version.
            StatusEffect.RemoveStatusEffect(target, typeof(ReflectiveBarrier1StatusEffect), false);
            if (Stat.GetStatAdjustment(activator, StatType.LightGuardianTemporaryHPReflectiveBarrier) > 0)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(ReflectiveBarrier1StatusEffect), durationSeconds);
            }

            ApplyWardEmpowerment(activator, target, durationSeconds);
        }

        /// <summary>
        /// Grants whoever holds the pool, the caster included, a weapon and Force damage bonus for
        /// as long as it lasts. This is the Light tree's own offensive expression: it comes out of
        /// protecting someone rather than out of draining an enemy, and it works when the only
        /// person a Light caster has to protect is themselves.
        /// </summary>
        private static void ApplyWardEmpowerment(uint activator, uint target, float durationSeconds)
        {
            // Pools replace one another regardless of caster, so a previous caster's bonus is
            // cleared here even when this caster does not own the trait.
            var empowerment = Stat.GetStatAdjustment(activator, StatType.LightGuardianTemporaryHPEmpowerment);
            TemporaryStatModifier.Replace(
                target,
                StatType.WeaponAndForceDamageDealtPercentAdjustment,
                empowerment,
                durationSeconds,
                StatType.LightGuardianTemporaryHPEmpowerment);
        }

        public static void ApplyCourageousResolve(uint activator)
        {
            if (!GetIsObjectValid(activator) ||
                Stat.GetStatAdjustment(activator, StatType.LightGuardianSenseResolve) <= 0)
            {
                return;
            }

            foreach (var friendly in AbilityTargeting.GetFriendlyTargetsNearLocation(activator, GetLocation(activator), 5f))
            {
                var hasForceTemporaryHP =
                    TemporaryHitPointEffects.IsActivePoolFromSource(
                        friendly,
                        TemporaryHitPointEffectKey.GuardianWard,
                        activator) ||
                    TemporaryHitPointEffects.IsActivePoolFromSource(
                        friendly,
                        TemporaryHitPointEffectKey.FatalDamageSave,
                        activator);
                var resistance = hasForceTemporaryHP
                    ? 15
                    : 10;
                StatusEffect.ApplyStatusEffect(activator, friendly, new CourageousResolve1StatusEffect(resistance), 30f);
            }
        }
    }
}
