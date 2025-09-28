using System.Numerics;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using ObjectType = SWLOR.NWN.API.NWScript.Enum.ObjectType;
using WeatherType = SWLOR.NWN.API.NWScript.Enum.WeatherType;

namespace SWLOR.Test.Shared.NWScript
{
    public partial class NWScriptServiceMock
    {
        // Mock data storage for areas
        private readonly Dictionary<uint, AreaData> _areaData = new();
        private readonly Dictionary<uint, Vector3> _objectPositions = new();
        private readonly Dictionary<uint, uint> _objectAreas = new();
        private readonly Dictionary<Location, LocationData> _locationData = new();
        private readonly Dictionary<uint, List<uint>> _areaObjects = new();
        private readonly Dictionary<uint, Dictionary<uint, bool>> _exploredTiles = new();
        private readonly Dictionary<uint, bool> _creatureExploresMinimap = new();
        private readonly Dictionary<uint, Dictionary<Location, TileData>> _tileData = new();
        private uint _enteringObject = OBJECT_INVALID;
        private uint _exitingObject = OBJECT_INVALID;
        private uint _currentAreaIterator = OBJECT_INVALID;
        private uint _currentObjectIterator = OBJECT_INVALID;
        private uint _currentPersistentObjectIterator = OBJECT_INVALID;

        public class AreaData
        {
            public string TilesetResRef { get; set; } = "";
            public bool IsInterior { get; set; } = false;
            public WeatherType Weather { get; set; } = WeatherType.Clear;
            public AreaNaturalType NaturalType { get; set; } = AreaNaturalType.Natural;
            public bool IsAboveGround { get; set; } = true;
            public SkyboxType SkyBox { get; set; } = SkyboxType.None;
            public Dictionary<FogType, FogData> FogSettings { get; set; } = new();
            public Dictionary<AreaLightColorType, int> LightColors { get; set; } = new();
            public Dictionary<AreaLightDirectionType, Vector3> LightDirections { get; set; } = new();
            public Dictionary<AreaDimensionType, int> AreaSizes { get; set; } = new();
            public MusicData Music { get; set; } = new();
            public AmbientSoundData AmbientSound { get; set; } = new();
            public Vector3 WindDirection { get; set; } = new Vector3(0, 0, 0);
            public float WindMagnitude { get; set; } = 0.0f;
            public float WindYaw { get; set; } = 0.0f;
            public float WindPitch { get; set; } = 0.0f;
        }

        public class LocationData
        {
            public uint Area { get; set; } = OBJECT_INVALID;
            public Vector3 Position { get; set; } = new Vector3(0, 0, 0);
            public float Orientation { get; set; } = 0.0f;
        }

        public class TileData
        {
            public int TileID { get; set; } = 0;
            public int Orientation { get; set; } = 0;
            public int Height { get; set; } = 0;
            public int MainLight1Color { get; set; } = 0;
            public int MainLight2Color { get; set; } = 0;
            public int SourceLight1Color { get; set; } = 0;
            public int SourceLight2Color { get; set; } = 0;
            public bool AnimLoop1 { get; set; } = false;
            public bool AnimLoop2 { get; set; } = false;
            public bool AnimLoop3 { get; set; } = false;
            public string Json { get; set; } = "";
        }

        public class MusicData
        {
            public bool IsPlaying { get; set; } = false;
            public int DayTrack { get; set; } = 0;
            public int NightTrack { get; set; } = 0;
            public int BattleTrack { get; set; } = 0;
            public int Delay { get; set; } = 0;
        }

        public class AmbientSoundData
        {
            public bool IsPlaying { get; set; } = false;
            public int DayTrack { get; set; } = 0;
            public int NightTrack { get; set; } = 0;
            public int DayVolume { get; set; } = 100;
            public int NightVolume { get; set; } = 100;
        }

        public class FogData
        {
            public FogColorType Color { get; set; } = FogColorType.Grey;
            public int Amount { get; set; } = 0;
        }

        private AreaData GetOrCreateAreaData(uint oArea)
        {
            if (!_areaData.ContainsKey(oArea))
                _areaData[oArea] = new AreaData();
            return _areaData[oArea];
        }

