using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class LeadershipCivicPerkRegressionTests
{
    [Test]
    public void CivicPerkIds_RemainCompatibleWithPersistedPlayerData()
    {
        ((int)PerkType.CityManagement).Should().Be(131);
        ((int)PerkType.Upkeep).Should().Be(134);
        ((int)PerkType.GuildRelations).Should().Be(219);
    }

    [Test]
    public void CivicPerkDefinitions_MatchCurrentApprovedRanks()
    {
        var perks = BuildCivicPerksWithout2daLookup();

        AssertPerk(
            perks[PerkType.CityManagement],
            "City Management",
            FeatType.CityManagementTrait,
            (2, 5, "Enables you to become mayor of a city. You can manage cities up to rank 2 (Village)."),
            (3, 10, "You can manage cities up to rank 3 (Township)."),
            (4, 15, "You can manage cities up to rank 4 (City)."),
            (5, 20, "You can manage cities up to rank 5 (Metropolis)."));

        AssertPerk(
            perks[PerkType.Upkeep],
            "Upkeep",
            FeatType.UpkeepTrait,
            (3, 10, "Weekly maintenance fees are reduced by 5%."),
            (4, 20, "Weekly maintenance fees are reduced by 10%."));

        AssertPerk(
            perks[PerkType.GuildRelations],
            "Guild Relations",
            FeatType.GuildRelationsTrait,
            (2, 5, "Improves GP and credit rewards from guild tasks by 5%."),
            (2, 10, "Improves GP and credit rewards from guild tasks by 10%."),
            (3, 15, "Improves GP and credit rewards from guild tasks by 15%."),
            (3, 20, "Improves GP and credit rewards from guild tasks by 20%."));

        perks[PerkType.CityManagement].RefundRequirement.Should().NotBeNull();
    }

    [TestCase(0, 1)]
    [TestCase(1, 2)]
    [TestCase(2, 3)]
    [TestCase(3, 4)]
    [TestCase(4, 5)]
    public void CityManagement_PreservesFoundingAndCityRankCaps(int perkRank, int expectedMaximumCityRank)
    {
        (perkRank + 1).Should().Be(expectedMaximumCityRank);

        var placeCityHall = ReadSource("Feature", "DialogDefinition", "PlaceCityHallDialog.cs");
        placeCityHall.Should().Contain("Perk.GetPerkLevel(player, PerkType.CityManagement) < 1");
        placeCityHall.Should().Contain("The City Management I perk is required to establish a city.");

        var property = ReadSource("Service", "Property.cs");
        var processCityLevel = ExtractMethod(property, "private static void ProcessCityLevel(WorldProperty city)");
        processCityLevel.Should().Contain("mayor.Perks[PerkType.CityManagement] + 1");
        processCityLevel.Should().Contain("if (mayorLevel < currentLevel)");
        processCityLevel.Should().Contain("for (var level = 1; level <= 5; level++)");
        processCityLevel.Should().Contain("if (level > mayorLevel)");
        processCityLevel.Should().Contain("if (citizenCount >= _citizensRequired[level])");

        var manageCity = ReadSource("Feature", "GuiDefinition", "ViewModel", "ManageCityViewModel.cs");
        var validateUpgrade = ExtractMethod(
            manageCity,
            "private bool ValidateUpgrade(PropertyUpgradeType upgradeType, int price)");
        validateUpgrade.Should().Contain("mayor.Perks[PerkType.CityManagement]");
        validateUpgrade.Should().Contain("if (mayorPerkLevel + 1 <= currentLevel)");
        validateUpgrade.Should().Contain("Mayor city management perk too low.");
    }

    [Test]
    public void CityManagement_PreservesMayorAndElectionRefundProtection()
    {
        var leadership = ReadSource("Feature", "PerkDefinition", "LeadershipPerkDefinition.cs");
        var cityManagement = ExtractMethod(leadership, "private void CityManagement()");

        cityManagement.Should().Contain(".RefundRequirement((player) =>");
        cityManagement.Should().Contain("if (dbCity.OwnerPlayerId == dbPlayer.Id)");
        cityManagement.Should().Contain("You are the mayor of a city. You cannot refund this perk until you abdicate your position.");
        cityManagement.Should().Contain("dbElection.CandidatePlayerIds.Contains(playerId)");
        cityManagement.Should().Contain("You are currently running for election. You cannot refund this perk until you withdraw from the race.");
    }

    [TestCase(0, 100, 700)]
    [TestCase(1, 100, 665)]
    [TestCase(2, 100, 630)]
    [TestCase(2, 101, 637)]
    public void Upkeep_PreservesFivePercentPerRankBaseMaintenanceReduction(
        int perkRank,
        int dailyLayoutPrice,
        int expectedWeeklyBasePrice)
    {
        CalculateDiscountedWeeklyBasePrice(perkRank, dailyLayoutPrice).Should().Be(expectedWeeklyBasePrice);

        var property = ReadSource("Service", "Property.cs");
        var processUpkeep = ExtractMethod(property, "private static void ProcessUpkeep(DateTime now, WorldProperty city)");
        processUpkeep.Should().Contain("dbMayor.Perks[PerkType.Upkeep] * 0.05f");
        processUpkeep.Should().Contain("var basePrice = layout.PricePerDay * 7;");
        processUpkeep.Should().Contain("basePrice -= (int)(basePrice * upkeepReductionPercent);");
        processUpkeep.Should().Contain("const int UpgradeBasePrice = 10000;");
        processUpkeep.Should().Contain("city.Upkeep += basePrice + upgradePrice;");
        processUpkeep.IndexOf("basePrice -=", StringComparison.Ordinal)
            .Should()
            .BeLessThan(processUpkeep.IndexOf("var upgradePrice", StringComparison.Ordinal));
    }

    [TestCase(0, 1000, 0)]
    [TestCase(1, 1000, 50)]
    [TestCase(2, 1000, 100)]
    [TestCase(3, 1000, 150)]
    [TestCase(4, 1000, 200)]
    public void GuildRelations_PreservesFivePercentPerRankGuildRewards(
        int perkRank,
        int baseAmount,
        int expectedBonus)
    {
        CalculateGuildRelationsBonus(perkRank, baseAmount).Should().Be(expectedBonus);

        var guild = ReadSource("Service", "Guild.cs");
        var calculateGP = ExtractMethod(
            guild,
            "public static int CalculateGPReward(uint player, GuildType guild, int baseAmount)");
        calculateGP.Should().Contain("Perk.GetPerkLevel(player, PerkType.GuildRelations) * 0.05f");
        calculateGP.Should().Contain("var rankBonus = 0.25f * dbGuild.Rank;");
        calculateGP.Should().Contain("GetAbilityModifier(AbilityType.Social, player) * 0.05f");
        calculateGP.Should().Contain("(perkBonus * baseAmount)");
        calculateGP.Should().Contain("return (int)amount;");

        var quest = ReadSource("Service", "Quest.cs");
        var calculateCredits = ExtractMethod(
            quest,
            "public static int CalculateQuestGoldReward(uint player, bool isGuildQuest, int baseAmount)");
        calculateCredits.Should().Contain("if (isGuildQuest)");
        calculateCredits.Should().Contain("Perk.GetPerkLevel(player, PerkType.GuildRelations)");
        calculateCredits.Should().Contain("guildRelations = perkLevel * 0.05f;");
        calculateCredits.Should().Contain("(int)(baseAmount * guildRelations)");
    }

    private static int CalculateDiscountedWeeklyBasePrice(int upkeepRank, int dailyLayoutPrice)
    {
        var basePrice = dailyLayoutPrice * 7;
        var upkeepReductionPercent = upkeepRank * 0.05f;
        return basePrice - (int)(basePrice * upkeepReductionPercent);
    }

    private static int CalculateGuildRelationsBonus(int guildRelationsRank, int baseAmount)
    {
        return (int)(baseAmount * guildRelationsRank * 0.05f);
    }

    private static void AssertPerk(
        PerkDetail perk,
        string name,
        FeatType traitFeat,
        params (int Price, int SkillRank, string Description)[] expectedLevels)
    {
        perk.Category.Should().Be(PerkCategoryType.Leadership);
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
            requirement.Type.Should().Be(SkillType.Leadership);
            requirement.RequiredRank.Should().Be(expected.SkillRank);

            if (rank == 1)
                level.GrantedFeats.Should().ContainSingle().Which.Should().Be(traitFeat);
            else
                level.GrantedFeats.Should().BeEmpty();
        }
    }

    private static Dictionary<PerkType, PerkDetail> BuildCivicPerksWithout2daLookup()
    {
        var definition = new LeadershipPerkDefinition();
        var methodNames = new[]
        {
            "CityManagement",
            "Upkeep",
            "GuildRelations"
        };

        foreach (var methodName in methodNames)
        {
            typeof(LeadershipPerkDefinition)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(definition, null);
        }

        var builder = typeof(LeadershipPerkDefinition)
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
