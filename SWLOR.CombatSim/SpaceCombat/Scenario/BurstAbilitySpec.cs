namespace SWLOR.CombatSim.SpaceCombat.Scenario;

/// <summary>
/// A one-press burst: one shot shaped like a fitted weapon's (its family, damage type, rank, size, flags and
/// Attack/Accuracy) with extra DMG. It is used whenever it is off cooldown and its cost can be paid, at the
/// first living enemy, and does not use up the weapon's own cycle.
/// </summary>
public sealed class BurstAbilitySpec
{
    public string Name { get; set; }

    /// <summary>Name of the fitted weapon whose shot profile this burst borrows.</summary>
    public string Weapon { get; set; }

    public int BonusDmg { get; set; }
    public double CooldownSeconds { get; set; } = 20;
    public double CapacitorCost { get; set; }
    public double StmCost { get; set; }
}
