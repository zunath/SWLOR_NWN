namespace SWLOR.CombatSim.Engine
{
    /// <summary>
    /// The enemy a whole team is attacking under <see cref="Scenario.TargetPolicy.Coordinated"/>.
    /// </summary>
    public sealed class SharedTarget
    {
        public Combatant Target { get; init; }
        public double PickedAt { get; init; }
    }
}
