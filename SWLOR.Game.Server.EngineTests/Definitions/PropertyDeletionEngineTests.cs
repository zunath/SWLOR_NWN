using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
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

    [EngineTest("Retrieving a player starport relocates docked ships and skips deleted ones", Category = "PropertyDeletion")]
    public static async Task StarportRetrievalSkipsDeletedShips(EngineTestContext ctx)
    {
        var city = CreateCity();
        var (building, interior) = CreateStarport(city, listInCity: true);
        var ship = CreateDockedShip(interior);
        interior.ChildPropertyIds[PropertyChildType.Starship] = new List<string> { Guid.NewGuid().ToString(), ship.Id };
        DB.Set(interior);
        var created = new List<WorldProperty> { city, building, interior, ship };

        try
        {
            await RunInScriptContextAsync(ctx, () =>
                Property.RunStructureChangedEvent(StructureType.Starport, StructureChangeType.Retrieved, building, OBJECT_INVALID));

            var dbShip = DB.Get<WorldProperty>(ship.Id);
            ctx.AssertEqual(string.Empty, dbShip.Positions[PropertyLocationType.DockPosition].InstancePropertyId,
                "docked ship no longer points at the retrieved starport");
            ctx.AssertEqual("npc_dock", dbShip.Positions[PropertyLocationType.DockPosition].AreaResref,
                "docked ship returns to its last NPC dock");
            ctx.AssertEqual(0, dbShip.ChildPropertyIds[PropertyChildType.RegisteredStarport].Count,
                "docked ship's starport registration is cleared");
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

    private static WorldProperty CreateCity()
    {
        return CreateProperty(PropertyType.City, CityAreaResref);
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
        ship.Positions[PropertyLocationType.LastNPCDockPosition] = new PropertyLocation { AreaResref = "npc_dock" };
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
