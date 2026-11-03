using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Items;

/// <summary>
/// Unit tests of overflow storage: the database is an in-memory fake, the engine and the object plugin are recording
/// fakes with real local variables, and the two native reads (does the item fit the character's grid, and saving the
/// character now) are answered by the test.
/// </summary>
[TestFixture]
public class OverflowStorageServiceTests
{
    private const uint ObjectInvalid = 0x7F000000;
    private const uint Player = 1;
    private const uint FirstItem = 101;
    private const uint SecondItem = 102;
    private const uint CarriedItem = 201;
    private const uint EquippedItem = 202;
    private const string PlayerId = "player1";
    private const string MarkVariable = "OVERFLOW_ITEM_ID";

    private FakeNWScript _engine = null!;
    private LocalVariables _locals = null!;
    private FakeObjectPlugin _objects = null!;
    private InMemoryDBService _db = null!;
    private NativeAnswers _service = null!;
    private IDisposable _engineScope = null!;
    private IDisposable _objectScope = null!;

    private readonly Dictionary<string, uint> _restoredFrom = new();
    private readonly HashSet<uint> _acquirable = new();
    private readonly List<uint> _carried = new();
    private readonly Dictionary<int, uint> _equipped = new();
    private int _carriedCursor;

    [SetUp]
    public void SetUp()
    {
        _restoredFrom.Clear();
        _acquirable.Clear();
        _carried.Clear();
        _equipped.Clear();

        _engine = new FakeNWScript();
        _locals = new LocalVariables(_engine);
        _objects = new FakeObjectPlugin();
        _db = new InMemoryDBService();
        _service = new NativeAnswers(_db);

        _engine.SetReturnHandler("GetObjectUUID", arguments => (uint)arguments[0]! == Player ? PlayerId : $"item-{(uint)arguments[0]!}");
        _engine.SetReturnHandler("GetIsObjectValid", arguments => (uint)arguments[0]! is Player or FirstItem or SecondItem or CarriedItem or EquippedItem);
        _engine.SetReturnHandler("GetFirstItemInInventory", arguments => CarriedAt(_carriedCursor = 0));
        _engine.SetReturnHandler("GetNextItemInInventory", arguments => CarriedAt(++_carriedCursor));
        _engine.SetReturnHandler("GetItemInSlot", arguments => _equipped.GetValueOrDefault((int)(InventorySlot)arguments[0]!, ObjectInvalid));

        _objects.SetReturnHandler("Deserialize", arguments => _restoredFrom.GetValueOrDefault((string)arguments[0]!, ObjectInvalid));
        _objects.SetReturnHandler("AcquireItem", arguments => _acquirable.Contains((uint)arguments[1]!));

        _engineScope = NWScriptRuntime.Override(_engine);
        _objectScope = ObjectPluginRuntime.Override(_objects);
    }

    [TearDown]
    public void TearDown()
    {
        _objectScope.Dispose();
        _engineScope.Dispose();
    }

    [Test]
    public void GetItemCount_CountsTheRecordsOfOnePlayer()
    {
        AddRecord("rec1", "data1");
        AddRecord("rec2", "data2");

        _service.GetItemCount(PlayerId).Should().Be(2);

        var query = (DBQuery<OverflowItem>)_db.Queries.Single();
        query.BuildQuery().QueryString.Should().Contain("@PlayerId:" + PlayerId);
    }

    [Test]
    public void Claim_WithNothingWaiting_SaysSo()
    {
        _service.Claim(Player);

        Messages().Should().Equal("You have no items in overflow storage.");
        _objects.CountOf("Deserialize").Should().Be(0);
        _service.Saves.Should().Be(0);
    }

