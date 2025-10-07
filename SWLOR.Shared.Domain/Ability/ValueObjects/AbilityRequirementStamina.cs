using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.Domain.Ability.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;

namespace SWLOR.Shared.Domain.Ability.ValueObjects
{
    /// <summary>
    /// Adds a stamina requirement to activate a perk.
    /// </summary>
    public class AbilityRequirementStamina : IAbilityActivationRequirement
    {
        public int RequiredSTM { get; }
        private readonly IServiceProvider _serviceProvider;

        public AbilityRequirementStamina(int requiredSTM, IServiceProvider serviceProvider)
        {
            RequiredSTM = requiredSTM;
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private IStatService StatService => _serviceProvider.GetRequiredService<IStatService>();
        private ICharacterResourceService CharacterResourceService => _serviceProvider.GetRequiredService<ICharacterResourceService>();

        public string CheckRequirements(uint player)
        {
            // DMs are assumed to be able to activate.
            if (GetIsDM(player)) return string.Empty;

            var stamina = CharacterResourceService.GetCurrentSTM(player);

            if (stamina >= RequiredSTM) return string.Empty;
            return $"Not enough stamina. (Required: {RequiredSTM})";
        }

        public void AfterActivationAction(uint player)
        {
            if (GetIsDM(player)) return;

            CharacterResourceService.ReduceStamina(player, RequiredSTM);
        }
    }
}
