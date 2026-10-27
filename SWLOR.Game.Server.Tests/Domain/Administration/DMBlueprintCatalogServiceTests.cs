using System.Numerics;
using System.Text.Json.Nodes;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Service.DMToolService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;
using static SWLOR.Game.Server.Tests.Support.DMPossessionEngine;

namespace SWLOR.Game.Server.Tests.Domain.Administration;

/// <summary>
/// Unit tests of the DM blueprint catalog instance. The engine, its JSON values and templates (<see cref="FakeJsonEngine"/>)
/// and the resource listing of the NWNX utility plugin are replaced by fakes, so palettes are read from templates
/// registered by the test.
/// </summary>
[TestFixture]
public class DMBlueprintCatalogServiceTests
{
    private const uint DM = 1;
    private const uint Created = 300;

    private const string CreaturePalette = """
        {"__data_type":"ITP ","MAIN":{"type":"list","value":[
          {"__struct_id":0,"NAME":{"type":"cexostring","value":"NPCs"},"LIST":{"type":"list","value":[
            {"__struct_id":0,"ID":{"type":"byte","value":44},"STRREF":{"type":"dword","value":35},"LIST":{"type":"list","value":[
              {"__struct_id":0,"RESREF":{"type":"resref","value":"guard"},"NAME":{"type":"cexostring","value":"Town Guard"}}
            ]}}
          ]}}
        ]}}
        """;

