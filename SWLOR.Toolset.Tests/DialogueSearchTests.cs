using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Conversations;
using SWLOR.Toolset.Domain.Documents;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// DialogueSearch adds spoken-text matches to Module Contents' ordinary search by reading every
    /// conversation and reporting which ones mention a query. These check the two ways that scan used
    /// to answer the wrong question: a common word could exhaust its cap on a handful of early,
    /// heavily-matching files and never reach the rest of the alphabetically ordered directory, and
    /// an open, unsaved conversation was always read from its last saved version rather than what the
    /// builder was actually looking at.
    /// </summary>
    [TestFixture]
    public class DialogueSearchTests
    {
        private string _root = string.Empty;
        private string _dlgDirectory = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), $"swlor_dialoguesearch_{Guid.NewGuid():N}");
            _dlgDirectory = Path.Combine(_root, "dlg");
            Directory.CreateDirectory(_dlgDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }

        // ---------- the limit counts conversations, not lines ----------

        [Test]
        public void TheLimitCountsConversationsNotLines()
        {
            // "aaa_multi" matches on two separate lines. If the cap still counted lines, it alone
            // would exhaust a limit of 2 and the scan would never reach the conversations that come
            // after it alphabetically.
            WriteConversation("aaa_multi", "the first line", "the second line");
            WriteConversation("bbb_single", "the third line");
            WriteConversation("ccc_single", "the fourth line");

            var hits = DialogueSearch.Search(_dlgDirectory, "the", limit: 2);

            hits.Select(hit => hit.ResRef).Should().Equal("aaa_multi", "bbb_single");
        }

        [Test]
        public void OnlyTheFirstMatchingLineIsKeptPerConversation()
        {
            // Module Contents collapses hits down to resrefs, so a second line from a conversation
            // already counted adds nothing but detail nobody reads - and detail that would otherwise
            // eat into the limit for no reason.
            WriteConversation("aaa_multi", "the first line", "the second line");

            var hits = DialogueSearch.Search(_dlgDirectory, "the");

            hits.Should().ContainSingle().Which.Text.Should().Be("the first line");
        }

        [Test]
        public void EveryMatchingConversationIsFoundWhenTheLimitIsNotReached()
        {
            WriteConversation("aaa_multi", "the first line", "the second line");
            WriteConversation("bbb_single", "the third line");
            WriteConversation("ccc_single", "the fourth line");

            var hits = DialogueSearch.Search(_dlgDirectory, "the", limit: 300);

            hits.Select(hit => hit.ResRef).Should()
                .BeEquivalentTo("aaa_multi", "bbb_single", "ccc_single");
        }

        // ---------- an open editor overlays the saved file ----------

        [Test]
        public void AnOpenEditorsUnsavedTextOverridesTheSavedFile()
        {
            // The file on disk still says the old line; an open editor's in-memory document already
            // carries the edit. The overlay hook has to be consulted before the file is read, or a
            // builder searching mid-edit gets the stale answer.
            WriteConversation("greeting", "Nothing to see here.");
            var live = DlgDocument.Load(Path.Combine(_dlgDirectory, "greeting.dlg.json"));
            live.Entries[0].Text = "The Veldite seam runs deep.";

            var hits = DialogueSearch.Search(
                _dlgDirectory, "veldite",
                openDocument: resRef => resRef == "greeting" ? live : null);

            hits.Select(hit => hit.ResRef).Should().Equal("greeting");
        }

        [Test]
        public void TheOverlayFallsBackToDiskWhenItReturnsNull()
        {
            WriteConversation("greeting", "The Veldite seam runs deep.");

            var hits = DialogueSearch.Search(
                _dlgDirectory, "veldite",
                openDocument: _ => null);

            hits.Select(hit => hit.ResRef).Should().Equal("greeting");
        }

        [Test]
        public void RemovedTextStopsMatchingOnceTheOverlaySaysSo()
        {
            // Symmetrical case: the saved file still contains the query, but the open editor has
            // already removed it. Trusting disk here is what used to keep a deleted mention matching
            // until the editor was saved.
            WriteConversation("greeting", "The Veldite seam runs deep.");
            var live = DlgDocument.Load(Path.Combine(_dlgDirectory, "greeting.dlg.json"));
            live.Entries[0].Text = "Nothing to see here.";

            var hits = DialogueSearch.Search(
                _dlgDirectory, "veldite",
                openDocument: resRef => resRef == "greeting" ? live : null);

            hits.Should().BeEmpty();
        }

        private void WriteConversation(string resRef, params string[] entryLines)
        {
            var entries = string.Join(",\n", entryLines.Select((line, index) => $$"""
                {
                  "__struct_id": {{index}},
                  "Text": { "type": "cexolocstring", "value": { "0": "{{line}}" } }
                }
                """));

            var json =
                $$"""
                {
                  "__data_type": "DLG ",
                  "EntryList": {
                    "type": "list",
                    "value": [
                      {{entries}}
                    ]
                  },
                  "ReplyList": { "type": "list", "value": [] },
                  "StartingList": { "type": "list", "value": [] }
                }
                """;

            File.WriteAllText(Path.Combine(_dlgDirectory, $"{resRef}.dlg.json"), json);
        }
    }
}
