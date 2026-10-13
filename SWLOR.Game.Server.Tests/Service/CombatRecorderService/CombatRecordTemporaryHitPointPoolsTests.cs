using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CombatRecorderService;

namespace SWLOR.Game.Server.Tests.Service.CombatRecorderService;

public class CombatRecordTemporaryHitPointPoolsTests
{
    [Test]
    public void SumAbsorbed_IgnoresAPoolGrantedDuringTheHit()
    {
        // The hit consumes a 5-point pool and a low-HP reaction grants a 20-point shield.
        var before = new[] { Pool(1, 5) };
        var after = new[] { Pool(2, 20) };

        CombatRecordTemporaryHitPointPools.SumAbsorbed(before, after).Should().Be(5);
        CombatRecordTemporaryHitPointPools.Sum(before).Should().Be(5);
        CombatRecordTemporaryHitPointPools.Sum(after).Should().Be(20);
    }

    [Test]
    public void SumAbsorbed_AddsWhatEachPreHitPoolLost()
    {
        var before = new[] { Pool(1, 30), Pool(2, 10) };
        var after = new[] { Pool(1, 8) };

        CombatRecordTemporaryHitPointPools.SumAbsorbed(before, after).Should().Be(32);
    }

    [Test]
    public void GetAbsorbed_IsZeroForAPoolThatGrew()
    {
        CombatRecordTemporaryHitPointPools.GetAbsorbed(Pool(1, 10), new[] { Pool(1, 25) }).Should().Be(0);
    }

    [Test]
    public void MissingPools_AbsorbNothing()
    {
        CombatRecordTemporaryHitPointPools.SumAbsorbed(null, new[] { Pool(1, 10) }).Should().Be(0);
        CombatRecordTemporaryHitPointPools.SumAbsorbed(new[] { Pool(1, 10) }, null).Should().Be(10);
        CombatRecordTemporaryHitPointPools.Sum(null).Should().Be(0);
    }

    private static CombatRecordTemporaryHitPointPool Pool(ulong effectId, int amount)
    {
        return new CombatRecordTemporaryHitPointPool { EffectId = effectId, EffectKey = $"POOL_{effectId}", Amount = amount };
    }
}
