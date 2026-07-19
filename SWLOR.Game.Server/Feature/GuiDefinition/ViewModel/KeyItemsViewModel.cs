using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Service.LogService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class KeyItemsViewModel: GuiViewModelBase<KeyItemsViewModel, GuiPayloadBase>,
        IGuiRefreshable<KeyItemReceivedRefreshEvent>
    {
        private readonly List<KeyItemType> _visibleKeyItems = new();
        private int _selectedIndex = -1;

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

        public GuiBindingList<bool> Selections
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public string SelectedIcon
        {
            get => Get<string>();
            set => Set(value);
        }

        public string SelectedName
        {
            get => Get<string>();
            set => Set(value);
        }

        public string SelectedType
        {
            get => Get<string>();
            set => Set(value);
        }

        public string SelectedDescription
        {
            get => Get<string>();
            set => Set(value);
        }

        public int SelectedCategoryId
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (Player != 0 && WindowToken > 0)
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
            LoadKeyItems(dbPlayer.KeyItems.Keys);
        }

        public void LoadKeyItems(IEnumerable<KeyItemType> keyItems)
        {
            var previouslySelectedType = _selectedIndex >= 0 && _selectedIndex < _visibleKeyItems.Count
                ? _visibleKeyItems[_selectedIndex]
                : KeyItemType.Invalid;

            var names = new GuiBindingList<string>();
            var icons = new GuiBindingList<string>();
            var selections = new GuiBindingList<bool>();
            _visibleKeyItems.Clear();

            foreach (var type in keyItems)
            {
                var detail = KeyItem.GetKeyItem(type);

                // If a key item filter is applied and this key item isn't part of this category,
                // skip it and move to the next.
                if (SelectedCategoryId != 0 && SelectedCategoryId != (int) detail.Category)
                    continue;

                _visibleKeyItems.Add(type);
                names.Add(detail.Name);
                try
                {
                    icons.Add(KeyItemIcon.GetIconResref(type));
                }
                catch (InvalidOperationException ex)
                {
                    Log.Write(
                        LogGroup.Error,
                        $"Failed to resolve icon for key item '{type}'. {ex}");
                    icons.Add(string.Empty);
                }
                selections.Add(false);
            }

            Icons = icons;
            Names = names;
            Selections = selections;

            var selectedIndex = previouslySelectedType != KeyItemType.Invalid
                ? _visibleKeyItems.IndexOf(previouslySelectedType)
                : -1;
            if (selectedIndex < 0 && _visibleKeyItems.Count > 0)
                selectedIndex = 0;

            SelectKeyItem(selectedIndex);
        }

        public void SelectKeyItem(int index)
        {
            if (_selectedIndex >= 0 && _selectedIndex < Selections.Count)
                Selections[_selectedIndex] = false;

            _selectedIndex = index;
            if (_selectedIndex < 0 || _selectedIndex >= _visibleKeyItems.Count)
            {
                SelectedIcon = string.Empty;
                SelectedName = "No Key Items";
                SelectedType = string.Empty;
                SelectedDescription = SelectedCategoryId == 0
                    ? "You do not have any Key Items."
                    : "No Key Items match the selected category.";
                return;
            }

            Selections[_selectedIndex] = true;

            var type = _visibleKeyItems[_selectedIndex];
            var detail = KeyItem.GetKeyItem(type);
            var categoryDetail = KeyItem.GetKeyItemCategory(detail.Category);

            SelectedIcon = Icons[_selectedIndex];
            SelectedName = detail.Name;
            SelectedType = categoryDetail.Name;
            SelectedDescription = detail.Description;
        }

        public Action OnSelectKeyItem() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _visibleKeyItems.Count)
                return;

            SelectKeyItem(index);
        };

        public void Refresh(KeyItemReceivedRefreshEvent payload)
        {
            LoadKeyItems();
        }
    }
}
