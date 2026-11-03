using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Items;

/// <summary>
/// Unit tests of the item trash audit instance: the engine (NWScript and the object plugin) is replaced by recording
/// fakes that model a player, a possessing DM's avatar, a trash can and two items with properties, and the calls
/// into the services that are not converted yet (the audit name, the identity key, the property text and the log
/// sinks) are answered by a subclass that keeps what was logged.
/// </summary>
[TestFixture]
public class ItemTrashServiceTests
{
    private const uint ObjectInvalid = 0x7F000000;
    private const uint Player = 1;
    private const uint PossessedAvatar = 2;
    private const uint Container = 10;
    private const uint Item = 20;
    private const uint Child = 21;
    private const nint DamageProperty = 101;
    private const nint CustomProperty = 102;

    private FakeNWScript _engine = null!;
    private LocalVariables _locals = null!;
    private FakeObjectPlugin _objects = null!;
    private IDisposable _engineScope = null!;
    private IDisposable _objectScope = null!;
    private AuditRecorder _service = null!;

    private readonly Dictionary<uint, List<uint>> _inventories = new();
    private readonly Dictionary<uint, List<nint>> _properties = new();
    private readonly Dictionary<uint, int> _inventoryCursor = new();
    private readonly Dictionary<uint, int> _propertyCursor = new();
    private readonly List<string> _timeline = new();
    private bool _failSnapshotOfItem;
    private bool _failSnapshotOfEveryItem;

    [SetUp]
    public void SetUp()
    {
        _inventories.Clear();
        _properties.Clear();
        _inventoryCursor.Clear();
        _propertyCursor.Clear();
        _timeline.Clear();
        _failSnapshotOfItem = false;
        _failSnapshotOfEveryItem = false;

        _engine = new FakeNWScript();
        _locals = new LocalVariables(_engine);
        _objects = new FakeObjectPlugin();
        _service = new AuditRecorder(_timeline);

        _engine.SetReturnHandler("GetIsObjectValid", arguments => (uint)arguments[0]! is Player or PossessedAvatar or Container or Item or Child);
        _engine.SetReturnHandler("GetObjectType", arguments => (uint)arguments[0]! >= Item ? ObjectType.Item : (uint)arguments[0]! == Container ? ObjectType.Placeable : ObjectType.Creature);
        _engine.SetReturnHandler("GetIsDMPossessed", arguments => (uint)arguments[0]! == PossessedAvatar);
        _engine.SetReturnHandler("GetMaster", arguments => (uint)arguments[0]! == PossessedAvatar ? Player : ObjectInvalid);
        _engine.SetReturnHandler("GetObjectUUID", arguments => $"id-{(uint)arguments[0]!}");
        _engine.SetReturnHandler("GetName", arguments => (uint)arguments[0]! == Item ? "Custom \"item\" {name}" : $"name-{(uint)arguments[0]!}");
        _engine.SetReturnHandler("GetResRef", arguments => $"resref-{(uint)arguments[0]!}");
        _engine.SetReturnHandler("GetTag", arguments => $"tag-{(uint)arguments[0]!}");
        _engine.SetReturn("GetBaseItemType", 42);
        _engine.SetReturnHandler("GetItemStackSize", arguments => (uint)arguments[0]! == Item ? 7 : 2);
        _engine.SetReturn("GetItemCharges", 4);
        _engine.SetReturn("GetPCPlayerName", "Account");
        _engine.SetReturn("GetPCPublicCDKey", "PublicKey");

        _engine.SetReturnHandler("GetFirstItemInInventory", arguments => InventoryAt((uint)arguments[0]!, _inventoryCursor[(uint)arguments[0]!] = 0));
        _engine.SetReturnHandler("GetNextItemInInventory", arguments => InventoryAt((uint)arguments[0]!, ++_inventoryCursor[(uint)arguments[0]!]));

        _engine.SetReturnHandler("GetFirstItemProperty", arguments => PropertyAt((uint)arguments[0]!, _propertyCursor[(uint)arguments[0]!] = 0));
        _engine.SetReturnHandler("GetNextItemProperty", arguments => PropertyAt((uint)arguments[0]!, ++_propertyCursor[(uint)arguments[0]!]));
        _engine.SetReturnHandler("GetIsItemPropertyValid", arguments => ((ItemProperty)arguments[0]!).Handle != 0);
        _engine.SetReturnHandler("GetItemPropertyType", arguments => ((ItemProperty)arguments[0]!).Handle == DamageProperty ? 1 : 1400);
        _engine.SetReturn("GetItemPropertySubType", 3);
        _engine.SetReturn("GetItemPropertyCostTable", 4);
        _engine.SetReturn("GetItemPropertyCostTableValue", 5);
        _engine.SetReturn("GetItemPropertyParam1", 6);
        _engine.SetReturn("GetItemPropertyParam1Value", 7);
        _engine.SetReturn("GetItemPropertyDurationType", 1);
        _engine.SetReturn("GetItemPropertyDuration", 120);
        _engine.SetReturn("GetItemPropertyDurationRemaining", 90);
        _engine.SetReturn("GetItemPropertyUsesPerDayRemaining", 2);
        _engine.SetReturn("GetItemPropertyTag", "PropertyTag");

        _objects.SetReturnHandler("Serialize", arguments =>
        {
            var item = (uint)arguments[0]!;
            // An assertion here would be swallowed by the audit's own error handling, so the order is recorded.
            _timeline.Add(_locals.Int(item, "ITEM_TRASH_AUDITED") == 0 ? $"serialize-{item}" : $"serialize-after-marker-{item}");
            if (_failSnapshotOfEveryItem || (_failSnapshotOfItem && item == Item))
                throw new InvalidOperationException("Serialization failed");
            return $"snapshot-{item}";
        });

        _inventories[Container] = new() { Item };
        _properties[Item] = new() { DamageProperty, CustomProperty };

        _engineScope = NWScriptRuntime.Override(_engine);
        _objectScope = ObjectPluginRuntime.Override(_objects);
    }

