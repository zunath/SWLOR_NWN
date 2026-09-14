using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CurrencyService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class PlayerInitializationEngineTests
    {
        [EngineTest("Legacy resource reset is stable after failed character exports", Category = "PlayerInitialization")]
        public static async Task RetryLegacyResources(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var playerId = GetObjectUUID(owner);
                var fileVersion = -1;
                var initialNativeHP = 0;
                var migration = new Feature.MigrationDefinition.PlayerMigration._1_LegacyPlayerMigration();
                var apply = typeof(Migration).GetMethod("ApplyPlayerMigration", BindingFlags.Static | BindingFlags.NonPublic);
                typeof(Feature.PlayerInitialization).GetMethod("ClearInventory", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { owner });
                DB.Set(new Player(playerId) { Version = -1 });
                try
                {
                    for (var attempt = 0; attempt < 4; attempt++)
                    {
                        var failExport = attempt < 3;
                        var failed = false;
                        try
                        {
                            apply.Invoke(null, new object[] { migration, owner,
                                (Func<Player>)(() => DB.Get<Player>(playerId)),
                                (Action<Player>)(player => DB.Set(player)),
                                (Func<int>)(() => fileVersion),
                                (Action<int>)(version =>
                                {
                                    if (failExport) throw new InvalidOperationException("Injected legacy export failure");
                                    fileVersion = version;
                                }) });
                        }
                        catch (TargetInvocationException exception) when (exception.InnerException is InvalidOperationException)
                        { failed = true; }
                        var saved = DB.Get<Player>(playerId);
                        ctx.AssertEqual(failExport, failed, "Only the injected export failure is expected");
                        ctx.AssertEqual(70, saved.MaxHP, "Legacy HP reset does not accumulate across failures");
                        ctx.AssertEqual(10, saved.MaxFP, "Legacy FP reset does not accumulate across failures");
                        ctx.AssertEqual(10, saved.MaxStamina, "Legacy stamina reset does not accumulate across failures");
                        ctx.AssertEqual(failExport ? -1 : 1, saved.Version, "Database completion requires the saved file");
                        if (attempt == 0) initialNativeHP = GetMaxHitPoints(owner);
                        ctx.AssertEqual(initialNativeHP, GetMaxHitPoints(owner), "Native legacy HP does not grow on retry");
                    }
                    ctx.AssertEqual(1, fileVersion, "Successful retry checkpoints the legacy migration");
                }
                finally { DB.Delete<Player>(playerId); }
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
                            initialize.GetMethod("GiveStartingRebuildToken", BindingFlags.Static | BindingFlags.NonPublic)
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
                    ctx.AssertEqual(1, saved.Currencies[CurrencyType.RebuildToken], "Retries retain exactly one starting token");
                    ctx.AssertEqual(failExport, saved.CharacterInitializationPending, "Completion follows a successful export");
                    if (attempt == 0) initialNativeHP = GetMaxHitPoints(owner);
                    ctx.AssertEqual(initialNativeHP, GetMaxHitPoints(owner), "Native HP does not grow on retry");
                }
                ctx.AssertEqual(Migration.GetLatestPlayerVersion(), fileVersion, "Successful retry checkpoints the character");
            });
        }

        [EngineTest("Male starter inventory is complete before its migration checkpoint", Category = "PlayerInitialization")]
        public static Task MaleStarterItems(EngineTestContext ctx) => VerifyStarterItems(ctx, RacialType.Human, Gender.Male, "nw_maletatcivout");

        [EngineTest("Female starter inventory is complete before its migration checkpoint", Category = "PlayerInitialization")]
        public static Task FemaleStarterItems(EngineTestContext ctx) => VerifyStarterItems(ctx, RacialType.Human, Gender.Female, "nw_femtatcivoutf");

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
