using System.Globalization;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Service;

public class RecastTimestampTests
{
    [Test]
    public void ShortCooldown_RemainsActiveUntilItsExactExpiry()
    {
        var now = new DateTime(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc).AddMilliseconds(125);
        var endsAt = now.AddSeconds(0.25);

        RecastTimestamp.TryParse(RecastTimestamp.Format(endsAt), out var storedExpiry).Should().BeTrue();

        storedExpiry.Should().Be(endsAt);
        storedExpiry.Should().BeAfter(now);
        storedExpiry.Should().BeAfter(endsAt.AddTicks(-1));
        (endsAt < storedExpiry).Should().BeFalse();
    }

    [Test]
    public void RepeatedReductions_PreserveTheRemainingFraction()
    {
        var now = new DateTime(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc).AddTicks(1234567);
        var stored = RecastTimestamp.Format(now.AddSeconds(2));

        for (var index = 0; index < 3; index++)
        {
            RecastTimestamp.TryParse(stored, out var endsAt).Should().BeTrue();
            stored = RecastTimestamp.Format(endsAt.AddSeconds(-0.5));
        }

        RecastTimestamp.TryParse(stored, out var reducedExpiry).Should().BeTrue();
        reducedExpiry.Should().Be(now.AddSeconds(0.5));
    }

    [TestCase("en-US")]
    [TestCase("th-TH")]
    [TestCase("ar-SA")]
    public void TimestampFormat_IsIndependentOfTheServerCulture(string culture)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            var endsAt = new DateTime(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc).AddTicks(1234567);

            var stored = RecastTimestamp.Format(endsAt);

            stored.Should().Be("2026-09-08T12:00:00.1234567Z");
            RecastTimestamp.TryParse(stored, out var parsed).Should().BeTrue();
            parsed.Should().Be(endsAt);
            parsed.Kind.Should().Be(DateTimeKind.Utc);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [Test]
    public void LegacyTimestamp_IsReadAsUtcAndCanBeReducedWithFractionalPrecision()
    {
        RecastTimestamp.TryParse("2026-09-08 12:00:02", out var endsAt).Should().BeTrue();
        endsAt.Should().Be(new DateTime(2026, 9, 8, 12, 0, 2, DateTimeKind.Utc));
        endsAt.Kind.Should().Be(DateTimeKind.Utc);

        RecastTimestamp.Format(endsAt.AddSeconds(-0.25)).Should().Be("2026-09-08T12:00:01.7500000Z");
    }

    [Test]
    public void TimestampWithOffset_IsNormalizedToUtc()
    {
        RecastTimestamp.TryParse("2026-09-08T17:30:00.1234567+05:30", out var endsAt).Should().BeTrue();

        endsAt.Should().Be(new DateTime(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc).AddTicks(1234567));
        endsAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    [TestCase("not a timestamp")]
    [TestCase("2026-02-30 12:00:00")]
    [TestCase("09/08/2026 12:00:00")]
    public void MalformedTimestamp_IsRejectedWithoutThrowing(string value)
    {
        RecastTimestamp.TryParse(value, out var endsAt).Should().BeFalse();
        endsAt.Should().Be(default(DateTime));
    }
}
