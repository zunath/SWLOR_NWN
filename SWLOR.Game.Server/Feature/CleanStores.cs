using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;

namespace SWLOR.Game.Server.Feature
{
    public class CleanStores
    {
        private const int IntervalHours = 1; // Determines the interval at which stores are cleaned. 1 = 1 hour
        private static readonly List<uint> _stores = new();

        /// <summary>
        /// When the module loads, place all stores inside the cache and schedule the cleanup process.
        /// </summary>
        [NWNEventHandler("mod_load")]
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
                        SetLocalBool(item, "STORE_SERVICE_IS_STORE_ITEM", true);
                    }

                    _stores.Add(store);
                }
            }

            Scheduler.ScheduleRepeating(DoCleanStores, TimeSpan.FromHours(IntervalHours));
        }

        /// <summary>
        /// When a store item is acquired, destroy the local flag indicating it's a store item.
        /// </summary>
        [NWNEventHandler("mod_acquire")]
        public static void AcquireItem()
        {
            var item = GetModuleItemAcquired();
            DeleteLocalBool(item, "STORE_SERVICE_IS_STORE_ITEM");
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
                    if (GetLocalBool(item, "STORE_SERVICE_IS_STORE_ITEM"))
                        continue;

                    DestroyObject(item);
                    count++;
                }

                Log.Write(LogGroup.StoreCleanup, $"Store cleaned: {GetName(store)}. Items destroyed: {count}");
            }
        }

        [NWNEventHandler("store_sell_aft")]
        public static void DestroySoldItem()
        {
            var item = StringToObject(EventsPlugin.GetEventData("ITEM"));
            var isSuccessful = EventsPlugin.GetEventData("RESULT") == "1";

            if (!isSuccessful)
                return;

            DestroyObject(item);
        }

    }
}
