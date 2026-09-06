using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Service;

public class PlayerFeedbackTests
{
    [TestCase(ServerEnvironmentType.Test, true, true)]
    [TestCase(ServerEnvironmentType.Development, true, true)]
    [TestCase(ServerEnvironmentType.Production, true, false)]
    [TestCase(ServerEnvironmentType.Production, false, false)]
    [TestCase(ServerEnvironmentType.Development, false, false)]
    [TestCase(ServerEnvironmentType.Test, false, false)]
    [TestCase(ServerEnvironmentType.Invalid, true, false)]
    [TestCase(ServerEnvironmentType.All, true, false)]
    public void Diagnostics_RequireAnExplicitDiagnosticEnvironment(
        ServerEnvironmentType environment, bool isExplicit, bool expected)
    {
        PlayerFeedback.AreDiagnosticsEnabled(environment, isExplicit).Should().Be(expected);
    }

    [TestCase(3, "STM", "Restored 3 STM.")]
    [TestCase(7, "FP", "Restored 7 FP.")]
    [TestCase(0, "STM", null)]
    [TestCase(-1, "FP", null)]
    public void ResourceFeedback_ReportsPositiveActualGainsOnly(int restored, string resource, string expected)
    {
        PlayerFeedback.BuildResourceRestoredMessage(restored, resource).Should().Be(expected);
    }

    [Test]
    public void AutomaticWarnings_SendImmediatelyThenWaitForTheInterval()
    {
        var now = new DateTime(2026, 9, 6, 12, 0, 0, DateTimeKind.Utc).Ticks;
        PlayerFeedback.IsWarningDue(now, 0, 60).Should().BeTrue();
        PlayerFeedback.IsWarningDue(now, now, 60).Should().BeFalse();
        PlayerFeedback.IsWarningDue(now + TimeSpan.FromSeconds(59).Ticks, now, 60).Should().BeFalse();
        PlayerFeedback.IsWarningDue(now + TimeSpan.FromSeconds(60).Ticks, now, 60).Should().BeTrue();
        PlayerFeedback.IsWarningDue(now, now + 1, 60).Should().BeTrue("clock corrections must not silence warnings indefinitely");
    }
}
