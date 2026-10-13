namespace SWLOR.CombatSim.Engine
{
    public sealed class CombatantStats
    {
        public double DamageDealt { get; set; }
        public double DamageTakenToHp { get; set; }
        public double DamageAbsorbedByTemporaryHp { get; set; }
        public double HealingReceived { get; set; }
        public double Overhealing { get; set; }
        public double HealingDone { get; set; }
        public double TemporaryHpGranted { get; set; }
        public double FpSpent { get; set; }
        public double StmSpent { get; set; }
        public double FpRefunded { get; set; }
        public double StmRefunded { get; set; }
        public double StarvedSeconds { get; set; }
        public double? FirstStarvedAt { get; set; }
        public int Deaths { get; set; }
        public int RevivesReceived { get; set; }
        public int AbilitiesUsed { get; set; }
        public int InterruptedCasts { get; set; }
        public int BlockedRevives { get; set; }
    }
}
