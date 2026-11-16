using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.InitiativeTrackerService;

namespace SWLOR.Game.Server.Tests.Service;

public class InitiativeTrackerSessionTests
{
    private const uint Area = 1;
    private const uint Host = 10;
    private const uint Second = 11;
    private const uint Third = 12;

    private static InitiativeTrackerSession CreateSession()
    {
        var session = new InitiativeTrackerSession(1, Area, Host);
        session.Join(Second);
        session.Join(Third);
        return session;
    }

    private static int[] Values(InitiativeTrackerSession session) => session.Entries.Select(entry => entry.Value).ToArray();

    [Test]
    public void Entries_SortHighestFirst_TiesKeepTheOrderAdded()
    {
        var session = CreateSession();
        session.SetMemberEntry(Host, 5);
        session.SetMemberEntry(Second, 18);
        session.AddNamedEntry("Rancor", 5);
        session.SetMemberEntry(Third, 12);

        Values(session).Should().Equal(18, 12, 5, 5);
        session.Entries[2].Owner.Should().Be(Host);
        session.Entries[3].Name.Should().Be("Rancor");
    }

    [Test]
    public void SetMemberEntry_UpdatesTheExistingEntryAndResorts()
    {
        var session = CreateSession();
        session.SetMemberEntry(Host, 5);
        session.SetMemberEntry(Second, 10);
        session.SetMemberEntry(Host, 15);

        session.Entries.Should().HaveCount(2);
        session.Entries[0].Owner.Should().Be(Host);
    }

    [Test]
    public void SetMemberEntry_RefusesNonMembersAndClampsValues()
    {
        var session = CreateSession();
        session.SetMemberEntry(99, 5).Should().BeFalse();
        session.SetMemberEntry(Host, 50000).Should().BeTrue();

        session.Entries.Should().ContainSingle().Which.Value.Should().Be(InitiativeTrackerSession.MaxValue);
    }

    [Test]
    public void AddNamedEntry_RefusesBlankNamesAndTrimsLongOnes()
    {
        var session = CreateSession();
        session.AddNamedEntry("   ", 5).Should().BeNull();
        var entry = session.AddNamedEntry(new string('a', 50), 5);

        entry.Name.Should().HaveLength(InitiativeTrackerSession.MaxNameLength);
        entry.IsNamed.Should().BeTrue();
    }

    [Test]
    public void MoveTurn_StartsAtTheTopAndWrapsAround()
    {
        var session = CreateSession();
        session.SetMemberEntry(Host, 20);
        session.SetMemberEntry(Second, 10);
        session.SetMemberEntry(Third, 5);

        session.CurrentEntryId.Should().BeNull();
        session.MoveTurn(true);
        session.CurrentEntryId.Should().Be(session.Entries[0].Id);
        session.MoveTurn(true);
        session.MoveTurn(true);
        session.MoveTurn(true);
        session.CurrentEntryId.Should().Be(session.Entries[0].Id);
        session.MoveTurn(false);
        session.CurrentEntryId.Should().Be(session.Entries[2].Id);
    }

    [Test]
    public void MoveTurn_BackFirstLandsOnTheBottom()
    {
        var session = CreateSession();
        session.SetMemberEntry(Host, 20);
        session.SetMemberEntry(Second, 10);

        session.MoveTurn(false);
        session.CurrentEntryId.Should().Be(session.Entries[1].Id);
    }

    [Test]
    public void MoveTurn_MovesByTheStep()
    {
        var session = CreateSession();
        session.SetMemberEntry(Host, 20);
        session.SetMemberEntry(Second, 10);
        session.SetMemberEntry(Third, 5);
        session.AddNamedEntry("Rancor", 1);
        session.MoveTurn(true);

        session.Step = 3;
        session.MoveTurn(true);
        session.CurrentEntryId.Should().Be(session.Entries[3].Id);
        session.MoveTurn(true);
        session.CurrentEntryId.Should().Be(session.Entries[2].Id);
        session.MoveTurn(false);
        session.CurrentEntryId.Should().Be(session.Entries[3].Id);
    }

    [Test]
    public void Step_StaysInRange()
    {
        var session = CreateSession();
        session.Step = 0;
        session.Step.Should().Be(InitiativeTrackerSession.MinStep);
        session.Step = 500;
        session.Step.Should().Be(InitiativeTrackerSession.MaxStep);
    }

    [Test]
    public void TurnFollowsItsEntry_WhenOthersAreAddedAbove()
    {
        var session = CreateSession();
        session.SetMemberEntry(Host, 10);
        session.MoveTurn(true);
        var current = session.CurrentEntryId;

        session.SetMemberEntry(Second, 15);

        session.CurrentEntryId.Should().Be(current);
        session.Entries[1].Id.Should().Be(current!.Value);
    }

    [Test]
    public void RemovingTheCurrentEntry_PassesTheTurnToTheNextOne()
    {
        var session = CreateSession();
        session.SetMemberEntry(Host, 20);
        session.SetMemberEntry(Second, 10);
        session.SetMemberEntry(Third, 5);
        session.MoveTurn(true);
        session.MoveTurn(true);

        session.RemoveEntry(session.Entries[1].Id);
        session.CurrentEntryId.Should().Be(session.GetEntryOf(Third).Id);

        session.RemoveEntry(session.GetEntryOf(Third).Id);
        session.CurrentEntryId.Should().Be(session.GetEntryOf(Host).Id);

        session.RemoveEntry(session.GetEntryOf(Host).Id);
        session.CurrentEntryId.Should().BeNull();
    }

    [Test]
    public void Leave_RemovesTheEntryAndPassesTheHostRole()
    {
        var session = CreateSession();
        session.SetMemberEntry(Host, 20);
        session.AddNamedEntry("Rancor", 1);

        session.Leave(Host);

        session.Host.Should().Be(Second);
        session.IsMember(Host).Should().BeFalse();
        session.Entries.Should().ContainSingle().Which.Name.Should().Be("Rancor");
    }

    [Test]
    public void Leave_LastMemberEmptiesTheSession()
    {
        var session = new InitiativeTrackerSession(1, Area, Host);
        session.Leave(Host);

        session.IsEmpty.Should().BeTrue();
    }

    [Test]
    public void Reset_ClearsEntriesAndTurnButKeepsMembers()
    {
        var session = CreateSession();
        session.SetMemberEntry(Host, 20);
        session.MoveTurn(true);

        session.Reset();

        session.Entries.Should().BeEmpty();
        session.CurrentEntryId.Should().BeNull();
        session.Members.Should().HaveCount(3);
    }
}
