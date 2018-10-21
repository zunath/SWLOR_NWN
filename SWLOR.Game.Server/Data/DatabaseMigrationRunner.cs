using Autofac;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
namespace SWLOR.Game.Server.Data
{
    public class DatabaseMigrationRunner: IStartable
    {
        private readonly IDataContext _db;
        private readonly IErrorService _error;

        public DatabaseMigrationRunner(
            IDataContext db,
            IErrorService error)
        {
            _db = db;
            _error = error;
        }

        public void Start()
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            string folderName = string.Format("{0}.Data.Migrations", executingAssembly.GetName().Name);
            Console.WriteLine("Starting database migration runner...");

            if (!_db.Database.Exists())
            {
                Console.WriteLine("Database not found. Generating database...");
                var ipAddress = Environment.GetEnvironmentVariable("SQL_SERVER_IP_ADDRESS");
                var username = Environment.GetEnvironmentVariable("SQL_SERVER_USERNAME");
                var password = Environment.GetEnvironmentVariable("SQL_SERVER_PASSWORD");
                SqlConnectionStringBuilder sqlString = new SqlConnectionStringBuilder()
                {
                    DataSource = ipAddress,
                    InitialCatalog = "MASTER",
                    UserID = username,
                    Password = password
                };

                using (var connection = new SqlConnection(sqlString.ToString()))
                {
                    connection.Open();
                    try
                    {
                        string dbName = Environment.GetEnvironmentVariable("SQL_SERVER_DATABASE");
                        string sql = $@"CREATE DATABASE [{dbName}]";

                        var command = connection.CreateCommand();
                        command.CommandText = sql;
                        command.ExecuteNonQuery();
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine("ERROR: Unable to create database. Please check your permissions.");
                        _error.LogError(ex);
                        return;
                    }
                }
                
                using(var connection = new SqlConnection(sqlString.ToString()))
                {
                    Console.WriteLine("Creating tables, procedures, views, etc...");
                    string sql = ReadResourceFile(folderName + ".Initialization.sql");
                    ExecuteBatchNonQuery(sql, connection);
                    
                    Console.WriteLine("Tables, procedures, views, etc... all created successfully!");
                }

                
                Console.WriteLine("Database generated!");
            }

            var currentVersion = _db.DatabaseVersions
                .OrderByDescending(o => o.ScriptName).FirstOrDefault();
            
            // Pull back all of the script files located in the Data/Migrations folder.
            // All files in this directory should be "Embedded Resources".
            // Ordering is done based on date and version number. 
            // Follow the established pattern when adding new migration scripts
            var resources = executingAssembly
                .GetManifestResourceNames()
                .Where(r =>
                {
                    return r.StartsWith(folderName) &&
                           r.EndsWith(".sql");
                })
                .OrderBy(o => o)
                .ToList();
            
            foreach (var resource in resources)
            {
                string fileName = resource.Remove(0, folderName.Length+1); // Length of name plus the period afterwards.
                ApplyMigrationScript(resource, fileName);
            }
        }

        private Tuple<DateTime, int> GetVersionInformation(string fileName)
        {
            // Remove file extension (.sql), split by period denoting date and version number.
            string fileNameNoExtension = Path.GetFileNameWithoutExtension(fileName);
            string[] data = fileNameNoExtension.Split('.');
            
            DateTime fileDate = DateTime.ParseExact(data[0], "yyyy-MM-dd", null);
            int fileVersion = int.Parse(data[1]);

            return new Tuple<DateTime, int>(fileDate, fileVersion);
        }

        private void ApplyMigrationScript(string resource, string fileName)
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            Console.WriteLine("Applying migration script: " + resource);
            
            string sql = ReadResourceFile(resource);
            
            var tran = _db.Database.BeginTransaction();
            try
            {
                _db.Database.ExecuteSqlCommand(sql);
                tran.Commit();
                
                var versionInfo = GetVersionInformation(fileName);
                _db.DatabaseVersions.Add(new DatabaseVersion
                {
                    DateApplied = DateTime.UtcNow,
                    ScriptName = resource,
                    VersionDate = versionInfo.Item1,
                    VersionNumber = versionInfo.Item2
                });
                _db.SaveChanges();

                Console.WriteLine("Migration script '" + resource + "' applied successfully!");
            }
            catch (Exception ex)
            {
                tran.Rollback();
                Console.WriteLine("ERROR: Database migration script named '" + resource + "' failed to apply. Canceling database migration process!");
                _error.LogError(ex, nameof(DatabaseMigrationRunner));
                return; // Early exit.
            }
        }

        private string ReadResourceFile(string resource)
        {
            var executingAssembly = Assembly.GetExecutingAssembly();

            using (var stream = executingAssembly.GetManifestResourceStream(resource))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        // Code I pulled from StackOverflow: https://stackoverflow.com/questions/40814/execute-a-large-sql-script-with-go-commands
        // Can't execute the entire script at once. You have to split out the "GO"s
        private void ExecuteBatchNonQuery(string sql, SqlConnection conn)
        {
            string sqlBatch = string.Empty;
            SqlCommand cmd = new SqlCommand(string.Empty, conn);
            conn.Open();
            sql += "\nGO";   // make sure last batch is executed.
            try
            {
                foreach (string line in sql.Split(new string[2] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (line.ToUpperInvariant().Trim() == "GO")
                    {
                        cmd.CommandText = sqlBatch;
                        cmd.ExecuteNonQuery();
                        sqlBatch = string.Empty;
                    }
                    else
                    {
                        sqlBatch += line + "\n";
                    }
                }
            }
            finally
            {
                conn.Close();
            }
        }
    }
}
