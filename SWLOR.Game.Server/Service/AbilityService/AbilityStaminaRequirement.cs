
using SWLOR.Game.Server.Entity;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.AbilityService
{
    /// <summary>
    /// Adds a stamina requirement to activate a perk.
    /// </summary>
    public class AbilityStaminaRequirement : IAbilityActivationRequirement
    {
        private readonly int _requiredSTM;

        public AbilityStaminaRequirement(int requiredSTM)
        {
            _requiredSTM = requiredSTM;
        }

        public string CheckRequirements(uint player)
        {
            // DMs are assumed to be able to activate.
            if (GetIsDM(player)) return string.Empty;

            var stamina = Stat.GetCurrentStamina(player);

            if (stamina >= _requiredSTM) return string.Empty;
            return $"Not enough stamina. (Required: {_requiredSTM})";
        }

        public void AfterActivationAction(uint player)
        {
            if (GetIsDM(player)) return;

            Stat.ReduceStamina(player, _requiredSTM);
        }
    }
}