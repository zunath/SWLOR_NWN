using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.DialogDefinition;
using SWLOR.Game.Server.Service.CityStaffService;
using SWLOR.Game.Server.Service.CityStoreService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The staff of a city's buildings: their blueprints, the waypoint each stands at in its building template, the layout
/// hooks that spawn them, and the store handlers that make their stores sell with the Sales tax and never buy.
/// </summary>
public class CityStaffStoreWiringTests
{
    private static readonly (CityStaffRole Role, string Area)[] Buildings =
    {
        (CityStaffRole.Clerk, "city_hall"),
        (CityStaffRole.Bartender, "cantina"),
        (CityStaffRole.Medic, "medical_center"),
        (CityStaffRole.Teller, "bank")
    };

    #region The rules

    [TestCase(CityStaffRole.Clerk, "cs_clerk", PropertyType.CityHall, CityStoreKind.CityHall)]
    [TestCase(CityStaffRole.Bartender, "cs_bartender", PropertyType.Cantina, CityStoreKind.Cantina)]
    [TestCase(CityStaffRole.Medic, "cs_medic", PropertyType.MedicalCenter, CityStoreKind.MedicalCenter)]
    [TestCase(CityStaffRole.Teller, "cs_teller", PropertyType.Bank, CityStoreKind.Invalid)]
    public void EachRole_HasABlueprintABuildingAndItsStore(CityStaffRole role, string blueprint, PropertyType building, CityStoreKind store)
    {
        CityStaffRules.GetBlueprint(role).Should().Be(blueprint);
        CityStaffRules.GetBuilding(role).Should().Be(building);
        CityStaffRules.GetStoreKind(role).Should().Be(store);
    }

    [Test]
    public void TheTellerOpensTheBank_SoItHasNoStore()
    {
        CityStaffRules.GetStoreKind(CityStaffRole.Teller).Should().Be(CityStoreKind.Invalid);
        CityStaffRules.GetServiceResponse(CityStaffRole.Teller).Should().Be("Open my bank account");
    }

    [Test]
    public void EveryRole_HasPlainAsciiPlayerText()
    {
        foreach (var role in Enum.GetValues<CityStaffRole>().Where(role => role != CityStaffRole.Invalid))
        {
            CityStaffRules.GetGreeting(role, "Test City").Should().MatchRegex("^[\\x20-\\x7E\\n]+$");
            CityStaffRules.GetServiceResponse(role).Should().MatchRegex("^[\\x20-\\x7E]+$");
        }
    }

    [Test]
    public void AGreeting_NamesTheCity_OrSaysTheCityWhenItHasNoName()
    {
        CityStaffRules.GetGreeting(CityStaffRole.Medic, "Port Nova").Should().Contain("Port Nova");
        CityStaffRules.GetGreeting(CityStaffRole.Medic, " ").Should().Contain("the city");
    }

    [Test]
    public void TheConversation_HasAMainPage()
    {
        new CityStaffDialog().Build().DefaultPageId.Should().Be("MAIN_PAGE");
    }

    #endregion

    #region The blueprints

    [Test]
    public void EveryStaffBlueprint_IsPlotAndOpensTheStaffConversation()
    {
        foreach (var (role, _) in Buildings)
        {
            var resref = CityStaffRules.GetBlueprint(role);
            var path = Path.Combine(Root, "Module", "utc", $"{resref}.utc.json");
            File.Exists(path).Should().BeTrue($"{resref} must be in the module");

            var utc = JObject.Parse(File.ReadAllText(path));
            Text(utc, "Tag").Should().Be(resref);
            Text(utc, "TemplateResRef").Should().Be(resref);
            utc["Plot"]!["value"]!.Value<int>().Should().Be(1, "staff cannot be harmed");
            utc["IsImmortal"]!["value"]!.Value<int>().Should().Be(1);
            Text(utc, "ScriptDialogue").Should().Be("x2_def_onconv");

            var locals = ((JArray)utc["VarTable"]!["value"]!).ToDictionary(
                local => local["Name"]!["value"]!.Value<string>()!,
                local => local["Value"]!["value"]!);
            locals["CONVERSATION"].Value<string>().Should().Be(nameof(CityStaffDialog));
            locals["CITY_STAFF_ROLE"].Value<int>().Should().Be((int)role);

            var name = utc["FirstName"]!["value"]!["0"]!.Value<string>();
            name.Should().NotBeNullOrWhiteSpace().And.MatchRegex("^[\\x20-\\x7E]+$");
            utc["Description"]!["value"]!["0"]!.Value<string>().Should().MatchRegex("^[\\x20-\\x7E]+$");
        }
    }

