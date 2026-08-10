using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.ShuttleService;

namespace SWLOR.Game.Server.Tests.Service;

public class ShuttleScheduleTests
{
    private static readonly PlanetType[] ActivePlanets = Enum.GetValues(typeof(PlanetType))
        .Cast<PlanetType>()
        .Where(p => p != PlanetType.Invalid && p.GetAttribute<PlanetType, PlanetAttribute>().IsActive)
        .ToArray();

    private static IEnumerable<(PlanetType Origin, PlanetType Destination)> OrderedPairs()
    {
        foreach (var origin in ActivePlanets)
        foreach (var destination in ActivePlanets)
        {
            if (origin != destination)
                yield return (origin, destination);
        }
    }

    private static IEnumerable<(PlanetType A, PlanetType B)> UnorderedPairs()
    {
        for (var i = 0; i < ActivePlanets.Length; i++)
        for (var j = i + 1; j < ActivePlanets.Length; j++)
        {
            yield return (ActivePlanets[i], ActivePlanets[j]);
        }
    }

    [Test]
    public void GetTransitSeconds_UsesExpectedBoundsForEveryOrderedPair()
    {
        foreach (var (origin, destination) in OrderedPairs())
        {
            var transit = GalaxyMap.GetTransitSeconds(origin, destination);

            if (GalaxyMap.IsOrbitalHop(origin, destination))
                transit.Should().Be(60, $"{origin}->{destination}");
            else
                transit.Should().BeInRange(300, 600, $"{origin}->{destination}");

            (transit % 15).Should().Be(0, $"{origin}->{destination}");
        }
    }

    [Test]
    public void GetInterplanetaryTransitSeconds_IsExactlyHalfOfEveryLegacyRouteDuration()
    {
        foreach (var (origin, destination) in OrderedPairs())
        {
            if (GalaxyMap.IsOrbitalHop(origin, destination))
                continue;

            GalaxyMap.GetTransitSeconds(origin, destination).Should().Be(
                GetLegacyInterplanetaryTransitSeconds(origin, destination) / 2,
                $"{origin}->{destination}");
        }
    }

    [Test]
    public void GetFare_IsWithinBoundsAndDivisibleBy5ForEveryOrderedPair()
    {
        foreach (var (origin, destination) in OrderedPairs())
        {
            var fare = GalaxyMap.GetFare(origin, destination);

            if (GalaxyMap.IsOrbitalHop(origin, destination))
                fare.Should().BeInRange(1, 50, $"{origin}->{destination}");
            else
                fare.Should().BeInRange(100, 1000, $"{origin}->{destination}");

            (fare % 5).Should().Be(0, $"{origin}->{destination}");
        }
    }

    [Test]
    public void GalaxyMap_IsSymmetricForEveryUnorderedPair()
    {
        foreach (var (a, b) in UnorderedPairs())
        {
            GalaxyMap.GetTransitSeconds(a, b).Should().Be(GalaxyMap.GetTransitSeconds(b, a), $"{a}<->{b}");
            GalaxyMap.GetFare(a, b).Should().Be(GalaxyMap.GetFare(b, a), $"{a}<->{b}");
            GalaxyMap.GetDistance(a, b).Should().Be(GalaxyMap.GetDistance(b, a), $"{a}<->{b}");
        }
    }

    [Test]
    public void GetTransitSecondsAndGetFare_AreNonDecreasingWithDistance()
    {
        var pairsByDistance = UnorderedPairs()
            .Select(pair => new
            {
                pair.A,
                pair.B,
                Distance = GalaxyMap.GetDistance(pair.A, pair.B)
            })
            .OrderBy(x => x.Distance)
            .ToList();

        for (var i = 1; i < pairsByDistance.Count; i++)
        {
            var previous = pairsByDistance[i - 1];
            var current = pairsByDistance[i];

            GalaxyMap.GetTransitSeconds(current.A, current.B).Should()
                .BeGreaterThanOrEqualTo(GalaxyMap.GetTransitSeconds(previous.A, previous.B));

            GalaxyMap.GetFare(current.A, current.B).Should()
                .BeGreaterThanOrEqualTo(GalaxyMap.GetFare(previous.A, previous.B));
        }
    }

