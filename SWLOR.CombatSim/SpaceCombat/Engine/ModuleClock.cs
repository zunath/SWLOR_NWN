namespace SWLOR.CombatSim.SpaceCombat.Engine;

/// <summary>
/// When a module is next due. A toggle's cycles are anchored to the fight start (cycle n is due at n x period),
/// so a skipped cycle does not shift the ones after it.
/// </summary>
public sealed class ModuleClock
{
    private const double Tolerance = 1e-9;

    public ModuleClock(double periodSeconds)
    {
        PeriodSeconds = periodSeconds;
    }

    public double PeriodSeconds { get; }
    public double NextDueAt { get; private set; }

    /// <summary>Cycles that ran.</summary>
    public int Fired { get; set; }

    /// <summary>Cycles that were due but could not be paid for.</summary>
    public int Skipped { get; set; }

    public bool IsDue(double now) => now >= NextDueAt - Tolerance;

    /// <summary>Moves to the cycle after the one just handled.</summary>
    public void AdvanceCycle() => NextDueAt += PeriodSeconds;

    /// <summary>A button press: the next one is due a full period after this moment.</summary>
    public void StartCooldown(double now) => NextDueAt = now + PeriodSeconds;
}
