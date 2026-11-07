using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.PlanetIdentityService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Planet identity (Player City Revamp): the engine side of the check that keeps a planet's signature dishes, drinks and decor to the
    /// cities on that planet. The rule itself is <see cref="PlanetIdentityRules"/>.
    /// </summary>
    public static class PlanetIdentity
    {
        /// <summary>Where a crafter stands: outside every city, in a city on a planet, or on a player station.</summary>
        public static CraftingPlace GetCraftingPlace(uint player)
        {
            return GetCraftingPlaceInArea(GetArea(player));
        }

        /// <summary>
        /// Where an area is. A building or house interior belongs to its city, so a Cookpot in a house counts as the city's.
        /// An apartment, a ship, or any public area is outside every city.
        /// </summary>
        public static CraftingPlace GetCraftingPlaceInArea(uint area)
        {
            var cityId = Property.GetCityIdForArea(area);
            if (string.IsNullOrWhiteSpace(cityId))
                return CraftingPlace.Outside;

            var city = DB.Get<WorldProperty>(cityId);
            if (city == null)
                return CraftingPlace.Outside;

            return Station.IsStation(city)
                ? CraftingPlace.Station
                : CraftingPlace.City(Station.GetCityPlanet(city));
        }
    }
}
