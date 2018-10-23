using Caliburn.Micro;
using System.Windows;
using SWLOR.Tools.Editor.Messages;
using SWLOR.Tools.Editor.ViewModels.Contracts;

namespace SWLOR.Tools.Editor
{
    public class ShellViewModel: PropertyChangedBase, IShellViewModel, IHandle<SettingsLoadedMessage>
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

            _eventAggregator.Subscribe(this);
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

        private int _width;

        public int Width
        {
            get => _width;
            set
            {
                _width = value;
                NotifyOfPropertyChange(() => Width);
            }
        }

        private int _height;

        public int Height
        {
            get => _height;
            set
            {
                _height = value;
                NotifyOfPropertyChange(() => Height);
            }
        }

        public void Handle(SettingsLoadedMessage message)
        {
            Height = message.Settings.Height;
            Width = message.Settings.Width;
        }
    }
}