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
            TranqCone(
                builder,
                FeatType.TranqCone1,
                "Tranq Cone I",
                level: 1,
                dazeDuration: 8,
                coneLength: 8f,
                coneWidth: 6f,
                stamina: 8);
        }

        private static void TranqCone2(AbilityBuilder builder)
        {
            TranqCone(
                builder,
                FeatType.TranqCone2,
                "Tranq Cone II",
                level: 2,
                dazeDuration: 10,
                coneLength: 10f,
                coneWidth: 7f,
                stamina: 10);
        }

        private static void TranqCone(
            AbilityBuilder builder,
            FeatType feat,
            string name,
            int level,
            int dazeDuration,
            float coneLength,
            float coneWidth,
            int stamina)
        {
            var ability = builder.Create(feat, PerkType.TranqCone)
                .Name(name)
                .Level(level);

            ConfigureTelegraphedArea(
                ability,
                Skill,
                CombatImpactAreaShape.Cone,
                BaseDamage,
                dazeDuration,
                typeof(DazedStatusEffect),
                coneLength,
                coneWidth,
                stamina);
        }
    }
}
