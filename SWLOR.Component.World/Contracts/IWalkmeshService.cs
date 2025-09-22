using SWLOR.NWN.API.Engine;

namespace SWLOR.Component.World.Contracts
{
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
    }
}