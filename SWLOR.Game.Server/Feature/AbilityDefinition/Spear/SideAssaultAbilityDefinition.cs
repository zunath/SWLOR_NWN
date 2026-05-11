using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Spear
{
    public class SideAssaultAbilityDefinition : SpearActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureWeapon(
                builder
                    .Create(FeatType.SideAssault1, PerkType.SideAssault)
                    .Name("Side Assault I")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.SideAssault, 3f),
                SkillType.Spear,
                12,
                0,
                null,
                4);
            ConfigureWeapon(
                builder
                    .Create(FeatType.SideAssault2, PerkType.SideAssault)
                    .Name("Side Assault II")
                    .Level(2)
                    .HasRecastDelay(RecastGroup.SideAssault, 3f),
                SkillType.Spear,
                25,
                0,
                null,
                6);
            ConfigureWeapon(
                builder
                    .Create(FeatType.SideAssault3, PerkType.SideAssault)
                    .Name("Side Assault III")
                    .Level(3)
                    .HasRecastDelay(RecastGroup.SideAssault, 3f),
                SkillType.Spear,
                35,
                0,
                null,
                8);

            return builder.Build();
        }
    }
}
