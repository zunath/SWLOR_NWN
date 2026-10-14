using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Embattled refreshes on every hostile hit. A hit that leaves the stack count unchanged restarts the
    /// pressure window on the active effect; a stack change replaces it.
    /// </summary>
    public static class EmbattledRefreshEngineTests
    {
        private const string Resref = "nw_bandit001";

        [EngineTest("Embattled restarts its window in place for unchanged stacks and replaces itself when stacks change", Category = "Embattled", TimeoutSeconds = 60f)]
        public static async Task UnchangedStacksRefreshInPlace(EngineTestContext ctx)
        {
            var defender = ctx.SpawnCreature(Resref);
            var first = ctx.SpawnCreature(Resref, 2f);
            var second = ctx.SpawnCreature(Resref, -2f);
            var control = ctx.SpawnCreature(Resref, 0f, 3f);
            // Spawn initialization resets NPC stats after the first frame.
            await ctx.DelaySecondsAsync(1f);
            foreach (var creature in new[] { defender, first, second, control })
                SetAILevel(creature, AILevel.VeryLow);
            ctx.SetNPCPerkLevel(defender, PerkType.SurroundedNotOutmatched, 1);
            ctx.SetNPCPerkLevel(control, PerkType.SurroundedNotOutmatched, 1);
            ctx.Assert(Stat.GetStatAdjustment(defender, StatType.EmbattledStackDefensePercent) > 0, "the defender has the Embattled trait");

            EmbattledStatusEffect.Refresh(defender, first);
            var applied = StatusEffect.GetStatusEffect<EmbattledStatusEffect>(defender);
            ctx.Assert(applied != null, "the first hostile hit applies Embattled");
            ctx.AssertEqual(1, applied.Stacks, "one attacker grants one stack");
            var oneStackDefense = StatusEffect.GetStatAdjustment(defender, StatType.PhysicalDefensePercentAdjustment);

            await ctx.DelaySecondsAsync(2f);
            EmbattledStatusEffect.Refresh(defender, first);
            // A fresh Embattled on another defender at the same moment sets the expected expiry.
            EmbattledStatusEffect.Refresh(control, first);
            var refreshed = StatusEffect.GetStatusEffect<EmbattledStatusEffect>(defender);
            ctx.Assert(ReferenceEquals(applied, refreshed), "another hit from the same attacker keeps the active Embattled");
            ctx.AssertEqual(1, refreshed.Stacks, "the stack count is unchanged");
            ctx.AssertEqual(oneStackDefense, StatusEffect.GetStatAdjustment(defender, StatType.PhysicalDefensePercentAdjustment),
                "the refresh does not add the stack's Defense again");
            ctx.Assert(refreshed.GetRemainingDurationSeconds(DateTime.UtcNow) > 5.5f, "the hit restarts the 6 s pressure window");

            // Past the first hit's window: only the restarted window keeps the effect active.
            await ctx.DelaySecondsAsync(4.5f);
            ctx.Assert(ReferenceEquals(applied, StatusEffect.GetStatusEffect<EmbattledStatusEffect>(defender)),
                "Embattled outlasts the first hit's window");

            // The restarted window must end with the fresh one; comparing them keeps server lag out of the result.
            await ctx.DelaySecondsAsync(1f);
            ctx.Assert(StatusEffect.HasStatusEffect<EmbattledStatusEffect>(defender), "Embattled lasts the full restarted window");
            await ctx.WaitUntilAsync(() => !StatusEffect.HasStatusEffect<EmbattledStatusEffect>(defender) ||
                                           !StatusEffect.HasStatusEffect<EmbattledStatusEffect>(control), 5f,
                "the restarted or the fresh Embattled to expire");
            await ctx.WaitUntilAsync(() => !StatusEffect.HasStatusEffect<EmbattledStatusEffect>(defender) &&
                                           !StatusEffect.HasStatusEffect<EmbattledStatusEffect>(control), 0.6f,
                "the restarted Embattled to expire together with the fresh one");

            EmbattledStatusEffect.Refresh(defender, first);
            applied = StatusEffect.GetStatusEffect<EmbattledStatusEffect>(defender);
            ctx.AssertEqual(1, applied.Stacks, "a new hit starts a fresh one-stack Embattled");
            EmbattledStatusEffect.Refresh(defender, second);
            var stacked = StatusEffect.GetStatusEffect<EmbattledStatusEffect>(defender);
            ctx.AssertEqual(2, stacked.Stacks, "a second attacker adds a stack");
            ctx.Assert(!ReferenceEquals(applied, stacked), "a stack change replaces the effect");
            ctx.AssertEqual(2 * applied.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment],
                stacked.StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment], "two stacks grant twice the Defense");

            await ctx.WaitUntilAsync(() => !StatusEffect.HasStatusEffect<EmbattledStatusEffect>(defender), 10f,
                "Embattled to expire once the pressure stops");
        }
    }
}
