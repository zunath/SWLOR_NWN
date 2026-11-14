using System.Numerics;
using SWLOR.Game.Server.Enumeration;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Area;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Domain.Foundation
{
    /// <summary>
    /// Tile magic: the logic that used to be static in the <c>TileMagic</c> facade, moved here unchanged. Static callers
    /// reach the container's instance through <c>GameServices.TileMagic</c>.
    /// </summary>
    public class TileMagicService : ITileMagicService
    {
        public void ChangeAreaGroundTiles(uint area, TileMagicType tileType, int columns, int rows, float zOffset = -0.4f)
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
        public void ChangeAreaGroundTiles(uint area, TileMagicType tileType, float zOffset = -0.4f)
        {
            ChangeAreaGroundTiles(area, tileType, GetAreaSize(Dimension.Width, area), GetAreaSize(Dimension.Height, area), zOffset);
        }

        /// <summary>
        /// Removes all tiles created by tile magic from a given area.
        /// </summary>
        /// <param name="area">The area to reset.</param>
        public void ResetAreaGroundTiles(uint area)
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
