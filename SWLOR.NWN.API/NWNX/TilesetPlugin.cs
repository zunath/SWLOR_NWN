using NWN.Core.NWNX;

namespace SWLOR.NWN.API.NWNX
{
    /// <summary>
    /// Provides advanced tileset and tile property inspection, along with the ability to override the
    /// tile grid of an area created with CreateArea(). Useful for procedurally generated areas that need
    /// to read tileset metadata (terrain, crossers, groups, doors) or replace an area's tile layout at runtime.
    /// </summary>
    public static class TilesetPlugin
    {
        /// <summary>
        /// Retrieves general data about the specified tileset.
        /// </summary>
        /// <param name="sTileset">The tileset ResRef to query.</param>
        /// <returns>A TilesetData struct containing general tileset data.</returns>
        public static TilesetData GetTilesetData(string sTileset)
        {
            return global::NWN.Core.NWNX.TilesetPlugin.GetTilesetData(sTileset);
        }

        /// <summary>
        /// Gets the name of sTileset's terrain at nIndex.
        /// </summary>
        /// <param name="sTileset">The tileset.</param>
        /// <param name="nIndex">The index of the terrain. Range: TilesetData.nNumTerrain > nIndex >= 0.</param>
        /// <returns>The terrain name or "" on error.</returns>
        public static string GetTilesetTerrain(string sTileset, int nIndex)
        {
            return global::NWN.Core.NWNX.TilesetPlugin.GetTilesetTerrain(sTileset, nIndex);
        }

        /// <summary>
        /// Gets the name of sTileset's crosser at nIndex.
        /// </summary>
        /// <param name="sTileset">The tileset.</param>
        /// <param name="nIndex">The index of the crosser. Range: TilesetData.nNumCrossers > nIndex >= 0.</param>
        /// <returns>The crosser name or "" on error.</returns>
        public static string GetTilesetCrosser(string sTileset, int nIndex)
        {
            return global::NWN.Core.NWNX.TilesetPlugin.GetTilesetCrosser(sTileset, nIndex);
        }

        /// <summary>
        /// Gets general data of the group at nIndex in sTileset.
        /// </summary>
        /// <param name="sTileset">The tileset.</param>
        /// <param name="nIndex">The index of the group. Range: TilesetData.nNumGroups > nIndex >= 0.</param>
        /// <returns>A TilesetGroupData struct containing the group data.</returns>
        public static TilesetGroupData GetTilesetGroupData(string sTileset, int nIndex)
        {
            return global::NWN.Core.NWNX.TilesetPlugin.GetTilesetGroupData(sTileset, nIndex);
        }

        /// <summary>
        /// Gets the tile ID at nTileIndex in nGroupIndex of sTileset.
        /// </summary>
        /// <param name="sTileset">The tileset.</param>
        /// <param name="nGroupIndex">The index of the group. Range: TilesetData.nNumGroups > nGroupIndex >= 0.</param>
        /// <param name="nTileIndex">The index of the tile. Range: (TilesetGroupData.nRows * TilesetGroupData.nColumns) > nTileIndex >= 0.</param>
        /// <returns>The tile ID or 0 on error.</returns>
        public static int GetTilesetGroupTile(string sTileset, int nGroupIndex, int nTileIndex)
        {
            return global::NWN.Core.NWNX.TilesetPlugin.GetTilesetGroupTile(sTileset, nGroupIndex, nTileIndex);
        }

        /// <summary>
        /// Gets the model name of a tile in sTileset.
        /// </summary>
        /// <param name="sTileset">The tileset.</param>
        /// <param name="nTileID">The tile ID.</param>
        /// <returns>The tile's model name or "" on error.</returns>
        public static string GetTileModel(string sTileset, int nTileID)
        {
            return global::NWN.Core.NWNX.TilesetPlugin.GetTileModel(sTileset, nTileID);
        }

        /// <summary>
        /// Gets the minimap texture name of a tile in sTileset.
        /// </summary>
        /// <param name="sTileset">The tileset.</param>
        /// <param name="nTileID">The tile ID.</param>
        /// <returns>The tile's minimap texture name or "" on error.</returns>
        public static string GetTileMinimapTexture(string sTileset, int nTileID)
        {
            return global::NWN.Core.NWNX.TilesetPlugin.GetTileMinimapTexture(sTileset, nTileID);
        }

