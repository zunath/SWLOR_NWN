using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Status effects that recompute a stat on each tick must keep the creature's status-effect
    /// stat total in step while active, and must leave nothing behind when removed.
    /// </summary>
    public static class StatusEffectTickStatEngineTests
    {
        private const float TickWaitSeconds = 5f;
        private const float SettleSeconds = 3.5f;
        private const string HasteGroup = "TickStatEngineTestHaste";

        [EngineTest("Tick-time stat change: Unstable Pressure low-HP penalty reaches the stat total", Category = "StatusEffect", TimeoutSeconds = 45f)]
        public static async Task UnstablePressureLowHPPenaltyReachesStatTotal(EngineTestContext ctx)
        {
            const StatType stat = StatType.ForceDamageTakenPercentAdjustment;
            var npc = ctx.SpawnCreature("civilian");
            await ctx.DelaySecondsAsync(1f);
            ctx.SuppressNPCNaturalRegen(npc);
            SetPlotFlag(npc, false);
            Stat.SetNPCMaxHitPoints(npc, 1000, true);
            ApplyAnchorEffect(ctx, npc);
            var baseline = Stat.GetStatAdjustment(npc, stat);

            ctx.Assert(StatusEffect.ApplyStatusEffect<UnstablePressureStatusEffect>(npc, npc, 30f),
                "Unstable Pressure must apply");
            var effect = StatusEffect.GetStatusEffect<UnstablePressureStatusEffect>(npc);
            var observations = new List<string>();

            ObjectPlugin.SetCurrentHitPoints(npc, 100);
            await ctx.WaitUntilAsync(() => effect.StatGroup.Stats[stat] == 5, TickWaitSeconds,
                "a tick to apply the low-HP Force damage penalty");
            var lowHPTotal = Stat.GetStatAdjustment(npc, stat) - baseline;
            observations.Add($"low HP: effect={effect.StatGroup.Stats[stat]} total={lowHPTotal}");

            ObjectPlugin.SetCurrentHitPoints(npc, 1000);
            await ctx.WaitUntilAsync(() => effect.StatGroup.Stats[stat] == 0, TickWaitSeconds,
                "a tick to clear the low-HP Force damage penalty");
            var recoveredTotal = Stat.GetStatAdjustment(npc, stat) - baseline;
            observations.Add($"recovered: effect={effect.StatGroup.Stats[stat]} total={recoveredTotal}");

            ObjectPlugin.SetCurrentHitPoints(npc, 100);
            await ctx.WaitUntilAsync(() => effect.StatGroup.Stats[stat] == 5, TickWaitSeconds,
                "a tick to reapply the low-HP Force damage penalty");
            StatusEffect.RemoveStatusEffect<UnstablePressureStatusEffect>(npc);
            var removedTotal = Stat.GetStatAdjustment(npc, stat) - baseline;
            observations.Add($"removed at low HP: total={removedTotal}");

            var detail = string.Join("; ", observations);
            ctx.SetResultDetail(detail);
            ctx.Assert(lowHPTotal == 5 && recoveredTotal == 0 && removedTotal == 0,
                $"the stat total must follow the effect's penalty and clear on removal ({detail})");
        }

        [EngineTest("Tick-time stat change: Signal Jammer haste suppression reaches the stat total", Category = "StatusEffect", TimeoutSeconds = 45f)]
        public static async Task SignalJammerHasteSuppressionReachesStatTotal(EngineTestContext ctx)
        {
            const StatType stat = StatType.AttackDelayReductionPercent;
            var npc = ctx.SpawnCreature("civilian");
            await ctx.DelaySecondsAsync(1f);
            ApplyAnchorEffect(ctx, npc);
            ctx.AssertEqual(0, Stat.GetStatAdjustment(npc, stat), "starting haste");

            ctx.Assert(StatusEffect.ApplyStatusEffect<SignalJammerStatusEffect>(npc, npc, 30f),
                "Signal Jammer must apply");
            var effect = StatusEffect.GetStatusEffect<SignalJammerStatusEffect>(npc);
            var observations = new List<string>();

            TemporaryStatModifier.Add(npc, stat, 20, 30f, HasteGroup);
            await ctx.WaitUntilAsync(() => effect.StatGroup.Stats[stat] != 0, TickWaitSeconds,
                "a tick to suppress the new haste");
            await ctx.DelaySecondsAsync(SettleSeconds);
            var suppressedEffect = effect.StatGroup.Stats[stat];
            var suppressedTotal = Stat.GetStatAdjustment(npc, stat);
            observations.Add($"20% haste added: effect={suppressedEffect} total={suppressedTotal}");

            ctx.Assert(StatusEffect.ApplyStatusEffect<SpotterStanceStatusEffect>(npc, npc, 30f),
                "Spotter Stance must apply");
            await ctx.DelaySecondsAsync(SettleSeconds);
            var slowedEffect = effect.StatGroup.Stats[stat];
            var slowedTotal = Stat.GetStatAdjustment(npc, stat);
            observations.Add($"Spotter Stance -10% added: effect={slowedEffect} total={slowedTotal}");
            StatusEffect.RemoveStatusEffect<SpotterStanceStatusEffect>(npc);

            TemporaryStatModifier.Consume(npc, stat, HasteGroup);
            await ctx.DelaySecondsAsync(SettleSeconds);
            var clearedEffect = effect.StatGroup.Stats[stat];
            var clearedTotal = Stat.GetStatAdjustment(npc, stat);
            observations.Add($"haste removed: effect={clearedEffect} total={clearedTotal}");

            TemporaryStatModifier.Add(npc, stat, 20, 30f, HasteGroup);
            await ctx.DelaySecondsAsync(SettleSeconds);
            StatusEffect.RemoveStatusEffect<SignalJammerStatusEffect>(npc);
            var removedTotal = Stat.GetStatAdjustment(npc, stat);
            var removedStatusTotal = StatusEffect.GetStatAdjustment(npc, stat);
            observations.Add($"jammer removed with 20% haste: total={removedTotal} status-effect total={removedStatusTotal}");
            TemporaryStatModifier.Consume(npc, stat, HasteGroup);

            var detail = string.Join("; ", observations);
            ctx.SetResultDetail(detail);
            ctx.Assert(suppressedEffect == -20 && suppressedTotal == 0 &&
                       slowedEffect == -20 && slowedTotal == -10 &&
                       clearedEffect == 0 && clearedTotal == 0 &&
                       removedTotal == 20 && removedStatusTotal == 0,
                $"the jammer must cancel exactly the outside haste, keep slows, and leave no residue on removal ({detail})");
        }

        /// <summary>
        /// Keeps the creature's status-effect tracker alive after the effect under test is removed;
        /// removing a creature's last effect discards the tracker and would hide a leftover stat.
        /// </summary>
        private static void ApplyAnchorEffect(EngineTestContext ctx, uint npc)
        {
            ctx.Assert(StatusEffect.ApplyStatusEffect<KoltoMistHealingStatusEffect>(npc, npc, 60f),
                "the anchor status effect must apply");
        }
    }
}
