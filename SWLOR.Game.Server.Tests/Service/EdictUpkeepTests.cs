using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.EdictService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Edict costs on a city's weekly upkeep: the sum and the itemized notice, which the upkeep perk never discounts, and
/// the places the weekly check and the rest of the city code call them.
/// </summary>
public class EdictUpkeepTests
{
    private static readonly EdictBillLine Festival = new(EdictType.FestivalSeason, "Festival Season", 10000);
    private static readonly EdictBillLine Guild = new(EdictType.GuildCharter, "Guild Charter", 15000);

    #region Bill

    [Test]
    public void EdictUpkeep_IsTheSumOfEachBilledEdictsWeeklyCost()
    {
        CityUpkeepRules.GetEdictUpkeep(new[] { Festival, Guild }).Should().Be(25000);
        CityUpkeepRules.GetEdictUpkeep(Array.Empty<EdictBillLine>()).Should().Be(0);
    }

    [Test]
    public void EdictCostsMatchThePlansVillageAndTownshipBudgets()
    {
        // The plan's finance table: a Village with Festival Season, and a Township with two Edicts.
        var village = CityUpkeepRules.GetWeeklyUpkeep(5000, 0, 1, 1, 1) + CityUpkeepRules.GetEdictUpkeep(new[] { Festival });
        var township = CityUpkeepRules.GetWeeklyUpkeep(5000, 0, 2, 2, 2)
                       + CityUpkeepRules.GetEdictUpkeep(new[]
                       {
                           new EdictBillLine(EdictType.ArtisansCharter, "Artisans' Charter", 10000),
                           new EdictBillLine(EdictType.ProspectorsCharter, "Prospectors' Charter", 10000)
                       });

        village.Should().Be(45000);
        township.Should().Be(85000);
    }

    #endregion

    #region Notice

    [Test]
    public void TheUpkeepNotice_ItemizesBaseEachEdictAndTheTotal()
    {
        var text = CityUpkeepRules.BuildUpkeepBreakdownText(35000, new[] { Festival, Guild }, Array.Empty<string>(), 60000);

        text.Split('\n').Should().Equal(
            "**Base and facilities**: 35,000 cr",
            "**Edict - Festival Season**: 10,000 cr",
            "**Edict - Guild Charter**: 15,000 cr",
            "**Total owed**: 60,000 cr");
    }

    [Test]
    public void TheUpkeepNotice_ListsEdictsThatWereNotBilledAndWhy()
    {
        var text = CityUpkeepRules.BuildUpkeepBreakdownText(
            35000,
            Array.Empty<EdictBillLine>(),
            new[] { "Hunting Grounds (city level too low)", "Alliance (waiting for the other city)" },
            35000);

        text.Should().Contain("**Edicts not billed**: Hunting Grounds (city level too low); Alliance (waiting for the other city)");
    }

    [Test]
    public void TheUpkeepNotice_IsPlainAsciiForTheGazette()
    {
        CityUpkeepRules.BuildUpkeepBreakdownText(35000, new[] { Festival }, new[] { "Embargo (city in disrepair)" }, 45000)
            .Should().MatchRegex("^[\\x20-\\x7E\\n]+$");
    }

    #endregion

    #region Wiring

    private static string ReadServer(params string[] path) =>
        File.ReadAllText(Path.Combine(new[] { RepositoryRoot, "SWLOR.Game.Server" }.Concat(path).ToArray()));

