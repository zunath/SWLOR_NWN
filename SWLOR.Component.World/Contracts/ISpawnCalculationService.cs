namespace SWLOR.Component.World.Contracts
{
    /// <summary>
    /// Service responsible for calculating spawn counts based on area characteristics.
    /// </summary>
    public interface ISpawnCalculationService
    {
        /// <summary>
        /// Calculates the number of resource spawns for an area.
        /// </summary>
        /// <param name="area">The area to calculate spawn counts for</param>
        /// <returns>The number of resource spawns</returns>
        int CalculateResourceSpawnCount(uint area);

        /// <summary>
        /// Calculates the number of creature spawns for an area.
        /// </summary>
        /// <param name="area">The area to calculate spawn counts for</param>
        /// <returns>The number of creature spawns</returns>
        int CalculateCreatureSpawnCount(uint area);
    }
}
