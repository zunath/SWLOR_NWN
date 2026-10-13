using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Caches that outlive a server frame must still return what Redis holds.
    /// </summary>
    public static class PlayerDataCacheEngineTests
    {
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
