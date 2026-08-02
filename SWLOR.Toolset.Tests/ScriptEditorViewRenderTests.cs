using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using AvaloniaEdit;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Editors;
using SWLOR.Toolset.Shell.Panels;
using SWLOR.Toolset.Shell.Views;
using SWLOR.Toolset.Workspace;

[assembly: AvaloniaTestApplication(typeof(SWLOR.Toolset.Tests.HeadlessAppBuilder))]

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Boots the real <see cref="App"/> headlessly, so tests exercise the same styles, themes and
    /// data templates the shipped application uses.
    /// </summary>
    public static class HeadlessAppBuilder
    {
        public static AppBuilder BuildAvaloniaApp() =>
            AppBuilder.Configure<App>().UseHeadless(new AvaloniaHeadlessPlatformOptions());
    }

    /// <summary>
    /// Proves the script editor actually renders.
    /// </summary>
    /// <remarks>
    /// The gap this closes: the script editor shipped showing a "Not Found" placeholder because its
    /// view sat in a namespace <see cref="ViewLocator"/>'s convention does not look in. It compiled,
    /// the app launched, and the launch-and-kill smoke test passed — because that test only proves a
    /// window appeared, never that a given tab renders. These tests resolve the view through the real
    /// locator and load its XAML, which is the part that was never checked.
    /// </remarks>
    public class ScriptEditorViewRenderTests
    {
        private static string SampleScript => Path.Combine(CorpusLocator.ModuleDirectory, "nss", "dmfi_activate.nss");

        [AvaloniaTest]
        public void TheLocatorBuildsARealEditorNotAPlaceholder()
        {
            if (!File.Exists(SampleScript))
                Assert.Ignore("module corpus not present");

            var viewModel = new ScriptEditorViewModel(
                SampleScript, "dmfi_activate", new OutputLogService(), new StubPrompts());

            var built = new ViewLocator().Build(viewModel);

            built.Should().BeOfType<ScriptEditorView>(
                "a TextBlock here means the convention did not find the view");
        }

        [AvaloniaTest]
        public void TheViewLoadsItsXamlAndContainsTheTextEditor()
        {
            var view = new ScriptEditorView();

            // Construction runs AvaloniaXamlLoader.Load: a malformed binding, a missing resource key
            // or a bad control reference throws here rather than rendering blank in the app.
            view.FindControl<TextEditor>("Editor")
                .Should().NotBeNull("the buffer control must exist for the editor to be usable");
        }

        [AvaloniaTest]
        public void SearchPanelIsInstalledAndOpensFromCtrlF()
        {
            var view = new ScriptEditorView();
            var window = new Window { Content = view };
            window.Show();

            SearchPanelInstalled(view).Should().BeTrue();
            SearchPanelOpen(view).Should().BeFalse();

            var editor = view.FindControl<TextEditor>("Editor")!;
            editor.TextArea.Focus();
            window.KeyPress(Key.F, RawInputModifiers.Control, PhysicalKey.F, "f");

            SearchPanelOpen(view).Should().BeTrue("Ctrl+F should open find without relying on menu gestures");
        }

        [AvaloniaTest]
        public void ScriptSearchViewLoadsItsXaml()
        {
            var view = new ScriptSearchView();

            view.FindControl<TextBox>("QueryBox")
                .Should().NotBeNull("the cross-script search panel needs a query box");
            view.FindControl<ListBox>("ResultsList")
                .Should().NotBeNull("the cross-script search panel needs a results list");
        }

        [AvaloniaTest]
        public void ScriptTemplateChoiceDialogLoadsItsXaml()
        {
            var dialog = new ScriptTemplateChoiceDialog();

            dialog.FindControl<ListBox>("TemplateList")
                .Should().NotBeNull("New Script needs a visible template picker");
        }

        [AvaloniaTest]
        public void BindingAScriptPutsItsTextInTheBuffer()
        {
            if (!File.Exists(SampleScript))
                Assert.Ignore("module corpus not present");

            var viewModel = new ScriptEditorViewModel(
                SampleScript, "dmfi_activate", new OutputLogService(), new StubPrompts());

            var view = new ScriptEditorView();
            var window = new Window { Content = view };
            window.Show();

            view.DataContext = viewModel;

            var editor = view.FindControl<TextEditor>("Editor")!;
            editor.Text.Should().Contain("dmw_CleanUp", "the file's contents must reach the buffer");
            editor.Text.Should().NotContain("\r", "the buffer works in \\n; CRLF is reapplied on save");
        }

        [AvaloniaTest]
        public void EditorChromeUsesIconsForOutlineAndContextualScriptSearch()
        {
            if (!File.Exists(SampleScript))
                Assert.Ignore("module corpus not present");

            var viewModel = new ScriptEditorViewModel(
                SampleScript,
                "dmfi_activate",
                new OutputLogService(),
                new StubPrompts(),
                workspaceSearch: new ScriptSearchViewModel(
                    Path.GetDirectoryName(SampleScript)!,
                    (_, _) => { }));

            var view = new ScriptEditorView();
            var window = new Window { Content = view };
            window.Show();
            view.DataContext = viewModel;

            var buttonLabels = view.GetVisualDescendants()
                .OfType<Button>()
                .Select(button => button.Content?.ToString())
                .Where(content => content != null)
                .ToList();

            buttonLabels.Should().NotContain(content =>
                content!.Contains("Lexicon", StringComparison.OrdinalIgnoreCase));
            buttonLabels.Should().NotContain(content =>
                content!.Contains("Minimize", StringComparison.OrdinalIgnoreCase));
            buttonLabels.Should().NotContain(content =>
                content!.Equals("Show", StringComparison.OrdinalIgnoreCase));

            var outline = view.FindControl<ScrollViewer>("OutlineList")!;
            var search = view.FindControl<Border>("WorkspaceSearchPanel")!;
            var outlineToggle = view.FindControl<Button>("OutlineToggleButton")!;
            var searchToggle = view.FindControl<Button>("WorkspaceSearchToggleButton")!;

            outlineToggle.Content.Should().BeOfType<Grid>(
                "outline collapse/expand should be an icon, not a text label");
            searchToggle.Content.Should().BeOfType<Avalonia.Controls.Shapes.Path>(
                "cross-script search should be represented by a search icon");
            outline.IsVisible.Should().BeFalse("the outline should start collapsed");
            search.IsVisible.Should().BeFalse();

            viewModel.ToggleOutlineCommand.Execute(null);
            outline.IsVisible.Should().BeTrue();

            viewModel.ToggleOutlineCommand.Execute(null);
            outline.IsVisible.Should().BeFalse();

            viewModel.ToggleWorkspaceSearchCommand.Execute(null);
            search.IsVisible.Should().BeTrue();
            outline.IsVisible.Should().BeFalse(
                "cross-script results replace the outline within the active script editor");
        }

        [AvaloniaTest]
        public void AFreshlyOpenedTabHasNothingToUndo()
        {
            if (!File.Exists(SampleScript))
                Assert.Ignore("module corpus not present");

            var viewModel = new ScriptEditorViewModel(
                SampleScript, "dmfi_activate", new OutputLogService(), new StubPrompts());

            var view = new ScriptEditorView();
            var window = new Window { Content = view };
            window.Show();
            view.DataContext = viewModel;

            // Seeding the document is not a user edit; leaving it on the stack would let Ctrl+Z on a
            // freshly opened tab wipe the file to empty.
            viewModel.CanUndo.Should().BeFalse();
            viewModel.IsDirty.Should().BeFalse();
        }

        [AvaloniaTest]
        public void EveryDockableViewActuallyConstructs()
        {
            // The locator instantiates these by reflection at runtime, so a XAML fault in any of them
            // is invisible until someone opens that panel.
            foreach (var viewModel in typeof(ViewLocator).Assembly.GetTypes()
                         .Where(t => t is { IsAbstract: false, IsInterface: false })
                         .Where(t => typeof(Dock.Model.Core.IDockable).IsAssignableFrom(t))
                         .Where(t => t.Name.EndsWith("ViewModel", StringComparison.Ordinal)))
            {
                var viewType = ViewLocator.ResolveViewType(viewModel);
                viewType.Should().NotBeNull("{0} has no view", viewModel.FullName);

                var act = () => Activator.CreateInstance(viewType!);
                act.Should().NotThrow("{0} must load its XAML", viewType!.FullName);
            }
        }

        /// <summary>Prompts never fire in these tests; every answer is the non-destructive one.</summary>
        private sealed class StubPrompts : SWLOR.Toolset.Services.IEditorPromptService
        {
            public Task<SWLOR.Toolset.Services.UnsavedChangesChoice> ConfirmCloseAsync(string name) =>
                Task.FromResult(SWLOR.Toolset.Services.UnsavedChangesChoice.Cancel);

            public Task<SWLOR.Toolset.Services.ExternalChangeChoice> ConfirmExternalChangeAsync(string path) =>
                Task.FromResult(SWLOR.Toolset.Services.ExternalChangeChoice.Cancel);

            public Task<string?> PromptForTextAsync(
                string headline, string message, string initialValue, string confirmLabel) =>
                Task.FromResult<string?>(null);

            public Task<string?> PromptForScriptTemplateAsync(IReadOnlyList<ScriptTemplateDefinition> templates) =>
                Task.FromResult<string?>(null);

            public Task<bool> ConfirmDestructiveAsync(string headline, string message, string confirmLabel) =>
                Task.FromResult(false);
        }

        private static bool SearchPanelInstalled(ScriptEditorView view) =>
            (bool)typeof(ScriptEditorView)
                .GetProperty("IsSearchPanelInstalledForTests", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .GetValue(view)!;

        private static bool SearchPanelOpen(ScriptEditorView view) =>
            (bool)typeof(ScriptEditorView)
                .GetProperty("IsSearchPanelOpenForTests", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .GetValue(view)!;
    }
}