    [Test]
    public void Claim_GivesEveryItemThatFitsSavesTheCharacterThenDeletesTheRecords()
    {
        AddRecord("rec1", "data1", FirstItem, resref: "blaster", quantity: 1);
        AddRecord("rec2", "data2", SecondItem, resref: "armor", quantity: 3);

        _service.Claim(Player);

        _objects.CallsTo("AcquireItem").Select(call => call.Arguments[1]).Should().Equal(FirstItem, SecondItem);
        _service.DeletedWhenSaved.Should().Be(0, "the character is saved before any record is deleted");
        _db.Deleted.Should().BeEquivalentTo("rec1", "rec2");
        _locals.Has(FirstItem, MarkVariable).Should().BeFalse("the mark is cleared once the record is gone");
        _locals.Has(SecondItem, MarkVariable).Should().BeFalse();
        _engine.CountOf("DestroyObject").Should().Be(0);
        Messages().Should().Equal("You claimed 2 item(s) from overflow storage.");
    }

    [Test]
    public void Claim_MarksEachItemWithItsRecordBeforeItIsGiven()
    {
        AddRecord("rec1", "data1", FirstItem);
        _service.Fitting.Add(FirstItem);
        _acquirable.Add(FirstItem);
        var markedWhenAcquired = string.Empty;
        _objects.SetReturnHandler("AcquireItem", arguments =>
        {
            markedWhenAcquired = _locals.String((uint)arguments[1]!, MarkVariable);
            return true;
        });

        _service.Claim(Player);

        markedWhenAcquired.Should().Be("rec1", "a crash after the item is given must leave the mark that finds its record");
    }

    [Test]
    public void Claim_WhenOnlySomeItemsFit_GivesThoseAndKeepsTheRest()
    {
        AddRecord("rec1", "data1", FirstItem);
        AddRecord("rec2", "data2", SecondItem);
        _service.Fitting.Remove(FirstItem);
        _acquirable.Remove(FirstItem);

        _service.Claim(Player);

        _engine.CallsTo("DestroyObject").Select(call => call.Arguments[0]).Should().Equal(FirstItem);
        _db.Deleted.Should().Equal("rec2");
        Messages().Should().Equal(
            "You claimed 1 item(s) from overflow storage. 1 item(s) remain; free inventory space and use /overflow again.");
    }

    [Test]
    public void Claim_WhenTheEngineRefusesAnItemThatFits_DestroysTheCopyAndKeepsTheRecord()
    {
        AddRecord("rec1", "data1", FirstItem);
        _acquirable.Remove(FirstItem);

        _service.Claim(Player);

        _engine.CallsTo("DestroyObject").Select(call => call.Arguments[0]).Should().Equal(FirstItem);
        _db.Deleted.Should().BeEmpty();
        Messages().Single().Should().StartWith("Your inventory is full.");
    }

    [Test]
    public void Claim_WhenNothingFits_TellsThePlayerHowManyItemsAreWaiting()
    {
        AddRecord("rec1", "data1", FirstItem);
        AddRecord("rec2", "data2", SecondItem);
        _service.Fitting.Clear();

        _service.Claim(Player);

        _engine.CallsTo("DestroyObject").Select(call => call.Arguments[0]).Should().Equal(FirstItem, SecondItem);
        _service.Saves.Should().Be(0);
        _db.Deleted.Should().BeEmpty();
        Messages().Should().Equal(
            "Your inventory is full. Free inventory space, then use /overflow again to claim your 2 item(s).");
    }

    [Test]
    public void Claim_SkipsARecordThatCannotBeRestored()
    {
        AddRecord("rec1", "unreadable", item: null);
        AddRecord("rec2", "data2", SecondItem);

        _service.Claim(Player);

        _db.Deleted.Should().Equal("rec2");
        _objects.CallsTo("AcquireItem").Should().ContainSingle();
        Messages().Single().Should().StartWith("You claimed 1 item(s) from overflow storage.");
    }

