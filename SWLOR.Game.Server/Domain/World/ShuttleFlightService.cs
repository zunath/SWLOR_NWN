using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// The flights in the air and their shuttle interiors: creating a flight and its instance, giving a flight back its live
    /// interior after a hot reload, tearing the instance down, and looking a flight up by its console or area. The logic
    /// moved here unchanged from <see cref="ShuttleService"/>.
    /// </summary>
    public class ShuttleFlightService : IShuttleFlightService
    {
        internal const string ShuttleInteriorResref = "shuttle";
        private const string LegacyShuttleInteriorResref = "starship1_int";
        internal const string ShuttleFlightIdVariable = "SHUTTLE_FLIGHT_ID";
        internal const string TerminalTag = "flights_terminal";
        internal const string TerminalPlanetVariable = "CURRENT_LOCATION";
        private const string EntranceWaypointTag = "PROPERTY_ENTRANCE";
        private const string ConsoleWaypointTag = "SHUTTLE_STATUS_CONSOLE";
        private const string ExitPlaceableTag = "building_exit";
        private const string ShipComputerTag = "ShipComputer";
        private const string PilotChairTag = "pilot_chair";
        // A droid using the parts-based "(Dynamic) Droid" appearance, which supports the sit animation.
        private const string PilotDroidResref = "maledroid";
        private const int PilotDroidSoundset = 910;

        private readonly IPlanetService _planet;
        private readonly IAreaService _area;
        private readonly IShuttleStateService _state;
        private readonly IShuttleClockService _clock;
        private readonly IShuttlePassengerService _passengers;

        public ShuttleFlightService(IPlanetService planet, IAreaService area, IShuttleStateService state, IShuttleClockService clock,
            IShuttlePassengerService passengers)
        {
            _planet = planet;
            _area = area;
            _state = state;
            _clock = clock;
            _passengers = passengers;
        }

        /// <summary>
        /// Gives a flight its live shuttle interior back: the area, the status console and the boarding point that
        /// <see cref="EnsureFlightInstance"/> found or made in it.
        /// </summary>
        public void AttachFlightInstance(ShuttleActiveFlight flight, uint area)
        {
            var entrancePosition = Vector3(1f, 1f, 0f);
            var entranceFacing = 0f;
            for (var obj = GetFirstObjectInArea(area); GetIsObjectValid(obj); obj = GetNextObjectInArea(area))
            {
                if (GetTag(obj) == EntranceWaypointTag)
                {
                    entrancePosition = GetPosition(obj);
                    entranceFacing = GetFacing(obj);
                }
                else if (GetObjectType(obj) == ObjectType.Placeable && GetLocalString(obj, ShuttleFlightIdVariable) == flight.FlightId)
                {
                    flight.Console = obj;
                }
            }

            flight.Area = area;
            flight.EntranceLocation = Location(area, entrancePosition, entranceFacing);
        }

        /// <summary>
        /// Returns the tracked flight for the given id, creating it and its instance if needed.
        /// </summary>
        public ShuttleActiveFlight GetOrCreateFlight(string flightId, PlanetType origin, PlanetType destination, DateTime departureUtc, DateTime arrivalUtc)
        {
            if (!_state.ActiveFlights.TryGetValue(flightId, out var flight))
            {
                flight = new ShuttleActiveFlight
                {
                    FlightId = flightId,
                    Origin = origin,
                    Destination = destination,
                    DepartureUtc = departureUtc,
                    ArrivalUtc = arrivalUtc,
                    LastBroadcastUtc = departureUtc
                };
                _state.ActiveFlights[flightId] = flight;
            }

            EnsureFlightInstance(flight);
            return flight;
        }

        /// <summary>
        /// Reads the dedicated template directly; instance templates are intentionally absent
        /// from the persistent-location cache. Older modules may not have its boarding tags.
        /// </summary>
        private IEnumerable<(string Tag, ObjectType Type)> GetShuttleTemplateObjects()
        {
            for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
            {
                if (GetResRef(area) != ShuttleInteriorResref)
                    continue;

                for (var obj = GetFirstObjectInArea(area); GetIsObjectValid(obj); obj = GetNextObjectInArea(area))
                    yield return (GetTag(obj), GetObjectType(obj));
                yield break;
            }
        }

        /// <summary>
        /// Uses the passenger layout only when its boarding and pilot contracts are present.
        /// A code-first deployment or module rollback continues using the original freighter.
        /// </summary>
        private string SelectFlightInterior(IEnumerable<(string Tag, ObjectType Type)> objects)
        {
            var hasEntrance = false;
            var hasPilotChair = false;
            foreach (var obj in objects)
            {
                hasEntrance |= obj.Tag == EntranceWaypointTag && obj.Type == ObjectType.Waypoint;
                hasPilotChair |= obj.Tag == PilotChairTag && obj.Type == ObjectType.Placeable;
            }

            return hasEntrance && hasPilotChair ? ShuttleInteriorResref : LegacyShuttleInteriorResref;
        }

        /// <summary>
        /// Creates the shuttle interior instance for a flight: removes the exit and ship computer,
        /// adds the status console, and seats the pilot droid.
        /// </summary>
        private void EnsureFlightInstance(ShuttleActiveFlight flight)
        {
            if (GetIsObjectValid(flight.Area))
                return;

            var destinationName = _planet.GetPlanetByType(flight.Destination).Name;
            var interiorResref = SelectFlightInterior(GetShuttleTemplateObjects());
            if (interiorResref != ShuttleInteriorResref)
                Log.Write(LogGroup.Server, "Passenger shuttle template is missing its boarding waypoint or pilot chair; using starship1_int. Repack and deploy the module to enable the shuttle interior.");

            var area = _area.CreateInstance(interiorResref, "shuttle_flight", $"Passenger Shuttle - {destinationName}");
            SetLocalString(area, ShuttleFlightIdVariable, flight.FlightId);
            flight.Area = area;

            var entrancePosition = Vector3(1f, 1f, 0f);
            var entranceFacing = 0f;
            Location consoleLocation = null;
            var pilotChair = OBJECT_INVALID;

            var objectsToDestroy = new List<uint>();
            for (var obj = GetFirstObjectInArea(area); GetIsObjectValid(obj); obj = GetNextObjectInArea(area))
            {
                var tag = GetTag(obj);
                if (tag == EntranceWaypointTag)
                {
                    entrancePosition = GetPosition(obj);
                    entranceFacing = GetFacing(obj);
                }
                else if (tag == ConsoleWaypointTag)
                {
                    consoleLocation = GetLocation(obj);
                }
                else if (tag == ExitPlaceableTag || tag == ShipComputerTag)
                {
                    // Passengers cannot leave a shuttle mid-flight, and the ship's computer
                    // (personal-ship controls) has no purpose aboard a scheduled shuttle.
                    objectsToDestroy.Add(obj);
                }
                else if (tag == PilotChairTag)
                {
                    pilotChair = obj;
                }
            }

            foreach (var obj in objectsToDestroy)
            {
                DestroyObject(obj);
            }

            flight.EntranceLocation = Location(area, entrancePosition, entranceFacing);

            // Interiors without a console point (the legacy freighter) fall back to the boarding point.
            var console = CreateObject(ObjectType.Placeable, TerminalTag, consoleLocation ?? flight.EntranceLocation);
            SetName(console, "Shuttle Status Console");
            SetLocalInt(console, ChatVariables.SuppressChatAuditVariable, 1);
            SetPlotFlag(console, true);
            SetLocalString(console, "CONVERSATION", "ShuttleStatusDialog");
            SetLocalString(console, ShuttleFlightIdVariable, flight.FlightId);
            DeleteLocalInt(console, TerminalPlanetVariable);
            flight.Console = console;

            SpawnPilotDroid(pilotChair);
        }

        /// <summary>
        /// Spawns the pilot droid and seats it in the shuttle's pilot chair.
        /// </summary>
        private void SpawnPilotDroid(uint pilotChair)
        {
            if (!GetIsObjectValid(pilotChair))
                return;

            var droid = CreateObject(ObjectType.Creature, PilotDroidResref, GetLocation(pilotChair));
            if (!GetIsObjectValid(droid))
                return;

            SetName(droid, "Shuttle Pilot Droid");
            SetSoundset(droid, PilotDroidSoundset);
            SetPlotFlag(droid, true);
            SetImmortal(droid, true);

            // Seat the droid at the controls once the freshly instanced area's scripts resume.
            AssignCommand(droid, () => ActionSit(pilotChair));
        }

        /// <summary>
        /// Delivers any stragglers and destroys the shuttle instance, retrying until it succeeds.
        /// </summary>
        public void DestroyFlightInstance(uint area, Location landingLocation)
        {
            if (!GetIsObjectValid(area))
                return;

            // Deliver any stragglers before tearing the area down.
            _passengers.DeliverPlayersInArea(area, landingLocation);

            var result = DestroyArea(area);
            if (result != 1)
            {
                _clock.Schedule(() => DestroyFlightInstance(area, landingLocation), TimeSpan.FromSeconds(30));
            }
        }

        /// <summary>
        /// Retrieves the flight registered to a shuttle status console or shuttle area, if any.
        /// </summary>
        /// <param name="obj">The console placeable or area to check.</param>
        /// <param name="destination">The flight's destination planet.</param>
        /// <param name="arrivalUtc">The flight's UTC arrival time.</param>
        /// <returns>true if a flight was found, false otherwise.</returns>
        public bool TryGetFlightInfo(uint obj, out PlanetType destination, out DateTime arrivalUtc)
        {
            destination = PlanetType.Invalid;
            arrivalUtc = default;

            var flightId = GetLocalString(obj, ShuttleFlightIdVariable);
            if (string.IsNullOrWhiteSpace(flightId) || !_state.ActiveFlights.TryGetValue(flightId, out var flight))
                return false;

            destination = flight.Destination;
            arrivalUtc = flight.ArrivalUtc;
            return true;
        }
    }
}
