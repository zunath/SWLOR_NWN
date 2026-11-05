using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the server and player data migrations. The logic lives in <see cref="IMigrationService"/>;
    /// the event handlers stay here and every member forwards to the container's instance.
    /// </summary>
    public static class Migration
    {
        /// <summary>
        /// Server migrations up to this version ran on production and were removed. A new database starts
        /// here; a database below it predates this build and cannot be upgraded by it.
        /// </summary>
        internal const int RetiredServerMigrationVersion = Domain.Core.MigrationService.RetiredServerMigrationVersion;
        internal const string PlayerFileVersionVariable = Domain.Core.MigrationService.PlayerFileVersionVariable;

        private static IMigrationService Instance => FacadeAccessor.Resolve<IMigrationService>();

        /// <summary>The game data the saved-character migrations read (the static <c>SavedCharacterData</c> reads it here).</summary>
        internal static SavedCharacterDataStore CharacterData => Instance.CharacterData;

        [NWNEventHandler(ScriptName.OnDatabaseLoaded)]
        public static void AfterDatabaseLoaded()
        {
            Instance.AfterDatabaseLoaded();
        }

        [NWNEventHandler(ScriptName.OnModuleCacheAfter)]
        public static void AfterCacheLoaded()
        {
            Instance.AfterCacheLoaded();
        }

        /// <summary>
        /// A new database starts at the retired version, so removed migrations never run; a database below it
        /// returns null because this build no longer has the migrations it needs.
        /// </summary>
        internal static ServerConfiguration ResolveConfiguration(ServerConfiguration stored)
        {
            return Instance.ResolveConfiguration(stored);
        }

        public static void RunServerMigrationsPostCache()
        {
            Instance.RunServerMigrationsPostCache();
        }

        /// <summary>
        /// Character migrations run on saved characters at boot (<see cref="CharacterFileUpgrade"/>). A character
        /// still behind the latest version was not upgraded, so it is refused instead of played in an outdated state.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCharacterInitAfter)]
        public static void AfterCharacterInitialization()
        {
            Instance.AfterCharacterInitialization();
        }

        /// <summary>
        /// Saves the character file at a checkpoint now, before its record is saved as complete.
        /// The ordinary export command queues a later save and cannot establish this ordering.
        /// </summary>
        internal static void SavePlayerFileCheckpoint(uint player, int version)
        {
            Instance.SavePlayerFileCheckpoint(player, version);
        }

        /// <summary>Retains initialization intent until both the starter character and its record are saved.</summary>
        internal static Player RunPlayerInitialization(Player original, Action<Player> initialize,
            Action<Player> savePlayer, Func<int> loadFileVersion, Action<int> saveFileVersion)
        {
            return Instance.RunPlayerInitialization(original, initialize, savePlayer, loadFileVersion, saveFileVersion);
        }

        /// <summary>
        /// Retrieves the latest migration version for players.
        /// </summary>
        /// <returns>The latest migration version for players.</returns>
        public static int GetLatestPlayerVersion()
        {
            return Instance.GetLatestPlayerVersion();
        }

        /// <summary>
        /// Upgrades every character file in the server vault. Returns false when any character could not be
        /// upgraded; those keep their saved state and are tried again at the next boot.
        /// </summary>
        internal static bool UpgradeServerVault()
        {
            return Instance.UpgradeServerVault();
        }

        /// <summary>
        /// Makes the next boot read the whole vault again, for a character file that was restored or imported after the
        /// vault was last marked current.
        /// </summary>
        internal static void RequestVaultRescan()
        {
            Instance.RequestVaultRescan();
        }
    }
}
