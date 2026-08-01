using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.NUnit;
using Avalonia.VisualTree;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Toolset.Domain.Conversations;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Editors;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Shell.Views;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests;

/// <summary>
/// Opening a conversation is a corpus-level contract: every Explorer row gets an editable NUI or
/// native-exception editor. Ordinary module conversations must use the graph-native editor.
/// </summary>
public sealed class ConversationEditorOpeningTests
{
    [Test]
    public void EveryAuthoredConversationHasAnExplicitOpeningRoute()
    {
        var graphDirectory = Path.Combine(
            CorpusLocator.RepositoryRoot,
            "SWLOR.Game.Server",
            "ConversationData");
        var dialogDirectory = Path.Combine(CorpusLocator.ModuleDirectory, "dlg");
        var ids = Directory.EnumerateFiles(dialogDirectory, "*.dlg.json")
            .Select(path => Path.GetFileName(path)[..^".dlg.json".Length])
            .Where(id => !IsGeneratedShell(id))
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

        var routes = ids.Select(id => ConversationEditorRoute.Resolve(
                id,
                Path.Combine(graphDirectory, id + ".conversation.json"),
                Path.Combine(dialogDirectory, id + ".dlg.json")))
            .ToArray();

        routes.Should().HaveCount(346);
        routes.Should().NotContain(route => route.Kind == ConversationEditorRouteKind.Missing);
        routes.Should().OnlyContain(route => route.OpensEditor,
            "every authored conversation shown in Module Contents must open an editor");
        routes.Count(route => route.Kind == ConversationEditorRouteKind.NuiGraph).Should().Be(329);
        routes.Count(route => route.Kind == ConversationEditorRouteKind.LegacyDialog).Should().Be(16);
        routes.Count(route => route.Kind == ConversationEditorRouteKind.LegacyException).Should().Be(1);
        routes.Single(route => route.Kind == ConversationEditorRouteKind.LegacyException).Path
            .Should().EndWith("dmfi_universal.dlg.json",
                "DMFI is the deliberate native NWN exception");
    }

    [Test]
    public void EveryNuiConversationGraphConstructsTheRealEditorViewModel()
    {
        var graphDirectory = Path.Combine(
            CorpusLocator.RepositoryRoot,
            "SWLOR.Game.Server",
            "ConversationData");
        var snippets = SnippetCatalog.Build();
        var failures = new List<string>();

        foreach (var path in Directory.EnumerateFiles(graphDirectory, "*.conversation.json")
                     .OrderBy(path => path, StringComparer.Ordinal))
        {
            var id = Path.GetFileName(path)[..^".conversation.json".Length];
            try
            {
                var editor = new NuiConversationEditorViewModel(
                    path,
                    id,
                    snippets,
                    null,
                    new OutputLogService(),
                    new StubPrompts());

                ConversationGraphValidator.Validate(editor.SnapshotGraph()).Should().BeEmpty(id);
                editor.OnClose();
            }
            catch (Exception ex)
            {
                failures.Add($"{id}: {ex}");
            }
        }

        failures.Should().BeEmpty(
            "every graph shown in Module Contents must survive the same constructor used by a click");
    }

    [Test]
    public void EveryLegacyConversationConstructsTheEditableLegacyViewModel()
    {
        var graphDirectory = Path.Combine(
            CorpusLocator.RepositoryRoot,
            "SWLOR.Game.Server",
            "ConversationData");
        var dialogDirectory = Path.Combine(CorpusLocator.ModuleDirectory, "dlg");
        var snippets = SnippetCatalog.Build();
        var failures = new List<string>();

        foreach (var path in Directory.EnumerateFiles(dialogDirectory, "*.dlg.json")
                     .Where(path => !File.Exists(Path.Combine(
                         graphDirectory,
                         Path.GetFileName(path)[..^".dlg.json".Length] + ".conversation.json")))
                     .Where(path => !IsGeneratedShell(
                         Path.GetFileName(path)[..^".dlg.json".Length]))
                     .OrderBy(path => path, StringComparer.Ordinal))
        {
            var id = Path.GetFileName(path)[..^".dlg.json".Length];
            try
            {
                var editor = new ConversationEditorViewModel(
                    path,
                    id,
                    snippets,
                    null,
                    new OutputLogService(),
                    new StubPrompts());

                editor.LiveDialog.Should().NotBeNull(id);
                editor.OnClose();
            }
            catch (Exception ex)
            {
                failures.Add($"{id}: {ex}");
            }
        }

        failures.Should().BeEmpty(
            "native exceptions must remain editable while their dedicated systems are retained");
    }

    [Test]
    public void MigratedScriptConversationUsesTheNuiGraph()
    {
        const string id = "dt_barman_gen";
        var graphPath = Path.Combine(
            CorpusLocator.RepositoryRoot, "SWLOR.Game.Server", "ConversationData", id + ".conversation.json");
        var route = ConversationEditorRoute.Resolve(
            id,
            graphPath,
            Path.Combine(CorpusLocator.ModuleDirectory, "dlg", id + ".dlg.json"));

        route.Kind.Should().Be(ConversationEditorRouteKind.NuiGraph);
        var editor = new NuiConversationEditorViewModel(
            graphPath, id, SnippetCatalog.Build(), null, new OutputLogService(), new StubPrompts());
        ConversationGraphValidator.Validate(editor.SnapshotGraph()).Should().BeEmpty();
        editor.OnClose();
    }

