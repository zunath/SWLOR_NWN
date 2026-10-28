using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.StationService;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class PlayerStationEngineTests
{
    private const string ContextCreatureResref = "nw_bandit001";

    [EngineTest("Every station anchor site is a waypoint in its space area", Category = "PlayerStation")]
    public static Task AnchorsArePlacedInTheModule(EngineTestContext ctx)
    {
        foreach (var anchor in StationTuning.Anchors)
        {
            var waypoint = GetWaypointByTag(anchor.Tag);
            ctx.Assert(GetIsObjectValid(waypoint), $"{anchor.Tag} exists");
            ctx.AssertEqual(anchor.AreaResref, GetResRef(GetArea(waypoint)), $"{anchor.Tag} area");
        }

        ctx.Assert(GetIsObjectValid(Area.GetAreaByResref(StationTuning.DeckAreaResref)) ||
                   Property.GetAllInstanceAreaResrefs().Contains(StationTuning.DeckAreaResref),
            "the station deck is an instance template");
        return Task.CompletedTask;
    }

    [EngineTest("A station's exterior stands beside its anchor with a docking point for its Starports", Category = "PlayerStation")]
    public static async Task ExteriorSpawnsAtTheAnchor(EngineTestContext ctx)
    {
        var anchor = StationRules.GetAnchor("Dantooine1");
        var station = CreateStation(anchor.Id);

        try
        {
            await RunInScriptContextAsync(ctx, () => Station.SpawnExterior(station));

            var anchorWaypoint = GetWaypointByTag(anchor.Tag);
            var dockingPoint = GetNearestObjectByTag("spc_dock", anchorWaypoint);
            ctx.Assert(GetIsObjectValid(dockingPoint), "a docking point stands at the anchor");
            ctx.AssertEqual(station.Id, GetLocalString(dockingPoint, Station.StationCityVariable), "the docking point names the station");
            ctx.AssertEqual(anchor.Tag, GetLocalString(dockingPoint, "STARPORT_TELEPORT_WAYPOINT"), "ships launch at the anchor");
            ctx.AssertEqual((int)anchor.HomePlanet, GetLocalInt(dockingPoint, "PLANET_TYPE_ID"), "the docking point serves the home port");
            ctx.Assert(GetDistanceBetween(dockingPoint, anchorWaypoint) < 1f, "the docking point is at the anchor");

            var model = GetNearestObjectByTag(StationTuning.ExteriorResref, anchorWaypoint);
            ctx.Assert(GetIsObjectValid(model), "the station model stands near the anchor");
            ctx.AssertEqual(station.CustomName, GetName(model), "the station model carries the station's name");
            ctx.Assert(Station.GetStationAtAnchor(anchor.Id)?.Id == station.Id, "the anchor site is taken");
        }
        finally
        {
            await RunInScriptContextAsync(ctx, () => Station.DespawnExterior(station.Id));
            DeleteAll(new[] { station });
        }
    }

    [EngineTest("Retrieving a station's Starport returns ships with no safe dock to the station's home port", Category = "PlayerStation")]
    public static async Task StarportRetrievalUsesTheHomePort(EngineTestContext ctx)
    {
        var station = CreateStation("Tatooine2");
        var building = CreateProperty(PropertyType.Structure, station.Id);
        building.StructureType = StructureType.Starport;
        var interior = CreateProperty(PropertyType.Starport, building.Id);
        building.ChildPropertyIds[PropertyChildType.Interior] = new List<string> { interior.Id };
        DB.Set(building);
        var ship = CreateProperty(PropertyType.Starship, string.Empty);
        ship.ChildPropertyIds[PropertyChildType.RegisteredStarport] = new List<string> { interior.Id };
        ship.Positions[PropertyLocationType.DockPosition] = new PropertyLocation { InstancePropertyId = interior.Id };
        ship.Positions[PropertyLocationType.SpacePosition] = new PropertyLocation { AreaResref = "tatooineorbit" };
        DB.Set(ship);
        interior.ChildPropertyIds[PropertyChildType.Starship] = new List<string> { ship.Id };
        DB.Set(interior);
        var created = new List<WorldProperty> { station, building, interior, ship };

        try
        {
            await RunInScriptContextAsync(ctx, () =>
                Property.RunStructureChangedEvent(StructureType.Starport, StructureChangeType.Retrieved, building, OBJECT_INVALID));

            var tatooine = Planet.GetPlanetByType(PlanetType.Tatooine);
            var landing = GetResRef(GetArea(GetWaypointByTag(tatooine.LandingWaypointTag)));
            ctx.AssertEqual(landing, DB.Get<WorldProperty>(ship.Id).Positions[PropertyLocationType.DockPosition].AreaResref,
                "a ship with no safe dock goes to the station's home port");
        }
        finally
        {
            DeleteAll(created);
        }
    }

    private static WorldProperty CreateStation(string anchorId)
    {
        var anchor = StationRules.GetAnchor(anchorId);
        var station = new WorldProperty
        {
            CustomName = "Engine Test Station",
            PropertyType = PropertyType.City,
            Layout = PropertyLayoutType.Station,
            ParentPropertyId = anchor.AreaResref,
            StationAnchorId = anchor.Id,
        };
        station.Upgrades[PropertyUpgradeType.CityLevel] = 2;
        DB.Set(station);
        return station;
    }

    private static async Task RunInScriptContextAsync(EngineTestContext ctx, Action action)
    {
        var creature = ctx.SpawnCreature(ContextCreatureResref);
        await ctx.WaitFrameAsync();
        await ctx.ExecuteInCreatureContextAsync(creature, action);
    }

    private static WorldProperty CreateProperty(PropertyType type, string parentPropertyId)
    {
        var property = new WorldProperty
        {
            CustomName = $"Engine Test {type}",
            PropertyType = type,
            ParentPropertyId = parentPropertyId
        };
        DB.Set(property);
        return property;
    }

    private static void DeleteAll(IEnumerable<WorldProperty> properties)
    {
        foreach (var property in properties)
        {
            if (DB.Exists<WorldProperty>(property.Id))
                DB.Delete<WorldProperty>(property.Id);
        }
    }
}
