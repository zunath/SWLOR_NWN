using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core.Async;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.EngineTestService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.EngineTestDefinition.AbilityBehaviors
{
    /// <summary>
    /// Shared engine-side executor for AbilityBehaviorCase collections. Each case spawns a
    /// fresh caster (and target when required), drives the real UsePerkFeat.TryUseAbility
    /// pipeline, and asserts the declared observable outcomes per activation type.
    /// Failures are collected per-case so one broken ability doesn't hide the rest of a tree.
    /// </summary>
    public static class AbilityBehaviorExecutor
    {
        private const string CasterResref = "nw_rat001";
        private const string TargetResref = "nw_rat001";
        private const int ResourcePool = 9999;
        private const float EffectWaitSeconds = 20f;
        private const float CostWaitSeconds = 15f;

        /// <summary>
        /// Temporary hit points applied to hostile targets so ability damage registers as an
        /// HP drop without killing the target - deaths would route through the corpse/loot
        /// pipeline and leave non-destroyable bodies between cases.
        /// </summary>
        private const int TargetTemporaryHP = 1000;

        public static async Task RunAsync(EngineTestContext ctx, List<AbilityBehaviorCase> cases)
        {
            var failures = new List<string>();
            var skippedFeats = new List<string>();

            // Ability impacts roll Combat.TryResolveAbilityHit, which legitimately misses up to
            // 5% of the time even at capped hit rates - across hundreds of cases a sweep would
            // almost always be red from ordinary misses. Behavior cases assert what an ability
            // DOES on a hit, so ability hit resolution is forced for the duration of the sweep.
            // Auto-attacks are forced to MISS at the same time: activation resumes the caster's
            // attack 0.1s after casting, and an ordinary melee hit would satisfy a damage
            // assertion (caster is the last damager) before the ability's own impact lands.
            // Both overrides are always restored afterward.
            Combat.SetAbilityHitResolutionOverride(true);
            Combat.SetAutoAttackHitResolutionOverride(false);
            try
            {
                for (var index = 0; index < cases.Count; index++)
                {
                    var behaviorCase = cases[index];
                    var progress = $"[{index + 1}/{cases.Count}]";
                    var remaining = $"{cases.Count - index - 1} case(s) remaining";

                    // A runner timeout must stop the whole sweep promptly - continuing would
                    // outlive the cancellation grace period and keep the combat overrides
                    // active while the runner cleans up.
                    ctx.CancellationToken.ThrowIfCancellationRequested();

                    if (!string.IsNullOrWhiteSpace(behaviorCase.SkipReason))
                    {
                        skippedFeats.Add(behaviorCase.Feat.ToString());
                        ctx.Log($"{progress} SKIP {behaviorCase.Feat} - {remaining}: {behaviorCase.SkipReason}");
                        continue;
                    }

                    try
                    {
                        await RunCaseAsync(ctx, behaviorCase);
                        ctx.Log($"{progress} PASS {behaviorCase.Feat} - {remaining}");
                    }
                    catch (Exception ex)
                    {
                        if (ctx.CancellationToken.IsCancellationRequested)
                            throw;

                        var message = ex is EngineTestAssertionException
                            ? ex.Message
                            : $"{ex.GetType().Name}: {ex.Message}";
                        failures.Add($"{behaviorCase.Feat}: {message}");
                        ctx.Log($"{progress} FAIL {behaviorCase.Feat} - {remaining}: {message}");
                    }

                    await NwTask.NextFrame();
                }
            }
            finally
            {
                Combat.SetAbilityHitResolutionOverride(null);
                Combat.SetAutoAttackHitResolutionOverride(null);
            }

            var skipped = skippedFeats.Count;
            var passed = cases.Count - skipped - failures.Count;
            var summary = $"{cases.Count} case(s): {passed} passed, {failures.Count} failed, {skipped} skipped.";
            if (skipped > 0)
            {
                summary += $" Skipped: {string.Join(", ", skippedFeats)}.";
            }

            ctx.Log(summary);

            // Surfaces case-level skips in the JSON report even when the tree passes -
            // otherwise known coverage gaps would be invisible outside the log.
            ctx.SetResultDetail(summary);

            if (failures.Count > 0)
            {
                var preview = string.Join(" | ", failures.Take(5));
                var overflow = failures.Count > 5 ? " | ... see the EngineTest log for the full list" : string.Empty;
                ctx.Fail($"{failures.Count}/{cases.Count} behavior case(s) failed: {preview}{overflow}");
            }
        }

        private static async Task RunCaseAsync(EngineTestContext ctx, AbilityBehaviorCase behaviorCase)
        {
            ctx.Assert(Ability.IsFeatRegistered(behaviorCase.Feat), "feat is not registered to any ability");
            var ability = Ability.GetAbilityDetail(behaviorCase.Feat);

            var caster = ctx.SpawnCreature(CasterResref, -1.5f, 0f);
            var target = caster;

            try
            {
                if (behaviorCase.Target == AbilityTargetKind.HostileCreature)
                {
                    // 2m separation: a +/-1.5 split put the pair at exactly 3.0m, the outer
                    // boundary of short-range melee abilities, which then failed range checks.
                    target = ctx.SpawnCreature(TargetResref, 1.0f, 0f);
                    ctx.MakeHostile(target);
                    ApplyEffectToObject(
                        DurationType.Temporary,
                        EffectTemporaryHitpoints(TargetTemporaryHP),
                        target,
                        3600f);
                }

                // Let spawn initialization scripts run before configuring resources - they
                // reset the FP/STAMINA locals to the (unraised) max and would overwrite us.
                await NwTask.NextFrame();
                ctx.SetNPCResources(caster, ResourcePool, ResourcePool);

                if (!string.IsNullOrWhiteSpace(behaviorCase.EquipMainHandResref))
                {
                    await ctx.EquipItemAsync(caster, behaviorCase.EquipMainHandResref, InventorySlot.RightHand);
                }

                var fpBefore = Stat.GetCurrentFP(caster);
                var stmBefore = Stat.GetCurrentStamina(caster);
                var targetHPBefore = GetCurrentHitPoints(target);
                var activatorStatAdjustmentsBefore = behaviorCase.ExpectedActivatorStatAdjustments
                    .ToDictionary(pair => pair.Key, pair => Stat.GetStatAdjustment(caster, pair.Key));

                // The activation must run in the CASTER's script context, exactly like the real
                // feat-use event: DelayCommand(activationDelay, CompleteActivation) schedules
                // against OBJECT_SELF, and the engine evaluates line-of-sight in OBJECT_SELF's
                // area. Called directly from the async runner (module context), casted impacts
                // never fire and every LOS check fails.
                var used = false;
                var activationAttempted = false;
                AssignCommand(caster, () =>
                {
                    used = UsePerkFeat.TryUseAbility(caster, target, behaviorCase.Feat, GetLocation(target));
                    activationAttempted = true;
                });
                await ctx.WaitUntilAsync(
                    () => activationAttempted,
                    5f,
                    "the assigned activation command to execute in the caster's context");

                if (!used)
                {
                    var denial = Ability.GetLastActivationDenialReason();
                    ctx.Fail(string.IsNullOrWhiteSpace(denial)
                        ? "TryUseAbility returned false - activation requirements were not met (no denial reason was recorded)"
                        : $"TryUseAbility returned false - {denial}");
                }

                if (ability.ActivationType == AbilityActivationType.Weapon)
                {
                    // Weapon abilities queue for the next landed hit; costs and recast apply at
                    // queue time. Landing the hit is combat-timing dependent, so queue state is
                    // the assertion boundary here.
                    ctx.Assert(
                        UsePerkFeat.IsWeaponAbilityQueued(caster, behaviorCase.Feat),
                        "weapon ability was not queued after activation");

                    AssertCosts(ctx, behaviorCase, caster, fpBefore, stmBefore);
                }
                else
                {
                    foreach (var effectType in behaviorCase.ExpectedActivatorStatusEffects)
                    {
                        await ctx.WaitUntilAsync(
                            () => StatusEffect.HasStatusEffect(caster, effectType),
                            EffectWaitSeconds,
                            $"activator status effect {effectType.Name} after impact");
                    }

                    foreach (var effectType in behaviorCase.ExpectedTargetStatusEffects)
                    {
                        await ctx.WaitUntilAsync(
                            () => StatusEffect.HasStatusEffect(target, effectType),
                            EffectWaitSeconds,
                            $"target status effect {effectType.Name} after impact");
                    }

                    foreach (var (statType, expectedAdjustment) in behaviorCase.ExpectedActivatorStatAdjustments)
                    {
                        var expectedValue = activatorStatAdjustmentsBefore[statType] + expectedAdjustment;
                        await ctx.WaitUntilAsync(
                            () => Stat.GetStatAdjustment(caster, statType) == expectedValue,
                            EffectWaitSeconds,
                            $"activator stat {statType} to change by {expectedAdjustment} after impact");
                    }

                    if (behaviorCase.ExpectsTargetDamage)
                    {
                        // Requiring the caster as last damager proves the damage came from this
                        // ability rather than a placed arena creature engaging the hostile
                        // target. Abilities whose damage arrives via a placeable (traps) may
                        // need per-case relaxation once validated on a live server.
                        await ctx.WaitUntilAsync(
                            () => GetCurrentHitPoints(target) < targetHPBefore &&
                                  GetLastDamager(target) == caster,
                            EffectWaitSeconds,
                            "this ability's damage (caster as last damager) to lower the target's hit points");
                    }

                    // Casted costs apply when activation completes (after the activation delay),
                    // so poll rather than assert immediately.
                    if (behaviorCase.ExpectsFPCost)
                    {
                        await ctx.WaitUntilAsync(
                            () => Stat.GetCurrentFP(caster) < fpBefore,
                            CostWaitSeconds,
                            "FP cost to be deducted");
                    }

                    if (behaviorCase.ExpectsSTMCost)
                    {
                        await ctx.WaitUntilAsync(
                            () => Stat.GetCurrentStamina(caster) < stmBefore,
                            CostWaitSeconds,
                            "Stamina cost to be deducted");
                    }
                }

                if (behaviorCase.ExpectsRecast)
                {
                    await ctx.WaitUntilAsync(
                        () => Recast.IsOnRecastDelay(caster, ability.RecastGroup).Item1,
                        CostWaitSeconds,
                        $"recast group {ability.RecastGroup} to be on cooldown");
                }
            }
            finally
            {
                // Fresh actors per case: destroy immediately rather than letting hundreds
                // accumulate until the tree test's cleanup.
                if (target != caster)
                    DestroyCaseActor(target);
                DestroyCaseActor(caster);
            }
        }

        /// <summary>
        /// Destroys a per-case actor, restoring destroyability first: a creature killed
        /// mid-case is marked non-destroyable by the death/loot pipeline and would otherwise
        /// linger at the shared spawn point for the rest of the sweep.
        /// </summary>
        private static void DestroyCaseActor(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return;

            // Both calls run inside one assigned context: SetIsDestroyable executes
            // immediately there (it operates on OBJECT_SELF, so it cannot be called
            // directly from this context), and the deferred destruction is processed
            // after that same context ends - guaranteeing the flag is set first.
            AssignCommand(creature, () =>
            {
                SetIsDestroyable(true, false, false);
                DestroyObject(creature);
            });
        }

        private static void AssertCosts(
            EngineTestContext ctx,
            AbilityBehaviorCase behaviorCase,
            uint caster,
            int fpBefore,
            int stmBefore)
        {
            if (behaviorCase.ExpectsFPCost)
            {
                ctx.Assert(Stat.GetCurrentFP(caster) < fpBefore, "FP cost was not deducted at queue time");
            }

            if (behaviorCase.ExpectsSTMCost)
            {
                ctx.Assert(Stat.GetCurrentStamina(caster) < stmBefore, "Stamina cost was not deducted at queue time");
            }
        }
    }
}
