using SWLOR.Game.Server.NWN;
using System;

namespace SWLOR.Game.Server.NWNX
{
    public static class NWNXItemProperty
    {
        private const string PLUGIN_NAME = "NWNX_ItemProperty";

        // Convert native itemproperty type to unpacked structure
        public static ItemPropertyUnpacked UnpackIP(ItemProperty ip)
        {
            const string func = "UnpackIP";

            NWNXCore.NWNX_PushArgumentItemProperty(PLUGIN_NAME, func, ip);
            NWNXCore.NWNX_CallFunction(PLUGIN_NAME, func);

            return new ItemPropertyUnpacked
            {
                Property = NWNXCore.NWNX_GetReturnValueInt(PLUGIN_NAME, func),
                SubType = NWNXCore.NWNX_GetReturnValueInt(PLUGIN_NAME, func),
                CostTable = NWNXCore.NWNX_GetReturnValueInt(PLUGIN_NAME, func),
                CostTableValue = NWNXCore.NWNX_GetReturnValueInt(PLUGIN_NAME, func),
                Param1 = NWNXCore.NWNX_GetReturnValueInt(PLUGIN_NAME, func),
                Param1Value = NWNXCore.NWNX_GetReturnValueInt(PLUGIN_NAME, func),
                UsesPerDay = NWNXCore.NWNX_GetReturnValueInt(PLUGIN_NAME, func),
                ChanceToAppear = NWNXCore.NWNX_GetReturnValueInt(PLUGIN_NAME, func),
                IsUseable = Convert.ToBoolean(NWNXCore.NWNX_GetReturnValueInt(PLUGIN_NAME, func)),
                SpellID = NWNXCore.NWNX_GetReturnValueInt(PLUGIN_NAME, func),
                Creator = NWNXCore.NWNX_GetReturnValueObject(PLUGIN_NAME, func),
                Tag = NWNXCore.NWNX_GetReturnValueString(PLUGIN_NAME, func)
            };
        }

        // Convert unpacked itemproperty structure to native type.
        public static ItemProperty PackIP(ItemPropertyUnpacked itemProperty)
        {
            const string sFunc = "PackIP";

            NWNXCore.NWNX_PushArgumentString(PLUGIN_NAME, sFunc, itemProperty.Tag);
            NWNXCore.NWNX_PushArgumentObject(PLUGIN_NAME, sFunc, itemProperty.Creator);
            NWNXCore.NWNX_PushArgumentInt(PLUGIN_NAME, sFunc, itemProperty.SpellID);
            NWNXCore.NWNX_PushArgumentInt(PLUGIN_NAME, sFunc, itemProperty.IsUseable ? 1 : 0);
            NWNXCore.NWNX_PushArgumentInt(PLUGIN_NAME, sFunc, itemProperty.ChanceToAppear);
            NWNXCore.NWNX_PushArgumentInt(PLUGIN_NAME, sFunc, itemProperty.UsesPerDay);
            NWNXCore.NWNX_PushArgumentInt(PLUGIN_NAME, sFunc, itemProperty.Param1Value);
            NWNXCore.NWNX_PushArgumentInt(PLUGIN_NAME, sFunc, itemProperty.Param1);
            NWNXCore.NWNX_PushArgumentInt(PLUGIN_NAME, sFunc, itemProperty.CostTableValue);
            NWNXCore.NWNX_PushArgumentInt(PLUGIN_NAME, sFunc, itemProperty.CostTable);
            NWNXCore.NWNX_PushArgumentInt(PLUGIN_NAME, sFunc, itemProperty.SubType);
            NWNXCore.NWNX_PushArgumentInt(PLUGIN_NAME, sFunc, itemProperty.Property);

            NWNXCore.NWNX_CallFunction(PLUGIN_NAME, sFunc);
            return NWNXCore.NWNX_GetReturnValueItemProperty(PLUGIN_NAME, sFunc);
        }
    }
}
