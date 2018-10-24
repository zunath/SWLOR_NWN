using System;
using System.Linq;
using Caliburn.Micro;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Tools.Editor.Factories.Contracts;
using SWLOR.Tools.Editor.Messages;
using SWLOR.Tools.Editor.ViewModels.Contracts;

namespace SWLOR.Tools.Editor.ViewModels
{
    public class DatabaseConnectionViewModel : 
        PropertyChangedBase, 
        IDatabaseConnectionViewModel,
        IHandle<SettingsLoadedMessage>,
        IHandle<ApplicationEndedMessage>
    {
        private IDataContext _db;
        private readonly IDataContextFactory _dbFactory;
        private readonly AppSettings _appSettings;
        private readonly IEventAggregator _eventAggregator;

        public DatabaseConnectionViewModel(
            IEventAggregator eventAggregator, 
            IDataContextFactory dbFactory,
            AppSettings appSettings)
        {
            _dbFactory = dbFactory;
            _appSettings = appSettings;
            _eventAggregator = eventAggregator;

            IPAddress = string.Empty;
            Username = string.Empty;
            Password = string.Empty;
            Database = string.Empty;

            eventAggregator.Subscribe(this);
        }

        private string _ipAddress;

        public string IPAddress
        {
            get => _ipAddress;
            set
            {
                _ipAddress = value;
                NotifyOfPropertyChange(() => IPAddress);
            }
        }

        private string _username;

        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                NotifyOfPropertyChange(() => Username);
            }
        }
        private string _password;

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                NotifyOfPropertyChange(() => Password);
            }
        }
        private string _database;

        public string Database
        {
            get => _database;
            set
            {
                _database = value;
                NotifyOfPropertyChange(() => Database);
            }
        }

        public void Connect()
        {
            try
            {
                _appSettings.DatabaseIPAddress = IPAddress;
                _appSettings.DatabaseUsername = Username;
                _appSettings.DatabasePassword = Password;
                _appSettings.DatabaseName = Database;

                _db = _dbFactory.CreateContext();
                var config = _db.ServerConfigurations.First();

                _eventAggregator.PublishOnUIThread(new DatabaseConnectionSucceededMessage());
            }
            catch(Exception ex)
            {
                _db = null;

                _eventAggregator.PublishOnUIThread(new DatabaseConnectionFailedMessage(ex));
            }

        }
        
        public void Handle(SettingsLoadedMessage message)
        {
            IPAddress = message.Settings.DatabaseIPAddress;
            Username = message.Settings.DatabaseUsername;
            Database = message.Settings.DatabaseName;
        }

        public void Handle(ApplicationEndedMessage message)
        {
            _appSettings.DatabaseIPAddress = IPAddress;
            _appSettings.DatabaseName = Database;
            _appSettings.DatabaseUsername = Username;
        }
    }
}
