using SWLOR.Game.Server.Core.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Core.NWNX
{
    public static class Item
    {
        private const string PLUGIN_NAME = "NWNX_Item";

        // Set oItem's weight. Will not persist through saving.
        public static void SetWeight(uint oItem, int w)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetWeight");
            Internal.NativeFunctions.nwnxPushInt(w);
            Internal.NativeFunctions.nwnxPushObject(oItem);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Set oItem's base value in gold pieces (Total cost = base_value +
        // additional_value). Will not persist through saving.
        // NOTE: Equivalent to SetGoldPieceValue NWNX2 function
        public static void SetBaseGoldPieceValue(uint oItem, int g)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetBaseGoldPieceValue");
            Internal.NativeFunctions.nwnxPushInt(g);
            Internal.NativeFunctions.nwnxPushObject(oItem);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Set oItem's additional value in gold pieces (Total cost = base_value +
        // additional_value). Will persist through saving.
        public static void SetAddGoldPieceValue(uint oItem, int g)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetAddGoldPieceValue");
            Internal.NativeFunctions.nwnxPushInt(g);
            Internal.NativeFunctions.nwnxPushObject(oItem);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Get oItem's base value in gold pieces.
        public static int GetBaseGoldPieceValue(uint oItem)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetBaseGoldPieceValue");
            Internal.NativeFunctions.nwnxPushObject(oItem);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        // Get oItem's additional value in gold pieces.
        public static int GetAddGoldPieceValue(uint oItem)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetAddGoldPieceValue");
            Internal.NativeFunctions.nwnxPushObject(oItem);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        // Set oItem's base item type. This will not be visible until the
        // item is refreshed (e.g. drop and take the item, or logging out
        // and back in).
        public static void SetBaseItemType(uint oItem, BaseItem baseitem)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetBaseItemType");
            Internal.NativeFunctions.nwnxPushInt((int)baseitem);
            Internal.NativeFunctions.nwnxPushObject(oItem);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Make a single change to the appearance of an item. This will not be visible to PCs until
        // the item is refreshed for them (e.g. by logging out and back in).
        // Helmet models and simple items ignore iIndex.
        // nType                            nIndex                              nValue
        // ITEM_APPR_TYPE_SIMPLE_MODEL      [Ignored]                           Model #
        // ITEM_APPR_TYPE_WEAPON_COLOR      ITEM_APPR_WEAPON_COLOR_*            0-255
        // ITEM_APPR_TYPE_WEAPON_MODEL      ITEM_APPR_WEAPON_MODEL_*            Model #
        // ITEM_APPR_TYPE_ARMOR_MODEL       ITEM_APPR_ARMOR_MODEL_*             Model #
        // ITEM_APPR_TYPE_ARMOR_COLOR       ITEM_APPR_ARMOR_COLOR_* [0]         0-255 [1]
        //
        // [0] Alternatively, where ITEM_APPR_TYPE_ARMOR_COLOR is specified, if per-part coloring is
        // desired, the following equation can be used for nIndex to achieve that:
        //
        // ITEM_APPR_ARMOR_NUM_COLORS + (ITEM_APPR_ARMOR_MODEL_ * ITEM_APPR_ARMOR_NUM_COLORS) + ITEM_APPR_ARMOR_COLOR_
        //
        // For example, to change the CLOTH1 channel of the torso, nIndex would be:
        //
        //     6 + (7 * 6) + 2 = 50
        //
        // [1] When specifying per-part coloring, the value 255 corresponds with the logical
        // function 'clear colour override', which clears the per-part override for that part.
        public static void SetItemAppearance(uint oItem, int nType, int nIndex, int nValue)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetItemAppearance");
            Internal.NativeFunctions.nwnxPushInt(nValue);
            Internal.NativeFunctions.nwnxPushInt(nIndex);
            Internal.NativeFunctions.nwnxPushInt(nType);
            Internal.NativeFunctions.nwnxPushObject(oItem);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Return a String containing the entire appearance for oItem which can later be
        // passed to RestoreItemAppearance().
        public static string GetEntireItemAppearance(uint oItem)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetEntireItemAppearance");
            Internal.NativeFunctions.nwnxPushObject(oItem);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopString();
        }

        // Restore an item's appearance with the value returned by GetEntireItemAppearance().
        public static void RestoreItemAppearance(uint oItem, string sApp)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "RestoreItemAppearance");
            Internal.NativeFunctions.nwnxPushString(sApp);
            Internal.NativeFunctions.nwnxPushObject(oItem);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Get oItem's base armor class
        public static int GetBaseArmorClass(uint oItem)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetBaseArmorClass");
            Internal.NativeFunctions.nwnxPushObject(oItem);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        // Get oItem's minimum level needed to equip
        public static int GetMinEquipLevel(uint oItem)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetMinEquipLevel");
            Internal.NativeFunctions.nwnxPushObject(oItem);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }
    }
}