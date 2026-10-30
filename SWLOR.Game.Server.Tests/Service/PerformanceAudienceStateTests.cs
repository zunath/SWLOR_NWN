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

    [Test]
    public void APerformerWhoWasWatchingStopsWatching()
    {
        var source = File.ReadAllText(SourcePath("Service", "PerformanceService", "PerformanceAudience.cs"));
        var serve = source.Substring(source.IndexOf("private static void ServePerformer(", StringComparison.Ordinal));
        serve = serve.Substring(0, serve.IndexOf("PayPerformerXp(", StringComparison.Ordinal));

        serve.Should().Contain("state.StopWatching();");
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