        public uint GetArea(uint oTarget) => _objectAreas.GetValueOrDefault(oTarget, OBJECT_INVALID);
        public uint GetEnteringObject() => _enteringObject;
        public uint GetExitingObject() => _exitingObject;
        public Vector3 GetPosition(uint oTarget) => _objectPositions.GetValueOrDefault(oTarget, new Vector3(0, 0, 0));

        public void MusicBackgroundPlay(uint oArea) => GetOrCreateAreaData(oArea).Music.IsPlaying = true;
        public void MusicBackgroundStop(uint oArea) => GetOrCreateAreaData(oArea).Music.IsPlaying = false;
        public void MusicBackgroundSetDelay(uint oArea, int nDelay) => GetOrCreateAreaData(oArea).Music.Delay = nDelay;
        public void MusicBackgroundChangeDay(uint oArea, int nTrack) => GetOrCreateAreaData(oArea).Music.DayTrack = nTrack;
        public void MusicBackgroundChangeNight(uint oArea, int nTrack) => GetOrCreateAreaData(oArea).Music.NightTrack = nTrack;
        public void MusicBattlePlay(uint oArea) => GetOrCreateAreaData(oArea).Music.IsPlaying = true;
        public void MusicBattleStop(uint oArea) => GetOrCreateAreaData(oArea).Music.IsPlaying = false;
        public void MusicBattleChange(uint oArea, int nTrack) => GetOrCreateAreaData(oArea).Music.BattleTrack = nTrack;
        public void AmbientSoundPlay(uint oArea) => GetOrCreateAreaData(oArea).AmbientSound.IsPlaying = true;
        public void AmbientSoundStop(uint oArea) => GetOrCreateAreaData(oArea).AmbientSound.IsPlaying = false;
        public void AmbientSoundChangeDay(uint oArea, int nTrack) => GetOrCreateAreaData(oArea).AmbientSound.DayTrack = nTrack;
        public void AmbientSoundChangeNight(uint oArea, int nTrack) => GetOrCreateAreaData(oArea).AmbientSound.NightTrack = nTrack;
        public void RecomputeStaticLighting(uint oArea) { }

        public int MusicBackgroundGetDayTrack(uint oArea) => GetOrCreateAreaData(oArea).Music.DayTrack;
        public int MusicBackgroundGetNightTrack(uint oArea) => GetOrCreateAreaData(oArea).Music.NightTrack;
        public void AmbientSoundSetDayVolume(uint oArea, int nVolume) => GetOrCreateAreaData(oArea).AmbientSound.DayVolume = nVolume;
        public void AmbientSoundSetNightVolume(uint oArea, int nVolume) => GetOrCreateAreaData(oArea).AmbientSound.NightVolume = nVolume;
        public int MusicBackgroundGetBattleTrack(uint oArea) => GetOrCreateAreaData(oArea).Music.BattleTrack;

        public bool GetIsAreaInterior(uint oArea = OBJECT_INVALID) => GetOrCreateAreaData(oArea).IsInterior;
        public WeatherType GetWeather(uint oArea) => GetOrCreateAreaData(oArea).Weather;
        public AreaNaturalType GetIsAreaNatural(uint oArea) => GetOrCreateAreaData(oArea).NaturalType;
        public bool GetIsAreaAboveGround(uint oArea) => GetOrCreateAreaData(oArea).IsAboveGround;

