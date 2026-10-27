using SWLOR.CombatSim.SpaceCombat.Scenario;

namespace SWLOR.CombatSim.SpaceCombat.Engine;

/// <summary>
/// Resolves one shot in the plan's order: hit, damage, critical, size damage penalty, resistance, then shield and hull.
/// Each shot draws the random numbers in that order (hit, critical, damage), and a miss draws only the first.
/// </summary>
public sealed class ShotResolver
{
    private readonly SpaceScenarioSpec _scenario;
    private readonly System.Random _random;

    public ShotResolver(SpaceScenarioSpec scenario, System.Random random)
    {
        _scenario = scenario;
        _random = random;
    }

    public ShotResult Fire(ShipState attacker, WeaponModuleSpec weapon, int bonusDmg, ShipState target)
    {
        if (_random.Next(1, 101) > SpaceShotMath.HitChance(_scenario, attacker, weapon, target))
            return new ShotResult(false, false, 0, 0);

        var critical = _random.Next(1, 101) <= SpaceShotMath.CriticalChance(_scenario, attacker, weapon, target);
        var (minimum, maximum) = SpaceShotMath.DamageRange(attacker, weapon, bonusDmg, target, critical);
        var damage = SWLOR.CombatSim.Engine.Simulation.SampleWeaponDamage(_random, minimum, maximum)
                     * SpaceShotMath.SizeDamageMultiplier(_scenario, attacker, weapon, target)
                     * SpaceShotMath.ResistanceMultiplier(target, weapon.DamageType);

        var applied = target.TakeDamage(damage, weapon.ShieldBypass, weapon.ShieldDamageMultiplier);
        attacker.DamageDealt += applied;
        return new ShotResult(true, critical, damage, applied);
    }
}
