using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Unarmed
{
    public class FlowStanceAbilityDefinition : UnarmedAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            ConfigureStance(
                builder.Create(FeatType.FlowStance1, PerkType.FlowStance)
                    .Name("Flow Stance")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.FlowStance, 30f),
                typeof(FlowStanceStatusEffect));
            return builder.Build();
        }
    }
}