        public void SetSkyBox(SkyboxType nSkyBox, uint oArea = OBJECT_INVALID) => GetOrCreateAreaData(oArea).SkyBox = nSkyBox;
        public void SetFogColor(FogType nFogType, FogColorType nFogColor, uint oArea = OBJECT_INVALID) 
        {
            var area = GetOrCreateAreaData(oArea);
            if (!area.FogSettings.ContainsKey(nFogType))
                area.FogSettings[nFogType] = new FogData();
            area.FogSettings[nFogType].Color = nFogColor;
        }
        public SkyboxType GetSkyBox(uint oArea = OBJECT_INVALID) => GetOrCreateAreaData(oArea).SkyBox;
        public FogColorType GetFogColor(FogType nFogType, uint oArea = OBJECT_INVALID) => 
            GetOrCreateAreaData(oArea).FogSettings.GetValueOrDefault(nFogType, new FogData()).Color;
        public void SetFogAmount(FogType nFogType, int nFogAmount, uint oArea = OBJECT_INVALID) 
        {
            var area = GetOrCreateAreaData(oArea);
            if (!area.FogSettings.ContainsKey(nFogType))
                area.FogSettings[nFogType] = new FogData();
            area.FogSettings[nFogType].Amount = nFogAmount;
        }
        public int GetFogAmount(FogType nFogType, uint oArea = OBJECT_INVALID) => 
            GetOrCreateAreaData(oArea).FogSettings.GetValueOrDefault(nFogType, new FogData()).Amount;

        public string GetTilesetResRef(uint oArea) => GetOrCreateAreaData(oArea).TilesetResRef;
        public int GetAreaSize(AreaDimensionType nAreaDimension, uint oArea = OBJECT_INVALID) => 
            GetOrCreateAreaData(oArea).AreaSizes.GetValueOrDefault(nAreaDimension, 0);

        public int DestroyArea(uint oArea) 
        {
            _areaData.Remove(oArea);
            return 1; // Success
        }

        public uint CreateArea(string sSourceResRef, string sNewTag = "", string sNewName = "") 
        {
            var newArea = (uint)(_areaData.Count + 1000); // Generate unique ID
            var areaData = GetOrCreateAreaData(newArea);
            areaData.TilesetResRef = sSourceResRef;
            return newArea;
        }

        public uint CopyArea(uint oArea) 
        {
            if (!_areaData.ContainsKey(oArea))
                return OBJECT_INVALID;
            
            var newArea = (uint)(_areaData.Count + 1000);
            _areaData[newArea] = _areaData[oArea];
            return newArea;
        }

        public uint GetFirstArea() 
        {
            _currentAreaIterator = _areaData.Keys.FirstOrDefault(OBJECT_INVALID);
            return _currentAreaIterator;
        }

        public uint GetNextArea() 
        {
            if (_currentAreaIterator == OBJECT_INVALID)
                return OBJECT_INVALID;
            
            var areas = _areaData.Keys.ToList();
            var currentIndex = areas.IndexOf(_currentAreaIterator);
            if (currentIndex >= 0 && currentIndex < areas.Count - 1)
            {
                _currentAreaIterator = areas[currentIndex + 1];
                return _currentAreaIterator;
            }
            
            _currentAreaIterator = OBJECT_INVALID;
            return OBJECT_INVALID;
        }

        public uint GetFirstObjectInArea(uint oArea = OBJECT_INVALID, ObjectType nObjectFilter = ObjectType.All) 
        {
            _currentObjectIterator = OBJECT_INVALID;
            if (_areaObjects.ContainsKey(oArea) && _areaObjects[oArea].Count > 0)
            {
                _currentObjectIterator = _areaObjects[oArea][0];
            }
            return _currentObjectIterator;
        }

        public uint GetNextObjectInArea(uint oArea = OBJECT_INVALID, ObjectType nObjectFilter = ObjectType.All) 
        {
            if (_currentObjectIterator == OBJECT_INVALID || !_areaObjects.ContainsKey(oArea))
                return OBJECT_INVALID;
            
            var objects = _areaObjects[oArea];
            var currentIndex = objects.IndexOf(_currentObjectIterator);
            if (currentIndex >= 0 && currentIndex < objects.Count - 1)
            {
                _currentObjectIterator = objects[currentIndex + 1];
                return _currentObjectIterator;
            }
            
            _currentObjectIterator = OBJECT_INVALID;
            return OBJECT_INVALID;
        }

        public Location GetLocation(uint oObject) 
        {
            var invalidLocation = new Location(0);
            if (_locationData.ContainsKey(invalidLocation))
            {
                var data = _locationData[invalidLocation];
                return CreateLocation(data.Area, data.Position, data.Orientation);
            }
            return invalidLocation;
        }

        public void ActionJumpToLocation(Location lLocation) { }

