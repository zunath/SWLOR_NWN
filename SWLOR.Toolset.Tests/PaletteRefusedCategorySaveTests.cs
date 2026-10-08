using FluentAssertions;
using Nwn.Authoring.Categories;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Shell.Panels;
using SWLOR.Toolset.Tests.Support;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// <see cref="CategoryService"/> answers a refused save by replacing its live tree with a fresh copy of
    /// the persisted one. The palette must then find its selected category in that copy rather than project
    /// the folder object it held before - which once threw out of the refresh.
    /// </summary>
    [TestFixture]
    public sealed class PaletteRefusedCategorySaveTests
    {
        private string _root = string.Empty;
        private string _module = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "swlor-palette-refused-save-" + Guid.NewGuid().ToString("N"));
            _module = Path.Combine(_root, "Module");
            foreach (var folder in new[] { "are", "utc", "utp" })
                Directory.CreateDirectory(Path.Combine(_module, folder));

            // A sidecar from a newer toolset: readable, never written.
            var sidecar = CategoryCatalog.DefaultPathFor(_module);
            Directory.CreateDirectory(Path.GetDirectoryName(sidecar)!);
            File.WriteAllText(sidecar, """
                { "version": 2, "sections": {
                    "utp": { "seeded": true, "folders": [ { "name": "Cargo" } ] }
                } }
                """);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
                ScratchDirectory.Delete(_root);
        }

        [Test]
        public async Task ARefusedRenameRefreshesWithTheRestoredFolderStillSelected()
        {
            var prompts = new QueuedTextPrompts();
            var palette = OpenPalette(prompts);
            var cargo = palette.PresentationState.Rows.Single(row => row.Name == "Cargo");
            palette.PresentationState.SelectedRow = cargo;
            prompts.Answers.Enqueue("Freight");

            await palette.Workflow.RenameCategoryAsync(cargo.Id, CancellationToken.None);

            palette.StatusMessage.Should().NotBeNullOrWhiteSpace("the refusal is reported");
            palette.PresentationState.Rows.Select(row => row.Name).Should().Contain("Cargo").And.NotContain("Freight");
            palette.PresentationState.SelectedRow?.Name.Should().Be("Cargo");
        }

        [Test]
        public async Task ARefusedSubcategoryRefreshesWithTheRestoredParentStillSelected()
        {
            var prompts = new QueuedTextPrompts();
            var palette = OpenPalette(prompts);
            var cargo = palette.PresentationState.Rows.Single(row => row.Name == "Cargo");
            palette.PresentationState.SelectedRow = cargo;
            prompts.Answers.Enqueue("Crates");

            await palette.Workflow.NewCategoryAsync(cargo.Id, CancellationToken.None);

            palette.PresentationState.SelectedRow?.Name.Should().Be("Cargo");
            palette.PresentationState.Rows.Select(row => row.Name).Should().NotContain("Crates");
        }

        private PaletteViewModel OpenPalette(QueuedTextPrompts prompts)
        {
            var log = new OutputLogService();
            var workspace = new WorkspaceContext(root => new ModuleWorkspace(root), log);
            workspace.OpenAndSettle(_module);
            var palette = new PaletteViewModel(workspace, new CategoryService(workspace, log), log, prompts: prompts)
            {
                SelectedType = ResourceType.Utp
            };
            palette.Refresh();
            return palette;
        }
    }
}
