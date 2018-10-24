using Caliburn.Micro;
using SWLOR.Tools.Editor.ViewModels.Contracts;

namespace SWLOR.Tools.Editor.ViewModels
{
    public class DataSyncViewModel: PropertyChangedBase, IDataSyncViewModel
    {
        private IDatabaseConnectionViewModel _dbConnectionVm;

        public DataSyncViewModel(IDatabaseConnectionViewModel dbConnectionVm)
        {
            _dbConnectionVm = dbConnectionVm;
        }

        public IDatabaseConnectionViewModel DatabaseConnectionVM
        {
            get => _dbConnectionVm;
            set
            {
                _dbConnectionVm = value;
                NotifyOfPropertyChange(() => DatabaseConnectionVM);
            }
        }

    }
}
