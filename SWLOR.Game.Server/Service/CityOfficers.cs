using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// A city's officers: appointing and removing them, and keeping their Handle City Affairs permission in step with
    /// <see cref="WorldProperty.Officers"/>. The rules live in <see cref="OfficerRules"/>; this class reads and writes the records.
    /// </summary>
    public static class CityOfficers
    {
        /// <summary>The city's level, which sets how many officers it can have.</summary>
        public static int GetCityLevel(WorldProperty city)
        {
            return city.Upgrades.GetValueOrDefault(PropertyUpgradeType.CityLevel, 1);
        }

        /// <summary>Whether the player may decide the city's situations: the mayor, or an officer.</summary>
        public static bool CanHandle(WorldProperty city, string playerId)
        {
            if (city == null || string.IsNullOrWhiteSpace(playerId))
                return false;

            if (playerId == city.OwnerPlayerId)
                return true;

            var player = DB.Get<Player>(playerId);
            var isCitizen = player != null && ElectionRules.IsCitizenOf(player.CitizenPropertyId, city.Id, player.IsDeleted);
            var permission = GetPermission(city.Id, playerId);
            var holdsPermission = permission != null &&
                                  permission.Permissions.TryGetValue(PropertyPermissionType.HandleCityAffairs, out var granted) &&
                                  granted;

            return OfficerRules.CanHandleCityAffairs(playerId, city.OwnerPlayerId, isCitizen, city.Officers, holdsPermission);
        }

        /// <summary>The title the player acts under in City Affairs.</summary>
        public static string GetTitle(WorldProperty city, string playerId)
        {
            return OfficerRules.GetTitle(playerId, city.OwnerPlayerId, city.Officers);
        }

        /// <summary>
        /// Appoints a citizen as an officer with a title. Only the mayor may appoint. Returns the reason it was refused, or
        /// empty on success.
        /// </summary>
        public static string Appoint(string cityId, string appointerId, string targetId, string title)
        {
            var city = LoadCity(cityId);
            if (city == null)
                return "This city no longer exists.";

            var target = string.IsNullOrWhiteSpace(targetId) ? null : DB.Get<Player>(targetId);
            var isCitizen = target != null && ElectionRules.IsCitizenOf(target.CitizenPropertyId, city.Id, target.IsDeleted);
            var error = OfficerRules.CheckAppointment(
                appointerId,
                city.OwnerPlayerId,
                targetId,
                isCitizen,
                city.Officers.Keys.ToList(),
                GetCityLevel(city));
            if (!string.IsNullOrEmpty(error))
                return error;

            // The permission is written first, so a restart between the two writes never leaves an officer without it.
            SetHandlePermission(city.Id, targetId, true);
            city.Officers[targetId] = new CityOfficer
            {
                Title = OfficerRules.NormalizeTitle(title),
                AppointedUtc = DateTime.UtcNow
            };
            DB.Set(city);

            Log.Write(LogGroup.Property, $"Player '{appointerId}' appointed '{targetId}' as '{city.Officers[targetId].Title}' of city '{city.CustomName}' ({city.Id}).");
            return string.Empty;
        }

        /// <summary>Changes an officer's title. Only the mayor may. Returns the reason it was refused, or empty on success.</summary>
        public static string SetTitle(string cityId, string requesterId, string officerId, string title)
        {
            var city = LoadCity(cityId);
            if (city == null)
                return "This city no longer exists.";

            if (requesterId != city.OwnerPlayerId)
                return "Only the mayor can change an officer's title.";

            if (!city.Officers.TryGetValue(officerId ?? string.Empty, out var officer))
                return "That citizen is not an officer.";

            officer.Title = OfficerRules.NormalizeTitle(title);
            DB.Set(city);
            return string.Empty;
        }

        /// <summary>Ends an officer's appointment. Only the mayor may. Returns the reason it was refused, or empty on success.</summary>
        public static string Remove(string cityId, string requesterId, string officerId)
        {
            var city = LoadCity(cityId);
            if (city == null)
                return "This city no longer exists.";

            if (requesterId != city.OwnerPlayerId)
                return "Only the mayor can remove an officer.";

            if (!city.Officers.ContainsKey(officerId ?? string.Empty))
                return "That citizen is not an officer.";

            EndOffice(city, officerId);
            DB.Set(city);
            Log.Write(LogGroup.Property, $"Player '{requesterId}' removed officer '{officerId}' of city '{city.CustomName}' ({city.Id}).");
            return string.Empty;
        }

        /// <summary>
        /// Ends a player's office because they left the city or were deleted. Does nothing when they are not an officer.
        /// </summary>
        public static void OnCitizenLeft(string cityId, string playerId)
        {
            if (string.IsNullOrWhiteSpace(cityId) || string.IsNullOrWhiteSpace(playerId))
                return;

            var city = DB.Get<WorldProperty>(cityId);
            if (city == null || !city.Officers.ContainsKey(playerId))
                return;

            EndOffice(city, playerId);
            DB.Set(city);
        }

        /// <summary>
        /// Ends the appointments of officers who are no longer citizens of the city, such as a character that was deleted.
        /// The caller saves the city. Returns true if any ended.
        /// </summary>
        public static bool RemoveDepartedOfficers(WorldProperty city)
        {
            var departed = new List<string>();
            foreach (var officerId in city.Officers.Keys)
            {
                var player = DB.Get<Player>(officerId);
                if (player == null || !ElectionRules.IsCitizenOf(player.CitizenPropertyId, city.Id, player.IsDeleted))
                    departed.Add(officerId);
            }

            foreach (var officerId in departed)
            {
                EndOffice(city, officerId);
            }

            return departed.Count > 0;
        }

        /// <summary>
        /// Ends the newest appointments above the limit for the city's level. The caller saves the city. Returns true if
        /// any ended.
        /// </summary>
        public static bool EndAppointmentsAboveLimit(WorldProperty city)
        {
            var excess = OfficerRules.SelectAppointmentsAboveLimit(city.Officers, GetCityLevel(city));
            foreach (var officerId in excess)
            {
                EndOffice(city, officerId);
                Log.Write(LogGroup.Property, $"Officer '{officerId}' of city '{city.CustomName}' ({city.Id}) ended because the city dropped to level {GetCityLevel(city)}.");
            }

            return excess.Count > 0;
        }

        /// <summary>
        /// When a different player takes the seat, every officer appointment and every city permission granted under the old
        /// mayor ends. The new mayor grants their own. The caller saves the city.
        /// </summary>
        public static void EndAllOnNewMayor(WorldProperty city, string newMayorId)
        {
            city.Officers.Clear();

            var query = new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), city.Id, false);
            var count = (int)DB.SearchCount(query);
            if (count <= 0)
                return;

            foreach (var permission in DB.Search(query.AddPaging(count, 0)).ToList())
            {
                if (permission.PlayerId == newMayorId)
                    continue;

                DB.Delete<WorldPropertyPermission>(permission.Id);
            }
        }

        private static void EndOffice(WorldProperty city, string playerId)
        {
            SetHandlePermission(city.Id, playerId, false);
            city.Officers.Remove(playerId);
        }

        private static WorldProperty LoadCity(string cityId)
        {
            var city = string.IsNullOrWhiteSpace(cityId) ? null : DB.Get<WorldProperty>(cityId);
            return city != null && city.PropertyType == PropertyType.City && !city.IsQueuedForDeletion
                ? city
                : null;
        }

        private static WorldPropertyPermission GetPermission(string cityId, string playerId)
        {
            return DB.Search(new DBQuery<WorldPropertyPermission>()
                    .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false)
                    .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), cityId, false))
                .FirstOrDefault();
        }

        /// <summary>Gives or takes the Handle City Affairs permission. A player with no permission record gets one when it is given.</summary>
        private static void SetHandlePermission(string cityId, string playerId, bool granted)
        {
            var permission = GetPermission(cityId, playerId);
            if (permission == null)
            {
                if (!granted)
                    return;

                var all = Property.GetPermissionsByPropertyType(PropertyType.City);
                permission = new WorldPropertyPermission
                {
                    PropertyId = cityId,
                    PlayerId = playerId,
                    Permissions = all.ToDictionary(type => type, _ => false),
                    GrantPermissions = all.ToDictionary(type => type, _ => false)
                };
            }

            permission.Permissions[PropertyPermissionType.HandleCityAffairs] = granted;
            if (!permission.GrantPermissions.ContainsKey(PropertyPermissionType.HandleCityAffairs))
                permission.GrantPermissions[PropertyPermissionType.HandleCityAffairs] = false;

            DB.Set(permission);
        }
    }
}
