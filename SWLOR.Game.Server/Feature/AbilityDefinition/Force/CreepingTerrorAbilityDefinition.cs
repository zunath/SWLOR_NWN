using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public sealed class CreepingTerrorAbilityDefinition : IAbilityListDefinition
    {
        private const float FieldRadius = 5f;
        private const float FieldRange = 15f;
        private const float PulseIntervalSeconds = 3f;
        private const float HobbleRefreshDurationSeconds = PulseIntervalSeconds + 0.2f;
        private const int CreepingTerror1Damage = 10;
        private const int CreepingTerror2Damage = 14;
        private const int CreepingTerror3Damage = 18;
        private const float CreepingTerror1DurationSeconds = 12f;
        private const float CreepingTerror2DurationSeconds = 15f;
        private const float CreepingTerror3DurationSeconds = 18f;
        private const VisualEffect FieldVisualEffect = VisualEffect.Vfx_Dur_Tentacle;
        private const VisualEffect PulseAreaVisualEffect = VisualEffect.Vfx_Fnf_Howl_Mind;
        private const VisualEffect TargetVisualEffect = VisualEffect.Vfx_Imp_Pulse_Negative;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            CreepingTerror1(builder);
            CreepingTerror2(builder);
            CreepingTerror3(builder);

            return builder.Build();
        }

        private static void CreepingTerror1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.CreepingTerror1, PerkType.CreepingTerror)
                .Name("Creeping Terror I")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.CreepingTerror, 30f)
                .SkillType(SkillType.Force)
                .CombatImpactDamageAbility(AbilityType.Willpower)
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .IsAreaAbility()
                .HasMaxRange(FieldRange)
                .RequiresTarget()
                .HasImpactAction(CreepingTerror1ImpactAction)
                .HasTargetingSphere(
                    Spell.CreepingTerror1,
                    FieldRadius,
                    AbilityTargetingFlags.HarmsEnemies)
                .IsCastedAbility()
                .IsHostileAbility()
                .TriggersDarkForceConversion()
                .BreaksStealth()
                .RequirementFP(4);
        }

        private static void CreepingTerror2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.CreepingTerror2, PerkType.CreepingTerror)
                .Name("Creeping Terror II")
                .Level(2)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.CreepingTerror, 30f)
                .SkillType(SkillType.Force)
                .CombatImpactDamageAbility(AbilityType.Willpower)
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .IsAreaAbility()
                .HasMaxRange(FieldRange)
                .RequiresTarget()
                .HasImpactAction(CreepingTerror2ImpactAction)
                .HasTargetingSphere(
                    Spell.CreepingTerror2,
                    FieldRadius,
                    AbilityTargetingFlags.HarmsEnemies)
                .IsCastedAbility()
                .IsHostileAbility()
                .TriggersDarkForceConversion()
                .BreaksStealth()
                .RequirementFP(6);
        }

        private static void CreepingTerror3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.CreepingTerror3, PerkType.CreepingTerror)
                .Name("Creeping Terror III")
                .Level(3)
                .HasActivationDelay(1.5f)
                .HasRecastDelay(RecastGroup.CreepingTerror, 30f)
                .SkillType(SkillType.Force)
                .CombatImpactDamageAbility(AbilityType.Willpower)
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .IsAreaAbility()
                .HasMaxRange(FieldRange)
                .RequiresTarget()
                .HasImpactAction(CreepingTerror3ImpactAction)
                .HasTargetingSphere(
                    Spell.CreepingTerror3,
                    FieldRadius,
                    AbilityTargetingFlags.HarmsEnemies)
                .IsCastedAbility()
                .IsHostileAbility()
                .TriggersDarkForceConversion()
                .BreaksStealth()
                .RequirementFP(8);
        }

        private static void CreepingTerror1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            CreateCreepingTerrorField(activator, target, targetLocation, CreepingTerror1Damage, CreepingTerror1DurationSeconds);
        }

        private static void CreepingTerror2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            CreateCreepingTerrorField(activator, target, targetLocation, CreepingTerror2Damage, CreepingTerror2DurationSeconds);
        }

        private static void CreepingTerror3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            CreateCreepingTerrorField(activator, target, targetLocation, CreepingTerror3Damage, CreepingTerror3DurationSeconds);
        }

        private static void CreateCreepingTerrorField(
            uint activator,
            uint target,
            Location targetLocation,
            int baseDamage,
            float durationSeconds)
        {
            var location = AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation);
            ApplyEffectAtLocation(DurationType.Temporary, EffectVisualEffect(FieldVisualEffect), location, durationSeconds);

            CombatAreaPulses.SchedulePulses(
                activator,
                location,
                durationSeconds,
                PulseIntervalSeconds,
                false,
                pulseLocation => ApplyCreepingTerrorPulse(activator, pulseLocation, baseDamage));
        }

        private static void ApplyCreepingTerrorPulse(uint activator, Location location, int baseDamage)
        {
            ApplyEffectAtLocation(DurationType.Instant, EffectVisualEffect(PulseAreaVisualEffect), location);

            foreach (var hostile in CombatAreaPulses.GetHostileCreatures(activator, location, FieldRadius))
            {
                StatusEffect.ApplyStatusEffect(activator, hostile, typeof(HobbleStatusEffect), HobbleRefreshDurationSeconds, CombatDamageType.Force);
                ApplyCreepingTerrorDamage(activator, hostile, baseDamage);
            }
        }

        private static void ApplyCreepingTerrorDamage(uint activator, uint target, int baseDamage)
        {
            var damage = AbilityEffectScaling.ScaleDirectEffect(
                baseDamage,
                GetAbilityScore(activator, AbilityType.Willpower),
                source: activator);
            damage = Resistance.ApplyResistanceToDamage(target, ResistanceType.Disruption, damage);
            damage = Combat.ApplyDamageOverTimeTakenModifiers(target, damage, CombatDamageType.Force);
            damage = Combat.ApplyDamageTakenModifiers(target, damage);
            if (damage <= 0)
                return;

            Combat.SendTemporaryHitPointDamageFeedback(activator, target, damage);
            AssignCommand(
                activator,
                () => ApplyEffectToObject(
                    DurationType.Instant,
                    EffectDamage(damage, CombatDamageType.Force.GetNWScriptDamageType()),
                    target));
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(TargetVisualEffect), target);
            Ability.ApplyDarkForceDamageRestoration(activator, damage);
            Combat.ApplyDamageDealtEffects(activator, target, damage, SkillType.Force, CombatDamageType.Force);
            StatusEffect.NotifyDamageStatusEffects(activator, target, damage, CombatDamageType.Force);
            Ability.ApplyHostileAbilityEnmity(activator, target, damage);
        }
    }
}
