using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.QuestContractService;

namespace SWLOR.Game.Server.Tests.Service;

public class QuestContractBatchTests
{
    private QuestContract _contract = null!;
    private Dictionary<string, QuestContractDelivery> _deliveries = null!;
    private int _writes;
    private int _failAt;
    private bool _failAfterWrite;

    [SetUp]
    public void SetUp()
    {
        _contract = new QuestContract
        {
            AuthorPlayerId = "author", Title = "Ore", Description = "Deliver ore.",
            Status = QuestContractStatus.Published, TotalCompletions = 10,
            CompletionsRemaining = 10, RewardCredits = 1000,
            Objectives = new() { new() { ItemResref = "ore", ItemName = "Ore", Quantity = 20 } }
        };
        _deliveries = new();
        _writes = _failAt = 0;
        _failAfterWrite = false;
    }

    [Test]
    public void Funding_ChargesFeeOnAllBatchesAndEditorShowsTheSameCost()
    {
        QuestContractBoard.CalculateFundedReward(1000, 10).Should().Be(10000);
        QuestContractBoard.CalculateTotalPublishCost(1000, 10).Should().Be(10500);
        var editor = new QuestContractEditorViewModel { BatchesText = "10", RewardCreditsText = "1000" };
        editor.EscrowText.Should().Contain("10000 cr");
        editor.PostingFeeText.Should().Contain("500 cr");
        editor.TotalCostText.Should().Contain("10500 cr");
        editor.IsAddRewardItemEnabled.Should().BeFalse();
        editor.BatchesText = "1";
        editor.IsAddRewardItemEnabled.Should().BeTrue();
    }

    [Test]
    public void Validation_RejectsMultipleItemRewardsAndUnfundableAmounts()
    {
        _contract.RewardItems.Add(new QuestContractItem());
        Validate().Should().Contain("only offer credit rewards");
        _contract.TotalCompletions = 1;
        Validate().Should().BeEmpty();
        _contract.RewardItems.Clear();
        _contract.RewardCredits = int.MaxValue;
        Validate().Should().Contain("too large");
        _contract.TotalCompletions = QuestContractBoard.MaxBatches;
        Validate().Should().Contain("too large");
        _contract.TotalCompletions++;
        Validate().Should().Contain("Batches must be between");
    }

    [Test]
    public void SamePlayerCanDeliverFreshBatchesWithoutReusingProgressOrPayments()
    {
        QuestContractFactory.BuildQuest(_contract).IsRepeatable.Should().BeTrue();
        Submit("worker", 1);
        Submit("other", 1);
        Complete("worker", 1).Should().BeTrue();
        _contract.Status.Should().Be(QuestContractStatus.Published);
        _contract.CompletionsRemaining.Should().Be(9);
        _deliveries.Values.Single(x => x.PlayerId == "other").HeldForCompletion.Should().BeTrue();
        Submit("worker", 2);
        Complete("worker", 2).Should().BeTrue();
        Payments().Should().HaveCount(2).And.OnlyContain(x => x.Credits == 1000);
        _deliveries.Values.Count(x => x.PlayerId == "author" && !x.HeldForCompletion).Should().Be(2);
        Complete("worker", 1).Should().BeFalse();
        Complete("worker", 2).Should().BeFalse();
        _contract.CompletionsRemaining.Should().Be(8);
    }

    [Test]
    public void FinalBatchPaysOnlyOnceAndReturnsOtherSubmissions()
    {
        _contract.TotalCompletions = _contract.CompletionsRemaining = 2;
        Submit("worker", 1);
        Submit("other", 1);
        Complete("worker", 1).Should().BeTrue();
        Submit("worker", 2);
        Complete("worker", 2).Should().BeTrue();
        _contract.Status.Should().Be(QuestContractStatus.Fulfilled);
        _contract.CompletionsRemaining.Should().Be(0);
        var refund = _deliveries.Values.Single(x => x.PlayerId == "other");
        QuestContractBoard.IsClaimable(refund).Should().BeTrue();
        Complete("other", 1).Should().BeFalse();
        Payments().Sum(x => x.Credits).Should().Be(2000);
    }

    [Test]
    public void AbandonedRepeatAttemptCannotBeMistakenForAnEarlierCompletedBatch()
    {
        Submit("worker", 1);
        Complete("worker", 1);
        var abandoned = Submit("worker", 2);
        QuestContractBoard.TryReleaseSubmission(abandoned, _contract, "worker").Should().BeTrue();
        _deliveries[abandoned.Id] = Clone(abandoned);
        Complete("worker", 2).Should().BeFalse();
        Payments().Should().ContainSingle();
        abandoned.PlayerId.Should().Be("worker");
    }

