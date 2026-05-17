using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Rifle
{
    public class TranqConeAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        private const SkillType Skill = SkillType.Rifle;
        private const int BaseDamage = 0;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            TranqCone1(builder);
            TranqCone2(builder);

            return builder.Build();
        }

        private static void TranqCone1(AbilityBuilder builder)
        {
            var ability = builder
                .Create(FeatType.TranqCone1, PerkType.TranqCone)
                .Name("Tranq Cone I")
                .Level(1)
                .UsesImpactAnimation(Animation.PointPistol)
                .HasRecastDelay(RecastGroup.TranqCone, 120f);

            ConfigureTranqCone(
                ability,
                tranquilizeDuration: 8,
                coneLength: 8f,
                coneWidth: 6f,
                maxTargets: 3,
                stamina: 8);
        }

        private static void TranqCone2(AbilityBuilder builder)
        {
            var ability = builder
                .Create(FeatType.TranqCone2, PerkType.TranqCone)
                .Name("Tranq Cone II")
                .Level(2)
                .UsesImpactAnimation(Animation.PointPistol)
                .HasRecastDelay(RecastGroup.TranqCone, 120f);

            ConfigureTranqCone(
                ability,
                tranquilizeDuration: 10,
                coneLength: 10f,
                coneWidth: 7f,
                maxTargets: 5,
                stamina: 10);
        }

        private static void ConfigureTranqCone(
            AbilityBuilder ability,
            int tranquilizeDuration,
            float coneLength,
            float coneWidth,
            int maxTargets,
            int stamina)
        {
            ConfigureTelegraphedArea(
                ability,
                Skill,
                CombatImpactAreaShape.Cone,
                BaseDamage,
                tranquilizeDuration,
                typeof(TranquilizedStatusEffect),
                coneLength,
                coneWidth,
                stamina,
                maxTargets: maxTargets);
        }
    }
}
