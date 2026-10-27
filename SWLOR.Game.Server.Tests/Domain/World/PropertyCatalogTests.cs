using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.World;

/// <summary>
/// Unit tests of the definitions catalog: the module cache handler fills it from the enums, the layout definitions and
/// the engine's areas, and the lookups answer from what it cached.
/// </summary>
[TestFixture]
public class PropertyCatalogTests
{
    private const string ApartmentResref = "apartment_002";
    private const uint ApartmentTemplate = 5000;

    private PropertyWorld _world = null!;

    private PropertyCatalog Catalog => _world.Catalog;

    [SetUp]
    public void SetUp() => _world = new PropertyWorld();

    [TearDown]
    public void TearDown() => _world.Dispose();

    [Test]
    public void Building_the_catalog_touches_neither_the_engine_nor_the_area_service()
    {
        _world.ClearCalls();

        _ = new PropertyCatalog(new PropertyWorld.StubAreas(_world));

        _world.Engine.Calls.Should().BeEmpty();
    }

    [Test]
    public void Nothing_is_cached_before_the_module_cache_handler_runs()
    {
        using var world = new PropertyWorld(cacheDefinitions: false);

        world.Catalog.PropertyTypes.Should().BeEmpty();
        world.Catalog.PermissionsByPropertyType.Should().BeEmpty();
        world.Catalog.GetStructuresByInteriorPropertyType(PropertyType.CityHall).Should().BeEmpty();
        world.Catalog.GetAllInstanceAreaResrefs().Should().BeEmpty();
        var lookup = () => world.Catalog.GetLayoutByType(PropertyLayoutType.ApartmentSmallStyle1);
        lookup.Should().Throw<KeyNotFoundException>();
    }

    [Test]
    public void Every_property_type_has_its_detail()
    {
        Catalog.PropertyTypes.Keys.Should().BeEquivalentTo(Enum.GetValues<PropertyType>());

        var apartment = Catalog.GetPropertyDetail(PropertyType.Apartment);
        apartment.Name.Should().Be("Apartment");
        apartment.SpawnType.Should().Be(PropertySpawnType.Instance);
        apartment.LoadType.Should().Be(PropertyLoadType.OnDemand);
        Catalog.GetPropertyDetail(PropertyType.Structure).SpawnType.Should().Be(PropertySpawnType.World);
        Catalog.GetPropertyDetail(PropertyType.City).SpawnType.Should().Be(PropertySpawnType.Area);
    }

    [Test]
    public void Layouts_are_cached_and_grouped_by_property_type()
    {
        var layout = Catalog.GetLayoutByType(PropertyLayoutType.ApartmentSmallStyle1);

        layout.PropertyType.Should().Be(PropertyType.Apartment);
        layout.AreaInstanceResref.Should().Be(ApartmentResref);
        Catalog.GetAllLayoutsByPropertyType(PropertyType.Apartment).Should().Contain(PropertyLayoutType.ApartmentSmallStyle1);
        Catalog.GetAllLayoutsByPropertyType(PropertyType.Starship).Should().Contain(PropertyLayoutType.LightFreighter1);
    }

    [Test]
    public void The_layouts_of_a_property_type_are_a_copy()
    {
        var layouts = Catalog.GetAllLayoutsByPropertyType(PropertyType.Apartment);
        var count = layouts.Count;

        layouts.Clear();

        Catalog.GetAllLayoutsByPropertyType(PropertyType.Apartment).Should().HaveCount(count);
    }

    [Test]
    public void Only_the_active_structures_and_permissions_are_cached()
    {
        Catalog.GetStructureByType(StructureType.Jukebox).Resref.Should().Be("jukebox");
        var inactive = () => Catalog.GetStructureByType(StructureType.Invalid);
        inactive.Should().Throw<KeyNotFoundException>();

        Catalog.GetPermissionByType(PropertyPermissionType.EditStructures).IsActive.Should().BeTrue();
        var missing = () => Catalog.GetPermissionByType(PropertyPermissionType.Invalid);
        missing.Should().Throw<KeyNotFoundException>();
    }

    [Test]
    public void Each_property_type_has_the_permissions_it_has_always_had()
    {
        Catalog.GetPermissionsByPropertyType(PropertyType.Apartment).Should().Equal(
            PropertyPermissionType.EditStructures,
            PropertyPermissionType.RetrieveStructures,
            PropertyPermissionType.RenameProperty,
            PropertyPermissionType.ExtendLease,
            PropertyPermissionType.CancelLease,
            PropertyPermissionType.EnterProperty,
            PropertyPermissionType.ChangeDescription,
            PropertyPermissionType.EditCategories);
        Catalog.GetPermissionsByPropertyType(PropertyType.Category).Should().Equal(PropertyPermissionType.AccessStorage);
        Catalog.GetPermissionsByPropertyType(PropertyType.City).Should().Contain(
            new[] { PropertyPermissionType.EditTaxes, PropertyPermissionType.AccessTreasury, PropertyPermissionType.ManageUpgrades, PropertyPermissionType.ManageUpkeep });
        Catalog.GetPermissionsByPropertyType(PropertyType.Starship).Should().Contain(
            new[] { PropertyPermissionType.PilotShip, PropertyPermissionType.RefitShip });
        Catalog.GetPermissionsByPropertyType(PropertyType.Lab).Should().Contain(PropertyPermissionType.ManageIncubators);

        foreach (var type in new[] { PropertyType.CityHall, PropertyType.Bank, PropertyType.MedicalCenter, PropertyType.Starport, PropertyType.Cantina, PropertyType.House })
            Catalog.GetPermissionsByPropertyType(type).Should().Contain(PropertyPermissionType.EditStructures, type.ToString());

        Catalog.PermissionsByPropertyType.Keys.Should().BeEquivalentTo(new[]
        {
            PropertyType.Apartment, PropertyType.CityHall, PropertyType.Starship, PropertyType.City, PropertyType.Category,
            PropertyType.Bank, PropertyType.MedicalCenter, PropertyType.Starport, PropertyType.Cantina, PropertyType.House, PropertyType.Lab
        });
    }

