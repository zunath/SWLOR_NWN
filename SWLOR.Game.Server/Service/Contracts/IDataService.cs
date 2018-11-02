using System.Collections.Concurrent;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IDataService
    {
        ConcurrentQueue<DatabaseAction> DataQueue { get; }

        void EnqueueDatabaseAction(DatabaseAction action);
    }
}