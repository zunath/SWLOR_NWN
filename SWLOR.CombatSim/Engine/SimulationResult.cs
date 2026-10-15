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

        public IEnumerable<DeathRecord> PlayerDeaths => Deaths.Where(d => !d.IsNpc);

        /// <summary>First death of a player combatant; NPC deaths (a PvE boss) are not counted.</summary>
        public double? FirstDeathAt => PlayerDeaths.Any() ? PlayerDeaths.Min(d => d.At) : null;
    }
}
