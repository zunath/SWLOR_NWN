using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Katar
{
    public class TwinGuardStanceAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureToggle(
                builder
                    .Create(FeatType.TwinGuardStance1, PerkType.TwinGuardStance)
                    .Name("Twin Guard Stance")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.TwinGuardStance, 180f),
                typeof(TwinGuardStanceStatusEffect));

            return builder.Build();
        }
    }
}
