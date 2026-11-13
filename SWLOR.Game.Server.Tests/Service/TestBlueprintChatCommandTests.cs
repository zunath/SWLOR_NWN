using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.ChatCommandDefinition;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Research;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Service;

// Guards the test-server researched blueprint commands: they exist on the test server only,
// pick recipes of the exact level, and build a fully researched blueprint.
public class TestBlueprintChatCommandTests
{
    private static readonly RecipeType First = (RecipeType)1;
    private static readonly RecipeType Second = (RecipeType)2;
    private static readonly RecipeType Third = (RecipeType)3;
    private static readonly RecipeType Fourth = (RecipeType)4;

    private static Dictionary<RecipeType, RecipeDetail> Recipes() => new()
    {
        [First] = new RecipeDetail { Resref = "a", Level = 50 },
        [Second] = new RecipeDetail { Resref = "b", Level = 52 },
        [Third] = new RecipeDetail { Resref = "c", Level = 52 },
        [Fourth] = new RecipeDetail { Resref = "d", Level = 52, IsActive = false }
    };

    private static string Name(string resref) => resref switch
    {
        "a" => "Plasteel Blade",
        "b" => "Plasteel Carbine",
        "c" => "Plasteel",
        _ => "Hidden"
    };

    [Test]
    public void Commands_OnTheTestServer_AreOpenToEveryTester()
    {
        var commands = new TestBlueprintChatCommand().BuildChatCommands(ServerEnvironmentType.Test, true);

        foreach (var name in new[] { "testblueprint50", "tbp50", "testblueprint52", "tbp52" })
        {
            commands.Should().ContainKey(name);
            commands[name].AvailableToAllOnTestEnvironment.Should().BeTrue();
        }
    }

    [TestCase(ServerEnvironmentType.Production, true)]
    [TestCase(ServerEnvironmentType.Test, false)]
    [TestCase(ServerEnvironmentType.Development, false)]
    public void Commands_OnProductionOrAnUnnamedEnvironment_DoNotExist(ServerEnvironmentType environment, bool isExplicit)
    {
        new TestBlueprintChatCommand().BuildChatCommands(environment, isExplicit).Should().BeEmpty();
    }

    [Test]
    public void FindRecipes_ReturnsOnlyActiveRecipesOfThatLevel()
    {
        var found = TestBlueprintChatCommand.FindRecipes(Recipes(), Name, 52, "");

        found.Select(match => match.Recipe).Should().Equal(Third, Second);
    }

    [Test]
    public void FindRecipes_ExactNameBeatsLongerNames()
    {
        TestBlueprintChatCommand.FindRecipes(Recipes(), Name, 52, "plasteel").Select(match => match.Recipe).Should().Equal(Third);
        TestBlueprintChatCommand.FindRecipes(Recipes(), Name, 52, "carbine").Select(match => match.Recipe).Should().Equal(Second);
        TestBlueprintChatCommand.FindRecipes(Recipes(), Name, 50, "carbine").Should().BeEmpty();
    }

    [Test]
    public void FindRecipes_SeparatesRecipesThatShareAnItemNameBySkill()
    {
        var recipes = new Dictionary<RecipeType, RecipeDetail>
        {
            [First] = new RecipeDetail { Resref = "t", Level = 52, Skill = SkillType.Engineering },
            [Second] = new RecipeDetail { Resref = "t", Level = 52, Skill = SkillType.Smithery }
        };

        var both = TestBlueprintChatCommand.FindRecipes(recipes, _ => "Token", 52, "token");
        both.Select(match => match.Name).Should().Equal("Token - Engineering", "Token - Smithery");

        TestBlueprintChatCommand.FindRecipes(recipes, _ => "Token", 52, "Token - Smithery").Select(match => match.Recipe).Should().Equal(Second);
        TestBlueprintChatCommand.FindRecipes(recipes, _ => "Token", 52, "token engineering").Select(match => match.Recipe).Should().Equal(First);
    }

    [Test]
    public void BuildResearchedBlueprint_IsResearchedToTheTop()
    {
        var blueprint = TestBlueprintChatCommand.BuildResearchedBlueprint(Second);

        blueprint.Recipe.Should().Be(Second);
        blueprint.Level.Should().Be(ResearchGrantRules.MaxLevel);
        blueprint.LicensedRuns.Should().BeGreaterThan(0);
        blueprint.EnhancementSlots.Should().Be(1);
    }
}
