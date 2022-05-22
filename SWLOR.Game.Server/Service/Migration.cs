using System;
using System.Collections.Generic;
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
            var serverConfig = DB.Get<ServerConfiguration>("SWLOR_CONFIG") ?? new ServerConfiguration();
            var migrations = _serverMigrations
                .Where(x => x.Key > serverConfig.MigrationVersion)
                .OrderBy(o => o.Key)
                .Select(s => s.Value);

            foreach (var migration in migrations)
            {
                try
                {
                    migration.Migrate();
                    serverConfig.MigrationVersion = migration.Version;
                    Log.Write(LogGroup.Migration, $"Server migration #{migration.Version} completed successfully.", true);
                }
                catch (Exception ex)
                {
                    // It's dangerous to proceed without a successful migration. Shut down the server in this situation.
                    Log.Write(LogGroup.Error, $"Server migration #{migration.Version} failed to apply. Exception: {ex.ToMessageAndCompleteStacktrace()}", true);
                    AdministrationPlugin.ShutdownServer();
                    break;
                }
            }
            
            DB.Set(serverConfig);
        }

        /// <summary>
        /// When a player logs into the server, run the migration process on their character.
        /// </summary>
        [NWNEventHandler("mod_enter")]
        public static void RunPlayerMigrations()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId) ?? new Player(playerId);

            var migrations = _playerMigrations
                .Where(x => x.Key > dbPlayer.Version)
                .OrderBy(o => o.Key)
                .Select(s => s.Value);

            foreach (var migration in migrations)
            {
                try
                {
                    migration.Migrate(player);
                    dbPlayer.Version = migration.Version;
                    Log.Write(LogGroup.Migration, $"Player migration #{migration.Version} applied to player {GetName(player)} [{playerId}] successfully.");
                }
                catch (Exception ex)
                {
                    Log.Write(LogGroup.Migration, $"Player migration #{migration.Version} failed to apply for player {GetName(player)} [{playerId}]. Exception: {ex.ToMessageAndCompleteStacktrace()}", true);
                    break;
                }
            }

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

    }
}
