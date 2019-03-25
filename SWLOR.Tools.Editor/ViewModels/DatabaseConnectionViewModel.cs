using System;
using Caliburn.Micro;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Service;

using SWLOR.Tools.Editor.Messages;
using SWLOR.Tools.Editor.ViewModels.Contracts;

namespace SWLOR.Tools.Editor.ViewModels
{
    public class DatabaseConnectionViewModel : 
        PropertyChangedBase, 
        IDatabaseConnectionViewModel,
        IHandle<SettingsLoaded>,
        IHandle<ApplicationEnded>
    {
        private readonly AppSettings _appSettings;
        private readonly IEventAggregator _eventAggregator;
        

        public DatabaseConnectionViewModel(
            IEventAggregator eventAggregator, 
            AppSettings appSettings)
        {
            _appSettings = appSettings;
            _eventAggregator = eventAggregator;

            NotConnected = true;
            IPAddress = string.Empty;
            Username = string.Empty;
            Password = string.Empty;
            Database = string.Empty;

            eventAggregator.Subscribe(this);
        }

        private bool _notConnected;

        public bool NotConnected
        {
            get => _notConnected;
            set
            {
                _notConnected = value;
                NotifyOfPropertyChange(() => NotConnected);
            }
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
                NotConnected = false;
                _appSettings.DatabaseIPAddress = IPAddress;
                _appSettings.DatabaseUsername = Username;
                _appSettings.DatabaseName = Database;
                _appSettings.DatabasePassword = Password;

                DataService.Initialize(IPAddress, Database, Username, Password, false);

                // Attempt to grab some data from the new connection, to ensure it's running.
                DataService.Get<ServerConfiguration>(1);
                
                Password = string.Empty;
                _eventAggregator.PublishOnUIThread(new DatabaseConnectionSucceeded());
            }
            catch(Exception ex)
            {
                _eventAggregator.PublishOnUIThread(new DatabaseConnectionFailed(ex));
                NotConnected = true;
            }

        }
        
        public void Handle(SettingsLoaded message)
        {
            IPAddress = message.Settings.DatabaseIPAddress;
            Username = message.Settings.DatabaseUsername;
            Database = message.Settings.DatabaseName;
        }

        public void Handle(ApplicationEnded message)
        {
            _appSettings.DatabaseIPAddress = IPAddress;
            _appSettings.DatabaseName = Database;
            _appSettings.DatabaseUsername = Username;
        }
    }
}
