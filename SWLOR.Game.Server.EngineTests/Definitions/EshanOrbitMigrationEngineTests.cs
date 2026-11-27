using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.EngineTests.Definitions;

/// <summary>
/// Ships saved with the old Eshan orbit, on the starlane arrival point, move to the waypoint's new spot beside the
/// Eshan dock, and keep it across restarts.
/// </summary>
public static class EshanOrbitMigrationEngineTests
{
    [EngineTest("Ships saved with the old Eshan orbit move beside the Eshan dock", Category = "Migration", TimeoutSeconds = 30f)]
    public static async Task SavedEshanOrbitsMoveBesideTheDock(EngineTestContext ctx)
    {
        var eshan = Planet.GetPlanetByType(PlanetType.Eshan);
        var orbit = GetWaypointByTag(eshan.SpaceOrbitWaypointTag);
        var starlane = GetWaypointByTag("STARLANE_LANDING_ESHAN_1");
        var dock = FindDock(GetArea(orbit), eshan.SpaceOrbitWaypointTag);
        var landing = GetWaypointByTag(eshan.LandingWaypointTag);
        var dantooineOrbit = GetWaypointByTag(Planet.GetPlanetByType(PlanetType.Dantooine).SpaceOrbitWaypointTag);
        ctx.Assert(GetIsObjectValid(orbit) && GetIsObjectValid(starlane) && GetIsObjectValid(dock), "the Eshan orbit, starlane arrival and dock resolve in the packed module");
        ctx.AssertEqual(_32_NextReleaseServerMigration.EshanOrbitAreaResref, GetResRef(GetArea(orbit)), "the Eshan orbit waypoint's area");
        ctx.Assert(GetDistanceBetween(orbit, dock) < 5f, $"the Eshan orbit waypoint sits beside its dock ({GetDistanceBetween(orbit, dock):0.00} m)");
        ctx.Assert(GetDistanceBetween(orbit, starlane) > 100f, "the Eshan orbit waypoint is away from the starlane arrival point");

        var oldOrbit = new PropertyLocation
        {
            AreaResref = _32_NextReleaseServerMigration.EshanOrbitAreaResref,
            X = _32_NextReleaseServerMigration.OldEshanOrbitX,
            Y = _32_NextReleaseServerMigration.OldEshanOrbitY,
            Z = 3f,
            Orientation = 35.9f
        };
        var legacy = CreateShip(new()
        {
            [PropertyLocationType.DockPosition] = ToPropertyLocation(landing),
            [PropertyLocationType.SpacePosition] = Copy(oldOrbit),
            [PropertyLocationType.LastNPCDockPosition] = ToPropertyLocation(landing),
            [PropertyLocationType.LastNPCSpacePosition] = Copy(oldOrbit),
        });
        var elsewhere = CreateShip(new()
        {
            [PropertyLocationType.SpacePosition] = ToPropertyLocation(dantooineOrbit),
            [PropertyLocationType.LastNPCSpacePosition] = ToPropertyLocation(dantooineOrbit),
        });

        try
        {
            var context = ctx.SpawnCreature("nw_bandit001");
            await ctx.WaitFrameAsync();
            for (var run = 1; run <= 2; run++)
            {
                new _32_NextReleaseServerMigration().Migrate();
                // A restart's module-load cleanup must keep the new orbit.
                await ctx.ExecuteInCreatureContextAsync(context, RunCleanUpData);

                var positions = DB.Get<WorldProperty>(legacy.Id).Positions;
                AssertAt(ctx, positions[PropertyLocationType.SpacePosition], orbit, $"run {run}: a docked ship launches beside the Eshan dock");
                AssertAt(ctx, positions[PropertyLocationType.LastNPCSpacePosition], orbit, $"run {run}: its safe orbit is beside the Eshan dock");
                AssertAt(ctx, positions[PropertyLocationType.DockPosition], landing, $"run {run}: it stays docked at the Eshan starport");

                positions = DB.Get<WorldProperty>(elsewhere.Id).Positions;
                AssertAt(ctx, positions[PropertyLocationType.SpacePosition], dantooineOrbit, $"run {run}: a ship at another planet keeps its orbit");
            }
        }
        finally
        {
            foreach (var property in new[] { legacy, elsewhere })
            {
                if (DB.Exists<WorldProperty>(property.Id))
                    DB.Delete<WorldProperty>(property.Id);
            }
        }

        ctx.SetResultDetail("Server migration 32 moved a ship's saved launch orbit and safe orbit from the old Eshan spot on the starlane arrival point to the Eshan_Orbit waypoint beside the dock, left a Dantooine ship alone, and the result held across two migration runs and module-load cleanups.");
    }

    private static uint FindDock(uint area, string orbitTag)
    {
        for (var obj = GetFirstObjectInArea(area); GetIsObjectValid(obj); obj = GetNextObjectInArea(area))
        {
            if (GetTag(obj) == "spc_dock" && GetLocalString(obj, "STARPORT_TELEPORT_WAYPOINT") == orbitTag)
                return obj;
        }

        return OBJECT_INVALID;
    }

    private static WorldProperty CreateShip(Dictionary<PropertyLocationType, PropertyLocation> positions)
    {
        var property = new WorldProperty
        {
            CustomName = "Engine Test Eshan Starship",
            PropertyType = PropertyType.Starship,
            Positions = positions
        };
        DB.Set(property);
        return property;
    }

    private static void RunCleanUpData() => PropertyMaintenance.CleanUpData();

    private static PropertyLocation ToPropertyLocation(uint waypoint)
    {
        var position = GetPosition(waypoint);
        return new PropertyLocation
        {
            AreaResref = GetResRef(GetArea(waypoint)),
            X = position.X,
            Y = position.Y,
            Z = position.Z,
            Orientation = GetFacing(waypoint)
        };
    }

    private static PropertyLocation Copy(PropertyLocation location) => new()
    {
        AreaResref = location.AreaResref,
        X = location.X,
        Y = location.Y,
        Z = location.Z,
        Orientation = location.Orientation
    };

    private static void AssertAt(EngineTestContext ctx, PropertyLocation location, uint waypoint, string label)
    {
        var position = GetPosition(waypoint);
        ctx.Assert(location != null &&
                   location.AreaResref == GetResRef(GetArea(waypoint)) &&
                   System.Math.Abs(location.X - position.X) < 0.01f &&
                   System.Math.Abs(location.Y - position.Y) < 0.01f,
            $"{label}: expected {GetTag(waypoint)} at ({position.X:0.00}, {position.Y:0.00}), was {location?.AreaResref} ({location?.X:0.00}, {location?.Y:0.00})");
    }
}
