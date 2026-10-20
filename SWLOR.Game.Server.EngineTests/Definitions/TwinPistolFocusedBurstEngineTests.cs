using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWNX;
using NWNXLib = NWN.Native.API.NWNXLib;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions;

/// <summary>Native ability-pipeline coverage; headless players do not constitute client evidence.</summary>
public static class TwinPistolFocusedBurstEngineTests
{
    private static uint _actor = OBJECT_INVALID;
    private static uint _target = OBJECT_INVALID;
    private static readonly List<(long Time, int Damage)> Impacts = new();
    private static int _ordinaryHitsDuringChannel;

    [NWNEventHandler(ScriptName.OnCreatureDamagedAfter)]
    public static void ObserveImpacts()
    {
        if (OBJECT_SELF == _target && GetTotalDamageDealt() > 0)
            Impacts.Add((Environment.TickCount64, GetTotalDamageDealt()));
    }

    [NWNEventHandler(ScriptName.OnSWLORDamage)]
    public static void ObserveOrdinaryHits()
    {
        if (OBJECT_SELF == _actor && Activity.IsBusy(_actor)) _ordinaryHitsDuringChannel++;
    }

    [EngineTest("Focused Burst channels three paired volleys and pauses ordinary attacks", Category = "TwinPistolFocusedBurst", TimeoutSeconds = 45f)]
    public static Task PairedChannel(EngineTestContext ctx) => CompleteChannel(ctx, crossTrained: false, procTraits: false);

    [EngineTest("Cross-trained Focused Burst channels three single-weapon portions with the same commitment", Category = "TwinPistolFocusedBurst", TimeoutSeconds = 45f)]
    public static Task CrossTrainedChannel(EngineTestContext ctx) => CompleteChannel(ctx, crossTrained: true, procTraits: false);

    [EngineTest("Cross-trained Focused Burst delivers a native gesture for every fired volley", Category = "TwinPistolFocusedBurstPresentation", TimeoutSeconds = 45f)]
    public static async Task CrossTrainedVolleyGestures(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        var actor = player.Creature;
        try
        {
            var (target, _, stamina) = await Prepare(ctx, player, crossTrained: true);
            var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(actor).AsNWSCreature();
            var ready = 0;
            await InActorFrame(ctx, actor, () =>
            {
                ClearAllActions();
                native.SetAnimation(1);
                ready = native.m_nAnimation;
                // Establish this engine/build's actual CastOut state rather than assuming
                // that a script animation enum equals the creature's native pose ID.
                PlayAnimation(Animation.CastOutAnimation, 1f, 0f);
            });
            await WaitFrames(ctx, () => native.m_nAnimation != ready, "a real native CastOut baseline");
            var castOut = native.m_nAnimation;
            await InActorFrame(ctx, actor, () =>
            {
                ClearAllActions();
                native.SetAnimation(1);
                ctx.Assert(native.m_nAnimation != castOut, "The baseline gesture is cleared before the paid channel");
                BeginObservation(actor, target);
                Press(ctx, actor, target);
            });
            var gestures = new List<int>();
            for (var volley = 0; volley < 3; volley++)
            {
                var expectedImpacts = volley + 1;
                await WaitFrames(ctx, () => Impacts.Count >= expectedImpacts && native.m_nAnimation == castOut,
                    $"native CastOut playback for paid volley {expectedImpacts}");
                gestures.Add(native.m_nAnimation);
                await InActorFrame(ctx, actor, () =>
                {
                    // Every later assertion must observe fresh native playback. A lingering
                    // first-volley pose cannot satisfy the second or third wave.
                    native.SetAnimation(1);
                    ctx.Assert(native.m_nAnimation != castOut, "Native Ready separates consecutive volley gestures");
                });
            }
            await WaitFrames(ctx, () => !Activity.IsBusy(actor), "the channel's original commitment to finish");
            await InActorFrame(ctx, actor, () => ClearAllActions());
            PairedRangedAttackCycle.Cancel(actor);
            EndObservation();
            ctx.AssertEqual(3, Impacts.Count, "Presentation adds no weapon impacts or ordinary attacks");
            ctx.AssertEqual(0, _ordinaryHitsDuringChannel, "Native gestures do not bypass ordinary-attack suppression");
            ctx.AssertEqual(stamina - Cost(), Stat.GetCurrentStamina(actor), "All three gestures share the original single STM payment");
            ctx.Assert(OnRecast(actor), "Presentation retains the paid channel cooldown");
            ctx.SetResultDetail($"Native rifle CastOut baseline {castOut}; three independently reset volley poses [{string.Join(",", gestures)}]; three damage portions, one cost/CD, no ordinary hits.");
        }
        finally { Cleanup(actor); }
    }

