using System;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.Service
{
    public static class ItemPlugin
    {
        private static IItemPluginService _service = new ItemPluginService();

        internal static void SetService(IItemPluginService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <inheritdoc cref="IItemPluginService.SetWeight"/>
        public static void SetWeight(uint oItem, int w) => _service.SetWeight(oItem, w);

        /// <inheritdoc cref="IItemPluginService.SetBaseGoldPieceValue"/>
        public static void SetBaseGoldPieceValue(uint oItem, int g) => _service.SetBaseGoldPieceValue(oItem, g);

        /// <inheritdoc cref="IItemPluginService.SetAddGoldPieceValue"/>
        public static void SetAddGoldPieceValue(uint oItem, int g) => _service.SetAddGoldPieceValue(oItem, g);

        /// <inheritdoc cref="IItemPluginService.GetBaseGoldPieceValue"/>
        public static int GetBaseGoldPieceValue(uint oItem) => _service.GetBaseGoldPieceValue(oItem);

        /// <inheritdoc cref="IItemPluginService.GetAddGoldPieceValue"/>
        public static int GetAddGoldPieceValue(uint oItem) => _service.GetAddGoldPieceValue(oItem);

        /// <inheritdoc cref="IItemPluginService.SetBaseItemType"/>
        public static void SetBaseItemType(uint oItem, BaseItemType baseitem) => _service.SetBaseItemType(oItem, baseitem);

        /// <inheritdoc cref="IItemPluginService.SetItemAppearance"/>
        public static void SetItemAppearance(uint oItem, ItemModelColorType nType, int nIndex, int nValue, bool updateCreatureAppearance = false) => _service.SetItemAppearance(oItem, nType, nIndex, nValue, updateCreatureAppearance);

        /// <inheritdoc cref="IItemPluginService.GetEntireItemAppearance"/>
        public static string GetEntireItemAppearance(uint oItem) => _service.GetEntireItemAppearance(oItem);

        /// <inheritdoc cref="IItemPluginService.RestoreItemAppearance"/>
        public static void RestoreItemAppearance(uint oItem, string sApp) => _service.RestoreItemAppearance(oItem, sApp);

        /// <inheritdoc cref="IItemPluginService.GetBaseArmorClass"/>
        public static int GetBaseArmorClass(uint oItem) => _service.GetBaseArmorClass(oItem);

        /// <inheritdoc cref="IItemPluginService.GetMinEquipLevel"/>
        public static int GetMinEquipLevel(uint oItem) => _service.GetMinEquipLevel(oItem);

        /// <inheritdoc cref="IItemPluginService.MoveTo"/>
        public static bool MoveTo(uint oItem, uint oLocation, bool bHideAllFeedback = false) => _service.MoveTo(oItem, oLocation, bHideAllFeedback);

        /// <inheritdoc cref="IItemPluginService.SetMinEquipLevelModifier"/>
        public static void SetMinEquipLevelModifier(uint oItem, int nModifier, bool bPersist = true) => _service.SetMinEquipLevelModifier(oItem, nModifier, bPersist);

        /// <inheritdoc cref="IItemPluginService.GetMinEquipLevelModifier"/>
        public static int GetMinEquipLevelModifier(uint oItem) => _service.GetMinEquipLevelModifier(oItem);

        /// <inheritdoc cref="IItemPluginService.SetMinEquipLevelOverride"/>
        public static void SetMinEquipLevelOverride(uint oItem, int nOverride, bool bPersist = true) => _service.SetMinEquipLevelOverride(oItem, nOverride, bPersist);

        /// <inheritdoc cref="IItemPluginService.GetMinEquipLevelOverride"/>
        public static int GetMinEquipLevelOverride(uint oItem) => _service.GetMinEquipLevelOverride(oItem);
    }
}
