using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Lightsaber
{
    public class CenteredAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            Centering1(builder);
            Centering2(builder);

            return builder.Build();
        }

        private static void Centering1(AbilityBuilder builder)
        {
            ConfigureSelfStatus(
                builder
                    .Create(FeatType.Centering1, PerkType.Centering)
                    .Name("Centering I")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.Centering, 60f),
                typeof(CenteringStatusEffect),
                30f,
                3,
                activator => Enmity.ReduceEnmityOnAll(activator, 25));
        }

        private static void Centering2(AbilityBuilder builder)
        {
            ConfigureSelfStatus(
                builder
                    .Create(FeatType.Centering2, PerkType.Centering)
                    .Name("Centering II")
                    .Level(2)
                    .HasRecastDelay(RecastGroup.Centering, 60f),
                typeof(CenteringStatusEffect),
                30f,
                5,
                activator => Enmity.ReduceEnmityOnAll(activator, 50));
        }
    }
}
