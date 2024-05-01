using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class GeneralPerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            Dash();

            return _builder.Build();
        }

        private void Dash()
        {
            void ToggleDash(uint player)
            {
                if (Ability.IsAbilityToggled(player, AbilityToggleType.Dash))
                {
                    Ability.ToggleAbility(player, AbilityToggleType.Dash, false);
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
                    if (Ability.IsAbilityToggled(player, AbilityToggleType.Dash))
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
