using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Domain.World;

/// <summary>
/// Unit tests of entering properties, the terminal and feat menus, and placing structures: who may do what, what the
/// player is told, and what is created when the answer is yes.
/// </summary>
[TestFixture]
public class PropertyServiceEntryTests : PropertyServiceTestBase
{
    private const uint Door = 7500;
    private const uint CityArea = 8000;

    /// <summary>A permission record that names every permission of a property type, granting only some of them.</summary>
    private WorldPropertyPermission PermitOnly(string playerId, string propertyId, PropertyType type, params PropertyPermissionType[] granted)
    {
        var permission = new WorldPropertyPermission { PlayerId = playerId, PropertyId = propertyId };
        foreach (var each in World.Catalog.GetPermissionsByPropertyType(type))
        {
            permission.Permissions[each] = granted.Contains(each);
            permission.GrantPermissions[each] = granted.Contains(each);
        }

        Db.With(permission);
        return permission;
    }

    private uint Loaded(WorldProperty property)
    {
        var area = World.NewObject();
        Service.RegisterInstance(property.Id, area, property.Layout);
        World.Loader.SetPropertyLoadState(property.Id, PropertyLoadState.Loaded);
        return area;
    }

    /// <summary>Runs the action the code assigned to the player and returns the location the player was sent to.</summary>
    private (uint Area, Vector3 Position, float Facing) RunAssignedJump()
    {
        var assigned = World.Engine.CallsTo("AssignCommand").Should().ContainSingle().Subject;
        assigned.Arguments[0].Should().Be(Player);
        ((Action)assigned.Arguments[1]!)();
        var jump = World.Engine.Calls.Single(call => call.Member is "JumpToLocation" or "ActionJumpToLocation");
        return World.Describe((Location)jump.Arguments[0]!);
    }

    // ---- entering a property

    [Test]
    public void Entering_a_property_needs_the_permission_to_enter_it()
    {
        var apartment = World.StoreApartment();
        PermitOnly(PlayerId, apartment.Id, PropertyType.Apartment, PropertyPermissionType.EditStructures);

        Service.EnterProperty(Player, apartment.Id);

        World.FloatingTextTo(Player).Should().Equal("You do not have permission to access that property.");
        World.Engine.CallsTo("AssignCommand").Should().BeEmpty();
    }

    [Test]
    public void Entering_a_property_that_is_no_longer_stored_tells_the_player_it_could_not_be_loaded()
    {
        Permit(PlayerId, "gone", PropertyPermissionType.EnterProperty);

        Service.EnterProperty(Player, "gone");

        World.MessagesTo(Player).Should().Equal("This property could not be loaded. Please notify staff.");
    }

    [Test]
    public void Entering_an_unloaded_property_queues_it_and_asks_the_player_to_wait()
    {
        var apartment = World.StoreApartment();
        Permit(PlayerId, apartment.Id, PropertyPermissionType.EnterProperty);

        Service.EnterProperty(Player, apartment.Id);

        World.MessagesTo(Player).Should().Equal("This property is still loading. Please try again shortly.");
        World.Engine.CallsTo("AssignCommand").Should().BeEmpty();
        World.Loader.Scheduled.Should().ContainSingle();
    }

    [Test]
    public void Entering_a_loaded_property_remembers_where_the_player_was_and_jumps_to_the_entrance()
    {
        var apartment = World.StoreApartment();
        Permit(PlayerId, apartment.Id, PropertyPermissionType.EnterProperty);
        var instance = Loaded(apartment);
        var outside = World.AddArea(8100, "outside");
        World.SetArea(Player, outside);
        World.Place(Player, new Vector3(9, 8, 7), 15f);

        Service.EnterProperty(Player, apartment.Id);

        World.Locals.Object(Player, "PROPERTY_STORED_LOCATION_AREA").Should().Be(outside);
        World.Locals.Has(Player, "PROPERTY_STORED_LOCATION_FACING").Should().BeTrue();
        var (area, _, _) = RunAssignedJump();
        area.Should().Be(instance);
    }

    [Test]
    public void A_dm_can_enter_any_property()
    {
        var dm = World.AddDm(6001);
        var apartment = World.StoreApartment();
        Loaded(apartment);

        Service.EnterProperty(dm, apartment.Id);

        World.Engine.CallsTo("AssignCommand").Should().ContainSingle().Which.Arguments[0].Should().Be(dm);
    }

