using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.QuestContractService;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Service;

public class QuestContractRepostTests
{
    // These rules touch neither the engine nor the database.
    private static readonly QuestContractBoardService Board = new(new InMemoryDBService());

    /// <summary>
    /// Checks reusable statuses produce a fresh draft whose editable values are independent of the
    /// source, with no duplicated escrow or lifecycle state.
    /// </summary>
    [TestCase(QuestContractStatus.Fulfilled)]
    [TestCase(QuestContractStatus.Expired)]
    [TestCase(QuestContractStatus.Cancelled)]
    public void PostAgainPrefillsAnIndependentEditableDraftWithoutCopyingEscrow(QuestContractStatus status)
    {
        var source = new QuestContract
        {
            AuthorPlayerId = "author", AuthorName = "Author", AuthorCDKey = "account",
            Status = status, Title = "Ore", Description = "Deliver ore.", RewardCredits = 1000,
            CompletionsRemaining = 0, CompletedByPlayerId = "worker", SettlementPending = 0,
            DatePublished = DateTime.UtcNow.AddDays(-10), DateExpires = DateTime.UtcNow.AddDays(20),
            DateClosed = DateTime.UtcNow,
            Objectives = new() { new() { ItemResref = "ore", ItemName = "Ore", Quantity = 20 } },
            RewardItems = new() { new() { Data = "never duplicate this escrow" } }
        };

        Board.ValidateRepost(source, "author", false).Should().BeEmpty();
        var draft = Board.CreateRepostDraft(source);
        draft.Id.Should().NotBe(source.Id);
        draft.Status.Should().Be(QuestContractStatus.Draft);
        draft.AuthorPlayerId.Should().Be(source.AuthorPlayerId);
        draft.AuthorName.Should().Be(source.AuthorName);
        draft.AuthorCDKey.Should().Be(source.AuthorCDKey);
        draft.Title.Should().Be(source.Title);
        draft.Description.Should().Be(source.Description);
        draft.RewardCredits.Should().Be(1000);
        draft.CompletionsRemaining.Should().Be(1);
        draft.RewardItems.Should().BeEmpty();
        draft.CompletedByPlayerId.Should().BeEmpty();
        draft.SettlementPending.Should().Be(0);
        draft.DatePublished.Should().Be(default);
        draft.DateExpires.Should().Be(default);
        draft.DateClosed.Should().BeNull();
        draft.Objectives.Should().BeEquivalentTo(source.Objectives);
        draft.Title = "Edited contract";
        draft.Description = "Edited description";
        draft.RewardCredits = 2000;
        draft.Objectives[0].Quantity = 1;
        source.Title.Should().Be("Ore");
        source.Description.Should().Be("Deliver ore.");
        source.RewardCredits.Should().Be(1000);
        source.Objectives[0].Quantity.Should().Be(20);
        source.RewardItems.Should().ContainSingle();
        Board.ValidateDraft(draft, draft.Title, draft.Description, _ => "Ore").Should().BeEmpty();
    }

    /// <summary>Rejects missing or foreign sources, existing drafts, active contracts, and moderated takedowns.</summary>
    [Test]
    public void PostAgainRejectsOtherAuthorsExistingDraftsAndModeratedOrActiveContracts()
    {
        var source = new QuestContract { AuthorPlayerId = "author", Status = QuestContractStatus.Fulfilled };
        Board.ValidateRepost(null, "author", false).Should().NotBeEmpty();
        Board.ValidateRepost(source, "other", false).Should().NotBeEmpty();
        Board.ValidateRepost(source, "author", true).Should().Contain("existing draft");
        foreach (var status in new[] { QuestContractStatus.Draft, QuestContractStatus.Published, QuestContractStatus.TakenDown })
        {
            source.Status = status;
            Board.ValidateRepost(source, "author", false).Should().NotBeEmpty();
        }
    }
}
