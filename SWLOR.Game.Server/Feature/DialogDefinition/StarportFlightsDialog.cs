using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.ShuttleService;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class StarportFlightsDialog: ConversationMenuDefinition
    {
        private class Model
        {
            public ShuttleStop Origin { get; set; }
            public ShuttleStop Destination { get; set; }
            public float Tax { get; set; }
            public string CityPropertyId { get; set; }
        }

        private const string MainPageId = "MAIN_PAGE";
        private const string ConfirmPageId = "CONFIRM_PAGE";

        public override ConversationMenuSpec Build()
        {
            var builder = new ConversationMenuBuilder()
                .WithDataModel(new Model())
                .AddInitializationAction(Initialize)
                .AddPage(MainPageId, MainPageInit)
                .AddPage(ConfirmPageId, ConfirmPageInit);

            return builder.Build();
        }

        /// <summary>
        /// Resolves the terminal's stop (a planet, or a player station) and the city transportation tax rate for
        /// property-based starports.
        /// </summary>
        private void Initialize()
        {
            var terminal = Owner;
            var area = GetArea(terminal);
            var propertyId = Property.GetPropertyId(area);
            var model = Data<Model>();

            model.Origin = GetTerminalStop(terminal);

            if (string.IsNullOrWhiteSpace(propertyId))
                return;

            var dbProperty = DB.Get<WorldProperty>(propertyId);
            var dbBuilding = DB.Get<WorldProperty>(dbProperty.ParentPropertyId);
            var dbCity = DB.Get<WorldProperty>(dbBuilding.ParentPropertyId);

            var transportationTaxRate = dbCity.Taxes[PropertyTaxType.Transportation];
            transportationTaxRate = Math.Clamp(transportationTaxRate, 0, 25);

            model.Tax = 0.01f * transportationTaxRate;
            model.CityPropertyId = dbCity.Id;
        }

        /// <summary>The terminal's stop: its planet, or the player station whose Starport it stands in.</summary>
        private static ShuttleStop GetTerminalStop(uint terminal)
        {
            var planet = (PlanetType)GetLocalInt(terminal, "CURRENT_LOCATION");
            var stationId = GetLocalString(terminal, Station.StationCityVariable);
            if (string.IsNullOrWhiteSpace(stationId))
                return ShuttleStop.ForPlanet(planet);

            var stop = Station.GetShuttleStop(stationId);
            return stop.IsStation ? stop : ShuttleStop.ForStation(stationId, planet, false);
        }

        /// <summary>
        /// Renders the main terminal page: pass gate, ticket purchase, or existing ticket status.
        /// </summary>
        private void MainPageInit(ConversationMenuPage page)
        {
            var model = Data<Model>();
            var player = Player;
            var origin = model.Origin;

            // Travel is locked until the player earns a CZ-220 Shuttle Pass by completing their
            // orientation on CZ-220. This keeps new arrivals on the tutorial station.
            if (!KeyItem.HasKeyItem(player, KeyItemType.CZ220ShuttlePass) && !GetIsDM(player))
            {
                page.Header = "The terminal rejects your credentials. You'll need a CZ-220 Shuttle Pass before you can book a flight - complete your orientation here on CZ-220 to earn one.";
                return;
            }

            var ride = Shuttle.GetRide(GetObjectUUID(player));

            if (ride == null)
            {
                BuildTicketPurchasePage(page, model, player, origin);
            }
            else if (ride.Status == ShuttleRideStatus.Ticketed)
            {
                BuildTicketedPage(page, player, origin, ride);
            }
            else // InTransit
            {
                page.Header = "You are already booked on a flight.";
            }
        }

        /// <summary>
        /// Renders the list of destinations a passenger can book a ticket to. A player station's terminal flies only to
        /// its home port; a planet's terminal also lists the stations it serves (plan section 25).
        /// </summary>
        private void BuildTicketPurchasePage(ConversationMenuPage page, Model model, uint player, ShuttleStop origin)
        {
            var hasSmugglerPass = KeyItem.HasKeyItem(player, KeyItemType.SmugglerPass);

            page.Header = "Shuttles run on a fixed schedule. When boarding is called, you must be " +
                          "within 15 meters of this terminal or you will miss your flight. " +
                          "Please select one of our available destinations below.";

            void AddDestination(ShuttleStop destination)
            {
                var route = new ShuttleRoute(origin, destination);
                var fare = ShuttleRouteRules.GetFare(route);
                var tax = (int)(model.Tax * fare);
                var price = fare + tax;
                var countdown = Time.GetTimeLongIntervals(Shuttle.GetNextDepartureUtc(route) - DateTime.UtcNow, false);
                var transit = Time.GetTimeLongIntervals(TimeSpan.FromSeconds(ShuttleRouteRules.GetTransitSeconds(route)), false);

                var optionText = $"{Station.GetStopName(destination)} [{price} cr] - departs in {countdown}, flight time {transit}";
                page.AddResponse(optionText, () =>
                {
                    model.Destination = destination;
                    GoToPage(ConfirmPageId);
                });
            }

            if (origin.IsStation)
            {
                if (origin.Planet != PlanetType.SmugglersMoon || hasSmugglerPass)
                    AddDestination(ShuttleStop.ForPlanet(origin.Planet));

                return;
            }

            var planets = Planet.GetAllPlanets();

            foreach (var (type, _) in planets)
            {
                if (origin.Planet == type ||
                    type == PlanetType.SmugglersMoonStation ||
                    (type == PlanetType.SmugglersMoon && !hasSmugglerPass))
                {
                    continue;
                }

                AddDestination(ShuttleStop.ForPlanet(type));
            }

            foreach (var station in ShuttleRouteRules.GetStationDestinations(origin, Station.GetShuttleStations()))
            {
                AddDestination(station);
            }
        }

        /// <summary>
        /// Renders the player's existing ticket status, with a refund option at any starport.
        /// </summary>
        private void BuildTicketedPage(ConversationMenuPage page, uint player, ShuttleStop terminalStop, ShuttleRide ride)
        {
            var route = Shuttle.GetRideRoute(ride);
            var origin = route.Origin;
            var destinationName = Station.GetStopName(route.Destination);
            var countdown = Time.GetTimeLongIntervals(Shuttle.GetNextDepartureUtc(route, ride.PlayerId) - DateTime.UtcNow, false);
            var transit = Time.GetTimeLongIntervals(TimeSpan.FromSeconds(ShuttleRouteRules.GetTransitSeconds(route)), false);

            page.Header = "Ticketed Flight: " + destinationName + "\n" +
                          "Departs in: " + countdown + "\n" +
                          "Flight time: " + transit + "\n\n" +
                          "Be within 15 meters of the flights terminal when boarding is called or you will miss this shuttle.";

            if (terminalStop != origin)
            {
                var originName = Station.GetStopName(origin);
                page.Header += $"\n\nThis ticket departs from {originName}. You may refund it here to book a different flight.";
            }

            page.AddResponse($"Refund Ticket ({ride.FarePaid} cr)", () =>
            {
                Shuttle.RefundTicket(player);
                GoToPage(MainPageId, false);
            });
        }

        /// <summary>
        /// Renders the fare, tax, and time breakdown and confirms the ticket purchase.
        /// </summary>
        private void ConfirmPageInit(ConversationMenuPage page)
        {
            var player = Player;
            var model = Data<Model>();
            var route = new ShuttleRoute(model.Origin, model.Destination);
            var destinationName = Station.GetStopName(model.Destination);
            var fare = ShuttleRouteRules.GetFare(route);
            var tax = (int)(model.Tax * fare);
            var price = fare + tax;
            var countdown = Time.GetTimeLongIntervals(Shuttle.GetNextDepartureUtc(route) - DateTime.UtcNow, false);
            var transit = Time.GetTimeLongIntervals(TimeSpan.FromSeconds(ShuttleRouteRules.GetTransitSeconds(route)), false);

            page.Header = "Selected Destination: " + destinationName + "\n" +
                          "Fare: " + fare + " cr\n" +
                          "Tax: " + tax + " cr\n" +
                          "Total Price: " + price + " cr\n" +
                          "Departs in: " + countdown + "\n" +
                          "Flight time: " + transit + "\n\n" +
                          "You may only hold one ticket at a time. You must be within 15 meters of this terminal " +
                          "when boarding is called or your ticket will roll over to the next shuttle. " +
                          "Tickets are refundable (fare only) before boarding.";

            if (GetGold(player) < price)
            {
                page.Header += "\n\nYou do not have enough credits to purchase this flight!";
            }
            else
            {
                page.AddResponse("Confirm Flight", () =>
                {
                    if (!Shuttle.TryPurchaseTicket(player, route, model.CityPropertyId, model.Tax))
                        return;

                    var confirmedCountdown = Time.GetTimeLongIntervals(Shuttle.GetNextDepartureUtc(route) - DateTime.UtcNow, false);
                    SendMessageToPC(player, ColorToken.Green($"Ticket purchased! Your shuttle to {destinationName} departs in {confirmedCountdown}."));

                    Close();
                });
            }
        }
    }
}
