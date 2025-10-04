using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX.Model;

namespace SWLOR.NWN.API.NWNX
{
    public class ItemPropertyPluginService : IItemPropertyPluginService
    {
        /// <inheritdoc/>
        public ItemPropertyUnpacked UnpackIP(ItemProperty ip)
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

        /// <inheritdoc/>
        public ItemProperty PackIP(ItemPropertyUnpacked itemProperty)
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

        /// <inheritdoc/>
        public ItemPropertyUnpacked GetActiveProperty(uint oItem, int nIndex)
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