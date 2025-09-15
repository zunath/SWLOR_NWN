using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.NWN.API.NWNX
{
    public static class ItemPlugin
    {
        /// <summary>
        /// Set an item's weight.
        /// </summary>
        /// <param name="oItem">The item object.</param>
        /// <param name="w">The weight, note this is in tenths of pounds.</param>
        /// <remarks>Will not persist through saving.</remarks>
        public static void SetWeight(uint oItem, int w)
        {
            global::NWN.Core.NWNX.ItemPlugin.SetWeight(oItem, w);
        }

        /// <summary>
        /// Set an item's base value in gold pieces.
        /// </summary>
        /// <param name="oItem">The item object.</param>
        /// <param name="g">The base gold value.</param>
        /// <remarks>Total cost = base_value + additional_value. Will not persist through saving. This value will also revert if item is identified or player relogs into server. Equivalent to SetGoldPieceValue NWNX2 function.</remarks>
        public static void SetBaseGoldPieceValue(uint oItem, int g)
        {
            global::NWN.Core.NWNX.ItemPlugin.SetBaseGoldPieceValue(oItem, g);
        }

        /// <summary>
        /// Set an item's additional value in gold pieces.
        /// </summary>
        /// <param name="oItem">The item object.</param>
        /// <param name="g">The additional gold value.</param>
        /// <remarks>Total cost = base_value + additional_value. Will persist through saving.</remarks>
        public static void SetAddGoldPieceValue(uint oItem, int g)
        {
            global::NWN.Core.NWNX.ItemPlugin.SetAddGoldPieceValue(oItem, g);
        }

        /// <summary>
        /// Get an item's base value in gold pieces.
        /// </summary>
        /// <param name="oItem">The item object.</param>
        /// <returns>The base gold piece value for the item.</returns>
        public static int GetBaseGoldPieceValue(uint oItem)
        {
            return global::NWN.Core.NWNX.ItemPlugin.GetBaseGoldPieceValue(oItem);
        }

        /// <summary>
        /// Get an item's additional value in gold pieces.
        /// </summary>
        /// <param name="oItem">The item object.</param>
        /// <returns>The additional gold piece value for the item.</returns>
        public static int GetAddGoldPieceValue(uint oItem)
        {
            return global::NWN.Core.NWNX.ItemPlugin.GetAddGoldPieceValue(oItem);
        }

        /// <summary>
        /// Set an item's base item type.
        /// </summary>
        /// <param name="oItem">The item object.</param>
        /// <param name="baseitem">The new base item.</param>
        /// <remarks>This will not be visible until the item is refreshed (e.g. drop and take the item, or logging out and back in).</remarks>
        public static void SetBaseItemType(uint oItem, BaseItem baseitem)
        {
            global::NWN.Core.NWNX.ItemPlugin.SetBaseItemType(oItem, (int)baseitem);
        }

        /// <summary>
        /// Make a single change to the appearance of an item.
        /// </summary>
        /// <param name="oItem">The item</param>
        /// <param name="nType">The type</param>
        /// <param name="nIndex">The index</param>
        /// <param name="nValue">The value</param>
        /// <param name="updateCreatureAppearance">If true, also update the appearance of oItem's possessor. Only works for armor/helmets/cloaks. Will remove the item from the quickbar as side effect.</param>
        /// <remarks>
        /// This will not be visible to PCs until the item is refreshed for them (e.g. by logging out and back in).
        /// Helmet models and simple items ignore nIndex.
        /// 
        /// nType                            nIndex                              nValue
        /// ITEM_APPR_TYPE_SIMPLE_MODEL      [Ignored]                           Model #
        /// ITEM_APPR_TYPE_WEAPON_COLOR      ITEM_APPR_WEAPON_COLOR_*            0-255
        /// ITEM_APPR_TYPE_WEAPON_MODEL      ITEM_APPR_WEAPON_MODEL_*            Model #
        /// ITEM_APPR_TYPE_ARMOR_MODEL       ITEM_APPR_ARMOR_MODEL_*             Model #
        /// ITEM_APPR_TYPE_ARMOR_COLOR       ITEM_APPR_ARMOR_COLOR_* [0]         0-255 [1]
        /// 
        /// [0] Where ITEM_APPR_TYPE_ARMOR_COLOR is specified, if per-part coloring is desired, the following equation can be used for nIndex to achieve that:
        /// ITEM_APPR_ARMOR_NUM_COLORS + (ITEM_APPR_ARMOR_MODEL_ * ITEM_APPR_ARMOR_NUM_COLORS) + ITEM_APPR_ARMOR_COLOR_
        /// 
        /// For example, to change the CLOTH1 channel of the torso, nIndex would be: 6 + (7 * 6) + 2 = 50
        /// 
        /// [1] When specifying per-part coloring, the value 255 corresponds with the logical function 'clear colour override', which clears the per-part override for that part.
        /// </remarks>
        public static void SetItemAppearance(uint oItem, ItemAppearanceType nType, int nIndex, int nValue, bool updateCreatureAppearance = false)
        {
            global::NWN.Core.NWNX.ItemPlugin.SetItemAppearance(oItem, (int)nType, nIndex, nValue, updateCreatureAppearance ? 1 : 0);
        }

        /// <summary>
        /// Return a string containing the entire appearance for an item.
        /// </summary>
        /// <param name="oItem">The item object.</param>
        /// <returns>A string representing the item's appearance.</returns>
        public static string GetEntireItemAppearance(uint oItem)
        {
            return global::NWN.Core.NWNX.ItemPlugin.GetEntireItemAppearance(oItem);
        }

        /// <summary>
        /// Restores an item's appearance using the value retrieved through GetEntireItemAppearance().
        /// </summary>
        /// <param name="oItem">The item object.</param>
        /// <param name="sApp">A string representing the item's appearance.</param>
        public static void RestoreItemAppearance(uint oItem, string sApp)
        {
            global::NWN.Core.NWNX.ItemPlugin.RestoreItemAppearance(oItem, sApp);
        }

        /// <summary>
        /// Get an item's base armor class.
        /// </summary>
        /// <param name="oItem">The item object.</param>
        /// <returns>The base armor class.</returns>
        public static int GetBaseArmorClass(uint oItem)
        {
            return global::NWN.Core.NWNX.ItemPlugin.GetBaseArmorClass(oItem);
        }

        /// <summary>
        /// Get an item's minimum level required to equip.
        /// </summary>
        /// <param name="oItem">The item object.</param>
        /// <returns>The minimum level required to equip the item.</returns>
        public static int GetMinEquipLevel(uint oItem)
        {
            return global::NWN.Core.NWNX.ItemPlugin.GetMinEquipLevel(oItem);
        }

        /// <summary>
        /// Move an item to a specific location.
        /// </summary>
        /// <param name="oItem">The item object to move.</param>
        /// <param name="oLocation">The destination location (area, container, or creature).</param>
        /// <param name="bHideAllFeedback">If true, hides all feedback messages generated by losing/acquiring items.</param>
        /// <returns>True if the item was successfully moved, false otherwise.</returns>
        /// <remarks>
        /// This function moves an item to a specific location in the game world.
        /// The destination can be an area, container, or creature inventory.
        /// Moving items from a container to the inventory of the container's owner (or the other way around) is always "silent" and won't trigger feedback messages.
        /// </remarks>
        public static bool MoveTo(uint oItem, uint oLocation, bool bHideAllFeedback = false)
        {
            var result = global::NWN.Core.NWNX.ItemPlugin.MoveTo(oItem, oLocation, bHideAllFeedback ? 1 : 0);
            return result != 0;
        }

        /// <summary>
        /// Set an item's minimum equip level modifier.
        /// </summary>
        /// <param name="oItem">The item object.</param>
        /// <param name="nModifier">The modifier value to add to the minimum equip level.</param>
        /// <param name="bPersist">Whether the modifier should persist to gff field. Strongly recommended to be true.</param>
        /// <remarks>
        /// This adds a modifier to the item's minimum equip level requirement.
        /// The total minimum level is the base level plus this modifier.
        /// This function must be used each server reset to reenable persistence. Recommended use on OBJECT_INVALID OnModuleLoad.
        /// If persistence is false, or not re-enabled, beware characters may trigger ELC logging in with now-invalid ItemLevelRestrictions equipped.
        /// </remarks>
        public static void SetMinEquipLevelModifier(uint oItem, int nModifier, bool bPersist = true)
        {
            global::NWN.Core.NWNX.ItemPlugin.SetMinEquipLevelModifier(oItem, nModifier, bPersist ? 1 : 0);
        }

        /// <summary>
        /// Get an item's minimum equip level modifier.
        /// </summary>
        /// <param name="oItem">The item object.</param>
        /// <returns>The current modifier value for the minimum equip level.</returns>
        /// <remarks>
        /// This returns the current modifier applied to the item's minimum equip level.
        /// The total minimum level is the base level plus this modifier.
        /// </remarks>
        public static int GetMinEquipLevelModifier(uint oItem)
        {
            return global::NWN.Core.NWNX.ItemPlugin.GetMinEquipLevelModifier(oItem);
        }

        /// <summary>
        /// Set an item's minimum equip level override.
        /// </summary>
        /// <param name="oItem">The item object.</param>
        /// <param name="nOverride">The override value for the minimum equip level.</param>
        /// <param name="bPersist">Whether the override should persist to gff field. Strongly recommended to be true.</param>
        /// <remarks>
        /// This sets an override value for the item's minimum equip level requirement.
        /// When set, this value completely replaces the base minimum level calculation.
        /// This function must be used each server reset to reenable persistence. Recommended use on OBJECT_INVALID OnModuleLoad.
        /// If persistence is false, or not re-enabled, beware characters may trigger ELC logging in with now-invalid ItemLevelRestrictions equipped.
        /// </remarks>
        public static void SetMinEquipLevelOverride(uint oItem, int nOverride, bool bPersist = true)
        {
            global::NWN.Core.NWNX.ItemPlugin.SetMinEquipLevelOverride(oItem, nOverride, bPersist ? 1 : 0);
        }

        /// <summary>
        /// Get an item's minimum equip level override.
        /// </summary>
        /// <param name="oItem">The item object.</param>
        /// <returns>The current override value for the minimum equip level, or -1 if no override is set.</returns>
        /// <remarks>
        /// This returns the current override value for the item's minimum equip level.
        /// If no override is set, returns -1 and the base level calculation is used.
        /// </remarks>
        public static int GetMinEquipLevelOverride(uint oItem)
        {
            return global::NWN.Core.NWNX.ItemPlugin.GetMinEquipLevelOverride(oItem);
        }
    }
}