    [Test]
    public void Structures_are_linked_back_to_the_property_type_of_their_interior()
    {
        Catalog.GetStructuresByInteriorPropertyType(PropertyType.CityHall).Should().Contain(StructureType.CityHall);
        Catalog.GetStructuresByInteriorPropertyType(PropertyType.Bank).Should().Contain(StructureType.Bank);
        Catalog.GetStructuresByInteriorPropertyType(PropertyType.Starship).Should().BeEmpty("no structure has a starship interior");
    }

    [Test]
    public void The_structures_of_an_interior_type_are_a_copy()
    {
        var structures = Catalog.GetStructuresByInteriorPropertyType(PropertyType.CityHall);
        structures.Clear();

        Catalog.GetStructuresByInteriorPropertyType(PropertyType.CityHall).Should().Contain(StructureType.CityHall);
    }

    [Test]
    public void Instance_templates_are_the_areas_of_the_engine_whose_resref_a_layout_uses()
    {
        using var world = new PropertyWorld(cacheDefinitions: false);
        world.Engine.SetReturnSequence("GetFirstArea", world.AddArea(ApartmentTemplate, ApartmentResref));
        world.Engine.SetReturnSequence("GetNextArea", world.AddArea(5001, "not_a_template"), PropertyWorld.Invalid);

        world.Catalog.CacheData();

        world.Catalog.GetInstanceTemplate(PropertyLayoutType.ApartmentSmallStyle1).Should().Be(ApartmentTemplate);
        var noTemplate = () => world.Catalog.GetInstanceTemplate(PropertyLayoutType.LightFreighter1);
        noTemplate.Should().Throw<KeyNotFoundException>("the engine has no area for that layout");
    }

    [Test]
    public void The_entrance_of_a_layout_is_the_property_entrance_waypoint_of_its_template_area()
    {
        using var world = new PropertyWorld(cacheDefinitions: false);
        var area = world.AddArea(ApartmentTemplate, ApartmentResref);
        var decoration = world.Valid(6001);
        var entrance = world.Valid(6002);
        world.SetTag(decoration, "DECORATION");
        world.SetTag(entrance, "PROPERTY_ENTRANCE");
        world.Engine.SetReturnHandler("GetFirstObjectInArea", arguments => (uint)arguments[0]! == area ? decoration : PropertyWorld.Invalid);
        world.Engine.SetReturnSequence("GetNextObjectInArea", entrance);
        world.Engine.SetReturnHandler("GetPosition", _ => new Vector3(1.5f, 2.5f, 3.5f));
        world.Engine.SetReturnHandler("GetFacing", _ => 90f);

        world.Catalog.CacheData();

        world.Catalog.GetEntrancePosition(PropertyLayoutType.ApartmentSmallStyle1).Should().Be(new Vector4(1.5f, 2.5f, 3.5f, 90f));
        world.Catalog.GetEntrancePosition(PropertyLayoutType.LightFreighter1).Should().Be(new Vector4(), "a template with no area has no entrance");
    }

    [Test]
    public void The_instance_area_resrefs_are_the_distinct_resrefs_of_the_instanced_property_types()
    {
        var resrefs = Catalog.GetAllInstanceAreaResrefs();

        resrefs.Should().Contain(ApartmentResref);
        resrefs.Should().OnlyHaveUniqueItems();
        resrefs.Should().NotContain(string.Empty);
    }

    [Test]
    public void A_structure_changed_event_without_an_action_does_nothing()
    {
        _world.ClearCalls();

        Catalog.RunStructureChangedEvent(StructureType.Jukebox, StructureChangeType.PositionChanged, new WorldProperty(), 1234);
        Catalog.RunStructureChangedEvent(StructureType.CityHall, StructureChangeType.Invalid, new WorldProperty(), 1234);

        _world.Engine.Calls.Should().BeEmpty();
    }

    [Test]
    public void A_retrieved_building_loses_its_door()
    {
        var building = _world.Valid(7001);
        var door = _world.Valid(7002);
        _world.Engine.SetReturnHandler("GetLocalObject", _ => door);

        Catalog.RunStructureChangedEvent(StructureType.CityHall, StructureChangeType.Retrieved, new WorldProperty(), building);

        _world.Engine.CallsTo("DestroyObject").Should().ContainSingle().Which.Arguments[0].Should().Be(door);
    }
}
