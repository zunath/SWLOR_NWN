using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.NWN.API.NWNX;
using NWNXLib = NWN.Native.API.NWNXLib;
using Exception = System.Exception;

namespace SWLOR.Game.Server.Service
{
    public static class Migration
    {
        private const int ConsoleProgressMigrationVersion = 22;

        /// <summary>
        /// Server migrations up to this version ran on production and were removed. A new database starts
        /// here; a database below it predates this build and cannot be upgraded by it.
        /// </summary>
        internal const int RetiredServerMigrationVersion = 28;
        internal const string PlayerFileVersionVariable = "PLAYER_MIGRATION_VERSION";
        private static int _currentMigrationVersion;
        private static ServerMigrationState _serverMigrationState = new(0);
        private static readonly Dictionary<int, IServerMigration> _serverMigrationsPostDatabase = new();
        private static readonly Dictionary<int, IServerMigration> _serverMigrationsPostCache = new();

        [NWNEventHandler(ScriptName.OnDatabaseLoaded)]
        public static void AfterDatabaseLoaded()
        {
            var stored = DB.Get<ServerConfiguration>("SWLOR_CONFIG");
            var config = ResolveConfiguration(stored);
            if (config == null)
            {
                _serverMigrationState = new ServerMigrationState(stored.MigrationVersion);
                _serverMigrationState.MarkFailed();
                Log.Write(LogGroup.Error, $"The database is at server migration {stored.MigrationVersion}, but this build only upgrades " +
                    $"databases at {RetiredServerMigrationVersion} or later. Upgrade it with an older build first. Shutting down server.", true);
                AdministrationPlugin.ShutdownServer();
                return;
            }
            if (stored == null)
                DB.Set(config);

            _currentMigrationVersion = config.MigrationVersion;
            _serverMigrationState = new ServerMigrationState(_currentMigrationVersion);

            LoadServerMigrations();

            RunServerMigrationsPostDatabase();
        }

        [NWNEventHandler(ScriptName.OnModuleCacheAfter)]
        public static void AfterCacheLoaded()
        {
            RunServerMigrationsPostCache();
            UpdateMigrationVersion();
            UpgradeCharacters();
        }

        /// <summary>
        /// Upgrades every saved character before players can connect, including characters whose players have
        /// not logged in since a release. A character that fails keeps its saved state, is tried again at the
        /// next boot, and cannot log in until then.
        /// </summary>
        private static void UpgradeCharacters()
        {
            if (_serverMigrationState.Failed)
                return;

            try
            {
                CharacterFileUpgrade.UpgradeServerVault();
            }
            catch (Exception ex)
            {
                Log.Write(LogGroup.Error, $"The character upgrade failed: {ex.ToMessageAndCompleteStacktrace()}. Characters it did not upgrade cannot log in.", true);
            }
        }

        private static void UpdateMigrationVersion()
        {
            if (!_serverMigrationState.Failed && _serverMigrationState.CompletedVersion > _currentMigrationVersion)
            {
                var config = GetServerConfiguration();
                config.MigrationVersion = _serverMigrationState.CompletedVersion;
                DB.Set(config);
            }
        }

