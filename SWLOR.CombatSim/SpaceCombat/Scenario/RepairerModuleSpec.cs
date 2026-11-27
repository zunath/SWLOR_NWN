namespace SWLOR.CombatSim.SpaceCombat.Scenario;

/// <summary>A toggled repairer working on its own ship.</summary>
public sealed class RepairerModuleSpec
{
    public string Name { get; set; }
    public RepairTarget Target { get; set; }
    public double AmountPerCycle { get; set; }
    public double CycleSeconds { get; set; } = 4;
    public double CapacitorPerCycle { get; set; }
}
