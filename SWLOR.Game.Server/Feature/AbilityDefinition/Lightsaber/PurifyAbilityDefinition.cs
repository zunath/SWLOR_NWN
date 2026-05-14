using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Lightsaber
{
    public class PurifyAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            builder
                .Create(FeatType.Purify1, PerkType.Purify)
                .Name("Purify")
                .Level(1)
                .HasActivationDelay(2f)
                .HasRecastDelay(RecastGroup.Purify, 30f)
                .HasImpactAction((activator, target, level, targetLocation) => PurifyAndMirror(activator))
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(4);

            return builder.Build();
        }
    }
}
