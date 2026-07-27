using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Conversations;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Shell.Panels;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// The Module Contents filter box, and specifically the part of it that reads the conversation
    /// corpus.
    /// </summary>
    /// <remarks>
    /// "Search what people say" opens every dialog file in the module — 609 of them, about a second.
    /// It used to do that inline from the property-changed handler, so each character typed froze the
    /// window for a full scan and all but the last one were for a prefix nobody wanted results for.
    /// What these check is that a keystroke no longer reads anything, and that a scan can be
    /// abandoned when the query it was for is gone.
    /// </remarks>
    [TestFixture]
    public class ModuleExplorerSearchTests
    {
        private string _root = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), $"swlor_explorer_{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path.Combine(_root, "dlg"));
            // The two folders ModuleWorkspace looks for before it accepts a root.
            Directory.CreateDirectory(Path.Combine(_root, "are"));
            Directory.CreateDirectory(Path.Combine(_root, "utc"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }

        /// <summary>
        /// The scan is the expensive thing, so the test measures whether it happened rather than how
        /// long anything took: a synchronous scan would have to open the file, and an unreadable file
        /// on a background thread cannot fail the assertion.
        /// </summary>
        [Test]
        public void TypingIntoTheFilterDoesNotReadTheConversationCorpus()
        {
            var path = Path.Combine(_root, "dlg", "greeting.dlg.json");
            File.WriteAllText(path, "{}");

            var log = new OutputLogService();
            var workspace = new WorkspaceContext(root => new ModuleWorkspace(root), log);
            workspace.Open(_root);

            var explorer = new ModuleExplorerViewModel(
                workspace,
                new PropertiesViewModel(workspace, log),
                new CategoryService(workspace, log),
                log)
            {
                SelectedType = ResourceType.Dlg,
                SearchDialogueText = true
            };

            // Held open for writing with no sharing: anything that reads it on this thread throws.
            using var exclusive = new FileStream(
                path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            var typing = () =>
            {
                foreach (var prefix in new[] { "v", "ve", "vel", "veld" })
                    explorer.Filter = prefix;
            };

            typing.Should().NotThrow("the keystroke path must not open a single conversation");
        }

        /// <summary>
        /// A scan for an abandoned query has to be droppable. Without this the panel pays for every
        /// prefix of a word in full even though only the last one is wanted.
        /// </summary>
        [Test]
        public void AnAbandonedDialogueScanStopsEarly()
        {
            for (var i = 0; i < 40; i++)
                WriteConversation($"chat{i:00}", "Nothing to see here.");

            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();

            var search = () => DialogueSearch.Search(
                Path.Combine(_root, "dlg"), "see", cancellationToken: cancelled.Token);

            search.Should().Throw<OperationCanceledException>();
        }

        [Test]
        public void ADialogueScanStillFindsWhatPeopleSay()
        {
            WriteConversation("mining", "The Veldite seam runs deep.");
            WriteConversation("weather", "Looks like rain.");

            var hits = DialogueSearch.Search(Path.Combine(_root, "dlg"), "veldite");

            hits.Select(hit => hit.ResRef).Should().ContainSingle().Which.Should().Be("mining");
        }

        private void WriteConversation(string resRef, string line)
        {
            var json =
                $$"""
                {
                  "__data_type": "DLG ",
                  "EntryList": {
                    "type": "list",
                    "value": [
                      {
                        "__struct_id": 0,
                        "Text": { "type": "cexolocstring", "value": { "0": "{{line}}" } }
                      }
                    ]
                  },
                  "ReplyList": { "type": "list", "value": [] },
                  "StartingList": { "type": "list", "value": [] }
                }
                """;

            File.WriteAllText(Path.Combine(_root, "dlg", $"{resRef}.dlg.json"), json);
        }
    }
}
