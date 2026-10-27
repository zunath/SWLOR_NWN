using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Service.DMToolService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Area;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.Testing;
using static SWLOR.Game.Server.Tests.Support.DMPossessionEngine;

namespace SWLOR.Game.Server.Tests.Domain.Administration;

/// <summary>
/// Unit tests of the DM object details instance: the one-line rows and the detail text the DM chooser and creator show.
/// The engine, its JSON templates (<see cref="FakeJsonEngine"/>) and the NWNX object, creature and utility plugins are
/// replaced by fakes; an object is described by the answers the engine gives about it.
/// </summary>
[TestFixture]
public class DMObjectDetailsServiceTests
{
    private const string Dot = " \u00B7 ";
    private const uint Area = 100;
    private const uint Storage = 900;
    private const uint TemporaryItem = 901;
    private const uint Obj = 500;

    private DMPossessionEngine _world = null!;
    private FakeNWScript _engine = null!;
    private FakeJsonEngine _json = null!;
    private FakeCreaturePlugin _creaturePlugin = null!;
    private FakeObjectPlugin _objectPlugin = null!;
    private FakeUtilPlugin _util = null!;
    private readonly List<IDisposable> _scopes = new();
    private readonly Dictionary<(string Member, uint Obj), object> _props = new();
    private readonly Dictionary<string, object> _defaults = new();
    private readonly Dictionary<(string TwoDA, string Column, int Row), string> _twoDA = new();
    private DMBlueprintCatalogService _catalog = null!;
    private DMObjectDetailsService _service = null!;
    private FacadeContainer _container = null!;
    private IDisposable _containerScope = null!;

    [SetUp]
    public void SetUp()
    {
        _props.Clear();
        _defaults.Clear();
        _twoDA.Clear();
        _world = new DMPossessionEngine();
        _engine = _world.Engine;
        _json = new FakeJsonEngine(_engine);
        _creaturePlugin = new FakeCreaturePlugin();
        _objectPlugin = new FakeObjectPlugin();
        _util = new FakeUtilPlugin();
        _util.SetReturnHandler("StripColors", arguments => System.Text.RegularExpressions.Regex.Replace((string)arguments[0]!, "<c[^>]*>|</c>", string.Empty));
        _scopes.Add(CreaturePluginRuntime.Override(_creaturePlugin));
        _scopes.Add(ObjectPluginRuntime.Override(_objectPlugin));
        _scopes.Add(UtilPluginRuntime.Override(_util));
        _engine.SetReturnHandler("GetStringByStrRef", arguments => "text " + arguments[0]);
        _engine.SetReturnHandler("Get2DAString", arguments =>
            _twoDA.TryGetValue(((string)arguments[0]!, (string)arguments[1]!, (int)arguments[2]!), out var value) ? value : string.Empty);
        _catalog = new DMBlueprintCatalogService(new DMPresenceService());
        _service = new DMObjectDetailsService(_catalog);
        // The service reads the players in an area through the Area facade, which resolves its instance from a container.
        _container = new FacadeContainer();
        _containerScope = _container.Install();
    }

    [TearDown]
    public void TearDown()
    {
        _containerScope.Dispose();
        _container.Dispose();
        foreach (var scope in _scopes)
            scope.Dispose();
        _scopes.Clear();
        _world.Dispose();
    }

    // Makes the engine answer a per-object question: Say("GetName", obj, "Rodian") and so on.
    private void Say(string member, uint obj, object value)
    {
        _props[(member, obj)] = value;
        if (_defaults.ContainsKey(member))
            return;

        _defaults[member] = value is string ? string.Empty : Activator.CreateInstance(value.GetType())!;
        _engine.SetReturnHandler(member, arguments =>
            _props.TryGetValue((member, (uint)arguments[0]!), out var answer) ? answer : _defaults[member]);
    }

    private void Thing(uint obj, ObjectType type, string name, string resref = "", string tag = "", bool inArea = true)
    {
        Say("GetObjectType", obj, type);
        _world.Name(obj, name);
        if (inArea)
        {
            _world.Place(obj, Area);
            _world.Name(Area, "Cantina");
        }
        Say("GetResRef", obj, resref);
        Say("GetTag", obj, tag);
        Say("GetPortraitResRef", obj, "po_" + name.ToLowerInvariant().Replace(' ', '_') + "_");
    }

    // The properties of the one item the engine is asked about, walked with GetFirstItemProperty and GetNextItemProperty.
    private void ItemProperties(params (ItemPropertyType Type, int Cost)[] properties)
    {
        var cursor = 0;
        var property = EngineStructures.Create<ItemProperty>(1);
        _engine.SetReturnHandler("GetFirstItemProperty", _ =>
        {
            cursor = 0;
            return property;
        });
        _engine.SetReturnHandler("GetNextItemProperty", _ =>
        {
            cursor++;
            return property;
        });
        _engine.SetReturnHandler("GetIsItemPropertyValid", _ => cursor < properties.Length);
        _engine.SetReturnHandler("GetItemPropertyType", _ => properties[cursor].Type);
        _engine.SetReturnHandler("GetItemPropertyCostTableValue", _ => properties[cursor].Cost);
    }

    private void TwoDA(string file, string column, int row, string value) => _twoDA[(file, column, row)] = value;

