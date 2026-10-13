namespace SWLOR.CombatSim.Report
{
    /// <summary>
    /// Results of many seeded runs of one scenario under one rule set.
    /// </summary>
    public sealed class BatchSummary
    {
        public string RuleSetName { get; init; }
        public int Runs { get; init; }
        public int RunsWithDeath { get; set; }
        public int DecisiveRuns { get; set; }
        public List<double> FirstDeathTimes { get; } = new();
        public List<double> FightEndTimes { get; } = new();
        public Dictionary<int, int> WinsByTeam { get; } = new();
        public double AverageDeaths { get; set; }
        public double AverageRevives { get; set; }
        public double AverageBlockedRevives { get; set; }
        public double TotalDamageToHp { get; set; }
        public double TotalAbsorbed { get; set; }
        public double TotalHealing { get; set; }
        public double TotalOverhealing { get; set; }
        public List<CombatantSummary> Combatants { get; } = new();

        /// <summary>Healing plus damage absorbed by temporary HP, divided by damage that reached HP.</summary>
        public double RecoveryRatio => TotalDamageToHp <= 0 ? 0 : (TotalHealing + TotalAbsorbed) / TotalDamageToHp;
    }
}
