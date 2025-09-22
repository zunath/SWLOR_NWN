using System;
using System.Collections.Generic;
using System.Globalization;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Data.Entity;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Models;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Model;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class TrainingStoreViewModel: GuiViewModelBase<TrainingStoreViewModel, GuiPayloadBase>,
        IGuiRefreshable<RPXPRefreshEvent>
    {
        private readonly IDatabaseService _db;
        private readonly IItemCacheService _itemCache;
        private readonly Property _property;

        public TrainingStoreViewModel(IGuiService guiService, IDatabaseService db, IItemCacheService itemCache, Property property) : base(guiService)
        {
            _db = db;
            _itemCache = itemCache;
            _property = property;
        }
        
        [ScriptHandler(ScriptName.OnOpenTrainingStore)]
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

            public TerminalItem(string icon, string resref, int basePrice)
            {
                Icon = icon;
                Resref = resref;
                BasePrice = basePrice;

                Name = _itemCache.GetItemNameByResref(resref);
            }
        }

        private static readonly List<TerminalItem> _availableItems = new()
        {
            new("iIT_BOOK_244", "refund_tome", 10000),
            new("iIT_BOOK_246", "recond_tome", 300000),
        };

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
            var cantinaBonus = _property.GetEffectiveUpgradeLevel(dbPlayer.CitizenPropertyId, PropertyUpgradeType.CantinaLevel) * 0.1f;

            AvailableXP = $"Available XP: {dbPlayer.UnallocatedXP}";

            var icons = new GuiBindingList<string>();
            var names = new GuiBindingList<string>();
            var priceTexts = new GuiBindingList<string>();
            Resrefs = new List<string>();
            Prices = new List<int>();

            foreach (var item in _availableItems)
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

        protected override void Initialize(GuiPayloadBase initialPayload)
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
