using System.Collections.Generic;
using System.Numerics;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

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
    /// a DM removes it or the server restarts.
    /// </summary>
    public static class BattleGrid
    {
        public const string AnchorTag = "DM_BATTLE_GRID";
        private const string AnchorResref = "plc_invisobj";

        /// <summary>Squares along each edge of one section (tron.mdl at scale 1 is 10 m with 0.5 m squares).</summary>
        public const int SquaresPerSection = 20;
        private const float BaseSquareSize = 0.5f;

        public const float MinSquareSize = 0.5f;
        public const float MaxSquareSize = 10f;
        public const float SquareSizeStep = 0.5f;
        public const int MinSections = 1;
        public const int MaxSections = 8;
        public const float MinHeight = -5f;
        public const float MaxHeight = 10f;
        public const float HeightStep = 0.1f;

        public static readonly BattleGridSettings DefaultSettings = new(2f, 2, 2, 0.2f);

        private const string PlacedVariable = "BATTLE_GRID_PLACED";
        private const string DrawnSectionsVariable = "BATTLE_GRID_DRAWN_SECTIONS";
        private const string SquareSizeVariable = "BATTLE_GRID_SQUARE_SIZE";
        private const string SectionsWideVariable = "BATTLE_GRID_SECTIONS_WIDE";
        private const string SectionsLongVariable = "BATTLE_GRID_SECTIONS_LONG";
        private const string HeightVariable = "BATTLE_GRID_HEIGHT";
        private const string CenterXVariable = "BATTLE_GRID_CENTER_X";
        private const string CenterYVariable = "BATTLE_GRID_CENTER_Y";
        private const string CenterZVariable = "BATTLE_GRID_CENTER_Z";

        /// <summary>
        /// Keeps settings inside the supported ranges, with the square size on its step.
        /// </summary>
        public static BattleGridSettings Clamp(BattleGridSettings settings)
        {
            var squareSize = MathF.Round(settings.SquareSize / SquareSizeStep) * SquareSizeStep;
            var height = MathF.Round(settings.Height / HeightStep) * HeightStep;
            return new BattleGridSettings(
                Math.Clamp(squareSize, MinSquareSize, MaxSquareSize),
                Math.Clamp(settings.SectionsWide, MinSections, MaxSections),
                Math.Clamp(settings.SectionsLong, MinSections, MaxSections),
                Math.Clamp(height, MinHeight, MaxHeight));
        }

        /// <summary>
        /// Moves a grid center onto a square corner, so grid lines fall on world multiples of the square size and
        /// match the area's 10 m tile edges whenever the square size divides 10 m.
        /// </summary>
        public static Vector3 SnapCenter(Vector3 center, float squareSize)
        {
            return new Vector3(
                MathF.Round(center.X / squareSize) * squareSize,
                MathF.Round(center.Y / squareSize) * squareSize,
                center.Z);
        }

        /// <summary>
        /// Returns the center of every section of a grid centered on <paramref name="center"/>.
        /// </summary>
        public static List<Vector3> GetSectionCenters(Vector3 center, BattleGridSettings settings)
        {
            var sectionSize = settings.SquareSize * SquaresPerSection;
            var centers = new List<Vector3>(settings.SectionsWide * settings.SectionsLong);
            for (var column = 0; column < settings.SectionsWide; column++)
            {
                var x = center.X + (column - (settings.SectionsWide - 1) / 2f) * sectionSize;
                for (var row = 0; row < settings.SectionsLong; row++)
                {
                    var y = center.Y + (row - (settings.SectionsLong - 1) / 2f) * sectionSize;
                    centers.Add(new Vector3(x, y, center.Z));
                }
            }

            return centers;
        }

        /// <summary>
        /// Returns true when the area has a grid with at least one section drawn.
        /// </summary>
        public static bool IsPlaced(uint area)
        {
            return GetLocalBool(area, PlacedVariable);
        }

        /// <summary>
        /// Returns how many sections of the area's grid were drawn. Sections the engine could not create, such as
        /// ones off the edge of the map, are not counted.
        /// </summary>
        public static int GetDrawnSections(uint area)
        {
            return IsPlaced(area) ? GetLocalInt(area, DrawnSectionsVariable) : 0;
        }

        /// <summary>
        /// Returns the area's grid settings, or the defaults when no DM has changed them.
        /// </summary>
        public static BattleGridSettings GetSettings(uint area)
        {
            if (GetLocalInt(area, SectionsWideVariable) <= 0)
                return DefaultSettings;

            return new BattleGridSettings(
                GetLocalFloat(area, SquareSizeVariable),
                GetLocalInt(area, SectionsWideVariable),
                GetLocalInt(area, SectionsLongVariable),
                GetLocalFloat(area, HeightVariable));
        }

        /// <summary>
        /// Stores new settings for the area and redraws its grid, keeping the grid's center on a square corner.
        /// </summary>
        public static void SetSettings(uint area, BattleGridSettings settings)
        {
            settings = Clamp(settings);
            // A click past a limit clamps back to the current settings; redrawing would rebuild every anchor for nothing.
            if (settings == Clamp(GetSettings(area)))
                return;

            SetLocalFloat(area, SquareSizeVariable, settings.SquareSize);
            SetLocalInt(area, SectionsWideVariable, settings.SectionsWide);
            SetLocalInt(area, SectionsLongVariable, settings.SectionsLong);
            SetLocalFloat(area, HeightVariable, settings.Height);

            if (IsPlaced(area))
                Place(area, GetCenter(area));
        }

        /// <summary>
        /// Returns the center of the area's grid.
        /// </summary>
        public static Vector3 GetCenter(uint area)
        {
            return new Vector3(
                GetLocalFloat(area, CenterXVariable),
                GetLocalFloat(area, CenterYVariable),
                GetLocalFloat(area, CenterZVariable));
        }

        /// <summary>
        /// Draws the area's grid centered on the square corner nearest <paramref name="position"/>, replacing any
        /// grid already in the area.
        /// </summary>
        /// <returns>The number of sections drawn; 0 leaves the area without a grid.</returns>
        public static int Place(uint area, Vector3 position)
        {
            var settings = Clamp(GetSettings(area));
            var center = SnapCenter(position, settings.SquareSize);

            DestroyAnchors(area);
            SetLocalFloat(area, CenterXVariable, center.X);
            SetLocalFloat(area, CenterYVariable, center.Y);
            SetLocalFloat(area, CenterZVariable, center.Z);

            var scale = settings.SquareSize / BaseSquareSize;
            var drawn = 0;
            foreach (var sectionCenter in GetSectionCenters(center, settings))
            {
                var anchor = CreateObject(ObjectType.Placeable, AnchorResref, Location(area, sectionCenter, 0f), false, AnchorTag);
                if (!GetIsObjectValid(anchor))
                    continue;

                // Named so the anchors read as the grid in DM object lists such as the Chooser.
                SetName(anchor, "Battle Grid");
                SetPlotFlag(anchor, true);
                SetUseableFlag(anchor, false);
                // The height rides on the visual so every section stays level with the spot the grid was placed on.
                ApplyEffectToObject(DurationType.Permanent,
                    EffectVisualEffect(VisualEffect.Vfx_Placement_Grid, false, scale, new Vector3(0f, 0f, settings.Height)),
                    anchor);
                drawn++;
            }

            // The area only counts as having a grid when something was drawn, so the window never reports a grid
            // that is not there. Sections that did draw stay: a grid running off the map still covers the map.
            if (drawn > 0)
            {
                SetLocalBool(area, PlacedVariable, true);
                SetLocalInt(area, DrawnSectionsVariable, drawn);
            }
            else
            {
                DeleteLocalBool(area, PlacedVariable);
                DeleteLocalInt(area, DrawnSectionsVariable);
            }

            return drawn;
        }

        /// <summary>
        /// Moves the area's grid by whole squares.
        /// </summary>
        public static void Move(uint area, int squaresEast, int squaresNorth)
        {
            if (!IsPlaced(area))
                return;

            var squareSize = Clamp(GetSettings(area)).SquareSize;
            var center = GetCenter(area);
            center.X += squaresEast * squareSize;
            center.Y += squaresNorth * squareSize;
            Place(area, center);
        }

        /// <summary>
        /// Removes the area's grid. Its settings stay for the next grid placed there.
        /// </summary>
        public static void Remove(uint area)
        {
            DestroyAnchors(area);
            DeleteLocalBool(area, PlacedVariable);
            DeleteLocalInt(area, DrawnSectionsVariable);
        }

        private static void DestroyAnchors(uint area)
        {
            var anchors = new List<uint>();
            for (var obj = GetFirstObjectInArea(area); GetIsObjectValid(obj); obj = GetNextObjectInArea(area))
            {
                if (GetObjectType(obj) == ObjectType.Placeable && GetTag(obj) == AnchorTag)
                    anchors.Add(obj);
            }

            foreach (var anchor in anchors)
                DestroyObject(anchor);
        }
    }
}
