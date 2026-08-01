using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Tests
{
    public class ItemReferenceScannerTests
    {
        private string _root = null!;
        private string _module = null!;
        private string _gameSource = null!;
        private string _generatorInputs = null!;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(
                Path.GetTempPath(),
                "SWLOR.Toolset.Tests",
                "item_reference_scanner_" + Guid.NewGuid().ToString("N"));
            _module = Path.Combine(_root, "Module");
            _gameSource = Path.Combine(_root, "SWLOR.Game.Server");
            _generatorInputs = Path.Combine(_root, "SWLOR.CLI", "InputFiles");

            foreach (var folder in new[] { "uti", "git", "itp", "nss" })
                Directory.CreateDirectory(Path.Combine(_module, folder));
            Directory.CreateDirectory(_gameSource);
            Directory.CreateDirectory(_generatorInputs);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }

        [Test]
        public void FindReferences_IgnoresGeneratedPalettesButKeepsRealReferences()
        {
            const string resRef = "basic_shovel";
            var selfPath = Path.Combine(_module, "uti", resRef + ".uti.json");
            File.WriteAllText(selfPath, $"{{\"TemplateResRef\":{{\"value\":\"{resRef}\"}}}}");
            File.WriteAllText(
                Path.Combine(_module, "itp", "itempalcus.itp.json"),
                $"{{\"RESREF\":{{\"value\":\"{resRef}\"}}}}");
            File.WriteAllText(
                Path.Combine(_module, "git", "placed.git.json"),
                $"{{\"TemplateResRef\":{{\"value\":\"{resRef}\"}}}}");
            File.WriteAllText(
                Path.Combine(_module, "nss", "loot.nss"),
                $"CreateItemOnObject(\"{resRef}\", OBJECT_SELF);");
            File.WriteAllText(
                Path.Combine(_gameSource, "LootDefinition.cs"),
                $"builder.AddItem(\"{resRef}\", 1);");
            File.WriteAllText(
                Path.Combine(_generatorInputs, "enhancement_list.tsv"),
                $"Cooking\tBasic Shovel\t{resRef}\t5\tStore");

            var references = ItemReferenceScanner.FindReferences(
                _module,
                _gameSource,
                resRef,
                selfPath,
                _generatorInputs);

            references.Should().BeEquivalentTo(
                "Module/git/placed.git.json",
                "Module/nss/loot.nss",
                "SWLOR.Game.Server/LootDefinition.cs",
                "SWLOR.CLI/InputFiles/enhancement_list.tsv");
            references.Should().NotContain(reference =>
                reference.Contains("itempalcus", StringComparison.OrdinalIgnoreCase));
        }
    }
}
