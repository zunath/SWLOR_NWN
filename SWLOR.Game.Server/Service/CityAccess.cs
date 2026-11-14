using System.Collections.Generic;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// What one player may do for one city right now, read once: their role, and the permissions on their record. Built by
    /// <see cref="CityPowers.Resolve"/>.
    /// </summary>
    public sealed class CityAccess
    {
        public CityAccess(CityRole role, IReadOnlyDictionary<PropertyPermissionType, bool> permissions, bool hasRecord)
        {
            Role = role;
            Permissions = permissions;
            HasRecord = hasRecord;
        }

        public CityRole Role { get; }

        /// <summary>The permissions on the player's record, or null when they have none.</summary>
        public IReadOnlyDictionary<PropertyPermissionType, bool> Permissions { get; }

        /// <summary>Whether the player has a permission record on the city, such as a delegate or the mayor.</summary>
        public bool HasRecord { get; }

        /// <summary>
        /// Whether the player has any standing in Manage City: a record, or the Acting Mayor's powers. A departed mayor
        /// has none, whatever their record says.
        /// </summary>
        public bool HasAccess =>
            Role != CityRole.DepartedMayor && (HasRecord || Role == CityRole.Mayor || Role == CityRole.ActingMayor);

        public bool Can(CityPower power)
        {
            return CityPowerRules.HasPower(Role, power, Permissions);
        }
    }
}
