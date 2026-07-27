using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    [TestFixture]
    public class ModuleFileWatcherTests
    {
        [TestCase(@"C:\module\utc\alask.utc.json", ResourceType.Utc, "alask")]
        [TestCase(@"C:\module\nss\on_enter.nss", ResourceType.Nss, "on_enter")]
        [TestCase(@"C:\module\nss\ON_EXIT.NSS", ResourceType.Nss, "ON_EXIT")]
        public void SupportedResourcePathsResolveTheirTypeAndResRef(
            string path,
            ResourceType expectedType,
            string expectedResRef)
        {
            ModuleFileWatcher.TryResolveResource(path, out var type, out var resRef).Should().BeTrue();

            type.Should().Be(expectedType);
            resRef.Should().Be(expectedResRef);
        }

        [TestCase(@"C:\module\config.json")]
        [TestCase(@"C:\module\packing\area.git.json.tmp")]
        [TestCase(@"C:\module\readme.txt")]
        public void NonResourcePathsAreIgnored(string path)
        {
            ModuleFileWatcher.TryResolveResource(path, out _, out _).Should().BeFalse();
        }

        [TestCase(@"C:\module\git\area.git.json")]
        [TestCase(@"C:\module\are\area.are.json")]
        [TestCase(@"C:\module\utd\door.utd.json")]
        [TestCase(@"C:\module\uti\door_key.uti.json")]
        [TestCase(@"C:\module\utw\waypoint.utw.json")]
        public void TransitionTagResourcesInvalidateTheTagIndex(string path)
        {
            ModuleFileWatcher.AffectsTagIndex(path).Should().BeTrue();
        }

        [TestCase(@"C:\module\utc\creature.utc.json")]
        [TestCase(@"C:\module\dlg\conversation.dlg.json")]
        public void UnrelatedResourcesDoNotInvalidateTheTagIndex(string path)
        {
            ModuleFileWatcher.AffectsTagIndex(path).Should().BeFalse();
        }

        [TestCase(@"C:\module\git\area.git.json")]
        [TestCase(@"C:\module\utc\creature.utc.json")]
        [TestCase(@"C:\module\dlg\conversation.dlg.json")]
        [TestCase(@"C:\module\are\area.are.json")]
        public void ScriptBearingResourcesInvalidateUsageIndex(string path)
        {
            ModuleFileWatcher.AffectsScriptUsages(path).Should().BeTrue();
        }

        [TestCase(@"C:\module\nss\script.nss")]
        [TestCase(@"C:\module\config.json")]
        public void ResourcesWithoutScriptSlotsDoNotInvalidateUsageIndex(string path)
        {
            ModuleFileWatcher.AffectsScriptUsages(path).Should().BeFalse();
        }

        [Test]
        public void WorkspaceCatalogRefreshInvalidatesScriptUsagesOnlyForScriptedResources()
        {
            var context = new WorkspaceContext(
                _ => throw new NotSupportedException(),
                new OutputLogService());
            var invalidations = 0;
            context.ScriptUsagesInvalidated += () => invalidations++;

            context.RefreshCatalogEntry(ResourceType.Utc, "creature");
            context.RefreshCatalogEntry(ResourceType.Nss, "script");

            invalidations.Should().Be(1);
        }

        [Test]
        public void WorkspaceTagInvalidationNotifiesLiveIndexConsumers()
        {
            var context = new WorkspaceContext(
                _ => throw new NotSupportedException(),
                new OutputLogService());
            var invalidations = 0;
            context.TagIndexInvalidated += () => invalidations++;

            context.InvalidateTagIndex();

            invalidations.Should().Be(1,
                "open behavior editors need a signal that their materialized object-tag choices are stale");
        }
    }
}
