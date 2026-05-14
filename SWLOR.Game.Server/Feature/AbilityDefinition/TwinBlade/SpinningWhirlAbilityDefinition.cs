using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.TwinBlade
{
    public class SpinningWhirlAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureTelegraphedArea(
                builder
                    .Create(FeatType.SpinningWhirl1, PerkType.SpinningWhirl)
                    .Name("Spinning Whirl I")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.SpinningWhirl, 60f),
                SkillType.TwinBlade,
                CombatImpactAreaShape.Sphere,
                10,
                0,
                null,
                5f,
                0f,
                5,
                true,
                maxTargets: 3);
            ConfigureTelegraphedArea(
                builder
                    .Create(FeatType.SpinningWhirl2, PerkType.SpinningWhirl)
                    .Name("Spinning Whirl II")
                    .Level(2)
                    .HasRecastDelay(RecastGroup.SpinningWhirl, 60f),
                SkillType.TwinBlade,
                CombatImpactAreaShape.Sphere,
                18,
                0,
                null,
                5f,
                0f,
                7,
                true,
                maxTargets: 3);
            ConfigureTelegraphedArea(
                builder
                    .Create(FeatType.SpinningWhirl3, PerkType.SpinningWhirl)
                    .Name("Spinning Whirl III")
                    .Level(3)
                    .HasRecastDelay(RecastGroup.SpinningWhirl, 60f),
                SkillType.TwinBlade,
                CombatImpactAreaShape.Sphere,
                28,
                0,
                null,
                5f,
                0f,
                10,
                true,
                maxTargets: 3);

            return builder.Build();
        }
    }
}