        public Location CreateLocation(uint oArea, Vector3 vPosition, float fOrientation) 
        {
            var location = new Location(0);
            _locationData[location] = new LocationData
            {
                Area = oArea,
                Position = vPosition,
                Orientation = fOrientation
            };
            return location;
        }

        public void ApplyEffectAtLocation(DurationType nDurationType, Effect eEffect, Location lLocation, float fDuration = 0.0f) { }
        public void ExploreAreaForPlayer(uint oArea, uint oPlayer, bool bExplored = true) { }
        public void SetTransitionTarget(uint oTransition, uint oTarget) { }
        public void SetWeather(uint oTarget, AreaWeatherType nWeather) => GetOrCreateAreaData(oTarget).Weather = (WeatherType)nWeather;

        public int SetTileExplored(uint creature, uint area, int x, int y, bool newState) 
        {
            if (!_exploredTiles.ContainsKey(creature))
                _exploredTiles[creature] = new Dictionary<uint, bool>();
            
            var key = (uint)((x << 16) | y);
            _exploredTiles[creature][key] = newState;
            return 1; // Success
        }

        public int GetTileExplored(uint creature, uint area, int x, int y) 
        {
            if (!_exploredTiles.ContainsKey(creature))
                return 0;
            
            var key = (uint)((x << 16) | y);
            return _exploredTiles[creature].GetValueOrDefault(key, false) ? 1 : 0;
        }

        public int SetCreatureExploresMinimap(uint creature, bool newState) 
        {
            _creatureExploresMinimap[creature] = newState;
            return 1; // Success
        }

        public int GetCreatureExploresMinimap(uint creature) => _creatureExploresMinimap.GetValueOrDefault(creature, false) ? 1 : 0;

        public int GetSurfaceMaterial(Location at) => 0;
        public float GetGroundHeight(Location at) => 0.0f;
        public bool GetIsInSubArea(uint oCreature, uint oSubArea = OBJECT_INVALID) => false;

        public void SetTileMainLightColor(Location lTileLocation, int nMainLight1Color, int nMainLight2Color) 
        {
            if (!_tileData.ContainsKey(OBJECT_INVALID))
                _tileData[OBJECT_INVALID] = new Dictionary<Location, TileData>();
            if (!_tileData[OBJECT_INVALID].ContainsKey(lTileLocation))
                _tileData[OBJECT_INVALID][lTileLocation] = new TileData();
            
            _tileData[OBJECT_INVALID][lTileLocation].MainLight1Color = nMainLight1Color;
            _tileData[OBJECT_INVALID][lTileLocation].MainLight2Color = nMainLight2Color;
        }

        public void SetTileSourceLightColor(Location lTileLocation, int nSourceLight1Color, int nSourceLight2Color) 
        {
            if (!_tileData.ContainsKey(OBJECT_INVALID))
                _tileData[OBJECT_INVALID] = new Dictionary<Location, TileData>();
            if (!_tileData[OBJECT_INVALID].ContainsKey(lTileLocation))
                _tileData[OBJECT_INVALID][lTileLocation] = new TileData();
            
            _tileData[OBJECT_INVALID][lTileLocation].SourceLight1Color = nSourceLight1Color;
            _tileData[OBJECT_INVALID][lTileLocation].SourceLight2Color = nSourceLight2Color;
        }

        public int GetTileMainLight1Color(Location lTile) => 
            _tileData.GetValueOrDefault(OBJECT_INVALID, new Dictionary<Location, TileData>())
                .GetValueOrDefault(lTile, new TileData()).MainLight1Color;

        public int GetTileMainLight2Color(Location lTile) => 
            _tileData.GetValueOrDefault(OBJECT_INVALID, new Dictionary<Location, TileData>())
                .GetValueOrDefault(lTile, new TileData()).MainLight2Color;

        public int GetTileSourceLight1Color(Location lTile) => 
            _tileData.GetValueOrDefault(OBJECT_INVALID, new Dictionary<Location, TileData>())
                .GetValueOrDefault(lTile, new TileData()).SourceLight1Color;

