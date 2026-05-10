using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Throwing
{
    public class ExplosiveTossAbilityDefinition : IAbilityListDefinition
    {
        private const SkillType Skill = SkillType.Throwing;
        private const float Radius = 3f;
        private const float TelegraphDelay = 0f;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ExplosiveToss1(builder);
            ExplosiveToss2(builder);
            ExplosiveToss3(builder);
            ExplosiveToss4(builder);

            return builder.Build();
        }

        private static void ExplosiveToss1(AbilityBuilder builder)
        {
            ExplosiveToss(builder, FeatType.ExplosiveToss1, "Explosive Toss I", level: 1, stamina: 4);
        }

        private static void ExplosiveToss2(AbilityBuilder builder)
        {
            ExplosiveToss(builder, FeatType.ExplosiveToss2, "Explosive Toss II", level: 2, stamina: 5);
        }

        private static void ExplosiveToss3(AbilityBuilder builder)
        {
            ExplosiveToss(builder, FeatType.ExplosiveToss3, "Explosive Toss III", level: 3, stamina: 7);
        }

        private static void ExplosiveToss4(AbilityBuilder builder)
        {
            ExplosiveToss(builder, FeatType.ExplosiveToss4, "Explosive Toss IV", level: 4, stamina: 9);
        }

        private static void ExplosiveToss(
            AbilityBuilder builder,
            FeatType feat,
            string name,
            int level,
            int stamina)
        {
            builder.Create(feat, PerkType.ExplosiveToss)
                .Name(name)
                .Level(level)
                .HasActivationDelay(0f)
                .SkillType(Skill)
                .IsAreaAbility()
                .HasImpactAction(ImpactAction)
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(stamina);
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var (baseDamage, duration, savingThrowDc, statusEffect) = level switch
            {
                1 => (8, 0, 0, null),
                2 => (16, 0, 0, null),
                3 => (26, 0, 0, null),
                4 => (38, 15, 16, typeof(ExposedStatusEffect)),
                _ => (0, 0, 0, null)
            };

            if (baseDamage <= 0)
                return;

            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                Skill,
                baseDamage,
                duration,
                savingThrowDc,
                SavingThrow.Fortitude,
                statusEffect,
                CombatImpactAreaShape.Sphere,
                TelegraphDelay,
                Radius);
        }
    }
}
