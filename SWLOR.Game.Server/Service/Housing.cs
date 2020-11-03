using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service
{
    public static class Housing
    {
        private static readonly Dictionary<FurnitureType, FurnitureAttribute> _activeFurniture = new Dictionary<FurnitureType, FurnitureAttribute>();
        private static readonly Dictionary<PlayerHouseType, PlayerHouseAttribute> _activePlayerHouses = new Dictionary<PlayerHouseType, PlayerHouseAttribute>();
        private static readonly Dictionary<PlayerHouseType, Vector3> _houseEntrancePositions = new Dictionary<PlayerHouseType, Vector3>();
        private static readonly Dictionary<string, uint> _activeHouseInstances = new Dictionary<string, uint>();

        /// <summary>
        /// When the module loads, cache all relevant data into memory.
        /// </summary>
        [NWNEventHandler("mod_load")]
        public static void CacheData()
        {
            LoadFurniture();
            LoadPlayerHouses();
        }

        /// <summary>
        /// When the module loads, read all furniture types and store them into the cache.
        /// </summary>
        private static void LoadFurniture()
        {
            var furnitureTypes = Enum.GetValues(typeof(FurnitureType)).Cast<FurnitureType>();
            foreach (var furniture in furnitureTypes)
            {
                var furnitureDetail = furniture.GetAttribute<FurnitureType, FurnitureAttribute>();

                if (furnitureDetail.IsActive)
                {
                    _activeFurniture[furniture] = furnitureDetail;
                }
            }
        }

        /// <summary>
        /// When the module loads, read all player house types and store them into the cache.
        /// </summary>
        private static void LoadPlayerHouses()
        {
            var houseTypes = Enum.GetValues(typeof(PlayerHouseType)).Cast<PlayerHouseType>();
            foreach (var houseType in houseTypes)
            {
                var houseDetail = houseType.GetAttribute<PlayerHouseType, PlayerHouseAttribute>();

                if (houseDetail.IsActive)
                {
                    _activePlayerHouses[houseType] = houseDetail;
                }

                _houseEntrancePositions[houseType] = GetEntrancePosition(houseDetail.AreaInstanceResref);
            }
        }

        /// <summary>
        /// Iterates over all areas to find the matching instance assigned to the specified resref.
        /// Then, the entrance waypoint is located and its coordinates are stored into cache.
        /// </summary>
        /// <param name="areaResref">The resref of the area to look for</param>
        /// <returns>X, Y, and Z coordinates of the entrance location</returns>
        private static Vector3 GetEntrancePosition(string areaResref)
        {
            for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
            {
                if (GetResRef(area) != areaResref) continue;

                for (var obj = GetFirstObjectInArea(area); GetIsObjectValid(obj); obj = GetNextObjectInArea(area))
                {
                    if (GetTag(obj) != "HOME_ENTRANCE") continue;

                    var position = GetPosition(obj);
                    return position;
                }
            }

            return new Vector3();
        }

        /// <summary>
        /// Retrieves all of the active player house types.
        /// </summary>
        /// <returns>All active house types</returns>
        public static Dictionary<PlayerHouseType, PlayerHouseAttribute> GetActiveHouseTypes()
        {
            return _activePlayerHouses.ToDictionary(x => x.Key, y => y.Value);
        }

        /// <summary>
        /// Retrieves a specific type of house's details.
        /// </summary>
        /// <param name="type">The player house layout type.</param>
        /// <returns>Details for the specified house layout type.</returns>
        public static PlayerHouseAttribute GetHouseTypeDetail(PlayerHouseType type)
        {
            return _activePlayerHouses[type];
        }

        /// <summary>
        /// Retrieves a specific type of furniture's details.
        /// </summary>
        /// <param name="type">The type of furniture.</param>
        /// <returns>Details for the specified furniture type.</returns>
        public static FurnitureAttribute GetFurnitureDetail(FurnitureType type)
        {
            return _activeFurniture[type];
        }

        /// <summary>
        /// Retrieves the X, Y, and Z coordinates of the home layout's entrance.
        /// </summary>
        /// <param name="type">The type of house layout to look for</param>
        /// <returns>The X, Y, and Z coordinates of the home layout's entrance.</returns>
        public static Vector3 GetEntrancePosition(PlayerHouseType type)
        {
            return _houseEntrancePositions[type];
        }

        /// <summary>
        /// Stores the original location of a player, before being ported into a house instance.
        /// </summary>
        /// <param name="player">The player whose location will be stored.</param>
        public static void StoreOriginalLocation(uint player)
        {
            var position = GetPosition(player);
            var facing = GetFacing(player);
            var area = GetArea(player);

            SetLocalFloat(player, "HOUSING_STORED_LOCATION_X", position.X);
            SetLocalFloat(player, "HOUSING_STORED_LOCATION_Y", position.Y);
            SetLocalFloat(player, "HOUSING_STORED_LOCATION_Z", position.Z);
            SetLocalFloat(player, "HOUSING_STORED_LOCATION_FACING", facing);
            SetLocalObject(player, "HOUSING_STORED_LOCATION_AREA", area);
        }

        /// <summary>
        /// Jumps player to their original location, which is where they were before entering a house instance.
        /// This will also clear the temporary data related to the original location.
        /// </summary>
        /// <param name="player">The player who will jump.</param>
        public static void JumpToOriginalLocation(uint player)
        {
            var position = Vector3(
                GetLocalFloat(player, "HOUSING_STORED_LOCATION_X"),
                GetLocalFloat(player, "HOUSING_STORED_LOCATION_Y"),
                GetLocalFloat(player, "HOUSING_STORED_LOCATION_Z"));
            var facing = GetLocalFloat(player, "HOUSING_STORED_LOCATION_FACING");
            var area = GetLocalObject(player, "HOUSING_STORED_LOCATION_AREA");
            var location = Location(area, position, facing);

            AssignCommand(player, () => ActionJumpToLocation(location));

            DeleteLocalFloat(player, "HOUSING_STORED_LOCATION_X");
            DeleteLocalFloat(player, "HOUSING_STORED_LOCATION_Y");
            DeleteLocalFloat(player, "HOUSING_STORED_LOCATION_Z");
            DeleteLocalFloat(player, "HOUSING_STORED_LOCATION_FACING");
            DeleteLocalObject(player, "HOUSING_STORED_LOCATION_AREA");
        }

        /// <summary>
        /// Creates an instance of an area and marks it as such.
        /// </summary>
        /// <param name="originalArea">The original area to copy.</param>
        /// <returns>The newly copied area.</returns>
        public static uint CreateInstance(uint originalArea)
        {
            var copy = CopyArea(originalArea);
            SetLocalBool(copy, "HOUSING_IS_INSTANCE", true);

            return copy;
        }

        /// <summary>
        /// Attempts to clean up an area instance.
        /// There must be no players in the area for this cleanup to happen and
        /// the area must be marked as an instance.
        /// It's recommended to call this on a short delay because players may
        /// still be considered in the area if they are transitioning between areas.
        /// </summary>
        /// <param name="area">The area to clean up.</param>
        public static void AttemptCleanUpInstance(uint area)
        {
            if (!GetLocalBool(area, "HOUSING_IS_INSTANCE")) return;

            var result = DestroyArea(area);
            var ownerPlayerUUID = GetLocalString(area, "HOUSING_OWNER_PLAYER_UUID");

            if (string.IsNullOrWhiteSpace(ownerPlayerUUID)) return;

            if (result == 1) // 1 = Successful deletion of the area
            {
                // Remove the area from the cache, if it exists.
                if (_activeHouseInstances.ContainsKey(ownerPlayerUUID))
                {
                    _activeHouseInstances.Remove(ownerPlayerUUID);
                }
            }
        }

        /// <summary>
        /// Locates an existing instance or creates a new instance of a player's home.
        /// </summary>
        /// <param name="ownerPlayerUUID"></param>
        /// <returns>The area instance.</returns>
        public static uint LoadPlayerHouse(string ownerPlayerUUID)
        {
            // Instance has already been loaded, just return it.
            if (_activeHouseInstances.ContainsKey(ownerPlayerUUID))
            {
                return _activeHouseInstances[ownerPlayerUUID];
            }

            // Create a new instance, load all furniture, and return the area object.
            var playerHouse = DB.Get<PlayerHouse>(ownerPlayerUUID);
            var detail = GetHouseTypeDetail(playerHouse.HouseType);
            var originalArea = Cache.GetAreaByResref(detail.AreaInstanceResref);
            var copy = CreateInstance(originalArea);
            SetLocalString(copy, "HOUSING_OWNER_PLAYER_UUID", ownerPlayerUUID);

            // Swap to custom name if available.
            if (!string.IsNullOrWhiteSpace(playerHouse.CustomName))
            {
                SetName(copy, playerHouse.CustomName);
            }
            // Otherwise use a generic "Owner's Property" name.
            else
            {
                var dbOwner = DB.Get<Player>(ownerPlayerUUID);
                var name = dbOwner.Name + "'s Property";
                SetName(copy, name);
            }

            var removedCount = 0;
            foreach(var (id, furniture) in playerHouse.Furnitures)
            {
                var furnitureDetail = _activeFurniture[furniture.FurnitureType];

                // In the event that a piece of furniture has been marked inactive after it was already placed, we need to remove it from the house.
                if (!furnitureDetail.IsActive)
                {
                    playerHouse.Furnitures.Remove(id);
                    removedCount++;
                    continue;
                }

                var position = new Vector3(furniture.X, furniture.Y, furniture.Z);
                var location = Location(copy, position, furniture.Orientation);

                var placeable = CreateObject(ObjectType.Placeable, furnitureDetail.Resref, location);
                SetLocalString(placeable, "HOUSING_FURNITURE_ID", id);
            }

            // Save any changes, if furniture was removed.
            if (removedCount > 0)
            {
                DB.Set(ownerPlayerUUID, playerHouse);
            }

            // Set the instance into cache and then return the area.
            _activeHouseInstances[ownerPlayerUUID] = copy;
            return copy;
        }

        /// <summary>
        /// Checks whether a player can place a furniture item inside of a property.
        /// Error messages will be sent to the player in the event they fail a check.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <param name="item">The item being placed</param>
        /// <returns>true if player can place the furniture, false otherwise</returns>
        public static bool CanPlaceFurniture(uint player, uint item)
        {
            var area = GetArea(player);

            // Ensure it's a furniture item.
            var furnitureTypeId = GetFurnitureTypeFromItem(item);
            if (furnitureTypeId == FurnitureType.Invalid) return false;

            // Ensure we're inside someone's house.
            var ownerPlayerUUID = GetLocalString(area, "HOUSING_OWNER_PLAYER_UUID");
            if (string.IsNullOrWhiteSpace(ownerPlayerUUID))
            {
                SendMessageToPC(player, "Furniture may only be placed inside properties.");
                return false;
            }

            // Ensure player has permissions
            var dbHouse = DB.Get<PlayerHouse>(ownerPlayerUUID);
            var playerId = GetObjectUUID(player);
            var permission = dbHouse.PlayerPermissions.ContainsKey(playerId)
                ? dbHouse.PlayerPermissions[playerId]
                : new PlayerHousePermission();

            if (!permission.CanPlaceFurniture)
            {
                SendMessageToPC(player, "You do not have permission to place furniture in this property.");
                return false;
            }

            // Too many items have been placed.
            var houseDetail = GetHouseTypeDetail(dbHouse.HouseType);
            if (dbHouse.Furnitures.Count >= houseDetail.FurnitureLimit)
            {
                SendMessageToPC(player, "You cannot place any more furniture inside this property.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Retrieves the furniture type from an item.
        /// Item's resref must start with 'furniture_' and end with 4 numbers.
        /// I.E: 'furniture_0004'
        /// Returns FurnitureType.Invalid on error.
        /// </summary>
        /// <param name="item">The item to retrieve from.</param>
        /// <returns>A furniture type associated with the item.</returns>
        public static FurnitureType GetFurnitureTypeFromItem(uint item)
        {
            var resref = GetResRef(item);
            if (!resref.StartsWith("furniture_")) return FurnitureType.Invalid;

            var id = resref.Substring(resref.Length-4, 4);

            if (!int.TryParse(id, out var furnitureId))
            {
                return FurnitureType.Invalid;
            }

            return (FurnitureType) furnitureId;
        }
    }
}
