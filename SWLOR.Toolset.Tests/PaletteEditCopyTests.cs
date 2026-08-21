using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Shell.Panels;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    public sealed class PaletteEditCopyTests
    {
        private string _testRoot = string.Empty;
        private string _moduleRoot = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _testRoot = Path.Combine(
                Path.GetTempPath(),
                "swlor-palette-edit-copy-" + Guid.NewGuid().ToString("N"));
            _moduleRoot = Path.Combine(_testRoot, "Module");
            foreach (var folder in new[] { "are", "utc", "utp" })
                Directory.CreateDirectory(Path.Combine(_moduleRoot, folder));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_testRoot))
                Directory.Delete(_testRoot, recursive: true);
        }

        [Test]
        public void EditCopyCreatesFilesRevealsAndFilesAnIndependentBlueprint()
        {
            const string sourceResRef = "test_crate";
            const string copyResRef = "test_crate001";
            var sourcePath = Path.Combine(_moduleRoot, "utp", sourceResRef + ".utp.json");
            File.WriteAllBytes(
                sourcePath,
                BlueprintTemplateFactory.CreateFileContent(
                    ResourceType.Utp,
                    sourceResRef,
                    "Test Crate"));

            var log = new OutputLogService();
            var workspace = new WorkspaceContext(root => new ModuleWorkspace(root), log);
            workspace.Open(_moduleRoot);
            var categories = new CategoryService(workspace, log);
            var furniture = categories.Section(ResourceType.Utp)!.AddFolder("Furniture");
            var containers = furniture.AddChild("Containers");
            containers.AddMember(sourceResRef);
            categories.SaveChanges().Saved.Should().BeTrue();

            var mutationLock = new ModuleMutationLock();
            var previousAmbient = ModuleMutationLock.ModuleWrites;
            ModuleMutationLock.ModuleWrites = mutationLock;
            try
            {
                var palette = new PaletteViewModel(
                    workspace,
                    categories,
                    log,
                    mutationLock: mutationLock)
                {
                    SelectedType = ResourceType.Utp
                };
                palette.Refresh();
                palette.SelectedRow = palette.Rows.Single(row => row.Name == "Furniture");
                var tile = new PaletteTileViewModel(
                    sourceResRef,
                    "Test Crate",
                    categoryPath: null,
                    PaletteSource.Custom);

                palette.EditCopyCommand.Execute(tile);

                var copyPath = Path.Combine(_moduleRoot, "utp", copyResRef + ".utp.json");
                File.Exists(copyPath).Should().BeTrue();
                File.Exists(sourcePath).Should().BeTrue();
                var copy = JsonGffDocument.Load(copyPath);
                copy.Root.GetStringOrNull("TemplateResRef").Should().Be(copyResRef);
                copy.Root.GetStringOrNull("Tag").Should().Be(sourceResRef,
                    "Edit Copy preserves authored fields other than blueprint identity");
                categories.Section(ResourceType.Utp)!
                    .Find("Furniture", "Containers")!
                    .Members.Should().Contain(copyResRef);
                palette.Source.Should().Be(PaletteSource.Custom);
                palette.SelectedTile?.ResRef.Should().Be(copyResRef);
                palette.StatusMessage.Should().Contain($"Copied Test Crate as {copyResRef}");
            }
            finally
            {
                ModuleMutationLock.ModuleWrites = previousAmbient;
            }
        }

        [Test]
        public void EditCopyAvailabilityIncludesStandardButFollowsTheModuleLock()
        {
            var log = new OutputLogService();
            var workspace = new WorkspaceContext(root => new ModuleWorkspace(root), log);
            workspace.Open(_moduleRoot);
            var mutationLock = new ModuleMutationLock();
            var palette = new PaletteViewModel(
                workspace,
                new CategoryService(workspace, log),
                log,
                mutationLock: mutationLock)
            {
                Source = PaletteSource.Standard
            };

            palette.CanWrite.Should().BeFalse("Standard blueprints cannot be edited in place");
            palette.CanEditCopy.Should().BeTrue("Edit Copy writes a new Custom blueprint");
            palette.HasBlueprintActions.Should().BeTrue();

            mutationLock.Set(true);

            palette.CanEditCopy.Should().BeFalse();
        }
    }
}
