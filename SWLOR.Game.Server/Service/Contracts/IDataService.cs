using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IDataService
    {
        ConcurrentQueue<DatabaseAction> DataQueue { get; }
        void Initialize(bool initializeCache);
        void Initialize(string ip, string database, string user, string password, bool initializeCache);

        T Get<T>(object id) where T : class, IEntity;
        IEnumerable<T> GetAll<T>() where T : class, IEntity;

        T Single<T>()
            where T : class, IEntity;
        T Single<T>(Func<T, bool> predicate)
            where T : class, IEntity;
        T SingleOrDefault<T>()
            where T : class, IEntity;
        T SingleOrDefault<T>(Func<T, bool> predicate)
            where T : class, IEntity;
        IEnumerable<T> Where<T>(Func<T, bool> predicate)
            where T : class, IEntity;

        void StoredProcedure(string procedureName, params SqlParameter[] args);
        IEnumerable<T> StoredProcedure<T>(string procedureName, params SqlParameter[] args);
        T StoredProcedureSingle<T>(string procedureName, params SqlParameter[] args);
        IEnumerable<TResult> StoredProcedure<T1, T2, TResult>(string procedureName, Func<T1, T2, TResult> map, string splitOn, SqlParameter arg);
        IEnumerable<TResult> StoredProcedure<T1, T2, T3, TResult>(string procedureName, Func<T1, T2, T3, TResult> map, string splitOn, SqlParameter arg);
        IEnumerable<TResult> StoredProcedure<T1, T2, T3, T4, TResult>(string procedureName, Func<T1, T2, T3, T4, TResult> map, string splitOn, SqlParameter arg);

        void SubmitDataChange(DatabaseAction action);
        void SubmitDataChange(IEntity data, DatabaseActionType actionType);
        void CachePlayerData(NWPlayer player);
        void RemoveCachedPlayerData(NWPlayer player);
    }
}