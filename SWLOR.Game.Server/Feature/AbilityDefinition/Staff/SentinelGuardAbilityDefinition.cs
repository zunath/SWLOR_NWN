using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Staff
{
    public class SentinelGuardAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigurePartyStatus(
                builder
                    .Create(FeatType.SentinelGuard1, PerkType.SentinelGuard)
                    .Name("Sentinel Guard")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.SentinelGuard, 120f),
                typeof(SentinelGuardStatusEffect),
                12f,
                10,
                true);

            return builder.Build();
        }
    }
}
