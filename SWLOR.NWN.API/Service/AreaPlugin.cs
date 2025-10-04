using System;
using System.Numerics;
using NWN.Core.NWNX;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.Service
{
    public static class AreaPlugin
    {
        private static IAreaPluginService _service = new AreaPluginService();

        internal static void SetService(IAreaPluginService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <inheritdoc cref="IAreaPluginService.GetNumberOfPlayersInArea"/>
        public static int GetNumberOfPlayersInArea(uint area) => _service.GetNumberOfPlayersInArea(area);

        /// <inheritdoc cref="IAreaPluginService.GetLastEntered"/>
        public static uint GetLastEntered(uint area) => _service.GetLastEntered(area);

        /// <inheritdoc cref="IAreaPluginService.GetLastLeft"/>
        public static uint GetLastLeft(uint area) => _service.GetLastLeft(area);

        /// <inheritdoc cref="IAreaPluginService.GetPVPSetting"/>
        public static PvPSettingType GetPVPSetting(uint area) => _service.GetPVPSetting(area);

        /// <inheritdoc cref="IAreaPluginService.SetPVPSetting"/>
        public static void SetPVPSetting(uint area, PvPSettingType pvpSetting) => _service.SetPVPSetting(area, pvpSetting);

        /// <inheritdoc cref="IAreaPluginService.GetAreaSpotModifier"/>
        public static int GetAreaSpotModifier(uint area) => _service.GetAreaSpotModifier(area);

        /// <inheritdoc cref="IAreaPluginService.SetAreaSpotModifier"/>
        public static void SetAreaSpotModifier(uint area, int spotModifier) => _service.SetAreaSpotModifier(area, spotModifier);

        /// <inheritdoc cref="IAreaPluginService.GetAreaListenModifier"/>
        public static int GetAreaListenModifier(uint area) => _service.GetAreaListenModifier(area);

        /// <inheritdoc cref="IAreaPluginService.SetAreaListenModifier"/>
        public static void SetAreaListenModifier(uint area, int listenModifier) => _service.SetAreaListenModifier(area, listenModifier);

        /// <inheritdoc cref="IAreaPluginService.GetNoRestingAllowed"/>
        public static bool GetNoRestingAllowed(uint area) => _service.GetNoRestingAllowed(area);

        /// <inheritdoc cref="IAreaPluginService.SetNoRestingAllowed"/>
        public static void SetNoRestingAllowed(uint area, bool bNoRestingAllowed) => _service.SetNoRestingAllowed(area, bNoRestingAllowed);

        /// <inheritdoc cref="IAreaPluginService.GetWindPower"/>
        public static int GetWindPower(uint area) => _service.GetWindPower(area);

        /// <inheritdoc cref="IAreaPluginService.SetWindPower"/>
        public static void SetWindPower(uint area, int windPower) => _service.SetWindPower(area, windPower);

        /// <inheritdoc cref="IAreaPluginService.GetWeatherChance"/>
        public static int GetWeatherChance(uint area, WeatherEffectType type) => _service.GetWeatherChance(area, type);

        /// <inheritdoc cref="IAreaPluginService.SetWeatherChance"/>
        public static void SetWeatherChance(uint area, WeatherEffectType type, int chance) => _service.SetWeatherChance(area, type, chance);

        /// <inheritdoc cref="IAreaPluginService.GetFogClipDistance"/>
        public static float GetFogClipDistance(uint area) => _service.GetFogClipDistance(area);

        /// <inheritdoc cref="IAreaPluginService.SetFogClipDistance"/>
        public static void SetFogClipDistance(uint area, float distance) => _service.SetFogClipDistance(area, distance);

        /// <inheritdoc cref="IAreaPluginService.GetShadowOpacity"/>
        public static int GetShadowOpacity(uint area) => _service.GetShadowOpacity(area);

        /// <inheritdoc cref="IAreaPluginService.SetShadowOpacity"/>
        public static void SetShadowOpacity(uint area, int shadowOpacity) => _service.SetShadowOpacity(area, shadowOpacity);

        /// <inheritdoc cref="IAreaPluginService.GetDayNightCycle"/>
        public static AreaDayNightCycleType GetDayNightCycle(uint area) => _service.GetDayNightCycle(area);

        /// <inheritdoc cref="IAreaPluginService.SetDayNightCycle"/>
        public static void SetDayNightCycle(uint area, AreaDayNightCycleType type) => _service.SetDayNightCycle(area, type);

        /// <inheritdoc cref="IAreaPluginService.GetSunMoonColors"/>
        public static int GetSunMoonColors(uint area, AreaLightColorType type) => _service.GetSunMoonColors(area, type);

        /// <inheritdoc cref="IAreaPluginService.SetSunMoonColors"/>
        public static void SetSunMoonColors(uint area, AreaLightColorType type, int color) => _service.SetSunMoonColors(area, type, color);

        /// <inheritdoc cref="IAreaPluginService.CreateTransition"/>
        public static uint CreateTransition(uint area, uint target, float x, float y, float z, float size = 2.0f, string tag = "") => 
            _service.CreateTransition(area, target, x, y, z, size, tag);

        /// <inheritdoc cref="IAreaPluginService.GetTileAnimationLoop"/>
        public static bool GetTileAnimationLoop(uint area, float tileX, float tileY, int animLoop) => 
            _service.GetTileAnimationLoop(area, tileX, tileY, animLoop);

        /// <inheritdoc cref="IAreaPluginService.SetTileAnimationLoop"/>
        public static void SetTileAnimationLoop(uint area, float tileX, float tileY, int animLoop, bool enabled) => 
            _service.SetTileAnimationLoop(area, tileX, tileY, animLoop, enabled);

        /// <inheritdoc cref="IAreaPluginService.CreateGenericTrigger"/>
        public static uint CreateGenericTrigger(uint area, float x, float y, float z, string tag = "", float size = 1.0f) => 
            _service.CreateGenericTrigger(area, x, y, z, tag, size);

        /// <inheritdoc cref="IAreaPluginService.AddObjectToExclusionList"/>
        public static void AddObjectToExclusionList(uint oObject) => _service.AddObjectToExclusionList(oObject);

        /// <inheritdoc cref="IAreaPluginService.RemoveObjectFromExclusionList"/>
        public static void RemoveObjectFromExclusionList(uint oObject) => _service.RemoveObjectFromExclusionList(oObject);

        /// <inheritdoc cref="IAreaPluginService.ExportGIT"/>
        public static bool ExportGIT(uint oArea, string sFileName = "", bool bExportVarTable = true, bool bExportUUID = true, int nObjectFilter = 0, string sAlias = "NWNX") => 
            _service.ExportGIT(oArea, sFileName, bExportVarTable, bExportUUID, nObjectFilter, sAlias);

        /// <inheritdoc cref="IAreaPluginService.GetTileInfo"/>
        public static TileInfo GetTileInfo(uint oArea, float fTileX, float fTileY) => _service.GetTileInfo(oArea, fTileX, fTileY);

        /// <inheritdoc cref="IAreaPluginService.ExportARE"/>
        public static bool ExportARE(uint oArea, string sFileName, string sNewName = "", string sNewTag = "", string sAlias = "NWNX") => 
            _service.ExportARE(oArea, sFileName, sNewName, sNewTag, sAlias);

        /// <inheritdoc cref="IAreaPluginService.GetAmbientSoundDay"/>
        public static int GetAmbientSoundDay(uint oArea) => _service.GetAmbientSoundDay(oArea);

        /// <inheritdoc cref="IAreaPluginService.GetAmbientSoundNight"/>
        public static int GetAmbientSoundNight(uint oArea) => _service.GetAmbientSoundNight(oArea);

        /// <inheritdoc cref="IAreaPluginService.GetAmbientSoundDayVolume"/>
        public static int GetAmbientSoundDayVolume(uint oArea) => _service.GetAmbientSoundDayVolume(oArea);

        /// <inheritdoc cref="IAreaPluginService.GetAmbientSoundNightVolume"/>
        public static int GetAmbientSoundNightVolume(uint oArea) => _service.GetAmbientSoundNightVolume(oArea);

        /// <inheritdoc cref="IAreaPluginService.CreateSoundObject"/>
        public static uint CreateSoundObject(uint oArea, Vector3 vPosition, string sResRef) => _service.CreateSoundObject(oArea, vPosition, sResRef);

        /// <inheritdoc cref="IAreaPluginService.RotateArea"/>
        public static void RotateArea(uint oArea, int nRotation) => _service.RotateArea(oArea, nRotation);

        /// <inheritdoc cref="IAreaPluginService.GetTileInfoByTileIndex"/>
        public static TileInfo GetTileInfoByTileIndex(uint oArea, int nTileIndex) => _service.GetTileInfoByTileIndex(oArea, nTileIndex);

        /// <inheritdoc cref="IAreaPluginService.GetPathExists"/>
        public static bool GetPathExists(uint oArea, Vector3 vStart, Vector3 vEnd, int nMaxDepth) => _service.GetPathExists(oArea, vStart, vEnd, nMaxDepth);

        /// <inheritdoc cref="IAreaPluginService.GetAreaFlags"/>
        public static int GetAreaFlags(uint oArea) => _service.GetAreaFlags(oArea);

        /// <inheritdoc cref="IAreaPluginService.SetAreaFlags"/>
        public static void SetAreaFlags(uint oArea, int nFlags) => _service.SetAreaFlags(oArea, nFlags);

        /// <inheritdoc cref="IAreaPluginService.GetAreaWind"/>
        public static AreaWind GetAreaWind(uint oArea) => _service.GetAreaWind(oArea);

        /// <inheritdoc cref="IAreaPluginService.SetDefaultObjectUiDiscoveryMask"/>
        public static void SetDefaultObjectUiDiscoveryMask(uint oArea, int nObjectTypes, int nMask, bool bForceUpdate = false) => 
            _service.SetDefaultObjectUiDiscoveryMask(oArea, nObjectTypes, nMask, bForceUpdate);
    }
}
