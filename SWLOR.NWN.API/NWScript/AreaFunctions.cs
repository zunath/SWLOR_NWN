using System.Numerics;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Area;
using ObjectType = SWLOR.NWN.API.NWScript.Enum.ObjectType;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Gets the area that the target is currently in.
        /// </summary>
        /// <param name="oTarget">The target to get the area for</param>
        /// <returns>The area object. Returns OBJECT_INVALID on error</returns>
        public static uint GetArea(uint oTarget)
        {
            return global::NWN.Core.NWScript.GetArea(oTarget);
        }

        /// <summary>
        /// Gets the object that last entered the caller.
        /// </summary>
        /// <returns>The entering object. Returns OBJECT_INVALID on error</returns>
        /// <remarks>The value returned depends on the object type of the caller: 1) If the caller is a door, it returns the object that last triggered it. 2) If the caller is a trigger, area of effect, module, area or encounter, it returns the object that last entered it. When used for doors, this should only be called from the OnAreaTransitionClick event. Otherwise, it should only be called in OnEnter scripts.</remarks>
        public static uint GetEnteringObject()
        {
            return global::NWN.Core.NWScript.GetEnteringObject();
        }

        /// <summary>
        /// Gets the object that last left the caller.
        /// </summary>
        /// <returns>The exiting object. Returns OBJECT_INVALID on error</returns>
        /// <remarks>This function works on triggers, areas of effect, modules, areas and encounters. Should only be called in OnExit scripts.</remarks>
        public static uint GetExitingObject()
        {
            return global::NWN.Core.NWScript.GetExitingObject();
        }

        /// <summary>
        /// Gets the position of the target.
        /// </summary>
        /// <param name="oTarget">The target to get the position for</param>
        /// <returns>The position vector. Returns (0.0f, 0.0f, 0.0f) on error</returns>
        public static Vector3 GetPosition(uint oTarget)
        {
            return global::NWN.Core.NWScript.GetPosition(oTarget);
        }

        /// <summary>
        /// Plays the background music for the specified area.
        /// </summary>
        /// <param name="oArea">The area to play background music for</param>
        public static void MusicBackgroundPlay(uint oArea)
        {
            global::NWN.Core.NWScript.MusicBackgroundPlay(oArea);
        }

        /// <summary>
        /// Stops the background music for the specified area.
        /// </summary>
        /// <param name="oArea">The area to stop background music for</param>
        public static void MusicBackgroundStop(uint oArea)
        {
            global::NWN.Core.NWScript.MusicBackgroundStop(oArea);
        }

        /// <summary>
        /// Sets the delay for the background music for the specified area.
        /// </summary>
        /// <param name="oArea">The area to set the music delay for</param>
        /// <param name="nDelay">The delay in milliseconds</param>
        public static void MusicBackgroundSetDelay(uint oArea, int nDelay)
        {
            global::NWN.Core.NWScript.MusicBackgroundSetDelay(oArea, nDelay);
        }

        /// <summary>
        /// Changes the background day track for the specified area.
        /// </summary>
        /// <param name="oArea">The area to change the day track for</param>
        /// <param name="nTrack">The track number to set</param>
        public static void MusicBackgroundChangeDay(uint oArea, int nTrack)
        {
            global::NWN.Core.NWScript.MusicBackgroundChangeDay(oArea, nTrack);
        }

        /// <summary>
        /// Changes the background night track for the specified area.
        /// </summary>
        /// <param name="oArea">The area to change the night track for</param>
        /// <param name="nTrack">The track number to set</param>
        public static void MusicBackgroundChangeNight(uint oArea, int nTrack)
        {
            global::NWN.Core.NWScript.MusicBackgroundChangeNight(oArea, nTrack);
        }

        /// <summary>
        /// Plays the battle music for the specified area.
        /// </summary>
        /// <param name="oArea">The area to play battle music for</param>
        public static void MusicBattlePlay(uint oArea)
        {
            global::NWN.Core.NWScript.MusicBattlePlay(oArea);
        }

        /// <summary>
        /// Stops the battle music for the specified area.
        /// </summary>
        /// <param name="oArea">The area to stop battle music for</param>
        public static void MusicBattleStop(uint oArea)
        {
            global::NWN.Core.NWScript.MusicBattleStop(oArea);
        }

        /// <summary>
        /// Changes the battle track for the specified area.
        /// </summary>
        /// <param name="oArea">The area to change the battle track for</param>
        /// <param name="nTrack">The track number to set</param>
        public static void MusicBattleChange(uint oArea, int nTrack)
        {
            global::NWN.Core.NWScript.MusicBattleChange(oArea, nTrack);
        }

        /// <summary>
        /// Plays the ambient sound for the specified area.
        /// </summary>
        /// <param name="oArea">The area to play ambient sound for</param>
        public static void AmbientSoundPlay(uint oArea)
        {
            global::NWN.Core.NWScript.AmbientSoundPlay(oArea);
        }

        /// <summary>
        /// Stops the ambient sound for the specified area.
        /// </summary>
        /// <param name="oArea">The area to stop ambient sound for</param>
        public static void AmbientSoundStop(uint oArea)
        {
            global::NWN.Core.NWScript.AmbientSoundStop(oArea);
        }

        /// <summary>
        /// Changes the ambient day track for the specified area.
        /// </summary>
        /// <param name="oArea">The area to change the ambient day track for</param>
        /// <param name="nTrack">The track number to set</param>
        public static void AmbientSoundChangeDay(uint oArea, int nTrack)
        {
            global::NWN.Core.NWScript.AmbientSoundChangeDay(oArea, nTrack);
        }

        /// <summary>
        /// Changes the ambient night track for the specified area.
        /// </summary>
        /// <param name="oArea">The area to change the ambient night track for</param>
        /// <param name="nTrack">The track number to set</param>
        public static void AmbientSoundChangeNight(uint oArea, int nTrack)
        {
            global::NWN.Core.NWScript.AmbientSoundChangeNight(oArea, nTrack);
        }

        /// <summary>
        /// Makes all clients in the area recompute the static lighting.
        /// </summary>
        /// <param name="oArea">The area to recompute lighting for</param>
        /// <remarks>This can be used to update the lighting after changing any tile lights or if placeables with lights have been added/deleted.</remarks>
        public static void RecomputeStaticLighting(uint oArea)
        {
            global::NWN.Core.NWScript.RecomputeStaticLighting(oArea);
        }

        /// <summary>
        /// Gets the day track for the specified area.
        /// </summary>
        /// <param name="oArea">The area to get the day track for</param>
        /// <returns>The day track number</returns>
        public static int MusicBackgroundGetDayTrack(uint oArea)
        {
            return global::NWN.Core.NWScript.MusicBackgroundGetDayTrack(oArea);
        }

        /// <summary>
        /// Gets the night track for the specified area.
        /// </summary>
        /// <param name="oArea">The area to get the night track for</param>
        /// <returns>The night track number</returns>
        public static int MusicBackgroundGetNightTrack(uint oArea)
        {
            return global::NWN.Core.NWScript.MusicBackgroundGetNightTrack(oArea);
        }

        /// <summary>
        /// Sets the ambient day volume for the specified area.
        /// </summary>
        /// <param name="oArea">The area to set the ambient day volume for</param>
        /// <param name="nVolume">The volume level (0-100)</param>
        public static void AmbientSoundSetDayVolume(uint oArea, int nVolume)
        {
            global::NWN.Core.NWScript.AmbientSoundSetDayVolume(oArea, nVolume);
        }

        /// <summary>
        /// Sets the ambient night volume for the specified area.
        /// </summary>
        /// <param name="oArea">The area to set the ambient night volume for</param>
        /// <param name="nVolume">The volume level (0-100)</param>
        public static void AmbientSoundSetNightVolume(uint oArea, int nVolume)
        {
            global::NWN.Core.NWScript.AmbientSoundSetNightVolume(oArea, nVolume);
        }

        /// <summary>
        /// Gets the battle track for the specified area.
        /// </summary>
        /// <param name="oArea">The area to get the battle track for</param>
        /// <returns>The battle track number</returns>
        public static int MusicBackgroundGetBattleTrack(uint oArea)
        {
            return global::NWN.Core.NWScript.MusicBackgroundGetBattleTrack(oArea);
        }

        /// <summary>
        /// Returns true if the area is flagged as either interior or underground.
        /// </summary>
        /// <param name="oArea">The area to check (default: OBJECT_INVALID)</param>
        /// <returns>True if the area is interior or underground</returns>
        public static bool GetIsAreaInterior(uint oArea = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetIsAreaInterior(oArea) != 0;
        }

        /// <summary>
        /// Gets the current weather conditions for the specified area.
        /// </summary>
        /// <param name="oArea">The area to get weather for</param>
        /// <returns>Weather conditions: WEATHER_CLEAR, WEATHER_RAIN, WEATHER_SNOW, or WEATHER_INVALID</returns>
        /// <remarks>If called on an interior area, this will always return WEATHER_CLEAR.</remarks>
        public static Weather GetWeather(uint oArea)
        {
            return (Weather)global::NWN.Core.NWScript.GetWeather(oArea);
        }

        /// <summary>
        /// Returns whether the area is natural or artificial.
        /// </summary>
        /// <param name="oArea">The area to check</param>
        /// <returns>AREA_NATURAL if the area is natural, AREA_ARTIFICIAL otherwise. Returns AREA_INVALID on error</returns>
        public static Natural GetIsAreaNatural(uint oArea)
        {
            return (Natural)global::NWN.Core.NWScript.GetIsAreaNatural(oArea);
        }

        /// <summary>
        /// Returns whether the area is above ground or underground.
        /// </summary>
        /// <param name="oArea">The area to check</param>
        /// <returns>True if the area is above ground, false if underground</returns>
        public static bool GetIsAreaAboveGround(uint oArea)
        {
            return global::NWN.Core.NWScript.GetIsAreaAboveGround(oArea) != 0;
        }

        /// <summary>
        /// Changes the sky that is displayed in the specified area.
        /// </summary>
        /// <param name="nSkyBox">The skybox to set (SKYBOX_* constants associated with skyboxes.2da)</param>
        /// <param name="oArea">The area to change the sky for. If no valid area is specified, uses the area of caller. If an object other than an area is specified, uses the area that the object is currently in (default: OBJECT_INVALID)</param>
        public static void SetSkyBox(Skybox nSkyBox, uint oArea = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.SetSkyBox((int)nSkyBox, oArea);
        }

        /// <summary>
        /// Sets the fog color in the specified area.
        /// </summary>
        /// <param name="nFogType">Specifies whether the Sun, Moon, or both fog types are set (FOG_TYPE_* constants)</param>
        /// <param name="nFogColor">The color to set the fog to (FOG_COLOR_* constants). Can also be represented as a hex RGB number (e.g., 0xFFEEDD where FF=red, EE=green, DD=blue)</param>
        /// <param name="oArea">The area to set fog color for. If no valid area is specified, uses the area of caller. If an object other than an area is specified, uses the area that the object is currently in (default: OBJECT_INVALID)</param>
        public static void SetFogColor(FogType nFogType, FogColor nFogColor, uint oArea = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.SetFogColor((int)nFogType, (int)nFogColor, oArea);
        }

        /// <summary>
        /// Gets the skybox that is currently displayed in the specified area.
        /// </summary>
        /// <param name="oArea">The area to get the skybox for. If no valid area is specified, uses the area of caller. If an object other than an area is specified, uses the area that the object is currently in (default: OBJECT_INVALID)</param>
        /// <returns>The skybox constant (SKYBOX_*)</returns>
        public static Skybox GetSkyBox(uint oArea = OBJECT_INVALID)
        {
            return (Skybox)global::NWN.Core.NWScript.GetSkyBox(oArea);
        }

        /// <summary>
        /// Gets the fog color in the specified area.
        /// </summary>
        /// <param name="nFogType">Specifies whether the Sun or Moon fog type is returned. Valid values are FOG_TYPE_SUN or FOG_TYPE_MOON</param>
        /// <param name="oArea">The area to get fog color for. If no valid area is specified, uses the area of caller. If an object other than an area is specified, uses the area that the object is currently in (default: OBJECT_INVALID)</param>
        /// <returns>The fog color constant (FOG_COLOR_*)</returns>
        public static FogColor GetFogColor(FogType nFogType, uint oArea = OBJECT_INVALID)
        {
            return (FogColor)global::NWN.Core.NWScript.GetFogColor((int)nFogType, oArea);
        }

        /// <summary>
        /// Sets the fog amount in the specified area.
        /// </summary>
        /// <param name="nFogType">Specifies whether the Sun, Moon, or both fog types are set (FOG_TYPE_* constants)</param>
        /// <param name="nFogAmount">The density that the fog is being set to</param>
        /// <param name="oArea">The area to set fog amount for. If no valid area is specified, uses the area of caller. If an object other than an area is specified, uses the area that the object is currently in (default: OBJECT_INVALID)</param>
        public static void SetFogAmount(FogType nFogType, int nFogAmount, uint oArea = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.SetFogAmount((int)nFogType, nFogAmount, oArea);
        }

        /// <summary>
        /// Gets the fog amount in the specified area.
        /// </summary>
        /// <param name="nFogType">Specifies whether the Sun or Moon fog type is returned. Valid values are FOG_TYPE_SUN or FOG_TYPE_MOON</param>
        /// <param name="oArea">The area to get fog amount for. If no valid area is specified, uses the area of caller. If an object other than an area is specified, uses the area that the object is currently in (default: OBJECT_INVALID)</param>
        /// <returns>The fog amount</returns>
        public static int GetFogAmount(FogType nFogType, uint oArea = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetFogAmount((int)nFogType, oArea);
        }

        /// <summary>
        /// Returns the resref of the tileset used to create the specified area.
        /// </summary>
        /// <param name="oArea">The area to get the tileset resref for</param>
        /// <returns>The tileset resref (TILESET_RESREF_* constant). Returns empty string on error</returns>
        /// <remarks>Possible values include: TILESET_RESREF_BEHOLDER_CAVES, TILESET_RESREF_CASTLE_INTERIOR, TILESET_RESREF_CITY_EXTERIOR, TILESET_RESREF_CITY_INTERIOR, TILESET_RESREF_CRYPT, TILESET_RESREF_DESERT, TILESET_RESREF_DROW_INTERIOR, TILESET_RESREF_DUNGEON, TILESET_RESREF_FOREST, TILESET_RESREF_FROZEN_WASTES, TILESET_RESREF_ILLITHID_INTERIOR, TILESET_RESREF_MICROSET, TILESET_RESREF_MINES_AND_CAVERNS, TILESET_RESREF_RUINS, TILESET_RESREF_RURAL, TILESET_RESREF_RURAL_WINTER, TILESET_RESREF_SEWERS, TILESET_RESREF_UNDERDARK</remarks>
        public static string GetTilesetResRef(uint oArea)
        {
            return global::NWN.Core.NWScript.GetTilesetResRef(oArea);
        }

        /// <summary>
        /// Gets the size of the specified area.
        /// </summary>
        /// <param name="nAreaDimension">The area dimension to determine (AREA_HEIGHT or AREA_WIDTH)</param>
        /// <param name="oArea">The area to get the size for. If no valid area is specified, uses the area of the caller. If an object other than an area is specified, uses the area that the object is currently in (default: OBJECT_INVALID)</param>
        /// <returns>The number of tiles that the area is wide/high, or zero on error</returns>
        public static int GetAreaSize(Dimension nAreaDimension, uint oArea = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetAreaSize((int)nAreaDimension, oArea);
        }

        /// <summary>
        /// Destroys the given area object and everything in it.
        /// </summary>
        /// <param name="oArea">The area to destroy</param>
        /// <returns>Return values: 0 = Object not an area or invalid, -1 = Area contains spawn location and removal would leave module without entrypoint, -2 = Players in area, 1 = Area destroyed successfully</returns>
        /// <remarks>If the area is in a module, the .are and .git data is left behind and you can spawn from it again. If the area is a temporary copy, the data will be deleted and you cannot spawn it again via the resref.</remarks>
        public static int DestroyArea(uint oArea)
        {
            return global::NWN.Core.NWScript.DestroyArea(oArea);
        }


        /// <summary>
        /// Instances a new area from the given source resref, which needs to be an existing module area.
        /// </summary>
        /// <param name="sSourceResRef">The source resref of the area to instance</param>
        /// <param name="sNewTag">Optional new area tag (default: empty string)</param>
        /// <param name="sNewName">Optional new displayed name (default: empty string)</param>
        /// <returns>The new area, or OBJECT_INVALID on failure</returns>
        /// <remarks>The new area is accessible immediately, but initialization scripts for the area and all contained creatures will only run after the current script finishes (so you can clean up objects before returning). When spawning a second instance of an existing area, you will have to manually adjust all transitions (doors, triggers) with the relevant script commands, or players might end up in the wrong area. Areas cannot have duplicate ResRefs, so your new area will have an autogenerated, sequential resref starting with "nw_"; for example: nw_5. You cannot influence this resref. If you destroy an area, that resref will become free for reuse for the next area created. If you need to know the resref of your new area, you can call GetResRef on it. When instancing an area from a loaded savegame, it will spawn the area as it was at time of save, NOT at module creation. This is because the savegame replaces the module data. Due to technical limitations, polymorphed creatures, personal reputation, and associates will currently fail to restore correctly.</remarks>
        public static uint CreateArea(string sSourceResRef, string sNewTag = "", string sNewName = "")
        {
            return global::NWN.Core.NWScript.CreateArea(sSourceResRef, sNewTag, sNewName);
        }

        /// <summary>
        /// Creates a copy of an existing area, including everything inside of it (except players).
        /// </summary>
        /// <param name="oArea">The area to copy</param>
        /// <returns>The new area, or OBJECT_INVALID on error</returns>
        /// <remarks>This is similar to CreateArea, except this variant will copy all changes made to the source area since it has spawned. CreateArea() will instance the area from the .are and .git data as it was at creation. The new area is accessible immediately, but initialization scripts for the area and all contained creatures will only run after the current script finishes (so you can clean up objects before returning). You will have to manually adjust all transitions (doors, triggers) with the relevant script commands, or players might end up in the wrong area. Areas cannot have duplicate ResRefs, so your new area will have an autogenerated, sequential resref starting with "nw_"; for example: nw_5. You cannot influence this resref. If you destroy an area, that resref will become free for reuse for the next area created. If you need to know the resref of your new area, you can call GetResRef on it.</remarks>
        public static uint CopyArea(uint oArea)
        {
            return global::NWN.Core.NWScript.CopyArea(oArea);
        }

        /// <summary>
        /// Returns the first area in the module.
        /// </summary>
        /// <returns>The first area in the module</returns>
        public static uint GetFirstArea()
        {
            return global::NWN.Core.NWScript.GetFirstArea();
        }

        /// <summary>
        /// Returns the next area in the module (after GetFirstArea).
        /// </summary>
        /// <returns>The next area in the module, or OBJECT_INVALID if no more areas are loaded</returns>
        public static uint GetNextArea()
        {
            return global::NWN.Core.NWScript.GetNextArea();
        }

        /// <summary>
        /// Gets the first object in the specified area.
        /// </summary>
        /// <param name="oArea">The area to get the first object from. If no valid area is specified, uses the caller's area (default: OBJECT_INVALID)</param>
        /// <param name="nObjectFilter">Allows filtering out undesired object types using bitwise "or". For example, to return only creatures and doors, use OBJECT_TYPE_CREATURE | OBJECT_TYPE_DOOR (default: ObjectType.All)</param>
        /// <returns>The first object in the area. Returns OBJECT_INVALID on error</returns>
        public static uint GetFirstObjectInArea(uint oArea = OBJECT_INVALID, ObjectType nObjectFilter = ObjectType.All)
        {
            return global::NWN.Core.NWScript.GetFirstObjectInArea(oArea, (int)nObjectFilter);
        }

        /// <summary>
        /// Gets the next object in the specified area.
        /// </summary>
        /// <param name="oArea">The area to get the next object from. If no valid area is specified, uses the caller's area (default: OBJECT_INVALID)</param>
        /// <param name="nObjectFilter">Allows filtering out undesired object types using bitwise "or". For example, to return only creatures and doors, use OBJECT_TYPE_CREATURE | OBJECT_TYPE_DOOR (default: ObjectType.All)</param>
        /// <returns>The next object in the area. Returns OBJECT_INVALID on error</returns>
        public static uint GetNextObjectInArea(uint oArea = OBJECT_INVALID, ObjectType nObjectFilter = ObjectType.All)
        {
            return global::NWN.Core.NWScript.GetNextObjectInArea(oArea, (int)nObjectFilter);
        }

        /// <summary>
        /// Gets the location of the specified object.
        /// </summary>
        /// <param name="oObject">The object to get the location for</param>
        /// <returns>The location of the object</returns>
        public static Location GetLocation(uint oObject)
        {
            return global::NWN.Core.NWScript.GetLocation(oObject);
        }

        /// <summary>
        /// Makes the subject jump to the specified location instantly (even between areas).
        /// </summary>
        /// <param name="lLocation">The location to jump to. If invalid, nothing will happen</param>
        public static void ActionJumpToLocation(Location lLocation)
        {
            global::NWN.Core.NWScript.ActionJumpToLocation(lLocation);
        }

        /// <summary>
        /// Creates a location object.
        /// </summary>
        /// <param name="oArea">The area for the location</param>
        /// <param name="vPosition">The position vector</param>
        /// <param name="fOrientation">The orientation angle</param>
        /// <returns>The created location</returns>
        public static Location Location(uint oArea, Vector3 vPosition, float fOrientation)
        {
            return global::NWN.Core.NWScript.Location(oArea, vPosition, fOrientation);
        }

        /// <summary>
        /// Applies the specified effect at the given location.
        /// </summary>
        /// <param name="nDurationType">The duration type for the effect</param>
        /// <param name="eEffect">The effect to apply</param>
        /// <param name="lLocation">The location to apply the effect at</param>
        /// <param name="fDuration">The duration of the effect (default: 0.0)</param>
        public static void ApplyEffectAtLocation(DurationType nDurationType, Effect eEffect, Location lLocation,
            float fDuration = 0.0f)
        {
            global::NWN.Core.NWScript.ApplyEffectAtLocation((int)nDurationType, eEffect, lLocation, fDuration);
        }

        /// <summary>
        /// Exposes or hides the entire map of the specified area for the player.
        /// </summary>
        /// <param name="oArea">The area that the map will be exposed/hidden for</param>
        /// <param name="oPlayer">The player the map will be exposed/hidden for</param>
        /// <param name="bExplored">Whether the map should be completely explored or hidden (default: true)</param>
        public static void ExploreAreaForPlayer(uint oArea, uint oPlayer, bool bExplored = true)
        {
            global::NWN.Core.NWScript.ExploreAreaForPlayer(oArea, oPlayer, bExplored ? 1 : 0);
        }

        /// <summary>
        /// Sets the transition target for the specified transition.
        /// </summary>
        /// <param name="oTransition">The transition object (can be any valid game object, except areas)</param>
        /// <param name="oTarget">The target object (can be any valid game object with a location, or OBJECT_INVALID to unlink)</param>
        /// <remarks>Rebinding a transition will NOT change the other end of the transition; for example, with normal doors you will have to do either end separately. Any valid game object can hold a transition target, but only some are used by the game engine (doors and triggers). This might change in the future. You can still set and query them for other game objects from nwscript. Transition target objects are cached: The toolset-configured destination tag is used for a lookup only once, at first use. Thus, attempting to use SetTag() to change the destination for a transition will not work in a predictable fashion.</remarks>
        public static void SetTransitionTarget(uint oTransition, uint oTarget)
        {
            global::NWN.Core.NWScript.SetTransitionTarget(oTransition, oTarget);
        }

        /// <summary>
        /// Sets the weather for the specified target.
        /// </summary>
        /// <param name="oTarget">If this is GetModule(), all outdoor areas will be modified by the weather constant. If it is an area, the target will play the weather only if it is an outdoor area</param>
        /// <param name="nWeather">The weather type (WEATHER_* constant). WEATHER_USER_AREA_SETTINGS will set the area back to random weather. WEATHER_CLEAR, WEATHER_RAIN, WEATHER_SNOW will make the weather go to the appropriate precipitation without stopping</param>
        public static void SetWeather(uint oTarget, WeatherType nWeather)
        {
            global::NWN.Core.NWScript.SetWeather(oTarget, (int)nWeather);
        }

        /// <summary>
        /// Sets whether the given creature has explored the tile at the specified coordinates.
        /// </summary>
        /// <param name="creature">The creature (must be a player- or player-possessed creature)</param>
        /// <param name="area">The area containing the tile</param>
        /// <param name="x">The x coordinate of the tile</param>
        /// <param name="y">The y coordinate of the tile</param>
        /// <param name="newState">Whether the tile should be explored or not</param>
        /// <returns>Return values: -1 = Area or creature invalid, 0 = Tile was not explored before setting newState, 1 = Tile was explored before setting newState</returns>
        /// <remarks>Keep in mind that tile exploration also controls object visibility in areas and the fog of war for interior and underground areas.</remarks>
        public static int SetTileExplored(uint creature, uint area, int x, int y, bool newState)
        {
            return global::NWN.Core.NWScript.SetTileExplored(creature, area, x, y, newState ? 1 : 0);
        }

        /// <summary>
        /// Returns whether the given tile at the specified coordinates is visible on the map for the creature.
        /// </summary>
        /// <param name="creature">The creature (must be a player- or player-possessed creature)</param>
        /// <param name="area">The area containing the tile</param>
        /// <param name="x">The x coordinate of the tile</param>
        /// <param name="y">The y coordinate of the tile</param>
        /// <returns>Return values: -1 = Area or creature invalid, 0 = Tile is not explored yet, 1 = Tile is explored</returns>
        /// <remarks>Keep in mind that tile exploration also controls object visibility in areas and the fog of war for interior and underground areas.</remarks>
        public static int GetTileExplored(uint creature, uint area, int x, int y)
        {
            return global::NWN.Core.NWScript.GetTileExplored(creature, area, x, y);
        }

        /// <summary>
        /// Sets whether the creature auto-explores the map as it walks around.
        /// </summary>
        /// <param name="creature">The creature to set auto-exploration for</param>
        /// <param name="newState">Whether the creature should auto-explore (true/false)</param>
        /// <returns>The previous state (or -1 if non-creature). Does nothing for non-creatures</returns>
        /// <remarks>Keep in mind that tile exploration also controls object visibility in areas and the fog of war for interior and underground areas. This means that if you turn off auto exploration, it falls to you to manage this through SetTileExplored(); otherwise, the player will not be able to see anything.</remarks>
        public static int SetCreatureExploresMinimap(uint creature, bool newState)
        {
            return global::NWN.Core.NWScript.SetCreatureExploresMinimap(creature, newState ? 1 : 0);
        }

        /// <summary>
        /// Returns whether the creature is set to auto-explore the map as it walks around.
        /// </summary>
        /// <param name="creature">The creature to check</param>
        /// <returns>True if the creature is set to auto-explore (on by default), false if creature is not actually a creature</returns>
        public static int GetCreatureExploresMinimap(uint creature)
        {
            return global::NWN.Core.NWScript.GetCreatureExploresMinimap(creature);
        }

        /// <summary>
        /// Gets the surface material at the given location (equivalent to the walkmesh type).
        /// </summary>
        /// <param name="at">The location to get the surface material for</param>
        /// <returns>The surface material. Returns 0 if the location is invalid or has no surface type</returns>
        public static int GetSurfaceMaterial(Location at)
        {
            return global::NWN.Core.NWScript.GetSurfaceMaterial(at);
        }

        /// <summary>
        /// Returns the z-offset at which the walkmesh is at the given location.
        /// </summary>
        /// <param name="at">The location to get the ground height for</param>
        /// <returns>The z-offset of the walkmesh. Returns -6.0 for invalid locations</returns>
        public static float GetGroundHeight(Location at)
        {
            return global::NWN.Core.NWScript.GetGroundHeight(at);
        }

        /// <summary>
        /// Returns whether the creature is in the given subarea (trigger, area of effect object, etc.).
        /// </summary>
        /// <param name="oCreature">The creature to check</param>
        /// <param name="oSubArea">The subarea to check (default: OBJECT_INVALID)</param>
        /// <returns>True if the creature is in the subarea, false otherwise</returns>
        /// <remarks>This function will tell you if the creature has triggered an onEnter event, not if it is physically within the space of the subarea</remarks>
        public static bool GetIsInSubArea(uint oCreature, uint oSubArea = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetIsInSubArea(oCreature, oSubArea) != 0;
        }

        /// <summary>
        /// Sets the main light color on the tile at the specified location.
        /// </summary>
        /// <param name="lTileLocation">The tile location (the vector part is the tile grid x,y coordinate)</param>
        /// <param name="nMainLight1Color">The main light 1 color (TILE_MAIN_LIGHT_COLOR_* constant)</param>
        /// <param name="nMainLight2Color">The main light 2 color (TILE_MAIN_LIGHT_COLOR_* constant)</param>
        public static void SetTileMainLightColor(Location lTileLocation, int nMainLight1Color, int nMainLight2Color)
        {
            global::NWN.Core.NWScript.SetTileMainLightColor(lTileLocation, nMainLight1Color, nMainLight2Color);
        }

        /// <summary>
        /// Sets the source light color on the tile at the specified location.
        /// </summary>
        /// <param name="lTileLocation">The tile location (the vector part is the tile grid x,y coordinate)</param>
        /// <param name="nSourceLight1Color">The source light 1 color (TILE_SOURCE_LIGHT_COLOR_* constant)</param>
        /// <param name="nSourceLight2Color">The source light 2 color (TILE_SOURCE_LIGHT_COLOR_* constant)</param>
        public static void SetTileSourceLightColor(Location lTileLocation, int nSourceLight1Color,
            int nSourceLight2Color)
        {
            global::NWN.Core.NWScript.SetTileSourceLightColor(lTileLocation, nSourceLight1Color, nSourceLight2Color);
        }

        /// <summary>
        /// Gets the color for the main light 1 of the tile at the specified location.
        /// </summary>
        /// <param name="lTile">The tile location (the vector part is the tile grid x,y coordinate)</param>
        /// <returns>The main light 1 color (TILE_MAIN_LIGHT_COLOR_* constant)</returns>
        public static int GetTileMainLight1Color(Location lTile)
        {
            return global::NWN.Core.NWScript.GetTileMainLight1Color(lTile);
        }

        /// <summary>
        /// Gets the color for the main light 2 of the tile at the specified location.
        /// </summary>
        /// <param name="lTile">The tile location (the vector part is the tile grid x,y coordinate)</param>
        /// <returns>The main light 2 color (TILE_MAIN_LIGHT_COLOR_* constant)</returns>
        public static int GetTileMainLight2Color(Location lTile)
        {
            return global::NWN.Core.NWScript.GetTileMainLight2Color(lTile);
        }

        /// <summary>
        /// Gets the color for the source light 1 of the tile at the specified location.
        /// </summary>
        /// <param name="lTile">The tile location (the vector part is the tile grid x,y coordinate)</param>
        /// <returns>The source light 1 color (TILE_SOURCE_LIGHT_COLOR_* constant)</returns>
        public static int GetTileSourceLight1Color(Location lTile)
        {
            return global::NWN.Core.NWScript.GetTileSourceLight1Color(lTile);
        }

        /// <summary>
        /// Gets the color for the source light 2 of the tile at the specified location.
        /// </summary>
        /// <param name="lTile">The tile location (the vector part is the tile grid x,y coordinate)</param>
        /// <returns>The source light 2 color (TILE_SOURCE_LIGHT_COLOR_* constant)</returns>
        public static int GetTileSourceLight2Color(Location lTile)
        {
            return global::NWN.Core.NWScript.GetTileSourceLight2Color(lTile);
        }

        /// <summary>
        /// Sets whether the map pin is enabled.
        /// </summary>
        /// <param name="oMapPin">The map pin to set</param>
        /// <param name="bEnabled">Whether the map pin is enabled (0=Off, 1=On) (default: true)</param>
        public static void SetMapPinEnabled(uint oMapPin, bool bEnabled = true)
        {
            global::NWN.Core.NWScript.SetMapPinEnabled(oMapPin, bEnabled ? 1 : 0);
        }

        /// <summary>
        /// Gets the area's object ID from the specified location.
        /// </summary>
        /// <param name="lLocation">The location to get the area from</param>
        /// <returns>The area's object ID</returns>
        public static uint GetAreaFromLocation(Location lLocation)
        {
            return global::NWN.Core.NWScript.GetAreaFromLocation(lLocation);
        }

        /// <summary>
        /// Gets the position vector from the specified location.
        /// </summary>
        /// <param name="lLocation">The location to get the position from</param>
        /// <returns>The position vector</returns>
        public static Vector3 GetPositionFromLocation(Location lLocation)
        {
            return global::NWN.Core.NWScript.GetPositionFromLocation(lLocation);
        }

        /// <summary>
        /// Sets the transition bitmap of a player (should only be called in area transition scripts).
        /// </summary>
        /// <param name="nPredefinedAreaTransition">To use a predefined area transition bitmap, use one of AREA_TRANSITION_*. To use a custom, user-defined area transition bitmap, use AREA_TRANSITION_USER_DEFINED and specify the filename in the second parameter</param>
        /// <param name="sCustomAreaTransitionBMP">The filename of a custom, user-defined area transition bitmap (default: empty string)</param>
        /// <remarks>This action should be run by the person "clicking" the area transition via AssignCommand.</remarks>
        public static void SetAreaTransitionBMP(AreaTransition nPredefinedAreaTransition,
            string sCustomAreaTransitionBMP = "")
        {
            global::NWN.Core.NWScript.SetAreaTransitionBMP((int)nPredefinedAreaTransition, sCustomAreaTransitionBMP);
        }


        /// <summary>
        /// Sets the detailed wind data for the specified area.
        /// </summary>
        /// <param name="oArea">The area to set wind data for</param>
        /// <param name="vDirection">The wind direction vector</param>
        /// <param name="fMagnitude">The wind magnitude</param>
        /// <param name="fYaw">The wind yaw angle</param>
        /// <param name="fPitch">The wind pitch angle</param>
        /// <remarks>The predefined values in the toolset are: NONE: vDirection=(1.0, 1.0, 0.0), fMagnitude=0.0, fYaw=0.0, fPitch=0.0; LIGHT: vDirection=(1.0, 1.0, 0.0), fMagnitude=1.0, fYaw=100.0, fPitch=3.0; HEAVY: vDirection=(1.0, 1.0, 0.0), fMagnitude=2.0, fYaw=150.0, fPitch=5.0</remarks>
        public static void SetAreaWind(uint oArea, Vector3 vDirection, float fMagnitude, float fYaw, float fPitch)
        {
            global::NWN.Core.NWScript.SetAreaWind(oArea, vDirection, fMagnitude, fYaw, fPitch);
        }

        /// <summary>
        /// Gets the light color in the specified area.
        /// </summary>
        /// <param name="nColorType">The color type to return (AREA_LIGHT_COLOR_* values)</param>
        /// <param name="oArea">The area to get light color for. If no valid area is specified, uses the area of caller. If an object other than an area is specified, uses the area that the object is currently in (default: OBJECT_INVALID)</param>
        /// <returns>The light color</returns>
        public static int GetAreaLightColor(AreaLightColorType nColorType, uint oArea = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetAreaLightColor((int)nColorType, oArea);
        }

        /// <summary>
        /// Sets the light color in the specified area.
        /// </summary>
        /// <param name="nColorType">The color type (AREA_LIGHT_COLOR_* constants)</param>
        /// <param name="nColor">The color to set (FOG_COLOR_* constants). Can also be represented as a hex RGB number (e.g., 0xFFEEDD where FF=red, EE=green, DD=blue)</param>
        /// <param name="oArea">The area to set light color for. If no valid area is specified, uses the area of caller. If an object other than an area is specified, uses the area that the object is currently in (default: OBJECT_INVALID)</param>
        /// <param name="fFadeTime">If above 0.0, it will fade to the new color in the amount of seconds specified (default: 0.0)</param>
        public static void SetAreaLightColor(
            AreaLightColorType nColorType, 
            FogColor nColor, 
            uint oArea = OBJECT_INVALID, 
            float fFadeTime = 0.0f)
        {
            global::NWN.Core.NWScript.SetAreaLightColor((int)nColorType, (int)nColor, oArea, fFadeTime);
        }

        /// <summary>
        /// Gets the light direction of origin in the specified area.
        /// </summary>
        /// <param name="nLightType">Specifies whether the Moon or Sun light direction is returned (AREA_LIGHT_DIRECTION_* values)</param>
        /// <param name="oArea">The area to get light direction for. If no valid area is specified, uses the area of caller. If an object other than an area is specified, uses the area that the object is currently in (default: OBJECT_INVALID)</param>
        /// <returns>The light direction vector</returns>
        public static Vector3 GetAreaLightDirection(AreaLightDirectionType nLightType, uint oArea = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetAreaLightDirection((int)nLightType, oArea);
        }

        /// <summary>
        /// Sets the light direction of origin in the specified area.
        /// </summary>
        /// <param name="nLightType">The light type (AREA_LIGHT_DIRECTION_* constants)</param>
        /// <param name="vDirection">The direction of origin of the light type, i.e. the direction the sun/moon is in from the area</param>
        /// <param name="oArea">The area to set light direction for. If no valid area is specified, uses the area of caller. If an object other than an area is specified, uses the area that the object is currently in (default: OBJECT_INVALID)</param>
        /// <param name="fFadeTime">If above 0.0, it will fade to the new direction in the amount of seconds specified (default: 0.0)</param>
        public static void SetAreaLightDirection(
            AreaLightDirectionType nLightType, 
            Vector3 vDirection, 
            uint oArea = OBJECT_INVALID, 
            float fFadeTime = 0.0f)
        {
            global::NWN.Core.NWScript.SetAreaLightDirection((int)nLightType, vDirection, oArea, fFadeTime);
        }

        /// <summary>
        /// Gets the first object within the specified persistent object.
        /// </summary>
        /// <param name="oPersistentObject">The persistent object to search within (default: OBJECT_INVALID)</param>
        /// <param name="nResidentObjectType">The type of objects to find (OBJECT_TYPE_* constants) (default: ObjectType.Creature)</param>
        /// <param name="nPersistentZone">The persistent zone (PERSISTENT_ZONE_ACTIVE. PERSISTENT_ZONE_FOLLOW is no longer used) (default: PersistentZone.Active)</param>
        /// <returns>The first object found. Returns OBJECT_INVALID if no object is found</returns>
        public static uint GetFirstInPersistentObject(uint oPersistentObject = OBJECT_INVALID,
            ObjectType nResidentObjectType = ObjectType.Creature,
            PersistentZone nPersistentZone = PersistentZone.Active)
        {
            return global::NWN.Core.NWScript.GetFirstInPersistentObject(oPersistentObject, (int)nResidentObjectType, (int)nPersistentZone);
        }

        /// <summary>
        /// Gets the next object within the specified persistent object.
        /// </summary>
        /// <param name="oPersistentObject">The persistent object to search within (default: OBJECT_INVALID)</param>
        /// <param name="nResidentObjectType">The type of objects to find (OBJECT_TYPE_* constants) (default: ObjectType.Creature)</param>
        /// <param name="nPersistentZone">The persistent zone (PERSISTENT_ZONE_ACTIVE. PERSISTENT_ZONE_FOLLOW is no longer used) (default: PersistentZone.Active)</param>
        /// <returns>The next object found. Returns OBJECT_INVALID if no object is found</returns>
        public static uint GetNextInPersistentObject(uint oPersistentObject = OBJECT_INVALID,
            ObjectType nResidentObjectType = ObjectType.Creature,
            PersistentZone nPersistentZone = PersistentZone.Active)
        {
            return global::NWN.Core.NWScript.GetNextInPersistentObject(oPersistentObject, (int)nResidentObjectType, (int)nPersistentZone);
        }

        /// <summary>
        /// Returns the creator of the specified area of effect object.
        /// </summary>
        /// <param name="oAreaOfEffectObject">The area of effect object to get the creator for (default: OBJECT_INVALID)</param>
        /// <returns>The creator of the area of effect object. Returns OBJECT_INVALID if the object is not a valid Area of Effect object</returns>
        public static uint GetAreaOfEffectCreator(uint oAreaOfEffectObject = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetAreaOfEffectCreator(oAreaOfEffectObject);
        }

        /// <summary>
        /// Gets the distance between two locations.
        /// </summary>
        /// <param name="lLocationA">The first location</param>
        /// <param name="lLocationB">The second location</param>
        /// <returns>The distance between the locations</returns>
        public static float GetDistanceBetweenLocations(Location lLocationA, Location lLocationB)
        {
            return global::NWN.Core.NWScript.GetDistanceBetweenLocations(lLocationA, lLocationB);
        }

        /// <summary>
        /// Changes a tile in an area and updates it for all players in the area.
        /// </summary>
        /// <param name="locTile">The location of the tile</param>
        /// <param name="nTileID">The ID of the tile (for values see the .set file of the tileset)</param>
        /// <param name="nOrientation">The orientation of the tile (0-3): 0 = Normal orientation, 1 = 90 degrees counterclockwise, 2 = 180 degrees counterclockwise, 3 = 270 degrees counterclockwise</param>
        /// <param name="nHeight">The height of the tile (default: 0)</param>
        /// <param name="nFlags">A bitmask of SETTILE_FLAG_* constants (default: SetTileFlagType.RecomputeLighting)</param>
        /// <remarks>For optimal use you should be familiar with how tilesets / .set files work. Will not update the height of non-creature objects. Creatures may get stuck on non-walkable terrain. SETTILE_FLAG_RELOAD_GRASS: reloads the area's grass, use if your tile used to have grass or should have grass now. SETTILE_FLAG_RELOAD_BORDER: reloads the edge tile border, use if you changed a tile on the edge of the area. SETTILE_FLAG_RECOMPUTE_LIGHTING: recomputes the area's lighting and static shadows, use most of time.</remarks>
        public static void SetTile(
            Location locTile,
            int nTileID,
            int nOrientation,
            int nHeight = 0,
            SetTileFlagType nFlags = SetTileFlagType.RecomputeLighting)
        {
            global::NWN.Core.NWScript.SetTile(locTile, nTileID, nOrientation, nHeight, (int)nFlags);
        }

        /// <summary>
        /// Gets the ID of the tile at the specified location.
        /// </summary>
        /// <param name="locTile">The location to get the tile ID for</param>
        /// <returns>The tile ID. Returns -1 on error</returns>
        public static int GetTileID(Location locTile)
        {
            return global::NWN.Core.NWScript.GetTileID(locTile);
        }

        /// <summary>
        /// Gets the orientation of the tile at the specified location.
        /// </summary>
        /// <param name="locTile">The location to get the tile orientation for</param>
        /// <returns>The tile orientation. Returns -1 on error</returns>
        public static int GetTileOrientation(Location locTile)
        {
            return global::NWN.Core.NWScript.GetTileOrientation(locTile);
        }

        /// <summary>
        /// Gets the height of the tile at the specified location.
        /// </summary>
        /// <param name="locTile">The location to get the tile height for</param>
        /// <returns>The tile height. Returns -1 on error</returns>
        public static int GetTileHeight(Location locTile)
        {
            return global::NWN.Core.NWScript.GetTileHeight(locTile);
        }

        /// <summary>
        /// Makes all clients in the area reload the area's grass.
        /// </summary>
        /// <param name="oArea">The area to reload grass for</param>
        /// <remarks>This can be used to update the grass of an area after changing a tile with SetTile() that will have or used to have grass.</remarks>
        public static void ReloadAreaGrass(uint oArea)
        {
            global::NWN.Core.NWScript.ReloadAreaGrass(oArea);
        }

        /// <summary>
        /// Sets the state of the tile animation loops of the tile at the specified location.
        /// </summary>
        /// <param name="locTile">The location of the tile</param>
        /// <param name="bAnimLoop1">The state of animation loop 1</param>
        /// <param name="bAnimLoop2">The state of animation loop 2</param>
        /// <param name="bAnimLoop3">The state of animation loop 3</param>
        public static void SetTileAnimationLoops(Location locTile, bool bAnimLoop1, bool bAnimLoop2, bool bAnimLoop3)
        {
            global::NWN.Core.NWScript.SetTileAnimationLoops(locTile, bAnimLoop1 ? 1 : 0, bAnimLoop2 ? 1 : 0, bAnimLoop3 ? 1 : 0);
        }

        /// <summary>
        /// Changes multiple tiles in an area and updates them for all players in the area.
        /// </summary>
        /// <param name="oArea">The area to change one or more tiles of</param>
        /// <param name="jTileData">A JsonArray() with one or more JsonObject()s with the following keys: index (tile index as JsonInt()), tileid (tile ID as JsonInt(), defaults to 0), orientation (tile orientation as JsonInt(), defaults to 0), height (tile height as JsonInt(), defaults to 0), animloop1/2/3 (animation state as JsonInt(), defaults to current value)</param>
        /// <param name="nFlags">A bitmask of SETTILE_FLAG_* constants (default: SetTileFlagType.RecomputeLighting)</param>
        /// <param name="sTileset">If not empty, it will also change the area's tileset. Warning: only use this if you really know what you're doing, it's very easy to break things badly. Make sure jTileData changes *all* tiles in the area and to a tile id that's supported by sTileset (default: empty string)</param>
        /// <remarks>See SetTile() for additional information. For example, a 3x3 area has the following tile indexes: 6 7 8, 3 4 5, 0 1 2</remarks>
        public static void SetTileJson(
            uint oArea,
            Json jTileData,
            SetTileFlagType nFlags = SetTileFlagType.RecomputeLighting,
            string sTileset = "")
        {
            global::NWN.Core.NWScript.SetTileJson(oArea, jTileData, (int)nFlags, sTileset);
        }

        /// <summary>
        /// Makes all clients in the area reload the inaccessible border tiles.
        /// </summary>
        /// <param name="oArea">The area to reload border tiles for</param>
        /// <remarks>This can be used to update the edge tiles after changing a tile with SetTile().</remarks>
        public static void ReloadAreaBorder(uint oArea)
        {
            global::NWN.Core.NWScript.ReloadAreaBorder(oArea);
        }
    }
}