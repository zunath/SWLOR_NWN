using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Saberstaff
{
    public class InfiniteConduitAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureSelfStatus(
                builder
                    .Create(FeatType.InfiniteConduit1, PerkType.InfiniteConduit)
                    .Name("Infinite Conduit")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.Capstone, 1800f),
                typeof(InfiniteConduitStatusEffect),
                20f,
                25,
                activationDelay: 2f);

            return builder.Build();
        }
    }
}
