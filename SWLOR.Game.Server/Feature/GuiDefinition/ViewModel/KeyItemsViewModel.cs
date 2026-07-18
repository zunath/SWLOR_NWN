using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Component;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class KeyItemsViewModel: GuiViewModelBase<KeyItemsViewModel, GuiPayloadBase>,
        IGuiRefreshable<KeyItemReceivedRefreshEvent>
    {
        // Row DTO replacing the three hand-synced GuiBindingList instances
        // LoadKeyItems used to build in lockstep.
        private sealed class KeyItemEntry
        {
            public string Name { get; }
            public string Type { get; }
            public string Description { get; }

            public KeyItemEntry(string name, string type, string description)
            {
                Name = name;
                Type = type;
                Description = description;
            }
        }

        private static readonly GuiTableSource<KeyItemsViewModel, KeyItemEntry> KeyItemsTable =
            new GuiTableSource<KeyItemsViewModel, KeyItemEntry>()
                .Column((m, v) => m.Names = v, r => r.Name)
                .Column((m, v) => m.Types = v, r => r.Type)
                .Column((m, v) => m.Descriptions = v, r => r.Description);

        public GuiBindingList<string> Names
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> Types
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> Descriptions
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public int SelectedCategoryId
        {
            get => Get<int>();
            set
            {
                Set(value);
                LoadKeyItems();
            }
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            SelectedCategoryId = 0;
            LoadKeyItems();
            WatchOnClient(model => model.SelectedCategoryId);
        }

        private void LoadKeyItems()
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);

            var rows = new List<KeyItemEntry>();

            foreach (var (type, _) in dbPlayer.KeyItems)
            {
                var detail = KeyItem.GetKeyItem(type);
                var categoryDetail = KeyItem.GetKeyItemCategory(detail.Category);

                // If a key item filter is applied and this key item isn't part of this category,
                // skip it and move to the next.
                if (SelectedCategoryId != 0 && SelectedCategoryId != (int) detail.Category)
                    continue;

                rows.Add(new KeyItemEntry(detail.Name, categoryDetail.Name, detail.Description));
            }

            KeyItemsTable.Refresh(this, rows);
        }

        public void Refresh(KeyItemReceivedRefreshEvent payload)
        {
            LoadKeyItems();
        }
    }
}
