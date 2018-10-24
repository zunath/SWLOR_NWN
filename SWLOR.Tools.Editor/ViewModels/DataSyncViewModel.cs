using System.Collections.Concurrent;
using System.Windows;
using Caliburn.Micro;
using SWLOR.Game.Server.Extension;
using SWLOR.Tools.Editor.Messages;
using SWLOR.Tools.Editor.ViewModels.Contracts;

namespace SWLOR.Tools.Editor.ViewModels
{
    public class DataSyncViewModel: 
        Screen, 
        IDataSyncViewModel,
        IHandle<DatabaseConnectionSucceededMessage>,
        IHandle<DatabaseConnectionFailedMessage>,
        IHandle<DatabaseConnectingMessage>
    {
        private IDatabaseConnectionViewModel _dbConnectionVm;
        private readonly IWindowManager _windowManager;
        private readonly IErrorViewModel _errorVM;

        public DataSyncViewModel(
            IDatabaseConnectionViewModel dbConnectionVm,
            IWindowManager windowManager,
            IErrorViewModel errorVM,
            IEventAggregator eventAggregator)
        {
            _dbConnectionVm = dbConnectionVm;
            _windowManager = windowManager;
            _errorVM = errorVM;

            ControlsEnabled = true;
            eventAggregator.Subscribe(this);
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

        private Visibility _dbConnectionViewVisibility;

        public Visibility DBConnectionViewVisibility
        {
            get => _dbConnectionViewVisibility;
            set
            {
                _dbConnectionViewVisibility = value;
                NotifyOfPropertyChange(() => DBConnectionViewVisibility);
            }
        }

        private bool _controlsEnabled;

        public bool ControlsEnabled
        {
            get => _controlsEnabled;
            set
            {
                _controlsEnabled = value;
                NotifyOfPropertyChange(() => ControlsEnabled);
            }
        }



        public void Handle(DatabaseConnectionSucceededMessage message)
        {
            DBConnectionViewVisibility = Visibility.Collapsed;
        }

        public void Handle(DatabaseConnectionFailedMessage message)
        {
            _windowManager.ShowDialog(_errorVM);
            ControlsEnabled = true;
        }

        public void Handle(DatabaseConnectingMessage message)
        {
            ControlsEnabled = false;
        }
    }
}
