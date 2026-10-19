using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class CharacterFileUpgradeEngineTests
    {
        private const string VaultVariable = "SWLOR_SERVERVAULT_DIRECTORY";

        /// <summary>
        /// The boot-time pass over a server vault: it upgrades a character file and its record together, keeps the
        /// original file as a backup, stores gear without room in overflow storage, and skips the vault once every
        /// character is current. A save interrupted after the record was written is finished at the next boot, and
        /// one interrupted before it is redone without adding overflow items twice.
        /// </summary>
        [EngineTest("Boot-time character upgrade saves files, records, backups and overflow storage once",
            Category = "CharacterFileUpgrade", TimeoutSeconds = 300)]
        public static async Task UpgradesServerVault(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var owner = fixture.Creature;
            await ctx.EquipItemAsync(owner, "nw_wswls001", InventorySlot.RightHand);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                // A 2x3 outfit fills six cells; sixty of them cover the 10x36 inventory a character file holds.
                for (var filler = 0; filler < 60; filler++)
                    CreateItemOnObject("nw_maletatcivout", owner);
            });

            var root = Path.Combine(Path.GetTempPath(), "swlor-character-upgrade-" + Guid.NewGuid().ToString("N"));
            var vault = Path.Combine(root, "servervault");
            var path = Path.Combine(vault, "TESTKEY1", "fixture.bic");
            var previousVault = Environment.GetEnvironmentVariable(VaultVariable);
            var previousFileVersion = 0;
            Player previousRecord = null;
            try
            {
                await ctx.ExecuteInCreatureContextAsync(owner, () =>
                {
                    var original = SavedCharacterMigration.SaveFile(owner);
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    File.WriteAllBytes(path, original);
                    previousRecord = SavedCharacterMigration.CopyRecord(DB.Get<Player>(fixture.Id));
                    previousFileVersion = DB.Get<ServerConfiguration>("SWLOR_CONFIG")?.CharacterFileVersion ?? 0;
                    Environment.SetEnvironmentVariable(VaultVariable, vault);
                    var latest = SavedCharacterMigration.LatestVersion;

                    // A record that last migrated at version 5 and a file without a checkpoint: migration 6 unequips
                    // everything, and the full inventory leaves no room for it.
                    StartUpgrade(fixture.Id, previousRecord);
                    ctx.Assert(SavedCharacterMigration.UpgradeServerVault(), "Every character in the vault is upgraded");
                    var upgraded = File.ReadAllBytes(path);
                    var record = DB.Get<Player>(fixture.Id);
                    ctx.AssertEqual(latest, SavedCharacterMigration.ReadFileVersion(upgraded), "The file saves the latest checkpoint");
                    ctx.AssertEqual(latest, record.Version, "The record reaches the latest version");
                    ctx.AssertEqual(latest, record.CharacterFileVersion, "The record holds the file's migrations");
                    var backups = Directory.GetFiles(Path.Combine(root, "servervault_backups"), "fixture.bic", SearchOption.AllDirectories);
                    ctx.AssertEqual(1, backups.Length, "The original file is backed up beside the vault");
                    ctx.Assert(File.ReadAllBytes(backups[0]).SequenceEqual(original), "The backup holds the original file");
                    ctx.Assert(!File.Exists(path + ".upgrade"), "No upgraded file is left beside the character file");
                    var overflow = OverflowRecords(fixture.Id);
                    ctx.Assert(overflow.Any(item => item.Resref == "nw_wswls001"), "The unequipped weapon waits in overflow storage");
                    ctx.AssertEqual(latest, DB.Get<ServerConfiguration>("SWLOR_CONFIG").CharacterFileVersion,
                        "The vault is marked current");

                    ctx.Assert(SavedCharacterMigration.UpgradeServerVault(), "A current vault succeeds");
                    ctx.Assert(File.ReadAllBytes(path).SequenceEqual(upgraded), "A current vault is not read again");

                    // A file restored after the vault was marked current waits for a rescan, which login requests.
                    File.WriteAllBytes(path, original);
                    ctx.Assert(SavedCharacterMigration.UpgradeServerVault(), "A current vault is skipped");
                    ctx.Assert(File.ReadAllBytes(path).SequenceEqual(original), "A restored file is not seen while the vault is marked current");
                    SavedCharacterMigration.RequestVaultRescan();
                    ctx.Assert(SavedCharacterMigration.UpgradeServerVault(), "A rescan upgrades the restored file");
                    ctx.AssertEqual(latest, SavedCharacterMigration.ReadFileVersion(File.ReadAllBytes(path)), "The restored file is current again");
                    File.WriteAllBytes(path, upgraded);

                    // Interrupted after the record was saved: the upgraded file beside the original is moved into place.
                    File.WriteAllBytes(path, original);
                    File.WriteAllBytes(path + ".upgrade", upgraded);
                    ResetVaultMarker();
                    ctx.Assert(SavedCharacterMigration.UpgradeServerVault(), "An interrupted save is finished");
                    ctx.Assert(File.ReadAllBytes(path).SequenceEqual(upgraded), "The upgraded file replaces the original");
                    ctx.Assert(!File.Exists(path + ".upgrade"), "The finished save leaves no upgraded file behind");
                    ctx.AssertEqual(overflow.Count, OverflowRecords(fixture.Id).Count, "Finishing the save adds no overflow items");

                    // Interrupted before the record was saved: the leftover file is discarded and the upgrade runs again.
                    File.WriteAllBytes(path, original);
                    File.WriteAllBytes(path + ".upgrade", upgraded);
                    StartUpgrade(fixture.Id, previousRecord);
                    ResetVaultMarker();
                    ctx.Assert(SavedCharacterMigration.UpgradeServerVault(), "A save interrupted before the record is redone");
                    ctx.AssertEqual(latest, SavedCharacterMigration.ReadFileVersion(File.ReadAllBytes(path)), "The redone file is current");
                    ctx.Assert(!File.Exists(path + ".upgrade"), "The discarded file is gone");
                    ctx.AssertEqual(latest, DB.Get<Player>(fixture.Id).Version, "The redone record is current");
                    ctx.AssertEqual(overflow.Count, OverflowRecords(fixture.Id).Count,
                        "A redone upgrade replaces its overflow items instead of adding copies");

                    // An interrupted save from an older release is finished, then brought to the latest version.
                    var older = SavedCharacterMigration.WithFileVersion(upgraded, latest - 1);
                    File.WriteAllBytes(path, original);
                    File.WriteAllBytes(path + ".upgrade", older);
                    var behind = SavedCharacterMigration.CopyRecord(DB.Get<Player>(fixture.Id));
                    behind.Version = latest - 1;
                    behind.CharacterFileVersion = latest - 1;
                    DB.Set(behind);
                    ResetVaultMarker();
                    ctx.Assert(SavedCharacterMigration.UpgradeServerVault(), "An older interrupted save is finished and upgraded");
                    ctx.AssertEqual(latest, SavedCharacterMigration.ReadFileVersion(File.ReadAllBytes(path)),
                        "The resumed file reaches the latest version");
                    ctx.AssertEqual(latest, DB.Get<Player>(fixture.Id).Version, "The resumed record reaches the latest version");

                    // A character still being created is left for login to set up.
                    File.WriteAllBytes(path, original);
                    var pending = SavedCharacterMigration.CopyRecord(previousRecord);
                    pending.Version = 5;
                    pending.CharacterFileVersion = 0;
                    pending.CharacterInitializationPending = true;
                    DB.Set(pending);
                    ResetVaultMarker();
                    ctx.Assert(SavedCharacterMigration.UpgradeServerVault(), "A pending character does not fail the pass");
                    ctx.Assert(File.ReadAllBytes(path).SequenceEqual(original), "A character still being created keeps its file");
                    ctx.AssertEqual(5, DB.Get<Player>(fixture.Id).Version, "A character still being created keeps its record");

                    // Two files without a checkpoint that share a record are both judged by the record's version before
                    // this boot advanced it, so each runs every migration and keeps its own overflow items.
                    foreach (var item in OverflowRecords(fixture.Id))
                        DB.Delete<OverflowItem>(item.Id);
                    var second = Path.Combine(vault, "TESTKEY2", "fixture.bic");
                    Directory.CreateDirectory(Path.GetDirectoryName(second)!);
                    File.WriteAllBytes(path, original);
                    File.WriteAllBytes(second, original);
                    StartUpgrade(fixture.Id, previousRecord);
                    ctx.Assert(SavedCharacterMigration.UpgradeServerVault(), "Both files that share a record are upgraded");
                    ctx.AssertEqual(latest, SavedCharacterMigration.ReadFileVersion(File.ReadAllBytes(second)), "The second file is current");
                    ctx.AssertEqual(overflow.Count * 2, OverflowRecords(fixture.Id).Count, "Each file keeps its own overflow items");
                });
            }
            finally
            {
                await ctx.ExecuteInCreatureContextAsync(owner, () =>
                {
                    Environment.SetEnvironmentVariable(VaultVariable, previousVault);
                    if (previousRecord != null)
                        DB.Set(previousRecord);
                    var config = DB.Get<ServerConfiguration>("SWLOR_CONFIG");
                    if (config != null)
                    {
                        config.CharacterFileVersion = previousFileVersion;
                        DB.Set(config);
                    }
                    foreach (var item in OverflowRecords(fixture.Id))
                        DB.Delete<OverflowItem>(item.Id);
                });
                if (Directory.Exists(root))
                    Directory.Delete(root, true);
            }
        }

        /// <summary>
        /// A character whose files cannot be written fails alone: the rest of the vault is still upgraded, and overflow
        /// items stored before a failed save are removed again, because the original file still holds those items.
        /// </summary>
        [EngineTest("A character the upgrade cannot save fails alone and leaves no claimable overflow items",
            Category = "CharacterFileUpgrade", TimeoutSeconds = 300)]
        public static async Task FailedCharacterSavesAreIsolated(EngineTestContext ctx)
        {
            using var first = await PlayerAbilityFixture.CreateAsync(ctx);
            using var second = await PlayerAbilityFixture.CreateAsync(ctx);
            var owner = first.Creature;
            await ctx.EquipItemAsync(owner, "nw_wswls001", InventorySlot.RightHand);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                for (var filler = 0; filler < 60; filler++)
                    CreateItemOnObject("nw_maletatcivout", owner);
            });

            var root = Path.Combine(Path.GetTempPath(), "swlor-character-upgrade-" + Guid.NewGuid().ToString("N"));
            var vault = Path.Combine(root, "servervault");
            var firstFolder = Path.Combine(vault, "TESTKEY1");
            var firstPath = Path.Combine(firstFolder, "first.bic");
            var secondPath = Path.Combine(vault, "TESTKEY2", "second.bic");
            var previousVault = Environment.GetEnvironmentVariable(VaultVariable);
            var previousFileVersion = 0;
            Player firstRecord = null;
            Player secondRecord = null;
            try
            {
                await ctx.ExecuteInCreatureContextAsync(owner, () =>
                {
                    var firstFile = SavedCharacterMigration.SaveFile(owner);
                    Directory.CreateDirectory(firstFolder);
                    Directory.CreateDirectory(Path.GetDirectoryName(secondPath)!);
                    File.WriteAllBytes(firstPath, firstFile);
                    File.WriteAllBytes(secondPath, SavedCharacterMigration.SaveFile(second.Creature));
                    firstRecord = SavedCharacterMigration.CopyRecord(DB.Get<Player>(first.Id));
                    secondRecord = SavedCharacterMigration.CopyRecord(DB.Get<Player>(second.Id));
                    previousFileVersion = DB.Get<ServerConfiguration>("SWLOR_CONFIG")?.CharacterFileVersion ?? 0;
                    Environment.SetEnvironmentVariable(VaultVariable, vault);
                    var latest = SavedCharacterMigration.LatestVersion;

                    // A leftover upgraded file the server cannot remove fails only its own character.
                    File.WriteAllBytes(firstPath + ".upgrade", firstFile);
                    File.SetUnixFileMode(firstFolder, UnixFileMode.UserRead | UnixFileMode.UserExecute);
                    StartUpgrade(first.Id, firstRecord);
                    StartUpgrade(second.Id, secondRecord);
                    ctx.Assert(!SavedCharacterMigration.UpgradeServerVault(), "The pass reports the character it could not upgrade");
                    ctx.AssertEqual(latest, SavedCharacterMigration.ReadFileVersion(File.ReadAllBytes(secondPath)), "The other character's file is upgraded");
                    ctx.AssertEqual(latest, DB.Get<Player>(second.Id).Version, "The other character's record is upgraded");
                    ctx.AssertEqual(5, DB.Get<Player>(first.Id).Version, "The failed character keeps its record");
                    ctx.Assert(File.ReadAllBytes(firstPath).SequenceEqual(firstFile), "The failed character keeps its file");
                    File.SetUnixFileMode(firstFolder, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
                    File.Delete(firstPath + ".upgrade");

                    // The upgraded file cannot be written after the overflow items were stored.
                    Directory.CreateDirectory(firstPath + ".upgrade");
                    ResetVaultMarker();
                    ctx.Assert(!SavedCharacterMigration.UpgradeServerVault(), "The failed save is reported");
                    ctx.AssertEqual(0, OverflowRecords(first.Id).Count, "No overflow item can be claimed while the original file holds it");
                    ctx.AssertEqual(5, DB.Get<Player>(first.Id).Version, "The record is unchanged");
                    ctx.Assert(File.ReadAllBytes(firstPath).SequenceEqual(firstFile), "The file is unchanged");

                    Directory.Delete(firstPath + ".upgrade");
                    ResetVaultMarker();
                    ctx.Assert(SavedCharacterMigration.UpgradeServerVault(), "The next boot upgrades the character");
                    ctx.Assert(OverflowRecords(first.Id).Any(item => item.Resref == "nw_wswls001"), "Its overflow items are stored once it is saved");

                    // A different file restored at the same path keeps its own overflow items: a failed save of it neither
                    // replaces nor removes the items the earlier upgrade stored.
                    var earlier = OverflowRecords(first.Id).OrderBy(item => item.Id).Select(item => (item.Id, item.Data)).ToList();
                    File.WriteAllBytes(firstPath, SavedCharacterMigration.WithFileVersion(firstFile, 5));
                    StartUpgrade(first.Id, firstRecord);
                    Directory.CreateDirectory(firstPath + ".upgrade");
                    ctx.Assert(!SavedCharacterMigration.UpgradeServerVault(), "The restored file's failed save is reported");
                    ctx.Assert(OverflowRecords(first.Id).OrderBy(item => item.Id).Select(item => (item.Id, item.Data)).SequenceEqual(earlier),
                        "The earlier upgrade's overflow items are untouched");
                    Directory.Delete(firstPath + ".upgrade");
                    ResetVaultMarker();
                    ctx.Assert(SavedCharacterMigration.UpgradeServerVault(), "The restored file is upgraded");
                    ctx.AssertEqual(earlier.Count * 2, OverflowRecords(first.Id).Count, "Each upgrade keeps its own overflow items");
                });
            }
            finally
            {
                await ctx.ExecuteInCreatureContextAsync(owner, () =>
                {
                    Environment.SetEnvironmentVariable(VaultVariable, previousVault);
                    if (firstRecord != null)
                        DB.Set(firstRecord);
                    if (secondRecord != null)
                        DB.Set(secondRecord);
                    var config = DB.Get<ServerConfiguration>("SWLOR_CONFIG");
                    if (config != null)
                    {
                        config.CharacterFileVersion = previousFileVersion;
                        DB.Set(config);
                    }
                    foreach (var item in OverflowRecords(first.Id).Concat(OverflowRecords(second.Id)))
                        DB.Delete<OverflowItem>(item.Id);
                });
                if (Directory.Exists(firstFolder))
                    File.SetUnixFileMode(firstFolder, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
                if (Directory.Exists(root))
                    Directory.Delete(root, true);
            }
        }

        private static void StartUpgrade(string playerId, Player original)
        {
            var record = SavedCharacterMigration.CopyRecord(original);
            record.Version = 5;
            record.CharacterFileVersion = 0;
            DB.Set(record);
            ResetVaultMarker();
        }

        private static void ResetVaultMarker()
        {
            var config = DB.Get<ServerConfiguration>("SWLOR_CONFIG") ?? new ServerConfiguration();
            config.CharacterFileVersion = 0;
            DB.Set(config);
        }

        private static System.Collections.Generic.List<OverflowItem> OverflowRecords(string playerId) =>
            DB.Search(new DBQuery<OverflowItem>()
                .AddFieldSearch(nameof(OverflowItem.PlayerId), playerId, false)
                .AddPaging(100, 0)).ToList();
    }
}
