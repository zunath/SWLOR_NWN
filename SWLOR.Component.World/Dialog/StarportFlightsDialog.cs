using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.World.Contracts;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Log.LogGroup;
using SWLOR.Shared.Domain.Common.Contracts;
using SWLOR.Shared.Domain.Common.Enums;
using SWLOR.Shared.Domain.Dialog.Contracts;
using SWLOR.Shared.Domain.Dialog.ValueObjects;
using SWLOR.Shared.Domain.Properties.Contracts;
using SWLOR.Shared.Domain.Properties.Entities;
using SWLOR.Shared.Domain.Properties.Enums;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.World.Dialog
{
    public class StarportFlightsDialog: DialogBase
    {
        private readonly ILogger _logger;
        private readonly IDatabaseService _db;
        private readonly IServiceProvider _serviceProvider;
        
        // Lazy-loaded services to break circular dependencies
        private IPlanetService PlanetService => _serviceProvider.GetRequiredService<IPlanetService>();
        private IPropertyService PropertyService => _serviceProvider.GetRequiredService<IPropertyService>();

        public StarportFlightsDialog(ILogger logger, IDatabaseService db, IServiceProvider serviceProvider, IDialogService dialogService, IDialogBuilder dialogBuilder) : base(dialogService, dialogBuilder)
        {
            _logger = logger;
            _db = db;
            _serviceProvider = serviceProvider;
        }
        
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
            var builder = DialogBuilder
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
            var propertyId = PropertyService.GetPropertyId(area);
            var model = GetDataModel<Model>();

            if (string.IsNullOrWhiteSpace(propertyId))
                return;

            var dbProperty = _db.Get<WorldProperty>(propertyId);
            var dbBuilding = _db.Get<WorldProperty>(dbProperty.ParentPropertyId);
            var dbCity = _db.Get<WorldProperty>(dbBuilding.ParentPropertyId);

            model.Tax = 0.01f * dbCity.Taxes[PropertyTaxType.Transportation];
            model.CityPropertyId = dbCity.Id;
        }


        private void MainPageInit(DialogPage page)
        {
            var model = GetDataModel<Model>();
            var terminal = GetDialogTarget();
            var currentLocation = (PlanetType)GetLocalInt(terminal, "CURRENT_LOCATION");

            page.Header = "Charter flights leave hourly. Please select one our available destinations below.";

            var planets = PlanetService.GetAllPlanets();

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
                        var dbCity = _db.Get<WorldProperty>(model.CityPropertyId);
                        if (dbCity == null)
                            return;

                        dbCity.Treasury += tax;
                        _db.Set(dbCity);
                        _logger.Write<PropertyLogGroup>($"{GetName(player)} paid {tax} credits in tax for their trip to {model.PlanetName}.");
                    }

                });
            }
        }
    }
}
