using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ItemService;
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

            // Charisma
            CreateItem("stim_cha1", AbilityType.Charisma, 2);
            CreateItem("stim_cha2", AbilityType.Charisma, 4);
            CreateItem("stim_cha3", AbilityType.Charisma, 6);

            // Strength
            CreateItem("stim_str1", AbilityType.Strength, 2);
            CreateItem("stim_str2", AbilityType.Strength, 4);
            CreateItem("stim_str3", AbilityType.Strength, 6);

            // Dexterity
            CreateItem("stim_dex1", AbilityType.Dexterity, 2);
            CreateItem("stim_dex2", AbilityType.Dexterity, 4);
            CreateItem("stim_dex3", AbilityType.Dexterity, 6);

            // Constitution
            CreateItem("stim_con1", AbilityType.Constitution, 2);
            CreateItem("stim_con2", AbilityType.Constitution, 4);
            CreateItem("stim_con3", AbilityType.Constitution, 6);

            // Wisdom
            CreateItem("stim_wis1", AbilityType.Wisdom, 2);
            CreateItem("stim_wis2", AbilityType.Wisdom, 4);
            CreateItem("stim_wis3", AbilityType.Wisdom, 6);

            // Intelligence
            CreateItem("stim_int1", AbilityType.Intelligence, 2);
            CreateItem("stim_int2", AbilityType.Intelligence, 4);
            CreateItem("stim_int3", AbilityType.Intelligence, 6);

        }
    }
}
