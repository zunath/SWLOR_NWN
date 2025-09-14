using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Queries the current value of the appearance settings on an item.
        /// </summary>
        /// <param name="oItem">The item to query</param>
        /// <param name="nType">The appearance type</param>
        /// <param name="nIndex">The appearance index</param>
        /// <returns>The appearance value</returns>
        /// <remarks>The parameters are identical to those of CopyItemAndModify().</remarks>
        public static int GetItemAppearance(uint oItem, ItemAppearanceType nType, int nIndex)
        {
            return global::NWN.Core.NWScript.GetItemAppearance(oItem, (int)nType, nIndex);
        }

        /// <summary>
        /// Returns the stack size of an item.
        /// </summary>
        /// <param name="oItem">The item to query</param>
        /// <returns>The stack size of the item</returns>
        public static int GetItemStackSize(uint oItem)
        {
            return global::NWN.Core.NWScript.GetItemStackSize(oItem);
        }

        /// <summary>
        /// Sets the stack size of an item.
        /// </summary>
        /// <param name="oItem">The item to change</param>
        /// <param name="nSize">The new size of stack</param>
        /// <remarks>Will be restricted to be between 1 and the maximum stack size for the item type. If a value less than 1 is passed it will set the stack to 1. If a value greater than the max is passed then it will set the stack to the maximum size.</remarks>
        public static void SetItemStackSize(uint oItem, int nSize)
        {
            global::NWN.Core.NWScript.SetItemStackSize(oItem, nSize);
        }

        /// <summary>
        /// Returns the charges left on an item.
        /// </summary>
        /// <param name="oItem">The item to query</param>
        /// <returns>The number of charges left</returns>
        public static int GetItemCharges(uint oItem)
        {
            return global::NWN.Core.NWScript.GetItemCharges(oItem);
        }

        /// <summary>
        /// Sets the charges left on an item.
        /// </summary>
        /// <param name="oItem">The item to change</param>
        /// <param name="nCharges">The number of charges</param>
        /// <remarks>If value below 0 is passed, charges will be set to 0. If value greater than maximum is passed, charges will be set to maximum. If the charges drops to 0 the item will be destroyed.</remarks>
        public static void SetItemCharges(uint oItem, int nCharges)
        {
            global::NWN.Core.NWScript.SetItemCharges(oItem, nCharges);
        }

        /// <summary>
        /// Duplicates the item and returns a new object.
        /// </summary>
        /// <param name="oItem">The item to copy</param>
        /// <param name="oTargetInventory">Create item in this object's inventory. If this parameter is not valid, the item will be created in oItem's location (default: OBJECT_INVALID)</param>
        /// <param name="bCopyVars">Copy the local variables from the old item to the new one (default: false)</param>
        /// <returns>The new item. Returns OBJECT_INVALID for non-items</returns>
        /// <remarks>Can only copy empty item containers. Will return OBJECT_INVALID if oItem contains other items. If it is possible to merge this item with any others in the target location, then it will do so and return the merged object.</remarks>
        public static uint CopyItem(uint oItem, uint oTargetInventory = OBJECT_INVALID, bool bCopyVars = false)
        {
            return global::NWN.Core.NWScript.CopyItem(oItem, oTargetInventory, bCopyVars ? 1 : 0);
        }

        /// <summary>
        /// In an onItemAcquired script, returns the size of the stack of the item that was just acquired.
        /// </summary>
        /// <returns>The stack size of the item acquired</returns>
        public static int GetModuleItemAcquiredStackSize()
        {
            return global::NWN.Core.NWScript.GetModuleItemAcquiredStackSize();
        }

        /// <summary>
        /// Gets the number of stacked items that the item comprises.
        /// </summary>
        /// <param name="oItem">The item to check</param>
        /// <returns>The number of stacked items</returns>
        public static int GetNumStackedItems(uint oItem)
        {
            return global::NWN.Core.NWScript.GetNumStackedItems(oItem);
        }

        /// <summary>
        /// Sets whether the provided item should be hidden when equipped.
        /// </summary>
        /// <param name="oItem">The item to modify</param>
        /// <param name="nValue">Whether the item should be hidden when equipped</param>
        /// <remarks>The intended usage of this function is to provide an easy way to hide helmets, but it can be used equally for any slot which has creature mesh visibility when equipped, e.g.: armour, helm, cloak, left hand, and right hand.</remarks>
        public static void SetHiddenWhenEquipped(uint oItem, bool nValue)
        {
            global::NWN.Core.NWScript.SetHiddenWhenEquipped(oItem, nValue ? 1 : 0);
        }

        /// <summary>
        /// Returns whether the provided item is hidden when equipped.
        /// </summary>
        /// <param name="oItem">The item to check</param>
        /// <returns>1 if the item is hidden when equipped, 0 otherwise</returns>
        public static int GetHiddenWhenEquipped(uint oItem)
        {
            return global::NWN.Core.NWScript.GetHiddenWhenEquipped(oItem);
        }

        /// <summary>
        /// Returns true if the item is flagged as infinite.
        /// </summary>
        /// <param name="oItem">The item to check</param>
        /// <returns>1 if the item is infinite, 0 otherwise</returns>
        /// <remarks>The infinite property affects the buying/selling behavior of the item in a store. An infinite item will still be available to purchase from a store after a player buys the item (non-infinite items will disappear from the store when purchased).</remarks>
        public static int GetInfiniteFlag(uint oItem)
        {
            return global::NWN.Core.NWScript.GetInfiniteFlag(oItem);
        }

        /// <summary>
        /// Sets the Infinite flag on an item.
        /// </summary>
        /// <param name="oItem">The item to change</param>
        /// <param name="bInfinite">Whether the item should be infinite (default: true)</param>
        /// <remarks>The infinite property affects the buying/selling behavior of the item in a store. An infinite item will still be available to purchase from a store after a player buys the item (non-infinite items will disappear from the store when purchased).</remarks>
        public static void SetInfiniteFlag(uint oItem, bool bInfinite = true)
        {
            global::NWN.Core.NWScript.SetInfiniteFlag(oItem, bInfinite ? 1 : 0);
        }

        /// <summary>
        /// Sets whether this item is 'stolen' or not.
        /// </summary>
        /// <param name="oItem">The item to modify</param>
        /// <param name="nStolen">Whether the item is stolen</param>
        public static void SetStolenFlag(uint oItem, bool nStolen)
        {
            global::NWN.Core.NWScript.SetStolenFlag(oItem, nStolen ? 1 : 0);
        }

        /// <summary>
        /// Returns true if the item can be pickpocketed.
        /// </summary>
        /// <param name="oItem">The item to check</param>
        /// <returns>True if the item can be pickpocketed</returns>
        public static bool GetPickpocketableFlag(uint oItem)
        {
            return global::NWN.Core.NWScript.GetPickpocketableFlag(oItem) != 0;
        }

        /// <summary>
        /// Sets the Pickpocketable flag on an item.
        /// </summary>
        /// <param name="oItem">The item to change</param>
        /// <param name="bPickpocketable">Whether the item can be pickpocketed</param>
        public static void SetPickpocketableFlag(uint oItem, bool bPickpocketable)
        {
            global::NWN.Core.NWScript.SetPickpocketableFlag(oItem, bPickpocketable ? 1 : 0);
        }

        /// <summary>
        /// Sets the droppable flag on an item.
        /// </summary>
        /// <param name="oItem">The item to change</param>
        /// <param name="bDroppable">Whether the item should be droppable</param>
        /// <remarks>Droppable items will appear on a creature's remains when the creature is killed.</remarks>
        public static void SetDroppableFlag(uint oItem, bool bDroppable)
        {
            global::NWN.Core.NWScript.SetDroppableFlag(oItem, bDroppable ? 1 : 0);
        }

        /// <summary>
        /// Returns true if the item can be dropped.
        /// </summary>
        /// <param name="oItem">The item to check</param>
        /// <returns>True if the item can be dropped</returns>
        /// <remarks>Droppable items will appear on a creature's remains when the creature is killed.</remarks>
        public static bool GetDroppableFlag(uint oItem)
        {
            return global::NWN.Core.NWScript.GetDroppableFlag(oItem) != 0;
        }

        /// <summary>
        /// Returns true if the placeable object is usable.
        /// </summary>
        /// <param name="oObject">The object to check (default: OBJECT_INVALID)</param>
        /// <returns>True if the object is usable</returns>
        public static bool GetUseableFlag(uint oObject = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetUseableFlag(oObject) != 0;
        }

        /// <summary>
        /// Returns true if the item is stolen.
        /// </summary>
        /// <param name="oStolen">The item to check</param>
        /// <returns>True if the item is stolen</returns>
        public static bool GetStolenFlag(uint oStolen)
        {
            return global::NWN.Core.NWScript.GetStolenFlag(oStolen) != 0;
        }

        /// <summary>
        /// Returns true if the item is a ranged weapon.
        /// </summary>
        /// <param name="oItem">The item to check</param>
        /// <returns>True if the item is a ranged weapon</returns>
        public static bool GetWeaponRanged(uint oItem)
        {
            return global::NWN.Core.NWScript.GetWeaponRanged(oItem) != 0;
        }

        /// <summary>
        /// Use this in a spell script to get the item used to cast the spell.
        /// </summary>
        /// <returns>The item used to cast the spell</returns>
        public static uint GetSpellCastItem()
        {
            return global::NWN.Core.NWScript.GetSpellCastItem();
        }

        /// <summary>
        /// Use this in an OnItemActivated module script to get the item that was activated.
        /// </summary>
        /// <returns>The item that was activated</returns>
        public static uint GetItemActivated()
        {
            return global::NWN.Core.NWScript.GetItemActivated();
        }

        /// <summary>
        /// Use this in an OnItemActivated module script to get the creature that activated the item.
        /// </summary>
        /// <returns>The creature that activated the item</returns>
        public static uint GetItemActivator()
        {
            return global::NWN.Core.NWScript.GetItemActivator();
        }

        /// <summary>
        /// Use this in an OnItemActivated module script to get the location of the item's target.
        /// </summary>
        /// <returns>The location of the item's target</returns>
        public static Location GetItemActivatedTargetLocation()
        {
            return global::NWN.Core.NWScript.GetItemActivatedTargetLocation();
        }

        /// <summary>
        /// Use this in an OnItemActivated module script to get the item's target.
        /// </summary>
        /// <returns>The item's target</returns>
        public static uint GetItemActivatedTarget()
        {
            return global::NWN.Core.NWScript.GetItemActivatedTarget();
        }

        /// <summary>
        /// Gets the Armor Class value of an item.
        /// </summary>
        /// <param name="oItem">The item to query</param>
        /// <returns>The Armor Class value, or 0 if the item is not valid or has no armor value</returns>
        public static int GetItemACValue(uint oItem)
        {
            return global::NWN.Core.NWScript.GetItemACValue(oItem);
        }

        /// <summary>
        /// Gets the base item type of an item.
        /// </summary>
        /// <param name="oItem">The item to query</param>
        /// <returns>The base item type (BASE_ITEM_*), or BASE_ITEM_INVALID if the item is not valid</returns>
        public static BaseItem GetBaseItemType(uint oItem)
        {
            return (BaseItem)global::NWN.Core.NWScript.GetBaseItemType(oItem);
        }

        /// <summary>
        /// Determines whether an item has a specific item property.
        /// </summary>
        /// <param name="oItem">The item to check</param>
        /// <param name="nProperty">The item property type to check for (ITEM_PROPERTY_*)</param>
        /// <returns>True if the item has the property, false if the item is not valid or does not have the property</returns>
        public static bool GetItemHasItemProperty(uint oItem, ItemPropertyType nProperty)
        {
            return global::NWN.Core.NWScript.GetItemHasItemProperty(oItem, (int)nProperty) == 1;
        }

        /// <summary>
        /// Gets the first item in a target's inventory to start cycling through items.
        /// </summary>
        /// <param name="oTarget">The target object to check inventory of (default: OBJECT_INVALID)</param>
        /// <returns>The first item in the inventory, or OBJECT_INVALID if the target is not a creature, item, placeable, or store, or if no item is found</returns>
        public static uint GetFirstItemInInventory(uint oTarget = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetFirstItemInInventory(oTarget);
        }

        /// <summary>
        /// Gets the next item in a target's inventory to continue cycling through items.
        /// </summary>
        /// <param name="oTarget">The target object to check inventory of (default: OBJECT_INVALID)</param>
        /// <returns>The next item in the inventory, or OBJECT_INVALID if the target is not a creature, item, placeable, or store, or if no more items are found</returns>
        public static uint GetNextItemInInventory(uint oTarget = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetNextItemInInventory(oTarget);
        }

        /// <summary>
        /// Determines whether an item has been identified.
        /// </summary>
        /// <param name="oItem">The item to check</param>
        /// <returns>True if the item has been identified, false otherwise</returns>
        public static bool GetIdentified(uint oItem)
        {
            return global::NWN.Core.NWScript.GetIdentified(oItem) != 0;
        }

        /// <summary>
        /// Sets whether an item has been identified.
        /// </summary>
        /// <param name="oItem">The item to modify</param>
        /// <param name="bIdentified">Whether the item should be identified</param>
        public static void SetIdentified(uint oItem, bool bIdentified)
        {
            global::NWN.Core.NWScript.SetIdentified(oItem, bIdentified ? 1 : 0);
        }

        /// <summary>
        /// Gets the gold piece value of an item.
        /// </summary>
        /// <param name="oItem">The item to query</param>
        /// <returns>The gold piece value, or 0 if the item is not valid</returns>
        public static int GetGoldPieceValue(uint oItem)
        {
            return global::NWN.Core.NWScript.GetGoldPieceValue(oItem);
        }

        /// <summary>
        /// Gets the item that was acquired in an OnItemAcquired script.
        /// </summary>
        /// <returns>The item that was acquired, or OBJECT_INVALID if the module is not valid</returns>
        public static uint GetModuleItemAcquired()
        {
            return global::NWN.Core.NWScript.GetModuleItemAcquired();
        }

        /// <summary>
        /// Gets the creature that previously possessed the item in an OnItemAcquired script.
        /// </summary>
        /// <returns>The creature that previously possessed the item, or OBJECT_INVALID if the item was picked up from the ground</returns>
        public static uint GetModuleItemAcquiredFrom()
        {
            return global::NWN.Core.NWScript.GetModuleItemAcquiredFrom();
        }

        /// <summary>
        /// Gets the object in a creature's specified inventory slot.
        /// </summary>
        /// <param name="nInventorySlot">The inventory slot to check (INVENTORY_SLOT_*)</param>
        /// <param name="oCreature">The creature to check (default: OBJECT_INVALID)</param>
        /// <returns>The item in the specified slot, or OBJECT_INVALID if the creature is not valid or there is no item in the slot</returns>
        public static uint GetItemInSlot(InventorySlot nInventorySlot, uint oCreature = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetItemInSlot((int)nInventorySlot, oCreature);
        }

        /// <summary>
        /// Checks if a base item type fits in a target's inventory.
        /// </summary>
        /// <param name="baseItemType">The base item type to check (BASE_ITEM_*)</param>
        /// <param name="target">The target creature, placeable, or item to check</param>
        /// <returns>True if the base item type fits in the inventory, false if not or on error</returns>
        /// <remarks>Does not check inside any container items possessed by the target</remarks>
        public static bool GetBaseItemFitsInInventory(BaseItem baseItemType, uint target)
        {
            return global::NWN.Core.NWScript.GetBaseItemFitsInInventory((int)baseItemType, target) == 1;
        }
    }
}