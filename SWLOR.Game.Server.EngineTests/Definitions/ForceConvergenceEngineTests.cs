using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class ForceConvergenceEngineTests
    {
        [EngineTest("Force Convergence restores 40% of maximum FP over 20 seconds and keeps its accuracy for 30", Category = "StatusEffect", TimeoutSeconds = 60f)]
        public static async Task RestoresFortyPercentOverTwentySeconds(EngineTestContext ctx)
        {
            var caster = ctx.SpawnCreature("nw_bandit001");
            // Spawn initialization refills the pools, so let it finish first.
            await ctx.DelaySecondsAsync(1f);
            ctx.SetNPCResources(caster, 100, 100);
            ctx.SuppressNPCNaturalRegen(caster);
            var maxFP = Stat.GetMaxFP(caster);
            Stat.ReduceFP(caster, Stat.GetCurrentFP(caster));
            await ctx.DelaySecondsAsync(1f);
            ctx.AssertEqual(0, Stat.GetCurrentFP(caster), "FP starts empty and nothing else restores it");

            StatusEffect.ApplyStatusEffect(caster, caster, typeof(ForceConvergenceStatusEffect), 30f);
            var total = GameMath.PercentOf(maxFP, ForceConvergenceStatusEffect.FPRestorePercent);

            await ctx.WaitUntilAsync(() => Stat.GetCurrentFP(caster) >= total, 28f, $"Convergence to restore {total} FP");
            ctx.AssertEqual(total, Stat.GetCurrentFP(caster), "Convergence restores 40% of max FP, not more");
            ctx.AssertEqual(5, Stat.GetStatAdjustment(caster, StatType.AbilityHitChancePercentAdjustment),
                "accuracy bonus outlasts the FP restore");

            await ctx.WaitUntilAsync(() => !StatusEffect.HasStatusEffect<ForceConvergenceStatusEffect>(caster),
                15f, "Convergence to expire");
            ctx.AssertEqual(total, Stat.GetCurrentFP(caster), "no FP is restored past the tenth tick");
            ctx.Log($"Force Convergence restored {total}/{maxFP} FP.");
        }
    }
}
