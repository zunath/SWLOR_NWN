
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Event.Store;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Service
{
    public static class StoreService
    {
        private static readonly HashSet<NWObject> _stores;

        static StoreService()
        {
            _stores = new HashSet<NWObject>();
        }

        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleLoad>(message => OnModuleLoad());
            MessageHub.Instance.Subscribe<OnModuleHeartbeat>(message => OnModuleHeartbeat());
            MessageHub.Instance.Subscribe<OnOpenStore>(message => OnStoreOpened());
            MessageHub.Instance.Subscribe<OnCloseStore>(message => OnStoreClosed());
            MessageHub.Instance.Subscribe<OnModuleAcquireItem>(message => OnModuleAcquireItem());
        }

        private static void OnModuleLoad()
        {
            // Loop through all areas.
            foreach (var area in NWModule.Get().Areas)
            {
                // Loop through any stores in this area.
                foreach (var store in area.Objects.Where(x => x.ObjectType == _.OBJECT_TYPE_STORE))
                {
                    // Loop through the store's inventory (i.e items which are being sold)
                    foreach (var item in store.InventoryItems)
                    {
                        // Mark this item so it doesn't get cleaned up.
                        item.SetLocalInt("STORE_SERVICE_IS_STORE_ITEM", _.TRUE);
                    }

                    _stores.Add(store);
                }
            }
        }

        private static void OnModuleAcquireItem()
        {
            NWItem item = _.GetModuleItemAcquired();
            item.DeleteLocalInt("STORE_SERVICE_IS_STORE_ITEM");
        }

        private static void OnModuleHeartbeat()
        {
            NWModule module = NWModule.Get();
            int ticks = module.GetLocalInt("STORE_SERVICE_TICKS") + 1;

            // Check to see if it's time to clean stores.
            if (ticks >= 300) // 300 ticks * 6 seconds = 30 minutes
            {
                foreach (var store in _stores)
                {
                    CleanStore(store);
                }
            }

            module.SetLocalInt("STORE_SERVICE_TICKS", ticks);
        }

        private static void CleanStore(NWObject store)
        {
            // Only process if store is still valid.
            if (!store.IsValid) return;

            // Only process if no players are accessing it.
            if (store.GetLocalInt("STORE_SERVICE_PLAYERS_ACCESSING") > 0) return;

            // Only process if the store was closed more than 10 minutes ago.
            string closeDateString = store.GetLocalString("STORE_SERVICE_LAST_CLOSE_DATE");
            if(!string.IsNullOrWhiteSpace(closeDateString))
            {
                DateTime closeDate = DateTime.Parse(closeDateString);
                if (DateTime.UtcNow < closeDate.AddMinutes(10)) return; // todo change to 10 minutes
            }

            // By this point we know that the store needs to be cleaned up.
            // We'll look for any items which aren't part of this store and destroy them.
            foreach (var item in store.InventoryItems)
            {
                if (item.GetLocalInt("STORE_SERVICE_IS_STORE_ITEM") == _.TRUE) continue;
                item.Destroy();
            }
        }

        private static void OnStoreOpened()
        {
            NWObject store = NWGameObject.OBJECT_SELF;
            int playersAccessing = store.GetLocalInt("STORE_SERVICE_PLAYERS_ACCESSING") + 1;
            store.SetLocalInt("STORE_SERVICE_PLAYERS_ACCESSING", playersAccessing);
        }

        private static void OnStoreClosed()
        {
            NWObject store = NWGameObject.OBJECT_SELF;
            int playersAccessing = store.GetLocalInt("STORE_SERVICE_PLAYERS_ACCESSING") - 1;
            if (playersAccessing <= 0)
            {
                playersAccessing = 0;
                // We don't want to immediately clean it,
                // to give players a grace period to come back and buy back something they mistakenly sold.
                // So we'll track the current timestamp and later check it on the heartbeat event.
                DateTime now = DateTime.UtcNow;
                string nowString = now.ToString(CultureInfo.InvariantCulture);
                store.SetLocalString("STORE_SERVICE_LAST_CLOSE_DATE", nowString);
            }
            store.SetLocalInt("STORE_SERVICE_PLAYERS_ACCESSING", playersAccessing);
        }
    }
}
