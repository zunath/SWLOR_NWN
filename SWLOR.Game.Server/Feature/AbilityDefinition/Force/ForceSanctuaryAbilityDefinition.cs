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

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public sealed class ForceSanctuaryAbilityDefinition : IAbilityListDefinition
    {
        private const float RadiusMeters = 4f;
        private const float DurationSeconds = 30f;
        private const VisualEffect AllyPulseVisualEffect = VisualEffect.Vfx_Imp_Holy_Aid;
        private const VisualEffect AreaMarkerVisualEffect = VisualEffect.Dur_Sanctuary;
        private const float AreaMarkerVisualEffectScale = 2f;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ForceSanctuary1(builder);

            return builder.Build();
        }

        private static void ForceSanctuary1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ForceSanctuary1, PerkType.ForceSanctuary)
                .Name("Force Sanctuary")
                .Level(1)
                .HasActivationDelay(1.5f)
                .UsesAnimation(Animation.LoopingConjure1)
                .PlaysSoundOnImpact("ksfx_frc_armor")
                .HasRecastDelay(RecastGroup.ForceSanctuary, 90f)
                .SkillType(SkillType.Force)
                .IsAreaAbility()
                .HasImpactAction(ForceSanctuary1ImpactAction)
                .HasTargetingSphere(
                    Spell.ForceSanctuary1,
                    RadiusMeters,
                    AbilityTargetingFlags.HelpsAllies)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementFP(6);
        }

        private static void ForceSanctuary1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var location = AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation);

            AbilityAreaEffects.ScheduleFriendlyZoneStatus(
                activator,
                location,
                RadiusMeters,
                DurationSeconds,
                typeof(ForceSanctuary1StatusEffect),
                AllyPulseVisualEffect,
                areaMarkerVisualEffect: AreaMarkerVisualEffect,
                areaMarkerVisualEffectScale: AreaMarkerVisualEffectScale);

            AbilityAreaEffects.ScheduleFriendlyZoneHealing(
                activator,
                location,
                RadiusMeters,
                DurationSeconds,
                2f,
                null,
                VisualEffect.None,
                onHealed: (friendly, targetWasBelowHalfHP) =>
                    ForceControlHealingEffects.ApplyRestorativeControlPower(
                        activator,
                        friendly,
                        targetWasBelowHalfHP));
        }
    }
}
