using Caliburn.Micro;
using SWLOR.Tools.Editor.ViewModels.Contracts;

namespace SWLOR.Tools.Editor.ViewModels
{
    public class EditorListViewModel : PropertyChangedBase, IEditorListViewModel
    {
        private IApartmentBuildingEditorViewModel _apartmentEditorVM;
        private ICustomEffectEditorViewModel _customEffectVM;
        private IDownloadEditorViewModel _downloadVM;
        private ILootEditorViewModel _lootEditorVM;
        private IPlantEditorViewModel _plantEditorVM;

        public EditorListViewModel(
            IApartmentBuildingEditorViewModel apartmentEditorVM,
            ICustomEffectEditorViewModel customEffectVM,
            IDownloadEditorViewModel downloadVM,
            ILootEditorViewModel lootEditorVM,
            IPlantEditorViewModel plantEditorVM)
        {
            _apartmentEditorVM = apartmentEditorVM;
            _customEffectVM = customEffectVM;
            _downloadVM = downloadVM;
            _lootEditorVM = lootEditorVM;
            _plantEditorVM = plantEditorVM;
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

        public IDownloadEditorViewModel DownloadVM
        {
            get => _downloadVM;
            set
            {
                _downloadVM = value;
                NotifyOfPropertyChange(() => DownloadVM);
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

        public IPlantEditorViewModel PlantEditorVM
        {
            get => _plantEditorVM;
            set
            {
                _plantEditorVM = value;
                NotifyOfPropertyChange(() => PlantEditorVM);
            }
        }

    }
}
