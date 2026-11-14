using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Tests.Core;

[TestFixture]
public class ClockTests
{
    [Test]
    public void UtcNowReadsTheSystemClockByDefault()
    {
        var before = DateTime.UtcNow;
        var read = Clock.UtcNow;
        var after = DateTime.UtcNow;

        read.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        read.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Test]
    public void TickCount64ReadsTheSystemTickCountByDefault()
    {
        var before = Environment.TickCount64;
        var read = Clock.TickCount64;
        var after = Environment.TickCount64;

        read.Should().BeInRange(before, after);
    }

    [Test]
    public void OverrideReplacesBothReadingsUntilDisposed()
    {
        var instant = new DateTime(2030, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        using (Clock.OverrideForTests(() => instant, () => 12345))
        {
            Clock.UtcNow.Should().Be(instant);
            Clock.TickCount64.Should().Be(12345);

            instant = instant.AddSeconds(30);
            Clock.UtcNow.Should().Be(new DateTime(2030, 1, 2, 3, 4, 35, DateTimeKind.Utc));
        }

        AssertReadsSystemClock();
    }

    [Test]
    public void OverridingOneReadingLeavesTheOtherOnTheSystemClock()
    {
        var instant = new DateTime(2030, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        using (Clock.OverrideForTests(utcNow: () => instant))
        {
            Clock.UtcNow.Should().Be(instant);
            var before = Environment.TickCount64;
            Clock.TickCount64.Should().BeInRange(before, Environment.TickCount64);
        }

        AssertReadsSystemClock();

        using (Clock.OverrideForTests(tickCount64: () => 99))
        {
            Clock.TickCount64.Should().Be(99);
            var before = DateTime.UtcNow;
            Clock.UtcNow.Should().BeOnOrAfter(before);
        }
    }

    [Test]
    public void DisposingRestoresTheSystemClockWhenTheTestBodyThrows()
    {
        var instant = new DateTime(2030, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        var body = () =>
        {
            using (Clock.OverrideForTests(() => instant))
            {
                Clock.UtcNow.Should().Be(instant);
                throw new InvalidOperationException("test body failed");
            }
        };

        body.Should().Throw<InvalidOperationException>();
        AssertReadsSystemClock();
    }

    [Test]
    public void OverridingWhileAnotherOverrideIsActiveThrows()
    {
        using (Clock.OverrideForTests(() => new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc)))
        {
            var second = () => Clock.OverrideForTests(() => new DateTime(2031, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            second.Should().Throw<InvalidOperationException>();
            Clock.UtcNow.Year.Should().Be(2030);
        }

        using (Clock.OverrideForTests(() => new DateTime(2032, 1, 1, 0, 0, 0, DateTimeKind.Utc)))
            Clock.UtcNow.Year.Should().Be(2032);
    }

    [Test]
    public void DisposingTwiceDoesNotClearALaterOverride()
    {
        var first = Clock.OverrideForTests(() => new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        first.Dispose();

        using (Clock.OverrideForTests(() => new DateTime(2031, 1, 1, 0, 0, 0, DateTimeKind.Utc)))
        {
            first.Dispose();

            Clock.UtcNow.Year.Should().Be(2031);
        }
    }

    private static void AssertReadsSystemClock()
    {
        var beforeTime = DateTime.UtcNow;
        var beforeTicks = Environment.TickCount64;
        var time = Clock.UtcNow;
        var ticks = Clock.TickCount64;

        time.Should().BeOnOrAfter(beforeTime).And.BeOnOrBefore(DateTime.UtcNow);
        ticks.Should().BeInRange(beforeTicks, Environment.TickCount64);
    }
}
