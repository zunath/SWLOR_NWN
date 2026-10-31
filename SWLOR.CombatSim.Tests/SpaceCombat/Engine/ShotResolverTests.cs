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
        resisted.Damage.Should().Be(Math.Round(plain.Damage * 0.6, MidpointRounding.AwayFromZero));
        resisted.Applied.Should().Be(resisted.Damage);
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
        small.Damage.Should().Be(Math.Round(large.Damage * 0.75 * 0.75, MidpointRounding.AwayFromZero));
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

    [Test]
    public void Fire_MultipliesTheDamageBonusAndTheResistanceSeparately()
    {
        // The server multiplies (1 + bonus %) and (1 - resistance %); folding the bonus into the resistance would give
        // 1 - (40 - 20) % = 0.8 instead of 1.2 x 0.6 = 0.72.
        var weapon = SpaceTestShips.Laser(dmg: 60);
        var seed = FirstHitSeed(new SpaceScenarioSpec(), ShipSize.Small, ShipSize.Small);
        var plain = FireFrom(weapon, _ => { }, target => target.Resistances.Thermal = 40, seed);
        var boosted = FireFrom(weapon, attacker => attacker.DamageBonusPercent.Thermal = 20, target => target.Resistances.Thermal = 40, seed);
        var unresisted = FireFrom(weapon, _ => { }, _ => { }, seed);

        boosted.Damage.Should().Be(Math.Round(unresisted.Damage * 1.2 * 0.6, MidpointRounding.AwayFromZero));
        plain.Damage.Should().Be(Math.Round(unresisted.Damage * 0.6, MidpointRounding.AwayFromZero));
    }

    [Test]
    public void Fire_ADamageBonusOnAnotherTypeChangesNothing()
    {
        var weapon = SpaceTestShips.Laser(dmg: 60);
        var seed = FirstHitSeed(new SpaceScenarioSpec(), ShipSize.Small, ShipSize.Small);
        var plain = FireFrom(weapon, _ => { }, _ => { }, seed);
        var other = FireFrom(weapon, attacker => attacker.DamageBonusPercent.EM = 50, _ => { }, seed);

        other.Damage.Should().Be(plain.Damage);
    }

    [Test]
    public void Fire_AFullyResistedShotDoesNothingButAnyOtherHitDoesAtLeastOne()
    {
        var weapon = SpaceTestShips.Laser(dmg: 20);
        var seed = FirstHitSeed(new SpaceScenarioSpec(), ShipSize.Small, ShipSize.Small);

        FireFrom(weapon, _ => { }, target => target.Resistances.Thermal = 100, seed).Damage.Should().Be(0);
        FireFrom(weapon, _ => { }, target => target.Resistances.Thermal = 99, seed).Damage.Should().Be(1);
    }

    [Test]
    public void Fire_DamageIsWholePoints()
    {
        var weapon = SpaceTestShips.Laser(dmg: 37);
        var hits = Enumerable.Range(1, 100)
            .Select(seed => FireFrom(weapon, a => a.DamageBonusPercent.Thermal = 13, t => t.Resistances.Thermal = 7, seed))
            .Where(shot => shot.Hit)
            .ToList();

        hits.Should().NotBeEmpty();
        hits.Should().OnlyContain(shot => shot.Damage == Math.Floor(shot.Damage));
    }

    [Test]
    public void Fire_TheTargetsDamageTakenPercentAppliesAfterRounding()
    {
        var weapon = SpaceTestShips.Laser(dmg: 60);
        var seed = FirstHitSeed(new SpaceScenarioSpec(), ShipSize.Small, ShipSize.Small);
        var plain = FireFrom(weapon, _ => { }, _ => { }, seed);
        var painted = FireFrom(weapon, _ => { }, target => target.DamageTakenPercent = 10, seed);

        painted.Damage.Should().Be(SWLOR.Game.Server.Service.SpaceService.SpaceCombatMath.ApplyDamageTaken((int)plain.Damage, 10));
        painted.Damage.Should().BeGreaterThan(plain.Damage);
    }

    [Test]
    public void Fire_SustainedFireAddsDmgForEachHitOnTheSameTargetUpToTheMaximum()
    {
        var weapon = SpaceTestShips.Laser(dmg: 20);
        var spec = SpaceTestShips.Ship(weapons: weapon);
        spec.Accuracy = 1000;
        spec.EnergyRepeatedHitDmg = 3;
        spec.EnergyRepeatedHitMaxDmg = 15;
        var attacker = ShipState.Create(spec, "Attacker", 0);
        var first = ShipState.Create(SpaceTestShips.Ship(hull: 100000), "First", 1);
        var second = ShipState.Create(SpaceTestShips.Ship(hull: 100000), "Second", 1);
        var resolver = new ShotResolver(new SpaceScenarioSpec(), new System.Random(1));

        for (var shot = 0; shot < 7; shot++)
            resolver.Fire(attacker, weapon, 0, first);
        attacker.RepeatedHits[weapon].Stacks.Should().Be(5);
        SpaceShotMath.RegisterRepeatedHit(attacker, weapon, first).Should().Be(15);

        resolver.Fire(attacker, weapon, 0, second);
        attacker.RepeatedHits[weapon].Target.Should().BeSameAs(second);
        attacker.RepeatedHits[weapon].Stacks.Should().Be(1);
    }

    [Test]
    public void SustainedFire_EachWeaponOwnsItsStacksAndThreeWeaponsShareTwoWeaponsWorth()
    {
        var lasers = new[] { SpaceTestShips.Laser("Laser 1"), SpaceTestShips.Laser("Laser 2"), SpaceTestShips.Laser("Laser 3") };
        var spec = SpaceTestShips.Ship(weapons: lasers);
        spec.EnergyRepeatedHitDmg = 3;
        spec.EnergyRepeatedHitMaxDmg = 15;
        var attacker = ShipState.Create(spec, "Attacker", 0);
        var target = ShipState.Create(SpaceTestShips.Ship(hull: 100000), "Target", 1);

        // The first laser's own run: 3, 6 ... 15 DMG, each x 2 / 3 weapons.
        SpaceShotMath.RegisterRepeatedHit(attacker, lasers[0], target).Should().Be(2);
        SpaceShotMath.RegisterRepeatedHit(attacker, lasers[0], target).Should().Be(4);
        // The second laser starts its own run.
        SpaceShotMath.RegisterRepeatedHit(attacker, lasers[1], target).Should().Be(2);
        attacker.RepeatedHits[lasers[0]].Stacks.Should().Be(2);
        attacker.RepeatedHits[lasers[1]].Stacks.Should().Be(1);
    }

    [Test]
    public void SustainedFire_TwoEnergyWeaponsKeepTheFullBonus()
    {
        var lasers = new[] { SpaceTestShips.Laser("Laser 1"), SpaceTestShips.Laser("Laser 2") };
        var spec = SpaceTestShips.Ship(weapons: lasers);
        spec.EnergyRepeatedHitDmg = 3;
        spec.EnergyRepeatedHitMaxDmg = 15;
        var attacker = ShipState.Create(spec, "Attacker", 0);
        var target = ShipState.Create(SpaceTestShips.Ship(hull: 100000), "Target", 1);

        SpaceShotMath.RegisterRepeatedHit(attacker, lasers[0], target).Should().Be(3);
        SpaceShotMath.RegisterRepeatedHit(attacker, lasers[0], target).Should().Be(6);
    }

    [Test]
    public void Fire_ABlindedShipMissesSomeLandedShots()
    {
        var weapon = SpaceTestShips.Laser();
        var spec = SpaceTestShips.Ship(weapons: weapon);
        spec.Accuracy = 1000;
        var hits = 0;
        for (var seed = 1; seed <= 200; seed++)
        {
            var attacker = ShipState.Create(spec, "Attacker", 0);
            attacker.BlindedUntil = 10;
            var target = ShipState.Create(SpaceTestShips.Ship(hull: 100000), "Target", 1);
            var resolver = new ShotResolver(new SpaceScenarioSpec(), new System.Random(seed)) { Now = 5 };
            if (resolver.Fire(attacker, weapon, 0, target).Hit)
                hits++;
        }

        hits.Should().BeInRange(60, 140, "about half of the landed shots miss while blinded");
    }

    [Test]
    public void Fire_ProximityWarheadsSplashOnlyFromALaunchersOwnCycle()
    {
        var launcher = SpaceTestShips.Laser("Missile");
        launcher.Family = ModuleFamily.Launcher;
        var spec = SpaceTestShips.Ship(weapons: launcher);
        spec.Accuracy = 1000;
        spec.LauncherSplashChance = 100;
        spec.LauncherSplashDamage = 8;

        var attacker = ShipState.Create(spec, "Attacker", 0);
        var target = ShipState.Create(SpaceTestShips.Ship(hull: 100000), "Target", 1);
        var shot = new ShotResolver(new SpaceScenarioSpec(), new System.Random(1)).Fire(attacker, launcher, 0, target);
        (100000 - target.Hull).Should().Be(shot.Damage + 8);

        var abilityAttacker = ShipState.Create(spec, "Attacker", 0);
        var abilityTarget = ShipState.Create(SpaceTestShips.Ship(hull: 100000), "Target", 1);
        var abilityShot = new ShotResolver(new SpaceScenarioSpec(), new System.Random(1)).Fire(abilityAttacker, launcher, 0, abilityTarget, isAbility: true);
        (100000 - abilityTarget.Hull).Should().Be(abilityShot.Damage);
    }

    [Test]
    public void CriticalChance_AddsTheShipsCriticalRatePercent()
    {
        var weapon = SpaceTestShips.Laser();
        var plain = ShipState.Create(SpaceTestShips.Ship(weapons: weapon), "Plain", 0);
        var spec = SpaceTestShips.Ship(weapons: weapon);
        spec.CriticalRatePercent = 10;
        var sharp = ShipState.Create(spec, "Sharp", 0);
        var target = ShipState.Create(SpaceTestShips.Ship(), "Target", 1);
        var scenario = new SpaceScenarioSpec();

        SpaceShotMath.CriticalChance(scenario, sharp, weapon, target)
            .Should().Be(SpaceShotMath.CriticalChance(scenario, plain, weapon, target) + 10);
    }

    private static ShotResult FireFrom(WeaponModuleSpec weapon, Action<ShipSpec> changeAttacker, Action<ShipSpec> changeTarget, int seed)
    {
        var attackerSpec = SpaceTestShips.Ship(weapons: weapon);
        changeAttacker(attackerSpec);
        var targetSpec = SpaceTestShips.Ship(hull: 100000);
        changeTarget(targetSpec);
        var attacker = ShipState.Create(attackerSpec, "Attacker", 0);
        var target = ShipState.Create(targetSpec, "Target", 1);
        return new ShotResolver(new SpaceScenarioSpec(), new System.Random(seed)).Fire(attacker, weapon, 0, target);
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
