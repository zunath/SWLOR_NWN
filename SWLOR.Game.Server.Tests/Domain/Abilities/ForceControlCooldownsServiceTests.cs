using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Abilities;

namespace SWLOR.Game.Server.Tests.Domain.Abilities;

[TestFixture]
public class ForceControlCooldownsServiceTests
{
    private static readonly DateTime Now = new(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public void ForceMend_BlocksTheSameCasterAndTargetUntilTheCooldownEnds()
    {
        var service = new ForceControlCooldownsService();

        service.TryUseForceMend(1, 2, Now, 24f).Should().BeTrue();
        service.TryUseForceMend(1, 2, Now.AddSeconds(23.9), 24f).Should().BeFalse();
        service.TryUseForceMend(1, 2, Now.AddSeconds(24), 24f).Should().BeTrue("the cooldown ends exactly at its deadline");
    }

    [Test]
    public void ForceMend_TracksEachCasterAndTargetPairSeparately()
    {
        var service = new ForceControlCooldownsService();

        service.TryUseForceMend(1, 2, Now, 24f).Should().BeTrue();

        service.TryUseForceMend(1, 3, Now, 24f).Should().BeTrue();
        service.TryUseForceMend(4, 2, Now, 24f).Should().BeTrue();
        service.TryUseForceMend(1, 2, Now, 24f).Should().BeFalse();
    }

    [Test]
    public void HarmonicRestoration_BlocksTheSameCasterUntilTheCooldownEnds()
    {
        var service = new ForceControlCooldownsService();

        service.TryUseHarmonicRestoration(1, Now, 20f).Should().BeTrue();
        service.TryUseHarmonicRestoration(1, Now.AddSeconds(19), 20f).Should().BeFalse();
        service.TryUseHarmonicRestoration(2, Now.AddSeconds(19), 20f).Should().BeTrue();
        service.TryUseHarmonicRestoration(1, Now.AddSeconds(20), 20f).Should().BeTrue();
    }

    [Test]
    public void TheTwoCooldownsDoNotShareState()
    {
        var service = new ForceControlCooldownsService();

        service.TryUseHarmonicRestoration(1, Now, 20f).Should().BeTrue();

        service.TryUseForceMend(1, 1, Now, 24f).Should().BeTrue();
    }

    [Test]
    public void ExpiredCooldownsAreDroppedSoTheyFreeTheKeys()
    {
        var service = new ForceControlCooldownsService();
        service.TryUseHarmonicRestoration(1, Now, 20f).Should().BeTrue();
        service.TryUseHarmonicRestoration(2, Now.AddSeconds(30), 20f).Should().BeTrue();

        // Caster 1's entry ended before this call and caster 2's has not; only caster 2 stays blocked.
        service.TryUseHarmonicRestoration(2, Now.AddSeconds(40), 20f).Should().BeFalse();
        service.TryUseHarmonicRestoration(1, Now.AddSeconds(40), 20f).Should().BeTrue();
    }
}
