using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Conversations;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Editors;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// An imported or externally edited DLG can carry a link whose Index is outside ReplyList.
    /// Opening the line that owns it must render a visible "broken route" row the builder can
    /// remove - not throw before the tab can show anything.
    /// </summary>
    [TestFixture]
    public class ConversationEditorDanglingLinkTests
    {
        private static readonly SnippetCatalog Snippets = SnippetCatalog.Build();

        private string _root = string.Empty;

        [SetUp]
        public void CreateScratchDialog() =>
            _root = Path.Combine(Path.GetTempPath(), "swlor-dangling-" + Guid.NewGuid().ToString("N"));

        [TearDown]
        public void DeleteScratchDialog()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }

        [Test]
        public void ALinkOutsideTheReplyListShowsAsABrokenRouteInsteadOfThrowing()
        {
            var path = CorruptedDantHerbs();
            var editor = new ConversationEditorViewModel(
                path, "dantherbs", Snippets, null, new OutputLogService(), new StubPrompts());

            // The unguarded opening targets entry 0, whose first reply link was corrupted.
            var anyone = editor.Situations.Single(row => row.Situation.Opening.TargetIndex == 0);
            editor.SelectSituationCommand.Execute(anyone);

            var broken = editor.Choices.Single(choice => choice.IsDangling);
            broken.Text.Should().Contain("no longer exists");
            broken.IsVisible.Should().BeTrue("a broken route must be seen to be repaired, never hidden");
            broken.CanAddFollowUp.Should().BeFalse();
            editor.Choices.Count(choice => !choice.IsDangling)
                .Should().BeGreaterThan(0, "the intact sibling route still renders");

            // Removing the broken route is its repair; afterwards the line shows only real choices.
            editor.RemoveChoiceCommand.Execute(broken);
            editor.Choices.Should().NotContain(choice => choice.IsDangling);
        }

        /// <summary>
        /// A scratch copy of the corpus dantherbs dialog with entry 0's first reply link pointed at
        /// reply #9999, which does not exist - the exact shape an external edit leaves behind.
        /// </summary>
        private string CorruptedDantHerbs()
        {
            Directory.CreateDirectory(_root);
            var source = Path.Combine(CorpusLocator.ModuleDirectory, "dlg", "dantherbs.dlg.json");
            var dialog = JsonNode.Parse(File.ReadAllText(source))!;

            dialog["EntryList"]!["value"]![0]!["RepliesList"]!["value"]![0]!["Index"]!["value"] = 9999;

            var path = Path.Combine(_root, "dantherbs.dlg.json");
            File.WriteAllText(path, dialog.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            return path;
        }

        private sealed class StubPrompts : IEditorPromptService
        {
            public Task<ExternalChangeChoice> ConfirmExternalChangeAsync(string filePath) =>
                Task.FromResult(ExternalChangeChoice.Cancel);

            public Task<UnsavedChangesChoice> ConfirmCloseAsync(string documentTitle) =>
                Task.FromResult(UnsavedChangesChoice.Cancel);

            public Task<bool> ConfirmDestructiveAsync(
                string headline, string message, string confirmLabel) =>
                Task.FromResult(true);

            public Task<string?> PromptForTextAsync(
                string headline, string message, string initialValue, string confirmLabel) =>
                Task.FromResult<string?>(null);
        }
    }
}
