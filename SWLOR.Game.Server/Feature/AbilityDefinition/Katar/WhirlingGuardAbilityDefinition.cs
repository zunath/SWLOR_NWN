using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Katar
{
    public class WhirlingGuardAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            WhirlingGuard1(builder);

            return builder.Build();
        }

        private static void WhirlingGuard1(AbilityBuilder builder)
        {
            ConfigureSelfStatus(
                builder
                    .Create(FeatType.WhirlingGuard1, PerkType.WhirlingGuard)
                    .Name("Whirling Guard")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.WhirlingGuard, 120f),
                typeof(WhirlingGuardStatusEffect),
                12f,
                12);
        }
    }
}