    private static string LocString(string text) => "{\"type\":\"cexlocstring\",\"value\":{\"0\":\"" + text + "\"}}";

    private static string Int(int value) => "{\"type\":\"int\",\"value\":" + value + "}";

    private static string Text(string value) => "{\"type\":\"cexostring\",\"value\":\"" + value + "\"}";

    private static DMBlueprint Blueprint(string resref, DMBlueprintCategory category, string faction = "") =>
        new(resref, "Name of " + resref, "Group / Leaf", category, DMBlueprintSource.Custom, null, faction);

    private void StorageContainer()
    {
        _engine.SetReturnHandler("GetObjectByTag", arguments => (string)arguments[0]! == "TEMP_ITEM_STORAGE" ? Storage : Invalid);
        _engine.SetReturn("CreateItemOnObject", TemporaryItem);
    }

    // ---- list rows of live objects

    [Test]
    public void GetObjectRow_ShowsACreaturesLevelAndHitPoints()
    {
        Thing(Obj, ObjectType.Creature, "Rodian");
        Say("GetHitDice", Obj, 7);
        Say("GetCurrentHitPoints", Obj, 12);
        Say("GetMaxHitPoints", Obj, 20);

        var (icon, summary) = _service.GetObjectRow(Obj);

        icon.Should().Be("po_rodian_s");
        summary.Should().Be("Lv 7" + Dot + "HP 12/20");
    }

    [Test]
    public void GetObjectRow_PrefersTheLevelOfTheCreaturesStatSkin()
    {
        const uint skin = 501;
        Thing(Obj, ObjectType.Creature, "Rodian");
        Say("GetHitDice", Obj, 7);
        _engine.SetReturnHandler("GetItemInSlot", arguments => (uint)arguments[1]! == Obj ? skin : Invalid);
        ItemProperties((ItemPropertyType.NPCLevel, 12));

        _service.GetObjectRow(Obj).Summary.Should().StartWith("Lv 12");
    }

    [Test]
    public void GetObjectRow_FallsBackToTheGenericCreatureIconWithoutAPortrait()
    {
        Thing(Obj, ObjectType.Creature, "Rodian");
        Say("GetPortraitResRef", Obj, " ");

        _service.GetObjectRow(Obj).Icon.Should().Be("dm_ai");
    }

    [Test]
    public void GetObjectRow_ShowsAnItemsIconAndBaseItemName()
    {
        Thing(Obj, ObjectType.Item, "Blaster", inArea: false);
        Say("GetBaseItemType", Obj, BaseItem.Longsword);
        TwoDA("baseitems", "DefaultIcon", (int)BaseItem.Longsword, "iit_blaster");
        TwoDA("baseitems", "Name", (int)BaseItem.Longsword, "4001");
        _engine.SetReturnHandler("ResManGetAliasFor", arguments => (string)arguments[0]! == "iit_blaster" ? "alias" : string.Empty);

        var (icon, summary) = _service.GetObjectRow(Obj);

        icon.Should().Be("iit_blaster");
        summary.Should().Be("text 4001");
    }

    [Test]
    public void GetObjectRow_ShowsThePlaceablesFlags()
    {
        Thing(Obj, ObjectType.Placeable, "Crate");
        Say("GetHasInventory", Obj, true);
        Say("GetLocked", Obj, true);

        var (icon, summary) = _service.GetObjectRow(Obj);

        icon.Should().Be("po_crate_s");
        summary.Should().Be("Container" + Dot + "Locked");
    }

    [Test]
    public void GetObjectRow_ShowsADoorsState()
    {
        Thing(Obj, ObjectType.Door, "Blast Door");
        Say("GetIsOpen", Obj, true);
        Say("GetPlotFlag", Obj, true);

        var (icon, summary) = _service.GetObjectRow(Obj);

        icon.Should().Be("ir_open");
        summary.Should().Be("Open" + Dot + "Plot");
    }

    [TestCase(ObjectType.Trigger, "dm_trigger")]
    [TestCase(ObjectType.Waypoint, "ir_moveto")]
    [TestCase(ObjectType.Store, "ir_buy")]
    [TestCase(ObjectType.Encounter, "dm_encounter")]
    public void GetObjectRow_ShowsOnlyAGenericIconForOtherObjects(ObjectType type, string icon)
    {
        Thing(Obj, type, "Thing");

        _service.GetObjectRow(Obj).Should().Be((icon, string.Empty));
    }

    [Test]
    public void GetObjectRow_UsesTheNoSelectionIconForAnUnknownKind()
    {
        Thing(Obj, ObjectType.Tile, "Tile");

        _service.GetObjectRow(Obj).Icon.Should().Be(DMObjectDetailsService.NoSelectionIcon);
    }

    // ---- details of live objects

    [Test]
    public void DescribeObject_SaysWhenTheObjectIsGone()
    {
        var details = _service.DescribeObject(Invalid);

        details.IconResref.Should().Be(DMObjectDetailsService.NoSelectionIcon);
        details.Title.Should().Be("Gone");
        details.Body.Should().Be("That object no longer exists.");
    }

