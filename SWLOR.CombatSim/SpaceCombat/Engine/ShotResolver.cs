using SWLOR.CombatSim.SpaceCombat.Scenario;
using SpaceCombatMath = SWLOR.Game.Server.Service.SpaceService.SpaceCombatMath;

namespace SWLOR.CombatSim.SpaceCombat.Engine;

/// <summary>
/// Resolves one shot in the server's order (Space.RollWeaponShot): hit (and Blind's extra miss), critical, Sustained
/// Fire DMG, damage roll, critical damage percent, damage-dealt percent, then size penalty x (1 + damage bonus %) x
/// (1 - resistance %), rounded half away from zero with a 1-damage minimum, then the target's damage-taken percent,
/// then shield and hull. A launcher's own cycle hit may then add Proximity Warheads' splash (SpaceOrdnance). Each shot
/// draws the random numbers in that order, and a miss draws only the first.
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

    /// <summary>The fight time of the step being resolved (for Blind).</summary>
    public double Now { get; set; }

    public System.Random Random => _random;

    public ShotResult Fire(ShipState attacker, WeaponModuleSpec weapon, int bonusDmg, ShipState target, bool isAbility = false)
    {
        if (_random.Next(1, 101) > SpaceShotMath.HitChance(_scenario, attacker, weapon, target))
            return new ShotResult(false, false, 0, 0);

        // A blinded ship's landed shot misses half the time (SpaceCombatMath.BlindMissChance).
        if (attacker.BlindedUntil > Now && _random.Next(1, 101) <= SpaceCombatMath.BlindMissChance)
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

        // Proximity Warheads: only a launcher's own cycles splash.
        if (!isAbility && weapon.Family == ModuleFamily.Launcher && attacker.Spec.LauncherSplashChance > 0 &&
            attacker.Spec.LauncherSplashDamage > 0 && target.IsAlive &&
            _random.Next(1, 101) <= attacker.Spec.LauncherSplashChance)
        {
            attacker.DamageDealt += target.TakeDamage(attacker.Spec.LauncherSplashDamage, false, 1.0);
        }

        return new ShotResult(true, critical, damage, applied);
    }
}
