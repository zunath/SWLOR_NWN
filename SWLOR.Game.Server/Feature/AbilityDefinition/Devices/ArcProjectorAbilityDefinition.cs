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

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Devices
{
    public sealed class ArcProjectorAbilityDefinition : IAbilityListDefinition
    {
        private const int Rank1BaseDamage = 22;
        private const int Rank2BaseDamage = 40;
        private const int Rank3BaseDamage = 60;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ArcProjector1(builder);
            ArcProjector2(builder);
            ArcProjector3(builder);

            return builder.Build();
        }

        private static void ArcProjector1(AbilityBuilder builder)
        {
            ConfigureArcProjector(builder, FeatType.ArcProjector1, "Arc Projector I", 1, Rank1BaseDamage, 3);
        }

        private static void ArcProjector2(AbilityBuilder builder)
        {
            ConfigureArcProjector(builder, FeatType.ArcProjector2, "Arc Projector II", 2, Rank2BaseDamage, 4);
        }

        private static void ArcProjector3(AbilityBuilder builder)
        {
            ConfigureArcProjector(builder, FeatType.ArcProjector3, "Arc Projector III", 3, Rank3BaseDamage, 5);
        }

        private static void ConfigureArcProjector(
            AbilityBuilder builder,
            FeatType feat,
            string name,
            int level,
            int baseDamage,
            int stamina)
        {
            builder
                .Create(feat, PerkType.ArcProjector)
                .Name(name)
                .Level(level)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.ArcProjector, 12f)
                .SkillType(SkillType.Devices)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .PlaysSoundOnImpact("ksfx_beam")
                .HasMaxRange(DeviceAbilityRange.Standard)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction((activator, target, _, targetLocation) =>
                    ApplyArcProjector(activator, target, targetLocation, baseDamage))
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(stamina);
        }

        private static void ApplyArcProjector(uint activator, uint target, Location targetLocation, int baseDamage)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Devices,
                baseDamage,
                0,
                null,
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Electrical,
                targetVisualEffect: VisualEffect.Vfx_Com_Hit_Electrical,
                damagePercentAdjustment: DeviceAbilityEffects.GetAssaultGadgetDamageAdjustment(activator),
                baseDamageAdjustment: DeviceAbilityEffects.GetAssaultGadgetBaseDamageAdjustment(activator),
                afterSuccessfulHit: hitTarget => ApplyArcProjectorHitEffects(activator, hitTarget),
                hitChancePercentAdjustment: DeviceAbilityEffects.GetAssaultGadgetAccuracyAdjustment(activator),
                criticalRatePercentAdjustment: DeviceAbilityEffects.GetAssaultGadgetCriticalRateAdjustment(activator));
        }

        private static void ApplyArcProjectorHitEffects(uint activator, uint target)
        {
            DeviceAbilityEffects.ApplyElectricArcVisual(activator, target);
            DeviceAbilityEffects.ApplyTacticalUplink(activator);
        }
    }
}
