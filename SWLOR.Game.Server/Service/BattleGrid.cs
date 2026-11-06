using System.Collections.Generic;
using System.Numerics;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Combat;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The settings of a DM battle grid: square size, how many sections it spans, and how far it floats above the
    /// spot it was placed on.
    /// </summary>
    public readonly record struct BattleGridSettings(float SquareSize, int SectionsWide, int SectionsLong, float Height);

    /// <summary>
    /// A grid DMs project over an area for space and ground battles. The grid is built from sections of the
    /// placement grid visual (tron.mdl: a flat 10 m plane drawn as 20 x 20 squares). Each section is a scaled copy
    /// of that visual on its own invisible anchor, so a large map needs no extra creatures, and the grid floats
    /// above the spot it was placed on so floor placeables cannot hide it. One grid exists per area; it lasts until
    /// a DM removes it or the server restarts. The logic lives in <see cref="IBattleGridService"/>; the constants stay
    /// here and every member forwards to the container's instance.
    /// </summary>
    public static class BattleGrid
    {
        private static IBattleGridService Instance => FacadeAccessor.Resolve<IBattleGridService>();

        public const string AnchorTag = "DM_BATTLE_GRID";

        /// <summary>Squares along each edge of one section (tron.mdl at scale 1 is 10 m with 0.5 m squares).</summary>
        public const int SquaresPerSection = 20;

        public const float MinSquareSize = 0.5f;
        public const float MaxSquareSize = 10f;
        public const float SquareSizeStep = 0.5f;
        public const int MinSections = 1;
        public const int MaxSections = 8;
        public const float MinHeight = -5f;
        public const float MaxHeight = 10f;
        public const float HeightStep = 0.1f;

        /// <summary>The settings an area's grid uses until a DM changes them.</summary>
        public static BattleGridSettings DefaultSettings => Instance.DefaultSettings;

        /// <summary>
        /// Keeps settings inside the supported ranges, with the square size on its step.
        /// </summary>
        public static BattleGridSettings Clamp(BattleGridSettings settings) => Instance.Clamp(settings);

        /// <summary>
        /// Moves a grid center onto a square corner, so grid lines fall on world multiples of the square size and
        /// match the area's 10 m tile edges whenever the square size divides 10 m.
        /// </summary>
        public static Vector3 SnapCenter(Vector3 center, float squareSize) => Instance.SnapCenter(center, squareSize);

        /// <summary>
        /// Returns the center of every section of a grid centered on <paramref name="center"/>.
        /// </summary>
        public static List<Vector3> GetSectionCenters(Vector3 center, BattleGridSettings settings) =>
            Instance.GetSectionCenters(center, settings);

        /// <summary>
        /// Returns true when the area has a grid with at least one section drawn.
        /// </summary>
        public static bool IsPlaced(uint area) => Instance.IsPlaced(area);

        /// <summary>
        /// Returns how many sections of the area's grid were drawn. Sections the engine could not create, such as
        /// ones off the edge of the map, are not counted.
        /// </summary>
        public static int GetDrawnSections(uint area) => Instance.GetDrawnSections(area);

        /// <summary>
        /// Returns the area's grid settings, or the defaults when no DM has changed them.
        /// </summary>
        public static BattleGridSettings GetSettings(uint area) => Instance.GetSettings(area);

        /// <summary>
        /// Stores new settings for the area and redraws its grid, keeping the grid's center on a square corner.
        /// </summary>
        public static void SetSettings(uint area, BattleGridSettings settings) => Instance.SetSettings(area, settings);

        /// <summary>
        /// Returns the center of the area's grid.
        /// </summary>
        public static Vector3 GetCenter(uint area) => Instance.GetCenter(area);

        /// <summary>
        /// Draws the area's grid centered on the square corner nearest <paramref name="position"/>, replacing any
        /// grid already in the area.
        /// </summary>
        /// <returns>The number of sections drawn; 0 leaves the area without a grid.</returns>
        public static int Place(uint area, Vector3 position) => Instance.Place(area, position);

        /// <summary>
        /// Moves the area's grid by whole squares.
        /// </summary>
        public static void Move(uint area, int squaresEast, int squaresNorth) => Instance.Move(area, squaresEast, squaresNorth);

        /// <summary>
        /// Removes the area's grid. Its settings stay for the next grid placed there.
        /// </summary>
        public static void Remove(uint area) => Instance.Remove(area);
    }
}
