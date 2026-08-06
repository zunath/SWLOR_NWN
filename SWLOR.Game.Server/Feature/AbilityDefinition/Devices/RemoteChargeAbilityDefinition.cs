using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
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
    public sealed class RemoteChargeAbilityDefinition : IAbilityListDefinition
    {
        // Distinct "Detonator Pack" placeable so an armed charge cannot be mistaken for a beacon emitter.
        private const string RemoteChargeMarkerResref = "_mdrn_pl_detonat";

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            RemoteCharge1(builder);
            RemoteCharge2(builder);

            return builder.Build();
        }

        private static void RemoteCharge1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.RemoteCharge1, PerkType.RemoteCharge)
                .Name("Remote Charge I")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.RemoteCharge, 18f)
                .SkillType(SkillType.Devices)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .UsesImpactAnimation(Animation.ThrowGrenade)
                .IsAreaAbility()
                .HasImpactAction(RemoteCharge1ImpactAction)
                .HasTargetingSphere(
                    Spell.RemoteCharge1,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies,
                    DeviceAbilityEffects.ApplyBlastRadiusBonus)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(4);
        }

        private static void RemoteCharge2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.RemoteCharge2, PerkType.RemoteCharge)
                .Name("Remote Charge II")
                .Level(2)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.RemoteCharge, 18f)
                .SkillType(SkillType.Devices)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .UsesImpactAnimation(Animation.ThrowGrenade)
                .IsAreaAbility()
                .HasImpactAction(RemoteCharge2ImpactAction)
                .HasTargetingSphere(
                    Spell.RemoteCharge2,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies,
                    DeviceAbilityEffects.ApplyBlastRadiusBonus)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(5);
        }

        private static void RemoteCharge1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            DetonateRemoteCharge(activator, target, targetLocation, 30, null);
        }

        private static void RemoteCharge2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            DetonateRemoteCharge(activator, target, targetLocation, 42, typeof(KnockdownStatusEffect));
        }

        private static void DetonateRemoteCharge(uint activator, uint target, Location targetLocation, int baseDamage, Type statusEffect)
        {
            var impactLocation = AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation);
            var blastRadius = DeviceAbilityEffects.ApplyBlastRadiusBonus(activator, 5f);
            DeviceAbilityEffects.CreateTemporaryFieldEngineerMarker(
                impactLocation,
                VisualEffect.Vfx_Dur_Aura_Pulse_Red_Orange,
                2f,
                3f,
                RemoteChargeMarkerResref);

            Ability.ApplyTelegraphedCombatImpact(
                activator,
                OBJECT_INVALID,
                impactLocation,
                SkillType.Devices,
                baseDamage,
                statusEffect == null ? 3 : 6,
                statusEffect,
                CombatImpactAreaShape.Sphere,
                3f,
                blastRadius,
                0f,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Fire,
                targetVisualEffect: VisualEffect.Vfx_Com_Hit_Fire,
                areaVisualEffect: VisualEffect.Fnf_Fireball,
                afterImpactAction: _ => DeviceAbilityEffects.ApplyDiagnosticSweep(activator, impactLocation, blastRadius),
                alwaysApplyAreaVisualEffect: true);
        }
    }
}
