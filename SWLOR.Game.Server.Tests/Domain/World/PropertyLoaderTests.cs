using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Domain.World;

/// <summary>
/// Unit tests of the instance loader: the load states, the queue that loads instanced properties a few at a time,
/// the players waiting for them, the startup load, and spawning a property into the game world. The scheduler is the
/// world's list of batches, run by hand, so a test sees exactly what one batch does.
/// </summary>
[TestFixture]
public class PropertyLoaderTests
{
    private const uint Player = 5001;
    private const string PlayerId = "player-uuid-1";
    private const uint OtherPlayer = 5002;
    private const string OtherPlayerId = "player-uuid-2";
    private const uint CityArea = 8000;
    private const string CityAreaResref = "city_area";

    private PropertyWorld _world = null!;

    private PropertyWorld.LoaderUnderTest Loader => _world.Loader;

    private QueryableDBService Db => _world.Db;

    [SetUp]
    public void SetUp() => _world = new PropertyWorld();

    [TearDown]
    public void TearDown() => _world.Dispose();

    private WorldProperty StoreStructure(string parentId, StructureType type = StructureType.Jukebox)
    {
        var structure = new WorldProperty
        {
            CustomName = "Structure",
            PropertyType = PropertyType.Structure,
            StructureType = type,
            OwnerPlayerId = PropertyWorld.OwnerId,
            ParentPropertyId = parentId
        };
        structure.Positions[PropertyLocationType.StaticPosition] = new PropertyLocation { AreaResref = "somewhere", X = 1, Y = 2, Z = 3, Orientation = 90 };
        Db.With(structure);
        return structure;
    }

    private WorldProperty StoreInstanced(PropertyType type, PropertyLayoutType layout, bool isPublic = false, string parentId = "")
    {
        var property = new WorldProperty
        {
            CustomName = type.ToString(),
            PropertyType = type,
            OwnerPlayerId = PropertyWorld.OwnerId,
            Layout = layout,
            IsPubliclyAccessible = isPublic,
            ParentPropertyId = parentId
        };
        Db.With(property);
        return property;
    }

    /// <summary>A player asks to enter a property that is not ready; the load is queued.</summary>
    private void RequestEntry(WorldProperty property, uint player = Player)
    {
        Loader.TryResolveEnterableInstance(player, property.Id, out _).Should().BeFalse();
    }

    private string Area(PropertyLayoutType layout) => _world.Catalog.GetLayoutByType(layout).AreaInstanceResref;

    // ---- construction and the scheduler

    [Test]
    public void Building_the_loader_touches_neither_the_engine_nor_the_database()
    {
        _world.ClearCalls();
        var db = new InMemoryDBService();

        _ = new PropertyLoader(db, _world.Areas, _world.Recorded);

        _world.Engine.Calls.Should().BeEmpty();
        db.Reads.Should().BeEmpty();
    }

    [Test]
    public void Batches_are_scheduled_two_tenths_of_a_second_apart_and_the_scheduler_runs_them()
    {
        var apartment = _world.StoreApartment();
        _world.AddPlayer(Player, PlayerId);

        RequestEntry(apartment);

        Loader.Scheduled.Should().ContainSingle().Which.Delay.Should().Be(TimeSpan.FromSeconds(0.2d));

        var real = new RealSchedulerLoader(Db, _world.Areas, _world.Recorded);
        var ran = false;
        real.Schedule(() => ran = true);
        Scheduler.Process();
        ran.Should().BeTrue("the default seam hands the batch to the static scheduler");
    }

    private sealed class RealSchedulerLoader : PropertyLoader
    {
        public RealSchedulerLoader(IDBService db, IAreaService areas, IPropertyCatalog catalog)
            : base(db, areas, catalog)
        {
        }

        public void Schedule(Action task) => ScheduleLoadBatch(task, TimeSpan.Zero);
    }

    // ---- the property id of an object and the registry of instances

    [Test]
    public void A_property_id_is_a_local_variable_of_the_object()
    {
        Loader.GetPropertyId(77).Should().BeEmpty();

        Loader.AssignPropertyId(77, "property-1");

        Loader.GetPropertyId(77).Should().Be("property-1");
        _world.Locals.String(77, "PROPERTY_ID").Should().Be("property-1");
    }

    [Test]
    public void Registering_an_instance_marks_its_area_with_the_property_id()
    {
        var area = _world.NewObject();

        Loader.RegisterInstance("property-1", area, PropertyLayoutType.ApartmentSmallStyle1);
        Loader.SetPropertyLoadState("property-1", PropertyLoadState.Loaded);

        Loader.GetPropertyId(area).Should().Be("property-1");
        Loader.TryGetLoadedInstance("property-1", out var instance).Should().BeTrue();
        instance.Area.Should().Be(area);
        instance.LayoutType.Should().Be(PropertyLayoutType.ApartmentSmallStyle1);
    }

    [Test]
    public void A_blank_property_id_has_no_load_state_to_set_and_has_failed()
    {
        Loader.SetPropertyLoadState(" ", PropertyLoadState.Loaded);
        Loader.SetPropertyLoadState(string.Empty, PropertyLoadState.Loaded);

        Loader.GetPropertyLoadState(string.Empty).Should().Be(PropertyLoadState.Failed);
        Loader.GetPropertyLoadState(null!).Should().Be(PropertyLoadState.Failed);
        Loader.GetPropertyLoadState(" ").Should().Be(PropertyLoadState.Failed);
    }

    // ---- the load state

    [Test]
    public void A_property_that_nothing_is_known_about_is_judged_from_the_database()
    {
        var apartment = _world.StoreApartment();
        var structure = StoreStructure("somewhere");

        Loader.GetPropertyLoadState("not-stored").Should().Be(PropertyLoadState.Failed);
        Loader.GetPropertyLoadState(apartment.Id).Should().Be(PropertyLoadState.Unloaded, "an instance has to be loaded");
        Loader.GetPropertyLoadState(structure.Id).Should().Be(PropertyLoadState.Loaded, "a world structure needs no load");
    }

    [Test]
    public void A_registered_instance_with_a_valid_area_is_loaded()
    {
        Loader.RegisterInstance("with-area", _world.NewObject(), PropertyLayoutType.ApartmentSmallStyle1);

        Loader.GetPropertyLoadState("with-area").Should().Be(PropertyLoadState.Loaded);
    }

