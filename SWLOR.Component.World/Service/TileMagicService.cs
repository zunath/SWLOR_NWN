using System.Numerics;
using SWLOR.Component.World.Contracts;
using SWLOR.Component.World.Enums;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Abstractions.Extensions;
using SWLOR.Shared.Core.Extension;
using SWLOR.Shared.Core.Log.LogGroup;

namespace SWLOR.Component.World.Service
{
    public class TileMagicService : ITileMagicService
    {
        private readonly ILogger _logger;

        public TileMagicService(ILogger logger)
        {
            _logger = logger;
        }

        /// When the module loads, load the tile magic configured on every area.
        public void ApplyAreaConfiguration()
        {
            for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
            {
                ApplyTileMagic(area);
            }
        }

        /// <summary>
        /// Spawns tilemagic across an area. Will only draw across the specified number of columns and rows.
        /// </summary>
        /// <param name="area">The area to draw in</param>
        /// <param name="tileType">The type of tile to draw</param>
        /// <param name="columns">The number of columns to draw</param>
        /// <param name="rows">The number of rows to draw</param>
        /// <param name="zOffset">The Z-Offset to draw at.</param>
        private void ChangeAreaGroundTiles(uint area, TileMagicType tileType, int columns, int rows, float zOffset = -0.4f)
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
                    ApplyEffectToObject(DurationType.Permanent, EffectVisualEffect((VisualEffectType)tileType), tile);
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
        private void ChangeAreaGroundTiles(uint area, TileMagicType tileType, float zOffset = -0.4f)
        {
            ChangeAreaGroundTiles(area, tileType, GetAreaSize(AreaDimensionType.Width, area), GetAreaSize(AreaDimensionType.Height, area), zOffset);
        }

        /// <summary>
        /// Removes all tiles created by tile magic from a given area.
        /// </summary>
        /// <param name="area">The area to reset.</param>
        private void ResetAreaGroundTiles(uint area)
        {
            for (var placeable = GetFirstObjectInArea(area); GetIsObjectValid(placeable); placeable = GetNextObjectInArea(area))
            {
                if (GetObjectType(placeable) == ObjectType.Placeable && GetTag(placeable) == "x2_tmp_tile")
                {
                    DestroyObject(placeable);
                }
            }
        }

        /// <summary>
        /// Applies tile magic to a specific area, if configured.
        /// </summary>
        /// <param name="area">The area to apply to</param>
        private void ApplyTileMagic(uint area)
        {
            var zPosition = GetLocalFloat(area, "TILE_MAGIC_Z");
            var tileTypeId = GetLocalInt(area, "TILE_MAGIC_TYPE");

            if (tileTypeId <= 0) return;

            try
            {
                var tile = (TileMagicType)tileTypeId;
                ChangeAreaGroundTiles(area, tile, zPosition);
            }
            catch (Exception ex)
            {
                _logger.Write<ErrorLogGroup>($"Area {GetName(area)} has an invalid tile magic type. Please fix the local variable. Exception: {ex.ToMessageAndCompleteStacktrace()}");
            }
        }
    }
}
