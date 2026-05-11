using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Pistol
{
    public class GunslingerFocusAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            builder.Create(FeatType.GunslingerFocus1, PerkType.GunslingerFocus)
                .Name("Gunslinger Focus")
                .Level(1)
                .HasRecastDelay(RecastGroup.GunslingerFocus, 120f);
            ConfigureSelfStatus(builder, typeof(GunslingerFocusStatusEffect), duration: 20f, stamina: 6);

            return builder.Build();
        }
    }
}
