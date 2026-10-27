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
            WeaponPluginRuntime.Current.SetWeaponFocusFeat(baseItem, feat);
        }

        /// <summary>
        /// Set a feat as epic weapon focus for a base item.
        /// </summary>
        /// <param name="baseItem">The base item id.</param>
        /// <param name="feat">The feat to set.</param>
        public static void SetEpicWeaponFocusFeat(BaseItem baseItem, FeatType feat)
        {
            WeaponPluginRuntime.Current.SetEpicWeaponFocusFeat(baseItem, feat);
        }

        /// <summary>
        /// Set a feat as greater weapon focus for a base item.
        /// </summary>
        /// <param name="baseItem">The base item id.</param>
        /// <param name="feat">The feat to set.</param>
        public static void SetGreaterWeaponFocusFeat(BaseItem baseItem, FeatType feat)
        {
            WeaponPluginRuntime.Current.SetGreaterWeaponFocusFeat(baseItem, feat);
        }

        /// <summary>
        /// Set required creature size for a weapon base item to be finessable.
        /// </summary>
        /// <param name="baseItem">The base item id.</param>
        /// <param name="nCreatureSize">The creature size minimum to consider this weapon finessable.</param>
        public static void SetWeaponFinesseSize(BaseItem baseItem, CreatureSize nCreatureSize)
        {
            WeaponPluginRuntime.Current.SetWeaponFinesseSize(baseItem, nCreatureSize);
        }

        /// <summary>
        /// Set weapon base item to be considered as unarmed for weapon finesse feat.
        /// </summary>
        /// <param name="baseItem">The base item id.</param>
        public static void SetWeaponUnarmed(BaseItem baseItem)
        {
            WeaponPluginRuntime.Current.SetWeaponUnarmed(baseItem);
        }

        /// <summary>
        /// Set base item as monk weapon.
        /// </summary>
        /// <param name="baseItem">The base item id.</param>
        /// <remarks>This method is deprecated. Use baseitems.2da instead.</remarks>
        public static void SetWeaponIsMonkWeapon(BaseItem baseItem)
        {
            WeaponPluginRuntime.Current.SetWeaponIsMonkWeapon(baseItem);
        }

        /// <summary>
        /// Set a feat as weapon improved critical for a base item.
        /// </summary>
        /// <param name="baseItem">The base item id.</param>
        /// <param name="feat">The feat to set.</param>
        public static void SetWeaponImprovedCriticalFeat(BaseItem baseItem, FeatType feat)
        {
            WeaponPluginRuntime.Current.SetWeaponImprovedCriticalFeat(baseItem, feat);
        }

        /// <summary>
        /// Set a feat as weapon specialization for a base item.
        /// </summary>
        /// <param name="baseItem">The base item id.</param>
        /// <param name="feat">The feat to set.</param>
        public static void SetWeaponSpecializationFeat(BaseItem baseItem, FeatType feat)
        {
            WeaponPluginRuntime.Current.SetWeaponSpecializationFeat(baseItem, feat);
        }

        /// <summary>
        /// Set a feat as greater weapon specialization for a base item.
        /// </summary>
        /// <param name="baseItem">The base item id.</param>
        /// <param name="feat">The feat to set.</param>
        public static void SetGreaterWeaponSpecializationFeat(BaseItem baseItem, FeatType feat)
        {
            WeaponPluginRuntime.Current.SetGreaterWeaponSpecializationFeat(baseItem, feat);
        }

        /// <summary>
        /// Set a feat as epic weapon specialization for a base item.
        /// </summary>
        /// <param name="baseItem">The base item id.</param>
        /// <param name="feat">The feat to set.</param>
        public static void SetEpicWeaponSpecializationFeat(BaseItem baseItem, FeatType feat)
        {
            WeaponPluginRuntime.Current.SetEpicWeaponSpecializationFeat(baseItem, feat);
        }

        /// <summary>
        /// Set a feat as epic weapon overwhelming critical for a base item.
        /// </summary>
        /// <param name="baseItem">The base item id.</param>
        /// <param name="feat">The feat to set.</param>
        public static void SetEpicWeaponOverwhelmingCriticalFeat(BaseItem baseItem, FeatType feat)
        {
            WeaponPluginRuntime.Current.SetEpicWeaponOverwhelmingCriticalFeat(baseItem, feat);
        }

        /// <summary>
        /// Set a feat as epic weapon devastating critical for a base item.
        /// </summary>
        /// <param name="baseItem">The base item id.</param>
        /// <param name="feat">The feat to set.</param>
        public static void SetEpicWeaponDevastatingCriticalFeat(BaseItem baseItem, FeatType feat)
        {
            WeaponPluginRuntime.Current.SetEpicWeaponDevastatingCriticalFeat(baseItem, feat);
        }

        /// <summary>
        /// Set a feat as weapon of choice for a base item.
        /// </summary>
        /// <param name="baseItem">The base item id.</param>
        /// <param name="feat">The feat to set.</param>
        public static void SetWeaponOfChoiceFeat(BaseItem baseItem, FeatType feat)
        {
            WeaponPluginRuntime.Current.SetWeaponOfChoiceFeat(baseItem, feat);
        }

        /// <summary>
        /// Set plugin options.
        /// </summary>
        /// <param name="nOption">The option to change from Weapon Options.</param>
        /// <param name="nVal">The new value of the option.</param>
        public static void SetOption(int nOption, int nVal)
        {
            WeaponPluginRuntime.Current.SetOption(nOption, nVal);
        }

        /// <summary>
        /// Set Devastating Critical Event Script.
        /// </summary>
        /// <param name="sScript">The script to call when a Devastating Critical occurs.</param>
        public static void SetDevastatingCriticalEventScript(string sScript)
        {
            WeaponPluginRuntime.Current.SetDevastatingCriticalEventScript(sScript);
        }

        /// <summary>
        /// Bypass Devastating Critical.
        /// </summary>
        /// <remarks>This is only for use with the Devastating Critical Event Script.</remarks>
        public static void BypassDevastatingCritical()
        {
            WeaponPluginRuntime.Current.BypassDevastatingCritical();
        }

        /// <summary>
        /// Get Devastating Critical Event Data.
        /// </summary>
        /// <remarks>This is only for use with the Devastating Critical Event Script.</remarks>
        /// <returns>A DevastatingCriticalData struct.</returns>
        public static DevastatingCriticalData GetDevastatingCriticalEventData()
        {
            return WeaponPluginRuntime.Current.GetDevastatingCriticalEventData();
        }


        /// <summary>
        /// Sets weapon to gain .5 strength bonus.
        /// </summary>
        /// <param name="oWeapon">Should be a melee weapon.</param>
        /// <param name="nEnable">TRUE for bonus. FALSE to turn off bonus.</param>
        /// <param name="bPersist">whether the two hand state should persist to the gff file.</param>
        public static void SetOneHalfStrength(uint oWeapon, bool nEnable, bool bPersist = false)
        {
            WeaponPluginRuntime.Current.SetOneHalfStrength(oWeapon, nEnable, bPersist);
        }

        /// <summary>
        /// Gets if the weapon is set to gain addition .5 strength bonus
        /// </summary>
        /// <param name="oWeapon">the weapon</param>
        /// <returns>FALSE/0 if weapon is not receiving the bonus. TRUE/1 if it does.</returns>
        public static int GetOneHalfStrength(uint oWeapon)
        {
            return WeaponPluginRuntime.Current.GetOneHalfStrength(oWeapon);
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
            WeaponPluginRuntime.Current.SetMaxRangedAttackDistanceOverride(nBaseItem, fMax, fMaxPassive, fPreferred);
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
