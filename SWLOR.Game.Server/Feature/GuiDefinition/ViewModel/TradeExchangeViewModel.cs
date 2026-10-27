using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.TradeService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class TradeExchangeViewModel : GuiViewModelBase<TradeExchangeViewModel, TradeExchangePayload>
    {
        public const string MainContentElement = "tradeexchange_content";
        public const string MainContentPartial = "TRADEEXCHANGE_MAIN";

        private const int DefaultUnits = 10;

        private TradeExchangeType _exchange;
        private string _landingAreaResref = string.Empty;
        private List<string> _shipIds = new();
        private List<CommodityType> _rows = new();
        private bool _loading;

        public string ExchangeName { get => Get<string>(); set => Set(value); }
        public GuiBindingList<GuiComboEntry> ShipOptions { get => Get<GuiBindingList<GuiComboEntry>>(); set => Set(value); }

        public int SelectedShipIndex
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_loading)
                    Refresh();
            }
        }

        public bool HasShip { get => Get<bool>(); set => Set(value); }
        public bool NoShipVisible { get => Get<bool>(); set => Set(value); }
        public string CargoText { get => Get<string>(); set => Set(value); }
        public string CreditsText { get => Get<string>(); set => Set(value); }
        public string UnitsText { get => Get<string>(); set => Set(value); }
        public string OtherCargoText { get => Get<string>(); set => Set(value); }
        public bool OtherCargoVisible { get => Get<bool>(); set => Set(value); }

        public GuiBindingList<string> RowNames { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> RowTooltips { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> RowRoles { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> RowBuyPrices { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> RowSellPrices { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> RowAboard { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<bool> RowCanBuy { get => Get<GuiBindingList<bool>>(); set => Set(value); }
        public GuiBindingList<bool> RowCanSell { get => Get<GuiBindingList<bool>>(); set => Set(value); }

        protected override void Initialize(TradeExchangePayload initialPayload)
        {
            _loading = true;
            _exchange = initialPayload.Exchange;
            _landingAreaResref = initialPayload.LandingAreaResref ?? string.Empty;

            var exchange = TradeLookup.GetExchange(_exchange);
            ExchangeName = exchange == null ? "Trade Exchange" : $"{exchange.Name} Trade Exchange";
            _rows = exchange == null
                ? new List<CommodityType>()
                : exchange.Markets
                    .OrderBy(x => x.Role == CommodityRole.Producer ? 0 : x.Role == CommodityRole.Consumer ? 1 : 2)
                    .ThenBy(x => TradeLookup.GetCommodity(x.Commodity)?.Name)
                    .Select(x => x.Commodity)
                    .ToList();

            ShipOptions = new GuiBindingList<GuiComboEntry>();
            SelectedShipIndex = 0;
            HasShip = false;
            NoShipVisible = true;
            CargoText = string.Empty;
            CreditsText = string.Empty;
            UnitsText = DefaultUnits.ToString();
            OtherCargoText = string.Empty;
            OtherCargoVisible = false;
            RowNames = new GuiBindingList<string>();
            RowTooltips = new GuiBindingList<string>();
            RowRoles = new GuiBindingList<string>();
            RowBuyPrices = new GuiBindingList<string>();
            RowSellPrices = new GuiBindingList<string>();
            RowAboard = new GuiBindingList<string>();
            RowCanBuy = new GuiBindingList<bool>();
            RowCanSell = new GuiBindingList<bool>();

            WatchOnClient(model => model.SelectedShipIndex);
            WatchOnClient(model => model.UnitsText);

            LoadShips();
            Refresh();
            _loading = false;

            ChangePartialView(MainContentElement, MainContentPartial);
        }

        private void LoadShips()
        {
            var ships = Trade.GetShipsDockedAt(Player, _landingAreaResref);
            _shipIds = ships.Select(x => x.Id).ToList();

            var options = new GuiBindingList<GuiComboEntry>();
            for (var index = 0; index < ships.Count; index++)
            {
                var name = DB.Get<WorldProperty>(ships[index].PropertyId)?.CustomName ?? "Ship";
                options.Add(new GuiComboEntry(name, index));
            }

            if (options.Count == 0)
                options.Add(new GuiComboEntry("No ship docked here", 0));

            ShipOptions = options;
            SelectedShipIndex = 0;
        }

        private PlayerShip GetSelectedShip()
        {
            if (SelectedShipIndex < 0 || SelectedShipIndex >= _shipIds.Count)
                return null;

            return DB.Get<PlayerShip>(_shipIds[SelectedShipIndex]);
        }

        private void Refresh()
        {
            var ship = GetSelectedShip();
            var hasShip = ship != null && Trade.IsDockedAt(ship, _landingAreaResref);
            HasShip = _shipIds.Count > 0;
            NoShipVisible = !hasShip;

            var cargo = hasShip ? ship.Cargo : new Dictionary<CommodityType, CargoLot>();
            var capacity = hasShip ? Trade.GetCargoCapacity(ship) : 0;
            CargoText = hasShip ? $"Cargo bay: {CargoHoldRules.GetUsedVolume(cargo)} / {capacity}" : "Cargo bay: -";
            CreditsText = $"Credits: {GetGold(Player)}";

            var exchange = TradeLookup.GetExchange(_exchange);
            var state = Trade.GetExchangeState(_exchange, DateTime.UtcNow);
            var social = GetAbilityScore(Player, AbilityType.Social);

            var names = new GuiBindingList<string>();
            var tooltips = new GuiBindingList<string>();
            var roles = new GuiBindingList<string>();
            var buyPrices = new GuiBindingList<string>();
            var sellPrices = new GuiBindingList<string>();
            var aboard = new GuiBindingList<string>();
            var canBuy = new GuiBindingList<bool>();
            var canSell = new GuiBindingList<bool>();

            foreach (var type in _rows)
            {
                var commodity = TradeLookup.GetCommodity(type);
                var market = exchange?.FindMarket(type);
                if (commodity == null || market == null)
                    continue;

                var pressure = Trade.GetPressure(state, type);
                var units = CargoHoldRules.GetUnits(cargo, type);
                names.Add(commodity.Name);
                tooltips.Add($"{commodity.Description} Volume {commodity.Volume} per unit.");
                roles.Add(GetRoleText(market.Role, commodity.Legality));
                buyPrices.Add(TradePricing.GetQuotedBuyPrice(commodity.BasePrice, market, pressure, social).ToString());
                sellPrices.Add(TradePricing.GetQuotedSellPrice(commodity.BasePrice, market, pressure, social).ToString());
                aboard.Add(units.ToString());
                canBuy.Add(hasShip && capacity > 0);
                canSell.Add(hasShip && units > 0);
            }

            RowNames = names;
            RowTooltips = tooltips;
            RowRoles = roles;
            RowBuyPrices = buyPrices;
            RowSellPrices = sellPrices;
            RowAboard = aboard;
            RowCanBuy = canBuy;
            RowCanSell = canSell;

            var elsewhere = cargo
                .Where(x => x.Value.Units > 0 && !_rows.Contains(x.Key))
                .Select(x => $"{x.Value.Units} {TradeLookup.GetCommodity(x.Key)?.Name}")
                .ToList();
            OtherCargoVisible = elsewhere.Count > 0;
            OtherCargoText = elsewhere.Count > 0 ? $"Aboard and not traded here: {string.Join(", ", elsewhere)}." : string.Empty;
        }

        private static string GetRoleText(CommodityRole role, CommodityLegality legality)
        {
            var text = role switch
            {
                CommodityRole.Producer => "Export",
                CommodityRole.Consumer => "Import",
                _ => "Traded",
            };

            return legality == CommodityLegality.Legal ? text : $"{text}, {legality}";
        }

        private int GetOrderUnits()
        {
            return int.TryParse(UnitsText?.Trim(), out var units) ? units : 0;
        }

        private void PlaceOrder(bool buying)
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _rows.Count || SelectedShipIndex < 0 || SelectedShipIndex >= _shipIds.Count)
                return;

            var shipId = _shipIds[SelectedShipIndex];
            if (buying)
                Trade.Buy(Player, shipId, _exchange, _landingAreaResref, _rows[index], GetOrderUnits());
            else
                Trade.Sell(Player, shipId, _exchange, _landingAreaResref, _rows[index], GetOrderUnits());

            Refresh();
        }

        public Action OnClickBuy() => () => PlaceOrder(true);

        public Action OnClickSell() => () => PlaceOrder(false);
    }
}
