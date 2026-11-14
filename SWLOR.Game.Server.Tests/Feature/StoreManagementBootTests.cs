using System.Collections;
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// The two <c>mod_load</c> handlers that split <c>StoreManagement.ProcessStores</c> (P4-8): <c>LoadStores</c> finds the
/// stores and starts the hourly cleanup (a hot reload reruns it), then <c>ProcessStores</c> marks the store items and
/// raises their prices (boot only). Together they make the calls the single handler made, item by item and in order.
/// The engine is a scripted module of two areas; the store list and the scheduler are the game's statics, read and
/// cleaned up around each test.
/// </summary>
[TestFixture]
[NonParallelizable]
public class StoreManagementBootTests
{
    private const uint ObjectInvalid = 0x7F000000;
    private const uint Cantina = 101;
    private const uint Market = 102;
    private const uint BarStore = 201;
    private const uint Crate = 202;
    private const uint MarketStore = 203;
    private const uint Blaster = 301;
    private const uint Bread = 302;
    private const uint Vibroblade = 303;
    private const string StoreItemFlag = "STORE_SERVICE_IS_STORE_ITEM";

    private FakeNWScript _engine = null!;
    private FakeItemPlugin _items = null!;
    private ItemWorld _world = null!;
    private readonly List<IDisposable> _scopes = new();
    private List<object> _scheduledBefore = null!;

