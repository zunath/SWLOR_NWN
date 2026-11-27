using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Domain.Foundation;

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

    /// <summary>Supplies the data directly, for tests and for callers that read it elsewhere.</summary>
    public static void Load(IReadOnlyDictionary<int, (int Width, int Height)> inventorySizes,
        IReadOnlyDictionary<int, (int CostTable, int Param1Table)> propertyDefinitions) =>
        Migration.CharacterData.Load(inventorySizes, propertyDefinitions);

    public static (int Width, int Height) GetInventorySize(BaseItem baseItem) => Migration.CharacterData.GetInventorySize(baseItem);

    public static (int CostTable, int Param1Table) GetPropertyDefinition(ItemPropertyType type) =>
        Migration.CharacterData.GetPropertyDefinition(type);
}
