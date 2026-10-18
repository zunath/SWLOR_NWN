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
