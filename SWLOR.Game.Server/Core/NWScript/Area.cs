using System;
using System.Numerics;
using NWN.Native.API;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Area;
using ObjectType = SWLOR.Game.Server.Core.NWScript.Enum.ObjectType;

namespace SWLOR.Game.Server.Core.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   Get the area that oTarget is currently in
        ///   * Return value on error: OBJECT_INVALID
        /// </summary>
        public static uint GetArea(uint oTarget)
        {
            VM.StackPush(oTarget);
            VM.Call(24);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   The value returned by this function depends on the object type of the caller:
        ///   1) If the caller is a door it returns the object that last
        ///   triggered it.
        ///   2) If the caller is a trigger, area of effect, module, area or encounter it
        ///   returns the object that last entered it.
        ///   * Return value on error: OBJECT_INVALID
        ///   When used for doors, this should only be called from the OnAreaTransitionClick
        ///   event.  Otherwise, it should only be called in OnEnter scripts.
        /// </summary>
        public static uint GetEnteringObject()
        {
            VM.Call(25);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   Get the object that last left the caller.  This function works on triggers,
        ///   areas of effect, modules, areas and encounters.
        ///   * Return value on error: OBJECT_INVALID
        ///   Should only be called in OnExit scripts.
        /// </summary>
        public static uint GetExitingObject()
        {
            VM.Call(26);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   Get the position of oTarget
        ///   * Return value on error: vector (0.0f, 0.0f, 0.0f)
        /// </summary>
        public static Vector3 GetPosition(uint oTarget)
        {
            VM.StackPush(oTarget);
            VM.Call(27);
            return VM.StackPopVector();
        }

        /// <summary>
        ///   Play the background music for oArea.
        /// </summary>
        public static void MusicBackgroundPlay(uint oArea)
        {
            VM.StackPush(oArea);
            VM.Call(425);
        }

        /// <summary>
        ///   Stop the background music for oArea.
        /// </summary>
        public static void MusicBackgroundStop(uint oArea)
        {
            VM.StackPush(oArea);
            VM.Call(426);
        }

        /// <summary>
        ///   Set the delay for the background music for oArea.
        ///   - oArea
        ///   - nDelay: delay in milliseconds
        /// </summary>
        public static void MusicBackgroundSetDelay(uint oArea, int nDelay)
        {
            VM.StackPush(nDelay);
            VM.StackPush(oArea);
            VM.Call(427);
        }

        /// <summary>
        ///   Change the background day track for oArea to nTrack.
        ///   - oArea
        ///   - nTrack
        /// </summary>
        public static void MusicBackgroundChangeDay(uint oArea, int nTrack)
        {
            VM.StackPush(nTrack);
            VM.StackPush(oArea);
            VM.Call(428);
        }

        /// <summary>
        ///   Change the background night track for oArea to nTrack.
        ///   - oArea
        ///   - nTrack
        /// </summary>
        public static void MusicBackgroundChangeNight(uint oArea, int nTrack)
        {
            VM.StackPush(nTrack);
            VM.StackPush(oArea);
            VM.Call(429);
        }

        /// <summary>
        ///   Play the battle music for oArea.
        /// </summary>
        public static void MusicBattlePlay(uint oArea)
        {
            VM.StackPush(oArea);
            VM.Call(430);
        }

        /// <summary>
        ///   Stop the battle music for oArea.
        /// </summary>
        public static void MusicBattleStop(uint oArea)
        {
            VM.StackPush(oArea);
            VM.Call(431);
        }

        /// <summary>
        ///   Change the battle track for oArea.
        ///   - oArea
        ///   - nTrack
        /// </summary>
        public static void MusicBattleChange(uint oArea, int nTrack)
        {
            VM.StackPush(nTrack);
            VM.StackPush(oArea);
            VM.Call(432);
        }

        /// <summary>
        ///   Play the ambient sound for oArea.
        /// </summary>
        public static void AmbientSoundPlay(uint oArea)
        {
            VM.StackPush(oArea);
            VM.Call(433);
        }

        /// <summary>
        ///   Stop the ambient sound for oArea.
        /// </summary>
        public static void AmbientSoundStop(uint oArea)
        {
            VM.StackPush(oArea);
            VM.Call(434);
        }

        /// <summary>
        ///   Change the ambient day track for oArea to nTrack.
        ///   - oArea
        ///   - nTrack
        /// </summary>
        public static void AmbientSoundChangeDay(uint oArea, int nTrack)
        {
            VM.StackPush(nTrack);
            VM.StackPush(oArea);
            VM.Call(435);
        }

        /// <summary>
        ///   Change the ambient night track for oArea to nTrack.
        ///   - oArea
        ///   - nTrack
        /// </summary>
        public static void AmbientSoundChangeNight(uint oArea, int nTrack)
        {
            VM.StackPush(nTrack);
            VM.StackPush(oArea);
            VM.Call(436);
        }

        /// <summary>
        ///   All clients in oArea will recompute the static lighting.
        ///   This can be used to update the lighting after changing any tile lights or if
        ///   placeables with lights have been added/deleted.
        /// </summary>
        public static void RecomputeStaticLighting(uint oArea)
        {
            VM.StackPush(oArea);
            VM.Call(516);
        }

        /// <summary>
        ///   Get the Day Track for oArea.
        /// </summary>
        public static int MusicBackgroundGetDayTrack(uint oArea)
        {
            VM.StackPush(oArea);
            VM.Call(558);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Get the Night Track for oArea.
        /// </summary>
        public static int MusicBackgroundGetNightTrack(uint oArea)
        {
            VM.StackPush(oArea);
            VM.Call(559);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Set the ambient day volume for oArea to nVolume.
        ///   - oArea
        ///   - nVolume: 0 - 100
        /// </summary>
        public static void AmbientSoundSetDayVolume(uint oArea, int nVolume)
        {
            VM.StackPush(nVolume);
            VM.StackPush(oArea);
            VM.Call(567);
        }

        /// <summary>
        ///   Set the ambient night volume for oArea to nVolume.
        ///   - oArea
        ///   - nVolume: 0 - 100
        /// </summary>
        public static void AmbientSoundSetNightVolume(uint oArea, int nVolume)
        {
            VM.StackPush(nVolume);
            VM.StackPush(oArea);
            VM.Call(568);
        }

        /// <summary>
        ///   Get the Battle Track for oArea.
        /// </summary>
        public static int MusicBackgroundGetBattleTrack(uint oArea)
        {
            VM.StackPush(oArea);
            VM.Call(569);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   This will return TRUE if the area is flagged as either interior or underground.
        /// </summary>
        public static bool GetIsAreaInterior(uint oArea = OBJECT_INVALID)
        {
            VM.StackPush(oArea);
            VM.Call(716);
            return Convert.ToBoolean(VM.StackPopInt());
        }

        /// <summary>
        ///   Gets the current weather conditions for the area oArea.
        ///   Returns: WEATHER_CLEAR, WEATHER_RAIN, WEATHER_SNOW, WEATHER_INVALID
        ///   Note: If called on an Interior area, this will always return WEATHER_CLEAR.
        /// </summary>
        public static Weather GetWeather(uint oArea)
        {
            VM.StackPush(oArea);
            VM.Call(724);
            return (Weather)VM.StackPopInt();
        }

        /// <summary>
        ///   Returns AREA_NATURAL if the area oArea is natural, AREA_ARTIFICIAL otherwise.
        ///   Returns AREA_INVALID, on an error.
        /// </summary>
        public static Natural GetIsAreaNatural(uint oArea)
        {
            VM.StackPush(oArea);
            VM.Call(725);
            return (Natural)VM.StackPopInt();
        }

        /// <summary>
        ///   Returns AREA_ABOVEGROUND if the area oArea is above ground, AREA_UNDERGROUND otherwise.
        ///   Returns AREA_INVALID, on an error.
        /// </summary>
        public static bool GetIsAreaAboveGround(uint oArea)
        {
            VM.StackPush(oArea);
            VM.Call(726);
            return VM.StackPopInt() != 0;
        }

        /// <summary>
        ///   Changes the sky that is displayed in the specified area.
        ///   nSkyBox = SKYBOX_* constants (associated with skyboxes.2da)
        ///   If no valid area (or object) is specified, it uses the area of caller.
        ///   If an object other than an area is specified, will use the area that the object is currently in.
        /// </summary>
        public static void SetSkyBox(Skybox nSkyBox, uint oArea = OBJECT_INVALID)
        {
            VM.StackPush(oArea);
            VM.StackPush((int)nSkyBox);
            VM.Call(777);
        }

        /// <summary>
        ///   Sets the fog color in the area specified.
        ///   nFogType = FOG_TYPE_* specifies wether the Sun, Moon, or both fog types are set.
        ///   nFogColor = FOG_COLOR_* specifies the color the fog is being set to.
        ///   The fog color can also be represented as a hex RGB number if specific color shades
        ///   are desired.
        ///   The format of a hex specified color would be 0xFFEEDD where
        ///   FF would represent the amount of red in the color
        ///   EE would represent the amount of green in the color
        ///   DD would represent the amount of blue in the color.
        ///   If no valid area (or object) is specified, it uses the area of caller.
        ///   If an object other than an area is specified, will use the area that the object is currently in.
        /// </summary>
        public static void SetFogColor(FogType nFogType, FogColor nFogColor, uint oArea = OBJECT_INVALID)
        {
            VM.StackPush(oArea);
            VM.StackPush((int)nFogColor);
            VM.StackPush((int)nFogType);
            VM.Call(780);
        }

        /// <summary>
        ///   Gets the skybox that is currently displayed in the specified area.
        ///   Returns:
        ///   SKYBOX_* constant
        ///   If no valid area (or object) is specified, it uses the area of caller.
        ///   If an object other than an area is specified, will use the area that the object is currently in.
        /// </summary>
        public static Skybox GetSkyBox(uint oArea = OBJECT_INVALID)
        {
            VM.StackPush(oArea);
            VM.Call(782);
            return (Skybox)VM.StackPopInt();
        }

        /// <summary>
        ///   Gets the fog color in the area specified.
        ///   nFogType specifies wether the Sun, or Moon fog type is returned.
        ///   Valid values for nFogType are FOG_TYPE_SUN or FOG_TYPE_MOON.
        ///   If no valid area (or object) is specified, it uses the area of caller.
        ///   If an object other than an area is specified, will use the area that the object is currently in.
        /// </summary>
        public static FogColor GetFogColor(FogType nFogType, uint oArea = OBJECT_INVALID)
        {
            VM.StackPush(oArea);
            VM.StackPush((int)nFogType);
            VM.Call(783);
            return (FogColor)VM.StackPopInt();
        }

        /// <summary>
        ///   Sets the fog amount in the area specified.
        ///   nFogType = FOG_TYPE_* specifies wether the Sun, Moon, or both fog types are set.
        ///   nFogAmount = specifies the density that the fog is being set to.
        ///   If no valid area (or object) is specified, it uses the area of caller.
        ///   If an object other than an area is specified, will use the area that the object is currently in.
        /// </summary>
        public static void SetFogAmount(FogType nFogType, int nFogAmount, uint oArea = OBJECT_INVALID)
        {
            VM.StackPush(oArea);
            VM.StackPush(nFogAmount);
            VM.StackPush((int)nFogType);
            VM.Call(784);
        }

        /// <summary>
        ///   Gets the fog amount in the area specified.
        ///   nFogType = nFogType specifies wether the Sun, or Moon fog type is returned.
        ///   Valid values for nFogType are FOG_TYPE_SUN or FOG_TYPE_MOON.
        ///   If no valid area (or object) is specified, it uses the area of caller.
        ///   If an object other than an area is specified, will use the area that the object is currently in.
        /// </summary>
        public static int GetFogAmount(FogType nFogType, uint oArea = OBJECT_INVALID)
        {
            VM.StackPush(oArea);
            VM.StackPush((int)nFogType);
            VM.Call(785);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   returns the resref (TILESET_RESREF_*) of the tileset used to create area oArea.
        ///   TILESET_RESREF_BEHOLDER_CAVES
        ///   TILESET_RESREF_CASTLE_INTERIOR
        ///   TILESET_RESREF_CITY_EXTERIOR
        ///   TILESET_RESREF_CITY_INTERIOR
        ///   TILESET_RESREF_CRYPT
        ///   TILESET_RESREF_DESERT
        ///   TILESET_RESREF_DROW_INTERIOR
        ///   TILESET_RESREF_DUNGEON
        ///   TILESET_RESREF_FOREST
        ///   TILESET_RESREF_FROZEN_WASTES
        ///   TILESET_RESREF_ILLITHID_INTERIOR
        ///   TILESET_RESREF_MICROSET
        ///   TILESET_RESREF_MINES_AND_CAVERNS
        ///   TILESET_RESREF_RUINS
        ///   TILESET_RESREF_RURAL
        ///   TILESET_RESREF_RURAL_WINTER
        ///   TILESET_RESREF_SEWERS
        ///   TILESET_RESREF_UNDERDARK
        ///   * returns an empty string on an error.
        /// </summary>
        public static string GetTilesetResRef(uint oArea)
        {
            VM.StackPush(oArea);
            VM.Call(814);
            return VM.StackPopString();
        }

        /// <summary>
        ///   Gets the size of the area.
        ///   - nAreaDimension: The area dimension that you wish to determine.
        ///   AREA_HEIGHT
        ///   AREA_WIDTH
        ///   - oArea: The area that you wish to get the size of.
        ///   Returns: The number of tiles that the area is wide/high, or zero on an error.
        ///   If no valid area (or object) is specified, it uses the area of the caller.
        ///   If an object other than an area is specified, will use the area that the object is currently in.
        /// </summary>
        public static int GetAreaSize(Dimension nAreaDimension, uint oArea = OBJECT_INVALID)
        {
            VM.StackPush(oArea);
            VM.StackPush((int)nAreaDimension);
            VM.Call(829);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Destroys the given area object and everything in it.
        /// 
        ///   If the area is in a module, the .are and .git data is left behind and you can spawn from
        ///   it again. If the area is a temporary copy, the data will be deleted and you cannot spawn it again
        ///   via the resref.
        ///
        ///   Return values:
        ///   0: Object not an area or invalid.
        ///   -1: Area contains spawn location and removal would leave module without entrypoint.
        ///   -2: Players in area.
        ///   1: Area destroyed successfully.
        /// </summary>
        public static int DestroyArea(uint oArea)
        {
            VM.StackPush(oArea);
            VM.Call(859);
            return VM.StackPopInt();
        }


        /// <summary>
        ///   Instances a new area from the given sSourceResRef, which needs to be a existing module area.
        ///   Will optionally set a new area tag and displayed name. The new area is accessible
        ///   immediately, but initialisation scripts for the area and all contained creatures will only
        ///   run after the current script finishes (so you can clean up objects before returning).
        ///   Returns the new area, or OBJECT_INVALID on failure.
        ///
        /// Note: When spawning a second instance of a existing area, you will have to manually
        ///   adjust all transitions (doors, triggers) with the relevant script commands,
        ///   or players might end up in the wrong area.
        ///
        /// Note: Areas cannot have duplicate ResRefs, so your new area will have a autogenerated,
        ///       sequential resref starting with "nw_"; for example: nw_5. You cannot influence this resref.
        ///       If you destroy an area, that resref will be come free for reuse for the next area created.
        ///       If you need to know the resref of your new area, you can call GetResRef on it.
        /// 
        /// Note: When instancing an area from a loaded savegame, it will spawn the area as it was at time of save, NOT
        ///       at module creation. This is because the savegame replaces the module data. Due to technical limitations,
        ///       polymorphed creatures, personal reputation, and associates will currently fail to restore correctly.
        /// </summary>
        public static uint CreateArea(string sSourceResRef, string sNewTag = "", string sNewName = "")
        {
            VM.StackPush(sNewName);
            VM.StackPush(sNewTag);
            VM.StackPush(sSourceResRef);
            VM.Call(858);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   Creates a copy of a existing area, including everything inside of it (except players).
        /// 
        ///   Will optionally set a new area tag and displayed name. The new area is accessible
        ///   immediately, but initialisation scripts for the area and all contained creatures will only
        ///   run after the current script finishes (so you can clean up objects before returning).
        ///   
        ///   This is similar to CreateArea, except this variant will copy all changes made to the source
        ///   area since it has spawned. CreateArea() will instance the area from the .are and .git data
        ///   as it was at creation.
        /// 
        ///   Returns the new area, or OBJECT_INVALID on error.
        ///   Note: You will have to manually adjust all transitions (doors, triggers) with the
        ///   relevant script commands, or players might end up in the wrong area.
        ///   Note: Areas cannot have duplicate ResRefs, so your new area will have a autogenerated,
        ///       sequential resref starting with "nw_"; for example: nw_5. You cannot influence this resref.
        ///       If you destroy an area, that resref will be come free for reuse for the next area created.
        ///       If you need to know the resref of your new area, you can call GetResRef on it.
        /// </summary>
        public static uint CopyArea(uint oArea)
        {
            VM.StackPush(oArea);
            VM.Call(860);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   Returns the first area in the module.
        /// </summary>
        public static uint GetFirstArea()
        {
            VM.Call(861);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   Returns the next area in the module (after GetFirstArea), or OBJECT_INVALID if no more
        ///   areas are loaded.
        /// </summary>
        public static uint GetNextArea()
        {
            VM.Call(862);
            return VM.StackPopObject();
        }

        /// <summary>
        /// Get the first object in oArea.
        /// If no valid area is specified, it will use the caller's area.
        /// - nObjectFilter: This allows you to filter out undesired object types, using bitwise "or".
        ///   For example, to return only creatures and doors, the value for this parameter would be OBJECT_TYPE_CREATURE | OBJECT_TYPE_DOOR
        /// * Return value on error: OBJECT_INVALID
        /// </summary>
        public static uint GetFirstObjectInArea(uint oArea = OBJECT_INVALID, ObjectType nObjectFilter = ObjectType.All)
        {
            VM.StackPush((int)nObjectFilter);
            VM.StackPush(oArea);
            VM.Call(93);
            return VM.StackPopObject();
        }

        /// <summary>
        /// Get the next object in oArea.
        /// If no valid area is specified, it will use the caller's area.
        /// - nObjectFilter: This allows you to filter out undesired object types, using bitwise "or".
        ///   For example, to return only creatures and doors, the value for this parameter would be OBJECT_TYPE_CREATURE | OBJECT_TYPE_DOOR
        /// * Return value on error: OBJECT_INVALID
        /// </summary>
        public static uint GetNextObjectInArea(uint oArea = OBJECT_INVALID, ObjectType nObjectFilter = ObjectType.All)
        {
            VM.StackPush((int)nObjectFilter);
            VM.StackPush(oArea);
            VM.Call(94);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   Get the location of oObject.
        /// </summary>
        public static Location GetLocation(uint oObject)
        {
            VM.StackPush(oObject);
            VM.Call(213);
            return VM.StackPopStruct((int)EngineStructure.Location);
        }

        /// <summary>
        ///   The subject will jump to lLocation instantly (even between areas).
        ///   If lLocation is invalid, nothing will happen.
        /// </summary>
        public static void ActionJumpToLocation(Location lLocation)
        {
            VM.StackPush((int)EngineStructure.Location, lLocation);
            VM.Call(214);
        }

        /// <summary>
        ///   Create a location.
        /// </summary>
        public static Location Location(uint oArea, Vector3 vPosition, float fOrientation)
        {
            VM.StackPush(fOrientation);
            VM.StackPush(vPosition);
            VM.StackPush(oArea);
            VM.Call(215);
            return VM.StackPopStruct((int)EngineStructure.Location);
        }

        /// <summary>
        ///   Apply eEffect at lLocation.
        /// </summary>
        public static void ApplyEffectAtLocation(DurationType nDurationType, Effect eEffect, Location lLocation,
            float fDuration = 0.0f)
        {
            VM.StackPush(fDuration);
            VM.StackPush((int)EngineStructure.Location, lLocation);
            VM.StackPush((int)EngineStructure.Effect, eEffect);
            VM.StackPush((int)nDurationType);
            VM.Call(216);
        }

        /// <summary>
        ///   Expose/Hide the entire map of oArea for oPlayer.
        ///   - oArea: The area that the map will be exposed/hidden for.
        ///   - oPlayer: The player the map will be exposed/hidden for.
        ///   - bExplored: TRUE/FALSE. Whether the map should be completely explored or hidden.
        /// </summary>
        public static void ExploreAreaForPlayer(uint oArea, uint oPlayer, bool bExplored = true)
        {
            VM.StackPush(bExplored ? 1 : 0);
            VM.StackPush(oPlayer);
            VM.StackPush(oArea);
            VM.Call(403);
        }

        /// <summary>
        ///   Sets the transition target for oTransition.
        ///   Notes:
        ///   - oTransition can be any valid game object, except areas.
        ///   - oTarget can be any valid game object with a location, or OBJECT_INVALID (to unlink).
        ///   - Rebinding a transition will NOT change the other end of the transition; for example,
        ///   with normal doors you will have to do either end separately.
        ///   - Any valid game object can hold a transition target, but only some are used by the game engine
        ///   (doors and triggers). This might change in the future. You can still set and query them for
        ///   other game objects from nwscript.
        ///   - Transition target objects are cached: The toolset-configured destination tag is
        ///   used for a lookup only once, at first use. Thus, attempting to use SetTag() to change the
        ///   destination for a transition will not work in a predictable fashion.
        /// </summary>
        public static void SetTransitionTarget(uint oTransition, uint oTarget)
        {
            VM.StackPush(oTarget);
            VM.StackPush(oTransition);
            VM.Call(863);
        }

        /// <summary>
        ///   Set the weather for oTarget.
        ///   - oTarget: if this is GetModule(), all outdoor areas will be modified by the
        ///   weather constant. If it is an area, oTarget will play the weather only if
        ///   it is an outdoor area.
        ///   - nWeather: WEATHER_*
        ///   -> WEATHER_USER_AREA_SETTINGS will set the area back to random weather.
        ///   -> WEATHER_CLEAR, WEATHER_RAIN, WEATHER_SNOW will make the weather go to
        ///   the appropriate precipitation *without stopping*.
        /// </summary>
        public static void SetWeather(uint oTarget, WeatherType nWeather)
        {
            VM.StackPush((int)nWeather);
            VM.StackPush(oTarget);
            VM.Call(507);
        }

        /// <summary>
        ///   Sets if the given creature has explored tile at x, y of the given area.
        ///   Note that creature needs to be a player- or player-possessed creature.
        ///   Keep in mind that tile exploration also controls object visibility in areas
        ///   and the fog of war for interior and underground areas.
        ///   Return values:
        ///   -1: Area or creature invalid.
        ///   0: Tile was not explored before setting newState.
        ///   1: Tile was explored before setting newState.
        /// </summary>
        public static int SetTileExplored(uint creature, uint area, int x, int y, bool newState)
        {
            VM.StackPush(newState ? 1 : 0);
            VM.StackPush(y);
            VM.StackPush(x);
            VM.StackPush(area);
            VM.StackPush(creature);
            VM.Call(866);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Returns whether the given tile at x, y, for the given creature in the stated
        ///   area is visible on the map.
        ///   Note that creature needs to be a player- or player-possessed creature.
        ///   Keep in mind that tile exploration also controls object visibility in areas
        ///   and the fog of war for interior and underground areas.
        ///   Return values:
        ///   -1: Area or creature invalid.
        ///   0: Tile is not explored yet.
        ///   1: Tile is explored.
        /// </summary>
        public static int GetTileExplored(uint creature, uint area, int x, int y)
        {
            VM.StackPush(y);
            VM.StackPush(x);
            VM.StackPush(area);
            VM.StackPush(creature);
            VM.Call(867);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Sets the creature to auto-explore the map as it walks around.
        ///   Keep in mind that tile exploration also controls object visibility in areas
        ///   and the fog of war for interior and underground areas.
        ///   This means that if you turn off auto exploration, it falls to you to manage this
        ///   through SetTileExplored(); otherwise, the player will not be able to see anything.
        ///   Valid arguments: TRUE and FALSE.
        ///   Does nothing for non-creatures.
        ///   Returns the previous state (or -1 if non-creature).
        /// </summary>
        public static int SetCreatureExploresMinimap(uint creature, bool newState)
        {
            VM.StackPush(newState ? 1 : 0);
            VM.StackPush(creature);
            VM.Call(868);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Returns TRUE if the creature is set to auto-explore the map as it walks around (on by default).
        ///   Returns FALSE if creature is not actually a creature.
        /// </summary>
        public static int GetCreatureExploresMinimap(uint creature)
        {
            VM.StackPush(creature);
            VM.Call(869);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Get the surface material at the given location. (This is
        ///   equivalent to the walkmesh type).
        ///   Returns 0 if the location is invalid or has no surface type.
        /// </summary>
        public static int GetSurfaceMaterial(Location at)
        {
            VM.StackPush((int)EngineStructure.Location, at);
            VM.Call(870);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Returns the z-offset at which the walkmesh is at the given location.
        ///   Returns -6.0 for invalid locations.
        /// </summary>
        public static float GetGroundHeight(Location at)
        {
            VM.StackPush((int)EngineStructure.Location, at);
            VM.Call(871);
            return VM.StackPopFloat();
        }

        /// <summary>
        ///   Is this creature in the given subarea? (trigger, area of effect object, etc..)
        ///   This function will tell you if the creature has triggered an onEnter event,
        ///   not if it is physically within the space of the subarea
        /// </summary>
        public static bool GetIsInSubArea(uint oCreature, uint oSubArea = OBJECT_INVALID)
        {
            VM.StackPush(oSubArea);
            VM.StackPush(oCreature);
            VM.Call(768);
            return VM.StackPopInt() != 0;
        }

        /// <summary>
        ///   Set the main light color on the tile at lTileLocation.
        ///   - lTileLocation: the vector part of this is the tile grid (x,y) coordinate of
        ///   the tile.
        ///   - nMainLight1Color: TILE_MAIN_LIGHT_COLOR_*
        ///   - nMainLight2Color: TILE_MAIN_LIGHT_COLOR_*
        /// </summary>
        public static void SetTileMainLightColor(Location lTileLocation, int nMainLight1Color, int nMainLight2Color)
        {
            VM.StackPush(nMainLight2Color);
            VM.StackPush(nMainLight1Color);
            VM.StackPush((int)EngineStructure.Location, lTileLocation);
            VM.Call(514);
        }

        /// <summary>
        ///   Set the source light color on the tile at lTileLocation.
        ///   - lTileLocation: the vector part of this is the tile grid (x,y) coordinate of
        ///   the tile.
        ///   - nSourceLight1Color: TILE_SOURCE_LIGHT_COLOR_*
        ///   - nSourceLight2Color: TILE_SOURCE_LIGHT_COLOR_*
        /// </summary>
        public static void SetTileSourceLightColor(Location lTileLocation, int nSourceLight1Color,
            int nSourceLight2Color)
        {
            VM.StackPush(nSourceLight2Color);
            VM.StackPush(nSourceLight1Color);
            VM.StackPush((int)EngineStructure.Location, lTileLocation);
            VM.Call(515);
        }

        /// <summary>
        ///   Get the color (TILE_MAIN_LIGHT_COLOR_*) for the main light 1 of the tile at
        ///   lTile.
        ///   - lTile: the vector part of this is the tile grid (x,y) coordinate of the tile.
        /// </summary>
        public static int GetTileMainLight1Color(Location lTile)
        {
            VM.StackPush((int)EngineStructure.Location, lTile);
            VM.Call(517);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Get the color (TILE_MAIN_LIGHT_COLOR_*) for the main light 2 of the tile at
        ///   lTile.
        ///   - lTile: the vector part of this is the tile grid (x,y) coordinate of the
        ///   tile.
        /// </summary>
        public static int GetTileMainLight2Color(Location lTile)
        {
            VM.StackPush((int)EngineStructure.Location, lTile);
            VM.Call(518);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Get the color (TILE_SOURCE_LIGHT_COLOR_*) for the source light 1 of the tile
        ///   at lTile.
        ///   - lTile: the vector part of this is the tile grid (x,y) coordinate of the
        ///   tile.
        /// </summary>
        public static int GetTileSourceLight1Color(Location lTile)
        {
            VM.StackPush((int)EngineStructure.Location, lTile);
            VM.Call(519);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Get the color (TILE_SOURCE_LIGHT_COLOR_*) for the source light 2 of the tile
        ///   at lTile.
        ///   - lTile: the vector part of this is the tile grid (x,y) coordinate of the
        ///   tile.
        /// </summary>
        public static int GetTileSourceLight2Color(Location lTile)
        {
            VM.StackPush((int)EngineStructure.Location, lTile);
            VM.Call(520);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Set whether oMapPin is enabled.
        ///   - oMapPin
        ///   - nEnabled: 0=Off, 1=On
        /// </summary>
        public static void SetMapPinEnabled(uint oMapPin, bool bEnabled = true)
        {
            VM.StackPush(bEnabled ? 1 : 0);
            VM.StackPush(oMapPin);
            VM.Call(386);
        }

        /// <summary>
        ///   Get the area's object ID from lLocation.
        /// </summary>
        public static uint GetAreaFromLocation(Location lLocation)
        {
            VM.StackPush((int)EngineStructure.Location, lLocation);
            VM.Call(224);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   Get the position vector from lLocation.
        /// </summary>
        public static Vector3 GetPositionFromLocation(Location lLocation)
        {
            VM.StackPush((int)EngineStructure.Location, lLocation);
            VM.Call(223);
            return VM.StackPopVector();
        }

        /// <summary>
        ///   Set the transition bitmap of a player; this should only be called in area
        ///   transition scripts. This action should be run by the person "clicking" the
        ///   area transition via AssignCommand.
        ///   - nPredefinedAreaTransition:
        ///   -> To use a predefined area transition bitmap, use one of AREA_TRANSITION_*
        ///   -> To use a custom, user-defined area transition bitmap, use
        ///   AREA_TRANSITION_USER_DEFINED and specify the filename in the second
        ///   parameter
        ///   - sCustomAreaTransitionBMP: this is the filename of a custom, user-defined
        ///   area transition bitmap
        /// </summary>
        public static void SetAreaTransitionBMP(AreaTransition nPredefinedAreaTransition,
            string sCustomAreaTransitionBMP = "")
        {
            VM.StackPush(sCustomAreaTransitionBMP);
            VM.StackPush((int)nPredefinedAreaTransition);
            VM.Call(203);
        }


        /// <summary>
        /// Sets the detailed wind data for oArea
        /// The predefined values in the toolset are:
        ///   NONE:  vDirection=(1.0, 1.0, 0.0), fMagnitude=0.0, fYaw=0.0,   fPitch=0.0
        ///   LIGHT: vDirection=(1.0, 1.0, 0.0), fMagnitude=1.0, fYaw=100.0, fPitch=3.0
        ///   HEAVY: vDirection=(1.0, 1.0, 0.0), fMagnitude=2.0, fYaw=150.0, fPitch=5.0
        /// </summary>
        public static void SetAreaWind(uint oArea, Vector3 vDirection, float fMagnitude, float fYaw, float fPitch)
        {
            VM.StackPush(fPitch);
            VM.StackPush(fYaw);
            VM.StackPush(fMagnitude);
            VM.StackPush(vDirection);
            VM.StackPush(oArea);
            VM.Call(919);
        }

        /// <summary>
        /// Gets the light color in the area specified.
        /// nColorType specifies the color type returned.
        ///    Valid values for nColorType are the AREA_LIGHT_COLOR_* values.
        /// If no valid area (or object) is specified, it uses the area of caller.
        /// If an object other than an area is specified, will use the area that the object is currently in.
        /// </summary>
        public static int GetAreaLightColor(AreaLightColorType nColorType, uint oArea = OBJECT_INVALID)
        {
            VM.StackPush(oArea);
            VM.StackPush((int)nColorType);
            VM.Call(1080);

            return VM.StackPopInt();
        }

        /// <summary>
        /// Sets the light color in the area specified.
        /// nColorType = AREA_LIGHT_COLOR_* specifies the color type.
        /// nColor = FOG_COLOR_* specifies the color the fog is being set to.
        /// The color can also be represented as a hex RGB number if specific color shades
        /// are desired.
        /// The format of a hex specified color would be 0xFFEEDD where
        /// FF would represent the amount of red in the color
        /// EE would represent the amount of green in the color
        /// DD would represent the amount of blue in the color.
        /// If no valid area (or object) is specified, it uses the area of caller.
        /// If an object other than an area is specified, will use the area that the object is currently in.
        /// If fFadeTime is above 0.0, it will fade to the new color in the amount of seconds specified. 
        /// </summary>
        public static void SetAreaLightColor(
            AreaLightColorType nColorType, 
            FogColor nColor, 
            uint oArea = OBJECT_INVALID, 
            float fFadeTime = 0.0f)
        {
            VM.StackPush(fFadeTime);
            VM.StackPush(oArea);
            VM.StackPush((int)nColor);
            VM.StackPush((int)nColorType);
            VM.Call(1081);
        }

        /// <summary>
        /// Gets the light direction of origin in the area specified.
        /// nLightType specifies whether the Moon or Sun light direction is returned.
        ///    Valid values for nColorType are the AREA_LIGHT_DIRECTION_* values.
        /// If no valid area (or object) is specified, it uses the area of caller.
        /// If an object other than an area is specified, will use the area that the object is currently in.
        /// </summary>
        public static Vector3 GetAreaLightDirection(AreaLightDirectionType nLightType, uint oArea = OBJECT_INVALID)
        {
            VM.StackPush((int)nLightType);
            VM.StackPush(oArea);
            VM.Call(1082);

            return VM.StackPopVector();
        }

        /// <summary>
        /// Sets the light direction of origin in the area specified.
        /// nLightType = AREA_LIGHT_DIRECTION_* specifies the light type.
        /// vDirection = specifies the direction of origin of the light type, i.e. the direction the sun/moon is in from the area.
        /// If no valid area (or object) is specified, it uses the area of caller.
        /// If an object other than an area is specified, will use the area that the object is currently in.
        /// If fFadeTime is above 0.0, it will fade to the new color in the amount of seconds specified. 
        /// </summary>
        public static void SetAreaLightDirection(
            AreaLightDirectionType nLightType, 
            Vector3 vDirection, 
            uint oArea = OBJECT_INVALID, 
            float fFadeTime = 0.0f)
        {
            VM.StackPush(fFadeTime);
            VM.StackPush(oArea);
            VM.StackPush(vDirection);
            VM.StackPush((int)nLightType);
            VM.Call(1083);
        }

    }
}