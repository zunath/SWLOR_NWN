using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Entity;
using Object = SWLOR.Game.Server.Core.NWNX.Object;
using Player = SWLOR.Game.Server.Entity.Player;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service
{
    public static class PlayerMarket
    {
        // Serves two purposes:
        //    1.) Tracks the names of stores.
        //    2.) Identifies that the store is active and should be displayed on the shop list.
        private static Dictionary<string, string> ActiveStoreNames { get; } = new Dictionary<string, string>();

        // Tracks the merchant object which contains the items being sold by a store.
        private static Dictionary<string, uint> StoreMerchants { get; } = new Dictionary<string, uint>();

        // Tracks the number of players accessing each store.
        private static Dictionary<string, int> StoresOpen { get; } = new Dictionary<string, int>();

        /// <summary>
        /// When the module loads, look for all player stores and see if they're open and active.
        /// Those that are will be added to the cache for later look-up.
        /// </summary>
        [NWNEventHandler("mod_load")]
        public static void LoadPlayerStores()
        {
            var keys = DB.SearchKeys("PlayerStore");

            foreach (var key in keys)
            {
                var dbPlayerStore = DB.Get<PlayerStore>(key);
                UpdateCacheEntry(key, dbPlayerStore);
            }
        }

        /// <summary>
        /// Determines if a store is currently open and if it should be displayed in the menu.
        /// </summary>
        /// <param name="store">The store to check.</param>
        /// <returns>true if store is available, false otherwise</returns>
        public static bool IsStoreOpen(PlayerStore store)
        {
            var validItemsForSale = store.ItemsForSale.Count(x => x.Value.Price > 0);

            if (store.IsOpen &&
                DateTime.UtcNow < store.DateLeaseExpires &&
                validItemsForSale > 0) 
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Retrieves all of the active stores.
        /// </summary>
        /// <returns>A dictionary containing all of the active stores.</returns>
        public static Dictionary<string, string> GetAllActiveStores()
        {
            return ActiveStoreNames.ToDictionary(x => x.Key, y => y.Value);
        }

        public static void OpenPlayerStore(uint player, string sellerPlayerId)
        {
            uint merchant;

            if (StoreMerchants.ContainsKey(sellerPlayerId))
            {
                merchant = StoreMerchants[sellerPlayerId];
            }
            else
            {
                var dbPlayerStore = DB.Get<PlayerStore>(sellerPlayerId);
                merchant = CreateMerchantObject(sellerPlayerId, dbPlayerStore);
                StoreMerchants[sellerPlayerId] = merchant;
            }

            OpenStore(merchant, player);
        }

        private static uint CreateMerchantObject(string sellerPlayerId, PlayerStore dbPlayerStore)
        {
            const string StoreResref = "player_store";
            var merchant = CreateObject(ObjectType.Store, StoreResref, GetLocation(OBJECT_SELF));
            SetLocalString(merchant, "SELLER_PLAYER_ID", sellerPlayerId);

            foreach (var item in dbPlayerStore.ItemsForSale)
            {
                if (item.Value.Price <= 0) continue;

                var deserialized = Object.Deserialize(item.Value.Data);
                Object.AcquireItem(merchant, deserialized);

                var originalBaseGPValue = Core.NWNX.Item.GetBaseGoldPieceValue(deserialized);
                var originalAdditionalGPValue = Core.NWNX.Item.GetAddGoldPieceValue(deserialized);

                SetLocalInt(deserialized, "ORIGINAL_BASE_GP_VALUE", originalBaseGPValue);
                SetLocalInt(deserialized, "ORIGINAL_ADDITIONAL_GP_VALUE", originalAdditionalGPValue);

                Core.NWNX.Item.SetBaseGoldPieceValue(deserialized, item.Value.Price);
                Core.NWNX.Item.SetAddGoldPieceValue(deserialized, 0);
            }

            return merchant;
        }

        /// <summary>
        /// Updates the cache with the latest information from this entity.
        /// This should be called after changing a player store's details.
        /// </summary>
        /// <param name="playerId">The store owner's player Id</param>
        /// <param name="dbPlayerStore">The player store entity</param>
        public static void UpdateCacheEntry(string playerId, PlayerStore dbPlayerStore)
        {
            if (IsStoreOpen(dbPlayerStore))
            {
                ActiveStoreNames[playerId] = dbPlayerStore.StoreName;
            }
            else
            {
                if (ActiveStoreNames.ContainsKey(playerId))
                {
                    ActiveStoreNames.Remove(playerId);
                }
            }
        }

        /// <summary>
        /// Returns true if one or more players are accessing a store.
        /// Otherwise, returns false.
        /// </summary>
        /// <param name="sellerPlayerId">The player Id of the seller.</param>
        /// <returns>True if being accessed, false otherwise</returns>
        public static bool IsStoreBeingAccessed(string sellerPlayerId)
        {
            return StoresOpen.ContainsKey(sellerPlayerId) && StoresOpen[sellerPlayerId] > 0;
        }

        /// <summary>
        /// When an item is added to the terminal, track it and reopen the market terminal dialog.
        /// </summary>
        [NWNEventHandler("mkt_term_dist")]
        public static void MarketTerminalDisturbed()
        {
            if (GetInventoryDisturbType() != DisturbType.Added) return;

            var player = GetLastDisturbed();
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var dbPlayerStore = DB.Get<PlayerStore>(playerId);
            var item = GetInventoryDisturbItem();
            var itemId = GetObjectUUID(item);
            var serialized = Object.Serialize(item);
            var listingLimit = 20;

            if (dbPlayerStore.ItemsForSale.Count >= listingLimit || // Listing limit reached.
                GetBaseItemType(item) == BaseItem.Gold ||           // Gold can't be listed.
                string.IsNullOrWhiteSpace(GetResRef(item)) ||       // Items without resrefs can't be listed.
                GetHasInventory(item))                              // Bags and other containers can't be listed.
            {
                Item.ReturnItem(player, item);
                SendMessageToPC(player, "This item cannot be listed.");
                return;
            }

            dbPlayerStore.ItemsForSale.Add(itemId, new PlayerStoreItem
            {
                Data = serialized,
                Name = GetName(item),
                Price = 0,
                StackSize = GetItemStackSize(item)
            });

            DB.Set(playerId, dbPlayerStore);
            DestroyObject(item);

            SendMessageToPC(player, $"Listing limit: {dbPlayerStore.ItemsForSale.Count} / {listingLimit}");
        }

        /// <summary>
        /// When the terminal is opened, send an instructional message to the player.
        /// </summary>
        [NWNEventHandler("mkt_term_open")]
        public static void MarketTerminalOpened()
        {
            var player = GetLastOpenedBy();
            FloatingTextStringOnCreature("Place the items you wish to sell into the container. When you're finished, click the terminal again.", player, false);
        }

        /// <summary>
        /// When the terminal is closed, reset all event scripts on it.
        /// </summary>
        [NWNEventHandler("mkt_term_closed")]
        public static void MarketTerminalClosed()
        {
            var terminal = OBJECT_SELF;
            SetEventScript(terminal, EventScript.Placeable_OnOpen, string.Empty);
            SetEventScript(terminal, EventScript.Placeable_OnClosed, string.Empty);
            SetEventScript(terminal, EventScript.Placeable_OnInventoryDisturbed, string.Empty);
            SetEventScript(terminal, EventScript.Placeable_OnUsed, "start_convo");
        }

        /// <summary>
        /// When a player's shop is opened, mark it as such.
        /// </summary>
        [NWNEventHandler("plyr_shop_open")]
        public static void PlayerShopOpened()
        {
            var store = OBJECT_SELF;
            var sellerPlayerId = GetLocalString(store, "SELLER_PLAYER_ID");

            if (!StoresOpen.ContainsKey(sellerPlayerId))
            {
                StoresOpen[sellerPlayerId] = 1;
            }
            else
            {
                StoresOpen[sellerPlayerId]--;
            }
        }

        /// <summary>
        /// When a player's shop is closed, mark it as closed.
        /// </summary>
        [NWNEventHandler("plyr_shop_closed")]
        public static void PlayerShopClosed()
        {
            var store = OBJECT_SELF;
            var sellerPlayerId = GetLocalString(store, "SELLER_PLAYER_ID");

            StoresOpen[sellerPlayerId]--;

            // If no one's accessing the store right now, destroy it and remove it from cache.
            if (StoresOpen[sellerPlayerId] <= 0)
            {
                StoreMerchants.Remove(sellerPlayerId);
                StoresOpen.Remove(sellerPlayerId);

                DestroyObject(store);
            }
        }

        /// <summary>
        /// When a player buys an item, deposit those credits into the owner's store till and remove the item from the database.
        /// </summary>
        [NWNEventHandler("store_buy_aft")]
        public static void PlayerShopBuyItem()
        {
            var buyer = OBJECT_SELF;
            var item = StringToObject(Events.GetEventData("ITEM"));
            var price = Convert.ToInt32(Events.GetEventData("PRICE"));
            var store = StringToObject(Events.GetEventData("STORE"));

            if (GetResRef(store) != "player_store") return;

            var sellerPlayerId = GetLocalString(store, "SELLER_PLAYER_ID");
            var dbPlayer = DB.Get<Player>(sellerPlayerId);
            var dbPlayerStore = DB.Get<PlayerStore>(sellerPlayerId);
            var itemId = GetObjectUUID(item);

            // Audit the purchase.
            Log.Write(LogGroup.PlayerMarket, $"{GetName(buyer)} purchased item '{GetName(item)}' x{GetItemStackSize(item)} for {price} credits from {dbPlayer.Name}'s store.");

            var taxed = price - (int)(price * dbPlayerStore.TaxRate);
            if (taxed < 1)
                taxed = 1;

            dbPlayerStore.Till += taxed;
            dbPlayerStore.ItemsForSale.Remove(itemId);
            DB.Set(sellerPlayerId, dbPlayerStore);

            DelayCommand(0.1f, () =>
            {
                // Set pricing back to normal
                var originalBaseGPValue = GetLocalInt(item, "ORIGINAL_BASE_GP_VALUE");
                var originalAdditionalGPValue = GetLocalInt(item, "ORIGINAL_ADDITIONAL_GP_VALUE");

                Core.NWNX.Item.SetBaseGoldPieceValue(item, originalBaseGPValue);
                Core.NWNX.Item.SetAddGoldPieceValue(item, originalAdditionalGPValue);

                DeleteLocalInt(item, "ORIGINAL_BASE_GP_VALUE");
                DeleteLocalInt(item, "ORIGINAL_ADDITIONAL_GP_VALUE");
            });
        }

    }
}
