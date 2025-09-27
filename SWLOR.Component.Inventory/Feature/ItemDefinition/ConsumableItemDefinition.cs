using Microsoft.Extensions.DependencyInjection;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Beasts.Contracts;
using SWLOR.Shared.Domain.Beasts.Enums;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Common.Enums;
using SWLOR.Shared.Domain.Crafting.ValueObjects;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Inventory.ValueObjects;

namespace SWLOR.Component.Inventory.Feature.ItemDefinition
{
    public class ConsumableItemDefinition: IItemListDefinition
    {
        private readonly IRandomService _random;
        private readonly IDatabaseService _db;
        private readonly IServiceProvider _serviceProvider;
        

        public ConsumableItemDefinition(
            IRandomService random, 
            IDatabaseService db, 
            IServiceProvider serviceProvider)
        {
            _random = random;
            _db = db;
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private IStatusEffectService StatusEffectService => _serviceProvider.GetRequiredService<IStatusEffectService>();
        private IBeastMasteryService BeastMasteryService => _serviceProvider.GetRequiredService<IBeastMasteryService>();
        private IItemService ItemService => _serviceProvider.GetRequiredService<IItemService>();
        private ICurrencyService CurrencyService => _serviceProvider.GetRequiredService<ICurrencyService>();
        private IItemBuilder Builder => _serviceProvider.GetRequiredService<IItemBuilder>();

        public Dictionary<string, ItemDetail> BuildItems()
        {
            SlugShake();
            Food();
            PetFood();
            RebuildToken();

            return Builder.Build();
        }

        private void SlugShake()
        {
            Builder.Create("slug_shake")
                .Delay(1f)
                .PlaysAnimation(AnimationType.FireForgetDrink)
                .ReducesItemCharge()
                .ApplyAction((user, item, target, location, itemPropertyIndex) =>
                {
                    var ability = AbilityType.Invalid;
                    
                    switch (_random.Next(5) + 1)
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
            Builder.Create("FOOD")
                .Delay(1f)
                .PlaysAnimation(AnimationType.FireForgetSalute)
                .ReducesItemCharge()
                .ValidationAction((user, item, target, location, itemPropertyIndex) =>
                {
                    if (StatusEffectService.HasStatusEffect(user, StatusEffectType.Food))
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

                        var bonusType = (ItemPropertyFoodSubType)GetItemPropertySubType(ip);
                        var amount = GetItemPropertyCostTableValue(ip);

                        switch (bonusType)
                        {
                            case ItemPropertyFoodSubType.HP:
                                foodEffect.HP += amount;
                                break;
                            case ItemPropertyFoodSubType.FP:
                                foodEffect.FP += amount;
                                break;
                            case ItemPropertyFoodSubType.STM:
                                foodEffect.STM += amount;
                                break;
                            case ItemPropertyFoodSubType.HPRegen:
                                foodEffect.HPRegen += amount;
                                break;
                            case ItemPropertyFoodSubType.FPRegen:
                                foodEffect.FPRegen += amount;
                                break;
                            case ItemPropertyFoodSubType.STMRegen:
                                foodEffect.STMRegen += amount;
                                break;
                            case ItemPropertyFoodSubType.RestRegen:
                                foodEffect.RestRegen += amount;
                                break;
                            case ItemPropertyFoodSubType.XPBonus:
                                foodEffect.XPBonusPercent += amount;
                                break;
                            case ItemPropertyFoodSubType.RecastReduction:
                                foodEffect.RecastReductionPercent += amount;
                                break;
                            case ItemPropertyFoodSubType.Duration:
                                duration += amount * (60f * 5); // 5 minutes per duration bonus
                                break;
                            case ItemPropertyFoodSubType.Might:
                                ApplyEffectToObject(DurationType.Temporary, EffectAbilityIncrease(AbilityType.Might, amount), user, duration);
                                break;
                            case ItemPropertyFoodSubType.Vitality:
                                ApplyEffectToObject(DurationType.Temporary, EffectAbilityIncrease(AbilityType.Vitality, amount), user, duration);
                                break;
                            case ItemPropertyFoodSubType.Perception:
                                ApplyEffectToObject(DurationType.Temporary, EffectAbilityIncrease(AbilityType.Perception, amount), user, duration);
                                break;
                            case ItemPropertyFoodSubType.Willpower:
                                ApplyEffectToObject(DurationType.Temporary, EffectAbilityIncrease(AbilityType.Willpower, amount), user, duration);
                                break;
                            case ItemPropertyFoodSubType.Agility:
                                ApplyEffectToObject(DurationType.Temporary, EffectAbilityIncrease(AbilityType.Agility, amount), user, duration);
                                break;
                            case ItemPropertyFoodSubType.Social:
                                ApplyEffectToObject(DurationType.Temporary, EffectAbilityIncrease(AbilityType.Social, amount), user, duration);
                                break;
                            case ItemPropertyFoodSubType.DefensePhysical:
                                foodEffect.DefensePhysical += amount;
                                break;
                            case ItemPropertyFoodSubType.DefenseForce:
                                foodEffect.DefenseForce += amount;
                                break;
                            case ItemPropertyFoodSubType.DefenseFire:
                                foodEffect.DefenseFire += amount;
                                break;
                            case ItemPropertyFoodSubType.DefensePoison:
                                foodEffect.DefensePoison += amount;
                                break;
                            case ItemPropertyFoodSubType.DefenseElectrical:
                                foodEffect.DefenseElectrical += amount;
                                break;
                            case ItemPropertyFoodSubType.DefenseIce:
                                foodEffect.DefenseIce += amount;
                                break;
                            case ItemPropertyFoodSubType.Evasion:
                                foodEffect.Evasion += amount;
                                break;
                            case ItemPropertyFoodSubType.ControlSmithery:
                                foodEffect.Control[SkillType.Smithery] += amount;
                                break;
                            case ItemPropertyFoodSubType.CraftsmanshipSmithery:
                                foodEffect.Craftsmanship[SkillType.Smithery] += amount;
                                break;
                            case ItemPropertyFoodSubType.ControlEngineering:
                                foodEffect.Control[SkillType.Engineering] += amount;
                                break;
                            case ItemPropertyFoodSubType.CraftsmanshipEngineering:
                                foodEffect.Craftsmanship[SkillType.Engineering] += amount;
                                break;
                            case ItemPropertyFoodSubType.ControlFabrication:
                                foodEffect.Control[SkillType.Fabrication] += amount;
                                break;
                            case ItemPropertyFoodSubType.CraftsmanshipFabrication:
                                foodEffect.Craftsmanship[SkillType.Fabrication] += amount;
                                break;
                            case ItemPropertyFoodSubType.ControlAgriculture:
                                foodEffect.Control[SkillType.Agriculture] += amount;
                                break;
                            case ItemPropertyFoodSubType.CraftsmanshipAgriculture:
                                foodEffect.Craftsmanship[SkillType.Agriculture] += amount;
                                break;
                            case ItemPropertyFoodSubType.Accuracy:
                                foodEffect.Accuracy += amount;
                                break;
                            case ItemPropertyFoodSubType.Attack:
                                foodEffect.Attack += amount;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }

                    StatusEffectService.Apply(user, user, StatusEffectType.Food, duration, foodEffect);
                });
        }

        private void PetFood()
        {
            Builder.Create("PET_FOOD")
                .Delay(1f)
                .PlaysAnimation(AnimationType.LoopingGetLow)
                .ValidationAction((user, item, target, location, itemPropertyIndex) =>
                {
                    var minimumLevel = (GetLocalInt(item, "BEAST_FOOD_TIER") - 1) * 10;
                    var beast = GetAssociate(AssociateType.Henchman, user);

                    if (!BeastMasteryService.IsPlayerBeast(beast))
                    {
                        return "You do not have a beast active.";
                    }

                    if (StatusEffectService.HasStatusEffect(beast, StatusEffectType.PetFood))
                    {
                        return "Your beast is not hungry.";
                    }

                    var beastId = BeastMasteryService.GetBeastId(beast);
                    var dbBeast = _db.Get<Beast>(beastId);

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
                    var beastId = BeastMasteryService.GetBeastId(beast);
                    var dbBeast = _db.Get<Beast>(beastId);

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

                    StatusEffectService.Apply(user, beast, StatusEffectType.PetFood, 1800f, xpBonus);

                    ItemService.ReduceItemStack(item, 1);
                });
        }

        private void RebuildToken()
        {
            Builder.Create("rebuild_token")
                .PlaysAnimation(AnimationType.LoopingGetMid)
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
                    CurrencyService.GiveCurrency(user, CurrencyType.RebuildToken, 1);
                    ItemService.ReduceItemStack(item, 1);
                    SendMessageToPC(user, $"Total Rebuild Tokens: {CurrencyService.GetCurrency(user, CurrencyType.RebuildToken)}");
                });
        }
    }
}
