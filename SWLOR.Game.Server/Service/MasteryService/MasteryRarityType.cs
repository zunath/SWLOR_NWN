namespace SWLOR.Game.Server.Service.MasteryService
{
    /// <summary>
    /// Governs how freely a mastery can be requested. See MASTERY_SPEC.md "Business rules".
    /// </summary>
    public enum MasteryRarityType
    {
        /// <summary>
        /// Free to take. No cap on how many Standard masteries a character can hold.
        /// </summary>
        Standard = 0,

        /// <summary>
        /// Max ONE Rare mastery per character. Staff can override with a reason.
        /// </summary>
        Rare = 1,

        /// <summary>
        /// Requests are hard-blocked with no override via the request flow. Staff can
        /// still direct-grant one of these masteries outside the request pipeline.
        /// </summary>
        OffLimit = 2
    }
}
