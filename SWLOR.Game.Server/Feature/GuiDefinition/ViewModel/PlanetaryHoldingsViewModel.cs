using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.HoldingService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class PlanetaryHoldingsViewModel : GuiViewModelBase<PlanetaryHoldingsViewModel, HoldingsPayload>
    {
        public const string TabContentElement = "planetaryholdings_content";
        public const string HoldingsTabPartial = "PLANETARYHOLDINGS_HOLDINGS";
        public const string BuyTabPartial = "PLANETARYHOLDINGS_BUY";

        private const int HoldingsTabId = 0;
        private const int BuyTabId = 1;

        private static readonly GuiTabGroup<PlanetaryHoldingsViewModel, HoldingsPayload> Tabs =
            new GuiTabGroup<PlanetaryHoldingsViewModel, HoldingsPayload>()
                .AddTab(HoldingsTabId, HoldingsTabPartial)
                .AddTab(BuyTabId, BuyTabPartial);

        private static readonly GuiToggleGroupSync TabToggles = new(HoldingsTabId, BuyTabId);

        private TradeExchangeType _exchange;
        private string _dockId = string.Empty;
        private List<string> _holdingIds = new();
        private List<string> _shipIds = new();
        private List<uint> _droids = new();
        private List<CommodityType> _needs = new();
        private List<HoldingType> _buyTypes = new();
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

        public string DockName { get => Get<string>(); set => Set(value); }
        public string CreditsText { get => Get<string>(); set => Set(value); }

        public GuiBindingList<GuiComboEntry> HoldingOptions { get => Get<GuiBindingList<GuiComboEntry>>(); set => Set(value); }

        public int SelectedHoldingIndex
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_loading)
                    Refresh();
            }
        }

        public bool HasHolding { get => Get<bool>(); set => Set(value); }
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
        public string CargoText { get => Get<string>(); set => Set(value); }
        public string AwayText { get => Get<string>(); set => Set(value); }
        public bool AwayVisible { get => Get<bool>(); set => Set(value); }
        public string SummaryText { get => Get<string>(); set => Set(value); }
        public string OutputText { get => Get<string>(); set => Set(value); }
        public string StockText { get => Get<string>(); set => Set(value); }
        public string UpkeepText { get => Get<string>(); set => Set(value); }
        public string DroidText { get => Get<string>(); set => Set(value); }
        public string EventText { get => Get<string>(); set => Set(value); }
        public bool EventVisible { get => Get<bool>(); set => Set(value); }
        public bool CanCollect { get => Get<bool>(); set => Set(value); }
        public bool CanPayUpkeep { get => Get<bool>(); set => Set(value); }
        public bool CanVisit { get => Get<bool>(); set => Set(value); }
        public string FineText { get => Get<string>(); set => Set(value); }
        public bool FineVisible { get => Get<bool>(); set => Set(value); }

        public GuiBindingList<string> NeedNames { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> NeedTooltips { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> NeedProgress { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> NeedAboard { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<bool> NeedCanDeliver { get => Get<GuiBindingList<bool>>(); set => Set(value); }

        public string UpgradeText { get => Get<string>(); set => Set(value); }
        public bool CanUpgrade { get => Get<bool>(); set => Set(value); }
        public string PlantText { get => Get<string>(); set => Set(value); }
        public bool CanBuildPlant { get => Get<bool>(); set => Set(value); }
        public bool RefineEnabled { get => Get<bool>(); set => Set(value); }

        public bool RefineChecked
        {
            get => Get<bool>();
            set
            {
                Set(value);
                if (_loading)
                    return;

                var holdingId = GetSelectedHoldingId();
                if (holdingId != null)
                    PlanetaryHoldings.SetRefining(Player, holdingId, value);
                Refresh();
            }
        }

        public GuiBindingList<GuiComboEntry> DroidOptions { get => Get<GuiBindingList<GuiComboEntry>>(); set => Set(value); }
        public int SelectedDroidIndex { get => Get<int>(); set => Set(value); }
        public bool CanAssignDroid { get => Get<bool>(); set => Set(value); }
        public bool CanRecallDroid { get => Get<bool>(); set => Set(value); }

        public GuiBindingList<string> LogLines { get => Get<GuiBindingList<string>>(); set => Set(value); }

        public string OwnedText { get => Get<string>(); set => Set(value); }
        public string BuyNote { get => Get<string>(); set => Set(value); }
        public GuiBindingList<string> BuyNames { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> BuyTooltips { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> BuyOutputs { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> BuyPrices { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> BuyUpkeeps { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<bool> BuyCanBuy { get => Get<GuiBindingList<bool>>(); set => Set(value); }

        protected override void Initialize(HoldingsPayload initialPayload)
        {
            _loading = true;
            _exchange = initialPayload.Exchange;
            _dockId = initialPayload.DockId ?? string.Empty;
            DockName = $"{TradeLookup.GetExchange(_exchange)?.Name ?? "This"} Starport: Planetary Holdings";
            CreditsText = string.Empty;

            TabToggleValue = HoldingsTabId;
            SelectedTabId = HoldingsTabId;
            HoldingOptions = new GuiBindingList<GuiComboEntry>();
            SelectedHoldingIndex = 0;
            HasHolding = false;
            ShipOptions = new GuiBindingList<GuiComboEntry>();
            SelectedShipIndex = 0;
            HasShip = false;
            CargoText = string.Empty;
            AwayText = string.Empty;
            AwayVisible = false;
            SummaryText = string.Empty;
            OutputText = string.Empty;
            StockText = string.Empty;
            UpkeepText = string.Empty;
            DroidText = string.Empty;
            EventText = string.Empty;
            EventVisible = false;
            CanCollect = false;
            CanPayUpkeep = false;
            CanVisit = false;
            FineText = string.Empty;
            FineVisible = false;
            NeedNames = new GuiBindingList<string>();
            NeedTooltips = new GuiBindingList<string>();
            NeedProgress = new GuiBindingList<string>();
            NeedAboard = new GuiBindingList<string>();
            NeedCanDeliver = new GuiBindingList<bool>();
            UpgradeText = string.Empty;
            CanUpgrade = false;
            PlantText = string.Empty;
            CanBuildPlant = false;
            RefineEnabled = false;
            RefineChecked = false;
            DroidOptions = new GuiBindingList<GuiComboEntry>();
            SelectedDroidIndex = 0;
            CanAssignDroid = false;
            CanRecallDroid = false;
            LogLines = new GuiBindingList<string>();
            OwnedText = string.Empty;
            BuyNote = "Upgrades, the Processing Plant and repairs take goods hauled from other planets to the holding's dock.";
            BuyNames = new GuiBindingList<string>();
            BuyTooltips = new GuiBindingList<string>();
            BuyOutputs = new GuiBindingList<string>();
            BuyPrices = new GuiBindingList<string>();
            BuyUpkeeps = new GuiBindingList<string>();
            BuyCanBuy = new GuiBindingList<bool>();

            WatchOnClient(model => model.TabToggleValue);
            WatchOnClient(model => model.SelectedHoldingIndex);
            WatchOnClient(model => model.SelectedShipIndex);
            WatchOnClient(model => model.SelectedDroidIndex);
            WatchOnClient(model => model.RefineChecked);

            LoadHoldings(initialPayload.HoldingId);
            LoadShips();
            Refresh();
            _loading = false;

            SelectTab(HoldingsTabId);
        }

        private void SelectTab(int tabId)
        {
            SelectedTabId = tabId;
            TabToggles.SyncTo(tabId, value => TabToggleValue = value);
            Tabs.Select(this, TabContentElement, tabId);
        }

        protected override void OnModalClosedRestore() => Tabs.Select(this, TabContentElement, SelectedTabId);

        private void LoadHoldings(string preferredId)
        {
            var holdings = PlanetaryHoldings.LoadHoldings(Player);
            _holdingIds = holdings.Select(x => x.Id).ToList();

            var options = new GuiBindingList<GuiComboEntry>();
            for (var index = 0; index < holdings.Count; index++)
                options.Add(new GuiComboEntry($"{PlanetaryHoldings.GetHoldingName(holdings[index])}, level {holdings[index].Level}", index));
            if (options.Count == 0)
                options.Add(new GuiComboEntry("No holdings", 0));

            var selected = _holdingIds.IndexOf(preferredId ?? string.Empty);
            if (selected < 0)
                selected = Math.Max(0, holdings.FindIndex(x => x.DockId == _dockId));

            var loading = _loading;
            _loading = true;
            HoldingOptions = options;
            SelectedHoldingIndex = selected;
            _loading = loading;
        }

        private void LoadShips()
        {
            var ships = Trade.GetShipsDockedAt(Player, _dockId);
            _shipIds = ships.Select(x => x.Id).ToList();

            var options = new GuiBindingList<GuiComboEntry>();
            for (var index = 0; index < ships.Count; index++)
                options.Add(new GuiComboEntry(DB.Get<WorldProperty>(ships[index].PropertyId)?.CustomName ?? "Ship", index));
            if (options.Count == 0)
                options.Add(new GuiComboEntry("No ship docked here", 0));

            var loading = _loading;
            _loading = true;
            ShipOptions = options;
            SelectedShipIndex = 0;
            _loading = loading;
        }

        private string GetSelectedHoldingId() =>
            SelectedHoldingIndex >= 0 && SelectedHoldingIndex < _holdingIds.Count ? _holdingIds[SelectedHoldingIndex] : null;

        private string GetSelectedShipId() =>
            SelectedShipIndex >= 0 && SelectedShipIndex < _shipIds.Count ? _shipIds[SelectedShipIndex] : null;

        private static string Name(CommodityType commodity) => TradeLookup.GetCommodity(commodity)?.Name ?? commodity.ToString();

        private static string DescribeGoods(IEnumerable<KeyValuePair<CommodityType, int>> goods) =>
            string.Join(", ", goods.Where(x => x.Value > 0).Select(x => $"{x.Value} {Name(x.Key)}"));

        private void Refresh()
        {
            CreditsText = $"Credits: {GetGold(Player)}";
            // Credits are taken on the player's action queue, after this handler; read them again once they are.
            AssignCommand(Player, () => CreditsText = $"Credits: {GetGold(Player)}");
            HasShip = _shipIds.Count > 0;
            var ship = GetSelectedShipId() is { } shipId ? DB.Get<PlayerShip>(shipId) : null;
            CargoText = ship == null
                ? "Cargo bay: no ship of yours is docked here."
                : $"Cargo bay: {Trade.GetUsedVolume(ship)} / {Trade.GetCargoCapacity(ship)}";

            var holdingId = GetSelectedHoldingId();
            var holding = holdingId == null ? null : PlanetaryHoldings.LoadHolding(Player, holdingId);
            var detail = holding == null ? null : HoldingRules.GetDetail(holding.Type);
            HasHolding = detail != null;
            if (detail == null)
                ClearHolding();
            else
                RefreshHolding(holding, detail, ship);

            RefreshBuy();
        }

        private void ClearHolding()
        {
            AwayVisible = false;
            SummaryText = _holdingIds.Count == 0 ? "You own no holdings. Buy one on the Buy tab." : string.Empty;
            OutputText = string.Empty;
            StockText = string.Empty;
            UpkeepText = string.Empty;
            DroidText = string.Empty;
            EventVisible = false;
            CanCollect = false;
            CanPayUpkeep = false;
            CanVisit = false;
            FineVisible = false;
            _needs = new List<CommodityType>();
            NeedNames = new GuiBindingList<string>();
            NeedTooltips = new GuiBindingList<string>();
            NeedProgress = new GuiBindingList<string>();
            NeedAboard = new GuiBindingList<string>();
            NeedCanDeliver = new GuiBindingList<bool>();
            UpgradeText = string.Empty;
            CanUpgrade = false;
            PlantText = string.Empty;
            CanBuildPlant = false;
            RefineEnabled = false;
            CanAssignDroid = false;
            CanRecallDroid = false;
            LogLines = new GuiBindingList<string>();
        }

        private void RefreshHolding(PlanetaryHolding holding, HoldingDetail detail, PlayerShip ship)
        {
            var atDock = holding.DockId == _dockId;
            var gold = GetGold(Player);
            var planet = PlanetaryHoldings.GetPlanetName(detail);
            var units = HoldingRules.GetDailyUnits(holding, detail);
            var droidPercent = HoldingRules.GetDroidPercent(holding);
            var chain = detail.Chain;
            var chainText = $"{chain.OwnUnits} {Name(detail.Output)} + {chain.InputUnits} {Name(chain.Input)} make {chain.RefinedUnits} {Name(chain.Refined)}";

            AwayVisible = !atDock;
            AwayText = $"This holding is at {planet}. Collect, deliver and visit its site from that dock.";
            SummaryText = $"Level {holding.Level} {detail.Name} at {planet}. {detail.Description}";
            OutputText = $"Makes {units} {Name(detail.Output)} a day" +
                         (droidPercent > 0 ? $" (+{droidPercent}% from its droid)" : string.Empty) +
                         (holding.HasProcessingPlant && holding.RefineOutput ? $"; the plant refines up to {HoldingRules.GetPlantUnits(units)} of it: {chainText}." : ".");
            var stock = DescribeGoods(holding.Warehouse);
            StockText = $"Warehouse: {(stock.Length > 0 ? stock : "empty")} ({HoldingRules.GetStoredVolume(holding.Warehouse)} / " +
                        $"{HoldingRules.GetStorageVolume(holding, detail)} volume).";
            UpkeepText = $"Upkeep {HoldingRules.GetDailyUpkeep(holding, detail)} credits a day; {holding.UpkeepOwed} owed " +
                         $"(lost at {HoldingRules.GetForeclosureLimit(holding, detail)}).";
            DroidText = string.IsNullOrWhiteSpace(holding.DroidItemId)
                ? "No staff droid. An industrial droid's Industry raises the output."
                : $"Staff droid: {holding.DroidName}, Industry {holding.DroidIndustry}, +{droidPercent}% output.";

            if (holding.RaidPending)
            {
                EventText = $"Bandit raid: raiders hold the site. Drive them off there (Visit Site) by {holding.RaidDeadline:yyyy-MM-dd HH:mm} UTC " +
                            "or they carry off half the warehouse.";
                EventVisible = true;
            }
            else if (holding.Setback != HoldingSetbackType.Invalid)
            {
                EventText = $"{HoldingRules.GetSetbackName(holding.Setback)}: output stopped until {holding.RepairUnitsNeeded} " +
                            $"{Name(detail.RepairGood)} are delivered.";
                EventVisible = true;
            }
            else if (holding.BoomDays > 0)
            {
                EventText = "Boom: the next day's output doubles.";
                EventVisible = true;
            }
            else
            {
                EventText = string.Empty;
                EventVisible = false;
            }

            FineVisible = holding.CustomsFine > 0;
            FineText = $"Customs fine {holding.CustomsFine} credits, due {holding.CustomsDeadline:yyyy-MM-dd HH:mm} UTC.";

            CanCollect = atDock && ship != null && holding.Warehouse.Values.Sum() > 0;
            CanPayUpkeep = holding.UpkeepOwed > 0;
            CanVisit = atDock;

            RefreshNeeds(holding, detail, atDock ? ship : null);

            var upgrade = HoldingRules.GetNextUpgrade(holding, detail);
            UpgradeText = upgrade == null
                ? "The holding is at its highest level."
                : $"Level {upgrade.Level}: {upgrade.Credits} credits and {DescribeGoods(upgrade.Goods)}, delivered.";
            CanUpgrade = HoldingRules.GetUpgradeRefusal(holding, detail, gold) == HoldingRefusal.None;
            PlantText = holding.HasProcessingPlant
                ? $"Processing Plant: {chainText}."
                : $"Processing Plant: {detail.Plant.Credits} credits and {DescribeGoods(detail.Plant.Goods)}. {chainText}.";
            CanBuildPlant = HoldingRules.GetPlantRefusal(holding, detail, gold) == HoldingRefusal.None;

            var loading = _loading;
            _loading = true;
            RefineEnabled = holding.HasProcessingPlant;
            RefineChecked = holding.HasProcessingPlant && holding.RefineOutput;
            _loading = loading;

            RefreshDroids(holding);

            var lines = new GuiBindingList<string>();
            foreach (var line in Enumerable.Reverse(holding.Log ?? new List<string>()))
                lines.Add(line);
            LogLines = lines;
        }

        private void RefreshNeeds(PlanetaryHolding holding, HoldingDetail detail, PlayerShip ship)
        {
            var needs = HoldingRules.GetNeeds(holding, detail);
            _needs = needs.Select(x => x.Commodity).ToList();
            var names = new GuiBindingList<string>();
            var tooltips = new GuiBindingList<string>();
            var progress = new GuiBindingList<string>();
            var aboard = new GuiBindingList<string>();
            var canDeliver = new GuiBindingList<bool>();
            foreach (var need in needs)
            {
                var carried = ship == null ? 0 : CargoHoldRules.GetUnits(ship.Cargo, need.Commodity);
                names.Add($"{Name(need.Commodity)} ({need.Purpose})");
                tooltips.Add($"{TradeLookup.GetCommodity(need.Commodity)?.Description} Wanted for: {need.Purpose}.");
                progress.Add($"{need.Have} / {need.Wanted}");
                aboard.Add(carried.ToString());
                canDeliver.Add(ship != null && carried > 0 && need.Missing > 0);
            }

            NeedNames = names;
            NeedTooltips = tooltips;
            NeedProgress = progress;
            NeedAboard = aboard;
            NeedCanDeliver = canDeliver;
        }

        private void RefreshDroids(PlanetaryHolding holding)
        {
            var hasDroid = !string.IsNullOrWhiteSpace(holding.DroidItemId);
            _droids = hasDroid ? new List<uint>() : Space.GetPostableDroids(Player);
            var options = new GuiBindingList<GuiComboEntry>();
            for (var index = 0; index < _droids.Count; index++)
                options.Add(new GuiComboEntry(Droid.GetDroidName(_droids[index]), index));
            if (options.Count == 0)
                options.Add(new GuiComboEntry(hasDroid ? holding.DroidName : "No droid free", 0));

            DroidOptions = options;
            SelectedDroidIndex = 0;
            CanAssignDroid = !hasDroid && _droids.Count > 0;
            CanRecallDroid = hasDroid;
        }

        private void RefreshBuy()
        {
            var owned = PlanetaryHoldings.CountAccountHoldings(Player, _holdingIds.Count);
            var gold = GetGold(Player);
            OwnedText = $"Your account owns {owned} of {HoldingTuning.MaximumHoldings} holdings ({_holdingIds.Count} on this character).";

            var details = HoldingRules.GetHoldingsAt(_exchange);
            _buyTypes = details.Select(x => x.Type).ToList();
            var names = new GuiBindingList<string>();
            var tooltips = new GuiBindingList<string>();
            var outputs = new GuiBindingList<string>();
            var prices = new GuiBindingList<string>();
            var upkeeps = new GuiBindingList<string>();
            var canBuy = new GuiBindingList<bool>();
            foreach (var detail in details)
            {
                names.Add(detail.Name);
                tooltips.Add($"{detail.Description} Processing Plant: {detail.Chain.OwnUnits} {Name(detail.Output)} + " +
                             $"{detail.Chain.InputUnits} {Name(detail.Chain.Input)} make {detail.Chain.RefinedUnits} {Name(detail.Chain.Refined)}." +
                             (detail.CustomsInspections ? " Customs inspect it." : string.Empty));
                outputs.Add($"{HoldingRules.GetDailyUnits(detail, 1, 0)} {Name(detail.Output)}");
                prices.Add(detail.PurchasePrice.ToString());
                upkeeps.Add(HoldingRules.GetDailyUpkeep(detail, 1, 0).ToString());
                canBuy.Add(HoldingRules.GetPurchaseRefusal(detail, _exchange, owned, gold) == HoldingRefusal.None);
            }

            BuyNames = names;
            BuyTooltips = tooltips;
            BuyOutputs = outputs;
            BuyPrices = prices;
            BuyUpkeeps = upkeeps;
            BuyCanBuy = canBuy;
        }

        private void RunOnSelected(Action<string> action)
        {
            var holdingId = GetSelectedHoldingId();
            if (holdingId == null)
                return;

            action(holdingId);
            Refresh();
        }

        public Action OnClickCollect() => () => RunOnSelected(id => PlanetaryHoldings.Collect(Player, id, GetSelectedShipId()));

        public Action OnClickPayUpkeep() => () => RunOnSelected(id => PlanetaryHoldings.PayUpkeep(Player, id));

        public Action OnClickPayFine() => () => RunOnSelected(id => PlanetaryHoldings.PayFine(Player, id));

        public Action OnClickUpgrade() => () => RunOnSelected(id => PlanetaryHoldings.Upgrade(Player, id));

        public Action OnClickBuildPlant() => () => RunOnSelected(id => PlanetaryHoldings.BuildPlant(Player, id));

        public Action OnClickRecallDroid() => () => RunOnSelected(id => PlanetaryHoldings.RecallDroid(Player, id));

        public Action OnClickAssignDroid() => () =>
        {
            if (SelectedDroidIndex < 0 || SelectedDroidIndex >= _droids.Count)
                return;

            var controller = _droids[SelectedDroidIndex];
            RunOnSelected(id => PlanetaryHoldings.AssignDroid(Player, id, controller));
        };

        public Action OnClickDeliver() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _needs.Count)
                return;

            var commodity = _needs[index];
            RunOnSelected(id => PlanetaryHoldings.Deliver(Player, id, GetSelectedShipId(), commodity));
        };

        public Action OnClickVisit() => () =>
        {
            var holdingId = GetSelectedHoldingId();
            if (holdingId == null)
                return;

            Gui.CloseWindow(Player, GuiWindowType.PlanetaryHoldings, Player);
            PlanetaryHoldings.VisitSite(Player, holdingId, _dockId);
        };

        public Action OnClickAbandon() => () =>
        {
            var holdingId = GetSelectedHoldingId();
            var holding = holdingId == null ? null : PlanetaryHoldings.LoadHolding(Player, holdingId);
            if (holding == null)
                return;

            ShowModal($"Give up your {PlanetaryHoldings.GetHoldingName(holding)}? You get {HoldingRules.GetSellBackRefund(holding)} credits back " +
                      "and its stock and supplies are lost.", () =>
            {
                PlanetaryHoldings.Abandon(Player, holdingId);
                LoadHoldings(null);
                Refresh();
            });
        };

        public Action OnClickBuy() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _buyTypes.Count)
                return;

            var type = _buyTypes[index];
            var detail = HoldingRules.GetDetail(type);
            ShowModal($"Buy {PlanetaryHoldings.WithArticle(detail.Name)} at {PlanetaryHoldings.GetPlanetName(detail)} for {detail.PurchasePrice} credits? It owes " +
                      $"{HoldingRules.GetDailyUpkeep(detail, 1, 0)} credits of upkeep a day.", () =>
            {
                if (!PlanetaryHoldings.Buy(Player, type, _exchange, _dockId))
                    return;

                LoadHoldings(null);
                SelectedHoldingIndex = _holdingIds.Count - 1;
                Refresh();
            });
        };
    }
}
