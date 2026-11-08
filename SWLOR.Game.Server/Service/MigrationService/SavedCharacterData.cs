using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Service.MigrationService;

/// <summary>
/// Game data player migrations read while upgrading saved characters on worker threads. The data lives on the
/// migration service's <see cref="SavedCharacterDataStore"/> (static code reaches it through <c>GameServices.Migration</c>);
/// this class keeps the static API the character migrations call, with no state of its own.
/// </summary>
internal static class SavedCharacterData
{
    /// <summary>Inventory grid of a container item, in item cells.</summary>
    public static int ContainerWidth => Migration.CharacterData.ContainerWidth;
    public static int ContainerHeight => Migration.CharacterData.ContainerHeight;

    public static bool IsLoaded => Migration.CharacterData.IsLoaded;

    /// <summary>Main thread only: reads baseitems.2da inventory sizes and itempropdef.2da tables.</summary>
    public static void Load() => Migration.CharacterData.Load();

    /// <summary>The dice an iprp_damagecost row rolls, for rows with both columns set.</summary>
    public static bool TryGetDamageCost(int row, out (int Dice, int Die) cost) =>
        Migration.CharacterData.TryGetDamageCost(row, out cost);

    /// <summary>The classes.2da hit die a native level of the class adds to the base HP.</summary>
    public static int GetClassHitDie(ClassType @class) => Migration.CharacterData.GetClassHitDie(@class);

    /// <summary>The classes.2da leveling package of a class, as <c>PlayerInitialization.EnsureNativeLevels</c> reads it.</summary>
    public static bool TryGetClassPackage(ClassType @class, out int package) =>
        Migration.CharacterData.TryGetClassPackage(@class, out package);

    /// <summary>Saved-data <see cref="Item.HasInventoryIcon"/> for an attack weapon.</summary>
    public static bool WeaponHasInventoryIcon(BaseItem baseItem, int simpleModel) =>
        Migration.CharacterData.WeaponHasInventoryIcon(baseItem, simpleModel);

    /// <summary>Supplies the data directly, for tests and for callers that read it elsewhere.</summary>
    public static void Load(IReadOnlyDictionary<int, (int Width, int Height)> inventorySizes,
        IReadOnlyDictionary<int, (int CostTable, int Param1Table)> propertyDefinitions,
        IReadOnlyDictionary<string, string> blueprints) =>
        Migration.CharacterData.Load(inventorySizes, propertyDefinitions, blueprints);

    /// <summary>A new item from a module blueprint, as <c>CreateItemOnObject</c> makes it, serialized (UTI, base64).</summary>
    public static string GetBlueprint(string resref) => Migration.CharacterData.GetBlueprint(resref);

    public static (int Width, int Height) GetInventorySize(BaseItem baseItem) => Migration.CharacterData.GetInventorySize(baseItem);

    public static (int CostTable, int Param1Table) GetPropertyDefinition(ItemPropertyType type) =>
        Migration.CharacterData.GetPropertyDefinition(type);
}
