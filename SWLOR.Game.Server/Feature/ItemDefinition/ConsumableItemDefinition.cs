using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.CurrencyService;
using SWLOR.Game.Server.Service.ItemService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Associate;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;
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
            PetFood();
            RebuildToken();
            KyberToken();

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
                    StatusEffect.ApplyStatusEffect(user, user, new SlugShakePenaltyStatusEffect(ability), 120f);
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
                    if (StatusEffect.HasStatusEffect(user, typeof(FoodStatusEffect)))
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
                            case FoodItemPropertySubType.CombatReadiness:
                                foodEffect.CombatReadinessPercent += amount;
                                break;
                            case FoodItemPropertySubType.Duration:
                                duration += amount * (60f * 5); // 5 minutes per duration bonus
                                break;
                            case FoodItemPropertySubType.Might:
                                foodEffect.Might += amount;
                                break;
                            case FoodItemPropertySubType.Vitality:
                                foodEffect.Vitality += amount;
                                break;
                            case FoodItemPropertySubType.Perception:
                                foodEffect.Perception += amount;
                                break;
                            case FoodItemPropertySubType.Willpower:
                                foodEffect.Willpower += amount;
                                break;
                            case FoodItemPropertySubType.Agility:
                                foodEffect.Agility += amount;
                                break;
                            case FoodItemPropertySubType.Social:
                                foodEffect.Social += amount;
                                break;
                            case FoodItemPropertySubType.DefensePhysical:
                                foodEffect.DefensePhysical += amount;
                                break;
                            case FoodItemPropertySubType.DefenseForce:
                                foodEffect.DefenseForce += amount;
                                break;
                            case FoodItemPropertySubType.ResistanceFire:
                                foodEffect.ResistanceFire += amount;
                                break;
                            case FoodItemPropertySubType.ResistancePoison:
                                foodEffect.ResistancePoison += amount;
                                break;
                            case FoodItemPropertySubType.ResistanceElectrical:
                                foodEffect.ResistanceElectrical += amount;
                                break;
                            case FoodItemPropertySubType.ResistanceIce:
                                foodEffect.ResistanceIce += amount;
                                break;
                            case FoodItemPropertySubType.ResistanceMind:
                                foodEffect.ResistanceMind += amount;
                                break;
                            case FoodItemPropertySubType.ResistanceMobility:
                                foodEffect.ResistanceMobility += amount;
                                break;
                            case FoodItemPropertySubType.ResistanceTrauma:
                                foodEffect.ResistanceTrauma += amount;
                                break;
                            case FoodItemPropertySubType.ResistanceDisruption:
                                foodEffect.ResistanceDisruption += amount;
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

                    StatusEffect.ApplyStatusEffect(user, user, new FoodStatusEffect(foodEffect), duration);
                });
        }

        private void PetFood()
        {
            _builder.Create("PET_FOOD")
                .Delay(1f)
                .PlaysAnimation(Animation.LoopingGetLow)
                .ValidationAction((user, item, target, location, itemPropertyIndex) =>
                {
                    var minimumLevel = (GetLocalInt(item, "BEAST_FOOD_TIER") - 1) * 10;
                    var beast = GetAssociate(AssociateType.Henchman, user);

                    if (!BeastMastery.IsPlayerBeast(beast))
                    {
                        return "You do not have a beast active.";
                    }

                    if (StatusEffect.HasStatusEffect(beast, typeof(PetFoodStatusEffect)))
                    {
                        return "Your beast is not hungry.";
                    }

                    var beastId = BeastMastery.GetBeastId(beast);
                    var dbBeast = DB.Get<Beast>(beastId);

                    if (dbBeast.Level < minimumLevel)
                    {
                        return $"Your beast's level is too low for that food. (Required: {minimumLevel}, current level: {dbBeast.Level})";
                    }

                    return string.Empty;
                })
                .ApplyAction((user, item, target, location, index) =>
                {
                    var foodType = (BeastFoodType)GetLocalInt(item, "BEAST_FOOD_TYPE_ID");
                    var foodTier = GetLocalInt(item, "BEAST_FOOD_TIER");
                    var beast = GetAssociate(AssociateType.Henchman, user);
                    var beastId = BeastMastery.GetBeastId(beast);
                    var dbBeast = DB.Get<Beast>(beastId);

                    var xpBonus = foodTier * 10;

                    if (dbBeast.FavoriteFood == foodType)
                    {
                        xpBonus += 10;
                        SendMessageToPC(user, "Your beast likes this food a lot!");
                    }
                    else if (dbBeast.HatedFood == foodType)
                    {
                        xpBonus -= 5;
                        SendMessageToPC(user, "Your beast doesn't like this food very much...");
                    }

                    StatusEffect.ApplyStatusEffect(user, beast, new PetFoodStatusEffect(xpBonus), 1800f);

                    Item.ReduceItemStack(item, 1);
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
                    Currency.GiveCurrency(user, CurrencyType.RebuildToken, 1);
                    Item.ReduceItemStack(item, 1);
                    SendMessageToPC(user, $"Total Rebuild Tokens: {Currency.GetCurrency(user, CurrencyType.RebuildToken)}");
                });
        }

        private void KyberToken()
        {
            _builder.Create("kyber_token")
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
                    Currency.GiveCurrency(user, CurrencyType.KyberToken, 1);
                    Item.ReduceItemStack(item, 1);
                    SendMessageToPC(user, $"Total Kyber Tokens: {Currency.GetCurrency(user, CurrencyType.KyberToken)}");
                });
        }
    }
}