    [Test]
    public void DescribeObject_DescribesACreature()
    {
        Thing(Obj, ObjectType.Creature, "Rodian", "rodian_bh", "RODIAN_TAG");
        Say("GetHitDice", Obj, 7);
        Say("GetCurrentHitPoints", Obj, 12);
        Say("GetMaxHitPoints", Obj, 20);
        Say("GetRacialType", Obj, 2);
        Say("GetGender", Obj, (int)Gender.Female);
        Say("GetPlotFlag", Obj, true);
        Say("GetDescription", Obj, "A <c>wary</c> hunter.  ");
        _engine.SetReturnHandler("GetAbilityScore", arguments => 10 + (int)(AbilityType)arguments[1]!);
                TwoDA("racialtypes", "Name", 2, "3002");
        _creaturePlugin.SetReturnHandler("GetFaction", _ => 1);
        _json.AddTemplate("repute", ResType.FAC,
            "{\"FactionList\":{\"type\":\"list\",\"value\":[{\"FactionName\":" + Text("Player") + "},{\"FactionName\":" + Text("Hostile") + "}]}}");
        _objectPlugin.SetReturnHandler("GetLocalVariableCount", _ => 3);
        _objectPlugin.SetReturnHandler("GetLocalVariable", arguments => (int)arguments[1]! switch
        {
            0 => new ObjectPlugin.LocalVariable { Type = LocalVariableType.String, Key = "LOOT_TABLE_1" },
            1 => new ObjectPlugin.LocalVariable { Type = LocalVariableType.Int, Key = "LOOT_TABLE_INT" },
            _ => new ObjectPlugin.LocalVariable { Type = LocalVariableType.String, Key = "loot_table_2" }
        });
        _engine.SetReturnHandler("GetLocalString", arguments => (string)arguments[1]! switch
        {
            "LOOT_TABLE_1" => "credits_low, 25",
            "loot_table_2" => "junk",
            _ => string.Empty
        });

        var details = _service.DescribeObject(Obj);

        details.IconResref.Should().Be("po_rodian_l");
        details.Title.Should().Be("Rodian");
        details.Body.Should().Be(string.Join("\n",
            "Level 7   Faction: Hostile",
            "HP 12 / 20",
            "MGT 10   PER 11   VIT 12",
            "AGI 13   WIL 14   SOC 15",
            "Race: text 3002   Gender: Female",
            "AI: Generic",
            "Plot",
            "Loot: credits_low (25%), junk",
            string.Empty,
            "A wary hunter.",
            string.Empty,
            "Resref: rodian_bh   Tag: RODIAN_TAG",
            "Area: Cantina"));
    }

    [Test]
    public void DescribeObject_ListsThePropertiesOfACreaturesSkinAndFlagsAPossessedCreature()
    {
        Thing(Obj, ObjectType.Creature, "Rodian");
        Say("GetIsDMPossessed", Obj, true);
        Say("GetImmortal", Obj, true);
        _engine.SetReturnHandler("GetItemInSlot", arguments => (uint)arguments[1]! == Obj ? TemporaryItem : Invalid);

        var details = _service.DescribeObject(Obj);

        details.Body.Should().Contain("Immortal" + Dot + "DM possessed");
        details.Body.Should().Contain("Faction: Faction 0");
    }

    [Test]
    public void DescribeObject_DescribesAnItem()
    {
        Thing(Obj, ObjectType.Item, "Blaster", "blaster_a", "BLASTER", inArea: false);
        Say("GetBaseItemType", Obj, BaseItem.Longsword);
        Say("GetWeight", Obj, 35);
        Say("GetGoldPieceValue", Obj, 1200);
        Say("GetDescription", Obj, "Standard issue.");
        TwoDA("baseitems", "Name", (int)BaseItem.Longsword, "4001");
        TwoDA("baseitems", "DefaultIcon", (int)BaseItem.Longsword, "iit_blaster");
        _engine.SetReturnHandler("ResManGetAliasFor", arguments => (string)arguments[0]! == "iit_blaster" ? "alias" : string.Empty);
        ItemProperties((ItemPropertyType.DMG, 9));

        var details = _service.DescribeObject(Obj);

        details.IconResref.Should().Be("iit_blaster");
        details.Title.Should().Be("Blaster");
        var lines = details.Body.Split('\n');
        lines[0].Should().Be("text 4001");
        lines[1].Should().Be("DMG 9");
        lines[2].Should().Be("Weight 3.5 lbs");
        lines[3].Should().Be("Value 1200 credits");
        lines.Should().NotContain(line => line.StartsWith("Area:"), "an item in an inventory has no area");
        lines.Should().Contain("Resref: blaster_a   Tag: BLASTER");
        lines.Should().Contain("Standard issue.");
    }

    [Test]
    public void DescribeObject_ShowsTheAreaOfAnItemLyingOnTheGround()
    {
        Thing(Obj, ObjectType.Item, "Blaster");

        _service.DescribeObject(Obj).Body.Should().EndWith("Area: Cantina");
    }

    [Test]
    public void DescribeObject_DescribesAPlaceable()
    {
        Thing(Obj, ObjectType.Placeable, "Crate");
        Say("GetUseableFlag", Obj, true);
        Say("GetHasInventory", Obj, true);
        Say("GetCurrentHitPoints", Obj, 5);
        Say("GetMaxHitPoints", Obj, 10);
        Say("GetDescription", Obj, "A crate.");
        _engine.SetReturnHandler("GetLocalInt", arguments => (string)arguments[1]! == "DM_SPAWNED_PLACEABLE" ? 1 : 0);
        _engine.SetReturnHandler("GetLocalString", arguments => (string)arguments[1]! == "DM_SPAWNED_BY_NAME" ? "Zorn" : string.Empty);

        var details = _service.DescribeObject(Obj);

        details.Body.Split('\n').Take(4).Should().Equal("Useable" + Dot + "Container", "HP 5 / 10", "Spawned by DM Zorn", string.Empty);
        details.Body.Should().Contain("A crate.");
    }

