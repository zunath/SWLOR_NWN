using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.AchievementService;
using SWLOR.Game.Server.Service.KeyItemService;

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
    [TestCase("eshanorbit", AchievementType.ExploreEshanOrbit,
        "Explore Eshan Orbit", "Explore the space surrounding Eshan.")]
    [TestCase("pw_sc_es_keshhei", AchievementType.ExploreEshanKeshanHeights,
        "Explore Keshan Heights", "Explore Keshan Heights on Eshan.")]
    [TestCase("pw_sc_eshanwilds", AchievementType.ExploreEshanSilverwoodExpanse,
        "Explore Silverwood Expanse", "Explore the Silverwood Expanse on Eshan.")]
    [TestCase("pw_ar_sc_eshanci", AchievementType.ExploreEshanSilverGateDistrict,
        "Explore Silver Gate District", "Explore Eshan City's Silver Gate District.")]
    [TestCase("pw_sc_eskeshpeak", AchievementType.ExploreEshanKeshanPeaks,
        "Explore Keshan Peaks", "Explore the Keshan Peaks on Eshan.")]
    [TestCase("pw_ar_sc_eshancm", AchievementType.ExploreEshanHighcrestQuarter,
        "Explore Highcrest Quarter", "Explore Eshan City's Highcrest Quarter.")]
    [TestCase("pw_sc_es_cavesh", AchievementType.ExploreEshanShimmerdeep,
        "Explore the Shimmerdeep", "Explore the Shimmerdeep on Eshan.")]
    [TestCase("sc_eshfarmland", AchievementType.ExploreEshanFarmlands,
        "Explore Eshan Farmlands", "Explore the farmlands on Eshan.")]
    [TestCase("pw_sc_eshbattle", AchievementType.ExploreEshanBattlegrounds,
        "Explore Eshan Battlegrounds", "Explore the battlegrounds on Eshan.")]
    [TestCase("pw_sc_eshanfield", AchievementType.ExploreEshanHearthWard,
        "Explore Hearth Ward", "Explore Eshan City's Hearth Ward.")]
    [TestCase("pwsc_eshstarport", AchievementType.ExploreEshanStarport,
        "Explore Eshan Starport", "Explore the starport on Eshan.")]
    [TestCase("pw_ar_sc_eshanto", AchievementType.ExploreEshanVerdantCrown,
        "Explore Verdant Crown", "Explore Eshan City's Verdant Crown.")]
    [TestCase("pw_sc_esh_riverw", AchievementType.ExploreEshanRiverway,
        "Explore the Riverway", "Explore Eshan City's Riverway.")]
    [TestCase("pw_sc_es_plains", AchievementType.ExploreEshanWinterMarches,
        "Explore the Winter Marches", "Explore the Winter Marches on Eshan.")]
    [TestCase("pw_sc_es_oldgate", AchievementType.ExploreEshanOldQuarter,
        "Explore the Old Quarter", "Explore Eshan City's Old Quarter.")]
    [TestCase("sc_eswildlandn", AchievementType.ExploreEshanVeylanGroves,
        "Explore the Veylan Groves", "Explore the Veylan Groves on Eshan.")]
    [TestCase("pw_sc_esmountd", AchievementType.ExploreEshanGrandProvingGrounds,
        "Explore the Grand Proving Grounds", "Explore the Grand Proving Grounds on Eshan.")]
    [TestCase("sc_esscraplands", AchievementType.ExploreEshanScraplands,
        "Explore the Scraplands", "Explore the Scraplands on Eshan.")]
    [TestCase("pw_sc_esctunnel", AchievementType.ExploreEshanCityTunnels,
        "Explore Eshan City Tunnels", "Explore the tunnels beneath Eshan City.")]
    [TestCase("pw_sc_esvein", AchievementType.ExploreEshanShattervein,
        "Explore the Shattervein", "Explore the Shattervein on Eshan.")]
    [TestCase("pw_sc_es_scrcave", AchievementType.ExploreEshanScraplandsCaves,
        "Explore the Scraplands Caves", "Explore the caves beneath Eshan's Scraplands.")]
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

    [TestCase("eshanorbit", KeyItemType.EshanOrbitMap, "esh_map_orbit")]
    [TestCase("pw_sc_es_keshhei", KeyItemType.EshanKeshanHeightsMap, "esh_map_height")]
    [TestCase("pw_sc_eshanwilds", KeyItemType.EshanSilverwoodExpanseMap, "esh_map_silver")]
    [TestCase("pw_ar_sc_eshanci", KeyItemType.EshanSilverGateDistrictMap, "esh_map_gate")]
    [TestCase("pw_sc_eskeshpeak", KeyItemType.EshanKeshanPeaksMap, "esh_map_peaks")]
    [TestCase("pw_ar_sc_eshancm", KeyItemType.EshanHighcrestQuarterMap, "esh_map_high")]
    [TestCase("pw_sc_es_cavesh", KeyItemType.EshanShimmerdeepMap, "esh_map_shimmer")]
    [TestCase("sc_eshfarmland", KeyItemType.EshanFarmlandsMap, "esh_map_farms")]
    [TestCase("pw_sc_eshbattle", KeyItemType.EshanBattlegroundsMap, "esh_map_battle")]
    [TestCase("pw_sc_eshanfield", KeyItemType.EshanHearthWardMap, "esh_map_hearth")]
    [TestCase("pwsc_eshstarport", KeyItemType.EshanStarportMap, "esh_map_starport")]
    [TestCase("pw_ar_sc_eshanto", KeyItemType.EshanVerdantCrownMap, "esh_map_verdant")]
    [TestCase("pw_sc_esh_riverw", KeyItemType.EshanRiverwayMap, "esh_map_river")]
    [TestCase("pw_sc_es_plains", KeyItemType.EshanWinterMarchesMap, "esh_map_marches")]
    [TestCase("pw_sc_es_oldgate", KeyItemType.EshanOldQuarterMap, "esh_map_oldquart")]
    [TestCase("sc_eswildlandn", KeyItemType.EshanVeylanGrovesMap, "esh_map_groves")]
    [TestCase("pw_sc_esmountd", KeyItemType.EshanGrandProvingGroundsMap, "esh_map_proving")]
    [TestCase("sc_esscraplands", KeyItemType.EshanScraplandsMap, "esh_map_scrap")]
    [TestCase("pw_sc_esctunnel", KeyItemType.EshanCityTunnelsMap, "esh_map_tunnels")]
    [TestCase("pw_sc_esvein", KeyItemType.EshanShatterveinMap, "esh_map_vein")]
    [TestCase("pw_sc_es_scrcave", KeyItemType.EshanScraplandsCavesMap, "esh_map_scrcave")]
    public void EshanAreaMap_HasMatchingMapItem(
        string areaResref,
        KeyItemType expectedMap,
        string mapItemResref)
    {
        using var area = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            FindRepositoryRoot().FullName, "Module", "git", $"{areaResref}.git.json")));
        var mapVariable = area.RootElement
            .GetProperty("VarTable")
            .GetProperty("value")
            .EnumerateArray()
            .Single(variable => variable.GetProperty("Name").GetProperty("value").GetString()
                                == "MAP_KEY_ITEM_ID");

        mapVariable.GetProperty("Value").GetProperty("value").GetInt32()
            .Should().Be((int)expectedMap);

        using var item = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            FindRepositoryRoot().FullName, "Module", "uti", $"{mapItemResref}.uti.json")));
        item.RootElement.GetProperty("TemplateResRef").GetProperty("value").GetString()
            .Should().Be(mapItemResref);
        item.RootElement.GetProperty("VarTable").GetProperty("value")[0]
            .GetProperty("Value").GetProperty("value").GetInt32()
            .Should().Be((int)expectedMap);
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
