using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class StoredObjectCorpusEngineTests
    {
        /// <summary>
        /// Verifies that stored object corpus converts without native errors.
        /// </summary>
        [EngineTest("Stored object corpus converts without native errors", Category = "StoredObjectCorpus", TimeoutSeconds = 7200)]
        public static async Task MigrateStoredObjects(EngineTestContext ctx)
        {
            var input = Environment.GetEnvironmentVariable("SWLOR_MIGRATION_CORPUS_INPUT");
            if (string.IsNullOrWhiteSpace(input))
                ctx.Skip("Supply a private serialized-object JSONL corpus in an isolated engine-test home.");

            var type = typeof(_22_CombatSystemReplacement).Assembly.GetType(
                "SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration.StoredItemDataMigration");
            var migrate = type.GetMethod("MigrateSerializedObject", BindingFlags.NonPublic | BindingFlags.Static);
            var expand = type.GetMethod("TryMigrateRootBlueprintVariants", BindingFlags.NonPublic | BindingFlags.Static);
            var resultType = type.GetNestedType("SerializedObjectMigrationResult", BindingFlags.NonPublic);
            var destroy = type.Assembly.GetType("SWLOR.Game.Server.Feature.MigrationDefinition.MigrationObject")
                .GetMethod("DestroyTemporaryObject", BindingFlags.Public | BindingFlags.Static);

            List<string> Convert(string data)
            {
                var arguments = new object[] { data, null, null, true };
                if ((bool)expand.Invoke(null, arguments))
                    return (List<string>)arguments[1];

                var result = migrate.Invoke(null, new object[] { data });
                return (bool)resultType.GetProperty("RemovedRoot").GetValue(result)
                    ? new List<string>()
                    : new List<string> { (string)resultType.GetProperty("Data").GetValue(result) };
            }

            var owner = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            using var output = new StreamWriter(input + ".results.jsonl");
            var processed = 0;
            var failures = 0;
            foreach (var batch in File.ReadLines(input).Chunk(10))
            {
                await ctx.ExecuteInCreatureContextAsync(owner, () =>
                {
                    foreach (var line in batch)
                    {
                        var record = JObject.Parse(line);
                        var obj = OBJECT_INVALID;
                        try
                        {
                            obj = ObjectPlugin.Deserialize((string)record["Data"]);
                            var before = GetIsObjectValid(obj) ? ObjectPlugin.Serialize(obj) : (string)record["Data"];
                            // The baseline fixture must release saved UUIDs before
                            // conversion loads the same archived object again.
                            if (GetIsObjectValid(obj))
                                destroy.Invoke(null, new object[] { obj });
                            obj = OBJECT_INVALID;
                            var migrated = Convert((string)record["Data"]);
                            var retried = migrated.SelectMany(Convert).ToList();
                            output.WriteLine(JsonConvert.SerializeObject(new
                            {
                                Id = (string)record["Id"], Before = before, Migrated = migrated, Retried = retried
                            }));
                        }
                        catch (Exception exception)
                        {
                            failures++;
                            output.WriteLine(JsonConvert.SerializeObject(new { Id = (string)record["Id"], Error = exception.ToString() }));
                        }
                        finally
                        {
                            if (GetIsObjectValid(obj))
                                destroy.Invoke(null, new object[] { obj });
                        }
                        processed++;
                    }
                });
                if (processed % 1000 == 0)
                {
                    output.Flush();
                    ctx.Log($"Processed {processed} stored objects; {failures} failures.");
                }
                await ctx.WaitFrameAsync();
            }
            ctx.Log($"Processed all {processed} supplied stored objects; {failures} failures. Compare the private result snapshots for semantic correctness.");
            ctx.AssertEqual(0, failures, "No stored-object conversion failures");
        }
    }
}
