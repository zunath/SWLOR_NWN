using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Shell.Panels;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Where a freshly created area is filed once the nonmodal New Area wizard's callback fires.
    /// </summary>
    /// <remarks>
    /// The wizard is a nonmodal overlay: the builder can switch Module Contents tabs or change the
    /// selected folder while it is still open on screen. The completion callback used to read
    /// <c>SelectedRow.Folder</c> at that later moment instead of the folder that was selected when
    /// "New Area..." was clicked, so a tab switch (or a different folder pick) while the form sat open
    /// could file the new area under a Dialogs or Scripts category instead of Areas. These drive the
    /// completion callback directly - bypassing <c>NewAreaViewModel.Create</c>'s real tileset/writer
    /// machinery, which is irrelevant to what this guards - to prove the folder captured at open time
    /// wins over whatever is selected when the callback actually runs.
    /// </remarks>
    [TestFixture]
    public class ModuleExplorerNewAreaFilingTests
    {
        private string _root = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), $"swlor_newarea_filing_{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path.Combine(_root, "are"));
            Directory.CreateDirectory(Path.Combine(_root, "utc"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }

        [Test]
        public void NewAreaFilesIntoTheFolderSelectedWhenTheWizardWasOpened()
        {
            var log = new OutputLogService();
            var workspace = new WorkspaceContext(root => new ModuleWorkspace(root), log);
            workspace.Open(_root);
            var categories = new CategoryService(workspace, log);

            var areaFolder = categories.Section(ResourceType.Area)!.Find("Tatooine") ?? categories.Section(ResourceType.Area)!.AddFolder("Tatooine");
            var scriptsFolder = categories.Section(ResourceType.Nss)!.Find("Utility") ?? categories.Section(ResourceType.Nss)!.AddFolder("Utility");

            var explorer = new ModuleExplorerViewModel(
                workspace,
                new PropertiesViewModel(workspace, log),
                categories,
                log)
            {
                SelectedType = ResourceType.Area
            };
            explorer.Initialize();

            // Select the Areas folder the area should end up in, then open the wizard from there.
            explorer.SelectedRow = explorer.Rows.Single(row => row.Folder == areaFolder);
            explorer.NewItemCommand.Execute(null);
            var wizard = explorer.ActiveNewArea;
            wizard.Should().NotBeNull("the Area tab's New command opens the wizard synchronously");

            // The builder now switches to the Scripts tab and picks a folder there while the nonmodal
            // wizard is still open - exactly the sequence the review comment called out.
            explorer.SelectedType = ResourceType.Nss;
            explorer.SelectedRow = explorer.Rows.Single(row => row.Folder == scriptsFolder);

            InvokeOnCreated(wizard!, "wp_new_area");

            areaFolder.Members.Should().Contain("wp_new_area",
                "the area must file into the folder that was selected when the wizard was opened");
            scriptsFolder.Members.Should().NotContain("wp_new_area",
                "the area must not be filed under a Scripts folder selected after the wizard was opened");
        }

        [Test]
        public void NewAreaFilesIntoNoFolderWhenNoneWasSelectedAtOpenTime()
        {
            var log = new OutputLogService();
            var workspace = new WorkspaceContext(root => new ModuleWorkspace(root), log);
            workspace.Open(_root);
            var categories = new CategoryService(workspace, log);
            var scriptsFolder = categories.Section(ResourceType.Nss)!.Find("Utility") ?? categories.Section(ResourceType.Nss)!.AddFolder("Utility");

            var explorer = new ModuleExplorerViewModel(
                workspace,
                new PropertiesViewModel(workspace, log),
                categories,
                log)
            {
                SelectedType = ResourceType.Area,
                SelectedRow = null
            };
            explorer.Initialize();

            explorer.NewItemCommand.Execute(null);
            var wizard = explorer.ActiveNewArea;
            wizard.Should().NotBeNull();

            explorer.SelectedType = ResourceType.Nss;
            explorer.SelectedRow = explorer.Rows.Single(row => row.Folder == scriptsFolder);

            InvokeOnCreated(wizard!, "wp_unsorted_area");

            scriptsFolder.Members.Should().NotContain("wp_unsorted_area",
                "with nothing selected when the wizard opened, the area must land in Unsorted - never " +
                "a folder that only became selected afterward");
        }

        /// <summary>
        /// Invokes the private completion callback <c>ModuleExplorerViewModel.NewArea</c> gave the
        /// wizard, standing in for a successful <c>NewAreaWriter.TryCreate</c> without needing a real
        /// tileset/NWN install - irrelevant machinery for a test about where the result gets filed.
        /// </summary>
        private static void InvokeOnCreated(NewAreaViewModel wizard, string resRef)
        {
            var field = typeof(NewAreaViewModel).GetField("_onCreated", BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new MissingFieldException(nameof(NewAreaViewModel), "_onCreated");
            var callback = (Action<string>)field.GetValue(wizard)!;
            callback(resRef);
        }
    }
}
