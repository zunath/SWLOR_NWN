using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityStaffService;
using SWLOR.Game.Server.Service.CityStoreService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The staff of a city's buildings (Player City Revamp, "Living cities"): a clerk in City Hall, a bartender in the
    /// Cantina, a medic in the Medical Center and a teller in the Bank, each at a fixed waypoint inside the building's
    /// template, so placement never depends on how the mayor laid the city out. They are created with each building
    /// instance and cannot be harmed. The Starport's dockmaster is the dockhand its layout already spawns. The rules
    /// live in <see cref="CityStaffRules"/>.
    /// </summary>
    public static class CityStaff
    {
        private const string RoleVariable = "CITY_STAFF_ROLE";
        private const string CityIdVariable = "CITY_STAFF_CITY_ID";
        private const string StaffVariable = "CITY_STAFF_NPC";

        /// <summary>The role of a staff member, or Invalid for anything else.</summary>
        public static CityStaffRole GetRole(uint npc)
        {
            return GetIsObjectValid(npc) ? (CityStaffRole)GetLocalInt(npc, RoleVariable) : CityStaffRole.Invalid;
        }

        /// <summary>The city a staff member works for, or an empty string.</summary>
        public static string GetCityId(uint npc)
        {
            return GetIsObjectValid(npc) ? GetLocalString(npc, CityIdVariable) : string.Empty;
        }

        /// <summary>
        /// Creates the staff member of a building instance at each of its staff waypoints, with its store beside it. A
        /// waypoint that already has its staff member is left alone.
        /// </summary>
        /// <param name="area">The building instance.</param>
        /// <param name="role">The building's staff role.</param>
        public static void Spawn(uint area, CityStaffRole role)
        {
            var city = GetCity(area);
            if (city == null)
                return;

            var blueprint = CityStaffRules.GetBlueprint(role);
            var storeKind = CityStaffRules.GetStoreKind(role);

            foreach (var waypoint in GetStaffWaypoints(area))
            {
                if (GetIsObjectValid(GetLocalObject(waypoint, StaffVariable)))
                    continue;

                var location = GetLocation(waypoint);
                var npc = CreateObject(ObjectType.Creature, blueprint, location);
                if (!GetIsObjectValid(npc))
                {
                    Log.Write(LogGroup.Error, $"City staff '{blueprint}' ({role}) of city '{city.CustomName}' ({city.Id}) could not be created. Check that the blueprint is in the module.", true);
                    continue;
                }

                SetPlotFlag(npc, true);
                SetLocalInt(npc, RoleVariable, (int)role);
                SetLocalString(npc, CityIdVariable, city.Id);
                SetLocalObject(waypoint, StaffVariable, npc);

                // The teller opens the bank, so it carries the city a bank terminal would.
                if (role == CityStaffRole.Teller)
                    Bank.SetCityBankId(npc, city.Id);

                if (storeKind != CityStoreKind.Invalid)
                    CityStores.Create(city, storeKind, location);
            }
        }

        /// <summary>The city a building instance belongs to, or null.</summary>
        public static WorldProperty GetCity(uint area)
        {
            var propertyId = Property.GetPropertyId(area);
            var dbProperty = string.IsNullOrWhiteSpace(propertyId) ? null : DB.Get<WorldProperty>(propertyId);
            var dbBuilding = dbProperty == null ? null : DB.Get<WorldProperty>(dbProperty.ParentPropertyId);

            return dbBuilding == null ? null : DB.Get<WorldProperty>(dbBuilding.ParentPropertyId);
        }

        private static List<uint> GetStaffWaypoints(uint area)
        {
            var waypoints = new List<uint>();
            for (var obj = GetFirstObjectInArea(area); GetIsObjectValid(obj); obj = GetNextObjectInArea(area))
            {
                if (GetTag(obj) == CityStaffRules.SpawnWaypointTag)
                    waypoints.Add(obj);
            }

            return waypoints;
        }
    }
}
