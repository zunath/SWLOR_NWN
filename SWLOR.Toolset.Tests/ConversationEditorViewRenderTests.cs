using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.VisualTree;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Editors;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Proves the conversation editor actually renders, per the rule the script editor's own failure
    /// established: a view that compiles and an app that launches are not evidence that a tab draws.
    /// </summary>
    /// <remarks>
    /// These load the real XAML through the real <see cref="ViewLocator"/> and bind a real view
    /// model, so a missing resource key, a mistyped binding or a view in the wrong namespace fails
    /// here rather than showing a builder an empty panel.
    /// </remarks>
    public class ConversationEditorViewRenderTests
    {
        private static readonly SnippetCatalog Snippets = SnippetCatalog.Build();

        private string _workingCopy = string.Empty;

        [SetUp]
        public void CopyConversation()
        {
            var source = Path.Combine(CorpusLocator.ModuleDirectory, "dlg", "dantherbs.dlg.json");
            _workingCopy = Path.Combine(Path.GetTempPath(), $"swlor-render-{Guid.NewGuid():N}.dlg.json");
            File.Copy(source, _workingCopy);
        }

        [TearDown]
        public void RemoveWorkingCopy()
        {
            if (File.Exists(_workingCopy))
                File.Delete(_workingCopy);
        }

        private ConversationEditorViewModel OpenEditor() =>
            new(_workingCopy, "dantherbs", Snippets, GameCode(), new OutputLogService(), new StubPrompts());

        private static IGameCodeIndex GameCode()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null)
            {
                var candidate = Path.Combine(current.FullName, "SWLOR.Game.Server");
                if (Directory.Exists(Path.Combine(candidate, "Feature", "QuestDefinition")))
                    return new GameCodeIndex(candidate);

                current = current.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate the SWLOR.Game.Server source tree.");
        }

        [AvaloniaTest]
        public void TheLocatorBuildsARealEditorNotAPlaceholder()
        {
            var built = new ViewLocator().Build(OpenEditor());

            built.Should().BeOfType<ConversationEditorView>(
                "a TextBlock here means the convention did not find the view");
        }

        [AvaloniaTest]
        public void TheViewLoadsItsXaml()
        {
            // Construction runs AvaloniaXamlLoader.Load: a malformed binding, a missing resource key
            // or a bad control reference throws here rather than rendering blank in the app.
            var act = () => new ConversationEditorView();

            act.Should().NotThrow();
        }

        [AvaloniaTest]
        public void HeavySurfacesAreDeferredUntilTheyAreUsed()
        {
            var viewModel = OpenEditor();
            var view = new ConversationEditorView();
            var window = new Window { Content = view, Width = 1200, Height = 800 };
            window.Show();

            try
            {
                view.GetVisualDescendants().Should().HaveCountLessThan(120,
                    "opening the shell must not construct the inactive editor trees");

                view.DataContext = viewModel;
                window.UpdateLayout();

                TextOnScreen().Should().NotContain("TEST AS",
                    "the separate Preview surface should stay unrealized while writing");
                TextOnScreen().Should().NotContain("Merchant dialogue",
                    "an inactive behavior should not construct its editor");

                var tabs = view.GetVisualDescendants().OfType<TabControl>().Single();
                tabs.SelectedIndex = 1;
                window.UpdateLayout();
                TextOnScreen().Should().Contain("TEST AS");

                tabs.SelectedIndex = 0;
                viewModel.SelectedBehavior = viewModel.BehaviorOptions.Single(option =>
                    option.Kind == ConversationBehaviorKind.Merchant);
                window.UpdateLayout();
                TextOnScreen().Should().Contain("Merchant dialogue");
            }
            finally
            {
                window.Close();
            }

            List<string> TextOnScreen() => view.GetVisualDescendants()
                .OfType<TextBlock>()
                .Select(block => block.Text ?? string.Empty)
                .ToList();
        }

        [AvaloniaTest]
        public void TheConversationAndItsRailReachTheScreen()
        {
            var viewModel = OpenEditor();
            var view = new ConversationEditorView();
            var window = new Window { Content = view, Width = 1200, Height = 800 };
            window.Show();

            view.DataContext = viewModel;
            window.UpdateLayout();

            var texts = view.GetVisualDescendants()
                .OfType<TextBlock>()
                .Select(block => block.Text ?? string.Empty)
                .ToList();

            // The behavior-tailored outline, and the one NWN ordering rule a writer has to know.
            texts.Should().Contain("Quest moments");
            texts.Should().Contain(text => text.Contains("first match wins"));

            texts.Should().Contain("Write");
            texts.Should().Contain("Preview", "preview is a separate tab rather than a side panel");

            // A real situation, titled from what the player is doing rather than from a condition key.
            texts.Should().Contain(text => text.Contains("Field Tinctures"));
        }

        [AvaloniaTest]
        public void TheLineBeingEditedIsInAnEditableBox()
        {
            var viewModel = OpenEditor();
            var view = new ConversationEditorView();
            var window = new Window { Content = view, Width = 1200, Height = 800 };
            window.Show();

            view.DataContext = viewModel;
            window.UpdateLayout();

            // Reading and writing are the same surface, so the NPC's line has to be in a TextBox
            // rather than a label - that is the whole editing model.
            var boxes = view.GetVisualDescendants().OfType<TextBox>().ToList();

            boxes.Should().Contain(box => box.Text == viewModel.LineText);
        }

        [AvaloniaTest]
        public void TheBehaviorPickerIsWideEnoughForEveryOption()
        {
            var viewModel = OpenEditor();
            var view = new ConversationEditorView();
            var window = new Window { Content = view, Width = 1200, Height = 800 };
            window.Show();

            try
            {
                view.DataContext = viewModel;
                window.UpdateLayout();

                var picker = view.FindControl<ComboBox>("BehaviorSelector");

                picker.Should().NotBeNull();
                picker!.HorizontalAlignment.Should().Be(Avalonia.Layout.HorizontalAlignment.Stretch);
                picker.Bounds.Width.Should().BeGreaterThanOrEqualTo(220,
                    "the popup inherits the picker width and must show 'Conversation' in full");
            }
            finally
            {
                window.Close();
            }
        }

        [AvaloniaTest]
        public void TheFindingsPanelShowsTheDeadOpening()
        {
            var viewModel = OpenEditor();
            var view = new ConversationEditorView();
            var window = new Window { Content = view, Width = 1200, Height = 900 };
            window.Show();

            view.DataContext = viewModel;
            window.UpdateLayout();

            var texts = view.GetVisualDescendants()
                .OfType<TextBlock>()
                .Select(block => block.Text ?? string.Empty)
                .ToList();

            texts.Should().Contain(text => text.Contains("can never happen"),
                "the finding is anchored under the conversation rather than in a panel nobody opens");
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

            public Task<bool> ConfirmDestructiveAsync(string headline, string message, string confirmLabel) =>
                Task.FromResult(false);
        }
    }
}
