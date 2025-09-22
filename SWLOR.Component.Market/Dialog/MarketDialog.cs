using SWLOR.Component.Market.Enums;
using SWLOR.Component.Market.Service;
using SWLOR.Component.Market.UI.Payload;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Log.LogGroup;
using SWLOR.Shared.Dialog.Contracts;
using SWLOR.Shared.Dialog.Model;
using SWLOR.Shared.Dialog.Service;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Enums;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Market.Dialog
{
    public class MarketDialog: DialogBase
    {
        private readonly ILogger _logger;
        private readonly IGuiService _guiService;
        private const string MainPageId = "MAIN_PAGE";

        public MarketDialog(IGuiService guiService, ILogger logger, IDialogService dialogService) 
            : base(dialogService)
        {
            _guiService = guiService;
            _logger = logger;
        }

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
                _logger.Write<ErrorLogGroup>($"{GetName(terminal)} is improperly configured with an invalid market Id. Area: {GetName(GetArea(terminal))}");
                return;
            }

            page.Header = $"{ColorToken.Green("Market: ")} {marketRegion.Name}\n\n" +
                          $"Goods may be bought and sold on the market here. What would you like to do?";

            page.AddResponse("Buy", () =>
            {
                _guiService.TogglePlayerWindow(player, GuiWindowType.MarketBuying, new MarketPayload(regionType), terminal);
                EndConversation();
            });

            page.AddResponse("Sell", () =>
            {
                _guiService.TogglePlayerWindow(player, GuiWindowType.MarketListing, new MarketPayload(regionType), terminal);
                EndConversation();
            });
        }
    }
}
