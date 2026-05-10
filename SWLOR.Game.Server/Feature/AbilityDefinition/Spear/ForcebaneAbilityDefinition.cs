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

            ConfigureAreaStatus(builder.Create(FeatType.Forcebane1, PerkType.Forcebane).Name("Forcebane").Level(1), typeof(ForcebaneStatusEffect), 8f, 50, false, fpDrainPercent: 50);

            return builder.Build();
        }
    }
}
