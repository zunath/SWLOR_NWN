using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.VisualTree;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Toolset.Domain.Conversations;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Editors;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Shell.Views;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests;

/// <summary>
/// Opening a conversation is a corpus-level contract: every Explorer row either gets the NUI
/// editor, the temporary legacy editor, or a visible explanation of the exact legacy exception.
/// None may disappear into a log or a broken modal.
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
        routes.Count(route => route.Kind == ConversationEditorRouteKind.NuiGraph).Should().Be(320);
        routes.Count(route => route.Kind == ConversationEditorRouteKind.LegacyDialog).Should().Be(17);
        routes.Count(route => route.Kind == ConversationEditorRouteKind.LegacyException).Should().Be(9);
        routes.Where(route => route.Kind == ConversationEditorRouteKind.LegacyException)
            .Should().OnlyContain(route =>
                !string.IsNullOrWhiteSpace(route.Reason) && route.Details.Count > 0,
                "a legacy exception must open a useful explanation, not a blank tab");
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

    [AvaloniaTest]
    public void LegacyExceptionRendersAsAReadableDocument()
    {
        var model = new ConversationOpenIssueViewModel(
            "zomb_telconv",
            "zomb_telconv",
            "'zomb_telconv' is a legacy NWN exception",
            "This conversation decides what to show with its own script.",
            @"Module\dlg\zomb_telconv.dlg.json",
            new[] { "opening: Uses custom condition script 'can_accept_1'." });
        var view = new ConversationOpenIssueView { DataContext = model };
        var window = new Window { Content = view, Width = 1100, Height = 760 };
        window.Show();

        try
        {
            window.UpdateLayout();
            var text = view.GetVisualDescendants()
                .OfType<TextBlock>()
                .Select(block => block.Text ?? string.Empty)
                .ToArray();

            text.Should().Contain("CONVERSATION COULD NOT BE EDITED");
            text.Should().Contain("'zomb_telconv' is a legacy NWN exception");
            text.Should().Contain(item => item.Contains("can_accept_1", StringComparison.Ordinal));
            view.GetVisualDescendants().OfType<ScrollViewer>().Should().ContainSingle(
                "the exception document should have one predictable scrollbar");
        }
        finally
        {
            window.Close();
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
