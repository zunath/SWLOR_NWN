using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NWN.Native.API;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Service.LogService;

namespace SWLOR.Game.Server.Service.MigrationService;

/// <summary>
/// Runs the character migrations over every saved character in the server vault at boot, before players can
/// connect, so characters whose players have not logged in since a release are upgraded too. Files are read,
/// converted and written on worker threads; records are loaded and saved on the main thread.
/// </summary>
internal static class CharacterFileUpgrade
{
    /// <summary>Files without the checkpoint predate it; their record version stands in, up to this version.</summary>
    private const int LastPlayerVersionWithoutFileCheckpoint = 12;

    private static readonly Lazy<ICharacterMigration[]> _migrations = new(() => typeof(ICharacterMigration).Assembly.GetTypes()
        .Where(type => !type.IsAbstract && !type.IsInterface && typeof(ICharacterMigration).IsAssignableFrom(type))
        .Select(type => (ICharacterMigration)Activator.CreateInstance(type))
        .OrderBy(migration => migration.Version)
        .ToArray());

    internal static IReadOnlyList<ICharacterMigration> Migrations => _migrations.Value;

    internal static int LatestVersion => Migrations.Count == 0 ? 0 : Migrations[^1].Version;

    /// <summary>The checkpoint saved in a character file: its root PlayerFileVer field.</summary>
    internal static int ReadFileVersion(SavedCharacter character) => character.ReadInteger(PlayerFileCheckpoint.FieldName);

    /// <summary>
    /// The version a character file has reached: its checkpoint, or for a file without one, its record's
    /// version up to the last version that predates checkpoints.
    /// </summary>
    internal static int ResolveFileVersion(int checkpoint, int recordVersion) =>
        checkpoint != 0 ? checkpoint : Math.Min(recordVersion, LastPlayerVersionWithoutFileCheckpoint);

    /// <summary>True when a character file (by its checkpoint) and its record have both reached the latest version.</summary>
    internal static bool IsCurrent(int checkpoint, Player record) =>
        record.Version >= LatestVersion && ResolveFileVersion(checkpoint, record.Version) >= LatestVersion;

    /// <summary>
    /// Applies every migration the file or the record is behind on. The file and the record keep their own
    /// checkpoints, as at login: a migration changes the file when the file is behind it and the record when
    /// the record is. Returns null when the character is current.
    /// </summary>
    internal static CharacterUpgradeResult Upgrade(byte[] file, Player record, Beast activeBeast = null)
    {
        var character = new SavedCharacter(file, record) { ActiveBeast = activeBeast };
        var fileVersion = ResolveFileVersion(ReadFileVersion(character), record.Version);
        var start = Math.Min(record.Version, fileVersion);
        if (start >= LatestVersion)
            return null;

        // Loading a character changes some saved items before login-time migrations saw them.
        Feature.MigrationDefinition.CharacterMigration.SavedCharacterLoad.Apply(character);

        foreach (var migration in Migrations.Where(migration => migration.Version > start))
        {
            if (fileVersion < migration.Version)
            {
                character.AppliesRecordEffects = record.CharacterFileVersion < migration.Version;
                migration.Migrate(character);
                fileVersion = migration.Version;
            }
            if (record.Version < migration.Version)
            {
                migration.MigratePlayerData(record);
                record.Version = migration.Version;
            }
        }

        character.WriteInteger(PlayerFileCheckpoint.FieldName, fileVersion);
        record.CharacterFileVersion = Math.Max(record.CharacterFileVersion, fileVersion);
        record.UpgradeNotices ??= new List<string>();
        // A retry after a crash repeats file migrations, and their notices with them.
        foreach (var notice in character.Notices.Where(notice => !record.UpgradeNotices.Contains(notice)))
            record.UpgradeNotices.Add(notice);
        return new CharacterUpgradeResult(character.Serialize(), record, character.Overflow, start);
    }

