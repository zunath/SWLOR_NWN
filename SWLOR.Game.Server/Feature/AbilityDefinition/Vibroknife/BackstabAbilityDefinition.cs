using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Vibroknife
{
    public class BackstabAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureCastedTarget(
                builder
                    .Create(FeatType.Backstab1, PerkType.Backstab)
                    .Name("Backstab I")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.Backstab, 60f),
                SkillType.Vibroknife,
                20,
                4);
            ConfigureCastedTarget(
                builder
                    .Create(FeatType.Backstab2, PerkType.Backstab)
                    .Name("Backstab II")
                    .Level(2)
                    .HasRecastDelay(RecastGroup.Backstab, 60f),
                SkillType.Vibroknife,
                40,
                6);
            ConfigureCastedTarget(
                builder
                    .Create(FeatType.Backstab3, PerkType.Backstab)
                    .Name("Backstab III")
                    .Level(3)
                    .HasRecastDelay(RecastGroup.Backstab, 60f),
                SkillType.Vibroknife,
                60,
                8,
                3,
                typeof(KnockdownStatusEffect));

            return builder.Build();
        }
    }
}
