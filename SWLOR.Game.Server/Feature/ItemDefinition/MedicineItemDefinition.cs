using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ItemService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Perk = SWLOR.Game.Server.Service.Perk;
using Random = SWLOR.Game.Server.Service.Random;
using Skill = SWLOR.Game.Server.Service.Skill;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    public class MedicineItemDefinition: IItemListDefinition
    {
        public Dictionary<string, ItemDetail> BuildItems()
        {
            var builder = new ItemBuilder();
            HealthPack(builder);
            ForcePack(builder);
            StaminaPack(builder);
            ResuscitationKit(builder);
            
            return builder.Build();
        }

        private void HealthPack(ItemBuilder builder)
        {
            void CreateItem(string tag, int amount, float duration)
            {
                builder.Create(tag)
                    .ValidationAction((user, item, target, location) =>
                    {
                        var currentHP = GetCurrentHitPoints(target);
                        var maxHP = GetMaxHitPoints(target);

                        if (currentHP >= maxHP)
                            return "Your target is undamaged.";

                        return string.Empty;
                    })
                    .InitializationMessage((user, item, target, location) =>
                    {
                        var name = GetName(target);
                        return $"You begin applying a health pack to {name}...";
                    })
                    .Delay(4.0f)
                    .UserFacesTarget()
                    .MaxDistance((user, item, target, location) =>
                    {
                        var perkLevel = Perk.GetEffectivePerkLevel(user, PerkType.RangedHealing);

                        return 3.5f + perkLevel;
                    })
                    .PlaysAnimation(Animation.LoopingGetMid)
                    .ReducesItemCharge((user, item, target, location) =>
                    {
                        var perkLevel = Perk.GetEffectivePerkLevel(user, PerkType.FrugalMedic);
                        var consumeChance = 10 * perkLevel;

                        return Random.D100(1) > consumeChance;
                    })
                    .ApplyAction((user, item, target, location) =>
                    {
                        ApplyEffectToObject(DurationType.Temporary, EffectRegenerate(amount, 6f), target, duration);
                        Skill.GiveSkillXP(user, SkillType.FirstAid, 150);
                        
                        PlaySound("use_bacta");
                    });
            }

            CreateItem("healing_kit", 2, 30f); // Healing Kit I
            CreateItem("healing_kit_p1", 3, 30f); // Healing Kit II
            CreateItem("healing_kit_p2", 4, 30f); // Healing Kit III
            CreateItem("healing_kit_p3", 5, 30f); // Healing Kit IV
            CreateItem("healing_kit_p4", 6, 30f); // Healing Kit V
        }

        private void ForcePack(ItemBuilder builder)
        {
            void CreateItem(string tag, StatusEffectType statusEffect, float duration)
            {
                builder.Create(tag)
                    .ValidationAction((user, item, target, location) =>
                    {

                        var currentFP = Stat.GetCurrentFP(target);
                        var maxFP = Stat.GetMaxFP(target);

                        if (currentFP >= maxFP)
                            return "Your target's FP is already full.";

                        return string.Empty;
                    })
                    .InitializationMessage((user, item, target, location) =>
                    {
                        var name = GetName(target);
                        return $"You begin applying a force pack to {name}...";
                    })
                    .Delay(4.0f)
                    .UserFacesTarget()
                    .MaxDistance((user, item, target, location) =>
                    {
                        var perkLevel = Perk.GetEffectivePerkLevel(user, PerkType.RangedHealing);

                        return 3.5f + perkLevel;
                    })
                    .PlaysAnimation(Animation.LoopingGetMid)
                    .ReducesItemCharge((user, item, target, location) =>
                    {
                        var perkLevel = Perk.GetEffectivePerkLevel(user, PerkType.FrugalMedic);
                        var consumeChance = 10 * perkLevel;

                        return Random.D100(1) > consumeChance;
                    })
                    .ApplyAction((user, item, target, location) =>
                    {
                        StatusEffect.Apply(user, target, statusEffect, duration);
                        Skill.GiveSkillXP(user, SkillType.FirstAid, 150);
                        PlaySound("use_bacta");
                    });
            }

            CreateItem("force_pack_1", StatusEffectType.ForcePack1, 30f);
            CreateItem("force_pack_2", StatusEffectType.ForcePack2, 30f);
            CreateItem("force_pack_3", StatusEffectType.ForcePack3, 30f);
            CreateItem("force_pack_4", StatusEffectType.ForcePack4, 30f);
            CreateItem("force_pack_5", StatusEffectType.ForcePack5, 30f);
        }

        private void StaminaPack(ItemBuilder builder)
        {
            void CreateItem(string tag, StatusEffectType statusEffect, float duration)
            {
                builder.Create(tag)
                    .ValidationAction((user, item, target, location) =>
                    {
                        var currentSTM = Stat.GetCurrentStamina(target);
                        var maxSTM = Stat.GetMaxStamina(target);

                        if (currentSTM >= maxSTM)
                            return "Your target's Stamina is already full.";

                        return string.Empty;
                    })
                    .InitializationMessage((user, item, target, location) =>
                    {
                        var name = GetName(target);
                        return $"You begin applying a stamina pack to {name}...";
                    })
                    .Delay(4.0f)
                    .UserFacesTarget()
                    .MaxDistance((user, item, target, location) =>
                    {
                        var perkLevel = Perk.GetEffectivePerkLevel(user, PerkType.RangedHealing);

                        return 3.5f + perkLevel;
                    })
                    .PlaysAnimation(Animation.LoopingGetMid)
                    .ReducesItemCharge((user, item, target, location) =>
                    {
                        var perkLevel = Perk.GetEffectivePerkLevel(user, PerkType.FrugalMedic);
                        var consumeChance = 10 * perkLevel;

                        return Random.D100(1) > consumeChance;
                    })
                    .ApplyAction((user, item, target, location) =>
                    {
                        StatusEffect.Apply(user, target, statusEffect, duration);
                        Skill.GiveSkillXP(user, SkillType.FirstAid, 150);
                        PlaySound("use_bacta");
                    });
            }

            CreateItem("stm_pack_1", StatusEffectType.StaminaPack1, 30f);
            CreateItem("stm_pack_2", StatusEffectType.StaminaPack2, 30f);
            CreateItem("stm_pack_3", StatusEffectType.StaminaPack3, 30f);
            CreateItem("stm_pack_4", StatusEffectType.StaminaPack4, 30f);
            CreateItem("stm_pack_5", StatusEffectType.StaminaPack5, 30f);
        }

        private void ResuscitationKit(ItemBuilder builder)
        {
            void CreateItem(string tag, float percentageHeal)
            {
                builder.Create(tag)
                    .ValidationAction((user, item, target, location) =>
                    {
                        if (!GetIsDead(target))
                            return "Your target is not dead.";

                        if (GetIsInCombat(user))
                            return "You are in combat.";
                        
                        return string.Empty;
                    })
                    .InitializationMessage((user, item, target, location) =>
                    {
                        var name = GetName(target);
                        return $"You begin resuscitating {name}...";
                    })
                    .Delay(12.0f)
                    .UserFacesTarget()
                    .MaxDistance((user, item, target, location) =>
                    {
                        var perkLevel = Perk.GetEffectivePerkLevel(user, PerkType.RangedHealing);

                        return 3.5f + perkLevel;
                    })
                    .PlaysAnimation(Animation.LoopingGetLow)
                    .ReducesItemCharge((user, item, target, location) =>
                    {
                        var perkLevel = Perk.GetEffectivePerkLevel(user, PerkType.FrugalMedic);
                        var consumeChance = 10 * perkLevel;

                        return Random.D100(1) > consumeChance;
                    })
                    .ApplyAction((user, item, target, location) =>
                    {
                        var hpRecover = (int)(GetMaxHitPoints(target) * percentageHeal);
                        
                        ApplyEffectToObject(DurationType.Instant, EffectResurrection(), target);
                        ApplyEffectToObject(DurationType.Instant, EffectHeal(hpRecover), target);
                        
                        Skill.GiveSkillXP(user, SkillType.FirstAid, 300);
                        PlaySound("use_bacta");
                    });
            }

            CreateItem("res_kit_1", 0.01f);
            CreateItem("res_kit_2", 0.11f);
            CreateItem("res_kit_3", 0.31f);
            CreateItem("res_kit_4", 0.51f);
        }
        
    }
}
