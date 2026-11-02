using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.Force;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Espionage
{
    /// <summary>
    /// Force Sensitive counterpart to Shadow Step. Uses Mind Trick's range, cast time, cooldown,
    /// FP cost and Willpower contest, but blinds an organic enemy to everyone in stealth instead of
    /// confusing it. It does not break the caster's stealth, rolls no hit and creates no enmity, so
    /// it can be used on a guard that has not noticed anyone.
    /// </summary>
    public class CloudMindAbilityDefinition : IAbilityListDefinition
    {
        public const float Range = 15f;
        public const float SecondTargetRadius = 5f;
        private const int CloudMind2MaxTargets = 2;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            CloudMind(builder, FeatType.CloudMind1, "Cloud Mind I", 1, 4, 1);
            CloudMind(builder, FeatType.CloudMind2, "Cloud Mind II", 2, 6, CloudMind2MaxTargets);

            return builder.Build();
        }

        private static void CloudMind(
            AbilityBuilder builder,
            FeatType feat,
            string name,
            int level,
            int fp,
            int maxTargets)
        {
            builder
                .Create(feat, PerkType.CloudMind)
                .DisplaysVisualEffectOnSuccessfulImpact(VisualEffect.Vfx_Ability_MindTrick)
                .UsesAuthoredAnimationAtImpact()
                .Name(name)
                .Level(level)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.CloudMind, 45f)
                .SkillType(SkillType.Espionage)
                .IsForcePower()
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .PlaysSoundOnImpact("ksfx_frc_mind")
                .IsSingleTargetAbility()
                .HasMaxRange(Range)
                .RequiresTarget()
                .HasCustomValidation((_, target, _, _) => MindTrickAbilityDefinition.ValidateNonMechanicalTarget(target))
                .HasImpactAction((activator, target, _, targetLocation) =>
                    ApplyCloudMind(activator, target, targetLocation, maxTargets))
                .IsCastedAbility()
                .IsHostileAbility()
                .PreservesStealthDuringActivation()
                .RequirementFP(fp);
        }

        private static void ApplyCloudMind(uint activator, uint target, Location targetLocation, int maxTargets)
        {
            Ability.PlayAbilityImpactAnimation(activator);

            var targets = maxTargets <= 1
                ? new[] { target }
                : AbilityTargeting.GetHostileTargetsNearLocation(
                    activator,
                    AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation),
                    SecondTargetRadius,
                    maxTargets,
                    target,
                    MindTrickAbilityDefinition.IsNonMechanical);

            var clouded = false;
            foreach (var creature in targets)
            {
                clouded |= TryCloud(activator, creature);
            }

            if (clouded)
                Ability.PlaySuccessfulImpactVisualEffect(activator, activator);
        }

        private static bool TryCloud(uint activator, uint target)
        {
            if (!GetIsObjectValid(target) || !MindTrickAbilityDefinition.IsNonMechanical(target))
                return false;

            var duration = MindTrickAbilityDefinition.CalculateMindTrickDuration(activator, target);
            if (duration <= 0)
            {
                SendMessageToPC(activator, "Your cloud mind was resisted.");
                return false;
            }

            // Mind Resistance can shorten or negate the cloud; the status service reports that.
            if (!StatusEffect.ApplyStatusEffect<CloudedMindStatusEffect>(activator, target, duration))
                return false;

            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Confusion_S), target);
            return true;
        }
    }
}