    [EngineTest("Focused Burst shares direct-damage and ability-hit proc limits across all six impacts", Category = "TwinPistolFocusedBurst", TimeoutSeconds = 45f)]
    public static Task SharedProcLimits(EngineTestContext ctx) => CompleteChannel(ctx, crossTrained: false, procTraits: true);

    [EngineTest("Focused Burst keeps its fixed commitment and preserves an armed next-cast speed bonus", Category = "TwinPistolFocusedBurst", TimeoutSeconds = 45f)]
    public static Task FixedDelayPreservesNextCast(EngineTestContext ctx) =>
        CompleteChannel(ctx, crossTrained: false, procTraits: false, timingBuff: true);

    [EngineTest("Walking interrupts Focused Burst and discards its unfired volleys without refunding the channel", Category = "TwinPistolFocusedBurst", TimeoutSeconds = 45f)]
    public static Task MovementCancellation(EngineTestContext ctx) => InterruptChannel(ctx, "movement");

    [EngineTest("Unequipping a Twin Pistol interrupts Focused Burst and discards its remaining volleys", Category = "TwinPistolFocusedBurst", TimeoutSeconds = 45f)]
    public static Task UnequipCancellation(EngineTestContext ctx) => InterruptChannel(ctx, "unequip");

    [EngineTest("Native death interrupts Focused Burst before its remaining volleys", Category = "TwinPistolFocusedBurst", TimeoutSeconds = 45f)]
    public static Task DeathCancellation(EngineTestContext ctx) => InterruptChannel(ctx, "death");

    [EngineTest("Native stun interrupts Focused Burst before its remaining volleys", Category = "TwinPistolFocusedBurst", TimeoutSeconds = 45f)]
    public static Task StunCancellation(EngineTestContext ctx) => InterruptChannel(ctx, "stun");

