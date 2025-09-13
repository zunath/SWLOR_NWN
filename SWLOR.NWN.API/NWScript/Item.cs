using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Core.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   Queries the current value of the appearance settings on an item. The parameters are
        ///   identical to those of CopyItemAndModify().
        /// </summary>
        public static int GetItemAppearance(uint oItem, ItemAppearanceType nType, int nIndex)
        {
            return NWN.Core.NWScript.GetItemAppearance(oItem, (int)nType, nIndex);
        }

        /// <summary>
        ///   Returns stack size of an item
        ///   - oItem: item to query
        /// </summary>
        public static int GetItemStackSize(uint oItem)
        {
            return NWN.Core.NWScript.GetItemStackSize(oItem);
        }

        /// <summary>
        ///   Sets stack size of an item.
        ///   - oItem: item to change
        ///   - nSize: new size of stack.  Will be restricted to be between 1 and the
        ///   maximum stack size for the item type.  If a value less than 1 is passed it
        ///   will set the stack to 1.  If a value greater than the max is passed
        ///   then it will set the stack to the maximum size
        /// </summary>
        public static void SetItemStackSize(uint oItem, int nSize)
        {
            NWN.Core.NWScript.SetItemStackSize(oItem, nSize);
        }

        /// <summary>
        ///   Returns charges left on an item
        ///   - oItem: item to query
        /// </summary>
        public static int GetItemCharges(uint oItem)
        {
            return NWN.Core.NWScript.GetItemCharges(oItem);
        }

        /// <summary>
        ///   Sets charges left on an item.
        ///   - oItem: item to change
        ///   - nCharges: number of charges.  If value below 0 is passed, # charges will
        ///   be set to 0.  If value greater than maximum is passed, # charges will
        ///   be set to maximum.  If the # charges drops to 0 the item
        ///   will be destroyed.
        /// </summary>
        public static void SetItemCharges(uint oItem, int nCharges)
        {
            NWN.Core.NWScript.SetItemCharges(oItem, nCharges);
        }

        /// <summary>
        ///   duplicates the item and returns a new object
        ///   oItem - item to copy
        ///   oTargetInventory - create item in this object's inventory. If this parameter
        ///   is not valid, the item will be created in oItem's location
        ///   bCopyVars - copy the local variables from the old item to the new one
        ///   * returns the new item
        ///   * returns OBJECT_INVALID for non-items.
        ///   * can only copy empty item containers. will return OBJECT_INVALID if oItem contains
        ///   other items.
        ///   * if it is possible to merge this item with any others in the target location,
        ///   then it will do so and return the merged object.
        /// </summary>
        public static uint CopyItem(uint oItem, uint oTargetInventory = OBJECT_INVALID, bool bCopyVars = false)
        {
            return NWN.Core.NWScript.CopyItem(oItem, oTargetInventory, bCopyVars ? 1 : 0);
        }

        /// <summary>
        ///   in an onItemAcquired script, returns the size of the stack of the item
        ///   that was just acquired.
        ///   * returns the stack size of the item acquired
        /// </summary>
        public static int GetModuleItemAcquiredStackSize()
        {
            return NWN.Core.NWScript.GetModuleItemAcquiredStackSize();
        }

        /// <summary>
        ///   Get the number of stacked items that oItem comprises.
        /// </summary>
        public static int GetNumStackedItems(uint oItem)
        {
            return NWN.Core.NWScript.GetNumStackedItems(oItem);
        }

        /// <summary>
        ///   Sets whether the provided item should be hidden when equipped.
        ///   - The intended usage of this function is to provide an easy way to hide helmets, but it
        ///   can be used equally for any slot which has creature mesh visibility when equipped,
        ///   e.g.: armour, helm, cloak, left hand, and right hand.
        ///   - nValue should be TRUE or FALSE.
        /// </summary>
        public static void SetHiddenWhenEquipped(uint oItem, bool nValue)
        {
            NWN.Core.NWScript.SetHiddenWhenEquipped(oItem, nValue ? 1 : 0);
        }

        /// <summary>
        ///   Returns whether the provided item is hidden when equipped.
        /// </summary>
        public static int GetHiddenWhenEquipped(uint oItem)
        {
            return NWN.Core.NWScript.GetHiddenWhenEquipped(oItem);
        }

        /// <summary>
        ///   returns TRUE if the item is flagged as infinite.
        ///   - oItem: an item.
        ///   The infinite property affects the buying/selling behavior of the item in a store.
        ///   An infinite item will still be available to purchase from a store after a player
        ///   buys the item (non-infinite items will disappear from the store when purchased).
        /// </summary>
        public static int GetInfiniteFlag(uint oItem)
        {
            return NWN.Core.NWScript.GetInfiniteFlag(oItem);
        }

        /// <summary>
        ///   Sets the Infinite flag on an item
        ///   - oItem: the item to change
        ///   - bInfinite: TRUE or FALSE, whether the item should be Infinite
        ///   The infinite property affects the buying/selling behavior of the item in a store.
        ///   An infinite item will still be available to purchase from a store after a player
        ///   buys the item (non-infinite items will disappear from the store when purchased).
        /// </summary>
        public static void SetInfiniteFlag(uint oItem, bool bInfinite = true)
        {
            NWN.Core.NWScript.SetInfiniteFlag(oItem, bInfinite ? 1 : 0);
        }

        /// <summary>
        ///   Sets whether this item is 'stolen' or not
        /// </summary>
        public static void SetStolenFlag(uint oItem, bool nStolen)
        {
            NWN.Core.NWScript.SetStolenFlag(oItem, nStolen ? 1 : 0);
        }

        /// <summary>
        ///   returns TRUE if the item CAN be pickpocketed
        /// </summary>
        public static bool GetPickpocketableFlag(uint oItem)
        {
            return NWN.Core.NWScript.GetPickpocketableFlag(oItem) != 0;
        }

        /// <summary>
        ///   Sets the Pickpocketable flag on an item
        ///   - oItem: the item to change
        ///   - bPickpocketable: TRUE or FALSE, whether the item can be pickpocketed.
        /// </summary>
        public static void SetPickpocketableFlag(uint oItem, bool bPickpocketable)
        {
            NWN.Core.NWScript.SetPickpocketableFlag(oItem, bPickpocketable ? 1 : 0);
        }

        /// <summary>
        ///   Sets the droppable flag on an item
        ///   - oItem: the item to change
        ///   - bDroppable: TRUE or FALSE, whether the item should be droppable
        ///   Droppable items will appear on a creature's remains when the creature is killed.
        /// </summary>
        public static void SetDroppableFlag(uint oItem, bool bDroppable)
        {
            NWN.Core.NWScript.SetDroppableFlag(oItem, bDroppable ? 1 : 0);
        }

        /// <summary>
        ///   returns TRUE if the item CAN be dropped
        ///   Droppable items will appear on a creature's remains when the creature is killed.
        /// </summary>
        public static bool GetDroppableFlag(uint oItem)
        {
            return NWN.Core.NWScript.GetDroppableFlag(oItem) != 0;
        }

        /// <summary>
        ///   returns TRUE if the placeable object is usable
        /// </summary>
        public static bool GetUseableFlag(uint oObject = OBJECT_INVALID)
        {
            return NWN.Core.NWScript.GetUseableFlag(oObject) != 0;
        }

        /// <summary>
        ///   returns TRUE if the item is stolen
        /// </summary>
        public static bool GetStolenFlag(uint oStolen)
        {
            return NWN.Core.NWScript.GetStolenFlag(oStolen) != 0;
        }

        /// <summary>
        ///   * Returns TRUE if oItem is a ranged weapon.
        /// </summary>
        public static bool GetWeaponRanged(uint oItem)
        {
            return NWN.Core.NWScript.GetWeaponRanged(oItem) != 0;
        }

        /// <summary>
        ///   Use this in a spell script to get the item used to cast the spell.
        /// </summary>
        public static uint GetSpellCastItem()
        {
            return NWN.Core.NWScript.GetSpellCastItem();
        }

        /// <summary>
        ///   Use this in an OnItemActivated module script to get the item that was activated.
        /// </summary>
        public static uint GetItemActivated()
        {
            return NWN.Core.NWScript.GetItemActivated();
        }

        /// <summary>
        ///   Use this in an OnItemActivated module script to get the creature that
        ///   activated the item.
        /// </summary>
        public static uint GetItemActivator()
        {
            return NWN.Core.NWScript.GetItemActivator();
        }

        /// <summary>
        ///   Use this in an OnItemActivated module script to get the location of the item's
        ///   target.
        /// </summary>
        public static Location GetItemActivatedTargetLocation()
        {
            return NWN.Core.NWScript.GetItemActivatedTargetLocation();
        }

        /// <summary>
        ///   Use this in an OnItemActivated module script to get the item's target.
        /// </summary>
        public static uint GetItemActivatedTarget()
        {
            return NWN.Core.NWScript.GetItemActivatedTarget();
        }

        /// <summary>
        ///   Get the Armour Class of oItem.
        ///   * Return 0 if the oItem is not a valid item, or if oItem has no armour value.
        /// </summary>
        public static int GetItemACValue(uint oItem)
        {
            return NWN.Core.NWScript.GetItemACValue(oItem);
        }

        /// <summary>
        ///   Get the base item type (BASE_ITEM_*) of oItem.
        ///   * Returns BASE_ITEM_INVALID if oItem is an invalid item.
        /// </summary>
        public static BaseItem GetBaseItemType(uint oItem)
        {
            return (BaseItem)NWN.Core.NWScript.GetBaseItemType(oItem);
        }

        /// <summary>
        ///   Determines whether oItem has nProperty.
        ///   - oItem
        ///   - nProperty: ITEM_PROPERTY_*
        ///   * Returns FALSE if oItem is not a valid item, or if oItem does not have
        ///   nProperty.
        /// </summary>
        public static bool GetItemHasItemProperty(uint oItem, ItemPropertyType nProperty)
        {
            return NWN.Core.NWScript.GetItemHasItemProperty(oItem, (int)nProperty) == 1;
        }

        /// <summary>
        ///   Get the first item in oTarget's inventory (start to cycle through oTarget's
        ///   inventory).
        ///   * Returns OBJECT_INVALID if the caller is not a creature, item, placeable or store,
        ///   or if no item is found.
        /// </summary>
        public static uint GetFirstItemInInventory(uint oTarget = OBJECT_INVALID)
        {
            return NWN.Core.NWScript.GetFirstItemInInventory(oTarget);
        }

        /// <summary>
        ///   Get the next item in oTarget's inventory (continue to cycle through oTarget's
        ///   inventory).
        ///   * Returns OBJECT_INVALID if the caller is not a creature, item, placeable or store,
        ///   or if no item is found.
        /// </summary>
        public static uint GetNextItemInInventory(uint oTarget = OBJECT_INVALID)
        {
            return NWN.Core.NWScript.GetNextItemInInventory(oTarget);
        }

        /// <summary>
        ///   Determined whether oItem has been identified.
        /// </summary>
        public static bool GetIdentified(uint oItem)
        {
            return NWN.Core.NWScript.GetIdentified(oItem) != 0;
        }

        /// <summary>
        ///   Set whether oItem has been identified.
        /// </summary>
        public static void SetIdentified(uint oItem, bool bIdentified)
        {
            NWN.Core.NWScript.SetIdentified(oItem, bIdentified ? 1 : 0);
        }

        /// <summary>
        ///   Get the gold piece value of oItem.
        ///   * Returns 0 if oItem is not a valid item.
        /// </summary>
        public static int GetGoldPieceValue(uint oItem)
        {
            return NWN.Core.NWScript.GetGoldPieceValue(oItem);
        }

        /// <summary>
        ///   Use this in an OnItemAcquired script to get the item that was acquired.
        ///   * Returns OBJECT_INVALID if the module is not valid.
        /// </summary>
        public static uint GetModuleItemAcquired()
        {
            return NWN.Core.NWScript.GetModuleItemAcquired();
        }

        /// <summary>
        ///   Use this in an OnItemAcquired script to get the creatre that previously
        ///   possessed the item.
        ///   * Returns OBJECT_INVALID if the item was picked up from the ground.
        /// </summary>
        public static uint GetModuleItemAcquiredFrom()
        {
            return NWN.Core.NWScript.GetModuleItemAcquiredFrom();
        }

        /// <summary>
        ///   Get the object which is in oCreature's specified inventory slot
        ///   - nInventorySlot: INVENTORY_SLOT_*
        ///   - oCreature
        ///   * Returns OBJECT_INVALID if oCreature is not a valid creature or there is no
        ///   item in nInventorySlot.
        /// </summary>
        public static uint GetItemInSlot(InventorySlot nInventorySlot, uint oCreature = OBJECT_INVALID)
        {
            return NWN.Core.NWScript.GetItemInSlot((int)nInventorySlot, oCreature);
        }

        /// <summary>
        /// Check if nBaseItemType fits in oTarget's inventory.
        /// Note: Does not check inside any container items possessed by oTarget.
        /// * nBaseItemType: a BASE_ITEM_* constant.
        /// * oTarget: a valid creature, placeable or item.
        /// Returns: TRUE if the baseitem type fits, FALSE if not or on error.
        /// </summary>
        public static bool GetBaseItemFitsInInventory(BaseItem baseItemType, uint target)
        {
            return NWN.Core.NWScript.GetBaseItemFitsInInventory((int)baseItemType, target) == 1;
        }
    }
}