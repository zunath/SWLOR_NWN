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

        public string CheckRequirements(uint player)
        {
            // DMs are assumed to be able to activate.
            if (GetIsDM(player)) return string.Empty;

            var stamina = Stat.GetCurrentStamina(player);

            if (stamina >= RequiredSTM) return string.Empty;
            return $"Not enough stamina. (Required: {RequiredSTM})";
        }

        public void AfterActivationAction(uint player)
        {
            if (GetIsDM(player)) return;

            Stat.ReduceStamina(player, RequiredSTM);
        }
    }
}