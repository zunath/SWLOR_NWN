using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Common.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.FirstAid
{
    public class CombatEnhancementAbilityDefinition: FirstAidBaseAbilityDefinition
    {
        public CombatEnhancementAbilityDefinition(IServiceProvider serviceProvider) 
            : base(serviceProvider)
        {
        }

        public override Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            CombatEnhancement1(builder);
            CombatEnhancement2(builder);
            CombatEnhancement3(builder);

            return builder.Build();
        }

        private string Validation(uint activator, uint target, int level, Location location)
        {
            if (!IsWithinRange(activator, target))
            {
                return "Your target is too far away.";
            }

            if (!HasStimPack(activator))
            {
                return "You have no stim packs.";
            }

            return string.Empty;
        }

        private void Impact(uint activator, uint target, int baseAmount)
        {
            for (var e = GetFirstEffect(target); GetIsEffectValid(e); e = GetNextEffect(target))
            {
                if (GetEffectTag(e) == "COMBAT_ENHANCEMENT" || GetEffectTag(e) == "FORCE_INSPIRATION")
                {
                    RemoveEffect(target, e);
                }
            }

            var willpowerMod = GetAbilityScore(activator, AbilityType.Willpower);
            const float BaseLength = 900f;
            var length = BaseLength + willpowerMod * 15f;

            var effect = EffectLinkEffects(
                EffectAbilityIncrease(AbilityType.Might, baseAmount),
                EffectAbilityIncrease(AbilityType.Perception, baseAmount));
            effect = EffectLinkEffects(effect, EffectAbilityIncrease(AbilityType.Vitality, baseAmount));
            effect = TagEffect(effect, "COMBAT_ENHANCEMENT");

            ApplyEffectToObject(DurationType.Temporary, effect, target, length);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Imp_Globe_Use), target);

            TakeStimPack(activator);
        }

        private void CombatEnhancement1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.CombatEnhancement1, PerkType.CombatEnhancement)
                .Name("Combat Enhancement I")
                .Level(1)
                .HasRecastDelay(RecastGroup.CombatEnhancement, 30f)
                .HasActivationDelay(2f)
                .RequirementStamina(5)
                .UsesAnimation(AnimationType.LoopingGetMid)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, _, _) =>
                {
                    Impact(activator, target, 1);

                    EnmityService.ModifyEnmity(activator, target, 250);
                    CombatPointService.AddCombatPoint(activator, target, SkillType.FirstAid, 3);
                });
        }
        private void CombatEnhancement2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.CombatEnhancement2, PerkType.CombatEnhancement)
                .Name("Combat Enhancement II")
                .Level(2)
                .HasRecastDelay(RecastGroup.CombatEnhancement, 30f)
                .HasActivationDelay(2f)
                .RequirementStamina(6)
                .UsesAnimation(AnimationType.LoopingGetMid)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, _, _) =>
                {
                    Impact(activator, target, 2);

                    EnmityService.ModifyEnmity(activator, target, 350);
                    CombatPointService.AddCombatPoint(activator, target, SkillType.FirstAid, 3);
                });
        }
        private void CombatEnhancement3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.CombatEnhancement3, PerkType.CombatEnhancement)
                .Name("Combat Enhancement III")
                .Level(3)
                .HasRecastDelay(RecastGroup.CombatEnhancement, 30f)
                .HasActivationDelay(2f)
                .RequirementStamina(7)
                .UsesAnimation(AnimationType.LoopingGetMid)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, _, _) =>
                {
                    Impact(activator, target, 3);

                    EnmityService.ModifyEnmity(activator, target, 450);
                    CombatPointService.AddCombatPoint(activator, target, SkillType.FirstAid, 3);
                });
        }
    }
}
