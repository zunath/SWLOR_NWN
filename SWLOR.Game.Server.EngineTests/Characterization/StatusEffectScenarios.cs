using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.EngineTests.Characterization.Support;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Characterization;

/// <summary>
/// Output locking for status effects: each effect is applied under a frozen clock, then the clock is advanced one
/// second at a time and the engine's own interval handler runs once per second, exactly as the server runs it. After
/// every step the scenario records whether the effect is still active, its remaining duration and tick count, the
/// target's hit points, the stat the effect adjusts and the effects the target carries. The same effect stepped on the
/// same fixtures always records the same rows.
/// </summary>
public static class StatusEffectScenarios
{
    private const int Seed = 20261027;
    private const int FramesToSettle = 3;

    /// <summary>One thing the scenario does at a point in time, then how many seconds it lets pass.</summary>
    private sealed record Step(string Action, int AdvanceSeconds, Func<uint, uint, string> Act = null);

    private sealed record Plan(
        string Label,
        Func<IStatusEffect> Create,
        float DurationSeconds,
        StatType AdjustedStat,
        int WoundTargetBy,
        Step[] Steps);

    [EngineTest("Characterization: status effects tick, refresh and expire over stepped time",
        Category = "Characterization", TimeoutSeconds = 600f)]
    public static async Task TickingAndExpiry(EngineTestContext ctx)
    {
        using var clock = new FrozenClock();
        using var arena = await ScenarioArena.CreateAsync(ctx);
        var scenario = arena.Context;
        ctx.AssertEqual(0, arena.CountCreatures(), "The quiet corridor starts without creatures");

        var table = new SnapshotTable(
            "StatusEffect.ApplyStatusEffect / OnNWNStatusEffectInterval",
            "Applies each status effect to a fixed target at step 0 under a frozen clock, then advances the clock a " +
            "second at a time, runs the engine's interval handler once per second and records the effect's state after " +
            "each planned step.",
            $"Source and target are two {ScenarioActors.Humanoid} with 2000 maximum HP and no regeneration. " +
            $"The clock starts at {FrozenClock.Start:yyyy-MM-dd HH:mm:ss} UTC and moves only by the listed steps. " +
            $"Random is reseeded with {Seed} plus the step number before every step. Elapsed and remaining are seconds. " +
            "'active' says whether the effect the plan applied first is still on the target; 'targetEffects' lists every effect the target carries.",
            new[] { "effect", "step", "action", "elapsed" },
            new[] { "active", "remaining", "durationTicks", "targetHP", "adjustedStat", "targetEffects", "note" },
            Seed) { AllowConstantOutputs = true };

        foreach (var plan in BuildPlans())
        {
            var source = await ScenarioActors.SpawnQuietAsync(scenario, ScenarioActors.Humanoid, -0.5f, 0f);
            var target = await ScenarioActors.SpawnQuietAsync(scenario, ScenarioActors.Humanoid, 1.5f, 0f);
            if (plan.WoundTargetBy > 0)
            {
                ApplyEffectToObject(DurationType.Instant, EffectDamage(plan.WoundTargetBy), target);
                await SettleAsync(scenario);
            }

            var start = clock.ElapsedSeconds;
            scenario.SeedRandom(Seed);
            var effect = plan.Create();
            var applied = StatusEffect.ApplyStatusEffect(source, target, effect, plan.DurationSeconds);
            await SettleAsync(scenario);
            table.Add(Row(plan.Label, 0, applied ? "apply" : "apply rejected", Round(clock.ElapsedSeconds - start),
                Observe(plan, target, effect, applied ? string.Empty : "rejected")));

            var step = 0;
            foreach (var planned in plan.Steps)
            {
                step++;
                scenario.SeedRandom(Seed + step);
                var note = planned.Act?.Invoke(source, target) ?? string.Empty;
                for (var second = 0; second < planned.AdvanceSeconds; second++)
                {
                    clock.Advance(1);
                    StatusEffect.OnNWNStatusEffectInterval(target);
                }

                await SettleAsync(scenario);
                table.Add(Row(plan.Label, step, planned.Action, Round(clock.ElapsedSeconds - start),
                    Observe(plan, target, effect, note)));
            }

            // No effect may outlive its plan: a later clock advance would tick it from the engine's own interval.
            StatusEffect.RemoveAllStatusEffects(target);
            StatusEffect.RemoveAllStatusEffects(source);
            await Retire(scenario, source, target);
        }

        ScenarioSnapshot.Verify("StatusEffect", "TickingAndExpiry", table);
    }

