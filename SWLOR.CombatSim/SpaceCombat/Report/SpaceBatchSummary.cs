using SWLOR.CombatSim.Report;

namespace SWLOR.CombatSim.SpaceCombat.Report;

/// <summary>Results of many seeded runs of one space scenario.</summary>
public sealed class SpaceBatchSummary
{
    public int Runs { get; init; }
    public int Seed { get; init; }

    /// <summary>Time to kill the defending side, for the runs in which it was destroyed.</summary>
    public List<double> TimesToKill { get; } = new();

    public int AttackerWins { get; set; }
    public List<double> AttackerHullRemaining { get; } = new();
    public List<double> AttackerShieldRemaining { get; } = new();
    public double AttackerMaxHull { get; set; }
    public double AttackerMaxShield { get; set; }

    /// <summary>Damage the attackers dealt per second of the fight, one value per run.</summary>
    public List<double> DamagePerSecond { get; } = new();

    public int RunsWithKill => TimesToKill.Count;
    public double AttackerWinRate => Runs == 0 ? 0 : AttackerWins / (double)Runs;
    public double? MedianTimeToKill => Percentile.Of(TimesToKill, 50);
    public double? P10TimeToKill => Percentile.Of(TimesToKill, 10);
    public double? P90TimeToKill => Percentile.Of(TimesToKill, 90);
    public double? MedianAttackerHullRemaining => Percentile.Of(AttackerHullRemaining, 50);
    public double? MedianAttackerShieldRemaining => Percentile.Of(AttackerShieldRemaining, 50);
    public double? MedianDamagePerSecond => Percentile.Of(DamagePerSecond, 50);
}
