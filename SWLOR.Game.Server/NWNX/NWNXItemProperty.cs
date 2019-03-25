using NWN;
using System;
using static SWLOR.Game.Server.NWNX.NWNXCore;

namespace SWLOR.Game.Server.NWNX
{
    public static class NWNXItemProperty
    {
        private const string NWNX_ItemProperty = "NWNX_ItemProperty";

        public static ItemPropertyUnpacked UnpackIP(ItemProperty ip)
        {
            const string FunctionName = "UnpackIP";

            NWNX_PushArgumentItemProperty(NWNX_ItemProperty, FunctionName, ip);
            NWNX_CallFunction(NWNX_ItemProperty, FunctionName);

            var n = new ItemPropertyUnpacked
            {
                ItemPropertyID = NWNX_GetReturnValueInt(NWNX_ItemProperty, FunctionName),
                Property = NWNX_GetReturnValueInt(NWNX_ItemProperty, FunctionName),
                SubType = NWNX_GetReturnValueInt(NWNX_ItemProperty, FunctionName),
                CostTable = NWNX_GetReturnValueInt(NWNX_ItemProperty, FunctionName),
                CostTableValue = NWNX_GetReturnValueInt(NWNX_ItemProperty, FunctionName),
                Param1 = NWNX_GetReturnValueInt(NWNX_ItemProperty, FunctionName),
                Param1Value = NWNX_GetReturnValueInt(NWNX_ItemProperty, FunctionName),
                UsesPerDay = NWNX_GetReturnValueInt(NWNX_ItemProperty, FunctionName),
                ChanceToAppear = NWNX_GetReturnValueInt(NWNX_ItemProperty, FunctionName),
                IsUseable = Convert.ToBoolean(NWNX_GetReturnValueInt(NWNX_ItemProperty, FunctionName)),
                SpellID = NWNX_GetReturnValueInt(NWNX_ItemProperty, FunctionName),
                Creator = NWNX_GetReturnValueObject(NWNX_ItemProperty, FunctionName),
                Tag = NWNX_GetReturnValueString(NWNX_ItemProperty, FunctionName)
            };

            return n;
        }

        public static ItemProperty PackIP(ItemPropertyUnpacked n)
        {
            const string sFunc = "PackIP";

            NWNX_PushArgumentString(NWNX_ItemProperty, sFunc, n.Tag);
            NWNX_PushArgumentObject(NWNX_ItemProperty, sFunc, n.Creator);
            NWNX_PushArgumentInt(NWNX_ItemProperty, sFunc, n.SpellID);
            NWNX_PushArgumentInt(NWNX_ItemProperty, sFunc, n.IsUseable ? 1 : 0);
            NWNX_PushArgumentInt(NWNX_ItemProperty, sFunc, n.ChanceToAppear);
            NWNX_PushArgumentInt(NWNX_ItemProperty, sFunc, n.UsesPerDay);
            NWNX_PushArgumentInt(NWNX_ItemProperty, sFunc, n.Param1Value);
            NWNX_PushArgumentInt(NWNX_ItemProperty, sFunc, n.Param1);
            NWNX_PushArgumentInt(NWNX_ItemProperty, sFunc, n.CostTableValue);
            NWNX_PushArgumentInt(NWNX_ItemProperty, sFunc, n.CostTable);
            NWNX_PushArgumentInt(NWNX_ItemProperty, sFunc, n.SubType);
            NWNX_PushArgumentInt(NWNX_ItemProperty, sFunc, n.Property);
            NWNX_PushArgumentInt(NWNX_ItemProperty, sFunc, n.ItemPropertyID);

            NWNX_CallFunction(NWNX_ItemProperty, sFunc);
            return NWNX_GetReturnValueItemProperty(NWNX_ItemProperty, sFunc);
        }

    }
}
