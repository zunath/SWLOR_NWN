using System;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.AbilityService
{
    /// <summary>
    /// Adds a stamina requirement to activate a perk.
    /// </summary>
    public class AbilityRequirementStamina : IAbilityActivationRequirement
    {
        public int RequiredSTM { get; }

        public AbilityRequirementStamina(int requiredSTM)
        {
            RequiredSTM = requiredSTM;
        }

        public string CheckRequirements(uint player, AbilityDetail ability = null)
        {
            // DMs are assumed to be able to activate.
            if (GetIsDM(player)) return string.Empty;

            var requiredSTM = GetRequiredStaminaForCheck(player, ability);
            var stamina = Stat.GetCurrentStamina(player);

            if (stamina >= requiredSTM) return string.Empty;
            return $"Not enough stamina. (Required: {requiredSTM})";
        }

        public void AfterActivationAction(uint player, AbilityDetail ability = null)
        {
            if (GetIsDM(player)) return;

            var requiredSTM = GetRequiredStaminaForActivation(player, ability);
            if (requiredSTM <= 0)
                return;

            Stat.ReduceStamina(player, requiredSTM);
        }

        private int GetRequiredStaminaForCheck(uint player, AbilityDetail ability)
        {
            var abilitySkillType = Combat.GetAbilitySkillType(player, ability);
            var requiredSTM = ability != null && abilitySkillType != SkillType.Invalid && Combat.HasNextAbilityNoStaminaCost(player, abilitySkillType)
                ? 0
                : RequiredSTM;

            return ApplyStaminaAdjustments(player, ability, requiredSTM, false);
        }

        private int GetRequiredStaminaForActivation(uint player, AbilityDetail ability)
        {
            var abilitySkillType = Combat.GetAbilitySkillType(player, ability);
            var requiredSTM = ability != null && abilitySkillType != SkillType.Invalid && Combat.ConsumeNextAbilityNoStaminaCost(player, abilitySkillType)
                ? 0
                : RequiredSTM;

            return ApplyStaminaAdjustments(player, ability, requiredSTM, true);
        }

        private static int ApplyStaminaAdjustments(uint player, AbilityDetail ability, int requiredSTM, bool consumeNextAdjustment)
        {
            if (ability == null || requiredSTM <= 0)
                return requiredSTM;

            var abilitySkillType = Combat.GetAbilitySkillType(player, ability);
            var adjustment = Combat.GetAbilityStaminaCostFlatAdjustment(player, ability);
            adjustment += consumeNextAdjustment
                ? Combat.ConsumeNextSkillAbilityStaminaCostAdjustment(player, abilitySkillType)
                : Combat.GetNextSkillAbilityStaminaCostAdjustment(player, abilitySkillType);
            adjustment += consumeNextAdjustment
                ? Combat.ConsumeNextAbilityStaminaCostAdjustment(player, ability.EffectiveLevelPerkType)
                : Combat.GetNextAbilityStaminaCostAdjustment(player, ability.EffectiveLevelPerkType);
            return Math.Max(0, requiredSTM + adjustment);
        }
    }
}
