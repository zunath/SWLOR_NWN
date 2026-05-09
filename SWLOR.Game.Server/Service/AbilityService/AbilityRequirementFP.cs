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

            var requiredFP = GetAdjustedRequiredFP(player);
            var fp = Stat.GetCurrentFP(player);

            if (fp >= requiredFP) return string.Empty;
            return $"Not enough FP. (Required: {requiredFP})";
        }

        public void AfterActivationAction(uint player)
        {
            if (GetIsDM(player)) return;

            Stat.ReduceFP(player, GetAdjustedRequiredFP(player));
        }

        private int GetAdjustedRequiredFP(uint player)
        {
            return Stat.GetAdjustedRequiredFP(player, RequiredFP);
        }
    }
}
