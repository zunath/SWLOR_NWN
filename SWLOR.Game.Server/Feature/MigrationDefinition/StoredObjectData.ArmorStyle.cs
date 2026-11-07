using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

internal sealed partial class StoredObjectData
{
    internal static bool MigrateRetiredArmorStyleData(string original, out string migrated)
    {
        migrated = original;
        if (string.IsNullOrWhiteSpace(original)) return false;
        var document = new StoredObjectData(Convert.FromBase64String(original));
        if (document.FileType is not ("UTI " or "UTC " or "BIC "))
            throw new InvalidDataException($"Unsupported stored object type '{document.FileType}'.");
        if (!MigrateRetiredArmorStyleNode(document.Root, !document.IsCreature, new HashSet<Node>())) return false;
        migrated = document.Serialize();
        return true;
    }

    /// <summary>
    /// The release's stored-item rules in one decode: enzymes (<see cref="EnzymeBaseItemMigration"/>) and retired
    /// armor styles (<see cref="RetiredArmorStyleMigration"/>).
    /// </summary>
    internal static bool MigrateEnzymeAndArmorStyleData(string original, out string migrated)
    {
        migrated = original;
        if (string.IsNullOrWhiteSpace(original)) return false;
        var document = new StoredObjectData(Convert.FromBase64String(original));
        if (document.FileType is not ("UTI " or "UTC " or "BIC "))
            throw new InvalidDataException($"Unsupported stored object type '{document.FileType}'.");
        var isItem = !document.IsCreature;
        var changed = MigrateEnzymeNode(document.Root, isItem, new HashSet<Node>());
        changed |= MigrateRetiredArmorStyleNode(document.Root, isItem, new HashSet<Node>());
        if (!changed) return false;
        migrated = document.Serialize();
        return true;
    }

    /// <summary>Creature body parts keep their values: body part 2 is a real body (tattooed arms).</summary>
    internal static bool MigrateRetiredArmorStyleNode(Node node, bool isItem, HashSet<Node> visited)
    {
        if (!visited.Add(node)) return false;
        var changed = false;
        if (isItem)
        {
            if (ReadBaseItem(node) == BaseItem.Armor)
            {
                foreach (var part in RetiredArmorStyleMigration.Parts)
                {
                    var label = "ArmorPart_" + ArmorPartLabels[(int)part];
                    if (ReadPart(node, label) != RetiredArmorStyleMigration.Removed) continue;
                    WritePart(node, label, RetiredArmorStyleMigration.Replacement);
                    changed = true;
                }
            }
            var droid = FindLocal(node, ConstructedDroidVariable, 3);
            if (droid != null)
            {
                var original = ReadText(droid);
                var migrated = HeadgearSlotMigration.MigrateDroid(original,
                    data => MigrateRetiredArmorStyleData(data, out var result) ? result : data, repairHeadgear: false);
                if (original != migrated)
                {
                    WriteText(droid, migrated);
                    changed = true;
                }
            }
        }
        foreach (var list in node.Fields.Where(IsInventoryList))
        foreach (var item in list.Children)
            changed |= MigrateRetiredArmorStyleNode(item, true, visited);
        return changed;
    }
}
