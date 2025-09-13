using System;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.NWN.API;

namespace SWLOR.Game.Server.Core.NWNX
{
    public static class ItemPropertyPlugin
    {
        private const string PLUGIN_NAME = "NWNX_ItemProperty";

        // Convert native itemproperty type to unpacked structure
        public static ItemPropertyUnpacked UnpackIP(ItemProperty ip)
        {
            const string func = "UnpackIP";

            NWNXCore.NWNX_PushArgumentItemProperty(ip);
            NWNXCore.NWNX_CallFunction(PLUGIN_NAME, func);

            return new ItemPropertyUnpacked
            {
                Id = NWNXCore.NWNX_GetReturnValueString(),
                Property = NWNXCore.NWNX_GetReturnValueInt(),
                SubType = NWNXCore.NWNX_GetReturnValueInt(),
                CostTable = NWNXCore.NWNX_GetReturnValueInt(),
                CostTableValue = NWNXCore.NWNX_GetReturnValueInt(),
                Param1 = NWNXCore.NWNX_GetReturnValueInt(),
                Param1Value = NWNXCore.NWNX_GetReturnValueInt(),
                UsesPerDay = NWNXCore.NWNX_GetReturnValueInt(),
                ChanceToAppear = NWNXCore.NWNX_GetReturnValueInt(),
                IsUseable = Convert.ToBoolean(NWNXCore.NWNX_GetReturnValueInt()),
                SpellId = NWNXCore.NWNX_GetReturnValueInt(),
                Creator = NWNXCore.NWNX_GetReturnValueObject(),
                Tag = NWNXCore.NWNX_GetReturnValueString()
            };
        }

        // Convert unpacked itemproperty structure to native type.
        public static ItemProperty PackIP(ItemPropertyUnpacked itemProperty)
        {
            const string sFunc = "PackIP";

            NWNXCore.NWNX_PushArgumentString(itemProperty.Tag);
            NWNXCore.NWNX_PushArgumentObject(itemProperty.Creator);
            NWNXCore.NWNX_PushArgumentInt(itemProperty.SpellId);
            NWNXCore.NWNX_PushArgumentInt(itemProperty.IsUseable ? 1 : 0);
            NWNXCore.NWNX_PushArgumentInt(itemProperty.ChanceToAppear);
            NWNXCore.NWNX_PushArgumentInt(itemProperty.UsesPerDay);
            NWNXCore.NWNX_PushArgumentInt(itemProperty.Param1Value);
            NWNXCore.NWNX_PushArgumentInt(itemProperty.Param1);
            NWNXCore.NWNX_PushArgumentInt(itemProperty.CostTableValue);
            NWNXCore.NWNX_PushArgumentInt(itemProperty.CostTable);
            NWNXCore.NWNX_PushArgumentInt(itemProperty.SubType);
            NWNXCore.NWNX_PushArgumentInt(itemProperty.Property);

            NWNXCore.NWNX_CallFunction(PLUGIN_NAME, sFunc);
            return NWNXCore.NWNX_GetReturnValueItemProperty();
        }

        /// @brief Gets the active item property at the index
        /// @param oItem - the item with the property
        /// @param nIndex - the index such as returned by some Item Events
        /// @return A constructed NWNX_IPUnpacked, except for creator, and spell id.
        public static ItemPropertyUnpacked GetActiveProperty(uint oItem, int nIndex)
        {
            const string sFunc = "GetActiveProperty";

            NWNXCore.NWNX_PushArgumentInt(nIndex);
            NWNXCore.NWNX_PushArgumentObject(oItem);
            NWNXCore.NWNX_CallFunction(PLUGIN_NAME, sFunc);

            return new ItemPropertyUnpacked
            {
                Property = NWNXCore.NWNX_GetReturnValueInt(),
                SubType = NWNXCore.NWNX_GetReturnValueInt(),
                CostTable = NWNXCore.NWNX_GetReturnValueInt(),
                CostTableValue = NWNXCore.NWNX_GetReturnValueInt(),
                Param1 = NWNXCore.NWNX_GetReturnValueInt(),
                Param1Value = NWNXCore.NWNX_GetReturnValueInt(),
                UsesPerDay = NWNXCore.NWNX_GetReturnValueInt(),
                ChanceToAppear = NWNXCore.NWNX_GetReturnValueInt(),
                IsUseable = Convert.ToBoolean(NWNXCore.NWNX_GetReturnValueInt()),
                Tag = NWNXCore.NWNX_GetReturnValueString()
            };
        }
    }
}