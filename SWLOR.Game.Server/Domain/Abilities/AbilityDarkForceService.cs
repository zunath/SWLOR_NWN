using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// The Dark Force conversion and restoration (part 14 of the ability split, Readmes/MaintainabilityRefactorAbilitySplit.md),
    /// moved from <c>AbilityService</c> unchanged. It reads the impact in progress from the impact state store. Calls into the
    /// stat and combat resource services sit behind <c>protected virtual</c> methods whose bodies are the original calls, so a
    /// unit test can answer them.
    /// </summary>
    public class AbilityDarkForceService : IAbilityDarkForceService
    {
        private readonly IAbilityImpactState _impactState;
        private readonly ICombatRecorderService _combatRecorder;
        private readonly IStatService _stat;
        private readonly ICombatResourcesService _resources;

        public AbilityDarkForceService(
            IAbilityImpactState impactState,
            ICombatRecorderService combatRecorder,
            IStatService stat,
            ICombatResourcesService resources)
        {
            _impactState = impactState;
            _combatRecorder = combatRecorder;
            _stat = stat;
            _resources = resources;
        }

        public int GetDarkForceTargetLowHPDamageAdjustment(uint activator, uint target)
        {
            if (!GetIsObjectValid(target))
                return 0;

            var trackedImpact = _impactState.GetTrackedAbilityImpact(activator);
            if (trackedImpact?.Ability?.TriggersDarkForceConversion != true)
                return 0;

            var threshold = GetStatAdjustment(activator, StatType.DarkForceTargetLowHPDamageThresholdPercent);
            var adjustment = GetStatAdjustment(activator, StatType.DarkForceTargetLowHPDamagePercentAdjustment);
            if (threshold <= 0 || adjustment == 0)
                return 0;

            var maxHP = GetMaxHitPoints(target);
            if (maxHP <= 0 || GetCurrentHitPoints(target) > maxHP * (threshold / 100f))
                return 0;

            return adjustment;
        }

        public void ApplyDarkForceConversion(uint activator, uint target, int damage)
        {
            if (damage <= 0 || !GetIsObjectValid(activator))
                return;

            var trackedImpact = _impactState.GetTrackedAbilityImpact(activator);
            if (trackedImpact == null ||
                trackedImpact.Ability?.TriggersDarkForceConversion != true)
            {
                return;
            }

            ApplyDarkForceDamageRestoration(activator, damage);
            ApplyDarkForceCastConversion(activator, target);
        }

        public void ApplyDarkForceDamageRestoration(uint activator, int damage)
        {
            if (damage <= 0 || !GetIsObjectValid(activator))
                return;

            var hpRestorePercent = GetStatAdjustment(activator, StatType.DarkForceDamageHPPercentRestore);
            if (hpRestorePercent > 0)
            {
                ApplyDamageDerivedHealing(activator, damage, hpRestorePercent);
            }
        }

        public void ApplyDarkForceCastConversion(uint activator, uint target)
        {
            var trackedImpact = _impactState.GetTrackedAbilityImpact(activator);
            if (trackedImpact == null ||
                trackedImpact.Ability?.TriggersDarkForceConversion != true)
            {
                return;
            }

            var fpRestore = GetStatAdjustment(activator, StatType.DarkForceDamageFPRestore);
            var hpCostPercent = GetStatAdjustment(activator, StatType.DarkForceDamageHPCostPercent);
            if (trackedImpact.DarkForceConversionApplied)
                return;

            if (fpRestore <= 0 && hpCostPercent <= 0)
                return;

            trackedImpact.DarkForceConversionApplied = true;

            if (fpRestore > 0)
            {
                using var recorderAttribution = _combatRecorder.BeginStatTriggerAttribution(activator, StatType.DarkForceDamageFPRestore);
                RestoreFP(activator, fpRestore);
            }

            var lowTargetThresholdPercent = GetStatAdjustment(activator, StatType.DarkForceDamageLowTargetHPThresholdPercent);
            var lowTargetHPCostPercent = GetStatAdjustment(activator, StatType.DarkForceDamageLowTargetHPCostPercent);
            if (lowTargetThresholdPercent > 0 &&
                lowTargetHPCostPercent > 0 &&
                GetIsObjectValid(target) &&
                GetMaxHitPoints(target) > 0 &&
                GetCurrentHitPoints(target) <= GetMaxHitPoints(target) * lowTargetThresholdPercent / 100)
            {
                hpCostPercent = lowTargetHPCostPercent;
            }

            if (hpCostPercent <= 0)
                return;

            var hpCost = Math.Max(1, GetMaxHitPoints(activator) * hpCostPercent / 100);
            hpCost = Math.Min(hpCost, Math.Max(0, GetCurrentHitPoints(activator) - 1));
            if (hpCost > 0)
            {
                if (_combatRecorder.IsRecording)
                    _combatRecorder.ExpectHitPointCost(activator, hpCost);
                AssignCommand(activator, () => ApplyEffectToObject(DurationType.Instant, EffectDamage(hpCost), activator));
            }
        }

        protected virtual int GetStatAdjustment(uint creature, StatType stat) => _stat.GetStatAdjustment(creature, stat);

        protected virtual int RestoreFP(uint creature, int amount) => _stat.RestoreFP(creature, amount);

        protected virtual int ApplyDamageDerivedHealing(uint creature, int damage, int percent) => _resources.ApplyDamageDerivedHealing(creature, damage, percent);
    }
}
