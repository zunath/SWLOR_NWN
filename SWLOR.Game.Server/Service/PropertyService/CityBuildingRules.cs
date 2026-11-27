using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.StationService;

namespace SWLOR.Game.Server.Service.PropertyService
{
    /// <summary>
    /// Which city structures count against a city's building limit and which against its structure limit.
    /// </summary>
    public static class CityBuildingRules
    {
        /// <summary>
        /// A city-only structure is a building when it has an interior (City Hall, houses, the Starport) or is a station
        /// building (Shipyard, Asteroid Drill, Jump Beacon). Open-ground city structures such as the Dueling Ring, Market Stall,
        /// Corral and Traveler's Lodge have no interior, so they count against the structure limit.
        /// </summary>
        public static bool CountsAsBuilding(StructureType structureType)
        {
            var detail = structureType.GetAttribute<StructureType, StructureAttribute>();
            if (detail == null || detail.RestrictedPropertyTypes != PropertyType.City)
                return false;

            return detail.LayoutType != PropertyLayoutType.Invalid || StationBuildingRules.IsStationBuilding(structureType);
        }
    }
}
