using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Unarmed
{
    public class BruiserStanceAbilityDefinition : UnarmedAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            ConfigureStance(
                builder.Create(FeatType.BruiserStance1, PerkType.BruiserStance)
                    .Name("Bruiser Stance")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.BruiserStance, 30f),
                typeof(BruiserStanceStatusEffect));
            return builder.Build();
        }
    }
}