    private static string RepositoryRoot
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                directory = directory.Parent;
            return directory!.FullName;
        }
    }

    private static string ExtractMethod(string source, string signature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, signature);

        var depth = 0;
        for (var index = source.IndexOf('{', start); index < source.Length; index++)
        {
            if (source[index] == '{')
                depth++;
            else if (source[index] == '}' && --depth == 0)
                return source.Substring(start, index - start + 1);
        }

        throw new InvalidOperationException($"Could not extract '{signature}'.");
    }

    [Test]
    public void TheWeeklyCheck_AddsEdictCostsAfterDecidingDisrepairAndBeforeTheNoticeIsSent()
    {
        var processUpkeep = ExtractMethod(ReadServer("Service", "Property.cs"), "private static void ProcessUpkeep(DateTime now, WorldProperty city)");

        processUpkeep.Should().Contain("Edict.GetWeeklyBill(city)");
        processUpkeep.Should().Contain("city.Upkeep += CityUpkeepRules.GetEdictUpkeep(edictBill);");
        processUpkeep.Should().Contain("CityUpkeepRules.BuildUpkeepBreakdownText(");

        var disrepairCheck = processUpkeep.IndexOf("if (city.Upkeep > 0)", StringComparison.Ordinal);
        var billing = processUpkeep.IndexOf("Edict.GetWeeklyBill(city)", StringComparison.Ordinal);
        var addBase = processUpkeep.IndexOf("city.Upkeep += weeklyUpkeep;", StringComparison.Ordinal);
        var notice = processUpkeep.IndexOf("\"City Upkeep Due\"", StringComparison.Ordinal);
        disrepairCheck.Should().BeLessThan(billing, "an unpaid bill pauses the Edicts before they are billed");
        billing.Should().BeLessThan(addBase, "the bill is chosen before this week's charges are added");
        addBase.Should().BeLessThan(notice);
    }

    [Test]
    public void TheLevelCheck_AppliesLevelPausesAfterTheLevelIsSetWithoutBillingResumes()
    {
        var processCityLevel = ExtractMethod(ReadServer("Service", "Property.cs"), "private static void ProcessCityLevel(WorldProperty city)");

        var setLevel = processCityLevel.IndexOf("city.Upgrades[PropertyUpgradeType.CityLevel] = currentLevel;", StringComparison.Ordinal);
        var pauses = processCityLevel.IndexOf("Edict.ApplyCityLevelPauses(city, DateTime.UtcNow, billResumes: false);", StringComparison.Ordinal);
        setLevel.Should().BeGreaterThanOrEqualTo(0);
        pauses.Should().BeGreaterThan(setLevel, "the weekly check bills a resumed Edict itself");
    }

    [Test]
    public void TheWeeklyCheck_RunsTheLevelCheckBeforeTheBill()
    {
        var processCities = ExtractMethod(ReadServer("Service", "Property.cs"), "private static void ProcessCities()");

        processCities.IndexOf("ProcessCityLevel(city);", StringComparison.Ordinal)
            .Should().BeLessThan(processCities.IndexOf("ProcessUpkeep(now, city);", StringComparison.Ordinal));
    }

    [Test]
    public void PayingUpkeep_GoesThroughTheTreasuryService()
    {
        var payUpkeep = ExtractMethod(
            ReadServer("Feature", "GuiDefinition", "ViewModel", "ManageCityViewModel.cs"),
            "public Action PayUpkeep() => () =>");

        payUpkeep.Should().Contain("CityTreasuryRules.CanSpend(dbCity, dbCity.Upkeep)");
        payUpkeep.Should().Contain("CityTreasury.PayUpkeep(dbCity, DateTime.UtcNow);");
        payUpkeep.Should().NotContain("dbCity.Treasury -=");
    }

    [Test]
    public void TheTreasuryWithdrawal_StillReadsOnlyTheOrdinaryTreasury()
    {
        var changePrice = ExtractMethod(
            ReadServer("Feature", "GuiDefinition", "ViewModel", "ManageCityViewModel.cs"),
            "public void ChangePrice(string recordId, int amount)");

        changePrice.Should().Contain("dbCity.Treasury < amount");
        changePrice.Should().NotContain("RestrictedTreasury");
    }

    [Test]
    public void EdictCharges_GoThroughTheTreasuryService()
    {
        var enact = ExtractMethod(ReadServer("Service", "Edict.cs"), "public static EdictRefusal Enact(");

        enact.Should().Contain("CityTreasury.Spend(city, enactment.Charge)");
    }

    [Test]
    public void ADeletedCity_EndsTheAlliancesNamingIt()
    {
        var deleteProperty = ExtractMethod(ReadServer("Service", "Property.cs"), "public static void DeleteProperty(WorldProperty property)");

        deleteProperty.Should().Contain("Edict.OnCityDeleted(property);");
    }

    #endregion
}
