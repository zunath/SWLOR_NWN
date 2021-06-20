using SWLOR.Game.Server.Core.NWNX.Enum;
using static SWLOR.Game.Server.Core.NWNX.NWNXCore;

namespace SWLOR.Game.Server.Core.NWNX
{
    public static class Tileset
    {
        private const string NWNX_Tileset = "NWNX_Tileset";

        /// <summary>
        /// Get general data of sTileset.
        /// </summary>
        /// <param name="tileset">The tileset</param>
        /// <returns>A tileset data object</returns>
        public static TilesetData GetTilesetData(string tileset)
        {
            NWNX_PushArgumentString(tileset);
            NWNX_CallFunction(NWNX_Tileset, "GetTilesetData");

            return new TilesetData
            {
                HasHeightTransition = NWNX_GetReturnValueInt(),
                IsInterior = NWNX_GetReturnValueInt() == 1,
                UnlocalizedName = NWNX_GetReturnValueString(),
                DisplayNameStringRef = NWNX_GetReturnValueInt(),
                FloorTerrain = NWNX_GetReturnValueString(),
                DefaultTerrain = NWNX_GetReturnValueString(),
                BorderTerrain = NWNX_GetReturnValueString(),
                GroupCount = NWNX_GetReturnValueInt(),
                CrosserCount = NWNX_GetReturnValueInt(),
                TerrainCount = NWNX_GetReturnValueInt(),
                HeightTransition = NWNX_GetReturnValueInt(),
                TileCount = NWNX_GetReturnValueInt(),
            };
        }

        /// <summary>
        /// Get the name of tileset's terrain at index.
        /// </summary>
        /// <param name="tileset">The tileset</param>
        /// <param name="index">The index of the terrain. Range: TilesetData.NumTerrain > index >= 0</param>
        /// <returns>The terrain name or empty string on error.</returns>
        public static string GetTilesetTerrain(string tileset, int index)
        {
            NWNX_PushArgumentInt(index);
            NWNX_PushArgumentString(tileset);
            NWNX_CallFunction(NWNX_Tileset, "GetTilesetTerrain");

            return NWNX_GetReturnValueString();
        }

        /// <summary>
        /// Get the name of tileset's crosser at index.
        /// </summary>
        /// <param name="tileset">The tileset</param>
        /// <param name="index">The index of the crosser. Range: TilesetData.NumCrossers > index >= 0</param>
        /// <returns></returns>
        public static string GetTilesetCrosser(string tileset, int index)
        {
            NWNX_PushArgumentInt(index);
            NWNX_PushArgumentString(tileset);
            NWNX_CallFunction(NWNX_Tileset, "GetTilesetCrosser");

            return NWNX_GetReturnValueString();
        }

        /// <summary>
        /// Get general data of the group at index in tileset.
        /// </summary>
        /// <param name="tileset">The tileset</param>
        /// <param name="index">The index of the group. Range: TilesetData.NumGroups > index >= 0</param>
        /// <returns>A TilesetGroupData object</returns>
        public static TilesetGroupData GetTilesetGroupData(string tileset, int index)
        {
            NWNX_PushArgumentInt(index);
            NWNX_PushArgumentString(tileset);
            NWNX_CallFunction(NWNX_Tileset, "GetTilesetGroupData");

            return new TilesetGroupData
            {
                Columns = NWNX_GetReturnValueInt(),
                Rows = NWNX_GetReturnValueInt(),
                StringRef = NWNX_GetReturnValueInt(),
                Name = NWNX_GetReturnValueString()
            };
        }

        /// <summary>
        /// Get the tile ID at tileIndex in groupIndex of tileset.
        /// </summary>
        /// <param name="tileset">The tileset</param>
        /// <param name="groupIndex">The index of the group. Range: TilesetData.NumGroups > groupIndex >= 0</param>
        /// <param name="tileIndex">The index of the tile. Range: (TilesetGroupData.Rows * TilesetGroupData.Columns) > tileIndex >= 0</param>
        /// <returns></returns>
        public static int GetTilesetGroupTile(string tileset, int groupIndex, int tileIndex)
        {
            NWNX_PushArgumentInt(tileIndex);
            NWNX_PushArgumentInt(groupIndex);
            NWNX_PushArgumentString(tileset);
            NWNX_CallFunction(NWNX_Tileset, "GetTilesetGroupTile");

            return NWNX_GetReturnValueInt();
        }

        /// <summary>
        /// Get the model name of a tile in tileset.
        /// </summary>
        /// <param name="tileset">The tileset</param>
        /// <param name="tileId">The tile ID</param>
        /// <returns>The model name or empty string on error</returns>
        public static string GetTileModel(string tileset, int tileId)
        {
            NWNX_PushArgumentInt(tileId);
            NWNX_PushArgumentString(tileset);
            NWNX_CallFunction(NWNX_Tileset, "GetTileModel");

            return NWNX_GetReturnValueString();
        }

        /// <summary>
        /// Get the minimap texture name of a tile in tileset.
        /// </summary>
        /// <param name="tileset">The tileset</param>
        /// <param name="tileId">The tile ID</param>
        /// <returns>The minimap texture name or empty string on error.</returns>
        public static string GetTileMinimapTexture(string tileset, int tileId)
        {
            NWNX_PushArgumentInt(tileId);
            NWNX_PushArgumentString(tileset);
            NWNX_CallFunction(NWNX_Tileset, "GetTileMinimapTexture");

            return NWNX_GetReturnValueString();
        }

