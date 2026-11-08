using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Domain.Core
{
    /// <summary>
    /// Draws and removes tile magic ground tiles in an area. Static callers reach the container's instance through
    /// <c>GameServices.TileMagic</c>.
    /// </summary>
    public interface ITileMagicService
    {
        /// <summary>Spawns tile magic across the given number of columns and rows of an area.</summary>
        void ChangeAreaGroundTiles(uint area, TileMagicType tileType, int columns, int rows, float zOffset = -0.4f);

        /// <summary>Draws tile magic tiles across an entire area.</summary>
        void ChangeAreaGroundTiles(uint area, TileMagicType tileType, float zOffset = -0.4f);

        /// <summary>Removes all tiles created by tile magic from a given area.</summary>
        void ResetAreaGroundTiles(uint area);
    }
}
