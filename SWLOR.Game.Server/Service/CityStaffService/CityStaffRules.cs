using SWLOR.Game.Server.Service.CityStoreService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Service.CityStaffService
{
    /// <summary>
    /// What each member of a city building's staff is: which building they stand in, which blueprint they are made from,
    /// and which store they open. Pure, so each rule can be tested.
    /// </summary>
    public static class CityStaffRules
    {
        /// <summary>The tag of the waypoint inside each building template where its staff member stands.</summary>
        public const string SpawnWaypointTag = "CITY_STAFF_SPAWN";

        /// <summary>The creature blueprint a staff member is made from.</summary>
        public static string GetBlueprint(CityStaffRole role)
        {
            return role switch
            {
                CityStaffRole.Clerk => "cs_clerk",
                CityStaffRole.Bartender => "cs_bartender",
                CityStaffRole.Medic => "cs_medic",
                CityStaffRole.Teller => "cs_teller",
                _ => string.Empty
            };
        }

        /// <summary>The kind of building a role works in.</summary>
        public static PropertyType GetBuilding(CityStaffRole role)
        {
            return role switch
            {
                CityStaffRole.Clerk => PropertyType.CityHall,
                CityStaffRole.Bartender => PropertyType.Cantina,
                CityStaffRole.Medic => PropertyType.MedicalCenter,
                CityStaffRole.Teller => PropertyType.Bank,
                _ => PropertyType.Invalid
            };
        }

        /// <summary>The store a role opens, or Invalid for a role with none (the teller opens the bank).</summary>
        public static CityStoreKind GetStoreKind(CityStaffRole role)
        {
            return role switch
            {
                CityStaffRole.Clerk => CityStoreKind.CityHall,
                CityStaffRole.Bartender => CityStoreKind.Cantina,
                CityStaffRole.Medic => CityStoreKind.MedicalCenter,
                _ => CityStoreKind.Invalid
            };
        }

        /// <summary>What a player reads when the staff member speaks first.</summary>
        public static string GetGreeting(CityStaffRole role, string cityName)
        {
            var city = string.IsNullOrWhiteSpace(cityName) ? "the city" : cityName.Trim();
            return role switch
            {
                CityStaffRole.Clerk => $"Welcome to {city} City Hall. The citizenship and city management terminals are along the walls. Can I help you with anything else?",
                CityStaffRole.Bartender => $"What can I pour for you? Everything at the bar is from {city}'s own stock.",
                CityStaffRole.Medic => $"The medical center of {city} is open to everyone. What do you need?",
                CityStaffRole.Teller => $"Welcome to the {city} bank. Your account is yours, wherever you left it.",
                _ => "Hello."
            };
        }

        /// <summary>The line of the response that opens the role's service.</summary>
        public static string GetServiceResponse(CityStaffRole role)
        {
            return role switch
            {
                CityStaffRole.Clerk => "Browse the supplies",
                CityStaffRole.Bartender => "Browse the bar",
                CityStaffRole.Medic => "Browse the medical supplies",
                CityStaffRole.Teller => "Open my bank account",
                _ => string.Empty
            };
        }
    }
}
