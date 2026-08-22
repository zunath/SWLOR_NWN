using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class ResearchPerkRegressionTests
{
    [Test]
    public void ResearchPerkIds_RemainCompatibleWithPersistedPlayerData()
    {
        ((int)PerkType.ScientificNetworking).Should().Be(256);
        ((int)PerkType.Research).Should().Be(297);
        ((int)PerkType.ResearchProjects).Should().Be(299);
    }

    [Test]
    public void ResearchPerkDefinitions_MatchCurrentApprovedRanks()
    {
        var perks = BuildResearchPerksWithout2daLookup();

        AssertPerk(
            perks[PerkType.Research],
            "Research",
            FeatType.ResearchTrait,
            (2, 10, "Grants ability to research tier 1 blueprints."),
            (2, 20, "Grants ability to research tier 2 blueprints."),
            (3, 30, "Grants ability to research tier 3 blueprints."),
            (3, 40, "Grants ability to research tier 4 blueprints."),
            (4, 50, "Grants ability to research tier 5 blueprints."));

        AssertPerk(
            perks[PerkType.ScientificNetworking],
            "Scientific Networking",
            FeatType.ScientificNetworkingTrait,
            (3, 25, "Blueprints are created with one additional licensed run per rank."),
            (4, 50, "Blueprints are created with one additional licensed run per rank."));

        AssertPerk(
            perks[PerkType.ResearchProjects],
            "Research Projects",
            FeatType.ResearchProjectsTrait,
            (2, 25, "Increases the maximum number of concurrent research jobs by 1, for a total of 2."),
            (3, 50, "Increases the maximum number of concurrent research jobs by 1, for a total of 3."));
    }

    [Test]
    public void Research_PreservesTerminalAccessAndRecipeTierGates()
    {
        var craft = ReadSource("Service", "Craft.cs");
        var canResearchRecipe = ExtractMethod(
            craft,
            "public static bool CanPlayerResearchRecipe(uint player, RecipeType recipeType)");
        var useResearchTerminal = ExtractMethod(craft, "public static void UseResearchTerminal()");
        var researchViewModel = ReadSource("Feature", "GuiDefinition", "ViewModel", "ResearchViewModel.cs");
        var buildJob = ExtractMethod(
            researchViewModel,
            "private ResearchJobDetails BuildResearchJobDetails(RecipeType recipeType, uint blueprintItem)");
        var validateJob = ExtractMethod(researchViewModel, "private string ValidateJob()");
        var recipesViewModel = ReadSource("Feature", "GuiDefinition", "ViewModel", "RecipesViewModel.cs");
        var validateBlueprint = ExtractMethod(recipesViewModel, "private bool ValidateBlueprint(uint item)");

        useResearchTerminal.Should().Contain("Perk.GetPerkLevel(player, PerkType.Research)");
        useResearchTerminal.Should().Contain("if (researchLevel <= 0)");
        useResearchTerminal.Should().Contain("Perk 'Research I' is required to use research terminals.");

        canResearchRecipe.Should().Contain("var tier = recipe.Level / 10 + 1;");
        canResearchRecipe.Should().Contain("if (tier > 5)");
        canResearchRecipe.Should().Contain("Perk.GetPerkLevel(player, PerkType.Research) >= tier");

        buildJob.Should().Contain("var perkLevel = recipe.Level / 10 + 1;");
        buildJob.Should().Contain("if (perkLevel > 5)");
        buildJob.Should().Contain("RequiredPerkLevel = perkLevel");
        validateJob.Should().Contain("Perk.GetPerkLevel(Player, PerkType.Research) < researchJob.RequiredPerkLevel");
        validateJob.Should().Contain("researchJob.CurrentLevel >= Craft.MaxResearchLevel");

        validateBlueprint.Should().Contain("var requiredLevel = recipe.Level / 10 + 1;");
        validateBlueprint.Should().Contain("if (requiredLevel > 5)");
        validateBlueprint.Should().Contain("if (researchLevel < requiredLevel)");
        validateBlueprint.Should().Contain("blueprint.Level >= Craft.MaxResearchLevel");
    }

    [TestCase(0, 1, 3)]
    [TestCase(1, 2, 4)]
    [TestCase(2, 3, 5)]
    public void ScientificNetworking_PreservesLicensedRunRanges(
        int rank,
        int expectedMinimum,
        int expectedMaximum)
    {
        var researchViewModel = ReadSource("Feature", "GuiDefinition", "ViewModel", "ResearchViewModel.cs");
        var buildJob = ExtractMethod(
            researchViewModel,
            "private ResearchJobDetails BuildResearchJobDetails(RecipeType recipeType, uint blueprintItem)");
        var completeJob = ExtractMethod(researchViewModel, "public Action ClickCompleteJob() => () =>");

        (1 + rank).Should().Be(expectedMinimum);
        (expectedMinimum + 2).Should().Be(expectedMaximum);
        buildJob.Should().Contain("var licensedRunsMinimum = 1 + Perk.GetPerkLevel(Player, PerkType.ScientificNetworking);");
        buildJob.Should().Contain("var licensedRunsMaximum = licensedRunsMinimum + 2;");
        completeJob.Should().Contain("var scientificNetworking = Perk.GetPerkLevel(Player, PerkType.ScientificNetworking);");
        completeJob.Should().Contain("blueprintDetails.LicensedRuns = Random.D3(1) + scientificNetworking;");
    }

    [TestCase(0, 1)]
    [TestCase(1, 2)]
    [TestCase(2, 3)]
    public void ResearchProjects_PreservesOnePlusRankConcurrentJobLimit(int rank, int expectedMaximum)
    {
        var researchViewModel = ReadSource("Feature", "GuiDefinition", "ViewModel", "ResearchViewModel.cs");
        var validation = ExtractMethod(researchViewModel, "private string ValidateJob()");

        (rank + 1).Should().Be(expectedMaximum);
        validation.Should().Contain("Perk.GetPerkLevel(Player, PerkType.ResearchProjects) + 1");
        validation.Should().Contain("currentJobs.Count(x => x.ParentPropertyId != _researchTerminalPropertyId)");
        validation.Should().Contain("if (currentJobCount >= maxConcurrentJobs)");
    }

    private static void AssertPerk(
        PerkDetail perk,
        string name,
        FeatType traitFeat,
        params (int Price, int SkillRank, string Description)[] expectedLevels)
    {
        perk.Category.Should().Be(PerkCategoryType.Fabrication);
        perk.Name.Should().Be(name);
        perk.PerkLevels.Keys.OrderBy(x => x).Should().Equal(Enumerable.Range(1, expectedLevels.Length));

        for (var rank = 1; rank <= expectedLevels.Length; rank++)
        {
            var expected = expectedLevels[rank - 1];
            var level = perk.PerkLevels[rank];

            level.Price.Should().Be(expected.Price);
            level.Description.Should().Be(expected.Description);
            level.StatBonuses.Should().BeEmpty();

            var requirement = level.Requirements
                .OfType<PerkRequirementSkill>()
                .Should()
                .ContainSingle()
                .Which;
            requirement.Type.Should().Be(SkillType.Fabrication);
            requirement.RequiredRank.Should().Be(expected.SkillRank);

            if (rank == 1)
                level.GrantedFeats.Should().ContainSingle().Which.Should().Be(traitFeat);
            else
                level.GrantedFeats.Should().BeEmpty();
        }
    }

    private static Dictionary<PerkType, PerkDetail> BuildResearchPerksWithout2daLookup()
    {
        var definition = new FabricationPerkDefinition();
        var methodNames = new[]
        {
            "Research",
            "ScientificNetworking",
            "ResearchProjects"
        };

        foreach (var methodName in methodNames)
        {
            typeof(FabricationPerkDefinition)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(FabricationPerkDefinition)
            .GetField("_builder", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(definition);

        return (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
            .GetField("_perks", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(builder)!;
    }

    private static string ReadSource(params string[] relativePath)
    {
        var path = relativePath.Aggregate(
            Path.Combine(FindRepositoryRoot().FullName, "SWLOR.Game.Server"),
            Path.Combine);
        return File.ReadAllText(path);
    }

    private static string ExtractMethod(string source, string signature)
    {
        var signatureIndex = source.IndexOf(signature, StringComparison.Ordinal);
        signatureIndex.Should().BeGreaterThanOrEqualTo(0);

        var openBraceIndex = source.IndexOf('{', signatureIndex);
        openBraceIndex.Should().BeGreaterThanOrEqualTo(0);

        var depth = 0;
        for (var index = openBraceIndex; index < source.Length; index++)
        {
            if (source[index] == '{')
            {
                depth++;
            }
            else if (source[index] == '}')
            {
                depth--;
                if (depth == 0)
                    return source.Substring(signatureIndex, index - signatureIndex + 1);
            }
        }

        throw new InvalidOperationException($"Could not extract method '{signature}'.");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

        while (directory != null &&
               !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("the repository root containing SWLOR.Game.Server.sln must be discoverable");
        return directory;
    }
}