    [Test]
    public void EveryStaffBlueprint_IsInTheCreaturePalette()
    {
        var palette = File.ReadAllText(Path.Combine(Root, "Module", "itp", "creaturepalcus.itp.json"));

        foreach (var (role, _) in Buildings)
            palette.Should().Contain($"\"{CityStaffRules.GetBlueprint(role)}\"");
    }

    [Test]
    public void EveryStaffRole_HasADistinctLook()
    {
        var looks = Buildings.Select(building =>
        {
            var utc = JObject.Parse(File.ReadAllText(Path.Combine(Root, "Module", "utc", $"{CityStaffRules.GetBlueprint(building.Role)}.utc.json")));
            var equipment = ((JArray)utc["Equip_ItemList"]!["value"]!).Select(item => item["EquippedRes"]!["value"]!.Value<string>());
            return $"{utc["Appearance_Type"]!["value"]}|{utc["Appearance_Head"]?["value"]}|{string.Join(",", equipment)}";
        }).ToList();

        looks.Should().OnlyHaveUniqueItems();
    }

    #endregion

    #region The waypoints

    [Test]
    public void EachBuildingTemplate_HasOneStaffWaypointInsideTheBuilding()
    {
        foreach (var (_, area) in Buildings)
        {
            var git = JObject.Parse(File.ReadAllText(Path.Combine(Root, "Module", "git", $"{area}.git.json")));
            var are = JObject.Parse(File.ReadAllText(Path.Combine(Root, "Module", "are", $"{area}.are.json")));
            var waypoints = ((JArray)git["WaypointList"]!["value"]!).Where(w => Text(w, "Tag") == CityStaffRules.SpawnWaypointTag).ToList();
            waypoints.Should().HaveCount(1, $"{area} places one staff member");

            var waypoint = waypoints[0];
            var x = Number(waypoint, "XPosition");
            var y = Number(waypoint, "YPosition");
            x.Should().BeInRange(1, are["Width"]!["value"]!.Value<int>() * 10 - 1);
            y.Should().BeInRange(1, are["Height"]!["value"]!.Value<int>() * 10 - 1);
            Number(waypoint, "ZPosition").Should().Be(0, "every floor of these templates is at 0");

            var facing = Math.Sqrt(Math.Pow(Number(waypoint, "XOrientation"), 2) + Math.Pow(Number(waypoint, "YOrientation"), 2));
            facing.Should().BeApproximately(1, 0.0001);
        }
    }

    // The positions were taken from the tile walkmeshes: on the floor, reachable from the entrance with half a meter to
    // spare, and well clear of the furniture. A nearer furniture piece or waypoint means somebody moved one of them.
    [Test]
    public void AStaffWaypoint_StaysClearOfTheFurnitureAndTheOtherWaypoints()
    {
        foreach (var (_, area) in Buildings)
        {
            var git = JObject.Parse(File.ReadAllText(Path.Combine(Root, "Module", "git", $"{area}.git.json")));
            var waypoints = (JArray)git["WaypointList"]!["value"]!;
            var staff = waypoints.Single(w => Text(w, "Tag") == CityStaffRules.SpawnWaypointTag);
            var sx = Number(staff, "XPosition");
            var sy = Number(staff, "YPosition");

            var others = waypoints.Where(w => w != staff).Select(w => (Number(w, "XPosition"), Number(w, "YPosition")))
                .Concat(((JArray)git["Placeable List"]!["value"]!).Select(p => (Number(p, "X"), Number(p, "Y"))));

            foreach (var (x, y) in others)
                Math.Sqrt(Math.Pow(sx - x, 2) + Math.Pow(sy - y, 2)).Should().BeGreaterThan(1.4, $"{area} staff stands too near an object at {x:0.0},{y:0.0}");
        }
    }

    [Test]
    public void TheStarportKeepsItsTwoDockmasterWaypoints()
    {
        var git = JObject.Parse(File.ReadAllText(Path.Combine(Root, "Module", "git", "starport.git.json")));

        ((JArray)git["WaypointList"]!["value"]!).Count(w => Text(w, "Tag") == "DOCKHAND_SPAWN").Should().Be(2);
    }

