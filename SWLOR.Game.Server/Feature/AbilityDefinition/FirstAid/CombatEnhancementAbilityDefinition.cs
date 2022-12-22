using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.FirstAid
{
    public class CombatEnhancementAbilityDefinition: FirstAidBaseAbilityDefinition
    {
        public override Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            CombatEnhancement1();
            CombatEnhancement2();
            CombatEnhancement3();

            return Builder.Build();
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
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Globe_Use), target);

            TakeStimPack(activator);
        }

        private void CombatEnhancement1()
        {
            Builder.Create(FeatType.CombatEnhancement1, PerkType.CombatEnhancement)
                .Name("Combat Enhancement I")
                .Level(1)
                .HasRecastDelay(RecastGroup.CombatEnhancement, 30f)
                .HasActivationDelay(2f)
                .RequirementStamina(5)
                .UsesAnimation(Animation.LoopingGetMid)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, _, _) =>
                {
                    Impact(activator, target, 1);

                    Enmity.ModifyEnmity(activator, target, 250);
                    CombatPoint.AddCombatPoint(activator, target, SkillType.FirstAid, 3);
                });
        }
        private void CombatEnhancement2()
        {
            Builder.Create(FeatType.CombatEnhancement2, PerkType.CombatEnhancement)
                .Name("Combat Enhancement II")
                .Level(2)
                .HasRecastDelay(RecastGroup.CombatEnhancement, 30f)
                .HasActivationDelay(2f)
                .RequirementStamina(6)
                .UsesAnimation(Animation.LoopingGetMid)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, _, _) =>
                {
                    Impact(activator, target, 2);

                    Enmity.ModifyEnmity(activator, target, 350);
                    CombatPoint.AddCombatPoint(activator, target, SkillType.FirstAid, 3);
                });
        }
        private void CombatEnhancement3()
        {
            Builder.Create(FeatType.CombatEnhancement3, PerkType.CombatEnhancement)
                .Name("Combat Enhancement III")
                .Level(3)
                .HasRecastDelay(RecastGroup.CombatEnhancement, 30f)
                .HasActivationDelay(2f)
                .RequirementStamina(7)
                .UsesAnimation(Animation.LoopingGetMid)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, _, _) =>
                {
                    Impact(activator, target, 3);

                    Enmity.ModifyEnmity(activator, target, 450);
                    CombatPoint.AddCombatPoint(activator, target, SkillType.FirstAid, 3);
                });
        }
    }
}