    [Test]
    public void A_registered_instance_with_a_lost_area_is_forgotten_and_unloaded()
    {
        var area = _world.NewObject();
        Loader.RegisterInstance("lost-area", area, PropertyLayoutType.ApartmentSmallStyle1);
        _world.Invalidate(area);

        Loader.GetPropertyLoadState("lost-area").Should().Be(PropertyLoadState.Unloaded);
        Loader.GetPropertyLoadState("lost-area").Should().Be(PropertyLoadState.Failed, "once forgotten, the database is asked and has no such property");
    }

    [Test]
    public void A_state_other_than_loaded_is_answered_as_it_was_set()
    {
        foreach (var state in new[] { PropertyLoadState.Queued, PropertyLoadState.Loading, PropertyLoadState.Failed, PropertyLoadState.Unloaded })
        {
            Loader.SetPropertyLoadState("property-" + state, state);

            Loader.GetPropertyLoadState("property-" + state).Should().Be(state);
        }
    }

    [Test]
    public void A_loaded_state_stands_while_the_instance_area_is_valid()
    {
        var apartment = _world.StoreApartment();
        var area = _world.NewObject();
        Loader.RegisterInstance(apartment.Id, area, apartment.Layout);
        Loader.SetPropertyLoadState(apartment.Id, PropertyLoadState.Loaded);

        Loader.GetPropertyLoadState(apartment.Id).Should().Be(PropertyLoadState.Loaded);
        Db.Store.Reads.Should().BeEmpty("a valid area settles it without asking the database");
    }

    [Test]
    public void A_loaded_state_whose_area_is_gone_is_unloaded_when_the_property_is_an_instance()
    {
        var apartment = _world.StoreApartment();
        var area = _world.NewObject();
        Loader.RegisterInstance(apartment.Id, area, apartment.Layout);
        Loader.SetPropertyLoadState(apartment.Id, PropertyLoadState.Loaded);
        _world.Invalidate(area);

        Loader.GetPropertyLoadState(apartment.Id).Should().Be(PropertyLoadState.Unloaded);
        Loader.TryGetLoadedInstance(apartment.Id, out _).Should().BeFalse();
        Loader.GetPropertyLoadFailure(apartment.Id).Should().BeEmpty();
    }

    [Test]
    public void A_loaded_state_whose_property_is_gone_from_the_database_is_a_failure()
    {
        var area = _world.NewObject();
        Loader.RegisterInstance("gone", area, PropertyLayoutType.ApartmentSmallStyle1);
        Loader.SetPropertyLoadState("gone", PropertyLoadState.Loaded);
        _world.Invalidate(area);

        Loader.GetPropertyLoadState("gone").Should().Be(PropertyLoadState.Failed);
        Loader.GetPropertyLoadFailure("gone").Should().Be("Property does not exist in the database.");
        Loader.GetPropertyLoadFailure("never-seen").Should().BeEmpty();
    }

    [Test]
    public void A_loaded_state_of_a_property_that_is_not_an_instance_stands()
    {
        var structure = StoreStructure("somewhere");
        Loader.SetPropertyLoadState(structure.Id, PropertyLoadState.Loaded);

        Loader.GetPropertyLoadState(structure.Id).Should().Be(PropertyLoadState.Loaded);
    }

    [Test]
    public void Only_a_loaded_state_with_a_registered_valid_instance_gives_an_instance()
    {
        var apartment = _world.StoreApartment();
        Loader.SetPropertyLoadState(apartment.Id, PropertyLoadState.Queued);

        Loader.TryGetLoadedInstance(apartment.Id, out var instance).Should().BeFalse();
        instance.Should().BeNull();
        Loader.TryGetLoadedInstance("not-stored", out _).Should().BeFalse();
    }

    // ---- on demand and startup

    [Test]
    public void Private_instances_load_on_demand_and_public_ones_at_startup()
    {
        var apartment = _world.StoreApartment();
        var privateHouse = StoreInstanced(PropertyType.House, PropertyLayoutType.SmallHouseStyle1, isPublic: false);
        var publicHouse = StoreInstanced(PropertyType.House, PropertyLayoutType.SmallHouseStyle1, isPublic: true);
        var bank = StoreInstanced(PropertyType.Bank, PropertyLayoutType.BankStyle1);
        var structure = StoreStructure("somewhere");

        Loader.IsPropertyOnDemand(null!).Should().BeFalse();
        Loader.IsPropertyOnDemand(apartment).Should().BeTrue("an apartment is always private");
        Loader.IsPropertyOnDemand(privateHouse).Should().BeTrue("an adjustable property that was not made public");
        Loader.IsPropertyOnDemand(publicHouse).Should().BeFalse();
        Loader.IsPropertyOnDemand(bank).Should().BeFalse("a bank loads at startup");
        Loader.IsPropertyOnDemand(structure).Should().BeFalse("only instances load on demand");
    }

    // ---- entering a property

    [Test]
    public void A_loaded_instance_can_be_entered_at_once()
    {
        var apartment = _world.StoreApartment();
        var area = _world.NewObject();
        Loader.RegisterInstance(apartment.Id, area, apartment.Layout);
        Loader.SetPropertyLoadState(apartment.Id, PropertyLoadState.Loaded);

        Loader.TryResolveEnterableInstance(Player, apartment.Id, out var instance).Should().BeTrue();

        instance.Area.Should().Be(area);
        _world.Engine.CallsTo("SendMessageToPC").Should().BeEmpty();
        Loader.Scheduled.Should().BeEmpty();
    }

    [Test]
    public void Entering_a_property_that_failed_or_is_not_an_instance_is_refused()
    {
        _world.AddPlayer(Player, PlayerId);
        var apartment = _world.StoreApartment();
        var structure = StoreStructure("somewhere");
        Loader.SetPropertyLoadState(apartment.Id, PropertyLoadState.Failed);
        Loader.SetPropertyLoadState("not-stored", PropertyLoadState.Unloaded);

        Loader.TryResolveEnterableInstance(Player, apartment.Id, out _).Should().BeFalse("it failed to load");
        Loader.TryResolveEnterableInstance(Player, "not-stored", out _).Should().BeFalse("the database has no such property");
        Loader.TryResolveEnterableInstance(Player, structure.Id, out _).Should().BeFalse("a structure has no interior to enter");

        _world.MessagesTo(Player).Should().Equal(Enumerable.Repeat("This property could not be loaded. Please notify staff.", 3));
        Loader.Scheduled.Should().BeEmpty();
    }

