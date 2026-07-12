using System.Numerics;
using NWN.Core.NWNX;
using SWLOR.Game.Server.Core;
using SWLOR.NWN.API.NWScript.Enum;
using TilesetPlugin = SWLOR.NWN.API.NWNX.TilesetPlugin;
using AreaPlugin = SWLOR.NWN.API.NWNX.AreaPlugin;

namespace SWLOR.Game.Server.Service.AreaGenerationService
{
    /// <summary>
    /// Turns a resolved layout into a live area instance via NWNX tileset overrides.
    /// Override data must be fully populated before the area instance is created,
    /// and geometry is never mutated once players can enter.
    /// </summary>
    public static class AreaSynthesizer
    {
        private const float TileSize = 10.0f;

        /// <summary>
        /// Creates an area instance whose tile grid is the resolved layout.
        /// The override binding on the placeholder resref is removed immediately after
        /// instancing — callers must serialize Realize calls (the generation queue does).
        /// Returns OBJECT_INVALID on engine failure.
        /// </summary>
        public static uint Realize(ResolvedLayout layout, string placeholderResref, string overrideName, string tag, string displayName, DungeonTileLighting lighting = null)
        {
            lighting ??= new DungeonTileLighting();
            TilesetPlugin.CreateTileOverride(overrideName, layout.TilesetResref, layout.Width, layout.Height);

            for (var index = 0; index < layout.Tiles.Length; index++)
            {
                var tile = layout.Tiles[index];
                TilesetPlugin.SetOverrideTileData(overrideName, index, new CustomTileData
                {
                    nTileID = tile.TileId,
                    nOrientation = tile.Orientation,
                    nHeight = tile.Height,
                    nMainLightColor1 = lighting.MainLight1,
                    nMainLightColor2 = lighting.MainLight2,
                    nSourceLightColor1 = lighting.SourceLight1,
                    nSourceLightColor2 = lighting.SourceLight2,
                    bAnimLoop1 = 1,
                    bAnimLoop2 = 1,
                    bAnimLoop3 = 1
                });
            }

            TilesetPlugin.SetAreaTileOverride(placeholderResref, overrideName);
            var area = CreateArea(placeholderResref, tag, displayName);
            TilesetPlugin.SetAreaTileOverride(placeholderResref, string.Empty);

            if (GetIsObjectValid(area))
            {
                SetEventScript(area, EventScript.Area_OnEnter, ScriptName.OnAreaEnter);
                SetEventScript(area, EventScript.Area_OnExit, ScriptName.OnAreaExit);
                SetEventScript(area, EventScript.Area_OnUserDefined, ScriptName.OnAreaUserDefined);
            }

            return area;
        }

        /// <summary>
        /// Validates that every room center is reachable from the entrance room center
        /// using tile path nodes. Must pass before any player is allowed in.
        /// </summary>
        public static bool ValidatePaths(uint area, ResolvedLayout layout, out string failureReason)
        {
            failureReason = string.Empty;

            LayoutRoom entrance = null;
            foreach (var room in layout.Rooms)
            {
                if (room.Role == RoomRole.Entrance)
                {
                    entrance = room;
                    break;
                }
            }

            if (entrance == null)
            {
                failureReason = "Layout has no entrance room.";
                return false;
            }

            var maxDepth = layout.Width * layout.Height;
            var start = TileCenterPosition(area, entrance.CenterTile.X, entrance.CenterTile.Y);

            foreach (var room in layout.Rooms)
            {
                if (room.Id == entrance.Id)
                    continue;

                // LayoutGroupStamper set-piece rooms (WallRooms) sit on fully-solid corner cells and
                // are entered via their own baked model walkmesh, not the abstract tile path graph
                // this check reasons about (their pathnodes are often not 'A') — skip them.
                if (room.IsSetPiece)
                    continue;

                var end = TileCenterPosition(area, room.CenterTile.X, room.CenterTile.Y);
                if (!AreaPlugin.GetPathExists(area, start, end, maxDepth))
                {
                    failureReason = $"No path from entrance room {entrance.Id} to room {room.Id} at tile ({room.CenterTile.X}, {room.CenterTile.Y}).";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Computes walkable spawn/jump points at the center of every fully-open room tile,
        /// with ground height sampled from the realized area.
        /// </summary>
        public static void ComputeWalkablePoints(uint area, ResolvedLayout layout, RuntimeAreaInstance instance)
        {
            instance.WalkablePoints.Clear();

            foreach (var room in layout.Rooms)
            {
                foreach (var (x, y) in room.Tiles)
                {
                    instance.WalkablePoints.Add(TileCenterPosition(area, x, y));
                }
            }
        }

        private static Vector3 TileCenterPosition(uint area, int tileX, int tileY)
        {
            var x = tileX * TileSize + TileSize / 2f;
            var y = tileY * TileSize + TileSize / 2f;
            var z = GetGroundHeight(Location(area, new Vector3(x, y, 0f), 0f));
            return new Vector3(x, y, z);
        }
    }
}
