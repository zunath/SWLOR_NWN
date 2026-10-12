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

    [SetUp]
    public void SetUp()
    {
        _contract = new QuestContract { Status = QuestContractStatus.Fulfilled, DateClosed = _now.AddDays(-30) };
        _deliveries = new();
        _deleted = false;
        _writes = _failAt = 0;
        _failAfterWrite = false;
    }

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

    [Test]
    public void LegacyExpiredHistoryUsesItsKnownExpiryDate()
    {
        _contract.Status = QuestContractStatus.Expired;
        _contract.DateClosed = null;
        _contract.DateExpires = _now.AddDays(-31);
        Prune().Should().BeTrue();
        _contract.DateClosed.Should().Be(_contract.DateExpires);
    }

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

    private bool Prune() => QuestContractHistory.Prune(_contract, _now, () => _deliveries.ToList(), Save,
        DeleteContract, DeleteDelivery);

    private void Save(QuestContract contract) => Write(() => _contract = contract);
    private void DeleteContract(string id) => Write(() => _deleted = true);
    private void DeleteDelivery(string id) => Write(() => _deliveries.RemoveAll(delivery => delivery.Id == id));

    private void Write(Action write)
    {
        var fail = ++_writes == _failAt;
        if (fail && !_failAfterWrite) throw new IOException("Interrupted before write");
        write();
        if (fail && _failAfterWrite) throw new IOException("Interrupted after write");
    }
}