    [TearDown]
    public void TearDown()
    {
        _objectScope.Dispose();
        _engineScope.Dispose();
    }

    [TestCase(Player)]
    [TestCase(PossessedAvatar)]
    public void Audit_LogsIdentityAndCompleteItemDetailsOnceBeforeAnythingIsDestroyed(uint disposer)
    {
        _service.Audit(disposer, Item, "trash-added");

        var record = _service.Records.Should().ContainSingle().Which;
        record["PlayerId"].Should().Be("id-1", "a possessing DM's avatar is recorded as the DM");
        record["PlayerName"].Should().Be("Canonical Player [A Masked Human]");
        record["IdentityKey"].Should().Be("disguise:mask-id");
        record["AccountName"].Should().Be("Account");
        record["PublicCDKey"].Should().Be("PublicKey");
        record["Source"].Should().Be("trash-added");
        JsonConvert.DeserializeObject<string>((string)record["ItemName"]).Should().Be("Custom \"item\" {name}");
        record["ItemId"].Should().Be("id-20");
        record["ParentItemId"].Should().Be(string.Empty);
        record["Resref"].Should().Be("resref-20");
        JsonConvert.DeserializeObject<string>((string)record["Tag"]).Should().Be("tag-20");
        record["BaseItemType"].Should().Be(42);
        record["Quantity"].Should().Be(7);
        record["Charges"].Should().Be(4);
        ((string)record["PropertyDescriptions"]).Should().Contain("Damage +5").And.NotContain("\n").And.Contain("\\n");
        record["SnapshotComplete"].Should().Be(true);
        record["SerializedItem"].Should().Be("snapshot-20");
        _timeline.Should().Equal("serialize-20", "audit-id-20");
        _locals.Int(Item, "ITEM_TRASH_AUDITED").Should().Be(1, "the item is marked so it is never audited twice");
    }

