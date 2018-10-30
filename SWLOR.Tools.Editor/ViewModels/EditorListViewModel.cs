using Caliburn.Micro;
using SWLOR.Tools.Editor.ViewModels.Contracts;

namespace SWLOR.Tools.Editor.ViewModels
{
    public class EditorListViewModel : PropertyChangedBase, IEditorListViewModel
    {
        private IApartmentBuildingEditorViewModel _apartmentEditorVM;
        private ILootEditorViewModel _lootEditorVM;

        public EditorListViewModel(
            IApartmentBuildingEditorViewModel apartmentEditorVM,
            ILootEditorViewModel lootEditorVM)
        {
            _apartmentEditorVM = apartmentEditorVM;
            _lootEditorVM = lootEditorVM;
        }

        public IApartmentBuildingEditorViewModel ApartmentEditorVM
        {
            get => _apartmentEditorVM;
            set
            {
                _apartmentEditorVM = value;
                NotifyOfPropertyChange(() => ApartmentEditorVM);
            }
        }

        public ILootEditorViewModel LootEditorVM
        {
            get => _lootEditorVM;
            set
            {
                _lootEditorVM = value;
                NotifyOfPropertyChange(() => LootEditorVM);
            }
        }

    }
}
