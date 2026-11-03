using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.WeatherService;
using SWLOR.Game.Server.Tests.Domain.World;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The authored settlement areas of the module carry the WEATHER_HAZARD_SAFE local that the weather instance reads
/// (<c>WeatherServiceTests</c> prove what it does with it). The Eshan cases also run the authored value through the weather
/// instance on a scripted world (<see cref="WeatherWorld"/>).
/// </summary>
public sealed class WeatherSettlementTests
{
    // The weather instance asks the static Property facade for the property of an area, which resolves from the container.
    private FacadeContainer _facades = null!;
    private IDisposable _facadeScope = null!;

    [OneTimeSetUp]
    public void BuildFacadeContainer() => _facades = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeFacadeContainer() => _facades.Dispose();

    [SetUp]
    public void InstallFacades() => _facadeScope = _facades.Install();

    [TearDown]
    public void UninstallFacades() => _facadeScope.Dispose();

    private const uint Settlement = 100;
    private const uint Player = 5001;

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
    [TestCase("v_jeditemple_v2")]
    [TestCase("v_repubbase_ext")]
    [TestCase("dan_repgarrison")]
    [TestCase("dath_cz_baseok")]
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

    [TestCase("pw_ar_sc_eshanci")]
    [TestCase("pw_ar_sc_eshancm")]
    [TestCase("pw_ar_sc_eshanto")]
    [TestCase("pw_sc_es_oldgate")]
    [TestCase("pw_sc_eshanfield")]
    [TestCase("pw_sc_esh_riverw")]
    [TestCase("pw_sc_eshhouse")]
    public void EshanSettlements_AuthoredSafetyPreventsSnowstormDamage(string resref)
    {
        var path = Path.Combine(FindRoot(), "Module", "git", $"{resref}.git.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var safety = document.RootElement.GetProperty("VarTable").GetProperty("value").EnumerateArray()
            .Where(variable => variable.GetProperty("Name").GetProperty("value").GetString() == "WEATHER_HAZARD_SAFE")
            .Should().ContainSingle($"{resref} is an authored settlement").Which;
        safety.GetProperty("Type").GetProperty("value").GetInt32().Should().Be(1);
        safety.GetProperty("Value").GetProperty("type").GetString().Should().Be("int");
        var safe = safety.GetProperty("Value").GetProperty("value").GetInt32();
        safe.Should().Be(1);

        using var world = new WeatherWorld();
        world.AddArea(Settlement, PlanetType.Eshan);
        world.AddPlayer(Player, Settlement);
        world.SetInt(Settlement, "WEATHER_HAZARD_SAFE", safe);
        world.Rolls(4, 9, 9, 9, 0);
        world.Service.SetWeather(Settlement);

        world.Service.DoWeatherEffects(Player);
        world.RunAssigned(Settlement);

        world.MessagesTo(Player).Should().Equal(WeatherFeedbackText.ShelteredSnowStorm);
        world.AssignedTo(Settlement).Should().Be(0);
        world.Hits().Should().BeEmpty($"snow in {resref} should remain harmless");
    }

    [Test]
    public void VeylanGroves_RetainWildernessSnowstormDamage()
    {
        var path = Path.Combine(FindRoot(), "Module", "git", "sc_eswildlandn.git.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var variables = document.RootElement.GetProperty("VarTable").GetProperty("value").EnumerateArray();
        variables.Should().NotContain(variable =>
            variable.GetProperty("Name").GetProperty("value").GetString() == "WEATHER_HAZARD_SAFE");

        using var world = new WeatherWorld();
        world.AddArea(Settlement, PlanetType.Eshan);
        world.AddPlayer(Player, Settlement);
        world.Rolls(4, 9, 9, 9, 0);
        world.Service.SetWeather(Settlement);

        world.Service.DoWeatherEffects(Player);
        world.RunAssigned(Settlement);

        world.MessagesTo(Player).Should().Equal(WeatherFeedbackText.SnowStorm);
        world.Hits().Should().ContainSingle("settlement protection must preserve Eshan's wilderness hazards");
    }
    private static string FindRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
