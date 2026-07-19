using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Damage-over-time effects are driven by a native EffectRunScript heartbeat, and the native effect
/// is removed by the engine once its own duration elapses. A tick is therefore only observed if its
/// heartbeat arrives before the native effect expires.
///
/// These tests simulate that engine behavior. Heartbeats arrive slightly late (they always do), so
/// without the grace period on the native duration the effect expires at exactly the instant the
/// final heartbeat is due and the last tick is silently dropped — a 12s/6s Bleed ticked once.
/// </summary>
public class StatusEffectTickSchedulingTests
{
    private const float HeartbeatInterval = 1f;
    private const double HeartbeatJitterSeconds = 0.05;

    // Must mirror StatusEffect.NativeDurationGraceSeconds. If that value shrinks below the
    // heartbeat jitter a DoT can accumulate over its lifetime, final ticks start dropping again.
    private const double NativeDurationGraceSeconds = 2d;

    private static double NativeDurationSeconds(int durationTicks, float frequency)
    {
        return durationTicks * Math.Max(1f, frequency) + NativeDurationGraceSeconds;
    }

    /// <summary>
    /// Drives an effect the way the engine does: a heartbeat every second (each arriving slightly
    /// late), stopping once the native effect's duration has elapsed. Returns the elapsed time of
    /// each observed tick.
    /// </summary>
    private static IReadOnlyList<double> RunEngine(ClockControlledStatusEffect effect, int durationTicks)
    {
        var nativeExpiry = NativeDurationSeconds(durationTicks, effect.Frequency);
        var tickTimes = new List<double>();

        effect.ApplyEffect(1, 2, durationTicks);
        effect.OnTick = () => tickTimes.Add(effect.ElapsedSeconds);

        for (var beat = 1; ; beat++)
        {
            var arrivesAt = beat * HeartbeatInterval + beat * HeartbeatJitterSeconds;

            // The engine has already torn the effect down; no further heartbeats are delivered.
            if (arrivesAt > nativeExpiry)
                break;

            effect.AdvanceTo(arrivesAt);
            effect.TickEffect(2);

            // TickStatusEffect removes the effect as soon as its final tick flags it.
            if (effect.IsFlaggedForRemoval)
                break;
        }

        return tickTimes;
    }

    [Test]
    public void EveryTickLands_BeforeTheNativeEffectExpires()
    {
        var effect = new ClockControlledStatusEffect(frequency: 6f);

        var ticks = RunEngine(effect, durationTicks: 2);

        ticks.Count.Should().Be(2, "a 12 second effect at 6 second frequency must tick twice");
        effect.IsFlaggedForRemoval.Should().BeTrue();
        effect.WasNaturallyExpired.Should().BeTrue();
    }

    [TestCase(2)]
    [TestCase(5)]
    [TestCase(8)]
    [TestCase(10)]
    public void AllTicksLand_AcrossTheDurationsUsedByRealDoTs(int durationTicks)
    {
        var effect = new ClockControlledStatusEffect(frequency: 6f);

        var ticks = RunEngine(effect, durationTicks);

        ticks.Count.Should().Be(durationTicks);
    }

    [Test]
    public void TickSchedulingDoesNotDrift_AcrossALongDuration()
    {
        var effect = new ClockControlledStatusEffect(frequency: 6f);

        var ticks = RunEngine(effect, durationTicks: 10);

        // Each tick fires on the first heartbeat at or after its nominal time. If the tick clock
        // drifted with the wall clock, later ticks would slip a full heartbeat behind and the last
        // one would fall outside the native duration entirely.
        for (var i = 0; i < ticks.Count; i++)
        {
            var nominal = (i + 1) * 6d;
            ticks[i].Should().BeGreaterThanOrEqualTo(nominal);
            ticks[i].Should().BeLessThan(nominal + HeartbeatInterval * 2);
        }
    }

    [Test]
    public void NoTicksFire_BeforeTheFirstFrequencyIntervalElapses()
    {
        var effect = new ClockControlledStatusEffect(frequency: 6f);

        effect.ApplyEffect(1, 2, durationTicks: 2);

        for (var beat = 1; beat <= 5; beat++)
        {
            effect.AdvanceTo(beat * HeartbeatInterval);
            effect.TickEffect(2);
        }

        effect.TickCount.Should().Be(0);
        effect.IsFlaggedForRemoval.Should().BeFalse();
    }

    [Test]
    public void StallLongerThanOneInterval_ResynchronizesInsteadOfFiringCatchUpTicks()
    {
        var effect = new ClockControlledStatusEffect(frequency: 6f);

        effect.ApplyEffect(1, 2, durationTicks: 2);

        // Server stalls for 30 seconds, then resumes its normal heartbeat.
        effect.AdvanceTo(30d);
        effect.TickEffect(2);
        effect.TickCount.Should().Be(1, "a stall should not replay every missed interval at once");

        effect.AdvanceTo(36d);
        effect.TickEffect(2);

        effect.TickCount.Should().Be(2);
        effect.IsFlaggedForRemoval.Should().BeTrue();
    }

    private sealed class ClockControlledStatusEffect : StatusEffectBase
    {
        private readonly DateTime _start = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private readonly float _frequency;

        // StatusEffect.CacheStatusEffects reflects over every IStatusEffect in the loaded assemblies
        // and instantiates it, so this needs a parameterless constructor or it breaks unrelated setup.
        public ClockControlledStatusEffect()
            : this(6f)
        {
        }

        public ClockControlledStatusEffect(float frequency)
        {
            _frequency = frequency;
        }

        public override string Name => "Clock Controlled";
        public override EffectIconType Icon => EffectIconType.Invalid;
        public override float Frequency => _frequency;

        public double ElapsedSeconds { get; private set; }
        public int TickCount { get; private set; }
        public Action OnTick { get; set; }

        protected override DateTime UtcNow => _start.AddSeconds(ElapsedSeconds);

        public void AdvanceTo(double elapsedSeconds)
        {
            ElapsedSeconds = elapsedSeconds;
        }

        protected override void Tick(uint creature)
        {
            TickCount++;
            OnTick?.Invoke();
        }
    }
}
