namespace SWLOR.CombatSim.Rules
{
    public enum ReviveRule
    {
        /// <summary>Revives work mid-fight and only movement interrupts the cast.</summary>
        Unrestricted,

        /// <summary>PvP: blocked while the fight is on. PvE: allowed, damage interrupts the cast, one mid-fight revive per target per lockout.</summary>
        BlockedInPvpLimitedInPve
    }
}