    [Test]
    public void Entering_an_unloaded_instance_queues_its_load_and_makes_the_player_wait()
    {
        _world.AddPlayer(Player, PlayerId);
        var apartment = _world.StoreApartment();

        RequestEntry(apartment);

        _world.MessagesTo(Player).Should().Equal("This property is still loading. Please try again shortly.");
        _world.Engine.CallsTo("FloatingTextStringOnCreature").Should().BeEmpty();
        Loader.GetPropertyLoadState(apartment.Id).Should().Be(PropertyLoadState.Queued);
        Loader.Scheduled.Should().ContainSingle();
        Loader.GetPropertyLoadDiagnostics().Single(x => x.PropertyId == apartment.Id).QueuePriority.Should().Be("PlayerRequest");
    }

    [Test]
    public void A_second_request_for_a_queued_load_promotes_it_instead_of_queueing_it_again()
    {
        _world.AddPlayer(Player, PlayerId);
        var apartment = _world.StoreApartment();
        // A staff retry queued the load; the player's request comes second and must not lower its priority.
        Loader.RetryPropertyLoad(apartment.Id).Should().BeTrue();
        Loader.GetPropertyLoadDiagnostics().Single().QueuePriority.Should().Be("StaffRequest");

        RequestEntry(apartment);

        var diagnostic = Loader.GetPropertyLoadDiagnostics().Single();
        diagnostic.QueuePriority.Should().Be("StaffRequest", "a request never lowers the priority");
        diagnostic.WaiterCount.Should().Be(1);
        Loader.Scheduled.Should().ContainSingle("the processor is already scheduled");
    }

    [Test]
    public void A_dm_does_not_wait_for_a_load()
    {
        var dm = _world.AddDm(6001);
        var apartment = _world.StoreApartment();

        RequestEntry(apartment, dm);

        Loader.GetPropertyLoadDiagnostics().Single().WaiterCount.Should().Be(0);
        _world.MessagesTo(dm).Should().Equal("This property is still loading. Please try again shortly.");
    }

    // ---- the load queue

    [Test]
    public void A_queued_instance_is_created_in_one_batch_and_its_structures_are_spawned_in_the_next()
    {
        _world.AddPlayer(Player, PlayerId);
        var apartment = _world.StoreApartment();
        var structure = StoreStructure(apartment.Id);
        var spawnActions = new List<uint>();
        _world.Catalog.GetLayoutByType(apartment.Layout).OnSpawnAction = spawned => spawnActions.Add(spawned);
        RequestEntry(apartment);
        _world.ClearCalls();

        Loader.RunNextBatch().Should().BeTrue();

        Loader.GetPropertyLoadState(apartment.Id).Should().Be(PropertyLoadState.Loading);
        var area = _world.Areas.Instances.Should().ContainSingle().Subject;
        _world.Areas.Created.Should().Equal(Area(apartment.Layout));
        spawnActions.Should().Equal(area);
        Loader.GetPropertyId(area).Should().Be(apartment.Id);
        _world.Engine.CallsTo("SetName").Should().ContainSingle().Which.Arguments.Should().Equal(area, "{PC} Apartment");
        _world.Engine.CallsTo("CreateObject").Should().BeEmpty("the structures wait for the next batch");
        Loader.Scheduled.Should().ContainSingle("work remains");

        Loader.RunNextBatch().Should().BeTrue();

        Loader.GetPropertyLoadState(apartment.Id).Should().Be(PropertyLoadState.Loaded);
        Loader.GetPlaceableByPropertyId(structure.Id).Should().NotBe(PropertyWorld.Invalid);
        _world.Recorded.Events.Should().ContainSingle().Which.Should().Match<(StructureType Structure, StructureChangeType Change, WorldProperty Property, uint Placeable)>(
            e => e.Structure == StructureType.Jukebox && e.Change == StructureChangeType.PositionChanged && e.Property.Id == structure.Id);
        _world.MessagesTo(Player).Should().Contain("Apartment is ready. Please try entering again.");
        Loader.Scheduled.Should().BeEmpty("the queue is empty");
        Loader.TryResolveEnterableInstance(Player, apartment.Id, out var instance).Should().BeTrue();
        instance.Area.Should().Be(area);
    }

    [Test]
    public void The_spawn_action_runs_again_for_a_rebuilt_area()
    {
        var apartment = _world.StoreApartment();
        var calls = 0;
        _world.Catalog.GetLayoutByType(apartment.Layout).OnSpawnAction = _ => calls++;
        RequestEntry(apartment);
        Loader.RunAllBatches();
        calls.Should().Be(1);

        // The area is lost and the property is asked for again: the lookup forgets the old area and its completed marker.
        _world.Invalidate(_world.Areas.Instances.Single());
        Loader.GetPropertyLoadState(apartment.Id).Should().Be(PropertyLoadState.Unloaded);
        RequestEntry(apartment);
        Loader.RunAllBatches();

        calls.Should().Be(2, "a new area needs the action again");
        _world.Areas.Created.Should().HaveCount(2);
    }

    [Test]
    public void Structures_are_spawned_five_to_a_batch()
    {
        var apartment = _world.StoreApartment();
        for (var count = 0; count < 7; count++)
            StoreStructure(apartment.Id);
        RequestEntry(apartment);

        var batches = Loader.RunAllBatches();

        batches.Should().Be(3, "one to create the area, then five and two structures");
        _world.Engine.CountOf("CreateObject").Should().Be(7);
        Loader.GetPropertyLoadState(apartment.Id).Should().Be(PropertyLoadState.Loaded);
    }

    [Test]
    public void The_queue_serves_the_highest_priority_first()
    {
        var requestedByPlayer = _world.StoreApartment(PropertyLayoutType.ApartmentSmallStyle1);
        var retriedByStaff = _world.StoreApartment(PropertyLayoutType.ApartmentMediumStyle1);
        _world.AddPlayer(Player, PlayerId);
        RequestEntry(requestedByPlayer);
        Loader.RetryPropertyLoad(retriedByStaff.Id).Should().BeTrue();

        Loader.RunNextBatch();

        // A staff retry outranks a player request.
        _world.Areas.Created.Should().Equal(new List<string> { Area(retriedByStaff.Layout) });
        Loader.GetPropertyLoadState(requestedByPlayer.Id).Should().Be(PropertyLoadState.Queued);
    }

    [Test]
    public void A_property_that_cannot_get_an_area_fails_with_the_context_staff_need()
    {
        _world.AddPlayer(Player, PlayerId);
        var apartment = _world.StoreApartment();
        _world.Areas.FailCreation = true;
        RequestEntry(apartment);

        Loader.RunAllBatches();

        Loader.GetPropertyLoadState(apartment.Id).Should().Be(PropertyLoadState.Failed);
        var failure = Loader.GetPropertyLoadFailure(apartment.Id);
        failure.Should().StartWith($"PropertyId: {apartment.Id}; Type: Apartment; Layout: ApartmentSmallStyle1; Phase: CreateArea; StructureId: ; Error: ");
        failure.Should().Contain("Unable to create property area from resref 'apartment_002'.");
        _world.MessagesTo(Player).Should().Contain("Apartment could not be loaded. Please notify staff.");
        Loader.Scheduled.Should().BeEmpty();
    }

