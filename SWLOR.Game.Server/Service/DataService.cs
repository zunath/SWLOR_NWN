
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service
{
    public class DataService : IDataService
    {
        public ConcurrentQueue<DatabaseAction> DataQueue { get; }
        private string _connectionString;

        public DataService()
        {
            DataQueue = new ConcurrentQueue<DatabaseAction>();
        }

        public void Initialize()
        {
            _connectionString = new SqlConnectionStringBuilder()
            {
                DataSource = Environment.GetEnvironmentVariable("SQL_SERVER_IP_ADDRESS"),
                InitialCatalog = Environment.GetEnvironmentVariable("SQL_SERVER_DATABASE"),
                UserID = Environment.GetEnvironmentVariable("SQL_SERVER_USERNAME"),
                Password = Environment.GetEnvironmentVariable("SQL_SERVER_PASSWORD")
            }.ToString();
        }

        public void Initialize(string ip, string database, string user, string password)
        {
            _connectionString = new SqlConnectionStringBuilder()
            {
                DataSource = ip,
                InitialCatalog = database,
                UserID = user,
                Password = password
            }.ToString();
        }

        /// <summary>
        /// Sends a request to change data into the queue. Processing is asynchronous
        /// and you cannot reliably retrieve the data immediately afterwards.
        /// </summary>
        public void SubmitDataChange(DatabaseAction action)
        {
            DataQueue.Enqueue(action);
        }

        /// <summary>
        /// Sends a request to change data into the queue. Processing is asynchronous
        /// and you cannot reliably retrieve the data immediately afterwards.
        /// </summary>
        /// <param name="data">The data to submit for processing</param>
        /// <param name="actionType">The type (Insert, Update, Delete, etc.) of change to make.</param>
        public void SubmitDataChange(IEntity data, DatabaseActionType actionType)
        {
            SubmitDataChange(new DatabaseAction(data, actionType));
        }

        /// <summary>
        /// Returns a single entity of a given type from the database.
        /// </summary>
        /// <typeparam name="T">The type of entity to retrieve.</typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public T Get<T>(object id)
            where T: class, IEntity
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                return connection.Get<T>(id);
            }
        }

        /// <summary>
        /// Returns all entities of a given type from the database.
        /// </summary>
        /// <typeparam name="T">The type of entity to retrieve.</typeparam>
        /// <returns></returns>
        public IEnumerable<T> GetAll<T>()
            where T: class, IEntity
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                return connection.GetAll<T>();
            }
        }


        public void StoredProcedure(string procedureName, params SqlParameter[] args)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Execute(BuildSQLQuery(procedureName, args), args);
            }
        }

        public IEnumerable<T> StoredProcedure<T>(string procedureName, params SqlParameter[] args)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                return connection.Query<T>(procedureName, args, commandType: CommandType.StoredProcedure);
            }
        }

        public IEnumerable<TResult> StoredProcedure<T1, T2, TResult>(string procedureName, Func<T1, T2, TResult> map, string splitOn, params SqlParameter[] args)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                return connection.Query(procedureName, map, args.Length <= 0 ? null : args, splitOn: splitOn, commandType: CommandType.StoredProcedure);
            }
        }

        public IEnumerable<TResult> StoredProcedure<T1, T2, T3, TResult>(string procedureName, Func<T1, T2, T3, TResult> map, string splitOn, params SqlParameter[] args)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                return connection.Query(procedureName, map, args.Length <= 0 ? null : args, splitOn: splitOn, commandType: CommandType.StoredProcedure);
            }
        }
        
        public IEnumerable<TResult> StoredProcedure<T1, T2, T3, T4, TResult>(string procedureName, Func<T1, T2, T3, T4, TResult> map, string splitOn, params SqlParameter[] args)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                return connection.Query(procedureName, map, args.Length <= 0 ? null : args, splitOn: splitOn, commandType: CommandType.StoredProcedure);
            }
        }

        public T StoredProcedureSingle<T>(string procedureName, params SqlParameter[] args)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                return connection.Query<T>(procedureName, args, commandType: CommandType.StoredProcedure).SingleOrDefault();
            }
        }

        private static string BuildSQLQuery(string procedureName, params SqlParameter[] args)
        {
            string sql = procedureName;

            for (int x = 0; x < args.Length; x++)
            {
                sql += " " + args[x].ParameterName;

                if (x + 1 < args.Length) sql += ",";
            }

            return sql;
        }
        

    }
}
