using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.TimeZoneService;

namespace SWLOR.Game.Server.Tests.Service;

public class PlayerTimeZonesTests
{
    [Test]
    public void EveryZone_ResolvesOnThisMachine()
    {
        foreach (var zone in PlayerTimeZones.All)
            PlayerTimeZones.GetInfo(zone).Should().NotBeNull(zone.Id);
    }

    [Test]
    public void Zones_HaveUniqueIdsAndShortLabels()
    {
        PlayerTimeZones.All.Select(zone => zone.Id).Should().OnlyHaveUniqueItems();
        PlayerTimeZones.All.Should().OnlyContain(zone => zone.Label.Length <= 28, "the Settings drop-down is 230 px wide");
        PlayerTimeZones.All.Should().OnlyContain(zone => zone.Label.All(c => c < 128), "game text is ASCII");
    }

    [Test]
    public void UnknownOrMissingZone_FallsBackToUtc()
    {
        PlayerTimeZones.Get(null).Should().BeSameAs(PlayerTimeZones.Utc);
        PlayerTimeZones.Get("Mars/Olympus_Mons").Should().BeSameAs(PlayerTimeZones.Utc);
        PlayerTimeZones.IndexOf(null).Should().Be(0);
        PlayerTimeZones.GetByIndex(-1).Should().BeSameAs(PlayerTimeZones.Utc);
        PlayerTimeZones.GetByIndex(PlayerTimeZones.All.Count).Should().BeSameAs(PlayerTimeZones.Utc);
    }

    [Test]
    public void Abbreviation_FollowsDaylightSaving()
    {
        var eastern = PlayerTimeZones.Get("America/New_York");
        var sydney = PlayerTimeZones.Get("Australia/Sydney");
        var winter = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var summer = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);

        PlayerTimeZones.GetAbbreviation(winter, eastern).Should().Be("EST");
        PlayerTimeZones.GetAbbreviation(summer, eastern).Should().Be("EDT");
        PlayerTimeZones.GetAbbreviation(winter, sydney).Should().Be("AEDT");
        PlayerTimeZones.GetAbbreviation(summer, sydney).Should().Be("AEST");
        PlayerTimeZones.GetAbbreviation(summer, PlayerTimeZones.Utc).Should().Be("UTC");
    }

    [Test]
    public void ZonesWithoutAShortName_ShowTheirOffset()
    {
        var winter = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var summer = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);

        PlayerTimeZones.GetAbbreviation(winter, PlayerTimeZones.Get("Europe/London")).Should().Be("UTC");
        PlayerTimeZones.GetAbbreviation(summer, PlayerTimeZones.Get("Europe/London")).Should().Be("UTC+1");
        PlayerTimeZones.GetAbbreviation(winter, PlayerTimeZones.Get("America/Sao_Paulo")).Should().Be("UTC-3");
        PlayerTimeZones.FormatOffset(new TimeSpan(-3, -30, 0)).Should().Be("UTC-3:30");
        PlayerTimeZones.FormatOffset(new TimeSpan(5, 30, 0)).Should().Be("UTC+5:30");
    }

    [Test]
    public void Abbreviation_IsUtcForAZoneThisHostCannotResolve()
    {
        var unresolved = new PlayerTimeZone("Mars/Olympus_Mons", "UTC-5 Mars", "EST", "EDT");
        var time = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        PlayerTimeZones.GetInfo(unresolved).Should().Be(TimeZoneInfo.Utc);
        PlayerTimeZones.GetAbbreviation(time, unresolved).Should().Be("UTC");
    }

    [Test]
    public void RepeatedHour_IsReadAsTheFirstOne()
    {
        // US clocks fall back from 02:00 EDT to 01:00 EST on 2026-11-01, so 01:30 happens twice.
        var eastern = PlayerTimeZones.Get("America/New_York");

        PlayerTimeZones.TryToUtc(new DateTime(2026, 11, 1, 1, 30, 0), eastern, out var utc).Should().BeTrue();
        utc.Should().Be(new DateTime(2026, 11, 1, 5, 30, 0, DateTimeKind.Utc));
    }

    [Test]
    public void ConversionRoundTrips()
    {
        var central = PlayerTimeZones.Get("Europe/Paris");
        var utc = new DateTime(2026, 6, 1, 18, 0, 0, DateTimeKind.Utc);

        var local = PlayerTimeZones.ToLocal(utc, central);
        local.Should().Be(new DateTime(2026, 6, 1, 20, 0, 0));
        PlayerTimeZones.TryToUtc(local, central, out var back).Should().BeTrue();
        back.Should().Be(utc);
    }
}
