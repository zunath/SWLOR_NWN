using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.NUnit;
using Avalonia.VisualTree;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Editors;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests;

/// <summary>
/// Protects the graph-native conversation editor's defining layout: writing and a faithful NUI
/// preview are visible together, and the preview follows live edits instead of being a stale tab.
/// </summary>
public sealed class NuiConversationEditorViewRenderTests
{
    private string _filePath = string.Empty;

    [SetUp]
    public void CreateConversation()
    {
        _filePath = Path.Combine(Path.GetTempPath(), $"swlor-nui-preview-{Guid.NewGuid():N}.conversation.json");
        var graph = new ConversationGraph
        {
            Id = "preview_test",
            Title = "preview_test"
        };

        var first = new ConversationNode
        {
            Id = "first",
            SpeakerName = "Selan Flembek"
        };
        first.Text.Add(new ConversationTextBlock
        {
            Text = "Welcome, {{player.name}}.",
            Style = ConversationTextStyle.Normal
        });
        first.Text.Add(new ConversationTextBlock
        {
            Text = "This matters.",
            Style = ConversationTextStyle.Highlight
        });
        first.Choices.Add(new ConversationChoiceLink { ChoiceId = "ask" });

        var second = new ConversationNode { Id = "second", SpeakerName = "Dockhand" };
        second.Text.Add(new ConversationTextBlock { Text = "The second line." });

        var choice = new ConversationChoice
        {
            Id = "ask",
            Text = new ConversationTextBlock
            {
                Text = "Tell me more.",
                Style = ConversationTextStyle.PlayerReply
            }
        };
        choice.Actions.Add(new ConversationAction
        {
            Key = "action-accept-quest",
            Arguments = { "preview_quest" }
        });
        choice.Next.Add(new ConversationLink
        {
            TargetNodeId = second.Id,
            Conditions =
            {
                new ConversationCondition
                {
                    Key = "condition-has-quest",
                    Arguments = { "preview_quest" }
                }
            }
        });

        graph.Nodes.Add(first.Id, first);
        graph.Nodes.Add(second.Id, second);
        graph.Choices.Add(choice.Id, choice);
        graph.EntryPoints.Add(new ConversationLink { TargetNodeId = first.Id });
        graph.EntryPoints.Add(new ConversationLink { TargetNodeId = second.Id });

        File.WriteAllText(_filePath, JsonConvert.SerializeObject(graph));
    }

    [TearDown]
    public void DeleteConversation()
    {
        if (File.Exists(_filePath))
            File.Delete(_filePath);
    }

