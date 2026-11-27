using System.Collections.Generic;
using System.Numerics;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Domain.Foundation
{
    /// <summary>
    /// The baked walkable locations of every area. Static callers reach the container's instance through
    /// <c>GameServices.Walkmesh</c>; the event handlers are on <see cref="WalkmeshService"/>.
    /// </summary>
    public interface IWalkmeshService
    {
        /// <summary>
        /// When the module content changes, rerun the baking process.
        /// </summary>
        void LoadWalkmeshes();

        /// <summary>
        /// When the module loads, retrieve the list of walkable locations from the database.
        /// These locations can be used to spawn objects randomly throughout an area.
        /// This only runs if the module content has NOT changed since the last run.
        /// </summary>
        void RetrieveWalkmeshes();

        /// <summary>
        /// Retrieves a random location from the walkmeshes in an area.
        /// </summary>
        /// <param name="area">The area to retrieve a random location for.</param>
        /// <returns>A random location within an area.</returns>
        Location GetRandomLocation(uint area);

        /// <summary>
        /// Retrieves a random location for a randomly placed placeable (resource node, terminal). These points also
        /// leave room to walk around the placeable. An area without such points falls back to
        /// <see cref="GetRandomLocation"/>.
        /// </summary>
        /// <param name="area">The area to retrieve a random location for.</param>
        /// <returns>A random location within an area.</returns>
        Location GetRandomPlaceableLocation(uint area);

        /// <summary>
        /// Returns the baked points of an area where a placeable leaves room to be walked around.
        /// </summary>
        /// <param name="area">The area to retrieve the points for.</param>
        /// <returns>The subset of the area's random spawn points that suit placeables.</returns>
        IReadOnlyList<Vector3> GetPlaceableSpawnPoints(uint area);
    }
}