    [AvaloniaTest]
    public void MigratedScriptConversationRendersNuiEditorWithoutLegacyWarning()
    {
        const string id = "dt_barman_gen";
        var path = Path.Combine(
            CorpusLocator.RepositoryRoot, "SWLOR.Game.Server", "ConversationData", id + ".conversation.json");
        var model = new NuiConversationEditorViewModel(
            path,
            id,
            SnippetCatalog.Build(),
            null,
            new OutputLogService(),
            new StubPrompts());
        var view = new NuiConversationEditorView { DataContext = model };
        var window = new Window { Content = view, Width = 1100, Height = 760 };
        window.Show();

        try
        {
            window.UpdateLayout();
            var text = view.GetVisualDescendants()
                .OfType<TextBlock>()
                .Select(block => block.Text ?? string.Empty)
                .ToArray();

            text.Should().NotContain(value => value.Contains("legacy", StringComparison.OrdinalIgnoreCase));
            text.Should().NotContain("CONVERSATION COULD NOT BE OPENED");
            view.GetVisualDescendants().OfType<TextBox>().Should().NotBeEmpty(
                "dt_barman_gen must render editable NUI graph fields");
        }
        finally
        {
            window.Close();
            model.OnClose();
        }
    }

    [AvaloniaTest]
    public void NuiPreviewUsesHumanPortraitFallbackWithoutPlaceholderCopy()
    {
        const string id = "cz_receptionist";
        var path = Path.Combine(
            CorpusLocator.RepositoryRoot, "SWLOR.Game.Server", "ConversationData", id + ".conversation.json");
        var model = new NuiConversationEditorViewModel(
            path, id, SnippetCatalog.Build(), null, new OutputLogService(), new StubPrompts());
        var view = new NuiConversationEditorView { DataContext = model };
        var window = new Window { Content = view, Width = 1300, Height = 760 };
        window.Show();

        try
        {
            window.UpdateLayout();
            model.PreviewPortraitSourceResref.Should().Be(NuiConversationEditorViewModel.DefaultPreviewPortraitResref);
            view.GetVisualDescendants().OfType<TextBlock>().Select(block => block.Text ?? string.Empty)
                .Should().NotContain(value =>
                    value.Equals("PORTRAIT", StringComparison.OrdinalIgnoreCase) ||
                    value.Contains("portrait supplied", StringComparison.OrdinalIgnoreCase));
            view.GetVisualDescendants().OfType<Image>().Should().NotBeEmpty();
        }
        finally
        {
            window.Close();
            model.OnClose();
        }
    }

    [AvaloniaTest]
    public void NuiPreviewMatchesTheRuntimeConversationListLayout()
    {
        const string id = "cz_receptionist";
        var path = Path.Combine(
            CorpusLocator.RepositoryRoot, "SWLOR.Game.Server", "ConversationData", id + ".conversation.json");
        var model = new NuiConversationEditorViewModel(
            path, id, SnippetCatalog.Build(), null, new OutputLogService(), new StubPrompts());
        var view = new NuiConversationEditorView { DataContext = model };
        var window = new Window { Content = view, Width = 1300, Height = 760 };
        window.Show();

        try
        {
            window.UpdateLayout();

            var frame = view.FindControl<Border>("PreviewWindowFrame");
            frame.Should().NotBeNull();
            frame!.Width.Should().Be(650);
            frame.Height.Should().Be(520);

            var dialogueScroll = view.FindControl<ScrollViewer>("PreviewDialogueScroll");
            var responseScroll = view.FindControl<ScrollViewer>("PreviewResponsesScroll");
            dialogueScroll.Should().NotBeNull();
            responseScroll.Should().NotBeNull();
            dialogueScroll!.VerticalScrollBarVisibility.Should().Be(ScrollBarVisibility.Visible);
            responseScroll!.VerticalScrollBarVisibility.Should().Be(ScrollBarVisibility.Visible);

            var choiceItems = view.FindControl<ItemsControl>("PreviewChoiceItems");
            choiceItems.Should().NotBeNull();
            var responseButtons = choiceItems!.GetVisualDescendants().OfType<Button>().ToArray();
            responseButtons.Should().NotBeEmpty();
            responseButtons.Should().OnlyContain(button =>
                Math.Abs(button.Bounds.Width - choiceItems.Bounds.Width) <= 2,
                "runtime NUI list cells stretch each response across the available list width");
            responseButtons.Should().OnlyContain(button => Math.Abs(button.Bounds.Height - 38) <= 0.1,
                "runtime NUI response buttons are 38 units high inside 42-unit list rows");

            var dialogueItems = view.FindControl<ItemsControl>("PreviewDialogueItems");
            dialogueItems.Should().NotBeNull();
            dialogueItems!.GetVisualDescendants().OfType<Grid>()
                .Where(grid => grid.DataContext is NuiConversationPreviewTextRow)
                .Should().OnlyContain(grid => Math.Abs(grid.Bounds.Height - 208) <= 0.1,
                    "runtime NUI dialogue segments occupy 208-unit list rows");
        }
        finally
        {
            window.Close();
            model.OnClose();
        }
    }

    [Test]
    public void IssueDocumentAcceptsNoDetailList()
    {
        var model = new ConversationOpenIssueViewModel(
            "missing",
            "missing",
            "Missing conversation",
            "No source exists.",
            "missing.conversation.json");

        model.Details.Should().BeEmpty();
        model.HasDetails.Should().BeFalse();
    }

    [AvaloniaTest]
    public void ErrorDialogAcceptsNoDetailList()
    {
        var dialog = new ErrorDialog("Could not open", "The source is unsupported.", null);

        dialog.FindControl<Border>("DetailsBorder")!.IsVisible.Should().BeFalse(
            "a missing optional detail list must never crash the error path");
    }

    private static bool IsGeneratedShell(string id) =>
        id.StartsWith("dialog", StringComparison.OrdinalIgnoreCase) &&
        int.TryParse(id["dialog".Length..], out _);

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
