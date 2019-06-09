using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public class StarportFlights: ConversationBase
    {
        private class Model
        {
            public int Price { get; set; }
            public string DestinationTag { get; set; }

            public Model(int price, string destinationTag)
            {
                Price = price;
                DestinationTag = destinationTag;
            }
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");
            DialogPage mainPage = new DialogPage("Charter flights leave hourly. Please select one our available destinations below.");
            DialogPage confirmPage = new DialogPage("<SET LATER>", 
                "Confirm Flight");

            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("ConfirmPage", confirmPage);
            return dialog;
        }

        public override void Initialize()
        {
            LoadDestinations();
        }

        private void LoadDestinations()
        {
            NWObject terminal = GetDialogTarget();
            int currentLocation = terminal.GetLocalInt("CURRENT_LOCATION");

            ClearPageResponses("MainPage");
            AddResponseToPage("MainPage", "Viscara", currentLocation != (int)Planet.Viscara);
            AddResponseToPage("MainPage", "Mon Cala", currentLocation != (int)Planet.MonCala);
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (pageName)
            {
                case "MainPage":
                    MainPageResponses(responseID);
                    break;
                case "ConfirmPage":
                    ConfirmPageResponses(responseID);
                    break;
            }
        }

        private void MainPageResponses(int responseID)
        {
            NWPlayer player = GetPC();
            string planet;
            int price;
            string destinationTag;
            switch (responseID)
            {
                case 1: // Viscara
                    planet = "Viscara";
                    price = 600;
                    destinationTag = "VISCARA_LANDING";
                    break;
                case 2: // Mon Cala
                    planet = "Mon Cala";
                    price = 600;
                    destinationTag = "MON_CALA_LANDING";
                    break;
                default: return;
            }

            string header = ColorTokenService.Green("Selected Destination: ") + planet + "\n";
            header += ColorTokenService.Green("Price: ") + price + "\n\n";
            header += "This trip is one-way and non-refundable. Are you sure you want to take this flight?";

            // Check if player has enough money
            if (player.Gold < price)
            {
                header += "\n\n" + ColorTokenService.Red("You do not have enough credits to purchase this flight!");
                SetResponseVisible("ConfirmPage", 1, false);
            }
            else
            {
                SetResponseVisible("ConfirmPage", 1, true);
            }

            // Store selected data for later use in the conversation.
            var model = new Model(price, destinationTag);
            SetDialogCustomData(model);

            SetPageHeader("ConfirmPage", header);
            ChangePage("ConfirmPage");

        }

        private void ConfirmPageResponses(int responseID)
        {
            var player = GetPC();
            var model = GetDialogCustomData<Model>();

            switch (responseID)
            {
                case 1: // Confirm
                    // Check gold again.
                    if (player.Gold < model.Price)
                    {
                        player.SendMessage(ColorTokenService.Red("You do not have enough credits to purchase this flight."));
                        return;
                    }

                    // Take the gold.
                    _.TakeGoldFromCreature(model.Price, player, _.TRUE);

                    // Get the location based on the waypoint tag.
                    Location location = _.GetLocation(_.GetWaypointByTag(model.DestinationTag));

                    // Transport player.
                    player.AssignCommand(() =>
                    {
                        _.ClearAllActions();
                        _.ActionJumpToLocation(location);
                    });

                    EndConversation();
                    break;
            }
        }
        
        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
        }

        public override void EndDialog()
        {
        }
    }
}
