using SWLOR.CombatSim.SpaceCombat.Scenario;

namespace SWLOR.CombatSim.SpaceCombat.Engine;

/// <summary>A damage over time effect on a ship: who left it, its numbers, its next tick and when it ends.</summary>
public sealed class ActiveDot
{
    public ShipState Source { get; init; }
    public DotSpec Spec { get; init; }
    public double NextTickAt { get; set; }
    public double ExpiresAt { get; set; }
}
