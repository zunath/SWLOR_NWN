namespace SWLOR.CombatSim.SpaceCombat.Scenario;

/// <summary>
/// Damage over time an ability leaves on each ship it hits (Burn, fragmentation). It ticks every period after it lands
/// for its duration; landing it again refreshes the duration and keeps the tick schedule, as the ground status effects
/// do. Each tick deals damage + 0..damageSpread to the shield, then the hull, untouched by the ship's resistances.
/// </summary>
public sealed class DotSpec
{
    public string Name { get; set; }
    public int Damage { get; set; }
    public int DamageSpread { get; set; }
    public double PeriodSeconds { get; set; } = 6;
    public double DurationSeconds { get; set; } = 30;
}
