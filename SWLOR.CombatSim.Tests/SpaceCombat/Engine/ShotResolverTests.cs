using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.SpaceCombat.Engine;
using SWLOR.CombatSim.SpaceCombat.Scenario;

namespace SWLOR.CombatSim.Tests.SpaceCombat.Engine;

[TestFixture]
public class ShotResolverTests
{
    /// <summary>The same shot with the same seed against two targets that differ only in the setup, so the hit, critical and damage rolls match.</summary>
    private static (ShotResult Plain, ShotResult Changed) FireTwice(
        SpaceScenarioSpec scenario, WeaponModuleSpec weapon, ShipSize attackerSize,
        Action<ShipSpec> changeTarget, ShipSize targetSize = ShipSize.Small)
    {
        for (var seed = 1; seed < 200; seed++)
        {
            var plain = Fire(scenario, weapon, attackerSize, _ => { }, targetSize, seed);
            if (!plain.Hit)
                continue;

            return (plain, Fire(scenario, weapon, attackerSize, changeTarget, targetSize, seed));
        }

        throw new InvalidOperationException("No seed produced a hit.");
    }

    private static ShotResult Fire(
        SpaceScenarioSpec scenario, WeaponModuleSpec weapon, ShipSize attackerSize, Action<ShipSpec> changeTarget, ShipSize targetSize, int seed)
    {
        var attacker = ShipState.Create(SpaceTestShips.Ship(size: attackerSize, weapons: weapon), "Attacker", 0);
        var targetSpec = SpaceTestShips.Ship(size: targetSize, hull: 100000);
        changeTarget(targetSpec);
        var target = ShipState.Create(targetSpec, "Target", 1);
        return new ShotResolver(scenario, new System.Random(seed)).Fire(attacker, weapon, 0, target);
    }

    [Test]
    public void Fire_AppliesTheTargetsResistanceToTheDamageType()
    {
        var (plain, resisted) = FireTwice(new SpaceScenarioSpec(), SpaceTestShips.Laser(dmg: 60), ShipSize.Small,
            target => target.Resistances.Thermal = 40);

        plain.Damage.Should().BeGreaterThan(0);
        resisted.Damage.Should().BeApproximately(plain.Damage * 0.6, 1e-9);
        resisted.Applied.Should().BeApproximately(plain.Applied * 0.6, 1e-9);
    }

    [Test]
    public void Fire_DoesNotApplyAResistanceToADifferentDamageType()
    {
        var (plain, other) = FireTwice(new SpaceScenarioSpec(), SpaceTestShips.Laser(dmg: 60), ShipSize.Small,
            target => target.Resistances.EM = 90);

        other.Damage.Should().Be(plain.Damage);
    }

    [Test]
    public void Fire_FromASmallerWeapon_LosesDamageForEachSizeStep()
    {
        var scenario = new SpaceScenarioSpec { SizeDamagePenaltyPerStep = 0.25 };

        var seed = FirstHitSeed(scenario, ShipSize.Small, ShipSize.Large);

        // Same seed and stats, so the rolls match and only the size rule differs: two steps below the target.
        var small = Fire(scenario, SpaceTestShips.Laser(dmg: 60), ShipSize.Small, _ => { }, ShipSize.Large, seed);
        var large = Fire(scenario, SpaceTestShips.Laser(dmg: 60), ShipSize.Large, _ => { }, ShipSize.Large, seed);

        large.Damage.Should().BeGreaterThan(0);
        small.Damage.Should().BeApproximately(large.Damage * 0.75 * 0.75, 1e-9);
    }

    [Test]
    public void Fire_WithIgnoreSizePenalty_LosesNoDamageToSize()
    {
        var scenario = new SpaceScenarioSpec { SizeDamagePenaltyPerStep = 0.25 };
        var torpedo = SpaceTestShips.Laser(dmg: 60);
        torpedo.IgnoreSizePenalty = true;
        var seed = FirstHitSeed(scenario, ShipSize.Small, ShipSize.Large);

        var small = Fire(scenario, torpedo, ShipSize.Small, _ => { }, ShipSize.Large, seed);
        var large = Fire(scenario, torpedo, ShipSize.Large, _ => { }, ShipSize.Large, seed);

        small.Damage.Should().Be(large.Damage);
    }

    [Test]
    public void Fire_AMissDamagesNothing()
    {
        var weapon = SpaceTestShips.Laser();
        var scenario = new SpaceScenarioSpec();
        var misses = Enumerable.Range(1, 200)
            .Select(seed => Fire(scenario, weapon, ShipSize.Small, _ => { }, ShipSize.Small, seed))
            .Where(shot => !shot.Hit)
            .ToList();

        misses.Should().NotBeEmpty();
        misses.Should().OnlyContain(shot => shot.Damage == 0 && shot.Applied == 0 && !shot.Critical);
    }

    // The first seed whose shot hits when the size penalties do not change the hit roll.
    private static int FirstHitSeed(SpaceScenarioSpec scenario, ShipSize attackerSize, ShipSize targetSize)
    {
        for (var seed = 1; seed < 200; seed++)
        {
            if (Fire(scenario, SpaceTestShips.Laser(dmg: 60), attackerSize, _ => { }, targetSize, seed).Hit)
                return seed;
        }

        throw new InvalidOperationException("No seed produced a hit.");
    }
}
