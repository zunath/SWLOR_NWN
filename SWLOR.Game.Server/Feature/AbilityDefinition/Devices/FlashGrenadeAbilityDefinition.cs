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
    public sealed class FlashGrenadeAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            FlashGrenade1(builder);

            return builder.Build();
        }

        private static void FlashGrenade1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.FlashGrenade1, PerkType.FlashGrenade)
                .Name("Flash Grenade")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.FlashGrenade, 15f)
                .SkillType(SkillType.Devices)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .UsesImpactAnimation(Animation.ThrowGrenade)
                .IsAreaAbility()
                .HasTargetingSphere(
                    Spell.FlashGrenade1,
                    4f,
                    AbilityTargetingFlags.HarmsEnemies,
                    DeviceAbilityEffects.ApplyBlastRadiusBonus)
                .HasImpactAction(FlashGrenade1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(2)
                .RequirementItem("explosives");
        }

        private static void FlashGrenade1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var impactLocation = AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation);
            ApplyEffectAtLocation(
                DurationType.Instant,
                EffectVisualEffect(VisualEffect.Vfx_Fnf_Sound_Burst),
                impactLocation);

            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                impactLocation,
                SkillType.Devices,
                0,
                30,
                typeof(FlashStatusEffect),
                CombatImpactAreaShape.Sphere,
                0f,
                DeviceAbilityEffects.ApplyBlastRadiusBonus(activator, 4f),
                0f,
                Array.Empty<Type>(),
                statusEffectFactory: () => new FlashGrenade1StatusEffect(GetFlashPenalty(activator, 8)),
                damageType: CombatDamageType.Force,
                targetVisualEffect: VisualEffect.Vfx_Imp_Sonic,
                areaVisualEffect: VisualEffect.None,
                maxTargets: 5);
        }

        private static int GetFlashPenalty(uint activator, int basePenalty)
        {
            return DeviceAbilityEffects.ApplyGrenadeControlPotencyBonus(activator, basePenalty);
        }
    }
}
