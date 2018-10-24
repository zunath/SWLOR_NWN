using System.Windows;
using Caliburn.Micro;
using SWLOR.Tools.Editor.ViewModels.Contracts;
using SWLOR.Tools.Editor.Views;

namespace SWLOR.Tools.Editor.ViewModels
{
    public class MenuBarViewModel : PropertyChangedBase, IMenuBarViewModel
    {
        private readonly IWindowManager _windowManager;
        private readonly IDataSyncViewModel _dataSyncVM;

        public MenuBarViewModel(IWindowManager windowManager,
            IDataSyncViewModel dataSyncVM)
        {
            _windowManager = windowManager;
            _dataSyncVM = dataSyncVM;
        }

        public void Import()
        {

        }

        public void Export()
        {

        }

        public void Sync()
        {
            _windowManager.ShowDialog(_dataSyncVM);
        }


        public void Exit()
        {
            Application.Current.Shutdown();
        }

    }
}
