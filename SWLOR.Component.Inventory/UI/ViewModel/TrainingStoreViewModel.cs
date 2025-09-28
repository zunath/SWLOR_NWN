using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.Caching.Contracts;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Properties.Contracts;
using SWLOR.Shared.Domain.Properties.Enums;
using SWLOR.Shared.Domain.UI.Events;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.Events.Inventory;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Model;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Inventory.UI.ViewModel
{
    public class TrainingStoreViewModel: GuiViewModelBase<TrainingStoreViewModel, IGuiPayload>,
        IGuiRefreshable<RPXPRefreshEvent>
    {
        private readonly IDatabaseService _db;
        private readonly IItemCacheService _itemCache;
        private readonly IServiceProvider _serviceProvider;

        public TrainingStoreViewModel(
            IGuiService guiService, 
            IDatabaseService db, 
            IItemCacheService itemCache, 
            IServiceProvider serviceProvider) 
            : base(guiService)
        {
            _db = db;
            _itemCache = itemCache;
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded service to break circular dependency
        private IPropertyService PropertyService => _serviceProvider.GetRequiredService<IPropertyService>();
        
        [ScriptHandler<OnOpenTrainingStore>]
        public void OpenTrainingStore()
        {
            var player = GetPCSpeaker();
            _guiService.TogglePlayerWindow(player, GuiWindowType.TrainingStore, null, OBJECT_SELF);
        }

        private class TerminalItem
        {
            public string Icon { get; set; }
            public string Resref { get; set; }
            public int BasePrice { get; set; }
            public string Name { get; set; }

            public TerminalItem(IItemCacheService itemCache, string icon, string resref, int basePrice)
            {
                Icon = icon;
                Resref = resref;
                BasePrice = basePrice;

                Name = itemCache.GetItemNameByResref(resref);
            }
        }

        private List<TerminalItem> GetAvailableItems()
        {
            return new List<TerminalItem>
            {
                new(_itemCache, "iIT_BOOK_244", "refund_tome", 10000),
                new(_itemCache, "iIT_BOOK_246", "recond_tome", 300000),
            };
        }

        public string AvailableXP
        {
            get => Get<string>();
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
            var playerId = GetObjectUUID(Player);
            var dbPlayer = _db.Get<Player>(playerId);
            var cantinaBonus = PropertyService.GetEffectiveUpgradeLevel(dbPlayer.CitizenPropertyId, PropertyUpgradeType.CantinaLevel) * 0.1f;

            AvailableXP = $"Available XP: {dbPlayer.UnallocatedXP}";

            var icons = new GuiBindingList<string>();
            var names = new GuiBindingList<string>();
            var priceTexts = new GuiBindingList<string>();
            Resrefs = new List<string>();
            Prices = new List<int>();

            foreach (var item in GetAvailableItems())
            {
                icons.Add(item.Icon);
                names.Add(item.Name);
                Resrefs.Add(item.Resref);

                var adjustedPrice = (int)(item.BasePrice - item.BasePrice * cantinaBonus);
                Prices.Add(adjustedPrice);
                priceTexts.Add($"{adjustedPrice.ToString("N0", CultureInfo.InvariantCulture)} XP");
            }

            Icons = icons;
            Names = names;
            PriceTexts = priceTexts;
        }

        protected override void Initialize(IGuiPayload initialPayload)
        {
            LoadData();
        }

        public void Refresh(RPXPRefreshEvent payload)
        {
            LoadData();
        }

        public Action BuyItem() => () =>
        {
            var index = NuiGetEventArrayIndex();

            ShowModal($"Are you sure you want to buy the {Names[index]} for {PriceTexts[index]} XP?", () =>
            {
                var playerId = GetObjectUUID(Player);
                var dbPlayer = _db.Get<Player>(playerId);

                if (dbPlayer.UnallocatedXP < Prices[index])
                {
                    FloatingTextStringOnCreature($"Not enough XP to make that purchase!", Player, false);
                    return;
                }

                dbPlayer.UnallocatedXP -= Prices[index];
                _db.Set(dbPlayer);

                CreateItemOnObject(Resrefs[index], Player);
                _guiService.PublishRefreshEvent(Player, new RPXPRefreshEvent());
            });
        };
    }
}
