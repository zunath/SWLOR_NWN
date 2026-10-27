using System.Collections.Generic;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Domain.Economy
{
    /// <summary>
    /// The global bank: items a player has stored under their account, shared by every bank terminal. The static
    /// facade <c>Service.Bank</c> forwards to the container's instance of this interface.
    /// </summary>
    public interface IBankService
    {
        /// <summary>
        /// Counts the items a player has in the bank.
        /// </summary>
        /// <param name="playerId">The id of the player whose bank is counted.</param>
        /// <returns>The number of banked items.</returns>
        long GetItemCount(string playerId);

        /// <summary>
        /// The filled share of the bank, from 0 to 1, for a number of banked items.
        /// </summary>
        float GetStoragePercentage(long itemCount);

        /// <summary>
        /// The "count / capacity Items" text shown for a number of banked items.
        /// </summary>
        string GetItemCountText(long itemCount);

        /// <summary>
        /// Whether a number of banked items fills the bank.
        /// </summary>
        bool IsFull(long itemCount);

        /// <summary>
        /// Lists a player's banked items, narrowed by name when search text is given.
        /// </summary>
        List<InventoryItem> SearchItems(string playerId, string searchText);

        /// <summary>
        /// Why an item cannot be deposited, or an empty string when it can.
        /// </summary>
        string GetDepositFailure(uint player, uint item);

        /// <summary>
        /// Stores an item in the player's bank and destroys the item.
        /// </summary>
        /// <returns>The stored record.</returns>
        InventoryItem DepositItem(uint player, uint item);

        /// <summary>
        /// Moves a banked item into the player's inventory. The bank keeps the item when it does not fit.
        /// </summary>
        /// <returns>An empty string on success, otherwise an error message to show the player.</returns>
        string WithdrawItem(uint player, string inventoryItemId);

        /// <summary>
        /// Ties a bank terminal to a city.
        /// </summary>
        void SetCityBankId(uint bank, string cityId);

        /// <summary>
        /// Moves a stored item to the global bank storage id.
        /// </summary>
        /// <returns>True when the item was changed.</returns>
        bool NormalizeStorageId(InventoryItem item);

        /// <summary>
        /// Why a player cannot use a city's bank terminal, or an empty string when they can.
        /// </summary>
        string GetCityBankAccessFailure(uint player, uint bank);
    }
}
