using System.Windows;
using Caliburn.Micro;
using SWLOR.Tools.Editor.ViewModels.Contracts;

namespace SWLOR.Tools.Editor.ViewModels
{
    public class MenuBarViewModel : PropertyChangedBase, IMenuBarViewModel
    {
        private readonly IWindowManager _windowManager;
        private readonly IDataSyncViewModel _dataSyncVM;
        private readonly IExportViewModel _exportVM;

        public MenuBarViewModel(
            IWindowManager windowManager,
            IDataSyncViewModel dataSyncVM,
            IExportViewModel exportVM)
        {
            _windowManager = windowManager;
            _dataSyncVM = dataSyncVM;
            _exportVM = exportVM;
        }

        public void Import()
        {

        }

        public void Export()
        {
            _windowManager.ShowDialog(_exportVM);
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