    // ---- entering a building

    private (WorldProperty Building, WorldProperty Interior) StoreBuilding(bool isPublic, PropertyType type = PropertyType.CityHall, PropertyLayoutType layout = PropertyLayoutType.CityHallStyle1)
    {
        var building = StoreStructure("a-city", StructureType.CityHall);
        var interior = StoreProperty(type, layout, parentId: building.Id);
        interior.IsPubliclyAccessible = isPublic;
        building.ChildPropertyIds[PropertyChildType.Interior] = new List<string> { interior.Id };
        World.SetResRef(Door, "door");
        World.Valid(Door);
        Service.AssignPropertyId(Door, building.Id);
        World.Engine.SetReturn("OBJECT_SELF", Door);
        return (building, interior);
    }

    [Test]
    public void A_building_that_is_not_ready_says_so()
    {
        World.Valid(Door);
        World.Engine.SetReturn("OBJECT_SELF", Door);
        Service.AssignPropertyId(Door, "not-stored");
        Service.EnterBuilding();

        var noInterior = StoreStructure("a-city");
        Service.AssignPropertyId(Door, noInterior.Id);
        Service.EnterBuilding();

        var twoInteriors = StoreStructure("a-city");
        twoInteriors.ChildPropertyIds[PropertyChildType.Interior] = new List<string> { "one", "two" };
        Service.AssignPropertyId(Door, twoInteriors.Id);
        Service.EnterBuilding();

        World.MessagesTo(Player).Should().Equal(Enumerable.Repeat("This building is not ready yet. Please try again shortly.", 3));
    }

    [Test]
    public void A_building_whose_interior_is_missing_tells_the_player_it_could_not_be_loaded()
    {
        var (_, interior) = StoreBuilding(isPublic: true);
        Db.Delete<WorldProperty>(interior.Id);

        Service.EnterBuilding();

        World.MessagesTo(Player).Should().Equal("This property could not be loaded. Please notify staff.");
    }

    [Test]
    public void A_public_building_can_be_entered_once_its_interior_is_loaded()
    {
        var (_, interior) = StoreBuilding(isPublic: true);
        var instance = Loaded(interior);

        Service.EnterBuilding();

        var (area, _, _) = RunAssignedJump();
        area.Should().Be(instance);
        World.Locals.Has(Player, "PROPERTY_STORED_LOCATION_X").Should().BeTrue();
    }

    [Test]
    public void A_public_building_whose_interior_is_still_loading_makes_the_player_wait()
    {
        StoreBuilding(isPublic: true);

        Service.EnterBuilding();

        World.MessagesTo(Player).Should().Equal("This property is still loading. Please try again shortly.");
        World.Engine.CallsTo("AssignCommand").Should().BeEmpty();
    }

    [Test]
    public void A_private_building_needs_the_permission_to_enter()
    {
        var (_, interior) = StoreBuilding(isPublic: false, PropertyType.House, PropertyLayoutType.SmallHouseStyle1);
        Loaded(interior);

        Service.EnterBuilding();
        World.MessagesTo(Player).Should().Equal("You do not have permission to enter.");
        World.Engine.CallsTo("AssignCommand").Should().BeEmpty();

        PermitOnly(PlayerId, interior.Id, PropertyType.House, PropertyPermissionType.EnterProperty);
        World.ClearCalls();
        Service.EnterBuilding();

        World.Engine.CallsTo("AssignCommand").Should().ContainSingle();
    }

    // ---- previewing a layout

    [Test]
    public void A_layout_can_be_previewed_in_its_template_area()
    {
        using var world = new PropertyWorld(cacheDefinitions: false);
        world.AddPlayer(Player, PlayerId);
        var template = world.AddArea(5000, "apartment_002");
        world.Engine.SetReturnSequence("GetFirstArea", template);
        world.Engine.SetReturnSequence("GetNextArea", PropertyWorld.Invalid);
        world.Catalog.CacheData();
        var outside = world.AddArea(8100, "outside");
        world.SetArea(Player, outside);

        world.Service.PreviewProperty(Player, PropertyLayoutType.ApartmentSmallStyle1);

        world.Locals.Object(Player, "PROPERTY_STORED_LOCATION_AREA").Should().Be(outside);
        var assigned = world.Engine.CallsTo("AssignCommand").Should().ContainSingle().Subject;
        ((Action)assigned.Arguments[1]!)();
        var jump = world.Engine.CallsTo("JumpToLocation").Should().ContainSingle().Subject;
        world.Describe((Location)jump.Arguments[0]!).Area.Should().Be(template);
    }

