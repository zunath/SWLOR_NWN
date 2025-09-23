using SWLOR.Component.Perk.Contracts;
using SWLOR.Component.Perk.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Contracts;
using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.Domain.Model;

namespace SWLOR.Component.Perk.Feature.PerkDefinition
{
    public class GeneralPerkDefinition: IPerkListDefinition
    {
        private readonly IAbilityService _abilityService;
        private readonly PerkBuilder _builder = new();

        public GeneralPerkDefinition(IAbilityService abilityService)
        {
            _abilityService = abilityService;
        }

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            Dash();

            return _builder.Build();
        }

        private void Dash()
        {
            void ToggleDash(uint player)
            {
                if (_abilityService.IsAbilityToggled(player, AbilityToggleType.Dash))
                {
                    _abilityService.ToggleAbility(player, AbilityToggleType.Dash, false);
                }
            }

            _builder.Create(PerkCategoryType.General, PerkType.Dash)
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
