using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Tests.Feature;

public class GuildTaskExpirationTests
{
    [TestCase("2026-10-15T09:59:59Z", "2026-10-15T10:00:00Z")]
    [TestCase("2026-10-15T10:00:00Z", "2026-10-15T10:00:00Z")]
    [TestCase("2026-10-15T10:00:01Z", "2026-10-16T10:00:00Z")]
    [TestCase("2026-12-31T23:59:59Z", "2027-01-01T10:00:00Z")]
    public void RotationDeadlineUsesTheServerRestartSchedule(string now, string expected)
    {
        ServerTasks.GetNextScheduledRestart(DateTime.Parse(now).ToUniversalTime())
            .Should().Be(DateTime.Parse(expected).ToUniversalTime());
    }

    [Test]
    public void OrdinaryQuestsHaveNoGuildExpirationText()
    {
        Guild.GetTaskRotationText(new QuestDetail()).Should().BeEmpty();
    }

    [Test]
    public void TaskOutsideRotationExplainsThatAcceptedTasksRemainCompletable()
    {
        var text = Guild.GetTaskRotationText(new QuestDetail
        {
            QuestId = "unlisted_guild_task",
            GuildType = GuildType.HuntersGuild
        });

        text.Should().Contain("Expired rotation task")
            .And.Contain("UTC")
            .And.Contain("earlier restart")
            .And.Contain("Accepted tasks do not expire");
    }
}
