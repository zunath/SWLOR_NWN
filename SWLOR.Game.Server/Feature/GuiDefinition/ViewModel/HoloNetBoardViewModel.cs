using System;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class HoloNetBoardViewModel : GuiViewModelBase<HoloNetBoardViewModel, GuiPayloadBase>
    {
        public const string MainContentElement = "holonet_content";
        public const string MainContentPartial = "HOLONET_MAIN";

        public string AgeText { get => Get<string>(); set => Set(value); }
        public GuiBindingList<string> RowNames { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> RowTooltips { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> RowBuyAt { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> RowBuyPrices { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> RowSellAt { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> RowSellPrices { get => Get<GuiBindingList<string>>(); set => Set(value); }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            var board = Trade.GetHoloNetBoard();
            var age = HoloNetRules.GetAgeMinutes(board.CapturedAt, DateTime.UtcNow);
            AgeText = $"Prices as of {age} minute(s) ago. The board updates every {TradeTuning.HoloNetRefreshMinutes} minutes.";

            var names = new GuiBindingList<string>();
            var tooltips = new GuiBindingList<string>();
            var buyAt = new GuiBindingList<string>();
            var buyPrices = new GuiBindingList<string>();
            var sellAt = new GuiBindingList<string>();
            var sellPrices = new GuiBindingList<string>();
            foreach (var row in board.Rows)
            {
                names.Add(row.Legality == CommodityLegality.Legal ? row.Name : $"{row.Name} ({row.Legality})");
                tooltips.Add(TradeLookup.GetCommodity(row.Commodity)?.Description ?? string.Empty);
                buyAt.Add(TradeLookup.GetExchange(row.BuyAt)?.Name ?? string.Empty);
                buyPrices.Add(row.BuyPrice.ToString());
                sellAt.Add(TradeLookup.GetExchange(row.SellAt)?.Name ?? string.Empty);
                sellPrices.Add(row.SellPrice.ToString());
            }

            RowNames = names;
            RowTooltips = tooltips;
            RowBuyAt = buyAt;
            RowBuyPrices = buyPrices;
            RowSellAt = sellAt;
            RowSellPrices = sellPrices;

            ChangePartialView(MainContentElement, MainContentPartial);
        }
    }
}
