using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Spear
{
    public class ForcebaneAbilityDefinition : SpearActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureAreaStatus(
                builder
                    .Create(FeatType.Forcebane1, PerkType.Forcebane)
                    .Name("Forcebane")
                    .Level(1)
                    .SkillType(SkillType.Spear)
                    .HasRecastDelay(RecastGroup.Capstone, 1800f),
                typeof(ForcebaneStatusEffect),
                8f,
                25,
                false,
                fpDrainPercent: 50,
                activationDelay: 2f);

            return builder.Build();
        }
    }
}
