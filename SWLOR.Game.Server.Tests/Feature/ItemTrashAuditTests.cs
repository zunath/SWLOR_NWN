using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.LogService;

namespace SWLOR.Game.Server.Tests.Feature;

public class ItemTrashAuditTests
{
    private Type _harness = null!;

    [OneTimeSetUp]
    public void CompileProductionDisposalHandlers()
    {
        var service = ReadSource("Service/ItemTrash.cs").DescendantNodes().OfType<ClassDeclarationSyntax>().Single();
        var trash = ReadSource("Feature/TrashCan.cs").DescendantNodes().OfType<ClassDeclarationSyntax>().Single();
        var confirm = ReadSource("Feature/DialogDefinition/DestroyItemDialog.cs").DescendantNodes()
            .OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.ValueText == "ConfirmPageInit");
        var getDm = SWLOR.Game.Server.Tests.Service.DMPossessionHarness.InstanceMethod(
            "Domain/Administration/DMPresenceService.cs", "GetDM");
        var source = $$"""
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Text.RegularExpressions;
            using Newtonsoft.Json;
            using static World;

            public enum ObjectType { Creature, Placeable, Item }
            public enum DisturbType { Added, Removed }
            public enum ItemPropertyType { Damage = 1, Accuracy = 2 }
            public sealed class Property
            {
                public int Type;
                public IntPtr Handle = new IntPtr(123);
            }
            public static class World
            {
                public const uint OBJECT_INVALID = 0;
                public static uint OBJECT_SELF = 10, LastOpenedBy = 1, LastClosedBy = 1, LastDisturbed = 1, DisturbedItem = 20;
                public static DisturbType Disturbance = DisturbType.Added;
                public static bool FailSnapshot, FailDescriptions;
                public static readonly List<string> Timeline = new();
                public static readonly Dictionary<uint, List<uint>> Inventory = new();
                public static readonly Dictionary<uint, List<Property>> Properties = new();
                public static readonly Dictionary<uint, int> InventoryIndexes = new(), PropertyIndexes = new();
                public static readonly Dictionary<(uint, string), bool> Bools = new();
                public static readonly Dictionary<(uint, string), uint> Objects = new();
                public static bool GetIsObjectValid(uint obj) => obj is 1 or 2 or 10 or 20 or 21;
                public static ObjectType GetObjectType(uint obj) => obj >= 20 ? ObjectType.Item : obj == 10 ? ObjectType.Placeable : ObjectType.Creature;
                public static bool GetIsPC(uint obj) => obj == 1;
                public static bool GetIsDM(uint obj) => false;
                public static bool GetIsDMPossessed(uint obj) => obj == 2;
                public static uint GetMaster(uint obj) => obj == 2 ? 1u : OBJECT_INVALID;
                public static string GetObjectUUID(uint obj) => $"id-{obj}";
                public static string GetName(uint obj) => obj == 20 ? "Custom \"item\" {name}" : $"name-{obj}";
                public static string GetResRef(uint obj) => $"resref-{obj}";
                public static string GetTag(uint obj) => $"tag-{obj}";
                public static int GetBaseItemType(uint obj) => 42;
                public static int GetItemStackSize(uint obj) => obj == 20 ? 7 : 2;
                public static int GetItemCharges(uint obj) => 4;
                public static string GetPCPlayerName(uint obj) => "Account";
                public static string GetPCPublicCDKey(uint obj) => "PublicKey";
                public static bool GetLocalBool(uint obj, string key) => Bools.GetValueOrDefault((obj, key));
                public static void SetLocalBool(uint obj, string key, bool value) => Bools[(obj, key)] = value;
                public static uint GetLocalObject(uint obj, string key) => Objects.GetValueOrDefault((obj, key));
                public static void SetLocalObject(uint obj, string key, uint value) => Objects[(obj, key)] = value;
                public static uint GetLastOpenedBy() => LastOpenedBy;
                public static uint GetLastClosedBy() => LastClosedBy;
                public static uint GetLastDisturbed() => LastDisturbed;
                public static uint GetInventoryDisturbItem() => DisturbedItem;
                public static DisturbType GetInventoryDisturbType() => Disturbance;
                public static uint GetFirstItemInInventory(uint obj) { InventoryIndexes[obj] = 0; return At(Inventory, obj, 0); }
                public static uint GetNextItemInInventory(uint obj) => At(Inventory, obj, ++InventoryIndexes[obj]);
                private static uint At(Dictionary<uint, List<uint>> rows, uint obj, int index)
                    => rows.TryGetValue(obj, out var items) && index < items.Count ? items[index] : OBJECT_INVALID;
                public static Property GetFirstItemProperty(uint obj) { PropertyIndexes[obj] = 0; return PropertyAt(obj, 0); }
                public static Property GetNextItemProperty(uint obj) => PropertyAt(obj, ++PropertyIndexes[obj]);
                private static Property PropertyAt(uint obj, int index)
                    => Properties.TryGetValue(obj, out var rows) && index < rows.Count ? rows[index] : null;
                public static bool GetIsItemPropertyValid(Property property) => property != null;
                public static ItemPropertyType GetItemPropertyType(Property property) => (ItemPropertyType)property.Type;
                public static int GetItemPropertySubType(Property property) => 3;
                public static int GetItemPropertyCostTable(Property property) => 4;
                public static int GetItemPropertyCostTableValue(Property property) => 5;
                public static int GetItemPropertyParam1(Property property) => 6;
                public static int GetItemPropertyParam1Value(Property property) => 7;
                public static int GetItemPropertyDurationType(Property property) => 1;
                public static int GetItemPropertyDuration(Property property) => 120;
                public static int GetItemPropertyDurationRemaining(Property property) => 90;
                public static int GetItemPropertyUsesPerDayRemaining(uint item, IntPtr property) => 2;
                public static string GetItemPropertyTag(Property property) => "PropertyTag";
                public static void DestroyObject(uint obj) => Timeline.Add($"destroy-{obj}");
                public static void SendMessageToPC(uint player, string text) { }
                public static void FloatingTextStringOnCreature(string text, uint player, bool broadcast) { }
            }
            public static class EventsPlugin { public static void SkipEvent() { } }
            public static class ColorToken { public static string Red(string text) => text; }
            public static class PlayerName { public static string GetAuditName(uint player) => "Canonical Player [A Masked Human]"; }
            public static class Disguise { public static string GetIdentityKey(uint player) => "disguise:mask-id"; }
            public static class DMPresence { {{getDm}} }
            public static class Item
            {
                public static string BuildItemPropertyString(uint item) => FailDescriptions
                    ? throw new FormatException("Missing property label") : "Damage +5\nAccuracy +5";
            }
            public static class ObjectPlugin
            {
                public static string Serialize(uint item)
                {
                    if (Bools.ContainsKey((item, "ITEM_TRASH_AUDITED"))) throw new Exception("Snapshot contains audit marker");
                    Timeline.Add($"serialize-{item}");
                    return FailSnapshot && item == 20 ? throw new Exception("Serialization failed") : $"snapshot-{item}";
                }
            }
            public enum LogGroup { ItemTrash }
            public static class Log
            {
                public static readonly List<Dictionary<string, object>> Records = new();
                private static Dictionary<string, object> Bind(string template, object[] values)
                {
                    var fields = Regex.Matches(template, @"\{(?<name>[^}]+)\}").Select(match => match.Groups["name"].Value).ToArray();
                    if (fields.Length != values.Length) throw new Exception("Unbound audit properties");
                    return fields.Select((name, index) => (name, value: values[index])).ToDictionary(row => row.name, row => row.value);
                }
                public static void WriteStructured(LogGroup group, string template, params object[] values)
                {
                    var record = Bind(template, values);
                    Records.Add(record);
                    Timeline.Add("audit-" + record["ItemId"]);
                }
                public static void WriteError(Exception error, string template, params object[] values)
                {
                    var record = Bind(template, values);
                    record["Error"] = error.Message;
                    Records.Add(record);
                }
            }
            public sealed class NWNEventHandlerAttribute : Attribute { public NWNEventHandlerAttribute(string script) { } }
            public static class ScriptName
            {
                public const string OnInputDropItemBefore = "drop", OnTrashOpened = "open", OnTrashClosed = "close", OnTrashDisturbed = "disturb";
            }
            {{service}}
            {{trash}}
            public sealed class ConversationMenuPage
            {
                public string Header;
                public readonly List<Action> Responses = new();
                public void AddResponse(string text, Action action) => Responses.Add(action);
            }
            public sealed class DestroyItemDialog
            {
                private sealed class Model { public uint Item = 20; public string ItemName = "Item"; }
                private readonly Model _model = new();
                private uint Player => 1;
                private T Data<T>() where T : class => _model as T;
                private void Close() { }
                {{confirm}}
                public void Run()
                {
                    var page = new ConversationMenuPage();
                    ConfirmPageInit(page);
                    page.Responses.Single()();
                }
            }
            public static class Harness
            {
                public static string Run(string scenario)
                {
                    World.Timeline.Clear(); World.Inventory.Clear(); World.Properties.Clear();
                    World.InventoryIndexes.Clear(); World.PropertyIndexes.Clear(); World.Bools.Clear(); World.Objects.Clear();
                    Log.Records.Clear();
                    World.LastOpenedBy = 1; World.LastClosedBy = 1; World.LastDisturbed = 1; World.DisturbedItem = 20;
                    World.Disturbance = DisturbType.Added; World.FailSnapshot = false; World.FailDescriptions = false;
                    World.Inventory[10] = new() { 20 };
                    World.Properties[20] = new() { new Property { Type = 1 }, new Property { Type = 1400 } };
                    if (scenario == "nested") World.Inventory[20] = new() { 21 };
                    if (scenario == "snapshot-failure") { World.FailSnapshot = true; World.Inventory[20] = new() { 21 }; }
                    if (scenario == "label-failure") World.FailDescriptions = true;
                    if (scenario == "possessed-player") World.LastDisturbed = 2;
                    if (scenario == "removed") World.Disturbance = DisturbType.Removed;
                    if (scenario == "invalid") World.DisturbedItem = 999;
                    if (scenario == "confirmed") new DestroyItemDialog().Run();
                    else if (scenario == "close-fallback")
                    {
                        TrashCan.AlertPlayer(); World.LastClosedBy = 0; TrashCan.CleanUp();
                    }
                    else if (scenario == "unknown-player")
                    {
                        World.LastDisturbed = 0; TrashCan.DestroyItem();
                    }
                    else
                    {
                        TrashCan.AlertPlayer();
                        TrashCan.DestroyItem();
                        if (scenario == "duplicate") TrashCan.CleanUp();
                    }
                    return JsonConvert.SerializeObject(new { Records = Log.Records, Timeline = World.Timeline });
                }
            }
            """;
        var paths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Append(typeof(JsonConvert).Assembly.Location).Distinct();
        var references = paths.Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("ItemTrashAuditHarness", [CSharpSyntaxTree.ParseText(source)],
            references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));
        _harness = Assembly.Load(stream.ToArray()).GetType("Harness")!;
    }

    [TestCase("added", "trash-added")]
    [TestCase("close-fallback", "trash-close")]
    [TestCase("confirmed", "destroy-item-confirmed")]
    [TestCase("possessed-player", "trash-added")]
    public void DisposalPaths_LogIdentityAndCompleteItemDetailsBeforeDestruction(string scenario, string source)
    {
        var result = Run(scenario);
        var record = result["Records"]!.Single();
        record["PlayerId"]!.Value<string>().Should().Be("id-1");
        record["PlayerName"]!.Value<string>().Should().Be("Canonical Player [A Masked Human]");
        record["IdentityKey"]!.Value<string>().Should().Be("disguise:mask-id");
        record["AccountName"]!.Value<string>().Should().Be("Account");
        record["PublicCDKey"]!.Value<string>().Should().Be("PublicKey");
        record["Source"]!.Value<string>().Should().Be(source);
        JsonConvert.DeserializeObject<string>(record["ItemName"]!.Value<string>()!).Should().Be("Custom \"item\" {name}");
        record["ItemId"]!.Value<string>().Should().Be("id-20");
        record["Resref"]!.Value<string>().Should().Be("resref-20");
        record["Quantity"]!.Value<int>().Should().Be(7);
        record["Charges"]!.Value<int>().Should().Be(4);
        record["PropertyDescriptions"]!.Value<string>().Should().Contain("Damage +5");
        record["PropertyDescriptions"]!.Value<string>().Should().NotContain("\n").And.Contain("\\n");
        record["SnapshotComplete"]!.Value<bool>().Should().BeTrue();
        record["SerializedItem"]!.Value<string>().Should().Be("snapshot-20");
        var timeline = result["Timeline"]!.Values<string>().ToList();
        timeline.IndexOf("serialize-20").Should().BeLessThan(timeline.IndexOf("audit-id-20"));
        timeline.IndexOf("audit-id-20").Should().BeLessThan(timeline.IndexOf("destroy-20"));
    }

    [Test]
    public void Properties_IncludeEveryRawFieldAndUnknownCustomTypes()
    {
        var record = Run("added")["Records"]!.Single();
        var properties = JArray.Parse(record["Properties"]!.Value<string>()!);
        properties.Should().HaveCount(2);
        properties[1]["Type"]!.Value<int>().Should().Be(1400);
        foreach (var property in properties)
        {
            property["SubType"]!.Value<int>().Should().Be(3);
            property["CostTable"]!.Value<int>().Should().Be(4);
            property["CostTableValue"]!.Value<int>().Should().Be(5);
            property["Param1"]!.Value<int>().Should().Be(6);
            property["Param1Value"]!.Value<int>().Should().Be(7);
            property["DurationType"]!.Value<int>().Should().Be(1);
            property["DurationSeconds"]!.Value<int>().Should().Be(120);
            property["RemainingSeconds"]!.Value<int>().Should().Be(90);
            property["UsesPerDayRemaining"]!.Value<int>().Should().Be(2);
            property["Tag"]!.Value<string>().Should().Be("PropertyTag");
        }
    }

    [Test]
    public void Containers_LogContentsWithTheirParentAndOwnQuantities()
    {
        var records = Run("nested")["Records"]!;
        records.Should().HaveCount(2);
        records[0]!["ParentItemId"]!.Value<string>().Should().BeEmpty();
        records[1]!["ParentItemId"]!.Value<string>().Should().Be("id-20");
        records[1]!["ItemId"]!.Value<string>().Should().Be("id-21");
        records[1]!["Quantity"]!.Value<int>().Should().Be(2);
    }

    [Test]
    public void ClosingAfterAdding_DoesNotDuplicateTheAuditForPendingDestruction()
    {
        Run("duplicate")["Records"]!.Should().HaveCount(1);
    }

    [TestCase("removed")]
    [TestCase("invalid")]
    public void NonDisposals_DoNotCreateTrashRecords(string scenario)
    {
        Run(scenario)["Records"]!.Should().BeEmpty();
    }

    [Test]
    public void SnapshotFailure_PreservesRawAuditAndContentsWithoutBlockingDisposal()
    {
        var result = Run("snapshot-failure");
        result["Timeline"]!.Values<string>().Should().Contain("destroy-20");
        var records = result["Records"]!.Where(record => record["Error"] == null).ToArray();
        records.Should().HaveCount(2);
        records[0]["SnapshotComplete"]!.Value<bool>().Should().BeFalse();
        JArray.Parse(records[0]["Properties"]!.Value<string>()!).Should().HaveCount(2);
        result["Records"]!.Any(record => record["Error"]?.Value<string>() == "Serialization failed").Should().BeTrue();
    }

    [Test]
    public void MissingPropertyLabels_PreserveAllRawPropertiesAndSnapshot()
    {
        var record = Run("label-failure")["Records"]!.Single();
        record["PropertyDescriptions"]!.Value<string>().Should().Contain("see raw Properties");
        JArray.Parse(record["Properties"]!.Value<string>()!).Should().HaveCount(2);
        record["SnapshotComplete"]!.Value<bool>().Should().BeTrue();
    }

    [Test]
    public void UnknownDisposer_IsExplicitInsteadOfInventingPlayerIdentity()
    {
        var record = Run("unknown-player")["Records"]!.Single();
        record["PlayerId"]!.Value<string>().Should().BeEmpty();
        record["PlayerName"]!.Value<string>().Should().Be("Unknown player");
    }

    [Test]
    public void TrashLogger_IsEnabledInEveryServerEnvironment()
    {
        typeof(LogGroup).GetField(nameof(LogGroup.ItemTrash))!.GetCustomAttribute<LogGroupAttribute>()!
            .Environment.Should().Be(ServerEnvironmentType.All);
    }

    private JObject Run(string scenario)
        => JObject.Parse((string)_harness.GetMethod("Run")!.Invoke(null, [scenario])!);

    private static SyntaxNode ReadSource(string path)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        return CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(directory!.FullName,
            "SWLOR.Game.Server", path))).GetRoot();
    }
}
