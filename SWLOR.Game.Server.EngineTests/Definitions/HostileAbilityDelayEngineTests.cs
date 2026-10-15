using System;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// The short delay hostile abilities share, through the real UsePerkFeat.TryUseAbility pipeline.
    /// Abilities are forced to miss so the target survives and isn't knocked around; an instant
    /// ability sets its recast when it goes off, hit or miss.
    /// </summary>
    public static class HostileAbilityDelayEngineTests
    {
        [EngineTest("A hostile ability used during the shared delay goes off when the delay ends", Category = "HostileAbilityDelay", TimeoutSeconds = 30f)]
        public static async Task HeldHostileAbility_GoesOffWhenTheDelayEnds(EngineTestContext ctx)
        {
            var (caster, target) = await SpawnAsync(ctx);
            Combat.SetAbilityHitResolutionOverride(false);
            try
            {
                var firstUsedAt = DateTime.UtcNow;
                await UseAbilityAsync(ctx, caster, target, FeatType.ForceTouch1);
                await WaitForRecastAsync(ctx, caster, RecastGroup.ForceTouch, "Force Touch to go off at once");

                await UseAbilityAsync(ctx, caster, target, FeatType.ForcePush1);
                ctx.Assert(!IsOnRecast(caster, RecastGroup.ForcePush), "Force Push is held while the delay runs, not used");

                await WaitForRecastAsync(ctx, caster, RecastGroup.ForcePush, "the held Force Push to go off");
                var waited = (DateTime.UtcNow - firstUsedAt).TotalSeconds;
                ctx.Log($"Force Push went off {waited:0.000}s after Force Touch.");
                ctx.Assert(waited >= HostileAbilityDelay.Seconds,
                    $"Force Push went off {waited:0.000}s after Force Touch, before the {HostileAbilityDelay.Seconds}s delay ended");
            }
            finally
            {
                Combat.SetAbilityHitResolutionOverride(null);
            }
        }

        [EngineTest("A later hostile ability replaces the one held for the shared delay", Category = "HostileAbilityDelay", TimeoutSeconds = 30f)]
        public static async Task LaterHostileAbility_ReplacesTheHeldOne(EngineTestContext ctx)
        {
            var (caster, target) = await SpawnAsync(ctx);
            Combat.SetAbilityHitResolutionOverride(false);
            try
            {
                await UseAbilityAsync(ctx, caster, target, FeatType.ForceTouch1);
                await WaitForRecastAsync(ctx, caster, RecastGroup.ForceTouch, "Force Touch to go off at once");

                await UseAbilityAsync(ctx, caster, target, FeatType.ForceLeap1);
                await UseAbilityAsync(ctx, caster, target, FeatType.ForcePush1);

                await WaitForRecastAsync(ctx, caster, RecastGroup.ForcePush, "the latest held ability, Force Push, to go off");
                await ctx.DelaySecondsAsync(1f);
                ctx.Assert(!IsOnRecast(caster, RecastGroup.ForceLeap), "the replaced Force Leap never goes off");
            }
            finally
            {
                Combat.SetAbilityHitResolutionOverride(null);
            }
        }

        [EngineTest("A hostile ability used just after the shared delay ends replaces one still waiting to go off", Category = "HostileAbilityDelay", TimeoutSeconds = 30f)]
        public static async Task HostileAbilityAfterTheDelay_ReplacesAPressStillWaiting(EngineTestContext ctx)
        {
            var (caster, target) = await SpawnAsync(ctx);
            Combat.SetAbilityHitResolutionOverride(false);
            try
            {
                await UseAbilityAsync(ctx, caster, target, FeatType.ForceTouch1);
                await WaitForRecastAsync(ctx, caster, RecastGroup.ForceTouch, "Force Touch to go off at once");
                await UseAbilityAsync(ctx, caster, target, FeatType.ForceLeap1);

                // The delay ends before the held Force Leap's timer runs, as it can when a frame is late.
                HostileAbilityDelay.EndForTesting(caster);
                await UseAbilityAsync(ctx, caster, target, FeatType.ForcePush1);
                await WaitForRecastAsync(ctx, caster, RecastGroup.ForcePush, "Force Push to go off at once");

                await ctx.DelaySecondsAsync(1.5f);
                ctx.Assert(!IsOnRecast(caster, RecastGroup.ForceLeap), "the replaced Force Leap never goes off");
            }
            finally
            {
                Combat.SetAbilityHitResolutionOverride(null);
            }
        }

        [EngineTest("An ability that isn't hostile doesn't wait for the shared hostile delay", Category = "HostileAbilityDelay", TimeoutSeconds = 30f)]
        public static async Task AbilityThatIsNotHostile_DoesNotWait(EngineTestContext ctx)
        {
            var (caster, target) = await SpawnAsync(ctx);
            Combat.SetAbilityHitResolutionOverride(false);
            try
            {
                var hostileUsedAt = DateTime.UtcNow;
                await UseAbilityAsync(ctx, caster, target, FeatType.ForceTouch1);
                await WaitForRecastAsync(ctx, caster, RecastGroup.ForceTouch, "Force Touch to go off at once");

                await UseAbilityAsync(ctx, caster, caster, FeatType.Innervate1);
                await WaitForRecastAsync(ctx, caster, RecastGroup.Innervate, "Innervate to go off");
                var waited = (DateTime.UtcNow - hostileUsedAt).TotalSeconds;
                ctx.Log($"Innervate went off {waited:0.000}s after Force Touch.");
                ctx.Assert(waited < HostileAbilityDelay.Seconds,
                    $"Innervate went off {waited:0.000}s after Force Touch; a heal must not wait for the hostile delay");
            }
            finally
            {
                Combat.SetAbilityHitResolutionOverride(null);
            }
        }

        private static async Task<(uint Caster, uint Target)> SpawnAsync(EngineTestContext ctx)
        {
            var caster = ctx.SpawnCreature("nw_rat001");
            var target = ctx.SpawnCreature("nw_rat001", 5f);
            SetAILevel(caster, AILevel.VeryLow);
            SetAILevel(target, AILevel.VeryLow);
            ctx.MakeHostile(target);

            // Spawn scripts reset the FP and STM locals a frame after creation.
            await ctx.WaitFrameAsync();
            ctx.SetNPCResources(caster, 50, 50);
            return (caster, target);
        }

        private static async Task UseAbilityAsync(EngineTestContext ctx, uint caster, uint target, FeatType feat)
        {
            var attempted = false;
            var used = false;
            var denial = string.Empty;
            AssignCommand(caster, () =>
            {
                used = UsePerkFeat.TryUseAbility(caster, target, feat, GetLocation(target));
                if (!used)
                    denial = Ability.GetLastActivationDenialReason();
                attempted = true;
            });
            await WaitFramesUntilAsync(ctx, () => attempted, $"{feat} to be attempted in the caster's script context");
            ctx.Assert(used, $"{feat} was refused: {denial}");
        }

        private static Task WaitForRecastAsync(EngineTestContext ctx, uint caster, RecastGroup group, string description)
        {
            return WaitFramesUntilAsync(ctx, () => IsOnRecast(caster, group), description);
        }

        // The context's own waits poll every 250ms, half the delay under test, so these check every frame.
        private static async Task WaitFramesUntilAsync(EngineTestContext ctx, Func<bool> condition, string description)
        {
            var deadline = DateTime.UtcNow.AddSeconds(3);
            while (!condition() && DateTime.UtcNow <= deadline)
                await ctx.WaitFrameAsync();

            ctx.Assert(condition(), $"Timed out after 3s waiting for {description}");
        }

        private static bool IsOnRecast(uint creature, RecastGroup group)
        {
            return Recast.IsOnRecastDelay(creature, group).Item1;
        }
    }
}