    [Test]
    public void DescribeObject_DescribesADoor()
    {
        const uint target = 600;
        Thing(Obj, ObjectType.Door, "Blast Door");
        _world.Place(target, Area);
        _world.Name(Area, "Cantina");
        Say("GetLocked", Obj, true);
        Say("GetLockKeyRequired", Obj, true);
        Say("GetLockKeyTag", Obj, "KEYCARD");
        Say("GetTransitionTarget", Obj, target);
        Say("GetTag", target, "EXIT");

        var details = _service.DescribeObject(Obj);

        details.Body.Split('\n').Take(3).Should().Equal("Locked", "Key required: KEYCARD", "Leads to: Cantina (EXIT)");
    }

    [Test]
    public void DescribeObject_DescribesATransitionTrigger()
    {
        Thing(Obj, ObjectType.Trigger, "Exit");
        Say("GetPortraitResRef", Obj, string.Empty);

        var details = _service.DescribeObject(Obj);

        details.IconResref.Should().Be("dm_trigger");
        details.Body.Should().Be(string.Join("\n", string.Empty, "Resref:    Tag: ", "Area: Cantina"));
    }

    [Test]
    public void DescribeObject_DescribesAStore()
    {
        Thing(Obj, ObjectType.Store, "Shop");
        _engine.SetReturnSequence("GetFirstItemInInventory", 701u);
        _engine.SetReturnSequence("GetNextItemInInventory", 702u, Invalid);
        Say("GetStoreGold", Obj, -1);
        Say("GetStoreMaxBuyPrice", Obj, 250);

        var details = _service.DescribeObject(Obj);

        details.Body.Split('\n').Take(2).Should().Equal("Sells 2 items", "Gold: unlimited   Max buy price: 250");
    }

    [Test]
    public void DescribeObject_DescribesAStoreWithOneItemAndLimitedGold()
    {
        Thing(Obj, ObjectType.Store, "Shop");
        _engine.SetReturnSequence("GetFirstItemInInventory", 701u);
        Say("GetStoreGold", Obj, 40);

        _service.DescribeObject(Obj).Body.Split('\n').Take(2).Should().Equal("Sells 1 item", "Gold: 40   Max buy price: 0");
    }

    [Test]
    public void DescribeObject_DescribesAnEncounter()
    {
        Thing(Obj, ObjectType.Encounter, "Ambush");
        Say("GetEncounterActive", Obj, 1);
        Say("GetEncounterSpawnsCurrent", Obj, 2);
        Say("GetEncounterSpawnsMax", Obj, 5);

        _service.DescribeObject(Obj).Body.Split('\n').Take(2).Should().Equal("Active", "Spawned 2 of 5");
    }

    [Test]
    public void DescribeObject_DescribesAnInactiveEncounter()
    {
        Thing(Obj, ObjectType.Encounter, "Ambush");

        _service.DescribeObject(Obj).Body.Split('\n')[0].Should().Be("Inactive");
    }

    [Test]
    public void DescribeObject_ShowsOnlyTheNamesOfOtherObjects()
    {
        Thing(Obj, ObjectType.Waypoint, "Marker", "wp_marker", "MARK");
        Say("GetPortraitResRef", Obj, string.Empty);

        var details = _service.DescribeObject(Obj);

        details.IconResref.Should().Be("ir_moveto");
        details.Body.Should().Be(string.Join("\n", string.Empty, "Resref: wp_marker   Tag: MARK", "Area: Cantina"));
    }

    // ---- areas

    [Test]
    public void CountPlayersInArea_IsZeroForAnAreaNoPlayerHasEntered()
    {
        _service.CountPlayersInArea(Area).Should().Be(0);
    }

    [Test]
    public void DescribeArea_SaysWhoIsThereAndHowBigItIs()
    {
        _world.Name(Area, "Cantina");
        Say("GetResRef", Area, "cantina");
        Say("GetTag", Area, "CANTINA");
        _engine.SetReturnHandler("GetAreaSize", arguments => (Dimension)arguments[0]! == Dimension.Width ? 8 : 6);

        var details = _service.DescribeArea(Area);

        details.IconResref.Should().Be(DMObjectDetailsService.AreaIcon);
        details.Title.Should().Be("Cantina");
        details.Body.Should().Be(string.Join("\n",
            "0 players here",
            "Size: 8 x 6 tiles",
            string.Empty,
            "Resref: cantina   Tag: CANTINA"));
    }

    // ---- blueprints

    [Test]
    public void DescribeBlueprint_SaysWhenTheTemplateCannotBeRead()
    {
        var details = _service.DescribeBlueprint(Blueprint("missing", DMBlueprintCategory.Creatures));

        details.IconResref.Should().Be("dm_ai");
        details.Title.Should().Be("Name of missing");
        details.Body.Should().Be("The missing template could not be read.");
    }

