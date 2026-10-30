using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.PerformanceService;
using static SWLOR.Game.Server.Tests.Service.PerformanceTestSnapshots;

namespace SWLOR.Game.Server.Tests.Service;

public class PerformanceAudienceStateTests
{
    [Test]
    public void ApplyingANewChoiceMovesTheFollowAtOnce()
    {
        var strong = Dancer(10, xpScaled: 9);
        var weak = Dancer(11, xpScaled: 3);
        var entry = new PerformanceWatcherEntry(1u, System.Numerics.Vector3.Zero, new List<PerformerSnapshot> { strong, weak });
        var state = new PerformanceAudienceState(1u);

        PerformanceFollow.Apply(entry, state);
        state.Follows(10).Should().BeTrue();

        state.DancerChoice = 11;
        PerformanceFollow.Apply(entry, state);

        entry.Dancer.Should().BeSameAs(weak);
        state.Follows(11).Should().BeTrue("a Flourish before the next tick must reach the performer just picked");
        state.Follows(10).Should().BeFalse();
    }

    [Test]
    public void StoppingWatchingStartsTheWarmUpOverAndKeepsPickedPerformers()
    {
        var state = new PerformanceAudienceState(1u)
        {
            DancerChoice = 10,
            MusicianChoice = 20,
            FollowedDancer = 10,
            ContinuousMinutes = 4,
            WatchingArea = 99
        };
        state.IsWatching.Should().BeTrue();

        state.StopWatching();

        state.IsWatching.Should().BeFalse();
        state.ContinuousMinutes.Should().Be(0);
        state.DancerChoice.Should().Be(10u);
        state.MusicianChoice.Should().Be(20u);
    }

    [TestCase(0, 0)]
    [TestCase(30, 0)]
    [TestCase(56, 1)]
    [TestCase(60, 1)]
    [TestCase(119, 2)]
    [TestCase(180, 3)]
    public void WatchedMinutesCountOnlyElapsedTime(int seconds, int expected)
    {
        PerformanceAudienceRules.WatchedMinutes(Start, Start.AddSeconds(seconds)).Should().Be(expected);
    }

    [TestCase(30.0, 65.0)]
    [TestCase(65.0, 65.0)]
    [TestCase(600.0, 600.0)]
    public void AFrozenInspiredOutlastsTheGapToTheNextTick(double frozen, double held)
    {
        PerformanceAudienceRules.HeldSeconds(frozen).Should().Be(held);

        var nextTickActual = held - PerformanceAudienceRules.TickSeconds;
        nextTickActual.Should().BeGreaterThan(0.0);
        PerformanceAudienceRules.FrozenRemainingSeconds(nextTickActual, true, frozen).Should().Be(frozen,
            "the next tick counts from the stored frozen value, not the padded duration");
    }

    [Test]
    public void StoppingWatchingForgetsWhenWatchingStarted()
    {
        var state = new PerformanceAudienceState(1u) { WatchingSinceUtc = Start };
        state.IsWatching.Should().BeTrue();

        state.StopWatching();

        state.WatchingSinceUtc.Should().BeNull();
        state.IsWatching.Should().BeFalse();
    }

    [Test]
    public void DeathAndAreaEntryResetTheCachedWatchingState()
    {
        var source = File.ReadAllText(SourcePath("Service", "PerformanceService", "PerformanceAudience.cs"));

        source.Should().MatchRegex(@"\[NWNEventHandler\(ScriptName\.OnModuleDeath\)\]\s+public static void StopWatchingOnDeath\(\)");
        source.Should().MatchRegex(@"\[NWNEventHandler\(ScriptName\.OnAreaEnter\)\]\s+public static void StopWatchingOnAreaEnter\(\)");
        Section(source, "public static void StopWatchingOnAreaEnter(").Should().Contain("state.StopWatching();");
    }

