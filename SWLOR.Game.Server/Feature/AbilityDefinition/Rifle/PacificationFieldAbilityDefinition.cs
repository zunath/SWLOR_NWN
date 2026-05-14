using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Rifle
{
    public class PacificationFieldAbilityDefinition : IAbilityListDefinition
    {
        private const float FieldDurationSeconds = 15f;
        private const float PulseIntervalSeconds = 5f;
        private const float FieldRadius = 5f;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            PacificationField1(builder);

            return builder.Build();
        }

        private static void PacificationField1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.PacificationField1, PerkType.PacificationField)
                .Name("Pacification Field")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.PacificationField, 180f)
                .IsAreaAbility()
                .HasImpactAction(PacificationField1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(14);
        }

        private static void PacificationField1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var location = AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation);
            ApplyEffectAtLocation(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Howl_Mind), location);

            CombatAreaPulses.SchedulePulses(
                activator,
                location,
                FieldDurationSeconds,
                PulseIntervalSeconds,
                false,
                pulseLocation =>
                {
                    ApplyEffectAtLocation(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Howl_Mind), pulseLocation);

                    foreach (var hostile in CombatAreaPulses.GetHostileCreatures(activator, pulseLocation, FieldRadius))
                    {
                        StatusEffect.ApplyStatusEffect(activator, hostile, typeof(PacificationFieldStatusEffect), PulseIntervalSeconds + 0.2f);
                        StatusEffect.ApplyStatusEffect(activator, hostile, typeof(DazedStatusEffect), 2f);
                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Dazed_S), hostile);
                    }
                });
        }
    }
}
