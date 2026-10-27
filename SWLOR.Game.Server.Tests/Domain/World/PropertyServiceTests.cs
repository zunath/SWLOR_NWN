using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Domain.World;

/// <summary>
/// Unit tests of the property service: creating and deleting properties, the boot-time cleanup, permissions and the
/// starship dock rules. The catalog, the loader and the city governance are the real parts over the scripted world.
/// </summary>
[TestFixture]
public class PropertyServiceTests : PropertyServiceTestBase
{
    private const uint CityArea = 8000;
    private const string CityAreaResref = "city_area";

    /// <summary>The one property of a type the service saved (it saves a parent again when it adds a child to it).</summary>
    private WorldProperty SavedProperty(PropertyType type) =>
        Db.Saved.OfType<WorldProperty>().Distinct().Single(x => x.PropertyType == type);

    // ---- construction and the parts

    [Test]
    public void Building_the_service_touches_nothing()
    {
        World.ClearCalls();
        var db = new InMemoryDBService();

        _ = new PropertyService(db, World.Areas, World.Planets, World.Recorded, World.Loader, World.City);

        World.Engine.Calls.Should().BeEmpty();
        db.Reads.Should().BeEmpty();
    }

    [Test]
    public void The_members_of_the_parts_are_forwarded_to_them()
    {
        var apartment = World.StoreApartment();
        var area = World.NewObject();
        Service.AssignPropertyId(area, apartment.Id);
        Service.RegisterInstance(apartment.Id, area, apartment.Layout);

        Service.GetPropertyId(area).Should().Be(apartment.Id);
        Service.GetPropertyLoadState(apartment.Id).Should().Be(PropertyLoadState.Loaded);
        Service.GetPropertyLoadFailure(apartment.Id).Should().BeEmpty();
        Service.GetPropertyLoadDiagnostics().Should().ContainSingle();
        Service.GetAllInstanceAreaResrefs().Should().Equal(World.Catalog.GetAllInstanceAreaResrefs());
        Service.GetLayoutByType(apartment.Layout).Should().BeSameAs(World.Catalog.GetLayoutByType(apartment.Layout));
        Service.GetStructureByType(StructureType.Jukebox).Should().BeSameAs(World.Catalog.GetStructureByType(StructureType.Jukebox));
        Service.GetAllLayoutsByPropertyType(PropertyType.Apartment).Should().Equal(World.Catalog.GetAllLayoutsByPropertyType(PropertyType.Apartment));
        Service.GetEntrancePosition(apartment.Layout).Should().Be(World.Catalog.GetEntrancePosition(apartment.Layout));
        Service.GetPermissionByType(PropertyPermissionType.EditStructures).Should().BeSameAs(World.Catalog.GetPermissionByType(PropertyPermissionType.EditStructures));
        Service.GetPermissionsByPropertyType(PropertyType.Apartment).Should().BeSameAs(World.Catalog.GetPermissionsByPropertyType(PropertyType.Apartment));
        Service.GetPropertyDetail(PropertyType.Apartment).Should().BeSameAs(World.Catalog.GetPropertyDetail(PropertyType.Apartment));
        Service.GetStructuresByInteriorPropertyType(PropertyType.CityHall).Should().Equal(World.Catalog.GetStructuresByInteriorPropertyType(PropertyType.CityHall));
        Service.GetPlaceableByPropertyId("none").Should().Be(PropertyWorld.Invalid);
        Service.GetCityLevelName(3).Should().Be("Township");
        Service.GetCitizensRequiredForNextCityLevel(2).Should().Be(10);
        Service.GetEffectiveUpgradeLevel("none", PropertyUpgradeType.StarportLevel).Should().Be(0);
        Service.TryGetLoadedInstance(apartment.Id, out var instance).Should().BeTrue();
        instance.Area.Should().Be(area);
        Service.TryResolveEnterableInstance(Player, apartment.Id, out _).Should().BeTrue();
        Service.RetryPropertyLoad("none").Should().BeFalse();
        Service.AbortQueuedPropertyLoad("none").Should().BeFalse();
        Service.NotifyPropertyLoadWaitersForStaff("none");
        Service.BroadcastCityUpkeepPaid(new WorldProperty { Upkeep = 1 });
        Service.RunStructureChangedEvent(StructureType.Jukebox, StructureChangeType.PositionChanged, new WorldProperty(), 1);
        World.Recorded.Events.Should().ContainSingle();
    }

    [Test]
    public void The_module_cache_handler_fills_the_catalog()
    {
        using var world = new PropertyWorld(cacheDefinitions: false);

        world.Service.CacheData();

        world.Catalog.PropertyTypes.Should().NotBeEmpty();
    }

    // ---- permissions

