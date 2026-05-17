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
    public sealed class DampeningFieldAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            DampeningField1(builder);
            DampeningField2(builder);

            return builder.Build();
        }

        private static void DampeningField1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.DampeningField1, PerkType.DampeningField)
                .Name("Dampening Field I")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.DampeningField, 60f)
                .SkillType(SkillType.Devices)
                .HasMaxRange(DeviceAbilityRange.Standard)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(DampeningField1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(5);
        }

        private static void DampeningField2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.DampeningField2, PerkType.DampeningField)
                .Name("Dampening Field II")
                .Level(2)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.DampeningField, 60f)
                .SkillType(SkillType.Devices)
                .HasMaxRange(DeviceAbilityRange.Standard)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(DampeningField2ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(7);
        }

        private static void DampeningField1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(DampeningField1StatusEffect), 10f);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Holy_Aid), friendly);
            }
        }

        private static void DampeningField2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(DampeningField2StatusEffect), 10f);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Holy_Aid), friendly);
            }
        }


    }
}
