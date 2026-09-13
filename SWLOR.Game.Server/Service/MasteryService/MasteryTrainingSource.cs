namespace SWLOR.Game.Server.Service.MasteryService
{
    /// <summary>
    /// Tags how a mastery level was (or is being) trained. Duration math lives in
    /// MasteryRules; this is primarily needed to recompute retrain-credit tier and
    /// Quick Slot refunds when a tier is later abandoned/revoked.
    /// </summary>
    public enum MasteryTrainingSource
    {
        /// <summary>The character's 1st ever mastery level trained. 14 days.</summary>
        Standard14 = 0,

        /// <summary>The character's 2nd ever mastery level trained. 21 days.</summary>
        Standard21 = 1,

        /// <summary>The character's 3rd+ ever mastery level trained. 28 days.</summary>
        Standard28 = 2,

        /// <summary>
        /// A Quick Slot was spent on this entry. 7 days (or 131 for tier 5).
        /// </summary>
        QuickSlot = 3,

        /// <summary>
        /// A tier 5 entry with no Quick Slot spent. 152 days.
        /// </summary>
        Tier5 = 4,

        /// <summary>A staff instant grant. 0 days.</summary>
        Instant = 5,

        /// <summary>A retrain credit was spent that grants a 14-day duration.</summary>
        Retrain14 = 6,

        /// <summary>A retrain credit was spent that grants a 7-day duration.</summary>
        Retrain7 = 7
    }
}
