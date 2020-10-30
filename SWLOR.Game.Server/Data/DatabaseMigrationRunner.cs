using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Dapper;
using Dapper.Contrib.Extensions;
using MySqlConnector;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Service.Legacy;

namespace SWLOR.Game.Server.Data
{
    /// <summary>
    /// This will look for the database supplied by the end user. If it doesn't exist, it will create it with the necessary DDL.
    /// It will then grab all migration scripts and run those sequentially until the database is at the current version.
    /// Scripts are located in Data/Migrations/ and all of the sql files should be marked as "Embedded Resource" so the app can pick them up.
    /// WARNING: Your scripts cannot end with a GO call at the moment. Make sure the final command in a migration script is a DB operation.
    /// </summary>
    public static class DatabaseMigrationRunner
    {
        /// <summary>
        /// Returns the folder name containing the sql migration files.
        /// </summary>
        private static string FolderName => $"{Assembly.GetExecutingAssembly().GetName().Name}.Data.Migrations";

        /// <summary>
        /// This is the entry point to the migration runner.
        /// </summary>
        public static void Start()
        {
            Console.WriteLine("Starting database migration runner. This can take a few moments...");

            BuildDatabase();
            ApplyMigrations();
            DataService.Initialize(true);
        }

        /// <summary>
        /// Builds the database if it doesn't exist. Nothing happens if there is already a database created.
        /// </summary>
        private static void BuildDatabase()
        {
            var exists = CheckDatabaseExists();

            if (!exists)
            {
                Console.WriteLine("Database not found. Generating database...");

                using (var connection = new MySqlConnection(DataService.SWLORConnectionString))
                {
                    Console.WriteLine("Creating tables, procedures, views, etc...");
                    var sql = ReadResourceFile(FolderName + ".Initialization.sql");
                    ExecuteBatchNonQuery(sql, connection);

                    Console.WriteLine("Tables, procedures, views, etc... all created successfully!");
                }


                Console.WriteLine("Database generated!");
            }
        }

        private static bool CheckDatabaseExists()
        {
            bool result;

            using (var connection = new MySqlConnection(DataService.SWLORConnectionString))
            {
                connection.Open();
                using (var command = new MySqlCommand($"SHOW TABLES LIKE 'ServerConfiguration'", connection) { CommandTimeout = 0 })
                {
                    var exists = command.ExecuteScalar();

                    result = (exists != null);
                }
            }

            return result;
        }

        /// <summary> 
        /// Pull back all of the script files located in the Data/Migrations folder.
        /// All files in this directory should be "Embedded Resources".
        /// Ordering is done based on date and version number. 
        /// Follow the established pattern when adding new migration scripts
        /// </summary>
        private static void ApplyMigrations()
        {
            var resources = GetScriptResources();
            foreach (var resource in resources)
            {
                ApplyMigrationScript(resource);
            }
        }

        /// <summary>
        /// Gets the actual file name from a full resource file path.
        /// </summary>
        /// <param name="resourceName">The full resource file path.</param>
        /// <returns></returns>
        private static string GetFileNameFromScriptResourceName(string resourceName)
        {
            return resourceName.Remove(0, FolderName.Length + 1); // Length of name plus the period afterwards.
        }

        /// <summary>
        /// Retrieves all scripts which the database runner needs to execute.
        /// Only the scripts which haven't been applied to the database will be retrieved.
        /// </summary>
        /// <returns></returns>
        private static IEnumerable<string> GetScriptResources()
        {
            DatabaseVersion currentVersion;
            using (var connection = new MySqlConnection(DataService.SWLORConnectionString))
            {
                var sql = "select ID, ScriptName, DateApplied, VersionDate, VersionNumber FROM DatabaseVersion ORDER BY VersionDate DESC, VersionNumber DESC LIMIT 1;";
                currentVersion = connection.QueryFirstOrDefault<DatabaseVersion>(sql);
            }

            var executingAssembly = Assembly.GetExecutingAssembly();

            var fullList = executingAssembly
                .GetManifestResourceNames()
                .Where(r => r.StartsWith(FolderName) &&
                            r.EndsWith(".sql") &&
                            r != (FolderName + ".Initialization.sql"))
                .OrderBy(o => o)
                .ToList();

            if (currentVersion == null)
                return fullList;

            return fullList.Where(r =>
            {
                var fileName = GetFileNameFromScriptResourceName(r);
                var versionInfo = GetVersionInformation(fileName);

                // Greater date than currently applied update.
                if (versionInfo.Item1 > currentVersion.VersionDate)
                    return true;

                // Current date but version is greater than currently applied update.
                if (versionInfo.Item1 == currentVersion.VersionDate)
                    return versionInfo.Item2 > currentVersion.VersionNumber;

                // Older than current version. Ignore this since it's already been applied.
                return false;
            }).ToList();
        }


