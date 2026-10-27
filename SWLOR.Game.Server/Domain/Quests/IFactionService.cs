using System.Collections.Generic;
using SWLOR.Game.Server.Service.FactionService;

namespace SWLOR.Game.Server.Domain.Quests
{
    /// <summary>
    /// Factions and a player's standing and points with them. The static facade <c>Service.Faction</c> forwards to the
    /// container's instance of this interface and keeps the event handler.
    /// </summary>
    public interface IFactionService
    {
        /// <summary>
        /// When the module caches, cache all faction details into memory.
        /// </summary>
        void LoadFactions();

        /// <summary>
        /// Retrieves all of the available factions registered in the system.
        /// </summary>
        /// <returns>A dictionary of all available factions registered in the system.</returns>
        Dictionary<FactionType, FactionAttribute> GetAllFactions();

        /// <summary>
        /// Retrieves details about a particular faction.
        /// Will throw an exception if faction is not registered.
        /// </summary>
        /// <param name="factionType">The type of faction to retrieve.</param>
        /// <returns>A faction detail matching the type.</returns>
        FactionAttribute GetFactionDetail(FactionType factionType);

        /// <summary>
        /// Adjusts a player's faction standing by a certain amount.
        /// </summary>
        /// <param name="player">The player whose faction standing will be adjusted.</param>
        /// <param name="faction">The faction to adjust</param>
        /// <param name="adjustBy">The amount to adjust by. This can be positive for increases and negative for decreases.</param>
        void AdjustPlayerFactionStanding(uint player, FactionType faction, int adjustBy);

        /// <summary>
        /// Adds or removes points towards a particular faction on a player.
        /// Amount can be positive or negative.
        /// </summary>
        /// <param name="player">The player to adjust</param>
        /// <param name="faction">The faction to adjust</param>
        /// <param name="adjustBy">The amount to adjust by.</param>
        void AdjustPlayerFactionPoints(uint player, FactionType faction, int adjustBy);
    }
}
