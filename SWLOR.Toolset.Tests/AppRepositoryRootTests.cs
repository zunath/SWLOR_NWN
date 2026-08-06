using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Gff;
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

        [Test]
        public void StartupHakLayersFollowTheSavedModulesPackedArchiveOrder()
        {
            var root = Path.Combine(
                Path.GetTempPath(),
                "swlor-app-startup-haks-" + Guid.NewGuid().ToString("N"));
            var moduleRoot = Path.Combine(root, "Module");
            var ifoDirectory = Path.Combine(moduleRoot, "ifo");
            var hakDirectory = Path.Combine(root, "hak");

            try
            {
                Directory.CreateDirectory(ifoDirectory);
                Directory.CreateDirectory(hakDirectory);
                File.WriteAllText(Path.Combine(hakDirectory, "second.HAK"), string.Empty);
                File.WriteAllText(Path.Combine(hakDirectory, "first.hak"), string.Empty);

                var ifo = new IfoDocument(new JsonGffDocument("IFO ", new JsonGffStruct()));
                ifo.SetHakNames(new[] { "first", "second" });
                File.WriteAllBytes(Path.Combine(ifoDirectory, "module.ifo.json"), ifo.ToBytes());

                var profile = new NwnIniProfile("nwn.ini", hakDirectory, null, null);
                var layers = (IReadOnlyList<ResourceIndex.HakLayer>?)typeof(App)
                    .GetMethod(
                        "ResolveStartupHakLayers",
                        BindingFlags.NonPublic | BindingFlags.Static)!
                    .Invoke(null, [moduleRoot, profile]);

                layers.Should().NotBeNull();
                layers!.Select(layer => layer.Name).Should().Equal("first", "second");
                layers.Select(layer => layer.DirectoryPath).Should().Equal(
                    Path.Combine(hakDirectory, "first.hak"),
                    Path.Combine(hakDirectory, "second.HAK"));
            }
            finally
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, recursive: true);
            }
        }

        [Test]
        public void StartupHakLayersFallBackToRepositorySourcesWhenAnyAssignedArchiveIsMissing()
        {
            var root = Path.Combine(
                Path.GetTempPath(),
                "swlor-app-startup-partial-haks-" + Guid.NewGuid().ToString("N"));
            var moduleRoot = Path.Combine(root, "Module");
            var ifoDirectory = Path.Combine(moduleRoot, "ifo");
            var hakDirectory = Path.Combine(root, "hak");

            try
            {
                Directory.CreateDirectory(ifoDirectory);
                Directory.CreateDirectory(hakDirectory);
                File.WriteAllText(Path.Combine(hakDirectory, "present.hak"), string.Empty);

                var ifo = new IfoDocument(new JsonGffDocument("IFO ", new JsonGffStruct()));
                ifo.SetHakNames(new[] { "present", "missing" });
                File.WriteAllBytes(Path.Combine(ifoDirectory, "module.ifo.json"), ifo.ToBytes());

                var profile = new NwnIniProfile("nwn.ini", hakDirectory, null, null);
                var layers = typeof(App)
                    .GetMethod(
                        "ResolveStartupHakLayers",
                        BindingFlags.NonPublic | BindingFlags.Static)!
                    .Invoke(null, [moduleRoot, profile]);

                layers.Should().BeNull(
                    "a partial packed stack must not replace the complete loose repository fallback");
            }
            finally
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, recursive: true);
            }
        }
    }
}
