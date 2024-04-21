using SWLOR.Core.NWScript.Enum;
using SWLOR.Core.NWScript.Enum.VisualEffect;
using SWLOR.Core.Service;
using SWLOR.Core.Service.AbilityService;
using SWLOR.Core.Service.PerkService;
using SWLOR.Core.Service.SkillService;
using SWLOR.Core.Service.StatusEffectService;

namespace SWLOR.Core.Feature.AbilityDefinition.FirstAid
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
                .Level(1)
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

                    TakeMedicalSupplies(activator);

                    Enmity.ModifyEnmityOnAll(activator, 200);
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.FirstAid, 3);
                });
        }
        private void TreatmentKit2()
        {
            Builder.Create(FeatType.TreatmentKit2, PerkType.TreatmentKit)
                .Name("Treatment Kit II")
                .Level(2)
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
                    StatusEffect.Remove(target, StatusEffectType.Disease);

                    TakeMedicalSupplies(activator);

                    Enmity.ModifyEnmityOnAll(activator, 350);
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.FirstAid, 3);
                });
        }
    }
}
