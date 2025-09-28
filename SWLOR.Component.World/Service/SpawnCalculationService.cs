using SWLOR.Component.World.Contracts;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Component.World.Service
{
    /// <summary>
    /// Service responsible for calculating spawn counts based on area characteristics.
    /// </summary>
    public class SpawnCalculationService : ISpawnCalculationService
    {
        /// <summary>
        /// Calculates the number of spawns to use in an area.
        /// If an int local variable 'RESOURCE_SPAWN_COUNT' is found, use that number.
        /// Otherwise the size of the area will be used to determine the count.
        /// </summary>
        /// <param name="area">The area to determine spawn counts for</param>
        /// <returns>A positive integer indicating the number of resource spawns to use in a given area.</returns>
        public int CalculateResourceSpawnCount(uint area)
        {
            var count = GetLocalInt(area, "RESOURCE_SPAWN_COUNT");

            // Found the local variable. Use that count.
            if (count > 0) return count;

            // Local variable wasn't found or was zero. 
            // Determine the count by the size of the area.
            var width = GetAreaSize(AreaDimensionType.Width, area);
            var height = GetAreaSize(AreaDimensionType.Height, area);
            var size = width * height;

            if (size <= 12)
                count = 2;
            else if (size <= 32)
                count = 6;
            else if (size <= 64)
                count = 10;
            else if (size <= 256)
                count = 25;
            else if (size <= 512)
                count = 40;
            else if (size <= 1024)
                count = 50;

            return count;
        }

        /// <summary>
        /// Calculates the number of creature spawns for an area.
        /// If an int local variable 'CREATURE_SPAWN_COUNT' is found, use that number.
        /// Otherwise the size of the area will be used to determine the count.
        /// </summary>
        /// <param name="area">The area to determine spawn counts for</param>
        /// <returns>A positive integer indicating the number of creature spawns to use in a given area.</returns>
        public int CalculateCreatureSpawnCount(uint area)
        {
            var count = GetLocalInt(area, "CREATURE_SPAWN_COUNT");

            // Found the local variable. Use that count.
            if (count > 0) return count;

            // Local variable wasn't found or was zero. 
            // Determine the count by the size of the area.
            var width = GetAreaSize(AreaDimensionType.Width, area);
            var height = GetAreaSize(AreaDimensionType.Height, area);
            var size = width * height;

            if (size <= 12)
                count = 3;
            else if (size <= 32)
                count = 6;
            else if (size <= 64)
                count = 14;
            else if (size <= 256)
                count = 20;
            else if (size <= 512)
                count = 35;
            else if (size <= 1024)
                count = 45;

            return count;
        }
    }
}
