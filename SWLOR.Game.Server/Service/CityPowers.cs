using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Who may do what for a city (Player City Revamp, "Permissions"). Every Manage City and City Affairs action asks here,
    /// so the mayor, an Acting Mayor, a departed mayor, officers, and permission holders are told apart in one place. The
    /// rules live in <see cref="CityPowerRules"/>; this class reads the city and the player's permission record.
    /// </summary>
    public static class CityPowers
    {
        /// <summary>The player's role in the city right now.</summary>
        public static CityRole GetRole(WorldProperty city, string playerId)
        {
            return CityPowerRules.GetRole(city, playerId, DateTime.UtcNow);
        }

        /// <summary>Whether the player is the sitting mayor. A mayor who stepped down is not.</summary>
        public static bool IsMayor(WorldProperty city, string playerId)
        {
            return GetRole(city, playerId) == CityRole.Mayor;
        }

        /// <summary>Whether the player is the mayor of record who stepped down and has no powers until the snap election ends.</summary>
        public static bool IsDepartedMayor(WorldProperty city, string playerId)
        {
            return GetRole(city, playerId) == CityRole.DepartedMayor;
        }

        /// <summary>Reads the player's role and permissions on the city once.</summary>
        public static CityAccess Resolve(WorldProperty city, string playerId)
        {
            var role = GetRole(city, playerId);
            var record = city == null || string.IsNullOrWhiteSpace(playerId) ? null : GetPermissionRecord(city.Id, playerId);
            return new CityAccess(role, record?.Permissions, record != null);
        }

        /// <summary>Whether the player has the power: by their role, or by a permission they hold.</summary>
        public static bool Can(WorldProperty city, string playerId, CityPower power)
        {
            return Resolve(city, playerId).Can(power);
        }

        /// <summary>
        /// Whether the player may open the Manage City window at City Hall: they hold one of its permissions, are an Acting
        /// Mayor, or must answer a step-down. A departed mayor may not.
        /// </summary>
        public static bool CanOpenManageCity(WorldProperty city, string playerId)
        {
            var access = Resolve(city, playerId);
            var hasStepDownToAnswer = StepDownRules.IsWaitingForAnswerFrom(city.StepDown, playerId, DateTime.UtcNow);
            return CityPowerRules.CanOpenManageCity(access.Role, access.Permissions, hasStepDownToAnswer);
        }

        /// <summary>The title the player acts under in a tax or Edict Gazette entry: Mayor, Acting Mayor, or Delegate.</summary>
        public static string GetDelegateTitle(WorldProperty city, string playerId)
        {
            return CityPowerRules.GetDelegateTitle(GetRole(city, playerId));
        }

        /// <summary>The player's permission record on the city, or null.</summary>
        public static WorldPropertyPermission GetPermissionRecord(string cityId, string playerId)
        {
            return DB.Search(new DBQuery<WorldPropertyPermission>()
                    .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false)
                    .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), cityId, false))
                .FirstOrDefault();
        }
    }
}
