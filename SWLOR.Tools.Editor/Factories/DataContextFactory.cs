using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Tools.Editor.Factories.Contracts;

namespace SWLOR.Tools.Editor.Factories
{
    public class DataContextFactory: IDataContextFactory
    {
        private readonly AppSettings _appSettings;

        public DataContextFactory(AppSettings appSettings)
        {
            _appSettings = appSettings;
        }

        public IDataContext CreateContext(string ipAddress, string username, string password, string database)
        {
            return new DataContext(ipAddress, username, password, database);
        }

        public IDataContext CreateContext()
        {
            return new DataContext(_appSettings.DatabaseIPAddress, _appSettings.DatabaseUsername, _appSettings.DatabasePassword, _appSettings.DatabaseName);
        }
    }
}
