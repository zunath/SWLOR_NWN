using SWLOR.CombatSim.SpaceCombat.Scenario;
using SpaceCombatMath = SWLOR.Game.Server.Service.SpaceService.SpaceCombatMath;

namespace SWLOR.CombatSim.SpaceCombat.Engine;

/// <summary>
/// Resolves one shot in the server's order (Space.RollWeaponShot): hit, critical, Sustained Fire DMG, damage roll,
/// critical damage percent, damage-dealt percent, then size penalty x (1 + damage bonus %) x (1 - resistance %),
/// rounded half away from zero with a 1-damage minimum, then the target's damage-taken percent, then shield and hull.
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
        bonusDmg += SpaceShotMath.RegisterRepeatedHit(attacker, weapon, target);
        var (minimum, maximum) = SpaceShotMath.DamageRange(attacker, weapon, bonusDmg, target, critical);
        var rolled = SWLOR.CombatSim.Engine.Simulation.SampleWeaponDamage(_random, minimum, maximum);
        rolled = SpaceCombatMath.CriticalDamage(rolled, critical, attacker.Spec.CriticalDamagePercent);
        rolled = SpaceCombatMath.ApplyDamageDealt(rolled, attacker.Spec.DamageDealtPercent, 0);

        var raw = rolled
                  * SpaceShotMath.SizeDamageMultiplier(_scenario, attacker, weapon, target)
                  * SpaceShotMath.DamageBonusMultiplier(attacker, weapon.DamageType)
                  * SpaceShotMath.ResistanceMultiplier(target, weapon.DamageType);

        var damage = (int)Math.Round(raw, MidpointRounding.AwayFromZero);
        // A shot that isn't fully resisted always does something.
        if (damage < 1 && raw > 0)
            damage = 1;
        if (target.Spec.DamageTakenPercent != 0)
            damage = SpaceCombatMath.ApplyDamageTaken(damage, target.Spec.DamageTakenPercent);

        var applied = target.TakeDamage(damage, weapon.ShieldBypass, weapon.ShieldDamageMultiplier);
        attacker.DamageDealt += applied;
        return new ShotResult(true, critical, damage, applied);
    }
}
