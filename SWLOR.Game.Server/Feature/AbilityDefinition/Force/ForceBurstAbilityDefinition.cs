using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.TelegraphService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public sealed class ForceBurstAbilityDefinition : IAbilityListDefinition
    {
        private const float RadiusMeters = 5f;
        private const int BaseDamage = 44;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureForceBurst(
                builder,
                FeatType.ForceBurst1,
                Spell.ForceBurst1,
                "Force Burst",
                1,
                BaseDamage,
                6);

            return builder.Build();
        }

        private static void ConfigureForceBurst(
            AbilityBuilder builder,
            FeatType feat,
            Spell spell,
            string name,
            int level,
            int baseDamage,
            int fp)
        {
            builder
                .Create(feat, PerkType.ForceBurst)
                .DisplaysVisualEffectOnSuccessfulImpact(VisualEffect.None)
                .UsesAuthoredAnimationAtImpact()
                .Name(name)
                .Level(level)
                .HasActivationDelay(1.5f)
                .HasRecastDelay(RecastGroup.ForceBurst, 15f)
                .SkillType(SkillType.Force)
                .CombatImpactDamageAbility(AbilityType.Willpower)
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .DisplaysVisualEffectWhenActivating()
                .PlaysSoundOnImpact("plr_force_blast")
                .IsAreaAbility()
                .HasTargetingSphere(
                    spell,
                    RadiusMeters,
                    AbilityTargetingFlags.HarmsEnemies)
                .HasMaxRange(15f)
                .RequiresTarget()
                .HasImpactAction((activator, target, _, targetLocation) =>
                    ApplyForceBurst(activator, target, targetLocation, baseDamage))
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementFP(fp);
        }

        private static void ApplyForceBurst(
            uint activator,
            uint target,
            Location targetLocation,
            int baseDamage)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Force,
                baseDamage,
                0,
                null,
                CombatImpactAreaShape.Sphere,
                0f,
                RadiusMeters,
                0f,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Force,
                targetVisualEffect: VisualEffect.VFX_IMP_KIN_L,
                afterSuccessfulHit: creature =>
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Silence), creature),
                onGeometryResolved: geometry => PlayCentralVisuals(activator, geometry));
        }

        private static void PlayCentralVisuals(uint activator, TelegraphGeometry geometry)
        {
            // MIRV needs an object endpoint; keep it at the warned sphere center.
            var center = Location(geometry.Area, geometry.Position, 0f);
            var visualAnchor = CreateObject(ObjectType.Placeable, "plc_invisobj", center);
            if (!GetIsObjectValid(visualAnchor))
                return;

            SetPlotFlag(visualAnchor, true);
            SetUseableFlag(visualAnchor, false);
            DestroyObject(visualAnchor, 3f);

            AssignCommand(activator, () =>
            {
                if (GetIsObjectValid(visualAnchor))
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Mirv_Fireball), visualAnchor);
            });

            ApplyWindPulse(visualAnchor);
            DelayCommand(0.1f, () => ApplyWindPulse(visualAnchor));
            DelayCommand(0.2f, () => ApplyWindPulse(visualAnchor));
        }

        private static void ApplyWindPulse(uint target)
        {
            if (GetIsObjectValid(target))
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Pulse_Wind), target);
        }
    }
}
