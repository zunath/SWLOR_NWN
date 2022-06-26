using SWLOR.Game.Server.Core.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Core.NWNX
{
    public static class ItemPlugin
    {
        private const string PLUGIN_NAME = "NWNX_Item";

        // Set oItem's weight. Will not persist through saving.
        public static void SetWeight(uint oItem, int w)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetWeight");
            NWNCore.NativeFunctions.nwnxPushInt(w);
            NWNCore.NativeFunctions.nwnxPushObject(oItem);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Set oItem's base value in gold pieces (Total cost = base_value +
        // additional_value). Will not persist through saving.
        // NOTE: Equivalent to SetGoldPieceValue NWNX2 function
        public static void SetBaseGoldPieceValue(uint oItem, int g)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetBaseGoldPieceValue");
            NWNCore.NativeFunctions.nwnxPushInt(g);
            NWNCore.NativeFunctions.nwnxPushObject(oItem);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Set oItem's additional value in gold pieces (Total cost = base_value +
        // additional_value). Will persist through saving.
        public static void SetAddGoldPieceValue(uint oItem, int g)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetAddGoldPieceValue");
            NWNCore.NativeFunctions.nwnxPushInt(g);
            NWNCore.NativeFunctions.nwnxPushObject(oItem);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Get oItem's base value in gold pieces.
        public static int GetBaseGoldPieceValue(uint oItem)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetBaseGoldPieceValue");
            NWNCore.NativeFunctions.nwnxPushObject(oItem);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        // Get oItem's additional value in gold pieces.
        public static int GetAddGoldPieceValue(uint oItem)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetAddGoldPieceValue");
            NWNCore.NativeFunctions.nwnxPushObject(oItem);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        // Set oItem's base item type. This will not be visible until the
        // item is refreshed (e.g. drop and take the item, or logging out
        // and back in).
        public static void SetBaseItemType(uint oItem, BaseItem baseitem)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetBaseItemType");
            NWNCore.NativeFunctions.nwnxPushInt((int)baseitem);
            NWNCore.NativeFunctions.nwnxPushObject(oItem);
            NWNCore.NativeFunctions.nwnxCallFunction();
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
        public static void SetItemAppearance(uint oItem, ItemAppearanceType nType, int nIndex, int nValue)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetItemAppearance");
            NWNCore.NativeFunctions.nwnxPushInt(nValue);
            NWNCore.NativeFunctions.nwnxPushInt(nIndex);
            NWNCore.NativeFunctions.nwnxPushInt((int)nType);
            NWNCore.NativeFunctions.nwnxPushObject(oItem);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Return a String containing the entire appearance for oItem which can later be
        // passed to RestoreItemAppearance().
        public static string GetEntireItemAppearance(uint oItem)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetEntireItemAppearance");
            NWNCore.NativeFunctions.nwnxPushObject(oItem);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopString();
        }

        // Restore an item's appearance with the value returned by GetEntireItemAppearance().
        public static void RestoreItemAppearance(uint oItem, string sApp)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "RestoreItemAppearance");
            NWNCore.NativeFunctions.nwnxPushString(sApp);
            NWNCore.NativeFunctions.nwnxPushObject(oItem);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Get oItem's base armor class
        public static int GetBaseArmorClass(uint oItem)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetBaseArmorClass");
            NWNCore.NativeFunctions.nwnxPushObject(oItem);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        // Get oItem's minimum level needed to equip
        public static int GetMinEquipLevel(uint oItem)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetMinEquipLevel");
            NWNCore.NativeFunctions.nwnxPushObject(oItem);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }
    }
}