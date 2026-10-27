namespace SWLOR.CombatSim.SpaceCombat.Scenario;

/// <summary>One ship: base stats from its hull, the pilot's attributes and the fitted modules.</summary>
public sealed class ShipSpec
{
    public string Name { get; set; }

    /// <summary>How many identical ships; they are numbered Name 1, Name 2... when more than one.</summary>
    public int Count { get; set; } = 1;

    public ShipSize Size { get; set; } = ShipSize.Small;
    public double MaxHull { get; set; }
    public double MaxShield { get; set; }
    public double MaxCapacitor { get; set; }
    public double ShieldRegenPerSecond { get; set; }
    public double CapacitorRegenPerSecond { get; set; }
    public int Attack { get; set; }
    public int Accuracy { get; set; }
    public int Defense { get; set; }
    public int Evasion { get; set; }
    public SpaceResistances Resistances { get; set; } = new();
    public PilotSpec Pilot { get; set; } = new();
    public List<WeaponModuleSpec> Weapons { get; set; } = new();
    public List<RepairerModuleSpec> Repairers { get; set; } = new();
    public List<PassiveModuleSpec> Passives { get; set; } = new();
    public List<BurstAbilitySpec> Abilities { get; set; } = new();
}
