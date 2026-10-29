namespace SWLOR.CombatSim.SpaceCombat.Scenario;

/// <summary>Rejects a space scenario whose numbers could not be simulated, naming the first problem.</summary>
public static class SpaceScenarioValidator
{
    private const double MinimumCycleSeconds = 0.01;

    public static void Validate(SpaceScenarioSpec scenario)
    {
        if (!(scenario.DurationSeconds >= 1 && scenario.DurationSeconds <= 3600))
            throw Invalid("durationSeconds must be from 1 to 3600.");
        if (!(scenario.TimeStepSeconds >= 0.01 && scenario.TimeStepSeconds <= 1))
            throw Invalid("timeStepSeconds must be from 0.01 to 1.");
        if (!(scenario.SizeHitPenaltyPerStep >= 0 && scenario.SizeHitPenaltyPerStep <= 100))
            throw Invalid("sizeHitPenaltyPerStep must be a whole number of percentage points from 0 to 100.");
        if (!(scenario.SizeDamagePenaltyPerStep >= 0 && scenario.SizeDamagePenaltyPerStep <= 1))
            throw Invalid("sizeDamagePenaltyPerStep must be from 0 to 1.");

        var minimum = scenario.CriticalRateMinimum ?? 0;
        var maximum = scenario.CriticalRateMaximum ?? 100;
        if (minimum < 0 || maximum > 100 || minimum > maximum)
            throw Invalid("criticalRateMinimum and criticalRateMaximum must satisfy 0 <= minimum <= maximum <= 100.");

        ValidateSide(scenario.Attackers, "attackers");
        ValidateSide(scenario.Defenders, "defenders");
    }

    private static void ValidateSide(SpaceSideSpec side, string key)
    {
        if (side?.Ships == null || side.Ships.Count == 0)
            throw Invalid($"{key} needs at least one ship.");

        var total = 0;
        foreach (var ship in side.Ships)
        {
            if (ship == null)
                throw Invalid($"{key} has an empty ship entry.");
            if (ship.Count < 1)
                throw Invalid($"Ship '{ship.Name}' needs a count of at least 1.");

            total += ship.Count;
            ValidateShip(ship);
        }

        if (total > SpaceScenarioSpec.MaximumShipsPerSide)
            throw Invalid($"{key} has {total} ships; the limit is {SpaceScenarioSpec.MaximumShipsPerSide}.");
    }

    private static void ValidateShip(ShipSpec ship)
    {
        if (string.IsNullOrWhiteSpace(ship.Name))
            throw Invalid("Every ship needs a name.");

        var label = $"Ship '{ship.Name}'";
        if (!Enum.IsDefined(ship.Size))
            throw Invalid($"{label} has an unknown size.");
        if (!(ship.MaxHull > 0))
            throw Invalid($"{label} needs maxHull above 0.");

        RequireNotNegative(label, "maxShield", ship.MaxShield);
        RequireNotNegative(label, "maxCapacitor", ship.MaxCapacitor);
        RequireNotNegative(label, "shieldRegenPerSecond", ship.ShieldRegenPerSecond);
        RequireNotNegative(label, "capacitorRegenPerSecond", ship.CapacitorRegenPerSecond);
        RequireNotNegative(label, "pilot.maxStm", ship.Pilot.MaxStm);
        RequireNotNegative(label, "pilot.stmRegenPerSecond", ship.Pilot.StmRegenPerSecond);
        RequireResistances(label, ship.Resistances);

        var weapons = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var weapon in ship.Weapons)
        {
            if (string.IsNullOrWhiteSpace(weapon.Name))
                throw Invalid($"{label} has a weapon without a name.");

            var name = $"{label} weapon '{weapon.Name}'";
            if (!weapons.Add(weapon.Name))
                throw Invalid($"{label} has two weapons named '{weapon.Name}'.");
            if (weapon.Rank is < 0 or > 50)
                throw Invalid($"{name} needs a rank from 0 to 50.");
            if (weapon.Dmg < 0)
                throw Invalid($"{name} needs dmg of at least 0.");
            if (!(weapon.CycleSeconds >= MinimumCycleSeconds))
                throw Invalid($"{name} needs cycleSeconds of at least {MinimumCycleSeconds}.");
            if (weapon.Size.HasValue && !Enum.IsDefined(weapon.Size.Value))
                throw Invalid($"{name} has an unknown size.");
            if (!Enum.IsDefined(weapon.Family))
                throw Invalid($"{name} has an unknown family.");
            if (!Enum.IsDefined(weapon.DamageType))
                throw Invalid($"{name} has an unknown damageType.");
            if (!(weapon.ShieldDamageMultiplier > 0) || double.IsInfinity(weapon.ShieldDamageMultiplier))
                throw Invalid($"{name} needs shieldDamageMultiplier above 0.");
            RequireNotNegative(name, "capacitorPerCycle", weapon.CapacitorPerCycle);
        }

        foreach (var repairer in ship.Repairers)
        {
            var name = $"{label} repairer '{repairer.Name}'";
            if (!(repairer.CycleSeconds >= MinimumCycleSeconds))
                throw Invalid($"{name} needs cycleSeconds of at least {MinimumCycleSeconds}.");
            RequireNotNegative(name, "amountPerCycle", repairer.AmountPerCycle);
            RequireNotNegative(name, "capacitorPerCycle", repairer.CapacitorPerCycle);
        }

        foreach (var passive in ship.Passives)
        {
            var name = $"{label} passive '{passive.Name}'";
            RequireNotNegative(name, "hull", passive.Hull);
            RequireNotNegative(name, "shield", passive.Shield);
            RequireResistances(name, passive.Resistances);
        }

        foreach (var ability in ship.Abilities)
        {
            var name = $"{label} ability '{ability.Name}'";
            if (!weapons.Contains(ability.Weapon ?? string.Empty))
                throw Invalid($"{name} borrows weapon '{ability.Weapon}', which this ship does not fit.");
            if (!(ability.CooldownSeconds >= MinimumCycleSeconds))
                throw Invalid($"{name} needs cooldownSeconds of at least {MinimumCycleSeconds}.");
            RequireNotNegative(name, "capacitorCost", ability.CapacitorCost);
            RequireNotNegative(name, "stmCost", ability.StmCost);
        }
    }

    private static void RequireNotNegative(string label, string key, double value)
    {
        if (!(value >= 0) || double.IsInfinity(value))
            throw Invalid($"{label} needs {key} of at least 0.");
    }

    private static void RequireResistances(string label, SpaceResistances resistances)
    {
        foreach (var value in new[] { resistances.EM, resistances.Thermal, resistances.Explosive })
        {
            if (value is < SpaceResistances.Minimum or > SpaceResistances.Maximum)
                throw Invalid($"{label} has a resistance outside {SpaceResistances.Minimum} to {SpaceResistances.Maximum}.");
        }
    }

    private static InvalidDataException Invalid(string message) => new(message);
}
