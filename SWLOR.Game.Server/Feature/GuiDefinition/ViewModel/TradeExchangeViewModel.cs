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
        public const string TabContentElement = "tradeexchange_content";
        public const string MarketTabPartial = "TRADEEXCHANGE_MARKET";
        public const string FreightTabPartial = "TRADEEXCHANGE_FREIGHT";

        private const int MarketTabId = 0;
        private const int FreightTabId = 1;
        private const int DefaultUnits = 10;

        private static readonly GuiTabGroup<TradeExchangeViewModel, TradeExchangePayload> Tabs =
            new GuiTabGroup<TradeExchangeViewModel, TradeExchangePayload>()
                .AddTab(MarketTabId, MarketTabPartial)
                .AddTab(FreightTabId, FreightTabPartial);

        private static readonly GuiToggleGroupSync TabToggles = new(MarketTabId, FreightTabId);

        private TradeExchangeType _exchange;
        private string _landingAreaResref = string.Empty;
        private List<string> _shipIds = new();
        private List<CommodityType> _rows = new();
        private List<string> _offerIds = new();
        private List<string> _contractIds = new();
        private bool _loading;

        public int SelectedTabId { get => Get<int>(); set => Set(value); }

        public int TabToggleValue
        {
            get => Get<int>();
            set
            {
                Set(value);
                TabToggles.HandleClientChange(value, SelectTab);
            }
        }

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

        public string OffersTitle { get => Get<string>(); set => Set(value); }
        public GuiBindingList<string> OfferRoutes { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> OfferTooltips { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> OfferVolumes { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> OfferFees { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> OfferDeposits { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> OfferMinutes { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<bool> OfferCanAccept { get => Get<GuiBindingList<bool>>(); set => Set(value); }

        public string ContractsTitle { get => Get<string>(); set => Set(value); }
        public GuiBindingList<string> ContractRoutes { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> ContractTooltips { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> ContractDue { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<GuiColor> ContractDueColors { get => Get<GuiBindingList<GuiColor>>(); set => Set(value); }
        public GuiBindingList<bool> ContractCanDeliver { get => Get<GuiBindingList<bool>>(); set => Set(value); }

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

            TabToggleValue = MarketTabId;
            SelectedTabId = MarketTabId;
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
            OffersTitle = string.Empty;
            OfferRoutes = new GuiBindingList<string>();
            OfferTooltips = new GuiBindingList<string>();
            OfferVolumes = new GuiBindingList<string>();
            OfferFees = new GuiBindingList<string>();
            OfferDeposits = new GuiBindingList<string>();
            OfferMinutes = new GuiBindingList<string>();
            OfferCanAccept = new GuiBindingList<bool>();
            ContractsTitle = string.Empty;
            ContractRoutes = new GuiBindingList<string>();
            ContractTooltips = new GuiBindingList<string>();
            ContractDue = new GuiBindingList<string>();
            ContractDueColors = new GuiBindingList<GuiColor>();
            ContractCanDeliver = new GuiBindingList<bool>();

            WatchOnClient(model => model.TabToggleValue);
            WatchOnClient(model => model.SelectedShipIndex);
            WatchOnClient(model => model.UnitsText);

            LoadShips();
            Refresh();
            _loading = false;

            SelectTab(MarketTabId);
        }

        private void SelectTab(int tabId)
        {
            SelectedTabId = tabId;
            TabToggles.SyncTo(tabId, value => TabToggleValue = value);
            var units = UnitsText;
            Tabs.Select(this, TabContentElement, tabId, () => UnitsText = units);
        }

        protected override void OnModalClosedRestore() => Tabs.Select(this, TabContentElement, SelectedTabId);

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
            CargoText = hasShip ? $"Cargo bay: {Trade.GetUsedVolume(ship)} / {capacity}" : "Cargo bay: -";
            CreditsText = $"Credits: {GetGold(Player)}";

            RefreshMarket(hasShip, cargo, capacity);
            RefreshFreight(hasShip ? ship : null);
        }

        private void RefreshMarket(bool hasShip, Dictionary<CommodityType, CargoLot> cargo, int capacity)
        {
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
                roles.Add(GetRoleText(market.Role, commodity.Legality, exchange.IsBlackMarket));
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

        private void RefreshFreight(PlayerShip ship)
        {
            var now = DateTime.UtcNow;
            var contracts = Trade.GetFreightContracts(Player);
            var taken = DB.Get<Player>(GetObjectUUID(Player))?.FreightOffersTaken ?? new List<string>();
            var offers = FreightContractRules.GetOffers(_exchange, now);
            var freeVolume = ship == null ? 0 : Trade.GetFreeVolume(ship);
            var capacity = ship == null ? 0 : Trade.GetCargoCapacity(ship);
            var credits = GetGold(Player);

            OffersTitle = $"Freight offered here (new offers at {FreightContractRules.GetNextOfferChange(now):HH:mm} UTC)";
            _offerIds = offers.Select(x => x.Id).ToList();
            var routes = new GuiBindingList<string>();
            var tooltips = new GuiBindingList<string>();
            var volumes = new GuiBindingList<string>();
            var fees = new GuiBindingList<string>();
            var deposits = new GuiBindingList<string>();
            var minutes = new GuiBindingList<string>();
            var canAccept = new GuiBindingList<bool>();
            foreach (var offer in offers)
            {
                var refusal = FreightContractRules.CanAccept(offer, contracts, taken, capacity, freeVolume, credits);
                routes.Add($"{TradeLookup.GetExchange(offer.Destination)?.Name} ({offer.Hops} jumps)");
                tooltips.Add(refusal == FreightRefusal.None
                    ? "The load is sealed: it fills cargo volume, cannot be sold and is not scanned. Deliver late and the deposit is forfeit."
                    : FreightContractRules.GetRefusalMessage(refusal));
                volumes.Add(offer.Volume.ToString());
                fees.Add(offer.Fee.ToString());
                deposits.Add(offer.Deposit.ToString());
                minutes.Add(offer.DeadlineMinutes.ToString());
                canAccept.Add(ship != null && refusal == FreightRefusal.None);
            }

            OfferRoutes = routes;
            OfferTooltips = tooltips;
            OfferVolumes = volumes;
            OfferFees = fees;
            OfferDeposits = deposits;
            OfferMinutes = minutes;
            OfferCanAccept = canAccept;

            ContractsTitle = $"Your freight contracts ({contracts.Count} of {TradeTuning.FreightMaximumActive})";
            _contractIds = contracts.Select(x => x.Id).ToList();
            var contractRoutes = new GuiBindingList<string>();
            var contractTooltips = new GuiBindingList<string>();
            var due = new GuiBindingList<string>();
            var dueColors = new GuiBindingList<GuiColor>();
            var canDeliver = new GuiBindingList<bool>();
            foreach (var contract in contracts)
            {
                var late = now > contract.Deadline;
                var left = (int)Math.Ceiling((contract.Deadline - now).TotalMinutes);
                var shipName = DB.Get<WorldProperty>(DB.Get<PlayerShip>(contract.ShipId)?.PropertyId ?? string.Empty)?.CustomName ?? "a ship";
                contractRoutes.Add($"{TradeLookup.GetExchange(contract.Origin)?.Name} to {TradeLookup.GetExchange(contract.Destination)?.Name}, {contract.Volume} vol");
                contractTooltips.Add($"Aboard {shipName}. Fee {contract.Fee} credits, deposit {contract.Deposit} credits.");
                due.Add(late ? "Late: deposit lost" : $"Due in {left} min");
                dueColors.Add(late ? GuiColor.Red : GuiColor.Green);
                canDeliver.Add(ship != null && contract.Destination == _exchange &&
                               ship.SealedFreight.Any(x => x.ContractId == contract.Id));
            }

            ContractRoutes = contractRoutes;
            ContractTooltips = contractTooltips;
            ContractDue = due;
            ContractDueColors = dueColors;
            ContractCanDeliver = canDeliver;
        }

        private static string GetRoleText(CommodityRole role, CommodityLegality legality, bool blackMarket)
        {
            var text = role switch
            {
                CommodityRole.Producer => "Export",
                CommodityRole.Consumer => "Import",
                _ => "Traded",
            };

            if (legality == CommodityLegality.Legal)
                return text;

            return blackMarket && role != CommodityRole.Producer ? $"Black market, {legality}" : $"{text}, {legality}";
        }

        private int GetOrderUnits()
        {
            return int.TryParse(UnitsText?.Trim(), out var units) ? units : 0;
        }

        private string GetSelectedShipId()
        {
            return SelectedShipIndex >= 0 && SelectedShipIndex < _shipIds.Count ? _shipIds[SelectedShipIndex] : null;
        }

        private void PlaceOrder(bool buying)
        {
            var index = NuiGetEventArrayIndex();
            var shipId = GetSelectedShipId();
            if (index < 0 || index >= _rows.Count || shipId == null)
                return;

            if (buying)
                Trade.Buy(Player, shipId, _exchange, _landingAreaResref, _rows[index], GetOrderUnits());
            else
                Trade.Sell(Player, shipId, _exchange, _landingAreaResref, _rows[index], GetOrderUnits());

            Refresh();
        }

        public Action OnClickBuy() => () => PlaceOrder(true);

        public Action OnClickSell() => () => PlaceOrder(false);

        public Action OnClickAcceptOffer() => () =>
        {
            var index = NuiGetEventArrayIndex();
            var shipId = GetSelectedShipId();
            if (index < 0 || index >= _offerIds.Count || shipId == null)
                return;

            Trade.AcceptFreight(Player, shipId, _exchange, _landingAreaResref, _offerIds[index]);
            Refresh();
        };

        public Action OnClickDeliver() => () =>
        {
            var index = NuiGetEventArrayIndex();
            var shipId = GetSelectedShipId();
            if (index < 0 || index >= _contractIds.Count || shipId == null)
                return;

            Trade.DeliverFreight(Player, shipId, _exchange, _landingAreaResref, _contractIds[index]);
            Refresh();
        };

        public Action OnClickAbandon() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _contractIds.Count)
                return;

            var contractId = _contractIds[index];
            ShowModal("Abandon this contract? The sealed load is unloaded and the deposit is forfeit.", () =>
            {
                Trade.AbandonFreight(Player, contractId);
                Refresh();
            });
        };

        public Action OnClickHoloNet() => () =>
        {
            if (!Gui.IsWindowOpen(Player, GuiWindowType.HoloNetBoard))
                Gui.TogglePlayerWindow(Player, GuiWindowType.HoloNetBoard);
        };
    }
}