    [Test]
    public void DescribeBlueprint_DescribesACreatureTemplate()
    {
        _json.AddTemplate("hunter", ResType.UTC, """
            {"MaxHitPoints":{"type":"short","value":45},
             "Str":{"type":"byte","value":14},"Dex":{"type":"byte","value":13},"Con":{"type":"byte","value":12},
             "Int":{"type":"byte","value":11},"Wis":{"type":"byte","value":10},"Cha":{"type":"byte","value":9},
             "Race":{"type":"byte","value":2},"Gender":{"type":"byte","value":0},
             "FactionID":{"type":"word","value":0},"Plot":{"type":"byte","value":1},"IsImmortal":{"type":"byte","value":0},
             "Conversation":{"type":"resref","value":"hunter_talk"},
             "ClassList":{"type":"list","value":[{"ClassLevel":{"type":"short","value":3}},{"ClassLevel":{"type":"short","value":4}}]},
             "Tag":{"type":"cexostring","value":"HUNTER"},
             "VarTable":{"type":"list","value":[
               {"Name":{"type":"cexostring","value":"LOOT_TABLE_A"},"Value":{"type":"cexostring","value":"bounty, 40"}},
               {"Name":{"type":"cexostring","value":"OTHER"},"Value":{"type":"cexostring","value":"ignored"}}]},
             "Description":{"type":"cexlocstring","value":{"0":"A <c>lean</c> tracker."}},
             "Portrait":{"type":"resref","value":"po_hunter"}}
            """);
        _json.AddTemplate("repute", ResType.FAC, "{\"FactionList\":{\"type\":\"list\",\"value\":[{\"FactionName\":" + Text("Hostile") + "}]}}");
        TwoDA("racialtypes", "Name", 2, "3002");

        var details = _service.DescribeBlueprint(Blueprint("hunter", DMBlueprintCategory.Creatures));

        details.IconResref.Should().Be("po_hunterl", "the portrait resref gets the size letter");
        details.Body.Split('\n').Should().Equal(
            "Level 7   Faction: Hostile",
            "HP 45",
            "MGT 14   PER 13   VIT 12",
            "AGI 11   WIL 10   SOC 9",
            "Race: text 3002   Gender: Male",
            "Plot",
            "Conversation: hunter_talk",
            "Loot: bounty (40%)",
            string.Empty,
            "A lean tracker.",
            string.Empty,
            "Palette: Custom / Group / Leaf",
            "Resref: hunter   Tag: HUNTER");
    }

    [Test]
    public void DescribeBlueprint_UsesTheBlueprintsFactionAndTheLevelAndHitPointsOfItsSkin()
    {
        _json.AddTemplate("elite", ResType.UTC, """
            {"MaxHitPoints":{"type":"short","value":45},
             "Equip_ItemList":{"type":"list","value":[
               {"__struct_id":1,"EquippedRes":{"type":"resref","value":"elite_skin"}},
               {"__struct_id":131072,"EquippedRes":{"type":"resref","value":"elite_armor"}}]}}
            """);
        StorageContainer();
        ItemProperties((ItemPropertyType.NPCLevel, 14));

        var details = _service.DescribeBlueprint(Blueprint("elite", DMBlueprintCategory.Creatures, faction: "Raiders"));

        details.Body.Split('\n')[0].Should().Be("Level 14   Faction: Raiders");
        _engine.CallsTo("CreateItemOnObject").Should().ContainSingle().Which.Arguments[0].Should().Be("elite_armor");
        _engine.CallsTo("DestroyObject").Should().ContainSingle().Which.Arguments[0].Should().Be(TemporaryItem);
    }

    [Test]
    public void DescribeBlueprint_PlacesACreatureItemOnTheGroundWhenTheContainerHasNoRoom()
    {
        const uint onGround = 902;
        _json.AddTemplate("elite", ResType.UTC, """
            {"Equip_ItemList":{"type":"list","value":[{"__struct_id":131072,"EquippedRes":{"type":"resref","value":"elite_armor"}}]}}
            """);
        _engine.SetReturnHandler("GetObjectByTag", _ => Storage);
        _engine.SetReturn("CreateObject", onGround);

        _service.DescribeBlueprint(Blueprint("elite", DMBlueprintCategory.Creatures));

        _engine.CallsTo("CreateObject").Should().ContainSingle().Which.Arguments[0].Should().Be(ObjectType.Item);
        _engine.CallsTo("DestroyObject").Should().ContainSingle().Which.Arguments[0].Should().Be(onGround);
    }

    [Test]
    public void DescribeBlueprint_SkipsTheSkinWhenThereIsNoStorageContainer()
    {
        _json.AddTemplate("elite", ResType.UTC, """
            {"Equip_ItemList":{"type":"list","value":[{"__struct_id":131072,"EquippedRes":{"type":"resref","value":"elite_armor"}}]}}
            """);

        var details = _service.DescribeBlueprint(Blueprint("elite", DMBlueprintCategory.Creatures));

        details.Body.Should().StartWith("Level 0   Faction: Faction -1");
        _engine.CallsTo("CreateItemOnObject").Should().BeEmpty();
    }

