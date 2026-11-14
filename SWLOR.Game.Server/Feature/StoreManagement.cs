using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature
{
    public class StoreManagement
    {
        private const int IntervalHours = 1; // Determines the interval at which stores are cleaned. 1 = 1 hour
        // The stores the boot found. Carried, not adopted, on a hot reload: the rerun LoadStores also finds the stores of area
        // instances made since boot (CreateArea copies a template's store with its items unmarked), which the hourly clean-up
        // would empty; the carried list replaces what it found.
        [ReloadState(ReloadStateKind.Carried)]
        private static readonly List<uint> _stores = new();
        private const string StoreServiceItem = "STORE_SERVICE_IS_STORE_ITEM";

        /// <summary>
        /// When the module loads, place all stores inside the cache and schedule the cleanup process. It only reads the
        /// world, so a hot reload runs it again (for the cleanup; the store list is carried); <see cref="ProcessStores"/> runs
        /// right after it.
        /// </summary>
        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void LoadStores()
        {
            for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
            {
                for (var store = GetFirstObjectInArea(area); GetIsObjectValid(store); store = GetNextObjectInArea(area))
                {
                    if (GetObjectType(store) != ObjectType.Store)
                        continue;

                    _stores.Add(store);
                }
            }

            Scheduler.ScheduleRepeating(DoCleanStores, TimeSpan.FromHours(IntervalHours));
        }

        /// <summary>
        /// After <see cref="LoadStores"/>, marks every item the stores of the module hold as a store item and adds its
        /// increased price to its base value. Boot only: a second run would raise the prices again.
        /// </summary>
        [BootPhase(BootReload.BootOnly, NothingToAdopt = "The marks and prices stay on the store items in the engine, and the store list is carried.")]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void ProcessStores()
        {
            foreach (var store in _stores)
            {
                for (var item = GetFirstItemInInventory(store); GetIsObjectValid(item); item = GetNextItemInInventory(store))
                {
                    SetLocalBool(item, StoreServiceItem, true);
                    ApplyIncreasedPriceItemProperty(item);
                }
            }
        }

        /// <summary>
        /// When a store item is acquired, destroy the local flag indicating it's a store item.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleAcquire)]
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

                Log.Write(LogGroup.StoreCleanup, $"Store cleaned: {GetName(store)}. Items destroyed: {count}");
            }
        }

        /// <summary>
        /// Destroys items sold to NPC stores immediately.
        /// </summary>
        [NWNEventHandler(ScriptName.OnStoreSellAfter)]
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
        [NWNEventHandler(ScriptName.OnStoreSellBefore)]
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
