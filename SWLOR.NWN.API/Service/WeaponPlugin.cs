using System;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.Service
{
    public static class WeaponPlugin
    {
        private static IWeaponPluginService _service = new WeaponPluginService();

        internal static void SetService(IWeaponPluginService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <inheritdoc cref="IWeaponPluginService.SetWeaponFocusFeat"/>
        public static void SetWeaponFocusFeat(BaseItemType baseItem, FeatType feat) => _service.SetWeaponFocusFeat(baseItem, feat);

        /// <inheritdoc cref="IWeaponPluginService.SetEpicWeaponFocusFeat"/>
        public static void SetEpicWeaponFocusFeat(BaseItemType baseItem, FeatType feat) => _service.SetEpicWeaponFocusFeat(baseItem, feat);

        /// <inheritdoc cref="IWeaponPluginService.SetGreaterWeaponFocusFeat"/>
        public static void SetGreaterWeaponFocusFeat(BaseItemType baseItem, FeatType feat) => _service.SetGreaterWeaponFocusFeat(baseItem, feat);

        /// <inheritdoc cref="IWeaponPluginService.SetWeaponFinesseSize"/>
        public static void SetWeaponFinesseSize(BaseItemType baseItem, CreatureSizeType nCreatureSize) => _service.SetWeaponFinesseSize(baseItem, nCreatureSize);

        /// <inheritdoc cref="IWeaponPluginService.SetWeaponUnarmed"/>
        public static void SetWeaponUnarmed(BaseItemType baseItem) => _service.SetWeaponUnarmed(baseItem);

        /// <inheritdoc cref="IWeaponPluginService.SetWeaponIsMonkWeapon"/>
        public static void SetWeaponIsMonkWeapon(BaseItemType baseItem) => _service.SetWeaponIsMonkWeapon(baseItem);

        /// <inheritdoc cref="IWeaponPluginService.SetWeaponImprovedCriticalFeat"/>
        public static void SetWeaponImprovedCriticalFeat(BaseItemType baseItem, FeatType feat) => _service.SetWeaponImprovedCriticalFeat(baseItem, feat);

        /// <inheritdoc cref="IWeaponPluginService.SetWeaponSpecializationFeat"/>
        public static void SetWeaponSpecializationFeat(BaseItemType baseItem, FeatType feat) => _service.SetWeaponSpecializationFeat(baseItem, feat);

        /// <inheritdoc cref="IWeaponPluginService.SetGreaterWeaponSpecializationFeat"/>
        public static void SetGreaterWeaponSpecializationFeat(BaseItemType baseItem, FeatType feat) => _service.SetGreaterWeaponSpecializationFeat(baseItem, feat);

        /// <inheritdoc cref="IWeaponPluginService.SetEpicWeaponSpecializationFeat"/>
        public static void SetEpicWeaponSpecializationFeat(BaseItemType baseItem, FeatType feat) => _service.SetEpicWeaponSpecializationFeat(baseItem, feat);

        /// <inheritdoc cref="IWeaponPluginService.SetEpicWeaponOverwhelmingCriticalFeat"/>
        public static void SetEpicWeaponOverwhelmingCriticalFeat(BaseItemType baseItem, FeatType feat) => _service.SetEpicWeaponOverwhelmingCriticalFeat(baseItem, feat);

        /// <inheritdoc cref="IWeaponPluginService.SetEpicWeaponDevastatingCriticalFeat"/>
        public static void SetEpicWeaponDevastatingCriticalFeat(BaseItemType baseItem, FeatType feat) => _service.SetEpicWeaponDevastatingCriticalFeat(baseItem, feat);

        /// <inheritdoc cref="IWeaponPluginService.SetWeaponOfChoiceFeat"/>
        public static void SetWeaponOfChoiceFeat(BaseItemType baseItem, FeatType feat) => _service.SetWeaponOfChoiceFeat(baseItem, feat);

        /// <inheritdoc cref="IWeaponPluginService.SetOption"/>
        public static void SetOption(int nOption, int nVal) => _service.SetOption(nOption, nVal);

        /// <inheritdoc cref="IWeaponPluginService.SetDevastatingCriticalEventScript"/>
        public static void SetDevastatingCriticalEventScript(string sScript) => _service.SetDevastatingCriticalEventScript(sScript);

        /// <inheritdoc cref="IWeaponPluginService.BypassDevastatingCritical"/>
        public static void BypassDevastatingCritical() => _service.BypassDevastatingCritical();

        /// <inheritdoc cref="IWeaponPluginService.GetDevastatingCriticalEventData"/>
        public static DevastatingCriticalData GetDevastatingCriticalEventData() => _service.GetDevastatingCriticalEventData();

        /// <inheritdoc cref="IWeaponPluginService.SetOneHalfStrength"/>
        public static void SetOneHalfStrength(uint oWeapon, bool nEnable, bool bPersist = false) => _service.SetOneHalfStrength(oWeapon, nEnable, bPersist);

        /// <inheritdoc cref="IWeaponPluginService.GetOneHalfStrength"/>
        public static int GetOneHalfStrength(uint oWeapon) => _service.GetOneHalfStrength(oWeapon);

        /// <inheritdoc cref="IWeaponPluginService.SetMaxRangedAttackDistanceOverride"/>
        public static void SetMaxRangedAttackDistanceOverride(BaseItemType nBaseItem, float fMax, float fMaxPassive, float fPreferred) => _service.SetMaxRangedAttackDistanceOverride(nBaseItem, fMax, fMaxPassive, fPreferred);
    }
}
