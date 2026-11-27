using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class TelegraphShaderUpdateEngineTests
    {
        [EngineTest("Telegraph shader updates resend only the slots whose values changed", Category = "TelegraphShader", TimeoutSeconds = 30f)]
        public static async Task UnchangedSlotsAreNotResent(EngineTestContext ctx)
        {
            using var player = await PlayerAbilityFixture.CreateAsync(ctx);
            var telegraphs = Telegraph;
            var caster = ctx.SpawnCreature("nw_bandit001", 2f);
            var players = new[] { player.Creature };

            await CountAsync(ctx, FrameMetric.TelegraphUniform, () => telegraphs.UpdateShadersForPlayers(players));
            ctx.AssertEqual(0L, await CountAsync(ctx, FrameMetric.TelegraphUniform, () => telegraphs.UpdateShadersForPlayers(players)),
                "Uniforms sent when no telegraph changed");

            string telegraphId = null;
            var created = await CountAsync(ctx, FrameMetric.TelegraphUniform, () => ctx.ExecuteInCreatureContextAsync(caster, () =>
                telegraphId = telegraphs.CreateSphereTelegraph(caster, GetPosition(caster), 3f, 10f, true, (_, _) => { })));
            ctx.Assert(telegraphId != null, "The telegraph was created.");
            ctx.AssertEqual(2L, created, "Uniforms sent for one new telegraph (shape, and position with fill timing)");

            ctx.AssertEqual(0L, await CountAsync(ctx, FrameMetric.TelegraphUniform, () => telegraphs.UpdateShadersForPlayers(players)),
                "Uniforms sent when the telegraph is unchanged");
            ctx.AssertEqual(1L, await CountAsync(ctx, FrameMetric.TelegraphUniform, () => telegraphs.CancelTelegraph(telegraphId)),
                "Uniforms sent when the telegraph's slot is cleared");
        }

        [EngineTest("The telegraph fill clock runs only while a viewer has a shape filling", Category = "TelegraphShader", TimeoutSeconds = 30f)]
        public static async Task FillClockRunsOnlyWhileAShapeFills(EngineTestContext ctx)
        {
            using var player = await PlayerAbilityFixture.CreateAsync(ctx);
            var telegraphs = Telegraph;
            var caster = ctx.SpawnCreature("nw_bandit001", 2f);

            ctx.AssertEqual(0L, await CountAsync(ctx, FrameMetric.TelegraphFillClock, () => ctx.DelaySecondsAsync(0.5f)),
                "Fill clock updates sent with no telegraph in the area");

            string telegraphId = null;
            var whileFilling = await CountAsync(ctx, FrameMetric.TelegraphFillClock, async () =>
            {
                await ctx.ExecuteInCreatureContextAsync(caster, () =>
                    telegraphId = telegraphs.CreateSphereTelegraph(caster, GetPosition(caster), 3f, 1f, true, (_, _) => { }));
                await ctx.DelaySecondsAsync(0.6f);
            });
            ctx.Assert(telegraphId != null, "The telegraph was created.");
            ctx.Assert(whileFilling >= 2, $"The fill clock kept moving while the shape filled ({whileFilling} updates).");

            await ctx.DelaySecondsAsync(1.5f);
            ctx.AssertEqual(0L, await CountAsync(ctx, FrameMetric.TelegraphFillClock, () => ctx.DelaySecondsAsync(0.5f)),
                "Fill clock updates sent after the fill finished");

            var flashId = string.Empty;
            var flash = await CountAsync(ctx, FrameMetric.TelegraphFillClock, async () =>
            {
                await ctx.ExecuteInCreatureContextAsync(caster, () =>
                    flashId = telegraphs.CreateTelegraph(caster, GetPosition(caster), 0f, new System.Numerics.Vector2(3f, 3f), 2f, true,
                        TelegraphType.Sphere, (_, _) => { }, isImpactFlash: true));
                await ctx.DelaySecondsAsync(0.5f);
            });
            ctx.AssertEqual(0L, flash, "Fill clock updates sent for an impact flash, which does not fill");
            telegraphs.CancelTelegraph(flashId);
        }

        private static Task<long> CountAsync(EngineTestContext ctx, FrameMetric metric, Action action) =>
            CountAsync(ctx, metric, () =>
            {
                action();
                return Task.CompletedTask;
            });

        private static async Task<long> CountAsync(EngineTestContext ctx, FrameMetric metric, Func<Task> action)
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

            return FrameProfiler.EndCapture().Totals.Count(metric);
        }
    }
}