    private static IEnumerable<Plan> BuildPlans()
    {
        yield return new Plan("Bleed", () => new BleedStatusEffect(), 24f, StatType.Invalid, 0,
            Advance(6, 6, 6, 6, 6));
        yield return new Plan("Poison", () => new PoisonStatusEffect(), 18f, StatType.Invalid, 0,
            Advance(6, 6, 6, 6));
        yield return new Plan("Burn", () => new BurnStatusEffect(), 12f, StatType.Invalid, 0,
            Advance(6, 6, 6));
        yield return new Plan("Regeneration",
            () => new RegenerativeHealingStatusEffect("Characterization regeneration", new HealAmount(20, 0.01f)),
            9f, StatType.Invalid, 400, Advance(3, 3, 3, 3));
        yield return new Plan("Bolster Attack I", () => new BolsterAttack1StatusEffect(), 10f,
            StatType.DamageDealtPercentAdjustment, 0, Advance(4, 4, 4, 4));
        yield return new Plan("Bolster Attack I then II", () => new BolsterAttack1StatusEffect(), 20f,
            StatType.DamageDealtPercentAdjustment, 0, new[]
            {
                new Step("advance 5", 5),
                new Step("apply Bolster Attack II", 0, (source, target) =>
                    StatusEffect.ApplyStatusEffect(source, target, new BolsterAttack2StatusEffect(), 20f)
                        ? "II applied" : "II rejected"),
                new Step("advance 5", 5),
                new Step("apply Bolster Attack I again", 0, (source, target) =>
                    StatusEffect.ApplyStatusEffect(source, target, new BolsterAttack1StatusEffect(), 20f)
                        ? "I applied" : "I rejected"),
                new Step("advance 20", 20)
            });
        yield return new Plan("Stunned", () => new StunnedStatusEffect(), 12f, StatType.Invalid, 0, new[]
        {
            new Step("advance 4", 4),
            new Step("apply Stunned again", 0, (source, target) =>
                StatusEffect.ApplyStatusEffect(source, target, new StunnedStatusEffect(), 30f)
                    ? "second stun applied" : "second stun rejected"),
            new Step("advance 4", 4),
            new Step("advance 4", 4),
            new Step("advance 1", 1, (_, target) =>
                $"stun immunity {Ability.HasHardCrowdControlImmunity(target, ImmunityType.Stun)}")
        });
        yield return new Plan("Bleed extended and refreshed", () => new BleedStatusEffect(), 12f, StatType.Invalid, 0, new[]
        {
            new Step("advance 6", 6),
            new Step("extend by 6", 0, (source, target) =>
                StatusEffect.ExtendStatusEffectDuration(target, typeof(BleedStatusEffect), source, 6f)
                    ? "extended" : "not extended"),
            new Step("advance 6", 6),
            new Step("refresh to 12", 0, (source, target) =>
                StatusEffect.RefreshStatusEffectDuration(target, typeof(BleedStatusEffect), source, 12f)
                    ? "refreshed" : "not refreshed"),
            new Step("advance 6", 6),
            new Step("advance 6", 6),
            new Step("advance 6", 6)
        });
    }

    private static Step[] Advance(params int[] seconds)
    {
        return seconds.Select(value => new Step("advance " + value, value)).ToArray();
    }

    private static object[] Observe(Plan plan, uint target, IStatusEffect effect, string note)
    {
        var tracked = StatusEffect.GetStatusEffect(target, effect.GetType());
        var active = tracked != null;
        var others = StatusEffect.GetCreatureStatusEffects(target).GetAllEffects()
            .Select(other => other.Name).OrderBy(name => name, StringComparer.Ordinal).ToList();
        return new object[]
        {
            active,
            active ? Round(tracked.GetRemainingDurationSeconds(Clock.UtcNow)) : 0d,
            active ? tracked.DurationTicks : 0,
            GetCurrentHitPoints(target),
            plan.AdjustedStat == StatType.Invalid ? 0 : Stat.GetStatAdjustment(target, plan.AdjustedStat),
            others,
            note
        };
    }

    private static object[] Row(string label, int step, string action, double elapsed, object[] observed)
    {
        return new object[] { label, step, action, elapsed }.Concat(observed).ToArray();
    }

    private static double Round(double value) => Math.Round(value, 2);

    private static async Task SettleAsync(EngineTestContext ctx)
    {
        for (var frame = 0; frame < FramesToSettle; frame++)
            await ctx.WaitFrameAsync();
    }

    private static async Task Retire(EngineTestContext ctx, params uint[] creatures)
    {
        foreach (var creature in creatures)
        {
            var doomed = creature;
            AssignCommand(doomed, () =>
            {
                SetIsDestroyable(true, false, false);
                DestroyObject(doomed);
            });
        }

        await SettleAsync(ctx);
    }
}
