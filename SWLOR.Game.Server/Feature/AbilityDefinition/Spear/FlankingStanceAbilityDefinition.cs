using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Spear
{
    public class FlankingStanceAbilityDefinition : SpearActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureToggle(builder.Create(FeatType.FlankingStance1, PerkType.FlankingStance).Name("Flanking Stance").Level(1), typeof(FlankingStanceStatusEffect));

            return builder.Build();
        }
    }
}
