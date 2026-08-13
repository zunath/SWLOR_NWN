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
    public sealed class IonLanceAbilityDefinition : IAbilityListDefinition
    {
        private const float LineLengthMeters = 8f;
        private const float LineWidthMeters = 2.5f;
        private const int Rank1BaseDamage = 16;
        private const int Rank2BaseDamage = 30;
        private const int Rank3BaseDamage = 44;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            IonLance1(builder);
            IonLance2(builder);
            IonLance3(builder);

            return builder.Build();
        }

        private static void IonLance1(AbilityBuilder builder)
        {
            ConfigureIonLance(builder, FeatType.IonLance1, Spell.IonLance1, "Ion Lance I", 1, Rank1BaseDamage, 4, 15f);
        }

        private static void IonLance2(AbilityBuilder builder)
        {
            ConfigureIonLance(builder, FeatType.IonLance2, Spell.IonLance2, "Ion Lance II", 2, Rank2BaseDamage, 5, 15f);
        }

        private static void IonLance3(AbilityBuilder builder)
        {
            ConfigureIonLance(builder, FeatType.IonLance3, Spell.IonLance3, "Ion Lance III", 3, Rank3BaseDamage, 6, 18f);
        }

        private static void ConfigureIonLance(
            AbilityBuilder builder,
            FeatType feat,
            Spell spell,
            string name,
            int level,
            int baseDamage,
            int stamina,
            float cooldown)
        {
            builder
                .Create(feat, PerkType.IonLance)
                .Name(name)
                .Level(level)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.IonLance, cooldown)
                .SkillType(SkillType.Devices)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .PlaysSoundOnImpact("ksfx_ion_ray")
                .IsAreaAbility()
                .HasTargetingLine(
                    spell,
                    LineLengthMeters,
                    LineWidthMeters,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf)
                .HasImpactAction((activator, target, _, targetLocation) =>
                    ApplyIonLance(activator, target, targetLocation, baseDamage))
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(stamina);
        }

        private static void ApplyIonLance(uint activator, uint target, Location targetLocation, int baseDamage)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Devices,
                baseDamage,
                0,
                null,
                CombatImpactAreaShape.Line,
                0f,
                LineLengthMeters,
                LineWidthMeters,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Electrical,
                targetVisualEffect: VisualEffect.Vfx_Com_Hit_Electrical,
                areaVisualEffect: VisualEffect.None,
                damagePercentAdjustment: DeviceAbilityEffects.GetAssaultGadgetDamageAdjustment(activator),
                baseDamageAdjustment: DeviceAbilityEffects.GetAssaultGadgetBaseDamageAdjustment(activator),
                afterSuccessfulHit: hitTarget => ApplyIonLanceHitEffects(activator, hitTarget),
                hitChancePercentAdjustment: DeviceAbilityEffects.GetAssaultGadgetAccuracyAdjustment(activator),
                criticalRatePercentAdjustment: DeviceAbilityEffects.GetAssaultGadgetCriticalRateAdjustment(activator));
        }

        private static void ApplyIonLanceHitEffects(uint activator, uint target)
        {
            DeviceAbilityEffects.ApplyElectricArcVisual(activator, target);
            DeviceAbilityEffects.ApplyTacticalUplink(activator);
        }
    }
}
