#nullable enable
using System.Buffers.Binary;
using System.Reflection;
using System.Text;
using System.Text.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PlayerMarketService;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// Saved items (UTI, base64) and saved characters built from the module's item blueprints (Module/uti), the way the
/// engine saves them, for the space item migration tests.
/// </summary>
internal static class SpaceItemFixtures
{
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private static readonly Assembly Game = typeof(Migration).Assembly;
    internal static readonly Type Document = Game.GetType("SWLOR.Game.Server.Feature.MigrationDefinition.StoredObjectData")!;
    internal static readonly Type NodeType = Document.GetNestedType("Node", BindingFlags.NonPublic)!;

    /// <summary>Loads a saved-data blueprint, built from Module/uti, for every item a retired item becomes.</summary>
    public static void LoadBlueprints() =>
        LegacySpaceItemBlueprints.Load(LegacySpaceItemCatalog.ReplacementResrefs.ToDictionary(resref => resref, resref =>
            new LegacySpaceItemBlueprint(Item(resref), BlueprintName(resref), "icon_" + resref, MarketCategoryType.StarshipParts)));

    /// <summary>A saved item from its blueprint, with extra fields (for example inventory cells) and extra properties.</summary>
    public static string Item(string resref, IEnumerable<object>? extraFields = null, IEnumerable<object>? extraProperties = null) =>
        Encode(ItemNode(resref, extraFields, extraProperties), "UTI ");

    public static object ItemNode(string resref, IEnumerable<object>? extraFields = null, IEnumerable<object>? extraProperties = null, uint type = uint.MaxValue)
    {
        using var json = JsonDocument.Parse(File.ReadAllBytes(BlueprintPath(resref)));
        var node = Struct(json.RootElement, type);
        var fields = (System.Collections.IList)NodeType.GetField("Fields")!.GetValue(node)!;
        foreach (var field in extraFields ?? Array.Empty<object>())
        {
            // A list the blueprint already has (such as its locals) gains the extra entries.
            var existing = fields.Cast<object>().FirstOrDefault(x => FieldName(x) == FieldName(field) && (uint)x.GetType().GetField("Type")!.GetValue(x)! == 15);
            if (existing == null)
            {
                fields.Add(field);
                continue;
            }
            var target = (System.Collections.IList)existing.GetType().GetField("Children")!.GetValue(existing)!;
            foreach (var child in (System.Collections.IList)field.GetType().GetField("Children")!.GetValue(field)!)
                target.Add(child);
        }
        if (extraProperties != null)
        {
            var list = fields.Cast<object>().Single(x => FieldName(x) == "PropertiesList");
            var children = (System.Collections.IList)list.GetType().GetField("Children")!.GetValue(list)!;
            foreach (var property in extraProperties)
                children.Add(property);
        }
        return node;
    }

    public static string BlueprintName(string resref)
    {
        using var json = JsonDocument.Parse(File.ReadAllBytes(BlueprintPath(resref)));
        return json.RootElement.GetProperty("LocalizedName").GetProperty("value").GetProperty("0").GetString()!;
    }

    public static bool HasBlueprint(string resref) => File.Exists(BlueprintPath(resref));

    /// <summary>An item property as ItemPropertyCustom saves it.</summary>
    public static object Property(int type, int subtype, int costTable, int costValue) => Node(0,
        Scalar(2, "PropertyName", type), Scalar(2, "Subtype", subtype), Scalar(0, "CostTable", costTable), Scalar(2, "CostValue", costValue),
        Scalar(0, "Param1", 255), Scalar(0, "Param1Value", 255), Scalar(0, "ChanceAppear", 100));

    public static object IntLocal(string name, int value) => Node(0, Text("Name", name), Scalar(4, "Type", 1), Scalar(5, "Value", value));

    public static object StringLocal(string name, string value) => Node(0, Text("Name", name), Scalar(4, "Type", 3), Text("Value", value));

    public static object Node(uint type, params object[] fields)
    {
        var node = Activator.CreateInstance(NodeType, true)!;
        NodeType.GetField("Type")!.SetValue(node, type);
        var list = (System.Collections.IList)NodeType.GetField("Fields")!.GetValue(node)!;
        foreach (var field in fields) list.Add(field);
        return node;
    }

