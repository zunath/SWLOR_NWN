using System;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.NWScript.Enumerations;

namespace SWLOR.Game.Server.NWScript
{
    public partial class _
    {
        /// <summary>
        ///  Returns the event script for the given object and handler.
        ///  Will return "" if unset, the object is invalid, or the object cannot
        ///  have the requested handler.
        /// </summary>
        public static string GetEventScript(NWGameObject oObject, EventScriptArea nHandler)
        {
            return GetEventScript(oObject, (int)nHandler);
        }
        /// <summary>
        ///  Returns the event script for the given object and handler.
        ///  Will return "" if unset, the object is invalid, or the object cannot
        ///  have the requested handler.
        /// </summary>
        public static string GetEventScript(NWGameObject oObject, EventScriptAreaOfEffect nHandler)
        {
            return GetEventScript(oObject, (int)nHandler);
        }

        /// <summary>
        ///  Returns the event script for the given object and handler.
        ///  Will return "" if unset, the object is invalid, or the object cannot
        ///  have the requested handler.
        /// </summary>
        public static string GetEventScript(NWGameObject oObject, EventScriptCreature nHandler)
        {
            return GetEventScript(oObject, (int)nHandler);
        }

        /// <summary>
        ///  Returns the event script for the given object and handler.
        ///  Will return "" if unset, the object is invalid, or the object cannot
        ///  have the requested handler.
        /// </summary>
        public static string GetEventScript(NWGameObject oObject, EventScriptDoor nHandler)
        {
            return GetEventScript(oObject, (int)nHandler);
        }

        /// <summary>
        ///  Returns the event script for the given object and handler.
        ///  Will return "" if unset, the object is invalid, or the object cannot
        ///  have the requested handler.
        /// </summary>
        public static string GetEventScript(NWGameObject oObject, EventScriptEncounter nHandler)
        {
            return GetEventScript(oObject, (int)nHandler);
        }

        /// <summary>
        ///  Returns the event script for the given object and handler.
        ///  Will return "" if unset, the object is invalid, or the object cannot
        ///  have the requested handler.
        /// </summary>
        public static string GetEventScript(NWGameObject oObject, EventScriptModule nHandler)
        {
            return GetEventScript(oObject, (int)nHandler);
        }

        /// <summary>
        ///  Returns the event script for the given object and handler.
        ///  Will return "" if unset, the object is invalid, or the object cannot
        ///  have the requested handler.
        /// </summary>
        public static string GetEventScript(NWGameObject oObject, EventScriptPlaceable nHandler)
        {
            return GetEventScript(oObject, (int)nHandler);
        }

        /// <summary>
        ///  Returns the event script for the given object and handler.
        ///  Will return "" if unset, the object is invalid, or the object cannot
        ///  have the requested handler.
        /// </summary>
        public static string GetEventScript(NWGameObject oObject, EventScriptStore nHandler)
        {
            return GetEventScript(oObject, (int)nHandler);
        }

        /// <summary>
        ///  Returns the event script for the given object and handler.
        ///  Will return "" if unset, the object is invalid, or the object cannot
        ///  have the requested handler.
        /// </summary>
        public static string GetEventScript(NWGameObject oObject, EventScriptTrigger nHandler)
        {
            return GetEventScript(oObject, (int)nHandler);
        }

        /// <summary>
        ///  Sets the given event script for the given object and handler.
        ///  Returns 1 on success, 0 on failure.
        ///  Will fail if oObject is invalid or does not have the requested handler.
        /// </summary>
        public static bool SetEventScript(NWGameObject oObject, EventScriptArea nHandler, string sScript)
        {
            return SetEventScript(oObject, (int)nHandler, sScript);
        }

        /// <summary>
        ///  Sets the given event script for the given object and handler.
        ///  Returns 1 on success, 0 on failure.
        ///  Will fail if oObject is invalid or does not have the requested handler.
        /// </summary>
        public static bool SetEventScript(NWGameObject oObject, EventScriptAreaOfEffect nHandler, string sScript)
        {
            return SetEventScript(oObject, (int)nHandler, sScript);
        }

