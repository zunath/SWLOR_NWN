using System;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.Service.AbilityService
{
    /// <summary>
    /// Adds an FP requirement to activate a perk.
    /// </summary>
    public class AbilityRequirementFP : IAbilityActivationRequirement
    {
        public int RequiredFP { get; }

        public AbilityRequirementFP(int requiredFP)
        {
            RequiredFP = requiredFP;
        }

        public string CheckRequirements(uint player, AbilityDetail ability = null)
        {
            // DMs are assumed to be able to activate.
            if (GetIsDM(player)) return string.Empty;

            var requiredFP = GetAdjustedRequiredFP(player, ability, false);
            var fp = Stat.GetCurrentFP(player);

            if (fp >= requiredFP) return string.Empty;
            return $"Not enough FP. (Required: {requiredFP})";
        }

        public void AfterActivationAction(uint player, AbilityDetail ability = null)
        {
            if (GetIsDM(player)) return;

            var requiredFP = GetAdjustedRequiredFP(player, ability, true);
            if (requiredFP <= 0)
                return;

            Stat.ReduceFP(player, requiredFP);
            QueuedAbilityBonuses.ApplyAbilityFPCostStaminaRestore(player, ability, requiredFP);
        }

        private int GetAdjustedRequiredFP(uint player, AbilityDetail ability, bool consumeNextAdjustment)
        {
            var adjusted = Stat.GetAdjustedRequiredFP(player, RequiredFP);
            if (ability == null || adjusted <= 0)
                return adjusted;

            var skillType = QueuedCombatActions.GetAbilitySkillType(player, ability);
            adjusted += consumeNextAdjustment
                ? QueuedAbilityBonuses.ConsumeNextAbilityFPCostAdjustment(player, skillType)
                : QueuedAbilityBonuses.GetNextAbilityFPCostAdjustment(player, skillType);

            adjusted = Math.Max(0, adjusted);
            return ApplyDarkForceConversionCostAdjustment(player, ability, adjusted);
        }

        private static int ApplyDarkForceConversionCostAdjustment(uint player, AbilityDetail ability, int adjustedCost)
        {
            if (adjustedCost <= 0 || ability.TriggersDarkForceConversion != true)
                return adjustedCost;

            var percentAdjustment = Stat.GetStatAdjustment(player, StatType.DarkForceConversionFPCostPercentAdjustment);
            if (percentAdjustment == 0)
                return adjustedCost;

            var adjusted = (int)Math.Ceiling(adjustedCost * (1 + percentAdjustment / 100f));
            return Math.Max(0, adjusted);
        }
    }
}
