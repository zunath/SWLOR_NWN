namespace SWLOR.CombatSim.SpaceCombat.Engine;

/// <summary>The outcome of one seeded fight.</summary>
public sealed class SpaceFightResult
{
    /// <summary>When one side was wiped out, or the scenario's duration when neither was.</summary>
    public double EndedAt { get; init; }

    /// <summary>When the last defender was destroyed; null when some survived.</summary>
    public double? DefendersDestroyedAt { get; init; }

    /// <summary>Every defender is destroyed and at least one attacker survives.</summary>
    public bool AttackersWon { get; init; }

    /// <summary>Hull and shield left across the attacking ships at the end.</summary>
    public double AttackerHullRemaining { get; init; }
    public double AttackerShieldRemaining { get; init; }

    /// <summary>The attackers' maximum hull and shield, for turning the remainder into a share.</summary>
    public double AttackerMaxHull { get; init; }
    public double AttackerMaxShield { get; init; }

    /// <summary>Shield and hull points the attackers removed from the defenders.</summary>
    public double DamageDealtByAttackers { get; init; }

    public double DamageDealtByDefenders { get; init; }
}
