using System;
using System.Linq;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.CharacterMigration;

/// <summary>
/// Saved-data <see cref="Native.LegacyItemProperties"/>: the conversion the engine applies to retired item property
/// rows whenever it loads an item. Login-time migrations always ran on converted items, so the upgrade converts a
/// character's items first, marking each converted item as the load does.
/// </summary>
internal static class SavedLegacyItemProperties
{
    private const int AccuracyCostTable = 45;
    private const int DamageCostTable = 34;
    private const int MaximumAccuracy = 100;

    /// <summary>Converts every equipped and carried item, including container contents, as loading the character would.</summary>
    public static void ConvertOnLoad(SavedCharacter character)
    {
        foreach (var item in character.Equipped.Concat(character.CarriedRecursive))
            if (Convert(item))
                item.SetLocalInt(Native.LegacyItemProperties.ConvertedVariable, 1);
    }

    /// <summary>
    /// Rewrites legacy Accuracy Bonus and Enhancement Bonus rows as one Accuracy property holding their capped sum
    /// (the rest become empty Accuracy rows), and a saber's legacy Damage Bonus rows as DMG.
    /// </summary>
    public static bool Convert(SavedItem item)
    {
        var properties = item.Properties;
        var accuracy = properties.Where(property => IsLegacyAccuracy(property.Type))
            .Aggregate(0, (total, property) => (int)Math.Min(MaximumAccuracy, (long)total + property.CostValue));
        var isSaber = item.BaseItem is BaseItem.Lightsaber or BaseItem.Saberstaff;
        var converted = false;
        var first = true;
        foreach (var property in properties)
        {
            if (IsLegacyAccuracy(property.Type))
            {
                Rewrite(property, ItemPropertyType.Accuracy, AccuracyCostTable, first ? accuracy : 0, ushort.MaxValue);
                first = false;
                converted = true;
            }
            else if (property.Type == ItemPropertyType.DamageBonus && isSaber &&
                     Native.LegacyItemProperties.TryGetLegacyDamageBonus((ushort)property.CostValue, out var damage))
            {
                Rewrite(property, ItemPropertyType.DMG, DamageCostTable, Math.Clamp(damage, 0, ushort.MaxValue), 0);
                converted = true;
            }
        }
        return converted;
    }

    private static bool IsLegacyAccuracy(ItemPropertyType type) =>
        type is ItemPropertyType.EnhancementBonus or ItemPropertyType.AccuracyBonus;

    private static void Rewrite(SavedItemProperty property, ItemPropertyType type, int costTable, int costValue, int subtype)
    {
        SavedGff.WriteScalar(property.Node, "PropertyName", 2, (int)type);
        SavedGff.WriteScalar(property.Node, "CostTable", 0, costTable);
        SavedGff.WriteScalar(property.Node, "CostValue", 2, costValue);
        SavedGff.WriteScalar(property.Node, "Subtype", 2, subtype);
    }
}
