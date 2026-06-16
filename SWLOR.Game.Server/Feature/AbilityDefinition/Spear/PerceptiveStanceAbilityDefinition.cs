using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Spear
{
    public class PerceptiveStanceAbilityDefinition : SpearActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureToggle(
                builder
                    .Create(FeatType.PerceptiveStance1, PerkType.PerceptiveStance)
                    .Name("Perceptive Stance")
                    .Level(1)
                    .SkillType(SkillType.Spear)
                    .HasRecastDelay(RecastGroup.PerceptiveStance, 180f)
                    .UsesAnimation(Animation.ParadeRest),
                typeof(PerceptiveStanceStatusEffect));

            return builder.Build();
        }
    }
}
