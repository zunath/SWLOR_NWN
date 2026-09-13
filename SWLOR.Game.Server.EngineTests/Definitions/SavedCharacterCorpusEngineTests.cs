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
    public static class SavedCharacterCorpusEngineTests
    {
        /// <summary>
        /// Verifies that saved character item corpus survives conversion and retries.
        /// </summary>
        [EngineTest("Saved character item corpus survives conversion and retries", Category = "SavedCharacterCorpus", TimeoutSeconds = 7200)]
        public static async Task MigrateSavedCharacters(EngineTestContext ctx)
        {
            var input = Environment.GetEnvironmentVariable("SWLOR_SAVED_CHARACTER_CORPUS_INPUT");
            if (string.IsNullOrWhiteSpace(input))
                ctx.Skip("Supply a private saved-character JSONL corpus in an isolated engine-test home.");

            var migrationType = typeof(_22_CombatSystemReplacement).Assembly.GetType(
                "SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration.StoredItemDataMigration");
            var method = migrationType.GetMethod("MigrateStoredObject", BindingFlags.NonPublic | BindingFlags.Static);
            var resultType = migrationType.GetNestedType("SerializedObjectMigrationResult", BindingFlags.NonPublic);
            var destroy = migrationType.Assembly.GetType("SWLOR.Game.Server.Feature.MigrationDefinition.MigrationObject")
                .GetMethod("DestroyTemporaryObject", BindingFlags.Public | BindingFlags.Static);
            var saberMigration = migrationType.Assembly.GetType(
                "SWLOR.Game.Server.Feature.MigrationDefinition.LegacySaberMigration")
                .GetMethod("NormalizeSabersOnObject", BindingFlags.NonPublic | BindingFlags.Static);
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
                        ctx.Assert(GetIsObjectValid(obj), "Saved character must deserialize in the native engine.");
                        var before = ObjectPlugin.Serialize(obj);
                        var changed = (bool)method.Invoke(null, new[] { (object)obj, Activator.CreateInstance(resultType, true) });
                        // BICs deserialize as NPCs, whose stored-object sweep
                        // intentionally skips the recalibration done at player login.
                        changed |= (int)saberMigration.Invoke(null, new object[] { obj }) > 0;
                        var migrated = ObjectPlugin.Serialize(obj);
                        var changedAgain = (bool)method.Invoke(null, new[] { (object)obj, Activator.CreateInstance(resultType, true) });
                        changedAgain |= (int)saberMigration.Invoke(null, new object[] { obj }) > 0;
                        var retried = ObjectPlugin.Serialize(obj);
                        output.WriteLine(JsonConvert.SerializeObject(new { Id = (string)record["Id"], Before = before, Migrated = migrated, Retried = retried, Changed = changed, ChangedAgain = changedAgain }));
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
                if (processed % 100 == 0)
                {
                    output.Flush();
                    ctx.Log($"Processed {processed} saved characters; {failures} failures.");
                }
                await ctx.WaitFrameAsync();
            }
            ctx.Log($"Processed all {processed} supplied saved characters; {failures} failures. Structural results are beside the private input corpus.");
            ctx.AssertEqual(0, failures, "No saved-character conversion failures");
        }
    }
}
