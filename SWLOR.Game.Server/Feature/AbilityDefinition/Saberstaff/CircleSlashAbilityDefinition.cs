using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Saberstaff
{
    public class CircleSlashAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureTelegraphedArea(
                builder
                    .Create(FeatType.CircleSlash1, PerkType.CircleSlash)
                    .Name("Circle Slash I")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.CircleSlash, 60f),
                SkillType.Saberstaff,
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
                    .Create(FeatType.CircleSlash2, PerkType.CircleSlash)
                    .Name("Circle Slash II")
                    .Level(2)
                    .HasRecastDelay(RecastGroup.CircleSlash, 60f),
                SkillType.Saberstaff,
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
                    .Create(FeatType.CircleSlash3, PerkType.CircleSlash)
                    .Name("Circle Slash III")
                    .Level(3)
                    .HasRecastDelay(RecastGroup.CircleSlash, 60f),
                SkillType.Saberstaff,
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
