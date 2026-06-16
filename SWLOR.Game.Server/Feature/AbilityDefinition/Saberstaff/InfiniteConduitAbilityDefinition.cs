using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
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
                    .SkillType(SkillType.Saberstaff)
                    .HasRecastDelay(RecastGroup.Capstone, CapstoneAbility.RecastDelaySeconds)
                    .UsesAnimation(Animation.CastOutAnimation),
                typeof(InfiniteConduitStatusEffect),
                CapstoneAbility.ActiveDurationSeconds,
                CapstoneAbility.StaminaCost,
                activationDelay: 2f);

            return builder.Build();
        }
    }
}
