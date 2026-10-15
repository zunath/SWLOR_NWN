using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.FrameProfilerService;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class TelegraphShaderUpdateEngineTests
    {
        [EngineTest("Telegraph shader updates resend only the slots whose values changed", Category = "TelegraphShader", TimeoutSeconds = 30f)]
        public static async Task UnchangedSlotsAreNotResent(EngineTestContext ctx)
        {
            using var player = await PlayerAbilityFixture.CreateAsync(ctx);
            var caster = ctx.SpawnCreature("nw_bandit001", 2f);
            var players = new[] { player.Creature };

            await CountUniformsAsync(ctx, () => Telegraph.UpdateShadersForPlayers(players));
            ctx.AssertEqual(0L, await CountUniformsAsync(ctx, () => Telegraph.UpdateShadersForPlayers(players)),
                "Uniforms sent when no telegraph changed");

            string telegraphId = null;
            var created = await CountUniformsAsync(ctx, () => ctx.ExecuteInCreatureContextAsync(caster, () =>
                telegraphId = Telegraph.CreateSphereTelegraph(caster, GetPosition(caster), 3f, 10f, true, (_, _) => { })));
            ctx.Assert(telegraphId != null, "The telegraph was created.");
            ctx.AssertEqual(2L, created, "Uniforms sent for one new telegraph (shape and position)");

            ctx.AssertEqual(0L, await CountUniformsAsync(ctx, () => Telegraph.UpdateShadersForPlayers(players)),
                "Uniforms sent when the telegraph is unchanged");
            ctx.AssertEqual(1L, await CountUniformsAsync(ctx, () => Telegraph.CancelTelegraph(telegraphId)),
                "Uniforms sent when the telegraph's slot is cleared");
        }

        private static Task<long> CountUniformsAsync(EngineTestContext ctx, Action action) =>
            CountUniformsAsync(ctx, () =>
            {
                action();
                return Task.CompletedTask;
            });

        private static async Task<long> CountUniformsAsync(EngineTestContext ctx, Func<Task> action)
        {
            FrameProfiler.BeginCapture();
            try
            {
                await ctx.WaitFrameAsync();
                await action();
                await ctx.WaitFrameAsync();
                await ctx.WaitFrameAsync();
            }
            catch
            {
                FrameProfiler.EndCapture();
                throw;
            }

            return FrameProfiler.EndCapture().Totals.Count(FrameMetric.TelegraphUniform);
        }
    }
}
