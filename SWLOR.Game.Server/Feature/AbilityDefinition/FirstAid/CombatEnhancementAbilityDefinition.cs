﻿using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

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
            var willpowerMod = GetAbilityModifier(AbilityType.Willpower, activator);
            const float BaseLength = 900f;
            var length = BaseLength + willpowerMod * 30f;

            ApplyEffectToObject(DurationType.Temporary, EffectAbilityIncrease(AbilityType.Might, baseAmount), target, length);
            ApplyEffectToObject(DurationType.Temporary, EffectAbilityIncrease(AbilityType.Perception, baseAmount), target, length);
            ApplyEffectToObject(DurationType.Temporary, EffectAbilityIncrease(AbilityType.Vitality, baseAmount), target, length);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Globe_Use), target);

            TakeStimPack(activator);
        }

        private void CombatEnhancement1()
        {
            Builder.Create(FeatType.CombatEnhancement1, PerkType.CombatEnhancement)
                .Name("Combat Enhancement I")
                .HasRecastDelay(RecastGroup.CombatEnhancement, 30f)
                .HasActivationDelay(2f)
                .RequirementStamina(5)
                .UsesAnimation(Animation.LoopingGetMid)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, _, _) =>
                {
                    Impact(activator, target, 2);
                });
        }
        private void CombatEnhancement2()
        {
            Builder.Create(FeatType.CombatEnhancement2, PerkType.CombatEnhancement)
                .Name("Combat Enhancement II")
                .HasRecastDelay(RecastGroup.CombatEnhancement, 30f)
                .HasActivationDelay(2f)
                .RequirementStamina(6)
                .UsesAnimation(Animation.LoopingGetMid)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, _, _) =>
                {
                    Impact(activator, target, 4);
                });
        }
        private void CombatEnhancement3()
        {
            Builder.Create(FeatType.CombatEnhancement3, PerkType.CombatEnhancement)
                .Name("Combat Enhancement III")
                .HasRecastDelay(RecastGroup.CombatEnhancement, 30f)
                .HasActivationDelay(2f)
                .RequirementStamina(7)
                .UsesAnimation(Animation.LoopingGetMid)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, target, _, _) =>
                {
                    Impact(activator, target, 6);
                });
        }
    }
}