    /// <summary>
    /// Upgrades every character file in the server vault. Returns false when any character could not be
    /// upgraded; those keep their saved state and are tried again at the next boot.
    /// </summary>
    internal static bool UpgradeServerVault()
    {
        var config = DB.Get<ServerConfiguration>("SWLOR_CONFIG");
        if (config != null && config.CharacterFileVersion >= LatestVersion)
            return true;

        var vault = GetServerVaultDirectory();
        if (!Directory.Exists(vault))
        {
            Log.Write(LogGroup.Migration, $"Character upgrade: no server vault at '{vault}'.", true);
            return true;
        }

        var stopwatch = Stopwatch.StartNew();
        SavedCharacterData.Load();
        var files = Directory.EnumerateFiles(vault, "*.bic", SearchOption.AllDirectories).ToArray();
        Log.Write(LogGroup.Migration, $"Character upgrade to version {LatestVersion}: checking {files.Length} character files in '{vault}'.", true);

        // The server image links its run folder's servervault to the mounted home; keep backups beside the real folder.
        var vaultRoot = Path.TrimEndingDirectorySeparator(vault);
        var realVault = new DirectoryInfo(vaultRoot).ResolveLinkTarget(true)?.FullName ?? vaultRoot;
        var backupDirectory = Path.Combine(Path.GetDirectoryName(realVault) ?? realVault,
            "servervault_backups", $"character-upgrade-{LatestVersion}-{DateTime.UtcNow:yyyyMMddHHmmss}");
        var counts = new UpgradeCounts();
        // Files that share a record wait for a later round, so each converts from the record the previous one saved.
        var queue = files;
        while (queue.Length > 0)
        {
            var seen = new HashSet<string>();
            var deferred = new List<string>();
            foreach (var batch in queue.Chunk(BatchSize))
            {
                UpgradeBatch(batch, vault, backupDirectory, seen, deferred, counts);
                Log.Write(LogGroup.Migration, $"Character upgrade: {counts.Done}/{files.Length} files ({counts.Upgraded} upgraded, " +
                    $"{counts.Current} current, {counts.Skipped} without an initialized record, {counts.Failed} failed) in {stopwatch.Elapsed:hh\\:mm\\:ss}.", true);
            }
            queue = deferred.ToArray();
        }

        Log.Write(LogGroup.Migration, $"Character upgrade to version {LatestVersion} finished in {stopwatch.ElapsedMilliseconds}ms: " +
            $"{counts.Upgraded} upgraded, {counts.Current} current, {counts.Skipped} without an initialized record, {counts.Failed} failed, " +
            $"{counts.Overflowed} items moved to overflow storage.", true);
        if (counts.Failed > 0)
            return false;

        config ??= new ServerConfiguration();
        config.CharacterFileVersion = LatestVersion;
        DB.Set(config);
        return true;
    }

    private const int BatchSize = 500;

    private static void UpgradeBatch(string[] batch, string vault, string backupDirectory, HashSet<string> seen,
        List<string> deferred, UpgradeCounts counts)
    {
        var pending = new CharacterFile[batch.Length];
        Parallel.For(0, batch.Length, index => pending[index] = CharacterFile.Read(batch[index]));

        // DB is not thread-safe: load records here and give each conversion its own copy.
        foreach (var character in pending)
        {
            if (character.Error != null || character.IsDM || string.IsNullOrEmpty(character.Id))
                continue;
            if (!seen.Add(character.Id))
            {
                character.Deferred = true;
                deferred.Add(character.Path);
                continue;
            }
            var record = DB.Get<Player>(character.Id);
            // A resumed file may still be behind a newer release; it is upgraded like any other below.
            if (character.ResumeOrDiscardPendingFile(record))
                Log.Write(LogGroup.Migration, $"Character upgrade finished an interrupted save for '{character.Path}' [{character.Id}].", true);
            // Characters still being created are set up at login (PlayerInitialization), not upgraded.
            if (record == null || record.IsDeleted || record.Version == 0 || record.CharacterInitializationPending)
                continue;
            character.Record = JsonConvert.DeserializeObject<Player>(JsonConvert.SerializeObject(record));
            if (!string.IsNullOrWhiteSpace(record.ActiveBeastId))
                character.ActiveBeast = DB.Get<Beast>(record.ActiveBeastId);
        }

        Parallel.For(0, pending.Length, index => pending[index].Convert());

        foreach (var character in pending)
        {
            if (character.Deferred)
                continue;
            if (character.Error != null)
            {
                counts.Failed++;
                Log.Write(LogGroup.Migration, $"Character upgrade failed for '{character.Path}' [{character.Id}]: {character.Error}", true);
                continue;
            }
            if (character.Record == null)
            {
                counts.Skipped++;
                continue;
            }
            if (character.Result == null)
            {
                counts.Current++;
                continue;
            }
            try
            {
                Commit(character, vault, backupDirectory);
                counts.Upgraded++;
                counts.Overflowed += character.Result.Overflow.Count;
            }
            catch (Exception ex)
            {
                counts.Failed++;
                Log.Write(LogGroup.Migration, $"Character upgrade could not save '{character.Path}' [{character.Id}]: {ex.ToMessageAndCompleteStacktrace()}", true);
            }
        }
    }

    private sealed class UpgradeCounts
    {
        public int Upgraded { get; set; }
        public int Current { get; set; }
        public int Skipped { get; set; }
        public int Failed { get; set; }
        public int Overflowed { get; set; }
        public int Done => Upgraded + Current + Skipped + Failed;
    }

