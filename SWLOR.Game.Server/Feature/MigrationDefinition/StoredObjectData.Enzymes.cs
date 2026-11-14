using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

internal sealed partial class StoredObjectData
{
    internal static bool MigrateEnzymeData(string original, out string migrated)
    {
        migrated = original;
        if (string.IsNullOrWhiteSpace(original)) return false;
        var document = new StoredObjectData(Convert.FromBase64String(original));
        if (document.FileType is not ("UTI " or "UTC " or "BIC "))
            throw new InvalidDataException($"Unsupported stored object type '{document.FileType}'.");
        if (!MigrateEnzymeNode(document.Root, !document.IsCreature, new HashSet<Node>())) return false;
        migrated = document.Serialize();
        return true;
    }

    internal static bool MigrateEnzymeNode(Node node, bool isItem, HashSet<Node> visited)
    {
        if (!visited.Add(node)) return false;
        var changed = MigrateRetiredAppearances(node, isItem);
        if (isItem)
        {
            var currentModel = ReadSimpleModel(node);
            var buildingModel = BuildingIconMigration.GetUpdatedModel(ReadResRef(node), (int)ReadBaseItem(node), currentModel);
            if (buildingModel != currentModel)
            {
                WriteSimpleModel(node, buildingModel);
                changed = true;
            }
            if (EnzymeBaseItemMigration.TryGetConversion(ReadResRef(node), ReadBaseItem(node), out var model))
            {
                WriteInteger(node, "BaseItem", (int)BaseItem.Enzyme);
                WriteSimpleModel(node, model);
                changed = true;
            }
            var staffParts = GaderffiiAppearanceMigration.GetUpdatedParts(ReadResRef(node), ReadBaseItem(node),
                ReadAppearanceParts(node, 3));
            if (staffParts != null)
            {
                for (var index = 0; index < staffParts.Length; index++)
                    WritePart(node, PartLabel(staffParts.Length, index), staffParts[index]);
                changed = true;
            }
            var droid = FindLocal(node, ConstructedDroidVariable, 3);
            if (droid != null)
            {
                var original = ReadText(droid);
                var migrated = HeadgearSlotMigration.MigrateDroid(original,
                    data => MigrateEnzymeData(data, out var result) ? result : data, repairHeadgear: false);
                if (original != migrated)
                {
                    WriteText(droid, migrated);
                    changed = true;
                }
            }
        }
        foreach (var list in node.Fields.Where(IsInventoryList))
        foreach (var item in list.Children)
            changed |= MigrateEnzymeNode(item, true, visited);
        return changed;
    }
}
