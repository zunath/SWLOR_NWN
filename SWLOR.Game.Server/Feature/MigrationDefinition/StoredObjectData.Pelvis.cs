using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

internal sealed partial class StoredObjectData
{
    private static readonly string[] ArmorPaletteLabels =
        { "Leather1Color", "Leather2Color", "Cloth1Color", "Cloth2Color", "Metal1Color", "Metal2Color" };

    private static string ArmorPaletteLabel(int index)
    {
        var colors = (int)AppearanceArmorColor.NumColors;
        return index < colors ? ArmorPaletteLabels[index] : $"APart_{(index - colors) / colors}_Col_{index % colors}";
    }

    private static int ReadPelvisPalette(Node node, int index) => ReadInteger(node, ArmorPaletteLabel(index));

    private static void WritePelvisPalette(Node node, int index, int value)
    {
        var label = ArmorPaletteLabel(index);
        // Native SaveItem omits the inherited-color sentinel.
        if (value == byte.MaxValue)
            node.Fields.RemoveAll(field => field.Name == label);
        else
            SavedGff.WriteScalar(node, label, 0, value);
    }

    internal static bool MigratePelvisData(string original, out string migrated)
    {
        migrated = original;
        if (string.IsNullOrWhiteSpace(original)) return false;
        var document = new StoredObjectData(Convert.FromBase64String(original));
        if (document.FileType is not ("UTI " or "UTC " or "BIC "))
            throw new InvalidDataException($"Unsupported stored object type '{document.FileType}'.");
        if (!MigratePelvisNode(document.Root, !document.IsCreature, new HashSet<Node>())) return false;
        migrated = document.Serialize();
        return true;
    }

    internal static bool MigratePelvisNode(Node node, bool isItem, HashSet<Node> visited)
    {
        if (!visited.Add(node)) return false;
        var changed = false;
        var label = isItem ? "ArmorPart_Pelvis" : "BodyPart_Pelvis";
        if ((!isItem || ReadBaseItem(node) == BaseItem.Armor) && ReadPart(node, label) == PelvisAppearanceMigration.Previous)
        {
            int ReadLocal(string name) => ReadLocalInteger(node, name);
            if (isItem)
            {
                int ReadPalette(int index) => ReadPelvisPalette(node, index);
                var colors = PelvisAppearanceMigration.ConvertedTints(ReadLocal, ReadPalette);
                var cloth1 = PelvisAppearanceMigration.EffectivePalette(TintMapLayerType.Cloth1, ReadPalette, ReadLocal);
                var cloth2 = PelvisAppearanceMigration.EffectivePalette(TintMapLayerType.Cloth2, ReadPalette, ReadLocal);
                var variables = node.Fields.SingleOrDefault(field => field.Name == "VarTable" && field.Type == 15)?.Children;
                var names = variables?.Where(variable => ReadInteger(variable, "Type") == 1).Select(ReadVariableName).ToArray()
                    ?? Array.Empty<string>();
                PelvisAppearanceMigration.NormalizeLegacyGlobalTints(names, colors, ReadLocal,
                    (name, value) => WriteLocalInteger(node, name, value), name =>
                        variables?.RemoveAll(variable => ReadVariableName(variable) == name && ReadInteger(variable, "Type") == 1));
                WritePelvisPalette(node, PelvisAppearanceMigration.ColorIndex(TintMapLayerType.Cloth1), cloth2);
                WritePelvisPalette(node, PelvisAppearanceMigration.ColorIndex(TintMapLayerType.Cloth2), cloth1);
                WriteLocalInteger(node, PelvisAppearanceMigration.OverrideName(TintMapLayerType.Cloth1), 1);
                WriteLocalInteger(node, PelvisAppearanceMigration.OverrideName(TintMapLayerType.Cloth2), 1);
                foreach (var (name, value) in colors) WriteLocalInteger(node, name, value);
            }
            else
            {
                foreach (var prefix in PelvisAppearanceMigration.MaterialPrefixes)
                foreach (var layer in PelvisAppearanceMigration.MaterialLayers)
                {
                    var value = ReadLocal(TintMapVariable.GetName(prefix + "035", layer));
                    if (value > 0) WriteLocalInteger(node,
                        TintMapVariable.GetName(prefix + "034", PelvisAppearanceMigration.DestinationLayer(layer)), value);
                }
            }
            PelvisAppearanceMigration.RemoveOldTints(name =>
                node.Fields.SingleOrDefault(field => field.Name == "VarTable" && field.Type == 15)?.Children
                    .RemoveAll(variable => ReadVariableName(variable) == name && ReadInteger(variable, "Type") == 1));
            WritePart(node, label, PelvisAppearanceMigration.Current);
            changed = true;
        }
        if (isItem)
        {
            var droid = FindLocal(node, ConstructedDroidVariable, 3);
            if (droid != null)
            {
                var original = ReadText(droid);
                var migrated = HeadgearSlotMigration.MigrateDroid(original,
                    data => MigratePelvisData(data, out var result) ? result : data, repairHeadgear: false);
                if (original != migrated)
                {
                    WriteText(droid, migrated);
                    changed = true;
                }
            }
        }
        foreach (var list in node.Fields.Where(IsInventoryList))
        foreach (var item in list.Children)
            changed |= MigratePelvisNode(item, true, visited);
        return changed;
    }
}
