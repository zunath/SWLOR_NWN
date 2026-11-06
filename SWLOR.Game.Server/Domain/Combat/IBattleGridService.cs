using System.Collections.Generic;
using System.Numerics;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// A grid DMs project over an area for space and ground battles. The grid is built from sections of the
    /// placement grid visual, each a scaled copy on its own invisible anchor. One grid exists per area; it lasts until
    /// a DM removes it or the server restarts.
    /// </summary>
    public interface IBattleGridService
    {
        /// <summary>The settings an area's grid uses until a DM changes them.</summary>
        BattleGridSettings DefaultSettings { get; }

        /// <summary>
        /// Keeps settings inside the supported ranges, with the square size on its step.
        /// </summary>
        BattleGridSettings Clamp(BattleGridSettings settings);

        /// <summary>
        /// Moves a grid center onto a square corner, so grid lines fall on world multiples of the square size and
        /// match the area's 10 m tile edges whenever the square size divides 10 m.
        /// </summary>
        Vector3 SnapCenter(Vector3 center, float squareSize);

        /// <summary>
        /// Returns the center of every section of a grid centered on <paramref name="center"/>.
        /// </summary>
        List<Vector3> GetSectionCenters(Vector3 center, BattleGridSettings settings);

        /// <summary>
        /// Returns true when the area has a grid with at least one section drawn.
        /// </summary>
        bool IsPlaced(uint area);

        /// <summary>
        /// Returns how many sections of the area's grid were drawn. Sections the engine could not create, such as
        /// ones off the edge of the map, are not counted.
        /// </summary>
        int GetDrawnSections(uint area);

        /// <summary>
        /// Returns the area's grid settings, or the defaults when no DM has changed them.
        /// </summary>
        BattleGridSettings GetSettings(uint area);

        /// <summary>
        /// Stores new settings for the area and redraws its grid, keeping the grid's center on a square corner.
        /// </summary>
        void SetSettings(uint area, BattleGridSettings settings);

        /// <summary>
        /// Returns the center of the area's grid.
        /// </summary>
        Vector3 GetCenter(uint area);

        /// <summary>
        /// Draws the area's grid centered on the square corner nearest <paramref name="position"/>, replacing any
        /// grid already in the area.
        /// </summary>
        /// <returns>The number of sections drawn; 0 leaves the area without a grid.</returns>
        int Place(uint area, Vector3 position);

        /// <summary>
        /// Moves the area's grid by whole squares.
        /// </summary>
        void Move(uint area, int squaresEast, int squaresNorth);

        /// <summary>
        /// Removes the area's grid. Its settings stay for the next grid placed there.
        /// </summary>
        void Remove(uint area);
    }
}
