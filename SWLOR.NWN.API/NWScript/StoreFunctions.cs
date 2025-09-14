namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Returns the amount of gold a store currently has.
        /// -1 indicates it is not using gold.
        /// -2 indicates the store could not be located.
        /// </summary>
        /// <param name="oidStore">The store to get the gold amount for</param>
        /// <returns>The amount of gold the store has, or -1 if not using gold, or -2 if store not found</returns>
        public static int GetStoreGold(uint oidStore)
        {
            return global::NWN.Core.NWScript.GetStoreGold(oidStore);
        }

        /// <summary>
        /// Sets the amount of gold a store has.
        /// -1 means the store does not use gold.
        /// </summary>
        /// <param name="oidStore">The store to set the gold amount for</param>
        /// <param name="nGold">The amount of gold to set (-1 means the store does not use gold)</param>
        public static void SetStoreGold(uint oidStore, int nGold)
        {
            global::NWN.Core.NWScript.SetStoreGold(oidStore, nGold);
        }

        /// <summary>
        /// Gets the maximum amount a store will pay for any item.
        /// -1 means price unlimited.
        /// -2 indicates the store could not be located.
        /// </summary>
        /// <param name="oidStore">The store to get the max buy price for</param>
        /// <returns>The maximum buy price, or -1 if unlimited, or -2 if store not found</returns>
        public static int GetStoreMaxBuyPrice(uint oidStore)
        {
            return global::NWN.Core.NWScript.GetStoreMaxBuyPrice(oidStore);
        }

        /// <summary>
        /// Sets the maximum amount a store will pay for any item.
        /// -1 means price unlimited.
        /// </summary>
        /// <param name="oidStore">The store to set the max buy price for</param>
        /// <param name="nMaxBuy">The maximum amount the store will pay for any item (-1 means unlimited)</param>
        public static void SetStoreMaxBuyPrice(uint oidStore, int nMaxBuy)
        {
            global::NWN.Core.NWScript.SetStoreMaxBuyPrice(oidStore, nMaxBuy);
        }

        /// <summary>
        /// Gets the amount a store charges for identifying an item.
        /// Default is 100. -1 means the store will not identify items.
        /// -2 indicates the store could not be located.
        /// </summary>
        /// <param name="oidStore">The store to get the identify cost for</param>
        /// <returns>The identify cost, or -1 if store will not identify items, or -2 if store not found</returns>
        public static int GetStoreIdentifyCost(uint oidStore)
        {
            return global::NWN.Core.NWScript.GetStoreIdentifyCost(oidStore);
        }

        /// <summary>
        /// Sets the amount a store charges for identifying an item.
        /// Default is 100. -1 means the store will not identify items.
        /// </summary>
        /// <param name="oidStore">The store to set the identify cost for</param>
        /// <param name="nCost">The cost for identifying items (-1 means store will not identify items)</param>
        public static void SetStoreIdentifyCost(uint oidStore, int nCost)
        {
            global::NWN.Core.NWScript.SetStoreIdentifyCost(oidStore, nCost);
        }

        /// <summary>
        /// Opens the store for the player character.
        /// </summary>
        /// <param name="oStore">The store to open</param>
        /// <param name="oPC">The player character to open the store for</param>
        /// <param name="nBonusMarkUp">Added to the store's default mark up percentage on items sold (-100 to 100)</param>
        /// <param name="nBonusMarkDown">Added to the store's default mark down percentage on items bought (-100 to 100)</param>
        public static void OpenStore(uint oStore, uint oPC, int nBonusMarkUp = 0, int nBonusMarkDown = 0)
        {
            global::NWN.Core.NWScript.OpenStore(oStore, oPC, nBonusMarkUp, nBonusMarkDown);
        }
    }
}