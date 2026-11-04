using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityGazetteService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Service.CityAffairsService
{
    /// <summary>
    /// Who may do what for a city (Player City Revamp, "Permissions"): a player's role, the powers each role holds, and how
    /// a granted permission adds to them. Pure, so every cell of the Plan's table can be tested.
    /// </summary>
    public static class CityPowerRules
    {
        /// <summary>The title an Acting Mayor or interim holder acts under in the Gazette.</summary>
        public const string ActingMayorTitle = "Acting Mayor";

        private static readonly PropertyPermissionType[] ManageCityPermissions =
        {
            PropertyPermissionType.RenameProperty,
            PropertyPermissionType.EditTaxes,
            PropertyPermissionType.AccessTreasury,
            PropertyPermissionType.ManageUpgrades,
            PropertyPermissionType.ManageUpkeep,
            PropertyPermissionType.ManageEdicts
        };

        #region Roles

        /// <summary>
        /// The role a player plays in a city right now. The mayor of record is a departed mayor once the interim holder
        /// has accepted; from then until the snap election ends the interim holder is the Acting Mayor. An Acting Mayor
        /// whose term has run out is an ordinary citizen, even before the next server start clears the record.
        /// </summary>
        public static CityRole GetRole(WorldProperty city, string playerId, DateTime nowUtc)
        {
            if (city == null)
                return CityRole.None;

            return GetRole(
                playerId,
                city.OwnerPlayerId,
                city.ActingMayorPlayerId,
                city.ActingMayorEndsUtc,
                city.StepDown,
                city.Officers?.Keys,
                nowUtc);
        }

        public static CityRole GetRole(
            string playerId,
            string mayorId,
            string actingMayorId,
            DateTime? actingMayorEndsUtc,
            CityStepDown stepDown,
            IEnumerable<string> officerIds,
            DateTime nowUtc)
        {
            if (string.IsNullOrWhiteSpace(playerId))
                return CityRole.None;

            var hasSteppedDown = StepDownRules.HasSteppedDown(stepDown);
            if (playerId == mayorId)
                return hasSteppedDown ? CityRole.DepartedMayor : CityRole.Mayor;

            if (hasSteppedDown)
            {
                if (playerId == stepDown.InterimHolderPlayerId)
                    return CityRole.ActingMayor;
            }
            else if (playerId == actingMayorId && ActingMayorRules.IsActive(actingMayorId, actingMayorEndsUtc, nowUtc))
            {
                return CityRole.ActingMayor;
            }

            if (officerIds != null)
            {
                foreach (var officerId in officerIds)
                {
                    if (officerId == playerId)
                        return CityRole.Officer;
                }
            }

            return CityRole.None;
        }

        #endregion

        #region Powers

        /// <summary>
        /// Whether the role holds the power by being that role, from the Plan's table. A granted permission is counted
        /// separately in <see cref="HasPower"/>.
        /// </summary>
        public static bool RoleHoldsPower(CityRole role, CityPower power)
        {
            switch (role)
            {
                case CityRole.Mayor:
                    return power != CityPower.Invalid;
                case CityRole.ActingMayor:
                    return power == CityPower.HandleCityAffairs ||
                           power == CityPower.EnactEdicts ||
                           power == CityPower.ChangeTaxes ||
                           power == CityPower.PayUpkeep ||
                           power == CityPower.StartProjects ||
                           power == CityPower.PostRequests;
                case CityRole.Officer:
                    return power == CityPower.HandleCityAffairs || power == CityPower.PostRequests;
                default:
                    return false;
            }
        }

        /// <summary>
        /// The city permission that gives a citizen the power, or null when no permission does. Only officers get Handle City
        /// Affairs, so the officer limit cannot be bypassed, and only the mayor appoints.
        /// </summary>
        public static PropertyPermissionType? GetPermissionFor(CityPower power)
        {
            switch (power)
            {
                case CityPower.EnactEdicts:
                case CityPower.RepealEdicts:
                    return PropertyPermissionType.ManageEdicts;
                case CityPower.ChangeTaxes:
                    return PropertyPermissionType.EditTaxes;
                case CityPower.WithdrawTreasury:
                    return PropertyPermissionType.AccessTreasury;
                case CityPower.PayUpkeep:
                    return PropertyPermissionType.ManageUpkeep;
                case CityPower.StartProjects:
                    return PropertyPermissionType.ManageUpgrades;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Whether a player has the power: the mayor has every power, a departed mayor none (their own permission record is
        /// ignored), and anyone else has what their role gives plus what a permission they hold gives.
        /// </summary>
        /// <param name="role">The player's role.</param>
        /// <param name="power">The power.</param>
        /// <param name="permissions">The player's permissions on the city, or null when they have no record.</param>
        public static bool HasPower(CityRole role, CityPower power, IReadOnlyDictionary<PropertyPermissionType, bool> permissions)
        {
            if (role == CityRole.DepartedMayor)
                return false;

            if (RoleHoldsPower(role, power))
                return true;

            var permission = GetPermissionFor(power);
            return permission.HasValue &&
                   permissions != null &&
                   permissions.TryGetValue(permission.Value, out var granted) &&
                   granted;
        }

        /// <summary>
        /// Whether the player may open the Manage City window: they hold one of its permissions, are an Acting Mayor, or
        /// are the interim holder who must answer a step-down. A departed mayor may not.
        /// </summary>
        public static bool CanOpenManageCity(
            CityRole role,
            IReadOnlyDictionary<PropertyPermissionType, bool> permissions,
            bool hasStepDownToAnswer)
        {
            if (role == CityRole.DepartedMayor)
                return false;

            if (role == CityRole.Mayor || role == CityRole.ActingMayor || hasStepDownToAnswer)
                return true;

            if (permissions == null)
                return false;

            foreach (var permission in ManageCityPermissions)
            {
                if (permissions.TryGetValue(permission, out var granted) && granted)
                    return true;
            }

            return false;
        }

        #endregion

        #region Titles

        /// <summary>
        /// The title an official acts under in a Gazette entry for a tax or an Edict: Mayor, Acting Mayor, or Delegate for
        /// anyone else who holds the permission.
        /// </summary>
        public static string GetDelegateTitle(CityRole role)
        {
            switch (role)
            {
                case CityRole.Mayor:
                    return CityGazetteRules.MayorTitle;
                case CityRole.ActingMayor:
                    return ActingMayorTitle;
                default:
                    return CityGazetteRules.DelegateTitle;
            }
        }

        #endregion
    }
}