    [Test]
    public void LeavingRangeBetweenTicksInterruptsWatching()
    {
        var source = File.ReadAllText(SourcePath("Service", "PerformanceService", "PerformanceAudience.cs"));

        source.Should().MatchRegex(@"\[NWNEventHandler\(ScriptName\.OnPlayerHeartbeat\)\]\s+public static void StopWatchingWhenOutOfRange\(\)");
        var check = Section(source, "public static void StopWatchingWhenOutOfRange(");
        check.Should().Contain("!state.WatchingSinceUtc.HasValue", "players who are not watching return after one lookup");
        check.Should().Contain("InterruptWatching(state);");
    }

    [Test]
    public void AFlourishPaysOnlyWatchersWhoCanWatchRightNow()
    {
        var source = File.ReadAllText(SourcePath("Service", "PerformanceService", "PerformanceAudience.cs"));

        Section(source, "public static void OnFlourish(").Should().Contain("!state.IsWarmedUp || !CanWatchNow(state.Creature)");
        var rule = Section(source, "private static bool CanWatchNow(");
        rule.Should().Contain("!GetIsInCombat(player)");
        rule.Should().Contain("!GetIsDead(player)");
    }

    [Test]
    public void StartingAPerformanceInterruptsWatchingAtOnce()
    {
        var performance = File.ReadAllText(SourcePath("Service", "Performance.cs"));
        var audience = File.ReadAllText(SourcePath("Service", "PerformanceService", "PerformanceAudience.cs"));

        Section(performance, "private static void StartPerformance(").Should().Contain("PerformanceAudience.OnPerformanceStarted(player);");
        Section(audience, "public static void OnPerformanceStarted(").Should().Contain("InterruptWatching(state);");
    }

    [Test]
    public void ThePlayersOwnMusicPlayerMakesTheBandSongSendAgain()
    {
        var source = File.ReadAllText(SourcePath("Service", "Music.cs"));

        Section(source, "public static void PlayForPlayer(").Should().Contain("PerformanceAudience.ForgetBandSong(player);");
        Section(source, "public static void StopForPlayer(").Should().Contain("PerformanceAudience.ForgetBandSong(player);");
    }

    [Test]
    public void TheWatchingWindowOpensItselfOnlyAfterACountedMinute()
    {
        var source = File.ReadAllText(SourcePath("Service", "PerformanceService", "PerformanceAudience.cs"));

        source.Should().Contain("if (!isOpen && !state.WatchingDismissed && state.ContinuousMinutes > 0)");
    }

    private static string Section(string source, string start)
    {
        var from = source.IndexOf(start, StringComparison.Ordinal);
        var to = source.IndexOf("/// <summary>", from, StringComparison.Ordinal);
        return source.Substring(from, (to < 0 ? source.Length : to) - from);
    }

    [Test]
    public void APerformerWhoWasWatchingStopsWatching()
    {
        var source = File.ReadAllText(SourcePath("Service", "PerformanceService", "PerformanceAudience.cs"));
        var serve = source.Substring(source.IndexOf("private static void ServePerformer(", StringComparison.Ordinal));
        serve = serve.Substring(0, serve.IndexOf("PayPerformerXp(", StringComparison.Ordinal));

        serve.Should().Contain("InterruptWatching(state);");
    }

    [Test]
    public void ASubdualDoesNotClaimRequiem()
    {
        var source = File.ReadAllText(SourcePath("Service", "Death.cs"));
        var death = source.Substring(source.IndexOf("public static void OnPlayerDeath()", StringComparison.Ordinal));
        death = death.Substring(0, death.IndexOf("StatusEffect.ClearStatusEffectsOnDeath(player);", StringComparison.Ordinal));

        death.Should().MatchRegex(@"if \(isSubdual\)\s+DeleteLocalInt\(player, DebtWaivedVariable\);\s+else\s+RecordDeathDebtWaiver\(player\);");
    }

    private static string SourcePath(params string[] parts)
    {
        return Path.Combine(new[] { FindRepositoryRoot(), "SWLOR.Game.Server" }.Concat(parts).ToArray());
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            directory = directory.Parent;

        directory.Should().NotBeNull("the repository root holds AGENTS.md");
        return directory.FullName;
    }
}
