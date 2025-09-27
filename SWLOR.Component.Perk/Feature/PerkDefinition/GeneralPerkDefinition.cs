using SWLOR.Component.Perk.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Contracts;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Perk.ValueObjects;

namespace SWLOR.Component.Perk.Feature.PerkDefinition
{
    public class GeneralPerkDefinition: IPerkListDefinition
    {
        private readonly IAbilityService _abilityService;

        public GeneralPerkDefinition(IAbilityService abilityService)
        {
            _abilityService = abilityService;
        }

        public Dictionary<PerkType, PerkDetail> BuildPerks(IPerkBuilder builder)
        {
            Dash(builder);

            return builder.Build();
        }

        private void Dash(IPerkBuilder builder)
        {
            void ToggleDash(uint player)
            {
                if (_abilityService.IsAbilityToggled(player, AbilityToggleType.Dash))
                {
                    _abilityService.ToggleAbility(player, AbilityToggleType.Dash, false);
                }
            }

            builder.Create(PerkCategoryType.General, PerkType.Dash)
                .Name("Dash")
                
                .AddPerkLevel()
                .Description("Grants the Dash ability. Increases movement rate by 10% while active.")
                .Price(2)
                .GrantsFeat(FeatType.Dash)

                .AddPerkLevel()
                .Description("Increases movement rate of Dash to 25%.")
                .Price(3)
                .PurchaseRequirement((player) =>
                {
                    if (_abilityService.IsAbilityToggled(player, AbilityToggleType.Dash))
                    {
                        return "Please disable Dash and try again.";
                    }

                    return string.Empty;
                })
                .TriggerPurchase(ToggleDash)
                .TriggerRefund(ToggleDash);
        }
    }
}
