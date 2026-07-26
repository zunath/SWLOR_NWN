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
    }
}
