using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.Tests.Service;

public class WeaponAttackPointLedgerTests
{
    private readonly DateTime _now = new(2026, 10, 13, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public void ALaunchedWeaponOrItsCapturedAmmunition_ConsumesExactlyOneCallback()
    {
        var ledger = new WeaponAttackPointLedger();
        ledger.Capture(1, 2, 3, 4, 10, _now).Should().BeTrue();
        ledger.Consume(1, 2, 4, _now.AddSeconds(1)).Should().BeTrue();
        ledger.Consume(1, 2, 3, _now.AddSeconds(1)).Should().BeFalse();
        ledger.Capture(1, 2, 3, 4, 10, _now.AddSeconds(1)).Should().BeFalse("repeated damage callbacks cannot credit one roll twice");
    }

    [Test]
    public void SwappedWeaponsAndDifferentActorsOrTargets_CannotConsumeACapturedShot()
    {
        var ledger = new WeaponAttackPointLedger();
        ledger.Capture(1, 2, 3, 4, 1, _now);
        ledger.Consume(1, 2, 5, _now).Should().BeFalse();
        ledger.Consume(2, 2, 3, _now).Should().BeFalse();
        ledger.Consume(1, 3, 3, _now).Should().BeFalse();
        ledger.Consume(1, 2, 3, _now).Should().BeTrue();
    }

    [Test]
    public void MultipleRollsSharingAnAmmoStack_AreConsumedIndividually()
    {
        var ledger = new WeaponAttackPointLedger();
        ledger.Capture(1, 2, 3, 4, 1, _now);
        ledger.Capture(1, 2, 3, 4, 2, _now);
        ledger.Consume(1, 2, 4, _now).Should().BeTrue();
        ledger.Consume(1, 2, 4, _now).Should().BeTrue();
        ledger.Consume(1, 2, 4, _now).Should().BeFalse();
    }

    [Test]
    public void ExpiredAndDisconnectedShots_DoNotSuppressFutureHits()
    {
        var ledger = new WeaponAttackPointLedger();
        ledger.Capture(1, 2, 3, 4, 1, _now);
        ledger.Consume(1, 2, 4, _now.AddSeconds(10)).Should().BeFalse();
        ledger.Capture(1, 2, 3, 4, 1, _now.AddSeconds(10)).Should().BeTrue();
        ledger.Clear(1);
        ledger.Consume(1, 2, 4, _now.AddSeconds(10)).Should().BeFalse();
    }

    [Test]
    public void LostCallbacks_CannotGrowThePendingQueueWithoutBound()
    {
        var ledger = new WeaponAttackPointLedger();
        for (var index = 0; index < 200; index++) ledger.Capture(1, 2, (uint)(index + 10), 4, index + 1, _now);
        ledger.Consume(1, 2, 10, _now).Should().BeFalse("the oldest unmatched callback is dropped");
        ledger.Consume(1, 2, 209, _now).Should().BeTrue();
    }
}
