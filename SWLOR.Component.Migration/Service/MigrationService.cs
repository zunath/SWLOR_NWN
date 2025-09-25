using System.Diagnostics;
using SWLOR.Component.Migration.Contracts;
using SWLOR.Component.Migration.Entity;
using SWLOR.Component.Migration.Enums;
using SWLOR.NWN.API.NWNX;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Extension;
using SWLOR.Shared.Core.Log.LogGroup;
using SWLOR.Shared.Domain.Character.Entities;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Infrastructure;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Events.NWNX;
using Exception = System.Exception;

namespace SWLOR.Component.Migration.Service
{
    public class MigrationService : IMigrationService
    {
        private readonly ILogger _logger;
        private readonly IDatabaseService _db;
        private static int _currentMigrationVersion;
        private static int _newMigrationVersion;
        private static readonly Dictionary<int, IServerMigration> _serverMigrationsPostDatabase = new();
        private static readonly Dictionary<int, IServerMigration> _serverMigrationsPostCache = new();
        private static readonly Dictionary<int, IPlayerMigration> _playerMigrations = new();

        public MigrationService(ILogger logger, IDatabaseService db)
        {
            _logger = logger;
            _db = db;
        }

        public void AfterDatabaseLoaded()
        {
            var config = GetServerConfiguration();
            _currentMigrationVersion = config.MigrationVersion;

            LoadServerMigrations();
            LoadPlayerMigrations();

            RunServerMigrationsPostDatabase();
        }

        public void AfterCacheLoaded()
        {
            RunServerMigrationsPostCache();
            UpdateMigrationVersion();
        }

        private void UpdateMigrationVersion()
        {
            if (_newMigrationVersion > _currentMigrationVersion)
            {
                var config = GetServerConfiguration();
                config.MigrationVersion = _newMigrationVersion;
                _db.Set(config);
            }
        }

        private ServerMigrationStatus GetServerConfiguration()
        {
            return _db.Get<ServerMigrationStatus>(ServerMigrationStatus.DefaultId) ?? new ServerMigrationStatus();
        }

        private IEnumerable<IServerMigration> GetMigrations(MigrationExecutionType executionType)
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

        private void RunMigrations(MigrationExecutionType executionType)
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
                    _logger.Write<MigrationLogGroup>($"Server migration ({executionType}) #{migration.Version} completed successfully. (Took {sw.ElapsedMilliseconds}ms)");
                }
                catch (Exception ex)
                {
                    // It's dangerous to proceed without a successful migration. Shut down the server in this situation.
                    _logger.Write<ErrorLogGroup>($"Server migration ({executionType}) #{migration.Version} failed to apply. Exception: {ex.ToMessageAndCompleteStacktrace()}. Shutting down server.");
                    AdministrationPlugin.ShutdownServer();
                    break;
                }
            }

            if (_newMigrationVersion < newVersion)
                _newMigrationVersion = newVersion;
        }

        private void RunServerMigrationsPostDatabase()
        {
            RunMigrations(MigrationExecutionType.PostDatabaseLoad);
        }

        public void RunServerMigrationsPostCache()
        {
            RunMigrations(MigrationExecutionType.PostCacheLoad);
        }

        /// <summary>
        /// When a player logs into the server and after initialization has run, run the migration process on their character.
        /// </summary>
        public void RunPlayerMigrations()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var sw = new Stopwatch();
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId) ?? new Player(playerId);

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
                    _logger.Write<MigrationLogGroup>($"Player migration #{migration.Version} applied to player {GetName(player)} [{playerId}] successfully. (Took {sw.ElapsedMilliseconds}ms)");
                }
                catch (Exception ex)
                {
                    _logger.Write<MigrationLogGroup>($"Player migration #{migration.Version} failed to apply for player {GetName(player)} [{playerId}]. Exception: {ex.ToMessageAndCompleteStacktrace()}");
                    break;
                }
            }

            // Migrations can edit the database player entity. Refresh it before updating the version.
            dbPlayer = _db.Get<Player>(playerId) ?? new Player(playerId);
            dbPlayer.Version = newVersion;
            _db.Set(dbPlayer);
        }

        private void LoadServerMigrations()
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

        private void LoadPlayerMigrations()
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
        public int GetLatestPlayerVersion()
        {
            return _playerMigrations.Max(m => m.Key);
        }

    }
}
