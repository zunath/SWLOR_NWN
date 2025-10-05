using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.Domain.Ability.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;

namespace SWLOR.Shared.Domain.Ability.ValueObjects
{
    /// <summary>
    /// Adds an FP requirement to activate a perk.
    /// </summary>
    public class AbilityRequirementFP : IAbilityActivationRequirement
    {
        public int RequiredFP { get; }
        private readonly IServiceProvider _serviceProvider;

        public AbilityRequirementFP(int requiredFP, IServiceProvider serviceProvider)
        {
            RequiredFP = requiredFP;
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private IStatService StatService => _serviceProvider.GetRequiredService<IStatService>();

        public string CheckRequirements(uint player)
        {
            // DMs are assumed to be able to activate.
            if (GetIsDM(player)) return string.Empty;

            var fp = StatService.GetCurrentFP(player);

            if (fp >= RequiredFP) return string.Empty;
            return $"Not enough FP. (Required: {RequiredFP})";
        }

        public void AfterActivationAction(uint player)
        {
            if (GetIsDM(player)) return;

            StatService.ReduceFP(player, RequiredFP);
        }
    }
}
