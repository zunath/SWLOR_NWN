using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public class SpearAppearanceTests
{
    [Test]
    public void PlayerSpearBlueprints_HaveEveryPartInTheAppearanceEditorCatalog()
    {
        var appearance = new SpearAppearanceDefinition();
        var parts = new[] { appearance.TopParts, appearance.MiddleParts, appearance.BottomParts };
        var partNames = new[] { "Top", "Middle", "Bottom" };
        var checkedBlueprints = new List<string>();
        var missingParts = new List<string>();

        foreach (var file in Directory.EnumerateFiles(
                     Path.Combine(FindRepositoryRoot().FullName, "Module", "uti"), "*.uti.json"))
        {
            var blueprint = JObject.Parse(File.ReadAllText(file));
            var baseItem = (BaseItem)(blueprint["BaseItem"]?["value"]?.Value<int>() ?? -1);
            if (baseItem != BaseItem.ShortSpear)
                continue;

            var noEconomy = (blueprint["VarTable"]?["value"] as JArray)?.Any(variable =>
                variable["Name"]?["value"]?.Value<string>() == Item.NoEconomyVariable &&
                variable["Type"]?["value"]?.Value<int>() == 1 &&
                variable["Value"]?["value"]?.Value<int>() == 1) == true;
            var name = blueprint["LocalizedName"]?["value"]?["0"]?.Value<string>();
            if (Item.IsEconomyRestricted(baseItem, name, noEconomy, hasInventoryIcon: true) ||
                blueprint["Plot"]?["value"]?.Value<int>() == 1 ||
                blueprint["Cursed"]?["value"]?.Value<int>() == 1)
                continue;

            checkedBlueprints.Add(Path.GetFileName(file));
            for (var part = 0; part < parts.Length; part++)
            {
                var field = $"ModelPart{part + 1}";
                var storedPart = Math.Max(
                    blueprint[field]?["value"]?.Value<int>() ?? 0,
                    blueprint["x" + field]?["value"]?.Value<int>() ?? 0);
                // UTI weapon parts store model * 10 + color; the editor uses color * 100 + model.
                var catalogId = storedPart / 10 + storedPart % 10 * 100;
                if (!parts[part].Contains(catalogId))
                    missingParts.Add($"{Path.GetFileName(file)} ({name}): {partNames[part]} {catalogId}");
            }
        }

        checkedBlueprints.Should().Contain("proto_spear.uti.json", "the reported spear must be covered");
        missingParts.Should().BeEmpty(
            "the editor rejects a weapon when any current part is absent from its catalog:\n" +
            string.Join("\n", missingParts));
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        directory.Should().NotBeNull("the repository root should be discoverable from the test directory");
        return directory!;
    }
}