    [Test]
    public void Claim_WhenTheCharacterCannotBeSaved_DestroysTheCopiesAndKeepsEveryRecord()
    {
        AddRecord("rec1", "data1", FirstItem);
        AddRecord("rec2", "data2", SecondItem);
        _service.SaveSucceeds = false;

        _service.Claim(Player);

        _engine.CallsTo("DestroyObject").Select(call => call.Arguments[0]).Should().Equal(FirstItem, SecondItem);
        _db.Deleted.Should().BeEmpty();
        Messages().Should().Equal("Your character could not be saved, so your items stay in overflow storage. Please try again.");
    }

    [Test]
    public void Claim_FirstDeletesTheRecordsOfItemsTheCharacterAlreadyHolds()
    {
        // A crash between saving the character and deleting the record leaves the item carried (or equipped) with its
        // mark; the next claim deletes that record instead of giving the item again.
        _carried.Add(CarriedItem);
        _equipped[(int)InventorySlot.RightHand] = EquippedItem;
        _engine.SetLocalString(CarriedItem, MarkVariable, "recCarried");
        _engine.SetLocalString(EquippedItem, MarkVariable, "recEquipped");

        _service.Claim(Player);

        _db.Deleted.Should().Equal("recCarried", "recEquipped");
        _locals.Has(CarriedItem, MarkVariable).Should().BeFalse();
        _locals.Has(EquippedItem, MarkVariable).Should().BeFalse();
        Messages().Should().Equal("You have no items in overflow storage.");
    }

    [Test]
    public void NotifyOnLogin_TellsThePlayerHowManyItemsWait()
    {
        AddRecord("rec1", "data1");
        AddRecord("rec2", "data2");

        _service.NotifyOnLogin(Player);

        Messages().Should().ContainSingle().Which
            .Should().Contain("2 item(s) from a character update did not fit in your inventory")
            .And.Contain("/overflow");
    }

    [Test]
    public void NotifyOnLogin_WithNothingWaiting_StaysQuiet()
    {
        _service.NotifyOnLogin(Player);

        Messages().Should().BeEmpty();
    }

    [Test]
    public void NotifyOnLogin_FirstDeletesTheRecordsOfItemsAlreadyHeld()
    {
        _carried.Add(CarriedItem);
        _engine.SetLocalString(CarriedItem, MarkVariable, "recCarried");
        AddRecord("rec1", "data1");

        _service.NotifyOnLogin(Player);

        _db.Deleted.Should().Equal("recCarried");
        _locals.Has(CarriedItem, MarkVariable).Should().BeFalse();
        Messages().Should().ContainSingle();
    }

    private void AddRecord(string id, string data, uint? item = null, string resref = "res", int quantity = 1)
    {
        _db.With(new OverflowItem { Id = id, PlayerId = PlayerId, Data = data, Resref = resref, Quantity = quantity });
        if (item.HasValue)
        {
            _restoredFrom[data] = item.Value;
            _service.Fitting.Add(item.Value);
            _acquirable.Add(item.Value);
        }
    }

    private uint CarriedAt(int index) => index < _carried.Count ? _carried[index] : ObjectInvalid;

    private List<string> Messages() =>
        _engine.CallsTo("SendMessageToPC").Select(call => (string)call.Arguments[1]!).ToList();

    /// <summary>The overflow storage with the two reads of the native engine answered by the test.</summary>
    private sealed class NativeAnswers : OverflowStorageService
    {
        private readonly InMemoryDBService _store;

        public NativeAnswers(InMemoryDBService store)
            : base(store)
        {
            _store = store;
        }

        public HashSet<uint> Fitting { get; } = new();
        public bool SaveSucceeds { get; set; } = true;
        public int Saves { get; private set; }
        public int DeletedWhenSaved { get; private set; } = -1;

        protected override bool FitsInInventory(uint player, uint item) => Fitting.Contains(item);

        protected override bool SaveCharacter(uint player)
        {
            Saves++;
            DeletedWhenSaved = _store.Deleted.Count;
            return SaveSucceeds;
        }
    }
}