        public int GetTileSourceLight2Color(Location lTile) => 
            _tileData.GetValueOrDefault(OBJECT_INVALID, new Dictionary<Location, TileData>())
                .GetValueOrDefault(lTile, new TileData()).SourceLight2Color;

        public void SetMapPinEnabled(uint oMapPin, bool bEnabled = true) { }

        public uint GetAreaFromLocation(Location lLocation) => 
            _locationData.GetValueOrDefault(lLocation, new LocationData()).Area;

        public Vector3 GetPositionFromLocation(Location lLocation) => 
            _locationData.GetValueOrDefault(lLocation, new LocationData()).Position;

        public void SetAreaTransitionBMP(AreaTransitionType nPredefinedAreaTransition, string sCustomAreaTransitionBMP = "") { }

        public void SetAreaWind(uint oArea, Vector3 vDirection, float fMagnitude, float fYaw, float fPitch) 
        {
            var area = GetOrCreateAreaData(oArea);
            area.WindDirection = vDirection;
            area.WindMagnitude = fMagnitude;
            area.WindYaw = fYaw;
            area.WindPitch = fPitch;
        }

        public int GetAreaLightColor(AreaLightColorType nColorType, uint oArea = OBJECT_INVALID) => 
            GetOrCreateAreaData(oArea).LightColors.GetValueOrDefault(nColorType, 0);

        public void SetAreaLightColor(AreaLightColorType nColorType, int nColorValue, uint oArea = OBJECT_INVALID) => 
            GetOrCreateAreaData(oArea).LightColors[nColorType] = nColorValue;

        public Vector3 GetAreaLightDirection(AreaLightDirectionType nLightType, uint oArea = OBJECT_INVALID) => 
            GetOrCreateAreaData(oArea).LightDirections.GetValueOrDefault(nLightType, new Vector3(0, 0, 0));

        public void SetAreaLightDirection(AreaLightDirectionType nLightType, Vector3 vDirection, uint oArea = OBJECT_INVALID) => 
            GetOrCreateAreaData(oArea).LightDirections[nLightType] = vDirection;

        public uint GetFirstInPersistentObject(uint oPersistentObject = OBJECT_INVALID,
            ObjectType nResidentObjectType = ObjectType.Creature,
            PersistentZoneType nPersistentZone = PersistentZoneType.Active) 
        {
            _currentPersistentObjectIterator = OBJECT_INVALID;
            return _currentPersistentObjectIterator;
        }

        public uint GetNextInPersistentObject(uint oPersistentObject = OBJECT_INVALID,
            ObjectType nResidentObjectType = ObjectType.Creature,
            PersistentZoneType nPersistentZone = PersistentZoneType.Active) 
        {
            _currentPersistentObjectIterator = OBJECT_INVALID;
            return _currentPersistentObjectIterator;
        }

        public uint GetAreaOfEffectCreator(uint oAreaOfEffectObject = OBJECT_INVALID) => OBJECT_INVALID;

        public float GetDistanceBetweenLocations(Location lLocationA, Location lLocationB) 
        {
            var posA = GetPositionFromLocation(lLocationA);
            var posB = GetPositionFromLocation(lLocationB);
            return System.Numerics.Vector3.Distance(posA, posB);
        }

        public void SetTile(Location lTileLocation, int nTileID, int nOrientation, int nHeightVariation) 
        {
            if (!_tileData.ContainsKey(OBJECT_INVALID))
                _tileData[OBJECT_INVALID] = new Dictionary<Location, TileData>();
            if (!_tileData[OBJECT_INVALID].ContainsKey(lTileLocation))
                _tileData[OBJECT_INVALID][lTileLocation] = new TileData();
            
            _tileData[OBJECT_INVALID][lTileLocation].TileID = nTileID;
            _tileData[OBJECT_INVALID][lTileLocation].Orientation = nOrientation;
            _tileData[OBJECT_INVALID][lTileLocation].Height = nHeightVariation;
        }

        public int GetTileID(Location locTile) => 
            _tileData.GetValueOrDefault(OBJECT_INVALID, new Dictionary<Location, TileData>())
                .GetValueOrDefault(locTile, new TileData()).TileID;

