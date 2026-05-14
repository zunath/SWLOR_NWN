using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Vibroknife
{
    public class IncapacitateAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureAreaStatus(
                builder
                    .Create(FeatType.Incapacitate1, PerkType.Incapacitate)
                    .Name("Incapacitate")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.Incapacitate, 120f),
                typeof(IncapacitateStatusEffect),
                20f,
                10,
                true,
                activationDelay: 2f);

            return builder.Build();
        }
    }
}
