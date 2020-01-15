using NWN;
using SWLOR.Game.Server.NWScript;
using static SWLOR.Game.Server.NWNX.NWNXCore;

namespace SWLOR.Game.Server.NWNX
{
    public static class NWNXItem
    {
        private const string NWNX_Item = "NWNX_Item";

        /// <summary>
        /// Set an item's weight
        /// Will not persist through saving
        /// </summary>
        /// <param name="oItem">The item object</param>
        /// <param name="weight">The weight, note this is in tenths of pounds</param>
        public static void SetWeight(NWGameObject oItem, int weight)
        {
            string sFunc = "SetWeight";

            NWNX_PushArgumentInt(NWNX_Item, sFunc, weight);
            NWNX_PushArgumentObject(NWNX_Item, sFunc, oItem);

            NWNX_CallFunction(NWNX_Item, sFunc);
        }

        /// <summary>
        /// Set an item's base value in gold pieces
        /// Total cost = base_value + additional_value
        /// Equivalent to SetGoldPieceValue NWNX2 function
        /// Will not persist through saving
        /// </summary>
        /// <param name="oItem">The item object.</param>
        /// <param name="gold">The base gold value.</param>
        public static void SetBaseGoldPieceValue(NWGameObject oItem, int gold)
        {
            string sFunc = "SetBaseGoldPieceValue";

            NWNX_PushArgumentInt(NWNX_Item, sFunc, gold);
            NWNX_PushArgumentObject(NWNX_Item, sFunc, oItem);

            NWNX_CallFunction(NWNX_Item, sFunc);
        }

        /// <summary>
        /// Set an item's additional value in gold pieces
        /// Total cost = base_value + additional_value
        /// Will persist through saving
        /// </summary>
        /// <param name="oItem">The item object.</param>
        /// <param name="gold">The additional gold value</param>
        public static void SetAddGoldPieceValue(NWGameObject oItem, int gold)
        {
            string sFunc = "SetAddGoldPieceValue";

            NWNX_PushArgumentInt(NWNX_Item, sFunc, gold);
            NWNX_PushArgumentObject(NWNX_Item, sFunc, oItem);

            NWNX_CallFunction(NWNX_Item, sFunc);
        }

        /// <summary>
        /// Get an item's base value in gold pieces.
        /// </summary>
        /// <param name="oItem">The item object.</param>
        /// <returns>The base gold piece value for the item</returns>
        public static int GetBaseGoldPieceValue(NWGameObject oItem)
        {
            string sFunc = "GetBaseGoldPieceValue";

            NWNX_PushArgumentObject(NWNX_Item, sFunc, oItem);

            NWNX_CallFunction(NWNX_Item, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Item, sFunc);
        }

        /// <summary>
        /// Get an item's additional value in gold pieces
        /// </summary>
        /// <param name="oItem">The item object</param>
        /// <returns>The additional gold piece value for the item.</returns>
        public static int GetAddGoldPieceValue(NWGameObject oItem)
        {
            string sFunc = "GetAddGoldPieceValue";

            NWNX_PushArgumentObject(NWNX_Item, sFunc, oItem);

            NWNX_CallFunction(NWNX_Item, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Item, sFunc);
        }

        /// <summary>
        /// Set an item's base item type
        /// This will not be visible until the item is refreshed (e.g. drop and take the item,
        /// or logging out and back in).
        /// </summary>
        /// <param name="oItem">The item object.</param>
        /// <param name="nBaseItem">The new base item</param>
        public static void SetBaseItemType(NWGameObject oItem, int nBaseItem)
        {
            string sFunc = "SetBaseItemType";

            NWNX_PushArgumentInt(NWNX_Item, sFunc, nBaseItem);
            NWNX_PushArgumentObject(NWNX_Item, sFunc, oItem);

            NWNX_CallFunction(NWNX_Item, sFunc);
        }

