using System.Numerics;
using NWN.Core.NWNX;
using SWLOR.Game.Server.Core;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Area;
using TilesetPlugin = SWLOR.NWN.API.NWNX.TilesetPlugin;
using AreaPlugin = SWLOR.NWN.API.NWNX.AreaPlugin;
using SWLOR.Game.Server.Service.AreaGenerationService.Atmosphere;

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
        public static uint Realize(ResolvedLayout layout, string placeholderResref, string overrideName, string tag, string displayName, DungeonTileLighting lighting = null,
            DungeonAreaAtmosphere atmosphere = null)
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
                ApplyAtmosphere(area, atmosphere);
            }

            return area;
        }

        /// <summary>
        /// Applies the runtime-settable subset of a composed family atmosphere to a freshly cloned
        /// instance (see DungeonAreaAtmosphere): skybox, day/night behavior, sun/moon
        /// ambient/diffuse colors, fog color/amount, wind, shadow opacity, fog clip distance, and
        /// weather chances. The instance otherwise inherits the placeholder area's daylight
        /// test-grid values, which is exactly the "generated areas feel like white test grids"
        /// problem the atmosphere declarations exist to fix. Null = no declared atmosphere; the
        /// clone keeps the placeholder's values, matching the offline emitter's null behavior.
        ///
        /// Three .are fields have NO runtime setter (engine/NWNX expose none) and are therefore
        /// honest offline-only fields the .are emission path alone can set: SunShadows/MoonShadows
        /// (shadow-casting booleans), LightingScheme, and LoadScreenID. Live instances keep the
        /// placeholder's values for those three.
        /// </summary>
        private static void ApplyAtmosphere(uint area, DungeonAreaAtmosphere atmosphere)
        {
            if (atmosphere == null)
                return;

            SetSkyBox((Skybox)atmosphere.SkyBox, area);

            // .are stores the pair (DayNightCycle, IsNight); the runtime models the same three
            // states as a single enum.
            var cycle = atmosphere.DayNightCycle
                ? DayNightCycle.CycleDayNight
                : atmosphere.IsNight
                    ? DayNightCycle.AlwaysDark
                    : DayNightCycle.AlwaysBright;
            AreaPlugin.SetDayNightCycle(area, cycle);

            // DungeonAreaAtmosphere stores colors in the .are dword encoding, which is the engine's
            // NATIVE byte order (BGR -- e.g. Tatooine's hand-built haze dword only reads as sandy
            // tan in BGR). Both color-setting APIs here take standard RGB hex instead and swap to
            // BGR internally: NWNX SetSunMoonColors swaps explicitly (see unified
            // Plugins/Area/Area.cpp), and base SetFogColor's FOG_COLOR_* constants are RGB-encoded.
            // Convert native -> RGB on the way in so the live instance's stored values land
            // byte-identical to the hand-built .are evidence. (NWNX GetSunMoonColors returns the
            // raw NATIVE value with no swap; base GetFogColor mirrors its own Set and returns RGB
            // -- the self-test readback accounts for the asymmetry.)
            AreaPlugin.SetSunMoonColors(area, AreaLightColorType.SunAmbient, SwapRedBlue(atmosphere.SunAmbientColor));
            AreaPlugin.SetSunMoonColors(area, AreaLightColorType.SunDiffuse, SwapRedBlue(atmosphere.SunDiffuseColor));
            AreaPlugin.SetSunMoonColors(area, AreaLightColorType.MoonAmbient, SwapRedBlue(atmosphere.MoonAmbientColor));
            AreaPlugin.SetSunMoonColors(area, AreaLightColorType.MoonDiffuse, SwapRedBlue(atmosphere.MoonDiffuseColor));

            SetFogColor(FogType.Sun, (FogColor)SwapRedBlue(atmosphere.SunFogColor), area);
            SetFogColor(FogType.Moon, (FogColor)SwapRedBlue(atmosphere.MoonFogColor), area);
            SetFogAmount(FogType.Sun, atmosphere.SunFogAmount, area);
            SetFogAmount(FogType.Moon, atmosphere.MoonFogAmount, area);

            AreaPlugin.SetWindPower(area, atmosphere.WindPower);
            AreaPlugin.SetShadowOpacity(area, atmosphere.ShadowOpacity);
            AreaPlugin.SetFogClipDistance(area, atmosphere.FogClipDist);
            AreaPlugin.SetWeatherChance(area, WeatherEffectType.Rain, atmosphere.ChanceRain);
            AreaPlugin.SetWeatherChance(area, WeatherEffectType.Snow, atmosphere.ChanceSnow);
            AreaPlugin.SetWeatherChance(area, WeatherEffectType.Lightning, atmosphere.ChanceLightning);
        }

        /// <summary>
        /// Swaps the red and blue channels of a 24-bit color dword -- converts between the .are/
        /// engine-native BGR encoding DungeonAreaAtmosphere carries and the RGB hex the color-setting
        /// script/NWNX APIs expect (see ApplyAtmosphere's conversion note).
        /// </summary>
        public static int SwapRedBlue(int color)
        {
            return ((color & 0x0000FF) << 16) | (color & 0x00FF00) | ((color >> 16) & 0x0000FF);
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
