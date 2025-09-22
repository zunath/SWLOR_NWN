using SWLOR.Component.Ability.Contracts;
using SWLOR.Component.Ability.Enums;
using SWLOR.Component.Ability.Model;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Force
{
    public class ForceInspirationAbilityDefinition: IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            ForceInspiration1(builder);
            ForceInspiration2(builder);
            ForceInspiration3(builder);

            return builder.Build();
        }

        private static void Impact(uint activator, uint target, int baseAmount) 
        {
            var willpowerMod = GetAbilityScore(activator, AbilityType.Willpower);
            const float BaseLength = 900f;
            var length = BaseLength + willpowerMod * 15f;

            var effect = EffectLinkEffects(EffectAbilityIncrease(AbilityType.Willpower, baseAmount),
                EffectAbilityIncrease(AbilityType.Agility, baseAmount));
            effect = EffectLinkEffects(effect, EffectAbilityIncrease(AbilityType.Might, baseAmount));

            for (var e = GetFirstEffect(target); GetIsEffectValid(e); e = GetNextEffect(target))
            {
                if (GetEffectTag(e) == "COMBAT_ENHANCEMENT" || GetEffectTag(e) == "FORCE_INSPIRATION")
                {
                    RemoveEffect(target, e);
                }
            }
            effect = TagEffect(effect, "FORCE_INSPIRATION");

            ApplyEffectToObject(DurationType.Temporary, effect, target, length);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Globe_Use), target);

        }

        private void ForceInspiration1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceInspiration1, PerkType.ForceInspiration)
                .Name("Force Inspiration I")
                .HasRecastDelay(RecastGroup.ForceInspiration, 30f)
                .HasActivationDelay(2f)
                .RequirementFP(5)
                .UsesAnimation(Animation.LoopingGetMid)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, _, _) =>
                {
                    Impact(activator, target, 1);
                });
        }
        private void ForceInspiration2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceInspiration2, PerkType.ForceInspiration)
                .Name("Force Inspiration II")
                .HasRecastDelay(RecastGroup.ForceInspiration, 30f)
                .HasActivationDelay(2f)
                .RequirementFP(6)
                .UsesAnimation(Animation.LoopingGetMid)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, _, _) =>
                {
                    Impact(activator, target, 2);
                });
        }
        private void ForceInspiration3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceInspiration3, PerkType.ForceInspiration)
                .Name("Force Inspiration III")
                .HasRecastDelay(RecastGroup.ForceInspiration, 30f)
                .HasActivationDelay(2f)
                .RequirementFP(7)
                .UsesAnimation(Animation.LoopingGetMid)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, _, _) =>
                {
                    Impact(activator, target, 3);
                });
        }
    }
}
