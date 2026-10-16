using System;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class TwinPistolFatalSaveEngineTests
{
    private static uint _actor = OBJECT_INVALID;
    private static int _dyingEvents;
    private static int _deathEvents;
    private static bool _dyingSettled;

    [NWNEventHandler(ScriptName.OnModuleDying)]
    public static void ObserveDying()
    {
        var actor = GetLastPlayerDying();
        if (actor != _actor) return;
        _dyingEvents++;
        // This shares the module's native timer owner with lifecycle DelayCommand(0).
        // Observe state only after that earlier cleanup deadline has also been processed.
        DelayCommand(.1f, () =>
        {
            if (_actor == actor) _dyingSettled = true;
        });
    }

    [NWNEventHandler(ScriptName.OnModuleDeath)]
    public static void ObserveDeath()
    {
        if (GetLastPlayerDied() == _actor) _deathEvents++;
    }

    [EngineTest("A native fatal save with no active cast preserves Flow Converging Fire and Twin Suns until actual death", Category = "TwinPistolFatalSave", TimeoutSeconds = 45f)]
    public static Task FatalSaveWithoutCast(EngineTestContext ctx) => Verify(ctx, duringCast: false);

    [EngineTest("A native fatal save cancels Final Salvo while preserving earned Twin traits until actual death", Category = "TwinPistolFatalSave", TimeoutSeconds = 45f)]
    public static Task FatalSaveDuringCast(EngineTestContext ctx) => Verify(ctx, duringCast: true);

    private static async Task Verify(EngineTestContext ctx, bool duringCast)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        var actor = player.Creature;
        var ability = Ability.GetAbilityDetail(FeatType.FinalSalvo);
        var stamina = 0;
        var targetHealth = 0;
        var seededAt = DateTime.UtcNow;
        Combat.SetAutoAttackHitResolutionOverride(false);
        Combat.SetAbilityHitResolutionOverride(true);
        try
        {
            var target = await Prepare(ctx, player);
            await ctx.ExecuteInCreatureContextAsync(actor, () => ActionAttack(target));
            await WaitFrames(ctx, () => GetIsInCombat(actor) && GetAttackTarget(actor) == target,
                "a real ordinary attack to establish combat before the fatal save", 8f);
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                ClearAllActions();
                PairedRangedAttackCycle.Cancel(actor);
                // Isolate lifecycle cleanup from ordinary attempts consuming the six-shot grant.
                // Trait generation uses the same existing event adapters as cancellation coverage.
                SeedEarnedTraits(actor, target);
                AssertEarnedTraits(ctx, actor, target);
                seededAt = DateTime.UtcNow;
                stamina = Stat.GetCurrentStamina(actor);
                targetHealth = GetCurrentHitPoints(target);
                Ability.ClearLastCompletedAbilityImpactSummary(actor);
                HostileAbilityDelay.EndForTesting(actor);
                if (duringCast)
                {
                    ctx.Assert(UsePerkFeat.TryUseAbility(actor, target, FeatType.FinalSalvo, GetLocation(target)),
                        $"Final Salvo begins its normal unpaid cast: {Ability.GetLastActivationDenialReason()}");
                    ctx.Assert(TwinPistolAbility.HasActiveActivation(actor) && Activity.IsBusy(actor),
                        "A real captured Twin activation exists before the native dying event");
                    ctx.Assert(!string.IsNullOrWhiteSpace(UsePerkFeat.GetActiveAbilityActivationId(actor)),
                        "The captured cast owns a shared activation identity");
                }
                else
                    ctx.AssertEqual(string.Empty, UsePerkFeat.GetActiveAbilityActivationId(actor),
                        "The ordinary fatal-save case exercises the empty activation-ID branch");

                TemporaryStatModifier.Add(actor, StatType.FatalDamageTemporaryHPPercent, 20, 30f);
                TemporaryStatModifier.Add(actor, StatType.FatalDamageTemporaryHPDurationSeconds, 30, 30f);
                ObjectPlugin.SetCurrentHitPoints(actor, 1);
                ctx.AssertEqual(1, GetCurrentHitPoints(actor), "Fatal damage begins at one actual native HP");
                ctx.AssertEqual(0, TemporaryHitPointEffects.GetRemaining(actor), "No shield predates the dying event");
                _actor = actor;
                _dyingEvents = _deathEvents = 0;
                _dyingSettled = false;
                ApplyEffectToObject(DurationType.Instant, EffectDamage(2), actor);
            });
            await WaitFrames(ctx, () => _dyingSettled && _dyingEvents == 1 && !GetIsDead(actor) && GetCurrentHitPoints(actor) > 0 &&
                TemporaryHitPointEffects.GetRemaining(actor) > 0 && !Activity.IsBusy(actor) &&
                !TwinPistolAbility.HasActiveActivation(actor), "the native dying save and deferred cast cancellation to settle", 1f);
            ctx.AssertEqual(1, _dyingEvents, "The real module dying hook fired exactly once");
            ctx.AssertEqual(0, _deathEvents, "The fatal-save survivor never entered committed death");
            ctx.Assert(GetIsInCombat(actor), "The living survivor remains in combat");
            ctx.AssertEqual(string.Empty, UsePerkFeat.GetActiveAbilityActivationId(actor),
                "The survivor has no active cast after deferred cancellation");
            AssertEarnedTraits(ctx, actor, target);
            ctx.AssertEqual(stamina, Stat.GetCurrentStamina(actor), "Fatal-save cleanup spends no stamina");

            if (duringCast)
            {
                await ctx.DelaySecondsAsync(2.2f);
                ctx.Assert((DateTime.UtcNow - seededAt).TotalSeconds < 6,
                    "Trait preservation is observed before Converging Fire's shortest authored expiry");
                AssertEarnedTraits(ctx, actor, target);
                ctx.AssertEqual(stamina, Stat.GetCurrentStamina(actor), "The canceled cast deadline spends no stamina or Flow");
                ctx.Assert(!Recast.IsOnRecastDelay(actor, ability.RecastGroup).Item1,
                    "The canceled cast starts no capstone recast");
                ctx.Assert(Ability.GetLastCompletedAbilityImpactSummary(actor) == null,
                    "The canceled cast deadline resolves no impact");
                ctx.AssertEqual(targetHealth, GetCurrentHitPoints(target), "The canceled cast deals no target damage");
            }

            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                TemporaryStatModifier.Clear(actor);
                ApplyEffectToObject(DurationType.Instant, EffectDeath(), actor);
            });
            await WaitFrames(ctx, () => GetIsDead(actor) && _deathEvents == 1,
                "a real committed native death after the survivor assertions", 1f);
            AssertClearedTraits(ctx, actor, "Committed death");
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                ApplyEffectToObject(DurationType.Instant, EffectResurrection(), actor);
                ctx.Assert(!GetIsDead(actor) && GetCurrentHitPoints(actor) > 0, "Native resurrection restores the actor");
                PairedRangedTraits.Reconcile(actor);
                AssertClearedTraits(ctx, actor, "Reconciliation after resurrection");
                ctx.AssertEqual(0, PairedRangedTraits.GetOrdinaryDamagePercentAdjustment(actor, target),
                    "Actual death cleared internal repeated-target and shot-grant state, not only their status displays");
            });
        }
        finally
        {
            _actor = OBJECT_INVALID;
            Combat.SetAutoAttackHitResolutionOverride(null);
            Combat.SetAbilityHitResolutionOverride(null);
            if (GetIsObjectValid(actor))
            {
                AssignCommand(actor, () => ClearAllActions());
                TwinPistolAbility.Cancel(actor);
                UsePerkFeat.InterruptAbilityActivation(actor);
                UsePerkFeat.ClearQueuedAbility(actor);
                PairedRangedAttackCycle.Cancel(actor);
                PairedRangedTraits.ClearActor(actor);
                TemporaryStatModifier.Clear(actor);
                HostileAbilityDelay.EndForTesting(actor);
                ReviveRulesEngineTests.DestroyCorpsePlaceable(actor);
                ReviveRulesEngineTests.ReleaseBody(actor);
            }
        }
    }

    private static async Task<uint> Prepare(EngineTestContext ctx, PlayerAbilityFixture player)
    {
        var actor = player.Creature;
        player.Update(record =>
        {
            record.Skills[SkillType.TwinPistol].Rank = 50;
            foreach (var perk in new[] { PerkType.FinalSalvo, PerkType.TwinPistolMomentum, PerkType.ConvergingFire })
                record.Perks[perk] = 1;
            foreach (var requirement in Perk.GetPerkDetails(PerkType.FinalSalvo).PerkLevels.Values
                         .SelectMany(level => level.Requirements).OfType<PerkRequirementQuest>())
                record.Quests[requirement.QuestId] = new PlayerQuest { TimesCompleted = 1 };
        });
        var main = await ctx.EquipItemAsync(actor, "b_twinpistol", InventorySlot.RightHand);
        await TwinPistolEquipmentEngineTests.WaitForPair(ctx, actor, main);
        var target = ctx.SpawnCreature("nw_rat001", 2f);
        await ctx.DelaySecondsAsync(1f);
        await ctx.ExecuteInCreatureContextAsync(actor, () =>
        {
            CreaturePlugin.AddFeat(actor, FeatType.FinalSalvo);
            SetPlotFlag(actor, false);
            SetImmortal(actor, false);
            foreach (var creature in new[] { actor, target })
            {
                SetEventScript(creature, EventScript.Creature_OnHeartbeat, "");
                SetEventScript(creature, EventScript.Creature_OnNotice, "");
                SetEventScript(creature, EventScript.Creature_OnMeleeAttacked, "");
            }
            SetPlotFlag(target, false);
            ctx.SuppressNPCNaturalRegen(target);
            Stat.SetNPCMaxHitPoints(target, 10000, true);
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), target, 45f);
            SetCommandable(false, target);
            ctx.MakeHostile(target);
            player.Update(record => record.Stamina = Stat.GetMaxStamina(actor));
        });
        return target;
    }

    private static void SeedEarnedTraits(uint actor, uint target)
    {
        for (var index = 0; index < 3; index++)
        {
            var activation = PairedRangedTraits.BeginAbility(actor, true, false);
            PairedRangedTraits.CompleteAbility(actor, activation, true, 3f, true, 0, 0);
            PairedRangedTraits.RecordOrdinaryAttempt(actor, target, long.MaxValue - index);
            PairedRangedTraits.RecordOrdinaryHit(actor, target, long.MaxValue - index);
        }
        var capstone = PairedRangedTraits.BeginAbility(actor, false, false);
        PairedRangedTraits.CompleteAbility(actor, capstone, true, 0f, false, 0, 0,
            ordinaryBonusAttempts: 6, ordinaryBonusDamagePercent: 25, ordinaryBonusSeconds: 12);
    }

    private static void AssertEarnedTraits(EngineTestContext ctx, uint actor, uint target)
    {
        ctx.AssertEqual(3, StatusEffect.GetStatusEffect<MomentumStatusEffect>(actor)?.Charges ?? 0,
            "The survivor retains three earned Flow charges");
        ctx.Assert(StatusEffect.GetStatusEffect<ConvergingFireStatusEffect>(actor) != null,
            "The survivor retains earned Converging Fire progress");
        ctx.AssertEqual(6, StatusEffect.GetStatusEffect<TwinSunsStatusEffect>(actor)?.Attempts ?? 0,
            "The survivor retains all six unconsumed Twin Suns attempts");
        ctx.AssertEqual(30, PairedRangedTraits.GetOrdinaryDamagePercentAdjustment(actor, target),
            "Preserved internal Converging Fire and Twin Suns state still grants its actual 5% plus 25% damage");
    }

    private static void AssertClearedTraits(EngineTestContext ctx, uint actor, string boundary)
    {
        ctx.Assert(StatusEffect.GetStatusEffect<MomentumStatusEffect>(actor) == null, $"{boundary} clears Flow");
        ctx.Assert(StatusEffect.GetStatusEffect<ConvergingFireStatusEffect>(actor) == null, $"{boundary} clears Converging Fire");
        ctx.Assert(StatusEffect.GetStatusEffect<TwinSunsStatusEffect>(actor) == null, $"{boundary} clears Twin Suns attempts");
    }

    private static async Task WaitFrames(EngineTestContext ctx, Func<bool> condition, string description, float timeout)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeout);
        while (!condition() && DateTime.UtcNow < deadline) await ctx.WaitFrameAsync();
        ctx.Assert(condition(), $"Timed out waiting for {description}");
    }
}