    [Test]
    public void A_structure_that_cannot_be_spawned_fails_the_load_and_names_the_structure()
    {
        var apartment = _world.StoreApartment();
        var good = StoreStructure(apartment.Id);
        var bad = StoreStructure(apartment.Id, StructureType.Invalid);
        RequestEntry(apartment);

        Loader.RunAllBatches();

        Loader.GetPropertyLoadState(apartment.Id).Should().Be(PropertyLoadState.Failed);
        var failure = Loader.GetPropertyLoadFailure(apartment.Id);
        failure.Should().Contain("Phase: SpawnStructures");
        failure.Should().ContainAny($"StructureId: {bad.Id};", $"StructureId: {good.Id};");
        failure.Should().Contain("Error: ");
    }

    [Test]
    public void A_property_deleted_while_it_waits_in_the_queue_fails_the_load()
    {
        var apartment = _world.StoreApartment();
        RequestEntry(apartment);
        Db.Delete<WorldProperty>(apartment.Id);

        Loader.RunAllBatches();

        Loader.GetPropertyLoadState(apartment.Id).Should().Be(PropertyLoadState.Failed);
        Loader.GetPropertyLoadFailure(apartment.Id).Should().Contain("Property does not exist in the database.").And.Contain("Type: Unknown; Layout: Unknown");
    }

    [Test]
    public void A_load_that_finds_its_area_already_registered_reuses_it()
    {
        var apartment = _world.StoreApartment();
        var area = _world.NewObject();
        Loader.RegisterInstance(apartment.Id, area, apartment.Layout);
        Loader.SetPropertyLoadState(apartment.Id, PropertyLoadState.Unloaded);
        Loader.RetryPropertyLoad(apartment.Id);

        Loader.RunAllBatches();

        _world.Areas.Created.Should().BeEmpty();
        Loader.TryGetLoadedInstance(apartment.Id, out var instance).Should().BeTrue();
        instance.Area.Should().Be(area);
    }

    // ---- staff repairs

    [Test]
    public void Retrying_a_property_that_does_not_exist_or_is_not_an_instance_does_nothing()
    {
        var structure = StoreStructure("somewhere");

        Loader.RetryPropertyLoad("not-stored").Should().BeFalse();
        Loader.RetryPropertyLoad(structure.Id).Should().BeFalse();
        Loader.Scheduled.Should().BeEmpty();
    }

    [Test]
    public void Retrying_a_load_in_progress_is_refused_and_a_queued_one_is_promoted()
    {
        var loading = _world.StoreApartment();
        var queued = _world.StoreApartment();
        Loader.SetPropertyLoadState(loading.Id, PropertyLoadState.Loading);
        RequestEntry(queued);
        _world.AddPlayer(Player, PlayerId);

        Loader.RetryPropertyLoad(loading.Id).Should().BeFalse();
        Loader.RetryPropertyLoad(queued.Id).Should().BeTrue();

        Loader.GetPropertyLoadDiagnostics().Single(x => x.PropertyId == queued.Id).QueuePriority.Should().Be("StaffRequest");
    }

    [Test]
    public void Retrying_a_failed_load_forgets_the_failure_and_queues_it_again()
    {
        var apartment = _world.StoreApartment();
        _world.Areas.FailCreation = true;
        RequestEntry(apartment);
        Loader.RunAllBatches();
        Loader.GetPropertyLoadState(apartment.Id).Should().Be(PropertyLoadState.Failed);
        _world.Areas.FailCreation = false;

        Loader.RetryPropertyLoad(apartment.Id).Should().BeTrue();

        Loader.GetPropertyLoadFailure(apartment.Id).Should().BeEmpty();
        Loader.GetPropertyLoadState(apartment.Id).Should().Be(PropertyLoadState.Queued);
        Loader.RunAllBatches();
        Loader.GetPropertyLoadState(apartment.Id).Should().Be(PropertyLoadState.Loaded);
    }

    [Test]
    public void A_failed_load_is_not_queued_again_unless_staff_ask()
    {
        var apartment = _world.StoreApartment();
        _world.Areas.FailCreation = true;
        RequestEntry(apartment);
        Loader.RunAllBatches();

        RequestEntry(apartment);

        Loader.GetPropertyLoadState(apartment.Id).Should().Be(PropertyLoadState.Failed);
        Loader.Scheduled.Should().BeEmpty();
    }

    [Test]
    public void Retrying_a_loaded_interior_requeues_the_exterior_structure_of_its_city()
    {
        var (_, structure, interior) = _world.StoreCityWithHall(CityAreaResref);
        _world.AddArea(CityArea, CityAreaResref);
        Loader.SetPropertyLoadState(interior.Id, PropertyLoadState.Loaded);
        var area = _world.NewObject();
        Loader.RegisterInstance(interior.Id, area, interior.Layout);

        Loader.RetryPropertyLoad(interior.Id).Should().BeTrue();
        Loader.RunAllBatches();

        Loader.GetPlaceableByPropertyId(structure.Id).Should().NotBe(PropertyWorld.Invalid, "the city hall building was spawned again");
    }

    [Test]
    public void Retrying_a_loaded_instance_that_has_no_exterior_structure_requeues_nothing()
    {
        var apartment = _world.StoreApartment();
        Loader.RegisterInstance(apartment.Id, _world.NewObject(), apartment.Layout);
        Loader.SetPropertyLoadState(apartment.Id, PropertyLoadState.Loaded);

        Loader.RetryPropertyLoad(apartment.Id).Should().BeFalse();

        Loader.Scheduled.Should().BeEmpty();
    }

    [Test]
    public void A_queued_load_can_be_aborted_before_it_starts()
    {
        _world.AddPlayer(Player, PlayerId);
        var apartment = _world.StoreApartment();
        RequestEntry(apartment);

        Loader.AbortQueuedPropertyLoad(apartment.Id).Should().BeTrue();

        Loader.GetPropertyLoadState(apartment.Id).Should().Be(PropertyLoadState.Failed);
        Loader.GetPropertyLoadFailure(apartment.Id).Should().Be("Load aborted by staff before it started.");
        _world.MessagesTo(Player).Should().Contain("Apartment could not be loaded. Please notify staff.");
        Loader.GetPropertyLoadDiagnostics().Single().IsQueued.Should().BeFalse();
        Loader.RunAllBatches();
        _world.Areas.Created.Should().BeEmpty("the job is gone");
    }

