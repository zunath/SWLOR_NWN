using SWLOR.Game.Server.Core.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Core.NWNX
{
    public static class ItemPlugin
    {
        private const string PLUGIN_NAME = "NWNX_Item";

        // Set oItem's weight. Will not persist through saving.
        public static void SetWeight(uint oItem, int w)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetWeight");
            NWNXPInvoke.NWNXPushInt(w);
            NWNXPInvoke.NWNXPushObject(oItem);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Set oItem's base value in gold pieces (Total cost = base_value +
        // additional_value). Will not persist through saving.
        // NOTE: Equivalent to SetGoldPieceValue NWNX2 function
        public static void SetBaseGoldPieceValue(uint oItem, int g)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetBaseGoldPieceValue");
            NWNXPInvoke.NWNXPushInt(g);
            NWNXPInvoke.NWNXPushObject(oItem);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Set oItem's additional value in gold pieces (Total cost = base_value +
        // additional_value). Will persist through saving.
        public static void SetAddGoldPieceValue(uint oItem, int g)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetAddGoldPieceValue");
            NWNXPInvoke.NWNXPushInt(g);
            NWNXPInvoke.NWNXPushObject(oItem);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Get oItem's base value in gold pieces.
        public static int GetBaseGoldPieceValue(uint oItem)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetBaseGoldPieceValue");
            NWNXPInvoke.NWNXPushObject(oItem);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopInt();
        }

        // Get oItem's additional value in gold pieces.
        public static int GetAddGoldPieceValue(uint oItem)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetAddGoldPieceValue");
            NWNXPInvoke.NWNXPushObject(oItem);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopInt();
        }

        // Set oItem's base item type. This will not be visible until the
        // item is refreshed (e.g. drop and take the item, or logging out
        // and back in).
        public static void SetBaseItemType(uint oItem, BaseItem baseitem)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetBaseItemType");
            NWNXPInvoke.NWNXPushInt((int)baseitem);
            NWNXPInvoke.NWNXPushObject(oItem);
            NWNXPInvoke.NWNXCallFunction();
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
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetItemAppearance");
            NWNXPInvoke.NWNXPushInt(nValue);
            NWNXPInvoke.NWNXPushInt(nIndex);
            NWNXPInvoke.NWNXPushInt((int)nType);
            NWNXPInvoke.NWNXPushObject(oItem);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Return a String containing the entire appearance for oItem which can later be
        // passed to RestoreItemAppearance().
        public static string GetEntireItemAppearance(uint oItem)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetEntireItemAppearance");
            NWNXPInvoke.NWNXPushObject(oItem);
            NWNXPInvoke.NWNXCallFunction();
            return NWNCore.NativeFunctions.nwnxPopString();
        }

        // Restore an item's appearance with the value returned by GetEntireItemAppearance().
        public static void RestoreItemAppearance(uint oItem, string sApp)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "RestoreItemAppearance");
            NWNCore.NativeFunctions.nwnxPushString(sApp);
            NWNXPInvoke.NWNXPushObject(oItem);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Get oItem's base armor class
        public static int GetBaseArmorClass(uint oItem)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetBaseArmorClass");
            NWNXPInvoke.NWNXPushObject(oItem);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopInt();
        }

        // Get oItem's minimum level needed to equip
        public static int GetMinEquipLevel(uint oItem)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetMinEquipLevel");
            NWNXPInvoke.NWNXPushObject(oItem);
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopInt();
        }
    }
}