    [Test]
    public void DescribeBlueprint_DescribesAnItemByCreatingItBriefly()
    {
        _json.AddTemplate("desc_blaster", ResType.UTI, "{\"Tag\":" + Text("BLASTER") + "}");
        StorageContainer();
        Say("GetBaseItemType", TemporaryItem, BaseItem.Longsword);
        TwoDA("baseitems", "Name", (int)BaseItem.Longsword, "4001");

        var details = _service.DescribeBlueprint(Blueprint("desc_blaster", DMBlueprintCategory.Items));

        details.Body.Split('\n').Should().StartWith(new[] { "text 4001", "Weight 0.0 lbs", "Value 0 credits" });
        details.Body.Should().EndWith("Palette: Custom / Group / Leaf\nResref: desc_blaster   Tag: BLASTER");
        _engine.CallsTo("DestroyObject").Should().NotBeEmpty().And.OnlyContain(call => (uint)call.Arguments[0]! == TemporaryItem,
            "every item made for inspection is destroyed again");
    }

    [Test]
    public void DescribeBlueprint_SaysWhenAnItemCannotBeCreated()
    {
        _json.AddTemplate("desc_blaster_nostorage", ResType.UTI, "{\"Tag\":" + Text("BLASTER") + "}");

        var details = _service.DescribeBlueprint(Blueprint("desc_blaster_nostorage", DMBlueprintCategory.Items));

        details.Body.Should().StartWith("The item could not be created for inspection.");
    }

    [Test]
    public void DescribeBlueprint_DescribesAPlaceableTemplate()
    {
        _json.AddTemplate("crate", ResType.UTP, """
            {"Appearance":{"type":"dword","value":9},"Useable":{"type":"byte","value":1},"HasInventory":{"type":"byte","value":1},
             "Static":{"type":"byte","value":1},"HP":{"type":"short","value":30},"Conversation":{"type":"resref","value":"crate_talk"},
             "Description":{"type":"cexlocstring","value":{"0":"Heavy."}},"Tag":{"type":"cexostring","value":"CRATE"},
             "VarTable":{"type":"list","value":[{"Name":{"type":"cexostring","value":"LOOT_TABLE_X"},"Value":{"type":"cexostring","value":"scrap"}}]}}
            """);
        TwoDA("placeables", "StrRef", 9, "5001");

        var details = _service.DescribeBlueprint(Blueprint("crate", DMBlueprintCategory.Placeables));

        details.Body.Split('\n').Should().Equal(
            "Appearance: text 5001",
            "Useable" + Dot + "Container" + Dot + "Static",
            "HP 30",
            "Conversation: crate_talk",
            "Loot: scrap",
            string.Empty,
            "Heavy.",
            string.Empty,
            "Palette: Custom / Group / Leaf",
            "Resref: crate   Tag: CRATE");
    }

    [Test]
    public void DescribeBlueprint_NamesAPlaceableAppearanceByItsLabelWhenItHasNoText()
    {
        _json.AddTemplate("crate", ResType.UTP, "{\"Appearance\":{\"type\":\"dword\",\"value\":9}}");
        TwoDA("placeables", "Label", 9, "Crate_Label");

        _service.DescribeBlueprint(Blueprint("crate", DMBlueprintCategory.Placeables)).Body.Should().StartWith("Appearance: Crate_Label");
    }

    [Test]
    public void DescribeBlueprint_DescribesAStoreTemplate()
    {
        _json.AddTemplate("shop", ResType.UTM, """
            {"MarkUp":{"type":"int","value":120},"MarkDown":{"type":"int","value":60},"StoreGold":{"type":"int","value":-1},
             "StoreList":{"type":"list","value":[
               {"ItemList":{"type":"list","value":[{"InventoryRes":{"type":"resref","value":"a"}},{"InventoryRes":{"type":"resref","value":"b"}}]}},
               {"ItemList":{"type":"list","value":[{"InventoryRes":{"type":"resref","value":"c"}}]}}]}}
            """);

        var details = _service.DescribeBlueprint(Blueprint("shop", DMBlueprintCategory.Stores));

        details.Body.Split('\n').Take(3).Should().Equal("Sells 3 items", "Mark-up 120%   Mark-down 60%", "Gold: unlimited");
        details.IconResref.Should().Be("ir_buy");
    }

    [Test]
    public void DescribeBlueprint_DescribesAStoreTemplateWithOneItemAndGold()
    {
        _json.AddTemplate("shop", ResType.UTM, """
            {"StoreGold":{"type":"int","value":500},"StoreList":{"type":"list","value":[{"ItemList":{"type":"list","value":[{}]}}]}}
            """);

        _service.DescribeBlueprint(Blueprint("shop", DMBlueprintCategory.Stores)).Body.Split('\n').Take(3)
            .Should().Equal("Sells 1 item", "Mark-up 0%   Mark-down 0%", "Gold: 500");
    }

    [TestCase(1, "Area transition")]
    [TestCase(2, "Trap")]
    [TestCase(0, "Generic trigger")]
    public void DescribeBlueprint_NamesTheKindOfATriggerTemplate(int type, string expected)
    {
        _json.AddTemplate("trigger", ResType.UTT, "{\"Type\":" + Int(type) + "}");

        var details = _service.DescribeBlueprint(Blueprint("trigger", DMBlueprintCategory.Triggers));

        details.Body.Split('\n')[0].Should().Be(expected);
        details.Body.Should().Contain("Created as a 4 m square.");
    }

