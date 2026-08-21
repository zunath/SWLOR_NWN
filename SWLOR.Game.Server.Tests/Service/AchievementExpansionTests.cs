using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.AchievementService;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.FishingService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.TaxiService;
using Player = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.Tests.Service;

public sealed class AchievementExpansionTests
{
    [Test]
    public void NewAchievements_AreActiveAndUseConsecutiveStableIds()
    {
        var achievements = Enum.GetValues<AchievementType>()
            .Where(type => (int)type is >= 181 and <= 210)
            .ToArray();

        achievements.Select(type => (int)type).Should().Equal(Enumerable.Range(181, 30));
        achievements.Should().HaveCount(30);

        foreach (var achievement in achievements)
        {
            var detail = typeof(AchievementType)
                .GetMember(achievement.ToString())
                .Single()
                .GetCustomAttribute<AchievementAttribute>();

            detail.Should().NotBeNull();
            detail!.IsActive.Should().BeTrue();
            detail.Name.Should().NotBeNullOrWhiteSpace();
            detail.Description.Should().NotBeNullOrWhiteSpace();
        }
    }

    [TestCase(FishingLocationType.ViscaraLake, PlanetType.Viscara)]
    [TestCase(FishingLocationType.MonCalaDacCitySurface, PlanetType.MonCala)]
    [TestCase(FishingLocationType.HutlarQionTundra, PlanetType.Hutlar)]
    [TestCase(FishingLocationType.TatooineBabySarlaccCave, PlanetType.Tatooine)]
    [TestCase(FishingLocationType.DathomirGrottos, PlanetType.Dathomir)]
    [TestCase(FishingLocationType.DantooineLake, PlanetType.Dantooine)]
    [TestCase(FishingLocationType.Invalid, PlanetType.Invalid)]
    public void FishingLocations_MapToTheirPlanets(FishingLocationType location, PlanetType expected)
    {
        AchievementTracking.GetFishingPlanet(location).Should().Be(expected);
    }

    [Test]
    public void FullyOperational_RequiresEveryDroidPartAndAnActiveInstruction()
    {
        var droid = new ConstructedDroid
        {
            SerializedCPU = "cpu",
            SerializedHead = "head",
            SerializedBody = "body",
            SerializedArms = "arms",
            SerializedLegs = "legs",
            ActivePerks = { new DroidPerk() }
        };

        AchievementTracking.IsFullyOperational(droid).Should().BeTrue();
        droid.SerializedArms = string.Empty;
        AchievementTracking.IsFullyOperational(droid).Should().BeFalse();
    }

    [Test]
    public void CompleteModuleSet_RequiresAllThreePowerCategories()
    {
        var status = new ShipStatus();
        status.HighPowerModules[1] = new ShipStatus.ShipStatusModule();
        status.LowPowerModules[1] = new ShipStatus.ShipStatusModule();

        AchievementTracking.HasCompleteModuleSet(status).Should().BeFalse();

        status.ConfigurationModules[1] = new ShipStatus.ShipStatusModule();
        AchievementTracking.HasCompleteModuleSet(status).Should().BeTrue();
    }

    [TestCase(100, 10, true)]
    [TestCase(100, 11, false)]
    [TestCase(0, 0, false)]
    public void LowHull_UsesTenPercentInclusive(int maximum, int current, bool expected)
    {
        AchievementTracking.IsLowHull(new ShipStatus { MaxHull = maximum, Hull = current })
            .Should().Be(expected);
    }

    [TestCase(30, 30, 24, true)]
    [TestCase(30, 29, 23, false)]
    [TestCase(30, 30, 30, false)]
    [TestCase(30, 40, 30, false)]
    public void GuardAchievement_RequiresGuardToChangeALethalHitIntoSurvival(
        int hitPoints,
        int incomingDamage,
        int adjustedDamage,
        bool expected)
    {
        AchievementTracking.GuardPreventedLethalHit(hitPoints, incomingDamage, adjustedDamage)
            .Should().Be(expected);
    }

    [Test]
    public void LocalKnowledge_RequiresEveryTaxiDestination()
    {
        var player = new Player();
        player.TaxiDestinations[1] = Enum.GetValues<TaxiDestinationType>()
            .Where(type => type != TaxiDestinationType.Invalid)
            .ToList();

        AchievementTracking.HasAllTaxiDestinations(player).Should().BeTrue();
        player.TaxiDestinations[1].Remove(TaxiDestinationType.DantooineMedical);
        AchievementTracking.HasAllTaxiDestinations(player).Should().BeFalse();
    }

    [Test]
    public void GuildBreadth_RequiresRankOneInEveryActiveGuild()
    {
        var player = new Player();
        foreach (var guild in Enum.GetValues<GuildType>().Where(type => type != GuildType.Invalid))
            player.Guilds[guild] = new PlayerGuild { Rank = 1 };

        AchievementTracking.HasGuildBreadth(player).Should().BeTrue();
        player.Guilds[GuildType.AgricultureGuild].Rank = 0;
        AchievementTracking.HasGuildBreadth(player).Should().BeFalse();
    }

    [Test]
    public void Polyglot_RequiresThreeNonBasicLanguagesAtRankTwenty()
    {
        var player = new Player();
        player.Skills[SkillType.Basic] = new PlayerSkill { Rank = 20 };
        player.Skills[SkillType.Bothese] = new PlayerSkill { Rank = 20 };
        player.Skills[SkillType.Cheunh] = new PlayerSkill { Rank = 20 };
        player.Skills[SkillType.Dosh] = new PlayerSkill { Rank = 19 };

        AchievementTracking.HasThreeMasteredLanguages(player).Should().BeFalse();
        player.Skills[SkillType.Dosh].Rank = 20;
        AchievementTracking.HasThreeMasteredLanguages(player).Should().BeTrue();
    }
}