    [Test]
    public void ViscaraToCZ220_IsACheapFastOrbitalHop()
    {
        GalaxyMap.GetDistance(PlanetType.Viscara, PlanetType.CZ220).Should().BeApproximately(5.0f, 0.001f);
        GalaxyMap.IsOrbitalHop(PlanetType.Viscara, PlanetType.CZ220).Should().BeTrue();
        GalaxyMap.GetTransitSeconds(PlanetType.Viscara, PlanetType.CZ220).Should().Be(60);
        GalaxyMap.GetFare(PlanetType.Viscara, PlanetType.CZ220).Should().Be(25);
        // Symmetric.
        GalaxyMap.GetTransitSeconds(PlanetType.CZ220, PlanetType.Viscara).Should().Be(60);
        GalaxyMap.GetFare(PlanetType.CZ220, PlanetType.Viscara).Should().Be(25);
    }

    [Test]
    public void ViscaraToDantooine_MatchesKnownAnchorValues()
    {
        GalaxyMap.GetTransitSeconds(PlanetType.Viscara, PlanetType.Dantooine).Should().Be(600);
        GalaxyMap.GetFare(PlanetType.Viscara, PlanetType.Dantooine).Should().Be(985);
    }

    private static int GetLegacyInterplanetaryTransitSeconds(PlanetType origin, PlanetType destination)
    {
        var distance = GalaxyMap.GetDistance(origin, destination);
        var raw = 600.0 + 600.0 * (distance - 5.0) / 77.2;
        var rounded = (int)(Math.Round(raw / 30.0, MidpointRounding.AwayFromZero) * 30.0);

        return Math.Clamp(rounded, 600, 1200);
    }

    [Test]
    public void GetNextDepartureUtc_IsDeterministicAndOnePeriodApartOnConsecutiveCalls()
    {
        var now = new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc);
        var origin = PlanetType.Viscara;
        var destination = PlanetType.Tatooine;

        var first = ShuttleSchedule.GetNextDepartureUtc(origin, destination, now);
        var second = ShuttleSchedule.GetNextDepartureUtc(origin, destination, now);

        first.Should().Be(second);
        first.Should().BeAfter(now);

        var period = ShuttleSchedule.GetPeriodSeconds(origin, destination);
        (first - now).TotalSeconds.Should().BeLessThanOrEqualTo(period);

