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
            Internal.NativeFunctions.StackPushInteger(nIndex);
            Internal.NativeFunctions.StackPushInteger((int)nType);
            Internal.NativeFunctions.StackPushObject(oItem);
            Internal.NativeFunctions.CallBuiltIn(732);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Returns stack size of an item
        ///   - oItem: item to query
        /// </summary>
        public static int GetItemStackSize(uint oItem)
        {
            Internal.NativeFunctions.StackPushObject(oItem);
            Internal.NativeFunctions.CallBuiltIn(605);
            return Internal.NativeFunctions.StackPopInteger();
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
            Internal.NativeFunctions.StackPushInteger(nSize);
            Internal.NativeFunctions.StackPushObject(oItem);
            Internal.NativeFunctions.CallBuiltIn(606);
        }

        /// <summary>
        ///   Returns charges left on an item
        ///   - oItem: item to query
        /// </summary>
        public static int GetItemCharges(uint oItem)
        {
            Internal.NativeFunctions.StackPushObject(oItem);
            Internal.NativeFunctions.CallBuiltIn(607);
            return Internal.NativeFunctions.StackPopInteger();
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
            Internal.NativeFunctions.StackPushInteger(nCharges);
            Internal.NativeFunctions.StackPushObject(oItem);
            Internal.NativeFunctions.CallBuiltIn(608);
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
            Internal.NativeFunctions.StackPushInteger(bCopyVars ? 1 : 0);
            Internal.NativeFunctions.StackPushObject(oTargetInventory);
            Internal.NativeFunctions.StackPushObject(oItem);
            Internal.NativeFunctions.CallBuiltIn(584);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   in an onItemAcquired script, returns the size of the stack of the item
        ///   that was just acquired.
        ///   * returns the stack size of the item acquired
        /// </summary>
        public static int GetModuleItemAcquiredStackSize()
        {
            Internal.NativeFunctions.CallBuiltIn(579);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Get the number of stacked items that oItem comprises.
        /// </summary>
        public static int GetNumStackedItems(uint oItem)
        {
            Internal.NativeFunctions.StackPushObject(oItem);
            Internal.NativeFunctions.CallBuiltIn(475);
            return Internal.NativeFunctions.StackPopInteger();
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
            Internal.NativeFunctions.StackPushInteger(nValue ? 1 : 0);
            Internal.NativeFunctions.StackPushObject(oItem);
            Internal.NativeFunctions.CallBuiltIn(864);
        }

        /// <summary>
        ///   Returns whether the provided item is hidden when equipped.
        /// </summary>
        public static int GetHiddenWhenEquipped(uint oItem)
        {
            Internal.NativeFunctions.StackPushObject(oItem);
            Internal.NativeFunctions.CallBuiltIn(865);
            return Internal.NativeFunctions.StackPopInteger();
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
            Internal.NativeFunctions.StackPushObject(oItem);
            Internal.NativeFunctions.CallBuiltIn(827);
            return Internal.NativeFunctions.StackPopInteger();
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
            Internal.NativeFunctions.StackPushInteger(bInfinite ? 1 : 0);
            Internal.NativeFunctions.StackPushObject(oItem);
            Internal.NativeFunctions.CallBuiltIn(828);
        }

        /// <summary>
        ///   Sets whether this item is 'stolen' or not
        /// </summary>
        public static void SetStolenFlag(uint oItem, bool nStolen)
        {
            Internal.NativeFunctions.StackPushInteger(nStolen ? 1 : 0);
            Internal.NativeFunctions.StackPushObject(oItem);
            Internal.NativeFunctions.CallBuiltIn(774);
        }

        /// <summary>
        ///   returns TRUE if the item CAN be pickpocketed
        /// </summary>
        public static bool GetPickpocketableFlag(uint oItem)
        {
            Internal.NativeFunctions.StackPushObject(oItem);
            Internal.NativeFunctions.CallBuiltIn(786);
            return Internal.NativeFunctions.StackPopInteger() != 0;
        }

        /// <summary>
        ///   Sets the Pickpocketable flag on an item
        ///   - oItem: the item to change
        ///   - bPickpocketable: TRUE or FALSE, whether the item can be pickpocketed.
        /// </summary>
        public static void SetPickpocketableFlag(uint oItem, bool bPickpocketable)
        {
            Internal.NativeFunctions.StackPushInteger(bPickpocketable ? 1 : 0);
            Internal.NativeFunctions.StackPushObject(oItem);
            Internal.NativeFunctions.CallBuiltIn(787);
        }

        /// <summary>
        ///   Sets the droppable flag on an item
        ///   - oItem: the item to change
        ///   - bDroppable: TRUE or FALSE, whether the item should be droppable
        ///   Droppable items will appear on a creature's remains when the creature is killed.
        /// </summary>
        public static void SetDroppableFlag(uint oItem, bool bDroppable)
        {
            Internal.NativeFunctions.StackPushInteger(bDroppable ? 1 : 0);
            Internal.NativeFunctions.StackPushObject(oItem);
            Internal.NativeFunctions.CallBuiltIn(705);
        }

        /// <summary>
        ///   returns TRUE if the item CAN be dropped
        ///   Droppable items will appear on a creature's remains when the creature is killed.
        /// </summary>
        public static bool GetDroppableFlag(uint oItem)
        {
            Internal.NativeFunctions.StackPushObject(oItem);
            Internal.NativeFunctions.CallBuiltIn(586);
            return Internal.NativeFunctions.StackPopInteger() != 0;
        }

        /// <summary>
        ///   returns TRUE if the placeable object is usable
        /// </summary>
        public static bool GetUseableFlag(uint oObject = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(587);
            return Internal.NativeFunctions.StackPopInteger() != 0;
        }

        /// <summary>
        ///   returns TRUE if the item is stolen
        /// </summary>
        public static bool GetStolenFlag(uint oStolen)
        {
            Internal.NativeFunctions.StackPushObject(oStolen);
            Internal.NativeFunctions.CallBuiltIn(588);
            return Internal.NativeFunctions.StackPopInteger() != 0;
        }

        /// <summary>
        ///   * Returns TRUE if oItem is a ranged weapon.
        /// </summary>
        public static bool GetWeaponRanged(uint oItem)
        {
            Internal.NativeFunctions.StackPushObject(oItem);
            Internal.NativeFunctions.CallBuiltIn(511);
            return Internal.NativeFunctions.StackPopInteger() != 0;
        }

        /// <summary>
        ///   Use this in a spell script to get the item used to cast the spell.
        /// </summary>
        public static uint GetSpellCastItem()
        {
            Internal.NativeFunctions.CallBuiltIn(438);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Use this in an OnItemActivated module script to get the item that was activated.
        /// </summary>
        public static uint GetItemActivated()
        {
            Internal.NativeFunctions.CallBuiltIn(439);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Use this in an OnItemActivated module script to get the creature that
        ///   activated the item.
        /// </summary>
        public static uint GetItemActivator()
        {
            Internal.NativeFunctions.CallBuiltIn(440);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Use this in an OnItemActivated module script to get the location of the item's
        ///   target.
        /// </summary>
        public static Location GetItemActivatedTargetLocation()
        {
            Internal.NativeFunctions.CallBuiltIn(441);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Location);
        }

        /// <summary>
        ///   Use this in an OnItemActivated module script to get the item's target.
        /// </summary>
        public static uint GetItemActivatedTarget()
        {
            Internal.NativeFunctions.CallBuiltIn(442);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Get the Armour Class of oItem.
        ///   * Return 0 if the oItem is not a valid item, or if oItem has no armour value.
        /// </summary>
        public static int GetItemACValue(uint oItem)
        {
            Internal.NativeFunctions.StackPushObject(oItem);
            Internal.NativeFunctions.CallBuiltIn(401);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Get the base item type (BASE_ITEM_*) of oItem.
        ///   * Returns BASE_ITEM_INVALID if oItem is an invalid item.
        /// </summary>
        public static BaseItem GetBaseItemType(uint oItem)
        {
            Internal.NativeFunctions.StackPushObject(oItem);
            Internal.NativeFunctions.CallBuiltIn(397);
            return (BaseItem)Internal.NativeFunctions.StackPopInteger();
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
            Internal.NativeFunctions.StackPushInteger((int)nProperty);
            Internal.NativeFunctions.StackPushObject(oItem);
            Internal.NativeFunctions.CallBuiltIn(398);
            return Internal.NativeFunctions.StackPopInteger() == 1;
        }

        /// <summary>
        ///   Get the first item in oTarget's inventory (start to cycle through oTarget's
        ///   inventory).
        ///   * Returns OBJECT_INVALID if the caller is not a creature, item, placeable or store,
        ///   or if no item is found.
        /// </summary>
        public static uint GetFirstItemInInventory(uint oTarget = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oTarget);
            Internal.NativeFunctions.CallBuiltIn(339);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Get the next item in oTarget's inventory (continue to cycle through oTarget's
        ///   inventory).
        ///   * Returns OBJECT_INVALID if the caller is not a creature, item, placeable or store,
        ///   or if no item is found.
        /// </summary>
        public static uint GetNextItemInInventory(uint oTarget = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oTarget);
            Internal.NativeFunctions.CallBuiltIn(340);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Determined whether oItem has been identified.
        /// </summary>
        public static bool GetIdentified(uint oItem)
        {
            Internal.NativeFunctions.StackPushObject(oItem);
            Internal.NativeFunctions.CallBuiltIn(332);
            return Internal.NativeFunctions.StackPopInteger() != 0;
        }

        /// <summary>
        ///   Set whether oItem has been identified.
        /// </summary>
        public static void SetIdentified(uint oItem, bool bIdentified)
        {
            Internal.NativeFunctions.StackPushInteger(bIdentified ? 1 : 0);
            Internal.NativeFunctions.StackPushObject(oItem);
            Internal.NativeFunctions.CallBuiltIn(333);
        }

        /// <summary>
        ///   Get the gold piece value of oItem.
        ///   * Returns 0 if oItem is not a valid item.
        /// </summary>
        public static int GetGoldPieceValue(uint oItem)
        {
            Internal.NativeFunctions.StackPushObject(oItem);
            Internal.NativeFunctions.CallBuiltIn(311);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Use this in an OnItemAcquired script to get the item that was acquired.
        ///   * Returns OBJECT_INVALID if the module is not valid.
        /// </summary>
        public static uint GetModuleItemAcquired()
        {
            Internal.NativeFunctions.CallBuiltIn(282);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Use this in an OnItemAcquired script to get the creatre that previously
        ///   possessed the item.
        ///   * Returns OBJECT_INVALID if the item was picked up from the ground.
        /// </summary>
        public static uint GetModuleItemAcquiredFrom()
        {
            Internal.NativeFunctions.CallBuiltIn(283);
            return Internal.NativeFunctions.StackPopObject();
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
            Internal.NativeFunctions.StackPushObject(oCreature);
            Internal.NativeFunctions.StackPushInteger((int)nInventorySlot);
            Internal.NativeFunctions.CallBuiltIn(155);
            return Internal.NativeFunctions.StackPopObject();
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
            Internal.NativeFunctions.StackPushObject(target);
            Internal.NativeFunctions.StackPushInteger((int) baseItemType);
            Internal.NativeFunctions.CallBuiltIn(944);
            return Internal.NativeFunctions.StackPopInteger() == 1;
        }
    }
}