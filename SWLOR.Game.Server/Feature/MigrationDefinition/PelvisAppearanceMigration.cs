using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>Consolidates pelvis 35 into 34, retaining the former garment's colors on the pelvis alone.</summary>
public static class PelvisAppearanceMigration
{
    public const int Previous = 35;
    public const int Current = 34;
    internal static readonly string[] MaterialPrefixes = { "pfh0_pelvis", "pmh0_pelvis" };
    internal static readonly TintMapLayerType[] MaterialLayers = { TintMapLayerType.Cloth1, TintMapLayerType.Cloth2, TintMapLayerType.Leather1 };

    public static bool MigrateSavedObject(string original, out string migrated) =>
        StoredObjectData.MigratePelvisData(original, out migrated);

    internal static bool MigrateOutfitListing(PlayerOutfit outfit)
    {
        if (outfit.PelvisId != Previous) return false;
        outfit.PelvisId = Current;
        return true;
    }

    internal static TintMapLayerType DestinationLayer(TintMapLayerType layer) => layer switch
    {
        TintMapLayerType.Cloth1 => TintMapLayerType.Cloth2,
        TintMapLayerType.Cloth2 => TintMapLayerType.Cloth1,
        _ => layer,
    };

    internal static AppearanceArmorColor ColorChannel(TintMapLayerType layer) => layer switch
    {
        TintMapLayerType.Cloth1 => AppearanceArmorColor.Cloth1,
        TintMapLayerType.Cloth2 => AppearanceArmorColor.Cloth2,
        _ => AppearanceArmorColor.Leather1,
    };

    internal static string OverrideName(TintMapLayerType layer) =>
        ArmorColorIndexCalculator.GetPerPartOverrideVariableName(AppearanceArmor.Pelvis, ColorChannel(layer));

    internal static int ColorIndex(TintMapLayerType layer) =>
        ArmorColorIndexCalculator.CalculatePerPart(AppearanceArmor.Pelvis, ColorChannel(layer));

    internal static int EffectivePalette(TintMapLayerType layer, Func<int, int> readPalette, Func<string, int> readLocal)
    {
        var part = readPalette(ColorIndex(layer));
        return ArmorColorIndexCalculator.ShouldUsePerPartColor(part, readLocal(OverrideName(layer)) > 0)
            ? part : readPalette((int)ColorChannel(layer));
    }

    internal static Dictionary<string, int> ConvertedTints(Func<string, int> readLocal, Func<int, int> readPalette)
    {
        var colors = new Dictionary<string, int>();
        foreach (var prefix in MaterialPrefixes)
        foreach (var layer in MaterialLayers)
        {
            var value = readLocal(TintMapVariable.GetName(prefix + "035", layer));
            if (value <= 0 && readLocal(OverrideName(layer)) <= 0)
            {
                var global = readLocal(TintMapVariable.GetItemGlobalColorStateName(layer));
                if (TintMapColor.TryFromStoredValue(global, out _)) value = global;
            }
            if (value <= 0) value = Math.Clamp(EffectivePalette(layer, readPalette, readLocal), 0, TintMapMaterialRegistry.PaletteColorCount - 1) + 1;
            colors[TintMapVariable.GetName(prefix + "034", DestinationLayer(layer))] = value;
        }
        return colors;
    }

    internal static void NormalizeLegacyGlobalTints(IEnumerable<string> localNames, IReadOnlyDictionary<string, int> convertedColors,
        Func<string, int> readLocal, Action<string, int> writeLocal, Action<string> removeLocal)
    {
        var names = localNames.ToArray();
        foreach (var layer in MaterialLayers)
        {
            if (!convertedColors.Any(entry => TintMapVariable.TryGetLayer(entry.Key, out var colorLayer) && colorLayer == layer &&
                    TintMapColor.TryFromStoredValue(entry.Value, out _)))
                continue;
            var marker = TintMapVariable.GetItemGlobalInheritanceStateName(layer);
            if (readLocal(marker) != 0) continue;
            var global = readLocal(TintMapVariable.GetItemGlobalColorStateName(layer));
            if (TintMapColor.TryFromStoredValue(global, out _))
            {
                foreach (var name in names)
                {
                    if (!TintMapVariable.TryParse(name, out var material, out var variableLayer) || variableLayer != layer || readLocal(name) != global)
                        continue;
                    // Unknown or shared materials keep their explicit color rather than guessing their owner.
                    for (var part = 0; part < (int)AppearanceArmor.Num; part++)
                    {
                        if (!TintMapMaterialRegistry.IsEquipmentMaterialExclusiveToArmorPart(material, layer, (AppearanceArmor)part))
                            continue;
                        if (readLocal(ArmorColorIndexCalculator.GetPerPartOverrideVariableName((AppearanceArmor)part, ColorChannel(layer))) <= 0)
                            removeLocal(name);
                        break;
                    }
                }
            }
            writeLocal(marker, 1);
        }
    }