    [Test]
    public void A_load_that_is_not_queued_or_is_already_running_cannot_be_aborted()
    {
        var apartment = _world.StoreApartment();
        var running = _world.StoreApartment();
        RequestEntry(running);
        Loader.RunNextBatch();

        Loader.AbortQueuedPropertyLoad(apartment.Id).Should().BeFalse("nothing is queued");
        Loader.AbortQueuedPropertyLoad(running.Id).Should().BeFalse("it has started");
    }

    [Test]
    public void Staff_can_tell_the_waiting_players_how_a_load_ended_or_that_it_still_runs()
    {
        _world.AddPlayer(Player, PlayerId);
        _world.AddPlayer(OtherPlayer, OtherPlayerId);
        var apartment = _world.StoreApartment();
        RequestEntry(apartment);
        _world.ClearCalls();

        Loader.NotifyPropertyLoadWaitersForStaff(apartment.Id);
        Loader.NotifyPropertyLoadWaitersForStaff(apartment.Id);

        _world.MessagesTo(Player).Should().Equal(Enumerable.Repeat("This property is still loading. Please try again shortly.", 2),
            "a load in progress keeps its waiters, so the second call reaches them too");
        Loader.GetPropertyLoadDiagnostics().Single().WaiterCount.Should().Be(1);

        Loader.RunAllBatches();
        _world.ClearCalls();
        Loader.NotifyPropertyLoadWaitersForStaff(apartment.Id);

        _world.MessagesTo(Player).Should().BeEmpty("the waiters were told when the load finished and are forgotten");
    }

    [Test]
    public void Waiters_of_a_failed_load_are_told_it_failed()
    {
        _world.AddPlayer(Player, PlayerId);
        var apartment = _world.StoreApartment();
        RequestEntry(apartment);
        Loader.SetPropertyLoadState(apartment.Id, PropertyLoadState.Failed);

        Loader.NotifyPropertyLoadWaitersForStaff(apartment.Id);

        _world.MessagesTo(Player).Should().Contain("Apartment could not be loaded. Please notify staff.");
        Loader.GetPropertyLoadDiagnostics().Single().WaiterCount.Should().Be(0);
    }

    [Test]
    public void Notifying_the_waiters_of_a_property_nobody_waits_for_does_nothing()
    {
        var apartment = _world.StoreApartment();
        _world.ClearCalls();

        Loader.NotifyPropertyLoadWaitersForStaff(apartment.Id);
        Loader.NotifyPropertyLoadWaitersForStaff("not-stored");

        _world.Engine.CallsTo("SendMessageToPC").Should().BeEmpty();
    }

    // ---- diagnostics

    [Test]
    public void Diagnostics_list_the_instanced_properties_worst_first_with_their_operational_fields()
    {
        _world.AddPlayer(Player, PlayerId);
        var failed = StoreInstanced(PropertyType.Apartment, PropertyLayoutType.ApartmentSmallStyle1);
        failed.CustomName = "Failed flat";
        var queued = _world.StoreApartment();
        queued.CustomName = "Queued flat";
        var unloaded = _world.StoreApartment();
        unloaded.CustomName = "Idle flat";
        var loaded = StoreInstanced(PropertyType.Bank, PropertyLayoutType.BankStyle1);
        StoreStructure(loaded.Id);
        StoreStructure(loaded.Id);
        StoreStructure("elsewhere");
        Loader.RegisterInstance(loaded.Id, _world.NewObject(), loaded.Layout);
        Loader.SetPropertyLoadState(loaded.Id, PropertyLoadState.Loaded);
        Loader.SetPropertyLoadState(failed.Id, PropertyLoadState.Failed);
        RequestEntry(queued);

        var diagnostics = Loader.GetPropertyLoadDiagnostics();

        diagnostics.Select(x => x.State).Should().Equal(PropertyLoadState.Failed, PropertyLoadState.Queued, PropertyLoadState.Unloaded, PropertyLoadState.Loaded);
        diagnostics.Should().NotContain(x => x.PropertyType == PropertyType.Structure);

        var queuedRow = diagnostics[1];
        queuedRow.PropertyId.Should().Be(queued.Id);
        queuedRow.Name.Should().Be(queued.CustomName);
        queuedRow.OwnerPlayerId.Should().Be(PropertyWorld.OwnerId);
        queuedRow.PropertyType.Should().Be(PropertyType.Apartment);
        queuedRow.LoadType.Should().Be(PropertyLoadType.OnDemand);
        queuedRow.QueuePriority.Should().Be("PlayerRequest");
        queuedRow.LastPhase.Should().Be("CreateArea");
        queuedRow.IsQueued.Should().BeTrue();
        queuedRow.WaiterCount.Should().Be(1);
        queuedRow.IsLoadedAreaValid.Should().BeFalse();
        queuedRow.SpawnedChildCount.Should().Be(0);
        queuedRow.ExpectedChildCount.Should().Be(0);

        var loadedRow = diagnostics[3];
        loadedRow.PropertyId.Should().Be(loaded.Id);
        loadedRow.QueuePriority.Should().BeEmpty();
        loadedRow.LastPhase.Should().Be("Loaded");
        loadedRow.IsQueued.Should().BeFalse();
        loadedRow.IsLoadedAreaValid.Should().BeTrue();
        loadedRow.ExpectedChildCount.Should().Be(2);
        loadedRow.SpawnedChildCount.Should().Be(2, "a loaded property has spawned everything it expects");
        diagnostics[2].SpawnedChildCount.Should().Be(0);
    }

    [Test]
    public void Diagnostics_of_a_load_in_progress_show_how_many_structures_are_spawned()
    {
        var apartment = _world.StoreApartment();
        for (var count = 0; count < 7; count++)
            StoreStructure(apartment.Id);
        RequestEntry(apartment);
        Loader.RunNextBatch();
        Loader.RunNextBatch();

        var row = Loader.GetPropertyLoadDiagnostics().Single();

        row.State.Should().Be(PropertyLoadState.Loading);
        row.LastPhase.Should().Be("SpawnStructures");
        row.ExpectedChildCount.Should().Be(7);
        row.SpawnedChildCount.Should().Be(5);
        row.IsLoadedAreaValid.Should().BeTrue();
    }

