using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using NWNXLib = NWN.Native.API.NWNXLib;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Service.MigrationService;

/// <summary>
/// Game data player migrations read while upgrading saved characters on worker threads. <see cref="Load"/>
/// reads it from the 2DAs on the main thread before any character is upgraded.
/// </summary>
internal static class SavedCharacterData
{
    private static IReadOnlyDictionary<int, (int Width, int Height)> _inventorySizes;
    private static IReadOnlyDictionary<int, (int CostTable, int Param1Table)> _propertyDefinitions;

    /// <summary>Inventory grid of a container item, in item cells.</summary>
    public static int ContainerWidth { get; private set; } = 7;
    public static int ContainerHeight { get; private set; } = 5;

    public static bool IsLoaded => _inventorySizes != null && _propertyDefinitions != null;

    /// <summary>Main thread only: reads baseitems.2da inventory sizes and itempropdef.2da tables.</summary>
    public static void Load()
    {
        var sizes = new Dictionary<int, (int, int)>();
        for (var row = 0; row < Get2DARowCount("baseitems"); row++)
        {
            if (int.TryParse(Get2DAString("baseitems", "InvSlotWidth", row), out var width) &&
                int.TryParse(Get2DAString("baseitems", "InvSlotHeight", row), out var height) && width > 0 && height > 0)
                sizes[row] = (width, height);
        }
        var definitions = new Dictionary<int, (int, int)>();
        for (var row = 0; row < Get2DARowCount("itempropdef"); row++)
        {
            definitions[row] = (ParseTable(Get2DAString("itempropdef", "CostTableResRef", row)),
                ParseTable(Get2DAString("itempropdef", "Param1ResRef", row)));
        }
        // These caches fill on first use; fill them before worker threads read them.
        Resistance.IsValidResistanceType(ResistanceType.Fire);
        Resistance.GetAllResistanceTypes();
        Combat.GetDefenseDamageTypes();
        Perk.GetAllActiveAbilityFeats(PerkType.Invalid);

        var storage = GetObjectByTag("TEMP_ITEM_STORAGE");
        // Container items all share one inventory grid; measure it from a bag.
        var bag = CreateItemOnObject("bag_b", storage);
        var repository = NWNXLib.g_pAppManager.m_pServerExoApp.GetItemByGameObjectID(bag)?.m_pItemRepository
            ?? throw new InvalidOperationException("A container could not be created to measure its inventory.");
        ContainerWidth = repository.m_nWidth;
        ContainerHeight = repository.m_nHeight;
        DestroyObject(bag);
        Load(sizes, definitions);
    }

    /// <summary>Supplies the data directly, for tests and for callers that read it elsewhere.</summary>
    public static void Load(IReadOnlyDictionary<int, (int Width, int Height)> inventorySizes,
        IReadOnlyDictionary<int, (int CostTable, int Param1Table)> propertyDefinitions)
    {
        _inventorySizes = inventorySizes;
        _propertyDefinitions = propertyDefinitions;
    }

    // An empty 2DA cell ("****") saves as 255, as the engine stores a property without that table.
    private static int ParseTable(string value) => int.TryParse(value, out var table) ? table : byte.MaxValue;

    public static (int Width, int Height) GetInventorySize(BaseItem baseItem)
    {
        if (_inventorySizes == null)
            throw new InvalidOperationException("Saved character data is not loaded.");
        return _inventorySizes.TryGetValue((int)baseItem, out var size)
            ? size
            : throw new InvalidOperationException($"No inventory size is known for base item {baseItem}.");
    }

    public static (int CostTable, int Param1Table) GetPropertyDefinition(ItemPropertyType type)
    {
        if (_propertyDefinitions == null)
            throw new InvalidOperationException("Saved character data is not loaded.");
        return _propertyDefinitions.TryGetValue((int)type, out var definition)
            ? definition
            : throw new InvalidOperationException($"No item property definition is known for {type}.");
    }
}
