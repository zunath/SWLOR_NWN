using System;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.EdictService;

namespace SWLOR.Game.Server.Service.ShuttleService
{
    /// <summary>
    /// The Home Port rules (Player City Revamp, "Home Port"): who may book a flight to a city's Starport from any shuttle
    /// terminal, and how the city's Starport level shortens it. Pure, so each rule can be tested; the facts (who the character is,
    /// which Edicts the city holds, the Starport's level) are looked up by <c>HomePorts</c>.
    /// </summary>
    public static class HomePortRules
    {
        /// <summary>The seconds each effective Starport level takes off an interplanetary flight.</summary>
        public const int SecondsPerStarportLevel = 30;

        /// <summary>The highest Starport level there is, so the most a flight loses is 150 seconds of its 300.</summary>
        public const int MaxStarportLevel = 5;

        public const string NoShuttlePassMessage = "You need a CZ-220 Shuttle Pass to book a flight home.";

        public const string EmbargoedMessage = "This city has closed its Starport to citizens of your city.";

        public const string NotEligibleMessage = "Only citizens of this city and of its ally can book a flight to its Starport.";

        public const string NoStarportMessage = "This city has no Starport to land at.";

        public const string SmugglersPassMessage = "You need a Smuggler's Pass to fly to Smuggler's Moon.";

        /// <summary>
        /// Whether a character may book a flight to the city's Starport: a citizen of the city, or a citizen of its ally, who owes no
        /// taxes, holds the CZ-220 Shuttle Pass, and whose city the host has no Embargo against. Open Gates does not make a
        /// visitor eligible: Home Port is for citizens and allies only.
        /// </summary>
        /// <param name="isEmbargoed">The host city has an Embargo in effect naming the character's city.</param>
        /// <param name="isCitizenHere">The character is a citizen of the host city.</param>
        /// <param name="isAlliedCitizen">The character is a citizen of a city the host city has an Alliance in effect with.</param>
        /// <param name="owedTaxes">The taxes the character owes their own city.</param>
        /// <param name="hasShuttlePass">The character holds the CZ-220 Shuttle Pass.</param>
        public static HomePortAccess Decide(bool isEmbargoed, bool isCitizenHere, bool isAlliedCitizen, int owedTaxes, bool hasShuttlePass)
        {
            if (!hasShuttlePass)
                return HomePortAccess.NoShuttlePass;

            var access = FacilityAccessRules.Decide(isEmbargoed, isCitizenHere, isAlliedCitizen, owedTaxes, openGatesInEffect: false);
            return access switch
            {
                FacilityAccessResult.Allowed => HomePortAccess.Allowed,
                FacilityAccessResult.Embargoed => HomePortAccess.Embargoed,
                FacilityAccessResult.OwesTaxes => HomePortAccess.OwesTaxes,
                _ => HomePortAccess.NotEligible
            };
        }

        /// <summary>The player-facing reason for a refusal, or an empty string for <see cref="HomePortAccess.Allowed"/>.</summary>
        public static string DescribeRefusal(HomePortAccess access, int owedTaxes)
        {
            return access switch
            {
                HomePortAccess.NoShuttlePass => NoShuttlePassMessage,
                HomePortAccess.Embargoed => EmbargoedMessage,
                HomePortAccess.OwesTaxes => $"You owe {owedTaxes} credits in taxes to your city. You cannot book a flight home until these are paid.",
                HomePortAccess.NotEligible => NotEligibleMessage,
                _ => string.Empty
            };
        }

        /// <summary>The seconds an effective Starport level takes off an interplanetary flight. Levels outside 0 to 5 count as the nearest.</summary>
        public static int GetStarportReductionSeconds(int starportLevel)
        {
            return SecondsPerStarportLevel * Math.Clamp(starportLevel, 0, MaxStarportLevel);
        }

        /// <summary>
        /// The flight time of the interplanetary leg that ends at the city's Starport: <see cref="GalaxyMap"/>'s 300 seconds minus
        /// 30 per effective Starport level, once per trip. A 60-second orbital hop is unchanged, since only interplanetary legs
        /// shorten.
        /// </summary>
        public static int GetInterplanetarySeconds(PlanetType origin, PlanetType destination, int starportLevel)
        {
            var seconds = GalaxyMap.GetTransitSeconds(origin, destination);
            return GalaxyMap.IsOrbitalHop(origin, destination)
                ? seconds
                : seconds - GetStarportReductionSeconds(starportLevel);
        }
    }
}
