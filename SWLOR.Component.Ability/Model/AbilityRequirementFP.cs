using SWLOR.Component.Ability.Contracts;
using SWLOR.Component.StatusEffect.Contracts;
using SWLOR.Shared.Domain.Contracts;
using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Component.Ability.Model
{
    /// <summary>
    /// Adds an FP requirement to activate a perk.
    /// </summary>
    public class AbilityRequirementFP : IAbilityActivationRequirement
    {
        public int RequiredFP { get; }
        private readonly IStatService _statService;
        private readonly IStatusEffectService _statusEffectService;

        public AbilityRequirementFP(int requiredFP, IStatService statService, IStatusEffectService statusEffectService)
        {
            RequiredFP = requiredFP;
            _statService = statService;
            _statusEffectService = statusEffectService;
        }

        public string CheckRequirements(uint player)
        {
            // DMs are assumed to be able to activate.
            if (GetIsDM(player)) return string.Empty;

            var fp = _statService.GetCurrentFP(player);

            if (fp >= RequiredFP) return string.Empty;
            return $"Not enough FP. (Required: {RequiredFP})";
        }

        public void AfterActivationAction(uint player)
        {
            if (GetIsDM(player)) return;

            // Force Attunement reduces FP costs to zero.
            if (_statusEffectService.HasStatusEffect(player, StatusEffectType.ForceAttunement)) return;

            _statService.ReduceFP(player, RequiredFP);
        }
    }
}
