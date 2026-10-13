using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Caches that outlive a server frame must still return what Redis holds and must not keep
    /// unsaved changes.
    /// </summary>
    public static class PlayerDataCacheEngineTests
    {
        [EngineTest("Player records read across frames return fresh instances without unsaved changes", Category = "PlayerDataCache", TimeoutSeconds = 30f)]
        public static async Task PlayerRecordsStayFresh(EngineTestContext ctx)
        {
            var id = Guid.NewGuid().ToString();
            try
            {
                DB.Set(new Player(id) { Name = "Saved" });
                await ctx.WaitFrameAsync();

                var first = DB.Get<Player>(id);
                ctx.AssertEqual("Saved", first.Name, "Record read in a later frame");
                first.Name = "Unsaved";
                await ctx.WaitFrameAsync();

                var second = DB.Get<Player>(id);
                ctx.Assert(!ReferenceEquals(first, second), "Each frame gets its own instance.");
                ctx.AssertEqual("Saved", second.Name, "Unsaved change is discarded at the end of the frame");

                second.Name = "Saved again";
                DB.Set(second);
                await ctx.WaitFrameAsync();
                ctx.AssertEqual("Saved again", DB.Get<Player>(id).Name, "Record read after a save");

                DB.Delete<Player>(id);
                await ctx.WaitFrameAsync();
                ctx.Assert(DB.Get<Player>(id) == null, "Deleted record is not returned.");
            }
            finally
            {
                DB.Delete<Player>(id);
            }
        }

        [EngineTest("Known names read across frames follow saves and direct deletes", Category = "PlayerDataCache", TimeoutSeconds = 45f)]
        public static async Task KnownNamesFollowWrites(EngineTestContext ctx)
        {
            using var observer = await PlayerAbilityFixture.CreateAsync(ctx);
            using var target = await PlayerAbilityFixture.CreateAsync(ctx, 3f);
            try
            {
                await ctx.ExecuteInCreatureContextAsync(observer.Creature, () =>
                    PlayerName.SetKnownName(observer.Creature, target.Creature, "Tessar"));
                await ctx.WaitFrameAsync();
                await ctx.WaitFrameAsync();

                ctx.Assert(PlayerName.TryGetKnownName(observer.Creature, target.Creature, out var knownName),
                    "Known name is found in a later frame.");
                ctx.AssertEqual("Tessar", knownName, "Known name read in a later frame");

                foreach (var record in DB.Search(new DBQuery<PlayerKnownName>()
                             .AddFieldSearch(nameof(PlayerKnownName.ObserverPlayerId), observer.Id, false)).ToList())
                    DB.Delete<PlayerKnownName>(record.Id);
                await ctx.WaitFrameAsync();

                ctx.Assert(!PlayerName.TryGetKnownName(observer.Creature, target.Creature, out _),
                    "A deleted known name is not served from the cache.");
            }
            finally
            {
                foreach (var record in DB.Search(new DBQuery<PlayerKnownName>()
                             .AddFieldSearch(nameof(PlayerKnownName.ObserverPlayerId), observer.Id, false)).ToList())
                    DB.Delete<PlayerKnownName>(record.Id);
            }
        }
    }
}
