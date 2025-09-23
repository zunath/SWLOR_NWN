using SWLOR.Component.Ability.Contracts;
using SWLOR.Component.Ability.Model;
using SWLOR.Component.Perk.Contracts;
using SWLOR.Component.StatusEffect.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Domain.Contracts;
using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.Domain.Model;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.FirstAid
{
    public class AdrenalStimAbilityDefinition : FirstAidBaseAbilityDefinition
    {
        public AdrenalStimAbilityDefinition(IRandomService random, IPerkService perkService, ICombatPointService combatPointService, IEnmityService enmityService, IAbilityService abilityService, IStatusEffectService statusEffectService) : base(random, perkService, combatPointService, enmityService, abilityService, statusEffectService)
        {
        }

        public override Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            AdrenalStim1(builder);
            AdrenalStim2(builder);
            AdrenalStim3(builder);

            return builder.Build();
        }

        private void AdrenalStim1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.AdrenalStim1, PerkType.AdrenalStim)
                .Name("Adrenal Stim I")
                .Level(1)
                .HasRecastDelay(RecastGroup.AdrenalStim, 60f * 3f)
                .HasActivationDelay(2f)
                .HasMaxRange(5f)
                .UsesAnimation(Animation.LoopingGetMid)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation((activator, target, level, location) =>
                {
                    if (!IsWithinRange(activator, target))
                    {
                        return "Your target is too far away.";
                    }

                    if (!HasStimPack(activator))
                    {
                        return "You have no stim packs.";
                    }

                    if (GetIsEnemy(target, activator) || GetIsEnemy(activator, target))
                    {
                        return "You can only use this ability on yourself or an ally.";
                    }

                    return string.Empty;
                })
                .HasImpactAction((activator, target, _, _) =>
                {
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Restoration), target);
                    StatusEffectService.Apply(activator, activator, StatusEffectType.AdrenalStim1, 30f);
                    ApplyEffectToObject(DurationType.Temporary, EffectAbilityDecrease(AbilityType.Willpower, 2), activator, 30f);
                });
        }

        private void AdrenalStim2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.AdrenalStim2, PerkType.AdrenalStim)
                .Name("Adrenal Stim II")
                .Level(2)
                .HasRecastDelay(RecastGroup.AdrenalStim, 60f * 3f)
                .HasActivationDelay(2f)
                .HasMaxRange(5f)
                .UsesAnimation(Animation.LoopingGetMid)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation((activator, target, level, location) =>
                {
                    if (!IsWithinRange(activator, target))
                    {
                        return "Your target is too far away.";
                    }

                    if (!HasStimPack(activator))
                    {
                        return "You have no stim packs.";
                    }

                    if (GetIsEnemy(target, activator) || GetIsEnemy(activator, target))
                    {
                        return "You can only use this ability on yourself or an ally.";
                    }

                    return string.Empty;
                })
                .HasImpactAction((activator, target, _, _) =>
                {
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Restoration), target);
                    StatusEffectService.Apply(activator, activator, StatusEffectType.AdrenalStim2, 30f);
                    ApplyEffectToObject(DurationType.Temporary, EffectAbilityDecrease(AbilityType.Willpower, 4), activator, 30f);
                });
        }

        private void AdrenalStim3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.AdrenalStim3, PerkType.AdrenalStim)
                .Name("Adrenal Stim III")
                .Level(3)
                .HasRecastDelay(RecastGroup.AdrenalStim, 60f * 3f)
                .HasActivationDelay(2f)
                .HasMaxRange(5f)
                .UsesAnimation(Animation.LoopingGetMid)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation((activator, target, level, location) =>
                {
                    if (!IsWithinRange(activator, target))
                    {
                        return "Your target is too far away.";
                    }

                    if (!HasStimPack(activator))
                    {
                        return "You have no stim packs.";
                    }

                    if (GetIsEnemy(target, activator) || GetIsEnemy(activator, target))
                    {
                        return "You can only use this ability on yourself or an ally.";
                    }

                    return string.Empty;
                })
                .HasImpactAction((activator, target, _, _) =>
                {
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Restoration), target);
                    StatusEffectService.Apply(activator, activator, StatusEffectType.AdrenalStim3, 48f);
                    ApplyEffectToObject(DurationType.Temporary, EffectAbilityDecrease(AbilityType.Willpower, 6), activator, 48f);
                });
        }
    }
}
