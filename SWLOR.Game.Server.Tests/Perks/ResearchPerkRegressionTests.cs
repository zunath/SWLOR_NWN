using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.PerkService;

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
    public void Research_GatesTerminalAccessAndRecipeTiersByFabricationRank()
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

        useResearchTerminal.Should().Contain("if (CraftRankMilestones.GetResearchTier(player) <= 0)");
        useResearchTerminal.Should().Contain("Fabrication rank {CraftRankMilestones.RequiredFabricationRankForResearchTier(1)} is required to use research terminals.");
        useResearchTerminal.Should().NotContain("Perk");

        canResearchRecipe.Should().Contain("var tier = recipe.Level / 10 + 1;");
        canResearchRecipe.Should().Contain("if (tier > 5)");
        canResearchRecipe.Should().Contain("CraftRankMilestones.GetResearchTier(player) >= tier");

        buildJob.Should().Contain("var researchTier = recipe.Level / 10 + 1;");
        buildJob.Should().Contain("if (researchTier > 5)");
        buildJob.Should().Contain("RequiredResearchTier = researchTier");
        validateJob.Should().Contain("CraftRankMilestones.GetResearchTier(Player) < researchJob.RequiredResearchTier");
        validateJob.Should().Contain("CraftRankMilestones.RequiredFabricationRankForResearchTier(researchJob.RequiredResearchTier)");
        validateJob.Should().Contain("researchJob.CurrentLevel >= Craft.MaxResearchLevel");

        validateBlueprint.Should().Contain("var requiredTier = recipe.Level / 10 + 1;");
        validateBlueprint.Should().Contain("if (requiredTier > 5)");
        validateBlueprint.Should().Contain("if (researchTier < requiredTier)");
        validateBlueprint.Should().Contain("CraftRankMilestones.RequiredFabricationRankForResearchTier(requiredTier)");
        validateBlueprint.Should().Contain("blueprint.Level >= Craft.MaxResearchLevel");
    }

    [Test]
    public void ResearchTerminalMessages_NameResearchJobsAndNoRetiredPerks()
    {
        var craft = ReadSource("Service", "Craft.cs");
        var useResearchTerminal = ExtractMethod(craft, "public static void UseResearchTerminal()");

        useResearchTerminal.Should().Contain("Another player's research job is active. This job will complete in: {completionTime}.");
        useResearchTerminal.Should().Contain("Another player's research job is active. This job has completed.");
        useResearchTerminal.Should().NotContain("incubation");
        useResearchTerminal.Should().NotContain("Perk '");
    }

    [Test]
    public void ScientificNetworking_AddsLicensedRunsFromFabricationRank()
    {
        var researchViewModel = ReadSource("Feature", "GuiDefinition", "ViewModel", "ResearchViewModel.cs");
        var buildJob = ExtractMethod(
            researchViewModel,
            "private ResearchJobDetails BuildResearchJobDetails(RecipeType recipeType, uint blueprintItem)");
        var completeJob = ExtractMethod(researchViewModel, "public Action ClickCompleteJob() => () =>");

        buildJob.Should().Contain("var licensedRunsMinimum = 1 + CraftRankMilestones.GetBlueprintLicensedRunBonus(Player);");
        buildJob.Should().Contain("var licensedRunsMaximum = licensedRunsMinimum + 2;");
        completeJob.Should().Contain("var licensedRunBonus = CraftRankMilestones.GetBlueprintLicensedRunBonus(Player);");
        completeJob.Should().Contain("blueprintDetails.LicensedRuns = Random.D3(1) + licensedRunBonus;");
    }

    [Test]
    public void ResearchProjects_LimitsConcurrentJobsByFabricationRank()
    {
        var researchViewModel = ReadSource("Feature", "GuiDefinition", "ViewModel", "ResearchViewModel.cs");
        var validation = ExtractMethod(researchViewModel, "private string ValidateJob()");

        validation.Should().Contain("var maxConcurrentJobs = CraftRankMilestones.GetMaxConcurrentResearchJobs(Player);");
        validation.Should().Contain("currentJobs.Count(x => x.ParentPropertyId != _researchTerminalPropertyId)");
        validation.Should().Contain("if (currentJobCount >= maxConcurrentJobs)");
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
