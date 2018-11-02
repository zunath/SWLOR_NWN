using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IDataService
    {
        ConcurrentQueue<DatabaseAction> DataQueue { get; }

        T Get<T>(object id) where T : class, IEntity;
        IEnumerable<T> GetAll<T>() where T : class, IEntity;
        void StoredProcedure(string procedureName, params SqlParameter[] args);
        List<T> StoredProcedure<T>(string procedureName, params SqlParameter[] args);
        T StoredProcedureSingle<T>(string procedureName, params SqlParameter[] args);
        void SubmitDataChange(DatabaseAction action);
        void SubmitDataChange(IEntity data, DatabaseActionType actionType);
    }
}