using SWLOR.NWN.API.NWNX.Model;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.Contracts;

public interface IWeaponPluginService
{
    /// <summary>
    /// Set nFeat as weapon focus feat for a base item.
    /// </summary>
    /// <param name="baseItem">The base item id.</param>
    /// <param name="feat">The feat to set.</param>
    void SetWeaponFocusFeat(BaseItemType baseItem, FeatType feat);

    /// <summary>
    /// Set a feat as epic weapon focus for a base item.
    /// </summary>
    /// <param name="baseItem">The base item id.</param>
    /// <param name="feat">The feat to set.</param>
    void SetEpicWeaponFocusFeat(BaseItemType baseItem, FeatType feat);

    /// <summary>
    /// Set a feat as greater weapon focus for a base item.
    /// </summary>
    /// <param name="baseItem">The base item id.</param>
    /// <param name="feat">The feat to set.</param>
    void SetGreaterWeaponFocusFeat(BaseItemType baseItem, FeatType feat);

    /// <summary>
    /// Set required creature size for a weapon base item to be finessable.
    /// </summary>
    /// <param name="baseItem">The base item id.</param>
    /// <param name="nCreatureSize">The creature size minimum to consider this weapon finessable.</param>
    void SetWeaponFinesseSize(BaseItemType baseItem, CreatureSizeType nCreatureSize);

    /// <summary>
    /// Set weapon base item to be considered as unarmed for weapon finesse feat.
    /// </summary>
    /// <param name="baseItem">The base item id.</param>
    void SetWeaponUnarmed(BaseItemType baseItem);

    /// <summary>
    /// Set base item as monk weapon.
    /// </summary>
    /// <param name="baseItem">The base item id.</param>
    /// <remarks>This method is deprecated. Use baseitems.2da instead.</remarks>
    void SetWeaponIsMonkWeapon(BaseItemType baseItem);

    /// <summary>
    /// Set a feat as weapon improved critical for a base item.
    /// </summary>
    /// <param name="baseItem">The base item id.</param>
    /// <param name="feat">The feat to set.</param>
    void SetWeaponImprovedCriticalFeat(BaseItemType baseItem, FeatType feat);

    /// <summary>
    /// Set a feat as weapon specialization for a base item.
    /// </summary>
    /// <param name="baseItem">The base item id.</param>
    /// <param name="feat">The feat to set.</param>
    void SetWeaponSpecializationFeat(BaseItemType baseItem, FeatType feat);

    /// <summary>
    /// Set a feat as greater weapon specialization for a base item.
    /// </summary>
    /// <param name="baseItem">The base item id.</param>
    /// <param name="feat">The feat to set.</param>
    void SetGreaterWeaponSpecializationFeat(BaseItemType baseItem, FeatType feat);

    /// <summary>
    /// Set a feat as epic weapon specialization for a base item.
    /// </summary>
    /// <param name="baseItem">The base item id.</param>
    /// <param name="feat">The feat to set.</param>
    void SetEpicWeaponSpecializationFeat(BaseItemType baseItem, FeatType feat);

    /// <summary>
    /// Set a feat as epic weapon overwhelming critical for a base item.
    /// </summary>
    /// <param name="baseItem">The base item id.</param>
    /// <param name="feat">The feat to set.</param>
    void SetEpicWeaponOverwhelmingCriticalFeat(BaseItemType baseItem, FeatType feat);

    /// <summary>
    /// Set a feat as epic weapon devastating critical for a base item.
    /// </summary>
    /// <param name="baseItem">The base item id.</param>
    /// <param name="feat">The feat to set.</param>
    void SetEpicWeaponDevastatingCriticalFeat(BaseItemType baseItem, FeatType feat);

    /// <summary>
    /// Set a feat as weapon of choice for a base item.
    /// </summary>
    /// <param name="baseItem">The base item id.</param>
    /// <param name="feat">The feat to set.</param>
    void SetWeaponOfChoiceFeat(BaseItemType baseItem, FeatType feat);

    /// <summary>
    /// Set plugin options.
    /// </summary>
    /// <param name="nOption">The option to change from Weapon Options.</param>
    /// <param name="nVal">The new value of the option.</param>
    void SetOption(int nOption, int nVal);

    /// <summary>
    /// Set Devastating Critical Event Script.
    /// </summary>
    /// <param name="sScript">The script to call when a Devastating Critical occurs.</param>
    void SetDevastatingCriticalEventScript(string sScript);

    /// <summary>
    /// Bypass Devastating Critical.
    /// </summary>
    /// <remarks>This is only for use with the Devastating Critical Event Script.</remarks>
    void BypassDevastatingCritical();

    /// <summary>
    /// Get Devastating Critical Event Data.
    /// </summary>
    /// <remarks>This is only for use with the Devastating Critical Event Script.</remarks>
    /// <returns>A DevastatingCriticalData struct.</returns>
    DevastatingCriticalData GetDevastatingCriticalEventData();

    /// <summary>
    /// Sets weapon to gain .5 strength bonus.
    /// </summary>
    /// <param name="oWeapon">Should be a melee weapon.</param>
    /// <param name="nEnable">TRUE for bonus. FALSE to turn off bonus.</param>
    /// <param name="bPersist">whether the two hand state should persist to the gff file.</param>
    void SetOneHalfStrength(uint oWeapon, bool nEnable, bool bPersist = false);

    /// <summary>
    /// Gets if the weapon is set to gain addition .5 strength bonus
    /// </summary>
    /// <param name="oWeapon">the weapon</param>
    /// <returns>FALSE/0 if weapon is not receiving the bonus. TRUE/1 if it does.</returns>
    int GetOneHalfStrength(uint oWeapon);

    /// <summary>
    /// Override the max attack distance of ranged weapons.
    /// </summary>
    /// <param name="nBaseItem">The baseitem id.</param>
    /// <param name="fMax">The maximum attack distance. Default is 40.0f.</param>
    /// <param name="fMaxPassive">The maximum passive attack distance. Default is 20.0f. Seems to be used by the engine to determine a new nearby target when needed.</param>
    /// <param name="fPreferred">The preferred attack distance. See the PrefAttackDist column in baseitems.2da, default seems to be 30.0f for ranged weapons.</param>
    /// <remarks>fMaxPassive should probably be lower than fMax, half of fMax seems to be a good start. fPreferred should be at least ~0.5f lower than fMax.</remarks>
    void SetMaxRangedAttackDistanceOverride(BaseItemType nBaseItem, float fMax, float fMaxPassive, float fPreferred);
}