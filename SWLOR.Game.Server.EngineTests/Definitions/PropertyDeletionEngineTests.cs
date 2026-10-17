using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class PropertyDeletionEngineTests
{
    private const string ContextCreatureResref = "nw_bandit001";
    private const string CityAreaResref = "property_deletion_engine_test";

    [EngineTest("Deleting a city removes buildings and interiors missing from its child lists", Category = "PropertyDeletion")]
    public static async Task CityDeletionRemovesUnlistedChildren(EngineTestContext ctx)
    {
        var city = CreateCity();
        var (listedBuilding, listedInterior) = CreateStarport(city, listInCity: true);
        var (unlistedBuilding, unlistedInterior) = CreateStarport(city, listInCity: false);
        var unlistedFurniture = CreateProperty(PropertyType.Structure, unlistedInterior.Id);
        var created = new List<WorldProperty> { city, listedBuilding, listedInterior, unlistedBuilding, unlistedInterior, unlistedFurniture };

        try
        {
            await RunInScriptContextAsync(ctx, () => Property.DeleteProperty(city));

            foreach (var property in created)
            {
                ctx.Assert(DB.Get<WorldProperty>(property.Id) == null,
                    $"{property.PropertyType} '{property.CustomName}' is deleted with the city");
            }
        }
        finally
        {
            DeleteAll(created);
        }
    }

    [EngineTest("Retrieving a player starport returns docked ships to their safe dock and orbit and skips deleted ones", Category = "PropertyDeletion")]
    public static async Task StarportRetrievalSkipsDeletedShips(EngineTestContext ctx)
    {
        // A real Viscara area, so a ship with no safe dock can fall back to the planet's starport.
        var city = CreateCity("velesinterior");
        var (building, interior) = CreateStarport(city, listInCity: true);
        var (otherBuilding, otherInterior) = CreateStarport(city, listInCity: true);
        var ship = CreateDockedShip(interior);
        // A stale entry: the ship has since docked and registered at another starport.
        var dockedElsewhere = CreateDockedShip(otherInterior);
        // Registered before safe orbits were recorded.
        var withoutSafeOrbit = CreateDockedShip(interior);
        withoutSafeOrbit.Positions.Remove(PropertyLocationType.LastNPCSpacePosition);
        DB.Set(withoutSafeOrbit);
        var withoutSafeDock = CreateDockedShip(interior);
        withoutSafeDock.Positions.Remove(PropertyLocationType.LastNPCDockPosition);
        withoutSafeDock.Positions.Remove(PropertyLocationType.LastNPCSpacePosition);
        DB.Set(withoutSafeDock);
        interior.ChildPropertyIds[PropertyChildType.Starship] = new List<string>
        {
            Guid.NewGuid().ToString(), withoutSafeDock.Id, dockedElsewhere.Id, withoutSafeOrbit.Id, ship.Id
        };
        DB.Set(interior);
        var created = new List<WorldProperty>
        {
            city, building, interior, otherBuilding, otherInterior, ship, dockedElsewhere, withoutSafeOrbit, withoutSafeDock
        };

        try
        {
            await RunInScriptContextAsync(ctx, () =>
                Property.RunStructureChangedEvent(StructureType.Starport, StructureChangeType.Retrieved, building, OBJECT_INVALID));

            var dbShip = DB.Get<WorldProperty>(ship.Id);
            ctx.AssertEqual(string.Empty, dbShip.Positions[PropertyLocationType.DockPosition].InstancePropertyId,
                "docked ship no longer points at the retrieved starport");
            ctx.AssertEqual("npc_dock", dbShip.Positions[PropertyLocationType.DockPosition].AreaResref,
                "docked ship returns to its last NPC dock");
            ctx.AssertEqual("npc_orbit", dbShip.Positions[PropertyLocationType.SpacePosition].AreaResref,
                "docked ship launches from its last NPC dock's orbit");
            ctx.AssertEqual(0, dbShip.ChildPropertyIds[PropertyChildType.RegisteredStarport].Count,
                "docked ship's starport registration is cleared");

            var dbWithoutSafeOrbit = DB.Get<WorldProperty>(withoutSafeOrbit.Id);
            ctx.AssertEqual("npc_dock", dbWithoutSafeOrbit.Positions[PropertyLocationType.DockPosition].AreaResref,
                "a ship without a recorded safe orbit still returns to its last NPC dock");
            ctx.AssertEqual("starport_orbit", dbWithoutSafeOrbit.Positions[PropertyLocationType.SpacePosition].AreaResref,
                "a ship without a recorded safe orbit keeps its orbit");

            var viscara = Planet.GetPlanetByType(PlanetType.Viscara);
            var velesLanding = GetResRef(GetArea(GetWaypointByTag(viscara.LandingWaypointTag)));
            var viscaraOrbit = GetResRef(GetArea(GetWaypointByTag(viscara.SpaceOrbitWaypointTag)));
            var dbWithoutSafeDock = DB.Get<WorldProperty>(withoutSafeDock.Id);
            ctx.AssertEqual(velesLanding, dbWithoutSafeDock.Positions[PropertyLocationType.DockPosition].AreaResref,
                "a ship with no safe dock goes to the starport of the city's planet");
            ctx.AssertEqual(viscaraOrbit, dbWithoutSafeDock.Positions[PropertyLocationType.SpacePosition].AreaResref,
                "a ship with no safe dock launches from that planet's orbit");
            ctx.AssertEqual(velesLanding, dbWithoutSafeDock.Positions[PropertyLocationType.LastNPCDockPosition].AreaResref,
                "the planet's starport becomes the safe dock");
            ctx.AssertEqual(0, dbWithoutSafeDock.ChildPropertyIds[PropertyChildType.RegisteredStarport].Count,
                "a ship with no safe dock loses its registration at the retrieved starport");

            var dbDockedElsewhere = DB.Get<WorldProperty>(dockedElsewhere.Id);
            ctx.AssertEqual(otherInterior.Id, dbDockedElsewhere.Positions[PropertyLocationType.DockPosition].InstancePropertyId,
                "a ship docked at another starport stays there");
            ctx.Assert(dbDockedElsewhere.ChildPropertyIds[PropertyChildType.RegisteredStarport].SequenceEqual(new[] { otherInterior.Id }),
                "a ship docked at another starport keeps that starport's registration");
        }
        finally
        {
            DeleteAll(created);
        }
    }

    [EngineTest("Deleting a starship removes it from its player starport's docked ships", Category = "PropertyDeletion")]
    public static async Task StarshipDeletionUnregistersFromStarport(EngineTestContext ctx)
    {
        var city = CreateCity();
        var (building, interior) = CreateStarport(city, listInCity: true);
        var ship = CreateDockedShip(interior);
        var otherShipId = Guid.NewGuid().ToString();
        interior.ChildPropertyIds[PropertyChildType.Starship] = new List<string> { ship.Id, otherShipId };
        DB.Set(interior);
        var created = new List<WorldProperty> { city, building, interior, ship };

        try
        {
            await RunInScriptContextAsync(ctx, () => Property.DeleteProperty(ship));

            ctx.Assert(DB.Get<WorldProperty>(ship.Id) == null, "starship is deleted");
            var dockedShips = DB.Get<WorldProperty>(interior.Id).ChildPropertyIds[PropertyChildType.Starship];
            ctx.Assert(!dockedShips.Contains(ship.Id), "deleted starship is removed from the starport");
            ctx.Assert(dockedShips.Contains(otherShipId), "other docked ships stay registered");
            ctx.Assert(DB.Get<WorldProperty>(interior.Id) != null, "starport is kept");
        }
        finally
        {
            DeleteAll(created);
        }
    }

    private static async Task RunInScriptContextAsync(EngineTestContext ctx, Action action)
    {
        var creature = ctx.SpawnCreature(ContextCreatureResref);
        await ctx.WaitFrameAsync();
        await ctx.ExecuteInCreatureContextAsync(creature, action);
    }

    private static WorldProperty CreateCity(string areaResref = CityAreaResref)
    {
        return CreateProperty(PropertyType.City, areaResref);
    }

    private static (WorldProperty Building, WorldProperty Interior) CreateStarport(WorldProperty city, bool listInCity)
    {
        var building = CreateProperty(PropertyType.Structure, city.Id);
        building.StructureType = StructureType.Starport;
        var interior = CreateProperty(PropertyType.Starport, building.Id);
        building.ChildPropertyIds[PropertyChildType.Interior] = new List<string> { interior.Id };
        DB.Set(building);

        if (listInCity)
        {
            if (!city.ChildPropertyIds.ContainsKey(PropertyChildType.Structure))
                city.ChildPropertyIds[PropertyChildType.Structure] = new List<string>();

            city.ChildPropertyIds[PropertyChildType.Structure].Add(building.Id);
            DB.Set(city);
        }

        return (building, interior);
    }

    private static WorldProperty CreateDockedShip(WorldProperty starport)
    {
        var ship = CreateProperty(PropertyType.Starship, string.Empty);
        ship.ChildPropertyIds[PropertyChildType.RegisteredStarport] = new List<string> { starport.Id };
        ship.Positions[PropertyLocationType.DockPosition] = new PropertyLocation { InstancePropertyId = starport.Id };
        ship.Positions[PropertyLocationType.SpacePosition] = new PropertyLocation { AreaResref = "starport_orbit" };
        ship.Positions[PropertyLocationType.LastNPCDockPosition] = new PropertyLocation { AreaResref = "npc_dock" };
        ship.Positions[PropertyLocationType.LastNPCSpacePosition] = new PropertyLocation { AreaResref = "npc_orbit" };
        DB.Set(ship);
        return ship;
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
