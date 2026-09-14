using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PlayerMarketService;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class MarketDialog: ConversationMenuDefinition
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
            var terminal = Owner;
            var regionType = (MarketRegionType)GetLocalInt(terminal, "MARKET_ID");
            var marketRegion = PlayerMarket.GetMarketRegion(regionType);

            if (regionType == MarketRegionType.Invalid)
            {
                FloatingTextStringOnCreature("Notify a DM that this market is improperly configured.", player, false);
                Log.Write(LogGroup.Error, $"{GetName(terminal)} is improperly configured with an invalid market Id. Area: {GetName(GetArea(terminal))}");
                return;
            }

            page.Header = $"Market: {marketRegion.Name}\n\n" +
                          $"Goods may be bought and sold on the market here. What would you like to do?";

            page.AddResponse("Buy", () =>
            {
                Gui.TogglePlayerWindow(player, GuiWindowType.MarketBuying, new MarketPayload(regionType), terminal);
                Close();
            });

            page.AddResponse("Sell", () =>
            {
                Gui.TogglePlayerWindow(player, GuiWindowType.MarketListing, new MarketPayload(regionType), terminal);
                Close();
            });
        }
    }
}
