using Avalonia.Controls;
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
/// legacy editor. Legacy dialog-context scripts may limit Preview, but never block authoring.
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
        routes.Count(route => route.Kind == ConversationEditorRouteKind.NuiGraph).Should().Be(320);
        routes.Count(route => route.Kind == ConversationEditorRouteKind.LegacyDialog).Should().Be(17);
        routes.Count(route => route.Kind == ConversationEditorRouteKind.LegacyException).Should().Be(9);
        routes.Where(route => route.Kind == ConversationEditorRouteKind.LegacyException)
            .Should().OnlyContain(route =>
                !string.IsNullOrWhiteSpace(route.Reason) && route.Details.Count > 0,
                "legacy-script conversations need an honest preview warning with useful details");
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
                var route = ConversationEditorRoute.Resolve(
                    id,
                    Path.Combine(graphDirectory, id + ".conversation.json"),
                    path);
                var editor = new ConversationEditorViewModel(
                    path,
                    id,
                    snippets,
                    null,
                    new OutputLogService(),
                    new StubPrompts(),
                    legacyPreviewNotice: route.Kind == ConversationEditorRouteKind.LegacyException
                        ? "Legacy NWScript conditions are preserved."
                        : null);

                editor.LiveDialog.Should().NotBeNull(id);
                editor.OnClose();
            }
            catch (Exception ex)
            {
                failures.Add($"{id}: {ex}");
            }
        }

        failures.Should().BeEmpty(
            "legacy NWScript affects preview fidelity, not whether the conversation can be edited");
    }

    [Test]
    public async Task EditingLegacyConversationPreservesScriptsAndCustomTokens()
    {
        const string id = "dt_barman_gen";
        var source = Path.Combine(CorpusLocator.ModuleDirectory, "dlg", id + ".dlg.json");
        var workingCopy = Path.Combine(Path.GetTempPath(), $"{id}-{Guid.NewGuid():N}.dlg.json");
        File.Copy(source, workingCopy);

        try
        {
            var before = DlgDocument.Load(workingCopy);
            var conditionScripts = before.AllLinks()
                .Select(link => link.Active)
                .Where(script => !string.IsNullOrWhiteSpace(script))
                .OrderBy(script => script, StringComparer.Ordinal)
                .ToArray();
            var actionScripts = before.Entries.Concat(before.Replies)
                .Select(node => node.Script)
                .Where(script => !string.IsNullOrWhiteSpace(script))
                .OrderBy(script => script, StringComparer.Ordinal)
                .ToArray();
            var customTokens = System.Text.RegularExpressions.Regex
                .Matches(File.ReadAllText(workingCopy), @"<CUSTOM\d+>")
                .Select(match => match.Value)
                .OrderBy(token => token, StringComparer.Ordinal)
                .ToArray();

            var editor = new ConversationEditorViewModel(
                workingCopy,
                id,
                SnippetCatalog.Build(),
                null,
                new OutputLogService(),
                new StubPrompts(),
                legacyPreviewNotice: "Legacy NWScript conditions are preserved.");
            editor.LineText += " ";

            (await editor.TrySaveAsync()).Should().BeTrue();
            editor.OnClose();

            var after = DlgDocument.Load(workingCopy);
            after.AllLinks()
                .Select(link => link.Active)
                .Where(script => !string.IsNullOrWhiteSpace(script))
                .OrderBy(script => script, StringComparer.Ordinal)
                .Should().Equal(conditionScripts);
            after.Entries.Concat(after.Replies)
                .Select(node => node.Script)
                .Where(script => !string.IsNullOrWhiteSpace(script))
                .OrderBy(script => script, StringComparer.Ordinal)
                .Should().Equal(actionScripts);
            System.Text.RegularExpressions.Regex
                .Matches(File.ReadAllText(workingCopy), @"<CUSTOM\d+>")
                .Select(match => match.Value)
                .OrderBy(token => token, StringComparer.Ordinal)
                .Should().Equal(customTokens);
        }
        finally
        {
            File.Delete(workingCopy);
        }
    }

    [AvaloniaTest]
    public void LegacyScriptConversationRendersTheRealEditor()
    {
        const string id = "dt_barman_gen";
        var path = Path.Combine(CorpusLocator.ModuleDirectory, "dlg", id + ".dlg.json");
        var model = new ConversationEditorViewModel(
            path,
            id,
            SnippetCatalog.Build(),
            null,
            new OutputLogService(),
            new StubPrompts(),
            legacyPreviewNotice:
                "This conversation uses legacy NWScript conditions. They stay attached and are saved unchanged.");
        var view = new ConversationEditorView { DataContext = model };
        var window = new Window { Content = view, Width = 1100, Height = 760 };
        window.Show();

        try
        {
            window.UpdateLayout();
            var text = view.GetVisualDescendants()
                .OfType<TextBlock>()
                .Select(block => block.Text ?? string.Empty)
                .ToArray();

            text.Should().Contain("LEGACY PREVIEW");
            text.Should().NotContain("CONVERSATION COULD NOT BE OPENED");
            view.GetVisualDescendants().OfType<TextBox>().Should().NotBeEmpty(
                "dt_barman_gen must render editable conversation fields, not a refusal page");
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
