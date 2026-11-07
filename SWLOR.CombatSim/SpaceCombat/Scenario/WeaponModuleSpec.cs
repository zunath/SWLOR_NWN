namespace SWLOR.CombatSim.SpaceCombat.Scenario;

/// <summary>A toggled weapon: fires itself every cycle while the capacitor can pay.</summary>
public sealed class WeaponModuleSpec
{
    public string Name { get; set; }
    public ModuleFamily Family { get; set; }
    public SpaceDamageType DamageType { get; set; }

    /// <summary>The ground weapon damage value fed into Combat.CalculateDamageRange.</summary>
    public int Dmg { get; set; }

    /// <summary>Module rank 0-50. Rank / 10 is the critical formula's weapon rank, and the damage formula's delta cap derives from it as on the ground.</summary>
    public int Rank { get; set; }

    public double CycleSeconds { get; set; } = 2;
    public double CapacitorPerCycle { get; set; }
    public int Attack { get; set; }
    public int Accuracy { get; set; }

    /// <summary>The weapon's size; the fitted ship's size when not given.</summary>
    public ShipSize? Size { get; set; }

    /// <summary>Torpedoes: damage skips the shield and goes to the hull.</summary>
    public bool ShieldBypass { get; set; }

    /// <summary>Torpedoes: neither size penalty applies.</summary>
    public bool IgnoreSizePenalty { get; set; }

    /// <summary>Damage dealt to a shield is multiplied by this (ion cannons: above 1). Shield absorbs damage times this, so a 2 means the shield loses twice what the hull would.</summary>
    public double ShieldDamageMultiplier { get; set; } = 1;
}
