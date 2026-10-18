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
    /// Applies every migration the file or the record is behind on. The file and the record keep their own
    /// checkpoints, as at login: a migration changes the file when the file is behind it and the record when
    /// the record is. Returns null when the character is current.
    /// </summary>
    internal static CharacterUpgradeResult Upgrade(byte[] file, Player record)
    {
        var character = new SavedCharacter(file, record);
        var fileVersion = ReadFileVersion(character);
        if (fileVersion == 0)
            fileVersion = Math.Min(record.Version, LastPlayerVersionWithoutFileCheckpoint);
        var start = Math.Min(record.Version, fileVersion);
        if (start >= LatestVersion)
            return null;

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

        var backupDirectory = Path.Combine(Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(vault)) ?? vault,
            "servervault_backups", $"character-upgrade-{LatestVersion}-{DateTime.UtcNow:yyyyMMddHHmmss}");
        var upgraded = 0;
        var current = 0;
        var skipped = 0;
        var failed = 0;
        var overflowed = 0;
        const int BatchSize = 500;
        foreach (var batch in files.Chunk(BatchSize))
        {
            var pending = new CharacterFile[batch.Length];
            Parallel.For(0, batch.Length, index => pending[index] = CharacterFile.Read(batch[index]));

            // DB is not thread-safe: load records here and give each conversion its own copy.
            foreach (var character in pending)
            {
                if (character.Error != null || character.IsDM || string.IsNullOrEmpty(character.Id))
                    continue;
                var record = DB.Get<Player>(character.Id);
                if (record == null || record.IsDeleted)
                    continue;
                character.Record = JsonConvert.DeserializeObject<Player>(JsonConvert.SerializeObject(record));
            }

            Parallel.For(0, pending.Length, index => pending[index].Convert());

            foreach (var character in pending)
            {
                if (character.Error != null)
                {
                    failed++;
                    Log.Write(LogGroup.Migration, $"Character upgrade failed for '{character.Path}' [{character.Id}]: {character.Error}", true);
                    continue;
                }
                if (character.Record == null)
                {
                    skipped++;
                    continue;
                }
                if (character.Result == null)
                {
                    current++;
                    continue;
                }
                try
                {
                    Commit(character, vault, backupDirectory);
                    upgraded++;
                    overflowed += character.Result.Overflow.Count;
                }
                catch (Exception ex)
                {
                    failed++;
                    Log.Write(LogGroup.Migration, $"Character upgrade could not save '{character.Path}' [{character.Id}]: {ex.ToMessageAndCompleteStacktrace()}", true);
                }
            }
            Log.Write(LogGroup.Migration, $"Character upgrade: {upgraded + current + skipped + failed}/{files.Length} files " +
                $"({upgraded} upgraded, {current} current, {skipped} without a record, {failed} failed) in {stopwatch.Elapsed:hh\\:mm\\:ss}.", true);
        }

        Log.Write(LogGroup.Migration, $"Character upgrade to version {LatestVersion} finished in {stopwatch.ElapsedMilliseconds}ms: " +
            $"{upgraded} upgraded, {current} current, {skipped} without a record, {failed} failed, {overflowed} items moved to overflow storage.", true);
        if (failed > 0)
            return false;

        config ??= new ServerConfiguration();
        config.CharacterFileVersion = LatestVersion;
        DB.Set(config);
        return true;
    }

    /// <summary>
    /// Saves the record before the file. A crash between the two leaves a record whose
    /// <see cref="Player.CharacterFileVersion"/> is ahead of its file, so the retry changes the file only.
    /// Overflow items use identities derived from the upgrade, so a retry replaces them instead of adding copies.
    /// </summary>
    private static void Commit(CharacterFile character, string vault, string backupDirectory)
    {
        var result = character.Result;
        for (var index = 0; index < result.Overflow.Count; index++)
        {
            var item = result.Overflow[index];
            DB.Set(new OverflowItem
            {
                Id = $"{character.Id}-{result.FromVersion}-{index}",
                PlayerId = character.Id,
                Name = item.Name,
                Resref = item.Resref,
                Tag = item.Tag,
                Quantity = item.Quantity,
                Data = item.Data,
            });
        }
        DB.Set(result.Record);

        var backup = Path.Combine(backupDirectory, Path.GetRelativePath(vault, character.Path));
        Directory.CreateDirectory(Path.GetDirectoryName(backup)!);
        if (!File.Exists(backup))
            File.Copy(character.Path, backup);
        var temporary = character.Path + ".upgrade";
        File.WriteAllBytes(temporary, result.File);
        File.Move(temporary, character.Path, true);
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
        public byte[] Data { get; private init; }
        public string Id { get; private init; }
        public bool IsDM { get; private init; }
        public Player Record { get; set; }
        public CharacterUpgradeResult Result { get; private set; }
        public string Error { get; private set; }

        public static CharacterFile Read(string path)
        {
            try
            {
                var data = File.ReadAllBytes(path);
                var character = new SavedCharacter(data, null);
                return new CharacterFile
                {
                    Path = path,
                    Data = data,
                    Id = character.Id,
                    IsDM = character.ReadInteger("IsDM") != 0,
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
                Result = Upgrade(Data, Record);
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
