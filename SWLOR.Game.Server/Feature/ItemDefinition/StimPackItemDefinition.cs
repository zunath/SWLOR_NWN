using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ItemService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Skill = SWLOR.Game.Server.Service.Skill;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    public class StimPackItemDefinition: IItemListDefinition
    {
        public Dictionary<string, ItemDetail> BuildItems()
        {
            var builder = new ItemBuilder();
            StimPacks(builder);

            return builder.Build();
        }

        private void StimPacks(ItemBuilder builder)
        {
            const string EffectTag = "STIM_PACK_EFFECT";
            
            void CreateItem(string tag, AbilityType ability, int amount)
            {
                builder.Create(tag)
                    .ValidationAction((user, item, target, location) =>
                    {
                        for (var effect = GetFirstEffect(target); GetIsEffectValid(effect); effect = GetNextEffect(target))
                        {
                            if (GetEffectTag(effect) == EffectTag)
                            {
                                return $"Your target is already under the effect of another stimulant.";
                            }
                        }
                        
                        return string.Empty;
                    })
                    .InitializationMessage((user, item, target, location) =>
                    {
                        var name = GetName(target);
                        return $"You begin injecting {name} with a stim pack.";
                    })
                    .Delay(2.0f)
                    .UserFacesTarget()
                    .MaxDistance((user, item, target, location) =>
                    {
                        var perkLevel = Perk.GetEffectivePerkLevel(user, PerkType.RangedHealing);

                        return 3.5f + perkLevel;
                    })
                    .PlaysAnimation(Animation.LoopingGetMid)
                    .ReducesItemCharge()
                    .ApplyAction((user, item, target, location) =>
                    {
                        var effect = EffectAbilityIncrease(ability, amount);
                        effect = TagEffect(effect, EffectTag);
                        ApplyEffectToObject(DurationType.Temporary, effect, target, 120f);

                        if (user != target)
                        {
                            SendMessageToPC(target, $"{GetName(user)} injects you with a stim pack.");
                        }
                        
                        Skill.GiveSkillXP(user, SkillType.FirstAid, 150);
                    });
            }

            // Diplomacy
            CreateItem("stim_cha1", AbilityType.Diplomacy, 2);
            CreateItem("stim_cha2", AbilityType.Diplomacy, 4);
            CreateItem("stim_cha3", AbilityType.Diplomacy, 6);

            // Might
            CreateItem("stim_str1", AbilityType.Might, 2);
            CreateItem("stim_str2", AbilityType.Might, 4);
            CreateItem("stim_str3", AbilityType.Might, 6);

            // Perception
            CreateItem("stim_dex1", AbilityType.Perception, 2);
            CreateItem("stim_dex2", AbilityType.Perception, 4);
            CreateItem("stim_dex3", AbilityType.Perception, 6);

            // Vitality
            CreateItem("stim_con1", AbilityType.Vitality, 2);
            CreateItem("stim_con2", AbilityType.Vitality, 4);
            CreateItem("stim_con3", AbilityType.Vitality, 6);

            // Willpower
            CreateItem("stim_wis1", AbilityType.Willpower, 2);
            CreateItem("stim_wis2", AbilityType.Willpower, 4);
            CreateItem("stim_wis3", AbilityType.Willpower, 6);
        }
    }
}
