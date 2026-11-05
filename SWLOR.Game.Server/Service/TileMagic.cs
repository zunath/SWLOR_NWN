using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Draws and removes tile magic ground tiles. The logic lives in <see cref="ITileMagicService"/>; every member
    /// here forwards to the container's instance.
    /// </summary>
    public static class TileMagic
    {
        private static ITileMagicService Instance => FacadeAccessor.Resolve<ITileMagicService>();

        /// <summary>
        /// Spawns tilemagic across an area. Will only draw across the specified number of columns and rows.
        /// </summary>
        public static void ChangeAreaGroundTiles(uint area, TileMagicType tileType, int columns, int rows, float zOffset = -0.4f)
        {
            Instance.ChangeAreaGroundTiles(area, tileType, columns, rows, zOffset);
        }

        /// <summary>
        /// Draws tile magic tiles across an entire area.
        /// </summary>
        public static void ChangeAreaGroundTiles(uint area, TileMagicType tileType, float zOffset = -0.4f)
        {
            Instance.ChangeAreaGroundTiles(area, tileType, zOffset);
        }

        /// <summary>
        /// Removes all tiles created by tile magic from a given area.
        /// </summary>
        public static void ResetAreaGroundTiles(uint area) => Instance.ResetAreaGroundTiles(area);
    }
}
