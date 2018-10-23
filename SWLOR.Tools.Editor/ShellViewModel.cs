using Caliburn.Micro;
using System.Windows;
using SWLOR.Tools.Editor.ViewModels.Contracts;

namespace SWLOR.Tools.Editor
{
    public class ShellViewModel: PropertyChangedBase, IShellViewModel
    {
        private IMenuBarViewModel _menuBarVM;
        private IEditorListViewModel _editorListVM;

        public ShellViewModel(
            IMenuBarViewModel menuBarVM,
            IEditorListViewModel editorListVM)
        {
            _menuBarVM = menuBarVM;
            _editorListVM = editorListVM;
        }

        public IMenuBarViewModel MenuBarVM
        {
            get => _menuBarVM;
            set
            {
                _menuBarVM = value;
                NotifyOfPropertyChange(() => MenuBarVM);
            }
        }

        public IEditorListViewModel EditorListVM
        {
            get => _editorListVM;
            set
            {
                _editorListVM = value;
                NotifyOfPropertyChange(() => EditorListVM);
            }
        }

    }
}