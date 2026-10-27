using System.Numerics;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.World;

/// <summary>The scripted world and the scenes the tests of the property service share.</summary>
public abstract class PropertyServiceTestBase
{
    protected const uint Player = 5001;
    protected const string PlayerId = "player-uuid-1";
    protected const uint OtherPlayer = 5002;
    protected const string OtherPlayerId = "player-uuid-2";
    protected const uint Item = 7300;
    protected const uint Terminal = 7400;

    protected PropertyWorld World { get; private set; } = null!;

    protected PropertyWorld.ServiceUnderTest Service => World.Service;

    protected InMemoryDBService Db => World.Db;

    [SetUp]
    public void SetUpWorld()
    {
        World = new PropertyWorld();
        World.AddPlayer(Player, PlayerId, "Tester");
        World.Engine.SetReturn("OBJECT_SELF", Player);
        World.Engine.SetReturn("GetLastUsedBy", Player);
    }

    [TearDown]
    public void TearDownWorld() => World.Dispose();

    /// <summary>An area the player stands in, marked with the id of a property.</summary>
    protected uint InsideProperty(WorldProperty property, uint area = 8100)
    {
        World.AddArea(area, "inside_" + area);
        Service.AssignPropertyId(area, property.Id);
        World.SetArea(Player, area);
        return area;
    }

    protected WorldPropertyPermission Permit(string playerId, string propertyId, params PropertyPermissionType[] granted)
    {
        var permission = new WorldPropertyPermission { PlayerId = playerId, PropertyId = propertyId };
        foreach (var type in granted)
        {
            permission.Permissions[type] = true;
            permission.GrantPermissions[type] = true;
        }

        Db.With(permission);
        return permission;
    }

    protected WorldProperty StoreProperty(PropertyType type, PropertyLayoutType layout, string owner = PropertyWorld.OwnerId, string parentId = "")
    {
        var property = new WorldProperty
        {
            CustomName = type.ToString(),
            PropertyType = type,
            OwnerPlayerId = owner,
            Layout = layout,
            ParentPropertyId = parentId
        };
        Db.With(property);
        return property;
    }

    protected WorldProperty StoreStructure(string parentId, StructureType type = StructureType.Jukebox)
    {
        var structure = new WorldProperty
        {
            CustomName = "Structure",
            PropertyType = PropertyType.Structure,
            StructureType = type,
            OwnerPlayerId = PropertyWorld.OwnerId,
            ParentPropertyId = parentId
        };
        Db.With(structure);
        return structure;
    }

    protected WorldPropertyCategory StoreCategory(string propertyId, string name = "Weapons")
    {
        var category = new WorldPropertyCategory { ParentPropertyId = propertyId, Name = name };
        Db.With(category);
        return category;
    }

    protected Player StorePlayer(string id, string cityId = "")
    {
        var player = new Player { Id = id, Name = "Player " + id, CitizenPropertyId = cityId };
        Db.With(player);
        return player;
    }

    /// <summary>The planet, its area and its two NPC starport waypoints, as <c>GetPlanetStarport</c> finds them.</summary>
    protected (uint Area, string Resref, PlanetAttribute Planet) AddPlanetWithStarport(PlanetType type, string areaResref, uint area, uint landing, uint orbit)
    {
        World.AddArea(area, areaResref);
        World.Planets.Types[area] = type;
        var planet = World.Planets.GetPlanetByType(type);
        World.AddWaypoint(planet.LandingWaypointTag, landing, area, new Vector3(1, 2, 3), 90f);
        World.AddWaypoint(planet.SpaceOrbitWaypointTag, orbit, World.AddArea(area + 1, areaResref + "_orbit"), new Vector3(4, 5, 6), 180f);
        return (area, areaResref, planet);
    }
}
