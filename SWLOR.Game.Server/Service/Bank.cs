using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Economy;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the global bank. The logic lives in <see cref="IBankService"/>; every member here forwards to
    /// the container's instance.
    /// </summary>
    public static class Bank
    {
        private static IBankService Instance => FacadeAccessor.Resolve<IBankService>();

        public static long GetItemCount(string playerId)
        {
            return Instance.GetItemCount(playerId);
        }

        public static float GetStoragePercentage(long itemCount)
        {
            return Instance.GetStoragePercentage(itemCount);
        }

        public static string GetItemCountText(long itemCount)
        {
            return Instance.GetItemCountText(itemCount);
        }

        public static bool IsFull(long itemCount)
        {
            return Instance.IsFull(itemCount);
        }

        public static List<InventoryItem> SearchItems(string playerId, string searchText)
        {
            return Instance.SearchItems(playerId, searchText);
        }

        public static string GetDepositFailure(uint player, uint item)
        {
            return Instance.GetDepositFailure(player, item);
        }

        public static InventoryItem DepositItem(uint player, uint item)
        {
            return Instance.DepositItem(player, item);
        }

        /// <summary>
        /// Moves a banked item into the player's inventory. The bank keeps the item when it does not fit.
        /// </summary>
        /// <returns>An empty string on success, otherwise an error message to show the player.</returns>
        public static string WithdrawItem(uint player, string inventoryItemId)
        {
            return Instance.WithdrawItem(player, inventoryItemId);
        }

        public static void SetCityBankId(uint bank, string cityId)
        {
            Instance.SetCityBankId(bank, cityId);
        }

        public static bool NormalizeStorageId(InventoryItem item)
        {
            return Instance.NormalizeStorageId(item);
        }

        public static string GetCityBankAccessFailure(uint player, uint bank)
        {
            return Instance.GetCityBankAccessFailure(player, bank);
        }
    }
}
