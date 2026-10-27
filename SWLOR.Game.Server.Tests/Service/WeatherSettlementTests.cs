using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The authored settlement areas of the module carry the WEATHER_HAZARD_SAFE local that the weather instance reads
/// (<c>WeatherServiceTests</c> prove what it does with it).
/// </summary>
public sealed class WeatherSettlementTests
{
    [TestCase("veles_exterior")]
    [TestCase("druz_shalim")]
    [TestCase("hutlar_outpost")]
    [TestCase("moncaladaccitysu")]
    [TestCase("dan_centcolony")]
    [TestCase("dan_colony")]
    [TestCase("dath_tribevill")]
    [TestCase("kash_village")]
    [TestCase("anchor_entreenor")]
    [TestCase("anchor_entreesud")]
    [TestCase("tat_anc_northdis")]
    [TestCase("tat_anc_southdis")]
    [TestCase("tat_tocheemain")]
    [TestCase("pw_ar_kashyk")]
    [TestCase("moseis_dow_ca001")]
    [TestCase("moseis_dow_can")]
    [TestCase("moseis_exit_sud")]
    [TestCase("moseis_lucky")]
    [TestCase("pw_ar_narcatwalk")]
    [TestCase("pw_ar_nardocks")]
    [TestCase("pw_ar_narpromena")]
    [TestCase("pw_ar_narscorpd")]
    [TestCase("pw_ar_narshahub")]
    [TestCase("pw_ar_narslum")]
    [TestCase("pw_ar_nsshipyard")]
    [TestCase("tat_anc_southent")]
    [TestCase("tat_anc_astropor")]
    public void CoreSettlementAreas_AuthorWEATHERHAZARDSAFE(string resref)
    {
        var path = Path.Combine(FindRoot(), "Module", "git", $"{resref}.git.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var variables = document.RootElement.GetProperty("VarTable").GetProperty("value");
        variables.EnumerateArray().Should().Contain(variable =>
            variable.GetProperty("Name").GetProperty("value").GetString() == "WEATHER_HAZARD_SAFE" &&
            variable.GetProperty("Value").GetProperty("value").GetInt32() == 1,
            $"{resref} should carry an authored WEATHER_HAZARD_SAFE local");
    }

    [Test]
    public void AnchorheadRoad_RemainsAnUnflaggedWeatherArea()
    {
        var path = Path.Combine(FindRoot(), "Module", "git", "anchor_road_est.git.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var variables = document.RootElement.GetProperty("VarTable").GetProperty("value");
        variables.EnumerateArray().Should().NotContain(variable =>
            variable.GetProperty("Name").GetProperty("value").GetString() == "WEATHER_HAZARD_SAFE");
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
