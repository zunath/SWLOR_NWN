namespace SWLOR.CombatSim.SpaceCombat.Scenario;

/// <summary>The pilot's attributes. Pilot rank adds nothing in space (plan section 9), so there is no rank here.</summary>
public sealed class PilotSpec
{
    public int Agi { get; set; } = 10;
    public int Per { get; set; } = 10;
    public int Vit { get; set; } = 10;
    public int Wil { get; set; } = 10;
    public int Mgt { get; set; } = 10;

    /// <summary>Largest pool of stamina (STM) that burst abilities may spend; 0 for none.</summary>
    public double MaxStm { get; set; }

    public double StmRegenPerSecond { get; set; }
}
