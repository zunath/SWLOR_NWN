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
        private const float Rank1HealPercentPerTick = 1f;
        private const float Rank2HealPercentPerTick = 2f;
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
                .Name("Kolto Mist I")
                .Level(1)
                .HasActivationDelay(1.5f)
                .UsesAnimation(Animation.ThrowGrenade)
                .PlaysSoundOnImpact("ksfx_healing")
                .HasRecastDelay(RecastGroup.KoltoMist, 18f)
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
                .Name("Kolto Mist II")
                .Level(2)
                .HasActivationDelay(1.5f)
                .UsesAnimation(Animation.ThrowGrenade)
                .PlaysSoundOnImpact("ksfx_healing")
                .HasRecastDelay(RecastGroup.KoltoMist, 18f)
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
            ApplyKoltoMist(activator, target, targetLocation, Rank1HealPercentPerTick);
        }

        private static void KoltoMist2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyKoltoMist(activator, target, targetLocation, Rank2HealPercentPerTick);
        }

        private static string ValidateTargetingRange(uint activator, uint target, int effectivePerkLevel, Location targetLocation)
        {
            var location = AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation);
            if (GetDistanceBetweenLocations(GetLocation(activator), location) <= RangeMeters)
                return string.Empty;

            return $"You are out of range. This ability has a range of {RangeMeters} meters.";
        }

        private static void ApplyKoltoMist(uint activator, uint target, Location targetLocation, float percentPerTick)
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

            var combatPointAwarded = false;
            for (var elapsed = TickIntervalSeconds; elapsed <= DurationSeconds + 0.01f; elapsed += TickIntervalSeconds)
            {
                var pulseDelay = elapsed;
                DelayCommand(pulseDelay, () =>
                {
                    if (!GetIsObjectValid(activator) || GetCurrentHitPoints(activator) <= 0)
                        return;

                    if (!GetIsObjectValid(GetAreaFromLocation(location)))
                        return;

                    var applied = ApplyKoltoMistPulse(activator, location, percentPerTick);
                    if (applied && !combatPointAwarded)
                    {
                        combatPointAwarded = true;
                        FirstAidTreatmentAdjustments.GrantCombatPoint(activator);
                    }
                });
            }
        }

        private static bool ApplyKoltoMistPulse(uint activator, Location location, float percentPerTick)
        {
            var applied = false;
            foreach (var friendly in AbilityTargeting.GetFriendlyTargetsNearLocation(activator, location, HealRadiusMeters))
            {
                FirstAidTreatmentAdjustments.ApplyMedicalScaledHeal(
                    activator,
                    friendly,
                    percentPerTick,
                    visualEffect: VisualEffect.Vfx_Imp_Head_Heal);
                FirstAidTreatmentAdjustments.ApplyTraumaMedicRiders(activator, friendly);
                StatusEffect.ApplyStatusEffect(
                    activator,
                    friendly,
                    typeof(KoltoMistHealingStatusEffect),
                    StatusRefreshDurationSeconds);
                applied = true;
            }

            return applied;
        }
    }
}
