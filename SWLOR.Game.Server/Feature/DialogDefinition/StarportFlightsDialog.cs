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
            public PlanetType Origin { get; set; }
            public PlanetType Destination { get; set; }
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
        /// Resolves the city transportation tax rate for property-based starports.
        /// </summary>
        private void Initialize()
        {
            var terminal = Owner;
            var area = GetArea(terminal);
            var propertyId = Property.GetPropertyId(area);
            var model = Data<Model>();

            model.Origin = (PlanetType)GetLocalInt(terminal, "CURRENT_LOCATION");

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

        /// <summary>
        /// Formats a transit time in seconds as a minute count, trimming a trailing zero decimal.
        /// </summary>
        private static string FormatMinutes(int seconds)
        {
            var minutes = seconds / 60m;
            return minutes == decimal.Truncate(minutes)
                ? minutes.ToString("0")
                : minutes.ToString("0.#");
        }

        /// <summary>
        /// Renders the main terminal page: pass gate, ticket purchase, or existing ticket status.
        /// </summary>
        private void MainPageInit(ConversationMenuPage page)
        {
            var model = Data<Model>();
            var player = Player;
            var terminal = Owner;
            var origin = (PlanetType)GetLocalInt(terminal, "CURRENT_LOCATION");

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
        /// Renders the list of destinations a passenger can book a ticket to.
        /// </summary>
        private void BuildTicketPurchasePage(ConversationMenuPage page, Model model, uint player, PlanetType origin)
        {
            var hasSmugglerPass = KeyItem.HasKeyItem(player, KeyItemType.SmugglerPass);

            page.Header = "Shuttles run on a fixed schedule. When boarding is called, you must be " +
                          "within 15 meters of this terminal or you will miss your flight. " +
                          "Please select one of our available destinations below.";

            var planets = Planet.GetAllPlanets();

            foreach (var (type, planet) in planets)
            {
                if (origin == type ||
                    type == PlanetType.SmugglersMoonStation ||
                    (type == PlanetType.SmugglersMoon && !hasSmugglerPass))
                {
                    continue;
                }

                var fare = GalaxyMap.GetFare(origin, type);
                var tax = (int)(model.Tax * fare);
                var price = fare + tax;
                var countdown = Time.GetTimeShortIntervals(Shuttle.GetNextDepartureUtc(origin, type) - DateTime.UtcNow, false);
                var minutes = FormatMinutes(GalaxyMap.GetTransitSeconds(origin, type));

                var optionText = $"{planet.Name} [{price} cr] - departs in {countdown}, flight time {minutes} min";
                page.AddResponse(optionText, () =>
                {
                    model.Destination = type;
                    GoToPage(ConfirmPageId);
                });
            }
        }

        /// <summary>
        /// Renders the player's existing ticket status, with a refund option at the origin starport.
        /// </summary>
        private void BuildTicketedPage(ConversationMenuPage page, uint player, PlanetType terminalPlanet, ShuttleRide ride)
        {
            var destinationName = Planet.GetPlanetByType(ride.Destination).Name;
            var countdown = Time.GetTimeShortIntervals(Shuttle.GetNextDepartureUtc(ride.Origin, ride.Destination) - DateTime.UtcNow, false);
            var minutes = FormatMinutes(GalaxyMap.GetTransitSeconds(ride.Origin, ride.Destination));

            page.Header = "Ticketed Flight: " + destinationName + "\n" +
                          "Departs in: " + countdown + "\n" +
                          "Flight time: " + minutes + " min\n\n" +
                          "Be within 15 meters of the flights terminal when boarding is called or you will miss this shuttle.";

            if (terminalPlanet == ride.Origin)
            {
                page.AddResponse($"Refund Ticket ({ride.FarePaid} cr)", () =>
                {
                    Shuttle.RefundTicket(player);
                    GoToPage(MainPageId, false);
                });
            }
            else
            {
                page.Header += "\n\nRefunds are only available at your departure starport.";
            }
        }

        /// <summary>
        /// Renders the fare, tax, and time breakdown and confirms the ticket purchase.
        /// </summary>
        private void ConfirmPageInit(ConversationMenuPage page)
        {
            var player = Player;
            var model = Data<Model>();
            var destinationName = Planet.GetPlanetByType(model.Destination).Name;
            var fare = GalaxyMap.GetFare(model.Origin, model.Destination);
            var tax = (int)(model.Tax * fare);
            var price = fare + tax;
            var countdown = Time.GetTimeShortIntervals(Shuttle.GetNextDepartureUtc(model.Origin, model.Destination) - DateTime.UtcNow, false);
            var minutes = FormatMinutes(GalaxyMap.GetTransitSeconds(model.Origin, model.Destination));

            page.Header = "Selected Destination: " + destinationName + "\n" +
                          "Fare: " + fare + " cr\n" +
                          "Tax: " + tax + " cr\n" +
                          "Total Price: " + price + " cr\n" +
                          "Departs in: " + countdown + "\n" +
                          "Flight time: " + minutes + " min\n\n" +
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
                    if (!Shuttle.TryPurchaseTicket(player, model.Origin, model.Destination, model.CityPropertyId, model.Tax))
                        return;

                    var confirmedCountdown = Time.GetTimeShortIntervals(Shuttle.GetNextDepartureUtc(model.Origin, model.Destination) - DateTime.UtcNow, false);
                    SendMessageToPC(player, ColorToken.Green($"Ticket purchased! Your shuttle to {destinationName} departs in {confirmedCountdown}."));

                    Close();
                });
            }
        }
    }
}