        /// <summary>
        ///  Sets the given event script for the given object and handler.
        ///  Returns 1 on success, 0 on failure.
        ///  Will fail if oObject is invalid or does not have the requested handler.
        /// </summary>
        public static bool SetEventScript(NWGameObject oObject, EventScriptCreature nHandler, string sScript)
        {
            return SetEventScript(oObject, (int)nHandler, sScript);
        }

        /// <summary>
        ///  Sets the given event script for the given object and handler.
        ///  Returns 1 on success, 0 on failure.
        ///  Will fail if oObject is invalid or does not have the requested handler.
        /// </summary>
        public static bool SetEventScript(NWGameObject oObject, EventScriptDoor nHandler, string sScript)
        {
            return SetEventScript(oObject, (int)nHandler, sScript);
        }

        /// <summary>
        ///  Sets the given event script for the given object and handler.
        ///  Returns 1 on success, 0 on failure.
        ///  Will fail if oObject is invalid or does not have the requested handler.
        /// </summary>
        public static bool SetEventScript(NWGameObject oObject, EventScriptEncounter nHandler, string sScript)
        {
            return SetEventScript(oObject, (int)nHandler, sScript);
        }
        /// <summary>
        ///  Sets the given event script for the given object and handler.
        ///  Returns 1 on success, 0 on failure.
        ///  Will fail if oObject is invalid or does not have the requested handler.
        /// </summary>
        public static bool SetEventScript(NWGameObject oObject, EventScriptModule nHandler, string sScript)
        {
            return SetEventScript(oObject, (int)nHandler, sScript);
        }
        /// <summary>
        ///  Sets the given event script for the given object and handler.
        ///  Returns 1 on success, 0 on failure.
        ///  Will fail if oObject is invalid or does not have the requested handler.
        /// </summary>
        public static bool SetEventScript(NWGameObject oObject, EventScriptPlaceable nHandler, string sScript)
        {
            return SetEventScript(oObject, (int)nHandler, sScript);
        }
        /// <summary>
        ///  Sets the given event script for the given object and handler.
        ///  Returns 1 on success, 0 on failure.
        ///  Will fail if oObject is invalid or does not have the requested handler.
        /// </summary>
        public static bool SetEventScript(NWGameObject oObject, EventScriptStore nHandler, string sScript)
        {
            return SetEventScript(oObject, (int)nHandler, sScript);
        }
        /// <summary>
        ///  Sets the given event script for the given object and handler.
        ///  Returns 1 on success, 0 on failure.
        ///  Will fail if oObject is invalid or does not have the requested handler.
        /// </summary>
        public static bool SetEventScript(NWGameObject oObject, EventScriptTrigger nHandler, string sScript)
        {
            return SetEventScript(oObject, (int)nHandler, sScript);
        }

        /// <summary>
        ///  Display floaty text above the specified creature.
        ///  The text will also appear in the chat buffer of each player that receives the
        ///  floaty text.
        ///  - sStringToDisplay: String
        ///  - oCreatureToFloatAbove
        ///  - bBroadcastToFaction: If this is true then only creatures in the same faction
        ///    as oCreatureToFloatAbove
        ///    will see the floaty text, and only if they are within range (30 metres).
        /// </summary>
        public static void FloatingTextStringOnCreature(string sStringToDisplay, NWGameObject oCreatureToFloatAbove, bool bBroadcastToFaction = false)
        {
            // Note: this method's parameters have been moved around to make the API easier to use. The order in which they are pushed to NWN have not been modified.
            Internal.NativeFunctions.StackPushInteger(bBroadcastToFaction ? 1 : 0);
            Internal.NativeFunctions.StackPushObject(oCreatureToFloatAbove != null ? oCreatureToFloatAbove.Self : NWGameObject.OBJECT_INVALID);
            Internal.NativeFunctions.StackPushString(sStringToDisplay);
            Internal.NativeFunctions.CallBuiltIn(526);
        }

        /// <summary>
        /// Returns the total level of a creature by adding up to three of their classes together.
        /// Returns 0 if there's an error.
        /// </summary>
        /// <param name="creature">The creature to sum levels up for</param>
        /// <returns></returns>
        public static int GetTotalLevel(NWGameObject creature)
        {
            return GetLevelByPosition(ClassPosition.First, creature) +
                   GetLevelByPosition(ClassPosition.Second, creature) +
                   GetLevelByPosition(ClassPosition.Third, creature);
        }

