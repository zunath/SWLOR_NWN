using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Katar
{
    public class ToxicRushAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureSelfStatus(
                builder
                    .Create(FeatType.ToxicRush1, PerkType.ToxicRush)
                    .Name("Toxic Rush")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.ToxicRush, 120f),
                typeof(ToxicRushStatusEffect),
                20f,
                10);

            return builder.Build();
        }
    }
}
