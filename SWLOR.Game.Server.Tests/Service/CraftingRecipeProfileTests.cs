using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Service;

public class CraftingRecipeProfileTests
{
    [Test]
    public void EntireRecipeCorpus_MatchesExplicitAuthoringCatalogAndPreservesTechniqueTags()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "design", "crafting", "recipe-profiles.csv"))) directory = directory.Parent;
        Assert.That(directory, Is.Not.Null);
        var metadata = File.ReadAllLines(Path.Combine(directory.FullName, "design", "crafting", "recipe-profiles.csv"))
            .Skip(1).Select(line => line.Split(',')).ToDictionary(row => row[0]);
        var seen = new HashSet<RecipeType>(); var pilots = new Dictionary<SkillType,int>();
        foreach (var type in typeof(IRecipeListDefinition).Assembly.GetTypes().Where(type => !type.IsAbstract && typeof(IRecipeListDefinition).IsAssignableFrom(type)))
        foreach (var (id, recipe) in ((IRecipeListDefinition)Activator.CreateInstance(type)).BuildRecipes())
        {
            Assert.That(seen.Add(id), Is.True, $"Duplicate recipe {id}"); Assert.That(metadata.ContainsKey(id.ToString()), Is.True, id.ToString());
            var expected = metadata[id.ToString()];
            Assert.That(recipe.CraftingProfile.ToString(), Is.EqualTo(expected[3]), id.ToString());
            Assert.That(recipe.CraftingTechnique.ToString(), Is.EqualTo(expected[4]), id.ToString());
            Assert.That(recipe.IsCraftingPilot.ToString().ToLowerInvariant(), Is.EqualTo(expected[5]), id.ToString());
            Assert.That(recipe.CraftingProfile, Is.Not.EqualTo(CraftProfile.Legacy), id.ToString());
            if (recipe.IsCraftingPilot) pilots[recipe.Skill] = pilots.GetValueOrDefault(recipe.Skill) + 1;
            if (recipe.Skill == SkillType.Espionage) Assert.That(recipe.CraftingTechnique, Is.Not.EqualTo(CraftTechnique.None), id.ToString());
            else Assert.That(recipe.CraftingTechnique, Is.EqualTo(CraftTechnique.None), id.ToString());
        }
        Assert.That(seen.Count, Is.EqualTo(metadata.Count)); Assert.That(pilots.Count, Is.EqualTo(5));
        Assert.That(pilots.Values, Is.All.EqualTo(3));
    }
    [Test]
    public void Rollback_ChangesNewCraftSelectionWithoutRewritingCommittedSessions()
    {
        var recipe = new RecipeDetail { CraftingProfile = CraftProfile.Delicate, IsCraftingPilot = true };
        Assert.That(CraftRolloutPolicy.UseConditions(CraftRollout.Pilot, recipe), Is.True);
        Assert.That(CraftRolloutPolicy.UseConditions(CraftRollout.Legacy, recipe), Is.False);
        recipe.IsCraftingPilot = false;
        Assert.That(CraftRolloutPolicy.UseConditions(CraftRollout.Pilot, recipe), Is.False);
        Assert.That(CraftRolloutPolicy.UseConditions(CraftRollout.Full, recipe), Is.True);
    }
}
