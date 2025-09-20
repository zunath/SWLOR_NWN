using System;
using System.Collections.Generic;
using System.Globalization;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Shared.Core.Event;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class TrainingStoreViewModel: GuiViewModelBase<TrainingStoreViewModel, GuiPayloadBase>,
        IGuiRefreshable<RPXPRefreshEvent>
    {
        [ScriptHandler(ScriptName.OnOpenTrainingStore)]
        public static void OpenTrainingStore()
        {
            var player = GetPCSpeaker();
            Gui.TogglePlayerWindow(player, GuiWindowType.TrainingStore, null, OBJECT_SELF);
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
            var dbPlayer = DB.Get<Player>(playerId);
            var cantinaBonus = Property.GetEffectiveUpgradeLevel(dbPlayer.CitizenPropertyId, PropertyUpgradeType.CantinaLevel) * 0.1f;

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
    }
}
