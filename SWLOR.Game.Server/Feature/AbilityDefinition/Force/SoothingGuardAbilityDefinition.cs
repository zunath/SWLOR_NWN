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
    public sealed class SoothingGuardAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            SoothingGuard1(builder);

            return builder.Build();
        }

        private static void SoothingGuard1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.SoothingGuard1, PerkType.SoothingGuard)
                .Name("Soothing Guard I")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.SoothingGuard, 36f)
                .SkillType(SkillType.Force)
                .IsSingleTargetAbility()
                .HasMaxRange(15f)
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(SoothingGuard1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementFP(4);
        }

        private static void SoothingGuard1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            foreach (var friendly in AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                StatusEffect.RemoveFirstStatusEffect(
                    friendly,
                    new[] { typeof(PoisonStatusEffect), typeof(BleedStatusEffect), typeof(BurnStatusEffect), typeof(ShockStatusEffect), typeof(DiseaseStatusEffect) },
                    false);
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(SoothingGuard1StatusEffect), 8f);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Remove_Condition), friendly);
            }
            LightGuardianPowerSupport.ApplyDeflectivePresence(activator);
        }


    }
}