        /// <summary>
        /// Gets the edges and corners of a tile in sTileset.
        /// </summary>
        /// <param name="sTileset">The tileset.</param>
        /// <param name="nTileID">The tile ID.</param>
        /// <returns>A TileEdgesAndCorners struct describing the tile's edge/corner terrain names.</returns>
        public static TileEdgesAndCorners GetTileEdgesAndCorners(string sTileset, int nTileID)
        {
            return global::NWN.Core.NWNX.TilesetPlugin.GetTileEdgesAndCorners(sTileset, nTileID);
        }

        /// <summary>
        /// Gets the number of doors of a tile in sTileset.
        /// </summary>
        /// <param name="sTileset">The tileset.</param>
        /// <param name="nTileID">The tile ID.</param>
        /// <returns>The amount of doors.</returns>
        public static int GetTileNumDoors(string sTileset, int nTileID)
        {
            return global::NWN.Core.NWNX.TilesetPlugin.GetTileNumDoors(sTileset, nTileID);
        }

        /// <summary>
        /// Gets the door data of a tile in sTileset.
        /// </summary>
        /// <param name="sTileset">The tileset.</param>
        /// <param name="nTileID">The tile ID.</param>
        /// <param name="nIndex">The index of the door on the tile. Default is 0.</param>
        /// <returns>A TileDoorData struct containing the door's type and position/orientation.</returns>
        public static TileDoorData GetTileDoorData(string sTileset, int nTileID, int nIndex = 0)
        {
            return global::NWN.Core.NWNX.TilesetPlugin.GetTileDoorData(sTileset, nTileID, nIndex);
        }

        /// <summary>
        /// Overrides the tiles of sAreaResRef with the tile data in sOverrideName.
        /// </summary>
        /// <param name="sAreaResRef">The resref of the area to override.</param>
        /// <param name="sOverrideName">The name of the override containing the custom tile data, or "" to remove the override.</param>
        /// <remarks>
        /// Binds the override to the area resref rather than to a specific area instance: any future
        /// CreateArea() call for sAreaResRef will load using sOverrideName's tile grid. The override must
        /// be fully populated (via CreateTileOverride/SetOverrideTileData) before the area instance is created.
        /// Passing an empty sOverrideName unbinds any override previously set for sAreaResRef.
        /// </remarks>
        public static void SetAreaTileOverride(string sAreaResRef, string sOverrideName)
        {
            global::NWN.Core.NWNX.TilesetPlugin.SetAreaTileOverride(sAreaResRef, sOverrideName);
        }

        /// <summary>
        /// Creates a tile override named sOverrideName.
        /// </summary>
        /// <param name="sOverrideName">The name of the override.</param>
        /// <param name="sTileSet">The tileset the override should use.</param>
        /// <param name="nWidth">The width of the override, in tiles. Range: 1-32.</param>
        /// <param name="nHeight">The height of the override, in tiles. Range: 1-32.</param>
        public static void CreateTileOverride(string sOverrideName, string sTileSet, int nWidth, int nHeight)
        {
            global::NWN.Core.NWNX.TilesetPlugin.CreateTileOverride(sOverrideName, sTileSet, nWidth, nHeight);
        }

        /// <summary>
        /// Deletes a tile override named sOverrideName.
        /// </summary>
        /// <param name="sOverrideName">The name of the override.</param>
        /// <remarks>
        /// This will also delete all custom tile data associated with sOverrideName.
        /// </remarks>
        public static void DeleteTileOverride(string sOverrideName)
        {
            global::NWN.Core.NWNX.TilesetPlugin.DeleteTileOverride(sOverrideName);
        }

        /// <summary>
        /// Sets custom tile data for the tile at nIndex in sOverrideName.
        /// </summary>
        /// <param name="sOverrideName">The name of the override.</param>
        /// <param name="nIndex">The tile's index within the override grid.</param>
        /// <param name="strCustomTileData">A CustomTileData struct describing the tile. bAnimLoop1/2/3 are booleans (0 or 1).</param>
        /// <remarks>
        /// An override must first be created with CreateTileOverride().
        /// </remarks>
        public static void SetOverrideTileData(string sOverrideName, int nIndex, CustomTileData strCustomTileData)
        {
            global::NWN.Core.NWNX.TilesetPlugin.SetOverrideTileData(sOverrideName, nIndex, strCustomTileData);
        }

        /// <summary>
        /// Deletes custom tile data of the tile at nIndex in sOverrideName.
        /// </summary>
        /// <param name="sOverrideName">The name of the override.</param>
        /// <param name="nIndex">The tile's index, or -1 to remove all custom tile data.</param>
        public static void DeleteOverrideTileData(string sOverrideName, int nIndex)
        {
            global::NWN.Core.NWNX.TilesetPlugin.DeleteOverrideTileData(sOverrideName, nIndex);
        }
    }
}
