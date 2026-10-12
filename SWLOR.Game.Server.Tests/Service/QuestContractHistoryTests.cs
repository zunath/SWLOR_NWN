using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.QuestContractService;

namespace SWLOR.Game.Server.Tests.Service;

public class QuestContractHistoryTests
{
    private readonly DateTime _now = new(2026, 10, 12, 12, 0, 0, DateTimeKind.Utc);
    private QuestContract _contract = null!;
    private List<QuestContractDelivery> _deliveries = null!;
    private bool _deleted;
    private int _writes;
    private int _failAt;
    private bool _failAfterWrite;

    /// <summary>Starts each case with settled history exactly at the retention boundary and no write failures.</summary>
    [SetUp]
    public void SetUp()
    {
        _contract = new QuestContract { Status = QuestContractStatus.Fulfilled, DateClosed = _now.AddDays(-30) };
        _deliveries = new();
        _deleted = false;
        _writes = _failAt = 0;
        _failAfterWrite = false;
    }

    /// <summary>Checks every terminal status immediately before and exactly at thirty days.</summary>
    [TestCase(QuestContractStatus.Fulfilled)]
    [TestCase(QuestContractStatus.Expired)]
    [TestCase(QuestContractStatus.Cancelled)]
    [TestCase(QuestContractStatus.TakenDown)]
    public void ClosedHistoryIsRemovedAtThirtyDays(QuestContractStatus status)
    {
        _contract.Status = status;
        _contract.DateClosed = _now.AddDays(-30).AddTicks(1);
        Prune().Should().BeFalse();
        _deleted.Should().BeFalse();
        _contract.DateClosed = _now.AddDays(-30);
        Prune().Should().BeTrue();
        _deleted.Should().BeTrue();
    }

    /// <summary>Ensures an old closing date never makes a draft or published contract eligible for deletion.</summary>
    [TestCase(QuestContractStatus.Draft)]
    [TestCase(QuestContractStatus.Published)]
    public void OpenContractsAreNeverPruned(QuestContractStatus status)
    {
        _contract.Status = status;
        _contract.DateClosed = _now.AddYears(-1);
        Prune().Should().BeFalse();
        _deleted.Should().BeFalse();
        _writes.Should().Be(0);
    }

    /// <summary>Verifies each unsettled ownership state independently blocks history deletion without writes.</summary>
    [Test]
    public void PendingSettlementEscrowAndHeldSubmissionsPreventDeletion()
    {
        _contract.SettlementPending = 1;
        Prune().Should().BeFalse();
        _contract.SettlementPending = 0;
        _contract.RewardItems.Add(new QuestContractItem { Data = "unsettled reward" });
        Prune().Should().BeFalse();
        _contract.RewardItems.Clear();
        _deliveries.Add(new QuestContractDelivery { HeldForCompletion = true });
        Prune().Should().BeFalse();
        _deleted.Should().BeFalse();
        _writes.Should().Be(0);
    }

    /// <summary>Checks that pruning removes empty receipts while preserving claimable value and source labels.</summary>
    [Test]
    public void EmptyReceiptsAreDeletedAndUnclaimedCreditsItemsAndTitlesSurvive()
    {
        var credits = new QuestContractDelivery { IsRewardPayment = true, Credits = 75, SourceContractTitle = "Reward" };
        var items = new QuestContractDelivery { Items = new() { new() { Data = "refund item" } }, SourceContractTitle = "Refund" };
        _deliveries.AddRange(new[] { credits, items, new QuestContractDelivery { IsRewardPayment = true }, new QuestContractDelivery() });
        Prune().Should().BeTrue();
        _deliveries.Should().Equal(credits, items);
        credits.Credits.Should().Be(75);
        items.Items.Should().ContainSingle().Which.Data.Should().Be("refund item");
        credits.SourceContractTitle.Should().Be("Reward");
        items.SourceContractTitle.Should().Be("Refund");
    }

    /// <summary>Uses the persisted expiry date to age legacy expired history without an extra grace period.</summary>
    [Test]
    public void LegacyExpiredHistoryUsesItsKnownExpiryDate()
    {
        _contract.Status = QuestContractStatus.Expired;
        _contract.DateClosed = null;
        _contract.DateExpires = _now.AddDays(-31);
        Prune().Should().BeTrue();
        _contract.DateClosed.Should().Be(_contract.DateExpires);
    }

