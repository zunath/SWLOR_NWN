using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Feature.PropertyLayoutDefinition
{
    public class StarportLayoutDefinition: IPropertyLayoutListDefinition
    {
        private readonly PropertyLayoutBuilder _builder = new();

        public Dictionary<PropertyLayoutType, PropertyLayout> Build()
        {
            Starport();

            return _builder.Build();
        }

        private static bool CanAccess(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player))
            {
                SendMessageToPC(player, "Only players can access starport facilities.");
                return false;
            }

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var terminal = OBJECT_SELF;
            var area = GetArea(terminal);
            var propertyId = Property.GetPropertyId(area);
            var dbProperty = DB.Get<WorldProperty>(propertyId);
            var dbBuilding = DB.Get<WorldProperty>(dbProperty.ParentPropertyId);
            var cityId = dbBuilding.ParentPropertyId;

            if (dbPlayer.CitizenPropertyId != cityId)
            {
                SendMessageToPC(player, "Only citizens may use starport facilities.");
                return false;
            }

            if (dbPlayer.PropertyOwedTaxes > 0)
            {
                SendMessageToPC(player, $"You owe {dbPlayer.PropertyOwedTaxes} credits in taxes to this city. You cannot use its facilities until these are paid. Use the Citizenship Terminal in City Hall to pay these.");
                return false;
            }

            return true;
        }

        [NWNEventHandler("prop_star_term")]
        public static void UsePropertyStarportTerminal()
        {
            var player = GetLastUsedBy();
            var terminal = OBJECT_SELF;
            if (!CanAccess(player))
                return;

            ExecuteScript("generic_convo", terminal);
        }

        private void SpawnStarportFlightTerminals(uint area, PlanetType planetType)
        {
            var planet = Planet.GetPlanetByType(planetType);
            void SpawnTerminal(uint waypoint)
            {
                var location = GetLocation(waypoint);
                var terminal = CreateObject(ObjectType.Placeable, "flights_terminal", location);
                SetPlotFlag(terminal, true);
                SetName(terminal, $"{planet.Name} Starport Flights Terminal");

                SetLocalInt(terminal, "CURRENT_LOCATION", (int)planetType);
                SetEventScript(terminal, EventScript.Placeable_OnUsed, "prop_star_term");
            }

            const string WaypointTag = "STARPORT_FLIGHTS_TERMINAL";
            var referenceObject = GetFirstObjectInArea(area);

            if (GetTag(referenceObject) == WaypointTag)
            {
                SpawnTerminal(referenceObject);
            }

            var count = 1;
            var waypoint = GetNearestObjectByTag(WaypointTag, referenceObject, count);
            while (GetIsObjectValid(waypoint))
            {
                SpawnTerminal(waypoint);

                count++;
                waypoint = GetNearestObjectByTag(WaypointTag, referenceObject, count);
            }
        }

        private uint GetLandingWaypoint(uint area)
        {
            var referenceObject = GetFirstObjectInArea(area);

            if (GetTag(referenceObject) == "STARSHIP_DOCKPOINT")
                return referenceObject;

            return GetNearestObjectByTag("STARSHIP_DOCKPOINT", referenceObject);
        }

        private void SpawnDockhands(uint area, PlanetType planetType)
        {
            var planet = Planet.GetPlanetByType(planetType);
            void SpawnDockhand(uint waypoint)
            {
                var location = GetLocation(waypoint);
                var landingWaypoint = GetLandingWaypoint(area);
                var npc = CreateObject(ObjectType.Creature, "spc_dockhand", location);
                CreateObject(ObjectType.Store, "dockhand_store", location);

                SetLocalString(npc, "STARPORT_TELEPORT_WAYPOINT", planet.SpaceOrbitWaypointTag);

                // Replace the waypoint string with an actual location.
                DeleteLocalString(npc, "STARPORT_LANDING_WAYPOINT");
                SetLocalLocation(npc, "STARPORT_LANDING_WAYPOINT", GetLocation(landingWaypoint));
            }

            const string WaypointTag = "DOCKHAND_SPAWN";
            var referenceObject = GetFirstObjectInArea(area);

            if (GetTag(referenceObject) == WaypointTag)
            {
                SpawnDockhand(referenceObject);
            }

            var count = 1;
            var waypoint = GetNearestObjectByTag(WaypointTag, referenceObject, count);
            while (GetIsObjectValid(waypoint))
            {
                SpawnDockhand(waypoint);

                count++;
                waypoint = GetNearestObjectByTag(WaypointTag, referenceObject, count);
            }
        }

        private void Starport()
        {
            _builder.Create(PropertyLayoutType.StarportStyle1)
                .PropertyType(PropertyType.Starport)
                .Name("Starport")
                .StructureLimit(80)
                .ItemStorageLimit(0)
                .BuildingLimit(0)
                .InitialPrice(0)
                .PricePerDay(0)
                .AreaInstance("starport")
                .OnSpawn(instance =>
                {
                    var propertyId = Property.GetPropertyId(instance);
                    var dbProperty = DB.Get<WorldProperty>(propertyId);
                    var dbBuilding = DB.Get<WorldProperty>(dbProperty.ParentPropertyId);
                    var dbCity = DB.Get<WorldProperty>(dbBuilding.ParentPropertyId);
                    var cityArea = Area.GetAreaByResref(dbCity.ParentPropertyId);
                    var planet = Planet.GetPlanetType(cityArea);

                    if (planet == PlanetType.Invalid)
                        return;

                    SpawnStarportFlightTerminals(instance, planet);
                    SpawnDockhands(instance, planet);

                    var dockPoint = GetLandingWaypoint(instance);
                    Space.RegisterLandingPoint(dockPoint, cityArea, false, propertyId);
                });
        }
    }
}
