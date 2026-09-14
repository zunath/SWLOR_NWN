using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.EngineTests.Framework;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Exercises the status effect pipeline directly (independent of any ability) using
    /// RegenerativeHealingStatusEffect's parameterless constructor - a simple buff with no
    /// CanApply requirements and a no-op Tick (0% heal) - to confirm application, natural
    /// expiration, and explicit removal all work inside the live engine.
    /// </summary>
    public static class StatusEffectEngineTests
    {
        /// <summary>
        /// Status effects tick on the shared status-effect interval (about every 6 real seconds),
        /// so expiration checks need generous margin past the effect's nominal duration.
        /// </summary>
        private const float ExpirationWaitTimeoutSeconds = 40f;

        [EngineTest("Refreshing a passive status preserves it until the renewed timer expires", Category = "StatusEffect", TimeoutSeconds = 20f)]
        public static async Task RefreshedPassiveStatusExpiresNormally(EngineTestContext ctx)
        {
            var npc = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            StatusEffect.ApplyStatusEffect<KoltoMistHealingStatusEffect>(npc, npc, 2f);
            var effect = StatusEffect.GetStatusEffect(npc, typeof(KoltoMistHealingStatusEffect));

            ctx.Assert(StatusEffect.RefreshStatusEffectDuration(npc, typeof(KoltoMistHealingStatusEffect), npc, 5f),
                "the passive status must accept a duration refresh");
            await ctx.DelaySecondsAsync(3f);
            ctx.Assert(StatusEffect.GetStatusEffect(npc, typeof(KoltoMistHealingStatusEffect)) == effect,
                "the refreshed status must survive both the old removal callback and its original expiration");
            await ctx.WaitUntilAsync(() => !StatusEffect.HasStatusEffect<KoltoMistHealingStatusEffect>(npc),
                5f, "the renewed passive timer to expire");
        }

        [EngineTest("Native removal still clears a renewed status", Category = "StatusEffect", TimeoutSeconds = 15f)]
        public static async Task NativeRemovalClearsRenewedStatus(EngineTestContext ctx)
        {
            var npc = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            StatusEffect.ApplyStatusEffect<KoltoMistHealingStatusEffect>(npc, npc, 60f);
            StatusEffect.ExtendStatusEffectDuration(npc, typeof(KoltoMistHealingStatusEffect), npc, 6f);
            var effect = StatusEffect.GetStatusEffect(npc, typeof(KoltoMistHealingStatusEffect));
            ctx.Assert(effect != null, "the renewed status must be present before native removal");
            RemoveEffectByTag(npc, effect.Id);

            await ctx.WaitUntilAsync(() => !StatusEffect.HasStatusEffect<KoltoMistHealingStatusEffect>(npc),
                5f, "native removal to clear the managed status");
        }

        [EngineTest("A directly applied status effect appears and naturally expires", Category = "StatusEffect", TimeoutSeconds = 60f)]
        public static async Task StatusEffectAppliesAndExpires(EngineTestContext ctx)
        {
            var npc = ctx.SpawnCreature("nw_rat001");

            var applied = StatusEffect.ApplyStatusEffect<RegenerativeHealingStatusEffect>(npc, npc, 5f);
            ctx.Assert(applied, "ApplyStatusEffect should report success.");
            ctx.Assert(StatusEffect.HasStatusEffect<RegenerativeHealingStatusEffect>(npc), "Status effect should be present immediately after being applied.");

            await ctx.WaitUntilAsync(
                () => !StatusEffect.HasStatusEffect<RegenerativeHealingStatusEffect>(npc),
                ExpirationWaitTimeoutSeconds,
                "the status effect to naturally expire past its 5s duration");
        }

        [EngineTest("RemoveStatusEffect clears an active status effect immediately", Category = "StatusEffect")]
        public static async Task RemoveStatusEffectClearsImmediately(EngineTestContext ctx)
        {
            var npc = ctx.SpawnCreature("nw_rat001");

            StatusEffect.ApplyStatusEffect<RegenerativeHealingStatusEffect>(npc, npc, 60f);
            ctx.Assert(StatusEffect.HasStatusEffect<RegenerativeHealingStatusEffect>(npc), "Status effect should be present after being applied.");

            StatusEffect.RemoveStatusEffect<RegenerativeHealingStatusEffect>(npc);

            // Removal is expected to be synchronous, but poll briefly in case any cleanup is
            // deferred to a subsequent frame or interval tick.
            await ctx.WaitUntilAsync(
                () => !StatusEffect.HasStatusEffect<RegenerativeHealingStatusEffect>(npc),
                5f,
                "the status effect to be cleared immediately after RemoveStatusEffect");
        }

        [EngineTest("An active hard-control status rejects same-type refresh", Category = "StatusEffect")]
        public static Task ActiveHardControlRejectsSameTypeRefresh(EngineTestContext ctx)
        {
            var npc = ctx.SpawnCreature("nw_rat001");

            var firstApplied = StatusEffect.ApplyStatusEffect<StunnedStatusEffect>(npc, npc, 30f);
            ctx.Assert(firstApplied, "the initial Stunned status should apply");
            var original = StatusEffect.GetCreatureStatusEffects(npc)
                .GetAllEffects()
                .Single(effect => effect is StunnedStatusEffect);

            var refreshApplied = StatusEffect.ApplyStatusEffect<StunnedStatusEffect>(npc, npc, 30f);
            ctx.Assert(!refreshApplied, "an active hard-control status must reject a same-type refresh");

            var activeStuns = StatusEffect.GetCreatureStatusEffects(npc)
                .GetAllEffects()
                .Where(effect => effect is StunnedStatusEffect)
                .ToList();
            ctx.Assert(activeStuns.Count == 1, "the rejected refresh must leave exactly one Stunned status");
            ctx.Assert(activeStuns[0].Id == original.Id, "the rejected refresh must preserve the original status instance");

            return Task.CompletedTask;
        }
    }
}
