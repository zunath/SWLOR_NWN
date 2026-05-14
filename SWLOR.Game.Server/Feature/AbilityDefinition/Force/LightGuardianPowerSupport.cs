using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public static class LightGuardianPowerSupport
    {
        public static void ApplyDeflectivePresence(uint activator)
        {
            if (!GetIsObjectValid(activator))
                return;

            var attackDeflection = Stat.GetStatAdjustment(activator, StatType.LightGuardianPowerAttackDeflection);
            var duration = Stat.GetStatAdjustment(activator, StatType.LightGuardianPowerAttackDeflectionDurationSeconds);
            if (attackDeflection == 0 || duration <= 0)
                return;

            TemporaryStatModifier.Replace(
                activator,
                StatType.AttackDeflection,
                attackDeflection,
                duration,
                StatType.LightGuardianPowerAttackDeflection);
        }
    }
}
