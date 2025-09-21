using SWLOR.Shared.Core.Contracts;

namespace SWLOR.Game.Server.Service.AbilityServicex
{
    /// <summary>
    /// Adds a stamina requirement to activate a perk.
    /// </summary>
    public class AbilityRequirementStamina : IAbilityActivationRequirement
    {
        public int RequiredSTM { get; }
        private readonly IStatService _statService;

        public AbilityRequirementStamina(int requiredSTM, IStatService statService)
        {
            RequiredSTM = requiredSTM;
            _statService = statService;
        }

        public string CheckRequirements(uint player)
        {
            // DMs are assumed to be able to activate.
            if (GetIsDM(player)) return string.Empty;

            var stamina = _statService.GetCurrentStamina(player);

            if (stamina >= RequiredSTM) return string.Empty;
            return $"Not enough stamina. (Required: {RequiredSTM})";
        }

        public void AfterActivationAction(uint player)
        {
            if (GetIsDM(player)) return;

            _statService.ReduceStamina(player, RequiredSTM);
        }
    }
}
