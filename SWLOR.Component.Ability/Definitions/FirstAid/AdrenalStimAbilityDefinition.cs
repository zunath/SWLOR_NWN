using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Perk.Enums;

namespace SWLOR.Component.Ability.Definitions.FirstAid
{
    public class AdrenalStimAbilityDefinition : FirstAidBaseAbilityDefinition
    {
        public AdrenalStimAbilityDefinition(IServiceProvider serviceProvider) : base(serviceProvider)
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
                .HasRecastDelay(RecastGroupType.AdrenalStim, 60f * 3f)
                .HasActivationDelay(2f)
                .HasMaxRange(5f)
                .UsesAnimation(AnimationType.LoopingGetMid)
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
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Imp_Restoration), target);
                    ApplyEffectToObject(DurationType.Temporary, EffectAbilityDecrease(AbilityType.Willpower, 2), activator, 30f);
                });
        }

        private void AdrenalStim2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.AdrenalStim2, PerkType.AdrenalStim)
                .Name("Adrenal Stim II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.AdrenalStim, 60f * 3f)
                .HasActivationDelay(2f)
                .HasMaxRange(5f)
                .UsesAnimation(AnimationType.LoopingGetMid)
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
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Imp_Restoration), target);
                    ApplyEffectToObject(DurationType.Temporary, EffectAbilityDecrease(AbilityType.Willpower, 4), activator, 30f);
                });
        }

        private void AdrenalStim3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.AdrenalStim3, PerkType.AdrenalStim)
                .Name("Adrenal Stim III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.AdrenalStim, 60f * 3f)
                .HasActivationDelay(2f)
                .HasMaxRange(5f)
                .UsesAnimation(AnimationType.LoopingGetMid)
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
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Imp_Restoration), target);
                    ApplyEffectToObject(DurationType.Temporary, EffectAbilityDecrease(AbilityType.Willpower, 6), activator, 48f);
                });
        }
    }
}
