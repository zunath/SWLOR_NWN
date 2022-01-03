﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatusEffectService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.FirstAid
{
    public class TreatmentKitAbilityDefinition: FirstAidBaseAbilityDefinition
    {
        public override Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            TreatmentKit1();
            TreatmentKit2();

            return Builder.Build();
        }

        private void TreatmentKit1()
        {
            Builder.Create(FeatType.TreatmentKit1, PerkType.TreatmentKit)
                .Name("Treatment Kit I")
                .HasRecastDelay(RecastGroup.TreatmentKit, 6f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .RequirementStamina(3)
                .UsesAnimation(Animation.LoopingGetMid)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation((activator, target, level, location) =>
                {
                    if (!IsWithinRange(activator, target))
                    {
                        return "Your target is too far away.";
                    }

                    if (!StatusEffect.HasStatusEffect(target, StatusEffectType.Bleed, StatusEffectType.Poison))
                    {
                        return "Your target is healthy.";
                    }

                    if (!HasMedicalSupplies(activator))
                    {
                        return "You have no medical supplies.";
                    }

                    return string.Empty;
                })
                .HasImpactAction((activator, target, _, _) =>
                {
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_G), target);
                    StatusEffect.Remove(target, StatusEffectType.Bleed);
                    StatusEffect.Remove(target, StatusEffectType.Poison);
                });
        }
        private void TreatmentKit2()
        {
            Builder.Create(FeatType.TreatmentKit2, PerkType.TreatmentKit)
                .Name("Treatment Kit II")
                .HasRecastDelay(RecastGroup.TreatmentKit, 6f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .RequirementStamina(3)
                .UsesAnimation(Animation.LoopingGetMid)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation((activator, target, level, location) =>
                {
                    if (!IsWithinRange(activator, target))
                    {
                        return "Your target is too far away.";
                    }

                    if (!StatusEffect.HasStatusEffect(target, StatusEffectType.Bleed, StatusEffectType.Poison, StatusEffectType.Shock, StatusEffectType.Burn))
                    {
                        return "Your target is healthy.";
                    }

                    if (!HasMedicalSupplies(activator))
                    {
                        return "You have no medical supplies.";
                    }

                    return string.Empty;
                })
                .HasImpactAction((activator, target, _, _) =>
                {
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_G), target);
                    StatusEffect.Remove(target, StatusEffectType.Bleed);
                    StatusEffect.Remove(target, StatusEffectType.Poison);
                    StatusEffect.Remove(target, StatusEffectType.Shock);
                    StatusEffect.Remove(target, StatusEffectType.Burn);
                });
        }
    }
}
