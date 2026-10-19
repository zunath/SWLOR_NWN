using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class PlayerFileMigrationEngineTests
    {
        /// <summary>
        /// Runs the boot-time character upgrade over private saved-character fixtures, each with its player record:
        /// the upgrade must complete, reach native level 40 and leave a current character that a retry does not change.
        /// </summary>
        [EngineTest("Saved character upgrade sequence", Category = "PlayerFileMigration", TimeoutSeconds = 7200)]
        public static async Task MigratePlayerFiles(EngineTestContext ctx)
        {
            var input = Environment.GetEnvironmentVariable("SWLOR_PLAYER_FILE_CORPUS_INPUT");
            if (string.IsNullOrWhiteSpace(input))
                ctx.Skip("Supply a private character and player-record corpus in an isolated test home.");

            var owner = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            using var output = new StreamWriter(input + ".results.jsonl");
            var count = 0;
            var failures = 0;
            foreach (var line in File.ReadLines(input))
            {
                var record = JObject.Parse(line);
                var result = new JObject { ["Id"] = record["Id"], ["OriginalVersion"] = record["OriginalVersion"] };
                try
                {
                    await ctx.ExecuteInCreatureContextAsync(owner, () =>
                    {
                        var file = Convert.FromBase64String((string)record["Data"]);
                        var player = record["Player"].ToObject<Player>();
                        var upgraded = SavedCharacterMigration.Upgrade(file, player);
                        result["Upgraded"] = upgraded != null;
                        if (upgraded == null)
                            return;
                        result["File"] = upgraded.Data;
                        result["Player"] = JObject.FromObject(upgraded.Record);
                        result["Overflow"] = upgraded.Overflow.Count;
                        result["Notices"] = upgraded.Notices.Count;
                        ctx.AssertEqual(SavedCharacterMigration.LatestVersion, upgraded.FileVersion, "The upgraded file holds the latest checkpoint");
                        ctx.AssertEqual(SavedCharacterMigration.LatestVersion, upgraded.Record.Version, "The upgraded record holds the latest version");
                        ctx.Assert(SavedCharacterMigration.Upgrade(upgraded.File, upgraded.Record) == null, "A retry finds nothing left to upgrade");
                        var restored = SavedCharacterMigration.Load(ctx, upgraded);
                        ctx.AssertEqual(40, GetHitDice(restored), "Native character progression must reach level 40");
                    });
                }
                catch (Exception exception)
                {
                    result["Error"] = exception.ToString();
                    failures++;
                }
                output.WriteLine(result.ToString(Formatting.None));
                output.Flush();
                count++;
                if (count % 80 == 0) ctx.Log($"Audited {count} saved player files; {failures} failures.");
            }
            ctx.SetResultDetail($"Audited {count} files through the saved-data upgrade; {failures} failures.");
            ctx.AssertEqual(0, failures, "No character upgrade failures");
        }
    }
}
