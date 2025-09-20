using System;
using System.Collections.Generic;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Core.Bioware;
using SWLOR.Shared.Core.Log.LogGroup;
using SWLOR.Shared.Core.Service;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Game.Server.Feature
{
    public class StoreManagement
    {
        private static readonly ILogger _logger = ServiceContainer.GetService<ILogger>();
        private static readonly IScheduler _scheduler = ServiceContainer.GetService<IScheduler>();
        private const int IntervalHours = 1; // Determines the interval at which stores are cleaned. 1 = 1 hour
        private static readonly List<uint> _stores = new();
        private const string StoreServiceItem = "STORE_SERVICE_IS_STORE_ITEM";

        /// <summary>
        /// When the module loads, place all stores inside the cache and schedule the cleanup process.
        /// </summary>
        [ScriptHandler<OnModuleLoad>]
        public static void ProcessStores()
        {
            for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
            {
                for (var store = GetFirstObjectInArea(area); GetIsObjectValid(store); store = GetNextObjectInArea(area))
                {
                    if (GetObjectType(store) != ObjectType.Store)
                        continue;

                    for (var item = GetFirstItemInInventory(store); GetIsObjectValid(item); item = GetNextItemInInventory(store))
                    {
                        SetLocalBool(item, StoreServiceItem, true);
                        ApplyIncreasedPriceItemProperty(item);
                    }

                    _stores.Add(store);
                }
            }

            _scheduler.ScheduleRepeating(DoCleanStores, TimeSpan.FromHours(IntervalHours));
        }

        /// <summary>
        /// When a store item is acquired, destroy the local flag indicating it's a store item.
        /// </summary>
        [ScriptHandler<OnModuleAcquire>]
        public static void AcquireItem()
        {
            ClearStoreServiceItemFlag();
            HandleIncreasedPriceItemProperty();
        }

        private static void ClearStoreServiceItemFlag()
        {
            var item = GetModuleItemAcquired();
            DeleteLocalBool(item, StoreServiceItem);
        }
        
        private static void HandleIncreasedPriceItemProperty()
        {
            var item = GetModuleItemAcquired();
            var creature = GetModuleItemAcquiredBy();

            if (GetIsPC(creature))
            {
                BiowareXP2.IPRemoveMatchingItemProperties(item, ItemPropertyType.IncreasedPrice, DurationType.Invalid, -1);
            }
            else
            {
                ApplyIncreasedPriceItemProperty(item);
            }
        }

        private static void ApplyIncreasedPriceItemProperty(uint item)
        {
            var increasedPrice = 0;
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                if (GetItemPropertyType(ip) == ItemPropertyType.IncreasedPrice)
                {
                    var index = GetItemPropertyCostTableValue(ip);
                    var value = Get2DAString("iprp_incprice", "Cost", index);
                    if (int.TryParse(value, out var price))
                    {
                        increasedPrice += price;
                    }
                }
            }

            if (increasedPrice > 0)
            {
                var price = ItemPlugin.GetBaseGoldPieceValue(item) + increasedPrice;
                ItemPlugin.SetBaseGoldPieceValue(item, price);
            }
        }

        /// <summary>
        /// Iterates through all the stores and destroys any non-store items.
        /// </summary>
        private static void DoCleanStores()
        {
            foreach (var store in _stores)
            {
                var count = 0;
                for (var item = GetFirstItemInInventory(store); GetIsObjectValid(item); item = GetNextItemInInventory(store))
                {
                    if (GetLocalBool(item, StoreServiceItem))
                        continue;

                    DestroyObject(item);
                    count++;
                }

                _logger.Write<StoreCleanupLogGroup>($"Store cleaned: {GetName(store)}. Items destroyed: {count}");
            }
        }

        /// <summary>
        /// Destroys items sold to NPC stores immediately.
        /// </summary>
        [ScriptHandler(ScriptName.OnStoreSellAfter)]
        public static void DestroySoldItem()
        {
            var item = StringToObject(EventsPlugin.GetEventData("ITEM"));
            var isSuccessful = EventsPlugin.GetEventData("RESULT") == "1";

            if (!isSuccessful)
                return;

            DestroyObject(item);
        }

        /// <summary>
        /// Prevents items from being sold from a henchman's inventory.
        /// </summary>
        [ScriptHandler(ScriptName.OnStoreSellBefore)]
        public static void PreventSalesFromHenchmenInventory()
        {
            var item = StringToObject(EventsPlugin.GetEventData("ITEM"));
            var owner = GetItemPossessor(item);
            var master = GetMaster(owner);

            if (GetIsObjectValid(master))
            {
                EventsPlugin.SkipEvent();
                SendMessageToPC(master, ColorToken.Red("Items cannot be directly sold from your henchman's inventory."));
            }
        }
    }
}
