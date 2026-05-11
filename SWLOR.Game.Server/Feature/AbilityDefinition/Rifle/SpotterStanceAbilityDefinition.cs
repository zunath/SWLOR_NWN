using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Rifle
{
    public class SpotterStanceAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureToggle(
                builder
                    .Create(FeatType.SpotterStance1, PerkType.SpotterStance)
                    .Name("Spotter Stance")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.SpotterStance, 180f),
                typeof(SpotterStanceStatusEffect));

            return builder.Build();
        }
    }
}