    [EngineTest("Focused Burst retains captured damage and threat budgets without consuming later bonus arms", Category = "TwinPistolFocusedBurstBonuses", TimeoutSeconds = 45f)]
    public static async Task CapturedBonusesAndThreat(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        var actor = player.Creature;
        try
        {
            var (target, _, stamina) = await Prepare(ctx, player, crossTrained: false);
            var initialThreat = 0;
            await InActorFrame(ctx, actor, () =>
            {
                ctx.AssertEqual(0, Stat.GetStatAdjustment(actor, StatType.EnmityPercentAdjustment), "The fixture has no general threat generation modifier");
                ctx.AssertEqual(0, Stat.GetStatAdjustment(actor, StatType.DamageEnmityPercentAdjustment), "The fixture has no damage threat modifier");
                TemporaryStatModifier.Add(actor, StatType.NextAttackGuardedHitDMGBonus, 1000, 30f,
                    StatType.NextAttackGuardedHitDMGBonus);
                TemporaryStatModifier.Add(actor, StatType.NextAttackGuardedHitEnmityBonus, 60, 30f,
                    StatType.NextAttackGuardedHitDMGBonus);
                initialThreat = Enmity.GetEnmityTable(target).GetValueOrDefault(actor);
                BeginObservation(actor, target);
                Press(ctx, actor, target);
            });
            await WaitFrames(ctx, () => Impacts.Count == 2, "the initially buffed paired volley");
            await InActorFrame(ctx, actor, () =>
            {
                ctx.AssertEqual(0, TemporaryStatModifier.GetStatAdjustment(actor,
                    StatType.NextAttackGuardedHitDMGBonus, StatType.NextAttackGuardedHitDMGBonus),
                    "The original guarded damage arm is consumed at the first successful volley");
                ctx.AssertEqual(0, TemporaryStatModifier.GetStatAdjustment(actor,
                    StatType.NextAttackGuardedHitEnmityBonus, StatType.NextAttackGuardedHitDMGBonus),
                    "The original guarded threat arm is captured once");
                TemporaryStatModifier.Add(actor, StatType.NextAttackGuardedHitDMGBonus, 2000, 30f,
                    StatType.NextAttackGuardedHitDMGBonus);
                TemporaryStatModifier.Add(actor, StatType.NextAttackGuardedHitEnmityBonus, 90, 30f,
                    StatType.NextAttackGuardedHitDMGBonus);
                Combat.GrantNextAbilityDamageBonus(actor, (int)PerkType.FocusedBurst, 333, 30);
            });
            await WaitFrames(ctx, () => Impacts.Count >= 6, "all three captured-bonus volleys");
            await InActorFrame(ctx, actor, () =>
            {
                // All damage has landed at 1.2 seconds. Stop the remaining hold before
                // ordinary attacks resume, so a new auto-attack cannot spend the fresh arms.
                ClearAllActions();
                UsePerkFeat.InterruptAbilityActivation(actor);
            });
            EndObservation();
            ctx.AssertEqual(6, Impacts.Count, "The captured bonus applies to three pairs of native damage portions");
            for (var wave = 0; wave < 3; wave++)
                ctx.Assert(Impacts.Skip(wave * 2).Take(2).All(impact => impact.Damage > 100),
                    $"Both hands in wave {wave + 1} retain the captured stress-test DMG bonus, including the heavy final wave");
            var damage = 20000 - GetCurrentHitPoints(target);
            ctx.AssertEqual(damage, Impacts.Sum(impact => impact.Damage), "The measured damage budget reconciles to actual target HP loss");
            var threat = Enmity.GetEnmityTable(target).GetValueOrDefault(actor) - initialThreat;
            ctx.AssertEqual(damage + 2 * (100 + 60), threat,
                "Three waves share each hand's original base-plus-guarded flat threat budget rather than tripling it");
            var freshGuarded = Combat.ConsumeNextAttackGuardedHitBonuses(actor);
            ctx.AssertEqual(2000, freshGuarded.DMGBonus, "Later waves consume none of the newly armed guarded damage");
            ctx.AssertEqual(90, freshGuarded.EnmityBonus, "Later waves consume none of the newly armed guarded threat");
            ctx.AssertEqual(333, Combat.ConsumeNextAbilityDamageBonus(actor, PerkType.FocusedBurst),
                "Later waves consume none of the newly armed next-ability damage");
            ctx.AssertEqual(stamina - Cost(), Stat.GetCurrentStamina(actor), "Captured and newly armed bonuses add no extra activation payment");
            ctx.Assert(OnRecast(actor), "Finishing all damage portions retains the paid cooldown");
            ctx.SetResultDetail($"Native guarded capture: wave DMG [{string.Join(",", Enumerable.Range(0, 3).Select(wave => Impacts.Skip(wave * 2).Take(2).Sum(impact => impact.Damage)))}]; " +
                $"total {damage}; threat {threat} = delivered DMG + 320 flat; original arm consumed once; fresh guarded 2000/90 and next-ability333 preserved.");
        }
        finally { Cleanup(actor); }
    }

