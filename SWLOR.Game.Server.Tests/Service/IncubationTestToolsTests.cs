using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.ChatCommandDefinition;
using SWLOR.Game.Server.Service.BeastMasteryService;

namespace SWLOR.Game.Server.Tests.Service;

// Guards the test-server incubation shortcuts: finishing a running stage, the per-player weekday
// override, and that the commands are open to every tester on the test server only.
public class IncubationTestToolsTests
{
    private const string PlayerId = "incubation-test-tools-player";

    [TearDown]
    public void ClearOverride() => IncubationTestTools.ClearDayOverride(PlayerId);

    [Test]
    public void FinishRunningStages_EndsOnlyRunningStagesAndKeepsTheStage()
    {
        var now = new DateTime(2026, 11, 4, 12, 0, 0, DateTimeKind.Utc);
        var running = new IncubationJob { CurrentStage = 2, DateStarted = now.AddHours(-1), DateCompleted = now.AddHours(35) };
        var done = new IncubationJob { CurrentStage = 1, DateStarted = now.AddHours(-40), DateCompleted = now.AddHours(-4) };

        var finished = IncubationTestTools.FinishRunningStages(new List<IncubationJob> { running, done }, now);

        finished.Should().ContainSingle().Which.Should().BeSameAs(running);
        running.DateCompleted.Should().Be(now);
        running.CurrentStage.Should().Be(2);
        done.DateCompleted.Should().Be(now.AddHours(-4));
    }

    [Test]
    public void DayOverride_ChangesTheDayRequirementForThatPlayerOnly()
    {
        var requirement = new MutationRequirementDayOfWeek(DateTime.UtcNow.DayOfWeek == DayOfWeek.Monday ? DayOfWeek.Tuesday : DayOfWeek.Monday);
        var day = requirement.Days[0];
        var mine = new IncubationJob { PlayerId = PlayerId };
        var other = new IncubationJob { PlayerId = "someone-else" };

        requirement.CheckRequirements(mine).Should().NotBeEmpty();

        IncubationTestTools.SetDayOverride(PlayerId, day);

        IncubationTestTools.GetServerDay(PlayerId).Should().Be(day);
        requirement.CheckRequirements(mine).Should().BeEmpty();
        requirement.CheckRequirements(other).Should().NotBeEmpty();

        IncubationTestTools.ClearDayOverride(PlayerId);
        IncubationTestTools.GetServerDay(PlayerId).Should().Be(DateTime.UtcNow.DayOfWeek);
    }

    [TestCase("monday", true, DayOfWeek.Monday)]
    [TestCase("Sunday", true, DayOfWeek.Sunday)]
    [TestCase("7", false, DayOfWeek.Sunday)]
    [TestCase("-1", false, DayOfWeek.Sunday)]
    [TestCase("funday", false, DayOfWeek.Sunday)]
    [TestCase("Monday,Tuesday", false, DayOfWeek.Sunday)]
    [TestCase("Wednesday, Monday", false, DayOfWeek.Sunday)]
    [TestCase("", false, DayOfWeek.Sunday)]
    public void TryParseDay_AcceptsDayNamesOnly(string arg, bool valid, DayOfWeek expected)
    {
        IncubationTestChatCommand.TryParseDay(arg, out var day).Should().Be(valid);
        if (valid)
            day.Should().Be(expected);
    }

    [Test]
    public void Commands_AreOpenToTestersAndAdminOnly()
    {
        var commands = new IncubationTestChatCommand().BuildChatCommands();

        foreach (var name in new[] { "incubationfinish", "incfinish", "incubationday", "incday" })
        {
            commands.Should().ContainKey(name);
            commands[name].Authorization.Should().Be(AuthorizationLevel.Admin);
            commands[name].AvailableToAllOnTestEnvironment.Should().BeTrue();
            commands[name].ValidateArguments.Should().NotBeNull();
        }
    }
}