    [Test]
    public void A_dm_always_has_every_permission()
    {
        var dm = World.AddDm(6001);
        var possessing = World.AddDm(6002, "dm-2", possessing: true);

        Service.HasPropertyPermission(dm, "nothing", PropertyPermissionType.EditStructures).Should().BeTrue();
        Service.HasPropertyPermission(possessing, "nothing", PropertyPermissionType.EditStructures).Should().BeTrue();
        Service.CanGrantPermission(dm, "nothing", PropertyPermissionType.EditStructures).Should().BeTrue();
        Service.CanGrantPermission(possessing, "nothing", PropertyPermissionType.EditStructures).Should().BeTrue();
    }

    [Test]
    public void A_creature_that_is_not_a_player_has_no_permission()
    {
        var creature = World.NewObject();
        Permit(PlayerId, "property-1", PropertyPermissionType.EditStructures);

        Service.HasPropertyPermission(creature, "property-1", PropertyPermissionType.EditStructures).Should().BeFalse();
        Service.CanGrantPermission(creature, "property-1", PropertyPermissionType.EditStructures).Should().BeFalse();
    }

    [Test]
    public void A_player_has_the_permissions_of_their_record_for_that_property_only()
    {
        var permission = Permit(PlayerId, "property-1");
        permission.Permissions[PropertyPermissionType.EditStructures] = true;
        permission.Permissions[PropertyPermissionType.RenameProperty] = false;
        permission.GrantPermissions[PropertyPermissionType.EditStructures] = false;
        permission.GrantPermissions[PropertyPermissionType.RenameProperty] = true;
        Permit(OtherPlayerId, "property-1", PropertyPermissionType.RenameProperty);
        Permit(PlayerId, "property-2", PropertyPermissionType.RetrieveStructures);

        Service.HasPropertyPermission(Player, "property-1", PropertyPermissionType.EditStructures).Should().BeTrue();
        Service.HasPropertyPermission(Player, "property-1", PropertyPermissionType.RenameProperty).Should().BeFalse();
        Service.CanGrantPermission(Player, "property-1", PropertyPermissionType.EditStructures).Should().BeFalse();
        Service.CanGrantPermission(Player, "property-1", PropertyPermissionType.RenameProperty).Should().BeTrue();
        Service.HasPropertyPermission(Player, "property-3", PropertyPermissionType.EditStructures).Should().BeFalse("no record");
        Service.CanGrantPermission(Player, "property-3", PropertyPermissionType.EditStructures).Should().BeFalse("no record");
    }

    [Test]
    public void The_permissions_of_a_players_categories_are_found_through_the_categories_of_the_property()
    {
        var weapons = StoreCategory("property-1", "Weapons");
        var armor = StoreCategory("property-1", "Armor");
        StoreCategory("property-2", "Elsewhere");
        var mine = Permit(PlayerId, weapons.Id, PropertyPermissionType.AccessStorage);
        var alsoMine = Permit(PlayerId, armor.Id, PropertyPermissionType.AccessStorage);
        Permit(OtherPlayerId, weapons.Id, PropertyPermissionType.AccessStorage);
        Permit(PlayerId, "property-1", PropertyPermissionType.EditStructures);

        var permissions = Service.GetCategoryPermissions(PlayerId, "property-1");

        permissions.Should().BeEquivalentTo(new[] { mine, alsoMine });
    }

    // ---- creating properties

    [Test]
    public void An_apartment_is_a_lease_with_storage_categories_and_full_permissions_for_its_owner()
    {
        var apartment = Service.CreateApartment(Player, PropertyLayoutType.ApartmentSmallStyle1);

        apartment.PropertyType.Should().Be(PropertyType.Apartment);
        apartment.CustomName.Should().Be("Apartment");
        apartment.OwnerPlayerId.Should().Be(PlayerId);
        apartment.Layout.Should().Be(PropertyLayoutType.ApartmentSmallStyle1);
        apartment.IsPubliclyAccessible.Should().BeFalse();
        apartment.ItemStorageCount.Should().Be(World.Catalog.GetLayoutByType(apartment.Layout).ItemStorageLimit);
        apartment.Dates[PropertyDateType.Lease].Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromMinutes(1));
        Db.Saved.Should().Contain(apartment);

        var ownerPermission = Db.Saved.OfType<WorldPropertyPermission>().Single(x => x.PropertyId == apartment.Id);
        ownerPermission.PlayerId.Should().Be(PlayerId);
        ownerPermission.Permissions.Keys.Should().BeEquivalentTo(World.Catalog.GetPermissionsByPropertyType(PropertyType.Apartment));
        ownerPermission.Permissions.Values.Should().OnlyContain(granted => granted);
        ownerPermission.GrantPermissions.Values.Should().OnlyContain(granted => granted);