    public static object Field(uint type, string name, byte[]? data = null, IEnumerable<object>? children = null)
    {
        object? list = null;
        if (type is 14 or 15)
        {
            list = Activator.CreateInstance(typeof(List<>).MakeGenericType(NodeType))!;
            foreach (var child in children ?? Array.Empty<object>()) ((System.Collections.IList)list).Add(child);
        }
        return Document.GetMethod("MakeField", Members)!.Invoke(null, new[] { (object)type, name, data, list })!;
    }

    public static object Scalar(uint type, string name, int value) => Field(type, name, BitConverter.GetBytes(value));

    public static object Text(string name, string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        return Field(10, name, BitConverter.GetBytes(bytes.Length).Concat(bytes).ToArray());
    }

    public static object List(string name, params object[] children) => Field(15, name, null, children);

    public static string Encode(object root, string fileType)
    {
        var document = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(Document);
        Document.GetField("_header", Members)!.SetValue(document, Encoding.ASCII.GetBytes(fileType + "V3.2"));
        Document.GetField("_root", Members)!.SetValue(document, root);
        return Convert.ToBase64String((byte[])Document.GetMethod("SerializeBytes", Members)!.Invoke(document, null)!);
    }

    private static string FieldName(object field) => (string)field.GetType().GetProperty("Name")!.GetValue(field)!;

    private static string BlueprintPath(string resref) => Path.Combine(RepositoryRoot(), "Module", "uti", resref + ".uti.json");

    public static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); directory != null; directory = directory.Parent)
            if (Directory.Exists(Path.Combine(directory.FullName, "Module", "uti")) && Directory.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server")))
                return directory.FullName;
        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }

    private static object Struct(JsonElement element, uint type)
    {
        var fields = new List<object>();
        foreach (var property in element.EnumerateObject())
        {
            if (property.Name.StartsWith("__"))
                continue;
            fields.Add(JsonField(property.Name, property.Value));
        }
        return Node(type, fields.ToArray());
    }

    private static object JsonField(string name, JsonElement wrapper)
    {
        var kind = wrapper.GetProperty("type").GetString();
        var value = wrapper.GetProperty("value");
        switch (kind)
        {
            case "byte": return Field(0, name, Payload(value.GetInt32(), 1));
            case "char": return Field(1, name, Payload(value.GetInt32(), 1));
            case "word": return Field(2, name, Payload(value.GetInt32(), 2));
            case "short": return Field(3, name, Payload(value.GetInt32(), 2));
            case "dword": return Field(4, name, BitConverter.GetBytes((uint)value.GetInt64()));
            case "int": return Field(5, name, BitConverter.GetBytes(value.GetInt32()));
            case "float": return Field(8, name, BitConverter.GetBytes(value.GetSingle()));
            case "cexostring": return Text(name, value.GetString()!);
            case "resref":
            {
                var bytes = Encoding.ASCII.GetBytes(value.GetString()!);
                return Field(11, name, new[] { (byte)bytes.Length }.Concat(bytes).ToArray());
            }
            case "cexolocstring":
            {
                var strings = value.EnumerateObject().Where(x => x.Name != "id").ToList();
                var strref = value.TryGetProperty("id", out var id) ? (uint)id.GetInt64() : uint.MaxValue;
                using var stream = new MemoryStream();
                using var writer = new BinaryWriter(stream);
                writer.Write(strref);
                writer.Write(strings.Count);
                foreach (var entry in strings)
                {
                    var bytes = Encoding.UTF8.GetBytes(entry.Value.GetString()!);
                    writer.Write(int.Parse(entry.Name));
                    writer.Write(bytes.Length);
                    writer.Write(bytes);
                }
                var body = stream.ToArray();
                return Field(12, name, BitConverter.GetBytes(body.Length).Concat(body).ToArray());
            }
            case "list":
                return Field(15, name, null, value.EnumerateArray()
                    .Select(child => Struct(child, child.TryGetProperty("__struct_id", out var structId) ? structId.GetUInt32() : 0u)).ToList());
            default:
                throw new NotSupportedException($"Field type {kind} ({name}) is not supported by the fixture.");
        }
    }

    private static byte[] Payload(int value, int width)
    {
        var data = new byte[4];
        if (width == 1)
            data[0] = unchecked((byte)value);
        else
            BinaryPrimitives.WriteUInt16LittleEndian(data, unchecked((ushort)value));
        return data;
    }
}
