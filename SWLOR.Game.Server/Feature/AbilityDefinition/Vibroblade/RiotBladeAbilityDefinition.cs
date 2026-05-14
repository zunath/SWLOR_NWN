using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Vibroblade
{
    public class RiotBladeAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureCastedTarget(
                builder
                    .Create(FeatType.RiotBlade1, PerkType.RiotBlade)
                    .Name("Riot Blade I")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.RiotBlade, 60f),
                SkillType.Vibroblade,
                15,
                3);
            ConfigureCastedTarget(
                builder
                    .Create(FeatType.RiotBlade2, PerkType.RiotBlade)
                    .Name("Riot Blade II")
                    .Level(2)
                    .HasRecastDelay(RecastGroup.RiotBlade, 60f),
                SkillType.Vibroblade,
                30,
                5);
            ConfigureCastedTarget(
                builder
                    .Create(FeatType.RiotBlade3, PerkType.RiotBlade)
                    .Name("Riot Blade III")
                    .Level(3)
                    .HasRecastDelay(RecastGroup.RiotBlade, 60f),
                SkillType.Vibroblade,
                45,
                8);

            return builder.Build();
        }
    }
}
