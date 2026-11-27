using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.TradeDefinition;
using SWLOR.Game.Server.Service.EdictService;
using SWLOR.Game.Server.Service.IndustryService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.StationService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Shadowport is refused where the city's Starport exchange is an open port, since it would change nothing and still cost 10,000 a
/// week: it is not offered in the Edicts tab or the platform editor, is refused when enacted, and is dropped from a winning platform
/// the way planet-based Edicts are dropped at a Wild Space station.
/// </summary>
public class ShadowportAvailabilityTests
{
    private static readonly DateTime Now = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);

    private static string Root
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                directory = directory.Parent;
            return directory?.FullName ?? throw new DirectoryNotFoundException();
        }
    }

    private static string Source(params string[] relative) =>
        File.ReadAllText(Path.Combine(new[] { Root, "SWLOR.Game.Server" }.Concat(relative).ToArray()));

    private static WorldProperty City(int level = 5)
    {
        var city = new WorldProperty { Id = "city", CustomName = "city", PropertyType = PropertyType.City, Treasury = 100000 };
        city.Upgrades[PropertyUpgradeType.CityLevel] = level;
        city.Dates[PropertyDateType.Upkeep] = Now.AddDays(3);
        return city;
    }

    private static readonly IReadOnlyDictionary<EdictType, CityEdict> NoEdicts = new Dictionary<EdictType, CityEdict>();

    // Which ports are open

    [Test]
    public void AnExchangeIsAnOpenPortWhenNoAuthorityRunsIt()
    {
        foreach (var exchange in TradeCatalog.Exchanges)
        {
            ShadowportRules.IsOpenPort(exchange.Type).Should().Be(exchange.Authority == TradeAuthority.None, exchange.Name);
        }

        ShadowportRules.IsOpenPort(TradeExchangeType.Invalid).Should().BeTrue("a city with no exchange has nothing for Shadowport to change");
    }

    [Test]
    public void ACityIsAtALawfulPortOnlyWhenItsExchangeHasACustomsAuthority()
    {
        foreach (var anchor in StationTuning.Anchors)
        {
            var open = ShadowportRules.IsOpenPort(StationRules.GetExchange(anchor.Id));
            if (anchor.IsWildSpace)
                open.Should().BeTrue($"{anchor.Id} is in Wild Space");
            else
                open.Should().Be(TradeLookup.GetExchange(anchor.Exchange).Authority == TradeAuthority.None, anchor.Id);
        }

        StationTuning.Anchors.Where(x => !x.IsWildSpace && !ShadowportRules.IsOpenPort(x.Exchange)).Select(x => x.Exchange)
            .Should().Contain(new[] { TradeExchangeType.Viscara, TradeExchangeType.MonCala, TradeExchangeType.Korriban });
    }

    // The Edicts tab and the platform editor

    [Test]
    public void ShadowportIsNotOfferedAtAnOpenPort_ButEveryOtherEdictIs()
    {
        EdictRules.GetOfferedEdicts(false, true).Select(x => x.Type).Should().NotContain(EdictType.Shadowport);
        EdictRules.GetOfferedEdicts(false, true).Select(x => x.Type)
            .Should().BeEquivalentTo(EdictRules.GetOfferedEdicts(false, false).Select(x => x.Type).Where(x => x != EdictType.Shadowport));

        EdictRules.GetOfferedEdicts(false, false).Select(x => x.Type).Should().Contain(EdictType.Shadowport);
        EdictRules.GetOfferedEdicts(false).Select(x => x.Type).Should().Contain(EdictType.Shadowport);
    }

    [Test]
    public void AWildSpaceStationLacksBothThePlanetEdictsAndShadowport()
    {
        var offered = EdictRules.GetOfferedEdicts(true, true).Select(x => x.Type).ToList();

        offered.Should().NotContain(new[]
        {
            EdictType.ProspectorsCharter, EdictType.HuntingGrounds, EdictType.IndustrialCharter, EdictType.Shadowport,
        });
        offered.Should().Contain(EdictType.FestivalSeason).And.Contain(EdictType.OpenGates);
    }

    [Test]
    public void AnOpenPortDoesNotHideThePlanetBasedEdicts()
    {
        foreach (var type in new[] { EdictType.ProspectorsCharter, EdictType.HuntingGrounds, EdictType.IndustrialCharter })
        {
            EdictRules.IsAvailableTo(type, false, true).Should().BeTrue(type.ToString());
            EdictRules.IsAvailableTo(type, true, true).Should().BeFalse(type.ToString());
        }

        EdictRules.IsAvailableTo(EdictType.Shadowport, false, false).Should().BeTrue();
        EdictRules.IsAvailableTo(EdictType.Shadowport, false, true).Should().BeFalse();
        EdictRules.IsAvailableTo(EdictType.Shadowport, true, false).Should().BeTrue("Wild Space alone does not hide a lawful-port Edict; its open port does");
    }

    [Test]
    public void OnlyShadowportNeedsALawfulPort()
    {
        EdictCatalog.Ordered.Where(x => x.RequiresLawfulPort).Select(x => x.Type).Should().Equal(EdictType.Shadowport);
    }

    // Enacting

    [Test]
    public void EnactingShadowportAtAnOpenPort_IsRefusedAsUnavailableHere_BeforeAnyOtherCheck()
    {
        EdictRules.GetEnactRefusal(EdictType.Shadowport, "city", "", false, 5, false, CityWideCondition.None, NoEdicts, isOpenPort: true)
            .Should().Be(EdictRefusal.UnavailableHere);
        EdictRules.GetEnactRefusal(EdictType.Shadowport, "city", "", false, 1, false, CityWideCondition.None, NoEdicts, isOpenPort: true)
            .Should().Be(EdictRefusal.UnavailableHere);

        EdictRules.GetEnactRefusal(EdictType.Shadowport, "city", "", false, 1, false, CityWideCondition.None, NoEdicts)
            .Should().Be(EdictRefusal.None);
        EdictRules.GetEnactRefusal(EdictType.Shadowport, "city", "", false, 5, false, CityWideCondition.None, NoEdicts, isOpenPort: false)
            .Should().Be(EdictRefusal.None);
    }

    [Test]
    public void ThePlannedEnactmentChargesNothingAndChangesNothingAtAnOpenPort()
    {
        var city = City();

        var plan = EdictChanges.PlanEnact(city, null, string.Empty, EdictType.Shadowport, false, Now, isOpenPort: true);
        var enactment = EdictChanges.Enact(city, null, string.Empty, EdictType.Shadowport, false, false, Now, isOpenPort: true);

        plan.Refusal.Should().Be(EdictRefusal.UnavailableHere);
        enactment.Refusal.Should().Be(EdictRefusal.UnavailableHere);
        enactment.Charge.Should().Be(0);
        city.Edicts.Should().BeEmpty();

        EdictChanges.Enact(city, null, string.Empty, EdictType.Shadowport, false, false, Now).Refusal.Should().Be(EdictRefusal.None);
        city.Edicts.Should().ContainKey(EdictType.Shadowport);
    }

    [Test]
    public void TheRefusalReasonNamesTheOpenPort_AndKeepsTheWildSpaceWordingForPlanetEdicts()
    {
        EdictRules.DescribeRefusal(EdictRefusal.UnavailableHere, EdictType.Shadowport)
            .Should().Be("Shadowport is not available where the Starport exchange is an open port, such as a station in Wild Space.");
        EdictRules.DescribeRefusal(EdictRefusal.UnavailableHere, EdictType.HuntingGrounds)
            .Should().Be("Hunting Grounds is not available to a station in Wild Space.");
    }

    // Platforms

    [Test]
    public void AShadowportPlatformIsRefusedAtAnOpenPort_WithTheSameReason()
    {
        var platform = new[] { new ElectionPlatformEdict { Type = EdictType.Shadowport } };

        ElectionPlatformRules.GetRefusal(platform, "city", 5, false, _ => true, isOpenPort: true)
            .Should().Be((EdictRefusal.UnavailableHere, EdictType.Shadowport));
        ElectionPlatformRules.GetRefusal(platform, "city", 5, false, _ => true).Refusal.Should().Be(EdictRefusal.None);
        ElectionPlatformRules.GetRefusal(platform, "city", 5, false, _ => true, isOpenPort: false).Refusal.Should().Be(EdictRefusal.None);
    }

    [Test]
    public void AWinningPlatformDropsShadowportAtAnOpenPort_AndDoesNotCountItAsPromised()
    {
        var city = City();
        var platform = new[]
        {
            new ElectionPlatformEdict { Type = EdictType.FestivalSeason },
            new ElectionPlatformEdict { Type = EdictType.Shadowport },
        };

        var result = EdictChanges.EnactPlatform(city, platform, _ => null, false, Now, isOpenPort: true);

        result.Dropped.Should().ContainSingle().Which.Should().Be((EdictType.Shadowport, EdictRefusal.UnavailableHere));
        city.Edicts.Keys.Should().Equal(EdictType.FestivalSeason);
        city.MayorRecord.Promised.Should().Be(1);

        var lawful = City();
        var kept = EdictChanges.EnactPlatform(lawful, platform, _ => null, false, Now);
        kept.Dropped.Should().BeEmpty();
        lawful.Edicts.Keys.Should().BeEquivalentTo(new[] { EdictType.FestivalSeason, EdictType.Shadowport });
        lawful.MayorRecord.Promised.Should().Be(2);
    }

    [Test]
    public void ADroppedShadowportFreesItsSlotForTheRestOfThePlatform()
    {
        var city = City(level: 1);
        var platform = new[]
        {
            new ElectionPlatformEdict { Type = EdictType.Shadowport },
            new ElectionPlatformEdict { Type = EdictType.FestivalSeason },
        };

        EdictChanges.EnactPlatform(city, platform, _ => null, false, Now, isOpenPort: true);

        city.Edicts.Keys.Should().Equal(EdictType.FestivalSeason);
    }

    // Where it is wired

    [Test]
    public void EveryPathThatOffersOrEnactsEdicts_AsksWhetherTheCityIsAtAnOpenPort()
    {
        var edict = Source("Service", "Edict.cs");
        edict.Should().Contain("public static bool IsOpenPort(WorldProperty city)");
        edict.Split("IsOpenPort(city)").Length.Should().Be(5, "the check, the settle plan, the enactment and the platform each ask once");

        Source("Feature", "GuiDefinition", "ViewModel", "ManageCityViewModel.Edicts.cs")
            .Should().Contain("EdictRules.GetOfferedEdicts(Edict.IsWildSpaceStation(city), Edict.IsOpenPort(city))");
        Source("Feature", "GuiDefinition", "ViewModel", "ElectionViewModel.Platform.cs")
            .Should().Contain("EdictRules.GetOfferedEdicts(context.IsWildSpaceStation, context.IsOpenPort)");
        Source("Service", "ElectionPlatforms.cs").Should().Contain("Edict.IsOpenPort(city)");
        Source("Service", "ElectionBallot.cs").Should().Contain("Edict.IsOpenPort(city));");
    }
}
