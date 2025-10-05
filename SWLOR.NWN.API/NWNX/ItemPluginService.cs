using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWNX
{
    public class ItemPluginService : IItemPluginService
    {
        /// <inheritdoc/>
        public void SetWeight(uint oItem, int w)
        {
            global::NWN.Core.NWNX.ItemPlugin.SetWeight(oItem, w);
        }

        /// <inheritdoc/>
        public void SetBaseGoldPieceValue(uint oItem, int g)
        {
            global::NWN.Core.NWNX.ItemPlugin.SetBaseGoldPieceValue(oItem, g);
        }

        /// <inheritdoc/>
        public void SetAddGoldPieceValue(uint oItem, int g)
        {
            global::NWN.Core.NWNX.ItemPlugin.SetAddGoldPieceValue(oItem, g);
        }

        /// <inheritdoc/>
        public int GetBaseGoldPieceValue(uint oItem)
        {
            return global::NWN.Core.NWNX.ItemPlugin.GetBaseGoldPieceValue(oItem);
        }

        /// <inheritdoc/>
        public int GetAddGoldPieceValue(uint oItem)
        {
            return global::NWN.Core.NWNX.ItemPlugin.GetAddGoldPieceValue(oItem);
        }

        /// <inheritdoc/>
        public void SetBaseItemType(uint oItem, BaseItemType baseitem)
        {
            global::NWN.Core.NWNX.ItemPlugin.SetBaseItemType(oItem, (int)baseitem);
        }

        /// <inheritdoc/>
        public void SetItemAppearance(uint oItem, ItemModelColorType nType, int nIndex, int nValue, bool updateCreatureAppearance = false)
        {
            global::NWN.Core.NWNX.ItemPlugin.SetItemAppearance(oItem, (int)nType, nIndex, nValue, updateCreatureAppearance ? 1 : 0);
        }

        /// <inheritdoc/>
        public string GetEntireItemAppearance(uint oItem)
        {
            return global::NWN.Core.NWNX.ItemPlugin.GetEntireItemAppearance(oItem);
        }

        /// <inheritdoc/>
        public void RestoreItemAppearance(uint oItem, string sApp)
        {
            global::NWN.Core.NWNX.ItemPlugin.RestoreItemAppearance(oItem, sApp);
        }

        /// <inheritdoc/>
        public int GetBaseArmorClass(uint oItem)
        {
            return global::NWN.Core.NWNX.ItemPlugin.GetBaseArmorClass(oItem);
        }

        /// <inheritdoc/>
        public int GetMinEquipLevel(uint oItem)
        {
            return global::NWN.Core.NWNX.ItemPlugin.GetMinEquipLevel(oItem);
        }

        /// <inheritdoc/>
        public bool MoveTo(uint oItem, uint oLocation, bool bHideAllFeedback = false)
        {
            var result = global::NWN.Core.NWNX.ItemPlugin.MoveTo(oItem, oLocation, bHideAllFeedback ? 1 : 0);
            return result != 0;
        }

        /// <inheritdoc/>
        public void SetMinEquipLevelModifier(uint oItem, int nModifier, bool bPersist = true)
        {
            global::NWN.Core.NWNX.ItemPlugin.SetMinEquipLevelModifier(oItem, nModifier, bPersist ? 1 : 0);
        }

        /// <inheritdoc/>
        public int GetMinEquipLevelModifier(uint oItem)
        {
            return global::NWN.Core.NWNX.ItemPlugin.GetMinEquipLevelModifier(oItem);
        }

        /// <inheritdoc/>
        public void SetMinEquipLevelOverride(uint oItem, int nOverride, bool bPersist = true)
        {
            global::NWN.Core.NWNX.ItemPlugin.SetMinEquipLevelOverride(oItem, nOverride, bPersist ? 1 : 0);
        }

        /// <inheritdoc/>
        public int GetMinEquipLevelOverride(uint oItem)
        {
            return global::NWN.Core.NWNX.ItemPlugin.GetMinEquipLevelOverride(oItem);
        }
    }
}