        /// <summary>
        ///  * Returns true if oCreature is a Player Controlled character.
        /// </summary>
        public static bool GetIsPC(NWGameObject oCreature)
        {
            Internal.NativeFunctions.StackPushObject(oCreature != null ? oCreature.Self : NWGameObject.OBJECT_INVALID);
            Internal.NativeFunctions.CallBuiltIn(217);
            return Internal.NativeFunctions.StackPopInteger() == 1;
        }

        /// <summary>
        ///  * Returns true if oCreature is the Dungeon Master.
        ///  Note: This will return false if oCreature is a DM Possessed creature.
        ///  To determine if oCreature is a DM Possessed creature, use GetIsDMPossessed()
        /// </summary>
        public static bool GetIsDM(NWGameObject oCreature)
        {
            Internal.NativeFunctions.StackPushObject(oCreature != null ? oCreature.Self : NWGameObject.OBJECT_INVALID);
            Internal.NativeFunctions.CallBuiltIn(420);
            return Internal.NativeFunctions.StackPopInteger() == 1;
        }

        /// <summary>
        /// Returns true if obj is a player. If obj is a DM, DM-possessed, or any other type of object it will return false.
        /// </summary>
        /// <param name="obj">The object to check</param>
        /// <returns>true if player, false otherwise</returns>
        public static bool GetIsPlayer(NWGameObject obj)
        {
            return GetIsPC(obj) && !GetIsDM(obj) && !GetIsDMPossessed(obj);
        }

        /// <summary>
        /// Returns true if obj is a DM or DM-possessed. Players or any other type of object will return false.
        /// </summary>
        /// <param name="obj">The object to check</param>
        /// <returns>true if DM or DM-possessed, false otherwise</returns>
        public static bool GetIsDungeonMaster(NWGameObject obj)
        {
            return GetIsDM(obj) || GetIsDMPossessed(obj);
        }

        /// <summary>
        /// Returns true if obj is a non-player, non-DM, non-possessed creature. 
        /// </summary>
        /// <param name="obj">The object to check</param>
        /// <returns>true if object is a NPC or false if not</returns>
        public static bool GetIsNPC(NWGameObject obj)
        {
            return !GetIsPlayer(obj) &&
                   !GetIsDungeonMaster(obj) &&
                   GetObjectType(obj) == ObjectType.Creature;
        }

        /// <summary>
        /// Retrieves a unique ID for a given object.
        /// Throws an exception if a player has not been assigned an ID yet.
        /// Assigns a new ID if a non-player has not been assigned an ID yet.
        /// </summary>
        /// <param name="obj">The object to retrieve the ID from</param>
        /// <returns>The ID of the object</returns>
        public static Guid GetGlobalID(NWGameObject obj)
        {
            if (GetIsPC(obj) && !GetIsDM(obj) && !GetIsDMPossessed(obj))
            {
                string tag = GetTag(obj);
                if (String.IsNullOrWhiteSpace(tag))
                    throw new Exception($"Player has not been assigned an ID yet. Player Name: {GetName(obj)}");

                return new Guid(tag);
            }
            else
            {
                var id = GetLocalString(obj, "GLOBAL_ID");
                if (String.IsNullOrWhiteSpace(id))
                {
                    id = Guid.NewGuid().ToString();
                    SetLocalString(obj, "GLOBAL_ID", id);
                }

                return new Guid(id);
            }
        }

        /// <summary>
        /// Gets an area by its resref. Returns OBJECT_INVALID if no area with the given resref can be found.
        /// </summary>
        /// <param name="resRef">The resref to search for.</param>
        /// <returns>An area with the matching resref, or OBJECT_INVALID if no area could be found.</returns>
        public static NWGameObject GetAreaByResRef(string resRef)
        {
            NWGameObject area = GetFirstArea();

            while (GetIsObjectValid(area))
            {
                if (GetResRef(area) == resRef)
                    return area;

                area = GetNextArea();
            }

            return NWGameObject.OBJECT_INVALID;
        }

        /// <summary>
        /// Destroys all items inside an object's inventory.
        /// </summary>
        /// <param name="obj">The objects whose inventory will be wiped.</param>
        public static void DestroyAllInventoryItems(NWGameObject obj)
        {
            NWGameObject item = GetFirstItemInInventory(obj);
            while (GetIsObjectValid(item))
            {
                DestroyObject(item);
                item = GetNextItemInInventory(obj);
            }
        }

