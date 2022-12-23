using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
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
            Mobility();
            Dash();

            return _builder.Build();
        }

        private void Mobility()
        {
            _builder.Create(PerkCategoryType.General, PerkType.Mobility)
                .Name("Mobility")

                .AddPerkLevel()
                .Description("Improves your ability to avoid attacks of opportunity.")
                .Price(3)
                .GrantsFeat(FeatType.Mobility);
        }

        private void Dash()
        {
            void ToggleDash(uint player)
            {
                var playerId = GetObjectUUID(player);
                var dbPlayer = DB.Get<Player>(playerId);

                if (dbPlayer.AbilityToggles.ContainsKey(AbilityToggleType.Dash) &&
                    dbPlayer.AbilityToggles[AbilityToggleType.Dash])
                {
                    AssignCommand(player, () => ActionUseFeat(FeatType.Dash, player));
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
                .PurchaseRequirement((player, type, level) =>
                {
                    if (Ability.IsAbilityToggled(player, AbilityToggleType.Dash))
                    {
                        return "Please disable Dash and try again.";
                    }

                    return string.Empty;
                })
                .RefundRequirement((player, type, level) =>
                {
                    if (Ability.IsAbilityToggled(player, AbilityToggleType.Dash))
                    {
                        return "Please disable Dash and try again.";
                    }

                    return string.Empty;
                })
                .TriggerPurchase((player, type, level) =>
                {
                    ToggleDash(player);
                })
                .TriggerRefund((player, type, level) =>
                {
                    ToggleDash(player);
                });
        }
    }
}