        var categories = Db.Saved.OfType<WorldPropertyCategory>().ToList();
        categories.Select(x => x.Name).Should().Equal("Weapons", "Armor", "Crafting", "Miscellaneous");
        categories.Should().OnlyContain(x => x.ParentPropertyId == apartment.Id);
        var categoryPermissions = Db.Saved.OfType<WorldPropertyPermission>().Where(x => x.PropertyId != apartment.Id).ToList();
        categoryPermissions.Select(x => x.PropertyId).Should().BeEquivalentTo(categories.Select(x => x.Id));
        categoryPermissions.Should().OnlyContain(x => x.PlayerId == PlayerId && x.Permissions[PropertyPermissionType.AccessStorage]);

        Service.GetPropertyLoadState(apartment.Id).Should().Be(PropertyLoadState.Unloaded, "an apartment loads when it is first entered");
        World.Areas.Created.Should().BeEmpty();
    }

    [Test]
    public void A_property_made_for_another_owner_also_gives_the_creator_permissions()
    {
        World.AddPlayer(OtherPlayer, OtherPlayerId, "Other");
        var building = StoreProperty(PropertyType.City, PropertyLayoutType.City, owner: OtherPlayerId);
        Service.CreateBuilding(Player, Item, building.Id, PropertyType.House, PropertyLayoutType.SmallHouseStyle1, StructureType.SmallHouseStyle1, World.MakeLocation(CityArea, Vector3.Zero, 0f));

        var interior = SavedProperty(PropertyType.House);
        interior.OwnerPlayerId.Should().Be(OtherPlayerId, "a building belongs to the owner of the city");
        var permissions = Db.Saved.OfType<WorldPropertyPermission>().Where(x => x.PropertyId == interior.Id).ToList();
        permissions.Select(x => x.PlayerId).Should().BeEquivalentTo(new[] { OtherPlayerId, PlayerId }, "the owner and the player who placed it");
    }

    [Test]
    public void A_starship_remembers_the_docks_it_was_created_with()
    {
        AddPlanetWithStarport(PlanetType.Viscara, "viscara_port", 8200, 9001, 9002);
        var spaceArea = World.AddArea(8300, "space_area");
        var landingArea = World.AddArea(8301, "landing_area");
        var space = World.MakeLocation(spaceArea, new Vector3(10, 11, 12), 30f);
        var landing = World.MakeLocation(landingArea, new Vector3(20, 21, 22), 60f);

        var ship = Service.CreateStarship(Player, PropertyLayoutType.LightFreighter1, PlanetType.Viscara, space, landing);

        ship.PropertyType.Should().Be(PropertyType.Starship);
        ship.CustomName.Should().Be("Starship");
        ship.OwnerPlayerId.Should().Be(PlayerId);
        var dock = ship.Positions[PropertyLocationType.DockPosition];
        (dock.AreaResref, dock.InstancePropertyId, dock.X, dock.Y, dock.Z, dock.Orientation).Should().Be(("landing_area", string.Empty, 20f, 21f, 22f, 60f));
        var spacePosition = ship.Positions[PropertyLocationType.SpacePosition];
        (spacePosition.AreaResref, spacePosition.X, spacePosition.Y, spacePosition.Z, spacePosition.Orientation).Should().Be(("space_area", 10f, 11f, 12f, 30f));
        var npcDock = ship.Positions[PropertyLocationType.LastNPCDockPosition];
        (npcDock.AreaResref, npcDock.X, npcDock.Y, npcDock.Z, npcDock.Orientation).Should().Be(("viscara_port", 1f, 2f, 3f, 90f));
        var npcSpace = ship.Positions[PropertyLocationType.LastNPCSpacePosition];
        (npcSpace.AreaResref, npcSpace.X, npcSpace.Y, npcSpace.Z, npcSpace.Orientation).Should().Be(("viscara_port_orbit", 4f, 5f, 6f, 180f));
        Service.GetPropertyLoadState(ship.Id).Should().Be(PropertyLoadState.Unloaded);
    }

    [Test]
    public void A_starship_created_while_docked_in_a_property_instance_records_the_property_not_the_area()
    {
        AddPlanetWithStarport(PlanetType.Viscara, "viscara_port", 8200, 9001, 9002);
        var spaceArea = World.AddArea(8300, "space_area");
        var landingArea = World.AddArea(8301, "player_starport_instance");
        Service.AssignPropertyId(landingArea, "starport-interior-1");

        var ship = Service.CreateStarship(
            Player,
            PropertyLayoutType.LightFreighter1,
            PlanetType.Viscara,
            World.MakeLocation(spaceArea, Vector3.Zero, 0f),
            World.MakeLocation(landingArea, Vector3.Zero, 0f));

        var dock = ship.Positions[PropertyLocationType.DockPosition];
        dock.AreaResref.Should().BeEmpty();
        dock.InstancePropertyId.Should().Be("starport-interior-1");
    }

    [Test]
    public void A_city_is_founded_around_a_city_hall_and_makes_the_founder_its_first_citizen()
    {
        var area = World.AddArea(CityArea, CityAreaResref, "Test Area");
        var founder = StorePlayer(PlayerId, "old-city");
        founder.PropertyOwedTaxes = 77;
        World.Objects.SetReturn("Serialize", "serialized-hall");
        var location = World.MakeLocation(area, new Vector3(5, 6, 7), 0f);

        Service.CreateCity(Player, area, Item, location);

        var city = SavedProperty(PropertyType.City);
        city.CustomName.Should().Be("Player City");
        city.OwnerPlayerId.Should().Be(PlayerId);
        city.ParentPropertyId.Should().Be(CityAreaResref);
        city.Layout.Should().Be(PropertyLayoutType.City);
        city.Dates[PropertyDateType.Upkeep].Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromMinutes(1));
        city.Dates[PropertyDateType.ElectionStart].Should().BeCloseTo(DateTime.UtcNow.AddDays(21), TimeSpan.FromMinutes(1));
        city.Dates[PropertyDateType.BelowRequiredCitizens].Should().BeCloseTo(DateTime.UtcNow.AddHours(18), TimeSpan.FromMinutes(1));
        city.Taxes.Should().Contain(new KeyValuePair<PropertyTaxType, int>(PropertyTaxType.Citizenship, 0))
            .And.Contain(new KeyValuePair<PropertyTaxType, int>(PropertyTaxType.Transportation, 0));
        city.Upgrades.Values.Should().OnlyContain(level => level == 1).And.HaveCount(4);
        Service.GetPropertyId(area).Should().Be(city.Id, "the pre-built area is the city");
        Service.GetPropertyLoadState(city.Id).Should().Be(PropertyLoadState.Loaded);

        var hall = SavedProperty(PropertyType.Structure);
        hall.StructureType.Should().Be(StructureType.CityHall);
        hall.ParentPropertyId.Should().Be(city.Id);
        hall.SerializedItem.Should().Be("serialized-hall");
        var interior = SavedProperty(PropertyType.CityHall);
        interior.ParentPropertyId.Should().Be(hall.Id);
        hall.ChildPropertyIds[PropertyChildType.Interior].Should().Equal(interior.Id);
        city.ChildPropertyIds[PropertyChildType.Structure].Should().Equal(hall.Id);
        World.Engine.CallsTo("DestroyObject").Should().ContainSingle().Which.Arguments[0].Should().Be(Item);

        var saved = Db.Get<Player>(PlayerId);
        saved.CitizenPropertyId.Should().Be(city.Id);
        saved.PropertyOwedTaxes.Should().Be(0, "a new citizenship does not inherit taxes");
    }

    [Test]
    public void A_structure_takes_its_storage_from_the_definition_and_the_item_bonuses()
    {
        var parent = StoreProperty(PropertyType.Apartment, PropertyLayoutType.ApartmentSmallStyle1);
        parent.ItemStorageCount = 10;
        var area = World.AddArea(8100, "apartment_area");
        var location = World.MakeLocation(area, new Vector3(1, 2, 3), 0f);
        World.Objects.SetReturn("Serialize", "serialized-item");
        World.Engine.SetReturnSequence("GetIsItemPropertyValid", true, true, false);
        World.Engine.SetReturnSequence("GetItemPropertyType", ItemPropertyType.StructureBonus, ItemPropertyType.AbilityBonus);
        World.Engine.SetReturnSequence("GetItemPropertyCostTableValue", 7);
        var baseStorage = World.Catalog.GetStructureByType(StructureType.Jukebox).ItemStorage;

        var structure = Service.CreateStructure(parent.Id, Item, StructureType.Jukebox, location);

        structure.PropertyType.Should().Be(PropertyType.Structure);
        structure.CustomName.Should().Be(World.Catalog.GetStructureByType(StructureType.Jukebox).Name);
        structure.OwnerPlayerId.Should().Be(parent.OwnerPlayerId);
        structure.ParentPropertyId.Should().Be(parent.Id);
        structure.StructureType.Should().Be(StructureType.Jukebox);
        structure.SerializedItem.Should().Be("serialized-item");
        structure.ItemStorageCount.Should().Be(baseStorage + 7);
        var position = structure.Positions[PropertyLocationType.StaticPosition];
        (position.AreaResref, position.X, position.Y, position.Z, position.Orientation).Should().Be(("apartment_area", 1f, 2f, 3f, 0f));
        parent.ChildPropertyIds[PropertyChildType.Structure].Should().Equal(structure.Id);
        parent.ItemStorageCount.Should().Be(10 + baseStorage + 7);
        Db.Saved.Should().ContainInOrder(structure, parent);

        var placeable = Service.GetPlaceableByPropertyId(structure.Id);
        placeable.Should().NotBe(PropertyWorld.Invalid);
        Service.GetPropertyId(placeable).Should().Be(structure.Id);
        World.Engine.CallsTo("SetPlotFlag").Should().ContainSingle().Which.Arguments.Should().Equal(placeable, true);
        World.Engine.CallsTo("DestroyObject").Should().ContainSingle().Which.Arguments[0].Should().Be(Item);
        World.Recorded.Events.Should().ContainSingle().Which.Placeable.Should().Be(placeable);
    }

    [Test]
    public void A_building_is_a_structure_with_an_interior_property_and_the_event_for_its_door()
    {
        var city = StoreProperty(PropertyType.City, PropertyLayoutType.City, parentId: CityAreaResref);
        var area = World.AddArea(CityArea, CityAreaResref);
        var location = World.MakeLocation(area, new Vector3(5, 6, 7), 0f);

        Service.CreateBuilding(Player, Item, city.Id, PropertyType.House, PropertyLayoutType.SmallHouseStyle1, StructureType.SmallHouseStyle1, location);

        var structure = SavedProperty(PropertyType.Structure);
        var interior = SavedProperty(PropertyType.House);
        interior.CustomName.Should().Be(structure.CustomName, "the interior is named after its building");
        interior.ParentPropertyId.Should().Be(structure.Id);
        interior.ItemStorageCount.Should().Be(World.Catalog.GetLayoutByType(PropertyLayoutType.SmallHouseStyle1).ItemStorageLimit);
        structure.ChildPropertyIds[PropertyChildType.Interior].Should().Equal(interior.Id);
        Db.Saved.OfType<WorldPropertyCategory>().Should().HaveCount(4, "a house has item storage");
        Service.GetPropertyLoadState(interior.Id).Should().Be(PropertyLoadState.Unloaded, "a private house loads on demand");
        World.Recorded.Events.Should().HaveCount(2, "once for the structure and once more now that its interior is linked");
        World.Recorded.Events.Select(e => e.Structure).Should().OnlyContain(type => type == StructureType.SmallHouseStyle1);
    }

    [Test]
    public void A_building_whose_interior_loads_at_startup_is_spawned_at_once()
    {
        var city = StoreProperty(PropertyType.City, PropertyLayoutType.City, parentId: CityAreaResref);
        var area = World.AddArea(CityArea, CityAreaResref);

        Service.CreateBuilding(Player, Item, city.Id, PropertyType.CityHall, PropertyLayoutType.CityHallStyle1, StructureType.CityHall, World.MakeLocation(area, Vector3.Zero, 0f));

        var interior = SavedProperty(PropertyType.CityHall);
        Service.GetPropertyLoadState(interior.Id).Should().Be(PropertyLoadState.Loaded);
        World.Areas.Created.Should().ContainSingle();
    }

    // ---- deleting properties

    [Test]
    public void Deleting_a_city_deletes_its_buildings_permissions_and_categories_and_releases_its_citizens()
    {
        var (city, hall, interior) = World.StoreCityWithHall(CityAreaResref);
        var orphan = StoreStructure(city.Id);
        var interiorCategory = StoreCategory(interior.Id);
        var cityPermission = Permit(PlayerId, city.Id, PropertyPermissionType.EditStructures);
        var categoryPermission = Permit(PlayerId, interiorCategory.Id, PropertyPermissionType.AccessStorage);
        var citizen = StorePlayer("citizen-1", city.Id);
        citizen.PropertyOwedTaxes = 55;
        var stranger = StorePlayer("stranger", "another-city");
        stranger.PropertyOwedTaxes = 9;
        var area = World.AddArea(CityArea, CityAreaResref);
        World.Loader.SpawnIntoWorld(hall, area);

        Service.DeleteProperty(city);

        Db.Deleted.Should().BeEquivalentTo(new[]
        {
            city.Id, hall.Id, interior.Id, orphan.Id, interiorCategory.Id, cityPermission.Id, categoryPermission.Id
        });
        citizen.CitizenPropertyId.Should().BeEmpty();
        citizen.PropertyOwedTaxes.Should().Be(0);
        Db.Saved.Should().Contain(citizen);
        stranger.CitizenPropertyId.Should().Be("another-city");
        stranger.PropertyOwedTaxes.Should().Be(9);
        Service.GetPlaceableByPropertyId(hall.Id).Should().Be(PropertyWorld.Invalid, "the loader forgot the building");
        World.Events.CallsTo("PushEventData").Select(call => call.Arguments[1]).Should().Contain(city.Id);
        World.Events.CallsTo("SignalEvent").Should().HaveCount(4, "the city and its three children");
        World.Events.CallsTo("SignalEvent").Should().OnlyContain(call => (string)call.Arguments[0]! == "SWLOR_DELETE_PROPERTY" && (uint)call.Arguments[1]! == PropertyWorld.Module);
    }

    [Test]
    public void Children_that_no_longer_exist_are_skipped_and_the_property_is_still_deleted()
    {
        var apartment = World.StoreApartment();
        apartment.ChildPropertyIds[PropertyChildType.Structure] = new List<string> { "vanished-child" };

        Service.DeleteProperty(apartment);

        Db.Deleted.Should().Equal(apartment.Id);
    }

    [Test]
    public void Deleting_a_starship_unregisters_it_from_the_starports_but_keeps_them()
    {
        var starport = StoreProperty(PropertyType.Starport, PropertyLayoutType.StarportStyle1);
        var otherShip = "other-ship";
        var stale = StoreProperty(PropertyType.Starport, PropertyLayoutType.StarportStyle1);
        stale.ChildPropertyIds[PropertyChildType.Starship] = new List<string> { otherShip };
        var ship = StoreProperty(PropertyType.Starship, PropertyLayoutType.LightFreighter1);
        starport.ChildPropertyIds[PropertyChildType.Starship] = new List<string> { ship.Id, otherShip };
        ship.ChildPropertyIds[PropertyChildType.RegisteredStarport] = new List<string> { starport.Id, stale.Id, "missing-starport" };

        Service.DeleteProperty(ship);

        starport.ChildPropertyIds[PropertyChildType.Starship].Should().Equal(otherShip);
        Db.Saved.Should().Contain(starport).And.NotContain(stale, "a starport that never listed the ship is not rewritten");
        Db.Deleted.Should().Equal(ship.Id);
    }

    [Test]
    public void Deleting_a_property_removes_the_instance_the_loader_kept_for_it()
    {
        var apartment = World.StoreApartment();
        World.Loader.TryResolveEnterableInstance(Player, apartment.Id, out _);
        World.Loader.RunAllBatches();
        World.Loader.TryGetLoadedInstance(apartment.Id, out _).Should().BeTrue();

        Service.DeleteProperty(apartment);

        World.Loader.TryGetLoadedInstance(apartment.Id, out _).Should().BeFalse();
        Service.GetPropertyLoadState(apartment.Id).Should().Be(PropertyLoadState.Failed, "it is gone from the database too");
    }

    // ---- the boot

    [Test]
    public void The_module_load_refreshes_permissions_processes_cities_cleans_up_and_loads_properties()
    {
        var expired = World.StoreApartment();
        expired.Dates[PropertyDateType.Lease] = DateTime.UtcNow.AddDays(-1);
        var current = World.StoreApartment();
        current.Dates[PropertyDateType.Lease] = DateTime.UtcNow.AddDays(3);
        var queued = World.StoreApartment();
        queued.IsQueuedForDeletion = true;
        queued.Dates[PropertyDateType.Lease] = DateTime.UtcNow.AddDays(3);
        var house = StoreProperty(PropertyType.House, PropertyLayoutType.SmallHouseStyle1);
        house.IsPubliclyAccessible = true;
        var city = StoreProperty(PropertyType.City, PropertyLayoutType.City, parentId: CityAreaResref);
        city.Dates[PropertyDateType.Upkeep] = DateTime.UtcNow.AddDays(5);
        city.Dates[PropertyDateType.ElectionStart] = DateTime.UtcNow.AddDays(5);
        city.Upgrades[PropertyUpgradeType.CityLevel] = 1;
        var permission = Permit(PlayerId, current.Id);
        World.AddArea(CityArea, CityAreaResref);

        Service.OnModuleLoad();

        Db.Deleted.Should().Contain(new[] { expired.Id, queued.Id }).And.NotContain(current.Id);
        expired.IsQueuedForDeletion.Should().BeTrue("the lease was marked before the sweep");
        permission.Permissions.Should().NotBeEmpty("the permissions were refreshed");
        city.Dates.Should().ContainKey(PropertyDateType.BelowRequiredCitizens, "the city was checked for its citizens");
        Service.GetPropertyLoadState(house.Id).Should().Be(PropertyLoadState.Queued, "the properties were loaded last");
        Service.GetPropertyId(World.Areas.GetAreaByResref(CityAreaResref)).Should().Be(city.Id);
    }

    [Test]
    public void A_property_that_cannot_be_deleted_at_boot_does_not_stop_the_others()
    {
        var broken = World.StoreApartment();
        broken.IsQueuedForDeletion = true;
        broken.Dates[PropertyDateType.Lease] = DateTime.UtcNow.AddDays(3);
        broken.ChildPropertyIds = null!;
        var fine = World.StoreApartment();
        fine.IsQueuedForDeletion = true;
        fine.Dates[PropertyDateType.Lease] = DateTime.UtcNow.AddDays(3);

        Service.OnModuleLoad();

        Db.Deleted.Should().Contain(fine.Id).And.NotContain(broken.Id, "it stays queued for the next boot");
    }

    [Test]
    public void Permissions_are_refreshed_against_the_list_of_their_property_type()
    {
        var apartment = World.StoreApartment();
        apartment.Dates[PropertyDateType.Lease] = DateTime.UtcNow.AddDays(3);
        var owner = Permit(PropertyWorld.OwnerId, apartment.Id, PropertyPermissionType.EditStructures);
        owner.Permissions[PropertyPermissionType.AccessStorage] = true;
        owner.GrantPermissions[PropertyPermissionType.AccessStorage] = true;
        var guest = Permit(PlayerId, apartment.Id);
        var upToDate = Permit(OtherPlayerId, apartment.Id);
        foreach (var type in World.Catalog.GetPermissionsByPropertyType(PropertyType.Apartment))
        {
            upToDate.Permissions[type] = false;
            upToDate.GrantPermissions[type] = false;
        }

        Service.OnModuleLoad();

        var master = World.Catalog.GetPermissionsByPropertyType(PropertyType.Apartment);
        owner.Permissions.Keys.Should().BeEquivalentTo(master, "AccessStorage is not an apartment permission any more");
        owner.GrantPermissions.Keys.Should().BeEquivalentTo(master);
        owner.Permissions.Values.Should().OnlyContain(granted => granted, "the owner is given the permissions that were added");
        guest.Permissions.Values.Should().OnlyContain(granted => !granted, "anyone else is given them switched off");
        guest.GrantPermissions.Values.Should().OnlyContain(granted => !granted);
        Db.Saved.Should().Contain(owner).And.Contain(guest).And.NotContain(upToDate, "a record that needed no change is not saved");
    }

    [Test]
    public void The_boot_clears_where_a_starship_was_and_returns_it_to_a_safe_dock()
    {
        var ship = StoreProperty(PropertyType.Starship, PropertyLayoutType.LightFreighter1);
        ship.Positions[PropertyLocationType.CurrentPosition] = new PropertyLocation { AreaResref = "somewhere" };
        ship.Positions[PropertyLocationType.DockPosition] = new PropertyLocation { AreaResref = "viscara_port" };
        var untouched = StoreProperty(PropertyType.Starship, PropertyLayoutType.LightFreighter1);
        untouched.Positions[PropertyLocationType.DockPosition] = new PropertyLocation { AreaResref = "viscara_port" };
        World.AddArea(8200, "viscara_port");

        Service.OnModuleLoad();

        ship.Positions.Should().NotContainKey(PropertyLocationType.CurrentPosition);
        Db.Saved.Should().Contain(ship).And.Contain(untouched);
    }

    // ---- the starship dock

    private (WorldProperty Ship, uint ShipOnlyArea) DockedShip(string dockAreaResref, string instancePropertyId = "")
    {
        var (_, _, _) = AddPlanetWithStarport(PlanetType.Viscara, "viscara_port", 8200, 9001, 9002);
        var shipOnly = World.AddArea(8400, "station");
        World.Engine.SetLocalBool(shipOnly, StarshipDockReturn.ShipOnlyLocationVariable, true);
        var ship = StoreProperty(PropertyType.Starship, PropertyLayoutType.LightFreighter1);
        ship.CustomName = "Ship";
        ship.Positions[PropertyLocationType.DockPosition] = new PropertyLocation { AreaResref = dockAreaResref, InstancePropertyId = instancePropertyId };
        ship.Positions[PropertyLocationType.LastNPCDockPosition] = new PropertyLocation { AreaResref = "viscara_port", X = 1 };
        ship.Positions[PropertyLocationType.LastNPCSpacePosition] = new PropertyLocation { AreaResref = "viscara_port_orbit", X = 2 };
        return (ship, shipOnly);
    }

    [Test]
    public void A_ship_docked_at_a_regular_starport_or_an_existing_player_starport_is_left_alone()
    {
        var (regular, _) = DockedShip("viscara_port");
        var starport = StoreProperty(PropertyType.Starport, PropertyLayoutType.StarportStyle1);
        var (inPlayerStarport, _) = DockedShip(string.Empty, starport.Id);

        Service.ReturnStarshipToSafeDock(regular).Should().Be(StarshipDockReturnResult.Unchanged);
        Service.ReturnStarshipToSafeDock(inPlayerStarport).Should().Be(StarshipDockReturnResult.Unchanged);
    }

    [Test]
    public void A_ship_docked_at_a_player_starport_that_is_gone_or_being_retrieved_returns_to_its_safe_dock()
    {
        var (gone, _) = DockedShip(string.Empty, "vanished-starport");
        var starport = StoreProperty(PropertyType.Starport, PropertyLayoutType.StarportStyle1);
        var (retrieved, _) = DockedShip(string.Empty, starport.Id);

        Service.ReturnStarshipToSafeDock(gone).Should().Be(StarshipDockReturnResult.ReturnedFromMissingStarport);
        Service.ReturnStarshipToSafeDock(retrieved, starport.Id).Should().Be(StarshipDockReturnResult.ReturnedFromMissingStarport);

        gone.Positions[PropertyLocationType.DockPosition].AreaResref.Should().Be("viscara_port");
        gone.Positions[PropertyLocationType.DockPosition].InstancePropertyId.Should().BeEmpty();
        retrieved.Positions[PropertyLocationType.DockPosition].AreaResref.Should().Be("viscara_port");
    }

    [Test]
    public void A_ship_with_no_dock_gets_its_safe_dock()
    {
        var (ship, _) = DockedShip("viscara_port");
        ship.Positions.Remove(PropertyLocationType.DockPosition);

        Service.ReturnStarshipToSafeDock(ship).Should().Be(StarshipDockReturnResult.RestoredMissingDock);

        ship.Positions[PropertyLocationType.DockPosition].AreaResref.Should().Be("viscara_port");
    }

    [Test]
    public void A_ship_left_in_a_ship_only_location_returns_to_its_safe_dock()
    {
        var (ship, _) = DockedShip("station");

        Service.ReturnStarshipToSafeDock(ship).Should().Be(StarshipDockReturnResult.ReturnedFromShipOnlyLocation);

        ship.Positions[PropertyLocationType.DockPosition].AreaResref.Should().Be("viscara_port");
    }

    [Test]
    public void A_ship_without_a_usable_safe_dock_goes_to_the_starport_of_the_planet_it_is_on()
    {
        var (ship, _) = DockedShip("viscara_port", "vanished-starport");
        ship.Positions.Remove(PropertyLocationType.LastNPCDockPosition);
        ship.Positions.Remove(PropertyLocationType.LastNPCSpacePosition);

        Service.ReturnStarshipToSafeDock(ship, fallbackAreaResref: "viscara_port").Should().Be(StarshipDockReturnResult.ReturnedToPlanetStarport);

        ship.Positions[PropertyLocationType.DockPosition].AreaResref.Should().Be("viscara_port");
        ship.Positions[PropertyLocationType.SpacePosition].AreaResref.Should().Be("viscara_port_orbit");
    }

    [Test]
    public void A_ship_that_has_nowhere_safe_to_go_is_reported()
    {
        var (ship, _) = DockedShip("station");
        ship.Positions.Remove(PropertyLocationType.LastNPCDockPosition);
        ship.Positions.Remove(PropertyLocationType.LastNPCSpacePosition);
        World.Planets.Types.Clear();

        Service.ReturnStarshipToSafeDock(ship).Should().Be(StarshipDockReturnResult.NoSafeDock);
    }

    [Test]
    public void A_safe_dock_recorded_in_a_ship_only_location_is_replaced_by_the_starport_of_its_planet()
    {
        var (ship, shipOnly) = DockedShip("viscara_port");
        ship.Positions[PropertyLocationType.LastNPCDockPosition] = new PropertyLocation { AreaResref = "station" };
        World.Planets.Types[shipOnly] = PlanetType.Viscara;

        Service.ReturnStarshipToSafeDock(ship).Should().Be(StarshipDockReturnResult.Unchanged);

        ship.Positions[PropertyLocationType.LastNPCDockPosition].AreaResref.Should().Be("viscara_port");
        ship.Positions[PropertyLocationType.LastNPCSpacePosition].AreaResref.Should().Be("viscara_port_orbit");
    }

    [Test]
    public void A_planet_whose_waypoints_are_missing_has_no_starport_to_return_to()
    {
        var (ship, _) = DockedShip("station");
        ship.Positions.Remove(PropertyLocationType.LastNPCDockPosition);
        ship.Positions.Remove(PropertyLocationType.LastNPCSpacePosition);
        World.Planets.Types[World.Areas.GetAreaByResref("station")] = PlanetType.Viscara;
        World.Engine.SetReturnHandler("GetWaypointByTag", _ => PropertyWorld.Invalid);

        Service.ReturnStarshipToSafeDock(ship).Should().Be(StarshipDockReturnResult.NoSafeDock);
    }

    // ---- stored location

    [Test]
    public void The_original_location_of_a_player_is_stored_and_used_to_jump_back()
    {
        var area = World.AddArea(8100, "somewhere");
        World.SetArea(Player, area);
        World.Place(Player, new Vector3(1, 2, 3), 45f);

        Service.StoreOriginalLocation(Player);

        World.Locals.Has(Player, "PROPERTY_STORED_LOCATION_X").Should().BeTrue();
        World.ClearCalls();

        Service.JumpToOriginalLocation(Player);

        var jump = World.Engine.CallsTo("AssignCommand").Should().ContainSingle().Subject;
        jump.Arguments[0].Should().Be(Player);
        ((Action)jump.Arguments[1]!)();
        var jumped = World.Engine.CallsTo("ActionJumpToLocation").Should().ContainSingle().Subject;
        World.Describe((SWLOR.NWN.API.Engine.Location)jumped.Arguments[0]!).Should().Be((area, new Vector3(1, 2, 3), 45f));
        World.Locals.Has(Player, "PROPERTY_STORED_LOCATION_X").Should().BeFalse("the stored location is forgotten once used");
        World.Locals.Has(Player, "PROPERTY_STORED_LOCATION_AREA").Should().BeFalse();
    }
}
