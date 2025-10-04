using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWNX
{
    public class WeaponPluginService : IWeaponPluginService
    {
        /// <inheritdoc/>
        public void SetWeaponFocusFeat(BaseItemType baseItem, FeatType feat)
        {
            global::NWN.Core.NWNX.WeaponPlugin.SetWeaponFocusFeat((int)baseItem, (int)feat);
        }

        /// <inheritdoc/>
        public void SetEpicWeaponFocusFeat(BaseItemType baseItem, FeatType feat)
        {
            global::NWN.Core.NWNX.WeaponPlugin.SetEpicWeaponFocusFeat((int)baseItem, (int)feat);
        }

        /// <inheritdoc/>
        public void SetGreaterWeaponFocusFeat(BaseItemType baseItem, FeatType feat)
        {
            global::NWN.Core.NWNX.WeaponPlugin.SetGreaterWeaponFocusFeat((int)baseItem, (int)feat);
        }

        /// <inheritdoc/>
        public void SetWeaponFinesseSize(BaseItemType baseItem, CreatureSizeType nCreatureSize)
        {
            global::NWN.Core.NWNX.WeaponPlugin.SetWeaponFinesseSize((int)baseItem, (int)nCreatureSize);
        }

        /// <inheritdoc/>
        public void SetWeaponUnarmed(BaseItemType baseItem)
        {
            global::NWN.Core.NWNX.WeaponPlugin.SetWeaponUnarmed((int)baseItem);
        }

        /// <inheritdoc/>
        public void SetWeaponIsMonkWeapon(BaseItemType baseItem)
        {
            global::NWN.Core.NWNX.WeaponPlugin.SetWeaponIsMonkWeapon((int)baseItem);
        }

        /// <inheritdoc/>
        public void SetWeaponImprovedCriticalFeat(BaseItemType baseItem, FeatType feat)
        {
            global::NWN.Core.NWNX.WeaponPlugin.SetWeaponImprovedCriticalFeat((int)baseItem, (int)feat);
        }

        /// <inheritdoc/>
        public void SetWeaponSpecializationFeat(BaseItemType baseItem, FeatType feat)
        {
            global::NWN.Core.NWNX.WeaponPlugin.SetWeaponSpecializationFeat((int)baseItem, (int)feat);
        }

        /// <inheritdoc/>
        public void SetGreaterWeaponSpecializationFeat(BaseItemType baseItem, FeatType feat)
        {
            global::NWN.Core.NWNX.WeaponPlugin.SetGreaterWeaponSpecializationFeat((int)baseItem, (int)feat);
        }

        /// <inheritdoc/>
        public void SetEpicWeaponSpecializationFeat(BaseItemType baseItem, FeatType feat)
        {
            global::NWN.Core.NWNX.WeaponPlugin.SetEpicWeaponSpecializationFeat((int)baseItem, (int)feat);
        }

        /// <inheritdoc/>
        public void SetEpicWeaponOverwhelmingCriticalFeat(BaseItemType baseItem, FeatType feat)
        {
            global::NWN.Core.NWNX.WeaponPlugin.SetEpicWeaponOverwhelmingCriticalFeat((int)baseItem, (int)feat);
        }

        /// <inheritdoc/>
        public void SetEpicWeaponDevastatingCriticalFeat(BaseItemType baseItem, FeatType feat)
        {
            global::NWN.Core.NWNX.WeaponPlugin.SetEpicWeaponDevastatingCriticalFeat((int)baseItem, (int)feat);
        }

        /// <inheritdoc/>
        public void SetWeaponOfChoiceFeat(BaseItemType baseItem, FeatType feat)
        {
            global::NWN.Core.NWNX.WeaponPlugin.SetWeaponOfChoiceFeat((int)baseItem, (int)feat);
        }

        /// <inheritdoc/>
        public void SetOption(int nOption, int nVal)
        {
            global::NWN.Core.NWNX.WeaponPlugin.SetOption(nOption, nVal);
        }

        /// <inheritdoc/>
        public void SetDevastatingCriticalEventScript(string sScript)
        {
            global::NWN.Core.NWNX.WeaponPlugin.SetDevastatingCriticalEventScript(sScript);
        }

        /// <inheritdoc/>
        public void BypassDevastatingCritical()
        {
            global::NWN.Core.NWNX.WeaponPlugin.BypassDevastatingCritical();
        }

        /// <inheritdoc/>
        public DevastatingCriticalData GetDevastatingCriticalEventData()
        {
            var coreResult = global::NWN.Core.NWNX.WeaponPlugin.GetDevastatingCriticalEventData();
            return new DevastatingCriticalData
            {
                Weapon = coreResult.oWeapon,
                Target = coreResult.oTarget,
                Damage = coreResult.nDamage
            };
        }


        /// <inheritdoc/>
        public void SetOneHalfStrength(uint oWeapon, bool nEnable, bool bPersist = false)
        {
            global::NWN.Core.NWNX.WeaponPlugin.SetOneHalfStrength(oWeapon, nEnable ? 1 : 0, bPersist ? 1 : 0);
        }

        /// <inheritdoc/>
        public int GetOneHalfStrength(uint oWeapon)
        {
            return global::NWN.Core.NWNX.WeaponPlugin.GetOneHalfStrength(oWeapon);
        }

        /// <inheritdoc/>
        public void SetMaxRangedAttackDistanceOverride(BaseItemType nBaseItem, float fMax, float fMaxPassive, float fPreferred)
        {
            global::NWN.Core.NWNX.WeaponPlugin.SetMaxRangedAttackDistanceOverride((int)nBaseItem, fMax, fMaxPassive, fPreferred);
        }
    }
}