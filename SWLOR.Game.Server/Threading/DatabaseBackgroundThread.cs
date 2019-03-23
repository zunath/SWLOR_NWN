using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.Threading.Contracts;
using SWLOR.Game.Server.ValueObject;
using System;
using System.Data.SqlClient;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Threading
{
    public class DatabaseBackgroundThread : IDatabaseThread
    {
        private readonly string _connectionString;
        
        public DatabaseBackgroundThread()
        {   
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
            if (DataService.DataQueue.IsEmpty) return;

            using (var connection = new SqlConnection(_connectionString))
            {
                while (!DataService.DataQueue.IsEmpty)
                {
                    if (!DataService.DataQueue.TryDequeue(out DatabaseAction request))
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
                        }
                        else if (request.Action == DatabaseActionType.Update)
                        {
                            foreach (var record in request.Data)
                            {
                                connection.Update(record.GetType(), record);
                            }
                        }
                        else if (request.Action == DatabaseActionType.Delete)
                        {
                            foreach (var record in request.Data)
                            {
                                connection.Delete(record.GetType(), record);
                            }
                        }

                    }
                    catch (SqlException ex)
                    {
                        Console.WriteLine("****EXCEPTION ON DATABASE BACKGROUND THREAD****");
                        ErrorService.LogError(ex, request.Action.ToString());
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("****EXCEPTION ON DATABASE BACKGROUND THREAD****");
                        ErrorService.LogError(ex, request.Action.ToString());
                    }
                }
            }
        }
    }
}
