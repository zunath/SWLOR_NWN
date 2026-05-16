using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Saberstaff
{
    public class TempestStanceAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureToggle(
                builder
                    .Create(FeatType.TempestStance1, PerkType.TempestStance)
                    .Name("Tempest Stance")
                    .Level(1)
                    .SkillType(SkillType.Saberstaff)
                    .HasRecastDelay(RecastGroup.TempestStance, 180f),
                typeof(TempestStanceStatusEffect));

            return builder.Build();
        }
    }
}
