using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;
using SWLOR.Shared.Domain.StatusEffect.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.FirstAid
{
    public class TreatmentKitAbilityDefinition: FirstAidBaseAbilityDefinition
    {
        public TreatmentKitAbilityDefinition(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public override Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            TreatmentKit1(builder);
            TreatmentKit2(builder);

            return builder.Build();
        }

        private void TreatmentKit1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.TreatmentKit1, PerkType.TreatmentKit)
                .Name("Treatment Kit I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.TreatmentKit, 6f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .RequirementStamina(3)
                .UsesAnimation(AnimationType.LoopingGetMid)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation((activator, target, level, location) =>
                {
                    if (!IsWithinRange(activator, target))
                    {
                        return "Your target is too far away.";
                    }

                    if (!StatusEffectService.HasStatusEffect(target, StatusEffectType.Bleed, StatusEffectType.Poison))
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
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Imp_Healing_G), target);
                    StatusEffectService.Remove(target, StatusEffectType.Bleed);
                    StatusEffectService.Remove(target, StatusEffectType.Poison);
                    RemoveEffect(target, EffectScriptType.Poison, EffectScriptType.Disease);

                    TakeMedicalSupplies(activator);

                    EnmityService.ModifyEnmityOnAll(activator, 200);
                    CombatPointService.AddCombatPointToAllTagged(activator, SkillType.FirstAid, 3);
                });
        }
        private void TreatmentKit2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.TreatmentKit2, PerkType.TreatmentKit)
                .Name("Treatment Kit II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.TreatmentKit, 6f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .RequirementStamina(3)
                .UsesAnimation(AnimationType.LoopingGetMid)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation((activator, target, level, location) =>
                {
                    if (!IsWithinRange(activator, target))
                    {
                        return "Your target is too far away.";
                    }

                    if (!StatusEffectService.HasStatusEffect(target, StatusEffectType.Bleed, StatusEffectType.Poison, StatusEffectType.Shock, StatusEffectType.Burn))
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
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Imp_Healing_G), target);
                    StatusEffectService.Remove(target, StatusEffectType.Bleed);
                    StatusEffectService.Remove(target, StatusEffectType.Poison);
                    StatusEffectService.Remove(target, StatusEffectType.Shock);
                    StatusEffectService.Remove(target, StatusEffectType.Burn);
                    StatusEffectService.Remove(target, StatusEffectType.Disease);
                    RemoveEffect(target, EffectScriptType.Poison, EffectScriptType.Disease);

                    TakeMedicalSupplies(activator);

                    EnmityService.ModifyEnmityOnAll(activator, 350);
                    CombatPointService.AddCombatPointToAllTagged(activator, SkillType.FirstAid, 3);
                });
        }
    }
}