    [EngineTest("First-volley reflection death cannot leave a paid Focused Burst channel busy", Category = "TwinPistolFocusedBurstReflection", TimeoutSeconds = 45f)]
    public static async Task FirstVolleyReflectionDeath(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        var actor = player.Creature;
        try
        {
            var (target, _, stamina) = await Prepare(ctx, player, crossTrained: false);
            await InActorFrame(ctx, actor, () =>
            {
                TemporaryStatModifier.Add(target, StatType.PhysicalDamageReflectionPercentAdjustment, 100, 30f);
                TemporaryStatModifier.Add(target, StatType.ElementalDamageReflectionPercentAdjustment, 100, 30f);
                SetPlotFlag(actor, false);
                SetImmortal(actor, false);
                ObjectPlugin.SetCurrentHitPoints(actor, 1);
                ctx.AssertEqual(1, GetCurrentHitPoints(actor), "The native actor starts at one HP before reflected damage");
                BeginObservation(actor, target);
                Press(ctx, actor, target);
            });
            await WaitFrames(ctx, () => GetIsDead(actor), "first-volley reflected damage to cause actual native death");
            ctx.Assert(!Activity.IsBusy(actor), "Channel startup cannot mark the already interrupted dead actor busy again");
            ctx.AssertEqual(string.Empty, UsePerkFeat.GetActiveAbilityActivationId(actor),
                "First-impact death releases the exact shared activation before startup returns");
            ctx.Assert(!TwinPistolAbility.HasActiveActivation(actor), "Reflection death releases the captured channel");
            var impactsAtDeath = Impacts.Count;
            await ctx.DelaySecondsAsync(1.7f);
            EndObservation();
            ctx.Assert(impactsAtDeath > 0 && impactsAtDeath < 6, "The lethal reflection comes from an actual delivered portion of the first volley");
            ctx.AssertEqual(impactsAtDeath, Impacts.Count, "A lethal first-volley reflection prevents every remaining portion");
            ctx.AssertEqual(stamina - Cost(), Stat.GetCurrentStamina(actor), "The lethal first paid volley retains exactly one STM payment");
            ctx.Assert(OnRecast(actor), "First-impact reflected death retains the paid channel cooldown");
            ctx.SetResultDetail($"Native first-impact reflection killed the one-HP actor; {Impacts.Count} delivered portion(s), no later impacts, paid cost/CD retained, busy state released.");
        }
        finally
        {
            Cleanup(actor);
            ReviveRulesEngineTests.DestroyCorpsePlaceable(actor);
            ReviveRulesEngineTests.ReleaseBody(actor);
        }
    }

