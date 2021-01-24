using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DialogService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class StarportFlightsDialog: DialogBase
    {
        private class Model
        {
            public int Price { get; set; }
            public string DestinationTag { get; set; }
            public string PlanetName { get; set; }
        }

        private const string MainPageId = "MAIN_PAGE";
        private const string ConfirmPageId = "CONFIRM_PAGE";

        public override PlayerDialog SetUp(uint player)
        {
            var builder = new DialogBuilder()
                .WithDataModel(new Model())
                .AddPage(MainPageId, MainPageInit)
                .AddPage(ConfirmPageId, ConfirmPageInit);

            return builder.Build();
        }

        private void MainPageInit(DialogPage page)
        {
            var model = GetDataModel<Model>();
            var terminal = GetDialogTarget();
            var currentLocation = (PlanetType)GetLocalInt(terminal, "CURRENT_LOCATION");

            page.Header = "Charter flights leave hourly. Please select one our available destinations below.";

            if (currentLocation != PlanetType.Viscara)
            {
                page.AddResponse("Viscara", () =>
                {
                    model.PlanetName = "Viscara";
                    model.Price = 100;
                    model.DestinationTag = "VISCARA_LANDING";

                    ChangePage(ConfirmPageId);
                });
            }

            if (currentLocation != PlanetType.MonCala)
            {
                page.AddResponse("Mon Cala", () =>
                {
                    model.PlanetName = "Mon Cala";
                    model.Price = 200;
                    model.DestinationTag = "MON_CALA_LANDING";

                    ChangePage(ConfirmPageId);
                });
            }

            if (currentLocation != PlanetType.Hutlar)
            {
                page.AddResponse("Hutlar", () =>
                {
                    model.PlanetName = "Hutlar";
                    model.Price = 300;
                    model.DestinationTag = "HUTLAR_LANDING";

                    ChangePage(ConfirmPageId);
                });
            }

            if (currentLocation != PlanetType.Tatooine)
            {
                page.AddResponse("Tatooine", () =>
                {
                    model.PlanetName = "Tatooine";
                    model.Price = 400;
                    model.DestinationTag = "TATOOINE_LANDING";

                    ChangePage(ConfirmPageId);
                });
            }
        }

        private void ConfirmPageInit(DialogPage page)
        {
            var player = GetPC();
            var model = GetDataModel<Model>();
            page.Header = ColorToken.Green("Selected Destination: ") + model.PlanetName + "\n" +
                ColorToken.Green("Price: ") + model.Price + "\n\n" +
                "This trip is one-way and non-refundable. Are you sure you want to take this flight?";

            var notEnoughGoldMessage = ColorToken.Red("You do not have enough credits to purchase this flight!");
            if (GetGold(player) < model.Price)
            {
                page.Header += "\n\n" + notEnoughGoldMessage;
            }
            else
            {
                page.AddResponse("Confirm Flight", () =>
                {
                    if (GetGold(player) < model.Price)
                    {
                        SendMessageToPC(player, notEnoughGoldMessage);
                        return;
                    }

                    TakeGoldFromCreature(model.Price, player, true);
                    var location = GetLocation(GetWaypointByTag(model.DestinationTag));

                    AssignCommand(player, () =>
                    {
                        ClearAllActions();
                        ActionJumpToLocation(location);
                    });

                    EndConversation();
                });
            }
        }
    }
}