    /// <summary>Starts one full retention window for undated legacy history and keeps it stable across sweeps.</summary>
    [TestCase(QuestContractStatus.Fulfilled)]
    [TestCase(QuestContractStatus.Cancelled)]
    [TestCase(QuestContractStatus.TakenDown)]
    [TestCase(QuestContractStatus.Expired)]
    public void LegacyHistoryWithoutAKnownClosingDateGetsAFullRetentionWindow(QuestContractStatus status)
    {
        _contract.Status = status;
        _contract.DateClosed = null;
        _contract.DatePublished = _contract.DateCreated = _now.AddYears(-1);
        Prune().Should().BeFalse();
        _contract.DateClosed.Should().Be(_now);
        Prune().Should().BeFalse();
        _writes.Should().Be(1, "the original retention window must not restart each boot");
        QuestContractHistory.Prune(_contract, _now.AddDays(30), () => _deliveries.ToList(), Save,
            DeleteContract, DeleteDelivery).Should().BeTrue();
    }

    /// <summary>Injects failures before and after each deletion to verify retries preserve unpaid credits.</summary>
    [TestCase(1, false)]
    [TestCase(1, true)]
    [TestCase(2, false)]
    [TestCase(2, true)]
    [TestCase(3, false)]
    [TestCase(3, true)]
    public void InterruptedDeletionCanRetryWithoutLosingUnclaimedValue(int failingWrite, bool afterWrite)
    {
        var pending = new QuestContractDelivery { IsRewardPayment = true, Credits = 75 };
        _deliveries.AddRange(new[] { pending, new QuestContractDelivery { IsRewardPayment = true }, new QuestContractDelivery() });
        _failAt = failingWrite;
        _failAfterWrite = afterWrite;
        Action prune = () => Prune();
        prune.Should().Throw<IOException>();
        _failAt = 0;
        if (!_deleted)
            Prune().Should().BeTrue();
        _deleted.Should().BeTrue();
        _deliveries.Should().ContainSingle().Which.Should().BeSameAs(pending);
        pending.Credits.Should().Be(75);
    }

    /// <summary>Checks source existence protects both reward and refund receipts regardless of their age.</summary>
    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(false, false)]
    public void EmptyDeliveryIsPrunedOnlyAfterItsSourceHasGone(bool sourceExists, bool isRewardPayment)
    {
        var receipt = new QuestContractDelivery { SourceContractId = "source", IsRewardPayment = isRewardPayment };
        var deletedIds = new List<string>();
        QuestContractHistory.PruneOrphanedDelivery(receipt, sourceId =>
        {
            sourceId.Should().Be("source");
            return sourceExists;
        }, deletedIds.Add).Should().Be(!sourceExists);
        if (sourceExists)
            deletedIds.Should().BeEmpty("receipts must remain while their source can recover settlement");
        else
            deletedIds.Should().Equal(receipt.Id);
    }

    /// <summary>Prevents orphan cleanup from consulting the source or deleting value and held ownership.</summary>
    [TestCase(75, false, false)]
    [TestCase(0, true, false)]
    [TestCase(0, false, true)]
    public void UnclaimedOrHeldDeliveriesAreNeverPrunedAsOrphans(int credits, bool hasItem, bool isHeld)
    {
        var delivery = new QuestContractDelivery { Credits = credits, HeldForCompletion = isHeld };
        if (hasItem) delivery.Items.Add(new QuestContractItem { Data = "unclaimed item" });
        QuestContractHistory.PruneOrphanedDelivery(delivery,
            _ => throw new InvalidOperationException("A delivery with value or held ownership must not check its source"),
            _ => throw new InvalidOperationException("A delivery with value or held ownership must not be deleted"))
            .Should().BeFalse();
        delivery.Credits.Should().Be(credits);
        delivery.Items.Count.Should().Be(hasItem ? 1 : 0);
        delivery.HeldForCompletion.Should().Be(isHeld);
    }

    /// <summary>Runs cleanup against the fixture's mutable store with failure-injecting write callbacks.</summary>
    private bool Prune() => QuestContractHistory.Prune(_contract, _now, () => _deliveries.ToList(), Save,
        DeleteContract, DeleteDelivery);

    /// <summary>Persists the fixture contract through the same failure boundary used for deletions.</summary>
    private void Save(QuestContract contract) => Write(() => _contract = contract);

    /// <summary>Marks source deletion while allowing failures immediately before or after the change.</summary>
    private void DeleteContract(string id) => Write(() => _deleted = true);

    /// <summary>Removes the matching receipt through an idempotent, failure-injecting store operation.</summary>
    private void DeleteDelivery(string id) => Write(() => _deliveries.RemoveAll(delivery => delivery.Id == id));

    /// <summary>Simulates a store interruption at one selected write, before or after its mutation.</summary>
    private void Write(Action write)
    {
        var fail = ++_writes == _failAt;
        if (fail && !_failAfterWrite) throw new IOException("Interrupted before write");
        write();
        if (fail && _failAfterWrite) throw new IOException("Interrupted after write");
    }
}
