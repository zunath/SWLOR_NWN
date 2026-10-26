using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NWN.Native.API;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CurrencyService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using ClassType = SWLOR.NWN.API.NWScript.Enum.ClassType;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class PlayerPerkFeatRestorationEngineTests
    {
        [EngineTest("Standard rebuild purchases survive the native player login path", Category = "PlayerPerkFeats")]
        public static Task StandardRebuildLogin(EngineTestContext ctx) => VerifyRebuildLogin(ctx, ClassType.Standard);

        [EngineTest("Force-sensitive rebuild purchases survive the native player login path", Category = "PlayerPerkFeats")]
        public static Task ForceSensitiveRebuildLogin(EngineTestContext ctx) => VerifyRebuildLogin(ctx, ClassType.ForceSensitive);

        private static async Task VerifyRebuildLogin(EngineTestContext ctx, ClassType characterClass)
        {
            var player = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(player, () => VerifyRebuildExport(ctx, player, characterClass));
        }

        private static unsafe void VerifyRebuildExport(EngineTestContext ctx, uint player, ClassType characterClass)
        {
            const string checkpointVariable = "PLAYER_MIGRATION_VERSION";
            CreaturePlugin.SetClassByPosition(player, 0, characterClass);
            var latest = SavedCharacterMigration.LatestVersion;
            var record = new Player(GetObjectUUID(player)) { Version = 14, UnallocatedSP = 166 };
            DB.Set(record);
            var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(player).AsNWSCreature();
            var wasPlayer = native.m_bPlayerCharacter;
            try
            {
                // The character is upgraded at boot; then perform the feat/perk reset used by a full rebuild.
                record.Version = latest;
                SetLocalInt(player, checkpointVariable, latest);
                record.Perks.Clear();
                PlayerInitialization.ResetFeatsToBaseline(player);
                record.RebuildComplete = true;

                foreach (var (perk, rank) in CreateRecord(player).Perks.Where(entry =>
                             entry.Value > 0 && entry.Key != (PerkType)int.MaxValue))
                {
                    for (var level = 1; level <= rank; level++)
                    {
                        record.Perks[perk] = level;
                        record.UnallocatedSP -= Perk.GetPerkDetails(perk).PerkLevels[level].Price;
                        DB.Set(record);
                        Perk.SyncGrantedFeats(player, perk, level, true);
                    }
                }
                AssertPurchasedFeats(ctx, player);
                var savedRecord = JsonConvert.SerializeObject(record);

                using var file = new CResGFF();
                using var root = new CResStruct();
                using var type = new CExoString("BIC ");
                using var format = new CExoString("V2.0");
                ctx.Assert(file.CreateGFFFile(root, type, format) != 0, "Create rebuilt character export");
                native.m_bPlayerCharacter = 1;
                ctx.Assert(native.SaveCreature(file, root, 0, 0, 1, 0) != 0, "Export the purchased feats as a player BIC");
                native.m_bPlayerCharacter = wasPlayer;
                fixed (byte* label = Encoding.ASCII.GetBytes("VarTable\0"))
                    ctx.AssertEqual(uint.MaxValue, file.GetFieldByLabel(root, label), "Player exports omit ordinary local variables");

                using var restored = new CNWSCreature(OBJECT_INVALID, 1, 1);
                // CNWSPlayer.LoadCreatureData calls ReadStatsFromGff directly; it never calls
                // CNWSCreature.LoadCreature. A generic object round-trip misses this login path.
                ctx.AssertEqual(0u, restored.m_pStats.ReadStatsFromGff(file, root, restored.m_cAppearance, 0, 1, 0, 1),
                    "Native player stats load succeeds (zero is success)");
                using var checkpointName = new CExoString(checkpointVariable);
                ctx.AssertEqual(latest, restored.m_ScriptVars.GetInt(checkpointName), "Player login restores the saved migration checkpoint");
                foreach (var feat in new[] { FeatType.FortressStrike3, FeatType.BastionStance1, FeatType.LastStandTrait })
                    ctx.Assert(restored.m_pStats.HasFeat((ushort)feat) != 0, $"Purchased feat {feat} survives before any login repair");

                // The boot-time upgrade reads the same checkpoint from the saved file: a current character is left alone,
                // so it neither repeats the feat reset nor the database rewards.
                var upgraded = SavedCharacterMigration.Upgrade(SavedCharacterMigration.SaveFile(player), record);
                ctx.Assert(upgraded == null, "A completed character upgrade must not repeat its feat reset or database rewards");
                ctx.AssertEqual(savedRecord, JsonConvert.SerializeObject(record), "Restart retains rebuilt perks and remaining SP");
            }
            finally
            {
                native.m_bPlayerCharacter = wasPlayer;
                DB.Delete<Player>(record.Id);
            }
        }

        [EngineTest("Character migration preserves purchased ability ranks in the saved file", Category = "PlayerPerkFeats", TimeoutSeconds = 120)]
        public static async Task MigrationPreservesPurchasedFeats(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                CreaturePlugin.SetClassByPosition(player, 0, ClassType.Standard);
                var record = CreateRecord(player);
                // The record already holds the effects of every migration through 15; only the file is behind.
                record.CharacterFileVersion = 15;
                DB.Set(record);
                PlayerInitialization.ResetFeatsToBaseline(player);
                Perk.RestorePlayerFeats(player);
                AssertPurchasedFeats(ctx, player);
                // Reproduce an older character file with an already-current database record.
                SetLocalInt(player, "PLAYER_MIGRATION_VERSION", 14);
                var upgraded = SavedCharacterMigration.Upgrade(SavedCharacterMigration.SaveFile(player), DB.Get<Player>(record.Id));
                ctx.Assert(upgraded != null, "The file is behind and is upgraded");
                var restored = SavedCharacterMigration.Load(ctx, upgraded);
                AssertPurchasedFeats(ctx, restored);
                ctx.AssertEqual(166, upgraded.Record.UnallocatedSP, "Migration does not charge for restored abilities");
                ctx.AssertEqual(3, upgraded.Record.Currencies[CurrencyType.RebuildToken], "File replay grants only migration 24's token, not migration 15's again");
                ctx.AssertEqual(SavedCharacterMigration.LatestVersion, upgraded.Record.Version, "The record reaches the latest version");
            });
        }

        [EngineTest("Previously stripped characters recover purchased feats without repeating rewards", Category = "PlayerPerkFeats")]
        public static async Task RestorePreviouslyStrippedCharacter(EngineTestContext ctx)
        {
            var player = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                var record = CreateRecord(player);
                DB.Set(record);
                try
                {
                    PlayerInitialization.ResetFeatsToBaseline(player);
                    CreaturePlugin.AddFeatByLevel(player, FeatType.FortressStrike1, 1);
                    CreaturePlugin.AddFeatByLevel(player, FeatType.Alertness, 1);
                    var before = JsonConvert.SerializeObject(DB.Get<Player>(record.Id));

                    Perk.RestorePlayerFeats(player);
                    AssertPurchasedFeats(ctx, player);
                    var featCount = CreaturePlugin.GetFeatCount(player);
                    var levelFeatCount = CreaturePlugin.GetFeatCountByLevel(player, 1);
                    Perk.RestorePlayerFeats(player);

                    AssertPurchasedFeats(ctx, player);
                    ctx.AssertEqual(featCount, CreaturePlugin.GetFeatCount(player), "Repeated login does not duplicate feats");
                    ctx.AssertEqual(levelFeatCount, CreaturePlugin.GetFeatCountByLevel(player, 1), "Level history does not accumulate feats");
                    ctx.Assert(CreaturePlugin.GetKnowsFeat(player, FeatType.Alertness), "Unrelated native feats survive restoration");
                    ctx.Assert(!CreaturePlugin.GetKnowsFeat(player, FeatType.Earthshatter2), "An unowned ability is not granted");
                    ctx.AssertEqual(before, JsonConvert.SerializeObject(DB.Get<Player>(record.Id)), "Perks, SP and rewards remain unchanged");
                }
                finally { DB.Delete<Player>(record.Id); }
            });
        }

        private static Player CreateRecord(uint player)
        {
            var record = new Player(GetObjectUUID(player)) { Version = 15, UnallocatedSP = 166 };
            record.Currencies[CurrencyType.RebuildToken] = 2;
            record.Perks[PerkType.FortressStrike] = 3;
            record.Perks[PerkType.BastionStance] = 1;
            record.Perks[PerkType.LastStand] = 1;
            record.Perks[PerkType.Earthshatter] = 0;
            record.Perks[(PerkType)int.MaxValue] = 1;
            return record;
        }

        private static void AssertPurchasedFeats(EngineTestContext ctx, uint player)
        {
            foreach (var feat in new[] { FeatType.FortressStrike3, FeatType.BastionStance1, FeatType.LastStandTrait })
            {
                ctx.Assert(CreaturePlugin.GetKnowsFeat(player, feat), $"Purchased feat {feat} is available");
                var levelFeats = Enumerable.Range(0, CreaturePlugin.GetFeatCountByLevel(player, 1))
                    .Select(index => CreaturePlugin.GetFeatByLevel(player, 1, index));
                ctx.AssertEqual(1, levelFeats.Count(candidate => candidate == feat), $"Purchased feat {feat} occurs once in level history");
            }
            ctx.Assert(!CreaturePlugin.GetKnowsFeat(player, FeatType.FortressStrike1), "The superseded first rank is removed");
            ctx.Assert(!CreaturePlugin.GetKnowsFeat(player, FeatType.FortressStrike2), "The superseded second rank is removed");
            ctx.Assert(CreaturePlugin.GetKnowsFeat(player, FeatType.PropertyMenu), "Baseline feats remain available");
        }
    }
}
