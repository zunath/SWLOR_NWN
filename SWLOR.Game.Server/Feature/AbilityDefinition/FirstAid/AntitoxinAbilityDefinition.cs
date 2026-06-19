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

namespace SWLOR.Game.Server.Feature.AbilityDefinition.FirstAid
{
    public sealed class AntitoxinAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            Antitoxin1(builder);

            return builder.Build();
        }

        private static void Antitoxin1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Antitoxin1, PerkType.Antitoxin)
                .Name("Antitoxin")
                .Level(1)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.FireForgetSalute)
                .PlaysSoundOnImpact("ksfx_frc_buff")
                .HasRecastDelay(RecastGroup.Antitoxin, 45f)
                .SkillType(SkillType.FirstAid)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(Antitoxin1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(3)
                .RequirementItem("stim_pack", preservePerkType: PerkType.FieldPharmacist, preserveChancePerLevel: 10);
        }

        private static void Antitoxin1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var duration = FirstAidTreatmentAdjustments.ApplyStimDurationBonus(activator, 120f);
            foreach (var friendly in AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                StatusEffect.RemoveFirstStatusEffect(friendly, new[] { typeof(PoisonStatusEffect), typeof(ToxinStatusEffect) }, false);
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(Antitoxin1StatusEffect), duration);
                FirstAidTreatmentAdjustments.ApplyCombatPharmacologyStimRiders(activator, friendly);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Remove_Condition), friendly);
            }
        }


    }
}
