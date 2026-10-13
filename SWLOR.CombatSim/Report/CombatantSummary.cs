namespace SWLOR.CombatSim.Report
{
    /// <summary>
    /// Per-combatant averages across all runs of one rule set.
    /// </summary>
    public sealed class CombatantSummary
    {
        public string Name { get; init; }
        public int TeamIndex { get; init; }
        public double AverageDeaths { get; set; }
        public double AverageDamageDealt { get; set; }
        public double AverageHealingDone { get; set; }
        public double AverageHealingReceived { get; set; }
        public double AverageOverhealing { get; set; }
        public double AverageDamageTakenToHp { get; set; }
        public double AverageAbsorbed { get; set; }
        public double AverageFpSpentPerMinute { get; set; }
        public double AverageStmSpentPerMinute { get; set; }
        public double AverageFpRefunded { get; set; }
        public double AverageStmRefunded { get; set; }
        public double AverageStarvedSeconds { get; set; }
        public double? MedianFirstStarvedAt { get; set; }
        public double ShareOfRunsStarved { get; set; }
    }
}