        public int GetTileOrientation(Location locTile) => 
            _tileData.GetValueOrDefault(OBJECT_INVALID, new Dictionary<Location, TileData>())
                .GetValueOrDefault(locTile, new TileData()).Orientation;

        public int GetTileHeight(Location locTile) => 
            _tileData.GetValueOrDefault(OBJECT_INVALID, new Dictionary<Location, TileData>())
                .GetValueOrDefault(locTile, new TileData()).Height;

        public void ReloadAreaGrass(uint oArea) { }

        public void SetTileAnimationLoops(Location locTile, bool bAnimLoop1, bool bAnimLoop2, bool bAnimLoop3) 
        {
            if (!_tileData.ContainsKey(OBJECT_INVALID))
                _tileData[OBJECT_INVALID] = new Dictionary<Location, TileData>();
            if (!_tileData[OBJECT_INVALID].ContainsKey(locTile))
                _tileData[OBJECT_INVALID][locTile] = new TileData();
            
            _tileData[OBJECT_INVALID][locTile].AnimLoop1 = bAnimLoop1;
            _tileData[OBJECT_INVALID][locTile].AnimLoop2 = bAnimLoop2;
            _tileData[OBJECT_INVALID][locTile].AnimLoop3 = bAnimLoop3;
        }

        public void SetTileJson(Location lTileLocation, string sJson, int nTileID, int nOrientation, int nHeightVariation) 
        {
            if (!_tileData.ContainsKey(OBJECT_INVALID))
                _tileData[OBJECT_INVALID] = new Dictionary<Location, TileData>();
            if (!_tileData[OBJECT_INVALID].ContainsKey(lTileLocation))
                _tileData[OBJECT_INVALID][lTileLocation] = new TileData();
            
            _tileData[OBJECT_INVALID][lTileLocation].Json = sJson;
            _tileData[OBJECT_INVALID][lTileLocation].TileID = nTileID;
            _tileData[OBJECT_INVALID][lTileLocation].Orientation = nOrientation;
            _tileData[OBJECT_INVALID][lTileLocation].Height = nHeightVariation;
        }

        public void ReloadAreaBorder(uint oArea) { }

        // Additional area methods from INWScriptService
        public Location Location(uint oArea, Vector3 vPosition, float fOrientation) => CreateLocation(oArea, vPosition, fOrientation);
        
        public void SetAreaLightColor(AreaLightColorType nColorType, FogColorType nColor, uint oArea = OBJECT_INVALID,
            float fFadeTime = 0) 
        {
            var area = GetOrCreateAreaData(oArea);
            area.LightColors[nColorType] = (int)nColor;
        }

        public void SetAreaLightDirection(AreaLightDirectionType nLightType, Vector3 vDirection,
            uint oArea = OBJECT_INVALID, float fFadeTime = 0) 
        {
            var area = GetOrCreateAreaData(oArea);
            area.LightDirections[nLightType] = vDirection;
        }

        public void SetTile(Location locTile, int nTileID, int nOrientation, int nHeight = 0,
            SetTileFlagType nFlags = SetTileFlagType.RecomputeLighting) 
        {
            SetTile(locTile, nTileID, nOrientation, nHeight);
        }

        public void SetTileJson(uint oArea, Json jTileData, SetTileFlagType nFlags = SetTileFlagType.RecomputeLighting,
            string sTileset = "") 
        {
            // Mock implementation - would need to parse JSON data
        }

        // Helper methods for testing
        public Dictionary<uint, AreaData> GetAreaData() => new Dictionary<uint, AreaData>(_areaData);
        public void ClearAreaData() => _areaData.Clear();
        public void SetObjectArea(uint oObject, uint oArea) => _objectAreas[oObject] = oArea;
        public void SetObjectPosition(uint oObject, Vector3 position) => _objectPositions[oObject] = position;
        public void AddObjectToArea(uint oArea, uint oObject) 
        {
            if (!_areaObjects.ContainsKey(oArea))
                _areaObjects[oArea] = new List<uint>();
            _areaObjects[oArea].Add(oObject);
        }
    }
}