        /// <summary>
        /// Returns the number of items in an object's inventory.
        /// Returns -1 if target does not have an inventory
        /// </summary>
        /// <param name="obj">The object to check</param>
        /// <returns>-1 if obj doesn't have an inventory, otherwise returns the number of items in the inventory</returns>
        public static int GetInventoryItemCount(NWGameObject obj)
        {
            if (!GetHasInventory(obj)) return -1;

            int count = 0;
            NWGameObject item = GetFirstItemInInventory(obj);
            while (GetIsObjectValid(item))
            {
                count++;
                item = GetNextItemInInventory(obj);
            }

            return count;
        }

        /// <summary>
        /// If creature is currently busy, returns true.
        /// Otherwise returns false.
        /// </summary>
        /// <param name="creature">The creature to check busy status of</param>
        /// <returns>true if busy, false otherwise</returns>
        public static bool GetIsBusy(NWGameObject creature)
        {
            return Convert.ToBoolean(GetLocalInt(creature, "IS_BUSY"));
        }

        /// <summary>
        /// Sets whether creature is busy.
        /// </summary>
        /// <param name="creature">The creature to change the busy status of</param>
        /// <param name="isBusy">true or false</param>
        public static void SetIsBusy(NWGameObject creature, bool isBusy)
        {
            SetLocalInt(creature, "IS_BUSY", Convert.ToInt32(isBusy));
        }


        /// <summary>
        /// 
        /// ----------------------------------------------------------------------------
        /// Add an item property in a safe fashion, preventing unwanted stacking
        /// Parameters:
        ///   oItem     - the item to add the property to
        ///   ip        - the itemproperty to add
        ///   fDuration - set 0.0f to add the property permanent, anything else is temporary
        ///   nAddItemPropertyPolicy - How to handle existing properties. Valid values are:
        ///	     X2_IP_ADDPROP_POLICY_REPLACE_EXISTING - remove any property of the same type, subtype, durationtype before adding;
        ///	     X2_IP_ADDPROP_POLICY_KEEP_EXISTING - do not add if any property with same type, subtype and durationtype already exists;
        ///	     X2_IP_ADDPROP_POLICY_IGNORE_EXISTING - add itemproperty in any case - Do not Use with OnHit or OnHitSpellCast props!
        ///   bIgnoreDurationType  - If set to true, an item property will be considered identical even if the DurationType is different. Be careful when using this
        ///	                          with X2_IP_ADDPROP_POLICY_REPLACE_EXISTING, as this could lead to a temporary item property removing a permanent one
        ///   bIgnoreSubType       - If set to true an item property will be considered identical even if the SubType is different.
        ///
        /// ----------------------------------------------------------------------------
        /// </summary>
        /// <param name="oItem"></param>
        /// <param name="ip"></param>
        /// <param name="fDuration"></param>
        /// <param name="nAddItemPropertyPolicy"></param>
        /// <param name="bIgnoreDurationType"></param>
        /// <param name="bIgnoreSubType"></param>
        public static void SafeAddItemProperty(NWGameObject oItem, ItemProperty ip, float fDuration, AddItemPropertyPolicy nAddItemPropertyPolicy, bool bIgnoreDurationType, bool bIgnoreSubType)
        {
            var nType = GetItemPropertyType(ip);
            var nSubType = GetItemPropertySubType(ip);
            DurationType nDuration;
            // if duration is 0.0f, make the item property permanent
            if (fDuration == 0.0f)
            {

                nDuration = DurationType.Permanent;
            }
            else
            {

                nDuration = DurationType.Temporary;
            }

            DurationType nDurationCompare = nDuration;
            if (bIgnoreDurationType)
            {
                nDurationCompare = DurationType.Invalid;
            }

            if (nAddItemPropertyPolicy == AddItemPropertyPolicy.ReplaceExisting)
            {

                // remove any matching properties
                if (bIgnoreSubType)
                {
                    nSubType = -1;
                }
                RemoveMatchingItemProperties(oItem, nType, nDurationCompare, nSubType);
            }
            else if (nAddItemPropertyPolicy == AddItemPropertyPolicy.KeepExisting)
            {
                // do not replace existing properties
                if (GetItemHasProperty(oItem, ip, nDurationCompare, bIgnoreSubType))
                {
                    return; // item already has property, return
                }
            }
            else //X2_IP_ADDPROP_POLICY_IGNORE_EXISTING
            {

            }

            if (nDuration == DurationType.Permanent)
            {
                AddItemProperty(nDuration, ip, oItem);
            }
            else
            {
                AddItemProperty(nDuration, ip, oItem, fDuration);
            }
        }



