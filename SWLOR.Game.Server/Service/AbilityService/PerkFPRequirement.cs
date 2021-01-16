using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.AbilityService
{
    /// <summary>
    /// Adds an FP requirement to activate a perk.
    /// </summary>
    public class PerkFPRequirement : IAbilityActivationRequirement
    {
        private readonly int _requiredFP;

        public PerkFPRequirement(int requiredFP)
        {
            _requiredFP = requiredFP;
        }

        public string CheckRequirements(uint player)
        {
            // DMs are assumed to be able to activate.
            if (GetIsDM(player)) return string.Empty;

            var fp = Stat.GetCurrentFP(player);

            if (fp >= _requiredFP) return string.Empty;
            return $"Not enough FP. (Required: {_requiredFP})";
        }

        public void AfterActivationAction(uint player)
        {
            if (GetIsDM(player)) return;

            // Force Attunement reduces FP costs to zero.
            if (StatusEffect.HasStatusEffect(player, StatusEffectType.ForceAttunement)) return;

            Stat.ReduceFP(player, _requiredFP);
        }
    }
}
