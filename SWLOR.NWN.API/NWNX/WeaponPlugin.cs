using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.NWN.API.NWNX
{
    public static class WeaponPlugin
    {
        /// <summary>
        /// Set nFeat as weapon focus feat for a base item.
        /// </summary>
        /// <param name="baseItem">The base item id.</param>
        /// <param name="feat">The feat to set.</param>
        public static void SetWeaponFocusFeat(BaseItem baseItem, FeatType feat)
        {
            global::NWN.Core.NWNX.WeaponPlugin.SetWeaponFocusFeat((int)baseItem, (int)feat);
        }

        /// <summary>
        /// Set a feat as epic weapon focus for a base item.
        /// </summary>
        /// <param name="baseItem">The base item id.</param>
        /// <param name="feat">The feat to set.</param>
        public static void SetEpicWeaponFocusFeat(BaseItem baseItem, FeatType feat)
        {
            global::NWN.Core.NWNX.WeaponPlugin.SetEpicWeaponFocusFeat((int)baseItem, (int)feat);
        }

        /// <summary>
        /// Set a feat as greater weapon focus for a base item.
        /// </summary>
        /// <param name="baseItem">The base item id.</param>
        /// <param name="feat">The feat to set.</param>
        public static void SetGreaterWeaponFocusFeat(BaseItem baseItem, FeatType feat)
        {
            global::NWN.Core.NWNX.WeaponPlugin.SetGreaterWeaponFocusFeat((int)baseItem, (int)feat);
        }

        /// <summary>
        /// Set required creature size for a weapon base item to be finessable.
        /// </summary>
        /// <param name="baseItem">The base item id.</param>
        /// <param name="nCreatureSize">The creature size minimum to consider this weapon finessable.</param>
        public static void SetWeaponFinesseSize(BaseItem baseItem, CreatureSize nCreatureSize)
        {
            global::NWN.Core.NWNX.WeaponPlugin.SetWeaponFinesseSize((int)baseItem, (int)nCreatureSize);
        }

        /// <summary>
        /// Set weapon base item to be considered as unarmed for weapon finesse feat.
        /// </summary>
        /// <param name="baseItem">The base item id.</param>
        public static void SetWeaponUnarmed(BaseItem baseItem)
        {
            global::NWN.Core.NWNX.WeaponPlugin.SetWeaponUnarmed((int)baseItem);
        }

        /// <summary>
        /// Set base item as monk weapon.
        /// </summary>
        /// <param name="baseItem">The base item id.</param>
        /// <remarks>This method is deprecated. Use baseitems.2da instead.</remarks>
        public static void SetWeaponIsMonkWeapon(BaseItem baseItem)
        {
            global::NWN.Core.NWNX.WeaponPlugin.SetWeaponIsMonkWeapon((int)baseItem);
        }

        /// <summary>
        /// Set a feat as weapon improved critical for a base item.
        /// </summary>
        /// <param name="baseItem">The base item id.</param>
        /// <param name="feat">The feat to set.</param>
        public static void SetWeaponImprovedCriticalFeat(BaseItem baseItem, FeatType feat)
        {
            global::NWN.Core.NWNX.WeaponPlugin.SetWeaponImprovedCriticalFeat((int)baseItem, (int)feat);
        }

        /// <summary>
        /// Set a feat as weapon specialization for a base item.
        /// </summary>
        /// <param name="baseItem">The base item id.</param>
        /// <param name="feat">The feat to set.</param>
        public static void SetWeaponSpecializationFeat(BaseItem baseItem, FeatType feat)
        {
            global::NWN.Core.NWNX.WeaponPlugin.SetWeaponSpecializationFeat((int)baseItem, (int)feat);
        }

        /// <summary>
        /// Set a feat as greater weapon specialization for a base item.
        /// </summary>
        /// <param name="baseItem">The base item id.</param>
        /// <param name="feat">The feat to set.</param>
        public static void SetGreaterWeaponSpecializationFeat(BaseItem baseItem, FeatType feat)
        {
            global::NWN.Core.NWNX.WeaponPlugin.SetGreaterWeaponSpecializationFeat((int)baseItem, (int)feat);
        }

        /// <summary>
        /// Set a feat as epic weapon specialization for a base item.
        /// </summary>
        /// <param name="baseItem">The base item id.</param>
        /// <param name="feat">The feat to set.</param>
        public static void SetEpicWeaponSpecializationFeat(BaseItem baseItem, FeatType feat)
        {
            global::NWN.Core.NWNX.WeaponPlugin.SetEpicWeaponSpecializationFeat((int)baseItem, (int)feat);
        }

        /// <summary>
        /// Set a feat as epic weapon overwhelming critical for a base item.
        /// </summary>
        /// <param name="baseItem">The base item id.</param>
        /// <param name="feat">The feat to set.</param>
        public static void SetEpicWeaponOverwhelmingCriticalFeat(BaseItem baseItem, FeatType feat)
        {
            global::NWN.Core.NWNX.WeaponPlugin.SetEpicWeaponOverwhelmingCriticalFeat((int)baseItem, (int)feat);
        }

        /// <summary>
        /// Set a feat as epic weapon devastating critical for a base item.
        /// </summary>
        /// <param name="baseItem">The base item id.</param>
        /// <param name="feat">The feat to set.</param>
        public static void SetEpicWeaponDevastatingCriticalFeat(BaseItem baseItem, FeatType feat)
        {
            global::NWN.Core.NWNX.WeaponPlugin.SetEpicWeaponDevastatingCriticalFeat((int)baseItem, (int)feat);
        }

        /// <summary>
        /// Set a feat as weapon of choice for a base item.
        /// </summary>
        /// <param name="baseItem">The base item id.</param>
        /// <param name="feat">The feat to set.</param>
        public static void SetWeaponOfChoiceFeat(BaseItem baseItem, FeatType feat)
        {
            global::NWN.Core.NWNX.WeaponPlugin.SetWeaponOfChoiceFeat((int)baseItem, (int)feat);
        }

        /// <summary>
        /// Set plugin options.
        /// </summary>
        /// <param name="nOption">The option to change from Weapon Options.</param>
        /// <param name="nVal">The new value of the option.</param>
        public static void SetOption(int nOption, int nVal)
        {
            global::NWN.Core.NWNX.WeaponPlugin.SetOption(nOption, nVal);
        }

        /// <summary>
        /// Set Devastating Critical Event Script.
        /// </summary>
        /// <param name="sScript">The script to call when a Devastating Critical occurs.</param>
        public static void SetDevastatingCriticalEventScript(string sScript)
        {
            global::NWN.Core.NWNX.WeaponPlugin.SetDevastatingCriticalEventScript(sScript);
        }

        /// <summary>
        /// Bypass Devastating Critical.
        /// </summary>
        /// <remarks>This is only for use with the Devastating Critical Event Script.</remarks>
        public static void BypassDevastatingCritical()
        {
            global::NWN.Core.NWNX.WeaponPlugin.BypassDevastatingCritical();
        }

        /// <summary>
        /// Get Devastating Critical Event Data.
        /// </summary>
        /// <remarks>This is only for use with the Devastating Critical Event Script.</remarks>
        /// <returns>A DevastatingCriticalData struct.</returns>
        public static DevastatingCriticalData GetDevastatingCriticalEventData()
        {
            var coreResult = global::NWN.Core.NWNX.WeaponPlugin.GetDevastatingCriticalEventData();
            return new DevastatingCriticalData
            {
                Weapon = coreResult.oWeapon,
                Target = coreResult.oTarget,
                Damage = coreResult.nDamage
            };
        }


        /// <summary>
        /// Sets weapon to gain .5 strength bonus.
        /// </summary>
        /// <param name="oWeapon">Should be a melee weapon.</param>
        /// <param name="nEnable">TRUE for bonus. FALSE to turn off bonus.</param>
        /// <param name="bPersist">whether the two hand state should persist to the gff file.</param>
        public static void SetOneHalfStrength(uint oWeapon, bool nEnable, bool bPersist = false)
        {
            global::NWN.Core.NWNX.WeaponPlugin.SetOneHalfStrength(oWeapon, nEnable ? 1 : 0, bPersist ? 1 : 0);
        }

        /// <summary>
        /// Gets if the weapon is set to gain addition .5 strength bonus
        /// </summary>
        /// <param name="oWeapon">the weapon</param>
        /// <returns>FALSE/0 if weapon is not receiving the bonus. TRUE/1 if it does.</returns>
        public static int GetOneHalfStrength(uint oWeapon)
        {
            return global::NWN.Core.NWNX.WeaponPlugin.GetOneHalfStrength(oWeapon);
        }

        /// <summary>
        /// Override the max attack distance of ranged weapons.
        /// </summary>
        /// <param name="nBaseItem">The baseitem id.</param>
        /// <param name="fMax">The maximum attack distance. Default is 40.0f.</param>
        /// <param name="fMaxPassive">The maximum passive attack distance. Default is 20.0f. Seems to be used by the engine to determine a new nearby target when needed.</param>
        /// <param name="fPreferred">The preferred attack distance. See the PrefAttackDist column in baseitems.2da, default seems to be 30.0f for ranged weapons.</param>
        /// <remarks>fMaxPassive should probably be lower than fMax, half of fMax seems to be a good start. fPreferred should be at least ~0.5f lower than fMax.</remarks>
        public static void SetMaxRangedAttackDistanceOverride(BaseItem nBaseItem, float fMax, float fMaxPassive, float fPreferred)
        {
            global::NWN.Core.NWNX.WeaponPlugin.SetMaxRangedAttackDistanceOverride((int)nBaseItem, fMax, fMaxPassive, fPreferred);
        }

        /// <summary>
        /// Devastating critical event data struct.
        /// </summary>
        public struct DevastatingCriticalData
        {
            /// <summary>
            /// The weapon object.
            /// </summary>
            public uint Weapon { get; set; }
            
            /// <summary>
            /// The target object.
            /// </summary>
            public uint Target { get; set; }
            
            /// <summary>
            /// The damage amount.
            /// </summary>
            public int Damage { get; set; }
        }
    }
}