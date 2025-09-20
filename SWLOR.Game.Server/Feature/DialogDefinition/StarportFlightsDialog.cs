using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DialogService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Shared.Core.Log;
using SWLOR.Shared.Core.Service;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class StarportFlightsDialog: DialogBase
    {
        private class Model
        {
            public int Price { get; set; }
            public string DestinationTag { get; set; }
            public string PlanetName { get; set; }
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

            if (string.IsNullOrWhiteSpace(propertyId))
                return;

            var dbProperty = DB.Get<WorldProperty>(propertyId);
            var dbBuilding = DB.Get<WorldProperty>(dbProperty.ParentPropertyId);
            var dbCity = DB.Get<WorldProperty>(dbBuilding.ParentPropertyId);

            model.Tax = 0.01f * dbCity.Taxes[PropertyTaxType.Transportation];
            model.CityPropertyId = dbCity.Id;
        }


        private void MainPageInit(DialogPage page)
        {
            var model = GetDataModel<Model>();
            var terminal = GetDialogTarget();
            var currentLocation = (PlanetType)GetLocalInt(terminal, "CURRENT_LOCATION");

            page.Header = "Charter flights leave hourly. Please select one our available destinations below.";

            var planets = Planet.GetAllPlanets();

            foreach (var (type, planet) in planets)
            {
                if (currentLocation != type)
                {
                    var tax = (int)(model.Tax * planet.NPCTransportationFee);
                    var price = planet.NPCTransportationFee + tax;
                    var optionText = $"{planet.Name} [{price} cr]";
                    page.AddResponse(optionText, () =>
                    {
                        model.PlanetName = planet.Name;
                        model.Price = planet.NPCTransportationFee;
                        model.DestinationTag = planet.LandingWaypointTag;

                        ChangePage(ConfirmPageId);
                    });
                }
            }
        }

        private void ConfirmPageInit(DialogPage page)
        {
            var player = GetPC();
            var model = GetDataModel<Model>();
            var tax = (int)(model.Tax * model.Price);
            var price = model.Price + tax;

            page.Header = ColorToken.Green("Selected Destination: ") + model.PlanetName + "\n" +
                          ColorToken.Green("Price: ") + price + "\n\n" +
                          "This trip is one-way and non-refundable. Are you sure you want to take this flight?";

            var notEnoughGoldMessage = ColorToken.Red("You do not have enough credits to purchase this flight!");
            if (GetGold(player) < price)
            {
                page.Header += "\n\n" + notEnoughGoldMessage;
            }
            else
            {
                page.AddResponse("Confirm Flight", () =>
                {
                    if (GetGold(player) < price)
                    {
                        SendMessageToPC(player, notEnoughGoldMessage);
                        return;
                    }

                    TakeGoldFromCreature(price, player, true);
                    var location = GetLocation(GetWaypointByTag(model.DestinationTag));

                    AssignCommand(player, () =>
                    {
                        ClearAllActions();
                        ActionJumpToLocation(location);
                    });

                    EndConversation();

                    if (!string.IsNullOrWhiteSpace(model.CityPropertyId))
                    {
                        var dbCity = DB.Get<WorldProperty>(model.CityPropertyId);
                        if (dbCity == null)
                            return;

                        dbCity.Treasury += tax;
                        DB.Set(dbCity);
                        Log.Write(LogGroup.Property, $"{GetName(player)} paid {tax} credits in tax for their trip to {model.PlanetName}.");
                    }

                });
            }
        }
    }
}