    [Test]
    public void Audit_IncludesEveryRawPropertyFieldAndUnknownCustomTypes()
    {
        _service.Audit(Player, Item, "trash-added");

        var properties = JArray.Parse((string)_service.Records.Single()["Properties"]);
        properties.Should().HaveCount(2);
        properties[0]["Type"]!.Value<int>().Should().Be(1);
        properties[0]["TypeName"]!.Value<string>().Should().Be("ACBonus");
        properties[1]["Type"]!.Value<int>().Should().Be(1400);
        properties[1]["TypeName"]!.Value<string>().Should().Be("1400");
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
    public void Audit_OfAContainerLogsItsContentsWithTheirParentAndOwnQuantities()
    {
        _inventories[Item] = new() { Child };

        _service.Audit(Player, Item, "trash-close");

        _service.Records.Should().HaveCount(2);
        _service.Records[0]["ParentItemId"].Should().Be(string.Empty);
        _service.Records[1]["ParentItemId"].Should().Be("id-20");
        _service.Records[1]["ItemId"].Should().Be("id-21");
        _service.Records[1]["Quantity"].Should().Be(2);
        _service.Records[1]["Source"].Should().Be("trash-close");
        _timeline.Should().Equal("serialize-20", "audit-id-20", "serialize-21", "audit-id-21");
    }

    [Test]
    public void Audit_OfAnItemAlreadyAudited_DoesNotLogItAgain()
    {
        _service.Audit(Player, Item, "trash-added");
        _service.Audit(Player, Item, "trash-close");

        _service.Records.Should().ContainSingle();
    }

    [Test]
    public void Audit_OfAContainerHoldingItself_VisitsEachItemOnce()
    {
        _inventories[Item] = new() { Item };

        _service.Audit(Player, Item, "trash-added");

        _service.Records.Should().ContainSingle();
    }

    [Test]
    public void Audit_OfAnObjectThatIsNotAValidItem_WritesNothing()
    {
        _service.Audit(Player, 999, "trash-added");
        _service.Audit(Player, Container, "trash-added");
        _service.Audit(Player, ObjectInvalid, "trash-added");

        _service.Records.Should().BeEmpty();
        _objects.CountOf("Serialize").Should().Be(0);
    }

    [Test]
    public void Audit_WhenTheSnapshotFails_KeepsTheRawRecordAndTheContentsAndLogsTheError()
    {
        _failSnapshotOfItem = true;
        _inventories[Item] = new() { Child };

        _service.Audit(Player, Item, "trash-added");

        _service.Errors.Should().ContainSingle().Which["Error"].Should().Be("Serialization failed");
        _service.Records.Should().HaveCount(2);
        _service.Records[0]["SnapshotComplete"].Should().Be(false);
        _service.Records[0]["SerializedItem"].Should().Be(string.Empty);
        JArray.Parse((string)_service.Records[0]["Properties"]).Should().HaveCount(2);
        _service.Records[1]["SnapshotComplete"].Should().Be(true);
    }

    [Test]
    public void Audit_WhenPropertyLabelsAreMissing_KeepsAllRawPropertiesAndTheSnapshot()
    {
        _service.FailDescriptions = true;

        _service.Audit(Player, Item, "trash-added");

        var record = _service.Records.Single();
        JsonConvert.DeserializeObject<string>((string)record["PropertyDescriptions"]).Should().Be("Unavailable (Missing property label); see raw Properties.");
        JArray.Parse((string)record["Properties"]).Should().HaveCount(2);
        record["SnapshotComplete"].Should().Be(true);
    }

    [Test]
    public void Audit_WithoutAKnownDisposer_SaysSoInsteadOfInventingAPlayerIdentity()
    {
        _service.Audit(ObjectInvalid, Item, "trash-added");

        var record = _service.Records.Single();
        record["PlayerId"].Should().Be(string.Empty);
        record["PlayerName"].Should().Be("Unknown player");
        record["IdentityKey"].Should().Be(string.Empty);
        record["AccountName"].Should().Be(string.Empty);
        record["PublicCDKey"].Should().Be(string.Empty);
    }

    [Test]
    public void Audit_WhenWritingTheRecordFails_LogsTheFailureAndNeverStopsTheDestruction()
    {
        _service.FailWrites = true;

        var audit = () => _service.Audit(Player, Item, "trash-added");

        audit.Should().NotThrow();
        var error = _service.Errors.Should().ContainSingle().Which;
        error["PlayerId"].Should().Be("id-1");
        error["ItemId"].Should().Be("id-20");
        error["Source"].Should().Be("trash-added");
        error["Error"].Should().Be("The log sink is down");
    }

    [Test]
    public void Audit_WhenNobodyIsKnownAndWritingFails_LogsAnEmptyPlayerId()
    {
        _service.FailWrites = true;

        _service.Audit(ObjectInvalid, Item, "trash-added");

        _service.Errors.Should().ContainSingle().Which["PlayerId"].Should().Be(string.Empty);
    }

    [Test]
    public void TrashLogger_IsEnabledInEveryServerEnvironment()
    {
        typeof(LogGroup).GetField(nameof(LogGroup.ItemTrash))!.GetCustomAttribute<LogGroupAttribute>()!
            .Environment.Should().Be(ServerEnvironmentType.All);
    }

    [Test]
    public void Audit_WithTheProductionLogCalls_StillMarksAndSnapshotsTheItem()
    {
        // The log sinks, the property text and the player lookups are the original static calls here. No logger is
        // registered in a unit test, so the writes are dropped; the audit itself still runs to the end.
        var production = new ItemTrashService(new DMPresenceService());
        _failSnapshotOfItem = true;

        production.Audit(ObjectInvalid, Item, "trash-added");

        _timeline.Should().Equal("serialize-20");
        _locals.Int(Item, "ITEM_TRASH_AUDITED").Should().Be(1);
    }
    private uint InventoryAt(uint container, int index) =>
        _inventories.TryGetValue(container, out var items) && index < items.Count ? items[index] : ObjectInvalid;

    private ItemProperty PropertyAt(uint item, int index) =>
        EngineStructures.Create<ItemProperty>(
            _properties.TryGetValue(item, out var rows) && index < rows.Count ? rows[index] : 0);

    /// <summary>
    /// The item trash audit with the calls into the services that are not converted yet answered by the test and the
    /// log sinks replaced by lists. A record binds each <c>{Name}</c> of the message template to its value, so a
    /// property the template names without a value (or the other way round) fails the test.
    /// </summary>
    private sealed class AuditRecorder : ItemTrashService
    {
        private readonly List<string> _timeline;

        public AuditRecorder(List<string> timeline)
            : base(new DMPresenceService())
        {
            _timeline = timeline;
        }

        public List<Dictionary<string, object>> Records { get; } = new();
        public List<Dictionary<string, object>> Errors { get; } = new();
        public bool FailDescriptions { get; set; }
        public bool FailWrites { get; set; }

        protected override string GetAuditName(uint player) => "Canonical Player [A Masked Human]";

        protected override string GetIdentityKey(uint player) => "disguise:mask-id";

        protected override string BuildItemPropertyString(uint item) =>
            FailDescriptions ? throw new FormatException("Missing property label") : "Damage +5\nAccuracy +5";

        protected override void WriteStructured(LogGroup group, string messageTemplate, params object[] propertyValues)
        {
            group.Should().Be(LogGroup.ItemTrash);
            if (FailWrites)
                throw new InvalidOperationException("The log sink is down");

            var record = Bind(messageTemplate, propertyValues);
            Records.Add(record);
            _timeline.Add("audit-" + record["ItemId"]);
        }

        protected override void WriteError(Exception exception, string messageTemplate, params object[] propertyValues)
        {
            var record = Bind(messageTemplate, propertyValues);
            record["Error"] = exception.Message;
            Errors.Add(record);
        }

        private static Dictionary<string, object> Bind(string template, object[] values)
        {
            var fields = Regex.Matches(template, @"\{(?<name>[^}]+)\}").Select(match => match.Groups["name"].Value).ToArray();
            fields.Length.Should().Be(values.Length, "every property of the message template needs a value");
            return fields.Select((name, index) => (name, value: values[index])).ToDictionary(row => row.name, row => row.value);
        }
    }
}
