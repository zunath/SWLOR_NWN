using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class TrainingStoreViewModel: GuiViewModelBase<TrainingStoreViewModel, GuiPayloadBase>,
        IGuiRefreshable<RPXPRefreshEvent>
    {
        [NWNEventHandler("open_train_store")]
        public static void OpenTrainingStore()
        {
            var player = GetPCSpeaker();
            Gui.TogglePlayerWindow(player, GuiWindowType.TrainingStore, null, OBJECT_SELF);
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
            var dbPlayer = DB.Get<Player>(playerId);

            AvailableXP = $"Available XP: {dbPlayer.UnallocatedXP}";

            var icons = new GuiBindingList<string>();
            var names = new GuiBindingList<string>();
            var priceTexts = new GuiBindingList<string>();
            Resrefs = new List<string>();
            Prices = new List<int>();

            // Retraining Tome (Perk Refund)
            icons.Add("iIT_BOOK_244");
            names.Add("Retraining Tome");
            Resrefs.Add("refund_tome");
            priceTexts.Add("10,000 XP");
            Prices.Add(10000);

            // Reconditioning Tome (Stat Rebuild)
            icons.Add("iIT_BOOK_246");
            names.Add("Reconditioning Tome");
            Resrefs.Add("recond_tome");
            priceTexts.Add("300,000 XP");
            Prices.Add(300000);

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
