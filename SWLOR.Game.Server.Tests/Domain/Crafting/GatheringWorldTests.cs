using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Tests.Support;
using static SWLOR.NWN.API.NWScript.NWScript;

namespace SWLOR.Game.Server.Tests.Domain.Crafting;

/// <summary>
/// The engine sides of the scavenge sites and the harvest nodes: what they keep on the player record (through the injected
/// database) and on the site or node. The rules that call them are covered by the lifecycle tests.
/// </summary>
[TestFixture]
public class GatheringWorldTests
{
    private const uint Player = 100;
    private const uint Site = 500;
    private const uint ObjectInvalid = 0x7F000000;

    private PlayerEngine _engine = null!;
    private LocalVariables _vars = null!;
    private InMemoryDBService _db = null!;
    private Player _record = null!;

    [SetUp]
    public void SetUp()
    {
        _engine = new PlayerEngine();
        _vars = new LocalVariables(_engine.Engine);
        _engine.Engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! != ObjectInvalid);
        _engine.Engine.SetReturn("GetFirstItemInInventory", ObjectInvalid);
        _engine.Engine.SetReturn("GetNextItemInInventory", ObjectInvalid);
        _db = new InMemoryDBService();
        _record = new Player(PlayerEngine.PlayerId);
        _db.With(_record);
    }

    [TearDown]
    public void TearDown() => _engine.Dispose();

    [Test]
    public void Scavenge_finds_are_saved_on_the_record_grouped_by_item()
    {
        var world = new NwnScavengeWorld(_db);

        world.SavePendingFinds(Player, new[]
        {
            new ScavengeFind("fiberp_ruined", 2, false),
            new ScavengeFind("ruby", 1, false),
            new ScavengeFind("fiberp_ruined", 3, false)
        });

        _record.PendingScavengeFinds.Should().Equal(new Dictionary<string, int> { ["fiberp_ruined"] = 5, ["ruby"] = 1 });
        _db.Saved.Should().ContainSingle();
    }

    [Test]
    public void Scavenge_finds_of_a_player_with_no_record_are_not_saved()
    {
        var world = new NwnScavengeWorld(new InMemoryDBService());

        world.SavePendingFinds(Player, new[] { new ScavengeFind("ruby", 1, false) });
        world.ReturnPendingFinds(Player);

        _engine.Messages().Should().BeEmpty();
    }

    [Test]
    public void Returning_with_nothing_waiting_saves_and_says_nothing()
    {
        var world = new NwnScavengeWorld(_db);

        world.ReturnPendingFinds(Player);

        _db.Saved.Should().BeEmpty();
        _engine.Messages().Should().BeEmpty();
    }

    [Test]
    public void Finds_with_no_room_stay_waiting_and_the_player_is_told()
    {
        _record.ScavengeFindsAwaitingRoom = new Dictionary<string, int> { ["ruby"] = 2 };
        var world = new NwnScavengeWorld(_db);

        world.ReturnPendingFinds(Player);

        _record.ScavengeFindsAwaitingRoom.Should().Equal(new Dictionary<string, int> { ["ruby"] = 2 });
        _engine.Messages().Should().ContainSingle().Which.Should().Contain("no room in your inventory");
    }

    [Test]
    public void A_stamp_with_nothing_to_deliver_is_cleared()
    {
        _record.PendingScavengeReturnSequence = 4;
        var world = new NwnScavengeWorld(_db);

        world.ReturnPendingFinds(Player);

        _record.PendingScavengeReturnSequence.Should().Be(0);
        _db.Saved.Should().ContainSingle();
    }

    [Test]
    public void A_searched_site_is_marked_and_a_held_site_remembers_who_it_is_held_for()
    {
        var world = new NwnScavengeWorld(_db);

        world.MarkSearched(Site);
        world.HoldSite(Player, Site, 3);

        _vars.Int(Site, NwnScavengeWorld.SearchedVariable).Should().Be(1);
        _vars.Object(Site, NwnScavengeWorld.OpeningVariable).Should().Be(Player);
        _vars.Has(Site, NwnScavengeWorld.HolderOpenedVariable).Should().BeFalse();
        _engine.Engine.CallsTo("DelayCommand").Should().BeEmpty("the lapse is delayed inside a command assigned to the module");
    }

    [Test]
    public void A_harvest_node_keeps_its_content_and_forgets_the_old_stock()
    {
        var world = new NwnProspectingWorld(_db);

        world.LoadNode(Site).Should().BeNull();
        SetLocalInt(Site, NwnProspectingWorld.LegacyStockVariable, 6);
        world.LegacyStock(Site).Should().Be(6);

        world.SaveNode(Site, "node-content");

        world.LoadNode(Site).Should().Be("node-content");
        world.LegacyStock(Site).Should().Be(0);
        world.NodeExists(Site).Should().BeTrue();
        world.NodeExists(ObjectInvalid).Should().BeFalse();
    }

    [Test]
    public void Returning_the_harvest_of_a_player_with_nothing_journaled_saves_nothing()
    {
        var world = new NwnProspectingWorld(_db);

        world.ReturnPendingHarvest(Player);
        world.ReturnPendingHarvest(Player);

        _db.Saved.Should().BeEmpty();
        _engine.Messages().Should().BeEmpty();
    }

    [Test]
    public void Harvest_waiting_for_room_that_still_has_no_room_stays_on_the_record()
    {
        _record.HarvestAwaitingRoom = new Dictionary<string, int> { ["ore_a"] = 1 };
        var world = new NwnProspectingWorld(_db);

        world.ReturnPendingHarvest(Player);

        _record.HarvestAwaitingRoom.Should().Equal(new Dictionary<string, int> { ["ore_a"] = 1 });
        _engine.Messages().Should().Contain(NwnProspectingWorld.HarvestHeldMessage);
    }
}