    [Test]
    public void Diagnostics_of_equal_states_are_ordered_by_type_then_name()
    {
        var bank = StoreInstanced(PropertyType.Bank, PropertyLayoutType.BankStyle1);
        bank.CustomName = "B";
        var second = _world.StoreApartment();
        second.CustomName = "Zed";
        var first = _world.StoreApartment();
        first.CustomName = "Alpha";

        Loader.GetPropertyLoadDiagnostics().Select(x => x.Name).Should().Equal("Alpha", "Zed", "B");
    }

    // ---- the startup load

    [Test]
    public void Loading_the_module_queues_startup_instances_defers_private_ones_and_spawns_area_and_world_properties()
    {
        var apartment = _world.StoreApartment();
        var privateHouse = StoreInstanced(PropertyType.House, PropertyLayoutType.SmallHouseStyle1);
        var publicHouse = StoreInstanced(PropertyType.House, PropertyLayoutType.SmallHouseStyle1, isPublic: true);
        var (city, hall, interior) = _world.StoreCityWithHall(CityAreaResref);
        var jukebox = StoreStructure(city.Id);
        var orphan = StoreStructure("no-such-parent");
        var insideApartment = StoreStructure(apartment.Id);
        _world.AddArea(CityArea, CityAreaResref);

        Loader.LoadProperties();

        Loader.GetPropertyLoadState(apartment.Id).Should().Be(PropertyLoadState.Unloaded);
        Loader.GetPropertyLoadState(privateHouse.Id).Should().Be(PropertyLoadState.Unloaded);
        Loader.GetPropertyLoadState(publicHouse.Id).Should().Be(PropertyLoadState.Queued);
        Loader.GetPropertyLoadState(interior.Id).Should().Be(PropertyLoadState.Queued);
        Loader.GetPropertyLoadDiagnostics().Where(x => x.IsQueued).Should().OnlyContain(x => x.QueuePriority == "Startup");
        Loader.GetPropertyId(CityArea).Should().Be(city.Id, "the pre-built area of a city is tagged with the city");
        Loader.GetPropertyLoadState(city.Id).Should().Be(PropertyLoadState.Loaded);
        Loader.Scheduled.Should().ContainSingle("one processor serves the whole startup queue");
        Loader.GetPlaceableByPropertyId(jukebox.Id).Should().Be(PropertyWorld.Invalid, "world structures are spawned by the queue");

        Loader.RunAllBatches();

        Loader.GetPropertyLoadState(publicHouse.Id).Should().Be(PropertyLoadState.Loaded);
        Loader.GetPropertyLoadState(interior.Id).Should().Be(PropertyLoadState.Loaded);
        Loader.GetPlaceableByPropertyId(jukebox.Id).Should().NotBe(PropertyWorld.Invalid);
        Loader.GetPlaceableByPropertyId(hall.Id).Should().NotBe(PropertyWorld.Invalid, "the building waits for its interior, then spawns");
        Loader.GetPlaceableByPropertyId(orphan.Id).Should().Be(PropertyWorld.Invalid);
        Loader.GetPlaceableByPropertyId(insideApartment.Id).Should().Be(PropertyWorld.Invalid, "an on-demand apartment's contents load with it");
    }

    [Test]
    public void Loading_the_module_forgets_everything_the_loader_knew_and_schedules_a_new_processor()
    {
        var publicHouse = StoreInstanced(PropertyType.House, PropertyLayoutType.SmallHouseStyle1, isPublic: true);
        Loader.RetryPropertyLoad(publicHouse.Id);
        Loader.Scheduled.Should().ContainSingle("a processor is scheduled and its flag is set");
        Loader.SetPropertyLoadState("leftover", PropertyLoadState.Failed);

        Loader.LoadProperties();

        Loader.Scheduled.Should().HaveCount(2, "the flag was reset before the startup loads were queued, so a new processor is scheduled");
        Loader.GetPropertyLoadState("leftover").Should().Be(PropertyLoadState.Failed, "no such property is stored");
        Loader.GetPropertyLoadFailure("leftover").Should().BeEmpty();
        Loader.GetPropertyLoadDiagnostics().Single().QueuePriority.Should().Be("Startup");
    }

    [Test]
    public void A_world_structure_waits_for_the_load_of_its_interior_before_it_spawns()
    {
        var (_, hall, interior) = _world.StoreCityWithHall(CityAreaResref);
        _world.AddArea(CityArea, CityAreaResref);
        Loader.LoadProperties();

        Loader.RunNextBatch();
        Loader.GetPlaceableByPropertyId(hall.Id).Should().Be(PropertyWorld.Invalid, "the interior is still loading");
        Loader.GetPropertyLoadState(interior.Id).Should().Be(PropertyLoadState.Loading);

        Loader.RunAllBatches();

        Loader.GetPlaceableByPropertyId(hall.Id).Should().NotBe(PropertyWorld.Invalid);
        _world.Recorded.Events.Select(e => e.Property.Id).Should().Contain(hall.Id);
    }

    [Test]
    public void A_completed_interior_requeues_its_exterior_structure()
    {
        var (_, hall, interior) = _world.StoreCityWithHall(CityAreaResref);
        _world.AddArea(CityArea, CityAreaResref);
        Loader.RetryPropertyLoad(interior.Id).Should().BeTrue("the interior is unloaded and gets queued");

        Loader.RunAllBatches();

        Loader.GetPropertyLoadState(interior.Id).Should().Be(PropertyLoadState.Loaded);
        Loader.GetPlaceableByPropertyId(hall.Id).Should().NotBe(PropertyWorld.Invalid, "finishing the interior queues the building");
    }

    [Test]
    public void The_structures_of_an_instance_are_spawned_with_the_instance_not_by_the_startup_queue()
    {
        var house = StoreInstanced(PropertyType.House, PropertyLayoutType.SmallHouseStyle1, isPublic: true);
        var structure = StoreStructure(house.Id);
        Loader.LoadProperties();
        Loader.GetPropertyLoadState(house.Id).Should().Be(PropertyLoadState.Queued);

        Loader.RunAllBatches();

        Loader.GetPropertyLoadState(house.Id).Should().Be(PropertyLoadState.Loaded);
        Loader.GetPlaceableByPropertyId(structure.Id).Should().NotBe(PropertyWorld.Invalid);
        _world.Engine.CountOf("CreateObject").Should().Be(1);
    }

