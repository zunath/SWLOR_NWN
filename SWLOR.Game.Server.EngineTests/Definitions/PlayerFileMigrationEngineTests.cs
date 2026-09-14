using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NWNXLib = NWN.Native.API.NWNXLib;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class PlayerFileMigrationEngineTests
    {
        /// <summary>
        /// Exercises ordered player migrations on private saved-character fixtures with native PC semantics.
        /// Synthetic client registration enables PC-only branches, but does not prove authenticated servervault export.
        /// </summary>
        [EngineTest("Saved character player migration sequence", Category = "PlayerFileMigration", TimeoutSeconds = 7200)]
        public static async Task MigratePlayerFiles(EngineTestContext ctx)
        {
            var input = Environment.GetEnvironmentVariable("SWLOR_PLAYER_FILE_CORPUS_INPUT");
            if (string.IsNullOrWhiteSpace(input))
                ctx.Skip("Supply a private character and player-record corpus in an isolated test home.");

            var migrations = typeof(Migration).Assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface && typeof(IPlayerMigration).IsAssignableFrom(t))
                .Select(t => (IPlayerMigration)Activator.CreateInstance(t)).OrderBy(m => m.Version).ToArray();
            var apply = typeof(Migration).GetMethod("ApplyPlayerMigration", BindingFlags.Static | BindingFlags.NonPublic);
            var destroy = typeof(Migration).Assembly.GetType("SWLOR.Game.Server.Feature.MigrationDefinition.MigrationObject")
                .GetMethod("DestroyTemporaryObject", BindingFlags.Public | BindingFlags.Static);
            var owner = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            using var output = new StreamWriter(input + ".results.jsonl");
            var count = 0;
            var failures = 0;
            var sequence = 0;
            async Task MigrateLine(string line)
            {
                var fixtureId = sequence++;
                var record = JObject.Parse(line);
                var obj = OBJECT_INVALID;
                global::NWN.Native.API.CNWSPlayer client = null;
                string playerId = null;
                var fileVersion = (int)record["OriginalVersion"];
                string savedFile = null;
                void SaveFile(int version)
                {
                    SetLocalInt(obj, "PLAYER_MIGRATION_VERSION", version);
                    savedFile = ObjectPlugin.Serialize(obj);
                    fileVersion = version;
                }
                var result = new JObject { ["Id"] = record["Id"], ["OriginalVersion"] = record["OriginalVersion"] };
                try
                {
                    await ctx.ExecuteInCreatureContextAsync(owner, () =>
                    {
                        obj = ObjectPlugin.Deserialize((string)record["Data"]);
                        ctx.Assert(GetIsObjectValid(obj), "Saved character must load");
                    });
                    // Let the native loader finish its inventory/equipment callbacks
                    // before turning this offline fixture into a synthetic player.
                    await ctx.DelaySecondsAsync(0.2f);
                    await ctx.ExecuteInCreatureContextAsync(owner, () =>
                    {
                        playerId = GetObjectUUID(obj);
                        var player = record["Player"].ToObject<Player>();
                        player.Id = playerId;
                        DB.Set(player);
                        NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(obj).AsNWSCreature().m_bPlayerCharacter = 1;
                        client = new global::NWN.Native.API.CNWSPlayer(0x7fff0000u + (uint)fixtureId);
                        client.SetGameObject(NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(obj).AsNWSObject());
                        client.m_oidPCObject = obj;
                        NWNXLib.g_pAppManager.m_pServerExoApp.GetPlayerList().Add(client);
                        ctx.Assert(GetIsPC(obj), "Fixture must exercise native player migration branches");
                        result["Before"] = ObjectPlugin.Serialize(obj);
                        var checkpoints = new JArray();
                        result["Checkpoints"] = checkpoints;
                        foreach (var migration in migrations.Where(m => m.Version > player.Version))
                        {
                            result["Applying"] = migration.Version;
                            apply.Invoke(null, new object[] { migration, obj,
                                (Func<Player>)(() => DB.Get<Player>(playerId)),
                                (Action<Player>)(updated => DB.Set(updated)), (Func<int>)(() => fileVersion), (Action<int>)SaveFile });
                            checkpoints.Add(new JObject { ["Version"] = migration.Version, ["Player"] = JObject.FromObject(DB.Get<Player>(playerId)) });
                        }
                        result["Immediate"] = ObjectPlugin.Serialize(obj);
                        result["PlayerImmediate"] = JObject.FromObject(DB.Get<Player>(playerId));
                        result["SavedFile"] = savedFile;
                        ctx.AssertEqual(40, GetHitDice(obj), "Native character progression must reach level 40");
                    });
                    await ctx.DelaySecondsAsync(0.2f);
                    await ctx.ExecuteInCreatureContextAsync(owner, () =>
                    {
                        result["Settled"] = ObjectPlugin.Serialize(obj);
                        result["PlayerSettled"] = JObject.FromObject(DB.Get<Player>(playerId));
                        foreach (var migration in migrations)
                            apply.Invoke(null, new object[] { migration, obj,
                                (Func<Player>)(() => DB.Get<Player>(playerId)),
                                (Action<Player>)(updated => DB.Set(updated)), (Func<int>)(() => fileVersion), (Action<int>)SaveFile });
                        result["Retried"] = ObjectPlugin.Serialize(obj);
                        result["PlayerRetried"] = JObject.FromObject(DB.Get<Player>(playerId));
                    });
                }
                catch (Exception exception)
                {
                    result["Error"] = exception.ToString();
                    failures++;
                }
                finally
                {
                    await ctx.ExecuteInCreatureContextAsync(owner, () =>
                    {
                        if (client != null)
                        {
                            NWNXLib.g_pAppManager.m_pServerExoApp.GetPlayerList().Remove(client);
                            client.m_oidPCObject = OBJECT_INVALID;
                            // Keep the object ID until disposal so native lookup hooks can remove the cached client.
                            client.Dispose();
                        }
                        if (GetIsObjectValid(obj))
                        {
                            NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(obj).AsNWSCreature().m_bPlayerCharacter = 0;
                            destroy.Invoke(null, new object[] { obj });
                        }
                        if (playerId != null) DB.Delete<Player>(playerId);
                    });
                    output.WriteLine(result.ToString(Formatting.None));
                    output.Flush();
                }
                count++;
                if (count % 80 == 0) ctx.Log($"Audited {count} saved player files; {failures} failures.");
            }
            foreach (var batch in File.ReadLines(input).Chunk(8))
                await Task.WhenAll(batch.Select(MigrateLine));
            ctx.SetResultDetail($"Audited {count} files using synthetic PC semantics; {failures} failures. Connected-client export is a separate check.");
            ctx.AssertEqual(0, failures, "No player-file migration failures");
        }
    }
}
