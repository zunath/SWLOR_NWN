using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class PropertyItemStorageViewModel: GuiViewModelBase<PropertyItemStorageViewModel, GuiPayloadBase>
    {
        private List<string> _categoryIds = new();

        public GuiBindingList<string> CategoryNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> CategoryToggles
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        private int SelectedCategoryIndex { get; set; }

        public string CategoryName
        {
            get => Get<string>();
            set => Set(value);
        }

        private List<string> _itemIds = new();

        public GuiBindingList<string> ItemNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> ItemToggles
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        private int SelectedItemIndex { get; set; }

        public bool IsCategorySelected
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsItemSelected
        {
            get => Get<bool>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            
        }

        public Action OnAddCategory() => () =>
        {

        };

        public Action OnDeleteCategory() => () =>
        {

        };

        public Action OnEditPermissions() => () =>
        {

        };

        public Action OnSaveName() => () =>
        {

        };

        public Action OnStoreItem() => () =>
        {

        };

        public Action OnRetrieveItem() => () =>
        {

        };

        public Action OnExamineItem() => () =>
        {

        };
    }
}