    #endregion

    #region The wiring

    [Test]
    public void EachBuildingLayout_SpawnsItsStaffWithTheInstance()
    {
        Source("Feature", "PropertyLayoutDefinition", "CityHallLayoutDefinition.cs").Should().Contain("CityStaff.Spawn(area, CityStaffRole.Clerk)");
        Source("Feature", "PropertyLayoutDefinition", "CantinaLayoutDefinition.cs").Should().Contain("CityStaff.Spawn(area, CityStaffRole.Bartender)");
        Source("Feature", "PropertyLayoutDefinition", "MedicalCenterLayoutDefinition.cs").Should().Contain("CityStaff.Spawn(instance, CityStaffRole.Medic)");
        Source("Feature", "PropertyLayoutDefinition", "BankLayoutDefinition.cs").Should().Contain("CityStaff.Spawn(area, CityStaffRole.Teller)");
    }

    [Test]
    public void TheStarportDockmaster_IsPlotAndSellsTheCitysStoreWithTheTax()
    {
        var layout = Source("Feature", "PropertyLayoutDefinition", "StarportLayoutDefinition.cs");
        layout.Should().Contain("SetPlotFlag(npc, true);");
        layout.Should().Contain("CityStores.Create(city, CityStoreKind.Starport, location);");
        layout.Should().NotContain("\"dockhand_store\"", "a city starport sells the city store, not the dockhand store");

        var dialog = Source("Feature", "DialogDefinition", "StarportDialog.cs");
        dialog.Should().Contain("CityStores.Open(player, store);");
        dialog.Should().Contain("GetNearestObjectByTag(\"dockhand_store\", Owner)", "an NPC starport keeps its own dockhand store");
    }

    [Test]
    public void TheStoreHandlers_AreBoundToTheEventScripts()
    {
        var stores = Source("Service", "CityStores.cs");
        stores.Should().Contain("[NWNEventHandler(ScriptName.OnStoreRequestBuyBefore)]");
        stores.Should().Contain("[NWNEventHandler(ScriptName.OnStoreRequestBuyAfter)]");
        stores.Should().Contain("[NWNEventHandler(ScriptName.OnStoreRequestSellBefore)]");

        ScriptName.OnStoreRequestBuyBefore.Length.Should().BeLessThanOrEqualTo(16);
        ScriptName.OnStoreRequestBuyAfter.Length.Should().BeLessThanOrEqualTo(16);
        ScriptName.OnStoreRequestSellBefore.Length.Should().BeLessThanOrEqualTo(16);

        var registration = Source("Feature", "EventRegistration.cs");
        registration.Should().Contain("\"NWNX_ON_STORE_REQUEST_BUY_BEFORE\", ScriptName.OnStoreRequestBuyBefore");
        registration.Should().Contain("\"NWNX_ON_STORE_REQUEST_BUY_AFTER\", ScriptName.OnStoreRequestBuyAfter");
        registration.Should().Contain("\"NWNX_ON_STORE_REQUEST_SELL_BEFORE\", ScriptName.OnStoreRequestSellBefore");
    }

    [Test]
    public void ACityStore_OpensWithTheSalesTaxAsABonusMarkUp_AndCreditsTheTreasuryAfterTheSale()
    {
        var stores = Source("Service", "CityStores.cs");

        stores.Should().Contain("NWScript.OpenStore(store, player, percent);");
        stores.Should().Contain("SalesTaxRules.GetTaxPortion(price, percent)");
        stores.Should().Contain("city.Treasury += tax;");
        stores.Should().Contain("EventsPlugin.SkipEvent();");
    }

    [Test]
    public void ADeletedCity_TakesItsStockAllowancesWithIt()
    {
        Source("Service", "Property.cs").Should().Contain("CityStockAllowances.DeleteAll(property.Id);");
    }

    #endregion

    private static string Text(JToken token, string key) => token[key]!["value"]!.Value<string>();

    private static double Number(JToken token, string key) => token[key]!["value"]!.Value<double>();

    private static string Source(params string[] path)
    {
        var full = Path.Combine(new[] { Root, "SWLOR.Game.Server" }.Concat(path).ToArray());
        return File.ReadAllText(full).Replace("\r\n", "\n");
    }

    private static string Root
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                directory = directory.Parent;
            return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
        }
    }
}