        /// <summary>
        /// // ----------------------------------------------------------------------------
        /// Removes all itemproperties with matching nItemPropertyType and
        /// nItemPropertyDuration (a DURATION_TYPE_* constant)
        /// ----------------------------------------------------------------------------
        /// </summary>
        /// <param name="oItem"></param>
        /// <param name="nItemPropertyType"></param>
        /// <param name="nItemPropertyDuration"></param>
        /// <param name="nItemPropertySubType"></param>
        public static void RemoveMatchingItemProperties(NWGameObject oItem, ItemPropertyType nItemPropertyType, DurationType nItemPropertyDuration, int nItemPropertySubType)
        {
            var prop = GetFirstItemProperty(oItem);

            while (GetIsItemPropertyValid(prop))
            {
                // same property type?
                if (GetItemPropertyType(prop) == nItemPropertyType)
                {
                    // same duration or duration ignored?
                    if (GetItemPropertyDurationType(prop) == nItemPropertyDuration || nItemPropertyDuration == DurationType.Invalid)
                    {
                        // same subtype or subtype ignored
                        if (GetItemPropertySubType(prop) == nItemPropertySubType || nItemPropertySubType == -1)
                        {
                            // Put a warning into the logfile if someone tries to remove a permanent ip with a temporary one!
                            /*if (nItemPropertyDuration == DurationType.Temporary &&  GetItemPropertyDurationType(ip) == DurationType.Permanent)
                            {
                               WriteTimestampedLogEntry("x2_inc_itemprop:: IPRemoveMatchingItemProperties() - WARNING: Permanent item property removed by temporary on "+GetTag(oItem));
                            }
                            */
                            RemoveItemProperty(oItem, prop);
                        }
                    }
                }

                prop = GetNextItemProperty(oItem);
            }
        }


        /// <summary>
        /// Returns true if item has given item property. False otherwise.
        /// </summary>
        /// <param name="oItem"></param>
        /// <param name="ipCompareTo"></param>
        /// <param name="nDurationCompare"></param>
        /// <param name="bIgnoreSubType"></param>
        /// <returns></returns>
        public static bool GetItemHasProperty(NWGameObject oItem, ItemProperty ipCompareTo, DurationType nDurationCompare, bool bIgnoreSubType)
        {
            var prop = GetFirstItemProperty(oItem);
            while (GetIsItemPropertyValid(prop))
            {
                if ((GetItemPropertyType(prop) == GetItemPropertyType(ipCompareTo)))
                {
                    if (GetItemPropertySubType(prop) == GetItemPropertySubType(ipCompareTo) || bIgnoreSubType)
                    {
                        if (GetItemPropertyDurationType(prop) == nDurationCompare || nDurationCompare == DurationType.Invalid)
                        {
                            return true; // if duration is not ignored and durationtypes are equal, true
                        }
                    }
                }

                prop = GetNextItemProperty(oItem);
            }

            return false;
        }

        /// <summary>
        /// Removes all item properties of a given type from an item.
        /// </summary>
        /// <param name="oItem"></param>
        /// <param name="nItemPropertyDuration"></param>
        public static void RemoveAllItemProperties(NWGameObject oItem, DurationType nItemPropertyDuration)
        {
            var prop = GetFirstItemProperty(oItem);
            while (GetIsItemPropertyValid(prop))
            {
                if (GetItemPropertyDurationType(prop) == nItemPropertyDuration)
                {
                    RemoveItemProperty(oItem, prop);
                }

                prop = GetNextItemProperty(oItem);
            }
        }

