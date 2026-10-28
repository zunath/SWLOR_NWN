using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.DialogDefinition;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.ShuttleService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.StationService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Player stations (plan section 25). A station is a player city (<see cref="PropertyType.City"/> with the
    /// <see cref="PropertyLayoutType.Station"/> layout) founded at an anchor site in space. Its land is an instance of the
    /// station deck, and its exterior is a station model with a docking point beside the anchor. Docking, the exchange and
    /// the shuttle all go through its Starport; everything else is the city's.
    /// </summary>
    public static class Station
    {
        /// <summary>Local string on a station's docking point and flights terminals: the station's city property id.</summary>
        public const string StationCityVariable = "STATION_CITY_ID";

        private const string DockingPointResref = "spc_dock";

        private static readonly Dictionary<string, (uint Model, uint DockingPoint)> _exteriors = new();
        private static readonly Dictionary<string, Dictionary<string, ShipDockPoint>> _dockPoints = new();

        /// <summary>Whether the property is a player station.</summary>
        public static bool IsStation(WorldProperty property)
        {
            return property != null &&
                   property.PropertyType == PropertyType.City &&
                   property.Layout == PropertyLayoutType.Station;
        }

        /// <summary>The anchor a station stands at, or null for anything else.</summary>
        public static StationAnchor GetAnchor(WorldProperty station)
        {
            return IsStation(station) ? StationRules.GetAnchor(station.StationAnchorId) : null;
        }

        /// <summary>The station standing at an anchor site, or null when the site is free.</summary>
        public static WorldProperty GetStationAtAnchor(string anchorId)
        {
            if (string.IsNullOrWhiteSpace(anchorId))
                return null;

            var query = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.StationAnchorId), anchorId, false)
                .AddFieldSearch(nameof(WorldProperty.IsQueuedForDeletion), false);
            return DB.Search(query).FirstOrDefault(IsStation);
        }

        /// <summary>How many Starport buildings stand on a city's land.</summary>
        public static int CountStarports(string cityId)
        {
            return (int)DB.SearchCount(new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.ParentPropertyId), cityId, false)
                .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)PropertyType.Structure)
                .AddFieldSearch(nameof(WorldProperty.StructureType), (int)StructureType.Starport));
        }

        /// <summary>The city a building's interior belongs to (interior, then building structure, then city).</summary>
        public static WorldProperty GetCityOfInterior(string interiorPropertyId)
        {
            var interior = string.IsNullOrWhiteSpace(interiorPropertyId) ? null : DB.Get<WorldProperty>(interiorPropertyId);
            var building = interior == null || string.IsNullOrWhiteSpace(interior.ParentPropertyId)
                ? null
                : DB.Get<WorldProperty>(interior.ParentPropertyId);
            return building == null || string.IsNullOrWhiteSpace(building.ParentPropertyId)
                ? null
                : DB.Get<WorldProperty>(building.ParentPropertyId);
        }

        /// <summary>Whether a building interior stands on a player station's deck.</summary>
        public static bool IsStationInterior(string interiorPropertyId)
        {
            return IsStation(GetCityOfInterior(interiorPropertyId));
        }

        /// <summary>
        /// The planet a city's starport serves: a planet city's planet, or a station's home port (its orbit's NPC dock, or the
        /// nearest open port in Wild Space).
        /// </summary>
        public static PlanetType GetCityPlanet(WorldProperty city)
        {
            if (city == null)
                return PlanetType.Invalid;

            var anchor = GetAnchor(city);
            if (anchor != null)
                return anchor.HomePlanet;

            return Planet.GetPlanetType(Area.GetAreaByResref(city.ParentPropertyId));
        }

        /// <summary>
        /// An area on the planet a city's starport serves, for returning ships: a planet city's own area, or the landing area
        /// of a station's home port.
        /// </summary>
        public static string GetCityPlanetAreaResref(WorldProperty city)
        {
            var anchor = GetAnchor(city);
            if (anchor == null)
                return city?.ParentPropertyId ?? string.Empty;

            var landing = GetWaypointByTag(Planet.GetPlanetByType(anchor.HomePlanet).LandingWaypointTag);
            return GetIsObjectValid(landing) ? GetResRef(GetArea(landing)) : string.Empty;
        }

        /// <summary>
        /// Spawns a station's exterior beside its anchor: the station model, scaled by the city level, and the docking point
        /// pilots use to dock at its Starport.
        /// </summary>
        public static void SpawnExterior(WorldProperty station)
        {
            DespawnExterior(station.Id);

            var anchor = GetAnchor(station);
            var area = anchor == null ? OBJECT_INVALID : Area.GetAreaByResref(anchor.AreaResref);
            var anchorWaypoint = anchor == null ? OBJECT_INVALID : GetWaypointByTag(anchor.Tag);
            if (!GetIsObjectValid(area) || !GetIsObjectValid(anchorWaypoint))
            {
                Log.Write(LogGroup.Error, $"Station '{station.CustomName}' ({station.Id}) has no anchor site '{station.StationAnchorId}' to stand at.");
                return;
            }

            var (x, y) = StationRules.GetExteriorPosition(anchor);
            var z = GetPosition(anchorWaypoint).Z;
            var modelLocation = Location(area, Vector3(x, y, z), 0f);
            var model = CreateObject(ObjectType.Placeable, StationTuning.ExteriorResref, modelLocation);
            SetName(model, station.CustomName);
            SetPlotFlag(model, true);
            station.Upgrades.TryGetValue(PropertyUpgradeType.CityLevel, out var cityLevel);
            SetObjectVisualTransform(model, ObjectVisualTransform.Scale, StationRules.GetExteriorScale(cityLevel));

            var dockingPoint = CreateObject(ObjectType.Placeable, DockingPointResref, GetLocation(anchorWaypoint));
            SetName(dockingPoint, $"Docking Point: {station.CustomName}");
            SetPlotFlag(dockingPoint, true);
            SetLocalInt(dockingPoint, "PLANET_TYPE_ID", (int)anchor.HomePlanet);
            SetLocalString(dockingPoint, "STARPORT_TELEPORT_WAYPOINT", anchor.Tag);
            SetLocalString(dockingPoint, StationCityVariable, station.Id);

            _exteriors[station.Id] = (model, dockingPoint);
        }

        /// <summary>Removes a station's exterior from space.</summary>
        public static void DespawnExterior(string stationId)
        {
            if (!_exteriors.TryGetValue(stationId, out var exterior))
                return;

            if (GetIsObjectValid(exterior.Model))
                DestroyObject(exterior.Model);
            if (GetIsObjectValid(exterior.DockingPoint))
                DestroyObject(exterior.DockingPoint);

            _exteriors.Remove(stationId);
        }

        /// <summary>Renames a station's exterior after the city is renamed.</summary>
        public static void RenameExterior(WorldProperty station)
        {
            if (!_exteriors.TryGetValue(station.Id, out var exterior))
                return;

            SetName(exterior.Model, station.CustomName);
            SetName(exterior.DockingPoint, $"Docking Point: {station.CustomName}");
        }

        /// <summary>Registers a station Starport's dock so the station's docking point lists it.</summary>
        public static void RegisterDockPoint(string stationId, uint waypoint, string starportPropertyId)
        {
            RemoveDockPoint(starportPropertyId);

            if (!_dockPoints.ContainsKey(stationId))
                _dockPoints[stationId] = new Dictionary<string, ShipDockPoint>();

            _dockPoints[stationId][starportPropertyId] = new ShipDockPoint
            {
                Location = GetLocation(waypoint),
                Name = string.Empty,
                PropertyId = starportPropertyId,
                IsNPC = false
            };
        }

        /// <summary>Removes a station Starport's dock.</summary>
        public static void RemoveDockPoint(string starportPropertyId)
        {
            foreach (var docks in _dockPoints.Values)
            {
                docks.Remove(starportPropertyId);
            }
        }

        /// <summary>The docks of a station's Starports, keyed by starport property id.</summary>
        public static Dictionary<string, ShipDockPoint> GetDockPoints(string stationId)
        {
            return _dockPoints.TryGetValue(stationId, out var docks)
                ? docks.ToDictionary(x => x.Key, x => x.Value)
                : new Dictionary<string, ShipDockPoint>();
        }

        /// <summary>The shuttle stop of a station, or the default stop when the id is not a station.</summary>
        public static ShuttleStop GetShuttleStop(string stationId)
        {
            var station = string.IsNullOrWhiteSpace(stationId) ? null : DB.Get<WorldProperty>(stationId);
            var anchor = GetAnchor(station);
            return anchor == null ? default : ShuttleStop.ForStation(station.Id, anchor.HomePlanet, anchor.IsWildSpace);
        }

        /// <summary>The name of a shuttle stop: the planet's, or the station's.</summary>
        public static string GetStopName(ShuttleStop stop)
        {
            if (!stop.IsStation)
                return Planet.GetPlanetByType(stop.Planet).Name;

            return DB.Get<WorldProperty>(stop.StationId)?.CustomName ?? "Player Station";
        }

        /// <summary>The stations a shuttle can reach: those whose Starport is loaded with its flights terminals.</summary>
        public static List<ShuttleStop> GetShuttleStations()
        {
            return _dockPoints
                .Where(x => x.Value.Count > 0)
                .Select(x => GetShuttleStop(x.Key))
                .Where(x => x.IsStation)
                .ToList();
        }

        /// <summary>Where shuttle passengers to a station arrive: the entrance of one of its loaded Starports.</summary>
        public static bool TryGetArrivalLocation(string stationId, out Location location)
        {
            location = default;
            if (!_dockPoints.TryGetValue(stationId, out var docks))
                return false;

            foreach (var starportId in docks.Keys)
            {
                if (!Property.TryGetLoadedInstance(starportId, out var instance))
                    continue;

                var entrance = Property.GetEntrancePosition(PropertyLayoutType.StarportStyle1);
                location = Location(instance.Area, Vector3(entrance.X, entrance.Y, entrance.Z), entrance.W);
                return true;
            }

            return false;
        }

        /// <summary>
        /// A Station Core was used. Founding opens the station placement menu when everything but the confirmation is in
        /// order; otherwise the player is told why.
        /// </summary>
        public static void BeginFounding(uint player, uint item, uint area, Vector3 position)
        {
            var request = BuildFoundingRequest(player, area, position);
            var refusal = StationRules.GetFoundingRefusal(request);
            if (refusal != StationFoundingRefusal.None)
            {
                FloatingTextStringOnCreature(StationRules.DescribeFoundingRefusal(refusal), player, false);
                return;
            }

            SetLocalObject(player, PlaceStationCoreDialog.ItemVariable, item);
            SetLocalString(player, PlaceStationCoreDialog.AnchorVariable, request.Anchor.Id);
            ConversationMenu.Start(player, player, nameof(PlaceStationCoreDialog));
        }

        /// <summary>What the founding rules need to know about the player, the area and the nearest anchor.</summary>
        public static StationFoundingRequest BuildFoundingRequest(uint player, uint area, Vector3 position)
        {
            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            var anchor = StationRules.FindAnchorInRange(GetResRef(area), position.X, position.Y);
            return new StationFoundingRequest
            {
                InSpace = GetLocalBool(area, "SPACE"),
                Anchor = anchor,
                AreaMaxSize = Space.GetAreaMaxShipSize(area),
                AnchorTaken = anchor != null && GetStationAtAnchor(anchor.Id) != null,
                IsCitizen = !string.IsNullOrWhiteSpace(dbPlayer?.CitizenPropertyId),
                CityManagementLevel = Perk.GetPerkLevel(player, PerkType.CityManagement),
                Credits = GetGold(player),
            };
        }
    }
}
