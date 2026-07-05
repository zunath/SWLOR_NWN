using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Service
{
    public static class Bank
    {
        private const string StorageId = "GLOBAL_BANK";
        private const int MaxItems = 120;
        private const string CityIdLocalName = "CITY_ID";

        public static long GetItemCount(string playerId)
        {
            return DB.SearchCount(BuildPlayerItemQuery(playerId));
        }

        public static float GetStoragePercentage(long itemCount)
        {
            return itemCount >= MaxItems
                ? 1f
                : (float)itemCount / MaxItems;
        }

        public static string GetItemCountText(long itemCount)
        {
            return $"{itemCount} / {MaxItems} Items";
        }

        public static bool IsFull(long itemCount)
        {
            return itemCount >= MaxItems;
        }

        public static List<InventoryItem> SearchItems(string playerId, string searchText)
        {
            var query = BuildPlayerItemQuery(playerId);

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query.AddFieldSearch(nameof(InventoryItem.Name), searchText, true);
            }

            return DB.Search(query).ToList();
        }

        public static string GetDepositFailure(uint player, uint item)
        {
            var canStore = Item.CanBePersistentlyStored(player, item);
            if (!string.IsNullOrWhiteSpace(canStore))
                return canStore;

            var playerId = GetObjectUUID(player);
            return IsFull(GetItemCount(playerId))
                ? "Your bank is full."
                : string.Empty;
        }

        public static InventoryItem DepositItem(uint player, uint item)
        {
            var dbItem = new InventoryItem
            {
                StorageId = StorageId,
                PlayerId = GetObjectUUID(player),
                Name = GetName(item),
                Tag = GetTag(item),
                Resref = GetResRef(item),
                Quantity = GetItemStackSize(item),
                Data = ObjectPlugin.Serialize(item),
                IconResref = Item.GetIconResref(item)
            };

            DB.Set(dbItem);
            Log.WriteStructured(
                LogGroup.Bank,
                "Bank deposit: PlayerId={PlayerId} InventoryItemId={InventoryItemId} Resref={Resref} Quantity={Quantity}",
                dbItem.PlayerId,
                dbItem.Id,
                dbItem.Resref,
                dbItem.Quantity);
            DestroyObject(item);

            return dbItem;
        }

        public static void WithdrawItem(uint player, string inventoryItemId)
        {
            var dbItem = DB.Get<InventoryItem>(inventoryItemId);
            var playerId = GetObjectUUID(player);

            if (dbItem == null || dbItem.PlayerId != playerId || dbItem.StorageId != StorageId)
                return;

            var item = ObjectPlugin.Deserialize(dbItem.Data);

            ObjectPlugin.AcquireItem(player, item);
            DB.Delete<InventoryItem>(dbItem.Id);
            Log.WriteStructured(
                LogGroup.Bank,
                "Bank withdrawal: PlayerId={PlayerId} InventoryItemId={InventoryItemId} Resref={Resref} Quantity={Quantity}",
                playerId,
                dbItem.Id,
                dbItem.Resref,
                dbItem.Quantity);
            RemoveLegacyItemProperties(item);
        }

        public static void SetCityBankId(uint bank, string cityId)
        {
            SetLocalString(bank, CityIdLocalName, cityId);
        }

        public static bool NormalizeStorageId(InventoryItem item)
        {
            if (item.StorageId == StorageId)
                return false;

            item.StorageId = StorageId;
            return true;
        }

        public static string GetCityBankAccessFailure(uint player, uint bank)
        {
            if (!GetIsPC(player) || GetIsDM(player))
                return "Only players can access this bank.";

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var cityId = GetLocalString(bank, CityIdLocalName);

            if (string.IsNullOrWhiteSpace(cityId))
                return "This bank terminal is not configured for a city.";

            if (dbPlayer.CitizenPropertyId != cityId)
                return "Only citizens may use this terminal.";

            return dbPlayer.PropertyOwedTaxes > 0
                ? $"You owe {dbPlayer.PropertyOwedTaxes} credits in taxes to this city. You cannot use its facilities until these are paid. Use the Citizenship Terminal in City Hall to pay these."
                : string.Empty;
        }

        private static DBQuery<InventoryItem> BuildPlayerItemQuery(string playerId)
        {
            return new DBQuery<InventoryItem>()
                .AddFieldSearch(nameof(InventoryItem.StorageId), StorageId, false)
                .AddFieldSearch(nameof(InventoryItem.PlayerId), playerId, false);
        }

        private static void RemoveLegacyItemProperties(uint item)
        {
            if (!Item.IsLegacyItem(item))
                return;

            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                RemoveItemProperty(item, ip);
            }
        }
    }
}
