using System.Collections.Concurrent;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service
{
    public class DataService : IDataService
    {
        public ConcurrentQueue<DatabaseAction> DataQueue { get; private set; }

        public DataService()
        {
            DataQueue = new ConcurrentQueue<DatabaseAction>();
        }

        public void EnqueueDatabaseAction(DatabaseAction action)
        {
            DataQueue.Enqueue(action);
        }

    }
}
