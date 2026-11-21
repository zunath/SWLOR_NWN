using SWLOR.CombatSim.SpaceCombat.Scenario;

namespace SWLOR.CombatSim.SpaceCombat.Engine;

/// <summary>
/// One seeded fight between the scenario's two sides on a fixed time step. In each step every ship that is alive
/// at its start acts, so ships that destroy each other in the same step both get their shots in. Each ship
/// fires at the first living enemy.
/// </summary>
public sealed class SpaceFight
{
    public const int AttackerSide = 0;
    public const int DefenderSide = 1;

    private readonly SpaceScenarioSpec _scenario;
    private readonly ShotResolver _resolver;
    private readonly bool[] _passive;

    public SpaceFight(SpaceScenarioSpec scenario, int seed)
    {
        _scenario = scenario;
        _resolver = new ShotResolver(scenario, new System.Random(seed));
        _passive = new[] { scenario.Attackers.Passive, scenario.Defenders.Passive };
        Ships = Spawn(scenario.Attackers, AttackerSide).Concat(Spawn(scenario.Defenders, DefenderSide)).ToList();
    }

    public IReadOnlyList<ShipState> Ships { get; }

    private static IEnumerable<ShipState> Spawn(SpaceSideSpec side, int sideIndex)
    {
        foreach (var spec in side.Ships)
        {
            for (var number = 1; number <= spec.Count; number++)
                yield return ShipState.Create(spec, spec.Count > 1 ? $"{spec.Name} {number}" : spec.Name, sideIndex);
        }
    }

    public SpaceFightResult Run()
    {
        var steps = (int)Math.Ceiling(_scenario.DurationSeconds / _scenario.TimeStepSeconds - 1e-9);
        double? defendersDestroyedAt = null;
        var endedAt = _scenario.DurationSeconds;

        for (var step = 0; step < steps; step++)
        {
            var now = step * _scenario.TimeStepSeconds;
            Step(now);

            var attackersAlive = Ships.Any(s => s.SideIndex == AttackerSide && s.IsAlive);
            var defendersAlive = Ships.Any(s => s.SideIndex == DefenderSide && s.IsAlive);
            if (!defendersAlive)
                defendersDestroyedAt = now;
            if (!attackersAlive || !defendersAlive)
            {
                endedAt = now;
                break;
            }
        }

        var attackers = Ships.Where(s => s.SideIndex == AttackerSide).ToList();
        return new SpaceFightResult
        {
            EndedAt = endedAt,
            DefendersDestroyedAt = defendersDestroyedAt,
            AttackersWon = defendersDestroyedAt.HasValue && attackers.Any(s => s.IsAlive),
            AttackerHullRemaining = attackers.Sum(s => s.Hull),
            AttackerShieldRemaining = attackers.Sum(s => s.Shield),
            AttackerMaxHull = attackers.Sum(s => s.MaxHull),
            AttackerMaxShield = attackers.Sum(s => s.MaxShield),
            DamageDealtByAttackers = attackers.Sum(s => s.DamageDealt),
            DamageDealtByDefenders = Ships.Where(s => s.SideIndex == DefenderSide).Sum(s => s.DamageDealt)
        };
    }

    /// <summary>
    /// Runs one step starting at <paramref name="now"/> seconds: modules that are due act, then pools regenerate, unless
    /// the step ended the fight, and never past the scenario's duration.
    /// </summary>
    public void Step(double now)
    {
        _resolver.Now = now;
        foreach (var ship in Ships)
            ship.Now = now;
        var actors = Ships.Where(s => s.IsAlive).ToList();
        foreach (var ship in actors)
            RunDots(ship, now);

        foreach (var ship in actors)
        {
            // A ship destroyed earlier in this step still fires its shots, but cannot repair itself back.
            if (ship.IsAlive)
                RunRepairers(ship, now);
            if (_passive[ship.SideIndex])
                continue;

            RunWeapons(ship, now);
            RunAbilities(ship, now);
        }

        // A fight that ended in this step stops here, so the result reads the pools as the killing shot left them.
        if (!Ships.Any(s => s.SideIndex == AttackerSide && s.IsAlive) || !Ships.Any(s => s.SideIndex == DefenderSide && s.IsAlive))
            return;

        var seconds = Math.Min(_scenario.TimeStepSeconds, _scenario.DurationSeconds - now);
        if (seconds <= 0)
            return;

        foreach (var ship in actors.Where(s => s.IsAlive))
            ship.Regenerate(seconds);
    }

    private ShipState FirstLivingEnemy(ShipState ship) =>
        Ships.FirstOrDefault(s => s.SideIndex != ship.SideIndex && s.IsAlive);

