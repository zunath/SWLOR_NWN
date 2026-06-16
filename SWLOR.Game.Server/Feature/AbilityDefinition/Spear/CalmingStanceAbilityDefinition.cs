using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Spear
{
    public class CalmingStanceAbilityDefinition : SpearActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureToggle(
                builder
                    .Create(FeatType.CalmingStance1, PerkType.CalmingStance)
                    .Name("Calming Stance")
                    .Level(1)
                    .SkillType(SkillType.Spear)
                    .HasRecastDelay(RecastGroup.CalmingStance, 180f)
                    .UsesAnimation(Animation.ParadeRest),
                typeof(CalmingStanceStatusEffect));

            return builder.Build();
        }
    }
}
