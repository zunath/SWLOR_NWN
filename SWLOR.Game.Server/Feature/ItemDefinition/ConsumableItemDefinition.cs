using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Core.NWScript.Enum.Item.Property;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.StatusEffectDefinition.StatusEffectData;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ItemService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    public class ConsumableItemDefinition: IItemListDefinition
    {
        private readonly ItemBuilder _builder = new();
        public Dictionary<string, ItemDetail> BuildItems()
        {
            SlugShake();
            Food();
            RebuildToken();

            return _builder.Build();
        }

        private void SlugShake()
        {
            _builder.Create("slug_shake")
                .Delay(1f)
                .PlaysAnimation(Animation.FireForgetDrink)
                .ReducesItemCharge()
                .ApplyAction((user, item, target, location, itemPropertyIndex) =>
                {
                    var ability = AbilityType.Invalid;
                    
                    switch (Random.Next(5) + 1)
                    {
                        case 1:
                            ability = AbilityType.Social;
                            break;
                        case 2:
                            ability = AbilityType.Vitality;
                            break;
                        case 3:
                            ability = AbilityType.Perception;
                            break;
                        case 4:
                            ability = AbilityType.Might;
                            break;
                        case 5:
                            ability = AbilityType.Willpower;
                            break;
                    }

                    var maxHP = GetMaxHitPoints(user);
                    ApplyEffectToObject(DurationType.Instant, EffectHeal(maxHP), user);
                    ApplyEffectToObject(DurationType.Temporary, EffectAbilityDecrease(ability, 50), user, 120f);

                });
        }

        private void Food()
        {
            _builder.Create("FOOD")
                .Delay(1f)
                .PlaysAnimation(Animation.FireForgetSalute)
                .ReducesItemCharge()
                .ValidationAction((user, item, target, location, itemPropertyIndex) =>
                {
                    if (StatusEffect.HasStatusEffect(user, StatusEffectType.Food))
                    {
                        return "You are not hungry.";
                    }

                    return string.Empty;
                })
                .ApplyAction((user, item, target, location, itemPropertyIndex) =>
                {
                    var foodEffect = new FoodEffectData();
                    var duration = 1800f; // 30 minutes by default for all food

                    for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
                    {
                        if (GetItemPropertyType(ip) != ItemPropertyType.FoodBonus)
                            continue;

                        var bonusType = (FoodItemPropertySubType)GetItemPropertySubType(ip);
                        var amount = GetItemPropertyCostTableValue(ip);

                        switch (bonusType)
                        {
                            case FoodItemPropertySubType.HP:
                                foodEffect.HP += amount;
                                break;
                            case FoodItemPropertySubType.FP:
                                foodEffect.FP += amount;
                                break;
                            case FoodItemPropertySubType.STM:
                                foodEffect.STM += amount;
                                break;
                            case FoodItemPropertySubType.HPRegen:
                                foodEffect.HPRegen += amount;
                                break;
                            case FoodItemPropertySubType.FPRegen:
                                foodEffect.FPRegen += amount;
                                break;
                            case FoodItemPropertySubType.STMRegen:
                                foodEffect.STMRegen += amount;
                                break;
                            case FoodItemPropertySubType.RestRegen:
                                foodEffect.RestRegen += amount;
                                break;
                            case FoodItemPropertySubType.XPBonus:
                                foodEffect.XPBonusPercent += amount;
                                break;
                            case FoodItemPropertySubType.RecastReduction:
                                foodEffect.RecastReductionPercent += amount;
                                break;
                            case FoodItemPropertySubType.Duration:
                                duration += amount * 60f; // 1 minute per duration bonus
                                break;
                            case FoodItemPropertySubType.Might:
                                ApplyEffectToObject(DurationType.Temporary, EffectAbilityIncrease(AbilityType.Might, amount), user, duration);
                                break;
                            case FoodItemPropertySubType.Vitality:
                                ApplyEffectToObject(DurationType.Temporary, EffectAbilityIncrease(AbilityType.Vitality, amount), user, duration);
                                break;
                            case FoodItemPropertySubType.Perception:
                                ApplyEffectToObject(DurationType.Temporary, EffectAbilityIncrease(AbilityType.Perception, amount), user, duration);
                                break;
                            case FoodItemPropertySubType.Willpower:
                                ApplyEffectToObject(DurationType.Temporary, EffectAbilityIncrease(AbilityType.Willpower, amount), user, duration);
                                break;
                            case FoodItemPropertySubType.Agility:
                                ApplyEffectToObject(DurationType.Temporary, EffectAbilityIncrease(AbilityType.Agility, amount), user, duration);
                                break;
                            case FoodItemPropertySubType.Social:
                                ApplyEffectToObject(DurationType.Temporary, EffectAbilityIncrease(AbilityType.Social, amount), user, duration);
                                break;
                            case FoodItemPropertySubType.DefensePhysical:
                                foodEffect.DefensePhysical += amount;
                                break;
                            case FoodItemPropertySubType.DefenseForce:
                                foodEffect.DefenseForce += amount;
                                break;
                            case FoodItemPropertySubType.DefenseFire:
                                foodEffect.DefenseFire += amount;
                                break;
                            case FoodItemPropertySubType.DefensePoison:
                                foodEffect.DefensePoison += amount;
                                break;
                            case FoodItemPropertySubType.DefenseElectrical:
                                foodEffect.DefenseElectrical += amount;
                                break;
                            case FoodItemPropertySubType.DefenseIce:
                                foodEffect.DefenseIce += amount;
                                break;
                            case FoodItemPropertySubType.Evasion:
                                foodEffect.Evasion += amount;
                                break;
                            case FoodItemPropertySubType.ControlSmithery:
                                foodEffect.Control[SkillType.Smithery] += amount;
                                break;
                            case FoodItemPropertySubType.CraftsmanshipSmithery:
                                foodEffect.Craftsmanship[SkillType.Smithery] += amount;
                                break;
                            case FoodItemPropertySubType.ControlEngineering:
                                foodEffect.Control[SkillType.Engineering] += amount;
                                break;
                            case FoodItemPropertySubType.CraftsmanshipEngineering:
                                foodEffect.Craftsmanship[SkillType.Engineering] += amount;
                                break;
                            case FoodItemPropertySubType.ControlFabrication:
                                foodEffect.Control[SkillType.Fabrication] += amount;
                                break;
                            case FoodItemPropertySubType.CraftsmanshipFabrication:
                                foodEffect.Craftsmanship[SkillType.Fabrication] += amount;
                                break;
                            case FoodItemPropertySubType.ControlAgriculture:
                                foodEffect.Control[SkillType.Agriculture] += amount;
                                break;
                            case FoodItemPropertySubType.CraftsmanshipAgriculture:
                                foodEffect.Craftsmanship[SkillType.Agriculture] += amount;
                                break;
                            case FoodItemPropertySubType.Accuracy:
                                foodEffect.Accuracy += amount;
                                break;
                            case FoodItemPropertySubType.Attack:
                                foodEffect.Attack += amount;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }

                    StatusEffect.Apply(user, user, StatusEffectType.Food, duration, foodEffect);
                });
        }

        private void RebuildToken()
        {
            _builder.Create("rebuild_token")
                .PlaysAnimation(Animation.LoopingGetMid)
                .ValidationAction((user, item, target, location, itemPropertyIndex) =>
                {
                    if (!GetIsPC(user) || GetIsDM(user) || GetIsDMPossessed(user))
                    {
                        return "Only players may use this item.";
                    }

                    return string.Empty;
                })
                .ApplyAction((user, item, target, location, itemPropertyIndex) =>
                {
                    var playerId = GetObjectUUID(user);
                    var dbPlayer = DB.Get<Player>(playerId);

                    dbPlayer.NumberRebuildsAvailable++;
                    DB.Set(dbPlayer);

                    Item.ReduceItemStack(item, 1);
                    SendMessageToPC(user, $"Total Rebuild Tokens: {dbPlayer.NumberRebuildsAvailable}");
                });
        }
    }
}
