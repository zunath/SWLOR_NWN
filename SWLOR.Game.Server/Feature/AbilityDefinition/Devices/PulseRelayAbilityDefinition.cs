using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Devices
{
    public sealed class PulseRelayAbilityDefinition : IAbilityListDefinition
    {
        private const float AllyRadius = 10f;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            PulseRelay1(builder);
            PulseRelay2(builder);

            return builder.Build();
        }

        private static void PulseRelay1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.PulseRelay1, PerkType.PulseRelay)
                .Name("Pulse Relay I")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.PulseRelay, 18f)
                .SkillType(SkillType.Devices)
                .IsAreaAbility()
                .HasImpactAction(PulseRelay1ImpactAction)
                .HasTargetingSphere(
                    Spell.PulseRelay1,
                    10f,
                    AbilityTargetingFlags.HelpsAllies | AbilityTargetingFlags.OriginOnSelf)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(3);
        }

        private static void PulseRelay2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.PulseRelay2, PerkType.PulseRelay)
                .Name("Pulse Relay II")
                .Level(2)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.PulseRelay, 18f)
                .SkillType(SkillType.Devices)
                .IsAreaAbility()
                .HasImpactAction(PulseRelay2ImpactAction)
                .HasTargetingSphere(
                    Spell.PulseRelay2,
                    10f,
                    AbilityTargetingFlags.HelpsAllies | AbilityTargetingFlags.OriginOnSelf)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(4);
        }

        private static void PulseRelay1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyPulseRelay(activator, typeof(CalibratedField1StatusEffect), 8f, false, 0f);
        }

        private static void PulseRelay2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyPulseRelay(activator, typeof(CalibratedField2StatusEffect), 10f, true, 3f);
        }

        private static void ApplyPulseRelay(
            uint activator,
            Type statusEffect,
            float durationSeconds,
            bool removesShock,
            float emitterExtensionSeconds)
        {
            foreach (var friendly in AbilityTargeting.GetFriendlyTargets(activator, activator, true, AllyRadius))
            {
                if (removesShock)
                {
                    StatusEffect.RemoveStatusEffect(friendly, typeof(ShockStatusEffect), false);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Remove_Condition), friendly);
                }

                StatusEffect.ApplyStatusEffect(activator, friendly, statusEffect, durationSeconds);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Ac_Bonus), friendly);
            }

            DeviceAbilityEffects.TriggerActiveFieldEngineerPulses(activator);
            DeviceAbilityEffects.ExtendActiveFieldEngineerPulses(activator, emitterExtensionSeconds);
        }
    }
}
