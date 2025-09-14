using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Core.Bioware
{
    public enum AddItemPropertyPolicy
    {
        ReplaceExisting,
        KeepExisting,
        IgnoreExisting
    }

    /// <summary>
    /// Code from Bioware's XP2 include files, converted to C#.
    /// </summary>
    public static class BiowareXP2
    {
        /// <summary>
        /// Add an item property in a safe fashion, preventing unwanted stacking
        /// </summary>
        /// <param name="oItem">the item to add the property to</param>
        /// <param name="ip">the itemproperty to add</param>
        /// <param name="fDuration">set 0.0f to add the property permanent, anything else is temporary</param>
        /// <param name="nAddItemPropertyPolicy">How to handle existing properties. Valid values are:
        ///	     X2_IP_ADDPROP_POLICY_REPLACE_EXISTING - remove any property of the same type, subtype, durationtype before adding;
        ///	     X2_IP_ADDPROP_POLICY_KEEP_EXISTING - do not add if any property with same type, subtype and durationtype already exists;
        ///	     X2_IP_ADDPROP_POLICY_IGNORE_EXISTING - add itemproperty in any case - Do not Use with OnHit or OnHitSpellCast props!</param>
        /// <param name="bIgnoreDurationType">If set to true, an item property will be considered identical even if the DurationType is different. Be careful when using this with X2_IP_ADDPROP_POLICY_REPLACE_EXISTING, as this could lead to a temporary item property removing a permanent one</param>
        /// <param name="bIgnoreSubType">If set to true an item property will be considered identical even if the SubType is different.</param>
        public static void IPSafeAddItemProperty(uint oItem, ItemProperty ip, float fDuration, AddItemPropertyPolicy nAddItemPropertyPolicy, bool bIgnoreDurationType, bool bIgnoreSubType)
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

            var nDurationCompare = nDuration;
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
                IPRemoveMatchingItemProperties(oItem, nType, nDurationCompare, nSubType);
            }
            else if (nAddItemPropertyPolicy == AddItemPropertyPolicy.KeepExisting)
            {
                // do not replace existing properties
                if (IPGetItemHasProperty(oItem, ip, nDurationCompare, bIgnoreSubType))
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
        public static void IPRemoveMatchingItemProperties(uint oItem, ItemPropertyType nItemPropertyType, DurationType nItemPropertyDuration, int nItemPropertySubType)
        {
            for(var prop = GetFirstItemProperty(oItem); GetIsItemPropertyValid(prop); prop = GetNextItemProperty(oItem))
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
        public static bool IPGetItemHasProperty(uint oItem, ItemProperty ipCompareTo, DurationType nDurationCompare, bool bIgnoreSubType)
        {
            for(var ip = GetFirstItemProperty(oItem); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(oItem))
            {
                if ((GetItemPropertyType(ip) == GetItemPropertyType(ipCompareTo)))
                {
                    if (GetItemPropertySubType(ip) == GetItemPropertySubType(ipCompareTo) || bIgnoreSubType)
                    {
                        if (GetItemPropertyDurationType(ip) == nDurationCompare || nDurationCompare == DurationType.Invalid)
                        {
                            return true; // if duration is not ignored and durationtypes are equal, true
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Removes all item properties of a given type from an item.
        /// </summary>
        /// <param name="oItem"></param>
        /// <param name="nItemPropertyDuration"></param>
        public static void IPRemoveAllItemProperties(uint oItem, DurationType nItemPropertyDuration)
        {
            for(var prop = GetFirstItemProperty(oItem); GetIsItemPropertyValid(prop); prop = GetNextItemProperty(oItem))
            {
                GetItemPropertyDurationType(prop);
                if (GetItemPropertyDurationType(prop) == nItemPropertyDuration)
                {
                    RemoveItemProperty(oItem, prop);
                }
            }
        }

    }
}
