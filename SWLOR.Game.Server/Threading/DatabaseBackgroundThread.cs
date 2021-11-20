﻿
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;
using System;
using Dapper.Contrib.Extensions;
using MySql.Data.MySqlClient;

namespace SWLOR.Game.Server.Threading
{
    public class DatabaseBackgroundThread
    {
        private MySqlConnection _connection;
        
        public void Start()
        {
            _connection = new MySqlConnection(DataService.SWLORConnectionString);
            _connection.Open();
        }

        public void Stop()
        {
            ProcessQueue();
            _connection.Close();
        }

        private void ProcessQueue()
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
                        foreach (var record in request.Data)
                        {
                            SqlMapperExtensions.Insert(_connection, record);
                        }
                    }
                    else if (request.Action == DatabaseActionType.Update)
                    {
                        foreach (var record in request.Data)
                        {
                            SqlMapperExtensions.Update(_connection, record);
                        }
                    }
                    else if (request.Action == DatabaseActionType.Delete)
                    {
                        foreach (var record in request.Data)
                        {
                            SqlMapperExtensions.Delete(_connection, record);
                        }
                    }

                }
                catch (MySqlException ex)
                {
                    Console.WriteLine("****EXCEPTION ON DATABASE BACKGROUND THREAD****");
                    Console.WriteLine("Data Type: " + request.DataType);
                    Console.WriteLine("Action: " + request.Action);
                    LoggingService.LogError(ex, request.Action.ToString());
                }
                catch (Exception ex)
                {
                    Console.WriteLine("****EXCEPTION ON DATABASE BACKGROUND THREAD****");
                    Console.WriteLine("Data Type: " + request.DataType);
                    Console.WriteLine("Action: " + request.Action);
                    LoggingService.LogError(ex, request.Action.ToString());
                }
            }
        }

        public void Run()
        {
            ProcessQueue();
        }

    }
}