    // ---- terminals and menus

    private WorldProperty StoreCityHallInterior(out WorldProperty building, out uint hallArea, string cityId = "city-1")
    {
        building = StoreStructure(cityId, StructureType.CityHall);
        var interior = StoreProperty(PropertyType.CityHall, PropertyLayoutType.CityHallStyle1, parentId: building.Id);
        hallArea = World.AddArea(8500, "city_hall_instance");
        Service.AssignPropertyId(hallArea, interior.Id);
        World.SetArea(Terminal, hallArea);
        World.Engine.SetReturn("OBJECT_SELF", Terminal);
        return interior;
    }

    [Test]
    public void The_apartment_terminal_opens_the_apartment_window_tethered_to_itself()
    {
        World.Engine.SetReturn("OBJECT_SELF", Terminal);

        Service.StartApartmentConversation();

        Service.Windows.Should().Equal((Player, GuiWindowType.ManageApartment, Terminal));
    }

    [Test]
    public void Only_players_may_use_the_citizenship_terminal()
    {
        var dm = World.AddDm(6001);
        var possessing = World.AddDm(6002, "dm-2", possessing: true);
        var creature = World.NewObject();

        foreach (var user in new[] { dm, possessing, creature })
        {
            World.Engine.SetReturn("GetLastUsedBy", user);
            Service.OpenCitizenshipMenu();

            World.MessagesTo(user).Should().Equal("Only players may use this terminal.");
        }

        Service.Windows.Should().BeEmpty();
    }

    [Test]
    public void A_citizen_of_another_city_must_revoke_their_citizenship_before_using_the_terminal()
    {
        StoreCityHallInterior(out _, out _);
        StorePlayer(PlayerId, "another-city");

        Service.OpenCitizenshipMenu();

        World.MessagesTo(Player).Should().ContainSingle().Which.Should().Contain("You are a citizen of another city. You cannot access this terminal unless you revoke your citizenship first.");
        Service.Windows.Should().BeEmpty();
    }

    [TestCase("")]
    [TestCase("00000000-0000-0000-0000-000000000000")]
    [TestCase("city-1")]
    public void Anyone_without_a_citizenship_or_with_this_citys_may_use_the_citizenship_terminal(string citizenOf)
    {
        StoreCityHallInterior(out _, out _);
        StorePlayer(PlayerId, citizenOf);

        Service.OpenCitizenshipMenu();

        Service.Windows.Should().Equal((Player, GuiWindowType.ManageCitizenship, Terminal));
        World.MessagesTo(Player).Should().BeEmpty();
    }

    [Test]
    public void The_city_management_terminal_needs_a_city_permission()
    {
        StoreCityHallInterior(out _, out _);

        Service.OpenCityManagementMenu();
        World.MessagesTo(Player).Should().ContainSingle().Which.Should().Contain("You do not have permission to access this terminal.");

        PermitOnly(PlayerId, "city-1", PropertyType.City, PropertyPermissionType.EditStructures);
        Service.OpenCityManagementMenu();
        World.MessagesTo(Player).Should().HaveCount(2, "a permission that has nothing to do with running the city does not open it");
        Service.Windows.Should().BeEmpty();
    }

    [TestCase(PropertyPermissionType.RenameProperty)]
    [TestCase(PropertyPermissionType.EditTaxes)]
    [TestCase(PropertyPermissionType.AccessTreasury)]
    [TestCase(PropertyPermissionType.ManageUpgrades)]
    [TestCase(PropertyPermissionType.ManageUpkeep)]
    public void Any_one_of_the_city_management_permissions_opens_the_city_management_window(PropertyPermissionType granted)
    {
        StoreCityHallInterior(out _, out _);
        PermitOnly(PlayerId, "city-1", PropertyType.City, granted);

        Service.OpenCityManagementMenu();

        Service.Windows.Should().Equal((Player, GuiWindowType.ManageCity, Terminal));
    }

