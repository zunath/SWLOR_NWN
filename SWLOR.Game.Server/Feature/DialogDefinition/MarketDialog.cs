using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DialogService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PlayerMarketService;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class MarketDialog: DialogBase
    {
        private const string MainPageId = "MAIN_PAGE";

        public override PlayerDialog SetUp(uint player)
        {
            var builder = new DialogBuilder()
                .AddPage(MainPageId, MainPageInit);

            return builder.Build();
        }

        private void MainPageInit(DialogPage page)
        {
            var player = GetPC();
            var terminal = OBJECT_SELF;
            var regionType = (MarketRegionType)GetLocalInt(terminal, "MARKET_ID");
            var marketRegion = PlayerMarket.GetMarketRegion(regionType);

            if (regionType == MarketRegionType.Invalid)
            {
                FloatingTextStringOnCreature("Notify a DM that this market is improperly configured.", player, false);
                Log.Write(LogGroup.Error, $"{GetName(terminal)} is improperly configured with an invalid market Id. Area: {GetName(GetArea(terminal))}");
                return;
            }

            page.Header = $"{ColorToken.Green("Market: ")} {marketRegion.Name}\n\n" +
                          $"Goods may be bought and sold on the market here. What would you like to do?";

            page.AddResponse("Buy", () =>
            {
                Gui.TogglePlayerWindow(player, GuiWindowType.MarketBuying, new MarketPayload(regionType), terminal);
                EndConversation();
            });

            page.AddResponse("Sell", () =>
            {
                Gui.TogglePlayerWindow(player, GuiWindowType.MarketListing, new MarketPayload(regionType), terminal);
                EndConversation();
            });
        }
    }
}