        /// <summary>
        /// Cause the action subject to play an animation
        ///  - nAnimation: ANIMATION_*
        ///  - fSpeed: Speed of the animation
        ///  - fDurationSeconds: Duration of the animation (this is not used for Fire and
        ///    Forget animations)
        /// </summary>
        /// <param name="nAnimation">The animation to play</param>
        /// <param name="fSpeed">The speed to play the animation at</param>
        /// <param name="fDurationSeconds">How long to play the animation</param>
        public static void ActionPlayAnimation(Animation nAnimation, float fSpeed = 1.0f, float fDurationSeconds = 0.0f)
        {
            Internal.NativeFunctions.StackPushFloat(fDurationSeconds);
            Internal.NativeFunctions.StackPushFloat(fSpeed);
            Internal.NativeFunctions.StackPushInteger((int)nAnimation);
            Internal.NativeFunctions.CallBuiltIn(40);
        }


        /// <summary>
        ///  duplicates the item and returns a new object
        ///  oItem - item to copy
        ///  oTargetInventory - create item in this object's inventory. If this parameter
        ///                     is not valid, the item will be created in oItem's location
        ///  bCopyVars - copy the local variables from the old item to the new one
        ///  * returns the new item
        ///  * returns OBJECT_INVALID for non-items.
        ///  * can only copy empty item containers. will return OBJECT_INVALID if oItem contains
        ///    other items.
        ///  * if it is possible to merge this item with any others in the target location,
        ///    then it will do so and return the merged object.
        /// </summary>
        public static NWGameObject CopyItem(NWGameObject oItem, NWGameObject oTargetInventory = null, bool bCopyVars = true)
        {
            Internal.NativeFunctions.StackPushInteger(bCopyVars ? 1 : 0);
            Internal.NativeFunctions.StackPushObject(oTargetInventory != null ? oTargetInventory.Self : NWGameObject.OBJECT_INVALID);
            Internal.NativeFunctions.StackPushObject(oItem != null ? oItem.Self : NWGameObject.OBJECT_INVALID);
            Internal.NativeFunctions.CallBuiltIn(584);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///  Create an object of the specified type at lLocation.
        ///  - nObjectType: ObjectType.Item, ObjectType.Creature, ObjectType.Placeable,
        ///    OBJECT_TYPE_STORE, ObjectType.Waypoint
        ///  - sTemplate
        ///  - lLocation
        ///  - bUseAppearAnimation
        ///  - sNewTag - if this string is not empty, it will replace the default tag from the template
        /// </summary>
        public static NWGameObject CreateObject(ObjectType nObjectType, string sTemplate, Location lLocation, bool bUseAppearAnimation = false, string sNewTag = "")
        {
            Internal.NativeFunctions.StackPushStringUTF8(sNewTag);
            Internal.NativeFunctions.StackPushInteger(bUseAppearAnimation ? 1 : 0);
            Internal.NativeFunctions.StackPushLocation(lLocation.Handle);
            Internal.NativeFunctions.StackPushStringUTF8(sTemplate);
            Internal.NativeFunctions.StackPushInteger((int)nObjectType);
            Internal.NativeFunctions.CallBuiltIn(243);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///  Determine whether oCreature has nFeat, and nFeat is useable.
        ///  - nFeat: FEAT_*
        ///  - oCreature
        /// </summary>
        public static bool GetHasFeat(Feat nFeat, NWGameObject oCreature)
        {
            Internal.NativeFunctions.StackPushObject(oCreature != null ? oCreature.Self : NWGameObject.OBJECT_INVALID);
            Internal.NativeFunctions.StackPushInteger((int)nFeat);
            Internal.NativeFunctions.CallBuiltIn(285);
            return Internal.NativeFunctions.StackPopInteger() == 1;
        }


        /// <summary>
        ///  Creates a new copy of an item, while making a single change to the appearance of the item.
        ///  Helmet models and simple items ignore iIndex.
        ///  iType                            iIndex                              iNewValue
        ///  ITEM_APPR_TYPE_SIMPLE_MODEL      [Ignored]                           Model #
        ///  ITEM_APPR_TYPE_WEAPON_COLOR      ITEM_APPR_WEAPON_COLOR_*            1-4
        ///  ITEM_APPR_TYPE_WEAPON_MODEL      ITEM_APPR_WEAPON_MODEL_*            Model #
        ///  ItemApprType.ArmorModel       ITEM_APPR_ARMOR_MODEL_*             Model #
        ///  ItemApprType.ArmorColor       ITEM_APPR_ARMOR_COLOR_* [0]         0-175 [1]
        /// 
        ///  [0] Alternatively, where ItemApprType.ArmorColor is specified, if per-part coloring is
        ///  desired, the following equation can be used for nIndex to achieve that:
        /// 
        ///    ITEM_APPR_ARMOR_NUM_COLORS + (ITEM_APPR_ARMOR_MODEL_ * ITEM_APPR_ARMOR_NUM_COLORS) + ITEM_APPR_ARMOR_COLOR_
        /// 
        ///  For example, to change the CLOTH1 channel of the torso, nIndex would be:
        /// 
        ///    6 + (7 * 6) + 2 = 50
        /// 
        ///  [1] When specifying per-part coloring, the value 255 is allowed and corresponds with the logical
        ///  function 'clear colour override', which clears the per-part override for that part.
        /// </summary>
        public static NWGameObject CopyItemAndModify(NWGameObject oItem, ItemApprType nType, ItemApprArmorModel nIndex, int nNewValue, bool bCopyVars = true)
        {
            return CopyItemAndModify(oItem, (int) nType, (int) nIndex, nNewValue, bCopyVars);
        }

        public static NWGameObject CopyItemAndModify(NWGameObject oItem, ItemApprType nType, ItemApprArmorColor nIndex, int nNewValue, bool bCopyVars = true)
        {
            return CopyItemAndModify(oItem, (int)nType, (int)nIndex, nNewValue, bCopyVars);
        }

        public static NWGameObject CopyItemAndModify(NWGameObject oItem, ItemApprType nType, ItemApprType nIndex, int nNewValue, bool bCopyVars = true)
        {
            return CopyItemAndModify(oItem, (int)nType, (int)nIndex, nNewValue, bCopyVars);
        }

        public static NWGameObject CopyItemAndModify(NWGameObject oItem, ItemApprType nType, ItemApprWeaponModel nIndex, int nNewValue, bool bCopyVars = true)
        {
            return CopyItemAndModify(oItem, (int)nType, (int)nIndex, nNewValue, bCopyVars);
        }
        public static NWGameObject CopyItemAndModify(NWGameObject oItem, ItemApprType nType, ItemApprWeaponColor nIndex, int nNewValue, bool bCopyVars = true)
        {
            return CopyItemAndModify(oItem, (int)nType, (int)nIndex, nNewValue, bCopyVars);
        }



        /// <summary>
        ///  Queries the current value of the appearance settings on an item. The parameters are
        ///  identical to those of CopyItemAndModify().
        /// </summary>
        public static int GetItemAppearance(NWGameObject oItem, ItemApprType nType, ItemApprArmorModel nIndex)
        {
            return GetItemAppearance(oItem, (int)nType, (int)nIndex);
        }
        public static int GetItemAppearance(NWGameObject oItem, ItemApprType nType, ItemApprArmorColor nIndex)
        {
            return GetItemAppearance(oItem, (int)nType, (int)nIndex);
        }
        public static int GetItemAppearance(NWGameObject oItem, ItemApprType nType, ItemApprWeaponModel nIndex)
        {
            return GetItemAppearance(oItem, (int)nType, (int)nIndex);
        }
        public static int GetItemAppearance(NWGameObject oItem, ItemApprType nType, ItemApprWeaponColor nIndex)
        {
            return GetItemAppearance(oItem, (int)nType, (int)nIndex);
        }
        public static int GetItemAppearance(NWGameObject oItem, ItemApprType nType, ItemApprType nIndex)
        {
            return GetItemAppearance(oItem, (int)nType, (int)nIndex);
        }

        public static bool GetLocalBoolean(NWGameObject obj, string name)
        {
            int val = GetLocalInt(obj, name);
            return Convert.ToBoolean(val);
        }

        public static void SetLocalBoolean(NWGameObject obj, string name, bool val)
        {
            int converted = Convert.ToInt32(val);
            SetLocalInt(obj, name, converted);
        }

        public static void DeleteLocalBoolean(NWGameObject obj, string name)
        {
            DeleteLocalInt(obj, name);
        }
    }
}
