using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Lightsaber
{
    public class FerocityStanceAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureToggle(
                builder
                    .Create(FeatType.FerocityStance1, PerkType.FerocityStance)
                    .Name("Ferocity Stance")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.FerocityStance, 180f),
                typeof(FerocityStanceStatusEffect));

            return builder.Build();
        }
    }
}
