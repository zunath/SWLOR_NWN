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
    public sealed class EclipseOfResolveAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            EclipseOfResolve1(builder);

            return builder.Build();
        }

        private static void EclipseOfResolve1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.EclipseOfResolve1, PerkType.EclipseOfResolve)
                .Name("Eclipse of Resolve")
                .Level(1)
                .HasActivationDelay(1.5f)
                .HasRecastDelay(RecastGroup.Capstone, CapstoneAbility.RecastDelaySeconds)
                .SkillType(SkillType.Force)
                .CombatImpactDamageAbility(AbilityType.Willpower)
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .PlaysSoundOnImpact("ksfx_frc_night")
                .IsAreaAbility()
                .HasImpactAction(EclipseOfResolve1ImpactAction)
                .HasTargetingSphere(
                    Spell.EclipseOfResolve1,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementFP(CapstoneAbility.ForceCost);
        }

        private static void EclipseOfResolve1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            LightGuardianPowerSupport.ApplyCourageousResolve(activator);

            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Force,
                0,
                45,
                typeof(EclipseOfResolve1StatusEffect),
                CombatImpactAreaShape.Sphere,
                0f,
                5f,
                0f,
                Array.Empty<Type>(),
                centerOnActivator: true,
                damageType: CombatDamageType.Force,
                afterSuccessfulHit: ApplyResolveDebuffVisual);
        }

        private static void ApplyResolveDebuffVisual(uint target)
        {
            ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Pulse_Red_Black), target, 1.0f);
        }

    }
}
