namespace SWLOR.CombatSim.Engine
{
    public sealed class SimulationResult
    {
        public double EndedAt { get; init; }

        /// <summary>Index of the last team standing, or null when the time limit ended the fight.</summary>
        public int? WinningTeam { get; init; }

        public IReadOnlyList<DeathRecord> Deaths { get; init; }
        public int Revives { get; init; }
        public IReadOnlyList<Combatant> Combatants { get; init; }

        public double? FirstDeathAt => Deaths.Count == 0 ? null : Deaths.Min(d => d.At);
    }
}