    [Test]
    public void DescribeBlueprint_DescribesATrapTriggerTemplate()
    {
        _json.AddTemplate("trap", ResType.UTT, """
            {"Type":{"type":"int","value":2},"TrapFlag":{"type":"byte","value":1},"TrapDetectDC":{"type":"byte","value":20},
             "DisarmDC":{"type":"byte","value":18},"TrapOneShot":{"type":"byte","value":1},
             "LinkedTo":{"type":"cexostring","value":"exit_wp"},"ScriptOnEnter":{"type":"resref","value":"trap_enter"}}
            """);

        _service.DescribeBlueprint(Blueprint("trap", DMBlueprintCategory.Triggers)).Body.Split('\n').Take(5).Should().Equal(
            "Trap", "Detect DC 20   Disarm DC 18", "Fires once", "Leads to: exit_wp", "On enter: trap_enter");
    }

    [Test]
    public void DescribeBlueprint_SaysAReusableTrapFiresEveryTime()
    {
        _json.AddTemplate("trap", ResType.UTT, "{\"TrapFlag\":" + Int(1) + "}");

        _service.DescribeBlueprint(Blueprint("trap", DMBlueprintCategory.Triggers)).Body.Should().Contain("Fires every time");
    }

    [Test]
    public void DescribeBlueprint_DescribesAnEncounterTemplate()
    {
        _json.AddTemplate("ambush", ResType.UTE, """
            {"MaxCreatures":{"type":"int","value":6},"RecCreatures":{"type":"int","value":3},"SpawnOption":{"type":"int","value":1},
             "Active":{"type":"byte","value":0},"PlayerOnly":{"type":"byte","value":1},
             "CreatureList":{"type":"list","value":[{"ResRef":{"type":"resref","value":"raider"}},{"ResRef":{"type":"resref","value":" "}},{"ResRef":{"type":"resref","value":"raider_boss"}}]}}
            """);

        var details = _service.DescribeBlueprint(Blueprint("ambush", DMBlueprintCategory.Encounters));

        details.Body.Split('\n').Take(5).Should().Equal(
            "Up to 6 creatures, 3 recommended",
            "Spawns once",
            "Inactive" + Dot + "Players only",
            "Creatures: raider, raider_boss",
            "Created as a 10 m square.");
    }

    [Test]
    public void DescribeBlueprint_DescribesAnEncounterTemplateThatSpawnsContinuously()
    {
        _json.AddTemplate("ambush", ResType.UTE, "{\"Active\":" + Int(1) + "}");

        _service.DescribeBlueprint(Blueprint("ambush", DMBlueprintCategory.Encounters)).Body.Split('\n')[1].Should().Be("Spawns continuously");
    }

    [Test]
    public void DescribeBlueprint_DescribesAWaypointTemplate()
    {
        _json.AddTemplate("marker", ResType.UTW, """
            {"HasMapNote":{"type":"byte","value":1},"MapNote":{"type":"cexlocstring","value":{"0":"Rendezvous"}},
             "Description":{"type":"cexlocstring","value":{"0":"A marker."}}}
            """);

        var details = _service.DescribeBlueprint(Blueprint("marker", DMBlueprintCategory.Waypoints));

        details.Body.Split('\n').Take(4).Should().Equal("Map note: Rendezvous", string.Empty, "A marker.", string.Empty);
        details.IconResref.Should().Be("ir_moveto");
    }

    // ---- list rows of blueprints

