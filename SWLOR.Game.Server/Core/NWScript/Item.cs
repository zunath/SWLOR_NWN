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
            VM.StackPush(nIndex);
            VM.StackPush((int)nType);
            VM.StackPush(oItem);
            VM.Call(732);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Returns stack size of an item
        ///   - oItem: item to query
        /// </summary>
        public static int GetItemStackSize(uint oItem)
        {
            VM.StackPush(oItem);
            VM.Call(605);
            return VM.StackPopInt();
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
            VM.StackPush(nSize);
            VM.StackPush(oItem);
            VM.Call(606);
        }

        /// <summary>
        ///   Returns charges left on an item
        ///   - oItem: item to query
        /// </summary>
        public static int GetItemCharges(uint oItem)
        {
            VM.StackPush(oItem);
            VM.Call(607);
            return VM.StackPopInt();
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
            VM.StackPush(nCharges);
            VM.StackPush(oItem);
            VM.Call(608);
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
            VM.StackPush(bCopyVars ? 1 : 0);
            VM.StackPush(oTargetInventory);
            VM.StackPush(oItem);
            VM.Call(584);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   in an onItemAcquired script, returns the size of the stack of the item
        ///   that was just acquired.
        ///   * returns the stack size of the item acquired
        /// </summary>
        public static int GetModuleItemAcquiredStackSize()
        {
            VM.Call(579);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Get the number of stacked items that oItem comprises.
        /// </summary>
        public static int GetNumStackedItems(uint oItem)
        {
            VM.StackPush(oItem);
            VM.Call(475);
            return VM.StackPopInt();
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
            VM.StackPush(nValue ? 1 : 0);
            VM.StackPush(oItem);
            VM.Call(864);
        }

        /// <summary>
        ///   Returns whether the provided item is hidden when equipped.
        /// </summary>
        public static int GetHiddenWhenEquipped(uint oItem)
        {
            VM.StackPush(oItem);
            VM.Call(865);
            return VM.StackPopInt();
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
            VM.StackPush(oItem);
            VM.Call(827);
            return VM.StackPopInt();
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
            VM.StackPush(bInfinite ? 1 : 0);
            VM.StackPush(oItem);
            VM.Call(828);
        }

        /// <summary>
        ///   Sets whether this item is 'stolen' or not
        /// </summary>
        public static void SetStolenFlag(uint oItem, bool nStolen)
        {
            VM.StackPush(nStolen ? 1 : 0);
            VM.StackPush(oItem);
            VM.Call(774);
        }

        /// <summary>
        ///   returns TRUE if the item CAN be pickpocketed
        /// </summary>
        public static bool GetPickpocketableFlag(uint oItem)
        {
            VM.StackPush(oItem);
            VM.Call(786);
            return VM.StackPopInt() != 0;
        }

        /// <summary>
        ///   Sets the Pickpocketable flag on an item
        ///   - oItem: the item to change
        ///   - bPickpocketable: TRUE or FALSE, whether the item can be pickpocketed.
        /// </summary>
        public static void SetPickpocketableFlag(uint oItem, bool bPickpocketable)
        {
            VM.StackPush(bPickpocketable ? 1 : 0);
            VM.StackPush(oItem);
            VM.Call(787);
        }

        /// <summary>
        ///   Sets the droppable flag on an item
        ///   - oItem: the item to change
        ///   - bDroppable: TRUE or FALSE, whether the item should be droppable
        ///   Droppable items will appear on a creature's remains when the creature is killed.
        /// </summary>
        public static void SetDroppableFlag(uint oItem, bool bDroppable)
        {
            VM.StackPush(bDroppable ? 1 : 0);
            VM.StackPush(oItem);
            VM.Call(705);
        }

        /// <summary>
        ///   returns TRUE if the item CAN be dropped
        ///   Droppable items will appear on a creature's remains when the creature is killed.
        /// </summary>
        public static bool GetDroppableFlag(uint oItem)
        {
            VM.StackPush(oItem);
            VM.Call(586);
            return VM.StackPopInt() != 0;
        }

        /// <summary>
        ///   returns TRUE if the placeable object is usable
        /// </summary>
        public static bool GetUseableFlag(uint oObject = OBJECT_INVALID)
        {
            VM.StackPush(oObject);
            VM.Call(587);
            return VM.StackPopInt() != 0;
        }

        /// <summary>
        ///   returns TRUE if the item is stolen
        /// </summary>
        public static bool GetStolenFlag(uint oStolen)
        {
            VM.StackPush(oStolen);
            VM.Call(588);
            return VM.StackPopInt() != 0;
        }

        /// <summary>
        ///   * Returns TRUE if oItem is a ranged weapon.
        /// </summary>
        public static bool GetWeaponRanged(uint oItem)
        {
            VM.StackPush(oItem);
            VM.Call(511);
            return VM.StackPopInt() != 0;
        }

        /// <summary>
        ///   Use this in a spell script to get the item used to cast the spell.
        /// </summary>
        public static uint GetSpellCastItem()
        {
            VM.Call(438);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   Use this in an OnItemActivated module script to get the item that was activated.
        /// </summary>
        public static uint GetItemActivated()
        {
            VM.Call(439);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   Use this in an OnItemActivated module script to get the creature that
        ///   activated the item.
        /// </summary>
        public static uint GetItemActivator()
        {
            VM.Call(440);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   Use this in an OnItemActivated module script to get the location of the item's
        ///   target.
        /// </summary>
        public static Location GetItemActivatedTargetLocation()
        {
            VM.Call(441);
            return VM.StackPopStruct((int)EngineStructure.Location);
        }

        /// <summary>
        ///   Use this in an OnItemActivated module script to get the item's target.
        /// </summary>
        public static uint GetItemActivatedTarget()
        {
            VM.Call(442);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   Get the Armour Class of oItem.
        ///   * Return 0 if the oItem is not a valid item, or if oItem has no armour value.
        /// </summary>
        public static int GetItemACValue(uint oItem)
        {
            VM.StackPush(oItem);
            VM.Call(401);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Get the base item type (BASE_ITEM_*) of oItem.
        ///   * Returns BASE_ITEM_INVALID if oItem is an invalid item.
        /// </summary>
        public static BaseItem GetBaseItemType(uint oItem)
        {
            VM.StackPush(oItem);
            VM.Call(397);
            return (BaseItem)VM.StackPopInt();
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
            VM.StackPush((int)nProperty);
            VM.StackPush(oItem);
            VM.Call(398);
            return VM.StackPopInt() == 1;
        }

        /// <summary>
        ///   Get the first item in oTarget's inventory (start to cycle through oTarget's
        ///   inventory).
        ///   * Returns OBJECT_INVALID if the caller is not a creature, item, placeable or store,
        ///   or if no item is found.
        /// </summary>
        public static uint GetFirstItemInInventory(uint oTarget = OBJECT_INVALID)
        {
            VM.StackPush(oTarget);
            VM.Call(339);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   Get the next item in oTarget's inventory (continue to cycle through oTarget's
        ///   inventory).
        ///   * Returns OBJECT_INVALID if the caller is not a creature, item, placeable or store,
        ///   or if no item is found.
        /// </summary>
        public static uint GetNextItemInInventory(uint oTarget = OBJECT_INVALID)
        {
            VM.StackPush(oTarget);
            VM.Call(340);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   Determined whether oItem has been identified.
        /// </summary>
        public static bool GetIdentified(uint oItem)
        {
            VM.StackPush(oItem);
            VM.Call(332);
            return VM.StackPopInt() != 0;
        }

        /// <summary>
        ///   Set whether oItem has been identified.
        /// </summary>
        public static void SetIdentified(uint oItem, bool bIdentified)
        {
            VM.StackPush(bIdentified ? 1 : 0);
            VM.StackPush(oItem);
            VM.Call(333);
        }

        /// <summary>
        ///   Get the gold piece value of oItem.
        ///   * Returns 0 if oItem is not a valid item.
        /// </summary>
        public static int GetGoldPieceValue(uint oItem)
        {
            VM.StackPush(oItem);
            VM.Call(311);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Use this in an OnItemAcquired script to get the item that was acquired.
        ///   * Returns OBJECT_INVALID if the module is not valid.
        /// </summary>
        public static uint GetModuleItemAcquired()
        {
            VM.Call(282);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   Use this in an OnItemAcquired script to get the creatre that previously
        ///   possessed the item.
        ///   * Returns OBJECT_INVALID if the item was picked up from the ground.
        /// </summary>
        public static uint GetModuleItemAcquiredFrom()
        {
            VM.Call(283);
            return VM.StackPopObject();
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
            VM.StackPush(oCreature);
            VM.StackPush((int)nInventorySlot);
            VM.Call(155);
            return VM.StackPopObject();
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
            VM.StackPush(target);
            VM.StackPush((int) baseItemType);
            VM.Call(944);
            return VM.StackPopInt() == 1;
        }
    }
}