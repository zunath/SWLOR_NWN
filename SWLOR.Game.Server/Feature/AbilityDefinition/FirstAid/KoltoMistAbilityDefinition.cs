using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.FirstAid
{
    public sealed class KoltoMistAbilityDefinition : IAbilityListDefinition
    {
        private const float HealRadiusMeters = 8f;
        private const float RangeMeters = 15f;
        private const float DurationSeconds = 30f;
        private const float TickIntervalSeconds = 3f;
        private const float StatusRefreshDurationSeconds = TickIntervalSeconds + 0.2f;
        private static readonly HealAmount Rank1HealPerTick = new(2, 0.25f);
        private static readonly HealAmount Rank2HealPerTick = new(9, 0.5f);
        private const VisualEffect CloudBurstVisualEffect = VisualEffect.Vfx_Fnf_Gas_Explosion_Mind;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            KoltoMist1(builder);
            KoltoMist2(builder);

            return builder.Build();
        }

        private static void KoltoMist1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.KoltoMist1, PerkType.KoltoMist)
                .DisplaysVisualEffectOnSuccessfulImpact(VisualEffect.Vfx_Ability_KoltoMist)
                .UsesImmediateAuthoredAnimation()
                .Name("Kolto Mist I")
                .Level(1)
                .HasActivationDelay(1.5f)
                .UsesAnimation(Animation.ThrowGrenade)
                .PlaysSoundOnImpact("ksfx_healing")
                .HasRecastDelay(RecastGroup.KoltoMist, DurationSeconds)
                .SkillType(SkillType.FirstAid)
                .IsAreaAbility()
                .HasMaxRange(RangeMeters)
                .HasCustomValidation(ValidateTargetingRange)
                .HasImpactAction(KoltoMist1ImpactAction)
                .HasTargetingSphere(
                    Spell.KoltoMist1,
                    HealRadiusMeters,
                    AbilityTargetingFlags.HelpsAllies)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(6)
                .RequirementItem("med_supplies");
        }

        private static void KoltoMist2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.KoltoMist2, PerkType.KoltoMist)
                .DisplaysVisualEffectOnSuccessfulImpact(VisualEffect.Vfx_Ability_KoltoMist)
                .UsesImmediateAuthoredAnimation()
                .Name("Kolto Mist II")
                .Level(2)
                .HasActivationDelay(1.5f)
                .UsesAnimation(Animation.ThrowGrenade)
                .PlaysSoundOnImpact("ksfx_healing")
                .HasRecastDelay(RecastGroup.KoltoMist, DurationSeconds)
                .SkillType(SkillType.FirstAid)
                .IsAreaAbility()
                .HasMaxRange(RangeMeters)
                .HasCustomValidation(ValidateTargetingRange)
                .HasImpactAction(KoltoMist2ImpactAction)
                .HasTargetingSphere(
                    Spell.KoltoMist2,
                    HealRadiusMeters,
                    AbilityTargetingFlags.HelpsAllies)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(7)
                .RequirementItem("med_supplies");
        }

        private static void KoltoMist1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyKoltoMist(activator, target, targetLocation, Rank1HealPerTick, FeatType.KoltoMist1);
        }

        private static void KoltoMist2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyKoltoMist(activator, target, targetLocation, Rank2HealPerTick, FeatType.KoltoMist2);
        }

        private static string ValidateTargetingRange(uint activator, uint target, int effectivePerkLevel, Location targetLocation)
        {
            var location = AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation);
            if (GetDistanceBetweenLocations(GetLocation(activator), location) <= RangeMeters)
                return string.Empty;

            return $"You are out of range. This ability has a range of {RangeMeters} meters.";
        }

        private static void ApplyKoltoMist(uint activator, uint target, Location targetLocation, HealAmount healPerTick, FeatType feat)
        {
            var location = AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation);

            AbilityAreaEffects.CreatePersistentSphereIndicator(
                activator,
                location,
                HealRadiusMeters,
                DurationSeconds,
                false);
            ApplyEffectAtLocation(DurationType.Instant, EffectVisualEffect(CloudBurstVisualEffect), location);
            // Script-free custom AoE row: renders the FogMind gas cloud visual only, without the
            // base game Mind Fog enter/heartbeat spell effects.
            ApplyEffectAtLocation(
                DurationType.Temporary,
                EffectAreaOfEffect(AreaOfEffect.KoltoMistCloud),
                location,
                DurationSeconds);

            var credited = new HashSet<uint>();
            for (var elapsed = TickIntervalSeconds; elapsed <= DurationSeconds + 0.01f; elapsed += TickIntervalSeconds)
            {
                var pulseDelay = elapsed;
                var playPulseVisual = Ability.CaptureSuccessfulImpactVisualEffect(activator);
                DelayCommand(pulseDelay, () =>
                {
                    if (!GetIsObjectValid(activator) || GetCurrentHitPoints(activator) <= 0)
                        return;

                    using var recorderScope = CombatRecorder.BeginAbilityFeatAttribution(activator, feat);
                    if (!GetIsObjectValid(GetAreaFromLocation(location)))
                        return;

                    var treated = ApplyKoltoMistPulse(activator, location, healPerTick, playPulseVisual);
                    FirstAidTreatmentAdjustments.GrantCombatPoint(activator, treated, credited);
                });
            }
        }

        private static List<uint> ApplyKoltoMistPulse(uint activator, Location location, HealAmount healPerTick, Action<uint> playPulseVisual)
        {
            var treated = new List<uint>();
            foreach (var friendly in AbilityTargeting.GetFriendlyTargetsNearLocation(activator, location, HealRadiusMeters))
            {
                var hitPointsBeforeHealing = GetCurrentHitPoints(friendly);
                FirstAidTreatmentAdjustments.ApplyMedicalScaledHeal(
                    activator,
                    friendly,
                    healPerTick,
                    visualEffect: VisualEffect.Vfx_Imp_Head_Heal);
                if (GetCurrentHitPoints(friendly) > hitPointsBeforeHealing)
                    playPulseVisual(friendly);
                FirstAidTreatmentAdjustments.ApplyTraumaMedicRiders(activator, friendly);
                if (StatusEffect.ApplyStatusEffect(
                    activator,
                    friendly,
                    typeof(KoltoMistHealingStatusEffect),
                    StatusRefreshDurationSeconds))
                    playPulseVisual(friendly);
                treated.Add(friendly);
            }

            return treated;
        }
    }
}
