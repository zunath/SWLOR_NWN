using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Shell.Panels;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>Resource deletion from each of Module Contents' Areas, Dialogs, and Scripts tabs.</summary>
    [TestFixture]
    public sealed class ModuleExplorerDeleteTests
    {
        private string _root = string.Empty;
        private string _module = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), $"swlor_explorer_delete_{Guid.NewGuid():N}");
            _module = Path.Combine(_root, "Module");
            foreach (var folder in new[] { "are", "dlg", "gic", "git", "ifo", "ncs", "nss", "utc" })
                Directory.CreateDirectory(Path.Combine(_module, folder));
            Directory.CreateDirectory(ModuleWorkspace.ResolveConversationDataRoot(_module));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }

        [Test]
        public async Task DeleteScript_RemovesSourceCompiledOutputAndFolderMembership()
        {
            const string resRef = "delete_script";
            var source = Path.Combine(_module, "nss", resRef + ".nss");
            var compiled = Path.Combine(_module, "ncs", resRef + ".ncs");
            File.WriteAllText(source, "void main() {}");
            File.WriteAllBytes(compiled, new byte[] { 1, 2, 3 });

            var prompts = new RecordingPrompts(answer: true);
            var (explorer, categories) = CreateExplorer(ResourceType.Nss, prompts);
            var folder = categories.Section(ResourceType.Nss)!.AddFolder("Utility");
            folder.AddMember(resRef);
            categories.Section(ResourceType.Nss)!.IsSeeded = true;
            categories.SaveChanges().Saved.Should().BeTrue();
            explorer.Refresh();
            explorer.SelectedRow = explorer.Rows.Single(row => row.Folder == folder).Children
                .Single(row => row.ResRef == resRef);

            await explorer.DeleteSelectedResourceCommand.ExecuteAsync(null);

            File.Exists(source).Should().BeFalse();
            File.Exists(compiled).Should().BeFalse("a deleted source must not leave runnable orphan bytecode");
            folder.Members.Should().NotContain(resRef);
            prompts.Message.Should().Contain("delete_script.nss").And.Contain("delete_script.ncs");
            explorer.Rows.SelectMany(row => row.Children).Should().NotContain(row => row.ResRef == resRef);
        }

        [Test]
        public async Task DeleteDialog_RemovesGraphAndLegacyForms()
        {
            const string resRef = "delete_dialog";
            var graph = Path.Combine(ModuleWorkspace.ResolveConversationDataRoot(_module), resRef + ".conversation.json");
            var legacy = Path.Combine(_module, "dlg", resRef + ".dlg.json");
            File.WriteAllText(graph, "{}");
            File.WriteAllText(legacy, "{}");

            var prompts = new RecordingPrompts(answer: true);
            var (explorer, _) = CreateExplorer(ResourceType.Dlg, prompts);
            explorer.SelectedRow = UnsortedResource(explorer, resRef);

            await explorer.DeleteSelectedResourceCommand.ExecuteAsync(null);

            File.Exists(graph).Should().BeFalse();
            File.Exists(legacy).Should().BeFalse(
                "deleting only the graph would make the shadowed legacy dialog reappear in Module Contents");
            prompts.Message.Should().Contain("delete_dialog.conversation.json").And.Contain("delete_dialog.dlg.json");
        }

        [Test]
        public async Task DeleteArea_RemovesTripletAndModuleRegistration()
        {
            const string resRef = "delete_area";
            CopyAreaTemplate(resRef);
            var ifoPath = Path.Combine(_module, "ifo", "module.ifo.json");
            File.Copy(Path.Combine(CorpusLocator.ModuleDirectory, "ifo", "module.ifo.json"), ifoPath);
            var ifo = IfoDocument.Load(ifoPath);
            AreaTemplateFactory.AddAreaToModule(ifo, resRef).Should().BeTrue();
            File.WriteAllBytes(ifoPath, ifo.ToBytes());

            var prompts = new RecordingPrompts(answer: true);
            var (explorer, _) = CreateExplorer(ResourceType.Area, prompts);
            explorer.SelectedRow = UnsortedResource(explorer, resRef);

            await explorer.DeleteSelectedResourceCommand.ExecuteAsync(null);

            File.Exists(Path.Combine(_module, "are", resRef + ".are.json")).Should().BeFalse();
            File.Exists(Path.Combine(_module, "git", resRef + ".git.json")).Should().BeFalse();
            File.Exists(Path.Combine(_module, "gic", resRef + ".gic.json")).Should().BeFalse();
            IfoDocument.Load(ifoPath).AreaResRefs.Should().NotContain(resRef);
            prompts.Message.Should().Contain("delete_area.are.json")
                .And.Contain("delete_area.git.json")
                .And.Contain("delete_area.gic.json")
                .And.Contain("module.ifo.json");
        }

        [Test]
        public async Task DecliningConfirmation_PreservesEveryScriptFile()
        {
            const string resRef = "keep_script";
            var source = Path.Combine(_module, "nss", resRef + ".nss");
            var compiled = Path.Combine(_module, "ncs", resRef + ".ncs");
            File.WriteAllText(source, "void main() {}");
            File.WriteAllBytes(compiled, new byte[] { 1 });

            var (explorer, _) = CreateExplorer(ResourceType.Nss, new RecordingPrompts(answer: false));
            explorer.SelectedRow = UnsortedResource(explorer, resRef);
            await explorer.DeleteSelectedResourceCommand.ExecuteAsync(null);

            File.Exists(source).Should().BeTrue();
            File.Exists(compiled).Should().BeTrue();
        }

        [Test]
        public async Task ModuleLockStartingDuringConfirmation_RefusesDelete()
        {
            const string resRef = "locked_script";
            var source = Path.Combine(_module, "nss", resRef + ".nss");
            File.WriteAllText(source, "void main() {}");
            var mutationLock = new ModuleMutationLock();
            var prompts = new RecordingPrompts(answer: true, onConfirm: () => mutationLock.Set(true));
            var (explorer, _) = CreateExplorer(ResourceType.Nss, prompts, mutationLock);
            explorer.SelectedRow = UnsortedResource(explorer, resRef);

            await explorer.DeleteSelectedResourceCommand.ExecuteAsync(null);

            File.Exists(source).Should().BeTrue();
            explorer.StatusMessage.Should().Contain("packed, validated, or built");
        }

        [Test]
        public async Task FileChangedDuringConfirmation_PreservesTheNewGeneration()
        {
            const string resRef = "changed_script";
            var source = Path.Combine(_module, "nss", resRef + ".nss");
            File.WriteAllText(source, "void main() { // old\n}");
            var prompts = new RecordingPrompts(
                answer: true,
                onConfirm: () => File.WriteAllText(source, "void main() { // new\n}"));
            var (explorer, _) = CreateExplorer(ResourceType.Nss, prompts);
            explorer.SelectedRow = UnsortedResource(explorer, resRef);

            await explorer.DeleteSelectedResourceCommand.ExecuteAsync(null);

            File.ReadAllText(source).Should().Contain("// new");
            explorer.StatusMessage.Should().Contain("changed while the delete confirmation was open");
        }

        private (ModuleExplorerViewModel Explorer, CategoryService Categories) CreateExplorer(
            ResourceType type,
            IEditorPromptService prompts,
            ModuleMutationLock? mutationLock = null)
        {
            var log = new OutputLogService();
            var workspace = new WorkspaceContext(root => new ModuleWorkspace(root), log);
            workspace.Open(_module);
            var categories = new CategoryService(workspace, log);
            categories.Section(type)!.IsSeeded = true;
            categories.SaveChanges().Saved.Should().BeTrue();
            var explorer = new ModuleExplorerViewModel(
                workspace,
                new PropertiesViewModel(workspace, log),
                categories,
                log,
                prompts: prompts,
                mutationLock: mutationLock)
            {
                SelectedType = type
            };
            explorer.Initialize();
            return (explorer, categories);
        }

        private static ExplorerNodeViewModel UnsortedResource(ModuleExplorerViewModel explorer, string resRef) =>
            explorer.Rows.Single(row => row.Name == "Unsorted").Children
                .Single(row => row.ResRef == resRef);

        private void CopyAreaTemplate(string targetResRef)
        {
            foreach (var (folder, extension) in new[]
                     {
                         ("are", "are.json"),
                         ("git", "git.json"),
                         ("gic", "gic.json")
                     })
            {
                File.Copy(
                    Path.Combine(CorpusLocator.ModuleDirectory, folder, "area_template." + extension),
                    Path.Combine(_module, folder, targetResRef + "." + extension));
            }
        }

        private sealed class RecordingPrompts(
            bool answer,
            Action? onConfirm = null) : IEditorPromptService
        {
            public string Message { get; private set; } = string.Empty;

            public Task<ExternalChangeChoice> ConfirmExternalChangeAsync(string filePath) =>
                Task.FromResult(ExternalChangeChoice.Cancel);

            public Task<UnsavedChangesChoice> ConfirmCloseAsync(string documentTitle) =>
                Task.FromResult(UnsavedChangesChoice.Cancel);

            public Task<bool> ConfirmDestructiveAsync(string headline, string message, string confirmLabel)
            {
                Message = message;
                onConfirm?.Invoke();
                return Task.FromResult(answer);
            }

            public Task<string?> PromptForTextAsync(
                string headline,
                string message,
                string initialValue,
                string confirmLabel) => Task.FromResult<string?>(null);
        }
    }
}
