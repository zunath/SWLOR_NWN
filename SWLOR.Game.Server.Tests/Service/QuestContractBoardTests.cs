using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.QuestContractService;

namespace SWLOR.Game.Server.Tests.Service;

public class QuestContractBoardTests
{
    [Test]
    public void QuestContractObjective_DefaultsQuantityToOne()
    {
        new QuestContractObjective().Quantity.Should().Be(1);
    }

    [Test]
    public void SanitizeContractText_NullInputReturnsEmpty()
    {
        QuestContractBoard.SanitizeContractText(null, 100).Should().Be(string.Empty);
    }

    [Test]
    public void SanitizeContractText_EmptyInputReturnsEmpty()
    {
        QuestContractBoard.SanitizeContractText(string.Empty, 100).Should().Be(string.Empty);
    }

    [Test]
    public void SanitizeContractText_WhitespaceInputReturnsEmpty()
    {
        QuestContractBoard.SanitizeContractText("   ", 100).Should().Be(string.Empty);
    }

    [Test]
    public void SanitizeContractText_StripsColorTokensAndControlCharacters()
    {
        var source = ReadSource("SWLOR.Game.Server", "Service", "QuestContractBoard.cs");
        var method = ExtractMethod(source, "public static string SanitizeContractText(string input, int maxLength)");

        method.Should().Contain("UtilPlugin.StripColors(input).Trim()");
        method.Should().Contain("if (char.IsControl(character))");
        method.Should().Contain("continue;");
    }

    [Test]
    public void SanitizeContractText_CollapsesWhitespaceAndCapsLength()
    {
        var source = ReadSource("SWLOR.Game.Server", "Service", "QuestContractBoard.cs");
        var method = ExtractMethod(source, "public static string SanitizeContractText(string input, int maxLength)");

        method.Should().Contain("while (sanitizedText.Contains(\"  \"))");
        method.Should().Contain("sanitizedText = sanitizedText.Replace(\"  \", \" \");");
        method.Should().Contain("if (sanitizedText.Length > maxLength)");
        method.Should().Contain("sanitizedText = sanitizedText.Substring(0, maxLength).TrimEnd();");
    }

    [Test]
    public void CalculatePostingFee_ChargesPercentAboveFloor()
    {
        QuestContractBoard.CalculatePostingFee(3000).Should().Be(150);
    }

    [Test]
    public void CalculatePostingFee_FloorsAtMinimumPostingFee()
    {
        QuestContractBoard.CalculatePostingFee(1000).Should().Be(QuestContractBoard.MinimumPostingFee);
    }

    [Test]
    public void CalculatePostingFee_BoundaryExactlyAtFloorStaysAtFloor()
    {
        QuestContractBoard.CalculatePostingFee(2000).Should().Be(QuestContractBoard.MinimumPostingFee);
        QuestContractBoard.CalculatePostingFee(2001).Should().Be(QuestContractBoard.MinimumPostingFee);
        QuestContractBoard.CalculatePostingFee(2100).Should().Be(105);
    }

    [Test]
    public void CalculatePostingFee_ZeroRewardStillChargesFloor()
    {
        QuestContractBoard.CalculatePostingFee(0).Should().Be(QuestContractBoard.MinimumPostingFee);
    }

    [Test]
    public void CalculateTotalPublishCost_AddsPercentFeeToEscrow()
    {
        QuestContractBoard.CalculateTotalPublishCost(5000).Should().Be(5250);
    }

    [Test]
    public void CalculateTotalPublishCost_UsesFloorFeeForSmallRewards()
    {
        QuestContractBoard.CalculateTotalPublishCost(500).Should().Be(600);
    }

    [Test]
    public void ValidateDraft_RequiresTitle()
    {
        var draft = CreateValidDraft();

        QuestContractBoard.ValidateDraft(draft, string.Empty, "Description", ResolveKnownItem)
            .Should().Be("Please enter a title for your contract.");
    }

    [Test]
    public void ValidateDraft_RequiresDescription()
    {
        var draft = CreateValidDraft();

        QuestContractBoard.ValidateDraft(draft, "Title", string.Empty, ResolveKnownItem)
            .Should().Be("Please enter a description for your contract.");
    }

    [Test]
    public void ValidateDraft_RequiresAtLeastOneObjective()
    {
        var draft = CreateValidDraft();
        draft.Objectives = new List<QuestContractObjective>();

        QuestContractBoard.ValidateDraft(draft, "Title", "Description", ResolveKnownItem)
            .Should().Be($"A contract must have between 1 and {QuestContractBoard.MaxObjectives} objectives.");
    }

