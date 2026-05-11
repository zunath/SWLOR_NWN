using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Lightsaber
{
    public class FocusedStanceAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureToggle(
                builder
                    .Create(FeatType.FocusedStance1, PerkType.FocusedStance)
                    .Name("Focused Stance")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.FocusedStance, 180f),
                typeof(FocusedStanceStatusEffect));

            return builder.Build();
        }
    }
}
