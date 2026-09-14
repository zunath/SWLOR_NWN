using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Conversations;
using SWLOR.Toolset.Domain.Documents;
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

            // The "Hold a moment..." offer targets the corrupted entry and stays reachable
            // regardless of dantherbs' own guard content - see CorruptedDantHerbs for why the
            // greeting this used to target no longer works for that.
            var anyone = editor.Situations.Single(row =>
                row.Situation.Opening.Target.Text.StartsWith("Hold a moment."));
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
        /// A scratch copy of the corpus dantherbs dialog with the "Hold a moment..." offer's entry's
        /// first reply link pointed at reply #9999, which does not exist - the exact shape an
        /// external edit leaves behind.
        /// </summary>
        /// <remarks>
        /// This used to corrupt entry 0, the plain greeting's entry, because that opening was
        /// unguarded and therefore always reachable. It no longer is: dantherbs' Field Tinctures
        /// offer carries a duplicate guard key that
        /// <see cref="ReachabilityTests.ADuplicateGuardKeyOnlyEvaluatesItsNegatedForm"/> made
        /// ReachabilityEvaluator stop over-enforcing, and the corrected offer now swallows the
        /// greeting along with every harvest_herbs-only opening beneath it. Selecting a now-dead
        /// situation renders no choices at all, so the corruption has to sit under something that is
        /// still reachable - the offer itself, which every real player is guaranteed to see.
        /// </remarks>
        private string CorruptedDantHerbs()
        {
            Directory.CreateDirectory(_root);
            var source = Path.Combine(CorpusLocator.ModuleDirectory, "dlg", "dantherbs.dlg.json");

            var entryIndex = DlgDocument.Load(source).Openings
                .Single(opening => opening.Target.Text.StartsWith("Hold a moment."))
                .TargetIndex;

            var dialog = JsonNode.Parse(File.ReadAllText(source))!;
            dialog["EntryList"]!["value"]![entryIndex]!["RepliesList"]!["value"]![0]!["Index"]!["value"] = 9999;

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