    // ---- the property menu feat

    private void UseFeat(FeatType feat) => World.EventData["FEAT_ID"] = ((int)feat).ToString();

    [Test]
    public void Other_feats_do_not_open_the_property_menu()
    {
        UseFeat(FeatType.PropertyMenu + 1);

        Service.PropertyMenu();

        Service.Windows.Should().BeEmpty();
        World.Engine.Calls.Should().NotContain(call => call.Member == "FloatingTextStringOnCreature");
    }

    [Test]
    public void The_property_menu_closes_when_it_is_already_open()
    {
        UseFeat(FeatType.PropertyMenu);
        World.Engine.SetReturn("NuiFindWindow", 4);

        Service.PropertyMenu();

        Service.Windows.Should().Equal((Player, GuiWindowType.ManageStructures, PropertyWorld.Invalid));
    }

    [Test]
    public void The_property_menu_only_opens_inside_a_player_property()
    {
        UseFeat(FeatType.PropertyMenu);
        World.SetArea(Player, World.AddArea(8100, "wilderness"));

        Service.PropertyMenu();

        World.FloatingTextTo(Player).Should().Equal("This menu can only be accessed within player properties.");
    }

    [Test]
    public void The_property_menu_needs_a_permission_over_structures_or_storage()
    {
        UseFeat(FeatType.PropertyMenu);
        var apartment = World.StoreApartment();
        InsideProperty(apartment);

        Service.PropertyMenu();
        World.FloatingTextTo(Player).Should().Equal("You do not have permission to access this property.");

        PermitOnly(PlayerId, apartment.Id, PropertyType.Apartment, PropertyPermissionType.RenameProperty);
        Service.PropertyMenu();
        World.FloatingTextTo(Player).Should().HaveCount(2);
        Service.Windows.Should().BeEmpty();
    }

    [TestCase(PropertyPermissionType.EditStructures)]
    [TestCase(PropertyPermissionType.RetrieveStructures)]
    public void A_permission_over_structures_opens_the_property_menu(PropertyPermissionType granted)
    {
        UseFeat(FeatType.PropertyMenu);
        var apartment = World.StoreApartment();
        InsideProperty(apartment);
        PermitOnly(PlayerId, apartment.Id, PropertyType.Apartment, granted);

        Service.PropertyMenu();

        Service.Windows.Should().Equal((Player, GuiWindowType.ManageStructures, PropertyWorld.Invalid));
    }

    [Test]
    public void A_permission_over_the_storage_of_the_property_opens_the_property_menu()
    {
        UseFeat(FeatType.PropertyMenu);
        var apartment = World.StoreApartment();
        InsideProperty(apartment);
        PermitOnly(PlayerId, apartment.Id, PropertyType.Apartment);
        var category = StoreCategory(apartment.Id);
        Permit(PlayerId, category.Id, PropertyPermissionType.AccessStorage);

        Service.PropertyMenu();

        Service.Windows.Should().ContainSingle().Which.Type.Should().Be(GuiWindowType.ManageStructures);
    }

    // ---- placing structures

    private static string ResrefOf(StructureType type) => $"structure_{(int)type:D4}";

    private void UseItem(StructureType type, string resref = null)
    {
        World.Valid(Item);
        World.SetResRef(Item, resref ?? ResrefOf(type));
        World.EventData["ITEM_OBJECT_ID"] = Item.ToString("x");
        World.EventData["TARGET_POSITION_X"] = "10";
        World.EventData["TARGET_POSITION_Y"] = "20";
        World.EventData["TARGET_POSITION_Z"] = "30";
    }

    private WorldProperty ApartmentToBuildIn(out uint area, bool permitted = true)
    {
        var apartment = World.StoreApartment();
        area = InsideProperty(apartment);
        if (permitted)
            PermitOnly(PlayerId, apartment.Id, PropertyType.Apartment, PropertyPermissionType.EditStructures);
        return apartment;
    }

    [Test]
    public void An_item_that_is_not_a_structure_is_left_to_the_game()
    {
        UseItem(StructureType.Jukebox, "nw_wswdg001");

        Service.PlaceStructure();

        World.Events.CallsTo("SkipEvent").Should().BeEmpty();
        Db.Store.Saved.Should().BeEmpty();
    }

