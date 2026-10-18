using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

internal sealed partial class StoredObjectData
{
    private static bool IsLegacyHeadgear(Node item)
    {
        var resref = item.Fields.SingleOrDefault(x => x.Name == "TemplateResRef" && x.Type == 11);
        var baseItem = item.Fields.SingleOrDefault(x => x.Name == "BaseItem" && x.Type == 5);
        return resref != null && baseItem != null && HeadgearSlotMigration.IsLegacyHeadgear(
            Encoding.ASCII.GetString(resref.Data.AsSpan(1)), (BaseItem)ReadScalar(baseItem));
    }

    internal static bool IsLegacyHeadgearRoot(string data) => !string.IsNullOrWhiteSpace(data) &&
        IsLegacyHeadgear(new StoredObjectData(Convert.FromBase64String(data))._root);

    internal static string RepairHeadgearSlots(string data)
    {
        var document = new StoredObjectData(Convert.FromBase64String(data));
        var visited = new HashSet<Node>();
        bool Migrate(Node node)
        {
            if (!visited.Add(node)) return false;
            var changed = false;
            var legacyHeadgear = IsLegacyHeadgear(node);
            if (legacyHeadgear)
            {
                node.Fields.Single(x => x.Name == "BaseItem").Data = BitConverter.GetBytes((int)BaseItem.Helmet);
                changed = true;
            }
            var resref = node.Fields.SingleOrDefault(x => x.Name == "TemplateResRef" && x.Type == 11);
            var baseItem = node.Fields.SingleOrDefault(x => x.Name == "BaseItem" && x.Type == 5);
            if (resref != null && baseItem != null)
            {
                var model = node.Fields.SingleOrDefault(x => x.Name == "ModelPart1");
                var extended = node.Fields.SingleOrDefault(x => x.Name == "xModelPart1");
                var current = extended != null ? (int)ReadScalar(extended) : model == null ? 0 : (int)ReadScalar(model);
                var corrected = HeadgearSlotMigration.GetCorrectedModel(Encoding.ASCII.GetString(resref.Data.AsSpan(1)),
                    legacyHeadgear ? BaseItem.Armor : (BaseItem)ReadScalar(baseItem), current);
                if (corrected != current)
                {
                    var field = MakeField(0, "ModelPart1", BitConverter.GetBytes(corrected));
                    if (model == null) node.Fields.Add(field);
                    else node.Fields[node.Fields.IndexOf(model)] = field;
                    if (extended != null)
                        node.Fields[node.Fields.IndexOf(extended)] = MakeField(2, "xModelPart1", BitConverter.GetBytes(corrected));
                    changed = true;
                }
            }
            var variables = node.Fields.SingleOrDefault(x => x.Name == "VarTable" && x.Type == 15);
            if (variables != null)
                foreach (var variable in variables.Children.Where(x => ReadVariableName(x) == "CONSTRUCTED_DROID"))
                {
                    var value = variable.Fields.SingleOrDefault(x => x.Name == "Value" && x.Type == 10);
                    if (value == null) continue;
                    var original = Encoding.UTF8.GetString(value.Data.AsSpan(4));
                    var migrated = HeadgearSlotMigration.MigrateDroid(original);
                    if (original == migrated) continue;
                    value.Data = StringData(migrated);
                    changed = true;
                }
            foreach (var list in node.Fields.Where(IsInventoryList))
            foreach (var item in list.Children)
                changed |= Migrate(item);
            return changed;
        }
        return Migrate(document._root) ? document.Serialize() : data;
    }

    private static byte[] StringData(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        return BitConverter.GetBytes(bytes.Length).Concat(bytes).ToArray();
    }

    internal static string ReadRootString(string data, string name)
    {
        var document = new StoredObjectData(Convert.FromBase64String(data));
        var variables = document._root.Fields.SingleOrDefault(x => x.Name == "VarTable" && x.Type == 15);
        var variable = variables?.Children.SingleOrDefault(x => ReadVariableName(x) == name);
        var value = variable?.Fields.SingleOrDefault(x => x.Name == "Value" && x.Type == 10);
        return value == null ? null : Encoding.UTF8.GetString(value.Data.AsSpan(4));
    }

    internal static string SetRootString(string data, string name, string text)
    {
        var document = new StoredObjectData(Convert.FromBase64String(data));
        var variables = document._root.Fields.SingleOrDefault(x => x.Name == "VarTable" && x.Type == 15);
        if (variables == null)
        {
            variables = MakeField(15, "VarTable", children: new List<Node>());
            document._root.Fields.Add(variables);
        }
        variables.Children.RemoveAll(x => ReadVariableName(x) == name);
        variables.Children.Add(new Node { Fields = new List<Field>
        {
            MakeField(10, "Name", StringData(name)), MakeField(4, "Type", BitConverter.GetBytes(3)),
            MakeField(10, "Value", StringData(text)),
        }});
        return document.Serialize();
    }
}
