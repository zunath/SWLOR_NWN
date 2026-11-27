using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Service.PlanetIdentityService
{
    /// <summary>
    /// Where a planet identity recipe may be crafted: at a crafting station inside a city on one planet, or inside a player station.
    /// </summary>
    /// <param name="Planet">The planet the city must be on. <see cref="PlanetType.Invalid"/> for a station.</param>
    /// <param name="OnStation">True when the recipe is crafted on a player station instead of on a planet.</param>
    public readonly record struct IdentityPlace(PlanetType Planet, bool OnStation)
    {
        /// <summary>A city on this planet.</summary>
        public static IdentityPlace ForPlanet(PlanetType planet)
        {
            if (planet == PlanetType.Invalid)
                throw new ArgumentException("A planet identity recipe needs a planet.", nameof(planet));

            return new IdentityPlace(planet, false);
        }

        /// <summary>A player station.</summary>
        public static IdentityPlace ForStation() => new(PlanetType.Invalid, true);
    }
}
