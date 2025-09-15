using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.FirstAid
{
    public class AdrenalStimAbilityDefinition : FirstAidBaseAbilityDefinition
    {
        public override Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            AdrenalStim1();
            AdrenalStim2();
            AdrenalStim3();

            return Builder.Build();
        }

        private void AdrenalStim1()
        {
            Builder.Create(FeatType.AdrenalStim1, PerkType.AdrenalStim)
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
                    StatusEffect.Apply(activator, activator, StatusEffectType.AdrenalStim1, 30f);
                    ApplyEffectToObject(DurationType.Temporary, EffectAbilityDecrease(AbilityType.Willpower, 2), activator, 30f);
                });
        }

        private void AdrenalStim2()
        {
            Builder.Create(FeatType.AdrenalStim2, PerkType.AdrenalStim)
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
                    StatusEffect.Apply(activator, activator, StatusEffectType.AdrenalStim2, 30f);
                    ApplyEffectToObject(DurationType.Temporary, EffectAbilityDecrease(AbilityType.Willpower, 4), activator, 30f);
                });
        }

        private void AdrenalStim3()
        {
            Builder.Create(FeatType.AdrenalStim3, PerkType.AdrenalStim)
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
                    StatusEffect.Apply(activator, activator, StatusEffectType.AdrenalStim3, 48f);
                    ApplyEffectToObject(DurationType.Temporary, EffectAbilityDecrease(AbilityType.Willpower, 6), activator, 48f);
                });
        }
    }
}
