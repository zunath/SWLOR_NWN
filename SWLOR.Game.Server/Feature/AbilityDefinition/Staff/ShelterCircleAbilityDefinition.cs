using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Staff
{
    public class ShelterCircleAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigurePartyStatus(
                builder
                    .Create(FeatType.ShelterCircle1, PerkType.ShelterCircle)
                    .Name("Shelter Circle")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.ShelterCircle, 180f),
                typeof(ShelterCircleStatusEffect),
                15f,
                20,
                true);

            return builder.Build();
        }
    }
}
