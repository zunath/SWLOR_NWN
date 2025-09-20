using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Service;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class KeyItemsViewModel: GuiViewModelBase<KeyItemsViewModel, GuiPayloadBase>,
        IGuiRefreshable<KeyItemReceivedRefreshEvent>
    {
        private static readonly IDatabaseService _db = ServiceContainer.GetService<IDatabaseService>();
        
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
            var dbPlayer = _db.Get<Player>(playerId);

            var names = new GuiBindingList<string>();
            var types = new GuiBindingList<string>();
            var descriptions = new GuiBindingList<string>();

            foreach (var (type, _) in dbPlayer.KeyItems)
            {
                var detail = KeyItem.GetKeyItem(type);
                var categoryDetail = KeyItem.GetKeyItemCategory(detail.Category);

                // If a key item filter is applied and this key item isn't part of this category,
                // skip it and move to the next.
                if (SelectedCategoryId != 0 && SelectedCategoryId != (int) detail.Category) 
                    continue;

                names.Add(detail.Name);
                types.Add(categoryDetail.Name);
                descriptions.Add(detail.Description);
            }

            Names = names;
            Types = types;
            Descriptions = descriptions;
        }

        public void Refresh(KeyItemReceivedRefreshEvent payload)
        {
            LoadKeyItems();
        }
    }
}
