using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Staff
{
    public class GuardingStepAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureSelfStatus(
                builder
                    .Create(FeatType.GuardingStep1, PerkType.GuardingStep)
                    .Name("Guarding Step")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.GuardingStep, 60f),
                typeof(GuardingStepStatusEffect),
                8f,
                6);

            return builder.Build();
        }
    }
}
