using SWLOR.Game.Server.Service.StatusEffectService;

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

        public string CheckRequirements(uint player)
        {
            // DMs are assumed to be able to activate.
            if (GetIsDM(player)) return string.Empty;

            var fp = Stat.GetCurrentFP(player);

            if (fp >= RequiredFP) return string.Empty;
            return $"Not enough FP. (Required: {RequiredFP})";
        }

        public void AfterActivationAction(uint player)
        {
            if (GetIsDM(player)) return;

            // Force Attunement reduces FP costs to zero.
            if (StatusEffect.HasStatusEffect(player, StatusEffectType.ForceAttunement)) return;

            Stat.ReduceFP(player, RequiredFP);
        }
    }
}
