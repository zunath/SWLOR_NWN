using SWLOR.Component.World.Enums;

namespace SWLOR.Component.World.Contracts
{
    internal interface ITaxiService
    {
        /// <summary>
        /// When the module loads, cache all taxi destinations.
        /// </summary>
        void LoadTaxiDestinations();

        /// <summary>
        /// Registers a taxi destination for a player.
        /// Once registered, the player can choose to quick travel to that destination.
        /// </summary>
        /// <param name="player">The player to register the destination to.</param>
        /// <param name="type">The destination type to register</param>
        void RegisterTaxiDestination(uint player, TaxiDestinationType type);

        /// <summary>
        /// Retrieves all of the taxi destinations for a given region Id.
        /// </summary>
        /// <param name="regionId">The region Id to search by.</param>
        /// <returns>A dictionary of taxi destination types and attributes.</returns>
        Dictionary<TaxiDestinationType, TaxiDestinationAttribute> GetDestinationsByRegionId(int regionId);
    }
}