        var next = ShuttleSchedule.GetNextDepartureUtc(origin, destination, first);
        next.Should().Be(first.AddSeconds(period));
    }

    [Test]
    public void GetPeriodSecondsAndGetOffsetSeconds_AreWithinExpectedRangesForEveryRoute()
    {
        var validPeriods = new[] { 240, 270, 300, 330, 360 };

        foreach (var (origin, destination) in OrderedPairs())
        {
            var period = ShuttleSchedule.GetPeriodSeconds(origin, destination);
            var offset = ShuttleSchedule.GetOffsetSeconds(origin, destination);

            if (GalaxyMap.IsOrbitalHop(origin, destination))
                period.Should().Be(60, $"{origin}->{destination}");
            else
                validPeriods.Should().Contain(period, $"{origin}->{destination}");

            offset.Should().BeInRange(0, period - 1, $"{origin}->{destination}");
        }
    }

    [Test]
    public void PeriodAndOffset_AreStaggeredAcrossRoutes()
    {
        var distinctSchedules = OrderedPairs()
            .Select(pair => (
                Period: ShuttleSchedule.GetPeriodSeconds(pair.Origin, pair.Destination),
                Offset: ShuttleSchedule.GetOffsetSeconds(pair.Origin, pair.Destination)))
            .Distinct()
            .Count();

        distinctSchedules.Should().BeGreaterThanOrEqualTo(10);
    }

    [Test]
    public void GetRouteHash_MatchesIndependentlyComputedFnv1aHash()
    {
        var forwardHash = ShuttleSchedule.GetRouteHash(PlanetType.Viscara, PlanetType.Tatooine);
        var reverseHash = ShuttleSchedule.GetRouteHash(PlanetType.Tatooine, PlanetType.Viscara);

        forwardHash.Should().Be(ComputeFnv1a("1>2"));
        reverseHash.Should().Be(ComputeFnv1a("2>1"));
        forwardHash.Should().NotBe(reverseHash);
    }

    private static uint ComputeFnv1a(string value)
    {
        const uint offsetBasis = 2166136261;
        const uint prime = 16777619;
        var hash = offsetBasis;

        foreach (var c in value)
        {
            hash ^= (byte)c;
            hash *= prime;
        }

        return hash;
    }

    [Test]
    public void GetDeparturesBetween_ReturnsExactlyThreePeriodsOfDeparturesInAscendingOrder()
    {
        var origin = PlanetType.Viscara;
        var destination = PlanetType.Tatooine;
        var period = ShuttleSchedule.GetPeriodSeconds(origin, destination);

        var from = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
        var expectedFirst = ShuttleSchedule.GetNextDepartureUtc(origin, destination, from);
        var to = expectedFirst.AddSeconds(period * 2);

        var departures = ShuttleSchedule.GetDeparturesBetween(origin, destination, from, to).ToList();

        departures.Should().HaveCount(3);
        departures.Should().BeInAscendingOrder();
        departures.Should().OnlyContain(d => d > from && d <= to);

        departures[0].Should().Be(expectedFirst);
        departures[1].Should().Be(expectedFirst.AddSeconds(period));
        departures[2].Should().Be(expectedFirst.AddSeconds(period * 2));
    }

    [Test]
    public void GetDeparturesBetween_EmptyWindowReturnsNoDepartures()
    {
        var origin = PlanetType.Viscara;
        var destination = PlanetType.Tatooine;
        var moment = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);

        var departures = ShuttleSchedule.GetDeparturesBetween(origin, destination, moment, moment).ToList();

        departures.Should().BeEmpty();
    }

    [Test]
    public void ActivePlanets_AllHaveNonZeroGalaxyCoordinates()
    {
        foreach (var planet in ActivePlanets)
        {
            var attribute = planet.GetAttribute<PlanetType, PlanetAttribute>();
            (attribute.GalaxyX, attribute.GalaxyY).Should().NotBe((0, 0), planet.ToString());
        }
    }

    [Test]
    public void BuildFlightIdAndTryParseFlightId_RoundTripsOriginDestinationAndDepartureTime()
    {
        var origin = PlanetType.Viscara;
        var destination = PlanetType.Tatooine;
        var departure = new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc);

        var flightId = ShuttleSchedule.BuildFlightId(origin, destination, departure);
        var parsed = ShuttleSchedule.TryParseFlightId(flightId, out var parsedOrigin, out var parsedDestination, out var parsedDeparture);

        parsed.Should().BeTrue();
        parsedOrigin.Should().Be(origin);
        parsedDestination.Should().Be(destination);
        parsedDeparture.Should().Be(departure);
        parsedDeparture.Kind.Should().Be(DateTimeKind.Utc);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("garbage")]
    [TestCase("1>2")]
    [TestCase("1@5")]
    [TestCase("x>y@z")]
    [TestCase("1>2@-1")]                    // negative ticks
    [TestCase("1>2@9223372036854775807")]   // long.MaxValue, past DateTime.MaxValue.Ticks
    public void TryParseFlightId_ReturnsFalseForMalformedInput(string flightId)
    {
        var result = ShuttleSchedule.TryParseFlightId(flightId, out var origin, out var destination, out var departureUtc);

        result.Should().BeFalse();
        origin.Should().Be(PlanetType.Invalid);
        destination.Should().Be(PlanetType.Invalid);
        departureUtc.Should().Be(default(DateTime));
    }
}