    /// <summary>
    /// Keeps the original file as a backup and writes the upgraded file beside it before saving the record, then
    /// moves it into place. A crash after the record is saved leaves that file behind, and the next boot finishes the
    /// move (<see cref="CharacterFile.ResumeOrDiscardPendingFile"/>); a crash before it leaves the record unchanged and
    /// the file is upgraded again. Overflow items use identities derived from the upgrade, so a retry replaces them
    /// instead of adding copies.
    /// </summary>
    private static void Commit(CharacterFile character, string vault, string backupDirectory)
    {
        var result = character.Result;
        for (var index = 0; index < result.Overflow.Count; index++)
        {
            var item = result.Overflow[index];
            DB.Set(new OverflowItem
            {
                Id = $"{character.Id}-{character.FileKey}-{result.FromVersion}-{index}",
                PlayerId = character.Id,
                Name = item.Name,
                Resref = item.Resref,
                Tag = item.Tag,
                Quantity = item.Quantity,
                Data = item.Data,
            });
        }
        var backup = Path.Combine(backupDirectory, Path.GetRelativePath(vault, character.Path));
        Directory.CreateDirectory(Path.GetDirectoryName(backup)!);
        if (!File.Exists(backup))
            File.Copy(character.Path, backup);
        var pending = CharacterFile.PendingPath(character.Path);
        File.WriteAllBytes(pending, result.File);
        DB.Set(result.Record);
        File.Move(pending, character.Path, true);
    }

    private static string GetServerVaultDirectory()
    {
        var configured = Environment.GetEnvironmentVariable("SWLOR_SERVERVAULT_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(configured))
            return configured;
        using var alias = new CExoString("SERVERVAULT");
        return NWNXLib.g_pExoBase.m_pcExoAliasList.GetAliasPath(alias).ToString();
    }

    private sealed class CharacterFile
    {
        public string Path { get; private init; }
        public byte[] Data { get; private set; }

        /// <summary>
        /// A stable identity for this file within the vault, so files that share a record keep separate overflow items.
        /// </summary>
        public string FileKey
        {
            get
            {
                // FNV-1a over the vault folder and file name. A cryptographic hash would load the runtime's OpenSSL
                // bindings into the server process.
                var name = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(Path)) + "/" +
                           System.IO.Path.GetFileName(Path).ToLowerInvariant();
                var hash = 14695981039346656037UL;
                foreach (var b in System.Text.Encoding.UTF8.GetBytes(name))
                    hash = (hash ^ b) * 1099511628211UL;
                return hash.ToString("x16");
            }
        }
        public string Id { get; private init; }
        public bool IsDM { get; private init; }
        public Player Record { get; set; }
        public Beast ActiveBeast { get; set; }
        public bool Deferred { get; set; }
        public CharacterUpgradeResult Result { get; private set; }
        public string Error { get; private set; }

        /// <summary>The upgraded file written beside a character file until its record is saved.</summary>
        public static string PendingPath(string path) => path + ".upgrade";

        /// <summary>The checkpoint of an upgraded file left by an interrupted save; 0 when none or unreadable.</summary>
        private int PendingVersion { get; init; }

        private bool HasPendingFile { get; init; }

        /// <summary>
        /// Main thread. An upgraded file left beside the character file is moved into place when the record already
        /// holds that upgrade, and deleted otherwise so the file is upgraded again. Returns true when it was moved.
        /// </summary>
        public bool ResumeOrDiscardPendingFile(Player record)
        {
            if (!HasPendingFile)
                return false;
            if (record != null && PendingVersion > 0 && record.CharacterFileVersion >= PendingVersion)
            {
                File.Move(PendingPath(Path), Path, true);
                Data = File.ReadAllBytes(Path);
                return true;
            }
            File.Delete(PendingPath(Path));
            return false;
        }

        public static CharacterFile Read(string path)
        {
            try
            {
                var data = File.ReadAllBytes(path);
                var character = new SavedCharacter(data, null);
                var pending = PendingPath(path);
                var hasPending = File.Exists(pending);
                var pendingVersion = 0;
                if (hasPending)
                {
                    try { pendingVersion = ReadFileVersion(new SavedCharacter(File.ReadAllBytes(pending), null)); }
                    catch (Exception) { pendingVersion = 0; }
                }
                return new CharacterFile
                {
                    Path = path,
                    Data = data,
                    Id = character.Id,
                    IsDM = character.ReadInteger("IsDM") != 0,
                    HasPendingFile = hasPending,
                    PendingVersion = pendingVersion,
                };
            }
            catch (Exception ex)
            {
                return new CharacterFile { Path = path, Error = ex.Message };
            }
        }

        public void Convert()
        {
            if (Error != null || Record == null)
                return;
            try
            {
                Result = Upgrade(Data, Record, ActiveBeast);
            }
            catch (Exception ex)
            {
                Error = ex.ToString();
            }
        }
    }
}

/// <summary>An upgraded character: its new file, its record, and the items moved to overflow storage.</summary>
internal sealed record CharacterUpgradeResult(byte[] File, Player Record, IReadOnlyList<SavedOverflowItem> Overflow, int FromVersion);
