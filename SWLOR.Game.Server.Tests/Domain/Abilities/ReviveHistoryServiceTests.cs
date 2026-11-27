using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Abilities;

namespace SWLOR.Game.Server.Tests.Domain.Abilities;

[TestFixture]
public class ReviveHistoryServiceTests
{
    private static readonly DateTime Now = new(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public void Death_IsNullUntilRecorded()
    {
        new ReviveHistoryService().GetDeathTime("player").Should().BeNull();
    }

    [Test]
    public void Death_ReturnsTheLatestRecordedTimeForThatKeyOnly()
    {
        var service = new ReviveHistoryService();

        service.RecordDeath("player", Now);
        service.RecordDeath("master:beast", Now.AddMinutes(1));
        service.RecordDeath("player", Now.AddMinutes(2));

        service.GetDeathTime("player").Should().Be(Now.AddMinutes(2));
        service.GetDeathTime("master:beast").Should().Be(Now.AddMinutes(1));
        service.GetDeathTime("other").Should().BeNull();
    }

    [Test]
    public void MidFightRevive_IsNullUntilRecordedAndKeptApartFromDeaths()
    {
        var service = new ReviveHistoryService();
        service.GetLastMidFightRevive("player").Should().BeNull();

        service.RecordDeath("player", Now);
        service.GetLastMidFightRevive("player").Should().BeNull();

        service.RecordMidFightRevive("player", Now.AddSeconds(5));
        service.RecordMidFightRevive("player", Now.AddSeconds(9));

        service.GetLastMidFightRevive("player").Should().Be(Now.AddSeconds(9));
        service.GetDeathTime("player").Should().Be(Now);
    }

    [Test]
    public void EachInstanceHasItsOwnHistory()
    {
        var first = new ReviveHistoryService();
        var second = new ReviveHistoryService();

        first.RecordDeath("player", Now);
        first.RecordMidFightRevive("player", Now);

        second.GetDeathTime("player").Should().BeNull();
        second.GetLastMidFightRevive("player").Should().BeNull();
    }
}
