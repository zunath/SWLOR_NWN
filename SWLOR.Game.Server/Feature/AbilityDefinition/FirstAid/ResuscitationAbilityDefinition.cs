using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.FirstAid
{
    public class ResuscitationAbilityDefinition: FirstAidBaseAbilityDefinition
    {
        public override Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Resuscitation1();
            Resuscitation2();
            Resuscitation3();

            return Builder.Build();
        }

        private string Validation(uint activator, uint target, int level, Location location)
        {
            if (!IsWithinRange(activator, target))
            {
                return "Your target is too far away.";
            }

            if (GetCurrentHitPoints(target) > 0)
            {
                return "Your target is not unconscious.";
            }

            if (!HasMedicalSupplies(activator))
            {
                return "You have no medical supplies.";
            }

            return string.Empty;
        }

        private void Impact(uint activator, uint target, int percentHeal)
        {
            var willpowerMod = GetAbilityModifier(AbilityType.Willpower, activator);
            percentHeal += willpowerMod * 2;
            var amount = (int)(GetMaxHitPoints(target) * (percentHeal * 0.01f));

            ApplyEffectToObject(DurationType.Instant, EffectResurrection(), target);
            
            if(percentHeal > 0)
                ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), target);

            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Head_Heal), target);
            TakeMedicalSupplies(activator);
        }

        private void Resuscitation1()
        {
            Builder.Create(FeatType.Resuscitation1, PerkType.Resuscitation)
                .Name("Resuscitation I")
                .HasRecastDelay(RecastGroup.Resuscitation, 180f)
                .HasActivationDelay(6f)
                .RequirementStamina(10)
                .UsesAnimation(Animation.LoopingGetLow)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, _, _) =>
                {
                    Impact(activator, target, 0);
                });
        }

        private void Resuscitation2()
        {
            Builder.Create(FeatType.Resuscitation2, PerkType.Resuscitation)
                .Name("Resuscitation II")
                .HasRecastDelay(RecastGroup.Resuscitation, 180f)
                .HasActivationDelay(6f)
                .RequirementStamina(10)
                .UsesAnimation(Animation.LoopingGetLow)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, _, _) =>
                {
                    Impact(activator, target, 25);
                });
        }

        private void Resuscitation3()
        {
            Builder.Create(FeatType.Resuscitation3, PerkType.Resuscitation)
                .Name("Resuscitation III")
                .HasRecastDelay(RecastGroup.Resuscitation, 180f)
                .HasActivationDelay(6f)
                .RequirementStamina(10)
                .UsesAnimation(Animation.LoopingGetLow)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, _, _) =>
                {
                    Impact(activator, target, 50);
                });
        }
    }
}
