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
    /// Handles scheduled passenger shuttle flights between planets, and between player stations and their home ports.
    /// Players purchase a ticket at a starport flights terminal, wait for the scheduled
    /// departure, ride an instanced shuttle interior, and are delivered to the destination
    /// planet's landing waypoint (or the station's Starport) when the flight arrives.
    /// </summary>
    public static class Shuttle
    {
        private class ActiveFlight
        {
            public string FlightId { get; set; } = string.Empty;
            public ShuttleRoute Route { get; set; }
            public DateTime DepartureUtc { get; set; }
            public DateTime ArrivalUtc { get; set; }
            public uint Area { get; set; } = OBJECT_INVALID;
            public uint Console { get; set; } = OBJECT_INVALID;
            public Location EntranceLocation { get; set; }
            public DateTime LastBroadcastUtc { get; set; }
        }

        internal const string ShuttleInteriorResref = "shuttle";
        private const string LegacyShuttleInteriorResref = "starship1_int";
        private const string ShuttleFlightIdVariable = "SHUTTLE_FLIGHT_ID";
        private const string TerminalTag = "flights_terminal";
        private const string TerminalPlanetVariable = "CURRENT_LOCATION";
        private const string EntranceWaypointTag = "PROPERTY_ENTRANCE";
        private const string ExitPlaceableTag = "building_exit";
        private const string ShipComputerTag = "ShipComputer";
        private const string PilotChairTag = "pilot_chair";
        // A droid using the parts-based "(Dynamic) Droid" appearance, which supports the sit animation.
        private const string PilotDroidResref = "maledroid";
        private const int PilotDroidSoundset = 910;
        private const float BoardingRangeMeters = 15f;
        private const int BoardingCallSeconds = 60;

        private static readonly Dictionary<string, ActiveFlight> _activeFlights = new();
        private static readonly Dictionary<string, ShuttleRoute> _ticketHolders = new();
        private static readonly Dictionary<string, DateTime> _boardingNotBeforeUtc = new();
        private static readonly Dictionary<ShuttleRoute, DateTime> _announcedBoardings = new();
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

        /// <summary>
        /// Restores ticketed and in-transit rides from the database after a server restart.
        /// </summary>
        private static void RecoverRidesAfterRestart(DateTime now)
        {
            var rides = SearchRidesByStatus(ShuttleRideStatus.Ticketed);
            foreach (var ride in rides)
            {
                _ticketHolders[ride.PlayerId] = GetRideRoute(ride);
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
                        Route = GetRideRoute(ride),
                        DepartureUtc = ride.DepartureUtc,
                        ArrivalUtc = ride.ArrivalUtc,
                        LastBroadcastUtc = now
                    };
                }
            }
        }

        /// <summary>
        /// Returns all shuttle rides currently in the given status.
        /// </summary>
        private static List<ShuttleRide> SearchRidesByStatus(ShuttleRideStatus status)
        {
            var query = new DBQuery<ShuttleRide>()
                .AddFieldSearch(nameof(ShuttleRide.Status), (int)status);
            var count = (int)DB.SearchCount(query);
            return DB.Search(query.AddPaging(count, 0)).ToList();
        }

        /// <summary>
        /// Returns all shuttle rides booked on the given flight.
        /// </summary>
        private static List<ShuttleRide> SearchRidesByFlightId(string flightId)
        {
            var query = new DBQuery<ShuttleRide>()
                .AddFieldSearch(nameof(ShuttleRide.FlightId), flightId, false);
            var count = (int)DB.SearchCount(query);
            return DB.Search(query.AddPaging(count, 0)).ToList();
        }

        /// <summary>
        /// Runs one scheduler tick: departures, boarding calls, arrivals, and transit broadcasts.
        /// </summary>
        private static void ProcessFlightSchedule()
        {
            var now = DateTime.UtcNow;

            ProcessDepartures(now);
            ProcessBoardingCalls(now);
            ProcessArrivals(now);
            ProcessTransitBroadcasts(now);

            _lastTickUtc = now;
        }

        /// <summary>
        /// Boards passengers for every scheduled departure that occurred since the last tick.
        /// </summary>
        private static void ProcessDepartures(DateTime now)
        {
            var routes = _ticketHolders.Values.Distinct().ToList();
            foreach (var route in routes)
            {
                var departures = ShuttleSchedule.GetDeparturesBetween(route, _lastTickUtc, now);
                foreach (var departure in departures)
                {
                    BoardPassengers(route, departure, now);
                }
            }
        }

        /// <summary>
        /// Boards eligible ticket holders onto a departing flight and messages those who missed it.
        /// </summary>
        private static void BoardPassengers(ShuttleRoute route, DateTime departureUtc, DateTime now)
        {
            var ticketHolders = _ticketHolders
                .Where(x => x.Value == route)
                .Select(x => x.Key)
                .ToList();

            var flightId = ShuttleSchedule.BuildFlightId(route, departureUtc);
            var transitSeconds = ShuttleRouteRules.GetTransitSeconds(route);
            var boardedAnyone = false;

            foreach (var playerId in ticketHolders)
            {
                // A saved ticket is not permission to board an unannounced flight immediately
                // after login. Only this session's purchase or ticket reminder enables boarding.
                if (!_boardingNotBeforeUtc.TryGetValue(playerId, out var notBefore) || departureUtc < notBefore)
                    continue;

                var player = GetOnlinePlayerById(playerId);
                if (!GetIsObjectValid(player) ||
                    GetIsDead(player) ||
                    GetIsDMPossessed(player) ||
                    !IsAtBoardingTerminal(player, route.Origin))
                {
                    NotifyMissedBoarding(player, route, now);
                    continue;
                }

                var ride = GetRide(playerId);
                if (ride == null || ride.Status != ShuttleRideStatus.Ticketed)
                {
                    _ticketHolders.Remove(playerId);
                    _boardingNotBeforeUtc.Remove(playerId);
                    continue;
                }

                var flight = GetOrCreateFlight(flightId, route, now, now.AddSeconds(transitSeconds));

                ride.Status = ShuttleRideStatus.InTransit;
                ride.FlightId = flightId;
                ride.DepartureUtc = flight.DepartureUtc;
                ride.ArrivalUtc = flight.ArrivalUtc;
                DB.Set(ride);
                _ticketHolders.Remove(playerId);
                _boardingNotBeforeUtc.Remove(playerId);
                boardedAnyone = true;

                var entrance = flight.EntranceLocation;
                AssignCommand(player, () =>
                {
                    ClearAllActions();
                    ActionJumpToLocation(entrance);
                });

                var destinationName = Station.GetStopName(route.Destination);
                var transit = Time.GetTimeLongIntervals(TimeSpan.FromSeconds(transitSeconds), false);
                SendMessageToPC(player, $"You board the shuttle to {destinationName}. Estimated flight time: {transit}.");
            }

            if (boardedAnyone)
            {
                Log.Write(LogGroup.Server, $"Shuttle flight {flightId} departed {Station.GetStopName(route.Origin)} for {Station.GetStopName(route.Destination)}.");
            }
        }

        /// <summary>
        /// Tells a ticket holder they missed the shuttle and when the next one departs.
        /// </summary>
        private static void NotifyMissedBoarding(uint player, ShuttleRoute route, DateTime now)
        {
            if (!GetIsObjectValid(player))
                return;

            var next = ShuttleSchedule.GetNextDepartureUtc(route, now);
            var wait = Time.GetTimeLongIntervals(next - now, false);
            var destinationName = Station.GetStopName(route.Destination);
            SendMessageToPC(player, ColorToken.Yellow($"You missed your shuttle to {destinationName}! Your ticket remains valid. The next shuttle departs in {wait} - be within {(int)BoardingRangeMeters} meters of the flights terminal. You can refund your ticket at any flights terminal before boarding."));
        }

        /// <summary>
        /// Announces the boarding call to ticket holders shortly before each departure.
        /// </summary>
        private static void ProcessBoardingCalls(DateTime now)
        {
            var routes = _ticketHolders.Values.Distinct().ToList();
            foreach (var route in routes)
            {
                var next = ShuttleSchedule.GetNextDepartureUtc(route, now);
                if ((next - now).TotalSeconds > BoardingCallSeconds)
                    continue;

                if (_announcedBoardings.TryGetValue(route, out var announced) && announced == next)
                    continue;

                _announcedBoardings[route] = next;

                var destinationName = Station.GetStopName(route.Destination);
                var wait = Time.GetTimeLongIntervals(next - now, false);
                foreach (var (playerId, ticketRoute) in _ticketHolders)
                {
                    if (ticketRoute != route)
                        continue;

                    if (!_boardingNotBeforeUtc.TryGetValue(playerId, out var notBefore) || next < notBefore)
                        continue;

                    var player = GetOnlinePlayerById(playerId);
                    if (!GetIsObjectValid(player))
                        continue;

                    SendMessageToPC(player, ColorToken.Cyan($"Now boarding: shuttle to {destinationName} departs in {wait}. Be within {(int)BoardingRangeMeters} meters of the flights terminal!"));
                }
            }
        }

        /// <summary>
        /// Processes every flight whose arrival time has passed.
        /// </summary>
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

        /// <summary>
        /// Delivers a flight's passengers to the destination and tears down the shuttle instance.
        /// </summary>
        private static void ProcessArrival(ActiveFlight flight)
        {
            var landingLocation = GetLandingLocation(flight.Route.Destination);
            var destinationName = Station.GetStopName(flight.Route.Destination);

            // Resolve every ride record on this flight. Delivery is decided from the passenger's
            // actual current area (engine truth), not the area's cached player list, which is not
            // reliably populated for runtime instances.
            foreach (var ride in SearchRidesByFlightId(flight.FlightId))
            {
                var player = GetOnlinePlayerById(ride.PlayerId);
                if (!GetIsObjectValid(player))
                {
                    DeliverOfflinePassenger(ride);
                    continue;
                }

                if (!GetIsObjectValid(flight.Area) || GetArea(player) == flight.Area)
                {
                    // Still aboard the shuttle (or the instance is gone after a restart) - deliver them.
                    DeliverPassenger(player, landingLocation);
                    SendMessageToPC(player, $"The shuttle touches down at {destinationName}.");
                }
                else
                {
                    // The player left the flight some other way (death respawn, DM port).
                    // They forfeit the trip - do not teleport them.
                    Log.Write(LogGroup.Server, $"Shuttle passenger {ride.PlayerId} abandoned flight {flight.FlightId} before arrival.");
                }

                DB.Delete<ShuttleRide>(ride.Id);
            }

            // Safety net: deliver anyone physically aboard without a ride record, so no player is
            // trapped when the instance is torn down.
            DeliverPlayersInArea(flight.Area, landingLocation);

            _activeFlights.Remove(flight.FlightId);

            if (GetIsObjectValid(flight.Area))
            {
                var area = flight.Area;
                Scheduler.Schedule(() => DestroyFlightInstance(area, landingLocation), TimeSpan.FromSeconds(30));
            }

            Log.Write(LogGroup.Server, $"Shuttle flight {flight.FlightId} arrived at {destinationName}.");
        }

        /// <summary>
        /// Delivers any stragglers and destroys the shuttle instance, retrying until it succeeds.
        /// </summary>
        private static void DestroyFlightInstance(uint area, Location landingLocation)
        {
            if (!GetIsObjectValid(area))
                return;

            // Deliver any stragglers before tearing the area down.
            DeliverPlayersInArea(area, landingLocation);

            var result = DestroyArea(area);
            if (result != 1)
            {
                Scheduler.Schedule(() => DestroyFlightInstance(area, landingLocation), TimeSpan.FromSeconds(30));
            }
        }

        /// <summary>
        /// Delivers every player physically present in the given area to the landing location.
        /// Iterates the area's objects directly rather than relying on the cached player list,
        /// which is not reliably populated for runtime instances.
        /// </summary>
        private static void DeliverPlayersInArea(uint area, Location landingLocation)
        {
            if (!GetIsObjectValid(area))
                return;

            for (var obj = GetFirstObjectInArea(area); GetIsObjectValid(obj); obj = GetNextObjectInArea(area))
            {
                if (GetIsPC(obj))
                    DeliverPassenger(obj, landingLocation);
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

        /// <summary>
        /// Records an offline passenger's landing location. Retains the elapsed ride until
        /// login so the passenger receives their destination notice before it is cleared.
        /// A station's deck is an instance, which a saved location cannot name, so a passenger
        /// to a station is recorded at its home port's landing.
        /// </summary>
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

            Log.Write(LogGroup.Server, $"Shuttle delivered offline passenger {ride.PlayerId} to {ride.Destination}.");
        }

        /// <summary>
        /// Has each in-flight shuttle console announce the remaining time to its passengers.
        /// </summary>
        private static void ProcessTransitBroadcasts(DateTime now)
        {
            foreach (var flight in _activeFlights.Values)
            {
                if (!GetIsObjectValid(flight.Console))
                    continue;

                if ((now - flight.LastBroadcastUtc).TotalSeconds < 60)
                    continue;

                flight.LastBroadcastUtc = now;

                var destinationName = Station.GetStopName(flight.Route.Destination);
                var remaining = Time.GetTimeLongIntervals(flight.ArrivalUtc - now, false);

                // The status console announces the ETA to nearby passengers at talk volume.
                var console = flight.Console;
                AssignCommand(console, () => SpeakString($"Now arriving at {destinationName} in {remaining}."));
            }
        }

        /// <summary>
        /// Returns the tracked flight for the given id, creating it and its instance if needed.
        /// </summary>
        private static ActiveFlight GetOrCreateFlight(string flightId, ShuttleRoute route, DateTime departureUtc, DateTime arrivalUtc)
        {
            if (!_activeFlights.TryGetValue(flightId, out var flight))
            {
                flight = new ActiveFlight
                {
                    FlightId = flightId,
                    Route = route,
                    DepartureUtc = departureUtc,
                    ArrivalUtc = arrivalUtc,
                    LastBroadcastUtc = departureUtc
                };
                _activeFlights[flightId] = flight;
            }

            EnsureFlightInstance(flight);
            return flight;
        }

        /// <summary>
        /// Reads the dedicated template directly; instance templates are intentionally absent
        /// from the persistent-location cache. Older modules may not have its boarding tags.
        /// </summary>
        private static IEnumerable<(string Tag, ObjectType Type)> GetShuttleTemplateObjects()
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
        private static string SelectFlightInterior(IEnumerable<(string Tag, ObjectType Type)> objects)
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
        private static void EnsureFlightInstance(ActiveFlight flight)
        {
            if (GetIsObjectValid(flight.Area))
                return;

            var destinationName = Station.GetStopName(flight.Route.Destination);
            var interiorResref = SelectFlightInterior(GetShuttleTemplateObjects());
            if (interiorResref != ShuttleInteriorResref)
                Log.Write(LogGroup.Server, "Passenger shuttle template is missing its boarding waypoint or pilot chair; using starship1_int. Repack and deploy the module to enable the shuttle interior.");

            var area = Area.CreateInstance(interiorResref, "shuttle_flight", $"Passenger Shuttle - {destinationName}");
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
            SetLocalInt(console, Communication.SuppressChatAuditVariable, 1);
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
        private static void SpawnPilotDroid(uint pilotChair)
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
        /// Returns where passengers to a stop land: a planet's landing waypoint, or a station's Starport.
        /// A station whose Starport is gone or not loaded lands its passengers at its home port.
        /// </summary>
        private static Location GetLandingLocation(ShuttleStop destination)
        {
            if (destination.IsStation && Station.TryGetArrivalLocation(destination.StationId, out var stationLocation))
                return stationLocation;

            var waypoint = GetWaypointByTag(Planet.GetPlanetByType(destination.Planet).LandingWaypointTag);
            return GetLocation(waypoint);
        }

        /// <summary>The route a ride record flies. A station that no longer exists keeps its home port and id.</summary>
        public static ShuttleRoute GetRideRoute(ShuttleRide ride)
        {
            return new ShuttleRoute(
                GetStop(ride.Origin, ride.OriginStationId),
                GetStop(ride.Destination, ride.DestinationStationId));
        }

        private static ShuttleStop GetStop(PlanetType planet, string stationId)
        {
            if (string.IsNullOrWhiteSpace(stationId))
                return ShuttleStop.ForPlanet(planet);

            var stop = Station.GetShuttleStop(stationId);
            return stop.IsStation ? stop : ShuttleStop.ForStation(stationId, planet, false);
        }

        /// <summary>
        /// Returns the online player with the given id, or OBJECT_INVALID if none is connected.
        /// </summary>
        private static uint GetOnlinePlayerById(string playerId)
        {
            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (GetObjectUUID(player) == playerId)
                    return player;
            }

            return OBJECT_INVALID;
        }

        /// <summary>
        /// Determines whether the player is within boarding range of a flights terminal for the given origin:
        /// a planet's terminals, or the station's own.
        /// </summary>
        private static bool IsAtBoardingTerminal(uint player, ShuttleStop origin)
        {
            var nth = 1;
            var terminal = GetNearestObjectByTag(TerminalTag, player, nth);
            while (GetIsObjectValid(terminal))
            {
                // Results are ordered by distance - once we pass the boarding range, stop looking.
                if (GetDistanceBetween(player, terminal) > BoardingRangeMeters)
                    return false;

                if ((PlanetType)GetLocalInt(terminal, TerminalPlanetVariable) == origin.Planet &&
                    GetLocalString(terminal, Station.StationCityVariable) == (origin.StationId ?? string.Empty))
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
            return TryPurchaseTicket(player, new ShuttleRoute(ShuttleStop.ForPlanet(origin), ShuttleStop.ForPlanet(destination)), cityPropertyId, taxRate);
        }

        /// <summary>
        /// Attempts to purchase a shuttle ticket for a route, which may start or end at a player station.
        /// Charges the fare plus city transportation tax and deposits the tax into the city treasury.
        /// </summary>
        public static bool TryPurchaseTicket(uint player, ShuttleRoute route, string cityPropertyId, float taxRate)
        {
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return false;

            if (!ShuttleRouteRules.IsValid(route))
                return false;

            var playerId = GetObjectUUID(player);
            var existingRide = GetRide(playerId);
            if (existingRide != null)
            {
                SendMessageToPC(player, ColorToken.Red("You already have a shuttle ticket."));
                return false;
            }

            var fare = ShuttleRouteRules.GetFare(route);
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
                Origin = route.Origin.Planet,
                Destination = route.Destination.Planet,
                OriginStationId = route.Origin.StationId ?? string.Empty,
                DestinationStationId = route.Destination.StationId ?? string.Empty,
                FarePaid = fare,
                TaxPaid = tax
            };
            DB.Set(ride);
            _ticketHolders[playerId] = route;
            _boardingNotBeforeUtc[playerId] = DateTime.UtcNow;

            if (tax > 0 && !string.IsNullOrWhiteSpace(cityPropertyId))
            {
                var dbCity = DB.Get<WorldProperty>(cityPropertyId);
                if (dbCity != null)
                {
                    dbCity.Treasury += tax;
                    DB.Set(dbCity);
                    Log.Write(LogGroup.Property, $"{GetName(player)} paid {tax} credits in tax for their trip to {Station.GetStopName(route.Destination)}.");
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
            _boardingNotBeforeUtc.Remove(playerId);

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

            return TryGetRideLoginRedirect(player, ride, out location);
        }

        private static bool TryGetRideLoginRedirect(uint player, ShuttleRide ride, out Location location)
        {
            location = default;
            var arrived = ride.ArrivalUtc <= DateTime.UtcNow;
            var dbPlayer = DB.Get<Player>(ride.PlayerId);
            if (dbPlayer == null || !dbPlayer.RebuildComplete)
            {
                // PersistentLocation owns mandatory rebuild routing. A completed flight
                // can report arrival and clear its record without moving the player out.
                if (arrived)
                    CompleteRideOnLogin(player, ride);
                return false;
            }

            if (arrived)
            {
                location = CompleteRideOnLogin(player, ride);
                return true;
            }

            var flight = GetOrCreateFlight(ride.FlightId, GetRideRoute(ride), ride.DepartureUtc, ride.ArrivalUtc);
            location = flight.EntranceLocation;
            return true;
        }

        private static Location CompleteRideOnLogin(uint player, ShuttleRide ride)
        {
            var route = GetRideRoute(ride);
            var location = GetLandingLocation(route.Destination);
            SendMessageToPC(player, $"Your shuttle has arrived at {Station.GetStopName(route.Destination)}.");
            DB.Delete<ShuttleRide>(ride.Id);
            Log.Write(LogGroup.Server, $"Shuttle delivered passenger {ride.PlayerId} to {ride.Destination} on login.");
            return location;
        }

        /// <summary>
        /// Reminds a returning ticket holder and gives them a full boarding-call interval
        /// to refund or leave the terminal before automatic boarding can resume.
        /// </summary>
        private static void RestoreTicketedSession(uint player, DateTime now)
        {
            var playerId = GetObjectUUID(player);
            // A ticket purchased during the login delay already has explicit boarding
            // consent for this session. Do not postpone the flight after confirmation.
            if (_boardingNotBeforeUtc.ContainsKey(playerId))
                return;

            var ride = GetRide(playerId);
            if (ride == null || ride.Status != ShuttleRideStatus.Ticketed)
                return;

            var route = GetRideRoute(ride);
            _ticketHolders[playerId] = route;
            _boardingNotBeforeUtc[playerId] = now.AddSeconds(BoardingCallSeconds);
            var next = GetNextDepartureUtc(route, playerId);
            var wait = Time.GetTimeLongIntervals(next - now, false);
            var originName = Station.GetStopName(route.Origin);
            var destinationName = Station.GetStopName(route.Destination);
            SendMessageToPC(player, ColorToken.Yellow($"You still have a shuttle ticket from {originName} to {destinationName}. Automatic boarding resumes in {wait} if you are within {(int)BoardingRangeMeters} meters of a flights terminal at {originName}. You can refund your ticket at any flights terminal before boarding."));
        }

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void ExitServer()
        {
            _boardingNotBeforeUtc.Remove(GetObjectUUID(GetExitingObject()));
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
            _boardingNotBeforeUtc.Remove(playerId);

            Scheduler.Schedule(() =>
            {
                var current = GetOnlinePlayerById(playerId);
                if (!GetIsObjectValid(current) || current != player)
                    return;

                var ride = GetRide(playerId);
                if (ride == null)
                    return;

                if (ride.Status == ShuttleRideStatus.Ticketed)
                {
                    RestoreTicketedSession(current, DateTime.UtcNow);
                    return;
                }

                if (ride.Status != ShuttleRideStatus.InTransit)
                    return;

                // Announce every resumed flight, including a reconnect directly into its
                // existing area. Login redirects may already have placed the passenger there.
                if (ride.ArrivalUtc > DateTime.UtcNow)
                {
                    var remaining = Time.GetTimeLongIntervals(ride.ArrivalUtc - DateTime.UtcNow, false);
                    SendMessageToPC(current, $"You resume your shuttle flight to {Station.GetStopName(GetRideRoute(ride).Destination)}. Estimated arrival: {remaining}.");
                }

                if (_activeFlights.TryGetValue(ride.FlightId, out var flight) &&
                    GetIsObjectValid(flight.Area) &&
                    GetArea(current) == flight.Area &&
                    ride.ArrivalUtc > DateTime.UtcNow)
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
        /// When a passenger leaves the shuttle interior before it lands - by any means other than
        /// arrival, such as a DM port, a death respawn, or a recall - they forfeit the trip.
        /// The ride record is deleted so they are not re-boarded on login or delivered to the
        /// destination while offline. A normal logout does not fire this event, so passengers who
        /// stay aboard and disconnect still resume or arrive as intended.
        /// </summary>
        [NWNEventHandler(ScriptName.OnAreaExit)]
        public static void HandleShuttleAreaExit()
        {
            var player = GetExitingObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var area = OBJECT_SELF;
            var flightId = GetLocalString(area, ShuttleFlightIdVariable);
            if (string.IsNullOrWhiteSpace(flightId))
                return;

            var ride = GetRide(GetObjectUUID(player));
            if (ride == null || ride.Status != ShuttleRideStatus.InTransit || ride.FlightId != flightId)
                return;

            // Arrival also moves passengers out of the interior, but only once the flight has
            // landed. An exit before that is a forced removal, not a normal disembarkation.
            if (ride.ArrivalUtc <= DateTime.UtcNow)
                return;

            DB.Delete<ShuttleRide>(ride.Id);
            Log.Write(LogGroup.Server, $"Shuttle passenger {ride.PlayerId} left flight {flightId} before arrival; ticket forfeited.");
        }

        /// <summary>
        /// Retrieves the next scheduled departure for a route.
        /// </summary>
        /// <param name="origin">The origin planet.</param>
        /// <param name="destination">The destination planet.</param>
        /// <param name="playerId">Optional passenger whose login boarding grace period applies.</param>
        /// <returns>The UTC time of the next departure.</returns>
        public static DateTime GetNextDepartureUtc(PlanetType origin, PlanetType destination, string playerId = null)
        {
            var notBefore = default(DateTime);
            if (!string.IsNullOrWhiteSpace(playerId))
                _boardingNotBeforeUtc.TryGetValue(playerId, out notBefore);

            return ShuttleSchedule.GetNextDepartureUtc(origin, destination, DateTime.UtcNow, notBefore);
        }

        /// <summary>
        /// Retrieves the next scheduled departure for a route that may start or end at a player station.
        /// </summary>
        /// <param name="route">The route.</param>
        /// <param name="playerId">Optional passenger whose login boarding grace period applies.</param>
        /// <returns>The UTC time of the next departure.</returns>
        public static DateTime GetNextDepartureUtc(ShuttleRoute route, string playerId = null)
        {
            var notBefore = default(DateTime);
            if (!string.IsNullOrWhiteSpace(playerId))
                _boardingNotBeforeUtc.TryGetValue(playerId, out notBefore);

            return ShuttleSchedule.GetNextDepartureUtc(route, DateTime.UtcNow, notBefore);
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

            destination = flight.Route.Destination.Planet;
            arrivalUtc = flight.ArrivalUtc;
            return true;
        }

        /// <summary>
        /// Retrieves the destination name (a planet or a player station) of the flight registered to a shuttle
        /// status console or shuttle area, if any.
        /// </summary>
        public static bool TryGetFlightDestinationName(uint obj, out string destinationName, out DateTime arrivalUtc)
        {
            destinationName = string.Empty;
            arrivalUtc = default;

            var flightId = GetLocalString(obj, ShuttleFlightIdVariable);
            if (string.IsNullOrWhiteSpace(flightId) || !_activeFlights.TryGetValue(flightId, out var flight))
                return false;

            destinationName = Station.GetStopName(flight.Route.Destination);
            arrivalUtc = flight.ArrivalUtc;
            return true;
        }
    }
}