    [TestCase("structure_0005", StructureType.Jukebox)]
    [TestCase("structure_5003", StructureType.Starport)]
    [TestCase("structure_0000", StructureType.Invalid)]
    [TestCase("structure_abcd", StructureType.Invalid)]
    [TestCase("crate_0005", StructureType.Invalid)]
    public void The_structure_type_of_an_item_is_the_number_its_resref_ends_with(string resref, StructureType expected)
    {
        World.SetResRef(Item, resref);

        Service.GetStructureTypeFromItem(Item).Should().Be(expected);
    }

    [Test]
    public void A_structure_item_with_no_structure_type_is_swallowed_without_a_word()
    {
        UseItem(StructureType.Invalid, "structure_0000");

        Service.PlaceStructure();

        World.Events.CallsTo("SkipEvent").Should().ContainSingle();
        World.FloatingTextTo(Player).Should().BeEmpty();
        World.MessagesTo(Player).Should().BeEmpty();
    }

    [Test]
    public void A_city_hall_cannot_be_placed_where_cities_may_not_be_founded()
    {
        var area = World.AddArea(CityArea, "wilderness");
        World.SetArea(Player, area);
        UseItem(StructureType.CityHall);

        Service.PlaceStructure();

        World.FloatingTextTo(Player).Should().Equal("Cities cannot be founded here.");
        World.Locals.Has(Player, "PROPERTY_CITY_HALL_ITEM").Should().BeFalse();
    }

    [Test]
    public void A_city_hall_opens_the_founding_conversation_with_the_item_and_position_remembered()
    {
        var area = World.AddArea(CityArea, "wilderness");
        World.SetArea(Player, area);
        World.Engine.SetLocalBool(area, "IS_BUILDABLE", true);
        UseItem(StructureType.CityHall);

        Service.PlaceStructure();

        World.Locals.Object(Player, "PROPERTY_CITY_HALL_ITEM").Should().Be(Item);
        World.Locals.Has(Player, "PROPERTY_CITY_HALL_X").Should().BeTrue();
        World.Engine.CallsTo("SetLocalFloat").Where(call => (string)call.Arguments[1]! == "PROPERTY_CITY_HALL_Z")
            .Should().ContainSingle().Which.Arguments[2].Should().Be(30f);
        Db.Store.Saved.Should().BeEmpty("the founding happens when the player confirms");
    }

    [Test]
    public void Structures_may_only_be_placed_inside_player_properties()
    {
        World.SetArea(Player, World.AddArea(8100, "wilderness"));
        UseItem(StructureType.Jukebox);

        Service.PlaceStructure();

        World.FloatingTextTo(Player).Should().Equal("Structures may only be placed within player properties.");
    }

    [Test]
    public void Placing_a_structure_needs_the_permission_to_edit_structures()
    {
        var apartment = ApartmentToBuildIn(out _, permitted: false);
        UseItem(StructureType.Jukebox);

        Service.PlaceStructure();
        PermitOnly(PlayerId, apartment.Id, PropertyType.Apartment, PropertyPermissionType.RetrieveStructures);
        Service.PlaceStructure();

        World.FloatingTextTo(Player).Should().Equal(
            "You do not have permission to place structures within this property.",
            "You do not have permission to place structures within this property.");
        Db.Store.Saved.OfType<WorldProperty>().Should().BeEmpty();
    }

    [Test]
    public void A_structure_is_placed_in_the_property_and_the_player_sees_how_many_fit()
    {
        var apartment = ApartmentToBuildIn(out var area);
        UseItem(StructureType.Jukebox);

        Service.PlaceStructure();

        World.Events.CallsTo("SkipEvent").Should().ContainSingle();
        var structure = Db.Store.Saved.OfType<WorldProperty>().Distinct().Single(x => x.PropertyType == PropertyType.Structure);
        structure.StructureType.Should().Be(StructureType.Jukebox);
        structure.ParentPropertyId.Should().Be(apartment.Id);
        var position = structure.Positions[PropertyLocationType.StaticPosition];
        (position.AreaResref, position.X, position.Y, position.Z).Should().Be(("inside_" + area, 10f, 20f, 30f));
        var limit = World.Catalog.GetLayoutByType(apartment.Layout).StructureLimit;
        World.MessagesTo(Player).Should().Equal($"Structure Limit: 1 / {limit}");
    }