    private static void RunRepairers(ShipState ship, double now)
    {
        foreach (var (spec, clock) in ship.Repairers)
        {
            while (clock.IsDue(now))
            {
                clock.AdvanceCycle();
                var missing = spec.Target == RepairTarget.Hull ? ship.MaxHull - ship.Hull : ship.MaxShield - ship.Shield;

                // A repairer with nothing to repair idles instead of burning capacitor.
                if (missing <= 0)
                    continue;

                if (ship.Capacitor < spec.CapacitorPerCycle)
                {
                    clock.Skipped++;
                    continue;
                }

                ship.Capacitor -= spec.CapacitorPerCycle;
                clock.Fired++;
                if (spec.Target == RepairTarget.Hull)
                    ship.Hull += Math.Min(missing, spec.AmountPerCycle);
                else
                    ship.Shield += Math.Min(missing, spec.AmountPerCycle);
            }
        }
    }

    private void RunWeapons(ShipState ship, double now)
    {
        foreach (var (spec, clock) in ship.Weapons)
        {
            while (clock.IsDue(now))
            {
                clock.AdvanceCycle();
                var target = FirstLivingEnemy(ship);
                if (target == null)
                    return;

                if (ship.Capacitor < spec.CapacitorPerCycle)
                {
                    clock.Skipped++;
                    continue;
                }

                ship.Capacitor -= spec.CapacitorPerCycle;
                clock.Fired++;
                _resolver.Fire(ship, spec, 0, target);
            }
        }
    }

    /// <summary>Damage over time that is due: each tick goes to the shield, then the hull.</summary>
    private void RunDots(ShipState ship, double now)
    {
        foreach (var dot in ship.Dots.ToList())
        {
            while (ship.IsAlive && dot.NextTickAt <= dot.ExpiresAt + 1e-9 && now >= dot.NextTickAt - 1e-9)
            {
                var amount = dot.Spec.Damage + (dot.Spec.DamageSpread > 0 ? _resolver.Random.Next(0, dot.Spec.DamageSpread + 1) : 0);
                dot.Source.DamageDealt += ship.TakeDamage(Math.Max(1, amount), false, 1.0);
                dot.NextTickAt += dot.Spec.PeriodSeconds;
            }

            if (dot.NextTickAt > dot.ExpiresAt + 1e-9)
                ship.Dots.Remove(dot);
        }
    }

    private static void ApplyOnHit(ShipState source, BurstAbilitySpec spec, ShipState target, double now)
    {
        foreach (var dotSpec in spec.Dots)
        {
            var dot = target.Dots.FirstOrDefault(d => d.Source == source && d.Spec.Name == dotSpec.Name);
            if (dot == null)
                target.Dots.Add(new ActiveDot
                {
                    Source = source, Spec = dotSpec, NextTickAt = now + dotSpec.PeriodSeconds, ExpiresAt = now + dotSpec.DurationSeconds
                });
            else
                dot.ExpiresAt = now + dotSpec.DurationSeconds;
        }

        if (spec.BlindSeconds > 0)
            target.BlindedUntil = Math.Max(target.BlindedUntil, now + spec.BlindSeconds);

        // Saturation Fire (SpaceHitSequences): the second area launcher hit within the window exposes the target.
        if (spec.AreaLauncher && source.Spec.SaturationExposedDefensePercent > 0)
        {
            var count = target.AreaHitSequences.TryGetValue(source, out var seq) && now - seq.LastHit <= SaturationWindowSeconds
                ? seq.Count + 1
                : 1;
            if (count >= SaturationHitsRequired)
            {
                target.AreaHitSequences.Remove(source);
                target.ExposedUntil = now + SaturationExposedSeconds;
                target.ExposedDefensePercent = source.Spec.SaturationExposedDefensePercent;
            }
            else
            {
                target.AreaHitSequences[source] = (count, now);
            }
        }
    }

    // Saturation Fire's numbers (PilotingOrdnancePerkDefinition: 2 hits within 30 s, Exposed for 30 s).
    private const int SaturationHitsRequired = 2;
    private const double SaturationWindowSeconds = 30;
    private const double SaturationExposedSeconds = 30;

    private void RunAbilities(ShipState ship, double now)
    {
        foreach (var (spec, clock) in ship.Abilities)
        {
            var target = FirstLivingEnemy(ship);
            if (target == null || !clock.IsDue(now) || ship.Capacitor < spec.CapacitorCost || ship.Stm < spec.StmCost)
                continue;

            var weapon = ship.Weapons.First(w => string.Equals(w.Spec.Name, spec.Weapon, StringComparison.OrdinalIgnoreCase)).Spec;
            ship.Capacitor -= spec.CapacitorCost;
            ship.Stm -= spec.StmCost;
            clock.StartCooldown(now);
            clock.Fired++;
            for (var shot = 0; shot < Math.Max(1, spec.Shots); shot++)
            {
                if (!target.IsAlive)
                    break;
                if (_resolver.Fire(ship, weapon, spec.BonusDmg, target, isAbility: true).Hit && target.IsAlive)
                    ApplyOnHit(ship, spec, target, now);
            }
        }
    }
}
