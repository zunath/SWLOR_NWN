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
        public static void ApplyDeflectivePresence(uint activator)
        {
            ApplyDeflectivePresence(activator, activator);
        }

        public static void ApplyDeflectivePresence(uint activator, uint target)
        {
            if (!GetIsObjectValid(activator))
                return;

            var attackDeflection = Stat.GetStatAdjustment(activator, StatType.LightGuardianPowerAttackDeflection);
            var duration = Stat.GetStatAdjustment(activator, StatType.LightGuardianPowerAttackDeflectionDurationSeconds);
            if (attackDeflection == 0 || duration <= 0)
                return;

            TemporaryStatModifier.Replace(
                target,
                StatType.RangedDeflection,
                attackDeflection,
                duration,
                StatType.LightGuardianPowerAttackDeflection);
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

            ApplyCourageousResolve(activator);
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
                var resistance = StatusEffect.HasStatusEffect(friendly, typeof(ReflectiveBarrier1StatusEffect), activator)
                    ? 15
                    : 10;
                StatusEffect.ApplyStatusEffect(activator, friendly, new CourageousResolve1StatusEffect(resistance), 30f);
            }
        }
    }
}
