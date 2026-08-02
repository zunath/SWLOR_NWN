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
/// Protects the graph-native conversation editor's defining layout: a compact tree drives editing,
/// while a faithful NUI preview follows live edits on its own tab.
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
    public void EditingAndGamePreviewRenderInSeparateTabs()
    {
        var viewModel = OpenEditor();
        var view = new NuiConversationEditorView { DataContext = viewModel };
        var window = new Window { Content = view, Width = 1500, Height = 900 };
        window.Show();

        try
        {
            window.UpdateLayout();
            var tabs = view.FindControl<TabControl>("ConversationEditorTabs");
            tabs.Should().NotBeNull();
            tabs!.Items.OfType<TabItem>().Select(tab => tab.Header?.ToString())
                .Should().Equal("Edit", "Preview");
            tabs.SelectedIndex.Should().Be(0, "editing is the primary workflow");

            var editText = view.GetVisualDescendants()
                .OfType<TextBlock>()
                .Select(block => block.Text ?? string.Empty)
                .ToList();
            editText.Should().Contain("Conversation tree");
            view.GetVisualDescendants().OfType<TextBox>().Should().NotBeEmpty(
                "the Edit tab contains the writing controls");
            view.FindControl<TextBox>("PrimaryNpcDialogueTextBox").Should().NotBeNull(
                "one NPC node should expose one primary dialogue field");
            view.GetVisualDescendants().OfType<Button>()
                .Select(button => button.Content?.ToString())
                .Should().NotContain("+ Styled text");
            var moreOptions = view.FindControl<Expander>("NpcMoreOptionsExpander");
            moreOptions.Should().NotBeNull();
            moreOptions!.IsExpanded = true;
            window.UpdateLayout();
            view.FindControl<ItemsControl>("FormattedPassagesItems")!.ItemCount.Should().Be(1);
            view.GetVisualDescendants().OfType<Button>()
                .Select(button => button.Content?.ToString())
                .Should().Contain("Remove", "every optional formatted passage must be removable");
            view.GetVisualDescendants().OfType<CheckBox>()
                .Select(checkBox => checkBox.Content?.ToString())
                .Should().NotContain("Only once per player",
                    "repeat behavior is expressed through explicit conversation conditions and action state");

            var selectedRow = viewModel.SelectedTreeRow;
            tabs.SelectedIndex = 1;
            window.UpdateLayout();
            var previewText = view.GetVisualDescendants()
                .OfType<TextBlock>()
                .Select(block => block.Text ?? string.Empty)
                .ToList();
            previewText.Should().Contain("IN-GAME PREVIEW");
            previewText.Should().Contain("Conversation", "the simulated title matches the runtime NUI window");
            previewText.Should().Contain("Selan Flembek");
            previewText.Should().Contain("Welcome, Player.", "dynamic tokens use representative preview values");
            previewText.Should().Contain("Tell me more.");
            view.FindControl<ScrollViewer>("PreviewDialogueScroll").Should().NotBeNull();
            view.FindControl<ScrollViewer>("PreviewResponsesScroll").Should().NotBeNull();

            tabs.SelectedIndex = 0;
            window.UpdateLayout();
            viewModel.SelectedTreeRow.Should().BeSameAs(selectedRow,
                "switching tabs must not lose the author's place in the tree");
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaTest]
    public void TreeUsesACompactFullWidthAreaAboveTheSelectedNodeEditor()
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
            var dragPreview = view.FindControl<Border>("TreeDragPreview");
            dragPreview.Should().NotBeNull();
            dragPreview!.IsVisible.Should().BeFalse("the destination preview only appears during a drag");
            dragPreview.Opacity.Should().BeLessThan(1d,
                "the dragged branch preview should remain translucent so its destination stays visible");

            treePanel!.Bounds.Width.Should().BeApproximately(selectedPanel!.Bounds.Width, 0.1,
                "the tree and inspector occupy separate full-width rows instead of cramped columns");
            treePanel.Bounds.Bottom.Should().BeLessThanOrEqualTo(selectedPanel.Bounds.Top,
                "the selected-node controls sit below the tree");
            treePanel.Bounds.Height.Should().BeApproximately(270, 0.1,
                "the tree should stay compact and leave most of the Edit tab for the selected node");
            selectedPanel.Bounds.Height.Should().BeGreaterThan(treePanel.Bounds.Height,
                "the selected-node editor is the primary authoring surface");
            ScrollViewer.GetHorizontalScrollBarVisibility(tree!).Should().Be(ScrollBarVisibility.Auto);
            ScrollViewer.GetVerticalScrollBarVisibility(tree).Should().Be(ScrollBarVisibility.Auto);
            tree.GetVisualDescendants().OfType<ListBoxItem>().Should().OnlyContain(item => item.Bounds.Height <= 32,
                "tree nodes should remain single compact rows");

            var text = view.GetVisualDescendants().OfType<TextBlock>()
                .Select(block => block.Text ?? string.Empty)
                .ToArray();
            text.Should().Contain("Conversation tree");
            text.Should().Contain(value => value.Contains("drag to reorder", StringComparison.Ordinal));
            text.Should().Contain("Show this line when…");
            text.Should().Contain("When this line appears…");
            view.GetVisualDescendants().OfType<Button>()
                .Select(button => button.Content?.ToString())
                .Should().NotContain(content => content == "↑" || content == "↓",
                    "dragging replaces the small reorder arrows");
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
    public void TreeBranchesCanBeFoldedIndividuallyAndExpandedAgain()
    {
        var viewModel = OpenEditor();
        var opening = viewModel.TreeRows.Single(row => row.IsNpc && row.Node?.Id == "first");
        opening.HasChildren.Should().BeTrue();
        opening.IsBranchExpanded.Should().BeTrue();

        viewModel.ToggleTreeBranchCommand.Execute(opening);

        var collapsedOpening = viewModel.TreeRows.Single(row => row.Key == opening.Key);
        collapsedOpening.IsBranchExpanded.Should().BeFalse();
        viewModel.TreeRows.Should().NotContain(row => row.Key.StartsWith(opening.Key + "/", StringComparison.Ordinal));

        viewModel.ToggleTreeBranchCommand.Execute(collapsedOpening);

        viewModel.TreeRows.Should().Contain(row => row.Key.StartsWith(opening.Key + "/", StringComparison.Ordinal));
        viewModel.TreeRows.Single(row => row.Key == opening.Key).IsBranchExpanded.Should().BeTrue();
    }

    [Test]
    public void AllTreeBranchesCanBeCollapsedAndExpandedWithoutEditingTheConversation()
    {
        var viewModel = OpenEditor();
        var originalCount = viewModel.TreeRows.Count;
        viewModel.IsDirty.Should().BeFalse();

        viewModel.CollapseAllTreeBranchesCommand.Execute(null);

        viewModel.TreeRows.Count.Should().BeLessThan(originalCount);
        viewModel.TreeRows.Where(row => row.HasChildren).Should().OnlyContain(row => !row.IsBranchExpanded);
        viewModel.IsDirty.Should().BeFalse("folding branches is editor view state, not conversation data");

        viewModel.ExpandAllTreeBranchesCommand.Execute(null);

        viewModel.TreeRows.Count.Should().Be(originalCount);
        viewModel.IsDirty.Should().BeFalse();
    }

    [Test]
    public void TreeStatusUsesTooltipIconsInsteadOfASecondLineOfText()
    {
        var viewModel = OpenEditor();
        var opening = viewModel.TreeRows.Single(row => row.IsNpc && row.Node?.Id == "first");
        var player = viewModel.TreeRows.Single(row => row.IsPlayer);
        var conditionalNpc = viewModel.TreeRows.Single(row => row.IsNpc && row.Depth == 2);

        opening.VisibilityIcon.Should().NotBeNullOrWhiteSpace();
        opening.VisibilityToolTip.Should().Contain("Always shown");
        player.HasActions.Should().BeTrue();
        player.ActionToolTip.Should().Contain("1 action");
        conditionalNpc.VisibilityToolTip.Should().Contain("Conditional");
    }

    [Test]
    public void NuiEditorOffersOnlyBehaviorChoicesWithDistinctAuthoringSurfaces()
    {
        var viewModel = OpenEditor();

        viewModel.BehaviorOptions.Select(option => option.Name).Should().Equal("Merchant", "Conversation");
        viewModel.BehaviorOptions.Should().NotContain(option => option.Kind == ConversationBehaviorKind.QuestGiver);
        viewModel.SelectedBehavior!.Kind.Should().Be(ConversationBehaviorKind.Conversation,
            "quest snippets are authored directly in the general conversation graph editor");
    }

    [Test]
    public void TreeDragDropChangesTheRuntimeEvaluationOrder()
    {
        var viewModel = OpenEditor();
        var firstOpening = viewModel.TreeRows.Single(row =>
            row.IsNpc && row.IsEntryPoint && row.Node?.Id == "first");
        var secondOpening = viewModel.TreeRows.Single(row =>
            row.IsNpc && row.IsEntryPoint && row.Node?.Id == "second");

        viewModel.CanDropTreeRow(secondOpening, firstOpening).Should().BeTrue();
        viewModel.DropTreeRow(secondOpening, firstOpening).Should().BeTrue();

        viewModel.SnapshotGraph().EntryPoints.Select(link => link.TargetNodeId)
            .Should().Equal(["second", "first"],
                "the highest opening is evaluated first by the runtime");

        viewModel.CanUndo.Should().BeTrue();
        viewModel.UndoCommand.Execute(null);
        viewModel.SnapshotGraph().EntryPoints.Select(link => link.TargetNodeId)
            .Should().Equal(["first", "second"], "a drag should be one undoable edit");
    }

    [Test]
    public void DraggingAnAdjacentPlayerResponseDownPlacesItAfterTheHoveredResponse()
    {
        var viewModel = OpenEditor();
        viewModel.AddChoiceCommand.Execute(null);
        var firstResponse = viewModel.TreeRows.Single(row => row.IsPlayer && row.Choice?.Id == "ask");
        var secondResponse = viewModel.TreeRows.Single(row =>
            row.IsPlayer && row.ParentNode?.Id == "first" && row.Choice?.Id != "ask");

        viewModel.TreeDropInsertsAfter(firstResponse, secondResponse).Should().BeTrue(
            "a downward drag onto a compact row should not require hitting its lower 12 pixels");
        viewModel.DropTreeRow(firstResponse, secondResponse).Should().BeTrue();

        viewModel.SnapshotGraph().Nodes["first"].Choices.Select(link => link.ChoiceId)
            .Should().Equal([secondResponse.Choice!.Id, "ask"]);
    }

    [Test]
    public void TreeDragDropDoesNotReparentConversationBranches()
    {
        var viewModel = OpenEditor();
        var opening = viewModel.TreeRows.Single(row => row.IsNpc && row.IsEntryPoint && row.Node?.Id == "first");
        var response = viewModel.TreeRows.Single(row => row.IsPlayer);
        var followUp = viewModel.TreeRows.Single(row => row.IsNpc && row.Depth == 2);

        viewModel.CanDropTreeRow(response, opening).Should().BeFalse();
        viewModel.CanDropTreeRow(followUp, opening).Should().BeFalse();
        viewModel.DropTreeRow(followUp, opening).Should().BeFalse();
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
    public void FormattedPassagesBelongToOneNpcLineAndCanBeRemoved()
    {
        var viewModel = OpenEditor();

        viewModel.PrimaryTextBlock!.Text.Should().Be("Welcome, {{player.name}}.");
        viewModel.AdditionalTextBlocks.Should().ContainSingle();
        viewModel.MoreOptionsHeader.Should().Be("More options · 1 formatted passage");

        var passage = viewModel.AdditionalTextBlocks.Single();
        passage.Style.Should().Be(ConversationTextStyle.Highlight);
        viewModel.RemoveTextBlockCommand.Execute(passage);

        viewModel.SnapshotGraph().Nodes["first"].Text.Should().ContainSingle();
        viewModel.AdditionalTextBlocks.Should().BeEmpty();
        viewModel.MoreOptionsHeader.Should().Be("More options");

        viewModel.AddTextBlockCommand.Execute(null);
        viewModel.AdditionalTextBlocks.Should().ContainSingle();
        viewModel.AdditionalTextBlocks[0].Style.Should().Be(ConversationTextStyle.Highlight);
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
