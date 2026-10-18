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
    private static IReadOnlyDictionary<string, string> _blueprints;
    private static IReadOnlyDictionary<(int BaseItem, int Model), bool> _weaponIcons;
    private static IReadOnlyDictionary<int, int> _classPackages;
    private static IReadOnlyDictionary<int, (int Dice, int Die)> _damageCosts;

    /// <summary>Inventory grid of a container item, in item cells.</summary>
    public static int ContainerWidth { get; private set; } = 7;
    public static int ContainerHeight { get; private set; } = 5;

    public static bool IsLoaded => _inventorySizes != null && _propertyDefinitions != null && _blueprints != null;

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

        var packages = new Dictionary<int, int>();
        for (var row = 0; row < Get2DARowCount("classes"); row++)
        {
            if (int.TryParse(Get2DAString("classes", "Package", row), out var package) && package >= 0 && package < (int)Package.Invalid)
                packages[row] = package;
        }
        _classPackages = packages;

        var damageCosts = new Dictionary<int, (int, int)>();
        for (var row = 0; row < Get2DARowCount("iprp_damagecost"); row++)
        {
            if (int.TryParse(Get2DAString("iprp_damagecost", "NumDice", row), out var dice) &&
                int.TryParse(Get2DAString("iprp_damagecost", "Die", row), out var die))
                damageCosts[row] = (dice, die);
        }
        _damageCosts = damageCosts;

        var blueprints = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var storage = GetObjectByTag("TEMP_ITEM_STORAGE");
        // Container items all share one inventory grid; measure it from a bag.
        var bag = CreateItemOnObject("bag_b", storage);
        var repository = NWNXLib.g_pAppManager.m_pServerExoApp.GetItemByGameObjectID(bag)?.m_pItemRepository
            ?? throw new InvalidOperationException("A container could not be created to measure its inventory.");
        ContainerWidth = repository.m_nWidth;
        ContainerHeight = repository.m_nHeight;
        DestroyObject(bag);
        foreach (var resref in ReplacementBlueprints())
        {
            if (blueprints.ContainsKey(resref))
                continue;
            var item = CreateItemOnObject(resref, storage);
            if (!GetIsObjectValid(item))
                throw new InvalidOperationException($"Replacement blueprint '{resref}' could not be created.");
            blueprints[resref] = ObjectPlugin.Serialize(item);
            DestroyObject(item);
        }
        Load(sizes, definitions, blueprints);
        _weaponIcons = LoadWeaponIcons(sizes.Keys);
        Feature.MigrationDefinition.CharacterMigration.SavedItemResistanceMigration.Load();
    }

    /// <summary>
    /// <see cref="Item.HasInventoryIcon"/> for every attack weapon base item and simple model: whether
    /// <c>Item.ResolveIconResref</c> finds a real icon, which <see cref="Item.IsEconomyRestricted(uint)"/> reads.
    /// Weapons are neither cloaks nor scrolls, so the simple-model and default-icon rules decide it.
    /// </summary>
    private static IReadOnlyDictionary<(int BaseItem, int Model), bool> LoadWeaponIcons(IEnumerable<int> baseItems)
    {
        static bool Exists(string resref) => !string.IsNullOrWhiteSpace(resref) &&
            (ResManGetAliasFor(resref, ResType.TGA) != string.Empty || ResManGetAliasFor(resref, ResType.DDS) != string.Empty);
        var icons = new Dictionary<(int, int), bool>();
        foreach (var row in baseItems)
        {
            if (!Item.IsAttackWeaponType((BaseItem)row))
                continue;
            var defaultIcon = Get2DAString("baseitems", "DefaultIcon", row);
            var hasDefaultIcon = !defaultIcon.StartsWith("iinvalid") && (Exists(defaultIcon) || Exists(defaultIcon + "_001"));
            if (Get2DAString("baseitems", "ModelType", row) != "0")
            {
                icons[(row, -1)] = hasDefaultIcon;
                continue;
            }
            var itemClass = Get2DAString("baseitems", "ItemClass", row);
            var stripped = defaultIcon.Length >= 4 && defaultIcon[^4] == '_' ? defaultIcon[..^4] : defaultIcon;
            for (var model = 0; model < MaximumSimpleModel; model++)
            {
                var id = model.ToString().PadLeft(3, '0');
                icons[(row, model)] = Exists(("i" + itemClass + "_" + id).ToLower()) || Exists(stripped + "_" + id) || hasDefaultIcon;
            }
        }
        return icons;
    }

    private const int MaximumSimpleModel = 1024;

    /// <summary>The dice an iprp_damagecost row rolls, for rows with both columns set.</summary>
    public static bool TryGetDamageCost(int row, out (int Dice, int Die) cost)
    {
        if (_damageCosts == null)
            throw new InvalidOperationException("Saved character data is not loaded.");
        return _damageCosts.TryGetValue(row, out cost);
    }

    /// <summary>The classes.2da leveling package of a class, as <c>PlayerInitialization.EnsureNativeLevels</c> reads it.</summary>
    public static bool TryGetClassPackage(ClassType @class, out int package)
    {
        if (_classPackages == null)
            throw new InvalidOperationException("Saved character data is not loaded.");
        return _classPackages.TryGetValue((int)@class, out package);
    }

    /// <summary>Saved-data <see cref="Item.HasInventoryIcon"/> for an attack weapon.</summary>
    public static bool WeaponHasInventoryIcon(BaseItem baseItem, int simpleModel)
    {
        if (_weaponIcons == null)
            throw new InvalidOperationException("Saved character data is not loaded.");
        return _weaponIcons.TryGetValue(((int)baseItem, -1), out var hasIcon) ||
               _weaponIcons.TryGetValue(((int)baseItem, simpleModel), out hasIcon)
            ? hasIcon
            : throw new InvalidOperationException($"No icon is known for weapon {baseItem} with model {simpleModel}.");
    }

    /// <summary>Every blueprint a character migration creates a replacement item from.</summary>
    private static IEnumerable<string> ReplacementBlueprints() =>
        DungeonSaberMigration.Replacements.Values.Concat(ObsoleteItemMigration.ConversionResRefs);

    /// <summary>Supplies the data directly, for tests and for callers that read it elsewhere.</summary>
    public static void Load(IReadOnlyDictionary<int, (int Width, int Height)> inventorySizes,
        IReadOnlyDictionary<int, (int CostTable, int Param1Table)> propertyDefinitions,
        IReadOnlyDictionary<string, string> blueprints)
    {
        _inventorySizes = inventorySizes;
        _propertyDefinitions = propertyDefinitions;
        _blueprints = blueprints;
    }

    /// <summary>A new item from a module blueprint, as <c>CreateItemOnObject</c> makes it, serialized (UTI, base64).</summary>
    public static string GetBlueprint(string resref)
    {
        if (_blueprints == null)
            throw new InvalidOperationException("Saved character data is not loaded.");
        return _blueprints.TryGetValue(resref, out var data)
            ? data
            : throw new InvalidOperationException($"No replacement blueprint is loaded for '{resref}'.");
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
