using Caliburn.Micro;
using System.Windows;
using SWLOR.Tools.Editor.Messages;
using SWLOR.Tools.Editor.ViewModels.Contracts;

namespace SWLOR.Tools.Editor
{
    public class ShellViewModel: PropertyChangedBase, IShellViewModel, IHandle<ApplicationStartedMessage>
    {
        private IEventAggregator _eventAggregator;
        private IMenuBarViewModel _menuBarVM;
        private IEditorListViewModel _editorListVM;

        public ShellViewModel(
            IEventAggregator eventAggregator,
            IMenuBarViewModel menuBarVM,
            IEditorListViewModel editorListVM)
        {
            _eventAggregator = eventAggregator;
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

        public void Handle(ApplicationStartedMessage message)
        {

        }
    }
}