using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.NativeHooks.Tests;

/// <summary>
/// The thread-static state the hooks share across calls lives beside the trampolines, one value per thread, so the
/// engine thread's value never belongs to a game build.
/// </summary>
[TestFixture]
public class ThreadStateTests
{
    [Test]
    public void The_player_file_load_depth_counts_nested_loads_on_its_own_thread()
    {
        var start = PlayerFileLoadDepth.Current;
        var otherThread = -1;

        PlayerFileLoadDepth.Enter();
        PlayerFileLoadDepth.Enter();
        var nested = PlayerFileLoadDepth.Current;
        var thread = new Thread(() => otherThread = PlayerFileLoadDepth.Current);
        thread.Start();
        thread.Join();
        PlayerFileLoadDepth.Exit();
        PlayerFileLoadDepth.Exit();

        nested.Should().Be(start + 2);
        otherThread.Should().Be(0, "another thread loads no player file");
        PlayerFileLoadDepth.Current.Should().Be(start);
    }

    [Test]
    public void A_thread_has_no_vitality_recalculation_until_it_begins_one_and_keeps_the_same_one()
    {
        VitalityRecalculation? otherThread = null;
        var thread = new Thread(() =>
        {
            var before = VitalityRecalculation.Current;
            var created = VitalityRecalculation.ForCurrentThread();
            otherThread = before == null && ReferenceEquals(created, VitalityRecalculation.ForCurrentThread()) ? created : null;
        });
        thread.Start();
        thread.Join();

        otherThread.Should().NotBeNull("a fresh thread has none, then one it keeps");
        VitalityRecalculation.ForCurrentThread().Should().NotBeSameAs(otherThread);
    }

    [Test]
    public void A_vitality_recalculation_tracks_one_score_per_stats_until_it_ends()
    {
        var recalculation = new VitalityRecalculation();

        recalculation.Begin(1, 27).Should().BeTrue();
        recalculation.Begin(1, 30).Should().BeFalse("a nested change keeps the outer score");
        recalculation.Update(1, 28);
        recalculation.Update(2, 40);
        recalculation.TryGetScore(1, out var score).Should().BeTrue();
        recalculation.TryGetScore(2, out _).Should().BeFalse("updating a stats object that is not recalculating does nothing");
        recalculation.End(1);

        score.Should().Be(28);
        recalculation.TryGetScore(1, out _).Should().BeFalse();
    }
}
