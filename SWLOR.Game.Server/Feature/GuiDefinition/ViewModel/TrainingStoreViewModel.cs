using System.Collections.Generic;
using System.Globalization;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CivicExchangeService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>
    /// The Training Store, and in its city mode (opened with a <see cref="CivicExchangePayload"/>) the Civic Exchange of a city's
    /// Cantina (Player City Revamp): the same window, Available XP and Buy buttons, with the Exchange's stock and prices instead.
    /// </summary>
    public class TrainingStoreViewModel: GuiViewModelBase<TrainingStoreViewModel, GuiPayloadBase>,
        IGuiRefreshable<RPXPRefreshEvent>
    {
        private const string TrainingStoreTitle = "Training Store";

        /// <summary>The city whose Civic Exchange the window shows, or empty for the Training Store.</summary>
        private string _civicExchangeCityId = string.Empty;

        private bool IsCivicExchange => !string.IsNullOrWhiteSpace(_civicExchangeCityId);

        [NWNEventHandler(ScriptName.OnOpenTrainingStore)]
        public static void OpenTrainingStore()
        {
            OpenTrainingStore(GetPCSpeaker(), OBJECT_SELF);
        }

        public static bool OpenTrainingStore(uint player, uint terminal)
        {
            if (!GetIsPC(player))
                return false;

            Gui.TogglePlayerWindow(player, GuiWindowType.TrainingStore, null, terminal);
            return true;
        }

        private class TerminalItem
        {
            public string Icon { get; set; }
            public string Resref { get; set; }
            public int BasePrice { get; set; }
            public string Name { get; set; }

            public TerminalItem(string icon, string resref, int basePrice)
            {
                Icon = icon;
                Resref = resref;
                BasePrice = basePrice;

                Name = Cache.GetItemNameByResref(resref);
            }
        }

        private static readonly List<TerminalItem> _availableItems = new()
        {
            new("iIT_BOOK_244", "refund_tome", 10000),
            new("iIT_BOOK_246", "recond_tome", 300000),
        };

        public string WindowTitle
        {
            get => Get<string>();
            set => Set(value);
        }

        public string AvailableXP
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiBindingList<string> Tooltips
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> Icons
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> Names
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public List<string> Resrefs { get; set; }

        public List<int> Prices { get; set; }

        public GuiBindingList<string> PriceTexts
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        private void LoadData()
        {
            if (IsCivicExchange)
            {
                LoadCivicExchange();
                return;
            }

            WindowTitle = TrainingStoreTitle;
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            var cantinaBonus = Property.GetEffectiveUpgradeLevel(dbPlayer.CitizenPropertyId, PropertyUpgradeType.CantinaLevel) * 0.1f;

            AvailableXP = $"Available XP: {dbPlayer.UnallocatedXP}";

            var icons = new GuiBindingList<string>();
            var names = new GuiBindingList<string>();
            var tooltips = new GuiBindingList<string>();
            var priceTexts = new GuiBindingList<string>();
            Resrefs = new List<string>();
            Prices = new List<int>();

            foreach (var item in _availableItems)
            {
                icons.Add(item.Icon);
                names.Add(item.Name);
                tooltips.Add(string.Empty);
                Resrefs.Add(item.Resref);

                var adjustedPrice = (int)(item.BasePrice - item.BasePrice * cantinaBonus);
                Prices.Add(adjustedPrice);
                priceTexts.Add($"{adjustedPrice.ToString("N0", CultureInfo.InvariantCulture)} XP");
            }

            Icons = icons;
            Names = names;
            Tooltips = tooltips;
            PriceTexts = priceTexts;
        }

        /// <summary>
        /// Lists what the city's Cantina stocks at its level, with each item's description as a tooltip. The window shows the same
        /// Available XP the Training Store does, with the week's crates left and the Cantina level beside it.
        /// </summary>
        private void LoadCivicExchange()
        {
            var dbPlayer = DB.Get<Player>(GetObjectUUID(Player));
            var city = DB.Get<WorldProperty>(_civicExchangeCityId);
            var cantinaLevel = CivicExchange.GetCantinaLevel(_civicExchangeCityId);
            var cratesRemaining = CivicExchange.GetCratesRemaining(Player);

            WindowTitle = $"Civic Exchange - {city?.CustomName ?? "City"}";
            AvailableXP = $"Available XP: {dbPlayer.UnallocatedXP.ToString("N0", CultureInfo.InvariantCulture)}   |   " +
                          $"Crates left this week: {cratesRemaining} of {CivicExchangeCatalog.WeeklyCrateLimit}   |   Cantina level {cantinaLevel}";

            var icons = new GuiBindingList<string>();
            var names = new GuiBindingList<string>();
            var tooltips = new GuiBindingList<string>();
            var priceTexts = new GuiBindingList<string>();
            Resrefs = new List<string>();
            Prices = new List<int>();

            foreach (var item in CivicExchangeCatalog.GetStock(cantinaLevel))
            {
                icons.Add(item.Icon);
                names.Add(item.Name);
                tooltips.Add(item.Description);
                priceTexts.Add($"{item.PriceXp.ToString("N0", CultureInfo.InvariantCulture)} XP");
                Resrefs.Add(item.Key);
                Prices.Add(item.PriceXp);
            }

            Icons = icons;
            Names = names;
            Tooltips = tooltips;
            PriceTexts = priceTexts;
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            // The view model is kept per player, so the mode is set again every time the window opens.
            _civicExchangeCityId = initialPayload is CivicExchangePayload exchange ? exchange.CityId : string.Empty;
            LoadData();
        }

        public void Refresh(RPXPRefreshEvent payload)
        {
            LoadData();
        }

        public Action BuyItem() => () =>
        {
            var index = NuiGetEventArrayIndex();

            if (IsCivicExchange)
            {
                BuyCivicExchangeItem(index);
                return;
            }

            ShowModal($"Are you sure you want to buy the {Names[index]} for {PriceTexts[index]} XP?", () =>
            {
                var playerId = GetObjectUUID(Player);
                var dbPlayer = DB.Get<Player>(playerId);

                if (dbPlayer.UnallocatedXP < Prices[index])
                {
                    FloatingTextStringOnCreature($"Not enough XP to make that purchase!", Player, false);
                    return;
                }

                dbPlayer.UnallocatedXP -= Prices[index];
                DB.Set(dbPlayer);

                CreateItemOnObject(Resrefs[index], Player);
                Gui.PublishRefreshEvent(Player, new RPXPRefreshEvent());
            });
        };

        /// <summary>
        /// Asks the citizen to confirm a Civic Exchange purchase, naming the food buff it replaces, then buys it. The service checks
        /// everything again, since the window may have been open a while.
        /// </summary>
        private void BuyCivicExchangeItem(int index)
        {
            var item = CivicExchangeCatalog.Get(Resrefs[index]);
            if (item == null)
                return;

            var cityId = _civicExchangeCityId;
            var prompt = $"Buy the {item.Name} for {item.PriceXp.ToString("N0", CultureInfo.InvariantCulture)} Available XP?";
            if (item.Kind == CivicExchangeItemKind.Buff && StatusEffect.HasStatusEffect<FoodStatusEffect>(Player))
                prompt += " It replaces the food buff you are under.";

            ShowModal(prompt, () =>
            {
                var failure = CivicExchange.TryBuy(Player, cityId, item.Key);
                if (!string.IsNullOrEmpty(failure))
                    FloatingTextStringOnCreature(failure, Player, false);

                Gui.PublishRefreshEvent(Player, new RPXPRefreshEvent());
            });
        }
    }
}