    private DMPossessionEngine _world = null!;
    private FakeJsonEngine _json = null!;
    private FakeUtilPlugin _util = null!;
    private FakeObjectPlugin _objectPlugin = null!;
    private IDisposable _utilScope = null!;
    private IDisposable _objectScope = null!;
    private DMBlueprintCatalogService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _world = new DMPossessionEngine();
        _json = new FakeJsonEngine(_world.Engine);
        _util = new FakeUtilPlugin();
        _util.SetReturnHandler("StripColors", arguments => arguments[0]);
        _objectPlugin = new FakeObjectPlugin();
        _utilScope = UtilPluginRuntime.Override(_util);
        _objectScope = ObjectPluginRuntime.Override(_objectPlugin);
        _world.Engine.SetReturnHandler("GetStringByStrRef", arguments => "text " + arguments[0]);
        _service = new DMBlueprintCatalogService(new DMPresenceService());
    }

    [TearDown]
    public void TearDown()
    {
        _objectScope.Dispose();
        _utilScope.Dispose();
        _world.Dispose();
    }

    private static DMBlueprint Blueprint(string resref, DMBlueprintCategory category) =>
        new(resref, "Name of " + resref, "Path", category, DMBlueprintSource.Custom);

    private static string LocString(string text) => "{\"type\":\"cexlocstring\",\"value\":{\"0\":\"" + text + "\"}}";

    // ---- palettes

    [Test]
    public void GetPalette_ReadsTheStandardPaletteFromThePalstdSkeleton()
    {
        _json.AddTemplate("creaturepalstd", ResType.ITP, CreaturePalette);

        var palette = _service.GetPalette(DMBlueprintCategory.Creatures, DMBlueprintSource.Standard);

        palette.CategoryPaths.Should().Equal("NPCs", "NPCs / text 35");
        var guard = palette.Blueprints.Should().ContainSingle().Subject;
        guard.Resref.Should().Be("guard");
        guard.Name.Should().Be("Town Guard");
        guard.Source.Should().Be(DMBlueprintSource.Standard);
        _util.CountOf("GetFirstResRef").Should().Be(0, "only the custom palette lists module blueprints no category holds");
    }

    [Test]
    public void GetPalette_ReadsThePaletteOnlyOnce()
    {
        _json.AddTemplate("creaturepalstd", ResType.ITP, CreaturePalette);

        var first = _service.GetPalette(DMBlueprintCategory.Creatures, DMBlueprintSource.Standard);
        var second = _service.GetPalette(DMBlueprintCategory.Creatures, DMBlueprintSource.Standard);

        second.Should().BeSameAs(first);
        _world.Engine.CallsTo("TemplateToJson").Should().ContainSingle();
    }

    [Test]
    public void GetPalette_KeepsEachCategoryAndSourceApart()
    {
        _json.AddTemplate("creaturepalstd", ResType.ITP, CreaturePalette);
        _json.AddTemplate("creaturepalcus", ResType.ITP, CreaturePalette);

        var standard = _service.GetPalette(DMBlueprintCategory.Creatures, DMBlueprintSource.Standard);
        var custom = _service.GetPalette(DMBlueprintCategory.Creatures, DMBlueprintSource.Custom);
        var items = _service.GetPalette(DMBlueprintCategory.Items, DMBlueprintSource.Standard);

        custom.Should().NotBeSameAs(standard);
        items.Blueprints.Should().BeEmpty("no template is registered for the item palette");
    }

    [Test]
    public void GetPalette_IsEmptyWhenThePaletteCannotBeRead()
    {
        var palette = _service.GetPalette(DMBlueprintCategory.Stores, DMBlueprintSource.Standard);

        palette.CategoryPaths.Should().BeEmpty();
        palette.Blueprints.Should().BeEmpty();
        _world.Engine.CallsTo("TemplateToJson").Should().ContainSingle().Which.Arguments[0].Should().Be("storepalstd");
    }

    [TestCase(DMBlueprintCategory.Creatures, "creaturepalcus")]
    [TestCase(DMBlueprintCategory.Placeables, "placeablepalcus")]
    [TestCase(DMBlueprintCategory.Items, "itempalcus")]
    [TestCase(DMBlueprintCategory.Stores, "storepalcus")]
    [TestCase(DMBlueprintCategory.Waypoints, "waypointpalcus")]
    [TestCase(DMBlueprintCategory.Triggers, "triggerpalcus")]
    [TestCase(DMBlueprintCategory.Encounters, "encounterpalcus")]
    public void GetPalette_NamesThePaletteSkeletonAfterTheCategory(DMBlueprintCategory category, string skeleton)
    {
        _service.GetPalette(category, DMBlueprintSource.Custom);

        _world.Engine.CallsTo("TemplateToJson").Should().ContainSingle().Which.Arguments.Should().Equal(skeleton, ResType.ITP);
    }

    [Test]
    public void GetPalette_RefusesACategoryItDoesNotKnow()
    {
        var read = () => _service.GetPalette((DMBlueprintCategory)99, DMBlueprintSource.Custom);

        read.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void GetPalette_AddsCustomBlueprintsNoCategoryHolds()
    {
        _json.AddTemplate("creaturepalcus", ResType.ITP, CreaturePalette);
        _json.AddTemplate("stray_droid", ResType.UTC,
            "{\"FirstName\":" + LocString("Stray") + ",\"LastName\":" + LocString("Droid") + "}");
        _json.AddTemplate("blank_npc", ResType.UTC, "{\"FirstName\":" + LocString("  ") + "}");
        _util.SetReturnSequence("GetFirstResRef", "guard");
        _util.SetReturnSequence("GetNextResRef", "stray_droid", "blank_npc", "unreadable", string.Empty);

        var palette = _service.GetPalette(DMBlueprintCategory.Creatures, DMBlueprintSource.Custom);

        _util.CallsTo("GetFirstResRef").Should().ContainSingle().Which.Arguments.Should().Equal(ResRefType.Creature, string.Empty, true);
        palette.CategoryPaths.Should().Equal("NPCs", "NPCs / text 35", DMBlueprintCatalogService.NotInPaletteCategory);
        palette.Blueprints.Select(blueprint => blueprint.Resref).Should().Equal("guard", "blank_npc", "stray_droid", "unreadable");
        var stray = palette.Blueprints.Single(blueprint => blueprint.Resref == "stray_droid");
        stray.Name.Should().Be("Stray Droid");
        stray.CategoryPath.Should().Be(DMBlueprintCatalogService.NotInPaletteCategory);
        stray.Source.Should().Be(DMBlueprintSource.Custom);
        palette.Blueprints.Single(blueprint => blueprint.Resref == "blank_npc").Name.Should().Be("blank_npc", "a template with no name shows its resref");
        palette.Blueprints.Single(blueprint => blueprint.Resref == "unreadable").Name.Should().Be("unreadable");
    }

    [TestCase(DMBlueprintCategory.Items, ResRefType.Item, ResType.UTI, "LocalizedName", "Spare Part")]
    [TestCase(DMBlueprintCategory.Waypoints, ResRefType.Waypoint, ResType.UTW, "LocalizedName", "Marker")]
    [TestCase(DMBlueprintCategory.Triggers, ResRefType.Trigger, ResType.UTT, "LocalizedName", "Tripwire")]
    [TestCase(DMBlueprintCategory.Encounters, ResRefType.Encounter, ResType.UTE, "LocalizedName", "Ambush")]
    [TestCase(DMBlueprintCategory.Placeables, ResRefType.Placeable, ResType.UTP, "LocName", "Crate")]
    [TestCase(DMBlueprintCategory.Stores, ResRefType.Store, ResType.UTM, "LocName", "Shop")]
    public void GetPalette_NamesUnlistedBlueprintsFromTheFieldTheirTemplateKeepsTheNameIn(
        DMBlueprintCategory category, ResRefType resRefType, ResType resType, string field, string name)
    {
        _json.AddTemplate("loose", resType, "{\"" + field + "\":" + LocString(name) + "}");
        _util.SetReturnSequence("GetFirstResRef", "loose");

        var palette = _service.GetPalette(category, DMBlueprintSource.Custom);

        _util.CallsTo("GetFirstResRef").Should().ContainSingle().Which.Arguments[0].Should().Be(resRefType);
        palette.Blueprints.Should().ContainSingle().Which.Name.Should().Be(name);
        _util.CountOf("StripColors").Should().Be(1, "colors are stripped from the name");
    }

    [Test]
    public void LoadCustomPalettes_ReadsTheCustomPaletteOfEveryCategory()
    {
        _service.LoadCustomPalettes();

        _world.Engine.CallsTo("TemplateToJson").Select(call => (string)call.Arguments[0]!).Should().BeEquivalentTo(
            "creaturepalcus", "placeablepalcus", "itempalcus", "storepalcus", "waypointpalcus", "triggerpalcus", "encounterpalcus");
    }

    // ---- templates

    [TestCase(DMBlueprintCategory.Creatures, ResType.UTC)]
    [TestCase(DMBlueprintCategory.Placeables, ResType.UTP)]
    [TestCase(DMBlueprintCategory.Items, ResType.UTI)]
    [TestCase(DMBlueprintCategory.Stores, ResType.UTM)]
    [TestCase(DMBlueprintCategory.Waypoints, ResType.UTW)]
    [TestCase(DMBlueprintCategory.Triggers, ResType.UTT)]
    [TestCase(DMBlueprintCategory.Encounters, ResType.UTE)]
    public void ReadTemplate_ReadsTheResourceTypeOfTheCategory(DMBlueprintCategory category, ResType type)
    {
        _json.AddTemplate("thing", type, "{\"Tag\":{\"type\":\"cexostring\",\"value\":\"found\"}}");

        var template = _service.ReadTemplate(Blueprint("THING", category));

        SWLOR.NWN.API.NWScript.NWScript.JsonGetType(template).Should().Be(JsonType.Object);
    }

    [Test]
    public void ReadTemplate_ReturnsJsonNullWhenTheTemplateCannotBeRead()
    {
        var template = _service.ReadTemplate(Blueprint("missing", DMBlueprintCategory.Creatures));

        SWLOR.NWN.API.NWScript.NWScript.JsonGetType(template).Should().Be(JsonType.Null);
    }

    // ---- creating objects

    [TestCase(DMBlueprintCategory.Creatures, ObjectType.Creature)]
    [TestCase(DMBlueprintCategory.Placeables, ObjectType.Placeable)]
    [TestCase(DMBlueprintCategory.Items, ObjectType.Item)]
    [TestCase(DMBlueprintCategory.Stores, ObjectType.Store)]
    [TestCase(DMBlueprintCategory.Waypoints, ObjectType.Waypoint)]
    [TestCase(DMBlueprintCategory.Triggers, ObjectType.Trigger)]
    [TestCase(DMBlueprintCategory.Encounters, ObjectType.Encounter)]
    public void GetObjectType_ReturnsTheKindOfObjectTheCategoryCreates(DMBlueprintCategory category, ObjectType type)
    {
        _service.GetObjectType(category).Should().Be(type);
    }

    [Test]
    public void GetObjectType_RefusesACategoryItDoesNotKnow()
    {
        var read = () => _service.GetObjectType((DMBlueprintCategory)99);

        read.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void GetSquareSize_IsLargerForEncountersThanForTriggers()
    {
        _service.GetSquareSize(DMBlueprintCategory.Triggers).Should().Be(4f);
        _service.GetSquareSize(DMBlueprintCategory.Encounters).Should().Be(10f);
    }

    [Test]
    public void CreateAtLocation_CreatesAPlaceableAndMarksTheDMWhoSpawnedIt()
    {
        _world.AddPlayer(DM, isDM: true);
        var location = EngineStructures.Create<Location>(5);
        _world.Engine.SetReturn("CreateObject", Created);

        var created = _service.CreateAtLocation(DM, Blueprint("crate", DMBlueprintCategory.Placeables), location);

        created.Should().Be(Created);
        var create = _world.Engine.CallsTo("CreateObject").Should().ContainSingle().Subject;
        create.Arguments[0].Should().Be(ObjectType.Placeable);
        create.Arguments[1].Should().Be("crate");
        create.Arguments[2].Should().BeSameAs(location);
        _world.Engine.CallsTo("SetLocalInt").Should().ContainSingle()
            .Which.Arguments.Should().Equal(Created, "DM_SPAWNED_PLACEABLE", 1);
        _world.Engine.CallsTo("SetLocalString").Select(call => (string)call.Arguments[1]!)
            .Should().Equal("DM_SPAWNED_BY", "DM_SPAWNED_BY_NAME");
        _world.Engine.CallsTo("SetLocalString").First().Arguments[2].Should().Be(DM.ToString());
    }

    [Test]
    public void CreateAtLocation_MarksAPlaceableForTheAvatarOfAPossessingDM()
    {
        const uint creature = 2;
        _world.AddPlayer(DM, isDM: true);
        _world.Possess(DM, creature, DMPossess);
        _world.Engine.SetReturn("CreateObject", Created);

        _service.CreateAtLocation(creature, Blueprint("crate", DMBlueprintCategory.Placeables), EngineStructures.Create<Location>(5));

        _world.Engine.CallsTo("SetLocalString").First().Arguments[2].Should().Be(DM.ToString(), "the placeable belongs to the DM, not the creature");
    }

    [Test]
    public void CreateAtLocation_PreparesACreatureLikeTheNativeDMCreator()
    {
        _world.AddPlayer(DM, isDM: true);
        _world.Engine.SetReturn("CreateObject", Created);
        _world.Engine.SetReturn("GetObjectType", ObjectType.Creature);

        var created = _service.CreateAtLocation(DM, Blueprint("rancor", DMBlueprintCategory.Creatures), EngineStructures.Create<Location>(5));

        created.Should().Be(Created);
        _world.Engine.CallsTo("ExecuteScript").Should().ContainSingle()
            .Which.Arguments.Should().Equal("x2_def_spawn", Created);
        _objectPlugin.CallsTo("SetConversationPrivate").Should().ContainSingle();
    }

    [Test]
    public void CreateAtLocation_MakesTheItemsOfACreatureUndroppable()
    {
        const uint item = 400;
        _world.AddPlayer(DM, isDM: true);
        _world.Engine.SetReturn("CreateObject", Created);
        _world.Engine.SetReturnSequence("GetFirstItemInInventory", item);

        _service.CreateAtLocation(DM, Blueprint("rancor", DMBlueprintCategory.Creatures), EngineStructures.Create<Location>(5));

        _world.Engine.CallsTo("SetDroppableFlag").Should().ContainSingle().Which.Arguments.Should().Equal(item, false);
    }

    [Test]
    public void CreateAtLocation_CreatesOtherKindsOfObjectWithoutPreparation()
    {
        _world.AddPlayer(DM, isDM: true);
        _world.Engine.SetReturn("CreateObject", Created);

        var created = _service.CreateAtLocation(DM, Blueprint("scrap", DMBlueprintCategory.Items), EngineStructures.Create<Location>(5));

        created.Should().Be(Created);
        _world.Engine.CallsTo("SetLocalInt").Should().BeEmpty();
        _world.Engine.CallsTo("ExecuteScript").Should().BeEmpty();
    }

    [Test]
    public void CreateAtLocation_ReturnsInvalidWhenTheEngineCreatesNothing()
    {
        _world.AddPlayer(DM, isDM: true);

        var created = _service.CreateAtLocation(DM, Blueprint("crate", DMBlueprintCategory.Placeables), EngineStructures.Create<Location>(5));

        created.Should().Be(Invalid);
        _world.Engine.CallsTo("SetLocalInt").Should().BeEmpty();
    }

    [Test]
    public void CreateAtLocation_BuildsATriggerFromItsTemplateWithASquareAroundTheLocation()
    {
        _world.AddPlayer(DM, isDM: true);
        _json.AddTemplate("tripwire", ResType.UTT, "{\"Tag\":{\"type\":\"cexostring\",\"value\":\"wire\"}}");
        _world.Engine.SetReturn("GetPositionFromLocation", new Vector3(10f, 20f, 1.5f));
        _world.Engine.SetReturn("JsonToObject", Created);
        var location = EngineStructures.Create<Location>(5);

        var created = _service.CreateAtLocation(DM, Blueprint("tripwire", DMBlueprintCategory.Triggers), location);

        created.Should().Be(Created);
        var call = _world.Engine.CallsTo("JsonToObject").Should().ContainSingle().Subject;
        call.Arguments[1].Should().BeSameAs(location);
        call.Arguments[3].Should().Be(true, "the object state keeps the template's variables and transition target");
        var template = JsonNode.Parse(Dump((Json)call.Arguments[0]!))!.AsObject();
        template["XPosition"]!["value"]!.GetValue<double>().Should().Be(10.0);
        template["YPosition"]!["value"]!.GetValue<double>().Should().Be(20.0);
        template["ZPosition"]!["value"]!.GetValue<double>().Should().Be(1.5);
        var points = template["Geometry"]!["value"]!.AsArray();
        points.Should().HaveCount(4);
        points[0]!["__struct_id"]!.GetValue<int>().Should().Be(3);
        points[0]!["PointX"]!["value"]!.GetValue<double>().Should().Be(-2.0, "a trigger is 4 meters wide");
        points[0]!["PointY"]!["value"]!.GetValue<double>().Should().Be(-2.0);
        points[0]!["PointZ"]!["value"]!.GetValue<double>().Should().Be(0.025);
        template["SpawnPointList"].Should().BeNull("only encounters need a spawn point");
    }

    [Test]
    public void CreateAtLocation_BuildsAnEncounterWithASpawnPointAtTheLocation()
    {
        _world.AddPlayer(DM, isDM: true);
        _json.AddTemplate("ambush", ResType.UTE, "{\"Tag\":{\"type\":\"cexostring\",\"value\":\"ambush\"}}");
        _world.Engine.SetReturn("GetPositionFromLocation", new Vector3(10f, 20f, 1.5f));
        _world.Engine.SetReturn("JsonToObject", Created);

        _service.CreateAtLocation(DM, Blueprint("ambush", DMBlueprintCategory.Encounters), EngineStructures.Create<Location>(5));

        var call = _world.Engine.CallsTo("JsonToObject").Should().ContainSingle().Subject;
        var template = JsonNode.Parse(Dump((Json)call.Arguments[0]!))!.AsObject();
        var points = template["Geometry"]!["value"]!.AsArray();
        points[0]!["__struct_id"]!.GetValue<int>().Should().Be(1);
        points[0]!["X"]!["value"]!.GetValue<double>().Should().Be(-5.0, "an encounter is 10 meters wide");
        var spawnPoint = template["SpawnPointList"]!["value"]!.AsArray().Should().ContainSingle().Subject!;
        spawnPoint["X"]!["value"]!.GetValue<double>().Should().Be(10.0, "spawn points are area positions, not offsets");
        spawnPoint["Y"]!["value"]!.GetValue<double>().Should().Be(20.0);
        spawnPoint["Orientation"]!["value"]!.GetValue<double>().Should().Be(0.0);
    }

    [Test]
    public void CreateAtLocation_ReturnsInvalidWhenATriggerTemplateCannotBeRead()
    {
        _world.AddPlayer(DM, isDM: true);

        var created = _service.CreateAtLocation(DM, Blueprint("missing", DMBlueprintCategory.Triggers), EngineStructures.Create<Location>(5));

        created.Should().Be(Invalid);
        _world.Engine.CallsTo("JsonToObject").Should().BeEmpty();
    }

    // ---- items in inventories

    [Test]
    public void CreateInInventory_RefusesBlueprintsThatAreNotItems()
    {
        var created = _service.CreateInInventory(DM, Blueprint("rancor", DMBlueprintCategory.Creatures), 77);

        created.Should().Be(Invalid);
        _world.Engine.CallsTo("CreateItemOnObject").Should().BeEmpty();
    }

    [Test]
    public void CreateInInventory_PutsTheItemInTheContainer()
    {
        _world.AddPlayer(DM, isDM: true);
        _world.Engine.SetReturn("CreateItemOnObject", Created);

        var created = _service.CreateInInventory(DM, Blueprint("scrap", DMBlueprintCategory.Items), 77);

        created.Should().Be(Created);
        var create = _world.Engine.CallsTo("CreateItemOnObject").Should().ContainSingle().Subject;
        create.Arguments[0].Should().Be("scrap");
        create.Arguments[1].Should().Be(77u);
    }

    [Test]
    public void CreateInInventory_ReturnsInvalidWhenTheItemCannotBeCreated()
    {
        _world.AddPlayer(DM, isDM: true);

        _service.CreateInInventory(DM, Blueprint("scrap", DMBlueprintCategory.Items), 77).Should().Be(Invalid);
    }

    [Test]
    public void CanReceiveItems_NeedsAValidObjectWithAnInventory()
    {
        _world.Engine.SetReturnHandler("GetHasInventory", arguments => (uint)arguments[0]! == 77);

        _service.CanReceiveItems(77).Should().BeTrue();
        _service.CanReceiveItems(78).Should().BeFalse();
        _service.CanReceiveItems(Invalid).Should().BeFalse();
    }

    private static string Dump(Json json) => SWLOR.NWN.API.NWScript.NWScript.JsonDump(json);
}
