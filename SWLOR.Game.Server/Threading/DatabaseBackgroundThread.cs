using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.Threading.Contracts;
using SWLOR.Game.Server.ValueObject;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Cache;
using System.Reflection;
using System.Threading;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Threading
{
    public class DatabaseBackgroundThread : IDatabaseThread
    {
        private bool _isExiting;
        private readonly IErrorService _error;
        private readonly IDataService _data;
        private readonly string _connectionString;
        
        public DatabaseBackgroundThread(
            IErrorService error,
            IDataService data)
        {
            _error = error;
            _data = data;
            
            _connectionString = new SqlConnectionStringBuilder()
            {
                DataSource = Environment.GetEnvironmentVariable("SQL_SERVER_IP_ADDRESS"),
                InitialCatalog = Environment.GetEnvironmentVariable("SQL_SERVER_DATABASE"),
                UserID = Environment.GetEnvironmentVariable("SQL_SERVER_USERNAME"),
                Password = Environment.GetEnvironmentVariable("SQL_SERVER_PASSWORD")
            }.ToString();
        }

        public void Run()
        {
            if (_data.DataQueue.IsEmpty) return;

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                while (!_data.DataQueue.IsEmpty)
                {
                    if (!_data.DataQueue.TryDequeue(out DatabaseAction request))
                    {
                        Console.WriteLine("DATABASE WORKER: Was unable to process an object. Will try again...");
                        return;
                    }

                    try
                    {
                        if (request.Action == DatabaseActionType.Insert)
                        {
                            foreach(var record in request.Data)
                            {
                                connection.Insert(record.GetType(), record);
                            }
                            //connection.Insert<object>(request.Data);
                        }
                        else if (request.Action == DatabaseActionType.Update)
                        {
                            foreach (var record in request.Data)
                            {
                                connection.Update(record.GetType(), record);
                            }
                            //connection.Update<object>(request.Data);
                        }
                        else if (request.Action == DatabaseActionType.Delete)
                        {
                            foreach (var record in request.Data)
                            {
                                connection.Delete(record.GetType(), record);
                            }
                            //connection.Delete<object>(request.Data);
                        }

                    }
                    catch (SqlException ex)
                    {
                        Console.WriteLine("****EXCEPTION ON DATABASE BACKGROUND THREAD****");
                        _error.LogError(ex, request.Action.ToString());

                        Thread.Sleep(3000); // todo debug
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("****EXCEPTION ON DATABASE BACKGROUND THREAD****");
                        _error.LogError(ex, request.Action.ToString());
                        
                        Thread.Sleep(3000); // todo debug
                    }
                }
            }
        }

        public void Exit()
        {
            _isExiting = true;
        }

        private string GetTableName(Type type)
        {
            var tableAttribute = (TableAttribute)type.GetCustomAttributes(typeof(TableAttribute)).First();
            return tableAttribute.Name;
        }

        private string BuildInsertSQL(IEntity entity)
        {
            var type = entity.GetType();
            bool isList = false;

            if (type.IsArray)
            {
                isList = true;
                type = type.GetElementType();
            }
            else if (type.IsGenericType)
            {
                var typeInfo = type.GetTypeInfo();
                bool implementsGenericIEnumerableOrIsGenericIEnumerable =
                    typeInfo.ImplementedInterfaces.Any(ti => ti.IsGenericType && ti.GetGenericTypeDefinition() == typeof(IEnumerable<>)) ||
                    typeInfo.GetGenericTypeDefinition() == typeof(IEnumerable<>);

                if (implementsGenericIEnumerableOrIsGenericIEnumerable)
                {
                    isList = true;
                    type = type.GetGenericArguments()[0];
                }
            }
            
            var name = GetTableName(type);

            string sql = string.Empty;

            return sql;
        }



    }
}