    [Test]
    public void A_structure_is_refused_when_the_property_is_full()
    {
        var apartment = ApartmentToBuildIn(out _);
        World.Catalog.GetLayoutByType(apartment.Layout).StructureLimit = 2;
        StoreStructure(apartment.Id);
        StoreStructure(apartment.Id);
        UseItem(StructureType.Jukebox);

        Service.PlaceStructure();

        World.FloatingTextTo(Player).Should().Equal("Structure limit reached for this property.");
        Db.Store.Saved.Should().BeEmpty();
    }

    [Test]
    public void Research_devices_have_a_limit_of_their_own()
    {
        var apartment = ApartmentToBuildIn(out _);
        World.Catalog.GetStructureByType(StructureType.Jukebox).Category = StructureCategoryType.ResearchDevice;
        World.Catalog.GetLayoutByType(apartment.Layout).ResearchDeviceLimit = 1;
        UseItem(StructureType.Jukebox);

        Service.PlaceStructure();
        World.MessagesTo(Player).Should().Equal("Research Device Limit: 1 / 1");

        World.ClearCalls();
        Service.PlaceStructure();
        World.FloatingTextTo(Player).Should().Equal("Research Device limit reached for this property.");
    }

    [Test]
    public void A_structure_that_is_neither_furniture_nor_a_research_device_is_reported_to_the_player()
    {
        ApartmentToBuildIn(out _);
        World.Catalog.GetStructureByType(StructureType.Jukebox).Category = StructureCategoryType.Invalid;
        UseItem(StructureType.Jukebox);

        Service.PlaceStructure();

        World.FloatingTextTo(Player).Should().Equal("Unable to place structure in this property. Notify an admin.");
    }

    [Test]
    public void A_structure_cannot_be_placed_in_a_type_of_property_it_is_restricted_from()
    {
        ApartmentToBuildIn(out _);
        World.Catalog.GetStructureByType(StructureType.Jukebox).RestrictedPropertyTypes = PropertyType.City;
        UseItem(StructureType.Jukebox);

        Service.PlaceStructure();

        World.FloatingTextTo(Player).Should().Equal("This Structure cannot be placed within this type of property.");
        Db.Store.Saved.Should().BeEmpty();
    }

    private WorldProperty CityToBuildIn(out uint area)
    {
        var city = StoreProperty(PropertyType.City, PropertyLayoutType.City, parentId: "city_area");
        area = InsideProperty(city);
        PermitOnly(PlayerId, city.Id, PropertyType.City, PropertyPermissionType.EditStructures);
        return city;
    }

    [Test]
    public void A_building_is_placed_in_a_city_and_counted_against_the_building_limit()
    {
        var city = CityToBuildIn(out _);
        UseItem(StructureType.SmallHouseStyle1);

        Service.PlaceStructure();

        var limit = World.Catalog.GetLayoutByType(city.Layout).BuildingLimit;
        World.MessagesTo(Player).Should().Equal($"Building Limit: 1 / {limit}");
        SavedOfType(PropertyType.Structure).StructureType.Should().Be(StructureType.SmallHouseStyle1);
        SavedOfType(PropertyType.House).ParentPropertyId.Should().Be(SavedOfType(PropertyType.Structure).Id, "the interior is created with the building");
    }

    [Test]
    public void The_buildings_of_a_city_are_counted_apart_from_its_other_structures()
    {
        var city = CityToBuildIn(out _);
        World.Catalog.GetLayoutByType(city.Layout).BuildingLimit = 1;
        World.Catalog.GetLayoutByType(city.Layout).StructureLimit = 1;
        StoreStructure(city.Id, StructureType.SmallHouseStyle1);
        UseItem(StructureType.SmallHouseStyle1);

        Service.PlaceStructure();
        World.FloatingTextTo(Player).Should().Equal("Building limit reached for this property.");

        World.ClearCalls();
        UseItem(StructureType.Jukebox);
        Service.PlaceStructure();

        World.MessagesTo(Player).Should().Equal("Structure Limit: 1 / 1");
    }

    private WorldProperty SavedOfType(PropertyType type) =>
        Db.Store.Saved.OfType<WorldProperty>().Distinct().Single(x => x.PropertyType == type);
}
