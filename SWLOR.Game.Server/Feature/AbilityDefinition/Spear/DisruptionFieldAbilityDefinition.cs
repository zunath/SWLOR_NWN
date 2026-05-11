using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Spear
{
    public class DisruptionFieldAbilityDefinition : SpearActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureAreaStatus(
                builder
                    .Create(FeatType.DisruptionField1, PerkType.DisruptionField)
                    .Name("Disruption Field")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.DisruptionField, 180f),
                typeof(DisruptionFieldStatusEffect),
                20f,
                5,
                false,
                fpDrainPercent: 20);

            return builder.Build();
        }
    }
}