        /// <summary>
        /// Get the edges and corners of a tile in tileset.
        /// </summary>
        /// <param name="tileset">The tileset</param>
        /// <param name="tileId">The tile ID</param>
        /// <returns>A TileEdgesAndCorners object.</returns>
        public static TileEdgesAndCorners GetTileEdgesAndCorners(string tileset, int tileId)
        {
            NWNX_PushArgumentInt(tileId);
            NWNX_PushArgumentString(tileset);
            NWNX_CallFunction(NWNX_Tileset, "GetTileEdgesAndCorners");

            return new TileEdgesAndCorners
            {
                Left = NWNX_GetReturnValueString(),
                BottomLeft = NWNX_GetReturnValueString(),
                Bottom = NWNX_GetReturnValueString(),
                BottomRight = NWNX_GetReturnValueString(),
                Right = NWNX_GetReturnValueString(),
                TopRight = NWNX_GetReturnValueString(),
                Top = NWNX_GetReturnValueString(),
                TopLeft = NWNX_GetReturnValueString(),
            };
        }

        /// <summary>
        /// Get the number of doors of a tile in tileset.
        /// </summary>
        /// <param name="tileset">The tileset</param>
        /// <param name="tileId">The tile ID</param>
        /// <returns>The amount of doors</returns>
        public static int GetTileNumDoors(string tileset, int tileId)
        {
            NWNX_PushArgumentInt(tileId);
            NWNX_PushArgumentString(tileset);
            NWNX_CallFunction(NWNX_Tileset, "GetTileNumDoors");

            return NWNX_GetReturnValueInt();
        }

        /// <summary>
        /// Get the door data of a tile in tileset.
        /// </summary>
        /// <param name="tileset">The tileset</param>
        /// <param name="tileId">The tile ID</param>
        /// <param name="index">The index of the door. Range: GetTileNumDoors() > index >= 0</param>
        /// <returns></returns>
        public static TileDoorData GetTileDoorData(string tileset, int tileId, int index = 0)
        {
            NWNX_PushArgumentInt(index);
            NWNX_PushArgumentInt(tileId);
            NWNX_PushArgumentString(tileset);
            NWNX_CallFunction(NWNX_Tileset, "GetTileDoorData");

            return new TileDoorData
            {
                Orientation = NWNX_GetReturnValueFloat(),
                Z = NWNX_GetReturnValueFloat(),
                Y = NWNX_GetReturnValueFloat(),
                X = NWNX_GetReturnValueFloat(),
                Type = NWNX_GetReturnValueInt()
            };
        }

        /// <summary>
        /// Override the tiles of areaResref with data in overrideName.
        /// </summary>
        /// <param name="areaResref">The resref of the area to override.</param>
        /// <param name="overrideName">The name of the override cotnaining the custom tile data or empty string to remove the override.</param>
        public static void SetAreaTileOverride(string areaResref, string overrideName)
        {
            NWNX_PushArgumentString(overrideName);
            NWNX_PushArgumentString(areaResref);
            NWNX_CallFunction(NWNX_Tileset, "SetAreaTileOverride");
        }

        /// <summary>
        /// Create a tile override named overrideName.
        /// </summary>
        /// <param name="overrideName">The name of the override.</param>
        /// <param name="tileset">The tileset the override should use.</param>
        /// <param name="width">The width of the area. Valid values: 1-32</param>
        /// <param name="height">The height of the area. Valid values: 1-32</param>
        public static void CreateTileOverride(string overrideName, string tileset, int width, int height)
        {
            NWNX_PushArgumentInt(height);
            NWNX_PushArgumentInt(width);
            NWNX_PushArgumentString(tileset);
            NWNX_PushArgumentString(overrideName);
            NWNX_CallFunction(NWNX_Tileset, "CreateTileOverride");
        }

        /// <summary>
        /// Delete a tile override named overrideName.
        /// This will also delete all custom tile data associated with overrideName.
        /// </summary>
        /// <param name="overrideName">The name of the override.</param>
        public static void DeleteTileOverride(string overrideName)
        {
            NWNX_PushArgumentString(overrideName);
            NWNX_CallFunction(NWNX_Tileset, "DeleteTileOverride");
        }

        /// <summary>
        /// Set custom tile data for the tile at index in overrideName.
        /// An override must first be created with CreateTileOverride().
        /// </summary>
        /// <param name="overrideName">The name of the override.</param>
        /// <param name="index">The index of the tile.</param>
        /// <param name="customTileData">The custom tile data.</param>
        public static void SetOverrideTileData(string overrideName, int index, CustomTileData customTileData)
        {
            NWNX_PushArgumentInt(customTileData.AnimationLoop3 ? 1 : 0);
            NWNX_PushArgumentInt(customTileData.AnimationLoop2 ? 1 : 0);
            NWNX_PushArgumentInt(customTileData.AnimationLoop1 ? 1 : 0);
            NWNX_PushArgumentInt(customTileData.SourceLightColor2);
            NWNX_PushArgumentInt(customTileData.SourceLightColor1);
            NWNX_PushArgumentInt(customTileData.MainLightColor2);
            NWNX_PushArgumentInt(customTileData.MainLightColor1);
            NWNX_PushArgumentInt(customTileData.Height);
            NWNX_PushArgumentInt(customTileData.Orientation);
            NWNX_PushArgumentInt(customTileData.TileId);
            NWNX_PushArgumentInt(index);
            NWNX_PushArgumentString(overrideName);
            NWNX_CallFunction(NWNX_Tileset, "SetOverrideTileData");
        }

        /// <summary>
        /// Delete custom tile data of the tile at index in overrideName.
        /// </summary>
        /// <param name="overrideName">The name of the override.</param>
        /// <param name="index">The tile's index of -1 to remove all custom tile data.</param>
        public static void DeleteOverrideTileData(string overrideName, int index)
        {
            NWNX_PushArgumentInt(index);
            NWNX_PushArgumentString(overrideName);
            NWNX_CallFunction(NWNX_Tileset, "DeleteOverrideTileData");
        }
    }
}