        /// <summary>
        /// A new database starts at the retired version, so removed migrations never run; a database below it
        /// returns null because this build no longer has the migrations it needs.
        /// </summary>
        internal static ServerConfiguration ResolveConfiguration(ServerConfiguration stored)
        {
            if (stored == null)
                return new ServerConfiguration { MigrationVersion = RetiredServerMigrationVersion };
            return stored.MigrationVersion < RetiredServerMigrationVersion ? null : stored;
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
            if (_serverMigrationState.Failed)
                return;

            var sw = new Stopwatch();
            var migrations = GetMigrations(executionType).ToList();

            for (var index = 0; index < migrations.Count; index++)
            {
                var migration = migrations[index];
                sw.Reset();
                try
                {
                    if (migration.Version >= ConsoleProgressMigrationVersion)
                    {
                        Log.Write(
                            LogGroup.Migration,
                            $"Starting server migration ({executionType}) #{migration.Version}." +
                            (migration.Version >= 24
                                ? $" {migration.GetType().Name}; {migrations.Count - index - 1} migrations remain after this in {executionType}."
                                : string.Empty),
                            true);
                    }

                    sw.Start();
                    _serverMigrationState.Run(migration, pending => pending.Migrate());
                    sw.Stop();

                    Log.Write(LogGroup.Migration, $"Server migration ({executionType}) #{migration.Version} completed successfully. (Took {sw.ElapsedMilliseconds}ms)", true);
                }
                catch (Exception ex)
                {
                    _serverMigrationState.MarkFailed();
                    // It's dangerous to proceed without a successful migration. Shut down the server in this situation.
                    Log.Write(LogGroup.Error, $"Server migration ({executionType}) #{migration.Version} failed to apply. Exception: {ex.ToMessageAndCompleteStacktrace()}. Shutting down server.", true);
                    AdministrationPlugin.ShutdownServer();
                    break;
                }
            }

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
        /// Character migrations run on saved characters at boot (<see cref="CharacterFileUpgrade"/>). A character
        /// still behind the latest version was not upgraded, so it is refused instead of played in an outdated state.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCharacterInitAfter)]
        public static void AfterCharacterInitialization()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var checkpoint = GetLocalInt(player, PlayerFileVersionVariable);
            if (dbPlayer == null || !CharacterFileUpgrade.IsCurrent(checkpoint, dbPlayer))
            {
                var fileVersion = dbPlayer == null ? checkpoint : CharacterFileUpgrade.ResolveFileVersion(checkpoint, dbPlayer.Version);
                Log.Write(LogGroup.Migration, $"Refusing login for {GetName(player)} [{playerId}]: the character is at version {fileVersion} " +
                    $"and its record at {dbPlayer?.Version}, but this server requires {GetLatestPlayerVersion()}. It is upgraded at the next boot.", true);
                // A file restored or imported after the vault was marked current is upgraded at the next boot.
                if (dbPlayer != null)
                    CharacterFileUpgrade.RequestVaultRescan();
                BootPC(player, "Your character needs an update that runs when the server restarts. Please try again after the next restart, or contact a server administrator.");
                return;
            }

            DeliverUpgradeNotices(player, dbPlayer);
            OverflowStorage.NotifyOnLogin(player);

            // Restore the base attributes and native feats the record holds, which saved characters may lack.
            Stat.RestorePlayerAttributes(player);
            Perk.RestorePlayerFeats(player);
            // Saved equipment does not emit equip events on login.
            Stat.RefreshCombatReadinessEquipment(player);
            Stat.RefreshPlayerMaxHP(player);
            Stat.RefreshPlayerEquipmentStats(player);
        }

        private static void DeliverUpgradeNotices(uint player, Player dbPlayer)
        {
            if (dbPlayer.UpgradeNotices == null || dbPlayer.UpgradeNotices.Count == 0)
                return;

            foreach (var notice in dbPlayer.UpgradeNotices)
                SendMessageToPC(player, notice);
            dbPlayer.UpgradeNotices.Clear();
            DB.Set(dbPlayer);
        }

        /// <summary>
        /// Saves the character file at a checkpoint now, before its record is saved as complete.
        /// The ordinary export command queues a later save and cannot establish this ordering.
        /// </summary>
        internal static void SavePlayerFileCheckpoint(uint player, int version)
        {
            var previousVersion = GetLocalInt(player, PlayerFileVersionVariable);
            SetLocalInt(player, PlayerFileVersionVariable, version);
            try
            {
                var client = NWNXLib.g_pAppManager.m_pServerExoApp.GetClientObjectByObjectId(player);
                if (client == null || client.SaveServerCharacter() == 0)
                    throw new InvalidOperationException("The migrated character file could not be saved.");
            }
            catch
            {
                if (previousVersion == 0)
                    DeleteLocalInt(player, PlayerFileVersionVariable);
                else
                    SetLocalInt(player, PlayerFileVersionVariable, previousVersion);
                throw;
            }
        }

        /// <summary>Retains initialization intent until both the starter character and its record are saved.</summary>
        internal static Player RunPlayerInitialization(Player original, Action<Player> initialize,
            Action<Player> savePlayer, Func<int> loadFileVersion, Action<int> saveFileVersion)
        {
            Player Copy(Player value) => JsonConvert.DeserializeObject<Player>(JsonConvert.SerializeObject(value));
            var pending = Copy(original);
            if (!pending.CharacterInitializationPending || pending.Version <= 0 || loadFileVersion() < pending.Version)
            {
                pending.CharacterInitializationPending = true;
                savePlayer(pending);
                // Initialization mutates many fields; don't expose partial results through the shared DB cache.
                pending = Copy(pending);
                initialize(pending);
                savePlayer(pending);
                saveFileVersion(pending.Version);
            }

            var completed = Copy(pending);
            completed.CharacterInitializationPending = false;
            savePlayer(completed);
            return completed;
        }

        private static void LoadServerMigrations()
        {
            _serverMigrationsPostDatabase.Clear();
            _serverMigrationsPostCache.Clear();
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

        /// <summary>
        /// Retrieves the latest migration version for players.
        /// </summary>
        /// <returns>The latest migration version for players.</returns>
        public static int GetLatestPlayerVersion()
        {
            return CharacterFileUpgrade.LatestVersion;
        }

    }
}
