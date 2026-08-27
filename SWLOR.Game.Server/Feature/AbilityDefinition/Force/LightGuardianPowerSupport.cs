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

            if (Stat.GetStatAdjustment(activator, StatType.LightGuardianTemporaryHPReflectiveBarrier) > 0)
            {
                StatusEffect.ApplyStatusEffect(activator, target, typeof(ReflectiveBarrier1StatusEffect), durationSeconds);
            }
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
                        "GUARDIAN_WARD",
                        activator) ||
                    TemporaryHitPointEffects.IsActivePoolFromSource(
                        friendly,
                        "FATAL_DAMAGE_SAVE",
                        activator);
                var resistance = hasForceTemporaryHP
                    ? 15
                    : 10;
                StatusEffect.ApplyStatusEffect(activator, friendly, new CourageousResolve1StatusEffect(resistance), 30f);
            }
        }
    }
}