    [Test]
    public void A_world_structure_is_dropped_when_a_property_it_depends_on_failed_or_is_missing()
    {
        var (city, hall, interior) = _world.StoreCityWithHall(CityAreaResref);
        _world.AddArea(CityArea, CityAreaResref);
        var missingInterior = StoreStructure(city.Id);
        missingInterior.ChildPropertyIds[PropertyChildType.Interior] = new List<string> { "missing-interior" };
        var noArea = StoreStructure(StoreCityAt("no_such_area").Id);
        Loader.LoadProperties();
        Loader.AbortQueuedPropertyLoad(interior.Id).Should().BeTrue();

        Loader.RunAllBatches(20);

        Loader.GetPlaceableByPropertyId(missingInterior.Id).Should().Be(PropertyWorld.Invalid, "its interior is not in the database");
        Loader.GetPlaceableByPropertyId(hall.Id).Should().Be(PropertyWorld.Invalid, "its interior failed to load");
        Loader.GetPlaceableByPropertyId(noArea.Id).Should().Be(PropertyWorld.Invalid, "the city's area does not exist");
        Loader.Scheduled.Should().BeEmpty("a dropped structure does not keep the queue alive");
    }

    private WorldProperty StoreCityAt(string areaResref)
    {
        var city = new WorldProperty { CustomName = "Other city", PropertyType = PropertyType.City, ParentPropertyId = areaResref };
        Db.With(city);
        return city;
    }

    [Test]
    public void A_structure_that_throws_while_resolving_or_spawning_does_not_stop_the_queue()
    {
        var city = StoreCityAt(CityAreaResref);
        _world.AddArea(CityArea, CityAreaResref);
        var twoInteriors = StoreStructure(city.Id);
        twoInteriors.ChildPropertyIds[PropertyChildType.Interior] = new List<string> { "one", "two" };
        var cannotSpawn = StoreStructure(city.Id, StructureType.Invalid);
        var fine = StoreStructure(city.Id);

        Loader.LoadProperties();
        Loader.RunAllBatches();

        Loader.GetPlaceableByPropertyId(twoInteriors.Id).Should().Be(PropertyWorld.Invalid, "an interior list of two cannot be resolved");
        Loader.GetPlaceableByPropertyId(cannotSpawn.Id).Should().Be(PropertyWorld.Invalid, "its structure type has no definition");
        Loader.GetPlaceableByPropertyId(fine.Id).Should().NotBe(PropertyWorld.Invalid, "the structures after the failures still spawn");
        Loader.Scheduled.Should().BeEmpty();
    }

    [Test]
    public void A_world_structure_with_an_interior_that_is_not_an_instance_spawns_at_once()
    {
        var city = StoreCityAt(CityAreaResref);
        _world.AddArea(CityArea, CityAreaResref);
        var plain = StoreStructure(city.Id);
        var interior = StoreStructure(plain.Id);
        plain.ChildPropertyIds[PropertyChildType.Interior] = new List<string> { interior.Id };
        var blank = StoreStructure(city.Id);
        blank.ChildPropertyIds[PropertyChildType.Interior] = new List<string> { " " };

        Loader.LoadProperties();
        Loader.RunAllBatches();

        Loader.GetPlaceableByPropertyId(plain.Id).Should().NotBe(PropertyWorld.Invalid);
        Loader.GetPlaceableByPropertyId(blank.Id).Should().NotBe(PropertyWorld.Invalid, "a blank interior id means there is no interior to wait for");
    }

    [Test]
    public void A_world_structure_does_not_wait_for_an_interior_that_loads_on_demand()
    {
        var city = StoreCityAt(CityAreaResref);
        _world.AddArea(CityArea, CityAreaResref);
        var structure = StoreStructure(city.Id);
        var apartmentLikeInterior = StoreInstanced(PropertyType.House, PropertyLayoutType.SmallHouseStyle1, isPublic: false, parentId: structure.Id);
        structure.ChildPropertyIds[PropertyChildType.Interior] = new List<string> { apartmentLikeInterior.Id };

        Loader.LoadProperties();
        Loader.RunAllBatches();

        Loader.GetPlaceableByPropertyId(structure.Id).Should().NotBe(PropertyWorld.Invalid);
        Loader.GetPropertyLoadState(apartmentLikeInterior.Id).Should().Be(PropertyLoadState.Unloaded);
    }

    // ---- spawning a property into the world

    [Test]
    public void A_structure_is_spawned_as_a_placeable_that_cannot_be_destroyed()
    {
        var structure = StoreStructure("parent", StructureType.Jukebox);
        var area = _world.NewObject();

        Loader.SpawnIntoWorld(structure, area);

        var placeable = Loader.GetPlaceableByPropertyId(structure.Id);
        placeable.Should().NotBe(PropertyWorld.Invalid);
        _world.Engine.CallsTo("CreateObject").Should().ContainSingle().Which.Arguments[1].Should().Be("jukebox");
        _world.Engine.CallsTo("SetPlotFlag").Should().ContainSingle().Which.Arguments.Should().Equal(placeable, true);
        Loader.GetPropertyId(placeable).Should().Be(structure.Id);
        _world.Recorded.Events.Should().ContainSingle().Which.Placeable.Should().Be(placeable);
    }

    [Test]
    public void Spawning_a_structure_that_already_has_a_placeable_only_replays_its_event()
    {
        var structure = StoreStructure("parent");
        var area = _world.NewObject();
        Loader.SpawnIntoWorld(structure, area);
        var placeable = Loader.GetPlaceableByPropertyId(structure.Id);
        _world.ClearCalls();

        Loader.SpawnIntoWorld(structure, area);

        _world.Engine.CallsTo("CreateObject").Should().BeEmpty();
        _world.Recorded.Events.Should().HaveCount(2);
        _world.Recorded.Events[1].Placeable.Should().Be(placeable);
        _world.Recorded.Events[1].Change.Should().Be(StructureChangeType.PositionChanged);
    }

    [Test]
    public void A_structure_whose_placeable_was_destroyed_is_spawned_again()
    {
        var structure = StoreStructure("parent");
        var area = _world.NewObject();
        Loader.SpawnIntoWorld(structure, area);
        _world.Invalidate(Loader.GetPlaceableByPropertyId(structure.Id));

        Loader.SpawnIntoWorld(structure, area);

        _world.Engine.CountOf("CreateObject").Should().Be(2);
    }

    [Test]
    public void An_instance_is_spawned_with_a_new_area_that_runs_the_spawn_action_once()
    {
        var apartment = _world.StoreApartment();
        var spawned = new List<uint>();
        _world.Catalog.GetLayoutByType(apartment.Layout).OnSpawnAction = spawned.Add;

        Loader.SpawnIntoWorld(apartment, PropertyWorld.Invalid);
        Loader.SpawnIntoWorld(apartment, PropertyWorld.Invalid);

        var area = _world.Areas.Instances.Should().ContainSingle("the second call finds the area").Subject;
        spawned.Should().Equal(area);
        Loader.GetPropertyLoadState(apartment.Id).Should().Be(PropertyLoadState.Loaded);
        _world.Engine.CallsTo("SetName").Should().ContainSingle().Which.Arguments.Should().Equal(area, "{PC} Apartment");
    }

