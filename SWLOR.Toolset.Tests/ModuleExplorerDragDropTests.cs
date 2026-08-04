using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Categories;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Shell.Panels;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>Folder drag/drop in each of Module Contents' three trees.</summary>
    [TestFixture]
    public sealed class ModuleExplorerDragDropTests
    {
        private string _root = string.Empty;
        private string _module = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), $"swlor_explorer_drag_{Guid.NewGuid():N}");
            _module = Path.Combine(_root, "Module");
            foreach (var folder in new[] { "are", "dlg", "nss", "utc" })
                Directory.CreateDirectory(Path.Combine(_module, folder));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }

        [TestCase(ResourceType.Area, "are.json")]
        [TestCase(ResourceType.Dlg, "dlg.json")]
        [TestCase(ResourceType.Nss, "nss")]
        public void AResourceCanBeDraggedIntoAndBackOutOfFolders(ResourceType type, string suffix)
        {
            File.WriteAllText(Path.Combine(_module, type.Extension(), $"resource_one.{suffix}"), "{}");

            var log = new OutputLogService();
            var workspace = new WorkspaceContext(root => new ModuleWorkspace(root), log);
            workspace.Open(_module);
            var categories = new CategoryService(workspace, log);
            var section = categories.Section(type)!;
            section.IsSeeded = true;
            var first = section.AddFolder("First");
            var second = section.AddFolder("Second");
            first.AddMember("resource_one");
            categories.SaveChanges().Saved.Should().BeTrue();

            var explorer = new ModuleExplorerViewModel(
                workspace,
                new PropertiesViewModel(workspace, log),
                categories,
                log)
            {
                SelectedType = type
            };
            explorer.Initialize();

            var source = explorer.Rows.Single(row => row.Folder == first).Children
                .Single(row => row.ResRef == "resource_one");
            var target = explorer.Rows.Single(row => row.Folder == second);

            explorer.CanDropResource(source, target).Should().BeTrue();
            explorer.DropResource(source, target).Should().BeTrue();
            first.Members.Should().NotContain("resource_one");
            second.Members.Should().Contain("resource_one");

            explorer.UndoResourceMoveCommand.CanExecute(null).Should().BeTrue();
            explorer.UndoResourceMoveCommand.Execute(null);
            first.Members.Should().Contain("resource_one");
            second.Members.Should().NotContain("resource_one");
            explorer.RedoResourceMoveCommand.CanExecute(null).Should().BeTrue();

            explorer.RedoResourceMoveCommand.Execute(null);
            first.Members.Should().NotContain("resource_one");
            second.Members.Should().Contain("resource_one");

            source = explorer.Rows.Single(row => row.Folder == second).Children
                .Single(row => row.ResRef == "resource_one");
            var unsorted = explorer.Rows.Single(row =>
                row.IsBranch && row.Folder == null && row.Name == "Unsorted");

            explorer.CanDropResource(source, unsorted).Should().BeTrue();
            explorer.DropResource(source, unsorted).Should().BeTrue();
            section.FoldersContaining("resource_one").Should().BeEmpty();
            explorer.Rows.Single(row => row.Name == "Unsorted").Children
                .Should().ContainSingle(row => row.ResRef == "resource_one");

            explorer.UndoResourceMoveCommand.Execute(null);
            second.Members.Should().Contain("resource_one");
            explorer.RedoResourceMoveCommand.Execute(null);
            section.FoldersContaining("resource_one").Should().BeEmpty();
        }

        [Test]
        public void UndoRestoresEveryPreviousFolderMembership()
        {
            File.WriteAllText(Path.Combine(_module, "nss", "resource_one.nss"), "void main() {}");

            var log = new OutputLogService();
            var workspace = new WorkspaceContext(root => new ModuleWorkspace(root), log);
            workspace.Open(_module);
            var categories = new CategoryService(workspace, log);
            var section = categories.Section(ResourceType.Nss)!;
            section.IsSeeded = true;
            var first = section.AddFolder("First");
            var second = section.AddFolder("Second");
            var third = section.AddFolder("Third");
            first.AddMember("resource_one");
            third.AddMember("resource_one");
            categories.SaveChanges().Saved.Should().BeTrue();

            var explorer = new ModuleExplorerViewModel(
                workspace,
                new PropertiesViewModel(workspace, log),
                categories,
                log)
            {
                SelectedType = ResourceType.Nss
            };
            explorer.Initialize();

            var source = explorer.Rows.Single(row => row.Folder == first).Children
                .Single(row => row.ResRef == "resource_one");
            var target = explorer.Rows.Single(row => row.Folder == second);
            explorer.DropResource(source, target).Should().BeTrue();

            explorer.UndoResourceMoveCommand.Execute(null);

            first.Members.Should().Contain("resource_one");
            second.Members.Should().NotContain("resource_one");
            third.Members.Should().Contain("resource_one");
        }

        [Test]
        public void UndoPersistsWhenTheInMemoryMembershipAlreadyMatchesItsDestination()
        {
            File.WriteAllText(Path.Combine(_module, "nss", "resource_one.nss"), "void main() {}");

            var log = new OutputLogService();
            var workspace = new WorkspaceContext(root => new ModuleWorkspace(root), log);
            workspace.Open(_module);
            var categories = new CategoryService(workspace, log);
            var section = categories.Section(ResourceType.Nss)!;
            section.IsSeeded = true;
            var first = section.AddFolder("First");
            var second = section.AddFolder("Second");
            first.AddMember("resource_one");
            categories.SaveChanges().Saved.Should().BeTrue();

            var explorer = new ModuleExplorerViewModel(
                workspace,
                new PropertiesViewModel(workspace, log),
                categories,
                log)
            {
                SelectedType = ResourceType.Nss
            };
            explorer.Initialize();

            var source = explorer.Rows.Single(row => row.Folder == first).Children
                .Single(row => row.ResRef == "resource_one");
            var target = explorer.Rows.Single(row => row.Folder == second);
            explorer.DropResource(source, target).Should().BeTrue();

            // Reproduce the state left by a failed persistence implementation: memory already has
            // the undo destination, while the sidecar still contains the completed move.
            second.RemoveMember("resource_one").Should().BeTrue();
            first.AddMember("resource_one").Should().BeTrue();

            explorer.UndoResourceMoveCommand.Execute(null);

            var persisted = CategoryCatalog.Load(
                CategoryCatalog.DefaultPathFor(_module), out var warning);
            warning.Should().BeNull();
            persisted.Section(ResourceType.Nss).FoldersContaining("resource_one")
                .Select(folder => folder.Name).Should().Equal("First");
            explorer.RedoResourceMoveCommand.CanExecute(null).Should().BeTrue();
        }

        [Test]
        public void RenamingAMovedResourceInvalidatesItsUndoHistory()
        {
            var oldPath = Path.Combine(_module, "nss", "resource_one.nss");
            var newPath = Path.Combine(_module, "nss", "resource_renamed.nss");
            File.WriteAllText(oldPath, "void main() {}");

            var log = new OutputLogService();
            var workspace = new WorkspaceContext(root => new ModuleWorkspace(root), log);
            workspace.Open(_module);
            var categories = new CategoryService(workspace, log);
            var section = categories.Section(ResourceType.Nss)!;
            section.IsSeeded = true;
            var first = section.AddFolder("First");
            var second = section.AddFolder("Second");
            first.AddMember("resource_one");
            categories.SaveChanges().Saved.Should().BeTrue();

            var explorer = new ModuleExplorerViewModel(
                workspace,
                new PropertiesViewModel(workspace, log),
                categories,
                log)
            {
                SelectedType = ResourceType.Nss
            };
            explorer.Initialize();

            var source = explorer.Rows.Single(row => row.Folder == first).Children
                .Single(row => row.ResRef == "resource_one");
            var target = explorer.Rows.Single(row => row.Folder == second);
            explorer.DropResource(source, target).Should().BeTrue();
            explorer.UndoResourceMoveCommand.CanExecute(null).Should().BeTrue();

            File.Move(oldPath, newPath);
            second.RemoveMember("resource_one").Should().BeTrue();
            second.AddMember("resource_renamed").Should().BeTrue();
            categories.SaveChanges().Saved.Should().BeTrue();
            workspace.RemoveCatalogEntry(ResourceType.Nss, "resource_one");
            workspace.RefreshCatalogEntry(ResourceType.Nss, "resource_renamed");

            explorer.UndoResourceMoveCommand.CanExecute(null).Should().BeFalse();
            section.AllFolders().Should().NotContain(folder => folder.Members.Contains("resource_one"));
            second.Members.Should().Contain("resource_renamed");
        }

        [Test]
        public void EmptyUnsortedRemainsVisibleAsADragOutTarget()
        {
            File.WriteAllText(Path.Combine(_module, "nss", "filed.nss"), "void main() {}");

            var log = new OutputLogService();
            var workspace = new WorkspaceContext(root => new ModuleWorkspace(root), log);
            workspace.Open(_module);
            var categories = new CategoryService(workspace, log);
            var section = categories.Section(ResourceType.Nss)!;
            section.IsSeeded = true;
            section.AddFolder("Filed").AddMember("filed");
            categories.SaveChanges().Saved.Should().BeTrue();

            var explorer = new ModuleExplorerViewModel(
                workspace,
                new PropertiesViewModel(workspace, log),
                categories,
                log)
            {
                SelectedType = ResourceType.Nss
            };
            explorer.Initialize();

            explorer.Rows.Should().ContainSingle(row => row.Name == "Unsorted" && row.Count == 0,
                "an empty synthetic folder is still the drop target for taking a resource out of a folder");
        }
    }
}
