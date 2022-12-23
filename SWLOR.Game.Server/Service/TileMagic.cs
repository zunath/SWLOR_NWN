using System.Numerics;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Area;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Service
{
    public static class TileMagic
    {
        /// <summary>
        /// Spawns tilemagic across an area. Will only draw across the specified number of columns and rows.
        /// </summary>
        /// <param name="area">The area to draw in</param>
        /// <param name="tileType">The type of tile to draw</param>
        /// <param name="columns">The number of columns to draw</param>
        /// <param name="rows">The number of rows to draw</param>
        /// <param name="zOffset">The Z-Offset to draw at.</param>
        public static void ChangeAreaGroundTiles(uint area, TileMagicType tileType, int columns, int rows, float zOffset = -0.4f)
        {
            var x = 5.0f;
            var z = zOffset;

            for (var col = 0; col <= columns; col++)
            {
                var y = -5.0f;
                for (var row = 0; row <= rows; row++)
                {
                    y += 10f;
                    var location = Location(area, new Vector3(x, y, z), 0f);

                    var tile = CreateObject(ObjectType.Placeable, "plc_invisobj", location, false, "x2_tmp_tile");
                    SetPlotFlag(tile, true);
                    ApplyEffectToObject(DurationType.Permanent, EffectVisualEffect((VisualEffect)tileType), tile);
                }

                x += 10f;
            }
        }

        /// <summary>
        /// Draws tile magic tiles across an entire area.
        /// </summary>
        /// <param name="area">The area to draw in</param>
        /// <param name="tileType">The type of tile to draw.</param>
        /// <param name="zOffset">The Z-Offset to draw at</param>
        public static void ChangeAreaGroundTiles(uint area, TileMagicType tileType, float zOffset = -0.4f)
        {
            ChangeAreaGroundTiles(area, tileType, GetAreaSize(Dimension.Width, area), GetAreaSize(Dimension.Height, area), zOffset);
        }

        /// <summary>
        /// Removes all tiles created by tile magic from a given area.
        /// </summary>
        /// <param name="area">The area to reset.</param>
        public static void ResetAreaGroundTiles(uint area)
        {
            for (var placeable = GetFirstObjectInArea(area); GetIsObjectValid(placeable); placeable = GetNextObjectInArea(area))
            {
                if (GetObjectType(placeable) == ObjectType.Placeable && GetTag(placeable) == "x2_tmp_tile")
                {
                    DestroyObject(placeable);
                }
            }
        }

    }
}
