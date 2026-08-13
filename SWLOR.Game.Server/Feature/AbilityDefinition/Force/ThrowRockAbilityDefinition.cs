using System;
using System.Collections.Generic;
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
    public sealed class ThrowRockAbilityDefinition : IAbilityListDefinition
    {
        private const int HitChancePercentAdjustment = 10;
        private const int Rank1BaseDamage = 22;
        private const int Rank2BaseDamage = 40;
        private const int Rank3BaseDamage = 60;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ThrowRock1(builder);
            ThrowRock2(builder);
            ThrowRock3(builder);

            return builder.Build();
        }

        private static void ThrowRock1(AbilityBuilder builder)
        {
            ConfigureThrowRock(
                builder,
                FeatType.ThrowRock1,
                "Throw Rock I",
                1,
                Rank1BaseDamage,
                3);
        }

        private static void ThrowRock2(AbilityBuilder builder)
        {
            ConfigureThrowRock(
                builder,
                FeatType.ThrowRock2,
                "Throw Rock II",
                2,
                Rank2BaseDamage,
                4);
        }

        private static void ThrowRock3(AbilityBuilder builder)
        {
            ConfigureThrowRock(
                builder,
                FeatType.ThrowRock3,
                "Throw Rock III",
                3,
                Rank3BaseDamage,
                5);
        }

        private static void ConfigureThrowRock(
            AbilityBuilder builder,
            FeatType feat,
            string name,
            int level,
            int baseDamage,
            int fp)
        {
            builder
                .Create(feat, PerkType.ThrowRock)
                .Name(name)
                .Level(level)
                .HasActivationDelay(1.5f)
                .UsesAnimation(Animation.CastOutAnimation)
                .HasRecastDelay(RecastGroup.ThrowRock, 6f)
                .SkillType(SkillType.Force)
                .CombatImpactDamageAbility(AbilityType.Willpower)
                .DisplaysVisualEffectWhenActivating(VisualEffect.None)
                .PlaysSoundOnImpact("ksfx_gravity")
                .IsSingleTargetAbility()
                .HasMaxRange(15f)
                .RequiresTarget()
                .HasImpactAction((activator, target, _, targetLocation) =>
                    ApplyThrowRock(activator, target, targetLocation, level, baseDamage))
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementFP(fp);
        }

        private static void ApplyThrowRock(uint activator, uint target, Location targetLocation, int level, int baseDamage)
        {
            AssignCommand(activator, () => ApplyRockMissile(target, level));

            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Force,
                baseDamage,
                0,
                null,
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Physical,
                targetVisualEffect: VisualEffect.Vfx_Imp_Dust_Explosion,
                hitChancePercentAdjustment: HitChancePercentAdjustment,
                playImpactAnimation: false);
        }

        private static void ApplyRockMissile(uint target, int level)
        {
            var rockType = level >= 3
                ? VisualEffect.Vfx_Imp_Mirv_Rock3
                : VisualEffect.Vfx_Imp_Mirv_Rock;

            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(rockType), target);
        }
    }
}
