using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;

namespace SWLOR.Toolset.Tests
{
    /// <summary>Verifies IfoDocument against Module/ifo/module.ifo.json.</summary>
    public class IfoDocumentTests
    {
        private static string ModuleIfoPath =>
            Path.Combine(CorpusLocator.ModuleDirectory, "ifo", "module.ifo.json");

        [Test]
        public void ModuleIfo_KnownValues_ReadCorrectly()
        {
            var document = IfoDocument.Load(ModuleIfoPath);

            document.Tag.Should().Be("SWLOR");
            document.EntryArea.Should().Be("ooc_area");
            document.Name.Text.Should().Be("Star Wars Legends of the Old Republic MODULE");
            // A floor rather than an exact count: since WP7.3 the toolset can create areas (which
            // register themselves here), so the module is a living corpus that legitimately grows.
            document.AreaList.Should().HaveCountGreaterThanOrEqualTo(438);
            document.AreaResRefs[0].Should().Be("anchor_entreenor");
            document.AreaResRefs[1].Should().Be("anchor_entreesud");
            document.HakNames[0].Should().Be("sw_2da");
            document.HakNames[1].Should().Be("sw_ability");

            var hakConfigPath = Path.Combine(CorpusLocator.RepositoryRoot, "Build", "hakbuilder.json");
            using var hakConfig = JsonDocument.Parse(File.ReadAllText(hakConfigPath));
            var configuredHakNames = hakConfig.RootElement
                .GetProperty("HakList")
                .EnumerateArray()
                .Select(entry => entry.GetProperty("Name").GetString())
                .ToArray();

            document.HakNames.Should().Equal(
                configuredHakNames,
                "a packed module only mounts the HAKs listed in module.ifo, so the module and " +
                "HAK build configuration must remain in the same order");
            document.HakNames.TakeLast(4).Should().Equal(
                "sw_tint_mtr",
                "sw_tint0",
                "sw_tint1",
                "sw_tint2");
        }
    }
}
