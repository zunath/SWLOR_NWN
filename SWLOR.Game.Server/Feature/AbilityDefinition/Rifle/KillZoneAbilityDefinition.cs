using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Rifle
{
    public class KillZoneAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureSelfStatus(
                builder
                    .Create(FeatType.KillZone1, PerkType.KillZone)
                    .Name("Kill Zone")
                    .Level(1)
                    .SkillType(SkillType.Rifle)
                    .HasRecastDelay(RecastGroup.KillZone, 120f),
                typeof(KillZoneStatusEffect),
                20f,
                10);

            return builder.Build();
        }
    }
}