    [Test]
    public void GetBlueprintRow_ShowsAnItemsBaseItemNameAndIcon()
    {
        _json.AddTemplate("row_blaster", ResType.UTI, "{\"BaseItem\":" + Int((int)BaseItem.Longsword) + "}");
        TwoDA("baseitems", "Name", (int)BaseItem.Longsword, "4001");
        StorageContainer();

        var (icon, summary) = _service.GetBlueprintRow(Blueprint("row_blaster", DMBlueprintCategory.Items));

        summary.Should().Be("text 4001");
        icon.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public void GetBlueprintRow_ShowsTheLeafCategoryOfAnItemWithoutABaseItem()
    {
        _json.AddTemplate("row_loose", ResType.UTI, "{}");
        StorageContainer();

        _service.GetBlueprintRow(Blueprint("row_loose", DMBlueprintCategory.Items)).Summary.Should().Be("Leaf");
    }

    [TestCase(DMBlueprintCategory.Stores, "ir_buy")]
    [TestCase(DMBlueprintCategory.Waypoints, "ir_moveto")]
    [TestCase(DMBlueprintCategory.Triggers, "dm_trigger")]
    [TestCase(DMBlueprintCategory.Encounters, "dm_encounter")]
    public void GetBlueprintRow_ShowsTheLeafCategoryAndAGenericIconWithoutReadingTheTemplate(DMBlueprintCategory category, string icon)
    {
        var row = _service.GetBlueprintRow(Blueprint("row_other_" + category, category));

        row.Should().Be((icon, "Leaf"));
        _engine.CallsTo("TemplateToJson").Should().BeEmpty();
    }

    [Test]
    public void GetBlueprintRow_ShowsThePlaceablesFlagsAndPortrait()
    {
        _json.AddTemplate("row_crate", ResType.UTP, """
            {"HasInventory":{"type":"byte","value":1},"Plot":{"type":"byte","value":1},"PortraitId":{"type":"word","value":12}}
            """);
        TwoDA("portraits", "BaseResRef", 12, "crate");

        var row = _service.GetBlueprintRow(Blueprint("row_crate", DMBlueprintCategory.Placeables));

        row.Should().Be(("po_crates", "Container" + Dot + "Plot"));
    }

    [Test]
    public void GetBlueprintRow_ShowsACreaturesLevelAndFactionFromItsTemplate()
    {
        _json.AddTemplate("row_hunter", ResType.UTC, """
            {"PortraitId":{"type":"word","value":0},"Portrait":{"type":"resref","value":"po_hunter"},
             "ClassList":{"type":"list","value":[{"ClassLevel":{"type":"short","value":5}}]}}
            """);

        var row = _service.GetBlueprintRow(Blueprint("row_hunter", DMBlueprintCategory.Creatures, faction: "Raiders"));

        row.Should().Be(("po_hunters", "Lv 5" + Dot + "Raiders"));
    }

    [Test]
    public void GetBlueprintRow_ReadsTheLevelFromTheSkinTemplateBeforeTheClassLevels()
    {
        _json.AddTemplate("row_elite", ResType.UTC, """
            {"ClassList":{"type":"list","value":[{"ClassLevel":{"type":"short","value":5}}]},
             "Equip_ItemList":{"type":"list","value":[{"__struct_id":131072,"EquippedRes":{"type":"resref","value":"row_elite_skin"}}]}}
            """);
        _json.AddTemplate("row_elite_skin", ResType.UTI, """
            {"PropertiesList":{"type":"list","value":[
              {"PropertyName":{"type":"word","value":1}},
              {"PropertyName":{"type":"word","value":99},"CostValue":{"type":"word","value":18}}]}}
            """);

        _service.GetBlueprintRow(Blueprint("row_elite", DMBlueprintCategory.Creatures)).Summary.Should().Be("Lv 18");
    }

    [Test]
    public void GetBlueprintRow_IgnoresASkinWithoutALevelProperty()
    {
        _json.AddTemplate("row_plain", ResType.UTC, """
            {"ClassList":{"type":"list","value":[{"ClassLevel":{"type":"short","value":2}}]},
             "Equip_ItemList":{"type":"list","value":[{"__struct_id":131072,"EquippedRes":{"type":"resref","value":"row_plain_skin"}}]}}
            """);
        _json.AddTemplate("row_plain_skin", ResType.UTI, "{\"PropertiesList\":{\"type\":\"list\",\"value\":[]}}");

        _service.GetBlueprintRow(Blueprint("row_plain", DMBlueprintCategory.Creatures)).Summary.Should().Be("Lv 2");
    }

    [Test]
    public void GetBlueprintRow_ShowsTheLeafCategoryWhenTheTemplateCannotBeRead()
    {
        _service.GetBlueprintRow(Blueprint("row_missing", DMBlueprintCategory.Creatures)).Should().Be(("dm_ai", "Leaf"));
    }

    [Test]
    public void GetBlueprintRow_KeepsTheRowForTheRestOfTheUptime()
    {
        _json.AddTemplate("row_kept", ResType.UTC, "{\"ClassList\":{\"type\":\"list\",\"value\":[{\"ClassLevel\":" + Int(4) + "}]}}");
        var blueprint = Blueprint("row_kept", DMBlueprintCategory.Creatures);

        var first = _service.GetBlueprintRow(blueprint);
        _engine.ClearCalls();
        var second = _service.GetBlueprintRow(Blueprint("ROW_KEPT", DMBlueprintCategory.Creatures));

        second.Should().Be(first);
        _engine.CallsTo("TemplateToJson").Should().BeEmpty("the resref is not case sensitive and the row is remembered");
    }

    [Test]
    public void GetBlueprintRow_KeepsEachInstancesRowsApart()
    {
        _json.AddTemplate("row_apart", ResType.UTC, "{\"ClassList\":{\"type\":\"list\",\"value\":[{\"ClassLevel\":" + Int(4) + "}]}}");
        var blueprint = Blueprint("row_apart", DMBlueprintCategory.Creatures);
        _service.GetBlueprintRow(blueprint);
        _engine.ClearCalls();

        new DMObjectDetailsService(_catalog).GetBlueprintRow(blueprint);

        _engine.CallsTo("TemplateToJson").Should().NotBeEmpty("state lives on the instance, not in static fields");
    }

    // ---- factions

    [Test]
    public void DescribeBlueprint_ReadsTheFactionNamesOnlyOnce()
    {
        _json.AddTemplate("a", ResType.UTC, "{\"FactionID\":" + Int(0) + "}");
        _json.AddTemplate("b", ResType.UTC, "{\"FactionID\":" + Int(5) + "}");
        _json.AddTemplate("repute", ResType.FAC, "{\"FactionList\":{\"type\":\"list\",\"value\":[{\"FactionName\":" + Text("Player") + "}]}}");

        _service.DescribeBlueprint(Blueprint("a", DMBlueprintCategory.Creatures)).Body.Should().StartWith("Level 0   Faction: Player");
        _service.DescribeBlueprint(Blueprint("b", DMBlueprintCategory.Creatures)).Body.Should().StartWith("Level 0   Faction: Faction 5");

        _engine.CallsTo("TemplateToJson").Count(call => (string)call.Arguments[0]! == "repute").Should().Be(1);
    }
}
