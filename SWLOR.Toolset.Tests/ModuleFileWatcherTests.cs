using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Workspace;
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
    }
}
