using System.Windows;
using Caliburn.Micro;
using SWLOR.Tools.Editor.ViewModels.Contracts;

namespace SWLOR.Tools.Editor.ViewModels
{
    public class MenuBarViewModel : PropertyChangedBase, IMenuBarViewModel
    {
        private readonly IWindowManager _windowManager;
        private readonly IDataSyncViewModel _dataSyncVM;
        private readonly IImportViewModel _importVM;
        private readonly IExportViewModel _exportVM;

        public MenuBarViewModel(
            IWindowManager windowManager,
            IDataSyncViewModel dataSyncVM,
            IImportViewModel importVM,
            IExportViewModel exportVM)
        {
            _windowManager = windowManager;
            _dataSyncVM = dataSyncVM;
            _importVM = importVM;
            _exportVM = exportVM;
        }

        public void Import()
        {
            _windowManager.ShowDialog(_importVM);
        }

        public void Export()
        {
            _exportVM.LoadAvailableResources();
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
