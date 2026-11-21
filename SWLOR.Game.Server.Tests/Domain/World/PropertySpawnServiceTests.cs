using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.Game.Server.Entity;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Domain.World;

/// <summary>
/// Unit tests of spawning a property into the world, through the spawn service and the registry it records in. The
/// loader tests cover the same spawns reached through the queue.
/// </summary>
[TestFixture]
public class PropertySpawnServiceTests
{
    private PropertyWorld _world = null!;

    private PropertySpawnService Spawner => _world.Loader.Spawner;

    private PropertyLoadRegistryService Registry => _world.Loader.Registry;

    [SetUp]
    public void SetUp() => _world = new PropertyWorld();

    [TearDown]
    public void TearDown() => _world.Dispose();

    [Test]
    public void Building_the_spawn_service_touches_neither_the_engine_nor_the_database()
    {
        _world.ClearCalls();
        var db = new InMemoryDBService();

        _ = new PropertySpawnService(_world.Areas, _world.Recorded, new PropertyLoadRegistryService(db, _world.Recorded));

        _world.Engine.Calls.Should().BeEmpty();
        db.Reads.Should().BeEmpty();
    }

    [Test]
    public void A_spawned_structure_is_remembered_by_the_registry_under_its_property_id()
    {
        var structure = new WorldProperty
        {
            CustomName = "Structure",
            PropertyType = PropertyType.Structure,
            StructureType = StructureType.Jukebox,
            OwnerPlayerId = PropertyWorld.OwnerId,
            ParentPropertyId = "parent"
        };
        structure.Positions[PropertyLocationType.StaticPosition] = new PropertyLocation { AreaResref = "somewhere", X = 1, Y = 2, Z = 3, Orientation = 90 };

        Spawner.SpawnIntoWorld(structure, _world.NewObject());

        Registry.TryGetStructurePlaceable(structure.Id, out var placeable).Should().BeTrue();
        Registry.GetPropertyId(placeable).Should().Be(structure.Id);
    }

    [Test]
    public void A_spawned_instance_marks_its_spawn_action_as_run_and_its_state_as_loaded()
    {
        var apartment = _world.StoreApartment();

        Spawner.SpawnIntoWorld(apartment, PropertyWorld.Invalid);

        Registry.IsSpawnActionCompleted(apartment.Id).Should().BeTrue();
        Registry.GetPropertyLoadState(apartment.Id).Should().Be(PropertyLoadState.Loaded);
        Registry.TryGetLoadedInstance(apartment.Id, out var instance).Should().BeTrue();
        Registry.GetPropertyId(instance.Area).Should().Be(apartment.Id);
    }

    [Test]
    public void A_structure_with_a_player_given_name_shows_it_on_its_placeable()
    {
        var placeable = _world.NewObject();
        var structure = new WorldProperty { CustomName = "Mara's Jukebox", StructureType = StructureType.Jukebox };

        Spawner.ApplyStructureWorldName(placeable, structure);

        _world.Engine.CallsTo("SetName").Should().ContainSingle().Which.Arguments[0].Should().Be(placeable);
    }
}
