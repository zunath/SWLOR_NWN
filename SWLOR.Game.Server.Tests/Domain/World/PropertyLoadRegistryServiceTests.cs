using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Domain.World;

/// <summary>
/// Unit tests of the members the registry gained when it was split from the loader: the failure, spawn action and
/// waiter bookkeeping the queue reaches through the registry, and the state a hot reload carries.
/// </summary>
[TestFixture]
public class PropertyLoadRegistryServiceTests
{
    private const uint Player = 5001;
    private const string PlayerId = "player-uuid-1";

    private PropertyWorld _world = null!;

    private PropertyLoadRegistryService Registry => _world.Loader.Registry;

    [SetUp]
    public void SetUp() => _world = new PropertyWorld();

    [TearDown]
    public void TearDown() => _world.Dispose();

    [Test]
    public void Building_the_registry_touches_neither_the_engine_nor_the_database()
    {
        _world.ClearCalls();
        var db = new InMemoryDBService();

        _ = new PropertyLoadRegistryService(db, _world.Recorded);

        _world.Engine.Calls.Should().BeEmpty();
        db.Reads.Should().BeEmpty();
    }

    [Test]
    public void A_failure_is_kept_until_it_is_cleared()
    {
        Registry.SetPropertyLoadFailure("property-1", "boom");

        Registry.GetPropertyLoadFailure("property-1").Should().Be("boom");

        Registry.ClearPropertyLoadFailure("property-1");

        Registry.GetPropertyLoadFailure("property-1").Should().BeEmpty();
    }

    [Test]
    public void Adopting_an_instance_registers_it_and_marks_its_spawn_action_as_run()
    {
        var area = _world.NewObject();
        var instance = new PropertyInstance(area, PropertyLayoutType.ApartmentSmallStyle1);

        Registry.IsSpawnActionCompleted("property-1").Should().BeFalse();

        Registry.AdoptInstance("property-1", instance);

        Registry.TryGetRegisteredInstance("property-1", out var found).Should().BeTrue();
        found.Should().BeSameAs(instance);
        Registry.IsSpawnActionCompleted("property-1").Should().BeTrue();
    }

    [Test]
    public void A_registered_instance_is_found_even_when_its_area_is_no_longer_valid()
    {
        var area = _world.NewObject();
        Registry.RegisterInstance("property-1", area, PropertyLayoutType.ApartmentSmallStyle1);
        Registry.SetPropertyLoadState("property-1", PropertyLoadState.Loaded);
        _world.Invalidate(area);

        Registry.TryGetRegisteredInstance("property-1", out _).Should().BeTrue();
        Registry.TryGetLoadedInstance("property-1", out _).Should().BeFalse();
    }

    [Test]
    public void Waiting_players_are_counted_and_released_when_the_property_is_announced()
    {
        _world.AddPlayer(Player, PlayerId);

        Registry.AddPropertyLoadWaiter(Player, "property-1");

        Registry.GetWaiterCount("property-1").Should().Be(1);

        Registry.NotifyPropertyLoadWaiters("property-1", loaded: false);

        Registry.GetWaiterCount("property-1").Should().Be(0);
        _world.MessagesTo(Player).Should().ContainSingle().Which.Should().Contain("could not be loaded");
    }

    [Test]
    public void Forgetting_a_property_drops_everything_the_registry_holds_for_it()
    {
        var area = _world.NewObject();
        var placeable = _world.NewObject();
        _world.AddPlayer(Player, PlayerId);
        Registry.RegisterInstance("property-1", area, PropertyLayoutType.ApartmentSmallStyle1);
        Registry.RegisterStructurePlaceable("property-1", placeable);
        Registry.SetPropertyLoadState("property-1", PropertyLoadState.Failed);
        Registry.SetPropertyLoadFailure("property-1", "boom");
        Registry.MarkSpawnActionCompleted("property-1");
        Registry.AddPropertyLoadWaiter(Player, "property-1");

        Registry.ForgetProperty("property-1");

        Registry.TryGetRegisteredInstance("property-1", out _).Should().BeFalse();
        Registry.TryGetStructurePlaceable("property-1", out _).Should().BeFalse();
        Registry.GetPropertyLoadFailure("property-1").Should().BeEmpty();
        Registry.IsSpawnActionCompleted("property-1").Should().BeFalse();
        Registry.GetWaiterCount("property-1").Should().Be(0);
    }

    [Test]
    public void Resetting_the_bookkeeping_keeps_the_instances_and_placeables_the_world_still_holds()
    {
        var area = _world.NewObject();
        var placeable = _world.NewObject();
        _world.AddPlayer(Player, PlayerId);
        Registry.RegisterInstance("property-1", area, PropertyLayoutType.ApartmentSmallStyle1);
        Registry.RegisterStructurePlaceable("structure-1", placeable);
        Registry.SetPropertyLoadFailure("property-1", "boom");
        Registry.MarkSpawnActionCompleted("property-1");
        Registry.AddPropertyLoadWaiter(Player, "property-1");

        Registry.ResetLoadBookkeeping();

        Registry.GetPropertyLoadFailure("property-1").Should().BeEmpty();
        Registry.IsSpawnActionCompleted("property-1").Should().BeFalse();
        Registry.GetWaiterCount("property-1").Should().Be(0);
        Registry.TryGetRegisteredInstance("property-1", out _).Should().BeTrue();
        Registry.TryGetStructurePlaceable("structure-1", out var kept).Should().BeTrue();
        kept.Should().Be(placeable);
    }

    [Test]
    public void The_carried_state_restores_the_states_failures_and_waiters_in_a_new_registry()
    {
        _world.AddPlayer(Player, PlayerId);
        Registry.SetPropertyLoadState("property-1", PropertyLoadState.Queued);
        Registry.SetPropertyLoadState("property-2", PropertyLoadState.Failed);
        Registry.SetPropertyLoadFailure("property-2", "boom");
        Registry.AddPropertyLoadWaiter(Player, "property-1");

        var carry = Registry.CarryState();
        var fresh = new PropertyLoadRegistryService(_world.Db, _world.Recorded);
        fresh.RestoreState(carry);

        carry.Properties().Select(property => property.Name).Should().Equal("states", "failures", "waiters");
        fresh.GetPropertyLoadFailure("property-2").Should().Be("boom");
        fresh.GetWaiterCount("property-1").Should().Be(1);
        fresh.GetPropertyLoadState("property-2").Should().Be(PropertyLoadState.Failed);
        fresh.GetPropertyLoadState("property-1").Should().Be(PropertyLoadState.Queued);
    }
}
