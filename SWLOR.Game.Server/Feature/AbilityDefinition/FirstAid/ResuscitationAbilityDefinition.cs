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

namespace SWLOR.Game.Server.Feature.AbilityDefinition.FirstAid
{
    public sealed class ResuscitationAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            Resuscitation1(builder);
            Resuscitation2(builder);

            return builder.Build();
        }

        private static void Resuscitation1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Resuscitation1, PerkType.Resuscitation)
                .Name("Resuscitation I")
                .Level(1)
                .HasActivationDelay(4f)
                .UsesAnimation(Animation.LoopingGetMid)
                .PlaysSoundOnImpact("ksfx_healing")
                .HasRecastDelay(RecastGroup.Resuscitation, 60f)
                .SkillType(SkillType.FirstAid)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target, requireDead: true))
                .HasImpactAction(Resuscitation1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(10)
                .RequirementItem("med_supplies");
        }

        private static void Resuscitation2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Resuscitation2, PerkType.Resuscitation)
                .Name("Resuscitation II")
                .Level(2)
                .HasActivationDelay(4f)
                .UsesAnimation(Animation.LoopingGetMid)
                .PlaysSoundOnImpact("ksfx_healing")
                .HasRecastDelay(RecastGroup.Resuscitation, 60f)
                .SkillType(SkillType.FirstAid)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target, requireDead: true))
                .HasImpactAction(Resuscitation2ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(10)
                .RequirementItem("med_supplies");
        }

        private static void Resuscitation1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            if (!GetIsObjectValid(target))
                return;

            ApplyEffectToObject(DurationType.Instant, EffectResurrection(), target);
            FirstAidTreatmentAdjustments.ApplyTraumaMedicRiders(activator, target);
            DelayCommand(0.1f, () => Ability.ReapplyAuraEffectsForCreature(target));
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Raise_Dead), target);
            FirstAidTreatmentAdjustments.GrantCombatPoint(activator);
        }

        private static void Resuscitation2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            if (!GetIsObjectValid(target))
                return;

            ApplyEffectToObject(DurationType.Instant, EffectResurrection(), target);
            FirstAidTreatmentAdjustments.ApplyTraumaMedicRiders(activator, target);
            // Resurrection is not settled until after the current engine command finishes.
            // Healing in the same tick is silently discarded because the target is still dead.
            DelayCommand(0.1f, () =>
            {
                AbilityEffectScaling.ApplyActivatedScaledHeal(activator, target, 20);
                Ability.ReapplyAuraEffectsForCreature(target);
            });
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Raise_Dead), target);
            FirstAidTreatmentAdjustments.GrantCombatPoint(activator);
        }
    }
}
