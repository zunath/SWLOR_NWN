using SWLOR.Component.Inventory.Contracts;
using SWLOR.Component.Inventory.UI.RefreshEvent;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Infrastructure;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Model;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Inventory.UI.ViewModel
{
    public class KeyItemsViewModel: GuiViewModelBase<KeyItemsViewModel, GuiPayloadBase>,
        IGuiRefreshable<KeyItemReceivedRefreshEvent>
    {
        private readonly IKeyItemService _keyItemService;
        private static readonly IDatabaseService _db = ServiceContainer.GetService<IDatabaseService>();

        public KeyItemsViewModel(IGuiService guiService, IKeyItemService keyItemService) : base(guiService)
        {
            _keyItemService = keyItemService;
        }
        
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
                var detail = _keyItemService.GetKeyItem(type);
                var categoryDetail = _keyItemService.GetKeyItemCategory(detail.Category);

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
