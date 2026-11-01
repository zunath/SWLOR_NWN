namespace SWLOR.CombatSim.SpaceCombat.Scenario;

/// <summary>A passive module: flat bonuses added to the ship's stats.</summary>
public sealed class PassiveModuleSpec
{
    public string Name { get; set; }
    public double Hull { get; set; }
    public double Shield { get; set; }
    public int Defense { get; set; }
    public int Evasion { get; set; }

    /// <summary>Percentage points added to the ship's resistances; the total is limited to -100..100.</summary>
    public SpaceResistances Resistances { get; set; } = new();
}