    /// <summary>The native counterpart used to verify saved-data conversion against engine reads and writes.</summary>
    public static bool MigrateObject(uint obj) => MigrateObject(obj, new HashSet<uint>());

    private static bool MigrateObject(uint obj, HashSet<uint> visited)
    {
        if (!GetIsObjectValid(obj) || !visited.Add(obj)) return false;
        var changed = false;
        var type = GetObjectType(obj);
        if (type == ObjectType.Item)
        {
            if (GetBaseItemType(obj) == BaseItem.Armor && GetItemAppearance(obj, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Pelvis) == Previous)
            {
                int ReadLocal(string name) => GetLocalInt(obj, name);
                int ReadPalette(int index) => GetItemAppearance(obj, ItemAppearanceType.ArmorColor, index);
                var colors = ConvertedTints(ReadLocal, ReadPalette);
                var cloth1 = EffectivePalette(TintMapLayerType.Cloth1, ReadPalette, ReadLocal);
                var cloth2 = EffectivePalette(TintMapLayerType.Cloth2, ReadPalette, ReadLocal);
                var localNames = Enumerable.Range(0, ObjectPlugin.GetLocalVariableCount(obj))
                    .Select(index => ObjectPlugin.GetLocalVariable(obj, index))
                    .Where(variable => variable.Type == LocalVariableType.Int).Select(variable => variable.Key).ToArray();
                NormalizeLegacyGlobalTints(localNames, colors, ReadLocal,
                    (name, value) => SetLocalInt(obj, name, value), name => DeleteLocalInt(obj, name));
                EquippedItemAppearance.Set(obj, ItemAppearanceType.ArmorColor, ColorIndex(TintMapLayerType.Cloth1), cloth2);
                EquippedItemAppearance.Set(obj, ItemAppearanceType.ArmorColor, ColorIndex(TintMapLayerType.Cloth2), cloth1);
                SetLocalInt(obj, OverrideName(TintMapLayerType.Cloth1), 1);
                SetLocalInt(obj, OverrideName(TintMapLayerType.Cloth2), 1);
                foreach (var (name, value) in colors) SetLocalInt(obj, name, value);
                RemoveOldTints(name => DeleteLocalInt(obj, name));
                EquippedItemAppearance.Set(obj, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Pelvis, Current);
                changed = true;
            }
            var original = GetLocalString(obj, "CONSTRUCTED_DROID");
            var migrated = HeadgearSlotMigration.MigrateDroid(original,
                data => MigrateSavedObject(data, out var result) ? result : data, repairHeadgear: false);
            if (original != migrated)
            {
                SetLocalString(obj, "CONSTRUCTED_DROID", migrated);
                changed = true;
            }
        }
        else if (type == ObjectType.Creature)
        {
            if (GetCreatureBodyPart(CreaturePart.Pelvis, obj) == Previous)
            {
                foreach (var prefix in MaterialPrefixes)
                foreach (var layer in MaterialLayers)
                {
                    var value = GetLocalInt(obj, TintMapVariable.GetName(prefix + "035", layer));
                    if (value > 0) SetLocalInt(obj, TintMapVariable.GetName(prefix + "034", DestinationLayer(layer)), value);
                }
                RemoveOldTints(name => DeleteLocalInt(obj, name));
                SetCreatureBodyPart(CreaturePart.Pelvis, Current, obj);
                changed = true;
            }
            for (var slot = 0; slot < NumberOfInventorySlots; slot++)
                changed |= MigrateObject(GetItemInSlot((InventorySlot)slot, obj), visited);
        }
        if (GetHasInventory(obj))
            for (var item = GetFirstItemInInventory(obj); GetIsObjectValid(item); item = GetNextItemInInventory(obj))
                changed |= MigrateObject(item, visited);
        return changed;
    }

    internal static void RemoveOldTints(Action<string> remove)
    {
        foreach (var prefix in MaterialPrefixes)
        foreach (var layer in MaterialLayers)
            remove(TintMapVariable.GetName(prefix + "035", layer));
    }
}