    private static async Task CompleteChannel(EngineTestContext ctx, bool crossTrained, bool procTraits, bool timingBuff = false)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        var actor = player.Creature;
        try
        {
            var (target, _, stamina) = await Prepare(ctx, player, crossTrained);
            if (procTraits)
                await ctx.ExecuteInCreatureContextAsync(actor, () =>
                {
                    // No cooldown masks duplicate procs: each successful weapon portion would
                    // restore another STM if it received its own activation budget.
                    TemporaryStatModifier.Add(actor, StatType.DamageDealtStaminaRestoreSkillType, (int)SkillType.TwinPistol, 30f);
                    TemporaryStatModifier.Add(actor, StatType.DamageDealtStaminaRestore, 1, 30f);
                    TemporaryStatModifier.Add(actor, StatType.SameTargetHostileAbilityHitCountRequired, 1, 30f);
                    TemporaryStatModifier.Add(actor, StatType.SameTargetHostileAbilityStaminaRestore, 1, 30f);
                });

            Combat.SetAutoAttackHitResolutionOverride(false);
            await ctx.ExecuteInCreatureContextAsync(actor, () => ActionAttack(target));
            await ctx.WaitUntilAsync(() => GetIsInCombat(actor) && GetAttackTarget(actor) == target,
                8f, "the native ordinary attack to establish combat before channeling");
            var started = 0L;
            await InActorFrame(ctx, actor, () =>
            {
                BeginObservation(actor, target);
                if (timingBuff)
                {
                    TemporaryStatModifier.Add(actor, StatType.ActivationDelayFlatAdjustment, -30, 30f);
                    Combat.GrantNextAbilityNoDelay(actor, SkillType.TwinPistol, 30, 75);
                }
                started = Environment.TickCount64;
                Press(ctx, actor, target);
                Combat.SetAutoAttackHitResolutionOverride(true);
            });
            var hands = crossTrained ? 1 : 2;
            await WaitFrames(ctx, () => Impacts.Count >= hands, "the first paid volley");
            ctx.Assert(Activity.IsBusy(actor), "The first volley does not release the channel commitment");
            ctx.Assert(OnRecast(actor), "The cooldown begins with the first paid volley");
            await ctx.DelaySecondsAsync(.15f);
            await InActorFrame(ctx, actor, () =>
            {
                // An explicit new attack order must not bypass suppression by replacing
                // the authored animation action which initially occupied the native queue.
                ClearAllActions();
                ActionAttack(target);
            });
            await WaitFrames(ctx, () => !Activity.IsBusy(actor), "the fixed channel to finish");
            Combat.SetAutoAttackHitResolutionOverride(false);
            await InActorFrame(ctx, actor, () => ClearAllActions());
            PairedRangedAttackCycle.Cancel(actor);
            EndObservation();

            ctx.AssertEqual(hands * 3, Impacts.Count, "Three waves deal one portion per captured eligible weapon");
            ctx.AssertEqual(0, _ordinaryHitsDuringChannel, "No native or companion auto-attack damage overlaps the channel");
            var waveTimes = Enumerable.Range(0, 3).Select(index => Impacts[index * hands].Time).ToArray();
            ctx.Assert(waveTimes[0] - started < 500, "The first volley starts immediately, without a windup");
            ctx.Assert(waveTimes[1] - waveTimes[0] >= 400 && waveTimes[2] - waveTimes[1] >= 400,
                "The three volleys resolve in separate timed waves rather than one instant attack");
            ctx.Assert(Environment.TickCount64 - started >= 1400, "The full stationary commitment lasts 1.5 seconds");
            ctx.AssertEqual(stamina - Cost() + (procTraits ? 2 : 0), Stat.GetCurrentStamina(actor),
                procTraits ? "All six portions trigger each of the two tested STM riders only once" : "The complete channel pays its STM cost exactly once");
            ctx.AssertEqual(20000 - GetCurrentHitPoints(target), Impacts.Sum(impact => impact.Damage),
                "Recorded native damage portions account for the target's complete HP loss");
            ctx.Assert(!TwinPistolAbility.HasActiveActivation(actor), "The completed channel releases its captured weapons");
            ctx.Assert(OnRecast(actor), "Completion does not clear or restart the paid cooldown");
            if (timingBuff)
            {
                ctx.AssertEqual((int)SkillType.TwinPistol, TemporaryStatModifier.GetStatAdjustment(actor,
                    StatType.NextAttackNoDelay, StatType.NextAttackNoDelay),
                    "The fixed channel preserves the next matching ability's armed delay bonus");
                ctx.AssertEqual(75, TemporaryStatModifier.GetStatAdjustment(actor,
                    StatType.NextAttackDelayReductionPercent, StatType.NextAttackNoDelay),
                    "The fixed channel consumes none of the armed partial reduction");
            }
            ctx.SetResultDetail($"Actual native activation: {hands * 3} portions; wave offsets " +
                $"{string.Join(",", waveTimes.Select(time => time - started))} ms; total delivered {Impacts.Sum(impact => impact.Damage)} DMG; " +
                $"STM {stamina}->{Stat.GetCurrentStamina(actor)}; ordinary channel impacts {_ordinaryHitsDuringChannel}.");
        }
        finally { Cleanup(actor); }
    }

    private static async Task InterruptChannel(EngineTestContext ctx, string interruption)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        var actor = player.Creature;
        try
        {
            var (target, main, stamina) = await Prepare(ctx, player, crossTrained: false);
            await InActorFrame(ctx, actor, () =>
            {
                BeginObservation(actor, target);
                Press(ctx, actor, target);
            });
            await WaitFrames(ctx, () => Impacts.Count == 2, "the first paired volley before interruption");
            ctx.Assert(Activity.IsBusy(actor), "The remaining volleys belong to a live channel");
            ctx.AssertEqual(stamina - Cost(), Stat.GetCurrentStamina(actor), "The first volley pays the whole activation cost");
            var origin = GetPosition(actor);
            await InActorFrame(ctx, actor, () =>
            {
                switch (interruption)
                {
                    case "movement":
                        ClearAllActions();
                        var destination = origin;
                        destination.Y += 2f;
                        ActionMoveToLocation(Location(GetArea(actor), destination, GetFacing(actor)), true);
                        break;
                    case "unequip":
                        CreaturePlugin.RunUnequip(actor, main);
                        break;
                    case "death":
                        SetPlotFlag(actor, false);
                        SetImmortal(actor, false);
                        ApplyEffectToObject(DurationType.Instant, EffectDeath(), actor);
                        break;
                    case "stun":
                        SetPlotFlag(actor, false);
                        SetImmortal(actor, false);
                        ApplyEffectToObject(DurationType.Temporary, EffectStunned(), actor, 3f);
                        break;
                }
            });
            if (interruption == "stun")
                await WaitFrames(ctx, () => HasNativeStun(actor), "the normal non-plot actor to receive an actual native stun");
            await WaitFrames(ctx, () => !Activity.IsBusy(actor), "the actual interruption to release the channel");
            if (interruption == "movement")
                ctx.Assert((GetPosition(actor) - origin).Length() >= .1f,
                    "The native walking command actually moves the actor through its normal action queue");
            await ctx.DelaySecondsAsync(1.7f);
            EndObservation();
            ctx.AssertEqual(2, Impacts.Count, "Interruption discards both remaining paired volleys");
            ctx.AssertEqual(stamina - Cost(), Stat.GetCurrentStamina(actor), "Interruption refunds none of the already paid channel cost");
            ctx.Assert(OnRecast(actor), "Interruption retains the channel's paid cooldown");
            ctx.Assert(!TwinPistolAbility.HasActiveActivation(actor), "Interruption releases the captured activation");
            ctx.AssertEqual(string.Empty, UsePerkFeat.GetActiveAbilityActivationId(actor), "The shared pipeline retains no interrupted activation identity");
            ctx.SetResultDetail($"Native {interruption}: one paired volley delivered {Impacts.Sum(impact => impact.Damage)} DMG; two later volleys discarded; cost and cooldown retained.");
        }
        finally
        {
            Cleanup(actor);
            if (interruption == "death")
            {
                ReviveRulesEngineTests.DestroyCorpsePlaceable(actor);
                ReviveRulesEngineTests.ReleaseBody(actor);
            }
        }
    }

    private static async Task<(uint Target, uint Main, int Stamina)> Prepare(
        EngineTestContext ctx, PlayerAbilityFixture player, bool crossTrained)
    {
        var actor = player.Creature;
        player.Update(record =>
        {
            record.Perks.Clear();
            record.Perks[PerkType.FocusedBurst] = 3;
            record.Skills[SkillType.TwinPistol].Rank = 50;
            record.Skills[SkillType.Rifle].Rank = 50;
        });
        var main = await ctx.EquipItemAsync(actor, crossTrained ? "b_rifle" : "b_twinpistol", InventorySlot.RightHand);
        if (!crossTrained) await TwinPistolEquipmentEngineTests.WaitForPair(ctx, actor, main);
        var target = ctx.SpawnCreature("nw_rat001", 2f);
        await ctx.DelaySecondsAsync(1f);
        await ctx.ExecuteInCreatureContextAsync(actor, () =>
        {
            CreaturePlugin.AddFeat(actor, FeatType.FocusedBurst3);
            foreach (var creature in new[] { actor, target })
            {
                SetEventScript(creature, EventScript.Creature_OnHeartbeat, "");
                SetEventScript(creature, EventScript.Creature_OnNotice, "");
                SetEventScript(creature, EventScript.Creature_OnMeleeAttacked, "");
            }
            SetPlotFlag(target, false);
            ctx.SuppressNPCNaturalRegen(target);
            Stat.SetNPCMaxHitPoints(target, 20000, true);
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), target, 40f);
            SetCommandable(false, target);
            TemporaryStatModifier.Add(target, StatType.RangedDeflection, -100, 40f);
            ctx.MakeHostile(target);
            player.Update(record => record.Stamina = Stat.GetMaxStamina(actor));
            Ability.ClearLastCompletedAbilityImpactSummary(actor);
        });
        Combat.SetAbilityHitResolutionOverride(true);
        Combat.SetAutoAttackHitResolutionOverride(false);
        return (target, main, Stat.GetCurrentStamina(actor));
    }

    private static void Press(EngineTestContext ctx, uint actor, uint target) =>
        ctx.Assert(UsePerkFeat.TryUseAbility(actor, target, FeatType.FocusedBurst3, GetLocation(target)),
            $"Focused Burst enters the real ability pipeline: {Ability.GetLastActivationDenialReason()}");

    private static int Cost() => Ability.GetAbilityDetail(FeatType.FocusedBurst3).Requirements
        .OfType<AbilityRequirementStamina>().Single().RequiredSTM;
    private static bool OnRecast(uint actor) => Recast.IsOnRecastDelay(actor,
        Ability.GetAbilityDetail(FeatType.FocusedBurst3).RecastGroup).Item1;

    private static void BeginObservation(uint actor, uint target)
    {
        _actor = actor;
        _target = target;
        Impacts.Clear();
        _ordinaryHitsDuringChannel = 0;
    }

    private static void EndObservation() { _actor = OBJECT_INVALID; _target = OBJECT_INVALID; }

    private static bool HasNativeStun(uint actor)
    {
        for (var effect = GetFirstEffect(actor); GetIsEffectValid(effect); effect = GetNextEffect(actor))
            if (GetEffectType(effect) == EffectTypeScript.Stunned) return true;
        return false;
    }

    private static async Task InActorFrame(EngineTestContext ctx, uint actor, Action action)
    {
        var executed = false;
        Exception failure = null;
        AssignCommand(actor, () =>
        {
            try { action(); }
            catch (Exception exception) { failure = exception; }
            finally { executed = true; }
        });
        await WaitFrames(ctx, () => executed, "the actor's scripted input frame");
        if (failure != null) throw failure;
    }

    private static async Task WaitFrames(EngineTestContext ctx, Func<bool> condition, string description)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!condition() && DateTime.UtcNow < deadline) await ctx.WaitFrameAsync();
        ctx.Assert(condition(), $"Timed out waiting for {description}");
    }

    private static void Cleanup(uint actor)
    {
        EndObservation();
        Combat.SetAutoAttackHitResolutionOverride(null);
        Combat.SetAbilityHitResolutionOverride(null);
        if (!GetIsObjectValid(actor)) return;
        AssignCommand(actor, () => ClearAllActions());
        TwinPistolAbility.Cancel(actor);
        UsePerkFeat.InterruptAbilityActivation(actor);
        UsePerkFeat.ClearQueuedAbility(actor);
        PairedRangedAttackCycle.Cancel(actor);
        PairedRangedTraits.ClearActor(actor);
        OrdinaryAttackTraits.ClearActor(actor);
        TemporaryStatModifier.Clear(actor);
    }
}
