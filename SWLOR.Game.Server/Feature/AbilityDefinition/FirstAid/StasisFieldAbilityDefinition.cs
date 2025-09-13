using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.FirstAid
{
    public class StasisFieldAbilityDefinition: FirstAidBaseAbilityDefinition
    {
        public override Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            StasisField1();
            StasisField2();
            StasisField3();

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
            var willpowerMod = GetAbilityModifier(AbilityType.Willpower, activator);
            const float BaseLength = 900f;
            var length = BaseLength + willpowerMod * 30f;

            for (var effect = GetFirstEffect(target); GetIsEffectValid(effect); effect = GetNextEffect(target))
            {
                if(GetEffectTag(effect) == "STASIS_FIELD")
                    RemoveEffect(target, effect);
            }

            var acEffect = EffectACIncrease(baseAmount, ArmorClassModiferType.Natural);
            acEffect = TagEffect(acEffect, "STASIS_FIELD");
            ApplyEffectToObject(DurationType.Temporary, acEffect, target, length);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Ac_Bonus), target);

            TakeStimPack(activator);
        }

        private void StasisField1()
        {
            Builder.Create(FeatType.StasisField1, PerkType.StasisField)
                .Name("Stasis Field I")
                .Level(1)
                .HasRecastDelay(RecastGroup.StasisField, 30f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .RequirementStamina(5)
                .UsesAnimation(Animation.LoopingGetMid)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, _, _) =>
                {
                    Impact(activator, target, 2);

                    Enmity.ModifyEnmityOnAll(activator, 250);
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.FirstAid, 3);
                });
        }

        private void StasisField2()
        {
            Builder.Create(FeatType.StasisField2, PerkType.StasisField)
                .Name("Stasis Field II")
                .Level(2)
                .HasRecastDelay(RecastGroup.StasisField, 30f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .RequirementStamina(6)
                .UsesAnimation(Animation.LoopingGetMid)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, _, _) =>
                {
                    Impact(activator, target, 4);

                    Enmity.ModifyEnmityOnAll(activator, 350);
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.FirstAid, 3);
                });
        }

        private void StasisField3()
        {
            Builder.Create(FeatType.StasisField3, PerkType.StasisField)
                .Name("Stasis Field III")
                .Level(3)
                .HasRecastDelay(RecastGroup.StasisField, 30f)
                .HasActivationDelay(2f)
                .HasMaxRange(30.0f)
                .RequirementStamina(7)
                .UsesAnimation(Animation.LoopingGetMid)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, _, _) =>
                {
                    Impact(activator, target, 6);

                    Enmity.ModifyEnmityOnAll(activator, 450);
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.FirstAid, 3);
                });
        }
    }
}