    [SetUp]
    public void SetUp()
    {
        Stores().Clear();
        _scheduledBefore = ScheduledItems().Cast<object>().ToList();

        _engine = new FakeNWScript();
        var world = _world = new ItemWorld(_engine);
        foreach (var obj in new[] { Cantina, Market, BarStore, Crate, MarketStore, Blaster, Bread, Vibroblade })
            world.Add(obj);
        world.Carry(BarStore, Blaster, Bread);
        world.Carry(MarketStore, Vibroblade);
        world.AddProperty(Blaster, ItemPropertyType.IncreasedPrice, costTableValue: 3);
        world.AddProperty(Vibroblade, ItemPropertyType.IncreasedPrice, costTableValue: 1);
        world.AddProperty(Vibroblade, ItemPropertyType.IncreasedPrice, costTableValue: 3);
        world.TwoDA("iprp_incprice", "Cost", 1, "50");
        world.TwoDA("iprp_incprice", "Cost", 3, "100");

        _engine.SetReturnSequence("GetFirstArea", Cantina);
        _engine.SetReturnSequence("GetNextArea", Market, ObjectInvalid);
        var contents = new Dictionary<uint, Queue<uint>>();
        _engine.SetReturnHandler("GetFirstObjectInArea", arguments =>
        {
            var area = (uint)arguments[0]!;
            contents[area] = new Queue<uint>(area == Cantina ? new[] { BarStore, Crate } : new[] { MarketStore });
            return contents[area].Dequeue();
        });
        _engine.SetReturnHandler("GetNextObjectInArea", arguments =>
            contents[(uint)arguments[0]!].TryDequeue(out var next) ? next : ObjectInvalid);
        _engine.SetReturnHandler("GetObjectType", arguments => (uint)arguments[0]! == Crate ? ObjectType.Placeable : ObjectType.Store);

        _items = new FakeItemPlugin();
        _items.SetReturnHandler("GetBaseGoldPieceValue", arguments => (uint)arguments[0]! == Blaster ? 10 : 20);
        _scopes.Add(NWScriptRuntime.Override(_engine));
        _scopes.Add(ItemPluginRuntime.Override(_items));
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var scope in _scopes)
            scope.Dispose();
        _scopes.Clear();
        foreach (var item in ScheduledItems().Cast<object>().Except(_scheduledBefore).ToList())
            ((IDisposable)item).Dispose();
        Stores().Clear();
    }

    [Test]
    public void LoadStores_then_ProcessStores_mark_and_price_every_store_item_as_the_single_handler_did()
    {
        StoreManagement.LoadStores();
        StoreManagement.ProcessStores();

        Stores().Cast<uint>().Should().Equal(BarStore, MarketStore);
        _engine.CallsTo("SetLocalBool").Select(call => (call.Arguments[0], call.Arguments[1], call.Arguments[2]))
            .Should().Equal((Blaster, StoreItemFlag, true), (Bread, StoreItemFlag, true), (Vibroblade, StoreItemFlag, true));
        _items.CallsTo("SetBaseGoldPieceValue").Select(call => (call.Arguments[0], call.Arguments[1]))
            .Should().Equal((Blaster, 110), (Vibroblade, 170));
        NewScheduledItems().Should().ContainSingle("the hourly cleanup is scheduled once");
    }

    [Test]
    public void LoadStores_alone_finds_the_stores_and_starts_the_cleanup_without_touching_an_item()
    {
        StoreManagement.LoadStores();

        Stores().Cast<uint>().Should().Equal(BarStore, MarketStore);
        NewScheduledItems().Should().ContainSingle();
        _engine.CountOf("SetLocalBool").Should().Be(0, "a hot reload reruns this part, so it must not mark the items again");
        _engine.CountOf("GetFirstItemInInventory").Should().Be(0);
        _items.CountOf("SetBaseGoldPieceValue").Should().Be(0, "a second price increase would compound the prices");
    }

    [Test]
    public void A_reload_keeps_the_stores_the_boot_found_and_leaves_out_the_copies_in_area_instances_made_since()
    {
        // An instance of the cantina made after boot (CreateArea copies its template, store included, items unmarked). Whatever
        // resref the engine gives the copy, the carried list keeps it out.
        const uint CantinaCopy = 104;
        const uint CopyStore = 204;
        _world.Add(CantinaCopy);
        _world.Add(CopyStore);
        var areas = new List<uint> { Cantina, Market };
        var cursor = 0;
        // A queued sequence answers before a handler; clear the set-up's two-area one.
        _engine.SetReturnSequence("GetFirstArea");
        _engine.SetReturnSequence("GetNextArea");
        _engine.SetReturnHandler("GetFirstArea", _ =>
        {
            cursor = 0;
            return areas[0];
        });
        _engine.SetReturnHandler("GetNextArea", _ => ++cursor < areas.Count ? areas[cursor] : ObjectInvalid);
        var contents = new Dictionary<uint, Queue<uint>>();
        _engine.SetReturnHandler("GetFirstObjectInArea", arguments =>
        {
            var area = (uint)arguments[0]!;
            contents[area] = new Queue<uint>(area switch
            {
                Cantina => new[] { BarStore, Crate },
                Market => new[] { MarketStore },
                _ => new[] { CopyStore },
            });
            return contents[area].Dequeue();
        });
        _engine.SetReturnHandler("GetNextObjectInArea", arguments =>
            contents[(uint)arguments[0]!].TryDequeue(out var next) ? next : ObjectInvalid);
        var fields = new[] { typeof(StoreManagement).GetField("_stores", BindingFlags.NonPublic | BindingFlags.Static)! };
        var carrier = new ReloadStateCarrier(typeof(StoreManagement).Assembly);

        StoreManagement.LoadStores();
        var carried = carrier.Write(fields, null);

        // The instance appears, then the new build starts with an empty list and reruns LoadStores.
        areas.Add(CantinaCopy);
        Stores().Clear();
        StoreManagement.LoadStores();
        Stores().Cast<uint>().Should().Equal(new[] { BarStore, MarketStore, CopyStore }, "the rerun finds every store in the world");
        carrier.Read(fields, null, carried);

        Stores().Cast<uint>().Should().Equal(new[] { BarStore, MarketStore }, "the hourly clean-up must not empty the instance's store");
        _engine.CountOf("SetLocalBool").Should().Be(0, "the items keep the marks the boot gave them");
        _items.CountOf("SetBaseGoldPieceValue").Should().Be(0, "the prices are never raised again");
        NewScheduledItems().Should().HaveCount(2, "each build schedules the hourly cleanup (the old build's ends with it)");
    }

    private static IList Stores() =>
        (IList)typeof(StoreManagement).GetField("_stores", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;

    private static IList ScheduledItems() =>
        (IList)typeof(Scheduler).GetField("_scheduledItems", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;

    private List<object> NewScheduledItems() => ScheduledItems().Cast<object>().Except(_scheduledBefore).ToList();
}
