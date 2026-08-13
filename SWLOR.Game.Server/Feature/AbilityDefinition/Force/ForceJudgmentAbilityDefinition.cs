using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public sealed class ForceJudgmentAbilityDefinition : IAbilityListDefinition
    {
        private const float RadiusMeters = 5f;
        private const int HitChancePercentAdjustment = 10;
        private const int Rank1BaseDamage = 18;
        private const int Rank2BaseDamage = 32;
        private const int Rank3BaseDamage = 48;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ForceJudgment1(builder);
            ForceJudgment2(builder);
            ForceJudgment3(builder);

            return builder.Build();
        }

        private static void ForceJudgment1(AbilityBuilder builder)
        {
            ConfigureForceJudgment(
                builder,
                FeatType.ForceJudgment1,
                Spell.ForceJudgment1,
                "Force Judgment I",
                1,
                Rank1BaseDamage,
                2,
                12f,
                typeof(ForceJudgment1StatusEffect),
                1);
        }

        private static void ForceJudgment2(AbilityBuilder builder)
        {
            ConfigureForceJudgment(
                builder,
                FeatType.ForceJudgment2,
                Spell.ForceJudgment2,
                "Force Judgment II",
                2,
                Rank2BaseDamage,
                3,
                12f,
                typeof(ForceJudgment2StatusEffect),
                2);
        }

        private static void ForceJudgment3(AbilityBuilder builder)
        {
            ConfigureForceJudgment(
                builder,
                FeatType.ForceJudgment3,
                Spell.ForceJudgment3,
                "Force Judgment III",
                3,
                Rank3BaseDamage,
                4,
                15f,
                typeof(ForceJudgment3StatusEffect),
                0);
        }

        private static void ConfigureForceJudgment(
            AbilityBuilder builder,
            FeatType feat,
            Spell spell,
            string name,
            int level,
            int baseDamage,
            int fp,
            float cooldown,
            Type statusEffect,
            int maxTargets)
        {
            var ability = builder
                .Create(feat, PerkType.ForceJudgment)
                .Name(name)
                .Level(level)
                .HasActivationDelay(1.5f)
                .HasRecastDelay(RecastGroup.ForceJudgment, cooldown)
                .SkillType(SkillType.Force)
                .CombatImpactDamageAbility(AbilityType.Willpower)
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .DisplaysVisualEffectWhenActivating()
                .PlaysSoundOnImpact("ksfx_use_force")
                .HasMaxRange(15f)
                .RequiresTarget()
                .HasImpactAction((activator, target, _, targetLocation) =>
                    ApplyForceJudgment(activator, target, targetLocation, baseDamage, statusEffect, maxTargets))
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementFP(fp);

            if (maxTargets == 1)
            {
                ability.IsSingleTargetAbility();
            }
            else
            {
                ability
                    .IsAreaAbility()
                    .HasTargetingSphere(
                        spell,
                        RadiusMeters,
                        AbilityTargetingFlags.HarmsEnemies);
            }
        }

        private static void ApplyForceJudgment(
            uint activator,
            uint target,
            Location targetLocation,
            int baseDamage,
            Type statusEffect,
            int maxTargets)
        {
            if (maxTargets == 1)
            {
                Ability.ApplyCombatImpact(
                    activator,
                    target,
                    targetLocation,
                    SkillType.Force,
                    baseDamage,
                    30,
                    statusEffect,
                    false,
                    Array.Empty<Type>(),
                    damageType: CombatDamageType.Force,
                    targetVisualEffect: VisualEffect.Vfx_Imp_Pulse_Negative,
                    hitChancePercentAdjustment: HitChancePercentAdjustment);
                return;
            }

            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Force,
                baseDamage,
                30,
                statusEffect,
                CombatImpactAreaShape.Sphere,
                0f,
                RadiusMeters,
                0f,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Force,
                targetVisualEffect: VisualEffect.Vfx_Imp_Pulse_Negative,
                areaVisualEffect: VisualEffect.None,
                maxTargets: maxTargets,
                hitChancePercentAdjustment: HitChancePercentAdjustment);
        }
    }
}
