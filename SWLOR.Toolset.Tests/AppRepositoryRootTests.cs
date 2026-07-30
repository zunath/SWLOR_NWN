using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Settings;

namespace SWLOR.Toolset.Tests
{
    [TestFixture]
    public sealed class AppRepositoryRootTests
    {
        [Test]
        public void GameDataRepositoryFollowsTheConfiguredModuleCheckout()
        {
            var root = Path.Combine(
                Path.GetTempPath(),
                "swlor-app-repo-" + Guid.NewGuid().ToString("N"));
            var moduleRoot = Path.Combine(root, "Module");
            var settingsPath = Path.Combine(root, "settings", "settings.json");

            try
            {
                Directory.CreateDirectory(moduleRoot);
                Directory.CreateDirectory(Path.Combine(root, "Build"));
                Directory.CreateDirectory(Path.Combine(root, "SWLOR_Haks"));
                File.WriteAllText(Path.Combine(root, "Build", "hakbuilder.json"), "{}");

                var settings = ToolsetSettings.Load(settingsPath);
                settings.ModuleRoot = moduleRoot;

                var resolved = typeof(App)
                    .GetMethod(
                        "ResolveRepoRoot",
                        BindingFlags.NonPublic | BindingFlags.Static)!
                    .Invoke(null, [settings]);

                resolved.Should().Be(root);
            }
            finally
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, recursive: true);
            }
        }
    }
}
