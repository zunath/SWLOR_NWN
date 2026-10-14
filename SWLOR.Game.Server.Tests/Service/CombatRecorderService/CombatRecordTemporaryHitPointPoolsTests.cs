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
    public void ReplacedPool_IsMeasuredAgainstWhatItHadLeft()
    {
        // The hit takes 10 of a 30-point pool, then a reaction replaces that key with a new pool.
        var before = new[] { Pool(1, 30) };
        var after = new[] { Pool(2, 50) };
        var atRemoval = new[] { Pool(1, 20) };

        var leftByHit = CombatRecordTemporaryHitPointPools.MergeKeepingFirst(after, atRemoval);

        CombatRecordTemporaryHitPointPools.SumAbsorbed(before, leftByHit).Should().Be(10);
        CombatRecordTemporaryHitPointPools.SumAbsorbed(before, after).Should().Be(30, "without the remainder the whole pool looks absorbed");
    }

    [Test]
    public void MergeKeepingFirst_KeepsTheEarliestAmountPerPool()
    {
        var merged = CombatRecordTemporaryHitPointPools.MergeKeepingFirst(new[] { Pool(1, 20) }, new[] { Pool(1, 5), Pool(3, 7) });

        merged.Should().HaveCount(2);
        merged.Single(pool => pool.EffectId == 1).Amount.Should().Be(20);
        merged.Single(pool => pool.EffectId == 3).Amount.Should().Be(7);
        CombatRecordTemporaryHitPointPools.MergeKeepingFirst(null, new[] { Pool(4, 1) }).Should().HaveCount(1);
        CombatRecordTemporaryHitPointPools.MergeKeepingFirst(new[] { Pool(4, 1) }, null).Should().HaveCount(1);
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
