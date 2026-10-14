namespace SWLOR.CombatSim.Scenario
{
    public sealed class TeamSpec
    {
        public string Name { get; set; }
        public TargetPolicy TargetPolicy { get; set; } = TargetPolicy.Random;

        /// <summary>Enemy this team attacks while it is alive, for <see cref="Scenario.TargetPolicy.Focus"/>.</summary>
        public string FocusTarget { get; set; }

        /// <summary>Seconds between target re-picks: each attacker's own, or the whole team's for <see cref="Scenario.TargetPolicy.Coordinated"/>. 0 keeps a target until it dies.</summary>
        public float RetargetSeconds { get; set; }

        public List<CombatantSpec> Members { get; set; } = new();
    }
}
