using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;

namespace SWLOR.Game.Server.Service.PlanetIdentityService
{
    /// <summary>
    /// The one extra check of the Player City Revamp's planet identity recipes ("Planet identity" and the station Identity row): the
    /// crafting station must stand in a city on the recipe's planet, or on a player station for a station recipe. A planet's signature
    /// dishes and drinks and its decor are ordinary recipes in every other way. The facts (the crafter's city and its planet) are looked
    /// up by <see cref="PlanetIdentity"/>; this class holds no database or engine calls.
    /// </summary>
    public static class PlanetIdentityRules
    {
        /// <summary>Whether a recipe that needs <paramref name="required"/> may be crafted where the crafter stands.</summary>
        public static IdentityRefusal Check(IdentityPlace required, CraftingPlace place)
        {
            if (!place.InCity)
                return IdentityRefusal.NotInCity;

            if (required.OnStation)
                return place.OnStation ? IdentityRefusal.None : IdentityRefusal.NotOnStation;

            if (place.OnStation)
                return IdentityRefusal.OnStation;

            return place.Planet == required.Planet ? IdentityRefusal.None : IdentityRefusal.WrongPlanet;
        }

        /// <summary>The line the recipe window shows among a recipe's requirements.</summary>
        public static string DescribeRequirement(IdentityPlace required)
        {
            return required.OnStation
                ? "Crafted on a player station."
                : $"Crafted in a city on {GetPlanetName(required.Planet)}.";
        }

        /// <summary>
        /// The reason a recipe cannot be crafted where the crafter stands, or an empty string when it can. It names what the recipe needs
        /// and where the crafter is, so the player knows where to go.
        /// </summary>
        public static string DescribeRefusal(IdentityPlace required, CraftingPlace place)
        {
            var refusal = Check(required, place);
            if (refusal == IdentityRefusal.None)
                return string.Empty;

            var needs = required.OnStation
                ? "This can only be crafted at a crafting station on a player station."
                : $"This can only be crafted at a crafting station in a city on {GetPlanetName(required.Planet)}.";

            switch (refusal)
            {
                case IdentityRefusal.NotInCity:
                    return $"{needs} You are not in a player city.";
                case IdentityRefusal.OnStation:
                    return $"{needs} You are on a player station.";
                case IdentityRefusal.WrongPlanet:
                case IdentityRefusal.NotOnStation:
                    return place.Planet == PlanetType.Invalid
                        ? $"{needs} This city is not on a planet the game recognizes."
                        : $"{needs} This city is on {GetPlanetName(place.Planet)}.";
                default:
                    return needs;
            }
        }

        /// <summary>The planet's name as players see it.</summary>
        public static string GetPlanetName(PlanetType planet)
        {
            return planet.GetAttribute<PlanetType, PlanetAttribute>().Name;
        }
    }
}
