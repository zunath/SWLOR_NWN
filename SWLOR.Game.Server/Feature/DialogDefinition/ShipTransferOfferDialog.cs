using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ConversationService;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    /// <summary>
    /// The recipient's side of a direct ship transfer (plan section 7): the ship's summary, then Accept or Decline.
    /// <see cref="ShipTransfer"/> holds the offer and checks everything again on Accept.
    /// </summary>
    public class ShipTransferOfferDialog : ConversationMenuDefinition
    {
        private const string MainPageId = "MAIN_PAGE";

        public override ConversationMenuSpec Build()
        {
            var builder = new ConversationMenuBuilder()
                .AddInitializationAction(Initialization)
                .AddPage(MainPageId, MainPageInit);

            return builder.Build();
        }

        private void Initialization()
        {
            if (ShipTransfer.GetOffer(Player) == null)
                Close();
        }

        private void MainPageInit(ConversationMenuPage page)
        {
            var offer = ShipTransfer.GetOffer(Player);
            var ship = offer == null ? null : DB.Get<PlayerShip>(offer.ShipId);
            if (ship == null)
            {
                page.Header = "The offer has expired.";
                return;
            }

            var giver = DMPlayerDirectory.GetOnlinePlayer(offer.GiverPlayerId);
            var giverName = GetIsObjectValid(giver) ? GetName(giver) : "Its owner";
            page.Header = $"{giverName} offers you a ship. They pay the {offer.Fee} credit transfer fee.\n\n" +
                          ShipTransfer.BuildSummary(ship);

            page.AddResponse(ColorToken.Green("Accept the ship"), () =>
            {
                ShipTransfer.Accept(Player);
                Close();
            });

            page.AddResponse("Decline", () =>
            {
                ShipTransfer.Decline(Player);
                Close();
            });
        }
    }
}
