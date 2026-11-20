using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Tests.Domain.Social;

/// <summary>
/// The hosted event record: its defaults, and that a copy can be changed without touching the cached original.
/// </summary>
[TestFixture]
public class HostedEventRecordTests
{
    [Test]
    public void ANewEventIsScheduledAndOpenWithEmptyText()
    {
        var hostedEvent = new HostedEvent();

        hostedEvent.Status.Should().Be(HostedEventStatus.Scheduled);
        hostedEvent.IsJoinOpen.Should().BeTrue();
        hostedEvent.AnnounceOnStart.Should().BeTrue();
        hostedEvent.Name.Should().BeEmpty();
        hostedEvent.Description.Should().BeEmpty();
        hostedEvent.HostName.Should().BeEmpty();
        hostedEvent.CreatedBy.Should().BeEmpty();
        hostedEvent.ArrivalAreaResref.Should().BeEmpty();
        hostedEvent.DiscordEventId.Should().BeEmpty();
        hostedEvent.ParticipantPlayerIds.Should().BeEmpty();
    }

    [Test]
    public void ACopyHasItsOwnParticipantsAndFields()
    {
        var original = new HostedEvent { Name = "Race", ArrivalX = 1.5f, DiscordStartUtc = new DateTime(2026, 11, 21, 0, 0, 0, DateTimeKind.Utc) };
        original.ParticipantPlayerIds.Add("p1");

        var copy = original.Copy();
        copy.ParticipantPlayerIds.Add("p2");
        copy.Name = "Changed";

        copy.ArrivalX.Should().Be(1.5f);
        copy.DiscordStartUtc.Should().Be(original.DiscordStartUtc);
        original.ParticipantPlayerIds.Should().BeEquivalentTo(new[] { "p1" });
        original.Name.Should().Be("Race");
    }

    [Test]
    public void ACopyOfAnEventWithoutParticipantsStartsEmpty()
    {
        var original = new HostedEvent { ParticipantPlayerIds = null };

        original.Copy().ParticipantPlayerIds.Should().BeEmpty();
    }

    [Test]
    public void AReturnPointStartsWithEmptyText()
    {
        var point = new HostedEventReturnPoint();

        point.EventId.Should().BeEmpty();
        point.EventName.Should().BeEmpty();
        point.AreaResref.Should().BeEmpty();
    }

    [Test]
    public void TheArrivalPointAndReturnPointKeepTheirCoordinates()
    {
        var hostedEvent = new HostedEvent { ArrivalX = 1f, ArrivalY = 2f, ArrivalZ = 3f, ArrivalFacing = 90f };
        var point = new HostedEventReturnPoint { X = 4f, Y = 5f, Z = 6f, Facing = 180f };

        (hostedEvent.ArrivalX, hostedEvent.ArrivalY, hostedEvent.ArrivalZ, hostedEvent.ArrivalFacing).Should().Be((1f, 2f, 3f, 90f));
        (point.X, point.Y, point.Z, point.Facing).Should().Be((4f, 5f, 6f, 180f));
    }

    [Test]
    public void ADismissalKeepsTheOccurrenceItCovers()
    {
        var start = new DateTime(2026, 11, 21, 19, 0, 0, DateTimeKind.Utc);

        new DiscordEventDismissal { Id = "d1", ScheduledStartUtc = start }.ScheduledStartUtc.Should().Be(start);
    }
}
