using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Saberstaff
{
    public class ForceCapacitorAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureSelfStatus(
                builder
                    .Create(FeatType.ForceCapacitor1, PerkType.ForceCapacitor)
                    .Name("Force Capacitor")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.ForceCapacitor, 180f),
                typeof(ForceCapacitorStatusEffect),
                20f,
                5);

            return builder.Build();
        }
    }
}