        /// <summary>
        /// Parses a script's name to retrieve the date and version number.
        /// </summary>
        /// <param name="fileName">The file, in yyyy-MM-dd.v format, where y is year, M is month, d is year, and v is version number.</param>
        /// <returns>A tuple containing the date and version number</returns>
        private static Tuple<DateTime, int> GetVersionInformation(string fileName)
        {
            // Remove file extension (.sql), split by period denoting date and version number.
            var fileNameNoExtension = Path.GetFileNameWithoutExtension(fileName);
            var data = fileNameNoExtension.Split('.');

            var fileDate = DateTime.ParseExact(data[0], "yyyy-MM-dd", null);
            var fileVersion = int.Parse(data[1]);

            return new Tuple<DateTime, int>(fileDate, fileVersion);
        }

        /// <summary>
        /// Applies an individual migration script to the database.
        /// </summary>
        /// <param name="resource">The full resource path of the including namespace.</param>
        private static void ApplyMigrationScript(string resource)
        {
            var fileName = GetFileNameFromScriptResourceName(resource);
            Console.WriteLine("Applying migration script: " + resource);

            using (var connection = new MySqlConnection(DataService.SWLORConnectionString))
            {
                try
                {
                    var sql = ReadResourceFile(resource);

                    if (!string.IsNullOrWhiteSpace(sql))
                    {
                        ExecuteBatchNonQuery(sql, connection);
                    }

                    AddDatabaseVersionRecord(fileName);
                    Console.WriteLine("Migration script '" + resource + "' applied successfully!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR: Database migration script named '" + resource + "' failed to apply. Canceling database migration process!");
                    LoggingService.LogError(ex, nameof(DatabaseMigrationRunner));
                }
            }
        }

        /// <summary>
        /// Retrieves an embedded resource, reads the file, and returns the SQL.
        /// </summary>
        /// <param name="resource">The fulle resource path of the including namespace.</param>
        /// <returns>The SQL commands</returns>
        private static string ReadResourceFile(string resource)
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            string sql;

            using (var stream = executingAssembly.GetManifestResourceStream(resource))
            {
                using (var reader = new StreamReader(stream))
                {
                    sql = reader.ReadToEnd();
                }
            }

            return sql.Trim();
        }

        /// <summary>
        /// Adds a database version record to the database, preventing this script from running again in the future.
        /// </summary>
        /// <param name="fileName">The file name, WITHOUT namespacing.</param>
        private static void AddDatabaseVersionRecord(string fileName)
        {
            var versionInfo = GetVersionInformation(fileName);
            var version = new DatabaseVersion
            {
                DateApplied = DateTime.UtcNow,
                ScriptName = fileName,
                VersionDate = versionInfo.Item1,
                VersionNumber = versionInfo.Item2
            };

            using (var connection = new MySqlConnection(DataService.SWLORConnectionString))
            {
                connection.Insert(version);
            }
        }

        private static void ExecuteBatchNonQuery(string sql, MySqlConnection conn)
        {
            var cmd = new MySqlCommand(sql, conn);
            conn.Open();
            try
            {
                cmd.CommandTimeout = 0;
                cmd.ExecuteNonQuery();
            }
            finally
            {
                conn.Close();
            }
        }
    }
}
