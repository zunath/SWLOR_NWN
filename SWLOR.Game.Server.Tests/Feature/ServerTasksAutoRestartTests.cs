using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// The daily 10:00 UTC restart still happens in normal runs and is skipped while engine tests run, which would
/// otherwise be shut down mid-run.
/// </summary>
[TestFixture]
public class ServerTasksAutoRestartTests
{
    private static DateTime At(int hour, int minute, int second) => new(2026, 11, 14, hour, minute, second, DateTimeKind.Utc);

    [Test]
    public void Restart_happens_inside_the_window_with_engine_tests_off()
    {
        ServerTasks.ShouldAutoRestart(At(10, 0, 6), false).Should().BeTrue();
        ServerTasks.ShouldAutoRestart(At(10, 0, 29), false).Should().BeTrue();
    }

    [Test]
    public void Restart_is_skipped_inside_the_window_with_engine_tests_on()
    {
        ServerTasks.ShouldAutoRestart(At(10, 0, 6), true).Should().BeFalse();
    }

    [TestCase(9, 59, 59)]
    [TestCase(10, 0, 0)]
    [TestCase(10, 0, 30)]
    [TestCase(10, 5, 0)]
    [TestCase(22, 0, 6)]
    public void Restart_does_not_happen_outside_the_window(int hour, int minute, int second)
    {
        ServerTasks.ShouldAutoRestart(At(hour, minute, second), false).Should().BeFalse();
    }
}
