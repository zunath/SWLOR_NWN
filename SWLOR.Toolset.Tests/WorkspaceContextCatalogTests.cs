using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// <see cref="WorkspaceContext.RefreshCatalogEntry"/>/<see cref="WorkspaceContext.RemoveCatalogEntry"/>
    /// against <see cref="BlueprintCatalog"/>: which resource kinds are actually written into it.
    /// </summary>
    /// <remarks>
    /// <see cref="BlueprintCatalog"/>'s initial build only indexes areas and blueprint types - never
    /// dialogs or scripts. Before this, saving, creating, or externally changing a DLG or NSS
    /// unconditionally inserted it into the catalog anyway, so the Search panel returned that one
    /// changed resource by resref while silently omitting every other, unchanged resource of the same
    /// type until the module was reopened and the catalog rebuilt from scratch. These check that
    /// dialogs/scripts stay out of the catalog on those events while the refresh notification Module
    /// Contents relies on still fires, that indexed types are unaffected, and that the shared predicate
    /// both call sites read agrees with what the initial build actually does.
    /// </remarks>
    [TestFixture]
    public class WorkspaceContextCatalogTests
    {
        private string _root = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), $"swlor_wscatalog_{Guid.NewGuid():N}");
            foreach (var folder in new[] { "are", "utc", "git", "gic", "dlg", "nss" })
                Directory.CreateDirectory(Path.Combine(_root, folder));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }

        [TestCase(ResourceType.Dlg)]
        [TestCase(ResourceType.Nss)]
        public void RefreshCatalogEntry_ForAnUnindexedType_DoesNotInsertIntoTheCatalogButStillNotifies(ResourceType type)
        {
            var log = new OutputLogService();
            var workspace = new WorkspaceContext(root => new ModuleWorkspace(root), log);
            workspace.Open(_root);
            workspace.Catalog!.BuildTask.GetAwaiter().GetResult();

            var notified = new List<(ResourceType Type, string ResRef)>();
            workspace.CatalogEntryRefreshed += (t, r) => notified.Add((t, r));

            workspace.RefreshCatalogEntry(type, "some_resref");

            workspace.Catalog!.TryGetEntry(type, "some_resref", out _).Should().BeFalse(
                "the initial build never indexes dialogs or scripts, so a change event must not partially seed them in");
            notified.Should().ContainSingle().Which.Should().Be((type, "some_resref"),
                "Module Contents still needs the refresh notification even though the catalog itself is untouched");
        }

        [TestCase(ResourceType.Dlg)]
        [TestCase(ResourceType.Nss)]
        public void RemoveCatalogEntry_ForAnUnindexedType_LeavesTheCatalogAloneAndStillNotifies(ResourceType type)
        {
            var log = new OutputLogService();
            var workspace = new WorkspaceContext(root => new ModuleWorkspace(root), log);
            workspace.Open(_root);
            workspace.Catalog!.BuildTask.GetAwaiter().GetResult();

            var notified = new List<(ResourceType Type, string ResRef)>();
            workspace.CatalogEntryRefreshed += (t, r) => notified.Add((t, r));

            var act = () => workspace.RemoveCatalogEntry(type, "some_resref");

            act.Should().NotThrow();
            workspace.Catalog!.TryGetEntry(type, "some_resref", out _).Should().BeFalse();
            notified.Should().ContainSingle().Which.Should().Be((type, "some_resref"));
        }

        [Test]
        public void RefreshCatalogEntry_ForAnIndexedType_StillUpdatesTheCatalog()
        {
            var log = new OutputLogService();
            var workspace = new WorkspaceContext(root => new ModuleWorkspace(root), log);
            workspace.Open(_root);
            workspace.Catalog!.BuildTask.GetAwaiter().GetResult();

            File.WriteAllText(
                Path.Combine(_root, "utc", "new_creature.utc.json"),
                "{\"__data_type\":\"UTC \"}");

            workspace.RefreshCatalogEntry(ResourceType.Utc, "new_creature");

            workspace.Catalog!.TryGetEntry(ResourceType.Utc, "new_creature", out _).Should().BeTrue(
                "a blueprint type is one the initial build indexes, so a refresh must still update it");
        }

        [TestCase(ResourceType.Area, true)]
        [TestCase(ResourceType.Utc, true)]
        [TestCase(ResourceType.Uti, true)]
        [TestCase(ResourceType.Dlg, false)]
        [TestCase(ResourceType.Nss, false)]
        public void IsCatalogIndexedType_MatchesWhatTheInitialBuildIndexes(ResourceType type, bool expected)
        {
            WorkspaceContext.IsCatalogIndexedType(type).Should().Be(expected);
        }

        [Test]
        public async Task SuccessfulCatalogBuildPublishesCompletionForLateNameResolution()
        {
            var log = new OutputLogService();
            var workspace = new WorkspaceContext(root => new ModuleWorkspace(root), log);
            var completed = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            workspace.CatalogBuildCompleted += () => completed.TrySetResult();

            workspace.Open(_root);

            await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }

        [Test]
        public void PlacementInvalidationPublishesARefreshNotification()
        {
            var log = new OutputLogService();
            var workspace = new WorkspaceContext(root => new ModuleWorkspace(root), log);
            workspace.Open(_root);
            var notifications = 0;
            workspace.PlacementIndexInvalidated += () => notifications++;

            workspace.InvalidatePlacementIndex();

            notifications.Should().Be(1);
        }

        [TestCase(ResourceType.Utc)]
        [TestCase(ResourceType.Uti)]
        [TestCase(ResourceType.Utp)]
        public void OrdinaryBlueprintCatalogRefreshDoesNotInvalidatePlacements(ResourceType type)
        {
            var workspace = new WorkspaceContext(
                root => new ModuleWorkspace(root),
                new OutputLogService());
            workspace.Open(_root);
            var notifications = 0;
            workspace.PlacementIndexInvalidated += () => notifications++;

            workspace.RefreshCatalogEntry(type, "ordinary_save");

            notifications.Should().Be(0,
                "blueprint contents are not inputs to the module-wide GIT placement index");
        }

        [Test]
        public void AreaCatalogRefreshDoesNotInvalidatePlacements()
        {
            var workspace = new WorkspaceContext(
                root => new ModuleWorkspace(root),
                new OutputLogService());
            workspace.Open(_root);
            var notifications = 0;
            workspace.PlacementIndexInvalidated += () => notifications++;

            workspace.RefreshCatalogEntry(ResourceType.Area, "changed_area");

            notifications.Should().Be(0,
                "ARE metadata is not an input to the module-wide GIT placement index");
        }

        [Test]
        public void AreaCatalogRemovalInvalidatesPlacements()
        {
            var workspace = new WorkspaceContext(
                root => new ModuleWorkspace(root),
                new OutputLogService());
            workspace.Open(_root);
            var notifications = 0;
            workspace.PlacementIndexInvalidated += () => notifications++;

            workspace.RemoveCatalogEntry(ResourceType.Area, "deleted_area");

            notifications.Should().Be(1,
                "removing an area removes all placements from its paired GIT");
        }
    }
}
