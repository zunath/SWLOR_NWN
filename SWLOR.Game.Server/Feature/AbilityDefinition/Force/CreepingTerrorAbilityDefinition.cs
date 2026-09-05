using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public sealed class CreepingTerrorAbilityDefinition : IAbilityListDefinition
    {
        private const float StandardFieldRadius = 5f;
        private const float LargeFieldRadius = 8f;
        private const float FieldRange = 15f;
        private const float PulseIntervalSeconds = 3f;
        private const int HobbleRefreshDurationSeconds = 30;
        private const int CreepingTerror1Damage = 10;
        private const int CreepingTerror2Damage = 14;
        private const int CreepingTerror3Damage = 18;
        private const float CreepingTerror1DurationSeconds = 30f;
        private const float CreepingTerror2DurationSeconds = 30f;
        private const float CreepingTerror3DurationSeconds = 30f;
        private const AreaOfEffect StandardFieldAreaOfEffect = AreaOfEffect.CreepingTerrorTentacles;
        private const AreaOfEffect LargeFieldAreaOfEffect = AreaOfEffect.CreepingTerrorLargeTentacles;

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
                .HasRecastDelay(RecastGroup.CreepingTerror, 32f)
                .SkillType(SkillType.Force)
                .CombatImpactDamageAbility(AbilityType.Willpower)
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .PlaysSoundOnImpact("ksfx_frc_fear")
                .IsAreaAbility()
                .HasMaxRange(FieldRange)
                .HasCustomValidation(ValidateTargetingRange)
                .HasImpactAction(CreepingTerror1ImpactAction)
                .HasTargetingSphere(
                    Spell.CreepingTerror1,
                    StandardFieldRadius,
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
                .HasRecastDelay(RecastGroup.CreepingTerror, 32f)
                .SkillType(SkillType.Force)
                .CombatImpactDamageAbility(AbilityType.Willpower)
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .PlaysSoundOnImpact("ksfx_frc_fear")
                .IsAreaAbility()
                .HasMaxRange(FieldRange)
                .HasCustomValidation(ValidateTargetingRange)
                .HasImpactAction(CreepingTerror2ImpactAction)
                .HasTargetingSphere(
                    Spell.CreepingTerror2,
                    StandardFieldRadius,
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
                .HasRecastDelay(RecastGroup.CreepingTerror, 32f)
                .SkillType(SkillType.Force)
                .CombatImpactDamageAbility(AbilityType.Willpower)
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .PlaysSoundOnImpact("ksfx_frc_fear")
                .IsAreaAbility()
                .HasMaxRange(FieldRange)
                .HasCustomValidation(ValidateTargetingRange)
                .HasImpactAction(CreepingTerror3ImpactAction)
                .HasTargetingSphere(
                    Spell.CreepingTerror3,
                    LargeFieldRadius,
                    AbilityTargetingFlags.HarmsEnemies)
                .IsCastedAbility()
                .IsHostileAbility()
                .TriggersDarkForceConversion()
                .BreaksStealth()
                .RequirementFP(8);
        }

        private static void CreepingTerror1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            CreateCreepingTerrorField(
                activator,
                target,
                targetLocation,
                FeatType.CreepingTerror1,
                CreepingTerror1Damage,
                CreepingTerror1DurationSeconds,
                StandardFieldRadius,
                StandardFieldAreaOfEffect);
        }

        private static void CreepingTerror2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            CreateCreepingTerrorField(
                activator,
                target,
                targetLocation,
                FeatType.CreepingTerror2,
                CreepingTerror2Damage,
                CreepingTerror2DurationSeconds,
                StandardFieldRadius,
                StandardFieldAreaOfEffect);
        }

        private static void CreepingTerror3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            CreateCreepingTerrorField(
                activator,
                target,
                targetLocation,
                FeatType.CreepingTerror3,
                CreepingTerror3Damage,
                CreepingTerror3DurationSeconds,
                LargeFieldRadius,
                LargeFieldAreaOfEffect);
        }

        private static string ValidateTargetingRange(uint activator, uint target, int effectivePerkLevel, Location targetLocation)
        {
            var location = AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation);
            if (GetDistanceBetweenLocations(GetLocation(activator), location) <= FieldRange)
                return string.Empty;

            return $"You are out of range. This ability has a range of {FieldRange} meters.";
        }

        private static void CreateCreepingTerrorField(
            uint activator,
            uint target,
            Location targetLocation,
            FeatType featType,
            int baseDamage,
            float durationSeconds,
            float radius,
            AreaOfEffect areaOfEffect)
        {
            var location = AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation);
            var scaledPulseDamage = AbilityEffectScaling.ScaleDirectEffect(
                baseDamage,
                GetAbilityScore(activator, AbilityType.Willpower),
                source: activator);
            AbilityAreaEffects.CreatePersistentSphereIndicator(
                activator,
                location,
                radius,
                durationSeconds,
                true);
            ApplyEffectAtLocation(DurationType.Temporary, EffectAreaOfEffect(areaOfEffect), location, durationSeconds);

            var sequence = Ability.GetAbilityImpactSequence(activator) ?? new AbilityImpactSequence();

            CombatAreaPulses.SchedulePulses(
                activator,
                location,
                durationSeconds,
                PulseIntervalSeconds,
                false,
                pulseLocation =>
                {
                    var ability = Ability.GetAbilityDetail(featType);
                    Ability.BeginAbilityImpact(activator, ability, countsAsAttackAttempt: false, sequence: sequence);
                    ApplyCreepingTerrorPulse(activator, pulseLocation, scaledPulseDamage, radius);
                    var summary = Ability.EndAbilityImpact(activator);
                    Combat.ApplyAbilityImpactEffects(activator, summary);
                });
        }

        private static void ApplyCreepingTerrorPulse(uint activator, Location location, int scaledPulseDamage, float radius)
        {
            foreach (var hostile in CombatAreaPulses.GetHostileCreatures(activator, location, radius))
            {
                ApplyCreepingTerrorDamage(activator, hostile, scaledPulseDamage);
            }
        }

        private static void ApplyCreepingTerrorDamage(uint activator, uint target, int scaledPulseDamage)
        {
            var damage = scaledPulseDamage;
            damage = Resistance.ApplyResistanceToDamage(target, ResistanceType.Disruption, damage);
            damage = Combat.ApplyDamageOverTimeTakenModifiers(target, damage, CombatDamageType.Force);
            damage = Combat.ApplyDamageTakenModifiers(target, damage, activator, CombatDamageType.Force);
            if (damage < 0)
                damage = 0;

            Ability.ApplyHostileCombatImpact(
                activator,
                target,
                SkillType.Force,
                damage,
                CombatDamageType.Force,
                statusEffect: typeof(HobbleStatusEffect),
                duration: HobbleRefreshDurationSeconds,
                awardsCombatPoints: false);
        }
    }
}
