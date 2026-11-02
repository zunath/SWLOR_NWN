using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.StationService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Service.CityAffairsService
{
    /// <summary>
    /// Planetary councils (Player City Revamp, "Planetary councils"): which council a city joins, who receives a summons, and
    /// what the notice says. Pure, so the station mapping and the wording can be tested.
    /// </summary>
    public static class PlanetaryCouncilRules
    {
        /// <summary>The longest text a DM can add to a summons.</summary>
        public const int MessageMaxLength = 300;

        /// <summary>How many days after it is posted a summons still shows at login for someone who has not read it.</summary>
        public const int NoticeDays = 14;

        /// <summary>
        /// The planet whose council a city joins. A planet city joins its own planet's. An orbit station joins its planet's.
        /// A Wild Space station joins its home port's planet, which the plan maps through the anchor's NPC exchange: Dathomir
        /// for Vesh Pulsar, Hutlar for every other system.
        /// </summary>
        /// <param name="isStation">The city is a player station.</param>
        /// <param name="isWildSpace">The station stands in a Wild Space system.</param>
        /// <param name="exchange">The anchor's NPC exchange, for a station.</param>
        /// <param name="orbitPlanet">The planet an orbit station's anchor serves, for a station.</param>
        /// <param name="cityPlanet">The planet a planet city stands on.</param>
        public static PlanetType GetCouncilPlanet(
            bool isStation,
            bool isWildSpace,
            TradeExchangeType exchange,
            PlanetType orbitPlanet,
            PlanetType cityPlanet)
        {
            if (!isStation)
                return cityPlanet;

            return isWildSpace ? GetPlanetOfExchange(exchange) : orbitPlanet;
        }

        /// <summary>The planet an NPC exchange stands on. An exchange has the same name as its planet.</summary>
        public static PlanetType GetPlanetOfExchange(TradeExchangeType exchange)
        {
            return Enum.TryParse<PlanetType>(exchange.ToString(), out var planet) && planet != PlanetType.Invalid
                ? planet
                : PlanetType.Invalid;
        }

        /// <summary>The council planet of a station anchor, or Invalid for an unknown anchor.</summary>
        public static PlanetType GetCouncilPlanetOfAnchor(StationAnchor anchor)
        {
            if (anchor == null)
                return PlanetType.Invalid;

            return GetCouncilPlanet(true, anchor.IsWildSpace, anchor.Exchange, anchor.HomePlanet, PlanetType.Invalid);
        }

        /// <summary>
        /// Who receives a summons for a city: the mayor of record, the officers, and whoever holds the Acting Mayor's powers.
        /// Each once.
        /// </summary>
        public static List<string> GetRecipients(WorldProperty city, DateTime nowUtc)
        {
            var recipients = new List<string>();
            void Add(string playerId)
            {
                if (!string.IsNullOrWhiteSpace(playerId) && !recipients.Contains(playerId))
                    recipients.Add(playerId);
            }

            Add(city.OwnerPlayerId);
            foreach (var officerId in city.Officers.Keys)
            {
                Add(officerId);
            }

            if (ActingMayorRules.IsActive(city.ActingMayorPlayerId, city.ActingMayorEndsUtc, nowUtc))
                Add(city.ActingMayorPlayerId);

            if (StepDownRules.HasSteppedDown(city.StepDown))
                Add(city.StepDown.InterimHolderPlayerId);

            return recipients;
        }

        /// <summary>Trims a DM's text and cuts it to <see cref="MessageMaxLength"/> characters.</summary>
        public static string NormalizeMessage(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var cleaned = new string(text.Where(character => !char.IsControl(character)).ToArray()).Trim();
            return cleaned.Length <= MessageMaxLength
                ? cleaned
                : cleaned.Substring(0, MessageMaxLength).TrimEnd();
        }

        /// <summary>The summons as the mayor and officers read it.</summary>
        public static string BuildSummonsText(string planetName, string dmMessage)
        {
            var text = $"A planetary council has been called for {planetName}: the mayors and officers of {planetName}'s cities " +
                       "are summoned to the planet's main settlement.";
            var extra = NormalizeMessage(dmMessage);
            return extra.Length == 0 ? text : $"{text} {extra}";
        }

        /// <summary>The Gazette text for a council a DM called.</summary>
        public static string BuildGazetteText(string planetName, string dmMessage)
        {
            return BuildSummonsText(planetName, dmMessage);
        }

        /// <summary>
        /// Whether a summons shows at login: it exists, the player has not read it, and it was posted within
        /// <see cref="NoticeDays"/> days.
        /// </summary>
        public static bool IsUnread(CityCouncilSummons summons, string seenSummonsId, DateTime nowUtc)
        {
            if (summons == null || string.IsNullOrWhiteSpace(summons.Id))
                return false;

            return summons.Id != seenSummonsId && nowUtc < summons.PostedUtc.AddDays(NoticeDays);
        }

        /// <summary>Whether the player is among those a summons is meant for.</summary>
        public static bool IsRecipient(WorldProperty city, string playerId, DateTime nowUtc)
        {
            return !string.IsNullOrWhiteSpace(playerId) && GetRecipients(city, nowUtc).Contains(playerId);
        }
    }
}
