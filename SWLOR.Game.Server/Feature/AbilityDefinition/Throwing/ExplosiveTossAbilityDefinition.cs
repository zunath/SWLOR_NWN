using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

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
            ExplosiveToss(builder, FeatType.ExplosiveToss1, "Explosive Toss I", level: 1, stamina: 4, ExplosiveToss1ImpactAction);
        }

        private static void ExplosiveToss2(AbilityBuilder builder)
        {
            ExplosiveToss(builder, FeatType.ExplosiveToss2, "Explosive Toss II", level: 2, stamina: 5, ExplosiveToss2ImpactAction);
        }

        private static void ExplosiveToss3(AbilityBuilder builder)
        {
            ExplosiveToss(builder, FeatType.ExplosiveToss3, "Explosive Toss III", level: 3, stamina: 7, ExplosiveToss3ImpactAction);
        }

        private static void ExplosiveToss4(AbilityBuilder builder)
        {
            ExplosiveToss(builder, FeatType.ExplosiveToss4, "Explosive Toss IV", level: 4, stamina: 9, ExplosiveToss4ImpactAction);
        }

        private static void ExplosiveToss(
            AbilityBuilder builder,
            FeatType feat,
            string name,
            int level,
            int stamina,
            AbilityImpactAction impactAction)
        {
            builder
                .Create(feat, PerkType.ExplosiveToss)
                .Name(name)
                .Level(level)
                .HasRecastDelay(RecastGroup.ExplosiveToss, 45f)
                .HasActivationDelay(0f)
                .SkillType(Skill)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .UsesImpactAnimation(Animation.ThrowGrenade)
                .HasMaxRange(ThrowingAbilityRange.Standard)
                .IsAreaAbility()
                .HasImpactAction(impactAction)
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(stamina);
        }

        private static void ExplosiveToss1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyExplosiveToss(activator, target, targetLocation, 8, 0, null);
        }

        private static void ExplosiveToss2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyExplosiveToss(activator, target, targetLocation, 16, 0, null);
        }

        private static void ExplosiveToss3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyExplosiveToss(activator, target, targetLocation, 26, 0, null);
        }

        private static void ExplosiveToss4ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyExplosiveToss(activator, target, targetLocation, 38, 15, typeof(ExposedStatusEffect));
        }

        private static void ApplyExplosiveToss(uint activator, uint target, Location targetLocation, int baseDamage, int duration, Type statusEffect)
        {
            var bleedDuration = Stat.GetStatAdjustment(activator, StatType.ExplosiveTossBleedDurationSeconds);
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                Skill,
                baseDamage,
                duration,
                statusEffect,
                CombatImpactAreaShape.Sphere,
                TelegraphDelay,
                Radius,
                maxTargets: 3,
                damageType: CombatDamageType.Fire,
                targetVisualEffect: VisualEffect.Vfx_Com_Hit_Fire,
                areaVisualEffect: VisualEffect.Vfx_Fnf_Gas_Explosion_Fire,
                afterSuccessfulHit: hitTarget => ApplyBleedIfUnlocked(activator, hitTarget, bleedDuration));
        }

        private static void ApplyBleedIfUnlocked(uint activator, uint target, int bleedDuration)
        {
            if (bleedDuration <= 0)
                return;

            StatusEffect.ApplyStatusEffect(activator, target, typeof(BleedStatusEffect), bleedDuration, CombatDamageType.Fire);
        }
    }
}
