using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.ShuttleService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Handles scheduled passenger shuttle flights between planets.
    /// Players purchase a ticket at a starport flights terminal, wait for the scheduled
    /// departure, ride an instanced shuttle interior, and are delivered to the destination
    /// planet's landing waypoint when the flight arrives.
    /// </summary>
    public static class Shuttle
    {
        private class ActiveFlight
        {
            public string FlightId { get; set; } = string.Empty;
            public PlanetType Origin { get; set; }
            public PlanetType Destination { get; set; }
            public DateTime DepartureUtc { get; set; }
            public DateTime ArrivalUtc { get; set; }
            public uint Area { get; set; } = OBJECT_INVALID;
            public Location EntranceLocation { get; set; }
            public DateTime LastBroadcastUtc { get; set; }
        }

        private const string ShuttleInteriorResref = "starship1_int";
        private const string ShuttleFlightIdVariable = "SHUTTLE_FLIGHT_ID";
        private const string TerminalTag = "flights_terminal";
        private const string TerminalPlanetVariable = "CURRENT_LOCATION";
        private const string EntranceWaypointTag = "PROPERTY_ENTRANCE";
        private const string ExitPlaceableTag = "building_exit";
        private const string ShipComputerTag = "ship_computer";
        private const string PilotChairTag = "pilot_chair";
        private const string PilotDroidResref = "receptiondroid";
        private const float BoardingRangeMeters = 15f;
        private const int BoardingCallSeconds = 60;

        private static readonly Dictionary<string, ActiveFlight> _activeFlights = new();
        private static readonly Dictionary<string, (PlanetType Origin, PlanetType Destination)> _ticketHolders = new();
        private static readonly Dictionary<(PlanetType Origin, PlanetType Destination), DateTime> _announcedBoardings = new();
        private static DateTime _lastTickUtc;

        /// <summary>
        /// When the module loads, recover in-transit flights from the database and
        /// begin processing the flight schedule.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void LoadShuttleFlights()
        {
            var now = DateTime.UtcNow;
            RecoverRidesAfterRestart(now);

            _lastTickUtc = now;
            Scheduler.ScheduleRepeating(ProcessFlightSchedule, TimeSpan.FromSeconds(2));
        }

        private static void RecoverRidesAfterRestart(DateTime now)
        {
            var rides = SearchRidesByStatus(ShuttleRideStatus.Ticketed);
            foreach (var ride in rides)
            {
                _ticketHolders[ride.PlayerId] = (ride.Origin, ride.Destination);
            }

            var inTransit = SearchRidesByStatus(ShuttleRideStatus.InTransit);
            foreach (var ride in inTransit)
            {
                if (ride.ArrivalUtc <= now)
                {
                    // The flight landed while the server was down. Deliver the passenger
                    // to the destination on their offline record; they log in at the landing pad.
                    DeliverOfflinePassenger(ride);
                }
                else if (!_activeFlights.ContainsKey(ride.FlightId))
                {
                    // Flight still in the air. Re-register it without an area; the shuttle
                    // interior is recreated lazily if a passenger logs back in before arrival.
                    _activeFlights[ride.FlightId] = new ActiveFlight
                    {
                        FlightId = ride.FlightId,
                        Origin = ride.Origin,
                        Destination = ride.Destination,
                        DepartureUtc = ride.DepartureUtc,
                        ArrivalUtc = ride.ArrivalUtc,
                        LastBroadcastUtc = now
                    };
                }
            }
        }

        private static List<ShuttleRide> SearchRidesByStatus(ShuttleRideStatus status)
        {
            var query = new DBQuery<ShuttleRide>()
                .AddFieldSearch(nameof(ShuttleRide.Status), (int)status);
            var count = (int)DB.SearchCount(query);
            return DB.Search(query.AddPaging(count, 0)).ToList();
        }

        private static List<ShuttleRide> SearchRidesByFlightId(string flightId)
        {
            var query = new DBQuery<ShuttleRide>()
                .AddFieldSearch(nameof(ShuttleRide.FlightId), flightId, false);
            var count = (int)DB.SearchCount(query);
            return DB.Search(query.AddPaging(count, 0)).ToList();
        }

        private static void ProcessFlightSchedule()
        {
            var now = DateTime.UtcNow;

            ProcessDepartures(now);
            ProcessBoardingCalls(now);
            ProcessArrivals(now);
            ProcessTransitBroadcasts(now);

            _lastTickUtc = now;
        }

        private static void ProcessDepartures(DateTime now)
        {
            var routes = _ticketHolders.Values.Distinct().ToList();
            foreach (var route in routes)
            {
                var departures = ShuttleSchedule.GetDeparturesBetween(route.Origin, route.Destination, _lastTickUtc, now);
                foreach (var departure in departures)
                {
                    BoardPassengers(route.Origin, route.Destination, departure, now);
                }
            }
        }

        private static void BoardPassengers(PlanetType origin, PlanetType destination, DateTime departureUtc, DateTime now)
        {
            var ticketHolders = _ticketHolders
                .Where(x => x.Value.Origin == origin && x.Value.Destination == destination)
                .Select(x => x.Key)
                .ToList();

            var flightId = ShuttleSchedule.BuildFlightId(origin, destination, departureUtc);
            var transitSeconds = GalaxyMap.GetTransitSeconds(origin, destination);
            var boardedAnyone = false;

            foreach (var playerId in ticketHolders)
            {
                var player = GetOnlinePlayerById(playerId);
                if (!GetIsObjectValid(player) ||
                    GetIsDead(player) ||
                    GetIsDMPossessed(player) ||
                    !IsAtBoardingTerminal(player, origin))
                {
                    NotifyMissedBoarding(player, origin, destination, now);
                    continue;
                }

                var ride = GetRide(playerId);
                if (ride == null || ride.Status != ShuttleRideStatus.Ticketed)
                {
                    _ticketHolders.Remove(playerId);
                    continue;
                }

                var flight = GetOrCreateFlight(flightId, origin, destination, now, now.AddSeconds(transitSeconds));

                ride.Status = ShuttleRideStatus.InTransit;
                ride.FlightId = flightId;
                ride.DepartureUtc = flight.DepartureUtc;
                ride.ArrivalUtc = flight.ArrivalUtc;
                DB.Set(ride);
                _ticketHolders.Remove(playerId);
                boardedAnyone = true;

                var entrance = flight.EntranceLocation;
                AssignCommand(player, () =>
                {
                    ClearAllActions();
                    ActionJumpToLocation(entrance);
                });

                var destinationName = Planet.GetPlanetByType(destination).Name;
                var transit = Time.GetTimeShortIntervals(TimeSpan.FromSeconds(transitSeconds), false);
                SendMessageToPC(player, $"You board the shuttle to {destinationName}. Estimated flight time: {transit}.");
            }

            if (boardedAnyone)
            {
                Log.Write(LogGroup.Server, $"Shuttle flight {flightId} departed {origin} for {destination}.");
            }
        }

        private static void NotifyMissedBoarding(uint player, PlanetType origin, PlanetType destination, DateTime now)
        {
            if (!GetIsObjectValid(player))
                return;

            var next = ShuttleSchedule.GetNextDepartureUtc(origin, destination, now);
            var wait = Time.GetTimeShortIntervals(next - now, false);
            var destinationName = Planet.GetPlanetByType(destination).Name;
            SendMessageToPC(player, ColorToken.Yellow($"You missed your shuttle to {destinationName}! Your ticket remains valid. The next shuttle departs in {wait} - be within {(int)BoardingRangeMeters} meters of the flights terminal."));
        }

        private static void ProcessBoardingCalls(DateTime now)
        {
            var routes = _ticketHolders.Values.Distinct().ToList();
            foreach (var route in routes)
            {
                var next = ShuttleSchedule.GetNextDepartureUtc(route.Origin, route.Destination, now);
                if ((next - now).TotalSeconds > BoardingCallSeconds)
                    continue;

                if (_announcedBoardings.TryGetValue(route, out var announced) && announced == next)
                    continue;

                _announcedBoardings[route] = next;

                var destinationName = Planet.GetPlanetByType(route.Destination).Name;
                var wait = Time.GetTimeShortIntervals(next - now, false);
                foreach (var (playerId, ticketRoute) in _ticketHolders)
                {
                    if (ticketRoute != route)
                        continue;

                    var player = GetOnlinePlayerById(playerId);
                    if (!GetIsObjectValid(player))
                        continue;

                    SendMessageToPC(player, ColorToken.Cyan($"Now boarding: shuttle to {destinationName} departs in {wait}. Be within {(int)BoardingRangeMeters} meters of the flights terminal!"));
                }
            }
        }

        private static void ProcessArrivals(DateTime now)
        {
            var arrived = _activeFlights.Values
                .Where(x => x.ArrivalUtc <= now)
                .ToList();

            foreach (var flight in arrived)
            {
                ProcessArrival(flight);
            }
        }

        private static void ProcessArrival(ActiveFlight flight)
        {
            var landingLocation = GetLandingLocation(flight.Destination);
            var destinationName = Planet.GetPlanetByType(flight.Destination).Name;

            // Deliver everyone still aboard the shuttle.
            if (GetIsObjectValid(flight.Area))
            {
                foreach (var player in Area.GetPlayersInArea(flight.Area))
                {
                    DeliverPassenger(player, landingLocation);
                    SendMessageToPC(player, $"The shuttle touches down at {destinationName}.");
                }
            }

            // Resolve every ride record on this flight.
            foreach (var ride in SearchRidesByFlightId(flight.FlightId))
            {
                var player = GetOnlinePlayerById(ride.PlayerId);
                if (!GetIsObjectValid(player))
                {
                    DeliverOfflinePassenger(ride);
                }
                else
                {
                    if (!GetIsObjectValid(flight.Area))
                    {
                        // The instance was never recreated after a restart but the passenger is
                        // online (e.g. they logged in during the arrival tick). Deliver them directly.
                        DeliverPassenger(player, landingLocation);
                        SendMessageToPC(player, $"The shuttle touches down at {destinationName}.");
                    }
                    else if (GetArea(player) != flight.Area)
                    {
                        // The player left the flight some other way (death respawn, DM port).
                        // They forfeit the trip - do not teleport them.
                        Log.Write(LogGroup.Server, $"Shuttle passenger {ride.PlayerId} abandoned flight {flight.FlightId} before arrival.");
                    }

                    DB.Delete<ShuttleRide>(ride.Id);
                }
            }

            _activeFlights.Remove(flight.FlightId);

            if (GetIsObjectValid(flight.Area))
            {
                var area = flight.Area;
                Scheduler.Schedule(() => DestroyFlightInstance(area, landingLocation), TimeSpan.FromSeconds(30));
            }

            Log.Write(LogGroup.Server, $"Shuttle flight {flight.FlightId} arrived at {flight.Destination}.");
        }

        private static void DestroyFlightInstance(uint area, Location landingLocation)
        {
            if (!GetIsObjectValid(area))
                return;

            // Deliver any stragglers before tearing the area down.
            foreach (var player in Area.GetPlayersInArea(area))
            {
                DeliverPassenger(player, landingLocation);
            }

            var result = DestroyArea(area);
            if (result != 1)
            {
                Scheduler.Schedule(() => DestroyFlightInstance(area, landingLocation), TimeSpan.FromSeconds(30));
            }
        }

        private static void DeliverPassenger(uint player, Location landingLocation)
        {
            if (GetIsDM(player) || GetIsDMPossessed(player))
                return;

            if (GetIsDead(player))
            {
                AssignCommand(player, () => JumpToLocation(landingLocation));
            }
            else
            {
                AssignCommand(player, () =>
                {
                    ClearAllActions();
                    ActionJumpToLocation(landingLocation);
                });
            }
        }

        private static void DeliverOfflinePassenger(ShuttleRide ride)
        {
            var dbPlayer = DB.Get<Player>(ride.PlayerId);
            if (dbPlayer != null)
            {
                var waypoint = GetWaypointByTag(Planet.GetPlanetByType(ride.Destination).LandingWaypointTag);
                if (GetIsObjectValid(waypoint))
                {
                    var position = GetPosition(waypoint);
                    dbPlayer.LocationAreaResref = GetResRef(GetArea(waypoint));
                    dbPlayer.LocationX = position.X;
                    dbPlayer.LocationY = position.Y;
                    dbPlayer.LocationZ = position.Z;
                    dbPlayer.LocationOrientation = GetFacing(waypoint);
                    DB.Set(dbPlayer);
                }
            }

            DB.Delete<ShuttleRide>(ride.Id);
            Log.Write(LogGroup.Server, $"Shuttle delivered offline passenger {ride.PlayerId} to {ride.Destination}.");
        }

        private static void ProcessTransitBroadcasts(DateTime now)
        {
            foreach (var flight in _activeFlights.Values)
            {
                if (!GetIsObjectValid(flight.Area))
                    continue;

                if ((now - flight.LastBroadcastUtc).TotalSeconds < 60)
                    continue;

                flight.LastBroadcastUtc = now;

                var destinationName = Planet.GetPlanetByType(flight.Destination).Name;
                var remaining = Time.GetTimeShortIntervals(flight.ArrivalUtc - now, false);
                foreach (var player in Area.GetPlayersInArea(flight.Area))
                {
                    SendMessageToPC(player, $"Arriving at {destinationName} in {remaining}.");
                }
            }
        }

        private static ActiveFlight GetOrCreateFlight(string flightId, PlanetType origin, PlanetType destination, DateTime departureUtc, DateTime arrivalUtc)
        {
            if (!_activeFlights.TryGetValue(flightId, out var flight))
            {
                flight = new ActiveFlight
                {
                    FlightId = flightId,
                    Origin = origin,
                    Destination = destination,
                    DepartureUtc = departureUtc,
                    ArrivalUtc = arrivalUtc,
                    LastBroadcastUtc = departureUtc
                };
                _activeFlights[flightId] = flight;
            }

            EnsureFlightInstance(flight);
            return flight;
        }

        private static void EnsureFlightInstance(ActiveFlight flight)
        {
            if (GetIsObjectValid(flight.Area))
                return;

            var destinationName = Planet.GetPlanetByType(flight.Destination).Name;
            var area = CreateArea(ShuttleInteriorResref, "shuttle_flight", $"Passenger Shuttle - {destinationName}");
            SetLocalString(area, ShuttleFlightIdVariable, flight.FlightId);
            flight.Area = area;

            var entrancePosition = Vector3(1f, 1f, 0f);
            var entranceFacing = 0f;
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

            var console = CreateObject(ObjectType.Placeable, TerminalTag, flight.EntranceLocation);
            SetName(console, "Shuttle Status Console");
            SetPlotFlag(console, true);
            SetLocalString(console, "CONVERSATION", "ShuttleStatusDialog");
            SetLocalString(console, ShuttleFlightIdVariable, flight.FlightId);
            DeleteLocalInt(console, TerminalPlanetVariable);

            SpawnPilotDroid(pilotChair);
        }

        private static void SpawnPilotDroid(uint pilotChair)
        {
            if (!GetIsObjectValid(pilotChair))
                return;

            var droid = CreateObject(ObjectType.Creature, PilotDroidResref, GetLocation(pilotChair));
            if (!GetIsObjectValid(droid))
                return;

            SetName(droid, "Shuttle Pilot Droid");
            SetPlotFlag(droid, true);
            SetImmortal(droid, true);

            // Seat the droid at the controls once the freshly instanced area's scripts resume.
            AssignCommand(droid, () => ActionSit(pilotChair));
        }

        private static Location GetLandingLocation(PlanetType destination)
        {
            var waypoint = GetWaypointByTag(Planet.GetPlanetByType(destination).LandingWaypointTag);
            return GetLocation(waypoint);
        }

        private static uint GetOnlinePlayerById(string playerId)
        {
            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (GetObjectUUID(player) == playerId)
                    return player;
            }

            return OBJECT_INVALID;
        }

        private static bool IsAtBoardingTerminal(uint player, PlanetType origin)
        {
            var nth = 1;
            var terminal = GetNearestObjectByTag(TerminalTag, player, nth);
            while (GetIsObjectValid(terminal))
            {
                // Results are ordered by distance - once we pass the boarding range, stop looking.
                if (GetDistanceBetween(player, terminal) > BoardingRangeMeters)
                    return false;

                if ((PlanetType)GetLocalInt(terminal, TerminalPlanetVariable) == origin)
                    return true;

                nth++;
                terminal = GetNearestObjectByTag(TerminalTag, player, nth);
            }

            return false;
        }

        /// <summary>
        /// Retrieves a player's active shuttle ride, or null if they have none.
        /// </summary>
        /// <param name="playerId">The player Id to look up.</param>
        /// <returns>The player's shuttle ride, or null.</returns>
        public static ShuttleRide GetRide(string playerId)
        {
            var query = new DBQuery<ShuttleRide>()
                .AddFieldSearch(nameof(ShuttleRide.PlayerId), playerId, false);
            return DB.Search(query).FirstOrDefault();
        }

        /// <summary>
        /// Attempts to purchase a shuttle ticket for the given route.
        /// Charges the fare plus city transportation tax and deposits the tax into the city treasury.
        /// </summary>
        /// <param name="player">The player purchasing the ticket.</param>
        /// <param name="origin">The origin planet.</param>
        /// <param name="destination">The destination planet.</param>
        /// <param name="cityPropertyId">The city property Id receiving the transportation tax, if any.</param>
        /// <param name="taxRate">The transportation tax rate (0.0 - 0.25).</param>
        /// <returns>true if the ticket was purchased, false otherwise.</returns>
        public static bool TryPurchaseTicket(uint player, PlanetType origin, PlanetType destination, string cityPropertyId, float taxRate)
        {
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return false;

            if (origin == PlanetType.Invalid ||
                destination == PlanetType.Invalid ||
                origin == destination)
                return false;

            var playerId = GetObjectUUID(player);
            var existingRide = GetRide(playerId);
            if (existingRide != null)
            {
                SendMessageToPC(player, ColorToken.Red("You already have a shuttle ticket."));
                return false;
            }

            var fare = GalaxyMap.GetFare(origin, destination);
            var tax = (int)(taxRate * fare);
            var price = fare + tax;

            if (GetGold(player) < price)
            {
                SendMessageToPC(player, ColorToken.Red("You do not have enough credits to purchase this flight!"));
                return false;
            }

            TakeGoldFromCreature(price, player, true);

            var ride = new ShuttleRide(playerId)
            {
                Status = ShuttleRideStatus.Ticketed,
                Origin = origin,
                Destination = destination,
                FarePaid = fare,
                TaxPaid = tax
            };
            DB.Set(ride);
            _ticketHolders[playerId] = (origin, destination);

            if (tax > 0 && !string.IsNullOrWhiteSpace(cityPropertyId))
            {
                var dbCity = DB.Get<WorldProperty>(cityPropertyId);
                if (dbCity != null)
                {
                    dbCity.Treasury += tax;
                    DB.Set(dbCity);
                    Log.Write(LogGroup.Property, $"{GetName(player)} paid {tax} credits in tax for their trip to {destination}.");
                }
            }

            return true;
        }

        /// <summary>
        /// Refunds a player's unboarded shuttle ticket. Only the fare is returned;
        /// taxes stay with the city treasury.
        /// </summary>
        /// <param name="player">The player refunding their ticket.</param>
        /// <returns>true if the ticket was refunded, false otherwise.</returns>
        public static bool RefundTicket(uint player)
        {
            var playerId = GetObjectUUID(player);
            var ride = GetRide(playerId);
            if (ride == null || ride.Status != ShuttleRideStatus.Ticketed)
                return false;

            GiveGoldToCreature(player, ride.FarePaid);
            DB.Delete<ShuttleRide>(ride.Id);
            _ticketHolders.Remove(playerId);

            SendMessageToPC(player, $"Your shuttle ticket has been refunded for {ride.FarePaid} credits. Taxes are non-refundable.");
            return true;
        }

        /// <summary>
        /// Determines where a logging-in player with an in-transit shuttle ride should be placed.
        /// Returns the destination landing pad if the flight has already arrived (and resolves the ride),
        /// or the shuttle interior if the flight is still in the air.
        /// </summary>
        /// <param name="player">The player logging in.</param>
        /// <param name="location">The location the player should be moved to.</param>
        /// <returns>true if the player must be redirected, false otherwise.</returns>
        public static bool TryGetLoginRedirect(uint player, out Location location)
        {
            location = default;

            if (!GetIsPC(player) || GetIsDM(player))
                return false;

            var playerId = GetObjectUUID(player);
            var ride = GetRide(playerId);
            if (ride == null || ride.Status != ShuttleRideStatus.InTransit)
                return false;

            if (ride.ArrivalUtc <= DateTime.UtcNow)
            {
                location = GetLandingLocation(ride.Destination);
                DB.Delete<ShuttleRide>(ride.Id);
                Log.Write(LogGroup.Server, $"Shuttle delivered passenger {playerId} to {ride.Destination} on login.");
                return true;
            }

            var flight = GetOrCreateFlight(ride.FlightId, ride.Origin, ride.Destination, ride.DepartureUtc, ride.ArrivalUtc);
            location = flight.EntranceLocation;
            return true;
        }

        /// <summary>
        /// When a player enters the server, verify in-transit passengers actually ended up
        /// aboard their shuttle. This is a backstop for cases where the game placed them
        /// into a stale instance or an unexpected location.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void EnterServer()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var playerId = GetObjectUUID(player);

            Scheduler.Schedule(() =>
            {
                var current = GetOnlinePlayerById(playerId);
                if (!GetIsObjectValid(current))
                    return;

                var ride = GetRide(playerId);
                if (ride == null || ride.Status != ShuttleRideStatus.InTransit)
                    return;

                if (_activeFlights.TryGetValue(ride.FlightId, out var flight) &&
                    GetIsObjectValid(flight.Area) &&
                    GetArea(current) == flight.Area)
                    return;

                if (TryGetLoginRedirect(current, out var location))
                {
                    var redirect = location;
                    AssignCommand(current, () =>
                    {
                        ClearAllActions();
                        ActionJumpToLocation(redirect);
                    });
                }
            }, TimeSpan.FromSeconds(2));
        }

        /// <summary>
        /// Retrieves the next scheduled departure for a route.
        /// </summary>
        /// <param name="origin">The origin planet.</param>
        /// <param name="destination">The destination planet.</param>
        /// <returns>The UTC time of the next departure.</returns>
        public static DateTime GetNextDepartureUtc(PlanetType origin, PlanetType destination)
        {
            return ShuttleSchedule.GetNextDepartureUtc(origin, destination, DateTime.UtcNow);
        }

        /// <summary>
        /// Retrieves the flight registered to a shuttle status console or shuttle area, if any.
        /// </summary>
        /// <param name="obj">The console placeable or area to check.</param>
        /// <param name="destination">The flight's destination planet.</param>
        /// <param name="arrivalUtc">The flight's UTC arrival time.</param>
        /// <returns>true if a flight was found, false otherwise.</returns>
        public static bool TryGetFlightInfo(uint obj, out PlanetType destination, out DateTime arrivalUtc)
        {
            destination = PlanetType.Invalid;
            arrivalUtc = default;

            var flightId = GetLocalString(obj, ShuttleFlightIdVariable);
            if (string.IsNullOrWhiteSpace(flightId) || !_activeFlights.TryGetValue(flightId, out var flight))
                return false;

            destination = flight.Destination;
            arrivalUtc = flight.ArrivalUtc;
            return true;
        }
    }
}
