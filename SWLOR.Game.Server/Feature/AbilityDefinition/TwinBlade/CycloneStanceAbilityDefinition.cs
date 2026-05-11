using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.TwinBlade
{
    public class CycloneStanceAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureToggle(
                builder
                    .Create(FeatType.CycloneStance1, PerkType.CycloneStance)
                    .Name("Cyclone Stance")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.CycloneStance, 180f),
                typeof(CycloneStanceStatusEffect));

            return builder.Build();
        }
    }
}
