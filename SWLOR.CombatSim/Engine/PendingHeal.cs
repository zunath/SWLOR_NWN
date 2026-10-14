namespace SWLOR.CombatSim.Engine
{
    /// <summary>A heal the game lands a moment after its ability, such as Resuscitation II's 0.1 seconds after the revive.</summary>
    public sealed class PendingHeal
    {
        public Combatant Source { get; init; }
        public Combatant Target { get; init; }
        public int Amount { get; init; }
        public HealKind Kind { get; init; }
        public double At { get; init; }
    }
}
