using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.ProspectingService;

namespace SWLOR.Game.Server.Tests.Service.Prospecting;

/// <summary>
/// Covers the harvest journal that keeps a Prospecting payout recoverable until the character file holds it: a crash after the node
/// and the XP are saved but before the export gives the units again at login, and a file that holds them drops the entry.
/// </summary>
public class HarvestReturnJournalTests
{
    [Test]
    public void APayoutIsJournaledWithItsSequence_CountedByResref()
    {
        var player = new Player();

        HarvestReturnJournal.Record(player, exported: 0, sequence: 3, new[] { "ore", "ore", "gem" });

        player.PendingHarvestReturns.Should().ContainSingle();
        player.PendingHarvestReturns[0].Sequence.Should().Be(3);
        player.PendingHarvestReturns[0].Items.Should().BeEquivalentTo(new Dictionary<string, int> { ["ore"] = 2, ["gem"] = 1 });
    }

    [Test]
    public void RecordingForgetsThePayoutsAnExportHasCovered()
    {
        var player = new Player();
        HarvestReturnJournal.Record(player, exported: 0, sequence: 1, new[] { "ore" });
        HarvestReturnJournal.Record(player, exported: 0, sequence: 2, new[] { "ore" });

        HarvestReturnJournal.Record(player, exported: 1, sequence: 3, new[] { "gem" });

        player.PendingHarvestReturns.Select(entry => entry.Sequence).Should().Equal(2, 3);
    }

    [Test]
    public void AnEmptyPayoutAddsNothing()
    {
        var player = new Player();

        HarvestReturnJournal.Record(player, exported: 0, sequence: 1, Array.Empty<string>());

        player.PendingHarvestReturns.Should().BeEmpty();
    }

    [Test]
    public void ARecordSavedBeforeTheJournalExistedReadsAsEmpty()
    {
        var player = new Player { PendingHarvestReturns = null };

        HarvestReturnJournal.TakeAtLogin(player, loaded: 0).Should().BeNull("there is nothing to save or give");
        HarvestReturnJournal.Record(player, exported: 0, sequence: 1, new[] { "ore" });

        player.PendingHarvestReturns.Should().ContainSingle();
    }

    [Test]
    public void AUnitWithNoRoomLeavesItsEntryAndWaitsOnTheRecord()
    {
        var player = new Player();
        HarvestReturnJournal.Record(player, exported: 0, sequence: 3, new[] { "ore", "ore", "gem" });
        HarvestReturnJournal.Record(player, exported: 0, sequence: 4, new[] { "ore" });

        HarvestReturnJournal.Hold(player, sequence: 3, "ore");
        HarvestReturnJournal.Hold(player, sequence: 3, "gem");

        player.PendingHarvestReturns.Select(entry => entry.Sequence).Should().Equal(3, 4);
        player.PendingHarvestReturns[0].Items.Should().BeEquivalentTo(new Dictionary<string, int> { ["ore"] = 1 });
        player.PendingHarvestReturns[1].Items.Should().BeEquivalentTo(new Dictionary<string, int> { ["ore"] = 1 }, "another payout is untouched");
        player.HarvestAwaitingRoom.Should().BeEquivalentTo(new Dictionary<string, int> { ["ore"] = 1, ["gem"] = 1 });
    }

    [Test]
    public void AnEntryWhoseUnitsAllWaitForRoomIsDropped()
    {
        var player = new Player();
        HarvestReturnJournal.Record(player, exported: 0, sequence: 3, new[] { "ore" });

        HarvestReturnJournal.Hold(player, sequence: 3, "ore");

        player.PendingHarvestReturns.Should().BeEmpty();
        player.HarvestAwaitingRoom.Should().BeEquivalentTo(new Dictionary<string, int> { ["ore"] = 1 });
    }

    [Test]
    public void TakingTheWaitingUnitsEmptiesThem_OneEntryPerUnit()
    {
        var player = new Player { HarvestAwaitingRoom = new Dictionary<string, int> { ["ore"] = 2, ["gem"] = 1 } };

        HarvestReturnJournal.TakeAwaiting(player).Should().BeEquivalentTo(new[] { "ore", "ore", "gem" });
        player.HarvestAwaitingRoom.Should().BeEmpty();
        HarvestReturnJournal.TakeAwaiting(new Player { HarvestAwaitingRoom = null }).Should().BeEmpty("a record saved before the field existed");
    }

    [Test]
    public void ALoginWhoseFilePredatesAPayout_GivesItAgain_AndOneThatHoldsItDropsIt()
    {
        var player = new Player();
        HarvestReturnJournal.Record(player, exported: 0, sequence: 4, new[] { "ore", "gem" });
        HarvestReturnJournal.Record(player, exported: 0, sequence: 5, new[] { "ore" });
        HarvestReturnJournal.Record(player, exported: 0, sequence: 6, new[] { "ore" });

        var redo = HarvestReturnJournal.TakeAtLogin(player, loaded: 4);

        redo.Should().BeEquivalentTo(new Dictionary<string, int> { ["ore"] = 2 }, "the file at sequence 4 holds the first payout only");
        player.PendingHarvestReturns.Should().BeEmpty("the redo is journaled again by the caller at a new sequence");
    }

    [Test]
    public void ALoginWhoseFileHoldsEveryPayout_GivesNothingAgain_ButStillEmptiesTheJournal()
    {
        var player = new Player();
        HarvestReturnJournal.Record(player, exported: 0, sequence: 2, new[] { "ore" });

        var redo = HarvestReturnJournal.TakeAtLogin(player, loaded: 2);

        redo.Should().BeEmpty();
        player.PendingHarvestReturns.Should().BeEmpty();
    }
}
