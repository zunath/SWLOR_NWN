namespace SWLOR.CombatSim.SpaceCombat.Scenario;

/// <summary>One side of the fight. A passive side fires no weapons or abilities (the "NPC does not fight back" baseline); its repairers still run.</summary>
public sealed class SpaceSideSpec
{
    public string Name { get; set; }
    public bool Passive { get; set; }
    public List<ShipSpec> Ships { get; set; } = new();
}
