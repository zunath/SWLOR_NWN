using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWNX;
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
                // Both happen in one script run so that timer can't run in between.
                await UseAbilityAsync(ctx, caster, target, FeatType.ForcePush1, () => HostileAbilityDelay.EndForTesting(caster));
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

        [EngineTest("A player's Rib Breaker and Slam pressed a frame apart land half a second apart", Category = "HostileAbilityDelay", TimeoutSeconds = 60f)]
        public static async Task PlayerWeaponAbilitiesPressedTogether_LandHalfASecondApart(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            var target = await SetUpStaffPlayerAsync(ctx, fixture);
            await AssertStaffAbilitiesLandApartAsync(ctx, player, target, async () =>
            {
                await UseAbilityAsync(ctx, player, target, FeatType.RibBreaker1);
                await ctx.WaitFrameAsync();
                await UseAbilityAsync(ctx, player, target, FeatType.Slam1);
            });
        }

        [EngineTest("A player's Rib Breaker and Slam used through the feat event land half a second apart", Category = "HostileAbilityDelay", TimeoutSeconds = 60f)]
        public static async Task PlayerWeaponAbilitiesThroughTheFeatEvent_LandHalfASecondApart(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            var target = await SetUpStaffPlayerAsync(ctx, fixture);
            CreaturePlugin.AddFeat(player, FeatType.RibBreaker1);
            CreaturePlugin.AddFeat(player, FeatType.Slam1);
            // The second press arrives a couple of frames later, as a client's would.
            await AssertStaffAbilitiesLandApartAsync(ctx, player, target, async () =>
            {
                AssignCommand(player, () => ActionUseFeat(FeatType.RibBreaker1, target));
                await ctx.WaitFrameAsync();
                await ctx.WaitFrameAsync();
                AssignCommand(player, () => ActionUseFeat(FeatType.Slam1, target));
            });
        }

        private static async Task<uint> SetUpStaffPlayerAsync(EngineTestContext ctx, PlayerAbilityFixture fixture)
        {
            var player = fixture.Creature;
            fixture.Update(record =>
            {
                record.Skills[SkillType.Staff].Rank = 50;
                record.Perks[PerkType.RibBreaker] = 1;
                record.Perks[PerkType.Slam] = 1;
            });
            await ctx.EquipItemAsync(player, "nw_wdbqs001", InventorySlot.RightHand);
            var target = ctx.SpawnCreature("nw_rat001", 2f);
            await ctx.WaitFrameAsync();
            SetAILevel(target, AILevel.VeryLow);
            ctx.MakeHostile(target);
            ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(5000), target, 120f);
            return target;
        }

        // Frame checks can read an ability's time up to a frame late.
        private const double FrameTolerance = 0.1;

        // Watches every frame for each ability's recast and each drop in the target's HP.
        private static async Task AssertStaffAbilitiesLandApartAsync(EngineTestContext ctx, uint player, uint target, Func<Task> press)
        {
            Combat.SetAbilityHitResolutionOverride(true);
            try
            {
                var startedAt = DateTime.UtcNow;
                var lastHitPoints = GetCurrentHitPoints(target);
                DateTime? ribBreakerAt = null;
                DateTime? slamAt = null;
                var hits = new List<string>();
                var hitTimes = new List<DateTime>();

                void Observe()
                {
                    var now = DateTime.UtcNow;
                    if (ribBreakerAt == null && IsOnRecast(player, RecastGroup.RibBreaker))
                        ribBreakerAt = now;
                    if (slamAt == null && IsOnRecast(player, RecastGroup.Slam))
                        slamAt = now;
                    var hitPoints = GetCurrentHitPoints(target);
                    if (hitPoints < lastHitPoints)
                    {
                        hits.Add($"-{lastHitPoints - hitPoints} at {(now - startedAt).TotalSeconds:0.000}s");
                        hitTimes.Add(now);
                    }
                    lastHitPoints = hitPoints;
                }

                async Task ObserveAsync()
                {
                    var deadline = DateTime.UtcNow.AddSeconds(3);
                    while (DateTime.UtcNow <= deadline)
                    {
                        await ctx.WaitFrameAsync();
                        Observe();
                    }
                }

                // The watch runs alongside the presses so neither ability's time is read late.
                var observing = ObserveAsync();
                await press();
                await observing;

                ctx.Log($"Rib Breaker went off at {(ribBreakerAt - startedAt)?.TotalSeconds:0.000}s, Slam at {(slamAt - startedAt)?.TotalSeconds:0.000}s; target hits: {string.Join(", ", hits)}");
                ctx.Assert(ribBreakerAt != null, "Rib Breaker went off");
                ctx.Assert(slamAt != null, "Slam went off");
                var gap = Math.Abs((slamAt.Value - ribBreakerAt.Value).TotalSeconds);
                ctx.Assert(gap >= HostileAbilityDelay.Seconds - FrameTolerance,
                    $"Slam went off {gap:0.000}s after Rib Breaker, inside the {HostileAbilityDelay.Seconds}s delay");
                ctx.Assert(hitTimes.Count >= 2, $"Both abilities hit the target: {string.Join(", ", hits)}");
                var damageGap = (hitTimes[hitTimes.Count - 1] - hitTimes[0]).TotalSeconds;
                ctx.Assert(damageGap >= HostileAbilityDelay.Seconds - FrameTolerance,
                    $"The hits landed {damageGap:0.000}s apart, inside the {HostileAbilityDelay.Seconds}s delay");
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

        private static async Task UseAbilityAsync(EngineTestContext ctx, uint caster, uint target, FeatType feat, Action beforeUse = null)
        {
            var attempted = false;
            var used = false;
            var denial = string.Empty;
            AssignCommand(caster, () =>
            {
                beforeUse?.Invoke();
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