    [Test]
    public void ValidateDraft_RejectsTooManyObjectives()
    {
        var draft = CreateValidDraft();
        draft.Objectives = new List<QuestContractObjective>();

        for (var i = 0; i <= QuestContractBoard.MaxObjectives; i++)
        {
            draft.Objectives.Add(new QuestContractObjective { ItemResref = $"item_{i}", Quantity = 1 });
        }

        QuestContractBoard.ValidateDraft(draft, "Title", "Description", ResolveKnownItem)
            .Should().Be($"A contract must have between 1 and {QuestContractBoard.MaxObjectives} objectives.");
    }

    [Test]
    public void ValidateDraft_RejectsObjectiveMissingItemResref()
    {
        var draft = CreateValidDraft();
        draft.Objectives[0].ItemResref = " ";

        QuestContractBoard.ValidateDraft(draft, "Title", "Description", ResolveKnownItem)
            .Should().Be("One or more objectives is missing an item.");
    }

    [Test]
    public void ValidateDraft_RejectsObjectiveQuantityBelowMinimum()
    {
        var draft = CreateValidDraft();
        draft.Objectives[0].Quantity = 0;

        QuestContractBoard.ValidateDraft(draft, "Title", "Description", ResolveKnownItem)
            .Should().Be($"Objective quantities must be between 1 and {QuestContractBoard.MaxObjectiveQuantity}.");
    }

    [Test]
    public void ValidateDraft_RejectsObjectiveQuantityAboveMaximum()
    {
        var draft = CreateValidDraft();
        draft.Objectives[0].Quantity = QuestContractBoard.MaxObjectiveQuantity + 1;

        QuestContractBoard.ValidateDraft(draft, "Title", "Description", ResolveKnownItem)
            .Should().Be($"Objective quantities must be between 1 and {QuestContractBoard.MaxObjectiveQuantity}.");
    }

    [Test]
    public void ValidateDraft_AcceptsMaximumObjectiveQuantity()
    {
        var draft = CreateValidDraft();
        draft.Objectives[0].Quantity = QuestContractBoard.MaxObjectiveQuantity;

        QuestContractBoard.ValidateDraft(draft, "Title", "Description", ResolveKnownItem)
            .Should().Be(string.Empty);
    }

    [Test]
    public void ValidateDraft_RejectsUnresolvableItemResref()
    {
        var draft = CreateValidDraft();
        draft.Objectives[0].ItemResref = "not_a_real_item";

        QuestContractBoard.ValidateDraft(draft, "Title", "Description", ResolveKnownItem)
            .Should().Be("Item 'not_a_real_item' is not a valid item.");
    }

    [Test]
    public void ValidateDraft_RejectsRewardCreditsBelowMinimum()
    {
        var draft = CreateValidDraft();
        draft.RewardCredits = QuestContractBoard.MinRewardCredits - 1;

        QuestContractBoard.ValidateDraft(draft, "Title", "Description", ResolveKnownItem)
            .Should().Be($"Reward credits must be at least {QuestContractBoard.MinRewardCredits}.");
    }

    [Test]
    public void ValidateDraft_AcceptsValidDraft()
    {
        var draft = CreateValidDraft();

        QuestContractBoard.ValidateDraft(draft, "Title", "Description", ResolveKnownItem)
            .Should().Be(string.Empty);
    }

    [Test]
    public void ValidateDraft_AllowsRewardItems()
    {
        var draft = CreateValidDraft();
        draft.RewardItems.Add(new QuestContractItem { Name = "Sword" });

        QuestContractBoard.ValidateDraft(draft, "Title", "Description", ResolveKnownItem)
            .Should().Be(string.Empty);
    }

    private static string ResolveKnownItem(string resref)
    {
        return resref == "not_a_real_item" ? string.Empty : "Some Item";
    }

    private static QuestContract CreateValidDraft()
    {
        return new QuestContract
        {
            Title = "Contract Title",
            Description = "Contract Description",
            Objectives = new List<QuestContractObjective>
            {
                new QuestContractObjective { ItemResref = "item_resref", ItemName = "Some Item", Quantity = 1 }
            },
            CompletionsRemaining = 1,
            RewardCredits = QuestContractBoard.MinRewardCredits
        };
    }

    private static string ReadSource(params string[] pathParts)
    {
        var fullPath = Path.Combine(new[] { FindRepositoryRoot().FullName }.Concat(pathParts).ToArray());
        return File.ReadAllText(fullPath);
    }

    private static string ExtractMethod(string source, string signature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, $"signature '{signature}' should exist");

        var openBrace = source.IndexOf('{', start);
        openBrace.Should().BeGreaterThanOrEqualTo(0, $"signature '{signature}' should have an opening brace");

        var depth = 0;
        for (var i = openBrace; i < source.Length; i++)
        {
            if (source[i] == '{')
            {
                depth++;
            }
            else if (source[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return source.Substring(start, i - start + 1);
                }
            }
        }

        throw new InvalidOperationException($"Method '{signature}' was not closed.");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("the tests should run inside the repository checkout");
        return directory!;
    }
}