        /// <summary>
        /// Make a single change to the appearance of an item. This will not be visible to PCs until
        /// the item is refreshed for them (e.g. by logging out and back in).
        /// Helmet models and simple items ignore iIndex.
        /// nType                            nIndex                              nValue
        /// ITEM_APPR_TYPE_SIMPLE_MODEL      [Ignored]                           Model #
        /// ITEM_APPR_TYPE_WEAPON_COLOR      ITEM_APPR_WEAPON_COLOR_*            0-255
        /// ITEM_APPR_TYPE_WEAPON_MODEL      ITEM_APPR_WEAPON_MODEL_*            Model #
        /// ItemApprType.ArmorModel       ITEM_APPR_ARMOR_MODEL_*             Model #
        /// ItemApprType.ArmorColor       ITEM_APPR_ARMOR_COLOR_* [0]         0-255 [1]
        ///
        /// [0] Alternatively, where ItemApprType.ArmorColor is specified, if per-part coloring is
        /// desired, the following equation can be used for nIndex to achieve that:
        ///
        /// ITEM_APPR_ARMOR_NUM_COLORS + (ITEM_APPR_ARMOR_MODEL_ * ITEM_APPR_ARMOR_NUM_COLORS) + ITEM_APPR_ARMOR_COLOR_
        ///
        /// For example, to change the CLOTH1 channel of the torso, nIndex would be:
        ///
        ///     6 + (7 * 6) + 2 = 50
        ///
        /// [1] When specifying per-part coloring, the value 255 corresponds with the logical
        /// function 'clear colour override', which clears the per-part override for that part.
        /// </summary>
        /// <param name="oItem">The item</param>
        /// <param name="nType">The type to use</param>
        /// <param name="nIndex">The index</param>
        /// <param name="nValue">The new value</param>
        public static void SetItemAppearance(NWGameObject oItem, int nType, int nIndex, int nValue)
        {
            string sFunc = "SetItemAppearance";

            NWNX_PushArgumentInt(NWNX_Item, sFunc, nValue);
            NWNX_PushArgumentInt(NWNX_Item, sFunc, nIndex);
            NWNX_PushArgumentInt(NWNX_Item, sFunc, nType);
            NWNX_PushArgumentObject(NWNX_Item, sFunc, oItem);

            NWNX_CallFunction(NWNX_Item, sFunc);

        }

        /// <summary>
        /// Return a string containing the entire appearance for an item.
        /// </summary>
        /// <param name="oItem">The item object</param>
        /// <returns>A string representing the item's appearance.</returns>
        public static string GetEntireItemAppearance(NWGameObject oItem)
        {
            string sFunc = "GetEntireItemAppearance";

            NWNX_PushArgumentObject(NWNX_Item, sFunc, oItem);

            NWNX_CallFunction(NWNX_Item, sFunc);
            return NWNX_GetReturnValueString(NWNX_Item, sFunc);
        }

        /// <summary>
        /// Restore an item's appearance with the value returned by GetEntireItemAppearance().
        /// </summary>
        /// <param name="oItem">The item to restore</param>
        /// <param name="sApp">A string representing the item's appearance.</param>
        public static void RestoreItemAppearance(NWGameObject oItem, string sApp)
        {
            string sFunc = "RestoreItemAppearance";

            NWNX_PushArgumentString(NWNX_Item, sFunc, sApp);
            NWNX_PushArgumentObject(NWNX_Item, sFunc, oItem);

            NWNX_CallFunction(NWNX_Item, sFunc);
        }

        /// <summary>
        /// Get an item's base armor class
        /// </summary>
        /// <param name="oItem">The item object.</param>
        /// <returns>The base armor class</returns>
        public static int GetBaseArmorClass(NWGameObject oItem)
        {
            string sFunc = "GetBaseArmorClass";

            NWNX_PushArgumentObject(NWNX_Item, sFunc, oItem);

            NWNX_CallFunction(NWNX_Item, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Item, sFunc);
        }


        /// <summary>
        /// Get an item's minimum level required to equip.
        /// </summary>
        /// <param name="oItem">The item object</param>
        /// <returns>The minimum level required to equip the item.</returns>
        public static int GetMinEquipLevel(NWGameObject oItem)
        {
            string sFunc = "GetMinEquipLevel";

            NWNX_PushArgumentObject(NWNX_Item, sFunc, oItem);

            NWNX_CallFunction(NWNX_Item, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Item, sFunc);
        }
    }
}
