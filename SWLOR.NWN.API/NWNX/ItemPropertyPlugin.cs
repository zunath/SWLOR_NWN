using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX.Enum;

namespace SWLOR.NWN.API.NWNX
{
    public static class ItemPropertyPlugin
    {
        /// <summary>
        /// Convert native itemproperty type to unpacked structure.
        /// </summary>
        /// <param name="ip">The itemproperty to convert.</param>
        /// <returns>A constructed ItemPropertyUnpacked.</returns>
        public static ItemPropertyUnpacked UnpackIP(ItemProperty ip)
        {
            var nwnxResult = global::NWN.Core.NWNX.ItempropPlugin.UnpackIP(ip);
            
            return new ItemPropertyUnpacked
            {
                Id = nwnxResult.sID,
                Property = nwnxResult.nProperty,
                SubType = nwnxResult.nSubType,
                CostTable = nwnxResult.nCostTable,
                CostTableValue = nwnxResult.nCostTableValue,
                Param1 = nwnxResult.nParam1,
                Param1Value = nwnxResult.nParam1Value,
                UsesPerDay = nwnxResult.nUsesPerDay,
                ChanceToAppear = nwnxResult.nChanceToAppear,
                IsUseable = nwnxResult.bUsable != 0,
                SpellId = nwnxResult.nSpellId,
                Creator = nwnxResult.oCreator,
                Tag = nwnxResult.sTag
            };
        }

        /// <summary>
        /// Convert unpacked itemproperty structure to native type.
        /// </summary>
        /// <param name="itemProperty">The ItemPropertyUnpacked structure to convert.</param>
        /// <returns>The itemproperty.</returns>
        public static ItemProperty PackIP(ItemPropertyUnpacked itemProperty)
        {
            var nwnxInput = new global::NWN.Core.NWNX.NWNX_IPUnpacked
            {
                sID = itemProperty.Id,
                nProperty = itemProperty.Property,
                nSubType = itemProperty.SubType,
                nCostTable = itemProperty.CostTable,
                nCostTableValue = itemProperty.CostTableValue,
                nParam1 = itemProperty.Param1,
                nParam1Value = itemProperty.Param1Value,
                nUsesPerDay = itemProperty.UsesPerDay,
                nChanceToAppear = itemProperty.ChanceToAppear,
                bUsable = itemProperty.IsUseable ? 1 : 0,
                nSpellId = itemProperty.SpellId,
                oCreator = itemProperty.Creator,
                sTag = itemProperty.Tag
            };
            
            return global::NWN.Core.NWNX.ItempropPlugin.PackIP(nwnxInput);
        }

        /// <summary>
        /// Gets the active item property at the index.
        /// </summary>
        /// <param name="oItem">The item with the property.</param>
        /// <param name="nIndex">The index such as returned by some Item Events.</param>
        /// <returns>A constructed ItemPropertyUnpacked, except for creator, and spell id.</returns>
        public static ItemPropertyUnpacked GetActiveProperty(uint oItem, int nIndex)
        {
            var nwnxResult = global::NWN.Core.NWNX.ItempropPlugin.GetActiveProperty(oItem, nIndex);
            
            return new ItemPropertyUnpacked
            {
                Id = string.Empty, // Not returned by GetActiveProperty
                Property = nwnxResult.nProperty,
                SubType = nwnxResult.nSubType,
                CostTable = nwnxResult.nCostTable,
                CostTableValue = nwnxResult.nCostTableValue,
                Param1 = nwnxResult.nParam1,
                Param1Value = nwnxResult.nParam1Value,
                UsesPerDay = nwnxResult.nUsesPerDay,
                ChanceToAppear = nwnxResult.nChanceToAppear,
                IsUseable = nwnxResult.bUsable != 0,
                SpellId = 0, // Not returned by GetActiveProperty
                Creator = 0, // Not returned by GetActiveProperty
                Tag = nwnxResult.sTag
            };
        }
    }
}