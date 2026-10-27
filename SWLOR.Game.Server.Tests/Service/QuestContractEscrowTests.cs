using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.QuestContractService;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Service;

public class QuestContractEscrowTests
{
    // These rules touch neither the engine nor the database.
    private static readonly QuestContractBoardService Board = new(new InMemoryDBService());

    [Test]
    public void PartialSubmission_CannotBeClaimedByEitherPartyWhileContractIsActive()
    {
        var contract = Contract(QuestContractStatus.Published);
        var delivery = Submission("worker");
        Board.TryReleaseSubmission(delivery, contract).Should().BeFalse();
        delivery.PlayerId.Should().Be("worker");
        Board.IsClaimable(delivery).Should().BeFalse();
    }

    [TestCase(QuestContractStatus.Cancelled)]
    [TestCase(QuestContractStatus.TakenDown)]
    [TestCase(QuestContractStatus.Expired)]
    public void UnfinishedContract_ReturnsEachParticipantsItems(QuestContractStatus status)
    {
        foreach (var playerId in new[] { "worker", "other-worker" })
        {
            var delivery = Submission(playerId);
            Board.TryReleaseSubmission(delivery, Contract(status)).Should().BeTrue();
            delivery.PlayerId.Should().Be(playerId);
            Board.IsClaimable(delivery).Should().BeTrue();
        }
    }

    [Test]
    public void Fulfillment_ReleasesOnlyWinnersItemsToAuthorAndRefundsOtherPlayers()
    {
        var contract = Contract(QuestContractStatus.Fulfilled);
        foreach (var playerId in new[] { "worker", "other-worker" })
        {
            var delivery = Submission(playerId);
            Board.TryReleaseSubmission(delivery, contract).Should().BeTrue();
            delivery.PlayerId.Should().Be(playerId == "worker" ? "author" : playerId);
            Board.TryReleaseSubmission(delivery, contract).Should().BeFalse("settlement is idempotent");
            delivery.Items.Should().ContainSingle();
        }
    }

    [Test]
    public void Abandonment_RefundsOnlyThatAttemptAndNeverLetsItBecomeAWinnersDelivery()
    {
        var contract = Contract(QuestContractStatus.Published);
        var abandoned = Submission("worker");
        var other = Submission("other-worker");
        Board.TryReleaseSubmission(abandoned, contract, "worker").Should().BeTrue();
        Board.TryReleaseSubmission(other, contract, "worker").Should().BeFalse();
        contract.Status = QuestContractStatus.Fulfilled;
        Board.TryReleaseSubmission(abandoned, contract).Should().BeFalse();
        abandoned.PlayerId.Should().Be("worker");
        QuestContractFactory.BuildQuest(contract).OnAbandonActions.Should().ContainSingle();
    }

    [Test]
    public void MissingContract_ReturnsSubmissionsAndLegacyDeliveriesRemainClaimable()
    {
        var delivery = Submission("worker");
        Board.TryReleaseSubmission(delivery, null).Should().BeTrue();
        delivery.PlayerId.Should().Be("worker");
        var legacy = JsonConvert.DeserializeObject<QuestContractDelivery>("{\"PlayerId\":\"worker\",\"Credits\":100}")!;
        Board.IsClaimable(legacy).Should().BeTrue();
        Board.IsClaimable(new QuestContractDelivery { IsRewardPayment = true }).Should().BeFalse();
    }

    private static QuestContract Contract(QuestContractStatus status) => new()
    {
        AuthorPlayerId = "author", CompletedByPlayerId = "worker", Status = status
    };

    private static QuestContractDelivery Submission(string playerId) => new()
    {
        PlayerId = playerId, HeldForCompletion = true,
        Items = new List<QuestContractItem> { new() { Data = "submitted item", StackSize = 4 } }
    };
}
