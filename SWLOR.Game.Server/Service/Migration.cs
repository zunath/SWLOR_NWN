using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.NWN.API.NWNX;
using SWLOR.Shared.Core.Extension;
using Exception = System.Exception;

namespace SWLOR.Game.Server.Service
{
    public static class Migration
    {
        private static int _currentMigrationVersion;
        private static int _newMigrationVersion;
        private static readonly Dictionary<int, IServerMigration> _serverMigrationsPostDatabase = new();
        private static readonly Dictionary<int, IServerMigration> _serverMigrationsPostCache = new();
        private static readonly Dictionary<int, IPlayerMigration> _playerMigrations = new();

        [NWNEventHandler(ScriptName.OnDatabaseLoaded)]
        public static void AfterDatabaseLoaded()
        {
            var config = GetServerConfiguration();
            _currentMigrationVersion = config.MigrationVersion;

            LoadServerMigrations();
            LoadPlayerMigrations();

            RunServerMigrationsPostDatabase();
        }

        [NWNEventHandler(ScriptName.OnModuleCacheAfter)]
        public static void AfterCacheLoaded()
        {
            RunServerMigrationsPostCache();
            UpdateMigrationVersion();
        }

        private static void UpdateMigrationVersion()
        {
            if (_newMigrationVersion > _currentMigrationVersion)
            {
                var config = GetServerConfiguration();
                config.MigrationVersion = _newMigrationVersion;
                DB.Set(config);
            }
        }

        private static ServerConfiguration GetServerConfiguration()
        {
            return DB.Get<ServerConfiguration>("SWLOR_CONFIG") ?? new ServerConfiguration();
        }

        private static IEnumerable<IServerMigration> GetMigrations(MigrationExecutionType executionType)
        {
            var serverConfig = GetServerConfiguration();
            var migrationVersion = serverConfig.MigrationVersion;
            if (executionType == MigrationExecutionType.PostDatabaseLoad)
            {
                var migrations = _serverMigrationsPostDatabase
                    .Where(x => x.Key > migrationVersion)
                    .OrderBy(o => o.Key)
                    .Select(s => s.Value);

                return migrations;
            }
            else
            {
                var migrations = _serverMigrationsPostCache
                    .Where(x => x.Key > migrationVersion)
                    .OrderBy(o => o.Key)
                    .Select(s => s.Value);

                return migrations;
            }
        }

        private static void RunMigrations(MigrationExecutionType executionType)
        {
            var sw = new Stopwatch();
            var migrations = GetMigrations(executionType);
            var newVersion = 0;

            foreach (var migration in migrations)
            {
                sw.Reset();
                try
                {
                    sw.Start();
                    migration.Migrate();
                    newVersion = migration.Version;
                    sw.Stop();
                    Log.Write(LogGroup.Migration, $"Server migration ({executionType}) #{migration.Version} completed successfully. (Took {sw.ElapsedMilliseconds}ms)", true);
                }
                catch (Exception ex)
                {
                    // It's dangerous to proceed without a successful migration. Shut down the server in this situation.
                    Log.Write(LogGroup.Error, $"Server migration ({executionType}) #{migration.Version} failed to apply. Exception: {ex.ToMessageAndCompleteStacktrace()}. Shutting down server.", true);
                    AdministrationPlugin.ShutdownServer();
                    break;
                }
            }

            if (_newMigrationVersion < newVersion)
                _newMigrationVersion = newVersion;
        }

        private static void RunServerMigrationsPostDatabase()
        {
            RunMigrations(MigrationExecutionType.PostDatabaseLoad);
        }

        public static void RunServerMigrationsPostCache()
        {
            RunMigrations(MigrationExecutionType.PostCacheLoad);
        }

        /// <summary>
        /// When a player logs into the server and after initialization has run, run the migration process on their character.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCharacterInitAfter)]
        public static void RunPlayerMigrations()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var sw = new Stopwatch();
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId) ?? new Player(playerId);

            var migrations = _playerMigrations
                .Where(x => x.Key > dbPlayer.Version)
                .OrderBy(o => o.Key)
                .Select(s => s.Value);
            var newVersion = dbPlayer.Version;

            foreach (var migration in migrations)
            {
                sw.Reset();
                try
                {
                    sw.Start();
                    migration.Migrate(player);
                    newVersion = migration.Version;
                    sw.Stop();
                    Log.Write(LogGroup.Migration, $"Player migration #{migration.Version} applied to player {GetName(player)} [{playerId}] successfully. (Took {sw.ElapsedMilliseconds}ms)");
                }
                catch (Exception ex)
                {
                    Log.Write(LogGroup.Migration, $"Player migration #{migration.Version} failed to apply for player {GetName(player)} [{playerId}]. Exception: {ex.ToMessageAndCompleteStacktrace()}", true);
                    break;
                }
            }

            // Migrations can edit the database player entity. Refresh it before updating the version.
            dbPlayer = DB.Get<Player>(playerId) ?? new Player(playerId);
            dbPlayer.Version = newVersion;
            DB.Set(dbPlayer);
        }

        private static void LoadServerMigrations()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(IServerMigration).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in types)
            {
                var instance = (IServerMigration)Activator.CreateInstance(type);

                if(instance.ExecutionType == MigrationExecutionType.PostDatabaseLoad)
                    _serverMigrationsPostDatabase.Add(instance.Version, instance);
                else 
                    _serverMigrationsPostCache.Add(instance.Version, instance);
            }
        }

        private static void LoadPlayerMigrations()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(IPlayerMigration).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in types)
            {
                var instance = (IPlayerMigration)Activator.CreateInstance(type);

                _playerMigrations.Add(instance.Version, instance);
            }
        }

        /// <summary>
        /// Retrieves the latest migration version for players.
        /// </summary>
        /// <returns>The latest migration version for players.</returns>
        public static int GetLatestPlayerVersion()
        {
            return _playerMigrations.Max(m => m.Key);
        }

    }
}
