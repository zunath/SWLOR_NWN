using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.AchievementService;

namespace SWLOR.Game.Server.Tests.Feature;

public class ExplorationAchievementTests
{
    [TestCase("viscara_forkwest", AchievementType.ExploreViscaraCrossroadsWest,
        "Explore Crossroads West", "Explore Crossroads West on Viscara.")]
    [TestCase("viscara_mountasc", AchievementType.ExploreViscaraMountainAscent,
        "Explore Mountain Ascent", "Explore the Mountain Ascent on Viscara.")]
    [TestCase("pw_ar_nscasino", AchievementType.ExploreSmugglersMoonCasino,
        "Smuggler's Moon - Casino", "Explore the Casino on Smuggler's Moon.")]
    [TestCase("pw_ar_narcatwalk", AchievementType.ExploreSmugglersMoonCatwalks,
        "Smuggler's Moon - Catwalks", "Explore the Catwalks on Smuggler's Moon.")]
    public void AreaExploration_GrantsTheCorrectActiveAchievement(
        string areaResref,
        AchievementType expectedAchievement,
        string expectedName,
        string expectedDescription)
    {
        using var area = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            FindRepositoryRoot().FullName, "Module", "git", $"{areaResref}.git.json")));
        var achievementVariable = area.RootElement
            .GetProperty("VarTable")
            .GetProperty("value")
            .EnumerateArray()
            .Single(variable => variable.GetProperty("Name").GetProperty("value").GetString()
                                == "EXPLORE_ACHIEVEMENT_ID");

        achievementVariable.GetProperty("Type").GetProperty("value").GetInt32()
            .Should().Be(1, "the area entry handler reads an integer local variable");
        var achievement = (AchievementType)achievementVariable
            .GetProperty("Value").GetProperty("value").GetInt32();
        achievement.Should().Be(expectedAchievement);

        var detail = achievement.GetAttribute<AchievementType, AchievementAttribute>();
        detail.IsActive.Should().BeTrue();
        detail.Name.Should().Be(expectedName);
        detail.Description.Should().Be(expectedDescription);
    }

    [Test]
    public void ViscaraExploration_DoesNotSharePersistedIdsWithSmugglersMoon()
    {
        ((int)AchievementType.ExploreSmugglersMoonCasino).Should().Be(121);
        ((int)AchievementType.ExploreSmugglersMoonCatwalks).Should().Be(122);
        ((int)AchievementType.ExploreViscaraCrossroadsWest).Should().Be(181);
        ((int)AchievementType.ExploreViscaraMountainAscent).Should().Be(182);

        Enum.GetValues<AchievementType>().Select(achievement => (int)achievement)
            .Should().OnlyHaveUniqueItems("achievement IDs are persisted on player accounts");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
        {
            directory = directory.Parent;
        }

        return directory ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
