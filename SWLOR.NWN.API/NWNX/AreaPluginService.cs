using System.Numerics;
using NWN.Core.NWNX;
using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWNX
{
    /// <summary>
    /// Provides comprehensive area management functionality including player tracking, area properties,
    /// environmental settings, tile management, and area export capabilities.
    /// This plugin allows for dynamic modification of area properties and monitoring of area events.
    /// </summary>
    public class AreaPluginService : IAreaPluginService
    {
        /// <inheritdoc/>
        public int GetNumberOfPlayersInArea(uint area)
        {
            return AreaPlugin.GetNumberOfPlayersInArea(area);
        }

        /// <inheritdoc/>
        public uint GetLastEntered(uint area)
        {
            return AreaPlugin.GetLastEntered(area);
        }

        /// <inheritdoc/>
        public uint GetLastLeft(uint area)
        {
            return AreaPlugin.GetLastLeft(area);
        }

        /// <inheritdoc/>
        public PvPSettingType GetPVPSetting(uint area)
        {
            int result = AreaPlugin.GetPVPSetting(area);
            return (PvPSettingType)result;
        }

        /// <inheritdoc/>
        public void SetPVPSetting(uint area, PvPSettingType pvpSetting)
        {
            AreaPlugin.SetPVPSetting(area, (int)pvpSetting);
        }

        /// <inheritdoc/>
        public int GetAreaSpotModifier(uint area)
        {
            return AreaPlugin.GetAreaSpotModifier(area);
        }

        /// <inheritdoc/>
        public void SetAreaSpotModifier(uint area, int spotModifier)
        {
            AreaPlugin.SetAreaSpotModifier(area, spotModifier);
        }

        /// <inheritdoc/>
        public int GetAreaListenModifier(uint area)
        {
            return AreaPlugin.GetAreaListenModifier(area);
        }

        /// <inheritdoc/>
        public void SetAreaListenModifier(uint area, int listenModifier)
        {
            AreaPlugin.SetAreaListenModifier(area, listenModifier);
        }

        /// <inheritdoc/>
        public bool GetNoRestingAllowed(uint area)
        {
            int result = AreaPlugin.GetNoRestingAllowed(area);
            return result != 0;
        }

        /// <inheritdoc/>
        public void SetNoRestingAllowed(uint area, bool bNoRestingAllowed)
        {
            AreaPlugin.SetNoRestingAllowed(area, bNoRestingAllowed ? 1 : 0);
        }

        /// <inheritdoc/>
        public int GetWindPower(uint area)
        {
            return AreaPlugin.GetWindPower(area);
        }

        /// <inheritdoc/>
        public void SetWindPower(uint area, int windPower)
        {
            AreaPlugin.SetWindPower(area, windPower);
        }

        /// <inheritdoc/>
        public int GetWeatherChance(uint area, WeatherEffectType type)
        {
            return AreaPlugin.GetWeatherChance(area, (int)type);
        }

        /// <inheritdoc/>
        public void SetWeatherChance(uint area, WeatherEffectType type, int chance)
        {
            AreaPlugin.SetWeatherChance(area, (int)type, chance);
        }

        /// <inheritdoc/>
        public float GetFogClipDistance(uint area)
        {
            return AreaPlugin.GetFogClipDistance(area);
        }

        /// <inheritdoc/>
        public void SetFogClipDistance(uint area, float distance)
        {
            AreaPlugin.SetFogClipDistance(area, distance);
        }

        /// <inheritdoc/>
        public int GetShadowOpacity(uint area)
        {
            return AreaPlugin.GetShadowOpacity(area);
        }

        /// <inheritdoc/>
        public void SetShadowOpacity(uint area, int shadowOpacity)
        {
            AreaPlugin.SetShadowOpacity(area, shadowOpacity);
        }

        /// <inheritdoc/>
        public AreaDayNightCycleType GetDayNightCycle(uint area)
        {
            int result = AreaPlugin.GetDayNightCycle(area);
            return (AreaDayNightCycleType)result;
        }

        /// <inheritdoc/>
        public void SetDayNightCycle(uint area, AreaDayNightCycleType type)
        {
            AreaPlugin.SetDayNightCycle(area, (int)type);
        }

        /// <inheritdoc/>
        public int GetSunMoonColors(uint area, AreaLightColorType type)
        {
            return AreaPlugin.GetSunMoonColors(area, (int)type);
        }

        /// <inheritdoc/>
        public void SetSunMoonColors(uint area, AreaLightColorType type, int color)
        {
            AreaPlugin.SetSunMoonColors(area, (int)type, color);
        }

        /// <inheritdoc/>
        public uint CreateTransition(uint area, uint target, float x, float y, float z, float size = 2.0f,
            string tag = "")
        {
            return AreaPlugin.CreateTransition(area, target, x, y, z, size, tag);
        }

        /// <inheritdoc/>
        public bool GetTileAnimationLoop(uint area, float tileX, float tileY, int animLoop)
        {
            int result = AreaPlugin.GetTileAnimationLoop(area, tileX, tileY, animLoop);
            return result != 0;
        }

        /// <inheritdoc/>
        public void SetTileAnimationLoop(uint area, float tileX, float tileY, int animLoop, bool enabled)
        {
            AreaPlugin.SetTileAnimationLoop(area, tileX, tileY, animLoop, enabled ? 1 : 0);
        }

        /// <inheritdoc/>
        public uint CreateGenericTrigger(uint area, float x, float y, float z, string tag = "",
            float size = 1.0f)
        {
            return AreaPlugin.CreateGenericTrigger(area, x, y, z, tag, size);
        }


        /// <inheritdoc/>
        public void AddObjectToExclusionList(uint oObject)
        {
            AreaPlugin.AddObjectToExclusionList(oObject);
        }

        /// <inheritdoc/>
        public void RemoveObjectFromExclusionList(uint oObject)
        {
            AreaPlugin.RemoveObjectFromExclusionList(oObject);
        }

        /// <inheritdoc/>
        public bool ExportGIT(uint oArea, string sFileName = "", bool bExportVarTable = true, bool bExportUUID = true, int nObjectFilter = 0, string sAlias = "NWNX")
        {
            int result = AreaPlugin.ExportGIT(oArea, sFileName, bExportVarTable ? 1 : 0, bExportUUID ? 1 : 0, nObjectFilter, sAlias);
            return result != 0;
        }


        /// <inheritdoc/>
        public TileInfo GetTileInfo(uint oArea, float fTileX, float fTileY)
        {
            return AreaPlugin.GetTileInfo(oArea, fTileX, fTileY);
        }

        /// <inheritdoc/>
        public bool ExportARE(uint oArea, string sFileName, string sNewName = "", string sNewTag = "", string sAlias = "NWNX")
        {
            int result = AreaPlugin.ExportARE(oArea, sFileName, sNewName, sNewTag, sAlias);
            return result != 0;
        }

        /// <inheritdoc/>
        public int GetAmbientSoundDay(uint oArea)
        {
            return AreaPlugin.GetAmbientSoundDay(oArea);
        }

        /// <inheritdoc/>
        public int GetAmbientSoundNight(uint oArea)
        {
            return AreaPlugin.GetAmbientSoundNight(oArea);
        }

        /// <inheritdoc/>
        public int GetAmbientSoundDayVolume(uint oArea)
        {
            return AreaPlugin.GetAmbientSoundDayVolume(oArea);
        }

        /// <inheritdoc/>
        public int GetAmbientSoundNightVolume(uint oArea)
        {
            return AreaPlugin.GetAmbientSoundNightVolume(oArea);
        }

        /// <inheritdoc/>
        public uint CreateSoundObject(uint oArea, Vector3 vPosition, string sResRef)
        {
            return AreaPlugin.CreateSoundObject(oArea, vPosition, sResRef);
        }


        /// <inheritdoc/>
        public void RotateArea(uint oArea, int nRotation)
        {
            AreaPlugin.RotateArea(oArea, nRotation);
        }

        /// <inheritdoc/>
        public TileInfo GetTileInfoByTileIndex(uint oArea, int nTileIndex)
        {
            return AreaPlugin.GetTileInfoByTileIndex(oArea, nTileIndex);
        }

        /// <inheritdoc/>
        public bool GetPathExists(uint oArea, Vector3 vStart, Vector3 vEnd, int nMaxDepth)
        {
            int result = AreaPlugin.GetPathExists(oArea, vStart, vEnd, nMaxDepth);
            return result != 0;
        }

        /// <inheritdoc/>
        public int GetAreaFlags(uint oArea)
        {
            return AreaPlugin.GetAreaFlags(oArea);
        }

        /// <inheritdoc/>
        public void SetAreaFlags(uint oArea, int nFlags)
        {
            AreaPlugin.SetAreaFlags(oArea, nFlags);
        }

        /// <inheritdoc/>
        public AreaWind GetAreaWind(uint oArea)
        {
            return AreaPlugin.GetAreaWind(oArea);
        }

        /// <inheritdoc/>
        public void SetDefaultObjectUiDiscoveryMask(uint oArea, int nObjectTypes, int nMask, bool bForceUpdate = false)
        {
            AreaPlugin.SetDefaultObjectUiDiscoveryMask(oArea, nObjectTypes, nMask, bForceUpdate ? 1 : 0);
        }
    }
}