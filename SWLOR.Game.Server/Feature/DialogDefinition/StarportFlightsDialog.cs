using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DialogService;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.ShuttleService;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class StarportFlightsDialog: DialogBase
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

        public override PlayerDialog SetUp(uint player)
        {
            var builder = new DialogBuilder()
                .WithDataModel(new Model())
                .AddInitializationAction(Initialize)
                .AddPage(MainPageId, MainPageInit)
                .AddPage(ConfirmPageId, ConfirmPageInit);

            return builder.Build();
        }

        private void Initialize()
        {
            var terminal = GetDialogTarget();
            var area = GetArea(terminal);
            var propertyId = Property.GetPropertyId(area);
            var model = GetDataModel<Model>();

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

        private static string FormatMinutes(int seconds)
        {
            var minutes = seconds / 60m;
            return minutes == decimal.Truncate(minutes)
                ? minutes.ToString("0")
                : minutes.ToString("0.#");
        }

        private void MainPageInit(DialogPage page)
        {
            var model = GetDataModel<Model>();
            var player = GetPC();
            var terminal = GetDialogTarget();
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

        private void BuildTicketPurchasePage(DialogPage page, Model model, uint player, PlanetType origin)
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
                    ChangePage(ConfirmPageId);
                });
            }
        }

        private void BuildTicketedPage(DialogPage page, uint player, PlanetType terminalPlanet, ShuttleRide ride)
        {
            var destinationName = Planet.GetPlanetByType(ride.Destination).Name;
            var countdown = Time.GetTimeShortIntervals(Shuttle.GetNextDepartureUtc(ride.Origin, ride.Destination) - DateTime.UtcNow, false);
            var minutes = FormatMinutes(GalaxyMap.GetTransitSeconds(ride.Origin, ride.Destination));

            page.Header = ColorToken.Green("Ticketed Flight: ") + destinationName + "\n" +
                          ColorToken.Green("Departs in: ") + countdown + "\n" +
                          ColorToken.Green("Flight time: ") + minutes + " min\n\n" +
                          "Be within 15 meters of the flights terminal when boarding is called or you will miss this shuttle.";

            if (terminalPlanet == ride.Origin)
            {
                page.AddResponse($"Refund Ticket ({ride.FarePaid} cr)", () =>
                {
                    Shuttle.RefundTicket(player);
                    ChangePage(MainPageId, false);
                });
            }
            else
            {
                page.Header += "\n\n" + ColorToken.Red("Refunds are only available at your departure starport.");
            }
        }

        private void ConfirmPageInit(DialogPage page)
        {
            var player = GetPC();
            var model = GetDataModel<Model>();
            var destinationName = Planet.GetPlanetByType(model.Destination).Name;
            var fare = GalaxyMap.GetFare(model.Origin, model.Destination);
            var tax = (int)(model.Tax * fare);
            var price = fare + tax;
            var countdown = Time.GetTimeShortIntervals(Shuttle.GetNextDepartureUtc(model.Origin, model.Destination) - DateTime.UtcNow, false);
            var minutes = FormatMinutes(GalaxyMap.GetTransitSeconds(model.Origin, model.Destination));

            page.Header = ColorToken.Green("Selected Destination: ") + destinationName + "\n" +
                          ColorToken.Green("Fare: ") + fare + " cr\n" +
                          ColorToken.Green("Tax: ") + tax + " cr\n" +
                          ColorToken.Green("Total Price: ") + price + " cr\n" +
                          ColorToken.Green("Departs in: ") + countdown + "\n" +
                          ColorToken.Green("Flight time: ") + minutes + " min\n\n" +
                          "You may only hold one ticket at a time. You must be within 15 meters of this terminal " +
                          "when boarding is called or your ticket will roll over to the next shuttle. " +
                          "Tickets are refundable (fare only) before boarding.";

            var notEnoughGoldMessage = ColorToken.Red("You do not have enough credits to purchase this flight!");
            if (GetGold(player) < price)
            {
                page.Header += "\n\n" + notEnoughGoldMessage;
            }
            else
            {
                page.AddResponse("Confirm Flight", () =>
                {
                    if (!Shuttle.TryPurchaseTicket(player, model.Origin, model.Destination, model.CityPropertyId, model.Tax))
                        return;

                    var confirmedCountdown = Time.GetTimeShortIntervals(Shuttle.GetNextDepartureUtc(model.Origin, model.Destination) - DateTime.UtcNow, false);
                    SendMessageToPC(player, ColorToken.Green($"Ticket purchased! Your shuttle to {destinationName} departs in {confirmedCountdown}."));

                    ChangePage(MainPageId, false);
                });
            }
        }
    }
}
