using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWNX.Model;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Test.Shared.NWScriptMocks.NWNXPluginMocks
{
    /// <summary>
    /// Mock implementation of the WeaponPlugin for testing purposes.
    /// Provides weapon feat management and configuration functionality.
    /// </summary>
    public class WeaponPluginMock: IWeaponPluginService
    {
        // Mock data storage
        private readonly Dictionary<BaseItemType, FeatType> _weaponFocusFeats = new();
        private readonly Dictionary<BaseItemType, FeatType> _epicWeaponFocusFeats = new();
        private readonly Dictionary<BaseItemType, FeatType> _greaterWeaponFocusFeats = new();
        private readonly Dictionary<BaseItemType, CreatureSizeType> _weaponFinesseSizes = new();
        private readonly HashSet<BaseItemType> _unarmedWeapons = new();
        private readonly HashSet<BaseItemType> _monkWeapons = new();
        private readonly Dictionary<BaseItemType, FeatType> _weaponImprovedCriticalFeats = new();
        private readonly Dictionary<BaseItemType, FeatType> _weaponSpecializationFeats = new();
        private readonly Dictionary<BaseItemType, FeatType> _greaterWeaponSpecializationFeats = new();
        private readonly Dictionary<BaseItemType, FeatType> _epicWeaponSpecializationFeats = new();
        private readonly Dictionary<BaseItemType, FeatType> _epicWeaponOverwhelmingCriticalFeats = new();
        private readonly Dictionary<BaseItemType, FeatType> _epicWeaponDevastatingCriticalFeats = new();
        private readonly Dictionary<BaseItemType, FeatType> _weaponOfChoiceFeats = new();
        private readonly Dictionary<int, int> _pluginOptions = new();
        private string _devastatingCriticalEventScript = string.Empty;
        private bool _bypassDevastatingCritical = false;
        private DevastatingCriticalData _lastDevastatingCriticalData = new();
        private readonly Dictionary<uint, bool> _oneHalfStrengthWeapons = new();
        private readonly Dictionary<BaseItemType, RangedAttackDistance> _rangedAttackDistances = new();

        /// <summary>
        /// Set nFeat as weapon focus feat for a base item.
        /// </summary>
        /// <param name="baseItem">The base item id.</param>
        /// <param name="feat">The feat to set.</param>
        public void SetWeaponFocusFeat(BaseItemType baseItem, FeatType feat)
        {
            _weaponFocusFeats[baseItem] = feat;
        }

        /// <summary>
        /// Set a feat as epic weapon focus for a base item.
        /// </summary>
        /// <param name="baseItem">The base item id.</param>
        /// <param name="feat">The feat to set.</param>
        public void SetEpicWeaponFocusFeat(BaseItemType baseItem, FeatType feat)
        {
            _epicWeaponFocusFeats[baseItem] = feat;
        }

        /// <summary>
        /// Set a feat as greater weapon focus for a base item.
        /// </summary>
        /// <param name="baseItem">The base item id.</param>
        /// <param name="feat">The feat to set.</param>
        public void SetGreaterWeaponFocusFeat(BaseItemType baseItem, FeatType feat)
        {
            _greaterWeaponFocusFeats[baseItem] = feat;
        }

        /// <summary>
        /// Set required creature size for a weapon base item to be finessable.
        /// </summary>
        /// <param name="baseItem">The base item id.</param>
        /// <param name="nCreatureSize">The creature size minimum to consider this weapon finessable.</param>
        public void SetWeaponFinesseSize(BaseItemType baseItem, CreatureSizeType nCreatureSize)
        {
            _weaponFinesseSizes[baseItem] = nCreatureSize;
        }

        /// <summary>
        /// Set weapon base item to be considered as unarmed for weapon finesse feat.
        /// </summary>
        /// <param name="baseItem">The base item id.</param>
        public void SetWeaponUnarmed(BaseItemType baseItem)
        {
            _unarmedWeapons.Add(baseItem);
        }

        /// <summary>
        /// Set base item as monk weapon.
        /// </summary>
        /// <param name="baseItem">The base item id.</param>
        public void SetWeaponIsMonkWeapon(BaseItemType baseItem)
        {
            _monkWeapons.Add(baseItem);
        }

        /// <summary>
        /// Set a feat as weapon improved critical for a base item.
        /// </summary>
        /// <param name="baseItem">The base item id.</param>
        /// <param name="feat">The feat to set.</param>
        public void SetWeaponImprovedCriticalFeat(BaseItemType baseItem, FeatType feat)
        {
            _weaponImprovedCriticalFeats[baseItem] = feat;
        }

        /// <summary>
        /// Set a feat as weapon specialization for a base item.
        /// </summary>
        /// <param name="baseItem">The base item id.</param>
        /// <param name="feat">The feat to set.</param>
        public void SetWeaponSpecializationFeat(BaseItemType baseItem, FeatType feat)
        {
            _weaponSpecializationFeats[baseItem] = feat;
        }

        /// <summary>
        /// Set a feat as greater weapon specialization for a base item.
        /// </summary>
        /// <param name="baseItem">The base item id.</param>
        /// <param name="feat">The feat to set.</param>
        public void SetGreaterWeaponSpecializationFeat(BaseItemType baseItem, FeatType feat)
        {
            _greaterWeaponSpecializationFeats[baseItem] = feat;
        }

        /// <summary>
        /// Set a feat as epic weapon specialization for a base item.
        /// </summary>
        /// <param name="baseItem">The base item id.</param>
        /// <param name="feat">The feat to set.</param>
        public void SetEpicWeaponSpecializationFeat(BaseItemType baseItem, FeatType feat)
        {
            _epicWeaponSpecializationFeats[baseItem] = feat;
        }

        /// <summary>
        /// Set a feat as epic weapon overwhelming critical for a base item.
        /// </summary>
        /// <param name="baseItem">The base item id.</param>
        /// <param name="feat">The feat to set.</param>
        public void SetEpicWeaponOverwhelmingCriticalFeat(BaseItemType baseItem, FeatType feat)
        {
            _epicWeaponOverwhelmingCriticalFeats[baseItem] = feat;
        }

        /// <summary>
        /// Set a feat as epic weapon devastating critical for a base item.
        /// </summary>
        /// <param name="baseItem">The base item id.</param>
        /// <param name="feat">The feat to set.</param>
        public void SetEpicWeaponDevastatingCriticalFeat(BaseItemType baseItem, FeatType feat)
        {
            _epicWeaponDevastatingCriticalFeats[baseItem] = feat;
        }

        /// <summary>
        /// Set a feat as weapon of choice for a base item.
        /// </summary>
        /// <param name="baseItem">The base item id.</param>
        /// <param name="feat">The feat to set.</param>
        public void SetWeaponOfChoiceFeat(BaseItemType baseItem, FeatType feat)
        {
            _weaponOfChoiceFeats[baseItem] = feat;
        }

        /// <summary>
        /// Set plugin options.
        /// </summary>
        /// <param name="nOption">The option to change from Weapon Options.</param>
        /// <param name="nVal">The new value of the option.</param>
        public void SetOption(int nOption, int nVal)
        {
            _pluginOptions[nOption] = nVal;
        }

        /// <summary>
        /// Set Devastating Critical Event Script.
        /// </summary>
        /// <param name="sScript">The script to call when a Devastating Critical occurs.</param>
        public void SetDevastatingCriticalEventScript(string sScript)
        {
            _devastatingCriticalEventScript = sScript;
        }

        /// <summary>
        /// Bypass Devastating Critical.
        /// </summary>
        public void BypassDevastatingCritical()
        {
            _bypassDevastatingCritical = true;
        }

        /// <summary>
        /// Get Devastating Critical Event Data.
        /// </summary>
        /// <returns>A DevastatingCriticalData struct.</returns>
        public DevastatingCriticalData GetDevastatingCriticalEventData()
        {
            return _lastDevastatingCriticalData;
        }

        /// <summary>
        /// Sets weapon to gain .5 strength bonus.
        /// </summary>
        /// <param name="oWeapon">Should be a melee weapon.</param>
        /// <param name="nEnable">TRUE for bonus. FALSE to turn off bonus.</param>
        /// <param name="bPersist">whether the two hand state should persist to the gff file.</param>
        public void SetOneHalfStrength(uint oWeapon, bool nEnable, bool bPersist = false)
        {
            _oneHalfStrengthWeapons[oWeapon] = nEnable;
        }

        /// <summary>
        /// Gets if the weapon is set to gain addition .5 strength bonus
        /// </summary>
        /// <param name="oWeapon">the weapon</param>
        /// <returns>FALSE/0 if weapon is not receiving the bonus. TRUE/1 if it does.</returns>
        public int GetOneHalfStrength(uint oWeapon)
        {
            return _oneHalfStrengthWeapons.TryGetValue(oWeapon, out var enabled) && enabled ? 1 : 0;
        }

        /// <summary>
        /// Override the max attack distance of ranged weapons.
        /// </summary>
        /// <param name="nBaseItem">The baseitem id.</param>
        /// <param name="fMax">The maximum attack distance. Default is 40.0f.</param>
        /// <param name="fMaxPassive">The maximum passive attack distance. Default is 20.0f.</param>
        /// <param name="fPreferred">The preferred attack distance.</param>
        public void SetMaxRangedAttackDistanceOverride(BaseItemType nBaseItem, float fMax, float fMaxPassive, float fPreferred)
        {
            _rangedAttackDistances[nBaseItem] = new RangedAttackDistance
            {
                Max = fMax,
                MaxPassive = fMaxPassive,
                Preferred = fPreferred
            };
        }

        // Helper methods for testing
        /// <summary>
        /// Resets all mock data to default values for testing.
        /// </summary>
        public void Reset()
        {
            _weaponFocusFeats.Clear();
            _epicWeaponFocusFeats.Clear();
            _greaterWeaponFocusFeats.Clear();
            _weaponFinesseSizes.Clear();
            _unarmedWeapons.Clear();
            _monkWeapons.Clear();
            _weaponImprovedCriticalFeats.Clear();
            _weaponSpecializationFeats.Clear();
            _greaterWeaponSpecializationFeats.Clear();
            _epicWeaponSpecializationFeats.Clear();
            _epicWeaponOverwhelmingCriticalFeats.Clear();
            _epicWeaponDevastatingCriticalFeats.Clear();
            _weaponOfChoiceFeats.Clear();
            _pluginOptions.Clear();
            _devastatingCriticalEventScript = string.Empty;
            _bypassDevastatingCritical = false;
            _lastDevastatingCriticalData = new DevastatingCriticalData();
            _oneHalfStrengthWeapons.Clear();
            _rangedAttackDistances.Clear();
        }

        /// <summary>
        /// Gets all weapon data for testing verification.
        /// </summary>
        /// <returns>A WeaponData object containing all settings.</returns>
        public WeaponData GetWeaponDataForTesting()
        {
            return new WeaponData
            {
                WeaponFocusFeats = new Dictionary<BaseItemType, FeatType>(_weaponFocusFeats),
                EpicWeaponFocusFeats = new Dictionary<BaseItemType, FeatType>(_epicWeaponFocusFeats),
                GreaterWeaponFocusFeats = new Dictionary<BaseItemType, FeatType>(_greaterWeaponFocusFeats),
                WeaponFinesseSizes = new Dictionary<BaseItemType, CreatureSizeType>(_weaponFinesseSizes),
                UnarmedWeapons = new HashSet<BaseItemType>(_unarmedWeapons),
                MonkWeapons = new HashSet<BaseItemType>(_monkWeapons),
                WeaponImprovedCriticalFeats = new Dictionary<BaseItemType, FeatType>(_weaponImprovedCriticalFeats),
                WeaponSpecializationFeats = new Dictionary<BaseItemType, FeatType>(_weaponSpecializationFeats),
                GreaterWeaponSpecializationFeats = new Dictionary<BaseItemType, FeatType>(_greaterWeaponSpecializationFeats),
                EpicWeaponSpecializationFeats = new Dictionary<BaseItemType, FeatType>(_epicWeaponSpecializationFeats),
                EpicWeaponOverwhelmingCriticalFeats = new Dictionary<BaseItemType, FeatType>(_epicWeaponOverwhelmingCriticalFeats),
                EpicWeaponDevastatingCriticalFeats = new Dictionary<BaseItemType, FeatType>(_epicWeaponDevastatingCriticalFeats),
                WeaponOfChoiceFeats = new Dictionary<BaseItemType, FeatType>(_weaponOfChoiceFeats),
                PluginOptions = new Dictionary<int, int>(_pluginOptions),
                DevastatingCriticalEventScript = _devastatingCriticalEventScript,
                BypassDevastatingCritical = _bypassDevastatingCritical,
                LastDevastatingCriticalData = _lastDevastatingCriticalData,
                OneHalfStrengthWeapons = new Dictionary<uint, bool>(_oneHalfStrengthWeapons),
                RangedAttackDistances = new Dictionary<BaseItemType, RangedAttackDistance>(_rangedAttackDistances)
            };
        }

        /// <summary>
        /// Sets devastating critical event data for testing.
        /// </summary>
        /// <param name="weapon">The weapon object.</param>
        /// <param name="target">The target object.</param>
        /// <param name="damage">The damage amount.</param>
        public void SetDevastatingCriticalEventData(uint weapon, uint target, int damage)
        {
            _lastDevastatingCriticalData = new DevastatingCriticalData
            {
                Weapon = weapon,
                Target = target,
                Damage = damage
            };
        }

        // Helper classes
        public class RangedAttackDistance
        {
            public float Max { get; set; }
            public float MaxPassive { get; set; }
            public float Preferred { get; set; }
        }

        public class WeaponData
        {
            public Dictionary<BaseItemType, FeatType> WeaponFocusFeats { get; set; } = new();
            public Dictionary<BaseItemType, FeatType> EpicWeaponFocusFeats { get; set; } = new();
            public Dictionary<BaseItemType, FeatType> GreaterWeaponFocusFeats { get; set; } = new();
            public Dictionary<BaseItemType, CreatureSizeType> WeaponFinesseSizes { get; set; } = new();
            public HashSet<BaseItemType> UnarmedWeapons { get; set; } = new();
            public HashSet<BaseItemType> MonkWeapons { get; set; } = new();
            public Dictionary<BaseItemType, FeatType> WeaponImprovedCriticalFeats { get; set; } = new();
            public Dictionary<BaseItemType, FeatType> WeaponSpecializationFeats { get; set; } = new();
            public Dictionary<BaseItemType, FeatType> GreaterWeaponSpecializationFeats { get; set; } = new();
            public Dictionary<BaseItemType, FeatType> EpicWeaponSpecializationFeats { get; set; } = new();
            public Dictionary<BaseItemType, FeatType> EpicWeaponOverwhelmingCriticalFeats { get; set; } = new();
            public Dictionary<BaseItemType, FeatType> EpicWeaponDevastatingCriticalFeats { get; set; } = new();
            public Dictionary<BaseItemType, FeatType> WeaponOfChoiceFeats { get; set; } = new();
            public Dictionary<int, int> PluginOptions { get; set; } = new();
            public string DevastatingCriticalEventScript { get; set; } = string.Empty;
            public bool BypassDevastatingCritical { get; set; }
            public DevastatingCriticalData LastDevastatingCriticalData { get; set; } = new();
            public Dictionary<uint, bool> OneHalfStrengthWeapons { get; set; } = new();
            public Dictionary<BaseItemType, RangedAttackDistance> RangedAttackDistances { get; set; } = new();
        }
    }
}
