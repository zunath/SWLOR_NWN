using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityProjectService;
using SWLOR.Game.Server.Service.TransferService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>The decisions, marks, ids and receipts of the transfer journal (Player City Revamp, farming and the Civic Exchange).</summary>
public class TransferJournalRulesTests
{
    private static readonly DateTime Now = new(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc);

    #region Deciding

    [TestCase(false, false, true, TransferSettlement.Drop)]
    [TestCase(false, true, true, TransferSettlement.Drop)]
    [TestCase(true, true, true, TransferSettlement.Forget)]
    [TestCase(true, true, false, TransferSettlement.Forget)]
    [TestCase(true, false, true, TransferSettlement.Apply)]
    [TestCase(true, false, false, TransferSettlement.ToOverflow)]
    public void ADeposit_IsSettledByTheMarkTheReceiptAndWhetherTheTargetTakesIt(bool marked, bool applied, bool targetTakes, TransferSettlement expected)
    {
        TransferJournalRules.Settle(TransferDirection.Deposit, marked, applied, targetTakes).Should().Be(expected);
    }

    [TestCase(true, true, TransferSettlement.Forget)]
    [TestCase(true, false, TransferSettlement.Forget)]
    [TestCase(false, true, TransferSettlement.ToOverflow)]
    [TestCase(false, false, TransferSettlement.Drop)]
    public void AWithdrawal_IsSettledByTheMarkAndTheReceipt(bool marked, bool applied, TransferSettlement expected)
    {
        TransferJournalRules.Settle(TransferDirection.Withdraw, marked, applied, targetTakes: false).Should().Be(expected);
    }

    #endregion

    #region Marks

    [Test]
    public void Marks_AreAddedOnceRemovedAndParsed()
    {
        var mark = TransferJournalRules.AddMark(string.Empty, "a");
        mark = TransferJournalRules.AddMark(mark, "b");
        mark = TransferJournalRules.AddMark(mark, "a");

        TransferJournalRules.ParseMarks(mark).Should().Equal("a", "b");
        TransferJournalRules.RemoveMark(mark, "a").Should().Be("b");
        TransferJournalRules.RemoveMark("b", "b").Should().BeEmpty();
        TransferJournalRules.ParseMarks(null).Should().BeEmpty();
        TransferJournalRules.ParseMarks(" , a, ,b ").Should().Equal("a", "b");
    }

    [Test]
    public void TheDonationJournal_KeepsItsMarksTheSameWay()
    {
        CityProjectDonationRules.AddMark("a", "b").Should().Be(TransferJournalRules.AddMark("a", "b"));
        CityProjectDonationRules.RemoveMark("a,b", "a").Should().Be(TransferJournalRules.RemoveMark("a,b", "a"));
        CityProjectDonationRules.ParseMarks("a,b").Should().Equal(TransferJournalRules.ParseMarks("a,b"));
    }

    #endregion

    #region Overflow

    [Test]
    public void OverflowRecordIds_NeverChange_AndDifferByItemAndChunk()
    {
        TransferJournalRules.GetOverflowRecordId("entry", 1, 2).Should().Be(TransferJournalRules.GetOverflowRecordId("entry", 1, 2));

        var ids = new[]
        {
            TransferJournalRules.GetOverflowRecordId("entry", 0, 0),
            TransferJournalRules.GetOverflowRecordId("entry", 0, 1),
            TransferJournalRules.GetOverflowRecordId("entry", 1, 0),
            TransferJournalRules.GetOverflowRecordId("other", 0, 0)
        };
        ids.Distinct().Should().HaveCount(ids.Length);
    }

    [Test]
    public void TheOverflowText_NamesTheItemsAndTheStore_InPlainAscii()
    {
        var single = TransferJournalRules.Create("p", "k", TransferDirection.Withdraw, "t", "Your harvest", Now);
        single.Items.Add(TransferJournalRules.Plain("wheat", "Wheat", 1));
        var several = TransferJournalRules.Create("p", "k", TransferDirection.Deposit, "t", "The feed you loaded", Now);
        several.Items.Add(TransferJournalRules.Plain("wheat", "Wheat", 3));
        several.Items.Add(TransferJournalRules.Plain("corn", string.Empty, 2));

        TransferJournalRules.DescribeOverflow(single).Should().Be("Your harvest could not be finished, so 1 x Wheat is now in your overflow storage. Use /overflow to claim it.");
        TransferJournalRules.DescribeOverflow(several).Should().Be("The feed you loaded could not be finished, so 3 x Wheat, 2 x corn are now in your overflow storage. Use /overflow to claim them.");
        TransferJournalRules.NotSavedText.Where(c => c > 0x7E).Should().BeEmpty();
        TransferJournalRules.DescribeOverflow(several).Where(c => c > 0x7E).Should().BeEmpty();
    }

    #endregion

    #region Entries

    [Test]
    public void AnEntry_CarriesWhatTheHandlerNeedsToApplyItAgain()
    {
        var entry = TransferJournalRules.Create("player", "farmhand.feed-load", TransferDirection.Deposit, "farmhand:1", "The feed you loaded", Now, "7", "extra");
        entry.Items.Add(TransferJournalRules.Plain("wheat", "Wheat", 3));
        entry.Items.Add(TransferJournalRules.Held("crate", "Crated Thune", "serialized"));

        entry.PlayerId.Should().Be("player");
        entry.Kind.Should().Be("farmhand.feed-load");
        entry.Direction.Should().Be(TransferDirection.Deposit);
        entry.TargetId.Should().Be("farmhand:1");
        entry.CreatedUtc.Should().Be(Now);
        entry.AppliedUtc.Should().BeNull();
        TransferJournalRules.ParsePayload(entry.Payload).Should().Equal("7", "extra");
        entry.Items[0].Data.Should().BeEmpty();
        entry.Items[1].Count.Should().Be(1);
        entry.Items[1].Data.Should().Be("serialized");
        TransferJournalRules.ParsePayload(string.Empty).Should().BeEmpty();
    }

    [Test]
    public void TheEntryIsAnIndexedEntity_ThatOlderPlayersLoadWithout()
    {
        typeof(TransferEntry).BaseType.Should().Be(typeof(EntityBase));
        typeof(TransferEntry).GetProperty(nameof(TransferEntry.PlayerId))!.GetCustomAttributes(typeof(IndexedAttribute), false).Should().NotBeEmpty();
        new Farmhand().TransferReceipts.Should().BeEmpty();
        new Player().TransferReceipts.Should().BeEmpty();
    }

    #endregion

    #region Receipts

    [Test]
    public void Receipts_AreKeptOnce_AndOnlyTheNewestAreKept()
    {
        var receipts = TransferReceipts.Add(null, "first");
        TransferReceipts.Add(receipts, "first").Should().HaveCount(1);

        for (var index = 0; index < TransferReceipts.Capacity + 5; index++)
        {
            receipts = TransferReceipts.Add(receipts, $"entry-{index}");
        }

        receipts.Should().HaveCount(TransferReceipts.Capacity);
        TransferReceipts.Has(receipts, "first").Should().BeFalse("the oldest are dropped first");
        TransferReceipts.Has(receipts, $"entry-{TransferReceipts.Capacity + 4}").Should().BeTrue();
        TransferReceipts.Has(null, "x").Should().BeFalse();
        TransferReceipts.Has(receipts, string.Empty).Should().BeFalse();
    }

    #endregion
}
