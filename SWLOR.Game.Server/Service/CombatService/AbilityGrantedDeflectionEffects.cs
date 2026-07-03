using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.CombatService
{
    public static class AbilityGrantedDeflectionEffects
    {
        public static void ApplyAbilityGrantedAttackDeflectionEffects(uint activator)
        {
            var fpRestore = Stat.GetStatAdjustment(activator, StatType.AbilityGrantedAttackDeflectionFPRestore);
            var cooldown = Stat.GetStatAdjustment(activator, StatType.AbilityGrantedAttackDeflectionFPRestoreCooldownSeconds);
            if (fpRestore <= 0 || !CombatStatTriggers.TryUseStatTrigger(activator, StatType.AbilityGrantedAttackDeflectionFPRestore, cooldown))
                return;

            Stat.RestoreFP(activator, fpRestore);
            AbilityRecoveryEffects.ApplyAbilityRestoredFPEffects(activator);
        }
    }
}
