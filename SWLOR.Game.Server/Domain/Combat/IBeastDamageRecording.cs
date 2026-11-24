namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The record of a hit a creature deals, as the domains below Quests read it. Combat owns the interface and the capstone proof
    /// service (<c>CapstoneProofService</c>) implements it, so a landed hit reaches the proof without naming Quests.
    /// </summary>
    public interface IBeastDamageRecording
    {
        /// <summary>Records that a beast hit a target.</summary>
        /// <param name="beast">The creature that dealt the damage.</param>
        /// <param name="target">The creature that took it.</param>
        void RecordBeastDamage(uint beast, uint target);
    }
}
