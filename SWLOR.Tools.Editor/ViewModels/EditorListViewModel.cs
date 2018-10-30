using Caliburn.Micro;
using SWLOR.Tools.Editor.ViewModels.Contracts;

namespace SWLOR.Tools.Editor.ViewModels
{
    public class EditorListViewModel : PropertyChangedBase, IEditorListViewModel
    {
        private IApartmentBuildingEditorViewModel _apartmentEditorVM;
        private ICustomEffectEditorViewModel _customEffectVM;
        private ILootEditorViewModel _lootEditorVM;

        public EditorListViewModel(
            IApartmentBuildingEditorViewModel apartmentEditorVM,
            ICustomEffectEditorViewModel customEffectVM,
            ILootEditorViewModel lootEditorVM)
        {
            _apartmentEditorVM = apartmentEditorVM;
            _customEffectVM = customEffectVM;
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

        public ICustomEffectEditorViewModel CustomEffectVM
        {
            get => _customEffectVM;
            set
            {
                _customEffectVM = value;
                NotifyOfPropertyChange(() => CustomEffectVM);
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
