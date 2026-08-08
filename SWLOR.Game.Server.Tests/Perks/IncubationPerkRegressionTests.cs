using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class IncubationPerkRegressionTests
{
    [Test]
    public void IncubationPerkIds_RemainCompatibleWithPersistedPlayerData()
    {
        ((int)PerkType.IncubationManagement).Should().Be(254);
        ((int)PerkType.DNAManipulation).Should().Be(272);
        ((int)PerkType.IncubationProcessing).Should().Be(273);
        ((int)PerkType.ErraticGenius).Should().Be(274);
    }

    [Test]
    public void IncubationPerkDefinitions_PreserveLegacyRanks()
    {
        var perks = BuildIncubationPerksWithout2daLookup();

        AssertPerk(
            perks[PerkType.DNAManipulation],
            "DNA Manipulation",
            FeatType.DNAManipulationTrait,
            (2, 5, "Enables you to harvest DNA from creatures between levels 0 and 10 and use incubators."),
            (2, 15, "Enables you to harvest DNA from creatures between levels 0 and 20."),
            (2, 25, "Enables you to harvest DNA from creatures between levels 0 and 30."),
            (3, 35, "Enables you to harvest DNA from creatures between levels 0 and 40."),
            (3, 45, "Enables you to harvest DNA from creatures between levels 0 and 50."));

        AssertPerk(
            perks[PerkType.IncubationProcessing],
            "Incubation Processing",
            FeatType.IncubationProcessingTrait,
            (2, 15, "Reduces incubation time by 10%."),
            (2, 25, "Reduces incubation time by 20%."),
            (3, 35, "Reduces incubation time by 30%."),
            (3, 45, "Reduces incubation time by 40%."));

        AssertPerk(
            perks[PerkType.ErraticGenius],
            "Erratic Genius",
            FeatType.ErraticGeniusTrait,
            (2, 20, "Increases the mutation chance by 2%."),
            (3, 30, "Increases the mutation chance by 4%."),
            (3, 40, "Increases the mutation chance by 8%."));

        AssertPerk(
            perks[PerkType.IncubationManagement],
            "Incubation Management",
            FeatType.IncubationManagementTrait,
            (2, 25, "Increases the maximum number of concurrent incubation jobs by 1, for a total of 2."),
            (3, 50, "Increases the maximum number of concurrent incubation jobs by 1, for a total of 3."));
    }

    [Test]
    public void DNAManipulation_PreservesIncubatorAccessAndHarvestLevelGates()
    {
        var extractor = ReadSource("Feature", "ItemDefinition", "DNAExtractorItemDefinition.cs");
        var beastMastery = ReadSource("Service", "BeastMastery.cs");
        var useIncubator = ExtractMethod(beastMastery, "public static void UseIncubator()");

        extractor.Should().Contain("var perkLevel = Perk.GetPerkLevel(user, PerkType.DNAManipulation);");
        extractor.Should().Contain("var maxLevel = perkLevel * 10;");
        extractor.Should().Contain("if (level > maxLevel)");
        extractor.Should().Contain("Insufficient 'DNA Manipulation' perk level.");

        useIncubator.Should().Contain("Perk.GetPerkLevel(player, PerkType.DNAManipulation)");
        useIncubator.Should().Contain("if (dnaManipulationLevel <= 0)");
        useIncubator.Should().Contain("Perk 'DNA Manipulation I' is required to use incubators.");
    }

    [TestCase(0, 10, 129600)]
    [TestCase(1, 10, 116641)]
    [TestCase(2, 10, 103681)]
    [TestCase(3, 10, 90721)]
    [TestCase(4, 10, 77761)]
    [TestCase(4, 30, 64800)]
    [TestCase(4, 50, 64800)]
    public void IncubationProcessing_PreservesLegacyStageDurations(
        int rank,
        int socialScore,
        int expectedSeconds)
    {
        CalculateLegacyStageDurationSeconds(rank, socialScore).Should().Be(expectedSeconds);

        var incubator = ReadSource("Feature", "GuiDefinition", "ViewModel", "IncubatorViewModel.cs");
        var calculation = ExtractMethod(incubator, "private int CalculateIncubationSeconds()");
        calculation.Should().Contain("Perk.GetPerkLevel(Player, PerkType.IncubationProcessing) * 10 + socialBonus");
        calculation.Should().Contain("BaseSecondsBetweenStages * timeReductionPercentage");
    }

    [Test]
    public void ErraticGenius_PreservesRankBonusesAndFirstStageOnlyApplication()
    {
        var incubator = ReadSource("Feature", "GuiDefinition", "ViewModel", "IncubatorViewModel.cs");
        var bonus = ExtractMethod(incubator, "private int GetErraticGeniusBonus()");
        var startJob = ExtractMethod(incubator, "private void StartJob(IncubationJob job)");

        bonus.Should().Contain("case 1:");
        bonus.Should().Contain("mutationBonus = 2;");
        bonus.Should().Contain("case 2:");
        bonus.Should().Contain("mutationBonus = 4;");
        bonus.Should().Contain("case 3:");
        bonus.Should().Contain("mutationBonus = 8;");
        startJob.Should().Contain("job.CurrentStage <= 0 && IsErraticGeniusChecked");
        startJob.Should().Contain("GetErraticGeniusBonus() : 0) * 10");
    }

    [Test]
    public void IncubationManagement_PreservesOnePlusRankConcurrentJobLimit()
    {
        var incubator = ReadSource("Feature", "GuiDefinition", "ViewModel", "IncubatorViewModel.cs");
        var validation = ExtractMethod(incubator, "private string ValidateCreateJob()");

        validation.Should().Contain("Perk.GetPerkLevel(Player, PerkType.IncubationManagement) + 1");
        validation.Should().Contain("currentJobs.Count(x => x.ParentPropertyId != _incubatorPropertyId)");
        validation.Should().Contain("if (currentJobCount >= maxConcurrentJobs)");
    }

    [Test]
    public void Incubator_RefreshesDisplayedPerkEffectsAfterPurchasesAndRefunds()
    {
        var incubator = ReadSource("Feature", "GuiDefinition", "ViewModel", "IncubatorViewModel.cs");
        incubator.Should().Contain("IGuiRefreshable<PerkAcquiredRefreshEvent>");
        incubator.Should().Contain("IGuiRefreshable<PerkRefundedRefreshEvent>");

        foreach (var signature in new[]
                 {
                     "public void Refresh(PerkAcquiredRefreshEvent payload)",
                     "public void Refresh(PerkRefundedRefreshEvent payload)"
                 })
        {
            var refresh = ExtractMethod(incubator, signature);
            refresh.Should().Contain("LoadPlayerStats();");
            refresh.Should().Contain("RefreshAllStats();");
            refresh.Should().Contain("RefreshIncubationTime();");
        }
    }

    private static int CalculateLegacyStageDurationSeconds(int incubationProcessingRank, int socialScore)
    {
        const int BaseSecondsBetweenStages = 129600;
        var social = socialScore - 10;
        var socialBonus = 0.5f * (social <= 0 ? 0 : social);
        if (socialBonus > 10)
            socialBonus = 10;

        var timeReductionPercentage = 0.01f * (incubationProcessingRank * 10 + socialBonus);
        return BaseSecondsBetweenStages - (int)(BaseSecondsBetweenStages * timeReductionPercentage);
    }

    private static void AssertPerk(
        PerkDetail perk,
        string name,
        FeatType traitFeat,
        params (int Price, int SkillRank, string Description)[] expectedLevels)
    {
        perk.Category.Should().Be(PerkCategoryType.BeastMasteryIncubation);
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
            requirement.Type.Should().Be(SkillType.BeastMastery);
            requirement.RequiredRank.Should().Be(expected.SkillRank);

            if (rank == 1)
                level.GrantedFeats.Should().ContainSingle().Which.Should().Be(traitFeat);
            else
                level.GrantedFeats.Should().BeEmpty();
        }
    }

    private static Dictionary<PerkType, PerkDetail> BuildIncubationPerksWithout2daLookup()
    {
        var definition = new BeastMasteryPerkDefinition();
        var methodNames = new[]
        {
            "DNAManipulation",
            "IncubationProcessing",
            "ErraticGenius",
            "IncubationManagement"
        };

        foreach (var methodName in methodNames)
        {
            typeof(BeastMasteryPerkDefinition)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(BeastMasteryPerkDefinition)
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
