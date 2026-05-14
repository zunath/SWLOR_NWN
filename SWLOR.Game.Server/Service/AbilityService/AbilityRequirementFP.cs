using System;

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

            Stat.ReduceFP(player, GetAdjustedRequiredFP(player, ability, true));
        }

        private int GetAdjustedRequiredFP(uint player, AbilityDetail ability, bool consumeNextAdjustment)
        {
            var adjusted = Stat.GetAdjustedRequiredFP(player, RequiredFP);
            if (ability == null || adjusted <= 0)
                return adjusted;

            var skillType = Combat.GetAbilitySkillType(player, ability);
            adjusted += consumeNextAdjustment
                ? Combat.ConsumeNextAbilityFPCostAdjustment(player, skillType)
                : Combat.GetNextAbilityFPCostAdjustment(player, skillType);

            return Math.Max(0, adjusted);
        }
    }
}
