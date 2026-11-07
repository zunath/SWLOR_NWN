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

    /// <summary>Shots the press fires, each rolled on its own (Double Shot fires two).</summary>
    public int Shots { get; set; } = 1;

    /// <summary>Damage over time left on the target by each hit (Volley's Burn, Shrapnel Warheads' fragmentation).</summary>
    public List<DotSpec> Dots { get; set; } = new();

    /// <summary>Seconds of Blind a hit leaves on the target (Flare Missile): half its landed shots miss meanwhile.</summary>
    public double BlindSeconds { get; set; }

    /// <summary>An area launcher ability: its hits count toward Saturation Fire (SpaceOrdnance.ApplyHitSequence).</summary>
    public bool AreaLauncher { get; set; }
    public double CooldownSeconds { get; set; } = 20;
    public double CapacitorCost { get; set; }
    public double StmCost { get; set; }
}
