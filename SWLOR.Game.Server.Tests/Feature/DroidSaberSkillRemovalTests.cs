using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// Droids cannot equip saber weapons, so no droid part may offer the saber skills and no store may sell their boosts.
/// </summary>
public class DroidSaberSkillRemovalTests
{
    private static readonly string[] RetiredBooks =
    {
        "recipe_dlsab1", "recipe_dlsab2", "recipe_dsabst1", "recipe_dsabst2",
    };

    [Test]
    public void DroidCpus_DoNotCarryLightsaberOrSaberstaff()
    {
        var files = Directory.GetFiles(Path.Combine(FindRepositoryRoot().FullName, "Module", "uti"), "d_*_cpu*.uti.json");
        files.Should().NotBeEmpty();

        foreach (var file in files)
        {
            using var item = JsonDocument.Parse(File.ReadAllText(file));
            var saberSkills = item.RootElement.GetProperty("PropertiesList").GetProperty("value").EnumerateArray()
                .Where(property =>
                    property.GetProperty("PropertyName").GetProperty("value").GetInt32() == (int)ItemPropertyType.DroidStat &&
                    property.GetProperty("Subtype").GetProperty("value").GetInt32() is (int)DroidStatSubType.Lightsaber or (int)DroidStatSubType.Saberstaff);
            saberSkills.Should().BeEmpty(Path.GetFileName(file));
        }
    }

    [Test]
    public void Stores_DoNotSellRetiredBoostRecipes()
    {
        var module = Path.Combine(FindRepositoryRoot().FullName, "Module");
        var files = Directory.GetFiles(Path.Combine(module, "utm"), "*.utm.json")
            .Concat(Directory.GetFiles(Path.Combine(module, "git"), "*.git.json"));

        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            foreach (var book in RetiredBooks)
                text.Should().NotContain($"\"{book}\"", Path.GetFileName(file));
        }
    }

    [TestCase(RecipeType.DroidLightsaberBoost1, 4801)]
    [TestCase(RecipeType.DroidSaberstaffBoost1, 4805)]
    [TestCase(RecipeType.DroidLightsaberBoost2, 4813)]
    [TestCase(RecipeType.DroidSaberstaffBoost2, 4817)]
    public void RetiredRecipeIds_StayReservedForSavedCharacters(RecipeType recipe, int id)
    {
        ((int)recipe).Should().Be(id);
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Module", "uti")))
            directory = directory.Parent;

        return directory ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
