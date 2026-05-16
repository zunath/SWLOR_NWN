using System;
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

namespace SWLOR.Game.Server.Feature.AbilityDefinition.FirstAid
{
    public sealed class EmergencyCocktailAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            EmergencyCocktail1(builder);

            return builder.Build();
        }

        private static void EmergencyCocktail1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.EmergencyCocktail1, PerkType.EmergencyCocktail)
                .Name("Emergency Cocktail")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.EmergencyCocktail, 300f)
                .SkillType(SkillType.FirstAid)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target))
                .HasImpactAction(EmergencyCocktail1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(8)
                .RequirementItem("stim_pack", 2, PerkType.FieldPharmacist, 10);
        }

        private static void EmergencyCocktail1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var duration = FirstAidTreatmentAdjustments.ApplyStimDurationBonus(activator, 18f);
            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                Stat.RestoreStamina(friendly, PercentOf(Stat.GetMaxStamina(friendly), 25));
                StatusEffect.ApplyStatusEffect(activator, friendly, new AdrenalStimStatusEffect(1), duration);
                AbilityEffectScaling.ApplyTemporaryHPPercent(activator, friendly, 15, duration);
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(PainSuppressant2StatusEffect), duration);
                StatusEffect.RemoveFirstStatusEffect(friendly, new[] { typeof(PoisonStatusEffect), typeof(ToxinStatusEffect) }, false);
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(Antitoxin1StatusEffect), duration);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Restoration), friendly);
            }
        }

        private static int PercentOf(int value, int percent)
        {
            return Math.Max(1, value * percent / 100);
        }
    }
}
