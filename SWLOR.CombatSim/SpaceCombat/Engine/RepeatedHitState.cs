namespace SWLOR.CombatSim.SpaceCombat.Engine;

/// <summary>Sustained Fire on one weapon: the ship its last hit landed on and the stacks built on it.</summary>
public sealed class RepeatedHitState
{
    public ShipState Target { get; set; }
    public int Stacks { get; set; }
}
