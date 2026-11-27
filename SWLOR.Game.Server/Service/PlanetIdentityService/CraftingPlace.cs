using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Service.PlanetIdentityService
{
    /// <summary>
    /// Where a crafter stands, as far as planet identity cares: outside any city, in a city on a planet, or on a player station.
    /// </summary>
    /// <param name="InCity">The crafter is on a city's land or inside one of its buildings.</param>
    /// <param name="OnStation">The city is a player station.</param>
    /// <param name="Planet">The planet a planet city is on. <see cref="PlanetType.Invalid"/> for a station or outside any city.</param>
    public readonly record struct CraftingPlace(bool InCity, bool OnStation, PlanetType Planet)
    {
        /// <summary>An apartment, a ship, a public area, or anywhere else outside a player city.</summary>
        public static CraftingPlace Outside => new(false, false, PlanetType.Invalid);

        /// <summary>A player station.</summary>
        public static CraftingPlace Station => new(true, true, PlanetType.Invalid);

        /// <summary>A city on a planet.</summary>
        public static CraftingPlace City(PlanetType planet) => new(true, false, planet);
    }
}
