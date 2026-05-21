using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
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

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Devices
{
    public sealed class EmergencyBunkerAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            EmergencyBunker1(builder);

            return builder.Build();
        }

        private static void EmergencyBunker1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.EmergencyBunker1, PerkType.EmergencyBunker)
                .Name("Emergency Bunker")
                .Level(1)
                .HasActivationDelay(2f)
                .HasRecastDelay(RecastGroup.Capstone, CapstoneAbility.RecastDelaySeconds)
                .SkillType(SkillType.Devices)
                .IsAreaAbility()
                .HasImpactAction(EmergencyBunker1ImpactAction)
                .HasTargetingSphere(
                    Spell.EmergencyBunker1,
                    4f,
                    AbilityTargetingFlags.HelpsAllies)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(CapstoneAbility.StaminaCost);
        }

        private static void EmergencyBunker1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var location = AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation);
            var duration = CapstoneAbility.ActiveDurationSeconds;

            AbilityAreaEffects.ScheduleFriendlyZoneStatus(
                activator,
                location,
                4f,
                duration,
                typeof(EmergencyBunker1StatusEffect),
                VisualEffect.Vfx_Imp_Ac_Bonus,
                (friendly, remainingDuration) => ApplyBunkerTemporaryHP(activator, friendly, remainingDuration));
        }

        private static void ApplyBunkerTemporaryHP(uint activator, uint target, float durationSeconds)
        {
            var temporaryHP = 60 + (int)Math.Ceiling(GetMaxHitPoints(target) * 0.08f);
            temporaryHP = DeviceAbilityEffects.ApplyCapacitorRigBonus(activator, temporaryHP);
            TemporaryHitPointEffects.ApplyFlat(target, temporaryHP, durationSeconds);
        }
    }
}
