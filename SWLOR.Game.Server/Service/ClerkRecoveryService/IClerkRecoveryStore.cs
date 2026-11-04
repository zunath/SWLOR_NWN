using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.LivestockService;

namespace SWLOR.Game.Server.Service.ClerkRecoveryService
{
    /// <summary>
    /// The records a clerk action touches, one crop or animal at a time. <see cref="ClerkRecoveryProcess"/> calls them in a fixed
    /// order, so the order can be tested without a database; the services implement them over the real records.
    /// </summary>
    public interface IClerkRecoveryStore
    {
        /// <summary>The mark on the crop or animal as it is saved now, or null when it carries none or is gone.</summary>
        ClerkRecoveryMark ReadMark();

        /// <summary>Saves the crop or animal with the mark.</summary>
        void WriteMark(ClerkRecoveryMark mark);

        /// <summary>
        /// Saves <paramref name="count"/> of an item to the player's overflow storage under record ids that start with
        /// <paramref name="recordPrefix"/>. Writing the same prefix again replaces those records instead of adding more.
        /// </summary>
        void WriteOverflowItems(string playerId, string recordPrefix, string resref, int count);

        /// <summary>Saves a crate holding the animal to the player's overflow storage, under the same repeatable record id.</summary>
        void WriteOverflowCrate(string playerId, string recordPrefix, LivestockCrate crate);

        /// <summary>
        /// Ends the action on the crop or animal and clears its mark: a harvested crop, a butchered or crated animal is deleted,
        /// and a collected-from animal records the collection.
        /// </summary>
        void FinishSource(ClerkRecoveryMark mark);
    }
}