    [Test]
    public void CancellationAfterThreeBatchesLeavesOnlySevenRewardsToRefund()
    {
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            Submit("worker", attempt);
            Complete("worker", attempt);
        }
        QuestContractBoard.CalculateFundedReward(_contract.RewardCredits, _contract.CompletionsRemaining).Should().Be(7000);
        _contract.Status = QuestContractStatus.Cancelled;
        var unfinished = Submit("other", 1);
        QuestContractBoard.TryReleaseSubmission(unfinished, _contract).Should().BeTrue();
        unfinished.PlayerId.Should().Be("other");
        Payments().Sum(x => x.Credits).Should().Be(3000);
    }

    [TestCaseSource(nameof(InterruptedWrites))]
    public void RecoveryAtEveryWriteCannotDuplicateClaimedPaymentsOrTransferItemsTwice(
        int totalBatches, int failingWrite, bool failAfterWrite)
    {
        _contract.TotalCompletions = _contract.CompletionsRemaining = totalBatches;
        Submit("worker", 1);
        Submit("other", 1);
        _failAt = failingWrite;
        _failAfterWrite = failAfterWrite;
        Action complete = () => Complete("worker", 1);
        complete.Should().Throw<IOException>();
        _failAt = 0;
        Complete("worker", 1);
        _contract.Status.Should().Be(totalBatches == 1 ? QuestContractStatus.Fulfilled : QuestContractStatus.Published);
        _contract.CompletionsRemaining.Should().Be(totalBatches - 1);
        _contract.SettlementPending.Should().Be(0);
        Payments().Should().ContainSingle().Which.Credits.Should().Be(1000);
        var receipt = Payments().Single();
        receipt.Credits = 0; // A claimed payment retains its receipt.
        _deliveries[receipt.Id] = receipt;
        Complete("worker", 1).Should().BeFalse();
        QuestContractSettlement.Settle(Clone(_contract), ReadDeliveries, SaveContract, SaveDelivery);
        Payments().Should().ContainSingle().Which.Credits.Should().Be(0);
        _deliveries.Values.Count(x => x.PlayerId == "author").Should().Be(1);
        _deliveries.Values.Single(x => x.PlayerId == "other").HeldForCompletion.Should().Be(totalBatches > 1);
    }

    public static IEnumerable<TestCaseData> InterruptedWrites()
    {
        foreach (var batches in new[] { 1, 2 })
        foreach (var afterWrite in new[] { false, true })
        for (var write = 1; write <= (batches == 1 ? 5 : 4); write++)
            yield return new TestCaseData(batches, write, afterWrite);
    }

    [Test]
    public void LegacyPendingSettlementKeepsItsOriginalPaymentAndWinner()
    {
        _contract = JsonConvert.DeserializeObject<QuestContract>(
            "{\"Id\":\"old\",\"AuthorPlayerId\":\"author\",\"Status\":5,\"CompletedByPlayerId\":\"worker\",\"SettlementPending\":1,\"RewardCredits\":1000}")!;
        Submit("worker", 1);
        Submit("other", 1);
        QuestContractSettlement.Settle(Clone(_contract), ReadDeliveries, SaveContract, SaveDelivery);
        Payments().Should().ContainSingle().Which.Id.Should().Be("old-reward");
        _deliveries.Values.Count(x => x.PlayerId == "author").Should().Be(1);
        _contract.TotalCompletions.Should().Be(1);
    }

    [TestCase(QuestContractStatus.Fulfilled)]
    [TestCase(QuestContractStatus.Expired)]
    [TestCase(QuestContractStatus.Cancelled)]
    public void PostAgainCopiesContentIntoAnUnfundedIndependentDraft(QuestContractStatus status)
    {
        _contract.Status = status;
        _contract.CompletionsRemaining = 0;
        _contract.RewardItems.Add(new QuestContractItem { Data = "never duplicate" });
        QuestContractBoard.ValidateRepost(_contract, "author", false).Should().BeEmpty();
        var draft = QuestContractBoard.CreateRepostDraft(_contract);
        draft.Id.Should().NotBe(_contract.Id);
        draft.Status.Should().Be(QuestContractStatus.Draft);
        draft.RewardCredits.Should().Be(1000);
        draft.TotalCompletions.Should().Be(10);
        draft.CompletionsRemaining.Should().Be(10);
        draft.RewardItems.Should().BeEmpty();
        draft.CompletedByPlayerId.Should().BeEmpty();
        draft.DateExpires.Should().Be(default);
        draft.Objectives[0].Quantity = 1;
        _contract.Objectives[0].Quantity.Should().Be(20);
    }

    [Test]
    public void PostAgainRejectsOtherAuthorsExistingDraftsAndModeratedOrActiveContracts()
    {
        _contract.Status = QuestContractStatus.Fulfilled;
        QuestContractBoard.ValidateRepost(_contract, "other", false).Should().NotBeEmpty();
        QuestContractBoard.ValidateRepost(_contract, "author", true).Should().Contain("existing draft");
        foreach (var status in new[] { QuestContractStatus.Draft, QuestContractStatus.Published, QuestContractStatus.TakenDown })
        {
            _contract.Status = status;
            QuestContractBoard.ValidateRepost(_contract, "author", false).Should().NotBeEmpty();
        }
    }

    private string Validate() => QuestContractBoard.ValidateDraft(_contract, "Ore", "Deliver ore.", _ => "Ore");
    private List<QuestContractDelivery> Payments() => ReadDeliveries().Where(x => x.IsRewardPayment).ToList();
    private List<QuestContractDelivery> ReadDeliveries() => _deliveries.Values.Select(Clone).ToList();
    private static T Clone<T>(T value) => JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value))!;

    private QuestContractDelivery Submit(string playerId, int attempt)
    {
        var delivery = new QuestContractDelivery
        {
            PlayerId = playerId, SourceContractId = _contract.Id, CompletionAttempt = attempt,
            HeldForCompletion = true, Items = new() { new() { Data = "ore", StackSize = 20 } }
        };
        _deliveries.Add(delivery.Id, Clone(delivery));
        return delivery;
    }

    private bool Complete(string playerId, int attempt) => QuestContractSettlement.Complete(
        Clone(_contract), playerId, attempt, ReadDeliveries, SaveContract, SaveDelivery);

    private void SaveContract(QuestContract contract) => Write(() => _contract = Clone(contract));
    private void SaveDelivery(QuestContractDelivery delivery) => Write(() => _deliveries[delivery.Id] = Clone(delivery));

    private void Write(Action write)
    {
        var fail = ++_writes == _failAt;
        if (fail && !_failAfterWrite) throw new IOException("Interrupted before write");
        write();
        if (fail && _failAfterWrite) throw new IOException("Interrupted after write");
    }
}