    [AvaloniaTest]
    public void WritingAndGamePreviewRenderSideBySide()
    {
        var viewModel = OpenEditor();
        var view = new NuiConversationEditorView { DataContext = viewModel };
        var window = new Window { Content = view, Width = 1500, Height = 900 };
        window.Show();

        try
        {
            window.UpdateLayout();
            var text = view.GetVisualDescendants()
                .OfType<TextBlock>()
                .Select(block => block.Text ?? string.Empty)
                .ToList();

            view.GetVisualDescendants().OfType<TabControl>().Should().BeEmpty(
                "preview is no longer hidden behind a separate tab");
            text.Should().Contain("IN-GAME PREVIEW");
            text.Should().Contain("Conversation", "the simulated title matches the runtime NUI window");
            text.Should().Contain("Selan Flembek");
            text.Should().Contain("Welcome, Player.", "dynamic tokens use representative preview values");
            text.Should().Contain("Tell me more.");
            view.GetVisualDescendants().OfType<TextBox>().Should().NotBeEmpty(
                "the writing controls remain visible beside the preview");
            view.FindControl<ScrollViewer>("PreviewDialogueScroll").Should().NotBeNull();
            view.FindControl<ScrollViewer>("PreviewResponsesScroll").Should().NotBeNull();
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void TreeUsesTheFullAuthoringWidthAboveTheSelectedNodeEditor()
    {
        var viewModel = OpenEditor();
        var view = new NuiConversationEditorView { DataContext = viewModel };
        var window = new Window { Content = view, Width = 1500, Height = 900 };
        window.Show();

        try
        {
            window.UpdateLayout();

            var treePanel = view.FindControl<Border>("ConversationTreePanel");
            var selectedPanel = view.FindControl<Border>("SelectedNodePanel");
            var tree = view.FindControl<ListBox>("ConversationTree");
            treePanel.Should().NotBeNull();
            selectedPanel.Should().NotBeNull();
            tree.Should().NotBeNull();

            treePanel!.Bounds.Width.Should().BeApproximately(selectedPanel!.Bounds.Width, 0.1,
                "the tree and inspector occupy separate full-width rows instead of cramped columns");
            treePanel.Bounds.Bottom.Should().BeLessThanOrEqualTo(selectedPanel.Bounds.Top,
                "the selected-node controls sit below the tree");
            ScrollViewer.GetHorizontalScrollBarVisibility(tree!).Should().Be(ScrollBarVisibility.Auto);
            ScrollViewer.GetVerticalScrollBarVisibility(tree).Should().Be(ScrollBarVisibility.Auto);

            var text = view.GetVisualDescendants().OfType<TextBlock>()
                .Select(block => block.Text ?? string.Empty)
                .ToArray();
            text.Should().Contain("Conversation tree");
            text.Should().Contain("Show this line when…");
            text.Should().Contain("When this line appears…");
        }
        finally
        {
            window.Close();
        }
    }

    [Test]
    public void TreeShowsNestedRoutesAndEditsOperationsOnTheSelectedRoute()
    {
        var viewModel = OpenEditor();

        viewModel.TreeRows.Select(row => (row.Kind, row.Depth, row.Text)).Should().ContainInOrder(
            (NuiConversationTreeRowKind.NpcLine, 0, "Welcome, {{player.name}}.This matters."),
            (NuiConversationTreeRowKind.PlayerChoice, 1, "Tell me more."),
            (NuiConversationTreeRowKind.NpcLine, 2, "The second line."));

        var playerRow = viewModel.TreeRows.Single(row => row.IsPlayer);
        viewModel.SelectedTreeRow = playerRow;
        viewModel.IsPlayerTreeRowSelected.Should().BeTrue();
        viewModel.Actions.Should().ContainSingle(action => action.Snippet.Key == "action-accept-quest");
        viewModel.ActionSectionTitle.Should().Be("When the player selects it…");

        var nestedNpcRow = viewModel.TreeRows.Single(row => row.IsNpc && row.Depth == 2);
        viewModel.SelectedTreeRow = nestedNpcRow;
        viewModel.IsNpcTreeRowSelected.Should().BeTrue();
        viewModel.Conditions.Should().ContainSingle(condition => condition.Snippet.Key == "condition-has-quest",
            "nested NPC checks belong to the incoming route, not only to top-level openings");
        viewModel.PreviewSpeaker.Should().Be("Dockhand");
    }

    [Test]
    public void TreeOrderingCommandsChangeTheRuntimeEvaluationOrder()
    {
        var viewModel = OpenEditor();
        var secondOpening = viewModel.TreeRows.Single(row =>
            row.IsNpc && row.IsEntryPoint && row.Node?.Id == "second");

        viewModel.MoveTreeRowUpCommand.Execute(secondOpening);

        viewModel.SnapshotGraph().EntryPoints.Select(link => link.TargetNodeId)
            .Should().Equal(["second", "first"],
                "the highest opening is evaluated first by the runtime");
    }

    [Test]
    public void EndingAResponseImmediatelyRemovesItsNestedBranchFromTheTree()
    {
        var viewModel = OpenEditor();
        viewModel.SelectedTreeRow = viewModel.TreeRows.Single(row => row.IsPlayer);

        viewModel.SelectedChoice!.EndsConversation = true;

        viewModel.TreeRows.Should().NotContain(row => row.Depth == 2);
        viewModel.SnapshotGraph().Choices["ask"].Next.Should().BeEmpty();
    }

    [Test]
    public void PreviewTracksTheSelectedLineAndLiveTextEdits()
    {
        var viewModel = OpenEditor();

        viewModel.SelectedOpening = viewModel.OpeningLines[1];
        viewModel.PreviewSpeaker.Should().Be("Dockhand");
        viewModel.PreviewTextBlocks.Single().Text.Should().Be("The second line.");

        viewModel.TextBlocks[0].Text = "The edited second line.";
        viewModel.PreviewTextBlocks.Single().Text.Should().Be("The edited second line.");
    }

    [Test]
    public void ALineWithoutResponsesShowsTheRuntimeGoodbyeChoice()
    {
        var viewModel = OpenEditor();

        viewModel.SelectedOpening = viewModel.OpeningLines[1];

        viewModel.PreviewChoices.Should().ContainSingle();
        viewModel.PreviewChoices[0].DisplayText.Should().Be("Goodbye.");
    }

    private NuiConversationEditorViewModel OpenEditor() => new(
        _filePath,
        "preview_test",
        SnippetCatalog.Build(),
        null,
        new OutputLogService(),
        new StubPrompts());

    private sealed class StubPrompts : IEditorPromptService
    {
        public Task<UnsavedChangesChoice> ConfirmCloseAsync(string name) =>
            Task.FromResult(UnsavedChangesChoice.Cancel);

        public Task<ExternalChangeChoice> ConfirmExternalChangeAsync(string path) =>
            Task.FromResult(ExternalChangeChoice.Cancel);

        public Task<string?> PromptForTextAsync(
            string headline,
            string message,
            string initialValue,
            string confirmLabel) => Task.FromResult<string?>(null);

        public Task<bool> ConfirmDestructiveAsync(string headline, string message, string confirmLabel) =>
            Task.FromResult(false);
    }
}
