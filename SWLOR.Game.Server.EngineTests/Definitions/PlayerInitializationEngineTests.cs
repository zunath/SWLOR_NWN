using System.Reflection;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CurrencyService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class PlayerInitializationEngineTests
    {
        [EngineTest("Saved characters without records remain unchanged at every native level", Category = "PlayerInitialization")]
        public static async Task MissingRecordFiles(EngineTestContext ctx)
        {
            var input = Environment.GetEnvironmentVariable("SWLOR_UNMATCHED_PLAYER_FILE_CORPUS_INPUT");
            if (string.IsNullOrWhiteSpace(input)) ctx.Skip("Supply private saved files without matching database records.");
            var guard = typeof(Feature.PlayerInitialization).GetMethod("RequiresExistingPlayerRecord", BindingFlags.Static | BindingFlags.NonPublic);
            var owner = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            using var output = new StreamWriter(input + ".results.jsonl");
            foreach (var line in File.ReadLines(input))
            {
                var fixture = JObject.Parse(line);
                await ctx.ExecuteInCreatureContextAsync(owner, () =>
                {
                    var obj = ObjectPlugin.Deserialize((string)fixture["Data"]);
                    ctx.Track(obj);
                    ctx.Assert(GetIsObjectValid(obj), "The original saved file loads");
                    var id = GetObjectUUID(obj);
                    ctx.Assert(DB.Get<Player>(id) == null, "The saved file has no initialized record");
                    var before = ObjectPlugin.Serialize(obj);
                    ctx.Assert((bool)guard.Invoke(null, new object[] { new Player(id) }), "Every saved file without creation intent is protected");
                    var after = ObjectPlugin.Serialize(obj);
                    ctx.AssertEqual(before, after, "Refusing setup leaves the character byte-identical");
                    ctx.Assert(DB.Get<Player>(id) == null, "Setup must not replace the missing record");
                    output.WriteLine(JsonConvert.SerializeObject(new { Id = (string)fixture["Id"], Before = before, After = after,
                        Level = GetHitDice(obj), Outcome = "MissingRecordProtected" }));
                });
            }
        }

        [EngineTest("Unfinished retired-class characters initialize with the current standard class", Category = "PlayerInitialization")]
        public static async Task RetiredStarterClass(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                CreaturePlugin.SetClassByPosition(owner, 0, (ClassType)55);
                typeof(Feature.PlayerInitialization).GetMethod("AutoLevelPlayer", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { owner });
                ctx.AssertEqual(ClassType.Standard, GetClassByPosition(1, owner), "Retired starter class becomes the current standard class");
                ctx.AssertEqual(40, GetHitDice(owner), "Initialization reaches the current native level");
            });
        }

        [EngineTest("Legacy resource reset is stable after failed character exports", Category = "PlayerInitialization")]
        public static async Task RetryLegacyResources(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var owner = fixture.Creature;
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                typeof(Feature.PlayerInitialization).GetMethod("ClearInventory", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { owner });
                var original = SavedCharacterMigration.SaveFile(owner);
                var record = new Player(GetObjectUUID(owner)) { Version = -1 };

                // The first upgrade saves the record and then loses the file write.
                var first = SavedCharacterMigration.Run(original, record, "_1_LegacyPlayerMigration");
                ctx.AssertEqual(70, first.Record.MaxHP, "Legacy HP reset sets the base HP");
                ctx.AssertEqual(10, first.Record.MaxFP, "Legacy FP reset sets the base FP");
                ctx.AssertEqual(10, first.Record.MaxStamina, "Legacy stamina reset sets the base stamina");
                var initialNativeHP = GetMaxHitPoints(SavedCharacterMigration.Load(ctx, first));

                // The record is ahead of the unchanged file, so each retry migrates the file and leaves the record alone.
                for (var attempt = 1; attempt <= 3; attempt++)
                {
                    var retried = SavedCharacterMigration.Run(original, first.Record, false, new[] { "_1_LegacyPlayerMigration" });
                    ctx.AssertEqual(70, retried.Record.MaxHP, "Legacy HP reset does not accumulate across failures");
                    ctx.AssertEqual(10, retried.Record.MaxFP, "Legacy FP reset does not accumulate across failures");
                    ctx.AssertEqual(10, retried.Record.MaxStamina, "Legacy stamina reset does not accumulate across failures");
                    ctx.AssertEqual(initialNativeHP, GetMaxHitPoints(SavedCharacterMigration.Load(ctx, retried)), "Native legacy HP does not grow on retry");
                    ctx.Assert(retried.File.SequenceEqual(first.File), "A retried file matches the first upgrade");
                }
            });
        }

        [EngineTest("Failed starter exports do not increase resource budgets on retry", Category = "PlayerInitialization")]
        public static async Task RetryStarterResources(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var saved = new Player(GetObjectUUID(owner));
                var fileVersion = 0;
                var failExport = true;
                var initialNativeHP = 0;
                var initialize = typeof(Feature.PlayerInitialization);
                var adjustStats = initialize.GetMethod("AdjustStats", BindingFlags.Static | BindingFlags.NonPublic);
                var run = typeof(Migration).GetMethod("RunPlayerInitialization", BindingFlags.Static | BindingFlags.NonPublic);
                initialize.GetMethod("AutoLevelPlayer", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { owner });

                void Run() => run.Invoke(null, new object[] { saved,
                    (Action<Player>)(player =>
                    {
                        var firstInitialization = player.Version == 0;
                        adjustStats.Invoke(null, new object[] { owner, player });
                        if (firstInitialization)
                            initialize.GetMethod("GiveStartingTokens", BindingFlags.Static | BindingFlags.NonPublic)
                                .Invoke(null, new object[] { player });
                    }),
                    (Action<Player>)(player => saved = JsonConvert.DeserializeObject<Player>(JsonConvert.SerializeObject(player))),
                    (Func<int>)(() => fileVersion),
                    (Action<int>)(version =>
                    {
                        if (failExport) throw new InvalidOperationException("Injected character export failure");
                        fileVersion = version;
                    }) });

                for (var attempt = 0; attempt < 4; attempt++)
                {
                    failExport = attempt < 3;
                    var failed = false;
                    try { Run(); }
                    catch (TargetInvocationException exception) when (exception.InnerException is InvalidOperationException)
                    { failed = true; }
                    ctx.AssertEqual(failExport, failed, "Only the injected export failure is expected");
                    ctx.AssertEqual(Stat.BaseHP, saved.MaxHP, "HP budget is initialized once across retries");
                    ctx.AssertEqual(Stat.BaseFP, saved.MaxFP, "FP budget is initialized once across retries");
                    ctx.AssertEqual(Stat.BaseSTM, saved.MaxStamina, "Stamina budget is initialized once across retries");
                    ctx.AssertEqual(1, saved.Currencies[CurrencyType.RebuildToken], "Retries retain exactly one starting rebuild token");
                    ctx.AssertEqual(1, saved.Currencies[CurrencyType.LanguageToken], "Retries retain exactly one starting language token");
                    ctx.AssertEqual(failExport, saved.CharacterInitializationPending, "Completion follows a successful export");
                    if (attempt == 0) initialNativeHP = GetMaxHitPoints(owner);
                    ctx.AssertEqual(initialNativeHP, GetMaxHitPoints(owner), "Native HP does not grow on retry");
                }
                ctx.AssertEqual(Migration.GetLatestPlayerVersion(), fileVersion, "Successful retry checkpoints the character");
            });
        }

        [EngineTest("Male starter inventory is complete before its migration checkpoint", Category = "PlayerInitialization")]
        public static Task MaleStarterItems(EngineTestContext ctx) => VerifyStarterItems(ctx, RacialType.Human, Gender.Male, "traveler_m");

        [EngineTest("Female starter inventory is complete before its migration checkpoint", Category = "PlayerInitialization")]
        public static Task FemaleStarterItems(EngineTestContext ctx) => VerifyStarterItems(ctx, RacialType.Human, Gender.Female, "traveler_f");

        [EngineTest("Droid starter inventory is complete before its migration checkpoint", Category = "PlayerInitialization")]
        public static Task DroidStarterItems(EngineTestContext ctx) => VerifyStarterItems(ctx, RacialType.Droid, Gender.Male, "dlarproto");

        private static async Task VerifyStarterItems(EngineTestContext ctx, RacialType race, Gender gender, string outfit)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                CreaturePlugin.SetRacialType(owner, race);
                global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(owner)
                    .AsNWSCreature().m_pStats.m_nGender = (byte)gender;
                typeof(Feature.PlayerInitialization).GetMethod("ClearInventory", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { owner });
                typeof(Feature.PlayerInitialization).GetMethod("GiveStartingItems", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { owner });
                ctx.Assert(!Item.IsEconomyRestricted(GetItemInSlot(InventorySlot.Chest, owner)), "The starting outfit is a player-facing economy item");
                var saved = ObjectPlugin.Serialize(owner);
                var restored = ObjectPlugin.Deserialize(saved);
                ctx.Track(restored);
                ctx.AssertEqual(outfit, GetResRef(GetItemInSlot(InventorySlot.Chest, restored)), "The first saved file already has the starting outfit equipped");
                ctx.Assert(GetIsObjectValid(GetItemPossessedBy(restored, "survival_knife")), "The starting knife is present");
                ctx.AssertEqual(2, Item.GetInventoryItemCount(restored), "Both starting consumables are retained");
                ctx.Assert(GetGold(restored) >= 200, "Starting credits survive the first save");
            });
        }
    }
}
