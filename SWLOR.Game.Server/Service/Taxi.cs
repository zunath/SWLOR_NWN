using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Service.TaxiService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the taxi destinations. The logic and state live in <see cref="ITaxiService"/>; every member
    /// here forwards to the container's instance, and the event handler stays here so its name in the handler order
    /// manifest does not change.
    /// </summary>
    public static class Taxi
    {
        private static ITaxiService Instance => FacadeAccessor.Resolve<ITaxiService>();

        /// <summary>
        /// When the module loads, cache all taxi destinations.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void LoadTaxiDestinations()
        {
            Instance.LoadTaxiDestinations();
        }

        /// <summary>
        /// Registers a taxi destination for a player.
        /// Once registered, the player can choose to quick travel to that destination.
        /// </summary>
        /// <param name="player">The player to register the destination to.</param>
        /// <param name="type">The destination type to register</param>
        public static void RegisterTaxiDestination(uint player, TaxiDestinationType type)
        {
            Instance.RegisterTaxiDestination(player, type);
        }

        /// <summary>
        /// Retrieves all of the taxi destinations for a given region Id.
        /// </summary>
        /// <param name="regionId">The region Id to search by.</param>
        /// <returns>A dictionary of taxi destination types and attributes.</returns>
        public static Dictionary<TaxiDestinationType, TaxiDestinationAttribute> GetDestinationsByRegionId(int regionId)
        {
            return Instance.GetDestinationsByRegionId(regionId);
        }
    }
}
