using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PropertyService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

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

        private void SpawnStarportFlightTerminals(uint area, PlanetType planetType)
        {
            var planet = Planet.GetPlanetByType(planetType);
            void SpawnTerminal(uint waypoint)
            {
                var location = GetLocation(waypoint);
                var terminal = CreateObject(ObjectType.Placeable, "flights_terminal", location);
                SetName(terminal, $"{planet.Name} Starport Flights Terminal");

                SetLocalInt(terminal, "CURRENT_LOCATION", (int)planetType);
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

        private void SpawnDockhands(uint area, PlanetType planet)
        {

        }

        private void Starport()
        {
            _builder.Create(PropertyLayoutType.StarportStyle1)
                .PropertyType(PropertyType.Starport)
                .Name("Starport")
                .StructureLimit(30)
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
                    var area = Cache.GetAreaByResref(dbCity.ParentPropertyId);
                    var planet = Planet.GetPlanetType(area);

                    if (planet == PlanetType.Invalid)
                        return;

                    SpawnStarportFlightTerminals(instance, planet);
                    SpawnDockhands(instance, planet);
                });
        }
    }
}
