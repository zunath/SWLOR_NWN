using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWNX;
using Player = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.Feature
{
    public class PersistentMapPin
    {
        private struct MapPinDetails
        {
            public string Text { get; set; }
            public float PositionX { get; set; }
            public float PositionY { get; set; }
        }

        /// <summary>
        /// Loads a map pin's data into an object.
        /// Only call this from within one of the map pin event handlers.
        /// </summary>
        /// <returns>An object containing the active map pin's details.</returns>
        private static MapPin LoadMapPin(bool getId = true, bool isDestroying = false)
        {
            return new MapPin
            {
                Id = getId ? Convert.ToInt32(EventsPlugin.GetEventData("PIN_ID")) : -1,
                Note = isDestroying ? string.Empty : EventsPlugin.GetEventData("PIN_NOTE"),
                X = isDestroying ? 0 : (float) Convert.ToDouble(EventsPlugin.GetEventData("PIN_X")),
                Y = isDestroying ? 0 : (float) Convert.ToDouble(EventsPlugin.GetEventData("PIN_Y"))
            };
        }

        /// <summary>
        /// Adds a map pin to the PC entity and saves it to the DB.
        /// </summary>
        [NWNEventHandler(ScriptName.OnMapPinAddPinBefore)]
        public static void AddMapPin()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var mapPin = LoadMapPin(false);
            mapPin.Id = GetNumberOfMapPins(player) + 1;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId) ?? new Player(playerId);
            var area = GetArea(player);
            var areaResref = GetResRef(area);

            if(!dbPlayer.MapPins.ContainsKey(areaResref))
                dbPlayer.MapPins[areaResref] = new List<MapPin>();

            dbPlayer.MapPins[areaResref].Add(mapPin);

            DB.Set(dbPlayer);
        }

        /// <summary>
        /// Removes a map pin from the PC entity and saves it to the DB.
        /// </summary>
        [NWNEventHandler(ScriptName.OnMapPinDestroyPinBefore)]
        public static void DeleteMapPin()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var mapPin = LoadMapPin(true, true);
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            if (dbPlayer == null) return;

            var area = GetArea(player);
            var areaResref = GetResRef(area);
            if (!dbPlayer.MapPins.ContainsKey(areaResref))
                return;

            var mapPins = dbPlayer.MapPins[areaResref];
            for(int index = mapPins.Count-1; index >= 0; index--)
            {
                if (mapPins[index].Id == mapPin.Id)
                {
                    mapPins.RemoveAt(index);
                    break;
                }
            }

            DB.Set(dbPlayer);
        }

        /// <summary>
        /// Updates an existing map pin and saves the changes to the DB.
        /// </summary>
        [NWNEventHandler(ScriptName.OnMapPinChangePinBefore)]
        public static void ChangeMapPin()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var mapPin = LoadMapPin();
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            if (dbPlayer == null) return;

            var area = GetArea(player);
            var areaResref = GetResRef(area);
            if (!dbPlayer.MapPins.ContainsKey(areaResref))
                return;

            var mapPins = dbPlayer.MapPins[areaResref];
            for (int index = mapPins.Count - 1; index >= 0; index--)
            {
                if (mapPins[index].Id == mapPin.Id)
                {
                    mapPins[index].X = mapPin.X;
                    mapPins[index].Y = mapPin.Y;
                    mapPins[index].Note = mapPin.Note;
                    break;
                }
            }

            DB.Set(dbPlayer);
        }

        /// <summary>
        /// Loads map pins on all areas for a player. This only happens one time per reset.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void LoadMapPins()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player) || GetLocalBool(player, "MAP_PINS_LOADED")) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            var mapPinTuple = dbPlayer
                .MapPins
                .SelectMany(s => s.Value.Select(v => Tuple.Create(s.Key, v)))
                .OrderBy(o => o.Item2.Id);

            int pinsAdded = 0;
            foreach (var (areaResref, mapPin) in mapPinTuple)
            {
                var area = Area.GetAreaByResref(areaResref);
                SetMapPin(player, mapPin.Note, mapPin.X, mapPin.Y, area);

                // Increment the count and update the ID.
                pinsAdded++;
                mapPin.Id = pinsAdded;
            }

            // Ensure this doesn't run again until the next reboot.
            SetLocalBool(player, "MAP_PINS_LOADED", true);

            // Save any changes to the IDs.
            DB.Set(dbPlayer);
        }

        /// <summary>
        /// Retrieves the number of map pins stored on a player.
        /// </summary>
        /// <param name="player">The player whose map pin count we're retrieving</param>
        /// <returns>The number of map pins assigned to a player</returns>
        private static int GetNumberOfMapPins(uint player)
        {
            return GetLocalInt(player, "NW_TOTAL_MAP_PINS");
        }

        /// <summary>
        /// Sets the number of map pins stored on a player.
        /// </summary>
        /// <param name="player">The player whose map pin count we're adjusting</param>
        /// <param name="numberOfMapPins">The new value to set.</param>
        public static void SetNumberOfMapPins(uint player, int numberOfMapPins)
        {
            SetLocalInt(player, "NW_TOTAL_MAP_PINS", numberOfMapPins);
        }

        /// <summary>
        /// Builds a strongly-typed map pin by the local variables stored on a player.
        /// </summary>
        /// <param name="player">The player whose map pin information we're retrieving</param>
        /// <param name="index">The index of the map pin.</param>
        /// <returns>A strongly-typed struct containing a specific map pin's details.</returns>
        private static MapPinDetails GetMapPinDetails(uint player, int index)
        {
            index++;
            MapPinDetails mapPin = new MapPinDetails()
            {
                Text = GetLocalString(player, "NW_MAP_PIN_NTRY_" + index),
                PositionX = GetLocalFloat(player, "NW_MAP_PIN_XPOS_" + index),
                PositionY = GetLocalFloat(player, "NW_MAP_PIN_YPOS_" + index),
            };

            return mapPin;
        }

        /// <summary>
        /// Sets a map pin on a player's map for a given area.
        /// </summary>
        /// <param name="player">The player whose map pin we're adding</param>
        /// <param name="note">The text of the map pin.</param>
        /// <param name="x">The X position of the map pin.</param>
        /// <param name="y">The Y position of the map pin.</param>
        /// <param name="area">The area to add the map pin to.</param>
        private static void SetMapPin(uint player, string note, float x, float y, uint area)
        {
            int numberOfMapPins = GetNumberOfMapPins(player);
            int storeAtIndex = -1;

            for (int index = 0; index < numberOfMapPins; index++)
            {
                var mapPin = GetMapPinDetails(player, index);
                if (string.IsNullOrWhiteSpace(mapPin.Text))
                {
                    storeAtIndex = index;
                    break;
                }
            }

            if (storeAtIndex == -1)
            {
                numberOfMapPins++;
                storeAtIndex = numberOfMapPins - 1;
            }

            storeAtIndex++;

            SetLocalString(player, "NW_MAP_PIN_NTRY_" + storeAtIndex, note);
            SetLocalFloat(player, "NW_MAP_PIN_XPOS_" + storeAtIndex, x);
            SetLocalFloat(player, "NW_MAP_PIN_YPOS_" + storeAtIndex, y);
            SetLocalObject(player, "NW_MAP_PIN_AREA_" + storeAtIndex, area);
            SetNumberOfMapPins(player, numberOfMapPins);
        }
    }
}
