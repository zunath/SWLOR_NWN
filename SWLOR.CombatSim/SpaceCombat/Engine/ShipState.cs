using SWLOR.CombatSim.SpaceCombat.Scenario;

namespace SWLOR.CombatSim.SpaceCombat.Engine;

/// <summary>One ship during a fight: its totals with passives added, its pools and its module clocks.</summary>
public sealed class ShipState
{
    private readonly int[] _resistances = new int[3];

    private ShipState(ShipSpec spec, string name, int sideIndex)
    {
        Spec = spec;
        Name = name;
        SideIndex = sideIndex;
    }

    public ShipSpec Spec { get; }
    public string Name { get; }
    public int SideIndex { get; }
    public ShipSize Size => Spec.Size;
    public PilotSpec Pilot => Spec.Pilot;

    public double MaxHull { get; private set; }
    public double MaxShield { get; private set; }
    public double MaxCapacitor => Spec.MaxCapacitor;
    public double Hull { get; set; }
    public double Shield { get; set; }
    public double Capacitor { get; set; }
    public double Stm { get; set; }

    /// <summary>Ship Defense plus passives.</summary>
    public int Defense { get; private set; }

    /// <summary>Ship Evasion plus passives plus the pilot's AGI x 3.</summary>
    public int Evasion { get; private set; }

    public bool IsAlive => Hull > 0;

    /// <summary>Shield and hull points this ship's shots removed from enemies.</summary>
    public double DamageDealt { get; set; }

    public List<(WeaponModuleSpec Spec, ModuleClock Clock)> Weapons { get; } = new();
    public List<(RepairerModuleSpec Spec, ModuleClock Clock)> Repairers { get; } = new();
    public List<(BurstAbilitySpec Spec, ModuleClock Clock)> Abilities { get; } = new();

    /// <summary>Resistance percent to a damage type, passives included, limited to -100..100.</summary>
    public int Resistance(SpaceDamageType type) => _resistances[(int)type];

    public static ShipState Create(ShipSpec spec, string name, int sideIndex)
    {
        var ship = new ShipState(spec, name, sideIndex)
        {
            MaxHull = spec.MaxHull + spec.Passives.Sum(p => p.Hull),
            MaxShield = spec.MaxShield + spec.Passives.Sum(p => p.Shield),
            Defense = spec.Defense + spec.Passives.Sum(p => p.Defense),
            Evasion = spec.Evasion + spec.Passives.Sum(p => p.Evasion) + spec.Pilot.Agi * 3
        };

        foreach (var type in Enum.GetValues<SpaceDamageType>())
        {
            var total = spec.Resistances.Get(type) + spec.Passives.Sum(p => p.Resistances.Get(type));
            ship._resistances[(int)type] = Math.Clamp(total, SpaceResistances.Minimum, SpaceResistances.Maximum);
        }

        ship.Hull = ship.MaxHull;
        ship.Shield = ship.MaxShield;
        ship.Capacitor = spec.MaxCapacitor;
        ship.Stm = spec.Pilot.MaxStm;

        foreach (var weapon in spec.Weapons)
            ship.Weapons.Add((weapon, new ModuleClock(weapon.CycleSeconds)));
        foreach (var repairer in spec.Repairers)
            ship.Repairers.Add((repairer, new ModuleClock(repairer.CycleSeconds)));
        foreach (var ability in spec.Abilities)
            ship.Abilities.Add((ability, new ModuleClock(ability.CooldownSeconds)));

        return ship;
    }

    /// <summary>
    /// Applies damage that has already been through every other stage. It goes to the shield first (the shield loses
    /// the damage times <paramref name="shieldDamageMultiplier"/>) and the part the shield could not take goes to the
    /// hull; <paramref name="shieldBypass"/> sends all of it to the hull. Returns the shield and hull points removed,
    /// which leaves out damage beyond the hull's last point.
    /// </summary>
    public double TakeDamage(double damage, bool shieldBypass, double shieldDamageMultiplier)
    {
        if (damage <= 0 || !IsAlive)
            return 0;

        var removed = 0.0;
        var toHull = damage;
        if (!shieldBypass && Shield > 0)
        {
            var shieldCost = damage * shieldDamageMultiplier;
            if (Shield >= shieldCost)
            {
                Shield -= shieldCost;
                return shieldCost;
            }

            toHull = damage - Shield / shieldDamageMultiplier;
            removed += Shield;
            Shield = 0;
        }

        var hullLoss = Math.Min(Hull, toHull);
        Hull -= hullLoss;
        return removed + hullLoss;
    }

    /// <summary>Continuous regeneration for one time step.</summary>
    public void Regenerate(double seconds)
    {
        Shield = Math.Min(MaxShield, Shield + Spec.ShieldRegenPerSecond * seconds);
        Capacitor = Math.Min(MaxCapacitor, Capacitor + Spec.CapacitorRegenPerSecond * seconds);
        Stm = Math.Min(Pilot.MaxStm, Stm + Pilot.StmRegenPerSecond * seconds);
    }
}
