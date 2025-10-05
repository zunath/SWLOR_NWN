using Microsoft.Extensions.DependencyInjection;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Associate.Contracts;
using SWLOR.Shared.Domain.Associate.Enums;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Inventory.Enums;
using SWLOR.Shared.Domain.Inventory.ValueObjects;

namespace SWLOR.Component.Inventory.Definitions.ItemDefinition
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
            
            // Initialize lazy services
            _beastMasteryService = new Lazy<IBeastMasteryService>(() => _serviceProvider.GetRequiredService<IBeastMasteryService>());
            _itemService = new Lazy<IItemService>(() => _serviceProvider.GetRequiredService<IItemService>());
            _currencyService = new Lazy<ICurrencyService>(() => _serviceProvider.GetRequiredService<ICurrencyService>());
            _builder = new Lazy<IItemBuilder>(() => _serviceProvider.GetRequiredService<IItemBuilder>());
        }

        // Lazy-loaded services to break circular dependencies
        private readonly Lazy<IBeastMasteryService> _beastMasteryService;
        private readonly Lazy<IItemService> _itemService;
        private readonly Lazy<ICurrencyService> _currencyService;
        private readonly Lazy<IItemBuilder> _builder;
        
        private IBeastMasteryService BeastMasteryService => _beastMasteryService.Value;
        private IItemService ItemService => _itemService.Value;
        private ICurrencyService CurrencyService => _currencyService.Value;
        private IItemBuilder Builder => _builder.Value;

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
                    return string.Empty;
                })
                .ApplyAction((user, item, target, location, itemPropertyIndex) =>
                {
                    // todo: implement food
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

                    // todo: implement pet food in new system
                    //if (StatusEffectService.HasStatusEffect(beast, StatusEffectType.PetFood))
                    //{
                    //    return "Your beast is not hungry.";
                    //}

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

                    // todo: implement pet food in new system
                    //StatusEffectService.Apply(user, beast, StatusEffectType.PetFood, 1800f, xpBonus);

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
