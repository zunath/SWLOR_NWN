namespace SWLOR.Game.Server.Service.MasteryService
{
    public enum MasteryRequestType
    {
        /// <summary>
        /// Requesting tier 1 of a mastery the character does not yet hold.
        /// </summary>
        NewMastery = 0,

        /// <summary>
        /// Requesting current tier + 1 of a mastery the character already holds.
        /// </summary>
        RankUp = 1,

        /// <summary>
        /// Requesting to abandon a tier (or the whole mastery) in exchange for a
        /// retrain credit toward a future training entry.
        /// </summary>
        Retrain = 2,

        /// <summary>
        /// Requesting a mastery be removed entirely, freeing all of its levels.
        /// </summary>
        Remove = 3,

        /// <summary>
        /// Requesting an unlisted mastery not present in the catalog.
        /// </summary>
        Custom = 4
    }
}
