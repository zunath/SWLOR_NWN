using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.MigrationService;
using Exception = System.Exception;

namespace SWLOR.Game.Server.Service
{
    public static class Migration
    {
        private static readonly Dictionary<int, IServerMigration> _serverMigrations = new();
        private static readonly Dictionary<int, IPlayerMigration> _playerMigrations = new();

        [NWNEventHandler("db_loaded")]
        public static void LoadMigrations()
        {
            LoadServerMigrations();
            LoadPlayerMigrations();

            RunServerMigrations();
        }

        private static void RunServerMigrations()
        {
            var sw = new Stopwatch();
            var serverConfig = DB.Get<ServerConfiguration>("SWLOR_CONFIG") ?? new ServerConfiguration();
            var migrationVersion = serverConfig.MigrationVersion;
            var migrations = _serverMigrations
                .Where(x => x.Key > migrationVersion)
                .OrderBy(o => o.Key)
                .Select(s => s.Value);
            var newVersion = serverConfig.MigrationVersion;

            foreach (var migration in migrations)
            {
                sw.Reset();
                try
                {
                    sw.Start();
                    migration.Migrate();
                    newVersion = migration.Version;
                    sw.Stop();
                    Log.Write(LogGroup.Migration, $"Server migration #{migration.Version} completed successfully. (Took {sw.ElapsedMilliseconds}ms)", true);
                }
                catch (Exception ex)
                {
                    // It's dangerous to proceed without a successful migration. Shut down the server in this situation.
                    Log.Write(LogGroup.Error, $"Server migration #{migration.Version} failed to apply. Exception: {ex.ToMessageAndCompleteStacktrace()}", true);
                    AdministrationPlugin.ShutdownServer();
                    break;
                }
            }
            
            // Migrations can edit the server configuration entity. Refresh it before updating the version.
            serverConfig = DB.Get<ServerConfiguration>("SWLOR_CONFIG") ?? new ServerConfiguration();
            serverConfig.MigrationVersion = newVersion;
            DB.Set(serverConfig);
        }

        /// <summary>
        /// When a player logs into the server and after initialization has run, run the migration process on their character.
        /// </summary>
        [NWNEventHandler("char_init_after")]
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

                _serverMigrations.Add(instance.Version, instance);
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
