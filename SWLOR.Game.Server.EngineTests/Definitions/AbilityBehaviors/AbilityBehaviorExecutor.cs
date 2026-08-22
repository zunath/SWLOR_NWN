using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core.Async;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.AbilityBehaviors
{
    /// <summary>
    /// Shared engine-side executor for AbilityBehaviorCase collections. Each case spawns a
    /// fresh caster (and target when required), drives the real UsePerkFeat.TryUseAbility
    /// pipeline, and asserts the declared observable outcomes per activation type.
    /// Failures are collected per-case so one broken ability doesn't hide the rest of a tree.
    /// </summary>
    public static class AbilityBehaviorExecutor
    {
        // The caster must be a humanoid: creatures like rats cannot equip weapons at all,
        // which silently breaks every weapon-gated tree (ActionEquipItem never completes).
        private const string CasterResref = "nw_bandit001";
        private const string TargetResref = "nw_rat001";
        private const string FriendlyTargetResref = "nw_bandit001";

        // Equipped for Weapon-activation cases that declare no weapon of their own: queued
        // abilities consume through the weapon's item_on_hit event, so the caster must be
        // armed to land the consuming hit. A stock shortsword keeps the fixture neutral.
        private const string FallbackWeaponResref = "nw_wswss001";
        private const int ResourcePool = 9999;
        private const float EffectWaitSeconds = 20f;
        private const float CostWaitSeconds = 15f;

        /// <summary>
        /// Temporary hit points applied to hostile targets so ability damage registers as an
        /// HP drop without killing the target - deaths would route through the corpse/loot
        /// pipeline and leave non-destroyable bodies between cases.
        /// </summary>
        private const int TargetTemporaryHP = 1000;

        /// <summary>
        /// A tree sweep aborts once this many cases have failed: that volume signals a
        /// systemic problem (broken fixture, dead arena, bad build) rather than individual
        /// ability bugs, and running out the remaining cases just burns the suite's wall
        /// clock producing hundreds of copies of the same failure.
        /// </summary>
        private const int SystemicFailureThreshold = 25;

        public static async Task RunAsync(EngineTestContext ctx, List<AbilityBehaviorCase> cases)
        {
            var failures = new List<string>();
            var skippedFeats = new List<string>();
            var abortedAsSystemic = false;
            var passedCount = 0;

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
                        passedCount++;
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

                        if (failures.Count >= SystemicFailureThreshold)
                        {
                            abortedAsSystemic = true;
                            ctx.Log($"{progress} ABORT - {failures.Count} failures reached the systemic-failure threshold ({SystemicFailureThreshold}); skipping the remaining {cases.Count - index - 1} case(s).");

                            // A failure volume this high means a SHARED fixture or impact
                            // path is broken, not this tree specifically - every remaining
                            // tree would repeat the same 20s-per-case timed-out failures
                            // and blow the CI job budget before the report is written.
                            EngineTest.RequestSuiteAbort(
                                $"behavior sweep hit {failures.Count} case failures in one tree - a shared fixture/impact regression would repeat in every remaining tree");
                            break;
                        }
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
            var passed = passedCount;
            var summary = $"{cases.Count} case(s): {passed} passed, {failures.Count} failed, {skipped} skipped.";
            if (abortedAsSystemic)
            {
                var notRun = cases.Count - passed - failures.Count - skipped;
                summary += $" ABORTED as systemic after {failures.Count} failures; {notRun} case(s) not run.";
            }
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

            var caster = ctx.SpawnCreature(CasterResref, -0.5f, 0f);
            var target = caster;

            try
            {
                if (behaviorCase.Target == AbilityTargetKind.HostileCreature)
                {
                    // 2m separation: a +/-1.5 split put the pair at exactly 3.0m, the outer
                    // boundary of short-range melee abilities, which then failed range checks.
                    target = ctx.SpawnCreature(TargetResref, -0.5f + behaviorCase.TargetDistanceMeters, 0f);
                    ctx.MakeHostile(target);
                    ApplyEffectToObject(
                        DurationType.Temporary,
                        EffectTemporaryHitpoints(TargetTemporaryHP),
                        target,
                        3600f);
                }
                else if (behaviorCase.Target == AbilityTargetKind.FriendlyCreature)
                {
                    // Same faction as the caster (SpawnCreature normalizes to Defender), so
                    // friendly-target validation accepts it without allowing self. The stock
                    // rat has only 1 HP, so a durable humanoid is required for ally-healing cases
                    // that deliberately wound the target before activation.
                    target = ctx.SpawnCreature(FriendlyTargetResref, -0.5f + behaviorCase.TargetDistanceMeters, 0f);
                }

                if (behaviorCase.TargetStartsDead)
                {
                    ApplyEffectToObject(DurationType.Instant, EffectDeath(), target);
                    await ctx.WaitUntilAsync(
                        () => GetIsDead(target),
                        5f,
                        "the target to be dead before activation");

                    // The death event's Loot.ProcessCorpse spawns an untracked "corpse"
                    // placeable with a 360s lifetime beside the body; left alone, one
                    // accumulates at the shared spawn point per revival case and outlives
                    // the whole sweep. Destroying it early is safe - the scheduled corpse
                    // cleanup no-ops on an already-destroyed placeable.
                    await NwTask.NextFrame();
                    DestroyCorpsePlaceable(target);
                }

                if (behaviorCase.TargetJoinsCasterParty)
                {
                    // AddHenchman fires the associate-add event, which routes through the real
                    // Party service registration - the same path live gameplay uses.
                    AssignCommand(caster, () => AddHenchman(caster, target));
                    await ctx.WaitUntilAsync(
                        () => Party.IsInParty(caster, target),
                        5f,
                        "the spawned ally to join the caster's party");
                }

                if (behaviorCase.TargetSetupStatusEffectFactory != null)
                {
                    // ReassignSource before applying: source-tracked effects (e.g. Guarded)
                    // register the target against the SOURCE stored on the instance, and a
                    // factory-created instance has none until it is assigned.
                    var setupEffect = behaviorCase.TargetSetupStatusEffectFactory();
                    setupEffect.ReassignSource(caster);
                    StatusEffect.ApplyStatusEffect(caster, target, setupEffect, 60f);
                }

                foreach (var setupEffectType in behaviorCase.TargetSetupStatusEffects)
                {
                    StatusEffect.ApplyStatusEffect(caster, target, setupEffectType, 60f);
                }

                // Let spawn initialization scripts run before configuring resources - they
                // reset the FP/STAMINA locals to the (unraised) max and would overwrite us.
                await NwTask.NextFrame();
                ctx.SetNPCResources(caster, ResourcePool, ResourcePool);

                // Out-of-combat NPCs heal 10% HP and restore 1 FP/STM per heartbeat tick;
                // inside a 20s assertion window that would satisfy a healing assertion on a
                // deliberately wounded caster even when the tested impact is broken, and
                // drift resource pools off the exact post-deduction values the cost
                // assertions verify.
                ctx.SuppressNPCNaturalRegen(caster);
                if (target != caster)
                    ctx.SuppressNPCNaturalRegen(target);

                foreach (var setupEffectType in behaviorCase.TargetSetupStatusEffects)
                {
                    await ctx.WaitUntilAsync(
                        () => StatusEffect.HasStatusEffect(target, setupEffectType),
                        5f,
                        $"setup target status effect {setupEffectType.Name} before activation");
                }

                foreach (var (setupPerk, setupLevel) in behaviorCase.SetupNPCPerkLevels)
                {
                    ctx.SetNPCPerkLevel(caster, setupPerk, setupLevel);
                }

                if (!string.IsNullOrWhiteSpace(behaviorCase.EquipMainHandResref))
                {
                    await ctx.EquipItemAsync(caster, behaviorCase.EquipMainHandResref, InventorySlot.RightHand);
                }

                if (ability.ActivationType == AbilityActivationType.Weapon)
                {
                    // Queued abilities consume through the weapon's item_on_hit event. This
                    // must be settled BEFORE activation: equipping fires the equip-validation
                    // event, which clears any queued ability.
                    var mainHand = GetItemInSlot(InventorySlot.RightHand, caster);
                    if (!GetIsObjectValid(mainHand))
                    {
                        mainHand = await ctx.EquipItemAsync(caster, FallbackWeaponResref, InventorySlot.RightHand);
                    }

                    // Blueprint-supplied weapons never passed through the fixture's equip
                    // path, so the live pipeline's PC-only OnHitCastSpell property is
                    // mirrored here for them too.
                    ctx.ApplyStandardOnHitProperty(mainHand);
                }

                if (behaviorCase.ExpectsActivatorHealing)
                {
                    // A full-health activator cannot show healing; wound it first.
                    ApplyEffectToObject(
                        DurationType.Instant,
                        EffectDamage(Math.Max(1, GetCurrentHitPoints(caster) / 2)),
                        caster);
                }

                if (behaviorCase.ExpectsTargetHealing)
                {
                    ctx.Assert(
                        target != caster && behaviorCase.Target == AbilityTargetKind.FriendlyCreature,
                        "target-healing assertions require a distinct FriendlyCreature target");
                    ApplyEffectToObject(
                        DurationType.Instant,
                        EffectDamage(Math.Max(1, GetCurrentHitPoints(target) / 2)),
                        target);
                }

                if (behaviorCase.TargetHitPointPercentBeforeActivation.HasValue)
                {
                    var targetHitPointPercent = behaviorCase.TargetHitPointPercentBeforeActivation.Value;
                    ctx.Assert(
                        targetHitPointPercent is >= 1 and <= 100,
                        "target HP setup percentage must be between 1 and 100");
                    var targetMaximumHitPoints = GetMaxHitPoints(target);
                    var desiredTargetHitPoints = Math.Max(1, targetMaximumHitPoints * targetHitPointPercent / 100);
                    var targetDamage = GetCurrentHitPoints(target) - desiredTargetHitPoints;
                    ctx.Assert(targetDamage >= 0, "target HP setup cannot raise hit points");
                    if (targetDamage > 0)
                    {
                        ApplyEffectToObject(DurationType.Instant, EffectDamage(targetDamage), target);
                    }

                    ctx.Assert(
                        GetCurrentHitPoints(target) == desiredTargetHitPoints,
                        $"target HP setup expected {desiredTargetHitPoints}/{targetMaximumHitPoints} before activation");
                }

                if (behaviorCase.MaximumActivatorDistanceToTargetAfterImpact.HasValue)
                {
                    ctx.Assert(
                        target != caster,
                        "post-impact distance assertions require a distinct target");
                    ctx.Assert(
                        GetDistanceBetween(caster, target) >
                        behaviorCase.MaximumActivatorDistanceToTargetAfterImpact.Value,
                        "the target must start outside the asserted post-impact distance or the movement assertion is vacuous");
                }

                foreach (var effectType in behaviorCase.ExpectedActivatorStatusEffects)
                {
                    ctx.Assert(
                        !StatusEffect.HasStatusEffect(caster, effectType),
                        $"activator status effect {effectType.Name} was already present before activation");
                }

                foreach (var effectType in behaviorCase.ExpectedTargetStatusEffects)
                {
                    ctx.Assert(
                        !StatusEffect.HasStatusEffect(target, effectType),
                        $"target status effect {effectType.Name} was already present before activation");
                }

                var fpBefore = Stat.GetCurrentFP(caster);
                var stmBefore = Stat.GetCurrentStamina(caster);
                var casterHPBefore = GetCurrentHitPoints(caster);
                var targetHPBefore = GetCurrentHitPoints(target);
                var activatorStatAdjustmentsBefore = behaviorCase.ExpectedActivatorStatAdjustments
                    .ToDictionary(pair => pair.Key, pair => Stat.GetStatAdjustment(caster, pair.Key));
                var targetStatAdjustmentsBefore = behaviorCase.ExpectedTargetStatAdjustments
                    .ToDictionary(pair => pair.Key, pair => Stat.GetStatAdjustment(target, pair.Key));
                var activatorTemporaryHPBefore = CountEffectsOfType(caster, EffectTypeScript.TemporaryHitpoints);
                var targetTemporaryHPBefore = CountEffectsOfType(target, EffectTypeScript.TemporaryHitpoints);

                // The activation must run in the CASTER's script context, exactly like the real
                // feat-use event: DelayCommand(activationDelay, CompleteActivation) schedules
                // against OBJECT_SELF, and the engine evaluates line-of-sight in OBJECT_SELF's
                // area. Called directly from the async runner (module context), casted impacts
                // never fire and every LOS check fails. Commands assigned while the creature is
                // briefly uncommandable (e.g. right after equips) are dropped silently, and the
                // window can reopen between a check and execution - so the assignment retries.
                var used = false;
                var activationAttempted = false;
                var activationDenial = string.Empty;
                Exception activationError = null;
                for (var attempt = 0; attempt < 3 && !activationAttempted; attempt++)
                {
                    await ctx.WaitUntilAsync(
                        () => GetCommandable(caster),
                        5f,
                        "the caster to become commandable before activation");

                    AssignCommand(caster, () =>
                    {
                        // Attempted is flagged FIRST and the call is guarded: an exception
                        // inside an assigned context is otherwise swallowed into the Error log
                        // and looks identical to the command never executing.
                        activationAttempted = true;
                        try
                        {
                            used = UsePerkFeat.TryUseAbility(caster, target, behaviorCase.Feat, GetLocation(target));

                            // Captured HERE, not after the next yield: the denial reason is a
                            // process-wide slot that any other creature's CanUseAbility call
                            // (AI, arena bystanders) can overwrite within a frame.
                            if (!used)
                            {
                                activationDenial = Ability.GetLastActivationDenialReason();
                            }
                        }
                        catch (Exception ex)
                        {
                            activationError = ex;
                        }
                    });

                    var deadline = DateTime.UtcNow.AddSeconds(2);
                    while (!activationAttempted && DateTime.UtcNow < deadline)
                    {
                        await NwTask.Delay(TimeSpan.FromMilliseconds(100), ctx.CancellationToken);
                        ctx.CancellationToken.ThrowIfCancellationRequested();
                    }
                }

                ctx.Assert(
                    activationAttempted,
                    "the assigned activation command never executed in the caster's context after 3 attempts");

                if (activationError != null)
                {
                    ctx.Fail($"activation threw {activationError.GetType().Name}: {activationError.Message}");
                }

                if (!used)
                {
                    ctx.Fail(string.IsNullOrWhiteSpace(activationDenial)
                        ? "TryUseAbility returned false - activation requirements were not met (no denial reason was recorded)"
                        : $"TryUseAbility returned false - {activationDenial}");
                }

                if (ability.ActivationType == AbilityActivationType.Weapon)
                {
                    // Weapon abilities queue for the next landed hit; costs and recast apply
                    // at queue time.
                    ctx.Assert(
                        UsePerkFeat.IsWeaponAbilityQueued(caster, behaviorCase.Feat),
                        "weapon ability was not queued after activation");

                    AssertCosts(ctx, behaviorCase, ability, caster, fpBefore, stmBefore);

                    // Land the queued hit so the on-hit impact pipeline (impact, riders,
                    // dequeue, native hook integration) is actually exercised. The sweep-wide
                    // override forces auto-attack MISSES to protect damage attribution, so it
                    // is flipped to forced HITS for just this attack. Consumption within the
                    // wait window proves a landed hit - queue expiry alone takes 30s.
                    var hitTarget = target != caster ? target : OBJECT_INVALID;
                    var spawnedHitDummy = false;
                    if (hitTarget == OBJECT_INVALID)
                    {
                        hitTarget = ctx.SpawnCreature(TargetResref, 1.0f, 0.5f);
                        ctx.MakeHostile(hitTarget);
                        ApplyEffectToObject(
                            DurationType.Temporary,
                            EffectTemporaryHitpoints(TargetTemporaryHP),
                            hitTarget,
                            3600f);
                        spawnedHitDummy = true;
                    }

                    try
                    {
                        var hitTargetHPBefore = GetCurrentHitPoints(hitTarget);
                        var hitTargetStatAdjustmentsBefore = behaviorCase.ExpectedTargetStatAdjustments
                            .ToDictionary(pair => pair.Key, pair => Stat.GetStatAdjustment(hitTarget, pair.Key));
                        var hitTargetTemporaryHPBefore =
                            CountEffectsOfType(hitTarget, EffectTypeScript.TemporaryHitpoints);

                        // Cleared so a post-consumption observation can only match THIS
                        // ability's completed impact, never an earlier case's.
                        Ability.ClearLastCompletedAbilityImpactSummary(caster);

                        Combat.SetAutoAttackHitResolutionOverride(true);
                        try
                        {
                            AssignCommand(caster, () => ActionAttack(hitTarget));
                            await ctx.WaitUntilAsync(
                                () => !UsePerkFeat.IsWeaponAbilityQueued(caster, behaviorCase.Feat),
                                EffectWaitSeconds,
                                "the queued weapon ability to be consumed by a landed hit");
                        }
                        finally
                        {
                            Combat.SetAutoAttackHitResolutionOverride(false);
                            AssignCommand(caster, () => ClearAllActions());
                        }

                        if (behaviorCase.ExpectsTargetDamage)
                        {
                            // The ability's damage rides the same landed hit as the weapon
                            // swing, so the HP-drop check below cannot attribute damage to
                            // the ability - and ImpactedTargetCount alone is insufficient
                            // (the impact records targets it merely VISITED, even at zero
                            // damage). AttributedDamage counts only damage the ability's own
                            // impact actually queued.
                            var impactSummary = Ability.GetLastCompletedAbilityImpactSummary(caster);
                            ctx.Assert(
                                impactSummary is { AttributedDamage: > 0 },
                                $"the queued ability's completed impact to attribute damage of its own (summary: {(impactSummary == null ? "none" : $"targets={impactSummary.ImpactedTargetCount}, attributedDamage={impactSummary.AttributedDamage}")})");
                        }

                        // Declared outcomes are verified against the creature the hit actually
                        // landed on - the on-hit impact pipeline runs against it, not the
                        // (possibly Self) activation target. Without this, a queued ability
                        // whose impact does nothing would still pass on queue/cost/recast alone.
                        await AssertImpactOutcomesAsync(
                            ctx,
                            behaviorCase,
                            caster,
                            hitTarget,
                            hitTargetHPBefore,
                            casterHPBefore,
                            activatorStatAdjustmentsBefore,
                            hitTargetStatAdjustmentsBefore,
                            activatorTemporaryHPBefore,
                            hitTargetTemporaryHPBefore);
                    }
                    finally
                    {
                        if (spawnedHitDummy)
                        {
                            DestroyCaseActor(hitTarget);
                        }
                    }
                }
                else
                {
                    // Costs are checked FIRST: they apply the moment activation completes,
                    // before impact effects, and small costs can be re-masked by regen while
                    // waiting on slow delayed impacts (pulse emitters, telegraphs).
                    await AssertCastedCostsAsync(ctx, behaviorCase, ability, caster, fpBefore, stmBefore);

                    await AssertImpactOutcomesAsync(
                        ctx,
                        behaviorCase,
                        caster,
                        target,
                        targetHPBefore,
                        casterHPBefore,
                        activatorStatAdjustmentsBefore,
                        targetStatAdjustmentsBefore,
                        activatorTemporaryHPBefore,
                        targetTemporaryHPBefore);
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
        /// Verifies every outcome the case declares - status effects, stat adjustments,
        /// damage, revival, temporary HP, healing - against the creature the impact
        /// actually ran on. Shared by both activation branches: casted impacts run on the
        /// activation target, queued weapon impacts on the creature the consuming hit landed on.
        /// </summary>
        private static async Task AssertImpactOutcomesAsync(
            EngineTestContext ctx,
            AbilityBehaviorCase behaviorCase,
            uint caster,
            uint impactTarget,
            int impactTargetHPBefore,
            int casterHPBefore,
            Dictionary<StatType, int> activatorStatAdjustmentsBefore,
            Dictionary<StatType, int> targetStatAdjustmentsBefore,
            int activatorTemporaryHPBefore,
            int targetTemporaryHPBefore)
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
                    () => StatusEffect.HasStatusEffect(impactTarget, effectType),
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

            foreach (var (statType, expectedAdjustment) in behaviorCase.ExpectedTargetStatAdjustments)
            {
                var expectedValue = targetStatAdjustmentsBefore[statType] + expectedAdjustment;
                await ctx.WaitUntilAsync(
                    () => Stat.GetStatAdjustment(impactTarget, statType) == expectedValue,
                    EffectWaitSeconds,
                    $"target stat {statType} to change by {expectedAdjustment} after impact");
            }

            foreach (var effectType in behaviorCase.ExpectedRemovedTargetStatusEffects)
            {
                await ctx.WaitUntilAsync(
                    () => !StatusEffect.HasStatusEffect(impactTarget, effectType),
                    EffectWaitSeconds,
                    $"pre-applied target status effect {effectType.Name} to be removed by impact");
            }

            if (behaviorCase.ExpectsTargetDamage)
            {
                // Requiring the caster as last damager proves the damage came from this
                // ability rather than a placed arena creature engaging the hostile
                // target. Abilities whose damage arrives via a placeable (traps) may
                // need per-case relaxation once validated on a live server.
                await ctx.WaitUntilAsync(
                    () => GetCurrentHitPoints(impactTarget) < impactTargetHPBefore &&
                          GetLastDamager(impactTarget) == caster,
                    EffectWaitSeconds,
                    "this ability's damage (caster as last damager) to lower the target's hit points");
            }

            if (behaviorCase.ExpectsTargetRevived)
            {
                await ctx.WaitUntilAsync(
                    () => !GetIsDead(impactTarget),
                    EffectWaitSeconds,
                    "the dead target to be revived by the impact");

                if (behaviorCase.MinimumTargetHitPointsAfterRevive > 0)
                {
                    await ctx.WaitUntilAsync(
                        () => GetCurrentHitPoints(impactTarget) >=
                              behaviorCase.MinimumTargetHitPointsAfterRevive,
                        EffectWaitSeconds,
                        $"the revived target to have at least {behaviorCase.MinimumTargetHitPointsAfterRevive} hit points");
                }

                if (behaviorCase.ExpectedTargetHealingPercentAfterRevive.HasValue)
                {
                    var healingPercent = behaviorCase.ExpectedTargetHealingPercentAfterRevive.Value;
                    var expectedHealing = AbilityEffectScaling.CalculateScaledPercentOfMaxHP(
                        caster,
                        impactTarget,
                        healingPercent);
                    var minimumHitPoints = 1 + expectedHealing;
                    await ctx.WaitUntilAsync(
                        () => GetCurrentHitPoints(impactTarget) >= minimumHitPoints,
                        EffectWaitSeconds,
                        $"the revived target to receive its full {healingPercent:0.##}% plus Willpower-scaled heal and reach at least {minimumHitPoints} hit points");
                }
            }

            if (behaviorCase.ExpectsActivatorTemporaryHP)
            {
                await ctx.WaitUntilAsync(
                    () => CountEffectsOfType(caster, EffectTypeScript.TemporaryHitpoints) >
                          activatorTemporaryHPBefore,
                    EffectWaitSeconds,
                    "the impact to add a new temporary-HP effect to the activator");
            }

            if (behaviorCase.ExpectsTargetTemporaryHP)
            {
                await ctx.WaitUntilAsync(
                    () => CountEffectsOfType(impactTarget, EffectTypeScript.TemporaryHitpoints) >
                          targetTemporaryHPBefore,
                    EffectWaitSeconds,
                    "the impact to add a new temporary-HP effect to the target");
            }

            if (behaviorCase.ExpectsActivatorHealing)
            {
                await ctx.WaitUntilAsync(
                    () => GetCurrentHitPoints(caster) > casterHPBefore,
                    EffectWaitSeconds,
                    "the activator's hit points to rise above their pre-activation value");
            }

            if (behaviorCase.ExpectsTargetHealing)
            {
                await ctx.WaitUntilAsync(
                    () => GetCurrentHitPoints(impactTarget) > impactTargetHPBefore,
                    EffectWaitSeconds,
                    "the friendly target's hit points to rise above their pre-activation value");
            }

            if (behaviorCase.MaximumActivatorDistanceToTargetAfterImpact.HasValue)
            {
                await ctx.WaitUntilAsync(
                    () => GetDistanceBetween(caster, impactTarget) <=
                          behaviorCase.MaximumActivatorDistanceToTargetAfterImpact.Value,
                    EffectWaitSeconds,
                    $"the activator to move within {behaviorCase.MaximumActivatorDistanceToTargetAfterImpact.Value:0.##}m of the target");
            }
        }

        /// <summary>
        /// Destroys the loot-corpse placeable Loot.ProcessCorpse spawned for a killed
        /// fixture. Matched by the CORPSE_BODY back-reference rather than proximity alone
        /// so a neighboring case's corpse can never be swept up by mistake.
        /// </summary>
        private static void DestroyCorpsePlaceable(uint body)
        {
            for (var nth = 1; ; nth++)
            {
                var placeable = GetNearestObject(ObjectType.Placeable, body, nth);
                if (!GetIsObjectValid(placeable) || GetDistanceBetween(body, placeable) > 10f)
                    break;

                if (GetLocalObject(placeable, Loot.CorpseBodyVariable) == body)
                {
                    DestroyObject(placeable);
                    break;
                }
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

        /// <summary>
        /// Computes the exact FP/STM amounts the live engine should deduct for this ability
        /// and caster, derived from the definition's activation requirements plus the public
        /// adjustment seams the runtime cost path consults. A fresh fixture caster carries no
        /// transient next-cost adjustments, so these mirror the runtime deduction exactly -
        /// verifying the AMOUNT catches a cost-application regression that a mere
        /// pool-went-down check would miss.
        /// </summary>
        private static (int FP, int STM) ComputeExpectedCosts(uint caster, AbilityDetail ability)
        {
            var expectedFP = 0;
            var expectedSTM = 0;
            foreach (var requirement in ability.Requirements)
            {
                switch (requirement)
                {
                    case AbilityRequirementFP fpRequirement:
                        expectedFP += Stat.GetAdjustedRequiredFP(caster, fpRequirement.RequiredFP);
                        break;
                    case AbilityRequirementStamina stmRequirement:
                    {
                        var percent = Stat.GetStatAdjustment(caster, StatType.AbilityStaminaCostPercentAdjustment);
                        var cost = (int)Math.Ceiling(stmRequirement.RequiredSTM * (1 + percent / 100f));
                        cost += Combat.GetAbilityStaminaCostFlatAdjustment(caster, ability);
                        expectedSTM += Math.Max(0, cost);
                        break;
                    }
                }
            }

            return (expectedFP, expectedSTM);
        }

        private static void AssertCosts(
            EngineTestContext ctx,
            AbilityBehaviorCase behaviorCase,
            AbilityDetail ability,
            uint caster,
            int fpBefore,
            int stmBefore)
        {
            var (expectedFP, expectedSTM) = ComputeExpectedCosts(caster, ability);

            if (behaviorCase.ExpectsFPCost)
            {
                var currentFP = Stat.GetCurrentFP(caster);
                ctx.Assert(
                    expectedFP > 0,
                    "the case declares an FP cost but the definition's requirements resolve to 0 FP");
                ctx.Assert(
                    currentFP == fpBefore - expectedFP,
                    $"the exact FP cost to be deducted at queue time (before={fpBefore}, current={currentFP}, expected deduction={expectedFP})");
            }

            if (behaviorCase.ExpectsSTMCost)
            {
                var currentSTM = Stat.GetCurrentStamina(caster);
                ctx.Assert(
                    expectedSTM > 0,
                    "the case declares a stamina cost but the definition's requirements resolve to 0 STM");
                ctx.Assert(
                    currentSTM == stmBefore - expectedSTM,
                    $"the exact stamina cost to be deducted at queue time (before={stmBefore}, current={currentSTM}, expected deduction={expectedSTM})");
            }
        }

        /// <summary>
        /// Waits for casted-ability costs to be deducted and verifies the exact AMOUNT: the
        /// fixture suppresses natural FP/STM regen and starts pools at max, so after the
        /// activation delay the pool must sit at exactly before-minus-expected. Costs apply
        /// when the activation delay completes, so a poll window is needed. Failure messages
        /// carry the pool evidence because cost bugs are indistinguishable from
        /// resource-fixture bugs without it.
        /// </summary>
        private static async Task AssertCastedCostsAsync(
            EngineTestContext ctx,
            AbilityBehaviorCase behaviorCase,
            AbilityDetail ability,
            uint caster,
            int fpBefore,
            int stmBefore)
        {
            var (expectedFP, expectedSTM) = ComputeExpectedCosts(caster, ability);

            if (behaviorCase.ExpectsFPCost)
            {
                ctx.Assert(
                    expectedFP > 0,
                    "the case declares an FP cost but the definition's requirements resolve to 0 FP");
                try
                {
                    await ctx.WaitUntilAsync(
                        () => Stat.GetCurrentFP(caster) < fpBefore,
                        CostWaitSeconds,
                        "the FP cost to be deducted after the activation delay");
                }
                catch (EngineTestAssertionException)
                {
                    ctx.Fail($"FP cost was not deducted (before={fpBefore}, current={Stat.GetCurrentFP(caster)}, max={Stat.GetMaxFP(caster)})");
                }

                // Impact-refund abilities restore part of their own cost in the same
                // window as the deduction (sometimes conditionally, e.g. on crit), so
                // only the NET dip is observable for them.
                if (!behaviorCase.ImpactRefundsCosts)
                {
                    var currentFP = Stat.GetCurrentFP(caster);
                    ctx.Assert(
                        currentFP == fpBefore - expectedFP,
                        $"the exact FP cost to be deducted (before={fpBefore}, current={currentFP}, expected deduction={expectedFP})");
                }
            }

            if (behaviorCase.ExpectsSTMCost)
            {
                ctx.Assert(
                    expectedSTM > 0,
                    "the case declares a stamina cost but the definition's requirements resolve to 0 STM");
                try
                {
                    await ctx.WaitUntilAsync(
                        () => Stat.GetCurrentStamina(caster) < stmBefore,
                        CostWaitSeconds,
                        "the Stamina cost to be deducted after the activation delay");
                }
                catch (EngineTestAssertionException)
                {
                    ctx.Fail($"Stamina cost was not deducted (before={stmBefore}, current={Stat.GetCurrentStamina(caster)}, max={Stat.GetMaxStamina(caster)})");
                }

                if (!behaviorCase.ImpactRefundsCosts)
                {
                    var currentSTM = Stat.GetCurrentStamina(caster);
                    ctx.Assert(
                        currentSTM == stmBefore - expectedSTM,
                        $"the exact stamina cost to be deducted (before={stmBefore}, current={currentSTM}, expected deduction={expectedSTM})");
                }
            }
        }

        /// <summary>
        /// True when any effect of the given engine effect type is present - for abilities
        /// that apply raw engine effects (e.g. EffectTemporaryHitpoints) with no status
        /// effect wrapper to query.
        /// </summary>
        private static int CountEffectsOfType(uint creature, EffectTypeScript effectType)
        {
            var count = 0;
            for (var effect = GetFirstEffect(creature); GetIsEffectValid(effect); effect = GetNextEffect(creature))
            {
                if (GetEffectType(effect) == effectType)
                    count++;
            }

            return count;
        }
    }
}
