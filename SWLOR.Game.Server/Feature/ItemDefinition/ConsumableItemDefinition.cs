using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Core.NWScript.Enum.Item.Property;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.StatusEffectDefinition.StatusEffectData;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ItemService;
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
                .ApplyAction((user, item, target, location) =>
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
                .ValidationAction((user, item, target, location) =>
                {
                    if (StatusEffect.HasStatusEffect(user, StatusEffectType.Food))
                    {
                        return "You are not hungry.";
                    }

                    return string.Empty;
                })
                .ApplyAction((user, item, target, location) =>
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
                        }
                    }

                    StatusEffect.Apply(user, user, StatusEffectType.Food, duration, foodEffect);
                });
        }

        private void RebuildToken()
        {
            _builder.Create("rebuild_token")
                .PlaysAnimation(Animation.LoopingGetMid)
                .ValidationAction((user, item, target, location) =>
                {
                    if (!GetIsPC(user) || GetIsDM(user) || GetIsDMPossessed(user))
                    {
                        return "Only players may use this item.";
                    }

                    return string.Empty;
                })
                .ApplyAction((user, item, target, location) =>
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
