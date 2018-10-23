using Caliburn.Micro;
using SWLOR.Tools.Editor.ViewModels.Contracts;

namespace SWLOR.Tools.Editor.ViewModels
{
    public class EditorListViewModel : PropertyChangedBase, IEditorListViewModel
    {
        private ILootEditorViewModel _lootEditorVM;

        public EditorListViewModel(ILootEditorViewModel lootEditorVM)
        {
            _lootEditorVM = lootEditorVM;
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