    [Test]
    public void An_instance_with_an_existing_area_runs_the_spawn_action_it_has_not_run_yet()
    {
        var apartment = _world.StoreApartment();
        var spawned = new List<uint>();
        _world.Catalog.GetLayoutByType(apartment.Layout).OnSpawnAction = spawned.Add;
        var area = _world.NewObject();
        Loader.RegisterInstance(apartment.Id, area, apartment.Layout);

        Loader.SpawnIntoWorld(apartment, PropertyWorld.Invalid);
        Loader.SpawnIntoWorld(apartment, PropertyWorld.Invalid);

        spawned.Should().Equal(area);
        _world.Areas.Created.Should().BeEmpty();
        Loader.GetPropertyLoadState(apartment.Id).Should().Be(PropertyLoadState.Loaded);
    }

    [Test]
    public void An_instance_without_a_spawn_action_spawns_without_one()
    {
        var apartment = _world.StoreApartment();

        Loader.SpawnIntoWorld(apartment, PropertyWorld.Invalid);

        Loader.GetPropertyLoadState(apartment.Id).Should().Be(PropertyLoadState.Loaded);
    }

    [Test]
    public void Spawning_an_instance_the_engine_cannot_create_throws()
    {
        var apartment = _world.StoreApartment();
        _world.Areas.FailCreation = true;

        var spawn = () => Loader.SpawnIntoWorld(apartment, PropertyWorld.Invalid);

        spawn.Should().Throw<InvalidOperationException>().WithMessage("Unable to create property area from resref 'apartment_002'.");
    }

    [Test]
    public void A_prebuilt_area_is_tagged_with_the_property_that_owns_it()
    {
        var city = StoreCityAt(CityAreaResref);
        var area = _world.AddArea(CityArea, CityAreaResref);

        Loader.SpawnIntoWorld(city, area);

        Loader.GetPropertyId(area).Should().Be(city.Id);
        Loader.GetPropertyLoadState(city.Id).Should().Be(PropertyLoadState.Loaded);
    }

    // ---- players in an instance

    private uint EnterableInstance(out WorldProperty apartment)
    {
        apartment = _world.StoreApartment();
        var area = _world.NewObject();
        Loader.RegisterInstance(apartment.Id, area, apartment.Layout);
        Loader.SetPropertyLoadState(apartment.Id, PropertyLoadState.Loaded);
        _world.Engine.SetReturn("OBJECT_SELF", area);
        return area;
    }

    [Test]
    public void A_player_who_enters_an_instance_is_listed_once_and_removed_when_they_leave()
    {
        _world.AddPlayer(Player, PlayerId);
        EnterableInstance(out var apartment);
        _world.Engine.SetReturn("GetExitingObject", Player);

        Loader.EnterPropertyInstance();
        Loader.EnterPropertyInstance();
        Loader.TryGetLoadedInstance(apartment.Id, out var instance);
        instance.Players.Should().Equal(Player);

        Loader.ExitPropertyInstance();
        Loader.ExitPropertyInstance();

        instance.Players.Should().BeEmpty();
    }

    [Test]
    public void Dms_and_creatures_are_not_listed_in_an_instance()
    {
        var dm = _world.AddDm(6001);
        EnterableInstance(out var apartment);
        Loader.TryGetLoadedInstance(apartment.Id, out var instance);

        foreach (var who in new[] { dm, 7777u })
        {
            _world.Engine.SetReturn("GetExitingObject", who);
            Loader.EnterPropertyInstance();
            Loader.ExitPropertyInstance();
        }

        instance.Players.Should().BeEmpty();
    }

    [Test]
    public void An_area_that_is_not_a_loaded_property_instance_lists_nobody()
    {
        _world.AddPlayer(Player, PlayerId);
        var area = _world.NewObject();
        _world.Engine.SetReturn("OBJECT_SELF", area);
        _world.Engine.SetReturn("GetExitingObject", Player);

        Loader.EnterPropertyInstance();
        Loader.ExitPropertyInstance();

        Loader.RegisterInstance("loading", area, PropertyLayoutType.ApartmentSmallStyle1);
        Loader.SetPropertyLoadState("loading", PropertyLoadState.Loading);
        Loader.EnterPropertyInstance();
        Loader.ExitPropertyInstance();

        Loader.SetPropertyLoadState("loading", PropertyLoadState.Loaded);
        Loader.TryGetLoadedInstance("loading", out var instance).Should().BeTrue();
        instance.Players.Should().BeEmpty("nobody was listed while the instance was not loaded");
    }

    // ---- forgetting a deleted property

    [Test]
    public void Forgetting_a_property_clears_its_instance_state_queue_entry_and_placeable()
    {
        var apartment = _world.StoreApartment();
        var structure = StoreStructure("parent");
        _world.AddPlayer(Player, PlayerId);
        RequestEntry(apartment);
        Loader.RunNextBatch();
        Loader.SpawnIntoWorld(structure, _world.NewObject());

        Loader.ForgetProperty(apartment.Id);
        Loader.ForgetProperty(structure.Id);

        Loader.GetPropertyLoadDiagnostics().Single().IsQueued.Should().BeFalse();
        Loader.GetPropertyLoadFailure(apartment.Id).Should().BeEmpty();
        Loader.GetPlaceableByPropertyId(structure.Id).Should().Be(PropertyWorld.Invalid);
        Loader.TryGetLoadedInstance(apartment.Id, out _).Should().BeFalse();
        Loader.GetPropertyLoadState(apartment.Id).Should().Be(PropertyLoadState.Unloaded, "the state is judged from the database again");
    }

    [Test]
    public void Remembering_a_structure_placeable_makes_it_the_placeable_of_the_structure()
    {
        Loader.GetPlaceableByPropertyId("structure-1").Should().Be(PropertyWorld.Invalid);

        Loader.RegisterStructurePlaceable("structure-1", 4321);

        Loader.GetPlaceableByPropertyId("structure-1").Should().Be(4321);
    }

    [Test]
    public void The_failure_message_is_sent_to_the_player()
    {
        Loader.SendPropertyLoadFailedMessage(Player);

        _world.MessagesTo(Player).Should().Equal("This property could not be loaded. Please notify staff.");
    }
}
