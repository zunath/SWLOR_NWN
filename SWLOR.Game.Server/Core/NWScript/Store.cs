namespace SWLOR.Game.Server.Core.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   Returns the amount of gold a store currently has. -1 indicates it is not using gold.
        ///   -2 indicates the store could not be located.
        /// </summary>
        public static int GetStoreGold(uint oidStore)
        {
            Internal.NativeFunctions.StackPushObject(oidStore);
            Internal.NativeFunctions.CallBuiltIn(759);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Sets the amount of gold a store has. -1 means the store does not use gold.
        /// </summary>
        public static void SetStoreGold(uint oidStore, int nGold)
        {
            Internal.NativeFunctions.StackPushInteger(nGold);
            Internal.NativeFunctions.StackPushObject(oidStore);
            Internal.NativeFunctions.CallBuiltIn(760);
        }

        /// <summary>
        ///   Gets the maximum amount a store will pay for any item. -1 means price unlimited.
        ///   -2 indicates the store could not be located.
        /// </summary>
        public static int GetStoreMaxBuyPrice(uint oidStore)
        {
            Internal.NativeFunctions.StackPushObject(oidStore);
            Internal.NativeFunctions.CallBuiltIn(761);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Sets the maximum amount a store will pay for any item. -1 means price unlimited.
        /// </summary>
        public static void SetStoreMaxBuyPrice(uint oidStore, int nMaxBuy)
        {
            Internal.NativeFunctions.StackPushInteger(nMaxBuy);
            Internal.NativeFunctions.StackPushObject(oidStore);
            Internal.NativeFunctions.CallBuiltIn(762);
        }

        /// <summary>
        ///   Gets the amount a store charges for identifying an item. Default is 100. -1 means
        ///   the store will not identify items.
        ///   -2 indicates the store could not be located.
        /// </summary>
        public static int GetStoreIdentifyCost(uint oidStore)
        {
            Internal.NativeFunctions.StackPushObject(oidStore);
            Internal.NativeFunctions.CallBuiltIn(763);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Sets the amount a store charges for identifying an item. Default is 100. -1 means
        ///   the store will not identify items.
        /// </summary>
        public static void SetStoreIdentifyCost(uint oidStore, int nCost)
        {
            Internal.NativeFunctions.StackPushInteger(nCost);
            Internal.NativeFunctions.StackPushObject(oidStore);
            Internal.NativeFunctions.CallBuiltIn(764);
        }

        /// <summary>
        ///   Open oStore for oPC.
        ///   - nBonusMarkUp is added to the stores default mark up percentage on items sold (-100 to 100)
        ///   - nBonusMarkDown is added to the stores default mark down percentage on items bought (-100 to 100)
        /// </summary>
        public static void OpenStore(uint oStore, uint oPC, int nBonusMarkUp = 0, int nBonusMarkDown = 0)
        {
            Internal.NativeFunctions.StackPushInteger(nBonusMarkDown);
            Internal.NativeFunctions.StackPushInteger(nBonusMarkUp);
            Internal.NativeFunctions.StackPushObject(oPC);
            Internal.NativeFunctions.StackPushObject(oStore);
            Internal.NativeFunctions.CallBuiltIn(378);
        }
    }
}