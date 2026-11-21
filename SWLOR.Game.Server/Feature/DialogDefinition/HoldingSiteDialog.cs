using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.HoldingService;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    /// <summary>The office terminal at a Planetary Holding's ground site: manage the holding, or go back to the dock.</summary>
    public class HoldingSiteDialog : ConversationMenuDefinition
    {
        private const string MainPageId = "MAIN_PAGE";

        public override ConversationMenuSpec Build()
        {
            var builder = new ConversationMenuBuilder()
                .AddPage(MainPageId, MainPageInit);

            return builder.Build();
        }

        private void MainPageInit(ConversationMenuPage page)
        {
            var player = Player;
            var holdingId = GetLocalString(Owner, PlanetaryHoldings.SiteHoldingIdVariable);
            var holding = PlanetaryHoldings.LoadHolding(player, holdingId);
            if (holding == null)
            {
                page.Header = "The terminal does not answer to you.";
            }
            else
            {
                var detail = HoldingRules.GetDetail(holding.Type);
                page.Header = $"{PlanetaryHoldings.GetHoldingName(holding)}, level {holding.Level}.\n\n{detail.Description}";
                page.AddResponse("Manage the holding", () =>
                {
                    Close();
                    var payload = new HoldingsPayload(detail.Exchange, holding.DockId, holding.Id);
                    Gui.TogglePlayerWindow(player, GuiWindowType.PlanetaryHoldings, payload, Owner);
                });
            }

            page.AddResponse("Return to the starport", () =>
            {
                Close();
                PlanetaryHoldings.LeaveSite(player);
            });
        }
    }
}
