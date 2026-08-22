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

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Devices
{
    public sealed class CryoSprayerAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            CryoSprayer1(builder);

            return builder.Build();
        }

        private static void CryoSprayer1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.CryoSprayer1, PerkType.CryoSprayer)
                .Name("Cryo Sprayer")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.CryoSprayer, 15f)
                .SkillType(SkillType.Devices)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .PlaysSoundOnImpact("ksfx_cold_ray")
                .IsAreaAbility()
                .HasImpactAction(CryoSprayer1ImpactAction)
                .HasTargetingCone(
                    Spell.CryoSprayer1,
                    6f,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(5);
        }

        private static void CryoSprayer1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Devices,
                6,
                30,
                typeof(HobbleStatusEffect),
                CombatImpactAreaShape.Cone,
                0f,
                6f,
                5f,
                Array.Empty<Type>(),
                centerOnActivator: !GetIsObjectValid(target),
                damageType: CombatDamageType.Ice,
                targetVisualEffect: VisualEffect.Vfx_Com_Hit_Frost,
                areaVisualEffect: VisualEffect.Vfx_Fnf_Icestorm,
                damagePercentAdjustment: DeviceAbilityEffects.GetAssaultGadgetDamageAdjustment(activator),
                baseDamageAdjustment: DeviceAbilityEffects.GetAssaultGadgetBaseDamageAdjustment(activator),
                afterSuccessfulHit: _ => DeviceAbilityEffects.ApplyTacticalUplink(activator),
                hitChancePercentAdjustment: DeviceAbilityEffects.GetAssaultGadgetAccuracyAdjustment(activator),
                criticalRatePercentAdjustment: DeviceAbilityEffects.GetAssaultGadgetCriticalRateAdjustment(activator));
        }

    }
}
