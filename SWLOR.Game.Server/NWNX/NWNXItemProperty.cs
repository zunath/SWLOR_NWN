using SWLOR.Game.Server.NWN;
using System;
using SWLOR.Game.Server.NWN;
using static SWLOR.Game.Server.NWNX.NWNXCore;

namespace SWLOR.Game.Server.NWNX
{
    public static class NWNXItemProperty
    {
        private const string PLUGIN_NAME = "NWNX_ItemProperty";

        // Convert native itemproperty type to unpacked structure
        public static ItemPropertyUnpacked UnpackIP(ItemProperty ip)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "UnpackIP");

            Internal.NativeFunctions.nwnxPushItemProperty(ip.Handle);
            Internal.NativeFunctions.nwnxCallFunction();

            return new ItemPropertyUnpacked
            {
                ItemPropertyID = Internal.NativeFunctions.nwnxPopInt(),
                Property = Internal.NativeFunctions.nwnxPopInt(),
                SubType = Internal.NativeFunctions.nwnxPopInt(),
                CostTable = Internal.NativeFunctions.nwnxPopInt(),
                CostTableValue = Internal.NativeFunctions.nwnxPopInt(),
                Param1 = Internal.NativeFunctions.nwnxPopInt(),
                Param1Value = Internal.NativeFunctions.nwnxPopInt(),
                UsesPerDay = Internal.NativeFunctions.nwnxPopInt(),
                ChanceToAppear = Internal.NativeFunctions.nwnxPopInt(),
                IsUseable = Convert.ToBoolean(Internal.NativeFunctions.nwnxPopInt()),
                SpellID = Internal.NativeFunctions.nwnxPopInt(),
                Creator = Internal.NativeFunctions.nwnxPopObject(),
                Tag = Internal.NativeFunctions.nwnxPopString()
            };
        }

        // Convert unpacked itemproperty structure to native type.
        public static ItemProperty PackIP(ItemPropertyUnpacked itemProperty)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "PackIP");
            Internal.NativeFunctions.nwnxPushString(itemProperty.Tag);
            Internal.NativeFunctions.nwnxPushObject(itemProperty.Creator ?? Internal.OBJECT_INVALID);
            Internal.NativeFunctions.nwnxPushInt(itemProperty.SpellID);
            Internal.NativeFunctions.nwnxPushInt(itemProperty.IsUseable ? 1 : 0);
            Internal.NativeFunctions.nwnxPushInt(itemProperty.ChanceToAppear);
            Internal.NativeFunctions.nwnxPushInt(itemProperty.UsesPerDay);
            Internal.NativeFunctions.nwnxPushInt(itemProperty.Param1Value);
            Internal.NativeFunctions.nwnxPushInt(itemProperty.Param1);
            Internal.NativeFunctions.nwnxPushInt(itemProperty.CostTableValue);
            Internal.NativeFunctions.nwnxPushInt(itemProperty.CostTable);
            Internal.NativeFunctions.nwnxPushInt(itemProperty.SubType);
            Internal.NativeFunctions.nwnxPushInt(itemProperty.Property);
            Internal.NativeFunctions.nwnxPushInt(itemProperty.ItemPropertyID);
            Internal.NativeFunctions.nwnxCallFunction();
            return new ItemProperty(Internal.NativeFunctions.nwnxPopItemProperty());
        }
    }
}
