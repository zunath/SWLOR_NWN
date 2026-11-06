using System;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Domain.Core
{
    /// <summary>
    /// Runs the server migrations against the database at boot, upgrades the saved characters, and guards the login of
    /// a character that is not current. The static facade <c>Service.Migration</c> keeps the event handlers and forwards
    /// every member to the container's instance of this interface.
    /// </summary>
    public interface IMigrationService
    {
        /// <summary>The game data the saved-character migrations read (the static <c>SavedCharacterData</c> reads it here).</summary>
        SavedCharacterDataStore CharacterData { get; }

        /// <summary>
        /// Resolves the stored server configuration, loads the server migrations and runs those that execute after the
        /// database loads. A database below the retired version shuts the server down.
        /// </summary>
        void AfterDatabaseLoaded();

        /// <summary>Runs the server migrations that execute after the module cache loads, records the new version and upgrades the saved characters.</summary>
        void AfterCacheLoaded();

        /// <summary>Runs the server migrations that execute after the module cache loads.</summary>
        void RunServerMigrationsPostCache();

        /// <summary>
        /// Character migrations run on saved characters at boot. A character still behind the latest version was not
        /// upgraded, so it is refused instead of played in an outdated state.
        /// </summary>
        void AfterCharacterInitialization();

        /// <summary>
        /// A new database starts at the retired version, so removed migrations never run; a database below it
        /// returns null because this build no longer has the migrations it needs.
        /// </summary>
        ServerConfiguration ResolveConfiguration(ServerConfiguration stored);

        /// <summary>Saves the character file at a checkpoint now, before its record is saved as complete.</summary>
        void SavePlayerFileCheckpoint(uint player, int version);

        /// <summary>Retains initialization intent until both the starter character and its record are saved.</summary>
        Player RunPlayerInitialization(Player original, Action<Player> initialize,
            Action<Player> savePlayer, Func<int> loadFileVersion, Action<int> saveFileVersion);

        /// <summary>Retrieves the latest migration version for players.</summary>
        int GetLatestPlayerVersion();

        /// <summary>
        /// Upgrades every character file in the server vault. Returns false when any character could not be
        /// upgraded; those keep their saved state and are tried again at the next boot.
        /// </summary>
        bool UpgradeServerVault();

        /// <summary>
        /// Makes the next boot read the whole vault again, for a character file that was restored or imported after the
        /// vault was last marked current.
        /// </summary>
        void RequestVaultRescan();
    }
}
