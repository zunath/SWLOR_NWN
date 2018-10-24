using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Tools.Editor.Factories.Contracts
{
    public interface IDataContextFactory
    {
        IDataContext CreateContext(string ipAddress, string username, string password, string database);
        IDataContext CreateContext();
    }
}
