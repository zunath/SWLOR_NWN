using Avalonia.Headless.NUnit;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Categories;
using SWLOR.Toolset.Domain.Conversations;
using SWLOR.Toolset.Domain.Documents;
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
    /// Dialogue-aware filtering opens every dialog file in the module — 609 of them, about a second.
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
                SelectedType = ResourceType.Dlg
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

        [AvaloniaTest]
        public async Task DialogueFilteringCombinesResRefsWithWhatPeopleSay()
        {
            WriteConversation("veldite_terminal", "Nothing unusual here.");
            WriteConversation("mining", "The Veldite seam runs deep.");
            var log = new OutputLogService();
            var workspace = new WorkspaceContext(root => new ModuleWorkspace(root), log);
            workspace.Open(_root);
            var explorer = new ModuleExplorerViewModel(
                workspace,
                new PropertiesViewModel(workspace, log),
                new CategoryService(workspace, log),
                log)
            {
                SelectedType = ResourceType.Dlg
            };
            explorer.Initialize();

            explorer.Filter = "veldite";

            ContainsResRef(explorer.Rows, "veldite_terminal").Should().BeTrue(
                "name and ResRef matches should appear without waiting for the dialogue scan");
            await WaitUntilAsync(() => !explorer.IsSearchingDialogue);
            ContainsResRef(explorer.Rows, "veldite_terminal").Should().BeTrue();
            ContainsResRef(explorer.Rows, "mining").Should().BeTrue(
                "spoken-text matches should join the ordinary search results");
        }

        [AvaloniaTest]
        public void GeneratedDialogueShellsNeverAppearOrCountAsAuthoringContent()
        {
            WriteConversation("dialog1", "Generated runtime shell.");
            WriteConversation("ordinary", "Hand-authored dialogue.");
            var log = new OutputLogService();
            var workspace = new WorkspaceContext(root => new ModuleWorkspace(root), log);
            workspace.Open(_root);
            var explorer = new ModuleExplorerViewModel(
                workspace,
                new PropertiesViewModel(workspace, log),
                new CategoryService(workspace, log),
                log)
            {
                SelectedType = ResourceType.Dlg
            };
            explorer.Initialize();

            ContainsResRef(explorer.Rows, "ordinary").Should().BeTrue();
            ContainsResRef(explorer.Rows, "dialog1").Should().BeFalse();
            explorer.Tabs.Single(tab => tab.Type == ResourceType.Dlg).Count.Should().Be(1);
        }

        [Test]
        public void AreaSearchHidesCategoriesWithoutMatchesAndRestoresThemWhenCleared()
        {
            File.WriteAllText(Path.Combine(_root, "are", "nanostation015.are.json"), "{}");
            File.WriteAllText(Path.Combine(_root, "are", "tatooine001.are.json"), "{}");

            var log = new OutputLogService();
            var workspace = new WorkspaceContext(root => new ModuleWorkspace(root), log);
            workspace.Open(_root);
            var categories = new CategoryService(workspace, log);
            var section = categories.Section(ResourceType.Area)!;
            section.IsSeeded = true;
            var stations = section.AddFolder("Stations");
            stations.AddChild("Nanostation").AddMember("nanostation015");
            stations.AddChild("Other Stations").AddMember("tatooine001");
            section.AddFolder("Planets").AddMember("tatooine001");
            section.AddFolder("Empty Category");

            var explorer = new ModuleExplorerViewModel(
                workspace,
                new PropertiesViewModel(workspace, log),
                categories,
                log);
            explorer.Initialize();

            var stationsRow = explorer.Rows.Single(row => row.Folder == stations);
            explorer.ToggleCommand.Execute(stationsRow);
            explorer.Rows.Should().Contain(row => row.Name == "Other Stations");
            explorer.Rows.Should().Contain(row => row.Name == "Planets");
            explorer.Rows.Should().Contain(row => row.Name == "Empty Category");
            explorer.Rows.Should().Contain(row => row.Name == CategorySection.UnsortedFolderName);

            explorer.Filter = "nanostation015";

            explorer.Rows.Should().Contain(row => row.Name == "Stations");
            explorer.Rows.Should().Contain(row => row.Name == "Nanostation");
            explorer.Rows.Should().NotContain(row => row.Name == "Other Stations");
            explorer.Rows.Should().NotContain(row => row.Name == "Planets");
            explorer.Rows.Should().NotContain(row => row.Name == "Empty Category");
            explorer.Rows.Should().NotContain(row => row.Name == CategorySection.UnsortedFolderName);

            explorer.Filter = string.Empty;

            explorer.Rows.Should().Contain(row => row.Name == "Other Stations");
            explorer.Rows.Should().Contain(row => row.Name == "Planets");
            explorer.Rows.Should().Contain(row => row.Name == "Empty Category");
            explorer.Rows.Should().Contain(row => row.Name == CategorySection.UnsortedFolderName);
        }

        /// <summary>
        /// The declared debounce has to actually be awaited before the corpus is read, not just sit
        /// there unused while the scan starts immediately. A single small conversation scans in a few
        /// milliseconds, so timing the gap between the keystroke and the scan settling is enough to
        /// tell "waited out the debounce" apart from "started reading right away".
        /// </summary>
        [AvaloniaTest]
        public async Task DialogueScanWaitsOutTheDebounceBeforeReadingTheCorpus()
        {
            WriteConversation("mining", "The Veldite seam runs deep.");
            var log = new OutputLogService();
            var workspace = new WorkspaceContext(root => new ModuleWorkspace(root), log);
            workspace.Open(_root);
            var explorer = new ModuleExplorerViewModel(
                workspace,
                new PropertiesViewModel(workspace, log),
                new CategoryService(workspace, log),
                log)
            {
                SelectedType = ResourceType.Dlg
            };
            explorer.Initialize();

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            explorer.Filter = "veldite";

            await WaitUntilAsync(() => !explorer.IsSearchingDialogue);
            stopwatch.Stop();

            stopwatch.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(200,
                "the declared debounce must be awaited before the scan runs, not skipped");
        }

        [Test]
        public void OpenConversationSnapshotsAreCapturedBeforeTheSearchWorkerStarts()
        {
            var source = File.ReadAllText(Path.Combine(
                FindRepositoryRoot().FullName,
                "SWLOR.Toolset",
                "Shell",
                "Panels",
                "ModuleExplorerViewModel.cs"));
            var openDialogsIndex = source.IndexOf(
                "SnapshotOpenConversationDocuments();", StringComparison.Ordinal);
            var openGraphsIndex = source.IndexOf(
                "SnapshotOpenNuiConversationGraphs();", StringComparison.Ordinal);
            var workerIndex = source.IndexOf("_ = Task.Run(", StringComparison.Ordinal);

            openDialogsIndex.Should().BeGreaterThanOrEqualTo(0);
            openGraphsIndex.Should().BeGreaterThanOrEqualTo(0);
            workerIndex.Should().BeGreaterThan(openDialogsIndex);
            workerIndex.Should().BeGreaterThan(openGraphsIndex,
                "the UI-owned graph editors must be snapshotted before background work begins");
        }

        [AvaloniaTest]
        public async Task SavingAConversationInvalidatesHitsForTheSameQuery()
        {
            WriteConversation("greeting", "Nothing to see here.");
            var log = new OutputLogService();
            var workspace = new WorkspaceContext(root => new ModuleWorkspace(root), log);
            workspace.Open(_root);
            var explorer = new ModuleExplorerViewModel(
                workspace,
                new PropertiesViewModel(workspace, log),
                new CategoryService(workspace, log),
                log)
            {
                SelectedType = ResourceType.Dlg
            };
            explorer.Initialize();
            explorer.Filter = "veldite";

            await WaitUntilAsync(() => !explorer.IsSearchingDialogue);
            ContainsResRef(explorer.Rows, "greeting").Should().BeFalse();

            WriteConversation("greeting", "The Veldite seam runs deep.");
            workspace.RefreshCatalogEntry(ResourceType.Dlg, "greeting");

            explorer.IsSearchingDialogue.Should().BeTrue(
                "a DLG refresh must invalidate and requeue the cached same-query scan");
            await WaitUntilAsync(() => !explorer.IsSearchingDialogue);
            ContainsResRef(explorer.Rows, "greeting").Should().BeTrue();
        }

        // ---------- an open conversation is deep-snapshotted, not passed to the worker live ----------

        /// <summary>
        /// <c>EditorService.SnapshotOpenConversationDocuments</c> hands the background scan a private
        /// copy of each open conversation - round-tripped through <c>ToBytes</c>/<c>Parse</c> on the
        /// UI thread - rather than the editor's live <see cref="DlgDocument"/>. This is the technique
        /// itself: a builder adding or editing lines on the live document after the snapshot was taken
        /// (the same moment a background scan could be mid-traversal of it) must not be visible
        /// through the copy, or the worker would be walking node/link lists out from under a
        /// concurrent mutation - the exact fault the review comment called out.
        /// </summary>
        [Test]
        public void ADeepSnapshotIsIndependentOfTheLiveDocumentItWasTakenFrom()
        {
            WriteConversation("greeting", "Nothing to see here.");
            var live = DlgDocument.Load(Path.Combine(_root, "dlg", "greeting.dlg.json"));

            var snapshot = DlgDocument.Parse(live.ToBytes());

            // The kind of edit a builder makes while a scan could be reading the same document.
            live.Entries[0].Text = "The Veldite seam runs deep.";
            live.AddEntry("A brand new line.");

            snapshot.Entries.Should().ContainSingle().Which.Text.Should().Be("Nothing to see here.");
        }

        /// <summary>
        /// The round-tripped copy has to remain a faithful stand-in for the live document, not just an
        /// inert one: dialogue search reads whatever text was live at snapshot time exactly as it
        /// would have read the live document itself, before the divergence above happens.
        /// </summary>
        [Test]
        public void ADeepSnapshotStillMatchesDialogueSearchLikeTheLiveDocumentWould()
        {
            WriteConversation("greeting", "Nothing to see here.");
            var live = DlgDocument.Load(Path.Combine(_root, "dlg", "greeting.dlg.json"));
            live.Entries[0].Text = "The Veldite seam runs deep.";

            var snapshot = DlgDocument.Parse(live.ToBytes());

            var hits = DialogueSearch.Search(
                Path.Combine(_root, "dlg"), "veldite",
                openDocument: resRef => resRef == "greeting" ? snapshot : null);

            hits.Select(hit => hit.ResRef).Should().Equal("greeting");
        }

        private static bool ContainsResRef(
            IEnumerable<ExplorerNodeViewModel> rows,
            string resRef)
        {
            return rows.Any(row =>
                (row.Item != null && row.Item.ResRef == resRef) ||
                ContainsResRef(row.Children, resRef));
        }

        private static DirectoryInfo FindRepositoryRoot()
        {
            DirectoryInfo? directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null &&
                   !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                directory = directory.Parent;

            directory.Should().NotBeNull("the tests should run inside the repository checkout");
            return directory!;
        }

        private static async Task WaitUntilAsync(Func<bool> condition)
        {
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (!condition())
            {
                if (DateTime.UtcNow >= deadline)
                    Assert.Fail("Timed out waiting for the dialogue search to settle.");

                await Task.Delay(25);
            }
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
