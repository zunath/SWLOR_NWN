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

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public sealed class ReflectiveBarrierAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ReflectiveBarrier1(builder);

            return builder.Build();
        }

        private static void ReflectiveBarrier1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ReflectiveBarrier1, PerkType.ReflectiveBarrier)
                .Name("Reflective Barrier")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.ReflectiveBarrier, 75f)
                .SkillType(SkillType.Force)
                .IsSingleTargetAbility()
                .HasMaxRange(15f)
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(ReflectiveBarrier1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementFP(5);
        }

        private static void ReflectiveBarrier1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(ReflectiveBarrier1StatusEffect), 20f);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Ac_Bonus), friendly);
            }
            LightGuardianPowerSupport.ApplyDeflectivePresence(activator);
        }


    }
}
